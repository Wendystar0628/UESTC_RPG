using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Maps;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Core;
using Campus.Gen.Parts.Street;
using Campus.Gen.World;
using GdUnit4;

namespace Campus.Gen.Furniture.Tests;

using static Campus.Gen.Furniture.Tests.FurnitureTestSupport;

[TestSuite]
public sealed class FurniturePlantingRowTests
{
    [TestCase]
    public void TreeRowsComeFromThePlantingInputAndFallBackWithOneWarning()
    {
        // The rows are read from the map's frozen planting rules, not copied: the real file gives the avenue's rows and its band,
        // and one row on each secondary road, with nothing to warn about.
        var (roads, diagnostics) = PlantingRows.Resolve(Inputs());
        Check(diagnostics.Count == 0, "warnings on the real planting file: " + string.Join(" | ", diagnostics.Select(d => d.Message)));
        Check(roads.Select(r => r.Name).SequenceEqual(FurnitureStyle.Roads.Select(r => r.Name)) && roads.Select(r => r.Tier).SequenceEqual(FurnitureStyle.Roads.Select(r => r.Tier)), "the roads or their tiers changed");
        // Expected offsets are parsed from the same file here (WP-23 owns the values and may change them): every row, then the band.
        var file = JsonNode.Parse(PlantingBytes.Value)!["roads"]!.AsArray();
        float[] Offsets(int i) => [.. file[i]!["rows"]!.AsArray().Select(r => r!["offset"]!.GetValue<float>()),
            .. file[i]!["band"] is JsonNode band ? [band["offset"]!.GetValue<float>()] : Array.Empty<float>()];
        Check(Enumerable.Range(0, 3).All(i => roads[i].Rows.SequenceEqual(Offsets(i))) && roads[0].Rows.Count >= 2, "rows differ from the planting file");
        // A changed offset is read back; a road the file leaves out keeps its fallback and is warned about once.
        var edited = Edit(root => root["roads"]![1]!["rows"] = new JsonArray(new JsonObject { ["offset"] = 6.5f }));
        Check(PlantingRows.Resolve(Inputs(edited)).Roads[1].Rows.SequenceEqual([6.5f]), "an edited offset is not read");
        var (kept, gap) = PlantingRows.Resolve(Inputs(Edit(root => root["roads"]!.AsArray().RemoveAt(2))));
        Check(kept[2].Rows.SequenceEqual(FurnitureStyle.Roads[2].Rows) && gap.Count == 1 && gap[0].Level == DiagnosticLevel.Warning && gap[0].Code == PlantingRows.RoadCode && gap[0].Message.Contains("丹桂路", StringComparison.Ordinal),
            "a road missing from the planting file is not warned about once");
        // Nothing to read, or nothing readable: the fallback stands, with exactly one warning, and the layout still builds.
        (IReadOnlyDictionary<string, ReadOnlyMemory<byte>>? Inputs, string Code)[] unreadable =
        [
            (null, PlantingRows.MissingCode), (new Dictionary<string, ReadOnlyMemory<byte>>(), PlantingRows.MissingCode),
            (Inputs([0x7B, 0x22]), PlantingRows.UnreadableCode), (Inputs("[]"u8.ToArray()), PlantingRows.UnreadableCode),
        ];
        foreach (var (inputs, code) in unreadable)
        {
            var (fallback, warnings) = PlantingRows.Resolve(inputs);
            Check(fallback.Select(r => r.Rows).Zip(FurnitureStyle.Roads.Select(r => r.Rows), (a, b) => a.SequenceEqual(b)).All(same => same), "the fallback rows are not the style's");
            Check(warnings.Count == 1 && warnings[0].Level == DiagnosticLevel.Warning && warnings[0].Code == code, $"{warnings.Count} diagnostics for {code}");
            var layout = FurniturePlanner.Build(World.Value, null, inputs);
            Check(layout.Diagnostics.Where(d => d.Code.StartsWith("furniture.planting.", StringComparison.Ordinal)).SequenceEqual(warnings) && layout.Items.Count == Build(World.Value).Items.Count, "the fallback layout differs from the real one, whose rows are the same");
        }
        Check(!Build(World.Value).Diagnostics.Any(d => d.Code.StartsWith("furniture.planting.", StringComparison.Ordinal)), "the real layout warns");
    }

    [TestCase]
    public void ChangingARowOffsetInThePlantingInputMovesTheBenches()
    {
        // A bench keeps RowClear off the planted rows, so moving a row onto an avenue bench forces that bench off its slot (or off
        // the walk altogether); the other roads' benches stay where they were.
        var world = World.Value;
        var before = Build(world);
        var avenue = world.Network.Roads.Where(l => StreetSite.RoadName(l) == FurnitureStyle.Roads[0].Name).ToList();
        var bench = before.Items.First(i => i.Kind == FurnitureKind.Bench && i.Key.StartsWith("rest:0:", StringComparison.Ordinal));
        var offset = avenue.Min(l => Distance(bench.Anchor, l));
        var moved = Build(world, null, Inputs(Edit(root => root["roads"]![0]!["rows"] = new JsonArray(new JsonObject { ["offset"] = offset }))));
        Console.WriteLine($"{bench.Key} stood {offset:F2} m off the avenue's centre line; a row there: {(moved.Items.FirstOrDefault(i => i.Key == bench.Key) is { } m ? $"moved {Vector2.Distance(m.Anchor, bench.Anchor):F2} m" : "dropped")}");
        var after = moved.Items.FirstOrDefault(i => i.Key == bench.Key);
        Check(after is null || Vector2.Distance(after.Anchor, bench.Anchor) > 0.5f, $"{bench.Key} stands where it did, on a tree row");
        // And the row it now keeps clear of is the new one: every bench of the avenue is off the offset line.
        foreach (var item in moved.Items.Where(i => i.Kind is FurnitureKind.Bench or FurnitureKind.Bin && i.Key.StartsWith("rest:0:", StringComparison.Ordinal)))
            Check(MathF.Abs(avenue.Min(l => Distance(item.Anchor, l)) - offset) >= FurnitureStyle.RowClear, $"{item.Key} stands on the changed row");
        // Signposts, fountains and the service things (13c2c) come last and keep off the rows and what stands before them: they may move too.
        foreach (var item in before.Items.Where(i => !i.Key.StartsWith("rest:0:", StringComparison.Ordinal) && !i.Key.StartsWith("lamp:0:", StringComparison.Ordinal)
            && i.Kind is not (FurnitureKind.Signpost or FurnitureKind.DrinkingFountain or FurnitureKind.VendingMachine or FurnitureKind.DeliveryLocker
                or FurnitureKind.BicycleBay or FurnitureKind.ParkedBicycle)))
            Check(moved.Items.Any(m => m.Key == item.Key && Vector2.Distance(m.Anchor, item.Anchor) < 0.01f), $"{item.Key} moved with the avenue's rows");
    }
}
