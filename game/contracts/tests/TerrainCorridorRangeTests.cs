using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using GdUnit4;

namespace Campus.Contracts.Tests;

[TestSuite]
public sealed class TerrainCorridorRangeTests
{
    [TestCase]
    public void LegacyConstructorAndMissingRangeKeepTheFullRoadWireContract()
    {
        var legacy = new TerrainCorridorOp("design:terrain:legacy", "Synthetic range boundary", new("source:road"),
            [new(0, 1), new(100, 1)], .015, .75)
        { Zone = new("fixture") };
        var bytes = TerrainOverlayJson.ToUtf8Bytes(new(new("fixture"), new("WP-00"), [legacy]));
        using var wire = JsonDocument.Parse(bytes);
        Require(!wire.RootElement.GetProperty("ops")[0].TryGetProperty("range", out _), "Legacy serialization must not acquire a range field.");
        var restored = (TerrainCorridorOp)TerrainOverlayJson.Parse(bytes).Ops[0];
        Require(restored.Range is null && restored.Anchors[0].S == 0 && restored.Anchors[^1].S == 100 && restored.Steps.Count == 0,
            "Missing range changed legacy source-road anchor semantics.");
        var explicitNull = JsonNode.Parse(Encoding.UTF8.GetString(bytes))!.AsObject();
        explicitNull["ops"]![0]!["range"] = null;
        Require(((TerrainCorridorOp)Parse(explicitNull).Ops[0]).Range is null, "Nullable range must canonicalize to the legacy absence state.");
        explicitNull["ops"]![0]!["anchors"]![0]!["s"] = 20;
        Expect<JsonException>(() => Parse(explicitNull));
    }

    [TestCase]
    public void ExplicitRangeRoundTripsAbsoluteStationsAndRealStepReferences()
    {
        var parsed = Parse(RangedFixture());
        var restored = (TerrainCorridorOp)TerrainOverlayJson.Parse(TerrainOverlayJson.ToUtf8Bytes(parsed)).Ops[0];
        Require(restored.Range == new TerrainStationRange(20, 80) &&
            restored.Anchors[0].S == 20 && restored.Anchors[1].S == 50 && restored.Anchors[^1].S == 80,
            "A range must preserve original source-road stations rather than rebasing its fragment to zero.");
        Require(restored.Steps is [{ S0: 30, S1: 35 }] && restored.Steps[0].Structure == new FeatureId("source:stairs"),
            "An in-range step lost its absolute interval or real structure reference.");
    }

    [TestCase]
    public void MalformedRangesUncoveredEndpointsAndEscapingStepsFailAtTheDtoBoundary()
    {
        foreach (var mutate in new Action<JsonObject>[]
        {
            root => Op(root)["range"]!["s0"] = -1,
            root => Op(root)["range"]!["s1"] = 20,
            root => Op(root)["range"]!["s1"] = 10,
            root => Op(root)["range"]!.AsObject().Remove("s1"),
            root => Op(root)["range"]!["unexpected"] = 1,
            root => Op(root)["range"]!["s1"] = "80",
            root => Op(root)["anchors"]![0]!["s"] = 0,
            root => Op(root)["anchors"]![2]!["s"] = 100,
            root => Op(root)["anchors"]![1]!["s"] = 20,
            root => Op(root)["steps"]![0]!["s0"] = 19,
            root => Op(root)["steps"]![0]!["s1"] = 81
        })
        {
            var root = RangedFixture(); mutate(root);
            Expect<JsonException>(() => Parse(root));
        }
        foreach (var range in new[] { new TerrainStationRange(double.NaN, 80), new TerrainStationRange(20, double.PositiveInfinity) })
        {
            var parsed = Parse(RangedFixture());
            var invalid = ((TerrainCorridorOp)parsed.Ops[0]) with { Range = range };
            Expect<ArgumentException>(() => TerrainOverlayJson.Validate(parsed with { Ops = [invalid] }));
        }
    }

    private static JsonObject Op(JsonObject root) => root["ops"]![0]!.AsObject();
    private static TerrainOverlayFile Parse(JsonObject root) => TerrainOverlayJson.Parse(Encoding.UTF8.GetBytes(root.ToJsonString()));
    private static JsonObject RangedFixture() => JsonNode.Parse("""
        {"zone":"fixture","owner":"WP-00","ops":[
          {"op":"corridor","id":"design:terrain:ranged","zone":"fixture","reason":"Synthetic range boundary",
           "road":"source:road","anchors":[{"s":20,"y":1},{"s":50,"y":2},{"s":80,"y":1}],
           "crossFall":0.015,"shoulder":0.75,"range":{"s0":20,"s1":80},
           "steps":[{"s0":30,"s1":35,"structure":"source:stairs"}]}]}
        """)!.AsObject();
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Expect<TException>(Action action) where TException : Exception
    {
        try { action(); }
        catch (TException) { return; }
        throw new InvalidOperationException($"Expected {typeof(TException).Name}.");
    }
}
