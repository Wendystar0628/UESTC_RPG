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

    private sealed class TransitPresentation(Task covered, Exception? cleanup = null) : IAppLoadingPresentation
    {
        public bool Active => true;
        public bool IsCovered => covered.IsCompletedSuccessfully;
        public float Progress => 0;
        public IAppLoadingLease Show(EnterRequest? request = null) => new Lease(covered, cleanup);
        private sealed class Lease(Task covered, Exception? cleanup) : IAppLoadingLease
        {
            public Task Covered => covered;
            public void Report(int completed, int total) { }
            public Task Hide() => Task.CompletedTask;
            public ValueTask DisposeAsync() => cleanup is null ? ValueTask.CompletedTask : new(Task.FromException(cleanup));
        }
    }
}
