using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Kernel.Bake;
using Campus.Kernel.Lod;
using Campus.Kernel.Stream;
using Campus.Kernel.Frame;
using Campus.Kernel.Renderer;
using Campus.Contracts.Fauna;
using Campus.Systems.Fauna;
using GdUnit4;
using Godot;
using N = System.Numerics;
using static Campus.Tests.StreamLifecycleTestSupport;

namespace Campus.Tests;
[TestSuite]
public sealed class StreamPreparationLifecycleTests
{
    [TestCase, TestCategory(TestReason.Regression), RequireGodotRuntime]
    public async Task RetiredVisualKeepsItsRequestIdentityAndEmptyGeometryCompletesOnlyWhenDrained()
    {
        var queue = new StreamMountQueue(() => 0);
        var key = new BakeKey(new("fixture"), "fixture.empty", "L4/0/0");
        var bundle = new TileBundle(key, StreamTile.Root.Tile, LodLevel.L4, new(N.Vector3.Zero, N.Vector3.One), []);
        var prepared = new PreparedTile(bundle); prepared.Initialize(bundle);
        var source = new List<PreparedTile> { prepared };
        var mounted = 0; var released = 0;
        var retired = new VisualMountWork(key, source, queue.Revision, default, BakePriority.Normal, null!, null!,
            () => throw new InvalidOperationException("Retired geometry became mounted."), () => released++, _ => { }, tile => tile.BeginRetirement(), () => true);
        queue.Enqueue(retired); queue.Retire(retired); source.Clear();
        var empty = new VisualMountWork(key, [], queue.Revision, default, BakePriority.Normal, null!, null!,
            () => mounted++, () => { }, _ => { }, tile => tile.BeginRetirement(), () => true);
        queue.Enqueue(empty);
        Check(retired.Key == key && empty.Key == key && mounted == 0 && !prepared.IsDisposed && queue.PendingRetirements == 1);
        queue.Drain(CoveredScreen);
        Check(queue.Failure is null && queue.Pending == 0 && queue.PendingRetirements == 0 && mounted == 1 && released == 1 && prepared.IsDisposed);
        await ReleasePreparedAsync(prepared);
    }

    [TestCase, TestCategory(TestReason.Regression), RequireGodotRuntime]
    public void VisualCompletionReportingAndCancellationFaultsRetainEveryOriginalCause()
    {
        var queue = new StreamMountQueue(() => 0);
        var original = new InvalidDataException("completion failed");
        var reporting = new InvalidOperationException("producer reporting failed");
        var cleanup = new IOException("cancellation callback failed");
        var key = new BakeKey(new("fixture"), "fixture.empty", "L4/0/0");
        var bundle = new TileBundle(key, StreamTile.Root.Tile, LodLevel.L4, new(N.Vector3.Zero, N.Vector3.One), []);
        var prepared = new PreparedTile(bundle);
        prepared.Initialize(bundle);
        Exception? received = null;
        var work = new VisualMountWork(key, [prepared], queue.Revision, default,
            BakePriority.Normal, null!, null!, () => throw original, () => throw cleanup,
            error => { received = error; throw reporting; }, tile => tile.BeginRetirement(), () => true);
        queue.Enqueue(work); queue.Drain(CoveredScreen);
        var causes = (queue.Failure as AggregateException ?? throw new InvalidOperationException("Visual callbacks lost their causes.")).Flatten().InnerExceptions;
        Check(ReferenceEquals(received, original) && causes.Contains(original) && causes.Contains(reporting) &&
            causes.Contains(cleanup) && work.IsCancelled && prepared.IsDisposed && queue.Pending == 0);
    }

    [TestCase, TestCategory(TestReason.Regression), TestCategory("NativeRender"), RequireGodotRuntime]
    public async Task InitializationFaultKeepsItsActualTaskAndReleasesPartialNativeStorage()
    {
        var bounds = new Box3(N.Vector3.Zero, N.Vector3.One);
        var key = new BakeKey(new("fixture"), "fixture.native", "L4/0/0");
        var valid = Bundle(key, bounds).Surfaces[0];
        var invalid = new BakedSurface(valid.Material, 0, valid.Primitive, valid.VertexCount, valid.IndexCount,
            valid.VertexData, valid.AttributeData, valid.IndexData, bounds);
        var bundle = new TileBundle(key, StreamTile.Root.Tile, LodLevel.L4, bounds, [valid, invalid]);
        var owner = new PreparedTile(bundle);
        var task = new StreamGeometryPreparation().PrepareAsync(owner, bundle);
        Exception? original = null;
        try { await task; throw new InvalidOperationException("Invalid native input initialized successfully."); }
        catch (InvalidDataException error) { original = error; }
        Check(task.IsFaulted && !owner.IsDisposed && owner.Draws.Count > 0);
        try { await task; throw new InvalidOperationException("Faulted initialization became successful."); }
        catch (Exception error) when (ReferenceEquals(error, original)) { }
        await ReleasePreparedAsync(owner);
        Check(owner.IsDisposed);
    }

    [TestCase, TestCategory(TestReason.Regression), TestCategory("NativeRender"), RequireGodotRuntime]
    public async Task RenderHandlesRevokeReadinessBeforeTheirNativeDrawsAndEmptyRootDrain()
    {
        var tree = (SceneTree)Engine.GetMainLoop(); var clock = 0d; var detached = false;
        Exception? releaseFailure = null;
        var queue = new StreamMountQueue(() => clock);
        using var material = new StandardMaterial3D();
        var materials = new Materials(material);
        var host = new RenderHost(materials, Campus.Contracts.Tests.PaletteFixture.Catalog, new Reflection(), queue);
        tree.Root.AddChild(host);
        var map = TestMapContext.BackdropDescriptor() with { Id = new("render-fixture") };
        var context = new TestMapContext(map, new Campus.Gen.Tests.Support.FakeWorld { Boundary = new([]) });
        host.OnMapLoaded(context);
        var source = new GeometryBundle(new(new ToonSpec(new("wall.lit"), new("wall.shade"))),
            [0, 0, 0, 1, 0, 0, 0, 0, 1], [0, 1, 0, 0, 1, 0, 0, 1, 0], [0, 1, 2]);
        var handle = host.Mount(new([source, source, source], []));
        try
        {
            Check(host.IsReady(handle) && !host.IsReleased(handle) && host.GetChild(0).GetChildCount() == 3);
            host.Unmount(handle);
            Check(!host.IsReady(handle) && !host.IsReleased(handle) && host.GetChildCount() == 1 &&
                !host.GetChild<Node3D>(0).Visible && queue.PendingRetirements == 1);
            queue.Drain(VisibleWorld, canReleaseRetired: () => detached);
            Check(host.GetChild(0).GetChildCount() == 3 && !host.IsReleased(handle));
            releaseFailure = new IOException("dynamic retirement detach failed");
            queue.Drain(VisibleWorld, canReleaseRetired: () => throw releaseFailure);
            Check(host.Failure is null && ReferenceEquals(queue.Failure, releaseFailure) &&
                queue.PendingRetirements == 1 && host.GetChild(0).GetChildCount() == 3 && !host.IsReleased(handle));
            releaseFailure = null; detached = true;
            queue.Enqueue(new MountWork(StreamMountKind.Visual, queue.Revision, _ => clock += KernelBudgets.StreamMountMilliseconds));
            queue.Drain(VisibleWorld, canReleaseRetired: () => detached);
            Check(host.GetChild(0).GetChildCount() == 2 && !host.IsReleased(handle));
            while (!host.IsReleased(handle)) { queue.Drain(VisibleWorld); await FrameAwaiter.ProcessAsync(host); }
            Check(host.GetChildCount() == 0 && queue.PendingRetirements == 0 && host.IsReleased(default));
            var original = new IOException("dynamic material preparation failed");
            materials.Failure = original;
            var failed = host.Mount(new([source], []));
            Check(ReferenceEquals(host.Failure, original) && !host.IsReady(failed) && !host.IsReleased(failed) && queue.PendingRetirements == 1);
            while (!host.IsReleased(failed)) { queue.Drain(VisibleWorld); await FrameAwaiter.ProcessAsync(host); }
            Check(host.GetChildCount() == 0 && ReferenceEquals(host.Failure, original));
            host.OnMapUnloading(map.Id);
        }
        finally { host.Free(); }
    }

    [TestCase, TestCategory(TestReason.Regression), TestCategory("NativeRender"), RequireGodotRuntime]
    public async Task FaunaReplacementKeepsItsDisplayLatchAndRetiredHandlesUntilNativeDrain()
    {
        var tree = (SceneTree)Engine.GetMainLoop(); var queue = new StreamMountQueue(() => 0);
        using var material = new StandardMaterial3D();
        var host = new RenderHost(new Materials(material), Campus.Contracts.Tests.PaletteFixture.Catalog, new Reflection(), queue);
        tree.Root.AddChild(host);
        var context = new TestMapContext(TestMapContext.BackdropDescriptor()); host.OnMapLoaded(context);
        var source = new GeometryBundle(new(new ToonSpec(new("wall.lit"), new("wall.shade"))),
            [0, 0, 0, 1, 0, 0, 0, 0, 1], [0, 1, 0, 0, 1, 0, 0, 1, 0], [0, 1, 2]);
        var output = new ContentOutput([source], []); var visible = false;
        var mounts = new FaunaTileMounts(); mounts.LoadMap(context.Descriptor.Id);
        mounts.Initialize(host, tile => { tile.Latches[0].Requested = true; tile.Latches[0].Desired = FaunaSet.Day; tile.Latches[0].AllowVisible = true; },
            _ => visible, error => throw error);
        var key = new BakeKey(context.Descriptor.Id, BakeModuleIds.FaunaRuntime, "L0/0/0");
        var content = new FaunaTileContent(key, new(0, 0), new([new("fixture", FaunaKind.Cat,
            new(N.Vector3.Zero, N.Vector3.One), [output, output, output])], []));
        var owner = mounts.CreateMount(content, BakePriority.Normal, queue.Revision);
        try
        {
            while (!owner.Step(1)) await FrameAwaiter.ProcessAsync(host);
            Check(mounts.TryGet(key, out var tile)); var latch = tile.Latches[0]; var old = latch.Handles.Single();
            latch.Desired = FaunaSet.Night; latch.Requested = true; latch.AllowVisible = false; latch.Version++;
            var update = tile.Work = mounts.CreateUpdate(tile);
            Check(!update.Step(1) && latch.RetiringHandles.Contains(old) && !host.IsReleased(old) && latch.Handles.Count == 1);
            Check(!host.GetChild<Node3D>(0).Visible && host.GetChild<Node3D>(1).Visible);
            visible = true;
            while (!update.Step(1)) await FrameAwaiter.ProcessAsync(host);
            Check(tile.Mounted && latch.Set == FaunaSet.Night && latch.Applied && !host.IsReleased(old));
            queue.Retire(owner);
            while (queue.PendingRetirements > 0) { queue.Drain(CoveredScreen); await FrameAwaiter.ProcessAsync(host); }
            Check(mounts.Tiles.Count == 0 && host.IsReleased(old) && host.GetChildCount() == 0);
        }
        finally
        {
            host.OnMapUnloading(context.Descriptor.Id); queue.Retire(owner);
            while (queue.PendingRetirements > 0) { queue.Drain(CoveredScreen); await FrameAwaiter.ProcessAsync(host); }
            host.Free();
        }
    }

    [TestCase, TestCategory(TestReason.Regression), TestCategory("NativeRender"), RequireGodotRuntime]
    public async Task CompletedPreparationMountsInTheCurrentFrameAndReusesItsSlot()
    {
        using var fixture = new StreamFixture(geometry: true);
        await fixture.Until(() => fixture.Geometry.Prepared.Count > 0, mount: false);
        var owner = fixture.Geometry.Prepared[0];
        Check(owner.Draws.Count > 0 && owner.Instances.Count == 0 && fixture.Mount.Pending == 0 &&
            fixture.Director.Statistics.PendingPreparations > 0);
        var started = fixture.Geometry.Prepared.Count;
        fixture.Tick();
        Check(owner.Instances.Count > 0 && fixture.Geometry.Prepared.Count > started);
        await fixture.Unload();
        Check(fixture.Director.IsUnloaded && fixture.Geometry.Prepared.All(tile => tile.IsDisposed));
    }

    [TestCase, TestCategory(TestReason.Regression), TestCategory("NativeRender"), RequireGodotRuntime]
    public async Task MultiMeshMountYieldsBeforePublicationAndCancellationReleasesEveryPartialOwner()
    {
        var bounds = new Box3(N.Vector3.Zero, N.Vector3.One);
        var key = new BakeKey(new("fixture"), "fixture.native", "L4/0/0");
        var bundle = Bundle(key, bounds, multiMeshes: true);
        Check(bundle.MultiMeshes.Count > 1);
        using var material = new StandardMaterial3D();
        var scenario = RenderingServer.ScenarioCreate();
        try
        {
            var completed = false;
            for (var interrupt = 1; interrupt < TestSamples.FrameLoop && !completed; interrupt++)
            {
                var owner = new PreparedTile(bundle);
                var materials = new StreamMaterials(new Materials(material), new Materials(material));
                try
                {
                    await new StreamGeometryPreparation().PrepareAsync(owner, bundle);
                    Check(owner.Draws.Count(draw => !draw.Base.IsValid) == bundle.MultiMeshes.Count && owner.Instances.Count == 0);
                    var ready = false;
                    var work = new VisualMountWork(key, [owner], 1, scenario, BakePriority.Normal, materials, new Reflection(),
                        () => ready = true, () => { }, error => throw error, tile => tile.BeginRetirement(), () => true);
                    for (var slice = 0; slice < interrupt && !completed; slice++)
                    {
                        completed = work.Step(double.Epsilon);
                        Check(ready == completed && (ready || owner.VisibleInstanceCount == 0));
                        Check(owner.Draws.All(draw => draw.PendingMultiMesh is null) || owner.Instances.Count == 0);
                    }
                    if (ready)
                    {
                        Check(owner.Instances.Count == owner.Draws.Count);
                        foreach (var draw in owner.Draws.Where(draw => draw.Base != draw.Mesh))
                            Check(RenderingServer.MultimeshGetInstanceCount(draw.Base) > 0 &&
                                RenderingServer.MultimeshGetBuffer(draw.Base).Length * sizeof(float) == BakedMultiMesh.InstanceStrideBytes);
                    }
                    work.Cancel();
                    while (!work.ReleaseStep(0)) await FrameAwaiter.ProcessAsync(((SceneTree)Engine.GetMainLoop()).Root);
                    Check(owner.IsDisposed && owner.Instances.Count == 0 && owner.Draws.Count == 0);
                }
                finally { while (!materials.ReleaseOne()) await FrameAwaiter.ProcessAsync(((SceneTree)Engine.GetMainLoop()).Root); }
            }
            Check(completed);
        }
        finally { RenderingServer.FreeRid(scenario); }
    }

    [TestCase, TestCategory(TestReason.Regression), TestCategory("NativeRender"), RequireGodotRuntime]
    public async Task ShutdownAwaitsLateNativePreparationAndReleasesItsActualOwner()
    {
        using var fixture = new StreamFixture(geometry: true);
        fixture.Geometry.Hold = true;
        await fixture.Until(() => fixture.Geometry.Waiting.Count > 0);
        var draining = fixture.Unload();
        Check(!draining.IsCompleted && !fixture.Director.IsUnloaded);
        Check(fixture.Geometry.Prepared.Count > 0 && fixture.Geometry.Prepared.All(tile => !tile.IsDisposed));
        fixture.Geometry.Release();
        await draining;
        Check(fixture.Geometry.Prepared.Count > 0 && fixture.Geometry.Prepared.All(tile => tile.IsDisposed) && fixture.Director.IsUnloaded);
    }

    [TestCase, TestCategory(TestReason.Regression), TestCategory("NativeRender"), RequireGodotRuntime]
    public async Task FailedNativePreparationRetainsItsOwnerUntilExplicitDrain()
    {
        using var fixture = new StreamFixture(geometry: true);
        fixture.Geometry.Hold = true;
        await fixture.Until(() => fixture.Geometry.Waiting.Count > 0);
        var original = new InvalidDataException("native preparation failed");
        fixture.Geometry.Fail(original);
        Check(fixture.Director.Failure is null);
        try { await fixture.Until(() => fixture.Director.Failure is not null); }
        catch (Exception error) when (ReferenceEquals(error, original)) { }
        Check(ReferenceEquals(fixture.Director.Failure, original));
        Check(fixture.Geometry.Prepared.Count > 0);
        fixture.Director._Process(0);
        Check(ReferenceEquals(fixture.Director.Failure, original));
        try { await fixture.Unload(); throw new InvalidOperationException("Drain lost failed initialization."); }
        catch (Exception error) when (ReferenceEquals(error, original)) { }
        Check(fixture.Director.IsUnloaded && fixture.Geometry.Prepared.All(tile => tile.IsDisposed));
        try { await fixture.Director.DrainAsync(); throw new InvalidOperationException("Repeated drain lost its original failure."); }
        catch (Exception error) when (ReferenceEquals(error, original)) { }
    }

    [TestCase, TestCategory(TestReason.Regression), RequireGodotRuntime]
    public async Task EmptyAggregateFailuresRemainPublishedAndReachDrainWithoutLosingNestedCauses()
    {
        using var fixture = new StreamFixture();
        await fixture.Until(() => fixture.Director.ViewReady);
        var original = new AggregateException();
        fixture.Mount.Drain(VisibleWorld, finish: _ => throw original);
        fixture.Director._Process(0);
        Check(ReferenceEquals(fixture.Director.Failure, original));
        fixture.Director._Process(0);
        Check(ReferenceEquals(fixture.Director.Failure, original));
        var later = new InvalidDataException("later mount failure");
        var nested = new AggregateException();
        fixture.Mount.Drain(VisibleWorld, finish: _ => throw new AggregateException(original, later, nested));
        fixture.Director._Process(0);
        var published = fixture.Director.Failure as AggregateException
            ?? throw new InvalidOperationException("Stream failure lost its collected causes.");
        Check(published.InnerExceptions.Count == 3 && ReferenceEquals(published.InnerExceptions[0], original) &&
            ReferenceEquals(published.InnerExceptions[1], later) && ReferenceEquals(published.InnerExceptions[2], nested));
        fixture.Director._Process(0);
        Check(ReferenceEquals(fixture.Director.Failure, published));
        try { await fixture.Unload(); throw new InvalidOperationException("Drain lost empty aggregate failures."); }
        catch (Exception error) when (ReferenceEquals(error, published)) { }
        Check(fixture.Director.IsUnloaded);
    }

    [TestCase, TestCategory(TestReason.Regression), RequireGodotRuntime]
    public async Task MountFailureIsCollectedByPollWithoutAdvancingReaders()
    {
        using var fixture = new StreamFixture();
        await fixture.Until(() => fixture.Director.ViewReady);
        var original = new InvalidDataException("mount queue failed");
        fixture.Mount.Drain(VisibleWorld, finish: _ => throw original);
        Check(fixture.Director.Failure is null);
        _ = fixture.Director.ResidentReady;
        _ = fixture.Director.DestinationReady;
        _ = fixture.Director.ViewReady;
        Check(fixture.Director.Failure is null);
        fixture.Director._Process(0);
        Check(ReferenceEquals(fixture.Director.Failure, original));
        fixture.Director._Process(0);
        Check(ReferenceEquals(fixture.Director.Failure, original));
        try { await fixture.Unload(); throw new InvalidOperationException("Drain lost the original mount failure."); }
        catch (Exception error) when (ReferenceEquals(error, original)) { }
        Check(fixture.Director.IsUnloaded);
    }

    [TestCase, TestCategory(TestReason.Regression), TestCategory("NativeRender"), RequireGodotRuntime]
    public async Task ResidentBackdropAppearsEvenWhenReadyChildrenSkipTheCoarseRoot()
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
            var prepared = new PreparedTile(bundle);
            prepared.Initialize(bundle);
            for (var i = 0; i < prepared.Draws.Count; i++) prepared.MountOne(i, scenario, material);
            var resident = prepared.Draws.Count(draw => draw.Resident);
            if (prepared.VisibleInstanceCount != resident || resident == 0 || resident == prepared.Draws.Count)
                throw new InvalidOperationException("Resident background stayed hidden until a root selection, or ordinary coarse geometry appeared early.");
            prepared.SetVisible(true, 0);
            if (prepared.VisibleInstanceCount != prepared.Draws.Count)
                throw new InvalidOperationException("Root activation failed to show ordinary coarse geometry beside its resident background.");
            prepared.SetVisible(false, 0);
            if (prepared.VisibleInstanceCount != resident)
                throw new InvalidOperationException("Refining the root hid its resident background.");
            prepared.BeginRetirement();
            if (prepared.ReleaseOne() || prepared.IsDisposed)
                throw new InvalidOperationException("One bounded release dropped the tile's remaining native ownership.");
            await ReleasePreparedAsync(prepared);
            if (!prepared.IsDisposed || prepared.Instances.Count != 0 || prepared.Draws.Count != 0 || !prepared.ReleaseOne())
                throw new InvalidOperationException("Full release could not resume after a bounded retirement step.");
            await ReleasePreparedAsync(prepared);
        }
        finally { RenderingServer.FreeRid(material); RenderingServer.FreeRid(scenario); }
    }
}
