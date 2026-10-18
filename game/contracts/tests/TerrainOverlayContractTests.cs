using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using GdUnit4;
using static GdUnit4.Assertions;
using static Campus.Contracts.Tests.TerrainOverlayFixture;

namespace Campus.Contracts.Tests;

[TestSuite]
public sealed class TerrainOverlayContractTests
{
    [TestCase]
    public void AllNineOperationsRoundTripWithCanonicalDefaultsAndMetadataFirst()
    {
        var parsed = TerrainOverlayJson.Parse(Encoding.UTF8.GetBytes(Fixture));
        if (((TerrainChannelOp)parsed.Ops[5]).Crossings is not { Count: 0 } ||
            ((TerrainChannelOp)parsed.Ops[5]).Wade is not { Count: 0 } ||
            ((TerrainCorridorOp)parsed.Ops[8]).Steps is not { Count: 0 })
            throw new InvalidOperationException("Omitted optional collections must retain their empty defaults.");
        var encoded = TerrainOverlayJson.ToUtf8Bytes(parsed);
        using var document = JsonDocument.Parse(encoded);
        foreach (var op in document.RootElement.GetProperty("ops").EnumerateArray())
            if (op.EnumerateObject().First().Name != "op") throw new InvalidOperationException("Operation metadata must be first.");
        var roundTrip = TerrainOverlayJson.Parse(encoded);
        if (!roundTrip.Ops.Select(op => op.Priority).SequenceEqual(parsed.Ops.Select(op => op.Priority)))
            throw new InvalidOperationException("Operation priorities failed to round-trip.");
        if (((TerrainChannelOp)roundTrip.Ops[5]).Crossings is not { Count: 0 } ||
            ((TerrainChannelOp)roundTrip.Ops[5]).Wade is not { Count: 0 } ||
            ((TerrainCorridorOp)roundTrip.Ops[8]).Steps is not { Count: 0 })
            throw new InvalidOperationException("Empty optional collections failed to round-trip.");
        var platform = (TerrainPlatformOp)roundTrip.Ops[0];
        var lake = (TerrainLakeOp)roundTrip.Ops[6];
        var pad = (TerrainPadOp)roundTrip.Ops[7];
        if (platform.Rings.Holes.Count != ((TerrainPlatformOp)parsed.Ops[0]).Rings.Holes.Count ||
            lake.Bed.Shelf != new TerrainBed(lake.Bed.MaxDepth).Shelf ||
            pad.Apron != new TerrainPadOp(pad.Id, pad.Reason, pad.Feature, pad.Grade) { Zone = pad.Zone }.Apron || pad.PadY is not null)
            throw new InvalidOperationException("Polygon holes or explicit normative defaults were lost.");
        var channel = (TerrainChannelOp)roundTrip.Ops[5];
        if (channel.Water != channel.Weirs[0].Upstream || channel.Weirs[0].Upstream == channel.Weirs[0].Downstream ||
            channel.Weirs[0].Structure != ((TerrainChannelOp)parsed.Ops[5]).Weirs[0].Structure)
            throw new InvalidOperationException("A channel lost its real structure/water-segment references.");
    }

    [TestCase]
    public void ExplicitPriorityAndPolylineSlopeRemainAuthoredInputs()
    {
        var root = JsonNode.Parse(Fixture)!.AsObject();
        var ops = root["ops"]!.AsArray();
        ops[0]!["priority"] = 1; // Overrides the type default; no hidden platform-after-mound sort is applied here.
        var slope = ops[3]!.AsObject();
        slope["geometry"] = JsonNode.Parse("""{"type":"polyline","line":{"points":[[0,0],[10,0]]}}""");
        slope.Remove("from"); slope.Remove("to"); slope["width"] = 2;
        ops[1]!["priority"] = int.MaxValue;
        ops[5]!["crossings"] = new JsonArray();
        ops[5]!["wade"] = new JsonArray();
        ops[8]!["steps"] = new JsonArray();
        var parsed = TerrainOverlayJson.Parse(Encoding.UTF8.GetBytes(root.ToJsonString()));
        if (parsed.Ops[0].Priority != 1 || parsed.Ops[1].Priority != int.MaxValue ||
            parsed.Ops[3] is not TerrainSlopeOp { Geometry: PolylineGeometry, Width: 2, From: null, To: null })
            throw new InvalidOperationException("Authored priority or valid polyline slope changed.");
        if (((TerrainChannelOp)parsed.Ops[5]).Crossings is not { Count: 0 } ||
            ((TerrainChannelOp)parsed.Ops[5]).Wade is not { Count: 0 } ||
            ((TerrainCorridorOp)parsed.Ops[8]).Steps is not { Count: 0 })
            throw new InvalidOperationException("Explicit empty arrays are valid and must not become null.");
    }

    [TestCase]
    public void UnknownOrIncompleteContractsFailRatherThanCreatePlausibleTerrain()
    {
        List<(Action<JsonObject> Mutate, bool Dry)> candidates =
        [
            (root => root["unapproved"] = 1, false),
            (root => root["ops"]![1]!["op"] = "noise", false),
            (root => root["ops"]![0]!.AsObject().Remove("op"), false),
            (root => root["ops"]![1]!["shape"]!.AsObject().Remove("shape"), false),
            (root => root["ops"]![1]!["unapproved"] = 1, false),
            (root => root["ops"]![0]!["id"] = "design:terrain:probe\n", false),
            (root => root["ops"]![0]!["side"]!["fill"] = "1:3\n", false),
            (root => root["ops"]![1]!["shape"]!["shape"] = "polygon", false),
            (root => root["ops"]![1]!["profile"] = "noise", false),
            (root => root["ops"]![1]!["profile"] = "hermite,hermite", false),
            (root => root["ops"]![6]!["bank"]!["kind"] = "soft,steps", false),
            (root => root["ops"]![5]!["weirs"]![0]!["kind"] = "weir,gate", false),
            (root => root["ops"]![7]!["edge"]!["kind"] = "edgeBand,terrace", false),
            (root => root["ops"]![0]!.AsObject().Remove("fillet"), false),
            (root => root["ops"]![0]!["priority"] = -1, false),
            (root => root["ops"]![0]!["priority"] = 2147483648L, false),
            (root => root["ops"]![5]!.AsObject().Remove("water"), false),
            (root => root["ops"]![5]!["waterY"] = null, false),
            (root => root["ops"]![5]!["dry"] = true, false),
            (root => root["ops"]![5]!["crossings"] = null, false),
            (root => root["ops"]![5]!["crossings"] = null, true),
            (root => root["ops"]![5]!["wade"] = null, false),
            (root => root["ops"]![5]!["wade"] = null, true),
            (root => root["ops"]![5]!["wade"] = new JsonArray(root["ops"]![0]!["rings"]!.DeepClone()), true),
            (root => root["ops"]![8]!["steps"] = null, false),
            (root => root["ops"]![5]!["bank"] = JsonNode.Parse("""{"kind":"swale","inner":"1:4"}"""), false),
            (root => root["ops"]![6]!["bank"] = JsonNode.Parse("""{"kind":"swale","inner":"1:4"}"""), false),
            (root => root["ops"]![5]!["crossings"] = JsonNode.Parse("""[{"road":"design:road:fixture","at":[4,0]}]"""), false),
            (root => root["ops"]![5]!["bank"] = JsonNode.Parse("""{"kind":"soft","slope":"1:4"}"""), true),
            (root => root["ops"]![5]!["bank"]!["outer"] = 1, true),
            (root => root["ops"]![5]!["bank"]!["outer"] = null, true),
            (root => root["ops"]![5]!["bank"]!["outer"] = "0", true),
            (root => root["ops"]![5]!["bank"]!["left"] = "1:4", true),
            (root => root["ops"]![5]!["bank"]!["slope"] = "1:4", true),
            (root => root["ops"]![5]!["bank"]!.AsObject().Remove("inner"), true),
        ];
        foreach (var invalidCrossing in new[]
        {
            """{"road":"design:road:fixture","at":[4,0],"endBefore":true}""",
            """{"road":"design:road:fixture","endBefore":false}""",
            """{"road":"design:road:fixture"}""",
            """{"at":[4,0]}""",
            """{"road":"design:road:fixture","at":null}""",
            """{"road":"design:road:fixture","at":[4,0],"roadWidth":2}"""
        })
            candidates.Add((root => root["ops"]![5]!["crossings"] = new JsonArray(JsonNode.Parse(invalidCrossing)), true));
        candidates.Add((root => root["ops"]![8]!["steps"] = JsonNode.Parse("""[{"s0":3,"s1":5}]"""), false));
        candidates.Add((root => root["ops"]![5]!["weirs"]![0]!["downstream"] = "design:water:upstream", false));
        candidates.Add((root => root["ops"]![8]!["anchors"]!.AsArray().RemoveAt(1), false));
        candidates.Add((root =>
        {
            root["ops"]![3]!["geometry"] = JsonNode.Parse("""{"type":"point","position":[0,0,0]}""");
        }, false));
        candidates.Add((root =>
        {
            var op = root["ops"]![0]!.AsObject();
            op.Remove("op"); op["op"] = "platform"; // Metadata moved after ordinary fields.
        }, false));

        foreach (var (mutate, dry) in candidates)
        {
            var bytes = InvalidCandidate(mutate, dry);
            bool rejected = false;
            try { TerrainOverlayJson.Parse(bytes); }
            catch (JsonException) { rejected = true; }
            AssertThat(rejected).OverrideFailureMessage("Invalid terrain candidate was accepted.").IsTrue();
        }
    }

}
