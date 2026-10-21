using System.Numerics;
using Campus.Gen.Geometry;
using Campus.Contracts;
using Campus.Gen.Buildings.Mesh;
using Campus.Gen.Buildings.Model;
using Campus.Gen.Buildings.Plan;
using Campus.Gen.Buildings.Rules;

namespace Campus.Gen.Buildings.Build;

/// <summary>Roof edge and slab, deterministic penthouse placement, equipment enclosures and masts.</summary>
internal sealed class BuildingRoofMesher(BuildingPlan plan, LodLevel lod, GeometrySink sink,
    BuildingSurfaces surfaces, BuildingSkin shell, RoofParts roofParts)
{
    private const float ParapetInner = -0.24f; // parapet inner face, relative to the wall plane
    private readonly bool _parapetOk = plan.SkinRings.All(r => r.Offset(ParapetInner - 0.06f) is not null);

    public void BuildEdge()
    {
        var yr = plan.RoofY;
        switch (plan.Profile.RoofEdge)
        {
            case Rules.RoofEdge.Cornice:
                {
                    var yc0 = plan.WallTopY;
                    var proj = plan.Small ? 0.22f : 0.34f;
                    var top = yr + (plan.Small ? 0.05f : 0.10f);
                    // Cornice with a drip lip at the front of the soffit.
                    shell.Sweep(surfaces.Trim, Profile.Of([shell.Point(0f, yc0, 0.55f), shell.Point(proj - 0.04f, yc0, 0.25f), shell.Point(proj - 0.04f, yc0 - 0.04f), shell.Point(proj, yc0 - 0.04f),
                    shell.Point(proj, top - 0.02f), shell.Point(proj - 0.02f, top), shell.Point(0.12f, top)], 4));
                    var copingBase = plan.EdgeTopY - 0.15f;
                    shell.Sweep(surfaces.Wall, Profile.Of([shell.Point(0.12f, top), shell.Point(0.12f, copingBase)]));
                    BuildCoping(0.12f, copingBase, plan.EdgeTopY);
                    break;
                }
            case Rules.RoofEdge.Parapet:
                {
                    // Top-floor band in the trim colour, then the coping on the parapet.
                    shell.Sweep(surfaces.Trim, Profile.Of([shell.Point(0f, yr - 0.25f, 0.3f), shell.Point(0.06f, yr - 0.25f), shell.Point(0.06f, yr + 0.18f), shell.Point(0.04f, yr + 0.2f), shell.Point(0f, yr + 0.2f)], 2));
                    BuildCoping(0f, plan.WallTopY, plan.EdgeTopY);
                    break;
                }
            default:
                throw new InvalidOperationException("Unknown roof edge.");
        }
    }

    /// <summary>
    /// Coping on the parapet from its outer face s0 to the inner face, y0 to top: the game/gen/parts/roof/ parapet cap (60 mm lip each
    /// side, drip under the outer one near) with the lightning strip on it, and the parapet's inner face down to the roof.
    /// Without a valid inner offset, or where the part cannot sweep a ring, game/gen/buildings/'s own coping profile.
    /// </summary>
    public void BuildCoping(float s0, float y0, float top)
    {
        if (_parapetOk && roofParts.Coping(s0, ParapetInner, y0, top, Materials.Spec(MaterialKind.Trim, plan.Profile.Trim)))
        {
            shell.Sweep(surfaces.Wall, Profile.Of([shell.Point(ParapetInner, y0), shell.Point(ParapetInner, plan.RoofY)]));
            roofParts.LightningStrip(s0, ParapetInner, top);
            return;
        }
        var lip = s0 + 0.06f;
        if (_parapetOk)
        {
            shell.Sweep(surfaces.Trim, Profile.Of([shell.Point(s0, y0), shell.Point(lip, y0), shell.Point(lip, top - 0.015f), shell.Point(lip - 0.015f, top),
                shell.Point(ParapetInner - 0.06f, top), shell.Point(ParapetInner - 0.06f, top - 0.07f), shell.Point(ParapetInner, top - 0.07f)], 2));
            shell.Sweep(surfaces.Wall, Profile.Of([shell.Point(ParapetInner, top - 0.07f), shell.Point(ParapetInner, plan.RoofY)]));
        }
        else
        {
            shell.Sweep(surfaces.Trim, Profile.Of([shell.Point(s0, y0), shell.Point(lip, y0), shell.Point(lip, top - 0.015f), shell.Point(lip - 0.015f, top), shell.Point(0f, top)], 2));
        }
    }

    public void BuildSlab()
    {
        if (_parapetOk) Prims.Cap(sink, surfaces.Roof, plan.SkinOuter, plan.SkinHoles, ParapetInner, 0f, plan.RoofY, up: true);
        else Prims.Cap(sink, surfaces.Roof, plan.SkinOuter, plan.SkinHoles, 0f, shell.Chamfer, plan.EdgeTopY, up: true);
    }

    // ---------------------------------------------------------------------------------------------
    // Roof relief: stair-core penthouses every ~30 m, one fenced plant enclosure and on some tall roofs a mast at a
    // corner (B7, ART-MODELING; the plant and mast are game/gen/parts/roof/ parts, RoofParts).
    // ---------------------------------------------------------------------------------------------
    public void BuildRelief()
    {
        // Plant stands on the slab: the recessed roof inside the parapet, or the flat top where there is none.
        var plantY = lod >= LodLevel.L3 || !_parapetOk ? plan.EdgeTopY : plan.RoofY;
        var boxes = RoofBoxes();
        foreach (var (ring, height, plant, centre) in boxes)
        {
            var y0 = plan.RoofY - 0.05f;
            if (lod >= LodLevel.L3 || !_parapetOk) y0 = MathF.Min(y0, plan.EdgeTopY - 0.05f);
            var top = plan.RoofY + height;
            var c = shell.Full && !plant ? BuildingPlan.WallChamfer : 0f;
            ProfilePoint Q(float s, float y) => new(s, y, s == 0f ? c : 0f);
            if (plant)
            {
                roofParts.Enclosure(ring, centre, plantY);
                continue;
            }
            if (lod <= LodLevel.L1)
            {
                Prims.Sweep(sink, surfaces.Wall, ring, Profile.Of([Q(0f, y0), Q(0f, top)]));
                var ct = top + 0.16f;
                Prims.Sweep(sink, surfaces.Trim, ring, Profile.Of([Q(0f, top), Q(0.05f, top), Q(0.05f, ct - 0.015f), Q(0.035f, ct), Q(-0.22f, ct)], 2));
                Prims.Cap(sink, surfaces.Roof, ring, [], -0.22f, 0f, ct, up: true);
                // Door on the long side facing the roof centre line.
                var f = EdgeFrame.Of(ring, 0);
                var mid = ring.Length[0] * 0.5f;
                f.Box(sink, surfaces.Metal with { Flags = 0 }, mid - 0.5f, mid + 0.5f, 0f, 0.04f, plan.RoofY, plan.RoofY + 2.05f);
            }
            else
            {
                Prims.Sweep(sink, surfaces.Wall, ring, Profile.Of([Q(0f, y0), Q(0f, top + 0.16f)]));
                Prims.Cap(sink, surfaces.Roof, ring, [], 0f, 0f, top + 0.16f, up: true);
            }
        }
        if (MastSpot(boxes) is { } mast) roofParts.Mast(mast, plantY);
    }

    /// <summary>
    /// Deterministic roof boxes inside the parapet: (ring, height above roof, is the plant enclosure, centre). Penthouses
    /// first; the enclosure takes the first candidate slot along the axis that keeps RoofRules.RoofMargin to the parapet,
    /// courtyards and penthouses (its fence line is the ring; it has no height of its own).
    /// </summary>
    private List<(PlanRing Ring, float Height, bool Plant, Vector2 Centre)> RoofBoxes()
    {
        var list = new List<(PlanRing, float, bool, Vector2)>();
        if (plan.Small || plan.Storeys < 2 || plan.Area < 120f || plan.StoreyHeight > 6.5f) return list;
        var axis = plan.Axis;
        var perp = new Vector2(-axis.Y, axis.X);
        var pts = plan.Outer.Points;
        float amin = float.MaxValue, amax = float.MinValue;
        foreach (var q in pts)
        {
            var a = Vector2.Dot(q, axis);
            amin = MathF.Min(amin, a);
            amax = MathF.Max(amax, a);
        }
        var length = amax - amin;
        var count = Math.Max(1, (int)MathF.Round(length / 30f));
        // Accepted boxes as (axis coordinate, across centre, half sizes) for the clearance test.
        var taken = new List<(float A, float B, float HalfA, float HalfB)>();
        for (var i = 0; i < count; i++) TryBox(amin + length * (i + 0.5f) / count, new Vector2(2.4f, 2.1f), false);
        if (plan.Area > RoofRules.PlantFromArea && roofParts.EnclosureHalf() is { } half)
            foreach (var t in new[] { count >= 2 ? 1f / count : 0.22f, 0.22f, 0.78f, 0.5f })
                if (TryBox(amin + length * t, half, true)) break;
        return list;

        bool TryBox(float a, Vector2 size, bool plant)
        {
            var span = Across(a, axis, perp);
            if (span is null) return false;
            var (b0, b1) = span.Value;
            var margin = plant ? RoofRules.RoofMargin : 0.6f;
            var halfA = size.X;
            var halfB = plant ? size.Y : MathF.Min(size.Y, (b1 - b0) * 0.5f - 1.0f);
            if (plant ? (b1 - b0) * 0.5f - margin < halfB : halfB < 1.2f) return false;
            var bc = (b0 + b1) * 0.5f;
            if (plant && taken.Any(t => MathF.Abs(t.A - a) < t.HalfA + halfA + margin && MathF.Abs(t.B - bc) < t.HalfB + halfB + margin)) return false;
            Vector2 At(float da, float db) => axis * (a + da) + perp * (bc + db);
            var corners = new[] { At(-halfA, -halfB), At(halfA, -halfB), At(halfA, halfB), At(-halfA, halfB) };
            if (PlanRings.SignedArea(corners) < 0) Array.Reverse(corners);
            var ok = corners.Append(At(0f, 0f)).All(q =>
                PlanRing.InsideFootprint(plan.Outer, plan.Holes, q) && PlanRing.DistanceToEdges(plan.Outer, plan.Holes, q) > margin);
            if (!ok) return false;
            var height = MathF.Min(2.9f, MathF.Max(2.4f, plan.StoreyHeight - 0.4f));
            list.Add((PlanRing.FromPoints(corners, false), height, plant, At(0f, 0f)));
            taken.Add((a, bc, halfA, halfB));
            return true;
        }
    }

    /// <summary>
    /// Mast spot on a tall roof (RoofRules.MastFromStoreys, every other building by hash): inset from one outside corner
    /// of the outer ring along its bisector, clear of the parapet and of every roof box. Null when none fits.
    /// </summary>
    private Vector2? MastSpot(List<(PlanRing Ring, float Height, bool Plant, Vector2 Centre)> boxes)
    {
        if (plan.Storeys < RoofRules.MastFromStoreys || (StableHash.Hash32(plan.Seed, "wp14.mast") & 1u) != 0u) return null;
        var ring = plan.Outer;
        var corners = Enumerable.Range(0, ring.Count).Where(i => ring.Convex[i] && ring.TurnCos[i] < 0.5f).ToArray();
        if (corners.Length == 0) return null;
        var first = (int)(StableHash.Hash32(plan.Seed, "wp14.mast.corner") % (uint)corners.Length);
        for (var k = 0; k < corners.Length; k++)
        {
            var i = corners[(first + k) % corners.Length];
            var inward = -(ring.Normal[(i + ring.Count - 1) % ring.Count] + ring.Normal[i]);
            if (inward.LengthSquared() < 1e-6f) continue;
            var spot = ring.Points[i] + VectorMath.Unit(inward) * RoofRules.MastInset;
            if (!PlanRing.InsideFootprint(plan.Outer, plan.Holes, spot) || PlanRing.DistanceToEdges(plan.Outer, plan.Holes, spot) < 0.9f) continue;
            if (boxes.Any(b => PlanRing.InsideFootprint(b.Ring, [], spot) || PlanRing.DistanceToEdges(b.Ring, [], spot) < 1.0f)) continue;
            return spot;
        }
        return null;
    }

    /// <summary>Widest interval of the footprint along perp at axis coordinate a.</summary>
    private (float, float)? Across(float a, Vector2 axis, Vector2 perp)
    {
        var hits = new List<float>();
        foreach (var ring in new[] { plan.Outer }.Concat(plan.Holes))
            for (var i = 0; i < ring.Count; i++)
            {
                var p0 = ring.Points[i];
                var p1 = ring.Points[(i + 1) % ring.Count];
                var a0 = Vector2.Dot(p0, axis);
                var a1 = Vector2.Dot(p1, axis);
                if ((a0 <= a) == (a1 <= a)) continue;
                var t = (a - a0) / (a1 - a0);
                hits.Add(Vector2.Dot(p0 + (p1 - p0) * t, perp));
            }
        hits.Sort();
        (float, float)? best = null;
        for (var i = 0; i + 1 < hits.Count; i += 2)
            if (best is null || hits[i + 1] - hits[i] > best.Value.Item2 - best.Value.Item1) best = (hits[i], hits[i + 1]);
        return best;
    }
}
