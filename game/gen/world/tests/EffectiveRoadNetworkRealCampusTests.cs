using System.Reflection;
using System.Text.Json;
using Campus.Contracts;
using GdUnit4;

namespace Campus.Gen.World.Tests;

[TestSuite]
public sealed class EffectiveRoadNetworkRealCampusTests
{
    [TestCase]
    public void EveryCurrentDesignCenterlineProjectsOnceAndApprovedBuildingSourcesRemainUnchanged()
    {
        var files = ReadFiles();
        var previous = JsonSerializer.Deserialize<WorldNetwork>(files["network.json"], ContractJson.Options)!;
        var loaded = LoadPrepared(files).World;
        var expected = loaded.Features.Values.Where(feature => feature.Id.Value.StartsWith("design:", StringComparison.Ordinal) &&
            feature.Kind.Value == "road" && feature.Geometry is PolylineGeometry && PedestrianType(feature.Props) && !Area(feature.Props))
            .OrderBy(feature => feature.Id.Value, StringComparer.Ordinal).ToArray();
        Require(expected.Length > 0, "The real current campus has no authored design centerlines.");
        // Only an authored hide of an exact original road ID removes it; every other original road keeps its order and bytes.
        var originalIds = previous.Roads.Select(road => road.Id.Value).ToHashSet(StringComparer.Ordinal);
        var hidden = files.Where(pair => pair.Key.StartsWith("overlay/", StringComparison.Ordinal) && pair.Key.EndsWith(".overlay.json", StringComparison.Ordinal) &&
                pair.Key is not "overlay/outskirts.overlay.json" and not "overlay/walkable.overlay.json" and not "overlay/fidelity.overlay.json")
            .SelectMany(pair => JsonSerializer.Deserialize<OverlayFile>(pair.Value, ContractJson.Options)!.Ops.OfType<HideOp>())
            .Select(op => op.Target.Value).Where(originalIds.Contains).ToHashSet(StringComparer.Ordinal);
        Require(loaded.Network.Roads.All(road => !hidden.Contains(road.Id.Value)), "An explicitly hidden original road survived in the effective network.");
        var kept = previous.Roads.Where(road => !hidden.Contains(road.Id.Value)).ToArray();
        Require(previous.Roads.Count == 826 && loaded.Network.Roads.Count == kept.Length + expected.Length,
            "Unexpected original-road removal or design projection count.");
        Require(loaded.Network.Roads.Select(road => road.Id).Distinct().Count() == loaded.Network.Roads.Count, "Final road IDs repeat.");
        Require(Canonical(loaded.Network.Roads.Take(kept.Length).ToArray()) == Canonical(kept), "Original road order or bytes changed without a hide.");
        Require(Canonical(loaded.Network.Waterways) == Canonical(previous.Waterways) && Canonical(loaded.Network.Bridges) == Canonical(previous.Bridges) &&
            Canonical(loaded.Network.Culverts) == Canonical(previous.Culverts) && Canonical(loaded.Network.Trees) == Canonical(previous.Trees), "Other network categories changed.");
        Require(loaded.Network.Roads.Skip(kept.Length).Select(road => road.Id).SequenceEqual(expected.Select(feature => feature.Id)), "Design road order changed.");
        foreach (var feature in expected)
        {
            var road = loaded.Network.Roads.Single(road => road.Id == feature.Id);
            Require(road.Line.Points.SequenceEqual(((PolylineGeometry)feature.Geometry).Line.Points) &&
                road.Width == feature.Props!.Value.GetProperty("width").GetSingle() && road.Props?.GetRawText() == feature.Props?.GetRawText(),
                "Real design road lost its authored point order, width or props: " + feature.Id.Value);
        }
        foreach (var area in loaded.Features.Values.Where(feature => feature.Kind.Value == "road" && feature.Id.Value.StartsWith("design:", StringComparison.Ordinal) &&
            feature.Geometry is PolygonGeometry or MultiPolygonGeometry))
            Require(loaded.Network.Roads.All(road => road.Id != area.Id), "A real road area became a centerline: " + area.Id.Value);

        var baselineFiles = files.Where(pair => !pair.Key.StartsWith("overlay/", StringComparison.Ordinal)).ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        var baseline = WorldModelLoader.Load(new("qingshuihe"), baselineFiles).World;
        var four = loaded.Features[new("qsh:relation:11665640")];
        Require(four.Parts.Select(part => part.SourceId).SequenceEqual(new[] { "osm:relation:11665640", "osm:way:828726368", "osm:way:828726369" }, StringComparer.Ordinal) &&
            four.Height == 7.8f && four.Storeys == 2, "Research Building 4 hidePart data regressed.");
        foreach (var part in four.Parts)
            Require(Canonical(part) == Canonical(baseline.Features[four.Id].Parts.Single(old => old.SourceId == part.SourceId)), "Research Building 4 surviving source changed.");
        foreach (var id in new[] { new FeatureId("qsh:relation:11334255"), new FeatureId("qsh:relation:11665641") })
            Require(Canonical(loaded.Features[id].Parts) == Canonical(baseline.Features[id].Parts), "Independent A/B source parts changed.");
        var one = loaded.Features[new("qsh:way:691679140")];
        Require(one.Parts.Single().SourceId == "osm:way:691679140" && one.Height == 3.9f && one.Storeys == 1, "Building 1 hidePart data regressed.");
        Require(loaded.Features.ContainsKey(new("qsh:relation:14153711")) && !loaded.Features.ContainsKey(new("qsh:way:779496166")), "Research Building 6 hide data regressed.");
        Console.WriteLine($"REAL_EFFECTIVE_ROADS original={previous.Roads.Count} hidden={hidden.Count} design={expected.Length} final={loaded.Network.Roads.Count}");
        foreach (var feature in expected)
            Console.WriteLine($"REAL_DESIGN_ROAD id={feature.Id.Value} points={((PolylineGeometry)feature.Geometry).Line.Points.Count} width={feature.Props!.Value.GetProperty("width").GetSingle()}");
    }

    [TestCase]
    public void SevenActualNamedSourceBindingsNeverBecomeNetworkOnlyAndRealAreaSourcesAreRejected()
    {
        var files = ReadFiles();
        using var campus = JsonDocument.Parse(files["campus.json"]);
        var prefix = campus.RootElement.GetProperty("meta").GetProperty("entityPrefix").GetString()!;
        foreach (var number in new[] { "217855174", "544277137", "544688571", "552968505", "604832423", "630914103", "683646721" })
        {
            var source = "osm:way:" + number;
            var raw = campus.RootElement.GetProperty("features").EnumerateArray().Single(feature => feature.GetProperty("id").GetString() == source);
            var entity = raw.GetProperty("entityId").GetString()!;
            var network = prefix + ":source:way:" + number;
            foreach (var target in new[] { network, entity, source })
            {
                var trial = new Dictionary<string, string>(files, StringComparer.Ordinal)
                {
                    ["overlay/zz-road-identity-fixture.overlay.json"] = JsonSerializer.Serialize(new OverlayFile(new("test"), new("WP-08"),
                        [new HideOp(new(target), "read-only real source identity regression")]), ContractJson.Options)
                };
                try { LoadPrepared(trial); throw new InvalidOperationException("Named source was treated as independent network-only road: " + target); }
                catch (WorldDataException error)
                {
                    Require(error.Message.Contains("Ambiguous", StringComparison.Ordinal) && error.Message.Contains(source, StringComparison.Ordinal) &&
                        error.Message.Contains(entity, StringComparison.Ordinal) && error.Message.Contains(network, StringComparison.Ordinal),
                        "Named road rejection lost its real source/entity/network identity: " + error.Message);
                }
            }
        }
    }

    internal static WorldModelLoadResult LoadPrepared(IReadOnlyDictionary<string, string> files)
    {
        // Test discovery follows the production ContentModule/MapScope metadata and real module Claims;
        // it supplies only the unique per-feature owners required by the pure loader's overlay boundary.
        var baselineFiles = files.Where(pair => !pair.Key.StartsWith("overlay/", StringComparison.Ordinal)).ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        var baseline = WorldModelLoader.Load(new("qingshuihe"), baselineFiles).World;
        var modules = Directory.EnumerateFiles(AppContext.BaseDirectory, "Campus.Gen.*.dll")
            .Where(path => Path.GetFileName(path) is not "Campus.Gen.Tests.dll" and not "Campus.Gen.World.dll")
            .Order(StringComparer.Ordinal).Select(path => Assembly.Load(new AssemblyName(Path.GetFileNameWithoutExtension(path))))
            .SelectMany(assembly => assembly.GetTypes().Where(type => type.IsClass && !type.IsAbstract && typeof(IContentModule).IsAssignableFrom(type))
                .Select(type => (Type: type, Attribute: type.GetCustomAttribute<ContentModuleAttribute>(), Scope: assembly.GetCustomAttribute<MapScopeAttribute>()?.Map,
                    IsStub: assembly.GetName().Name == "Campus.Gen.Parts.Stubs")))
            .Where(item => item.Attribute is not null)
            .Select(item => (item.Type, Attribute: item.Attribute!, Map: string.IsNullOrEmpty(item.Attribute!.Map) ? item.Scope : item.Attribute.Map, item.IsStub))
            .Where(item => item.Map is null or "qingshuihe")
            .GroupBy(item => (item.Attribute.Id, item.Map))
            .Select(group => group.Any(item => !item.IsStub) ? group.Single(item => !item.IsStub) : group.Single()).ToArray();
        var candidates = new Dictionary<FeatureId, HashSet<WpId>>();
        foreach (var item in modules)
        {
            var constructor = item.Type.GetConstructors().Where(constructor => constructor.GetParameters().All(parameter => parameter.HasDefaultValue))
                .OrderBy(constructor => constructor.GetParameters().Length).First();
            var module = (IContentModule)constructor.Invoke(constructor.GetParameters().Select(parameter => parameter.DefaultValue).ToArray());
            foreach (var claim in module.Claims)
            {
                IEnumerable<FeatureId> features = claim switch
                {
                    FeatureClaim feature when item.Map is not null => [feature.Feature],
                    AreaClaim area => baseline.Features.Values.Where(feature => feature.Zone == area.Zone).Select(feature => feature.Id),
                    _ => throw new InvalidOperationException("Invalid global feature claim or unknown real claim kind.")
                };
                foreach (var feature in features)
                {
                    if (!candidates.TryGetValue(feature, out var owners)) candidates[feature] = owners = [];
                    owners.Add(new(item.Attribute!.Owner));
                }
            }
        }
        Require(modules.Length > 0 && candidates.Count > 0, "No real registered module claims were captured.");
        var ownersByFeature = candidates.Where(pair => pair.Value.Count == 1).ToDictionary(pair => pair.Key, pair => pair.Value.Single());
        return WorldModelLoader.Load(new("qingshuihe"), files, new() { ClaimedBy = ownersByFeature });
    }

    internal static Dictionary<string, string> ReadFiles()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var data = Path.Combine(directory.FullName, "game", "data", "maps", "qingshuihe");
            if (!File.Exists(Path.Combine(data, "campus.json"))) continue;
            return Directory.EnumerateFiles(data, "*.json", SearchOption.AllDirectories).Order(StringComparer.Ordinal)
                .ToDictionary(path => Path.GetRelativePath(data, path).Replace('\\', '/'), File.ReadAllText, StringComparer.Ordinal);
        }
        throw new DirectoryNotFoundException("Real campus data is missing.");
    }

    private static bool PedestrianType(JsonElement? props) => props is { ValueKind: JsonValueKind.Object } value &&
        (value.TryGetProperty("roadType", out var roadType) ? roadType.GetString() : value.TryGetProperty("highway", out var highway) ? highway.GetString() : null) is "footway" or "path" or "pedestrian";
    private static bool Area(JsonElement? props) => props is { ValueKind: JsonValueKind.Object } value && value.TryGetProperty("area", out var area) && area.GetString() == "yes";
    private static string Canonical<T>(T value) => JsonSerializer.Serialize(value, ContractJson.Options);
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
