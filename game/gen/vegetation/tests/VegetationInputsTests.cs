using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Gen.World;
using GdUnit4;

namespace Campus.Gen.Vegetation.Tests;

/// <summary>
/// The contract-32 read entry: the species table from the global inputs, the planting rules from the map's
/// vegetation directory, the canopy channels painted as an exclusive ground-mask contributor.
/// </summary>
[TestSuite]
public sealed class VegetationInputsTests
{
    private static readonly Lazy<(string Root, IWorldModel World)> Data = new(() =>
    {
        var root = FindRepository();
        var data = Path.Combine(root, "game", "data", "maps", "qingshuihe");
        var files = new[] { "campus.json", "network.json", "interiors.json", "map.json" }
            .ToDictionary(name => name, name => File.ReadAllText(Path.Combine(data, name)), StringComparer.Ordinal);
        return (root, WorldModelLoader.Load(new("qingshuihe"), files).World);
    });

    [TestCase]
    public void InputsComeOnlyFromTheBoundary()
    {
        // No species table in the global inputs is an error naming the key; no planting file means a map without trees.
        var empty = new BuildContext { World = Data.Value.World, Map = null!, WorldSeed = 1u, Catalog = Catalog };
        Expect<KeyNotFoundException>(() => VegetationInputs.Layout(empty));
        var bare = Context(withPlanting: false);
        var (layout, rules) = VegetationInputs.Layout(bare);
        if (layout.Trees.Count != 0 || rules is not null || new VegetationModule().Tiles(bare).Any())
            throw new InvalidOperationException("a map without planting rules got trees");
    }

    [TestCase]
    public void TheModuleOwnsTheCanopyChannelsAndOverwritesThem()
    {
        var ctx = Context(withPlanting: true);
        var module = new VegetationModule();
        var tree = VegetationInputs.Layout(ctx).Layout.Trees.First(t => t.Source == PlantSource.Road);
        var cell = new TileKey((int)MathF.Floor(tree.Position.X / 64f), (int)MathF.Floor(tree.Position.Z / 64f));
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
            // Only Mask1.R/G and Mask2.B are ours; everything else stays as the caller left it.
            if (mask1[i + 2] != 77 || mask1[i + 3] != 77 || mask2[i] != 77 || mask2[i + 1] != 77 || mask2[i + 3] != 77)
                throw new InvalidOperationException("painted outside the declared channels");
        }
        if (mask1.Where((_, i) => i % 4 == 1).All(v => v == 77))
            throw new InvalidOperationException("the G channel was not overwritten");
        if (module.Channels != (GroundMaskChannels.GinkgoLeaves | GroundMaskChannels.OtherLeaves | GroundMaskChannels.CanopyShelter))
            throw new InvalidOperationException("channel claim");
    }

    private static BuildContext Context(bool withPlanting)
    {
        var root = Data.Value.Root;
        var global = BuildInputSnapshot.Freeze(new Dictionary<string, ReadOnlyMemory<byte>>(StringComparer.Ordinal)
        {
            [VegetationInputs.SpeciesKey] = File.ReadAllBytes(Path.Combine(root, "game", "data", "vegetation", "species.json"))
        });
        var map = new Dictionary<string, ReadOnlyMemory<byte>>(StringComparer.Ordinal);
        if (withPlanting)
            map[VegetationInputs.PlantingKey] = File.ReadAllBytes(Path.Combine(root, "game", "data", "maps", "qingshuihe", "vegetation", "planting.json"));
        return new BuildContext { World = Data.Value.World, Map = null!, WorldSeed = 1u, Catalog = Catalog, GlobalInputs = global, SourceInputs = BuildInputSnapshot.Freeze(map).Entries };
    }

    private static PaletteCatalog Catalog => MotherModelTests.Palette;

    private static void Expect<TException>(Action action) where TException : Exception
    {
        try { action(); }
        catch (TException) { return; }
        throw new InvalidOperationException($"Expected {typeof(TException).Name}.");
    }

    private static string FindRepository()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "ownership.json"))) return current.FullName;
            current = current.Parent;
        }
        throw new InvalidOperationException("repository root not found");
    }
}
