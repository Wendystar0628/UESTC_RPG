using System.Numerics;
using System.Text;
using Campus.Contracts;
using GdUnit4;

namespace Campus.Gen.Walkable.Tests;

[TestSuite]
public sealed class WalkBakeFreshnessTests
{
    [TestCase]
    public void MapRuleRevisionRejectsPreviousBakeAndSelectionIgnoresOutputs()
    {
        var files = new Dictionary<string, ReadOnlyMemory<byte>>(StringComparer.Ordinal)
        {
            ["map.json"] = Bytes("{\"id\":\"example\"}"),
            ["campus.json"] = Bytes("{\"features\":[]}")
        };
        const string previousHash = "bae492c424b5369f01dac6d4c36b3301837097d3110285b88cef3a85eb78ccca";
        ExpectFailure(() => WalkBakeInputHash.RequireCurrent(Product(previousHash), files),
            WalkBakeFreshnessFailure.StaleInputs, "walk.stale:");
        var expected = WalkBakeInputHash.ComputeForMap(files);
        WalkBakeInputHash.RequireCurrent(Product(expected), files);
        files.Add("boot/walk.bake.json", Bytes("{\"oldProduct\":true}"));
        files.Add("fidelity.json", Bytes("{\"generated\":true}"));
        files.Add("atlas/markers.json", Bytes("[]"));
        Require(WalkBakeInputHash.ComputeForMap(files) == expected, "Downstream outputs must not invalidate their own upstream bake.");
        var reversed = files.Reverse().ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        Require(WalkBakeInputHash.ComputeForMap(reversed) == expected, "Dictionary insertion order must not affect map freshness.");
    }

    [TestCase]
    public void TerrainDefaultsAndTerrainOverlayEditsRejectThePreviouslyBakedProduct()
    {
        var files = Inputs();
        var original = Product(WalkBakeInputHash.ComputeForMap(files));
        WalkBakeInputHash.RequireCurrent(original, files);
        files["map.json"] = Bytes(Encoding.UTF8.GetString(files["map.json"].Span).Replace("\"channelDepth\":2.5", "\"channelDepth\":3.5", StringComparison.Ordinal));
        ExpectFailure(() => WalkBakeInputHash.RequireCurrent(original, files), WalkBakeFreshnessFailure.StaleInputs, "walk.stale:");

        files = Inputs();
        files["overlay/terrain/fixture.terrain.json"] = Bytes(Encoding.UTF8.GetString(files["overlay/terrain/fixture.terrain.json"].Span)
            .Replace("\"padY\":0.5", "\"padY\":1.0", StringComparison.Ordinal));
        ExpectFailure(() => WalkBakeInputHash.RequireCurrent(original, files), WalkBakeFreshnessFailure.StaleInputs, "walk.stale:");

        files = Inputs();
        files.Remove("overlay/terrain/fixture.terrain.json");
        ExpectFailure(() => WalkBakeInputHash.RequireCurrent(original, files), WalkBakeFreshnessFailure.StaleInputs, "walk.stale:");
        files = Inputs();
        files["zones.json"] = Bytes("{\"zones\":[{\"id\":\"core\",\"boundary\":[[0,0],[16,0],[16,8],[0,8]]}]}");
        ExpectFailure(() => WalkBakeInputHash.RequireCurrent(original, files), WalkBakeFreshnessFailure.StaleInputs, "walk.stale:");
    }

    [TestCase]
    public void UnknownDamagedAndStaleProductsHaveSeparateRejectionCodes()
    {
        var files = Inputs();
        var product = Product(WalkBakeInputHash.ComputeForMap(files));
        ExpectFailure(() => WalkBakeInputHash.RequireCurrent(product with { Version = WalkBakeData.CurrentVersion + 1 }, files),
            WalkBakeFreshnessFailure.UnknownVersion, "walk.version:");
        ExpectFailure(() => WalkBakeInputHash.RequireCurrent(product with { Walk = product.Walk with { Classes = [] } }, files),
            WalkBakeFreshnessFailure.InvalidData, "walk.invalid:");
        ExpectFailure(() => WalkBakeInputHash.RequireCurrent(product with { Walk = null! }, files),
            WalkBakeFreshnessFailure.InvalidData, "walk.invalid:");
        ExpectFailure(() => WalkBakeInputHash.RequireCurrent(null, files), WalkBakeFreshnessFailure.InvalidData, "walk.invalid:");
        ExpectFailure(() => WalkBakeInputHash.RequireCurrent(product with { InputHash = new string('0', 64) }, files),
            WalkBakeFreshnessFailure.StaleInputs, "walk.stale:");
    }

    [TestCase]
    public void IncompleteSourceSetsCannotMasqueradeAsAValidCurrentMap()
    {
        var files = Inputs();
        files.Remove("map.json");
        try { _ = WalkBakeInputHash.ComputeForMap(files); }
        catch (ArgumentException error) when (error.Message.Contains("map.json", StringComparison.Ordinal)) { return; }
        throw new InvalidOperationException("Missing map descriptor must not produce a freshness hash.");
    }

    private static Dictionary<string, ReadOnlyMemory<byte>> Inputs() => new(StringComparer.Ordinal)
    {
        ["map.json"] = Bytes("{\"id\":\"example\",\"terrainDefaults\":{\"lakeBankSlope\":3,\"lakeMaxDepth\":5,\"channelBankSlope\":2,\"channelDepth\":2.5,\"corridorShoulder\":1}}"),
        ["campus.json"] = Bytes("{\"features\":[]}"),
        ["zones.json"] = Bytes("{\"zones\":[]}"),
        ["overlay/terrain/fixture.terrain.json"] = Bytes("{\"zone\":\"fixture\",\"owner\":\"WP-08\",\"ops\":[{\"op\":\"pad\",\"id\":\"design:terrain:fixture-pad\",\"reason\":\"freshness fixture\",\"zone\":\"fixture\",\"feature\":\"design:fixture-pad-base\",\"grade\":6,\"padY\":0.5}]}")
    };

    private static WalkBakeData Product(string hash) => new(WalkBakeData.CurrentVersion, new MapId("example"), hash,
        new WalkGrid(Vector2.Zero, 4, 4, new byte[16], new byte[16], new float[16]),
        new SafePointGrid(Vector2.Zero, 1, 1, Vector3.Zero, [short.MinValue, short.MinValue, short.MinValue]), []);
    private static ReadOnlyMemory<byte> Bytes(string text) => Encoding.UTF8.GetBytes(text);
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
    private static void ExpectFailure(Action action, WalkBakeFreshnessFailure failure, string prefix)
    {
        try { action(); }
        catch (WalkBakeFreshnessException error)
        {
            Require(error.Failure == failure && error.Message.StartsWith(prefix, StringComparison.Ordinal), "Walk freshness failure was misclassified.");
            return;
        }
        throw new InvalidOperationException("An invalid startup walk product was accepted.");
    }
}
