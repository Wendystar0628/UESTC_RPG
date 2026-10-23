using Campus.Camera;
using Campus.Gen.Tests.Support;
using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Maps;
using Campus.Contracts.Save;
using N = System.Numerics;
using GdUnit4;
using Godot;

namespace Campus.Tests;

[TestSuite]
public sealed class ObserverTransitRuntimeTests
{
    [TestCase, TestCategory(TestReason.Regression), RequireGodotRuntime]
    public async Task TransitFaultsKeepTheirCausesAndSuccessfulOrCancelledRunsKeepTheirResults()
    {
        if (Engine.GetMainLoop() is not SceneTree)
            throw new InvalidOperationException("Transit regression requires a real Godot scene tree.");
        var camera = new TestCameraRig();
        var terrain = new FlowTerrainPreparation();
        var maps = new TestMapService { Context = FlightBoundsTests.CreateContext() };
        foreach (var failCleanup in new[] { false, true })
        {
            var original = new InvalidDataException("transit destination preparation failed");
            var cleanup = failCleanup ? new InvalidOperationException("loading lease release failed") : null;
            var runtime = new FlowRuntimeTestStub(() => true) { Failure = original };
            var driver = new TransitDriver(camera, new TransitPresentation(Task.CompletedTask, cleanup), runtime, maps, terrain, terrain);
            var handle = driver.Start(camera.Pose, camera.Pose);
            try { await handle.Done; throw new InvalidOperationException("Transit failure was converted into a result."); }
            catch (Exception error) when (handle.Done.IsFaulted)
            {
                if (cleanup is null)
                {
                    if (!ReferenceEquals(error, original)) throw new InvalidOperationException("Transit lost its original failure.");
                }
                else if (error is not AggregateException aggregate || !aggregate.InnerExceptions.Contains(original) ||
                    !aggregate.InnerExceptions.Contains(cleanup))
                    throw new InvalidOperationException("Transit lost a preparation or cleanup cause.");
            }
            await driver.CancelAsync();
        }
        var success = new TransitDriver(camera, new TransitPresentation(Task.CompletedTask),
            new FlowRuntimeTestStub(() => true), maps, terrain, terrain);
        if (await success.Start(camera.Pose, camera.Pose).Done != TransitResult.Arrived)
            throw new InvalidOperationException("Successful transit changed its result.");
        var covered = new TaskCompletionSource();
        var cancelled = new TransitDriver(camera, new TransitPresentation(covered.Task),
            new FlowRuntimeTestStub(() => true), maps, terrain, terrain).Start(camera.Pose, camera.Pose);
        cancelled.Cancel();
        if (await cancelled.Done != TransitResult.Cancelled || cancelled.Done.IsFaulted)
            throw new InvalidOperationException("Cancelled transit was treated as failure.");
    }

    [TestCase, TestCategory(TestReason.Regression), RequireGodotRuntime]
    public async Task IndependentTransitAndCleanupCancellationsKeepTheirOriginalFailures()
    {
        if (Engine.GetMainLoop() is not SceneTree)
            throw new InvalidOperationException("Transit regression requires a real Godot scene tree.");
        var camera = new TestCameraRig();
        var terrain = new FlowTerrainPreparation();
        var maps = new TestMapService { Context = FlightBoundsTests.CreateContext() };
        var original = new OperationCanceledException("arrival preparation cancelled independently");
        var cleanup = new OperationCanceledException("loading lifetime cancelled independently");
        var runtime = new FlowRuntimeTestStub(() => true) { Failure = original };
        var driver = new TransitDriver(camera, new TransitPresentation(Task.CompletedTask, cleanup),
            runtime, maps, terrain, terrain);
        var handle = driver.Start(camera.Pose, camera.Pose);
        try { await handle.Done; throw new InvalidOperationException("Independent cancellation was converted into a transit result."); }
        catch (Exception error) when (handle.Done.IsFaulted)
        {
            if (error is not AggregateException aggregate || !aggregate.InnerExceptions.Contains(original) ||
                !aggregate.InnerExceptions.Contains(cleanup))
                throw new InvalidOperationException("Transit lost an independent cancellation cause.");
        }
        await driver.CancelAsync();
    }

    [TestCase, TestCategory(TestReason.Regression), RequireGodotRuntime]
    public async Task CancelledModeEntryWaitsForItsExecutionAndLoadingCleanupFailure()
    {
        var camera = new TestCameraRig();
        var terrain = new FlowTerrainPreparation();
        var maps = new TestMapService { Context = FlightBoundsTests.CreateContext() };
        var cleanup = new InvalidDataException("loading cleanup failed after cancellation");
        var loading = new TransitPresentation(Task.CompletedTask, cleanup, holdCleanup: true);
        var runtime = new FlowRuntimeTestStub(() => true);
        var driver = new TransitDriver(camera, loading, runtime, maps, terrain, terrain);
        var applying = new TaskCompletionSource<ModeSwitchResult>();
        var handle = driver.SwitchMode(_ => applying.Task);
        handle.Cancel();
        var drain = driver.CancelAsync();
        if (handle.Done.IsCompleted || drain.IsCompleted || loading.Leases.Single().Releasing.Task.IsCompleted)
            throw new InvalidOperationException("Cancellation completed before the mode execution returned.");
        if (!ReferenceEquals(drain, driver.CancelAsync()))
            throw new InvalidOperationException("Concurrent cancellation did not share its drain.");

        applying.SetResult(ModeSwitchResult.Ok);
        var lease = loading.Leases.Single();
        await lease.Releasing.Task;
        if (handle.Done.IsCompleted || drain.IsCompleted || runtime.Arrivals != 0)
            throw new InvalidOperationException("Cancelled execution completed before loading cleanup or published an arrival.");
        lease.Released.SetResult();
        try { await drain; throw new InvalidOperationException("Drain lost cancelled execution cleanup failure."); }
        catch (Exception error) when (ReferenceEquals(error, cleanup)) { }
        try { await handle.Done; throw new InvalidOperationException("Done lost cancelled execution cleanup failure."); }
        catch (Exception error) when (ReferenceEquals(error, cleanup)) { }
        await driver.CancelAsync();
    }

    [TestCase, TestCategory(TestReason.Regression), RequireGodotRuntime]
    public async Task ReplacedRunsDrainTogetherAndTheDriverCanRunAgain()
    {
        var camera = new TestCameraRig();
        var terrain = new FlowTerrainPreparation();
        var maps = new TestMapService { Context = FlightBoundsTests.CreateContext() };
        var covered = new TaskCompletionSource();
        var loading = new TransitPresentation(covered.Task, holdCleanup: true);
        var driver = new TransitDriver(camera, loading, new FlowRuntimeTestStub(() => true), maps, terrain, terrain);
        var first = driver.Start(camera.Pose, camera.Pose);
        var second = driver.Start(camera.Pose, camera.Pose);
        var drain = driver.CancelAsync();
        await Task.WhenAll(loading.Leases.Select(lease => lease.Releasing.Task));
        var firstLease = loading.Leases[0];
        var secondLease = loading.Leases[1];
        secondLease.Released.SetResult();
        if (await second.Done != TransitResult.Cancelled || drain.IsCompleted || first.Done.IsCompleted)
            throw new InvalidOperationException("Drain forgot the superseded execution's loading ownership.");
        firstLease.Released.SetResult();
        await drain;
        if (await first.Done != TransitResult.Cancelled)
            throw new InvalidOperationException("Replaced transit changed its cancellation result.");

        covered.SetResult();
        var next = driver.Start(camera.Pose, camera.Pose);
        var nextLease = loading.Leases[^1];
        await nextLease.Releasing.Task;
        nextLease.Released.SetResult();
        if (await next.Done != TransitResult.Arrived)
            throw new InvalidOperationException("A drained driver could not execute the next transit.");
        await driver.DisposeAsync();
    }

    [TestCase, TestCategory(TestReason.Regression), RequireGodotRuntime]
    public async Task ForcedSnapshotsHoldThePreRequestStateUntilThePreparedDestinationIsAccepted()
    {
        var baseMap = FlightBoundsTests.CreateContext();
        var terrain = new PendingDestination();
        var map = new TestMapContext(baseMap.Descriptor, new FakeWorld
        {
            Boundary = baseMap.World.Boundary, Outskirts = baseMap.World.Outskirts, Spatial = terrain
        });
        var maps = new TestMapService { Context = map, Current = map.Descriptor.Id };
        var camera = new SnapshotCamera();
        var runtime = new FlowRuntimeTestStub(() => true);
        var loading = new TransitPresentation(Task.CompletedTask, holdCleanup: true);
        var driver = new TransitDriver(camera, loading, runtime, maps, terrain, terrain);
        try
        {
            camera.Driver = driver;
            var before = camera.Pose with { FovDeg = null };
            var target = new Pose(new(45, 25, 12), 71, -18);
            var handle = driver.Start(camera.Pose, target);
            await terrain.Entered.Task;
            if (terrain.Prepared || handle.Done.IsCompleted || runtime.Arrivals != 0)
                throw new InvalidOperationException("The regression did not hold a real unprepared destination.");
            camera.SetPose(new(new(99, 9, 99), 0, 0));
            var pending = camera.CaptureSettledObserver(maps.Current);
            if (pending.View.Pose != before || terrain.SpaceQueries != 0)
                throw new InvalidOperationException("Forced capture sampled a moving camera or queried an unprepared target.");
            terrain.Release.SetResult();
            var lease = loading.Leases.Single();
            await lease.Releasing.Task;
            var accepted = camera.CaptureSettledObserver(maps.Current);
            if (accepted.View.Pose != target || terrain.SpaceQueries != 1 || runtime.Arrivals != 1)
                throw new InvalidOperationException("Prepared target was not atomically accepted with its space.");
            camera.SetPose(new(new(88, 8, 88), 0, 0));
            if (!ReferenceEquals(accepted, camera.CaptureSettledObserver(maps.Current)) || terrain.SpaceQueries != 1)
                throw new InvalidOperationException("Forced capture rebuilt its accepted endpoint through a live spatial query.");
            lease.Released.SetResult();
            if (await handle.Done != TransitResult.Arrived) throw new InvalidOperationException("Prepared transit did not arrive.");
            var moved = new Pose(new(55, 25, 15), 3, -5);
            camera.SetPose(moved);
            terrain.Reset();
            var next = driver.Start(moved, target);
            await terrain.Entered.Task;
            if (camera.CaptureSettledObserver(maps.Current).View.Pose != moved)
                throw new InvalidOperationException("The next request lost normal movement since the preceding transit.");
            terrain.Release.SetResult();
            var nextLease = loading.Leases.Last();
            await nextLease.Releasing.Task;
            nextLease.Released.SetResult();
            await next.Done;
        }
        finally
        {
            terrain.Release.TrySetResult();
            foreach (var lease in loading.Leases) lease.Released.TrySetResult();
            await driver.DisposeAsync();
        }
    }

    private sealed class SnapshotCamera : TestCameraRig
    {
        public ITransitDriver? Driver { get; set; }
        public override ObserverSaveSnapshot CaptureSettledObserver(MapId map) =>
            Driver?.SaveSnapshot ?? base.CaptureSettledObserver(map);
    }

    private sealed class PendingDestination : ITerrainPreparation, ICameraPoseValidator, ISpatialIndex
    {
        public TaskCompletionSource Entered { get; private set; } = new();
        public TaskCompletionSource Release { get; private set; } = new();
        public bool Prepared { get; private set; }
        public int SpaceQueries { get; private set; }
        public void Reset() { Prepared = false; Entered = new(); Release = new(); }
        public bool IsLegal(Pose pose, bool outsideBuffer) => true;
        public Task<Pose> LegalizePrepared(Pose pose, CancellationToken cancellation = default) => Task.FromResult(pose);
        public bool IsPrepared(IWorldModel world, N.Vector2 point) => Prepared;
        public async Task PrepareRegion(IMapContext context, MapRect region, CancellationToken cancellation = default)
        {
            Entered.TrySetResult();
            await Release.Task.WaitAsync(cancellation);
            Prepared = true;
        }
        public Task PreparePoints(IMapContext context, IReadOnlyList<N.Vector2> points, CancellationToken cancellation = default) =>
            throw new NotSupportedException();
        public IReadOnlyList<FeatureId> Query(Box3 box) => [];
        public float GroundY(float x, float z) => throw new NotSupportedException();
        public WalkClass WalkClassAt(float x, float z) => throw new NotSupportedException();
        public CameraMedium MediumAt(N.Vector3 position)
        {
            if (!Prepared) throw new InvalidOperationException("Target space read before preparation.");
            SpaceQueries++;
            return new(MediumKind.Air);
        }
    }

    private sealed class TransitPresentation(Task covered, Exception? cleanup = null, bool holdCleanup = false) : IAppLoadingPresentation
    {
        public bool Active => true;
        public bool IsCovered => covered.IsCompletedSuccessfully;
        public float Progress => 0;
        public List<Lease> Leases { get; } = [];
        public IAppLoadingLease Show(EnterRequest? request = null)
        {
            var lease = new Lease(covered, cleanup, holdCleanup);
            Leases.Add(lease);
            return lease;
        }
        public sealed class Lease(Task covered, Exception? cleanup, bool holdCleanup) : IAppLoadingLease
        {
            public TaskCompletionSource Releasing { get; } = new();
            public TaskCompletionSource Released { get; } = new();
            public Task Covered => covered;
            public void Report(int completed, int total) { }
            public Task Hide() => Task.CompletedTask;
            public async ValueTask DisposeAsync()
            {
                Releasing.TrySetResult();
                if (holdCleanup) await Released.Task;
                if (cleanup is not null) throw cleanup;
            }
        }
    }
}
