using System.Numerics;
using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Campus.Contracts;
using Campus.Pipeline.Map;
using Campus.Tests;
using GdUnit4;

namespace Campus.Gen.World.Tests;

/// <summary>One-shot Q-045 export. Delete this and Q045WorldFormatReadback after Q-046 switches production.</summary>
[TestSuite]
public sealed class Q045WorldFormatConversion
{
    internal static readonly string[] Zones = ["core", "south", "west", "north", "center-east", "waterfront", "public", "boundary", "atlas", "offcampus"];
    // Literal property reads in the existing consumers, including classification needed to derive the network.
    private static readonly HashSet<string> UsedProps = new(("anchor area axis banks bearings bridge carriage category centre cluster contour covered " +
        "diameter direction dishes door doorAt doorFacing faceDeg facing footRadius form from gnomonElevationDeg groundY half " +
        "highway keep kerb kind layer leisure length name name:zh northFacet open panelStrip parent part perPit plant planter " +
        "props radius road rows sagitta seatWall setbacks sidewalk size soilAbove sourceId species sport stone subcategory tank thickness " +
        "to toward treads tunnel walks water waterway width yawDeg").Split(' '), StringComparer.Ordinal);

    /// <summary>Run only this filtered test during the Q-046 freeze; leaves the eleven candidate JSON files in output/q045/world.</summary>
    [TestCase, TestCategory(TestReason.Contract), TestCategory("Slow")]
    public void ExportEffectiveWorldAndCompareTargetReadback()
    {
        var files = RealMapFixture.ReadTextFiles();
        var loaded = RealMapFixture.LoadPrepared(files);
        var directory = RepositoryFixture.FilePath("output", "q045", "world");
        Export(loaded, files, directory);
        var target = Q045WorldFormatReadback.Read(directory);
        Q045WorldFormatReadback.Compare(loaded, target);
    }

    internal static void Export(WorldModelLoadResult loaded, IReadOnlyDictionary<string, string> files, string directory)
    {
        var world = loaded.World;
        using var campus = JsonDocument.Parse(files["campus.json"]);
        var meta = campus.RootElement.GetProperty("meta");
        var prefix = meta.GetProperty("entityPrefix").GetString()!;
        var geo = new JsonObject();
        foreach (var key in new[] { "origin", "crs", "bounds", "entityPrefix", "license", "source", "date" })
            geo[key] = JsonNode.Parse(meta.GetProperty(key).GetRawText());
        geo["boundary"] = Node(world.Boundary);
        var groups = Zones.ToDictionary(zone => zone, _ => new JsonObject { ["features"] = new JsonArray(), ["trees"] = new JsonArray() }, StringComparer.Ordinal);
        var notes = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        var zones = ResolveZones(loaded, files, notes, prefix);
        var network = world.Network.Roads.Concat(world.Network.Waterways).ToDictionary(line => line.Id.Value, StringComparer.Ordinal);
        var consumedLines = new HashSet<string>(StringComparer.Ordinal);
        foreach (var feature in world.Features.Values.OrderBy(feature => feature.Id.Value, StringComparer.Ordinal))
        {
            var record = new JsonObject { ["id"] = feature.Id.Value, ["kind"] = feature.Kind.Value };
            Put(record, "name", feature.Name); Put(record, "height", feature.Height); Put(record, "storeys", feature.Storeys);
            Put(record, "family", feature.Family?.Value); Put(record, "facade", feature.Facade?.Value);
            if (Slim(feature.Props) is { } props) record["props"] = props;
            if (notes.TryGetValue(feature.Id.Value, out var reasons)) record["note"] = string.Join("\n", reasons.Distinct(StringComparer.Ordinal));
            if (feature.Id.Value.StartsWith("design:", StringComparison.Ordinal) && record["note"] is null)
                throw new InvalidOperationException($"Missing authored design note: {feature.Id.Value}");
            if (feature.Parts.Count == 0)
            {
                record["geometry"] = Node<OverlayGeometry>(feature.Geometry);
                if (network.ContainsKey(feature.Id.Value)) consumedLines.Add(feature.Id.Value);
            }
            else
            {
                var parts = new JsonArray();
                foreach (var part in feature.Parts)
                {
                    var item = new JsonObject { ["id"] = part.SourceId };
                    Put(item, "height", part.Height); Put(item, "storeys", part.Storeys);
                    if (Slim(part.Props) is { } partProps) item["props"] = partProps;
                    var lineId = OsmFeatureIds.FromPart(prefix, part.SourceId).Source;
                    if (network.TryGetValue(lineId, out var line))
                    {
                        consumedLines.Add(lineId);
                        item["line"] = Node(line.Line.Points); item["width"] = line.Width;
                        var merged = (JsonObject?)item["props"] ?? new JsonObject();
                        if (Slim(line.Props) is { } lineProps)
                            foreach (var property in lineProps) merged[property.Key] = property.Value?.DeepClone();
                        item["props"] = merged;
                        var derived = Q045WorldFormatReadback.Buffer(line.Line, line.Width, world.Boundary);
                        if (line.Props.TryText("area") == "yes" || !Q045WorldFormatReadback.SameGeometry(part.Geometry, derived))
                            item["geometry"] = Node<OverlayGeometry>(part.Geometry);
                    }
                    else item["geometry"] = Node<OverlayGeometry>(part.Geometry);
                    if (part.FacadeGeometry is { } facade) item["facadePolygons"] = Node(WorldFeatureGeometry.PolygonsOf(facade));
                    parts.Add(item);
                }
                record["parts"] = parts;
            }
            ((JsonArray)groups[zones[feature.Id.Value]]["features"]!).Add(record);
        }
        // These source lines never had an in-campus footprint, but are still part of the effective world network.
        foreach (var line in network.Values.Where(line => !consumedLines.Contains(line.Id.Value)).OrderBy(line => line.Id.Value, StringComparer.Ordinal))
        {
            var props = Slim(line.Props) ?? new JsonObject(); props["width"] = line.Width;
            var kind = world.Network.Roads.Any(road => road.Id == line.Id) ? "road" : "water";
            ((JsonArray)groups["offcampus"]["features"]!).Add(new JsonObject
            {
                ["id"] = line.Id.Value, ["kind"] = kind,
                ["geometry"] = Node<OverlayGeometry>(new PolylineGeometry(line.Line)), ["props"] = props
            });
        }
        var seeds = world.Features.Values.Where(feature => zones[feature.Id.Value] != "offcampus").ToArray();
        foreach (var tree in world.Network.Trees)
        {
            var point = new Vector2(tree.Position.X, tree.Position.Z);
            var zone = Q045WorldFormatReadback.Inside(world.Boundary, point) ? Nearest(point, seeds, zones) : "offcampus";
            ((JsonArray)groups[zone]["trees"]!).Add(Node(tree));
        }
        Directory.CreateDirectory(directory);
        Write(Path.Combine(directory, "geo.json"), geo);
        foreach (var zone in Zones) Write(Path.Combine(directory, zone + ".json"), groups[zone]);
    }

    private static Dictionary<string, string> ResolveZones(WorldModelLoadResult loaded, IReadOnlyDictionary<string, string> files,
        Dictionary<string, List<string>> notes, string prefix)
    {
        var features = loaded.World.Features;
        var aliases = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var feature in features.Values)
        {
            aliases[feature.Id.Value] = feature.Id.Value;
            foreach (var part in feature.Parts)
            {
                aliases[part.SourceId] = feature.Id.Value;
                aliases[OsmFeatureIds.FromPart(prefix, part.SourceId).Source] = feature.Id.Value;
            }
        }
        using (var campus = JsonDocument.Parse(files["campus.json"]))
            foreach (var raw in campus.RootElement.GetProperty("features").EnumerateArray())
            {
                var source = raw.GetProperty("id").GetString()!;
                var entity = raw.TryGetProperty("entityId", out var id) && id.ValueKind == JsonValueKind.String
                    ? id.GetString()! : OsmFeatureIds.FromPart(prefix, source).Source;
                if (features.ContainsKey(new(entity))) aliases[source] = entity;
            }
        var zones = new Dictionary<string, string>(StringComparer.Ordinal);
        var hidden = new List<string>();
        foreach (var pair in files.Where(pair => pair.Key.StartsWith("overlay/", StringComparison.Ordinal) && pair.Key.Contains(".d/", StringComparison.Ordinal))
            .OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            using var doc = JsonDocument.Parse(pair.Value);
            var root = doc.RootElement;
            if (!root.TryGetProperty("zone", out var rawZone) || !root.TryGetProperty("ops", out var ops)) continue;
            var zone = rawZone.GetString()!;
            if (!Zones.Contains(zone, StringComparer.Ordinal)) throw new InvalidOperationException("Unknown export zone: " + zone);
            foreach (var op in ops.EnumerateArray())
            {
                var kind = op.GetProperty("op").GetString();
                var target = op.TryGetProperty("target", out var t) ? t.GetString() : op.TryGetProperty("id", out var id) ? id.GetString() : null;
                var reason = op.TryGetProperty("reason", out var r) ? r.GetString() : null;
                if (kind is "hide" or "hidePart") { hidden.Add($"{kind} {target}: {reason}"); continue; }
                if (target is null || !aliases.TryGetValue(target, out var actual)) continue;
                zones.TryAdd(actual, zone);
                if (!string.IsNullOrWhiteSpace(reason))
                {
                    if (!notes.TryGetValue(actual, out var reasons)) notes[actual] = reasons = [];
                    reasons.Add(reason);
                }
            }
        }
        // Explicit per-feature code claims precede terrain references; AreaClaim is the old broad zones.json assignment.
        var ownerZones = new Dictionary<string, string>(StringComparer.Ordinal)
        { ["WP-16"] = "core", ["WP-18"] = "south", ["WP-19"] = "west", ["WP-20"] = "north", ["WP-21"] = "center-east",
          ["WP-28"] = "waterfront", ["WP-29"] = "public", ["WP-30"] = "boundary", ["WP-48c"] = "atlas" };
        var registrations = RegistrationDiscovery.Discover(new[] { "Campus.Gen", "Campus.Gen.Maps.Qingshuihe" }.Select(Assembly.Load));
        foreach (var entry in RegistrationDiscovery.ForMap(registrations, entry => entry.Map, RealMapFixture.Entry().Descriptor.Id.Value)
            .Where(entry => entry.Kind == "content" && ownerZones.ContainsKey(entry.Owner)))
            foreach (var claim in ((IContentModule)Activator.CreateInstance(entry.Type)!).Claims.OfType<FeatureClaim>())
                if (features.ContainsKey(claim.Feature)) zones.TryAdd(claim.Feature.Value, ownerZones[entry.Owner]);
        foreach (var pair in files.Where(pair => pair.Key.EndsWith(".terrain.json", StringComparison.Ordinal)).OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            using var doc = JsonDocument.Parse(pair.Value);
            var zone = doc.RootElement.GetProperty("zone").GetString()!;
            foreach (var value in Strings(doc.RootElement))
                if (aliases.TryGetValue(value, out var actual)) zones.TryAdd(actual, zone);
        }
        foreach (var feature in features.Values)
            if (!Q045WorldFormatReadback.Intersects(feature.Geometry, loaded.World.Boundary)) zones[feature.Id.Value] = "offcampus";
        var seeds = features.Values.Where(feature => zones.TryGetValue(feature.Id.Value, out var zone) && zone != "offcampus")
            .OrderBy(feature => feature.Id.Value, StringComparer.Ordinal).ToArray();
        foreach (var feature in features.Values)
            if (!zones.ContainsKey(feature.Id.Value)) zones[feature.Id.Value] = Nearest(Center(feature), seeds, zones);
        // Hide reasons belong in the migration commit message, never in a second authoritative data document.
        foreach (var reason in hidden) Console.WriteLine(reason);
        return zones;
    }

    private static IEnumerable<string> Strings(JsonElement node)
    {
        if (node.ValueKind == JsonValueKind.String) yield return node.GetString()!;
        else if (node.ValueKind == JsonValueKind.Array)
            foreach (var child in node.EnumerateArray()) foreach (var value in Strings(child)) yield return value;
        else if (node.ValueKind == JsonValueKind.Object)
            foreach (var child in node.EnumerateObject()) foreach (var value in Strings(child.Value)) yield return value;
    }

    private static Vector2 Center(WorldFeature feature) => new((feature.Bounds.Min.X + feature.Bounds.Max.X) / 2,
        (feature.Bounds.Min.Z + feature.Bounds.Max.Z) / 2);

    private static string Nearest(Vector2 point, IReadOnlyList<WorldFeature> seeds, IReadOnlyDictionary<string, string> zones) =>
        zones[seeds.OrderBy(feature => Vector2.DistanceSquared(point, Center(feature))).ThenBy(feature => feature.Id.Value, StringComparer.Ordinal).First().Id.Value];

    internal static JsonObject? Slim(JsonElement? raw)
    {
        if (raw is not { ValueKind: JsonValueKind.Object } source) return null;
        var props = new JsonObject();
        if (source.TryGetProperty("sourceTags", out var tags) && tags.ValueKind == JsonValueKind.Object)
            foreach (var pair in tags.EnumerateObject()) Keep(pair);
        foreach (var pair in source.EnumerateObject()) Keep(pair);
        if (!props.ContainsKey("highway") && source.TryGetProperty("roadType", out var roadType)) props["highway"] = JsonNode.Parse(roadType.GetRawText());
        return props;
        void Keep(JsonProperty property)
        {
            if (UsedProps.Contains(property.Name) && property.Value.ValueKind != JsonValueKind.Null)
                props[property.Name] = JsonNode.Parse(property.Value.GetRawText());
        }
    }

    private static JsonNode? Node<T>(T value) => JsonSerializer.SerializeToNode(value, ContractJson.Options);
    private static void Put<T>(JsonObject target, string key, T? value) { if (value is not null) target[key] = Node(value); }

    // Ordinary two-space JSON, keeping numerical coordinate arrays on one line like the authored shards.
    private static void Write(string path, JsonNode node)
    {
        var options = new JsonSerializerOptions(ContractJson.Options) { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
        var text = new StringBuilder();
        Format(node, 0);
        File.WriteAllText(path, text.Append('\n').ToString(), new UTF8Encoding(false));
        void Format(JsonNode? value, int depth)
        {
            if (value is JsonObject obj)
            {
                text.Append("{\n"); var pairs = obj.ToArray();
                for (var i = 0; i < pairs.Length; i++)
                { text.Append(' ', (depth + 1) * 2).Append(JsonSerializer.Serialize(pairs[i].Key)).Append(": "); Format(pairs[i].Value, depth + 1); text.Append(i + 1 < pairs.Length ? ",\n" : "\n"); }
                text.Append(' ', depth * 2).Append('}');
            }
            else if (value is JsonArray array && array.Any(item => item is JsonObject))
            {
                text.Append("[\n");
                for (var i = 0; i < array.Count; i++)
                { text.Append(' ', (depth + 1) * 2); Format(array[i], depth + 1); text.Append(i + 1 < array.Count ? ",\n" : "\n"); }
                text.Append(' ', depth * 2).Append(']');
            }
            else text.Append(value?.ToJsonString(options) ?? "null");
        }
    }
}
