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
            var interiors = new InteriorStream(null!, null!, null!, null!, _ => { }, false); interiors.Load(context);
            Set(Field(director, "_preparation")!, "_catalog", catalog); Set(director, "_maps", new TestMapService()); Set(director, "_interiors", interiors);
            Set(Field(director, "_selection")!, "_destinationConsumed", true); Set(Field(director, "_selection")!, "_destinationPlanned", true); Set(Field(director, "_selection")!, "_selectionApplied", true);
            if (!director.DestinationReady || director.ViewReady)
                throw new InvalidOperationException("The consumed arrival inherited readiness from an unrelated unprepared view.");
            var target = new Pose(new(1, 2, 3), 0, 0);
            director.Prefetch(target, ViewMode.Fly);
            // A mount finishing for the old view must not certify this newly registered destination.
            Set(Field(director, "_selection")!, "_selectionApplied", true);
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
            Set(Field(director, "_preparation")!, "_catalog", catalog); Set(director, "_maps", new TestMapService());
            Set(director, "_camera", new TestCameraRig(pose: pose));
            Set(Field(director, "_selection")!, "_plannedContext", context); Set(Field(director, "_selection")!, "_plannedWorld", context.World);
            Set(Field(director, "_selection")!, "_plannedPose", pose); Set(Field(director, "_selection")!, "_plannedLive", true); Set(Field(director, "_selection")!, "_planReady", true);
            Set(Field(director, "_selection")!, "_selectionApplied", true); Set(Field(director, "_selection")!, "_destinationPlanned", true);
            Set(Field(director, "_selection")!, "_destination", pose); Set(Field(director, "_selection")!, "_destinationMode", ViewMode.Fly);
            var required = (HashSet<StreamTile>)Field(Field(director, "_selection")!, "_viewRequired")!;
            var refined = (HashSet<StreamTile>)Field(Field(director, "_selection")!, "_refined")!;
            var tree = new LodTree(new(new(0, 0, 0), new(128, 10, 128)), new());
            Set(Field(director, "_selection")!, "_tree", tree);
            tree.Select(pose.Position, 0, 1080, pose.FovDeg!.Value, LodLevel.L1, required, refined);
            ((HashSet<StreamTile>)Field(Field(director, "_selection")!, "_destinationRequired")!).UnionWith(required);
            ((HashSet<StreamTile>)Field(Field(director, "_selection")!, "_destinationRefined")!).UnionWith(refined);
            var fine = required.Single(tile => tile.Level == LodLevel.L1);
            var ready = required.Where(tile => tile != fine).ToHashSet();
            var getGroup = Field(director, "_residency")!.GetType().GetMethod("GetGroup", BindingFlags.Instance | BindingFlags.NonPublic)!;
            object? fineGroup = null;
            foreach (var tile in required)
            {
                var group = getGroup.Invoke(Field(director, "_residency"), [tile])!;
                group.GetType().GetField("GeometryRequested")!.SetValue(group, true);
                group.GetType().GetField("Mounted")!.SetValue(group, ready.Contains(tile));
                if (tile == fine) fineGroup = group;
            }
            var visibility = (TileVisibility)Field(Field(director, "_selection")!, "_visibility")!;
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

            var children = tree.Children(fine);
            foreach (var child in children)
            {
                var group = getGroup.Invoke(Field(director, "_residency"), [child])!;
                group.GetType().GetField("GeometryRequested")!.SetValue(group, true);
                group.GetType().GetField("Mounted")!.SetValue(group, true);
            }
            visibility.Apply(children.Skip(1).ToHashSet(), 0, false, (_, _, _) => { });
            if (director.ViewReady || director.DestinationReady)
                throw new InvalidOperationException("Partial descendant coverage certified a missing part of the selected tile.");
            visibility.Apply(children.ToHashSet(), 0, false, (_, _, _) => { });
            if (!director.ViewReady || !director.DestinationReady)
                throw new InvalidOperationException("Complete finer coverage failed because its selected parent was no longer drawn.");

            // Both orderings share the real display frontier: current finer, then destination finer.
            var destinationRequired = (HashSet<StreamTile>)Field(Field(director, "_selection")!, "_destinationRequired")!;
            var destinationRefined = (HashSet<StreamTile>)Field(Field(director, "_selection")!, "_destinationRefined")!;
            var resolve = Field(director, "_selection")!.GetType().GetMethod("ResolveVisible", BindingFlags.Instance | BindingFlags.NonPublic)!;
            foreach (var currentFiner in new[] { true, false })
            {
                tree.Select(pose.Position, 0, 1080, pose.FovDeg.Value,
                    currentFiner ? LodLevel.L0 : LodLevel.L1, required, refined);
                tree.Select(pose.Position, 0, 1080, pose.FovDeg.Value,
                    currentFiner ? LodLevel.L1 : LodLevel.L0, destinationRequired, destinationRefined);
                resolve.Invoke(Field(director, "_selection"), null);
                var displayed = (HashSet<StreamTile>)Field(Field(director, "_selection")!, "_visible")!;
                visibility.Apply(displayed, 0, false, (_, _, _) => { });
                if (!displayed.SetEquals(children) || !director.ViewReady || !director.DestinationReady)
                    throw new InvalidOperationException("An active destination and current eye could not share the finer complete display frontier.");
            }
        }
        finally { director.Free(); }
    }

    [TestCase]
    [TestCategory(TestReason.Regression)]
    [RequireGodotRuntime]
    public void ElevatedMixedFrontierNeedsOnlyItsDrawnGeometryAndStillRejectsAHole()
    {
        var director = new StreamDirector();
        try
        {
            var context = Context();
            var pose = new Pose(new(-193, 158, 111), 0, -68, 60);
            var catalog = ModuleTileCatalog.Build(new(new Dictionary<string, IReadOnlyList<TileKey>>(), []));
            BindUnrenderedCatalog(director, context, Task.FromResult(catalog));
            Set(Field(director, "_preparation")!, "_catalog", catalog); Set(director, "_maps", new TestMapService());
            Set(director, "_camera", new TestCameraRig(pose: pose));
            Set(Field(director, "_selection")!, "_plannedContext", context); Set(Field(director, "_selection")!, "_plannedWorld", context.World);
            Set(Field(director, "_selection")!, "_plannedPose", pose); Set(Field(director, "_selection")!, "_plannedLive", true); Set(Field(director, "_selection")!, "_planReady", true);
            Set(Field(director, "_selection")!, "_selectionApplied", true); Set(Field(director, "_selection")!, "_destinationPlanned", true);
            Set(Field(director, "_selection")!, "_destination", pose); Set(Field(director, "_selection")!, "_destinationMode", ViewMode.Fly);
            var required = (HashSet<StreamTile>)Field(Field(director, "_selection")!, "_viewRequired")!;
            var refined = (HashSet<StreamTile>)Field(Field(director, "_selection")!, "_refined")!;
            var tree = new LodTree(new(new(-512, 0, -512), new(512, 10, 512)), new());
            Set(Field(director, "_selection")!, "_tree", tree);
            tree.Select(pose.Position, 0, 1080, pose.FovDeg!.Value, LodLevel.L0, required, refined);
            ((HashSet<StreamTile>)Field(Field(director, "_selection")!, "_destinationRequired")!).UnionWith(required);
            ((HashSet<StreamTile>)Field(Field(director, "_selection")!, "_destinationRefined")!).UnionWith(refined);
            var frontier = required.Where(tile => !refined.Contains(tile)).ToHashSet();
            if (frontier.Select(tile => tile.Level).Distinct().Count() < 2)
                throw new InvalidOperationException("The elevated fixture must select a mixed detail frontier.");
            var getGroup = Field(director, "_residency")!.GetType().GetMethod("GetGroup", BindingFlags.Instance | BindingFlags.NonPublic)!;
            var groups = required.ToDictionary(tile => tile, tile => getGroup.Invoke(Field(director, "_residency"), [tile])!);
            foreach (var (tile, group) in groups)
            {
                group.GetType().GetField("GeometryRequested")!.SetValue(group, true);
                group.GetType().GetField("Mounted")!.SetValue(group, tile == StreamTile.Root || frontier.Contains(tile));
            }
            var resolve = Field(director, "_selection")!.GetType().GetMethod("ResolveVisible", BindingFlags.Instance | BindingFlags.NonPublic)!;
            var visibility = (TileVisibility)Field(Field(director, "_selection")!, "_visibility")!;
            void Present()
            {
                resolve.Invoke(Field(director, "_selection"), null);
                visibility.Apply((HashSet<StreamTile>)Field(Field(director, "_selection")!, "_visible")!, 0, false, (_, _, _) => { });
            }
            Present();
            if (!visibility.Drawn.SetEquals(frontier) || !director.ViewReady || !director.DestinationReady)
                throw new InvalidOperationException("Undrawn ancestors blocked complete ready mixed-detail geometry.");
            groups[StreamTile.Root].GetType().GetField("Mounted")!.SetValue(groups[StreamTile.Root], false);
            if (director.ViewReady || director.DestinationReady)
                throw new InvalidOperationException("Complete finer geometry certified an unmounted resident root.");
            groups[StreamTile.Root].GetType().GetField("Mounted")!.SetValue(groups[StreamTile.Root], true);
            var missing = frontier.OrderBy(tile => tile.Level).First();
            groups[missing].GetType().GetField("Mounted")!.SetValue(groups[missing], false);
            groups[missing.Parent].GetType().GetField("Mounted")!.SetValue(groups[missing.Parent], true);
            Present();
            if (!visibility.Drawn.Contains(missing.Parent) || director.ViewReady || director.DestinationReady)
                throw new InvalidOperationException("A coarser parent certified a missing selected leaf.");
            groups[missing.Parent].GetType().GetField("Mounted")!.SetValue(groups[missing.Parent], false);
            groups[StreamTile.Root].GetType().GetField("Mounted")!.SetValue(groups[StreamTile.Root], false);
            Present();
            if (visibility.Drawn.Count != 0 || director.ViewReady || director.DestinationReady)
                throw new InvalidOperationException("An incomplete frontier was shown without complete coarser coverage.");
        }
        finally { director.Free(); }
    }

    [TestCase]
    [TestCategory(TestReason.Regression)]
    [TestCategory("NativeRender")]
    [RequireGodotRuntime]
    public void ResidentBackdropAppearsEvenWhenReadyChildrenSkipTheCoarseRoot()
    {
        var bounds = new Box3(System.Numerics.Vector3.Zero, System.Numerics.Vector3.One);
        var tree = new LodTree(bounds, new StreamSettings());
        var visible = new HashSet<StreamTile>();
        tree.ResolveVisible(new HashSet<StreamTile> { StreamTile.Root }, _ => true, visible);
        if (visible.Contains(StreamTile.Root)) throw new InvalidOperationException("The fixture must skip the ready coarse root.");

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
                throw new InvalidOperationException("Resident background stayed hidden until a root selection, or ordinary coarse geometry appeared early.");
            prepared.SetVisible(false, 0);
            if (prepared.VisibleInstanceCount != resident)
                throw new InvalidOperationException("Refining the root hid its resident background.");
        }
        finally { RenderingServer.FreeRid(material); RenderingServer.FreeRid(scenario); }
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    [RequireGodotRuntime]
    public async Task ShutdownDrainsTheCatalogAndDeliversLateFailuresWithoutPublication()
    {
        foreach (var fail in new[] { false, true })
        {
            var director = new StreamDirector();
            try
            {
                var context = Context();
                var work = new TaskCompletionSource<ModuleTileCatalog>();
                var original = new InvalidOperationException("Retired catalog failed.");
                BindUnrenderedCatalog(director, context, work.Task);
                director.OnMapUnloading(context.Descriptor.Id);
                var draining = director.DrainAsync();
                if (director.IsUnloaded || draining.IsCompleted)
                    throw new InvalidOperationException("Unload abandoned its actual pending catalog.");
                if (fail) work.SetException(original);
                else work.SetResult(ModuleTileCatalog.Build(new(new Dictionary<string, IReadOnlyList<TileKey>>(), [])));
                try { await draining; if (fail) throw new InvalidOperationException("The lifetime lost its original catalog failure."); }
                catch (Exception error) when (ReferenceEquals(error, original)) { }
                if (!director.IsUnloaded || director.ResidentReady ||
                    !ReferenceEquals(director.Failure, fail ? original : null) || Field(Field(director, "_preparation")!, "_catalog") is not null)
                    throw new InvalidOperationException("A retired catalog published or its failure lost its owning lifetime.");
                director._ExitTree();
            }
            finally { director.Free(); }
        }
    }

    [TestCase, TestCategory(TestReason.Regression)]
    [RequireGodotRuntime]
    public async Task CancelledSelectionAndPinsRetainTheirTasksAndLateFailuresUntilDrain()
    {
        var director = new StreamDirector();
        try
        {
            var context = Context();
            var catalog = ModuleTileCatalog.Build(new(new Dictionary<string, IReadOnlyList<TileKey>>(), []));
            BindUnrenderedCatalog(director, context, Task.FromResult(catalog));
            var terrain = new PendingTerrain();
            Set(director, "_terrain", terrain);
            var selection = Field(director, "_selection")!;
            var pose = new Pose(new(1, 2, 3), 0, 0);
            selection.GetType().GetMethod("PreparePlanningTerrain", BindingFlags.NonPublic | BindingFlags.Instance)!
                .Invoke(selection, [pose, true]);
            director.SetPinnedDestinations([new(pose, ViewMode.Fly)]);
            var pins = Field(director, "_pins")!;
            pins.GetType().GetMethod("PollPinnedPreparation", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(pins, null);
            director.OnMapUnloading(context.Descriptor.Id);
            var draining = director.DrainAsync();
            if (terrain.Work.Count != 2 || draining.IsCompleted || director.IsUnloaded || terrain.Tokens.Any(token => !token.IsCancellationRequested))
                throw new InvalidOperationException("Cancelled planning or pins lost their actual worker lifetime.");
            var planning = new InvalidDataException("late planning failure");
            var pinned = new InvalidDataException("late pin failure");
            terrain.Work[0].SetException(planning); terrain.Work[1].SetException(pinned);
            try { await draining; throw new InvalidOperationException("Late terrain faults were not delivered."); }
            catch (AggregateException error)
            {
                var failures = error.Flatten().InnerExceptions;
                if (failures.Count != 2 || !failures.Contains(planning) || !failures.Contains(pinned)) throw;
            }
            if (!director.IsUnloaded || director.ResidentReady) throw new InvalidOperationException("Drained terrain republished the retired map.");
            director._ExitTree();
        }
        finally { director.Free(); }
    }

    [TestCase, TestCategory(TestReason.Regression), TestCategory("NativeRender")]
    [RequireGodotRuntime]
    public async Task ShutdownAwaitsLateNativePreparationAndReleasesItsActualOwner()
    {
        var director = new StreamDirector();
        PreparedTile? prepared = null;
        try
        {
            var context = Context();
            var catalog = ModuleTileCatalog.Build(new(new Dictionary<string, IReadOnlyList<TileKey>>(), []));
            BindUnrenderedCatalog(director, context, Task.FromResult(catalog));
            var preparation = Field(director, "_preparation")!;
            var residency = Field(director, "_residency")!;
            var group = residency.GetType().GetMethod("GetGroup", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(residency, [StreamTile.Root])!;
            var work = new TaskCompletionSource<PreparedTile>();
            var pendingType = preparation.GetType().GetNestedType("Preparing", BindingFlags.NonPublic)!;
            var pending = pendingType.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Single(constructor => constructor.GetParameters().Length == 5).Invoke([StreamTile.Root, 1L, work.Task, group, 0L]);
            ((System.Collections.IList)Field(preparation, "_preparing")!).Add(pending);
            director.OnMapUnloading(context.Descriptor.Id);
            var draining = director.DrainAsync();
            if (draining.IsCompleted || director.IsUnloaded) throw new InvalidOperationException("Native preparation outlived its stream drain.");
            var bounds = new Box3(System.Numerics.Vector3.Zero, System.Numerics.Vector3.One);
            var source = new GeometryBundle(new(new ToonSpec(new("wall.lit"), new("wall.shade"))),
                [0, 0, 0, 1, 0, 0, 0, 0, 1], [0, 1, 0, 0, 1, 0, 0, 1, 0], [0, 1, 2]);
            var bundle = new TileBundle(new(context.Descriptor.Id, "fixture.native", "L4/0/0"), StreamTile.Root.Tile, LodLevel.L4, bounds,
                [BakeSurfacePacker.Pack(source, bounds, Campus.Contracts.Tests.PaletteFixture.Catalog)]);
            prepared = PreparedTile.Create(bundle);
            if (prepared.Draws.Count == 0) throw new InvalidOperationException("Native fixture created no actual mesh resource.");
            work.SetResult(prepared);
            await draining;
            if (!prepared.IsDisposed || !director.IsUnloaded) throw new InvalidOperationException("Late native preparation was not released by the owning drain.");
            director._ExitTree();
        }
        finally { prepared?.Dispose(); director.Free(); }
    }

    private sealed class PendingTerrain : ITerrainPreparation
    {
        public List<TaskCompletionSource> Work { get; } = [];
        public List<CancellationToken> Tokens { get; } = [];
        public bool IsPrepared(IWorldModel world, System.Numerics.Vector2 point) => false;
        public Task PreparePoints(IMapContext context, string product, IReadOnlyList<System.Numerics.Vector2> points, CancellationToken cancellation = default)
        {
            var work = new TaskCompletionSource(); Work.Add(work); Tokens.Add(cancellation); return work.Task;
        }
        public Task PrepareRegion(IMapContext context, MapRect region, CancellationToken cancellation = default) => throw new NotSupportedException();
    }

    private static void BindUnrenderedCatalog(StreamDirector director, IMapContext context, Task<ModuleTileCatalog> task)
    {
        // Drive the actual node's CPU lifecycle without constructing a renderer, scene or parallel resident.
        Set(director, "_context", context); Set(director, "_mapGeneration", 1L); Set(Field(director, "_preparation")!, "_catalogGeneration", 1L);
        Set(Field(director, "_preparation")!, "_catalogTask", task); Set(director, "_mount", new StreamMountQueue(() => 0));
        Set(Field(director, "_selection")!, "_visibility", new TileVisibility(.25f)); Set(Field(director, "_residency")!, "_materials", new StreamMaterials(null!, null!));
    }
    private static object? Field(object target, string name) =>
        target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(target);
    private static void Set(object target, string name, object value) =>
        target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(target, value);
    private static IMapContext Context() => new TestMapContext(BackdropInputsTests.Descriptor(), new FakeWorld { Boundary = new([]) },
        BuildInputSnapshot.Freeze(new Dictionary<string, ReadOnlyMemory<byte>>(), "fixture"), "res://fixture/");
}
