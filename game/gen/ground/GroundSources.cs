using System.Numerics;
using System.Text.Json;
using Campus.Contracts;

namespace Campus.Gen.Ground;

/// <summary>
/// The only place that reads the world model's feature and network records: everything downstream works on
/// <see cref="GroundShape"/> and <see cref="RoadLine"/>.
/// A feature with <see cref="WorldFeature.Parts"/> is read part by part and its aggregate geometry is never built: each
/// part's surface kind comes from its own source record (<c>kind</c> in the part's props, else the parent's kind),
/// the type properties (roadType, sport, width) from that same record, and the shapes from the part's own geometry.
/// A multi-polygon part gives one shape per polygon, each with its own holes, so a hole stays a hole and polygons
/// never flip each other's inside. A feature without parts is read from its own kind, props and geometry.
/// Output order is stable (feature ID, part source ID, polygon order), so builds are deterministic.
/// </summary>
internal static class GroundSources
{
    public static (List<GroundShape> Shapes, List<RoadLine> Roads) Read(IWorldModel world)
    {
        var shapes = new List<GroundShape>();
        foreach (var feature in world.Features.Values.OrderBy(f => f.Id.Value, StringComparer.Ordinal))
            foreach (var source in Sources(feature))
                AddSurfaces(shapes, feature.Id, source);

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

    /// <summary>One source record of a feature; <see cref="Label"/> names it in error messages.</summary>
    private readonly record struct Source(string Label, string Kind, JsonElement? Props, OverlayGeometry Geometry);

    private static IEnumerable<Source> Sources(WorldFeature feature)
    {
        if (feature.Parts.Count == 0)
        {
            yield return new("its geometry", feature.Kind.Value, feature.Props, feature.Geometry);
            yield break;
        }
        foreach (var part in feature.Parts.OrderBy(p => p.SourceId, StringComparer.Ordinal))
            yield return new("part " + part.SourceId, PartKind(feature, part), part.Props ?? feature.Props, part.Geometry);
    }

    /// <summary>
    /// Kind of one part: its source record's own "kind", else the parent feature's kind (the world model's part filter
    /// and the building planner use the same rule). An entity whose own kind is poi can still own building parts.
    /// </summary>
    private static string PartKind(WorldFeature feature, WorldFeaturePart part) =>
        GroundClassifier.Text(part.Props, "kind") ?? feature.Kind.Value;

    private static void AddSurfaces(List<GroundShape> shapes, FeatureId id, Source source)
    {
        if (GroundClassifier.KindOf(source.Kind, source.Props) is not { } kind) return;
        switch (source.Geometry)
        {
            case PolygonGeometry polygon:
                if (Polygon(id, kind, polygon.Rings) is { } shape) shapes.Add(shape);
                break;
            case MultiPolygonGeometry multi:
                foreach (var rings in multi.Polygons)
                    if (Polygon(id, kind, rings) is { } piece) shapes.Add(piece);
                break;
            case PolylineGeometry when kind == SurfaceKind.Building:
                break;   // a footprint needs an area
            case PolylineGeometry polyline:
                var type = GroundClassifier.RoadType(source.Props);
                var width = GroundClassifier.Width(source.Props, type, 0f);
                if (Capsule(id, kind, polyline.Line.Points, width) is { } capsule) shapes.Add(capsule);
                break;
            case PointGeometry:
                break;   // an entity known only by its anchor has no extent to shape the ground
            default:
                throw new InvalidOperationException(
                    $"Unsupported ground geometry {source.Geometry.GetType().Name} in {source.Label} of {id.Value} (kind {source.Kind}).");
        }
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
