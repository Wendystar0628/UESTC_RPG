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
    [TestCase, TestCategory(TestReason.Regression)]
    [RequireGodotRuntime]
    public void PendingDestinationCannotQueryTerrainOrInheritAppliedSelection()
    {
        var director = new StreamDirector();
        try
        {
            var context = new TestMapContext(BackdropInputsTests.Descriptor(), new FakeWorld
                { Spatial = new FlatSpatial(forbidden: "Terrain has not been prepared.") });
            var catalog = ModuleTileCatalog.Build(new(new Dictionary<string, IReadOnlyList<TileKey>>(), []));
            BindUnrenderedCatalog(director, context, Task.FromResult(catalog));
            var interiors = new InteriorStream(null!, null!, null!, null!, _ => { }); interiors.Load(context);
            Set(director, "_catalog", catalog); Set(director, "_maps", new TestMapService()); Set(director, "_interiors", interiors);
            Set(director, "_destinationConsumed", true); Set(director, "_destinationPlanned", true); Set(director, "_selectionApplied", true);
            if (!director.DestinationReady || director.ViewReady)
                throw new InvalidOperationException("The consumed arrival inherited readiness from an unrelated unprepared view.");
            var target = new Pose(new(1, 2, 3), 0, 0);
            director.Prefetch(target, ViewMode.Fly);
            // A mount finishing for the old view must not certify this newly registered destination.
            Set(director, "_selectionApplied", true);
            if (director.Destination?.Pose != target || !director.HasPendingDestination || director.DestinationReady || director.ViewReady)
                throw new InvalidOperationException("A pending unprepared destination inherited the previous view's readiness.");
            var destination = director.Destination;
            var pins = new[] { new MapStreamDestination(target, ViewMode.Walk), new MapStreamDestination(target, ViewMode.Fly) };
            director.SetPinnedDestinations(pins);
            pins[0] = pins[1];
            if (director.PinnedDestinations.Count != pins.Length || director.PinnedDestinations[0].Mode != ViewMode.Walk ||
                !ReferenceEquals(director.Destination, destination) || !director.HasPendingDestination || director.DestinationReady || director.ViewReady)
                throw new InvalidOperationException("Pinned CPU preloads changed the active arrival or retained the caller's mutable collection.");
            director.SetPinnedDestinations(Array.Empty<MapStreamDestination>());
            if (director.PinnedDestinations.Count != 0 || !ReferenceEquals(director.Destination, destination))
                throw new InvalidOperationException("Clearing CPU preloads replaced the active destination.");
        }
        finally { director.Free(); }
    }

    [TestCase, TestCategory(TestReason.Regression)]
    [RequireGodotRuntime]
    public void CurrentViewAndArrivalRequireFineGeometryToReplaceItsCoarseAncestor()
    {
        var director = new StreamDirector();
        try
        {
            var context = Context();
            var pose = new Pose(new(64, 2, 64), 0, 0, 60);
            var catalog = ModuleTileCatalog.Build(new(new Dictionary<string, IReadOnlyList<TileKey>>(), []));
            BindUnrenderedCatalog(director, context, Task.FromResult(catalog));
            Set(director, "_catalog", catalog); Set(director, "_maps", new TestMapService());
            Set(director, "_camera", new TestCameraRig(pose: pose));
            Set(director, "_plannedContext", context); Set(director, "_plannedWorld", context.World);
            Set(director, "_plannedPose", pose); Set(director, "_plannedLive", true); Set(director, "_planReady", true);
            Set(director, "_selectionApplied", true); Set(director, "_destinationPlanned", true);
            Set(director, "_destination", pose); Set(director, "_destinationMode", ViewMode.Fly);
            var required = (HashSet<StreamTile>)Field(director, "_viewRequired")!;
            var refined = (HashSet<StreamTile>)Field(director, "_refined")!;
            var tree = new LodTree(new(new(0, 0, 0), new(128, 10, 128)), new());
            tree.Select(pose.Position, 0, 1080, pose.FovDeg!.Value, LodLevel.L1, required, refined);
            ((HashSet<StreamTile>)Field(director, "_destinationRequired")!).UnionWith(required);
            ((HashSet<StreamTile>)Field(director, "_destinationRefined")!).UnionWith(refined);
            var fine = required.Single(tile => tile.Level == LodLevel.L1);
            var ready = required.Where(tile => tile != fine).ToHashSet();
            var getGroup = typeof(StreamDirector).GetMethod("GetGroup", BindingFlags.Instance | BindingFlags.NonPublic)!;
            object? fineGroup = null;
            foreach (var tile in required)
            {
                var group = getGroup.Invoke(director, [tile])!;
                group.GetType().GetField("GeometryRequested")!.SetValue(group, true);
                group.GetType().GetField("Mounted")!.SetValue(group, ready.Contains(tile));
                if (tile == fine) fineGroup = group;
            }
            var visibility = (TileVisibility)Field(director, "_visibility")!;
            var visible = new HashSet<StreamTile>();
            tree.ResolveVisible(refined, ready.Contains, visible);
            visibility.Apply(visible, 0, false, (_, _, _) => { });
            if (director.ViewReady || director.DestinationReady)
                throw new InvalidOperationException("Coarse coverage certified missing selected fine geometry.");
            fineGroup!.GetType().GetField("Mounted")!.SetValue(fineGroup, true);
            if (director.ViewReady || director.DestinationReady)
                throw new InvalidOperationException("Mounted fine geometry was certified while its ancestor was still presented.");
            ready.Add(fine); tree.ResolveVisible(refined, ready.Contains, visible);
            visibility.Apply(visible, 0, false, (_, _, _) => { });
            if (!director.ViewReady || !director.DestinationReady)
                throw new InvalidOperationException("Current view and formal arrival did not share complete selected-geometry readiness.");
        }
        finally { director.Free(); }
    }

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
                else work.SetResult(ModuleTileCatalog.Build(new(new Dictionary<string, IReadOnlyList<TileKey>>(), [])));
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
                else work.SetResult(ModuleTileCatalog.Build(new(new Dictionary<string, IReadOnlyList<TileKey>>(), [])));
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
        BuildInputSnapshot.Freeze(new Dictionary<string, ReadOnlyMemory<byte>>(), "fixture"), "res://fixture/");
}
