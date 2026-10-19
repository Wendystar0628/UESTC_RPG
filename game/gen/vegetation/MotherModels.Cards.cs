using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.Vegetation;

public static partial class MotherModels
{
    /// <summary>
    /// One leaf card; Owner is the plan mass it sits on (that mass is never pruned while the card is drawn). Cards of one
    /// cluster share Cluster, the first card's index (their importance rank), H2 (they shed and fold away together) and
    /// the winter anchor; H3 stays per card (bloom and the card's own leaf shape).
    /// </summary>
    private sealed record Card(Vector3 Center, Vector3 Normal, Vector3 U, Vector3 V, float Width, float Height,
        float H2, float H3, Vector3 Anchor, Vector3 BlobCenter, int Owner, int Cluster);

    // ---------------------------------------------------------------------------------------------------------
    // Cards (WLD-VEGETATION, max-011 reviews): cards come in clusters of 3–5. A cluster's centre sits on the
    // outermost crown mass along a ray aimed at the crown shell, pressed into it by at least 30% of the card radius,
    // and its cards overlap around that centre facing the shell normal, so each clump grows out of the outline and none
    // hovers alone. veg_card keeps them at full size only on the silhouette ring. Cluster directions follow a Halton
    // sequence, so every prefix of whole clusters (the lower levels) still covers the whole crown evenly.
    // ---------------------------------------------------------------------------------------------------------

    private static List<Card> PlaceCards(TreePlan plan, CrownShell shell, int total)
    {
        var cards = new List<Card>(total + ClusterMax);
        var bumps = plan.Blobs.Select((b, i) => Bumps(plan, b, i)).ToArray();
        for (var i = 1; cards.Count < total && i <= total * 4; i++)
        {
            var z = 1f - 2f * Halton(i, 2);
            var phi = 6.2831853f * Halton(i, 3);
            var s = MathF.Sqrt(MathF.Max(0f, 1f - z * z));
            var d = new Vector3(s * MathF.Cos(phi), z, s * MathF.Sin(phi));
            // The underside carries half the density: it is seen less and holds the hanging fringe.
            if (d.Y < -0.5f && (i & 1) == 0) continue;
            var target = shell.EllipsoidPoint(d);
            var origin = shell.Center + (target - shell.Center) * 2f;
            if (!OuterHit(plan, bumps, origin, shell.Center - origin, out var hit, out var owner)) continue;
            AddCluster(cards, plan, shell, hit, owner);
        }
        return cards;
    }

    /// <summary>
    /// The crown-underside layer (<see cref="UnderClearance"/>): rays straight up from below the crown over a Halton
    /// spread of its footprint meet the lowest mass; where the shell faces down there, a cluster is pressed in as on the
    /// outline. Cluster indices count within the layer, so every prefix of whole clusters covers the underside evenly.
    /// </summary>
    private static List<Card> PlaceUnderCards(TreePlan plan, CrownShell shell)
    {
        var cards = new List<Card>();
        if (plan.CrownCenter.Y - plan.CrownRy < UnderClearance || plan.Blobs.Count == 0) return cards;
        var bumps = plan.Blobs.Select((b, i) => Bumps(plan, b, i)).ToArray();
        var (aspect, spread) = CardAspect(plan);
        var low = plan.Blobs.Min(b => b.Center.Y - b.Radius) - 1f;
        var reach = plan.Blobs.Max(b => Vector2.Distance(new(b.Center.X, b.Center.Z), new(shell.Center.X, shell.Center.Z)) + b.Radius);
        var size = MathF.Min(plan.Form.CardSize, CardSizeOfDiameter * shell.Diameter / spread);
        var clusterArea = size * size * aspect * (ClusterMin + ClusterMax) * 0.5f * 0.5f;
        var target = Math.Min(UnderClustersMax, (int)MathF.Round(UnderCover * MathF.PI * reach * reach / clusterArea));
        var clusters = 0;
        for (var i = 1; clusters < target && i <= target * 6; i++)
        {
            var r = reach * MathF.Sqrt(Halton(i, 2));
            var phi = 6.2831853f * Halton(i, 3);
            var origin = new Vector3(shell.Center.X + r * MathF.Cos(phi), low, shell.Center.Z + r * MathF.Sin(phi));
            if (!OuterHit(plan, bumps, origin, Vector3.UnitY, out var hit, out var owner)) continue;
            if (shell.Normal(hit).Y > UnderFacing) continue;
            var start = cards.Count;
            AddCluster(cards, plan, shell, hit, owner, UnderCardScale);
            // Renumber the cluster within the layer (the rank veg_card folds the layer by).
            for (var k = start; k < cards.Count; k++) cards[k] = cards[k] with { Cluster = start };
            clusters++;
        }
        return cards;
    }

    private static (float Aspect, float Spread) CardAspect(TreePlan plan)
    {
        var aspect = plan.Species.Card == CardShape.Strand ? 1.8f : 1f;
        return (aspect, 0.5f * MathF.Sqrt(1f + aspect * aspect));
    }

    /// <summary>
    /// One cluster of 3–5 cards at a mass hit. The cluster centre is pressed into the owning mass along its radial
    /// direction at the hit (so the clump stays in that mass even where the shell normal grazes it), measured from the
    /// drawn faceted mass, which lies up to FacetSag of the mass radius inside the smooth lump. A card corner then reaches
    /// at most (1 − press) card radii out, well inside the 5% band; the cards themselves face the shell.
    /// </summary>
    private static void AddCluster(List<Card> cards, TreePlan plan, CrownShell shell, Vector3 hit, int owner, float scale = 1f)
    {
        var (aspect, spread) = CardAspect(plan);
        var bottom = plan.CrownCenter.Y - plan.CrownRy;
        var size = scale * MathF.Min(plan.Form.CardSize * plan.Range(0.85f, 1.2f), CardSizeOfDiameter * shell.Diameter / spread);
        var radius = spread * size;
        var blob = plan.Blobs[owner];
        var m = Vector3.Normalize(hit - blob.Center + new Vector3(0f, 1e-6f, 0f));
        var clusterCenter = hit - m * (radius * plan.Range(ClusterPressMin, ClusterPressMax) + blob.Radius * FacetSag);
        var anchor = blob.Anchor + plan.RandomDirection() * plan.Range(0f, 0.3f);
        var h2 = plan.Next();
        var cluster = cards.Count;
        var members = plan.Count(ClusterMin, ClusterMax);
        var tangent = Vector3.Normalize(Vector3.Cross(MathF.Abs(m.Y) < 0.95f ? Vector3.UnitY : Vector3.UnitX, m));
        var bitangent = Vector3.Cross(m, tangent);
        var turn = plan.Next() * 6.2831853f;
        for (var k = 0; k < members; k++)
        {
            // Members ring the centre (the first one on it), never further out than the centre itself.
            var a = turn + k * 6.2831853f / members;
            var r = k == 0 ? 0f : size * ClusterSpread * plan.Range(0.6f, 1f);
            var center = clusterCenter + (tangent * MathF.Cos(a) + bitangent * MathF.Sin(a)) * r
                - m * (radius * plan.Range(0f, 0.15f) + r * r / MathF.Max(0.2f, 2f * blob.Radius));
            var cn = shell.Normal(center);
            var droop = plan.Form.Droop;
            if (plan.Species.Crown == CrownType.Cloud && center.Y < bottom + plan.CrownRy * 0.6f) droop = MathF.Max(droop, 0.7f);
            var (u, v) = Frame(cn, plan.Next() * 6.2831853f, droop);
            var card = size * plan.Range(0.88f, 1.08f);
            cards.Add(new Card(center, cn, u, v, card, card * aspect, h2, plan.Next(), anchor, blob.Center, owner, cluster));
        }
    }

    /// <summary>First crown mass met by a ray (the outermost one seen from outside) and the lumpy surface point there.</summary>
    private static bool OuterHit(TreePlan plan, Vector4[][] bumps, Vector3 origin, Vector3 direction, out Vector3 point, out int owner)
    {
        var dir = Vector3.Normalize(direction);
        var best = float.MaxValue;
        owner = -1;
        for (var i = 0; i < plan.Blobs.Count; i++)
        {
            var b = plan.Blobs[i];
            var r = b.Radius * (1f + b.Lump * 0.3f);
            var oc = origin - b.Center;
            var half = Vector3.Dot(oc, dir);
            var disc = half * half - (oc.LengthSquared() - r * r);
            if (disc < 0f) continue;
            var t = -half - MathF.Sqrt(disc);
            if (t > 0f && t < best) (best, owner) = (t, i);
        }
        point = default;
        if (owner < 0) return false;
        var o = plan.Blobs[owner];
        var hit = origin + dir * best;
        var u = Vector3.Normalize(hit - o.Center + new Vector3(0f, 1e-6f, 0f));
        point = Surface(o.Center, o.Radius, o.Lump, bumps[owner], u);
        return true;
    }

    private static float Halton(int index, int radix)
    {
        var f = 1f;
        var r = 0f;
        for (var i = index; i > 0; i /= radix)
        {
            f /= radix;
            r += f * (i % radix);
        }
        return r;
    }

    /// <summary>Fan-palm fronds radiating from the crown head, upper ones rising, lower ones hanging.</summary>
    private static List<Card> PalmFronds(TreePlan plan, int count)
    {
        var head = plan.Blobs[0];
        var cards = new List<Card>(count);
        for (var i = 0; i < count; i++)
        {
            var f = (i + 0.5f) / count;
            var azimuth = i * 2.3999632f + plan.Range(-0.2f, 0.2f);
            var pitch = (0.75f - 1.35f * f) + plan.Range(-0.12f, 0.12f);
            var along = Vector3.Normalize(TreePlan.Azimuth(azimuth) * MathF.Cos(pitch) + Vector3.UnitY * MathF.Sin(pitch));
            var size = plan.Form.CardSize * plan.Range(0.85f, 1.1f);
            var side = Vector3.Normalize(Vector3.Cross(Vector3.UnitY, along) + new Vector3(0f, 0f, 1e-4f));
            var normal = Vector3.Normalize(Vector3.Cross(side, along));
            var center = head.Center + along * (size * 0.5f + head.Radius * 0.6f);
            // Fronds keep their own plane normal: veg_card turns each frond about it.
            cards.Add(new Card(center, normal, side, along, size, size, plan.Next(), plan.Next(), head.Center, head.Center, 0, i));
        }
        return cards;
    }

    /// <summary>
    /// In-plane axes of a card facing d, rotated by psi; droop turns the base→tip axis V towards hanging.
    /// The base basis (T0 = Y × d, B0 = d × T0) matches veg_card, which re-aims the card at the camera.
    /// </summary>
    private static (Vector3 U, Vector3 V) Frame(Vector3 d, float psi, float droop)
    {
        var t0 = Vector3.Cross(Vector3.UnitY, d);
        t0 = t0.LengthSquared() > 1e-6f ? Vector3.Normalize(t0) : Vector3.UnitX;
        var b0 = Vector3.Cross(d, t0);
        var c = MathF.Cos(psi);
        var s = MathF.Sin(psi);
        var u = t0 * c + b0 * s;
        var v = Vector3.Cross(d, u);
        if (droop > 0f)
        {
            var down = -Vector3.UnitY - d * Vector3.Dot(-Vector3.UnitY, d);
            if (down.LengthSquared() > 1e-4f)
            {
                v = Vector3.Normalize(Vector3.Lerp(v, Vector3.Normalize(down), droop));
                u = Vector3.Normalize(Vector3.Cross(v, d));
            }
        }
        return (u, v);
    }

    /// <summary>
    /// Rank of an outline card in the level bands. Palm fronds are few and all needed at every level, so they never shrink.
    /// A cluster shares the rank of its first card, so the level bands fold whole clusters away.
    /// </summary>
    private static float Importance(TreePlan plan, Card card) =>
        plan.Form.Id == "palm" ? 0f : card.Cluster / (float)Math.Max(1, CardCapacity(plan));

    private static void WriteCard(MeshWriter w, TreePlan plan, CrownShell shell, Card card, Ids ids, float importance)
    {
        var first = w.VertexCount;
        var t0 = Gradient(plan, card.Center, plan.Form.Id == "palm" ? card.Normal : shell.Normal(card.Center));
        // Needle species still take their foliage colour from the seasonal row. Their card colour ID carries the
        // reviewed winter-outline colour through the existing vertex layout; near twig strokes keep tree.bare.
        var color = plan.Species.Deciduous && plan.Species.Card == CardShape.Needle ? ids.WinterTwig : ids.Leaf;
        ReadOnlySpan<Vector2> corners = [new(0f, 0f), new(1f, 0f), new(1f, 1f), new(0f, 1f)];
        foreach (var uv in corners)
        {
            var p = card.Center + card.U * ((uv.X - 0.5f) * card.Width) + card.V * ((uv.Y - 0.5f) * card.Height);
            w.Add(p, card.Normal, 1f, color, t0, new Vector4(card.Center, card.H2), new Vector4(card.Anchor, card.H3),
                new Vector4(card.BlobCenter, importance), uv);
        }
        w.Tri(first, first + 2, first + 1);
        w.Tri(first, first + 3, first + 2);
        w.Cards++;
    }

    private static int CardCapacity(TreePlan plan) => plan.Form.Id == "palm" ? 20 : CardCount(plan.Species.Crown, VegLevel.L0Plus);

    /// <summary>Crown-gradient t0 = 0.36 + 0.26·n.y + 0.30·(height in crown − 0.5) + 0.12·outward (WLD-VEGETATION).</summary>
    private static byte Gradient(TreePlan plan, Vector3 p, Vector3 n)
    {
        var h = Math.Clamp((p.Y - (plan.CrownCenter.Y - plan.CrownRy)) / (2f * plan.CrownRy), 0f, 1f);
        var outward = Math.Clamp(Vector2.Distance(new(p.X, p.Z), new(plan.CrownCenter.X, plan.CrownCenter.Z)) / MathF.Max(0.1f, plan.CrownRx), 0f, 1f);
        var t0 = Math.Clamp(0.36f + 0.26f * n.Y + 0.30f * (h - 0.5f) + 0.12f * outward, 0f, 1f);
        return (byte)MathF.Round(t0 * 255f);
    }

    private static Box3 Union(Box3 a, Box3 b) => new(Vector3.Min(a.Min, b.Min), Vector3.Max(a.Max, b.Max));
}
