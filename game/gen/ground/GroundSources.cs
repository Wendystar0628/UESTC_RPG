using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Gen.World;
using Campus.Gen.Geometry;

namespace Campus.Gen.Ground;

/// <summary>
/// The only place that reads the world model's feature and network records: everything downstream works on
/// <see cref="GroundShape"/> and <see cref="RoadLine"/>.
/// A feature with <see cref="WorldFeature.Parts"/> is read part by part and its aggregate geometry is never built: each
/// part's surface kind comes from its own source record (<c>kind</c> in the part's props, else the parent's kind),
/// the type properties (highway, sport, width) from that same record, and the shapes from the part's own geometry.
/// A multi-polygon part gives one shape per polygon, each with its own holes, so a hole stays a hole and polygons
/// never flip each other's inside. A feature without parts is read from its own kind, props and geometry.
/// Centre lines come from the effective road network (<see cref="OwnedByNetwork"/>); areas are always laid as areas.
/// Output order is stable (feature ID, part source ID, polygon order), so builds are deterministic.
/// </summary>
internal static class GroundSources
{
    public static (List<GroundShape> Shapes, List<RoadLine> Roads) Read(IWorldModel world)
    {
        // Centre lines held by the network are laid from there and only there.
        var network = world.Network.Roads.Select(l => l.Id).ToHashSet();
        var shapes = new List<GroundShape>();
        foreach (var feature in world.Features.Values.OrderBy(f => f.Id.Value, StringComparer.Ordinal))
        {
            if (feature.Kind.Value == PitKind)
            {
                AddPits(shapes, feature);
                continue;
            }
            foreach (var source in Sources(feature))
            {
                if (OwnedByNetwork(feature, source, network)) continue;
                AddSurfaces(shapes, feature.Id, source);
            }
        }

        var roads = new List<RoadLine>();
        foreach (var line in world.Network.Roads.OrderBy(l => l.Id.Value, StringComparer.Ordinal))
        {
            // area=yes marks an area perimeter, not a paved centre line.
            if (WorldProps.TryText(line.Props, "area") == "yes") continue;
            var points = PlanLines.CleanPath(line.Line.Points);
            if (points.Length < 2) continue;
            var type = GroundClassifier.RoadType(line.Props);
            var width = GroundClassifier.Width(line.Props, type, line.Width);
            var kind = GroundClassifier.RoadKind(type);
            roads.Add(new(line.Id, points, width, kind == SurfaceKind.Carriageway, type));
            // Every centre line also contributes a capsule chain: it gives roads without a polygon a surface, and where
            // polygons exist it rounds their joins (buffered OSM ways meet with flat caps and leave notches at bends).
            if (Capsule(line.Id, kind, points, width) is { } capsule) shapes.Add(capsule);
        }
        AddBridgeFootprints(world, roads, shapes);
        // Rounded kerbs at junction corners, as extra carriageway polygons merged by the union.
        var fillets = RoadGraph.Fillets(roads);
        for (var k = 0; k < fillets.Count; k++)
            if (Polygon(new FeatureId("fillet:" + k.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                    SurfaceKind.Carriageway, new PolygonRings(fillets[k])) is { } fillet)
                shapes.Add(fillet);
        return (shapes, roads);
    }

    /// <summary>The deck's edge margin beyond the outermost lines (stone base, abutment bearing) that no lawn shows through.</summary>
    private const float BridgeMargin = 0.6f;
    /// <summary>The footprint runs this far past each end of the bridge's lines, onto the banks (the deck's bearing and its approach slab).</summary>
    private const float BridgeEnd = 3f;

    private static Vector2[] Extended(Vector2[] points)
    {
        var list = points.ToList();
        var head = Vector2.Normalize(list[0] - list[1]);
        var tail = Vector2.Normalize(list[^1] - list[^2]);
        list[0] += head * BridgeEnd;
        list[^1] += tail * BridgeEnd;
        return list.ToArray();
    }

    /// <summary>
    /// A registered bridge deck (game/gen/maps/qingshuihe/waterfront/'s "bridge-deck" overlay point: its axis line, the footway lines it joins and the sidewalk it adds)
    /// leaves no lawn inside its footprint (Opus review 28a): the land under it, where the deck meets the banks, is paved like a walk.
    /// Over water the water stays. The network lines themselves are paved as ordinary roads along their whole length, so the stretch of
    /// a bridge line that lies on land is road like any other.
    /// </summary>
    private static void AddBridgeFootprints(IWorldModel world, List<RoadLine> roads, List<GroundShape> shapes)
    {
        foreach (var feature in world.Features.Values.OrderBy(f => f.Id.Value, StringComparer.Ordinal))
        {
            if (feature.Kind.Value != "bridge-deck" || feature.Props is not { ValueKind: JsonValueKind.Object } props) continue;
            var ids = new List<string>();
            if (WorldProps.TryText(props, "axis") is { } axis) ids.Add(axis);
            if (props.TryGetProperty("walks", out var walks) && walks.ValueKind == JsonValueKind.Array)
                foreach (var w in walks.EnumerateArray())
                    if (w.ValueKind == JsonValueKind.String) ids.Add(w.GetString()!);
            var sidewalk = props.TryGetProperty("sidewalk", out var sw) && sw.ValueKind == JsonValueKind.Number && sw.TryGetSingle(out var sv) ? sv : 0f;
            foreach (var id in ids.Order(StringComparer.Ordinal))
            {
                var line = roads.FirstOrDefault(r => r.Id.Value == id);
                if (line is null) continue;
                var width = line.Width + (id == ids[0] ? 2f * sidewalk : 0f) + 2f * BridgeMargin;
                if (Capsule(new FeatureId(feature.Id.Value + ":footprint:" + id), SurfaceKind.Walk, Extended(line.Points), width) is { } capsule) shapes.Add(capsule);
            }
        }
    }


    /// <summary>
    /// One source record of a feature; <see cref="Label"/> names it in error messages. <see cref="Entity"/> is the owning
    /// entity's record (its OSM tags), which a part's own record does not repeat.
    /// </summary>
    private readonly record struct Source(string Label, string Kind, JsonElement? Props, WorldGeometry Geometry,
        JsonElement? Entity = null, string? PartSourceId = null);

    private static IEnumerable<Source> Sources(WorldFeature feature)
    {
        if (feature.Parts.Count == 0)
        {
            yield return new("its geometry", feature.Kind.Value, feature.Props, feature.Geometry, feature.Props);
            yield break;
        }
        foreach (var part in feature.Parts.OrderBy(p => p.SourceId, StringComparer.Ordinal))
            yield return new("part " + part.SourceId, PartKind(feature, part), part.Props ?? feature.Props, part.Geometry, feature.Props, part.SourceId);
    }

    /// <summary>
    /// Kind of one part: its source record's own "kind", else the parent feature's kind (the world model's part filter
    /// and the building planner use the same rule). An entity whose own kind is poi can still own building parts.
    /// </summary>
    private static string PartKind(WorldFeature feature, WorldFeaturePart part) =>
        WorldProps.TryText(part.Props, "kind") ?? feature.Kind.Value;

    /// <summary>
    /// True for a road drawn as a centre line whose effective copy lives in <c>Network.Roads</c>, so that copy is the one
    /// laid (a capsule per line, in <see cref="Read"/>). The feature's ID must be in the network. Areas never match: a polygon or a
    /// multi-polygon, holes included, is laid as the area it is, whatever the network holds under its ID.
    /// </summary>
    private static bool OwnedByNetwork(WorldFeature feature, Source source, HashSet<FeatureId> network)
    {
        return source.Geometry is PolylineGeometry && source.Kind == "road" && network.Contains(feature.Id);
    }

    /// <summary>Overlay kind of a tree pit or planting strip.</summary>
    internal const string PitKind = "tree-pit";

    /// <summary>
    /// Tree pits and planting strips: one pit shape per polygon of the feature, its frame heights from the record
    /// (<c>height</c> is the frame top, <c>soilAbove</c> the soil top, <c>kerb</c> the frame thickness). A record whose
    /// props say <c>stone: site</c> has its stone built by a site package, so the ground leaves it alone. A point (a
    /// round bed) has no outline and is never the ground's.
    /// </summary>
    private static void AddPits(List<GroundShape> shapes, WorldFeature feature)
    {
        if (WorldProps.TryText(feature.Props, "stone") == "site") return;
        var profile = new PitProfile(feature.Height, WorldProps.TryNumber(feature.Props, "soilAbove"), WorldProps.TryNumber(feature.Props, "kerb"));
        switch (feature.Geometry)
        {
            case PolygonGeometry polygon:
                if (Polygon(feature.Id, SurfaceKind.Pit, polygon.Rings, pit: profile) is { } shape) shapes.Add(shape);
                break;
            case MultiPolygonGeometry multi:
                foreach (var rings in multi.Polygons)
                    if (Polygon(feature.Id, SurfaceKind.Pit, rings, pit: profile) is { } piece) shapes.Add(piece);
                break;
        }
    }

    private static void AddSurfaces(List<GroundShape> shapes, FeatureId id, Source source)
    {
        if (GroundClassifier.KindOf(source.Kind, source.Props, source.Entity) is not { } kind) return;
        // Only a record that names a sport is a marked pitch; a pitch without a sport tag is plain grass (no white lines).
        var marked = kind == SurfaceKind.Turf && WorldProps.TryText(source.Props, "sport") is { Length: > 0 };
        void Add(GroundShape? shape)
        {
            if (shape is null) return;
            shape.PartSourceId = source.PartSourceId;
            if (shape.DirectionPath is null && source.Geometry is PolylineGeometry line)
                shape.DirectionPath = PlanLines.CleanPath(line.Line.Points);
            shapes.Add(shape);
        }
        switch (source.Geometry)
        {
            case PolygonGeometry polygon:
                Add(Surface(id, kind, polygon.Rings, marked));
                break;
            case MultiPolygonGeometry multi:
                foreach (var rings in multi.Polygons)
                    Add(Surface(id, kind, rings, marked));
                break;
            case PolylineGeometry when kind == SurfaceKind.Building:
                break;   // a footprint needs an area
            case PolylineGeometry polyline when kind == SurfaceKind.Track:
                Add(SportGround.TrackLine(id, polyline.Line.Points, WorldProps.TryNumber(source.Props, "width")));
                break;
            case PolylineGeometry polyline:
                var type = GroundClassifier.RoadType(source.Props);
                var width = GroundClassifier.Width(source.Props, type, 0f);
                Add(Capsule(id, kind, polyline.Line.Points, width));
                break;
            case PointGeometry:
                break;   // an entity known only by its anchor has no extent to shape the ground
            default:
                throw new InvalidOperationException(
                    $"Unsupported ground geometry {source.Geometry.GetType().Name} in {source.Label} of {id.Value} (kind {source.Kind}).");
        }
    }

    /// <summary>A polygon source as a ground shape; a running track polygon becomes a ring (<see cref="SportGround.TrackRing"/>).</summary>
    private static GroundShape? Surface(FeatureId id, SurfaceKind kind, PolygonRings rings, bool marked) =>
        kind == SurfaceKind.Track ? SportGround.TrackRing(id, rings) : Polygon(id, kind, rings, marked);

    internal static GroundShape? Polygon(FeatureId id, SurfaceKind kind, PolygonRings rings, bool marked = false, PitProfile? pit = null)
    {
        var list = new List<Vector2[]>();
        var outer = PlanLines.CleanPath(rings.Outer);
        if (outer.Length < 3) return null;
        list.Add(outer);
        foreach (var hole in rings.Holes)
        {
            var h = PlanLines.CleanPath(hole);
            if (h.Length >= 3) list.Add(h);
        }
        var min = outer[0];
        var max = outer[0];
        foreach (var p in outer) { min = Vector2.Min(min, p); max = Vector2.Max(max, p); }
        return new GroundShape { Kind = kind, Id = id, Rings = list.ToArray(), Min = min, Max = max, Marked = marked, Pit = pit };
    }

    private static GroundShape? Capsule(FeatureId id, SurfaceKind kind, IReadOnlyList<Vector2> line, float width)
    {
        var points = PlanLines.CleanPath(line);
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
            DirectionPath = points,
            HalfWidth = half,
            Min = min - new Vector2(half),
            Max = max + new Vector2(half)
        };
    }

}
