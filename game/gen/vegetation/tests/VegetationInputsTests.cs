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
        var empty = new BuildContext { World = new FakeWorld(), Map = null!, WorldSeed = 1u, Catalog = Catalog };
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
        return new BuildContext { World = withPlanting ? PavementTests.World() : new FakeWorld(), Map = null!, WorldSeed = 1u, Catalog = Catalog, GlobalInputs = global, SourceInputs = BuildInputSnapshot.Freeze(map, "fixture").Entries };
    }

    private static PaletteCatalog Catalog => MotherModelTests.Palette;

}
