using System.Numerics;
using System.Text.Json;
using Campus.Contracts;

namespace Campus.Gen.Zones.North.Framework;

/// <summary>Plan helpers over contract geometry (world x, z in metres). Plain arithmetic and square roots only (D-165).</summary>
public static class Plan
{
    public static IEnumerable<PolygonRings> Polygons(OverlayGeometry geometry) => geometry switch
    {
        PolygonGeometry polygon => [polygon.Rings],
        MultiPolygonGeometry multi => multi.Polygons,
        PolylineGeometry or PointGeometry => [],
        _ => throw new ArgumentException("Unknown geometry.", nameof(geometry)),
    };

    /// <summary>Outer ring of a feature's first polygon, with repeated and closing points dropped.</summary>
    public static IReadOnlyList<Vector2>? OuterRing(WorldFeature feature)
    {
        var rings = Polygons(feature.Geometry).FirstOrDefault();
        return rings is null ? null : Clean(rings.Outer);
    }

    /// <summary>Drops points closer than 5 cm to their predecessor, and a closing point equal to the first.</summary>
    public static IReadOnlyList<Vector2> Clean(IReadOnlyList<Vector2> ring)
    {
        var points = new List<Vector2>(ring.Count);
        foreach (var p in ring)
            if (points.Count == 0 || Vector2.Distance(points[^1], p) > 0.05f) points.Add(p);
        if (points.Count > 1 && Vector2.Distance(points[0], points[^1]) <= 0.05f) points.RemoveAt(points.Count - 1);
        return points;
    }

    /// <summary>Twice the signed area: positive when the ring turns from +x toward +z.</summary>
    public static float SignedArea2(IReadOnlyList<Vector2> ring)
    {
        double sum = 0;
        for (var i = 0; i < ring.Count; i++)
        {
            var a = ring[i];
            var b = ring[(i + 1) % ring.Count];
            sum += (double)a.X * b.Y - (double)b.X * a.Y;
        }
        return (float)sum;
    }

    /// <summary>Even-odd point in ring.</summary>
    public static bool Contains(IReadOnlyList<Vector2> ring, Vector2 p)
    {
        var inside = false;
        for (int i = 0, j = ring.Count - 1; i < ring.Count; j = i++)
        {
            var a = ring[i];
            var b = ring[j];
            if ((a.Y > p.Y) != (b.Y > p.Y) && p.X < (b.X - a.X) * (p.Y - a.Y) / (b.Y - a.Y) + a.X) inside = !inside;
        }
        return inside;
    }

    /// <summary>Distance from p to the segment ab.</summary>
    public static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b) => Vector2.Distance(p, Closest(p, a, b));

    public static Vector2 Closest(Vector2 p, Vector2 a, Vector2 b)
    {
        var d = b - a;
        var l = d.X * d.X + d.Y * d.Y;
        if (l < 1e-12f) return a;
        var t = Math.Clamp(((p.X - a.X) * d.X + (p.Y - a.Y) * d.Y) / l, 0f, 1f);
        return a + d * t;
    }

    /// <summary>Intersection of segments pq and ab: the point and the parameter along pq, or null.</summary>
    public static (Vector2 Point, float T)? Intersect(Vector2 p, Vector2 q, Vector2 a, Vector2 b)
    {
        var d1 = q - p;
        var d2 = b - a;
        var den = d1.X * d2.Y - d1.Y * d2.X;
        if (MathF.Abs(den) < 1e-9f) return null;
        var t = ((a.X - p.X) * d2.Y - (a.Y - p.Y) * d2.X) / den;
        var s = ((a.X - p.X) * d1.Y - (a.Y - p.Y) * d1.X) / den;
        if (t < 0 || t > 1 || s < 0 || s > 1) return null;
        return (p + d1 * t, t);
    }

    public static Vector2 Unit(Vector2 v)
    {
        var length = MathF.Sqrt(v.X * v.X + v.Y * v.Y);
        return length > 1e-9f ? v / length : Vector2.Zero;
    }

    /// <summary>
    /// Building footprints of a feature by the same rule as the generic generator: every part whose kind is building,
    /// otherwise the feature itself when its kind is building.
    /// </summary>
    public static IEnumerable<IReadOnlyList<Vector2>> BuildingRings(WorldFeature f)
    {
        if (f.Parts.Count > 0)
        {
            foreach (var part in f.Parts)
            {
                var kind = part.Props is { ValueKind: JsonValueKind.Object } props && props.TryGetProperty("kind", out var k) &&
                    k.ValueKind == JsonValueKind.String ? k.GetString() : f.Kind.Value;
                if (kind != "building") continue;
                foreach (var rings in Polygons(part.Geometry)) yield return Clean(rings.Outer);
            }
            yield break;
        }
        if (f.Kind.Value != "building") yield break;
        foreach (var rings in Polygons(f.Geometry)) yield return Clean(rings.Outer);
    }
}
