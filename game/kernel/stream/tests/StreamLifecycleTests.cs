using Campus.Gen.Tests.Support;
using System.Reflection;
using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Maps;
using Campus.Kernel.Bake;
using Campus.Kernel.Lod;
using Campus.Kernel.Stream;
using GdUnit4;
using Godot;

namespace Campus.Tests;

[TestSuite]
public sealed class StreamLifecycleTests
{
    [TestCase]
    [TestCategory(TestReason.Regression)]
    [TestCategory("NativeRender")]
    [RequireGodotRuntime]
    public void ResidentBackdropAppearsEvenWhenReadyChildrenSkipTheRootFallback()
    {
        var bounds = new Box3(System.Numerics.Vector3.Zero, System.Numerics.Vector3.One);
        var tree = new LodTree(bounds, new StreamSettings());
        var visible = new HashSet<StreamTile>();
        tree.ResolveVisible(new HashSet<StreamTile> { StreamTile.Root }, _ => true, visible);
        if (visible.Contains(StreamTile.Root)) throw new InvalidOperationException("The fixture must skip the ready root fallback.");

        var source = new GeometryBundle(new(new ToonSpec(new("wall.lit"), new("wall.shade"))),
            [0, 0, 0, 1, 0, 0, 0, 0, 1], [0, 1, 0, 0, 1, 0, 0, 1, 0], [0, 1, 2]);
        var palette = Campus.Contracts.Tests.PaletteFixture.Catalog;
        var key = new BakeKey(new("fixture"), "fixture.resident", "L4/0/0");
        var bundle = new TileBundle(key, StreamTile.Root.Tile, LodLevel.L4, bounds,
            [BakeSurfacePacker.Pack(source with { Shader = ShaderId.DioramaFar }, bounds, palette),
             BakeSurfacePacker.Pack(source, bounds, palette)]);
        var scenario = RenderingServer.ScenarioCreate();
        var material = RenderingServer.MaterialCreate();
        try
        {
            using var prepared = PreparedTile.Create(bundle);
            for (var i = 0; i < prepared.Draws.Count; i++) prepared.MountOne(i, scenario, material);
            var resident = prepared.Draws.Count(draw => draw.Resident);
            if (prepared.VisibleInstanceCount != resident || resident == 0 || resident == prepared.Draws.Count)
                throw new InvalidOperationException("Resident background stayed hidden until a root selection, or ordinary fallback geometry appeared early.");
            prepared.SetVisible(false, 0);
            if (prepared.VisibleInstanceCount != resident)
                throw new InvalidOperationException("Refining the root hid its resident background.");
        }
        finally { RenderingServer.FreeRid(material); RenderingServer.FreeRid(scenario); }
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    [RequireGodotRuntime]
    public void ShutdownDrainsTheCatalogAndLateResultsDoNotPublish()
    {
        foreach (var fail in new[] { false, true })
        {
            var director = new StreamDirector();
            try
            {
                var context = Context();
                var work = new TaskCompletionSource<ModuleTileCatalog>();
                BindUnrenderedCatalog(director, context, work.Task);
                director.OnMapUnloading(context.Descriptor.Id);
                if (director.IsUnloaded) throw new InvalidOperationException("Unload did not retain its real pending CPU task.");
                if (fail) work.SetException(new InvalidOperationException("Retired catalog failed."));
                else work.SetResult(ModuleTileCatalog.Build(Build(context), []));
                director._Process(0);
                if (!director.IsUnloaded || director.ResidentReady || director.Error is not null || Field(director, "_catalog") is not null)
                    throw new InvalidOperationException("A retired catalog became active map data or an active-map error.");
                director._ExitTree();
            }
            finally { director.Free(); }
        }
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    [RequireGodotRuntime]
    public async Task ExitDropsPendingCatalogSuccessAndObservesLateFaultWithoutNativeWork()
    {
        foreach (var fail in new[] { false, true })
        {
            var director = new StreamDirector();
            try
            {
                var context = Context();
                var work = new TaskCompletionSource<ModuleTileCatalog>();
                BindUnrenderedCatalog(director, context, work.Task);
                director._ExitTree();
                if (Field(director, "_catalogTask") is not null) throw new InvalidOperationException("Exited node retained catalog publication ownership.");
                if (fail) work.SetException(new InvalidOperationException("Exited catalog failed."));
                else work.SetResult(ModuleTileCatalog.Build(Build(context), []));
                await Task.Yield();
                director._Process(0);
                if (director.ResidentReady || Field(director, "_catalog") is not null)
                    throw new InvalidOperationException("A late result published after the real stream node exited.");
            }
            finally { director.Free(); }
        }
    }

    private static void BindUnrenderedCatalog(StreamDirector director, IMapContext context, Task<ModuleTileCatalog> task)
    {
        // Drive the actual node's CPU lifecycle without constructing a renderer, scene or parallel resident.
        Set(director, "_context", context); Set(director, "_mapGeneration", 1L); Set(director, "_catalogGeneration", 1L);
        Set(director, "_catalogTask", task); Set(director, "_mount", new StreamMountQueue(() => 0));
        Set(director, "_visibility", new TileVisibility(.25f)); Set(director, "_materials", new StreamMaterials(null!, null!));
    }
    private static object? Field(StreamDirector target, string name) =>
        typeof(StreamDirector).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(target);
    private static void Set(StreamDirector target, string name, object value) =>
        typeof(StreamDirector).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(target, value);
    private static IMapContext Context() => new TestMapContext(BackdropInputsTests.Descriptor(), new FakeWorld { Boundary = new([]) },
        BuildInputSnapshot.Freeze(new Dictionary<string, ReadOnlyMemory<byte>>()), "res://fixture/");
    private static BuildContext Build(IMapContext context) => new()
    { Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog, World = context.World, Map = context.Descriptor, WorldSeed = 1 };
}
