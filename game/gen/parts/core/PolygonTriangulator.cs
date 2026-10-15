using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.Parts.Core;

/// <summary>Counterclockwise outer ring, clockwise holes; indices address the flattened rings.</summary>
public sealed record TriangulatedPolygon(Vector2[] Vertices, int[] Indices, int[] RingStarts);

/// <summary>Deterministic triangulation of simple, non-touching polygon rings. Invalid topology fails closed.</summary>
public static class PolygonTriangulator
{
    public static TriangulatedPolygon Triangulate(PolygonRings polygon)
    {
        ArgumentNullException.ThrowIfNull(polygon);
        var rings = new List<Vector2[]> { Clean(polygon.Outer, true) };
        ArgumentNullException.ThrowIfNull(polygon.Holes);
        foreach (var hole in polygon.Holes) rings.Add(Clean(hole, false));
        for (var r = 0; r < rings.Count; r++)
        {
            var ring = rings[r];
            for (var i = 0; i < ring.Length; i++)
                for (var j = i + 1; j < ring.Length; j++)
                    if (j != i + 1 && !(i == 0 && j == ring.Length - 1) &&
                        Intersects(ring[i], ring[(i + 1) % ring.Length], ring[j], ring[(j + 1) % ring.Length]))
                        throw new ArgumentException("A polygon ring intersects itself.", nameof(polygon));
            for (var s = 0; s < r; s++)
                foreach (var (a, b) in Edges(ring))
                    foreach (var (c, d) in Edges(rings[s]))
                        if (Intersects(a, b, c, d))
                            throw new ArgumentException("Polygon rings must not touch or intersect.", nameof(polygon));
            if (r > 0 && !Inside(ring[0], rings[0]))
                throw new ArgumentException("A hole lies outside the outer ring.", nameof(polygon));
            for (var s = 1; s < r; s++)
                if (Inside(ring[0], rings[s]) || Inside(rings[s][0], ring))
                    throw new ArgumentException("Holes must not overlap or contain one another.", nameof(polygon));
        }
        var starts = new int[rings.Count];
        var points = new List<Vector2>();
        for (var i = 0; i < rings.Count; i++) { starts[i] = points.Count; points.AddRange(rings[i]); }
        var vertices = points.ToArray();
        var indices = Earcut.Triangulate(vertices, starts);
        double expected = rings.Sum(SignedArea), actual = 0;
        for (var i = 0; i < indices.Length; i += 3)
        {
            var area = Cross(vertices[indices[i]], vertices[indices[i + 1]], vertices[indices[i + 2]]) * 0.5;
            if (area <= 0) throw new ArgumentException("Triangulation produced an inverted or empty triangle.", nameof(polygon));
            actual += area;
        }
        if (expected <= 0 || Math.Abs(actual - expected) > Math.Max(1e-10, expected * 1e-8))
            throw new ArgumentException("Triangulation did not cover the polygon exactly.", nameof(polygon));
        return new(vertices, indices, starts);
    }

    internal static double Cross(Vector2 a, Vector2 b, Vector2 c) =>
        ((double)b.X - a.X) * ((double)c.Y - a.Y) - ((double)b.Y - a.Y) * ((double)c.X - a.X);

    internal static double SignedArea(Vector2[] ring)
    {
        double area = 0;
        var origin = ring[0];
        for (var i = 1; i + 1 < ring.Length; i++) area += Cross(origin, ring[i], ring[i + 1]);
        return area * 0.5;
    }

    private static Vector2[] Clean(IReadOnlyList<Vector2> input, bool positive)
    {
        ArgumentNullException.ThrowIfNull(input);
        var points = new List<Vector2>(input.Count);
        foreach (var p in input)
        {
            if (!float.IsFinite(p.X) || !float.IsFinite(p.Y)) throw new ArgumentException("Polygon coordinates must be finite.", nameof(input));
            if (points.Count == 0 || p != points[^1]) points.Add(p);
        }
        if (points.Count > 1 && points[0] == points[^1]) points.RemoveAt(points.Count - 1);
        for (var i = 0; points.Count >= 3 && i < points.Count;)
        {
            var previous = points[(i + points.Count - 1) % points.Count];
            var next = points[(i + 1) % points.Count];
            if (Cross(previous, points[i], next) == 0 && OnSegment(previous, points[i], next))
            { points.RemoveAt(i); i = 0; }
            else i++;
        }
        if (points.Count < 3) throw new ArgumentException("A ring needs at least three non-collinear vertices.", nameof(input));
        var ring = points.ToArray();
        var area = SignedArea(ring);
        if (area == 0) throw new ArgumentException("A ring must enclose a nonzero area.", nameof(input));
        if ((area > 0) != positive) Array.Reverse(ring);
        return ring;
    }

    private static IEnumerable<(Vector2, Vector2)> Edges(Vector2[] ring)
    {
        for (var i = 0; i < ring.Length; i++) yield return (ring[i], ring[(i + 1) % ring.Length]);
    }
    private static bool OnSegment(Vector2 a, Vector2 p, Vector2 b) =>
        p.X >= Math.Min(a.X, b.X) && p.X <= Math.Max(a.X, b.X) && p.Y >= Math.Min(a.Y, b.Y) && p.Y <= Math.Max(a.Y, b.Y);
    private static bool Intersects(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
    {
        var abC = Math.Sign(Cross(a, b, c)); var abD = Math.Sign(Cross(a, b, d));
        var cdA = Math.Sign(Cross(c, d, a)); var cdB = Math.Sign(Cross(c, d, b));
        return abC != abD && cdA != cdB || abC == 0 && OnSegment(a, c, b) || abD == 0 && OnSegment(a, d, b) ||
            cdA == 0 && OnSegment(c, a, d) || cdB == 0 && OnSegment(c, b, d);
    }
    private static bool Inside(Vector2 p, Vector2[] ring)
    {
        var inside = false;
        foreach (var (a, b) in Edges(ring))
            if ((a.Y > p.Y) != (b.Y > p.Y) && p.X < ((double)b.X - a.X) * (p.Y - a.Y) / ((double)b.Y - a.Y) + a.X)
                inside = !inside;
        return inside;
    }
}
