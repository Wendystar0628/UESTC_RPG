using System.Reflection;
using System.Text.Json;
using Campus.Contracts;
using Campus.Pipeline.Map;
using GdUnit4;
using static GdUnit4.Assertions;

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
        AssertThat(expected.Length > 0).OverrideFailureMessage("The real current campus has no authored design centerlines.").IsTrue();
        // Only an authored hide of an exact original road ID removes it; every other original road keeps its order and bytes.
        var originalIds = previous.Roads.Select(road => road.Id.Value).ToHashSet(StringComparer.Ordinal);
        var hidden = files.Where(pair => pair.Key.StartsWith("overlay/", StringComparison.Ordinal) &&
                (pair.Key.Split('/') is ["overlay", var group, var child] && child.EndsWith(".json", StringComparison.Ordinal) &&
                    group.EndsWith(".d", StringComparison.Ordinal) && group is not "interior.d" and not "terrain.d" and not "outskirts.d" and not "walkable.d" and not "fidelity.d"))
            .SelectMany(pair => JsonSerializer.Deserialize<OverlayFile>(pair.Value, ContractJson.Options)!.Ops.OfType<HideOp>())
            .Select(op => op.Target.Value).Where(originalIds.Contains).ToHashSet(StringComparer.Ordinal);
        AssertThat(loaded.Network.Roads.All(road => !hidden.Contains(road.Id.Value))).OverrideFailureMessage("An explicitly hidden original road survived in the effective network.").IsTrue();
        var kept = previous.Roads.Where(road => !hidden.Contains(road.Id.Value)).ToArray();
        AssertThat(loaded.Network.Roads.Count == kept.Length + expected.Length).OverrideFailureMessage("Unexpected original-road removal or design projection count.").IsTrue();
        AssertThat(loaded.Network.Roads.Select(road => road.Id).Distinct().Count() == loaded.Network.Roads.Count).OverrideFailureMessage("Final road IDs repeat.").IsTrue();
        AssertThat(Canonical(loaded.Network.Roads.Take(kept.Length).ToArray()) == Canonical(kept)).OverrideFailureMessage("Original road order or bytes changed without a hide.").IsTrue();
        AssertThat(Canonical(loaded.Network.Waterways) == Canonical(previous.Waterways) && Canonical(loaded.Network.Bridges) == Canonical(previous.Bridges) &&
            Canonical(loaded.Network.Culverts) == Canonical(previous.Culverts) && Canonical(loaded.Network.Trees) == Canonical(previous.Trees)).OverrideFailureMessage("Other network categories changed.").IsTrue();
        AssertThat(loaded.Network.Roads.Skip(kept.Length).Select(road => road.Id).SequenceEqual(expected.Select(feature => feature.Id))).OverrideFailureMessage("Design road order changed.").IsTrue();
        foreach (var feature in expected)
        {
            var road = loaded.Network.Roads.Single(road => road.Id == feature.Id);
            AssertThat(road.Line.Points.SequenceEqual(((PolylineGeometry)feature.Geometry).Line.Points) &&
                road.Width == feature.Props!.Value.GetProperty("width").GetSingle() && road.Props?.GetRawText() == feature.Props?.GetRawText()).OverrideFailureMessage("Real design road lost its authored point order, width or props: " + feature.Id.Value).IsTrue();
        }
        foreach (var area in loaded.Features.Values.Where(feature => feature.Kind.Value == "road" && feature.Id.Value.StartsWith("design:", StringComparison.Ordinal) &&
            feature.Geometry is PolygonGeometry or MultiPolygonGeometry))
            AssertThat(loaded.Network.Roads.All(road => road.Id != area.Id)).OverrideFailureMessage("A real road area became a centerline: " + area.Id.Value).IsTrue();

        var baselineFiles = files.Where(pair => !pair.Key.StartsWith("overlay/", StringComparison.Ordinal)).ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        var baseline = WorldModelLoader.Load(new("qingshuihe"), baselineFiles).World;
        var four = loaded.Features[new("qsh:relation:11665640")];
        AssertThat(four.Parts.Select(part => part.SourceId).SequenceEqual(new[] { "osm:relation:11665640", "osm:way:828726368", "osm:way:828726369" }, StringComparer.Ordinal) &&
            four.Height == 7.8f && four.Storeys == 2).OverrideFailureMessage("Research Building 4 hidePart data regressed.").IsTrue();
        foreach (var part in four.Parts)
            AssertThat(Canonical(part) == Canonical(baseline.Features[four.Id].Parts.Single(old => old.SourceId == part.SourceId))).OverrideFailureMessage("Research Building 4 surviving source changed.").IsTrue();
        foreach (var id in new[] { new FeatureId("qsh:relation:11334255"), new FeatureId("qsh:relation:11665641") })
            AssertThat(Canonical(loaded.Features[id].Parts) == Canonical(baseline.Features[id].Parts)).OverrideFailureMessage("Independent A/B source parts changed.").IsTrue();
        var one = loaded.Features[new("qsh:way:691679140")];
        AssertThat(one.Parts.Single().SourceId == "osm:way:691679140" && one.Height == 3.9f && one.Storeys == 1).OverrideFailureMessage("Building 1 hidePart data regressed.").IsTrue();
        AssertThat(loaded.Features.ContainsKey(new("qsh:relation:14153711")) && !loaded.Features.ContainsKey(new("qsh:way:779496166"))).OverrideFailureMessage("Research Building 6 hide data regressed.").IsTrue();
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
                    ["overlay/zz-road-identity-fixture.d/0000-fixture.json"] = JsonSerializer.Serialize(new OverlayFile(new("test"), new("WP-08"),
                        [new HideOp(new(target), "read-only real source identity regression")]), ContractJson.Options)
                };
                try { LoadPrepared(trial); throw new InvalidOperationException("Named source was treated as independent network-only road: " + target); }
                catch (WorldDataException error)
                {
                    AssertThat(error.Message.Contains("Ambiguous", StringComparison.Ordinal) && error.Message.Contains(source, StringComparison.Ordinal) &&
                        error.Message.Contains(entity, StringComparison.Ordinal) && error.Message.Contains(network, StringComparison.Ordinal)).OverrideFailureMessage("Named road rejection lost its real source/entity/network identity: " + error.Message).IsTrue();
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
        var registrations = RegistrationDiscovery.Discover(new[] { "Campus.Gen", "Campus.Gen.Maps.Qingshuihe" }.Select(Assembly.Load));
        var modules = RegistrationDiscovery.ForMap(registrations, entry => entry.Map, "qingshuihe")
            .Where(entry => entry.Kind == "content").ToArray();
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
                    owners.Add(new(item.Owner));
                }
            }
        }
        AssertThat(modules.Length > 0 && candidates.Count > 0).OverrideFailureMessage("No real registered module claims were captured.").IsTrue();
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
}
