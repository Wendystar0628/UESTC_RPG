using Campus.Tests;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using GdUnit4;
using static GdUnit4.Assertions;
using static Campus.Contracts.Tests.TerrainOverlayFixture;

namespace Campus.Contracts.Tests;

[TestSuite]
public sealed class TerrainChannelContractTests
{
    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void ChannelWadePreservesLegacyConstructorAndDeconstructSignatures()
    {
        var parsed = TerrainOverlayJson.Parse(Encoding.UTF8.GetBytes(Fixture));
        var source = (TerrainChannelOp)parsed.Ops[5];
        var minimal = new TerrainChannelOp(source.Id, source.Reason, source.Water, source.Depth,
            source.Bank, source.WaterY, source.Weirs)
        {
            Zone = source.Zone
        };
        var full = new TerrainChannelOp(source.Id, source.Reason, source.Water, source.Depth,
            source.Bank, source.WaterY, source.Weirs, source.Line, source.Width, source.Priority, source.Crossings)
        {
            Zone = source.Zone
        };
        var (id, reason, water, depth, bank, waterY, weirs, line, width, priority, crossings) = full;
        if (id != source.Id || reason != source.Reason || water != source.Water || depth != source.Depth ||
            bank != source.Bank || waterY != source.WaterY || weirs != source.Weirs || line != source.Line ||
            width != source.Width || priority != source.Priority || crossings != source.Crossings ||
            minimal.Wade is not { Count: 0 } || full.Wade is not { Count: 0 })
            throw new InvalidOperationException("Legacy channel construction or deconstruction changed.");
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void ChannelWadeOmittedAndEmptyListsStayEmptyForWetAndDryChannels()
    {
        foreach (var dry in new[] { false, true })
            foreach (var explicitEmpty in new[] { false, true })
            {
                var root = dry ? DryFixture() : JsonNode.Parse(Fixture)!.AsObject();
                if (explicitEmpty) root["ops"]![5]!["wade"] = new JsonArray();
                var parsed = TerrainOverlayJson.Parse(Encoding.UTF8.GetBytes(root.ToJsonString()));
                var roundTrip = TerrainOverlayJson.Parse(TerrainOverlayJson.ToUtf8Bytes(parsed));
                if (((TerrainChannelOp)parsed.Ops[5]).Wade is not { Count: 0 } ||
                    ((TerrainChannelOp)roundTrip.Ops[5]).Wade is not { Count: 0 })
                    throw new InvalidOperationException("An omitted or empty channel Wade list became null or nonempty.");
            }
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void ChannelWadeRoundTripsIndependentPolygonsAndTheirHoles()
    {
        var root = JsonNode.Parse(Fixture)!.AsObject();
        var wade = new JsonArray(root["ops"]![0]!["rings"]!.DeepClone(),
            JsonNode.Parse("""{"outer":[[12,0],[16,0],[16,4],[12,4]],"holes":[[[13,1],[14,1],[13,2]]]}"""));
        root["ops"]![5]!["wade"] = wade;
        var parsed = TerrainOverlayJson.Parse(Encoding.UTF8.GetBytes(root.ToJsonString()));
        var encoded = TerrainOverlayJson.ToUtf8Bytes(parsed);
        var roundTrip = TerrainOverlayJson.Parse(encoded);
        var channel = (TerrainChannelOp)roundTrip.Ops[5];
        var wire = JsonNode.Parse(encoded)!;
        if (channel.Wade.Count != 2 || channel.Wade.Any(polygon => polygon.Holes.Count != 1) ||
            !JsonNode.DeepEquals(wade, wire["ops"]![5]!["wade"]) ||
            channel.Water != ((TerrainChannelOp)parsed.Ops[5]).Water || channel.WaterY != -0.5 || channel.Depth != 1.2)
            throw new InvalidOperationException("Channel Wade polygons, holes, or existing water/depth fields changed.");
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void DryChannelsAreExplicitAndStepsRetainTheirRealStructureReference()
    {
        var root = DryFixture();
        var channel = root["ops"]![5]!;
        channel["crossings"] = JsonNode.Parse("""[{"road":"design:road:underpass","at":[4,0]},{"road":"design:road:stop","endBefore":true}]""");
        root["ops"]![8]!["steps"] = JsonNode.Parse("""[{"s0":3,"s1":5,"structure":"design:stairs:fixture"}]""");
        var parsed = TerrainOverlayJson.Parse(Encoding.UTF8.GetBytes(root.ToJsonString()));
        var encoded = TerrainOverlayJson.ToUtf8Bytes(parsed);
        var roundTrip = TerrainOverlayJson.Parse(encoded);
        if (roundTrip.Ops[5] is not TerrainChannelOp { Dry: true, Water: null, WaterY: null, Line: not null, Width: 2 })
            throw new InvalidOperationException("A dry channel acquired water or lost its explicit line.");
        var dryChannel = (TerrainChannelOp)roundTrip.Ops[5];
        if (dryChannel.Bank is not { Kind: TerrainBankKind.Swale, Slope: null, Inner: "1:4", Outer: "1:6" } ||
            dryChannel.Crossings is not [TerrainUnderpassCrossing { At.X: 4, At.Y: 0 }, TerrainEndBeforeCrossing])
            throw new InvalidOperationException("Symmetric swale or crossing alternatives were conflated.");
        var steps = ((TerrainCorridorOp)roundTrip.Ops[8]).Steps;
        if (steps.Count != 1 || steps[0].Structure != ((TerrainCorridorOp)parsed.Ops[8]).Steps[0].Structure)
            throw new InvalidOperationException("A steps interval lost its real stair reference.");
        using var wire = JsonDocument.Parse(encoded);
        var dry = wire.RootElement.GetProperty("ops")[5];
        if (dry.GetProperty("water").ValueKind != JsonValueKind.Null || dry.GetProperty("waterY").ValueKind != JsonValueKind.Null)
            throw new InvalidOperationException("Required null fields must round-trip explicitly despite global null omission.");
        var crossings = dry.GetProperty("crossings");
        if (crossings[0].TryGetProperty("endBefore", out _) || crossings[1].TryGetProperty("at", out _) ||
            !crossings[1].GetProperty("endBefore").GetBoolean())
            throw new InvalidOperationException("Crossing JSON must preserve exactly one locator form.");
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void ZeroOrOmittedSwaleOuterCanonicalizesWithoutCreatingAnOuterSlope()
    {
        foreach (var omit in new[] { false, true })
        {
            var root = DryFixture();
            var bank = root["ops"]![5]!["bank"]!.AsObject();
            if (omit) bank.Remove("outer"); else bank["outer"] = 0;
            var parsed = TerrainOverlayJson.Parse(Encoding.UTF8.GetBytes(root.ToJsonString()));
            var encoded = TerrainOverlayJson.ToUtf8Bytes(parsed);
            var roundTrip = TerrainOverlayJson.Parse(encoded);
            if (((TerrainChannelOp)roundTrip.Ops[5]).Bank.Outer is not null)
                throw new InvalidOperationException("An absent outer segment acquired a slope.");
            using var wire = JsonDocument.Parse(encoded);
            if (wire.RootElement.GetProperty("ops")[5].GetProperty("bank").TryGetProperty("outer", out _))
                throw new InvalidOperationException("Canonical no-outer form must omit the optional field.");
        }
    }

}
