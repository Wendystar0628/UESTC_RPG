using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Geometry;

namespace Campus.Gen.Ground;

/// <summary>Dashed centre lines and zebra crossings derived from the exact carriageway field.</summary>
internal static class RoadMarkings
{
    internal static List<RoadGraph.Junction> AddTo(GroundMarkings result, GroundLayout layout, GroundSection section)
    {
        var spec = section.Markings;
        var cars = layout.Roads.Where(r => r.Carriageway).ToList();
        var walks = layout.Roads.Where(r => !r.Carriageway && r.Type != "pedestrian" && r.Type != "steps").ToList();
        var junctions = RoadGraph.Junctions(cars);

        // Crossings at junction arms.
        var zebras = new List<(Vector2 Centre, Vector2 Along, float HalfWidth, float Length)>();
        foreach (var node in junctions)
        {
            foreach (var arm in node.Arms)
            {
                if (arm.Road.Width < spec.ZebraJunctionMinWidth) continue;
                var d = node.Radius + spec.ZebraSetback + spec.ZebraLength * 0.5f;
                var (centre, dir) = RoadGraph.Walk(arm.Road.Points, arm.Vertex, arm.Forward, d);
                if (centre is not { } c) continue;
                zebras.Add((c, dir, arm.Road.Width * 0.5f, spec.ZebraLength));
            }
        }

        // Crossings where a footpath crosses a carriageway away from junctions.
        foreach (var walk in walks)
            foreach (var road in cars)
                for (var i = 0; i + 1 < walk.Points.Length; i++)
                    for (var k = 0; k + 1 < road.Points.Length; k++)
                    {
                        if (PlanLines.SegmentIntersection(walk.Points[i], walk.Points[i + 1], road.Points[k], road.Points[k + 1], 1e-6f) is not { } x) continue;
                        if (NearJunction(junctions, x, spec.ZebraLength + spec.ZebraSetback)) continue;
                        var along = Vector2.Normalize(road.Points[k + 1] - road.Points[k]);
                        zebras.Add((x, along, road.Width * 0.5f, MathF.Max(spec.ZebraLength, walk.Width + 1f)));
                    }

        var placed = new List<Vector2>();
        foreach (var (centre, along, half, length) in zebras)
        {
            if (placed.Any(p => Vector2.DistanceSquared(p, centre) < 36f)) continue;
            if (!OnAsphalt(layout, centre, 1f)) continue;
            placed.Add(centre);
            result.Crossings.Add(centre);
            AddZebra(result, layout, centre, along, half, length, spec);
        }
        // Centre lines, kept clear of junctions and crossings.
        foreach (var road in cars)
        {
            if (road.Width < spec.CentreLineMinWidth) continue;
            var period = spec.DashLength + spec.DashGap;
            var length = PlanLines.Length(road.Points);
            var start = spec.DashGap * 0.5f;
            for (var s = start; s + spec.DashLength <= length; s += period)
            {
                var p0 = PlanLines.Sample(road.Points, s).Point;
                var p1 = PlanLines.Sample(road.Points, s + spec.DashLength).Point;
                var mid = (p0 + p1) * 0.5f;
                if (NearJunction(junctions, mid, spec.ZebraSetback + spec.ZebraLength + 1.5f)) continue;
                if (placed.Any(p => Vector2.DistanceSquared(p, mid) < 25f)) continue;   // keep clear of mid-block crossings
                if (!OnAsphalt(layout, mid, 0.8f) || !OnAsphalt(layout, p0, 0.5f) || !OnAsphalt(layout, p1, 0.5f)) continue;
                result.AddStrip(p0, p1, spec.CentreLineWidth, GroundColor.Marking);
            }
        }

        return junctions;
    }

    private static void AddZebra(GroundMarkings result, GroundLayout layout, Vector2 c, Vector2 along, float halfWidth, float length, MarkingSpec spec)
    {
        var across = new Vector2(-along.Y, along.X);
        var usable = halfWidth - spec.ZebraMargin;
        if (usable <= spec.ZebraStripe) return;
        var pitch = spec.ZebraStripe + spec.ZebraGap;
        var n = (int)MathF.Floor((2f * usable + spec.ZebraGap) / pitch);
        if (n < 1) return;
        var first = -0.5f * (n * pitch - spec.ZebraGap) + spec.ZebraStripe * 0.5f;
        for (var k = 0; k < n; k++)
        {
            var o = c + across * (first + k * pitch);
            var a = o - along * (length * 0.5f);
            var b = o + along * (length * 0.5f);
            if (!OnAsphalt(layout, o, spec.ZebraStripe) || !OnAsphalt(layout, a, 0.05f) || !OnAsphalt(layout, b, 0.05f)) continue;
            result.AddStrip(a, b, spec.ZebraStripe, GroundColor.Marking);
        }
    }

    private static bool OnAsphalt(GroundLayout layout, Vector2 p, float inset) =>
        layout.SignedDistance(SurfaceKind.Carriageway, p, inset + 1f) < -inset &&
        layout.SignedDistance(SurfaceKind.Water, p, 1f) > 0f;

    // ---------------------------------------------------------------- network topology

    internal static bool NearJunction(List<RoadGraph.Junction> junctions, Vector2 p, float extra)
    {
        foreach (var j in junctions)
        {
            var r = j.Radius + extra;
            if (Vector2.DistanceSquared(j.At, p) < r * r) return true;
        }
        return false;
    }
}
