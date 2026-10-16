using System.Globalization;
using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.Vegetation;

/// <summary>Near-pool levels (ENG-PERF §9): L0+ 0–15 m, L0 15–40 m, L1 40–150 m on a 12 m tree.</summary>
public enum VegLevel : byte { L0Plus = 0, L0 = 1, L1 = 2 }

/// <summary>
/// One mother model at one level: the wood surface (trunk and branches, veg_trunk) and the crown surface (leaf cards
/// first, then the crown masses, all two-sided, veg_card; max-011 review). The crown is one material, so the outline
/// pass only draws its outer contour; every crown vertex carries the normal of one smooth shell (<see cref="CrownShell"/>),
/// so the crown shades as one lit side and one shadow band. Lower levels' cards are a prefix of higher levels' (CUSTOM2.w
/// is the importance rank). TrunkRadius at breast height and BranchHeight feed the trunk collider (WLD-VEGETATION §10).
/// </summary>
public sealed record MotherModel(
    string Species,
    int Variant,
    VegLevel Level,
    GeometryBundle Wood,
    GeometryBundle Crown,
    float Height,
    float TrunkRadius,
    float BranchHeight,
    float CrownRadius,
    Box3 Bounds)
{
    public int Triangles => (Wood.Index.Length + Crown.Index.Length) / 3;

    /// <summary>Leaf cards at the front of <see cref="Crown"/> (4 vertices and 2 triangles each); the masses follow.</summary>
    public int Cards { get; init; }

    /// <summary>Horizontal diameter of the crown shell, the yardstick of the 5% card rule.</summary>
    public float CrownDiameter { get; init; }

    /// <summary>Branch ends the winter twig strokes hang from (WLD-VEGETATION §1.8); also the song-post candidates (§12).</summary>
    public IReadOnlyList<Vector3> BranchTips { get; init; } = [];
}

/// <summary>
/// Mother-model generator (WLD-VEGETATION §1, §11.2; ENG-PERF §9). Pure and deterministic: the same species,
/// variant and level always give the same bytes. Vertex channels:
/// COLOR = (lit colour ID, AO, flags, spare) where spare is the crown-gradient t0 on crown vertices (§1.3) and the
/// whitewash colour ID on trunk vertices; CUSTOM0 = (card centre | mass centre, h2 | mass hash; −1 marks wood);
/// CUSTOM1 = (branch-tip anchor, h3 bloom hash); CUSTOM2 = (mass or ball centre, card importance rank in [0, 1) or −1 on
/// mass vertices, which never shrink with the level bands). NORMAL on the crown is the shell normal; on cards it is the
/// shell normal at the card centre (palm fronds: the frond's own normal).
/// </summary>
public static class MotherModels
{
    /// <summary>Trunks start this far below the local ground (WLD-TERRAIN §4.4: HeightAt minus burial).</summary>
    public const float Burial = 0.3f;

    /// <summary>Bark: warm grey-brown, low chroma (max-011 review; colour IDs 252–253, ext/WP-23.json).</summary>
    public static readonly PaletteToken Bark = new("tree.bark.lit");
    public static readonly PaletteToken BarkShade = new("tree.bark.shade");

    /// <summary>Winter twig strokes keep the bare-branch name (WLD-VEGETATION §1.8); packed into the species table.</summary>
    public static readonly PaletteToken Twig = new("tree.bare.lit");

    /// <summary>Lime-washed lower trunk (WLD-VEGETATION §1.7), switched per instance by the variant bit in veg_trunk.</summary>
    public static readonly PaletteToken Whitewash = new("wall.greyWhite.lit");

    /// <summary>
    /// Card budget per crown type at L0+, L0 and L1 (ENG-PERF §9); lower levels take the top-ranked prefix. Every type keeps
    /// the same fractions (L0 ≈ 40%, L1 ≈ 5.6% of L0+), because veg_card shrinks the extra cards by those two fractions
    /// (VEG_KEEP_L0, VEG_KEEP_L1) before the level swaps.
    /// </summary>
    public static int CardCount(CrownType crown, VegLevel level) => (crown, level) switch
    {
        (CrownType.Card, VegLevel.L0Plus) => 1800,
        (CrownType.Card, VegLevel.L0) => 720,
        (CrownType.Card, VegLevel.L1) => 100,
        (CrownType.Mixed, VegLevel.L0Plus) => 900,
        (CrownType.Mixed, VegLevel.L0) => 360,
        (CrownType.Mixed, VegLevel.L1) => 50,
        (CrownType.Cloud, VegLevel.L0Plus) => 380,
        (CrownType.Cloud, VegLevel.L0) => 150,
        (CrownType.Cloud, VegLevel.L1) => 21,
        _ => throw new ArgumentOutOfRangeException(nameof(level), level, "Unknown vegetation level.")
    };

    /// <summary>Hard ceiling per mother model at L0+ (ENG-PERF §9: cloud crowns ≤ 10 000 triangles).</summary>
    public const int MaxTrianglesL0Plus = 10000;

    /// <summary>Cloud crowns at L0 (15–40 m): 2.5k (max-011 review); may rise to 3.5k only if the outline still shows facets.</summary>
    public const int CloudTrianglesL0 = 2500;

    /// <summary>
    /// The 3.5k allowance (max-011 review), taken because the 2.5k L0 outline still showed facets at 15–40 m: the most
    /// exposed balls (the ones that make the silhouette) go back to icosphere level 2 within it.
    /// </summary>
    public const int CloudTrianglesL0Outline = 3500;

    /// <summary>L1 target (ENG-PERF §9: 0.6–0.9k); L1 merges its masses down to this many.</summary>
    public const int MaxMassesL1 = 6;

    /// <summary>Cards stay within this fraction of the crown diameter outside the crown masses (max-011 review: 5%).</summary>
    public const float CardReach = 0.05f;

    /// <summary>Card half-diagonal cap as a fraction of the crown diameter, so small crowns keep their cards inside the 5% band.</summary>
    public const float CardSizeOfDiameter = 0.035f;

    /// <summary>Crown-mass AO by depth below the shell: 1 on the shell, 1 − this deep in the crown (pushes the terminator only).</summary>
    public const float MassAoDepth = 0.15f;

    /// <summary>The shadow proxy's masses sit this much inside the visible crown, so they never cast onto its lit side.</summary>
    public const float ProxyScale = 0.92f;

    public static ToonSpec WoodSpec() => new(Bark, BarkShade)
    {
        Kind = ToonKind.VegTrunk,
        Steps = ShadeSteps.Three,
        Band = 0.06f,
        Normal = ToonNormal.Smooth,
        Line = LineClass.Tree,
        Translucency = 0f,
        Wet = 0f
    };

    public static ToonSpec CrownSpec(SpeciesInfo species)
    {
        ArgumentNullException.ThrowIfNull(species);
        return new(species.LeafLit, species.LeafShade)
        {
            Kind = ToonKind.VegCard,
            Steps = ShadeSteps.Three,
            Band = 0.06f,
            Normal = ToonNormal.Smooth,
            Line = LineClass.Tree,
            Translucency = 0.8f,
            Wet = 0f
        };
    }

    public static MotherModel Build(SpeciesInfo species, int variant, VegLevel level, PaletteRegistry palette)
    {
        ArgumentNullException.ThrowIfNull(species);
        ArgumentNullException.ThrowIfNull(palette);
        if (variant is < 0 or >= TreeForms.Variants)
            throw new ArgumentOutOfRangeException(nameof(variant), variant, "Mother-model variants are 0–3.");
        var plan = new TreePlan(species, variant);
        var shell = new CrownShell(plan);
        var ids = IdsOf(species, palette);
        var wood = new MeshWriter(withUv: false);
        var crown = new MeshWriter(withUv: true);

        WriteWood(wood, plan, level, ids);
        var palm = species.Form == "palm";
        var cardTarget = palm ? (level switch { VegLevel.L0Plus => 20, VegLevel.L0 => 16, _ => 10 }) : CardCount(species.Crown, level);
        var allCards = palm ? PalmFronds(plan, 20) : PlaceCards(plan, shell, CardCount(species.Crown, VegLevel.L0Plus));
        var drawn = allCards.Take(cardTarget).ToList();
        foreach (var card in drawn) WriteCard(crown, plan, shell, card, ids);
        var cards = crown.Cards;
        // Masses that carry a drawn card stay, so no card is left floating where its mass was dropped.
        var hosts = drawn.Select(c => c.Owner).ToHashSet();

        var blobs = BlobsFor(plan, level);
        if (level == VegLevel.L0Plus)
        {
            // Keep the hard ceiling: the least exposed masses step down to icosphere level 1 (they barely show), and only
            // if that is not enough do free non-core masses go, least exposed first.
            int Total() => wood.Triangles + crown.Triangles + Cost(blobs);
            const int ceiling = MaxTrianglesL0Plus - 100;
            if (Total() > ceiling)
                foreach (var i in ByExposure(plan, blobs))
                {
                    if (Total() <= ceiling) break;
                    var (blob, index, scale, detail) = blobs[i];
                    if (detail > 1) blobs[i] = (blob, index, scale, 1);
                }
            if (Total() > ceiling) Prune(plan, blobs, hosts, ceiling - wood.Triangles - crown.Triangles, minDetail: 1);
        }
        else if (level == VegLevel.L0 && species.Crown == CrownType.Cloud)
        {
            Prune(plan, blobs, hosts, CloudTrianglesL0 - wood.Triangles - crown.Triangles, minDetail: 0);
            // Cores first (the big balls of each cloud), then the other balls from the most exposed down.
            var order = ByExposure(plan, blobs);
            order.Reverse();
            order.InsertRange(0, Enumerable.Range(0, blobs.Count).Where(i => blobs[i].Blob.Core));
            foreach (var i in order)
            {
                var (blob, index, scale, detail) = blobs[i];
                var extra = Icosphere.Get(2).Index.Length / 3 - Icosphere.Get(detail).Index.Length / 3;
                if (wood.Triangles + crown.Triangles + Cost(blobs) + extra > CloudTrianglesL0Outline) break;
                blobs[i] = (blob, index, scale, 2);
            }
        }
        foreach (var (blob, index, scale, detail) in blobs) WriteBlob(crown, plan, shell, blob, index, scale, detail, ids);

        var woodBundle = wood.Bundle(WoodSpec(), flags: 0);
        var crownBundle = crown.Bundle(CrownSpec(species), flags: 8);
        var bounds = Union(wood.Bounds(), crown.Bounds());
        return new MotherModel(species.Id, variant, level, woodBundle, crownBundle, plan.Height,
            plan.TrunkRadius, plan.BranchY, plan.CrownRx, bounds)
        { Cards = cards, CrownDiameter = shell.Diameter, BranchTips = plan.BranchTips().ToArray() };
    }

    /// <summary>
    /// The ShadowsOnly proxy of a mother model (WLD-VEGETATION §11.3, ENG-PERF §9): L1 wood plus the L1 masses scaled by
    /// <see cref="ProxyScale"/>, drawn with veg_trunk and its shadow_proxy holes. One per mother model, shared by all levels.
    /// </summary>
    public static GeometryBundle ShadowProxy(SpeciesInfo species, int variant, PaletteRegistry palette)
    {
        ArgumentNullException.ThrowIfNull(species);
        ArgumentNullException.ThrowIfNull(palette);
        var plan = new TreePlan(species, variant);
        var shell = new CrownShell(plan);
        var ids = IdsOf(species, palette);
        var w = new MeshWriter(withUv: false);
        WriteWood(w, plan, VegLevel.L1, ids);
        foreach (var (blob, index, _, detail) in BlobsFor(plan, VegLevel.L1))
            WriteBlob(w, plan, shell, blob, index, ProxyScale, detail, ids);
        return w.Bundle(WoodSpec(), flags: 0);
    }

    private static Ids IdsOf(SpeciesInfo species, PaletteRegistry palette) =>
        new(palette.Get(species.LeafLit).Id, palette.Get(Bark).Id, palette.Get(Whitewash).Id);

    private readonly record struct Ids(byte Leaf, byte Bark, byte Whitewash);

    /// <summary>One leaf card; Owner is the plan mass it sits on (that mass is never pruned while the card is drawn).</summary>
    private sealed record Card(Vector3 Center, Vector3 Normal, Vector3 U, Vector3 V, float Width, float Height,
        float H2, float H3, Vector3 Anchor, Vector3 BlobCenter, int Owner);

    /// <summary>
    /// Icosphere level of one mass: cloud balls 2 at L0+ and 1 below (ENG-PERF §9); card and mixed masses sit under
    /// cards, so L0+ uses 1 and L0 drops the non-core ones to the bare icosahedron. L1 uses merged masses at level 1.
    /// </summary>
    private static int BlobDetail(CrownType crown, VegLevel level, bool core) => (crown, level) switch
    {
        (CrownType.Card, VegLevel.L0Plus) => 1,
        (_, VegLevel.L0Plus) => 2,
        (CrownType.Cloud, _) => 1,
        (_, VegLevel.L0) => core ? 1 : 0,
        _ => 1
    };

    /// <summary>Masses drawn at a level, with their plan index, radius scale and detail (L1 keeps the enlarged core masses).</summary>
    private static List<(Blob Blob, int Index, float Scale, int Detail)> BlobsFor(TreePlan plan, VegLevel level)
    {
        var list = new List<(Blob, int, float, int)>();
        var crown = plan.Species.Crown;
        if (level == VegLevel.L1)
        {
            var merged = plan.MergedBlobs(MaxMassesL1);
            // Merged masses take indices after the plan's own, so their bumps differ from any near-level blob.
            for (var i = 0; i < merged.Count; i++) list.Add((merged[i], plan.Blobs.Count + i, 1f, 1));
            return list;
        }
        for (var i = 0; i < plan.Blobs.Count; i++)
        {
            var blob = plan.Blobs[i];
            list.Add((blob, i, 1f, BlobDetail(crown, level, blob.Core)));
        }
        return list;
    }

    private static int Cost(List<(Blob Blob, int Index, float Scale, int Detail)> blobs) =>
        blobs.Sum(b => Icosphere.Get(b.Detail).Index.Length / 3);

    /// <summary>Non-core masses, least exposed first: exposure is the share of a mass's surface outside every other mass.</summary>
    private static List<int> ByExposure(TreePlan plan, List<(Blob Blob, int Index, float Scale, int Detail)> blobs)
    {
        var probe = Icosphere.Get(1).Vertices;
        var order = new List<(int I, float E)>();
        for (var i = 0; i < blobs.Count; i++)
        {
            var (blob, index, _, _) = blobs[i];
            if (blob.Core) continue;
            var bumps = Bumps(plan, blob, index);
            var open = 0;
            foreach (var d in probe)
            {
                var p = Surface(blob.Center, blob.Radius, blob.Lump, bumps, d);
                if (!blobs.Any(o => o.Blob != blob && Vector3.Distance(p, o.Blob.Center) < o.Blob.Radius * 0.95f)) open++;
            }
            order.Add((i, open / (float)probe.Length));
        }
        return order.OrderBy(o => o.E).ThenBy(o => o.I).Select(o => o.I).ToList();
    }

    /// <summary>
    /// Fits the masses into a budget: drops the cardless non-core masses that show least (with the shell normals a hidden
    /// mass adds nothing but triangles), then steps the least exposed of the rest down one icosphere level, not below
    /// minDetail (L0+ keeps level 1: a bare icosahedron would leave its cards hovering over the flat faces up close).
    /// </summary>
    private static void Prune(TreePlan plan, List<(Blob Blob, int Index, float Scale, int Detail)> blobs, HashSet<int> hosts, int budget, int minDetail)
    {
        while (Cost(blobs) > budget)
        {
            var free = ByExposure(plan, blobs).Where(i => !hosts.Contains(blobs[i].Index)).ToList();
            if (free.Count == 0) break;
            blobs.RemoveAt(free[0]);
        }
        foreach (var i in ByExposure(plan, blobs))
        {
            if (Cost(blobs) <= budget) break;
            var (blob, index, scale, detail) = blobs[i];
            if (detail > minDetail) blobs[i] = (blob, index, scale, detail - 1);
        }
    }

    // ---------------------------------------------------------------------------------------------------------
    // Wood: tapered tubes with a lobed root flare; trunks 32 / 16 / 8 sides (ENG-PERF §9), branch tips capped.
    // ---------------------------------------------------------------------------------------------------------

    private static void WriteWood(MeshWriter w, TreePlan plan, VegLevel level, Ids ids)
    {
        var flarePhase = plan.Next() * 6.2831853f;
        foreach (var limb in plan.Limbs)
        {
            var sides = (limb.Level, level) switch
            {
                (0, VegLevel.L0Plus) => 32,
                (0, VegLevel.L0) => 16,
                (0, _) => 6,
                (1, VegLevel.L0Plus) => 12,
                (1, VegLevel.L0) => 6,
                (1, _) => 4,
                (_, VegLevel.L1) => 0,
                (_, VegLevel.L0) => 4,
                _ => 6
            };
            if (sides == 0) continue;
            var points = new List<Vector3>(limb.Points);
            var radii = new List<float>(limb.Radii);
            if (points.Count > 4)
            {
                // L0 and L1 keep every other ring: limbs are gentle curves, and the silhouette is what reads past 15 m.
                // L0+ thins only the trunk above the crown base, where the crown hides it (a 19 m leader is 20 rings).
                var above = level == VegLevel.L0Plus ? (limb.Level == 0 ? plan.BranchY + 1f : float.MaxValue) : float.MinValue;
                for (var i = points.Count - 2; i > 0; i -= 2)
                    if (points[i].Y > above)
                    {
                        points.RemoveAt(i);
                        radii.RemoveAt(i);
                    }
            }
            if (limb.Level == 0 && points.Count > 2 && level != VegLevel.L1)
            {
                // Extra rings above the ground carry the root flare.
                foreach (var f in new[] { 0.45f, 0.2f })
                {
                    points.Insert(2, Vector3.Lerp(points[1], points[2], f));
                    radii.Insert(2, radii[1] + (radii[2] - radii[1]) * f);
                }
            }
            var flare = limb.Level == 0 && plan.Form.Id != "shrub" ? level switch { VegLevel.L0Plus => 0.55f, VegLevel.L0 => 0.4f, _ => 0.2f } : 0f;
            var cap = limb.Level > 0 || plan.Form.Leader;
            WriteTube(w, points, radii, sides, flare, flarePhase, cap, limb.Level == 0, ids);
        }
    }

    private static void WriteTube(MeshWriter w, List<Vector3> points, List<float> radii, int sides, float flare,
        float flarePhase, bool cap, bool trunk, Ids ids)
    {
        var n = points.Count;
        var tangent = Vector3.Normalize(points[1] - points[0]);
        var normal = Vector3.Normalize(Vector3.Cross(tangent, MathF.Abs(tangent.Y) < 0.9f ? Vector3.UnitY : Vector3.UnitX));
        var first = w.VertexCount;
        for (var i = 0; i < n; i++)
        {
            var t = i == 0 ? points[1] - points[0] : i == n - 1 ? points[i] - points[i - 1] : points[i + 1] - points[i - 1];
            t = Vector3.Normalize(t);
            // Parallel transport keeps the ring seams from twisting.
            normal = Vector3.Normalize(normal - t * Vector3.Dot(normal, t));
            var binormal = Vector3.Cross(t, normal);
            var y = points[i].Y;
            for (var s = 0; s < sides; s++)
            {
                var a = s * 6.2831853f / sides;
                var c = DetMath.Cos(a);
                var sn = DetMath.Sin(a);
                var radial = normal * c + binormal * sn;
                var r = radii[i];
                if (flare > 0f)
                {
                    var k = 1f - Math.Clamp(y / 0.9f, 0f, 1f);
                    r *= 1f + flare * k * k * (1f + 0.35f * DetMath.Cos(5f * a + flarePhase));
                }
                w.Add(points[i] + radial * r, radial, ao: 1f, ids.Bark, trunk ? ids.Whitewash : (byte)0,
                    new Vector4(0f, 0f, 0f, -1f), Vector4.Zero, Vector4.Zero, Vector2.Zero);
            }
        }
        for (var i = 0; i < n - 1; i++)
            for (var s = 0; s < sides; s++)
            {
                var a = first + i * sides + s;
                var b = first + i * sides + (s + 1) % sides;
                var c = a + sides;
                var d = b + sides;
                w.Tri(a, c, b);
                w.Tri(b, c, d);
            }
        if (!cap) return;
        var lastT = Vector3.Normalize(points[n - 1] - points[n - 2]);
        var tip = w.Add(points[n - 1] + lastT * (radii[n - 1] * 1.6f), lastT, 1f, ids.Bark, 0,
            new Vector4(0f, 0f, 0f, -1f), Vector4.Zero, Vector4.Zero, Vector2.Zero);
        var ring = first + (n - 1) * sides;
        for (var s = 0; s < sides; s++) w.Tri(ring + s, tip, ring + (s + 1) % sides);
    }

    // ---------------------------------------------------------------------------------------------------------
    // Crown masses: lumpy icospheres that only shape the silhouette. Their normals come from the crown shell, so
    // neighbouring masses never start their own light and shade (max-011 review).
    // ---------------------------------------------------------------------------------------------------------

    private static void WriteBlob(MeshWriter w, TreePlan plan, CrownShell shell, Blob blob, int index, float scale, int detail, Ids ids)
    {
        var sphere = Icosphere.Get(detail);
        var bumps = Bumps(plan, blob, index);
        var radius = blob.Radius * scale;
        var first = w.VertexCount;
        foreach (var d in sphere.Vertices)
        {
            var p = Surface(blob.Center, radius, blob.Lump, bumps, d);
            var n = shell.Normal(p);
            var depth = Math.Clamp(shell.Depth(p) / MathF.Max(0.1f, shell.Diameter * 0.15f), 0f, 1f);
            var ao = 1f - MassAoDepth * depth * depth * (3f - 2f * depth);
            w.Add(p, n, ao, ids.Leaf, Gradient(plan, p, n), new Vector4(blob.Center, blob.Hash),
                new Vector4(blob.Anchor, blob.BloomHash), new Vector4(blob.Center, -1f), Vector2.Zero);
        }
        // Icosphere winding faces outward with a counter-clockwise front; D0 meshes take the opposite order.
        for (var t = 0; t < sphere.Index.Length; t += 3)
            w.Tri(first + sphere.Index[t], first + sphere.Index[t + 2], first + sphere.Index[t + 1]);
    }

    private static Vector4[] Bumps(TreePlan plan, Blob blob, int index)
    {
        var rng = new Rng(StableHash.Hash32(0x23A1u, plan.Species.Id, plan.Variant.ToString(CultureInfo.InvariantCulture),
            index.ToString(CultureInfo.InvariantCulture)));
        var bumps = new Vector4[blob.Bumps];
        for (var i = 0; i < bumps.Length; i++)
        {
            var z = rng.NextFloat() * 2f - 1f;
            var phi = rng.NextFloat() * 6.2831853f;
            var s = MathF.Sqrt(MathF.Max(0f, 1f - z * z));
            bumps[i] = new Vector4(s * DetMath.Cos(phi), z, s * DetMath.Sin(phi), 0.6f + 0.8f * rng.NextFloat());
        }
        return bumps;
    }

    /// <summary>Lumpy surface point along unit direction d: soft bumps of the given amplitude, bottom squashed to 0.7.</summary>
    private static Vector3 Surface(Vector3 center, float radius, float lump, Vector4[] bumps, Vector3 d)
    {
        var sum = 0f;
        foreach (var b in bumps)
        {
            var k = Math.Clamp((Vector3.Dot(d, new Vector3(b.X, b.Y, b.Z)) - 0.78f) / 0.22f, 0f, 1f);
            sum += k * k * (3f - 2f * k) * b.W;
        }
        var r = radius * (1f + lump * (Math.Min(sum, 1.4f) - 0.35f));
        var p = d * r;
        if (p.Y < 0f) p.Y *= 0.7f;
        return center + p;
    }

    // ---------------------------------------------------------------------------------------------------------
    // Cards (WLD-VEGETATION §11.2, max-011 review): every card sits on the outermost crown mass along a ray aimed at
    // the crown shell, at most CardReach × diameter outside it, facing the shell normal; veg_card then keeps them at
    // full size only on the silhouette ring. Directions follow a Halton sequence, so every prefix (the lower levels)
    // still covers the whole crown evenly.
    // ---------------------------------------------------------------------------------------------------------

    private static List<Card> PlaceCards(TreePlan plan, CrownShell shell, int total)
    {
        var cards = new List<Card>(total);
        var crown = plan.Species.Crown;
        var bottom = plan.CrownCenter.Y - plan.CrownRy;
        var reach = CardReach * shell.Diameter;
        var bumps = plan.Blobs.Select((b, i) => Bumps(plan, b, i)).ToArray();
        for (var i = 1; cards.Count < total && i <= total * 4; i++)
        {
            var z = 1f - 2f * Halton(i, 2);
            var phi = 6.2831853f * Halton(i, 3);
            var s = MathF.Sqrt(MathF.Max(0f, 1f - z * z));
            var d = new Vector3(s * DetMath.Cos(phi), z, s * DetMath.Sin(phi));
            // The underside carries half the density: it is seen less and holds the hanging fringe.
            if (d.Y < -0.5f && (i & 1) == 0) continue;
            var target = shell.EllipsoidPoint(d);
            var origin = shell.Center + (target - shell.Center) * 2f;
            if (!OuterHit(plan, bumps, origin, shell.Center - origin, out var hit, out var owner)) continue;
            var n = shell.Normal(hit);
            var aspect = plan.Species.Card == CardShape.Strand ? 1.8f : 1f;
            var spread = 0.5f * MathF.Sqrt(1f + aspect * aspect);
            var size = MathF.Min(plan.Form.CardSize * plan.Range(0.85f, 1.2f), CardSizeOfDiameter * shell.Diameter / spread);
            // Centre on the mass surface or up to 0.3 card inside it: silhouette cards then overlap the mass edge instead
            // of hovering beside it, and a corner (half diagonal ≤ 3.5% of the diameter) stays inside the 5% band.
            var center = hit + n * plan.Range(-0.3f * size, MathF.Min(0f, 0.8f * reach - spread * size));
            var droop = plan.Form.Droop;
            if (crown == CrownType.Cloud && center.Y < bottom + plan.CrownRy * 0.6f) droop = MathF.Max(droop, 0.7f);
            var (u, v) = Frame(n, plan.Next() * 6.2831853f, droop);
            var blob = plan.Blobs[owner];
            var anchor = blob.Anchor + plan.RandomDirection() * plan.Range(0f, 0.3f);
            cards.Add(new Card(center, n, u, v, size, size * aspect, plan.Next(), plan.Next(), anchor, blob.Center, owner));
        }
        return cards;
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
            var along = Vector3.Normalize(TreePlan.Azimuth(azimuth) * DetMath.Cos(pitch) + Vector3.UnitY * DetMath.Sin(pitch));
            var size = plan.Form.CardSize * plan.Range(0.85f, 1.1f);
            var side = Vector3.Normalize(Vector3.Cross(Vector3.UnitY, along) + new Vector3(0f, 0f, 1e-4f));
            var normal = Vector3.Normalize(Vector3.Cross(side, along));
            var center = head.Center + along * (size * 0.5f + head.Radius * 0.6f);
            // Fronds keep their own plane normal: veg_card turns each frond about it.
            cards.Add(new Card(center, normal, side, along, size, size, plan.Next(), plan.Next(), head.Center, head.Center, 0));
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
        var c = DetMath.Cos(psi);
        var s = DetMath.Sin(psi);
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

    private static void WriteCard(MeshWriter w, TreePlan plan, CrownShell shell, Card card, Ids ids)
    {
        var first = w.VertexCount;
        // Palm fronds are few and all needed at every level, so they never shrink in the level bands.
        var importance = plan.Form.Id == "palm" ? 0f : w.Cards / (float)Math.Max(1, CardCapacity(plan));
        var t0 = Gradient(plan, card.Center, plan.Form.Id == "palm" ? card.Normal : shell.Normal(card.Center));
        ReadOnlySpan<Vector2> corners = [new(0f, 0f), new(1f, 0f), new(1f, 1f), new(0f, 1f)];
        foreach (var uv in corners)
        {
            var p = card.Center + card.U * ((uv.X - 0.5f) * card.Width) + card.V * ((uv.Y - 0.5f) * card.Height);
            w.Add(p, card.Normal, 1f, ids.Leaf, t0, new Vector4(card.Center, card.H2), new Vector4(card.Anchor, card.H3),
                new Vector4(card.BlobCenter, importance), uv);
        }
        w.Tri(first, first + 2, first + 1);
        w.Tri(first, first + 3, first + 2);
        w.Cards++;
    }

    private static int CardCapacity(TreePlan plan) => plan.Form.Id == "palm" ? 20 : CardCount(plan.Species.Crown, VegLevel.L0Plus);

    /// <summary>Crown-gradient t0 = 0.36 + 0.26·n.y + 0.30·(height in crown − 0.5) + 0.12·outward (WLD-VEGETATION §1.3).</summary>
    private static byte Gradient(TreePlan plan, Vector3 p, Vector3 n)
    {
        var h = Math.Clamp((p.Y - (plan.CrownCenter.Y - plan.CrownRy)) / (2f * plan.CrownRy), 0f, 1f);
        var outward = Math.Clamp(Vector2.Distance(new(p.X, p.Z), new(plan.CrownCenter.X, plan.CrownCenter.Z)) / MathF.Max(0.1f, plan.CrownRx), 0f, 1f);
        var t0 = Math.Clamp(0.36f + 0.26f * n.Y + 0.30f * (h - 0.5f) + 0.12f * outward, 0f, 1f);
        return (byte)MathF.Round(t0 * 255f);
    }

    private static Box3 Union(Box3 a, Box3 b) => new(Vector3.Min(a.Min, b.Min), Vector3.Max(a.Max, b.Max));

    /// <summary>Growable vertex arrays for one GeometryBundle.</summary>
    private sealed class MeshWriter(bool withUv)
    {
        private readonly List<float> _position = [];
        private readonly List<float> _normal = [];
        private readonly List<float> _ao = [];
        private readonly List<byte> _color = [];
        private readonly List<byte> _spare = [];
        private readonly List<float> _c0 = [];
        private readonly List<float> _c1 = [];
        private readonly List<float> _c2 = [];
        private readonly List<float> _uv = [];
        private readonly List<int> _index = [];

        public int VertexCount => _ao.Count;
        public int Triangles => _index.Count / 3;
        public int Cards { get; set; }

        public int Add(Vector3 p, Vector3 n, float ao, byte color, byte spare, Vector4 c0, Vector4 c1, Vector4 c2, Vector2 uv)
        {
            _position.AddRange([p.X, p.Y, p.Z]);
            _normal.AddRange([n.X, n.Y, n.Z]);
            _ao.Add(Math.Clamp(ao, 0f, 1f));
            _color.Add(color);
            _spare.Add(spare);
            _c0.AddRange([c0.X, c0.Y, c0.Z, c0.W]);
            _c1.AddRange([c1.X, c1.Y, c1.Z, c1.W]);
            _c2.AddRange([c2.X, c2.Y, c2.Z, c2.W]);
            if (withUv) _uv.AddRange([uv.X, uv.Y]);
            return _ao.Count - 1;
        }

        public void Tri(int a, int b, int c) => _index.AddRange([a, b, c]);

        public Box3 Bounds()
        {
            if (_position.Count == 0) return new Box3(Vector3.Zero, Vector3.Zero);
            var min = new Vector3(float.MaxValue);
            var max = new Vector3(float.MinValue);
            for (var i = 0; i < _position.Count; i += 3)
            {
                var p = new Vector3(_position[i], _position[i + 1], _position[i + 2]);
                min = Vector3.Min(min, p);
                max = Vector3.Max(max, p);
            }
            return new Box3(min, max);
        }

        public GeometryBundle Bundle(ToonSpec spec, byte flags)
        {
            var bundle = new GeometryBundle(new ToonRef(spec), [.. _position], [.. _normal], [.. _index], [.. _ao], withUv ? [.. _uv] : null)
            {
                ColorIds = [.. _color],
                Reserved = [.. _spare],
                Flags = flags == 0 ? null : Enumerable.Repeat(flags, _ao.Count).ToArray(),
                Custom0 = [.. _c0],
                Custom1 = [.. _c1],
                Custom2 = [.. _c2]
            };
            bundle.ValidateVertexData();
            return bundle;
        }
    }
}
