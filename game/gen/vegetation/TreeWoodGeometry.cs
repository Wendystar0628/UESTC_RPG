using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Geometry;
using static Campus.Gen.Vegetation.MotherModels;

namespace Campus.Gen.Vegetation;

/// <summary>Writes the tree limb hierarchy as tapered tubes.</summary>
internal static class TreeWoodGeometry
{
    // ---------------------------------------------------------------------------------------------------------
    // Wood: tapered tubes with a lobed root flare; trunks 32 / 16 / 8 sides (ENG-PERF), branch tips capped.
    // ---------------------------------------------------------------------------------------------------------

    /// <summary>The existing low-detail branch section, shared by L0 secondaries and L1 branches.</summary>
    private const int SimplifiedBranchSides = 4;

    internal static void WriteWood(VegetationPrototypeGeometry w, TreePlan plan, VegLevel level, MotherColorIds ids)
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
                (1, _) => SimplifiedBranchSides,
                (_, VegLevel.L1) when plan.Species.Deciduous => SimplifiedBranchSides,
                (_, VegLevel.L1) => 0,
                (_, VegLevel.L0) => SimplifiedBranchSides,
                _ => 6
            };
            if (sides == 0) continue;
            var points = new List<Vector3>(limb.Points);
            var radii = new List<float>(limb.Radii);
            if (level == VegLevel.L1 && plan.Species.Deciduous && limb.Level > 0)
            {
                // Retain the same skeleton and every winter-card anchor at middle distance. Spend fewer rings on each
                // branch instead of removing the entire secondary level: main branches keep their bend, secondaries
                // use the chord between their attachment and tip. Counts, taper and reach still come from TreePlan.
                var middle = points.Count / 2;
                var keep = limb.Level == 1 ? new[] { 0, middle, points.Count - 1 } : new[] { 0, points.Count - 1 };
                points = keep.Select(i => limb.Points[i]).ToList();
                radii = keep.Select(i => limb.Radii[i]).ToList();
            }
            else if (points.Count > 4)
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
                    radii.Insert(2, ScalarMath.Lerp(radii[1], radii[2], f));
                }
            }
            var flare = limb.Level == 0 && !TreeForms.IsBall(plan.Form.Id) ? level switch { VegLevel.L0Plus => 0.55f, VegLevel.L0 => 0.4f, _ => 0.2f } : 0f;
            var cap = limb.Level > 0 || plan.Form.Leader;
            WriteTube(w, points, radii, sides, flare, flarePhase, cap, ids);
        }
    }

    internal static void WriteTube(VegetationPrototypeGeometry w, List<Vector3> points, List<float> radii, int sides, float flare,
        float flarePhase, bool cap, MotherColorIds ids, float tag = 0f)
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
                var c = MathF.Cos(a);
                var sn = MathF.Sin(a);
                var radial = normal * c + binormal * sn;
                var r = radii[i];
                if (flare > 0f)
                {
                    var k = 1f - Math.Clamp(y / 0.9f, 0f, 1f);
                    r *= 1f + flare * k * k * (1f + 0.35f * MathF.Cos(5f * a + flarePhase));
                }
                w.Add(points[i] + radial * r, radial, ao: 1f, ids.Bark, 0,
                    new Vector4(0f, 0f, 0f, -1f), Vector4.Zero, new Vector4(0f, 0f, 0f, tag), Vector2.Zero);
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
}
