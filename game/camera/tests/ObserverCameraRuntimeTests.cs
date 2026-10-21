using Campus.Camera;
using Campus.Contracts;
using Campus.Contracts.AppFlow;
using GdUnit4;
using Godot;

namespace Campus.Tests;

[TestSuite]
public sealed class ObserverCameraRuntimeTests
{
    [TestCase]
    [TestCategory(TestReason.Regression)]
    [TestCategory("NativeRender")]
    [RequireGodotRuntime]
    public void MapMountKeepsPresentationPoseAndDoesNotRequireAnInlineSpawn()
    {
        if (Engine.GetMainLoop() is not SceneTree)
            throw new InvalidOperationException("The camera map-mount regression requires a real Godot scene tree.");
        if (OS.GetThreadCallerId() != OS.GetMainThreadId())
            throw new InvalidOperationException("The camera map-mount regression must run on Godot's main thread.");

        // Empty inline Spawns is valid when DefaultSpawn names a separate waypoint.
        var context = FlightBoundsTests.CreateContext();
        var camera = new ObserverCamera();
        try
        {
            // Invoke the production callback against native Node/RenderingServer resources.
            // This isolated regression does not enter _Ready or claim full startup coverage.
            var presentation = new Pose(new(400, 500, 3100), 13, -2.5f, 38);
            camera.SetPose(presentation);
            camera.OnMapLoaded(context);
            if (camera.Pose != presentation)
                throw new InvalidOperationException("MapAware replaced the presentation pose before MapService selected the destination.");
        }
        finally
        {
            if (GodotObject.IsInstanceValid(camera)) camera.Free();
        }
    }

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
