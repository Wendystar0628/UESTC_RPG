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
