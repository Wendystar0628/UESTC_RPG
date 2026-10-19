using System.Text.Json;
using System.Text;
using Campus.Contracts;
using Campus.Tests;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Campus.Gen.World.Tests;

[TestSuite]
public sealed class EffectiveRoadNetworkRealCampusTests
{
    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void EveryCurrentDesignCenterlineProjectsOnceAndKeepsOtherNetworkCategories()
    {
        var files = RealMapFixture.ReadTextFiles();
        var previous = JsonSerializer.Deserialize<WorldNetwork>(files["network.json"], ContractJson.Options)!;
        var loaded = RealMapFixture.LoadPrepared(files).World;
        var terrain = files.Where(pair => pair.Key.StartsWith("overlay/terrain/", StringComparison.Ordinal)
                && pair.Key.EndsWith(".terrain.json", StringComparison.Ordinal) && pair.Key.Split('/').Length == 3)
            .SelectMany(pair => TerrainOverlayJson.Parse(Encoding.UTF8.GetBytes(pair.Value)).Ops).ToArray();
        AssertThat(loaded.Terrain is ITerrainWaterSource && loaded.Terrain is ITerrainBankSource &&
            loaded.TerrainOperations.Keys.Order(StringComparer.Ordinal).SequenceEqual(terrain.Select(op => op.Id).Order(StringComparer.Ordinal)))
            .OverrideFailureMessage("The real map did not publish the complete authored terrain evaluator.").IsTrue();
        foreach (var op in terrain)
        {
            if (op is TerrainPadOp { PadY: { } pad } authoredPad)
                AssertThat(Math.Abs(loaded.Terrain.PadY(authoredPad.Feature) - pad) <= TestTolerance.Position).IsTrue();
            if (op is TerrainLakeOp lake)
                AssertThat(loaded.Terrain.WaterY(lake.Water) is { } lakeY && Math.Abs(lakeY - lake.WaterY) <= TestTolerance.Position).IsTrue();
            if (op is TerrainChannelOp { Dry: false, Water: { } water, WaterY: { } waterY })
                AssertThat(loaded.Terrain.WaterY(water) is { } channelY && Math.Abs(channelY - waterY) <= TestTolerance.Position).IsTrue();
        }
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

    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void ActualNamedSourceBindingsNeverBecomeNetworkOnly()
    {
        var files = RealMapFixture.ReadTextFiles();
        using var campus = JsonDocument.Parse(files["campus.json"]);
        var prefix = campus.RootElement.GetProperty("meta").GetProperty("entityPrefix").GetString()!;
        var networkRoads = JsonSerializer.Deserialize<WorldNetwork>(files["network.json"], ContractJson.Options)!
            .Roads.Select(road => road.Id.Value).ToHashSet(StringComparer.Ordinal);
        var named = campus.RootElement.GetProperty("features").EnumerateArray()
            .Where(feature => feature.GetProperty("kind").GetString() == "road" &&
                feature.GetProperty("entityId").ValueKind == JsonValueKind.String)
            .Where(feature => networkRoads.Contains(OsmFeatureIds.FromPart(prefix, feature.GetProperty("id").GetString()!).Source)).ToArray();
        AssertThat(named.Length > 0).OverrideFailureMessage("The real campus has no named network road source scenario.").IsTrue();
        foreach (var raw in named)
        {
            var source = raw.GetProperty("id").GetString()!;
            var entity = raw.GetProperty("entityId").GetString()!;
            var network = OsmFeatureIds.FromPart(prefix, source).Source;
            foreach (var target in new[] { network, entity, source })
            {
                var trial = new Dictionary<string, string>(files, StringComparer.Ordinal)
                {
                    ["overlay/zz-road-identity-fixture.d/0000-fixture.json"] = JsonSerializer.Serialize(new OverlayFile(new("test"), new("WP-08"),
                        [new HideOp(new(target), "read-only real source identity regression")]), ContractJson.Options)
                };
                try { RealMapFixture.LoadPrepared(trial); throw new InvalidOperationException("Named source was treated as independent network-only road: " + target); }
                catch (WorldDataException) { }
            }
        }
    }

    private static bool PedestrianType(JsonElement? props) => props is { ValueKind: JsonValueKind.Object } value &&
        (value.TryGetProperty("roadType", out var roadType) ? roadType.GetString() : value.TryGetProperty("highway", out var highway) ? highway.GetString() : null) is "footway" or "path" or "pedestrian";
    private static bool Area(JsonElement? props) => props is { ValueKind: JsonValueKind.Object } value && value.TryGetProperty("area", out var area) && area.GetString() == "yes";
    private static string Canonical<T>(T value) => JsonSerializer.Serialize(value, ContractJson.Options);
}
