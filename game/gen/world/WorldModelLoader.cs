using Campus.Contracts.Bake;
using Campus.Gen.Geometry;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
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
}

public sealed class WorldDataException(string file, string entry, string message, Exception? inner = null)
    : Exception($"{file} [{entry}]: {message}", inner)
{
    public string File { get; } = file;
    public string Entry { get; } = entry;
}

/// <summary>Pure parser. The kernel supplies only the active map's relative filenames and bytes.</summary>

public static class WorldModelLoader
{
    internal static readonly Encoding Utf8 = new UTF8Encoding(false, true);
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

    /// <summary>Parse once, then let the kernel resolve cached terrain before publishing the world.</summary>
    public static WorldModelPreparation PrepareWorld(WorldModelLoadInputs inputs,
        IReadOnlyDictionary<string, ReadOnlyMemory<byte>> files, WorldLoadOptions? options = null) =>
        PrepareCore(inputs.Descriptor.Id, files.ToDictionary(pair => pair.Key, pair => Decode(pair.Key, pair.Value), StringComparer.Ordinal), options ?? new(), inputs);

    private static string Decode(string file, ReadOnlyMemory<byte> bytes)
    { try { return Utf8.GetString(bytes.Span).TrimStart('\uFEFF'); } catch (DecoderFallbackException error) { throw new WorldDataException(file, "encoding", "Invalid UTF-8.", error); } }

    public static WorldModelLoadResult Load(MapId map, IReadOnlyDictionary<string, string> files, WorldLoadOptions? options = null)
        => LoadChecked(map, files, options, null);

    private static WorldModelLoadResult LoadChecked(MapId map, IReadOnlyDictionary<string, string> files,
        WorldLoadOptions? options, WorldModelLoadInputs? inputs)
    {
        try { return PrepareCore(map, files, options ?? new(), inputs).BuildForAssets(); }
        catch (Exception error) when (error is JsonException or ArgumentException or InvalidOperationException or KeyNotFoundException or FormatException)
        { throw new WorldDataException("world/geo.json", "root", error.Message, error); }
    }

    private static WorldModelPreparation PrepareCore(MapId map, IReadOnlyDictionary<string, string> files,
        WorldLoadOptions options, WorldModelLoadInputs? inputs)
    {
        var input = NormalizeInputs(files);
        const string geoFile = "world/geo.json";
        if (!input.TryGetValue(geoFile, out var geoText)) throw new WorldDataException(geoFile, "root", "Required map data is missing.");
        using var document = Parse(geoFile, geoText);
        var root = document.RootElement;
        var geo = ReadGeoFrame(root, geoFile);
        var boundary = Value<PolygonRings>(root.GetProperty("boundary"));
        var prefix = Text(root, "entityPrefix");
        var features = new SortedDictionary<string, WorldFeature>(StringComparer.Ordinal);
        var partIds = new HashSet<string>(StringComparer.Ordinal);
        var lineIds = new HashSet<FeatureId>();
        var treeIds = new HashSet<FeatureId>();
        var roads = new List<NetworkLine>();
        var waterways = new List<NetworkLine>();
        var trees = new List<NetworkTree>();
        ReadZones(input, geoFile, prefix, boundary, features, partIds, treeIds, trees, AddLine);
        var orderedRoads = OrderNetwork(roads, line => line.Id, prefix).ToArray();
        var orderedWater = OrderNetwork(waterways, line => line.Id, prefix).ToArray();
        var network = new WorldNetwork(orderedRoads, orderedWater,
            orderedRoads.Where(line => line.Props.TryText("bridge") == "yes").ToArray(),
            orderedWater.Where(line => line.Props.TryText("tunnel") == "culvert").ToArray(),
            OrderNetwork(trees, tree => tree.Id, prefix).ToArray());
        var frozen = new ReadOnlyDictionary<FeatureId, WorldFeature>(features.Values.ToDictionary(f => f.Id));
        var featureIds = frozen.Keys.ToHashSet();
        var roadIds = orderedRoads.Select(road => road.Id).ToHashSet();
        var roadReferences = roadIds.Concat(frozen.Values.Where(feature => WorldFeatureKinds.Parts(feature, "road").Any())
            .Select(feature => feature.Id)).ToHashSet();
        ValidateFeatureReferences(frozen, featureIds, roadIds, roadReferences);
        if (inputs is not null)
            for (var index = 0; index < inputs.Descriptor.Env.ReflectLakes.Count; index++)
                RequireReference("map.json", map.Value, $"env.reflectLakes[{index}]", inputs.Descriptor.Env.ReflectLakes[index], featureIds);
        var sources = WorldTerrainInputs.ReadSources(map, input);
        WorldTerrainInputs.ValidateReferences(sources, featureIds, roadReferences);
        var terrainContext = WorldTerrainInputs.Prepare(inputs, sources, boundary, frozen, network, prefix);
        if (input.ContainsKey("outskirts.json") && options.Outskirts is null)
            throw new WorldDataException("outskirts.json", "root", "A parsed outskirts model is required until the producer's file contract is available.");
        var outskirts = options.Outskirts;
        var operations = WorldTerrainInputs.Operations(sources);
        var limitRing = outskirts?.CutLine is { Count: >= 3 } cutLine ? cutLine : boundary.Outer;
        var roam = ReadRoam(map, input);
        var margin = outskirts is null ? roam?.FallbackMargin ?? 0 : 0;
        var bounds = new Box3(new(limitRing.Min(p => p.X) - margin, roam?.FloorY ?? 0, limitRing.Min(p => p.Y) - margin),
            new(limitRing.Max(p => p.X) + margin, roam?.CeilY ?? Math.Max(1, frozen.Count > 0 ? frozen.Values.Max(f => f.Bounds.Max.Y) : 0), limitRing.Max(p => p.Y) + margin));
        var waypoints = input.TryGetValue("waypoints.json", out var waypointText)
            ? Deserialize<WaypointFile>("waypoints.json", waypointText).Waypoints : [];
        IInteriorIndex Interiors(ITerrain terrain) => options.Interiors ??
            new WorldInteriorIndex(frozen.Values, input.GetValueOrDefault("interiors.json"), inputs is null ? null : terrain);
        WorldModelSnapshot Snapshot(ITerrain terrain, IInteriorIndex interiors, IFidelityModel fidelity) =>
            new(frozen, boundary, network, interiors, outskirts, fidelity, operations, terrain);
        var fidelitySourceStamp = Convert.ToHexStringLower(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new
        {
            world = input.Where(pair => pair.Key.StartsWith("world/", StringComparison.Ordinal) && !pair.Key.EndsWith(".terrain.json", StringComparison.Ordinal)).ToArray(),
            waypoints, interiors = input.GetValueOrDefault("interiors.json"), outskirts = outskirts?.Blocks
        }, ContractJson.Options)));
        return new(terrainContext is null ? FlatTerrain.Default.SourceInputs : TerrainSourceInputs.Prepare(terrainContext, sources),
            terrainContext is null ? null : () => TerrainCompiler.Compile(terrainContext, sources),
            fidelitySourceStamp,
            terrain => WorldFidelityCompiler.Compile(frozen, boundary, network, Interiors(terrain), outskirts, waypoints),
            (terrain, diagnostics, fidelity) =>
            {
                var world = Snapshot(terrain, Interiors(terrain), new WorldFidelityModel(fidelity));
                return new(world, geo, PlanPolygons.Centroid(boundary.Outer), bounds, TileGrid.At(bounds.Min), TileGrid.At(bounds.Max)) { Diagnostics = diagnostics };
            }, (world, terrain) => Snapshot(terrain, world.Interiors, world.Fidelity));

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

    private static GeoFrame ReadGeoFrame(JsonElement root, string geoFile)
    {
        var origin = root.GetProperty("origin");
        var limits = root.GetProperty("bounds");
        var geo = new GeoFrame(origin[0].GetDouble(), origin[1].GetDouble(),
            int.Parse(Text(root, "crs").Split(':')[^1], CultureInfo.InvariantCulture),
            new(Finite(limits[0]), Finite(limits[1])), new(Finite(limits[2]), Finite(limits[3])));
        if (geo.BoundaryMin.X >= geo.BoundaryMax.X || geo.BoundaryMin.Y >= geo.BoundaryMax.Y)
            throw new WorldDataException(geoFile, "bounds", "Invalid bounds.");
        return geo;
    }

    private static SortedDictionary<string, string> NormalizeInputs(IReadOnlyDictionary<string, string> files)
    {
        var input = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var pair in files.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            var key = pair.Key.Replace('\\', '/');
            if (key.StartsWith('/') || key.Contains(':') || key.Split('/').Any(s => s is ".." or "." or ""))
                throw new WorldDataException(pair.Key, "path", "Expected a relative path within the current map.");
            if (key == "fidelity.json" || key.StartsWith("overlay/fidelity/", StringComparison.Ordinal))
                throw new WorldDataException(key, "path", "Retired fidelity overlay input; precision is compiled from the authored world.");
            if (key.StartsWith("overlay/interior/", StringComparison.Ordinal))
                throw new WorldDataException(key,"path","Retired interior overlay input; configure the provider in interiors.json.");
            if (key.StartsWith("overlay/", StringComparison.Ordinal) && key["overlay/".Length..].Split('/')[0].EndsWith(".d", StringComparison.Ordinal))
                throw new WorldDataException(key, "path", "Retired feature overlay input; edit world/<zone>.json.");
            if (!input.TryAdd(key, pair.Value)) throw new WorldDataException(key, "path", "Duplicate normalized path.");
        }
        return input;
    }

    private static List<WorldFeaturePart> ReadParts(JsonElement raw, string kind, string file, string prefix,
        PolygonRings boundary, HashSet<string> partIds, Action<NetworkLine, string, string> AddLine)
    {
        var parts = new List<WorldFeaturePart>();
        if (raw.TryGetProperty("parts", out var members))
            foreach (var member in members.EnumerateArray())
            {
                var partId = Text(member, "id");
                if (!partIds.Add(partId)) throw new WorldDataException(file, partId, "Duplicate part ID.");
                Polyline? line = member.TryGetProperty("line", out var nodes) ? new(Value<Vector2[]>(nodes)) : null;
                var width = OptionalFloat(member, "width") ?? 0;
                var geometry = member.TryGetProperty("geometry", out var shape) ? Value<WorldGeometry>(shape)
                    : Buffer(line ?? throw new ArgumentException("Missing part geometry: " + partId), width, boundary)
                        ?? throw new ArgumentException("Empty buffered part: " + partId);
                var part = new WorldFeaturePart(partId, geometry, OptionalFloat(member, "height"), OptionalInteger(member, "storeys"))
                {
                    Props = Props(member),
                    FacadeGeometry = ReadFacadeGeometry(member, WorldFeatureGeometry.PolygonsOf(geometry))
                };
                parts.Add(part);
                if (line is not null)
                    AddLine(new(new(WorldFeatureIds.FromPart(prefix, partId).Source), line, width)
                        { Props = NetworkPrecisionProps(part.Props, Props(raw)) }, kind, file);
            }
        return parts;
    }

    private static WorldFeature ReadFeature(JsonElement raw, string id, ZoneId zone, string file, string prefix,
        PolygonRings boundary, HashSet<string> partIds, Action<NetworkLine, string, string> AddLine)
    {
        var kind = new DesignKind(Text(raw, "kind"));
        var parts = ReadParts(raw, kind.Value, file, prefix, boundary, partIds, AddLine);
        var combined = parts.Count == 0 ? Value<WorldGeometry>(raw.GetProperty("geometry"))
            : Geometry(parts.SelectMany(part => WorldFeatureGeometry.PolygonsOf(part.Geometry)).ToArray());
        var height = OptionalFloat(raw, "height");
        var feature = new WorldFeature(new(id), kind, combined)
        {
            Parts = parts.ToArray(), Zone = zone, Name = OptionalText(raw, "name"), Height = height,
            Storeys = OptionalInteger(raw, "storeys"), Props = Props(raw), Bounds = BoundsOf(combined, height ?? 0),
            Family = OptionalText(raw, "family") is { } family ? new(family) : null,
            Facade = OptionalText(raw, "facade") is { } facade ? new(facade) : null
        };
        return feature;
    }

    private static void ReadZones(SortedDictionary<string, string> input, string geoFile, string prefix,
        PolygonRings boundary, SortedDictionary<string, WorldFeature> features, HashSet<string> partIds,
        HashSet<FeatureId> treeIds, List<NetworkTree> trees, Action<NetworkLine, string, string> AddLine)
    {
        foreach (var (file, text) in input.Where(pair => pair.Key.StartsWith("world/", StringComparison.Ordinal)
            && pair.Key.EndsWith(".json", StringComparison.Ordinal) && !pair.Key.EndsWith(".terrain.json", StringComparison.Ordinal) && pair.Key.Split('/').Length == 2 && pair.Key != geoFile))
        {
            using var zoneDocument = Parse(file, text);
            var zone = new ZoneId(file["world/".Length..^".json".Length]);
            foreach (var raw in zoneDocument.RootElement.GetProperty("features").EnumerateArray())
            {
                var id = Text(raw, "id");
                try
                {
                    var feature = ReadFeature(raw, id, zone, file, prefix, boundary, partIds, AddLine);
                    if (!features.TryAdd(id, feature)) throw new WorldDataException(file, id, "Duplicate world feature ID.");
                    var sourceLine = WorldFeatureIds.TryFromSource(prefix, id, out _);
                    var designRoad = feature.Kind.Value == "road" && feature.TryText("highway") is "footway" or "path" or "pedestrian";
                    if (feature.Parts.Count == 0 && feature.Geometry is PolylineGeometry polyline && (sourceLine || designRoad && feature.TryText("area") != "yes"))
                        AddLine(new(feature.Id, polyline.Line, feature.TryNumber("width") ?? 0) { Props = feature.Props }, feature.Kind.Value, file);
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
    }

    private static IEnumerable<T> OrderNetwork<T>(IEnumerable<T> items, Func<T, FeatureId> id, string prefix) => items
        .OrderBy(item => WorldFeatureIds.TryFromSource(prefix, id(item).Value, out _) ? 0 : 1)
        .ThenBy(item => WorldFeatureIds.TryFromSource(prefix, id(item).Value, out _) ? long.Parse(id(item).Value.Split(':')[^1], CultureInfo.InvariantCulture) : 0)
        .ThenBy(item => id(item).Value, StringComparer.Ordinal);

    /// <summary>Validate declared world links after every zone and derived network source has been assembled.</summary>
    private static void ValidateFeatureReferences(IReadOnlyDictionary<FeatureId, WorldFeature> features,
        IReadOnlySet<FeatureId> featureIds, IReadOnlySet<FeatureId> roadIds, IReadOnlySet<FeatureId> roadReferences)
    {
        foreach (var feature in features.Values.OrderBy(feature => feature.Id.Value, StringComparer.Ordinal))
        {
            Read(feature, feature.Props, feature.Kind.Value, "props");
            foreach (var part in feature.Parts)
                Read(feature, part.Props, part.Props.TryText("kind") ?? feature.Kind.Value, $"parts[{part.SourceId}].props");
        }

        void Read(WorldFeature feature, JsonElement? source, string kind, string field)
        {
            if (source is not { ValueKind: JsonValueKind.Object } props) return;
            var file = $"world/{feature.Zone!.Value.Value}.json";
            try
            {
                Text("cluster");
                switch (kind)
                {
                    case "contourSegment" or "layout": Text("parent"); break;
                    case "bridge-deck":
                        Reference("axis", props.OptionalText("axis"), roadIds);
                        Text("banks", "start"); Text("banks", "end"); Many("walks", roadIds);
                        break;
                    case "bridge-stone": Text("bridge"); break;
                    case "escalator": Text("building"); break;
                    case "buildingDetail": Text("backstage"); break;
                    case "steps": Reference("road", props.OptionalText("road"), roadReferences); break;
                    case "sport": Text("sportGroup"); break;
                    case "sportGroup": Many("members", featureIds); break;
                    case "bank":
                        Text("water");
                        if (props.TryGetProperty("openings", out var openings) && openings.ValueKind != JsonValueKind.Null)
                        {
                            var index = 0;
                            foreach (var opening in openings.EnumerateArray())
                            {
                                Reference($"openings[{index}].edge", opening.OptionalText("edge"));
                                Reference($"openings[{index++}].widthFrom", opening.OptionalText("widthFrom"));
                            }
                        }
                        break;
                }
            }
            catch (Exception error) when (error is JsonException or InvalidOperationException)
            { throw new WorldDataException(file, feature.Id.Value, $"Invalid reference in {field}: {error.Message}", error); }

            void Text(params string[] path) => Reference(string.Join('.', path), props.OptionalText(path));
            void Many(string key, IReadOnlySet<FeatureId> targets)
            {
                if (!props.TryGetProperty(key, out var values) || values.ValueKind == JsonValueKind.Null) return;
                var index = 0;
                foreach (var value in values.EnumerateArray()) Reference($"{key}[{index++}]", value.GetString(), targets);
            }
            void Reference(string path, string? target, IReadOnlySet<FeatureId>? targets = null)
            {
                if (target is not null) RequireReference(file, feature.Id.Value, field + '.' + path, new(target), targets ?? featureIds);
            }
        }
    }

    internal static void RequireReference(string file, string source, string field, FeatureId target,
        IReadOnlySet<FeatureId> targets)
    {
        if (!targets.Contains(target))
            throw new WorldDataException(file, source, $"{field} references nonexistent world ID '{target.Value}'.");
    }

    /// <summary>Entity precision metadata applies to its source lines; a member may explicitly refine it.</summary>
    private static JsonElement? NetworkPrecisionProps(JsonElement? member, JsonElement? entity)
    {
        if (entity is not { ValueKind: JsonValueKind.Object } parent) return member;
        var keys = new[] { "fidelity", "walkNetwork" };
        if (!keys.Any(key => parent.TryGetProperty(key, out _))) return member;
        var values = member is { ValueKind: JsonValueKind.Object } props
            ? props.EnumerateObject().ToDictionary(property => property.Name, property => property.Value, StringComparer.Ordinal)
            : new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (var key in keys)
            if (!values.ContainsKey(key) && parent.TryGetProperty(key, out var value)) values.Add(key, value);
        return JsonSerializer.SerializeToElement(values, ContractJson.Options);
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

    private static WorldGeometry? ReadFacadeGeometry(JsonElement raw, IReadOnlyList<PolygonRings> physical)
    {
        if (!raw.TryGetProperty("facadePolygons", out var value)) return null;
        var polygons = Value<PolygonRings[]>(value);
        if (polygons.Length == 0 || polygons.Length != physical.Count
            || polygons.Where((polygon, i) => polygon.Holes.Count != physical[i].Holes.Count).Any())
            throw new ArgumentException("Facade polygons and holes must correspond to the physical footprint.");
        return Geometry(polygons);
    }

    private static WorldGeometry? Buffer(Polyline line, float width, PolygonRings boundary)
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

    internal static void CheckMap(MapId map, MapId? declared, string file)
    { if (declared is { } id && id != map) throw new WorldDataException(file, "map", "Overlay belongs to a different map."); }
    internal static WorldGeometry Geometry(IReadOnlyList<PolygonRings> polygons) => polygons.Count == 1 ? new PolygonGeometry(polygons[0]) : new MultiPolygonGeometry(polygons);
    internal static Box3 BoundsOf(WorldGeometry geometry, float height)
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
