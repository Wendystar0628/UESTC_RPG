using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using Campus.Contracts;
using Campus.Tests;
using Campus.Contracts.Vegetation;
using Campus.Contracts.Maps;
using Campus.Gen.World;
using GdUnit4;

namespace Campus.Gen.Vegetation.Tests;

using static GdUnit4.Assertions;

/// <summary>
/// The trees reach the kernel as static instance streams (0n): Build gives every planted tree exactly once per level band,
/// over the shared mother-model prototypes, and nothing else; the trace hook flattens to Build and Collide.
/// </summary>
[TestSuite]
public sealed class VegetationBuildTests
{
    private static readonly Lazy<(string Root, BuildContext Context)> Data = new(Load);

    private static (string, BuildContext) Load()
    {
        var root = RepositoryFixture.Root;
        var world = PavementTests.World(false);
        var global = BuildInputSnapshot.Freeze(new Dictionary<string, ReadOnlyMemory<byte>>(StringComparer.Ordinal)
        {
            [VegetationInputs.SpeciesKey] = File.ReadAllBytes(Path.Combine(root, "game", "data", "vegetation", "species.json"))
        });
        var map = BuildInputSnapshot.Freeze(new Dictionary<string, ReadOnlyMemory<byte>>(StringComparer.Ordinal)
        {
            [VegetationInputs.PlantingKey] = RealMapFixture.ReadFiles()[VegetationInputs.PlantingKey]
        });
        var ctx = new BuildContext
        {
            World = world,
            Map = null!,
            WorldSeed = 1u,
            GlobalInputs = global,
            SourceInputs = map.Entries,
            Catalog = Palette(root)
        };
        return (root, ctx);
    }

    [TestCase]
    public void EveryTreeIsInstancedOnceAtEachBand()
    {
        var ctx = Data.Value.Context;
        var module = new VegetationModule();
        var trees = VegetationInputs.Layout(ctx).Layout.Trees;
        var tiles = module.Tiles(ctx).ToArray();
        var near = tiles.Select(t => module.Build(ctx, t, LodLevel.L0)).ToArray();
        AssertThat(near.All(o => o.Instances.Count > 0)).OverrideFailureMessage("an occupied cell built no instances").IsTrue();
        AssertThat(near.All(o => o.Meshes.Count == 0 && o.Parts.Count == 0)).OverrideFailureMessage("vegetation must hand over instance streams only").IsTrue();
        Records(near, trees, VegPool.L0);

        // Far tiles (128 m and 256 m): the same trees on the far stand-in.
        foreach (var lod in new[] { LodLevel.L1, LodLevel.L2 })
        {
            var far = trees.Select(t => TileGrid.At(t.Position, lod))
                .Distinct().Select(t => module.Build(ctx, t, lod)).ToArray();
            Records(far, trees, VegPool.Far);
        }
        AssertThat(module.Build(ctx, tiles[0], LodLevel.L3).Instances.Count == 0 && module.Build(ctx, new(0, 0), LodLevel.L4).Instances.Count == 0)
            .OverrideFailureMessage("crown clouds are not built yet").IsTrue();
        AssertThat(module.Build(ctx, new(100000, 100000), LodLevel.L0) == ContentOutput.Empty).OverrideFailureMessage("an empty cell must stay empty").IsTrue();
    }

    [TestCase]
    public void StreamsShareOnePrototypePerMotherModelAndLevel()
    {
        var ctx = Data.Value.Context;
        var module = new VegetationModule();
        var streams = module.Tiles(ctx).SelectMany(t => module.Build(ctx, t, LodLevel.L0).Instances).ToArray();
        foreach (var group in streams.GroupBy(s => s.Prototype.Id))
            AssertThat(group.Select(s => s.Prototype).Distinct().Count() == 1).OverrideFailureMessage($"{group.Key}: prototype rebuilt per tile").IsTrue();
        var ids = streams.Select(s => s.Id).ToArray();
        AssertThat(ids.Distinct(StringComparer.Ordinal).Count() == ids.Length).OverrideFailureMessage("duplicate stream IDs").IsTrue();
        foreach (var s in streams)
        {
            AssertThat(s.Prototype.Surfaces.Count is 1 or 2).OverrideFailureMessage($"{s.Id}: surfaces").IsTrue();
            var palette = ctx.Catalog!;
            foreach (var surface in s.Prototype.Surfaces) palette.Validate(surface.Material.Spec);
        }
        var far = streams.Select(s => s.Prototype).Distinct().First();
        var parts = far.Id.Split('/');
        var info = VegetationInputs.Species(ctx).Get(parts[1]);
        var variant = int.Parse(parts[2], CultureInfo.InvariantCulture);
        var l1 = VegetationPrototypes.Get(info, variant, VegPool.L1, ctx.Catalog!);
        var stand = VegetationPrototypes.Get(info, variant, VegPool.Far, ctx.Catalog!);
        var standTriangles = VegetationPrototypes.Triangles(stand);
        Console.WriteLine($"{far.Id}: far stand-in {standTriangles} triangles, L1 {VegetationPrototypes.Triangles(l1)}");
        AssertThat(stand.Id != l1.Id && stand.Surfaces.Count == 2 && stand.Surfaces[0].Material.Spec.Kind == ToonKind.VegTrunk
            && stand.Surfaces[1].Material.Spec.Kind == ToonKind.VegCard && standTriangles <= 200)
            .OverrideFailureMessage("the mainline far stand-in is a trunk and a mass crown within 200 triangles (ENG-PERF L2: 150–300)").IsTrue();
        AssertThat(VegetationPrototypes.Get(info, variant, VegPool.Shadow, ctx.Catalog!).Surfaces.Count == 1).OverrideFailureMessage("one shadow proxy surface").IsTrue();
    }

    [TestCase]
    public void TraceSourcesFlattenToBuildAndCollide()
    {
        var ctx = Data.Value.Context;
        var module = new VegetationModule();
        var sources = module.TraceSources(ctx, LodLevel.L0).ToArray();
        var trees = VegetationInputs.Layout(ctx).Layout.Trees;
        AssertThat(sources.Select(t => t.SourceId).Distinct(StringComparer.Ordinal).Count() == sources.Length)
            .OverrideFailureMessage("all trees have stable unique source IDs").IsTrue();
        foreach (var tile in module.Tiles(ctx).Take(40))
        {
            var ids = trees.Where(t => TileGrid.At(t.Position) == tile)
                .Select(t => "object/vegetation/" + t.Key).ToHashSet(StringComparer.Ordinal);
            var traces = sources.Where(t => ids.Contains(t.SourceId)).ToArray();
            var built = Flatten(module.Build(ctx, tile, LodLevel.L0).Instances);
            var traced = Flatten(traces.SelectMany(t => t.Output.Instances));
            AssertThat(built.SequenceEqual(traced)).OverrideFailureMessage($"{tile}: traced records differ from Build").IsTrue();
            var colliders = module.Collide(ctx, tile).Select(c => c.ToString()).Order(StringComparer.Ordinal);
            var tracedColliders = traces.SelectMany(t => t.Colliders).Select(c => c.ToString()).Order(StringComparer.Ordinal);
            AssertThat(colliders.SequenceEqual(tracedColliders)).OverrideFailureMessage($"{tile}: traced colliders differ from Collide").IsTrue();
            AssertThat(traces.Select(t => t.SourceId).Distinct(StringComparer.Ordinal).Count() == traces.Length).OverrideFailureMessage($"{tile}: duplicate source IDs").IsTrue();
        }
    }

    [TestCase]
    public void TheHeaviestTilesStayWithinTheDeclaredBudget()
    {
        var ctx = Data.Value.Context;
        var module = new VegetationModule();
        var trees = VegetationInputs.Layout(ctx).Layout.Trees;
        int Weigh(ContentOutput o) => o.Instances.Sum(s => s.Count * VegetationPrototypes.Triangles(s.Prototype));
        var heaviest = module.Tiles(ctx).Select(t => module.Build(ctx, t, LodLevel.L0)).ToArray();
        var l0 = heaviest.Max(Weigh);
        var draws = heaviest.Max(o => o.Instances.Sum(s => s.Prototype.Surfaces.Count));
        var l1 = trees.Select(t => TileGrid.At(t.Position, LodLevel.L1))
            .Distinct().Max(t => Weigh(module.Build(ctx, t, LodLevel.L1)));
        Console.WriteLine($"vegetation budget: L0 cell {l0}, L1 tile {l1}, draws {draws}; trees {trees.Count}");
        AssertThat(l0 <= module.Budget.TrianglesL0 && l1 <= module.Budget.TrianglesL1 && draws <= module.Budget.DrawCalls)
            .OverrideFailureMessage($"L0 {l0}, L1 {l1}, draws {draws} over {module.Budget}").IsTrue();
    }

    [TestCase]
    public void OnlyTheKernelMakesEngineInstances()
    {
        // ENG-ARCH (G4 / arch rules): WP-23 code hands geometry over; it never creates engine instances itself.
        var root = Data.Value.Root;
        string[] banned = ["MultiMeshInstance3D", "MeshInstance3D", "RenderingServer.InstanceCreate", "RenderingServer.Multimesh", "MeshCreate"];
        foreach (var dir in new[] { Path.Combine(root, "game", "gen", "vegetation"), Path.Combine(root, "game", "systems", "vegetation") })
            foreach (var file in Directory.EnumerateFiles(dir, "*.cs", SearchOption.AllDirectories)
                         .Where(f => !f.Contains(Path.DirectorySeparatorChar + "tests" + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                                     && !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.Ordinal)))
            {
                var text = File.ReadAllText(file);
                foreach (var word in banned)
                    AssertThat(!text.Contains(word, StringComparison.Ordinal)).OverrideFailureMessage($"{Path.GetFileName(file)} uses {word}").IsTrue();
            }
    }

    /// <summary>Every record of the streams equals its tree's transform and custom, each tree exactly once, on the pool's prototype.</summary>
    private static void Records(IEnumerable<ContentOutput> outputs, IReadOnlyList<PlantedTree> trees, VegPool pool)
    {
        var expected = new Dictionary<string, List<float[]>>(StringComparer.Ordinal);
        foreach (var tree in trees)
        {
            var record = new float[InstanceStreamLayout.FloatsPerInstance];
            VegetationPrototypes.Write(record, tree);
            var key = VegetationPrototypes.Id(tree.Species, tree.Variant, pool);
            if (!expected.TryGetValue(key, out var list)) expected.Add(key, list = []);
            list.Add(record);
        }
        var count = 0;
        foreach (var stream in outputs.SelectMany(o => o.Instances))
        {
            AssertThat(expected.TryGetValue(stream.Prototype.Id, out var list)).OverrideFailureMessage($"{stream.Id}: unknown prototype {stream.Prototype.Id}").IsTrue();
            for (var i = 0; i < stream.Count; i++)
            {
                var record = stream.Data.Span.Slice(i * 16, 16).ToArray();
                var at = list!.FindIndex(r => r.AsSpan().SequenceEqual(record));
                AssertThat(at >= 0).OverrideFailureMessage($"{stream.Id}: record {i} matches no tree").IsTrue();
                list.RemoveAt(at);
                count++;
                var bounds = stream.Bounds;
                AssertThat(record[3] >= bounds.Min.X && record[3] <= bounds.Max.X && record[11] >= bounds.Min.Z && record[11] <= bounds.Max.Z)
                    .OverrideFailureMessage($"{stream.Id}: bounds miss a trunk").IsTrue();
            }
        }
        AssertThat(count == trees.Count && expected.Values.All(l => l.Count == 0)).OverrideFailureMessage($"{pool}: {count} instances for {trees.Count} trees").IsTrue();
    }

    private static List<string> Flatten(IEnumerable<GeometryInstanceStream> streams) => streams
        .SelectMany(s => Enumerable.Range(0, s.Count).Select(i => s.Prototype.Id + ":" + string.Join(',',
            s.Data.Span.Slice(i * 16, 16).ToArray().Select(v => BitConverter.SingleToInt32Bits(v).ToString("x8", CultureInfo.InvariantCulture)))))
        .Order(StringComparer.Ordinal).ToList();

    private static PaletteCatalog Palette(string root)
    {
        var entries = new List<PaletteEntry>();
        var palette = Path.Combine(root, "game", "style", "palette");
        var files = new[] { Path.Combine(palette, "core.json") }
            .Concat(Directory.GetFiles(Path.Combine(palette, "ext"), "WP-*.json").Order(StringComparer.Ordinal));
        foreach (var file in files)
            entries.AddRange(JsonSerializer.Deserialize<PaletteRegistryData>(File.ReadAllBytes(file), ContractJson.Options)!.Entries);
        return new PaletteCatalog(new PaletteRegistry(entries), []);
    }

}
