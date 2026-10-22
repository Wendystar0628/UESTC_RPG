using Campus.Camera;
using Campus.Contracts;
using Campus.Contracts.AppFlow;
using GdUnit4;
using Godot;

namespace Campus.Tests;

[TestSuite]
public sealed class ObserverCameraRuntimeTests
{
    [TestCase, TestCategory(TestReason.Regression), RequireGodotRuntime]
    public void CursorLeasesAndRawHeldEventsRemainIndependentOfMovementLocks()
    {
        var originalMouseMode = Input.MouseMode;
        var camera = new ObserverCamera();
        var controls = new ObserverControls(camera, camera.Tuning);
        var visibilityChanges = new List<bool>();
        controls.CursorVisibilityChanged += visibilityChanges.Add;
        try
        {
            controls.Initialize(null, automated: true);
            controls.SetFocused(true);
            if (controls.CursorHeld || controls.CursorVisible || controls.Locked)
                throw new InvalidOperationException("The focused cursor owner did not start without held actions or leases.");
            var binding = InputMap.ActionGetEvents(InputBindings.CursorAction).OfType<InputEventKey>().Single();
            using var down = new InputEventKey
            {
                PhysicalKeycode = binding.PhysicalKeycode, Location = binding.Location, Pressed = true
            };
            using var up = new InputEventKey
            {
                PhysicalKeycode = binding.PhysicalKeycode, Location = binding.Location, Pressed = false
            };
            controls.ObserveInput(down);
            if (!controls.CursorHeld || !controls.CursorVisible || controls.Locked)
                throw new InvalidOperationException("The raw cursor press did not show a cursor independently of movement locks.");
            using var panel = controls.RequestCursor(InputLockReason.Menu);
            using var secondPanel = controls.RequestCursor(InputLockReason.Menu);
            using var page = controls.RequestCursor(InputLockReason.Atlas);
            controls.ObserveInput(up); // Release reaches _Input even when the GUI consumes it later.
            panel.Dispose(); panel.Dispose(); page.Dispose();
            if (controls.CursorHeld || !controls.CursorVisible || controls.Locked)
                throw new InvalidOperationException("A release or repeated disposal removed another same-reason cursor lease.");
            secondPanel.Dispose();
            // The owner publishes capture policy; a headless display need not capture a native mouse.
            if (controls.CursorVisible)
                throw new InvalidOperationException("The final cursor lease did not restore the owner's capture policy.");
            using (controls.LockInput(InputLockReason.Menu))
                if (!controls.Locked || controls.CursorVisible)
                    throw new InvalidOperationException("Movement locks were conflated with cursor visibility.");
            if (controls.Locked) throw new InvalidOperationException("Movement input remained locked after its lease ended.");

            controls.ObserveInput(down);
            controls.SetFocused(false);
            if (controls.CursorHeld || !controls.CursorVisible)
                throw new InvalidOperationException("Focus loss did not clear the held cursor action and release capture.");
            controls.SetFocused(true);
            if (controls.CursorVisible)
                throw new InvalidOperationException("Returning focus restored a stale cursor press.");
            if (!visibilityChanges.SequenceEqual(new[] { true, false, true, false }))
                throw new InvalidOperationException("The cursor owner did not publish its actual visibility transitions.");
        }
        finally
        {
            // This isolated owner regression does not activate or refocus an OS window.
            controls.CursorVisibilityChanged -= visibilityChanges.Add;
            try { controls.SetFocused(false); }
            finally
            {
                try { camera.Free(); }
                finally { Input.MouseMode = originalMouseMode; }
            }
        }
    }

    [TestCase, TestCategory(TestReason.Regression), RequireGodotRuntime]
    public void NativeCursorBindingDistinguishesKeyLocationFromModifierFlags()
    {
        var binding = InputMap.ActionGetEvents(InputBindings.CursorAction).OfType<InputEventKey>().Single();
        using var sameKey = new InputEventKey
        {
            PhysicalKeycode = binding.PhysicalKeycode, Location = binding.Location, Pressed = true
        };
        if (!sameKey.IsActionPressed(InputBindings.CursorAction))
            throw new InvalidOperationException("The actual physical cursor binding did not match its native action.");
        using var modifierCombination = new InputEventKey
        {
            PhysicalKeycode = Key.F, AltPressed = true, Pressed = true
        };
        if (modifierCombination.IsActionPressed(InputBindings.CursorAction))
            throw new InvalidOperationException("An unrelated physical key's Alt modifier activated the cursor action.");
        if (binding.Location != KeyLocation.Unspecified)
        {
            using var oppositeSide = new InputEventKey
            {
                PhysicalKeycode = binding.PhysicalKeycode, Pressed = true,
                Location = binding.Location == KeyLocation.Left ? KeyLocation.Right : KeyLocation.Left
            };
            if (oppositeSide.IsActionPressed(InputBindings.CursorAction))
                throw new InvalidOperationException("The opposite key location matched a sided cursor binding.");
        }
    }

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
