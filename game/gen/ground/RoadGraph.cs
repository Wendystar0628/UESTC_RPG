using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Geometry;

namespace Campus.Gen.Ground;

/// <summary>Carriageway topology from the centre lines: junction nodes, their arms and the kerb fillets at corners.</summary>
internal static class RoadGraph
{
    public sealed record Arm(RoadLine Road, int Vertex, bool Forward);
    public sealed record Junction(Vector2 At, IReadOnlyList<Arm> Arms, float Radius);

    /// <summary>Nodes where at least three arms of at least two carriageways meet (shared OSM nodes).</summary>
    public static List<Junction> Junctions(IEnumerable<RoadLine> roads)
    {
        var nodes = new SortedDictionary<(long, long), List<Arm>>();
        foreach (var road in roads)
        {
            if (!road.Carriageway) continue;
            for (var v = 0; v < road.Points.Length; v++)
            {
                var key = (Quantize(road.Points[v].X), Quantize(road.Points[v].Y));
                if (!nodes.TryGetValue(key, out var arms)) nodes.Add(key, arms = []);
                if (v > 0) arms.Add(new(road, v, false));
                if (v + 1 < road.Points.Length) arms.Add(new(road, v, true));
            }
        }
        var result = new List<Junction>();
        foreach (var arms in nodes.Values)
        {
            if (arms.Count < 3 || arms.Select(a => a.Road.Id).Distinct().Count() < 2) continue;
            var at = arms[0].Road.Points[arms[0].Vertex];
            result.Add(new(at, arms, arms.Max(a => a.Road.Width) * 0.5f));
        }
        return result;
    }

    /// <summary>Point and direction at arc length d from a vertex, walking forward or backward; null if the line is shorter.</summary>
    public static (Vector2? Point, Vector2 Dir) Walk(Vector2[] points, int vertex, bool forward, float d)
    {
        var arm = forward ? points[vertex..] : points[..(vertex + 1)].Reverse().ToArray();
        if (arm.Length < 2 || d > PlanLines.Length(arm)) return (null, Vector2.UnitX);
        var sample = PlanLines.Sample(arm, d);
        return (sample.Point, sample.Tangent);
    }

    /// <summary>
    /// Kerb fillets: for every pair of neighbouring arms at a junction, the corner between their kerb lines is rounded
    /// with an arc (radius scaled with the narrower road, 4-8 m), returned as extra carriageway polygons. Uses no
    /// trigonometry: arms are ordered by a pseudo-angle and the arc is sampled by normalised interpolation.
    /// </summary>
    public static List<Vector2[]> Fillets(IEnumerable<RoadLine> roads)
    {
        var result = new List<Vector2[]>();
        foreach (var junction in Junctions(roads))
        {
            var arms = new List<(Vector2 Dir, float Half, float Reach)>();
            foreach (var arm in junction.Arms)
            {
                // Direction of the arm over its first few metres (robust against short kinks).
                var probe = Math.Min(4f, ArmLength(arm));
                if (probe < 1f) continue;
                var (p, _) = Walk(arm.Road.Points, arm.Vertex, arm.Forward, probe);
                if (p is not { } q) continue;
                var dir = Vector2.Normalize(q - junction.At);
                arms.Add((dir, arm.Road.Width * 0.5f, ArmLength(arm)));
            }
            arms.Sort((a, b) => PseudoAngle(a.Dir).CompareTo(PseudoAngle(b.Dir)));
            for (var k = 0; k < arms.Count; k++)
            {
                var a = arms[k];
                var b = arms[(k + 1) % arms.Count];
                if (Fillet(junction.At, a, b) is { } patch) result.Add(patch);
            }
        }
        return result;
    }

    private static Vector2[]? Fillet(Vector2 node, (Vector2 Dir, float Half, float Reach) a, (Vector2 Dir, float Half, float Reach) b)
    {
        var cross = a.Dir.X * b.Dir.Y - a.Dir.Y * b.Dir.X;
        var dot = Vector2.Dot(a.Dir, b.Dir);
        // b counter-clockwise from a by 20-170 degrees: cos 170 = -0.985, sin 20 = 0.342.
        if (cross <= 0f || dot < -0.985f || (dot > 0f && cross < 0.342f)) return null;
        var radius = Math.Clamp(MathF.Min(a.Half, b.Half) * 1.4f, 4f, 8f);
        var na = new Vector2(-a.Dir.Y, a.Dir.X);   // a's kerb side facing b
        var nb = new Vector2(b.Dir.Y, -b.Dir.X);   // b's kerb side facing a
        if (PlanLines.LineIntersection(node + na * (a.Half + radius), a.Dir, node + nb * (b.Half + radius), b.Dir, 1e-5f) is not { } centre) return null;
        var t1 = centre - na * radius;
        var t2 = centre - nb * radius;
        // Tangent points must lie on the arms, within their straight-ish first stretch.
        var ta = Vector2.Dot(t1 - node, a.Dir);
        var tb = Vector2.Dot(t2 - node, b.Dir);
        if (ta <= 0f || tb <= 0f || ta > MathF.Min(a.Reach, 16f) || tb > MathF.Min(b.Reach, 16f)) return null;
        // From the node out along both centre lines, so the patch overlaps the roads instead of just touching them.
        var points = new List<Vector2> { node, node + a.Dir * ta, t1 };
        var v1 = t1 - centre;
        var v2 = t2 - centre;
        const int segments = 10;
        for (var s = 1; s < segments; s++)
        {
            var t = (float)s / segments;
            points.Add(centre + Vector2.Normalize(v1 + (v2 - v1) * t) * radius);
        }
        points.Add(t2);
        points.Add(node + b.Dir * tb);
        return points.ToArray();
    }

    private static float ArmLength(Arm arm)
    {
        var points = arm.Road.Points;
        return (float)PlanLines.Length(arm.Forward ? points[arm.Vertex..] : points[..(arm.Vertex + 1)]);
    }

    /// <summary>Monotonic in the angle of (x, z), in [0, 4); no trigonometry.</summary>
    private static float PseudoAngle(Vector2 d)
    {
        var sum = MathF.Abs(d.X) + MathF.Abs(d.Y);
        if (sum <= 0f) return 0f;
        var r = d.X / sum;
        return d.Y >= 0f ? 1f - r : 3f + r;
    }

    private static long Quantize(float v) => (long)MathF.Round(v * 20f);
}
