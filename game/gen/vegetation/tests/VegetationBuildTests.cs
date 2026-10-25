using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Campus.Contracts;
using Campus.Contracts.Bake;
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

    private static readonly VegetationModule Module = new();

    /// <summary>Every occupied cell built once at L0, in tile order; the cases below only read these.</summary>
    private static readonly Lazy<(TileKey Tile, ContentOutput Output)[]> Near = new(() =>
        [.. Module.Tiles(Data.Value.Context).Select(tile => (tile, Module.Build(Data.Value.Context, tile, LodLevel.L0)))]);

    /// <summary>The far cells that hold a tree, built once per far level.</summary>
    private static readonly Dictionary<LodLevel, Lazy<ContentOutput[]>> Far = new[] { LodLevel.L1, LodLevel.L2, LodLevel.L3, LodLevel.L4 }.ToDictionary(lod => lod, lod => new Lazy<ContentOutput[]>(() =>
    {
        var ctx = Data.Value.Context;
        return [.. VegetationInputs.Layout(ctx).Layout.Trees.Select(t => TileGrid.At(t.Position, lod)).Distinct().Select(tile => Module.Build(ctx, tile, lod))];
    }));

    private static (string, BuildContext) Load()
    {
        var root = RepositoryFixture.Root;
        var world = VegetationTestSupport.World();
        var global = BuildInputSnapshot.Freeze(new Dictionary<string, ReadOnlyMemory<byte>>(StringComparer.Ordinal)
        {
            [VegetationInputs.SpeciesKey] = File.ReadAllBytes(Path.Combine(root, "game", "data", "vegetation", "species.json"))
        }, "fixture");
        var map = BuildInputSnapshot.Freeze(new Dictionary<string, ReadOnlyMemory<byte>>(StringComparer.Ordinal)
        {
            [VegetationInputs.PlantingKey] = RealMapFixture.ReadFiles()[VegetationInputs.PlantingKey]
        }, "fixture");
        var ctx = new BuildContext
        {
            CodeHash = "fixture", WindowNumbering = new((RealMapFixture.Entry().Descriptor).Id, new string('0', 64), []),
            World = world,
            Map = RealMapFixture.Entry().Descriptor,
            WorldSeed = 1u,
            GlobalInputs = global,
            SourceInputs = map.Entries,
            Catalog = VegetationTestSupport.Palette
        };
        return (root, ctx);
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void EveryTreeIsInstancedOnceAtEachBand()
    {
        var ctx = Data.Value.Context;
        var module = Module;
        var trees = VegetationInputs.Layout(ctx).Layout.Trees;
        var near = Near.Value.Select(n => n.Output).Where(o => o.Instances.Count > 0).ToArray();
        AssertThat(near.All(o => o.Instances.Count > 0)).OverrideFailureMessage("an occupied cell built no instances").IsTrue();
        AssertThat(near.All(o => o.Meshes.Count == 0 && o.Parts.Count == 0)).OverrideFailureMessage("vegetation must hand over instance streams only").IsTrue();
        Records(near, trees, VegPool.Far);

        // Every geometry level, including whole-map coverage, covers the same far trees.
        foreach (var lod in new[] { LodLevel.L1, LodLevel.L2, LodLevel.L3, LodLevel.L4 })
            Records(Far[lod].Value, trees, VegPool.Far);
        AssertThat(module.Build(ctx, new(100000, 100000), LodLevel.L0) == ContentOutput.Empty).OverrideFailureMessage("an empty cell must stay empty").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void StreamsShareOnePrototypePerMotherModelAndLevel()
    {
        var ctx = Data.Value.Context;
        var streams = Near.Value.SelectMany(n => n.Output.Instances).ToArray();
        var prepared = Module.Tiles(ctx).SelectMany(tile => Module.PrepareTrees(ctx, tile)).SelectMany(m => m.Prototypes)
            .Concat(streams.Select(stream => stream.Prototype)).DistinctBy(p => p.Id).ToDictionary(p => p.Id, StringComparer.Ordinal);
        var species = VegetationInputs.Species(ctx);
        foreach (var prototype in prepared.Values)
        {
            var (id, _, pool) = VegetationPrototypes.Parse(prototype.Id);
            var tree = species.Get(id);
            var kinds = new List<ToonKind>();
            if (pool == VegPool.Shadow || TreeForms.Get(tree.Form).Structure != TreeStructure.StemlessClusters) kinds.Add(ToonKind.VegTrunk);
            if (pool != VegPool.Shadow)
            {
                kinds.Add(ToonKind.VegCard);
                if (MotherModels.HasTwigMass(tree)) kinds.Add(ToonKind.VegTwigMass);
            }
            AssertThat(prototype.Surfaces.Select(surface => surface.Material.Spec.Kind).SequenceEqual(kinds))
                .OverrideFailureMessage($"{prototype.Id}: surfaces").IsTrue();
        }
        foreach (var group in streams.GroupBy(s => s.Prototype.Id))
        {
            AssertThat(group.Select(s => s.Prototype).Distinct().Count() == 1).OverrideFailureMessage($"{group.Key}: prototype rebuilt per tile").IsTrue();
            AssertThat(ReferenceEquals(group.First().Prototype, prepared[group.Key])).OverrideFailureMessage($"{group.Key}: runtime rebuilt the prototype").IsTrue();
        }
        var ids = streams.Select(s => s.Id).ToArray();
        AssertThat(ids.Distinct(StringComparer.Ordinal).Count() == ids.Length).OverrideFailureMessage("duplicate stream IDs").IsTrue();
        foreach (var s in streams)
        {
            var palette = ctx.Catalog!;
            foreach (var surface in s.Prototype.Surfaces) palette.Validate(surface.Material.Spec);
        }
        var far = streams.Select(s => s.Prototype).Distinct().First();
        var parts = far.Id.Split('/');
        var info = species.Get(parts[1]);
        var variant = int.Parse(parts[2], CultureInfo.InvariantCulture);
        foreach (var pool in Enum.GetValues<VegPool>())
        {
            var prototype = VegetationPrototypes.Get(info, variant, pool, ctx);
            AssertThat(ReferenceEquals(prototype, prepared[prototype.Id])).OverrideFailureMessage($"{prototype.Id}: reparsed species rebuilt the prototype").IsTrue();
        }
        var l1 = VegetationPrototypes.Get(info, variant, VegPool.L1, ctx);
        var stand = VegetationPrototypes.Get(info, variant, VegPool.Far, ctx);
        AssertThat(stand.Id != l1.Id && stand.Surfaces.Count == (MotherModels.HasTwigMass(info) ? 3 : 2) && stand.Surfaces[0].Material.Spec.Kind == ToonKind.VegTrunk
            && stand.Surfaces[1].Material.Spec.Kind == ToonKind.VegCard )
            .OverrideFailureMessage("the mainline far stand-in is a trunk and a mass crown").IsTrue();
        AssertThat(VegetationPrototypes.Get(info, variant, VegPool.Shadow, ctx).Surfaces.Count == 1).OverrideFailureMessage("one shadow proxy surface").IsTrue();
        var otherInputs = new BuildContext
        {
            CodeHash = "fixture", WindowNumbering = new((ctx.Map).Id, new string('0', 64), []),
            World = ctx.World, Map = ctx.Map, WorldSeed = ctx.WorldSeed, Catalog = ctx.Catalog,
            GlobalInputs = BuildInputSnapshot.Freeze(ctx.GlobalInputs.Entries, "fixture")
        };
        AssertThat(!ReferenceEquals(l1, VegetationPrototypes.Get(info, variant, VegPool.L1, otherInputs)))
            .OverrideFailureMessage("another frozen input snapshot reused a mother prototype").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void TraceSourcesFlattenToBuildAndCollide()
    {
        var ctx = Data.Value.Context;
        var module = Module;
        var sources = module.TraceSources(ctx, LodLevel.L0).ToArray();
        var trees = VegetationInputs.Layout(ctx).Layout.Trees;
        AssertThat(sources.Select(t => t.SourceId).Distinct(StringComparer.Ordinal).Count() == sources.Length)
            .OverrideFailureMessage("all trees have stable unique source IDs").IsTrue();
        foreach (var (tile, output) in Near.Value.Where(pair => pair.Output.Instances.Count > 0).Take(40))
        {
            var ids = trees.Where(t => TileGrid.At(t.Position) == tile)
                .Select(t => "object/vegetation/" + t.Key).ToHashSet(StringComparer.Ordinal);
            var traces = sources.Where(t => ids.Contains(t.SourceId)).ToArray();
            var built = Flatten(output.Instances);
            var traced = Flatten(traces.SelectMany(t => t.Output.Instances));
            AssertThat(built.SequenceEqual(traced)).OverrideFailureMessage($"{tile}: traced records differ from Build").IsTrue();
            var prototypes = output.Instances.ToDictionary(s => s.Prototype.Id, s => s.Prototype, StringComparer.Ordinal);
            AssertThat(traces.SelectMany(t => t.Output.Instances).All(s => ReferenceEquals(s.Prototype, prototypes[s.Prototype.Id])))
                .OverrideFailureMessage($"{tile}: trace rebuilt a prototype").IsTrue();
            var colliders = module.Collide(ctx, tile).Select(c => c.ToString()).Order(StringComparer.Ordinal);
            var tracedColliders = traces.SelectMany(t => t.Colliders).Select(c => c.ToString()).Order(StringComparer.Ordinal);
            AssertThat(colliders.SequenceEqual(tracedColliders)).OverrideFailureMessage($"{tile}: traced colliders differ from Collide").IsTrue();
            AssertThat(traces.Select(t => t.SourceId).Distinct(StringComparer.Ordinal).Count() == traces.Length).OverrideFailureMessage($"{tile}: duplicate source IDs").IsTrue();
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void TilePayloadUsesTheConsumedSourceAndExplicitNearPrototypeOrder()
    {
        var source = Data.Value.Context;
        var tile = TileGrid.At(VegetationInputs.Layout(source).Layout.Trees.First().Position);
        var producer = new VegetationBakeProducer();
        producer.Prepare(new Lazy<IReadOnlyList<PreparedContentModule>>(() =>
            [new("vegetation", Module, source, Module.Tiles(source).ToArray())]), null!);
        var key = new BakeKey(source.Map.Id, BakeModuleIds.VegetationTrees, FormattableString.Invariant($"L0/{tile.X}/{tile.Z}"));
        // Deliberately incomplete request inputs: the producer must use the original source context.
        var request = new BuildContext { CodeHash = "fixture", WindowNumbering = new((source.Map).Id, new string('0', 64), []), Map = source.Map, World = source.World, WorldSeed = source.WorldSeed + 1, Catalog = source.Catalog };
        var result = (VegetationTileContent)producer.Build(key, request, "fixture-input");
        var expected = Module.PrepareTrees(source, tile);
        AssertThat(result.Key == key && result.Tile == tile && result.Mothers.Count == expected.Count).IsTrue();
        for (var m = 0; m < result.Mothers.Count; m++)
        {
            var mother = result.Mothers[m];
            AssertThat(mother.Records.Span.SequenceEqual(expected[m].Records.Span)).IsTrue();
            AssertThat(mother.Prototypes.Select(p => VegetationPrototypes.Parse(p.Id).Pool)
                .SequenceEqual(new[] { VegPool.L0Plus, VegPool.L0, VegPool.L1, VegPool.Shadow })).IsTrue();
            for (var r = 0; r < mother.Records.Length; r += InstanceStreamLayout.FloatsPerInstance)
                AssertThat(TileGrid.At(new System.Numerics.Vector3(mother.Records.Span[r + 3], mother.Records.Span[r + 7], mother.Records.Span[r + 11])) == tile).IsTrue();
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void OnlyTheKernelMakesEngineInstances()
    {
        // ENG-ARCH (G4 / arch rules): game/gen/vegetation/ code hands geometry over; it never creates engine instances itself.
        var root = RepositoryFixture.Root;
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

}
