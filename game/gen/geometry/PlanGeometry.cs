using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.Geometry;

/// <summary>Plan geometry uses world X/Z, keeps holes, and omits closing duplicates.</summary>
public static partial class PlanGeometry
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

    public static bool Contains(PolygonRings polygon, Vector2 p) =>
        PointInRing(p, polygon.Outer) && !polygon.Holes.Any(h => PointInRing(p, h));

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

    /// <summary>
    /// Mitred offset with one output vertex per input vertex, positive to the right of the path (dz, −dx): a corner moves along
    /// (n0 + n1) / (1 + n0·n1) of its edges' unit normals, held to <paramref name="limit"/> at sharp corners; an open line's ends
    /// move along their own edge's normal.
    /// </summary>
    public static Vector2[] MitreOffset(IReadOnlyList<Vector2> points, float distance, bool closed, float limit = float.PositiveInfinity)
    {
        ArgumentNullException.ThrowIfNull(points);
        var n = points.Count;
        var output = new Vector2[n];
        for (var i = 0; i < n; i++)
        {
            Vector2 m;
            if (!closed && i == 0) m = RightNormal(points[0], points[1]);
            else if (!closed && i == n - 1) m = RightNormal(points[n - 2], points[n - 1]);
            else
            {
                Vector2 n0 = RightNormal(points[(i + n - 1) % n], points[i]), n1 = RightNormal(points[i], points[(i + 1) % n]);
                m = (n0 + n1) / (1f + n0.X * n1.X + n0.Y * n1.Y);
                var length = m.Length();
                if (length > limit) m *= limit / length;
            }
            output[i] = points[i] + m * distance;
        }
        return output;
    }

    private static Vector2 RightNormal(Vector2 a, Vector2 b)
    {
        var v = new Vector2(b.Y - a.Y, a.X - b.X);
        var length = MathF.Sqrt(v.X * v.X + v.Y * v.Y);
        return length > 1e-9f ? v / length : Vector2.UnitX;
    }

    /// <summary>
    /// Closed-ring inset with one vertex per corner and scalar double normals/intersections. Positive distance moves left;
    /// zero-length edges fail. Unlike <see cref="MitreOffset"/>, no fallback normal or miter limit is applied.
    /// </summary>
    public static Vector2[] MitreInset(IReadOnlyList<Vector2> ring, float distance)
    {
        var inset = new Vector2[ring.Count];
        for (var i = 0; i < ring.Count; i++)
        {
            var p = ring[(i + ring.Count - 1) % ring.Count]; var c = ring[i]; var n = ring[(i + 1) % ring.Count];
            var previous = RightUnitNormal(p, c); var next = RightUnitNormal(c, n);
            var divisor = 1 + (double)previous.X * next.X + (double)previous.Y * next.Y;
            inset[i] = new((float)(c.X - distance * (previous.X + next.X) / divisor), (float)(c.Y - distance * (previous.Y + next.Y) / divisor));
        }
        return inset;
    }

    /// <summary>Right-hand normal normalized in scalar double precision; nonfinite and zero-length edges fail.</summary>
    public static Vector2 RightUnitNormal(Vector2 a, Vector2 b)
    {
        float x = b.Y - a.Y, y = a.X - b.X;
        if (!float.IsFinite(x) || !float.IsFinite(y)) throw new ArgumentException("Coordinates must be finite.", nameof(a));
        var length = Math.Sqrt((double)x * x + (double)y * y);
        if (length <= 1e-12) throw new ArgumentException("A direction or edge must be nonzero.");
        return new((float)(x / length), (float)(y / length));
    }

    public static float DistanceToRing(Vector2 p, IReadOnlyList<Vector2> ring)
    {
        var distance = float.PositiveInfinity;
        for (var i = 0; i < ring.Count; i++) distance = Math.Min(distance, PlanLines.SegmentDistanceSquared(p, ring[i], ring[(i + 1) % ring.Count]));
        return MathF.Sqrt(distance);
    }

    /// <summary>Area centroid; callers that allow a degenerate ring can retain the scalar-float vertex-mean fallback.</summary>
    public static Vector2 Centroid(IReadOnlyList<Vector2> ring, bool meanIfDegenerate = false)
    {
        Vector2? fallback = null;
        if (meanIfDegenerate)
        {
            float x = 0, z = 0;
            foreach (var point in ring) { x += point.X; z += point.Y; }
            fallback = new(x / ring.Count, z / ring.Count);
        }
        return Centroid(new PolygonRings(ring), fallback);
    }
}
