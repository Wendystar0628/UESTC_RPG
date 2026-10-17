using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.Walkable;

internal static class WalkGeometry
{
    internal static bool Contains(PolygonRings polygon, Vector2 point)
    {
        if (!Contains(polygon.Outer, point)) return false;
        for (var i = 0; i < polygon.Holes.Count; i++)
            if (Contains(polygon.Holes[i], point)) return false;
        return true;
    }

    private static bool Contains(IReadOnlyList<Vector2> ring, Vector2 point)
    {
        var inside = false;
        for (int i = 0, j = ring.Count - 1; i < ring.Count; j = i++)
        {
            var a = ring[j];
            var b = ring[i];
            if (SegmentDistanceSquared(point, a, b) <= 0.000001f) return true;
            if ((a.Y > point.Y) != (b.Y > point.Y) &&
                point.X < (b.X - a.X) * (point.Y - a.Y) / (b.Y - a.Y) + a.X)
                inside = !inside;
        }
        return inside;
    }

    internal static float SegmentDistanceSquared(Vector2 p, Vector2 a, Vector2 b)
    {
        var dx = b.X - a.X;
        var dz = b.Y - a.Y;
        var lengthSquared = dx * dx + dz * dz;
        var t = lengthSquared > 0f ? Math.Clamp(((p.X - a.X) * dx + (p.Y - a.Y) * dz) / lengthSquared, 0f, 1f) : 0f;
        var x = p.X - a.X - t * dx;
        var z = p.Y - a.Y - t * dz;
        return x * x + z * z;
    }

    internal static float EdgeDistanceSquared(PolygonRings polygon, Vector2 point)
    {
        var best = RingDistanceSquared(polygon.Outer, point);
        foreach (var hole in polygon.Holes) best = Math.Min(best, RingDistanceSquared(hole, point));
        return best;
    }

    private static float RingDistanceSquared(IReadOnlyList<Vector2> ring, Vector2 point)
    {
        var best = float.PositiveInfinity;
        for (int i = 0, j = ring.Count - 1; i < ring.Count; j = i++)
            best = Math.Min(best, SegmentDistanceSquared(point, ring[j], ring[i]));
        return best;
    }

    internal static (Vector2 Min, Vector2 Max) Bounds(IReadOnlyList<Vector2> points)
    {
        if (points.Count == 0) throw new ArgumentException("Geometry has no points.", nameof(points));
        var min = points[0];
        var max = min;
        foreach (var p in points)
        {
            if (!float.IsFinite(p.X) || !float.IsFinite(p.Y)) throw new ArgumentException("Geometry coordinates must be finite.", nameof(points));
            min = Vector2.Min(min, p);
            max = Vector2.Max(max, p);
        }
        return (min, max);
    }
}
