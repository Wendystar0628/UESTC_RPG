using System.Text.Json.Nodes;
using Campus.Kernel.Registry;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Campus.Tests;

[TestSuite]
public sealed class CoreRuntimeSnapshotTests
{
    private const string Snapshot = """
        {"maps":{"current":"fixture-map","visits":[{"map":"fixture-map","lastPose":{"position":[10,1.72,20],"yawDeg":30,"pitchDeg":0},"mode":"walk"}]},"view":{"mode":"walk","pose":{"position":[10,1.72,20],"yawDeg":30,"pitchDeg":0},"space":{"feature":"fixture:room","storey":1},"speedStep":2},"walk":{"feet":{"position":[10,0.1,20],"yawDeg":30,"pitchDeg":0},"crouching":false,"speedStep":2,"safePoints":[{"position":[10,0.1,20],"yawDeg":30,"pitchDeg":0},{"position":[40,0.1,50],"yawDeg":60,"pitchDeg":0}]},"world":{"worldHours":17.5,"season":"autumn","rate":0},"weather":{"seed":1,"automatic":true,"wetness":0.25}}
        """;

    [TestCase]
    public void CentimetreLandingCorrectionMatches()
    {
        var restored = JsonNode.Parse(Snapshot)!.AsObject();
        foreach (var position in Positions(restored)) position[1] = position[1]!.GetValue<double>() - .01;
        var captured = restored.ToJsonString();
        AssertThat(StageGateDriver.CoreSnapshotsMatch(Snapshot, captured))
            .OverrideFailureMessage("A 1 cm support correction should match all allowed runtime positions.").IsTrue();
    }

    [TestCase]
    public void EveryAllowedPositionRejectsMoreThanTwoCentimetresInEuclideanDistance()
    {
        for (var index = 0; index < Positions(JsonNode.Parse(Snapshot)!.AsObject()).Length; index++)
        {
            var restored = JsonNode.Parse(Snapshot)!.AsObject();
            var position = Positions(restored)[index];
            // Each axis stays below 2 cm, while the combined displacement exceeds it.
            for (var axis = 0; axis < 3; axis++) position[axis] = position[axis]!.GetValue<double>() + .015;
            AssertThat(!StageGateDriver.CoreSnapshotsMatch(Snapshot, restored.ToJsonString()))
                .OverrideFailureMessage("Runtime position " + index + " exceeded the 2 cm distance limit.").IsTrue();
        }
    }

    [TestCase]
    public void NonPositionFieldsAndArrayStructureRemainExact()
    {
        Action<JsonObject>[] changes =
        [
            state => state["walk"]!["feet"]!["yawDeg"] = 30.001,
            state => state["walk"]!["crouching"] = true,
            state => state["walk"]!["speedStep"] = 3,
            state => state["view"]!["mode"] = "fly",
            state => state["view"]!["pose"]!["pitchDeg"] = .001,
            state => state["view"]!["space"]!["feature"] = "fixture:other-room",
            state => state["view"]!["space"]!["storey"] = 2,
            state => state["maps"]!["current"] = "fixture-other-map",
            state => state["maps"]!["visits"]![0]!["map"] = "fixture-other-map",
            state => state["maps"]!["visits"]![0]!["mode"] = "fly",
            state => state["world"]!["worldHours"] = 17.50001,
            state => state["world"]!["rate"] = .00001,
            state => state["weather"]!["seed"] = 2,
            state => state["weather"]!["wetness"] = .25001,
            state => state["walk"]!["safePoints"]!.AsArray().RemoveAt(1),
            state =>
            {
                var points = state["walk"]!["safePoints"]!.AsArray();
                var first = points[0];
                points.RemoveAt(0);
                points.Add(first);
            }
        ];
        for (var i = 0; i < changes.Length; i++)
        {
            var restored = JsonNode.Parse(Snapshot)!.AsObject();
            changes[i](restored);
            AssertThat(!StageGateDriver.CoreSnapshotsMatch(Snapshot, restored.ToJsonString()))
                .OverrideFailureMessage("Non-position or array change " + i + " must not match.").IsTrue();
        }
    }

    private static JsonArray[] Positions(JsonObject state) =>
    [
        state["walk"]!["feet"]!["position"]!.AsArray(),
        state["view"]!["pose"]!["position"]!.AsArray(),
        state["maps"]!["visits"]![0]!["lastPose"]!["position"]!.AsArray(),
        .. state["walk"]!["safePoints"]!.AsArray().Select(point => point!["position"]!.AsArray())
    ];
}
