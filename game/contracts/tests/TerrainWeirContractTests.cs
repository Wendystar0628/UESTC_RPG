using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using GdUnit4;
using static GdUnit4.Assertions;
using static Campus.Contracts.Tests.TerrainOverlayFixture;

namespace Campus.Contracts.Tests;

[TestSuite]
public sealed class TerrainWeirContractTests
{
    [TestCase]
    public void TwoStepWeirsUseAnIndependentMiddleLevelRegardlessOfListOrLineDirection()
    {
        foreach (var reverseList in new[] { false, true })
            foreach (var reverseLine in new[] { false, true })
            {
                var root = JsonNode.Parse(TwoStepFixture)!;
                var middle = root["ops"]![1]!;
                var weirs = middle["weirs"]!.AsArray();
                if (reverseLine)
                {
                    middle["line"] = JsonNode.Parse("""{"points":[[1.5,0],[0,0]]}""");
                    weirs[0]!["s"] = 1.5; weirs[1]!["s"] = 0d;
                }
                if (reverseList)
                {
                    var first = weirs[0]!.DeepClone();
                    weirs.RemoveAt(0); weirs.Add(first);
                }
                var parsed = TerrainOverlayJson.Parse(Encoding.UTF8.GetBytes(root.ToJsonString()));
                var roundTrip = TerrainOverlayJson.Parse(TerrainOverlayJson.ToUtf8Bytes(parsed));
                var channels = roundTrip.Ops.Cast<TerrainChannelOp>().ToArray();
                var chain = channels[1];
                if (channels.Select(c => c.Water).Distinct().Count() != 3 ||
                    channels[0].WaterY != -0.5 || chain.WaterY != -0.85 || channels[2].WaterY != -1.2 ||
                    chain.Weirs.Count != 2 || chain.Weirs.Any(w => w.Drop != 0.35) ||
                    Math.Abs(chain.Weirs[0].S - chain.Weirs[1].S) != 1.5)
                    throw new InvalidOperationException("Two-step water identities, fixed levels, drops or station spacing changed.");
                for (var i = 0; i < chain.Weirs.Count; i++)
                    if (chain.Weirs[i].Structure.Value != weirs[i]!["structure"]!.GetValue<string>() ||
                        chain.Weirs[i].S != weirs[i]!["s"]!.GetValue<double>())
                        throw new InvalidOperationException("Round-trip reordered weirs or inferred a station direction.");
                var upper = chain.Weirs.Single(w => w.Upstream == channels[0].Water);
                var lower = chain.Weirs.Single(w => w.Downstream == channels[2].Water);
                if (upper.Downstream != chain.Water || lower.Upstream != chain.Water || upper.Structure == lower.Structure)
                    throw new InvalidOperationException("Two distinct structures must join A to B and B to C.");
            }
    }

    [TestCase]
    public void TwoStepWeirsRejectOversizeDropsShortStationsAndReusedStructures()
    {
        foreach (var invalid in new[] { "drop", "station", "structure", "reverse-station", "reverse-structure" })
        {
            var root = JsonNode.Parse(TwoStepFixture)!;
            _ = TerrainOverlayJson.Parse(Encoding.UTF8.GetBytes(root.ToJsonString()));
            var weirs = root["ops"]![1]!["weirs"]!.AsArray();
            switch (invalid)
            {
                case "drop": weirs.RemoveAt(1); weirs[0]!["drop"] = 0.7; break;
                case "station": case "reverse-station": weirs[1]!["s"] = 1.499; break;
                case "structure": case "reverse-structure": weirs[1]!["structure"] = weirs[0]!["structure"]!.GetValue<string>(); break;
                default: throw new InvalidOperationException("Unknown negative case.");
            }
            if (invalid.StartsWith("reverse-", StringComparison.Ordinal))
            {
                var first = weirs[0]!.DeepClone();
                weirs.RemoveAt(0); weirs.Add(first);
            }
            try { TerrainOverlayJson.Parse(Encoding.UTF8.GetBytes(root.ToJsonString())); }
            catch (JsonException)
            { continue; }
            throw new InvalidOperationException($"Invalid two-step candidate was accepted: {invalid}");
        }
    }

    [TestCase]
    public void ChainedWeirRulePreservesParallelStructuresAndOtherKinds()
    {
        foreach (var kind in new[] { "parallel", "gate", "culvert" })
        {
            var root = JsonNode.Parse(TwoStepFixture)!;
            var weirs = root["ops"]![1]!["weirs"]!;
            weirs[1]!["s"] = 0;
            if (kind == "parallel")
            {
                weirs[1]!["upstream"] = "design:water:fixture-d";
                weirs[1]!["downstream"] = "design:water:fixture-b";
            }
            else weirs[1]!["kind"] = kind;
            _ = TerrainOverlayJson.Parse(Encoding.UTF8.GetBytes(root.ToJsonString()));
        }
    }

    [TestCase]
    public void WeirNumbersRemainFiniteWithoutClamping()
    {
        var parsed = TerrainOverlayJson.Parse(Encoding.UTF8.GetBytes(TwoStepFixture));
        var middle = (TerrainChannelOp)parsed.Ops[1];
        foreach (var value in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity })
            foreach (var field in new[] { "s", "drop" })
            {
                var weirs = middle.Weirs.ToArray();
                weirs[0] = field == "s" ? weirs[0] with { S = value } : weirs[0] with { Drop = value };
                var ops = parsed.Ops.ToArray(); ops[1] = middle with { Weirs = weirs };
                try { TerrainOverlayJson.Validate(parsed with { Ops = ops }); }
                catch (ArgumentException) { continue; }
                throw new InvalidOperationException($"Non-finite weir {field} was accepted.");
            }
    }

}
