using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.Ground;

/// <summary>
/// The only place that reads the world model's feature and network records. When WorldFeature gains per-source
/// parts, only this adapter changes: everything downstream works on <see cref="GroundShape"/> and <see cref="RoadLine"/>.
/// Output order is stable (sorted by feature ID), so builds are deterministic.
/// </summary>
internal static class GroundSources
{
    public static (List<GroundShape> Shapes, List<RoadLine> Roads) Read(IWorldModel world)
    {
        var shapes = new List<GroundShape>();
        var polygonIds = new HashSet<FeatureId>();
        foreach (var feature in world.Features.Values.OrderBy(f => f.Id.Value, StringComparer.Ordinal))
        {
            if (GroundClassifier.KindOf(feature) is not { } kind) continue;
            switch (feature.Geometry)
            {
                case PolygonGeometry polygon:
                    if (Polygon(feature.Id, kind, polygon.Rings) is { } shape)
                    {
                        shapes.Add(shape);
                        if (kind != SurfaceKind.Building) polygonIds.Add(feature.Id);
                    }
                    break;
                case PolylineGeometry when kind == SurfaceKind.Building:
                    break;
                case PolylineGeometry polyline:
                    var type = GroundClassifier.RoadType(feature.Props);
                    var width = GroundClassifier.Width(feature.Props, type, 0f);
                    if (Capsule(feature.Id, kind, polyline.Line.Points, width) is { } capsule) shapes.Add(capsule);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported ground geometry for {feature.Id.Value}.");
            }
        }

        var roads = new List<RoadLine>();
        foreach (var line in world.Network.Roads.OrderBy(l => l.Id.Value, StringComparer.Ordinal))
        {
            var points = Clean(line.Line.Points);
            if (points.Length < 2) continue;
            var type = GroundClassifier.RoadType(line.Props);
            var width = GroundClassifier.Width(line.Props, type, line.Width);
            var kind = GroundClassifier.RoadKind(type);
            roads.Add(new(line.Id, points, width, kind == SurfaceKind.Carriageway, type));
            // Every centre line also contributes a capsule chain: it gives roads without a polygon a surface, and where
            // polygons exist it rounds their joins (buffered OSM ways meet with flat caps and leave notches at bends).
            if (Capsule(line.Id, kind, points, width) is { } capsule) shapes.Add(capsule);
        }
        // Rounded kerbs at junction corners, as extra carriageway polygons merged by the union.
        var fillets = RoadGraph.Fillets(roads);
        for (var k = 0; k < fillets.Count; k++)
            if (Polygon(new FeatureId("fillet:" + k.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                    SurfaceKind.Carriageway, new PolygonRings(fillets[k])) is { } fillet)
                shapes.Add(fillet);
        return (shapes, roads);
    }

    private static GroundShape? Polygon(FeatureId id, SurfaceKind kind, PolygonRings rings)
    {
        var list = new List<Vector2[]>();
        var outer = Clean(rings.Outer);
        if (outer.Length < 3) return null;
        list.Add(outer);
        foreach (var hole in rings.Holes)
        {
            var h = Clean(hole);
            if (h.Length >= 3) list.Add(h);
        }
        var min = outer[0];
        var max = outer[0];
        foreach (var p in outer) { min = Vector2.Min(min, p); max = Vector2.Max(max, p); }
        return new GroundShape { Kind = kind, Id = id, Rings = list.ToArray(), Min = min, Max = max };
    }

    private static GroundShape? Capsule(FeatureId id, SurfaceKind kind, IReadOnlyList<Vector2> line, float width)
    {
        var points = Clean(line);
        if (points.Length < 2 || !(width > 0f)) return null;
        var half = width * 0.5f;
        var min = points[0];
        var max = points[0];
        foreach (var p in points) { min = Vector2.Min(min, p); max = Vector2.Max(max, p); }
        return new GroundShape
        {
            Kind = kind,
            Id = id,
            Line = points,
            HalfWidth = half,
            Min = min - new Vector2(half),
            Max = max + new Vector2(half)
        };
    }

    /// <summary>Drops non-finite points, consecutive duplicates and a repeated closing vertex.</summary>
    internal static Vector2[] Clean(IReadOnlyList<Vector2> points)
    {
        var result = new List<Vector2>(points.Count);
        foreach (var p in points)
        {
            if (!float.IsFinite(p.X) || !float.IsFinite(p.Y)) continue;
            if (result.Count > 0 && Vector2.DistanceSquared(result[^1], p) < 1e-8f) continue;
            result.Add(p);
        }
        if (result.Count > 2 && Vector2.DistanceSquared(result[0], result[^1]) < 1e-8f) result.RemoveAt(result.Count - 1);
        return result.ToArray();
    }

}
