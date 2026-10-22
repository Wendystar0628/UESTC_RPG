using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.Geometry;

/// <summary>Plan-ring normalization, orientation, containment and boundary distance in world X/Z.</summary>
public static class PlanRings
{

    public static IReadOnlyList<Vector2> OpenRing(IEnumerable<Vector2> input, bool outer = true)
    {
        var points = new List<Vector2>();
        foreach (var p in input)
        {
            if (!float.IsFinite(p.X) || !float.IsFinite(p.Y)) throw new ArgumentException("Non-finite ring coordinate.");
            if (points.Count == 0 || points[^1] != p) points.Add(p);
        }
        if (points.Count > 1 && points[0] == points[^1]) points.RemoveAt(points.Count - 1);
        if (points.Count < 3 || Math.Abs(SignedArea(points)) < 0.000001) throw new ArgumentException("A ring needs three distinct non-collinear vertices.");
        if ((SignedArea(points) > 0) != outer) points.Reverse();
        return points.AsReadOnly();
    }


    public static double SignedArea(IReadOnlyList<Vector2> ring)
    {
        double area = 0;
        for (var i = 0; i < ring.Count; i++)
        {
            var a = ring[i]; var b = ring[(i + 1) % ring.Count];
            area += (double)a.X * b.Y - (double)b.X * a.Y;
        }
        return area * 0.5;
    }


    /// <summary>Scalar double orientation; coordinates are promoted before subtraction.</summary>
    public static double Cross(Vector2 a, Vector2 b, Vector2 c) =>
        ((double)b.X - a.X) * ((double)c.Y - a.Y) - ((double)b.Y - a.Y) * ((double)c.X - a.X);


    /// <summary>Area accumulated as a fan about the first vertex, retaining triangulation's operation order.</summary>
    public static double FanSignedArea(IReadOnlyList<Vector2> ring)
    {
        double area = 0;
        var origin = ring[0];
        for (var i = 1; i + 1 < ring.Count; i++) area += Cross(origin, ring[i], ring[i + 1]);
        return area * 0.5;
    }


    public static bool PointInRing(Vector2 p, IReadOnlyList<Vector2> ring)
    {
        var inside = false;
        for (int i = 0, j = ring.Count - 1; i < ring.Count; j = i++)
        {
            var a = ring[j]; var b = ring[i];
            if (PlanLines.SegmentDistanceSquared(p, a, b) <= 0.00000001f) return true;
            if ((a.Y > p.Y) != (b.Y > p.Y) && p.X < (double)(b.X - a.X) * (p.Y - a.Y) / (b.Y - a.Y) + a.X)
                inside = !inside;
        }
        return inside;
    }


    public static bool Contains(PolygonRings polygon, Vector2 p)
    {
        if (!PointInRing(p, polygon.Outer)) return false;
        for (var i = 0; i < polygon.Holes.Count; i++)
            if (PointInRing(p, polygon.Holes[i])) return false;
        return true;
    }


    /// <summary>
    /// Single-precision even-odd crossing test, each edge read from ring[i] back to ring[i − 1]; boundary points may go either
    /// way. <see cref="PointInRing"/> is the boundary-inclusive test.
    /// </summary>
    public static bool InsideByCrossings(IReadOnlyList<Vector2> ring, Vector2 p)
    {
        var inside = false;
        for (int i = 0, j = ring.Count - 1; i < ring.Count; j = i++)
        {
            Vector2 a = ring[i], b = ring[j];
            if ((a.Y > p.Y) != (b.Y > p.Y) && p.X < (b.X - a.X) * (p.Y - a.Y) / (b.Y - a.Y) + a.X) inside = !inside;
        }
        return inside;
    }


    public static float DistanceToRing(Vector2 p, IReadOnlyList<Vector2> ring)
    {
        var distance = float.PositiveInfinity;
        for (var i = 0; i < ring.Count; i++) distance = Math.Min(distance, PlanLines.SegmentDistanceSquared(p, ring[i], ring[(i + 1) % ring.Count]));
        return MathF.Sqrt(distance);
    }

    /// <summary>Drop adjacent and closing duplicates, preserving the first point and traversal direction.</summary>
    public static Vector2[] DeduplicateRing(IReadOnlyList<Vector2> ring, float distanceSquared = 1e-8f)
    {
        var points = new List<Vector2>(ring.Count);
        foreach (var point in ring)
            if (points.Count == 0 || Vector2.DistanceSquared(points[^1], point) > distanceSquared) points.Add(point);
        while (points.Count > 1 && Vector2.DistanceSquared(points[0], points[^1]) <= distanceSquared) points.RemoveAt(points.Count - 1);
        return points.ToArray();
    }

    /// <summary>Clean short edges, collinear vertices and reversing spikes, then orient without rotating the start vertex.</summary>
    public static Vector2[]? CleanRing(IReadOnlyList<Vector2> input, bool outer = true, float minEdge = 0,
        float collinearTolerance = 0, float duplicateTolerance = 0, float minimumArea = 0, float reversalTolerance = 0,
        int maxPasses = int.MaxValue, float? duplicateDistanceSquared = null)
    {
        if (input.Any(point => !VectorMath.Finite(point))) return null;
        var points = DeduplicateRing(input, duplicateDistanceSquared ?? duplicateTolerance * duplicateTolerance).ToList();
        var passes = 0;
        for (var changed = true; changed && points.Count >= 3 && passes++ < maxPasses;)
        {
            changed = false;
            for (var i = 0; i < points.Count && points.Count >= 3; i++)
            {
                var a = points[(i + points.Count - 1) % points.Count];
                var p = points[i];
                var b = points[(i + 1) % points.Count];
                var chord = b - a;
                var chordLength = chord.Length();
                var deviation = chordLength > 1e-4f ? MathF.Abs(CrossFloat(chord, p - a)) / chordLength : 0;
                var reverses = Vector2.Dot(p - a, b - p) < 0 && deviation < reversalTolerance;
                if (deviation < collinearTolerance && !reverses && chordLength > 1e-4f || reverses)
                {
                    points.RemoveAt(i--);
                    changed = true;
                }
                else if (Vector2.Distance(p, b) < minEdge)
                {
                    var c = points[(i + 2) % points.Count];
                    var keepP = MathF.Abs(CrossFloat(c - p, a - p));
                    var keepB = MathF.Abs(CrossFloat(c - b, a - b));
                    points.RemoveAt(keepP >= keepB ? (i + 1) % points.Count : i);
                    i--;
                    changed = true;
                }
            }
        }
        if (points.Count < 3) return null;
        var area = (float)SignedArea(points);
        if (MathF.Abs(area) < minimumArea || area == 0) return null;
        if ((area > 0) != outer) points.Reverse();
        return points.ToArray();
    }

    /// <summary>Existing positive-side crossing rule: zero lies on the nonpositive side; adjacent edges are excluded.</summary>
    public static bool SelfIntersects(IReadOnlyList<Vector2> ring)
    {
        for (var i = 0; i < ring.Count; i++)
            for (var j = i + 2; j < ring.Count; j++)
            {
                if (i == 0 && j == ring.Count - 1) continue;
                var a = ring[i]; var b = ring[(i + 1) % ring.Count];
                var c = ring[j]; var d = ring[(j + 1) % ring.Count];
                if ((CrossFloat(b - a, c - a) > 0) != (CrossFloat(b - a, d - a) > 0)
                    && (CrossFloat(d - c, a - c) > 0) != (CrossFloat(d - c, b - c) > 0)) return true;
            }
        return false;
    }

    public static float CrossFloat(Vector2 a, Vector2 b) => a.X * b.Y - a.Y * b.X;
}
