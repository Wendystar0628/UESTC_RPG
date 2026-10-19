using Campus.Gen.Geometry;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Numerics;
using System.Text;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Maps;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Buffer;
using NtsGeometry = NetTopologySuite.Geometries.Geometry;

namespace Campus.Gen.World;

public sealed record WorldLoadOptions
{
    public IInteriorIndex? Interiors { get; init; }
    public IOutskirtsModel? Outskirts { get; init; }
    public IFidelityModel? Fidelity { get; init; }
}

public sealed class WorldDataException(string file, string entry, string message, Exception? inner = null)
    : Exception($"{file} [{entry}]: {message}", inner)
{
    public string File { get; } = file;
    public string Entry { get; } = entry;
}

/// <summary>Pure parser. The kernel supplies only the active map's relative filenames and bytes.</summary>
public static partial class WorldModelLoader
{
    /// <summary>Fallback storey height when a building's world record gives no height.</summary>
    public const float DefaultStoreyHeight = 3.6f;
    private static readonly Encoding Utf8 = new UTF8Encoding(false, true);
    private static readonly GeometryFactory Factory = new();

    /// <summary>Load a world without terrain; terrain files require the descriptor-based production entry.</summary>
    public static WorldModelLoadResult Load(MapId map, IReadOnlyDictionary<string, ReadOnlyMemory<byte>> files, WorldLoadOptions? options = null) =>
        Load(map, files.ToDictionary(p => p.Key, p => Decode(p.Key, p.Value), StringComparer.Ordinal), options);

    /// <summary>The frozen map descriptor and bytes compile one terrain before world publication.</summary>
    public static WorldModelLoadResult LoadWorld(WorldModelLoadInputs inputs,
        IReadOnlyDictionary<string, ReadOnlyMemory<byte>> files, WorldLoadOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(inputs);
        return LoadChecked(inputs.Descriptor.Id,
            files.ToDictionary(p => p.Key, p => Decode(p.Key, p.Value), StringComparer.Ordinal), options, inputs);
    }

    private static string Decode(string file, ReadOnlyMemory<byte> bytes)
    { try { return Utf8.GetString(bytes.Span).TrimStart('\uFEFF'); } catch (DecoderFallbackException error) { throw new WorldDataException(file, "encoding", "Invalid UTF-8.", error); } }

    public static WorldModelLoadResult Load(MapId map, IReadOnlyDictionary<string, string> files, WorldLoadOptions? options = null)
        => LoadChecked(map, files, options, null);

    private static WorldModelLoadResult LoadChecked(MapId map, IReadOnlyDictionary<string, string> files,
        WorldLoadOptions? options, WorldModelLoadInputs? inputs)
    {
        try { return LoadCore(map, files, options ?? new(), inputs); }
        catch (Exception error) when (error is JsonException or ArgumentException or InvalidOperationException or KeyNotFoundException or FormatException)
        { throw new WorldDataException("world/geo.json", "root", error.Message, error); }
    }

    private static WorldModelLoadResult LoadCore(MapId map, IReadOnlyDictionary<string, string> files,
        WorldLoadOptions options, WorldModelLoadInputs? inputs)
    {
        var input = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var pair in files.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            var key = pair.Key.Replace('\\', '/');
            if (key.StartsWith('/') || key.Contains(':') || key.Split('/').Any(s => s is ".." or "." or ""))
                throw new WorldDataException(pair.Key, "path", "Expected a relative path within the current map.");
            if (key.StartsWith("overlay/", StringComparison.Ordinal) && key["overlay/".Length..].Split('/')[0].EndsWith(".d", StringComparison.Ordinal))
                throw new WorldDataException(key, "path", "Retired feature overlay input; edit world/<zone>.json.");
            if (!input.TryAdd(key, pair.Value)) throw new WorldDataException(key, "path", "Duplicate normalized path.");
        }
        const string geoFile = "world/geo.json";
        if (!input.TryGetValue(geoFile, out var geoText)) throw new WorldDataException(geoFile, "root", "Required map data is missing.");
        using var document = Parse(geoFile, geoText);
        var root = document.RootElement;
        var origin = root.GetProperty("origin");
        var limits = root.GetProperty("bounds");
        var geo = new GeoFrame(origin[0].GetDouble(), origin[1].GetDouble(),
            int.Parse(Text(root, "crs").Split(':')[^1], CultureInfo.InvariantCulture),
            new(Finite(limits[0]), Finite(limits[1])), new(Finite(limits[2]), Finite(limits[3])));
        if (geo.BoundaryMin.X >= geo.BoundaryMax.X || geo.BoundaryMin.Y >= geo.BoundaryMax.Y)
            throw new WorldDataException(geoFile, "bounds", "Invalid bounds.");
        var boundary = Value<PolygonRings>(root.GetProperty("boundary"));
        var prefix = Text(root, "entityPrefix");
        var features = new SortedDictionary<string, WorldFeature>(StringComparer.Ordinal);
        var partIds = new HashSet<string>(StringComparer.Ordinal);
        var lineIds = new HashSet<FeatureId>();
        var treeIds = new HashSet<FeatureId>();
        var roads = new List<NetworkLine>();
        var waterways = new List<NetworkLine>();
        var trees = new List<NetworkTree>();
        foreach (var (file, text) in input.Where(pair => pair.Key.StartsWith("world/", StringComparison.Ordinal)
            && pair.Key.EndsWith(".json", StringComparison.Ordinal) && pair.Key.Split('/').Length == 2 && pair.Key != geoFile))
        {
            using var zoneDocument = Parse(file, text);
            var zone = new ZoneId(file["world/".Length..^".json".Length]);
            foreach (var raw in zoneDocument.RootElement.GetProperty("features").EnumerateArray())
            {
                var id = Text(raw, "id");
                try
                {
                    var kind = new DesignKind(Text(raw, "kind"));
                    var parts = new List<WorldFeaturePart>();
                    if (raw.TryGetProperty("parts", out var members))
                        foreach (var member in members.EnumerateArray())
                        {
                            var partId = Text(member, "id");
                            if (!partIds.Add(partId)) throw new WorldDataException(file, partId, "Duplicate part ID.");
                            Polyline? line = member.TryGetProperty("line", out var nodes) ? new(Value<Vector2[]>(nodes)) : null;
                            var width = OptionalFloat(member, "width") ?? 0;
                            var geometry = member.TryGetProperty("geometry", out var shape) ? Value<OverlayGeometry>(shape)
                                : Buffer(line ?? throw new ArgumentException("Missing part geometry: " + partId), width, boundary)
                                    ?? throw new ArgumentException("Empty buffered part: " + partId);
                            var part = new WorldFeaturePart(partId, geometry, OptionalFloat(member, "height"), OptionalInteger(member, "storeys"))
                            {
                                Props = Props(member),
                                FacadeGeometry = ReadFacadeGeometry(member, WorldFeatureGeometry.PolygonsOf(geometry))
                            };
                            parts.Add(part);
                            if (line is not null)
                                AddLine(new(new(OsmFeatureIds.FromPart(prefix, partId).Source), line, width) { Props = part.Props }, kind.Value, file);
                        }
                    var combined = parts.Count == 0 ? Value<OverlayGeometry>(raw.GetProperty("geometry"))
                        : Geometry(parts.SelectMany(part => WorldFeatureGeometry.PolygonsOf(part.Geometry)).ToArray());
                    var height = OptionalFloat(raw, "height");
                    var feature = new WorldFeature(new(id), kind, combined)
                    {
                        Parts = parts.ToArray(), Zone = zone, Name = OptionalText(raw, "name"), Height = height,
                        Storeys = OptionalInteger(raw, "storeys"), Props = Props(raw), Bounds = BoundsOf(combined, height ?? 0),
                        Family = OptionalText(raw, "family") is { } family ? new(family) : null,
                        Facade = OptionalText(raw, "facade") is { } facade ? new(facade) : null
                    };
                    if (!features.TryAdd(id, feature)) throw new WorldDataException(file, id, "Duplicate world feature ID.");
                    var sourceLine = OsmFeatureIds.TryFromSource(prefix, id, out _);
                    var designRoad = kind.Value == "road" && feature.TryText("highway") is "footway" or "path" or "pedestrian";
                    if (parts.Count == 0 && combined is PolylineGeometry polyline && (sourceLine || designRoad && feature.TryText("area") != "yes"))
                        AddLine(new(feature.Id, polyline.Line, feature.TryNumber("width") ?? 0) { Props = feature.Props }, kind.Value, file);
                }
                catch (Exception error) when (error is JsonException or ArgumentException or InvalidOperationException or KeyNotFoundException or FormatException)
                { throw new WorldDataException(file, id, error.Message, error); }
            }
            foreach (var raw in zoneDocument.RootElement.GetProperty("trees").EnumerateArray())
            {
                var tree = Value<NetworkTree>(raw);
                if (!treeIds.Add(tree.Id)) throw new WorldDataException(file, tree.Id.Value, "Duplicate tree ID.");
                if (!VectorMath.Finite(tree.Position)) throw new WorldDataException(file, tree.Id.Value, "Invalid tree coordinate.");
                trees.Add(tree);
            }
        }
        var orderedRoads = OrderNetwork(roads, line => line.Id, prefix).ToArray();
        var orderedWater = OrderNetwork(waterways, line => line.Id, prefix).ToArray();
        var network = new WorldNetwork(orderedRoads, orderedWater,
            orderedRoads.Where(line => line.Props.TryText("bridge") == "yes").ToArray(),
            orderedWater.Where(line => line.Props.TryText("tunnel") == "culvert").ToArray(),
            OrderNetwork(trees, tree => tree.Id, prefix).ToArray());
        var frozen = new ReadOnlyDictionary<FeatureId, WorldFeature>(features.Values.ToDictionary(f => f.Id));
        var interiorOverlays = ReadInteriorOverlays(map, input, frozen);
        var sources = ReadTerrainSources(map, input);
        var (terrain, diagnostics) = CompileTerrain(inputs, sources, boundary, frozen, network, prefix);
        var interiors = options.Interiors ?? new WorldInteriorIndex(frozen.Values, input.GetValueOrDefault("interiors.json"),
            inputs is null ? null : terrain);
        if (input.ContainsKey("outskirts.json") && options.Outskirts is null)
            throw new WorldDataException("outskirts.json", "root", "A parsed outskirts model is required until the producer's file contract is available.");
        var outskirts = options.Outskirts;
        var world = new WorldModelSnapshot(frozen, boundary, network, interiors, outskirts, interiorOverlays, options,
            TerrainOperations(sources), terrain);
        var limitRing = outskirts?.CutLine is { Count: >= 3 } cutLine ? cutLine : boundary.Outer;
        var roam = ReadRoam(map, input);
        var margin = outskirts is null ? roam?.FallbackMargin ?? 0 : 0;
        var bounds = new Box3(new(limitRing.Min(p => p.X) - margin, roam?.FloorY ?? 0, limitRing.Min(p => p.Y) - margin),
            new(limitRing.Max(p => p.X) + margin, roam?.CeilY ?? Math.Max(1, frozen.Count > 0 ? frozen.Values.Max(f => f.Bounds.Max.Y) : 0), limitRing.Max(p => p.Y) + margin));
        return new(world, geo, PlanGeometry.Centroid(boundary.Outer), bounds, TileGrid.At(bounds.Min), TileGrid.At(bounds.Max))
        { Diagnostics = diagnostics };

        void AddLine(NetworkLine line, string kind, string file)
        {
            if (!lineIds.Add(line.Id)) throw new WorldDataException(file, line.Id.Value, "Duplicate network line ID.");
            if (line.Line.Points.Count < 2 || line.Width < 0 || !float.IsFinite(line.Width) || line.Line.Points.Any(p => !VectorMath.Finite(p)))
                throw new WorldDataException(file, line.Id.Value, "Invalid centerline or width.");
            if (kind == "road" || line.Props.TryText("highway") is not null) roads.Add(line);
            else if (kind is "water" or "weir" || line.Props.TryText("waterway") is not null) waterways.Add(line);
            else throw new WorldDataException(file, line.Id.Value, "Unclassified network line.");
        }
    }

    private static IEnumerable<T> OrderNetwork<T>(IEnumerable<T> items, Func<T, FeatureId> id, string prefix) => items
        .OrderBy(item => OsmFeatureIds.TryFromSource(prefix, id(item).Value, out _) ? 0 : 1)
        .ThenBy(item => OsmFeatureIds.TryFromSource(prefix, id(item).Value, out _) ? long.Parse(id(item).Value.Split(':')[^1], CultureInfo.InvariantCulture) : 0)
        .ThenBy(item => id(item).Value, StringComparer.Ordinal);

    private static IReadOnlyList<InteriorOverlayFile> ReadInteriorOverlays(MapId map, SortedDictionary<string, string> input,
        IReadOnlyDictionary<FeatureId, WorldFeature> features)
    {
        var overlays = new List<InteriorOverlayFile>();
        foreach (var (file, text) in input.Where(pair => pair.Key.StartsWith("overlay/interior/", StringComparison.Ordinal)
            && pair.Key.EndsWith(".interior.json", StringComparison.Ordinal)))
        {
            var overlay = Deserialize<InteriorOverlayFile>(file, text);
            CheckMap(map, overlay.Map, file);
            foreach (var op in overlay.Ops)
            {
                var id = op switch { SealOp seal => seal.Building, InteriorConfigOp config => config.Building, _ => throw new WorldDataException(file, "op", "Unknown interior operation.") };
                if (!features.ContainsKey(id)) throw new WorldDataException(file, id.Value, "Target feature does not exist.");
            }
            overlays.Add(overlay);
        }
        return overlays.AsReadOnly();
    }

    private static RoamLimits? ReadRoam(MapId map, SortedDictionary<string, string> input)
    {
        if (!input.TryGetValue("map.json", out var text)) return null;
        using var document = Parse("map.json", text);
        var root = document.RootElement;
        if (OptionalText(root, "id") is { } id && id != map.Value) throw new WorldDataException("map.json", "id", "Map descriptor belongs to another map.");
        if (!root.TryGetProperty("roam", out var element)) throw new WorldDataException("map.json", "roam", "Required roam limits are missing.");
        var roam = Deserialize<RoamLimits>("map.json", element.GetRawText());
        if (!float.IsFinite(roam.FallbackMargin) || roam.FallbackMargin < 0 || !float.IsFinite(roam.FloorY)
            || !float.IsFinite(roam.CeilY) || roam.FloorY >= roam.CeilY)
            throw new WorldDataException("map.json", "roam", "Invalid fallback margin or vertical limits.");
        return roam;
    }

    private static OverlayGeometry? ReadFacadeGeometry(JsonElement raw, IReadOnlyList<PolygonRings> physical)
    {
        if (!raw.TryGetProperty("facadePolygons", out var value)) return null;
        var polygons = Value<PolygonRings[]>(value);
        if (polygons.Length == 0 || polygons.Length != physical.Count
            || polygons.Where((polygon, i) => polygon.Holes.Count != physical[i].Holes.Count).Any())
            throw new ArgumentException("Facade polygons and holes must correspond to the physical footprint.");
        return Geometry(polygons);
    }

    private static OverlayGeometry? Buffer(Polyline line, float width, PolygonRings boundary)
    {
        var alignment = Factory.CreateLineString(line.Points.Select(point => new Coordinate(point.X, point.Y)).ToArray());
        var parameters = new BufferParameters { EndCapStyle = EndCapStyle.Flat, JoinStyle = JoinStyle.Mitre };
        var clipped = BufferOp.Buffer(alignment, width / 2d, parameters).Intersection(Polygon(boundary));
        var polygons = Polygons(clipped).Select(polygon => new PolygonRings(Ring(polygon.ExteriorRing),
            Enumerable.Range(0, polygon.NumInteriorRings).Select(index => Ring(polygon.GetInteriorRingN(index))).ToArray())).ToArray();
        return polygons.Length == 0 ? null : Geometry(polygons);
    }

    private static Polygon Polygon(PolygonRings polygon) => Factory.CreatePolygon(Closed(polygon.Outer), polygon.Holes.Select(Closed).ToArray());
    private static LinearRing Closed(IReadOnlyList<Vector2> points) => Factory.CreateLinearRing(points.Select(point => new Coordinate(point.X, point.Y)).Append(new(points[0].X, points[0].Y)).ToArray());
    private static IReadOnlyList<Vector2> Ring(LineString ring) => ring.Coordinates.Take(ring.NumPoints - 1).Select(point => new Vector2((float)point.X, (float)point.Y)).ToArray();
    private static IEnumerable<Polygon> Polygons(NtsGeometry geometry)
    {
        if (geometry.IsEmpty) yield break;
        if (geometry is Polygon polygon) { yield return polygon; yield break; }
        if (geometry is GeometryCollection collection)
            for (var i = 0; i < collection.NumGeometries; i++) foreach (var item in Polygons(collection.GetGeometryN(i))) yield return item;
    }

    private static void CheckMap(MapId map, MapId? declared, string file)
    { if (declared is { } id && id != map) throw new WorldDataException(file, "map", "Overlay belongs to a different map."); }
    internal static OverlayGeometry Geometry(IReadOnlyList<PolygonRings> polygons) => polygons.Count == 1 ? new PolygonGeometry(polygons[0]) : new MultiPolygonGeometry(polygons);
    internal static Box3 BoundsOf(OverlayGeometry geometry, float height)
    {
        if (!float.IsFinite(height) || height < 0) throw new ArgumentException("Invalid feature height.");
        if (geometry is PointGeometry point) return new(point.Position, point.Position);
        var points = geometry is PolylineGeometry line ? line.Line.Points : WorldFeatureGeometry.PolygonsOf(geometry).SelectMany(p => p.Outer).ToArray();
        if (points.Count == 0) throw new ArgumentException("Empty feature geometry.");
        return new(new(points.Min(p => p.X), 0, points.Min(p => p.Y)), new(points.Max(p => p.X), height, points.Max(p => p.Y)));
    }
    private static T Value<T>(JsonElement element) => element.Deserialize<T>(ContractJson.Options) ?? throw new JsonException("Null value.");
    private static JsonElement? Props(JsonElement element) => element.TryGetProperty("props", out var value) ? value.Clone() : null;
    private static int? OptionalInteger(JsonElement element, string key)
    {
        if (!element.TryGetProperty(key, out var value) || value.ValueKind == JsonValueKind.Null) return null;
        if (!value.TryGetDecimal(out var number) || number != decimal.Truncate(number) || number < int.MinValue || number > int.MaxValue)
            throw new JsonException($"{key} must be an integer in the Int32 range.");
        return (int)number;
    }
    internal static float Finite(JsonElement e) { var n = e.GetSingle(); return float.IsFinite(n) ? n : throw new JsonException("Non-finite number."); }
    internal static string Text(JsonElement e, string key) => e.GetProperty(key).GetString() ?? throw new JsonException($"Missing {key}.");
    internal static string? OptionalText(JsonElement e, string key) => e.TryGetProperty(key, out var value) && value.ValueKind != JsonValueKind.Null ? value.GetString() : null;
    internal static float? OptionalFloat(JsonElement e, string key) => e.TryGetProperty(key, out var value) && value.ValueKind != JsonValueKind.Null ? Finite(value) : null;
    internal static JsonDocument Parse(string file, string text)
    { try { return JsonDocument.Parse(text); } catch (JsonException error) { throw new WorldDataException(file, error.Path ?? "root", error.Message, error); } }
    internal static T Deserialize<T>(string file, string text)
    { try { return JsonSerializer.Deserialize<T>(text, ContractJson.Options) ?? throw new JsonException("Null document."); } catch (JsonException error) { throw new WorldDataException(file, error.Path ?? "root", error.Message, error); } }
}
