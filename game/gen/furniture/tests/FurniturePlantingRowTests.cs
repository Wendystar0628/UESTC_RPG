using Campus.Gen.Geometry;
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
using Campus.Tests;

namespace Campus.Gen.Furniture.Tests;

using static Campus.Gen.Furniture.Tests.FurnitureTestSupport;

using static GdUnit4.Assertions;

[TestSuite]
public sealed class FurniturePlantingRowTests
{
    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void TreeRowsComeFromThePlantingInputAndAreLeftOutWithOneWarning()
    {
        // The rows are read from the map's frozen planting rules, not copied: every road the real file lists gives its rows and its band,
        // with nothing to warn about.
        var (roads, diagnostics) = PlantingRows.Resolve(Inputs());
        AssertThat(diagnostics.Count == 0).OverrideFailureMessage("warnings on the real planting file: " + string.Join(" | ", diagnostics.Select(d => d.Message))).IsTrue();
        // Expected offsets are parsed from the same file here (game/gen/vegetation/ owns the values and may change them): every row, then the band.
        var file = JsonNode.Parse(PlantingBytes.Value)!["roads"]!.AsArray();
        float[] Offsets(int i) => [.. file[i]!["rows"]!.AsArray().Select(r => r!["offset"]!.GetValue<float>()),
            .. file[i]!["band"] is JsonNode band ? [band["offset"]!.GetValue<float>()] : Array.Empty<float>()];
        AssertThat(roads.Count == file.Count && Enumerable.Range(0, file.Count).All(i => roads[i].Name == file[i]!["name"]!.GetValue<string>() && roads[i].Offsets.SequenceEqual(Offsets(i)))).OverrideFailureMessage("rows differ from the planting file").IsTrue();
        // A changed offset is read back; a road whose offsets cannot be read is left out and warned about once.
        var edited = Edit(root => root["roads"]![1]!["rows"] = new JsonArray(new JsonObject { ["offset"] = 6.5f }));
        AssertThat(PlantingRows.Resolve(Inputs(edited)).Roads[1].Offsets.SequenceEqual([6.5f])).OverrideFailureMessage("an edited offset is not read").IsTrue();
        var (kept, gap) = PlantingRows.Resolve(Inputs(Edit(root => root["roads"]![2]!["rows"] = new JsonArray(new JsonObject { ["offset"] = -1f }))));
        AssertThat(kept.Count == roads.Count - 1 && gap.Count == 1 && gap[0].Level == DiagnosticLevel.Warning && gap[0].Code == PlantingRows.RoadCode).OverrideFailureMessage("a road with unreadable offsets is not warned about once").IsTrue();
        // The optional input is missing: no rows, with exactly one warning, and the layout still builds
        // (judged on the avenue's road alone: the rows only touch the roads they are on, and the real layout is planned once elsewhere).
        (IReadOnlyDictionary<string, ReadOnlyMemory<byte>>? Inputs, string Code)[] missing =
        [
            (null, PlantingRows.MissingCode), (new Dictionary<string, ReadOnlyMemory<byte>>(), PlantingRows.MissingCode),
        ];
        foreach (var (inputs, code) in missing)
        {
            var (none, warnings) = PlantingRows.Resolve(inputs);
            AssertThat(none.Count == 0 && warnings.Count == 1 && warnings[0].Level == DiagnosticLevel.Warning && warnings[0].Code == code).OverrideFailureMessage($"{warnings.Count} diagnostics for {code}").IsTrue();
            var layout = FurniturePlanner.Build(FurnitureTestSupport.GlobalInputs, Campus.Contracts.Tests.PaletteFixture.Catalog, OpenWorld.Value, null, inputs);
            AssertThat(layout.Diagnostics.Where(d => d.Code.StartsWith("furniture.planting.", StringComparison.Ordinal)).SequenceEqual(warnings) && layout.Items.Any(i => i.Kind == FurnitureKind.StreetLamp)).OverrideFailureMessage("the layout without rows does not build with the one warning").IsTrue();
        }
        AssertThat(!RealLayout.Value.Diagnostics.Any(d => d.Code.StartsWith("furniture.planting.", StringComparison.Ordinal))).OverrideFailureMessage("the real layout warns").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Regression)]
    [TestCategory("Slow")]
    public void MalformedPlantingInputReachesTheBuildFailureFlow()
    {
        foreach (var bytes in new[] { new byte[] { 0x7B, 0x22 }, "[]"u8.ToArray() })
        {
            var inputs = Inputs(bytes);
            Action[] readers =
            [
                () => { _ = PlantingRows.Resolve(inputs); },
                () => { _ = FurniturePlanner.Build(FurnitureTestSupport.GlobalInputs, Campus.Contracts.Tests.PaletteFixture.Catalog, OpenWorld.Value, null, inputs); }
            ];
            foreach (var read in readers)
            {
                InvalidDataException? failure = null;
                try { read(); }
                catch (InvalidDataException error) { failure = error; }
                AssertThat(failure?.InnerException is JsonException).OverrideFailureMessage("Malformed planting input lost its JSON failure before reaching the build caller.").IsTrue();
            }
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void ChangingARowOffsetInThePlantingInputMovesTheBenches()
    {
        // A bench keeps RowClear off the planted rows, so moving a row onto an avenue bench forces that bench off its slot (or off
        // the walk altogether); the other roads' benches stay where they were.
        var world = World.Value;
        var before = RealLayout.Value;
        var avenue = world.Network.Roads.Where(l => StreetNetwork.RoadName(l) == Avenue.Name).ToList();
        var restKey = $"rest:{Avenue.Key}:";
        var bench = before.Items.First(i => i.Kind == FurnitureKind.Bench && i.Key.StartsWith(restKey, StringComparison.Ordinal));
        var offset = avenue.Min(l => (float)PlanLines.Project(l.Line.Points, bench.Anchor).Distance);
        var moved = Build(world, null, Inputs(Edit(root => root["roads"]![0]!["rows"] = new JsonArray(new JsonObject { ["offset"] = offset }))));
        var after = moved.Items.FirstOrDefault(i => i.Key == bench.Key);
        AssertThat(after is null || Vector2.Distance(after.Anchor, bench.Anchor) > 0.5f).OverrideFailureMessage($"{bench.Key} stands where it did, on a tree row").IsTrue();
        // And the row it now keeps clear of is the new one: every bench of the avenue is off the offset line.
        foreach (var item in moved.Items.Where(i => i.Kind is FurnitureKind.Bench or FurnitureKind.Bin && i.Key.StartsWith(restKey, StringComparison.Ordinal)))
            AssertThat(MathF.Abs(avenue.Min(l => (float)PlanLines.Project(l.Line.Points, item.Anchor).Distance) - offset) >= FurnitureStyle.RowClear).OverrideFailureMessage($"{item.Key} stands on the changed row").IsTrue();
        // Signposts, fountains and the service things (13c2c) come last and keep off the rows and what stands before them: they may move too.
        foreach (var item in before.Items.Where(i => !i.Key.StartsWith(restKey, StringComparison.Ordinal) && !i.Key.StartsWith($"lamp:{Avenue.Key}:", StringComparison.Ordinal)
            && i.Kind is not (FurnitureKind.Signpost or FurnitureKind.DrinkingFountain or FurnitureKind.VendingMachine or FurnitureKind.DeliveryLocker
                or FurnitureKind.BicycleBay or FurnitureKind.ParkedBicycle or FurnitureKind.Barrier)))
            AssertThat(moved.Items.Any(m => m.Key == item.Key && Vector2.Distance(m.Anchor, item.Anchor) < TestTolerance.Position)).OverrideFailureMessage($"{item.Key} moved with the avenue's rows").IsTrue();
    }
}
