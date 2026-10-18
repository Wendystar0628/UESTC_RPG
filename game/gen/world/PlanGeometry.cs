using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.World;

/// <summary>Plan geometry uses world X/Z, keeps holes, and omits closing duplicates.</summary>
public static class PlanGeometry
{
    /// <summary>Facade layout follows the current source part, falling back to its physical geometry.</summary>
    public static OverlayGeometry FacadeGeometryOf(WorldFeaturePart part) => part.FacadeGeometry ?? part.Geometry;

    /// <summary>Current parts in their existing order, or the current authored geometry after reshape/add.</summary>
    public static IReadOnlyList<PolygonRings> FacadePolygonsOf(WorldFeature feature) => feature.Parts.Count == 0
        ? WorldModelLoader.Polygons(feature.Geometry).ToArray()
        : feature.Parts.SelectMany(part => WorldModelLoader.Polygons(FacadeGeometryOf(part))).ToArray();

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

    public static bool PointInRing(Vector2 p, IReadOnlyList<Vector2> ring)
    {
        var inside = false;
        for (int i = 0, j = ring.Count - 1; i < ring.Count; j = i++)
        {
            var a = ring[j]; var b = ring[i];
            if (SegmentDistanceSquared(p, a, b) <= 0.00000001f) return true;
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

    public static float SegmentDistanceSquared(Vector2 p, Vector2 a, Vector2 b)
    {
        var d = b - a;
        var length = d.LengthSquared();
        var t = length == 0 ? 0 : Math.Clamp(Vector2.Dot(p - a, d) / length, 0, 1);
        return Vector2.DistanceSquared(p, a + d * t);
    }

    public static float DistanceToRing(Vector2 p, IReadOnlyList<Vector2> ring)
    {
        var distance = float.PositiveInfinity;
        for (var i = 0; i < ring.Count; i++) distance = Math.Min(distance, SegmentDistanceSquared(p, ring[i], ring[(i + 1) % ring.Count]));
        return MathF.Sqrt(distance);
    }

    public static Vector2 Centroid(IReadOnlyList<Vector2> ring)
    {
        double x = 0, z = 0, crossSum = 0;
        for (var i = 0; i < ring.Count; i++)
        {
            var a = ring[i]; var b = ring[(i + 1) % ring.Count];
            var cross = (double)a.X * b.Y - (double)b.X * a.Y;
            x += (a.X + b.X) * cross; z += (a.Y + b.Y) * cross; crossSum += cross;
        }
        if (Math.Abs(crossSum) < 0.000001) throw new ArgumentException("Degenerate ring.");
        return new((float)(x / (3 * crossSum)), (float)(z / (3 * crossSum)));
    }
}
