using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Campus.Contracts;
using Campus.Tests;
using Campus.Gen.Tests.Support;
using Campus.Contracts.Maps;
using Campus.Contracts.Vegetation;
using System.Numerics;
using GdUnit4;

namespace Campus.Gen.Vegetation.Tests;

using static GdUnit4.Assertions;

/// <summary>
/// The contract-32 read entry: the species table from the global inputs, the planting rules from the map's
/// vegetation directory, the canopy channels painted as an exclusive ground-mask contributor.
/// </summary>
[TestSuite]
public sealed class VegetationInputsTests
{
    [TestCase, TestCategory(TestReason.Contract)]
    public void InputsComeOnlyFromTheBoundary()
    {
        // No species table in the global inputs is an error naming the key; no planting file means a map without trees.
        var empty = new BuildContext { CodeHash = "fixture", WindowNumbering = new(new MapId("fixture"), new string('0', 64), []), World = new FakeWorld(), Map = null!, WorldSeed = 1u, Catalog = Catalog };
        var caughtExpectedException = false;
        try { VegetationInputs.Layout(empty); }
        catch (KeyNotFoundException) { caughtExpectedException = true; }
        AssertThat(caughtExpectedException).OverrideFailureMessage($"Expected {nameof(KeyNotFoundException)}.").IsTrue();
        var bare = Context(withPlanting: false);
        var (layout, rules) = VegetationInputs.Layout(bare);
        if (layout.Trees.Count != 0 || rules is not null || new VegetationModule().Tiles(bare).Any())
            throw new InvalidOperationException("a map without planting rules got trees");
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void TheModuleOwnsTheCanopyChannelsAndOverwritesThem()
    {
        var ctx = Context(withPlanting: true);
        var module = new VegetationModule();
        var tree = VegetationInputs.Layout(ctx).Layout.Trees.First(t => t.Source == PlantSource.Road);
        var cell = TileGrid.At(tree.Position);
        // Owner enumeration must be source-only even with terrain access unavailable.
        var sourceOnly = ctx.WithWorld(new FakeWorld(ctx.World) { Terrain = null! });
        AssertThat(module.Tiles(sourceOnly).Contains(cell)).IsTrue();
        if (!module.Tiles(ctx).Contains(cell) || module.Collide(ctx, cell).Count == 0)
            throw new InvalidOperationException("tiles or trunk colliders disagree with the layout");
        var grid = new GroundMaskGrid(new MapRect(new(tree.Position.X - 32f, tree.Position.Z - 32f), new(tree.Position.X + 32f, tree.Position.Z + 32f)), 64, 64, 1f);
        var mask1 = Enumerable.Repeat((byte)77, grid.ByteCount).ToArray();
        var mask2 = Enumerable.Repeat((byte)77, grid.ByteCount).ToArray();
        module.Paint(ctx, grid, mask1, mask2);
        var centre = (32 * 64 + 32) * 4;
        if (mask1[centre] < 200 || mask2[centre + 2] < 200) throw new InvalidOperationException("no leaves or shelter under an avenue ginkgo");
        for (var i = 0; i < grid.ByteCount; i += 4)
        {
            // Only Mask1.R/G/A and Mask2.B are ours; everything else stays as the caller left it.
            if (mask1[i + 2] != 77 || mask2[i] != 77 || mask2[i + 1] != 77 || mask2[i + 3] != 77)
                throw new InvalidOperationException("painted outside the declared channels");
            if (mask1[i + 3] > mask1[i + 1])
                throw new InvalidOperationException("maple potential is not a subset of other leaves");
        }
        if (mask1.Where((_, i) => i % 4 == 1).All(v => v == 77))
            throw new InvalidOperationException("the G channel was not overwritten");
        if (mask1.Where((_, i) => i % 4 == 3).All(v => v == 77))
            throw new InvalidOperationException("the A channel was not overwritten");
        if (module.Channels != (GroundMaskChannels.GinkgoLeaves | GroundMaskChannels.OtherLeaves | GroundMaskChannels.CanopyShelter | GroundMaskChannels.MapleLeaves))
            throw new InvalidOperationException("channel claim");
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void GrassTilesPartitionJitteredPlacementAcrossTheOwnerBoundary()
    {
        var polygon = new PolygonRings([new(-1.2f, 1f), new(1.2f, 1f), new(1.2f, 3f), new(-1.2f, 3f)]);
        var data = new GrassRuntimeData(new MapId("fixture"),
            [new GrassAreaData(new FeatureId("grass-test"), null, GrassCategory.Short, [polygon], null)]);
        var tiles = GrassField.Tiles(data);
        AssertThat(tiles.SequenceEqual(new[] { new TileKey(-1, 0), new TileKey(0, 0) })).IsTrue();
        var all = new List<GrassClump>();
        foreach (var tile in tiles)
        {
            var grass = GrassField.Build(data, new FakeWorld(), new PlantingLayout([]), 77, tile);
            AssertThat(grass.Count > 0 && grass.All(c => TileGrid.At(c.Position) == tile)).IsTrue();
            var repeat = GrassField.Build(data, new FakeWorld(), new PlantingLayout([]), 77, tile);
            AssertThat(grass.SequenceEqual(repeat)).IsTrue();
            all.AddRange(grass);
        }
        AssertThat(all.Select(c => c.Position).Distinct().Count() == all.Count).IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void GrassDirectoryKeepsBoundaryCellsMemberOrderAndLocalCapacity()
    {
        var size = GrassPoolIndex.CellSize;
        Box3 At(float x, float z) => new(new(x, 0, z), new(x, 1, z));
        Box3[] bounds = [At(size, 0), At(-size, 0), At(size + 1, 0), At(0, 0), At(size * 12, 0), At(-1, 0)];
        var index = GrassPoolIndexBuilder.Build(bounds);
        GrassPoolCell[] cells =
        [
            new(1, 0, 0, 2, new(bounds[0].Min, bounds[2].Max)),
            new(-1, 0, 2, 2, new(bounds[1].Min, bounds[5].Max)),
            new(0, 0, 4, 1, bounds[3]),
            new(12, 0, 5, 1, bounds[4])
        ];
        AssertThat(index.Cells.SequenceEqual(cells) && index.Members.Span.SequenceEqual(new[] { 0, 2, 1, 5, 3, 4 })).IsTrue();
        AssertThat(index.Capacity == bounds.Length - 1).OverrideFailureMessage("The isolated far cell must not inflate the near pool.").IsTrue();
        var dense = GrassPoolIndexBuilder.Build(Enumerable.Repeat(bounds[0], 6).ToArray());
        AssertThat(dense.Capacity == dense.Members.Length && dense.Cells.Single().Count == dense.Members.Length).IsTrue();
        var empty = GrassPoolIndexBuilder.Build([]);
        AssertThat(empty.Capacity == 1 && empty.Cells.Count == 0 && empty.Members.Length == 0).IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void BothGrassStylesBakeEveryRecordIntoTheirOwnDirectory()
    {
        var clumps = Enum.GetValues<GrassStyle>().SelectMany(style => new[]
        {
            new GrassClump(new(-4, 0, -4), 0, 1, .1f, 2, .3f, style),
            new GrassClump(new(4, 1, 4), .5f, 1, .4f, 3, .6f, style)
        }).ToArray();
        var mothers = GrassClumps.Mothers(clumps);
        AssertThat(mothers.Select(m => m.Key).SequenceEqual(new[] { "grass/short", "grass/tall" })).IsTrue();
        foreach (var mother in mothers)
        {
            var index = mother.GrassIndex!;
            AssertThat(mother.Records.Length == mother.Bounds.Count * InstanceStreamLayout.FloatsPerInstance &&
                mother.Bounds.Count == 2 && index.Capacity == mother.Bounds.Count &&
                index.Members.Span.SequenceEqual(new[] { 0, 1 }) && index.Cells.Sum(c => c.Count) == mother.Bounds.Count).IsTrue();
            for (var i = 0; i < mother.Bounds.Count; i++)
            {
                var record = mother.Records.Span.Slice(i * InstanceStreamLayout.FloatsPerInstance, InstanceStreamLayout.FloatsPerInstance);
                var x = record[3];
                var input = clumps.Single(c => c.Style == (mother.Key.EndsWith("short", StringComparison.Ordinal) ? GrassStyle.Short : GrassStyle.Tall) && c.Position.X == x);
                AssertThat(record[7] == input.Position.Y && record[11] == input.Position.Z && record[12] == input.Seed &&
                    record[13] == input.Tone && record[14] == input.WindPos).IsTrue();
            }
        }
    }

    private static BuildContext Context(bool withPlanting)
    {
        var root = RepositoryFixture.Root;
        var global = BuildInputSnapshot.Freeze(new Dictionary<string, ReadOnlyMemory<byte>>(StringComparer.Ordinal)
        {
            [VegetationInputs.SpeciesKey] = File.ReadAllBytes(Path.Combine(root, "game", "data", "vegetation", "species.json"))
        }, "fixture");
        var map = new Dictionary<string, ReadOnlyMemory<byte>>(StringComparer.Ordinal);
        if (withPlanting)
            map[VegetationInputs.PlantingKey] = RealMapFixture.ReadFiles()[VegetationInputs.PlantingKey];
        return new BuildContext { CodeHash = "fixture", WindowNumbering = new(new MapId("fixture"), new string('0', 64), []), World = withPlanting ? PavementTests.World() : new FakeWorld(), Map = null!, WorldSeed = 1u, Catalog = Catalog, GlobalInputs = global, SourceInputs = BuildInputSnapshot.Freeze(map, "fixture").Entries };
    }

    private static PaletteCatalog Catalog => MotherModelTests.Palette;

}
