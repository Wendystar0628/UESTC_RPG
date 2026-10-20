using Campus.Tests;
using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Campus.Contracts.Tests;

[TestSuite]
public sealed class TerrainCorridorRangeTests
{
    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void LegacyConstructorAndMissingRangeKeepTheFullRoadWireContract()
    {
        var legacy = new TerrainCorridorOp("design:terrain:legacy", "Synthetic range boundary", new("source:road"),
            [new(0, 1), new(100, 1)], .015, .75)
        { Zone = new("fixture") };
        var bytes = TerrainOverlayJson.ToUtf8Bytes(new(new("fixture"), [legacy]));
        using var wire = JsonDocument.Parse(bytes);
        AssertThat(!wire.RootElement.GetProperty("ops")[0].TryGetProperty("range", out _)).OverrideFailureMessage("Legacy serialization must not acquire a range field.").IsTrue();
        var restored = (TerrainCorridorOp)TerrainOverlayJson.Parse(bytes).Ops[0];
        AssertThat(restored.Range is null && restored.Anchors[0].S == 0 && restored.Anchors[^1].S == 100 && restored.Steps.Count == 0).OverrideFailureMessage("Missing range changed legacy source-road anchor semantics.").IsTrue();
        var explicitNull = JsonNode.Parse(Encoding.UTF8.GetString(bytes))!.AsObject();
        explicitNull["ops"]![0]!["range"] = null;
        AssertThat(((TerrainCorridorOp)Parse(explicitNull).Ops[0]).Range is null).OverrideFailureMessage("Nullable range must canonicalize to the legacy absence state.").IsTrue();
        explicitNull["ops"]![0]!["anchors"]![0]!["s"] = 20;
        {
            bool rejected = false;
            try { Parse(explicitNull); }
            catch (JsonException) { rejected = true; }
            AssertThat(rejected).OverrideFailureMessage("Expected JsonException.").IsTrue();
        }
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void ExplicitRangeRoundTripsAbsoluteStationsAndRealStepReferences()
    {
        var parsed = Parse(RangedFixture());
        var restored = (TerrainCorridorOp)TerrainOverlayJson.Parse(TerrainOverlayJson.ToUtf8Bytes(parsed)).Ops[0];
        AssertThat(restored.Range == new TerrainStationRange(20, 80) &&
            restored.Anchors[0].S == 20 && restored.Anchors[1].S == 50 && restored.Anchors[^1].S == 80).OverrideFailureMessage("A range must preserve original source-road stations rather than rebasing its fragment to zero.").IsTrue();
        AssertThat(restored.Steps is [{ S0: 30, S1: 35 }] && restored.Steps[0].Structure == ((TerrainCorridorOp)parsed.Ops[0]).Steps[0].Structure).OverrideFailureMessage("An in-range step lost its absolute interval or real structure reference.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
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
            {
                bool rejected = false;
                try { Parse(root); }
                catch (JsonException) { rejected = true; }
                AssertThat(rejected).OverrideFailureMessage("Expected JsonException.").IsTrue();
            }
        }
        foreach (var range in new[] { new TerrainStationRange(double.NaN, 80), new TerrainStationRange(20, double.PositiveInfinity) })
        {
            var parsed = Parse(RangedFixture());
            var invalid = ((TerrainCorridorOp)parsed.Ops[0]) with { Range = range };
            AssertThrown(() => TerrainOverlayJson.Validate(parsed with { Ops = [invalid] })).IsInstanceOf<ArgumentException>();
        }
    }

    private static JsonObject Op(JsonObject root) => root["ops"]![0]!.AsObject();
    private static TerrainOverlayFile Parse(JsonObject root) => TerrainOverlayJson.Parse(Encoding.UTF8.GetBytes(root.ToJsonString()));
    private static JsonObject RangedFixture() => JsonNode.Parse("""
        {"zone":"fixture","ops":[
          {"op":"corridor","id":"design:terrain:ranged","zone":"fixture","reason":"Synthetic range boundary",
           "road":"source:road","anchors":[{"s":20,"y":1},{"s":50,"y":2},{"s":80,"y":1}],
           "crossFall":0.015,"shoulder":0.75,"range":{"s0":20,"s1":80},
           "steps":[{"s0":30,"s1":35,"structure":"source:stairs"}]}]}
        """)!.AsObject();
}
