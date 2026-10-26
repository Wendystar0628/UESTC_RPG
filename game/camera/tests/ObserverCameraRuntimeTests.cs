using Campus.Camera;
using Campus.Camera.Walk;
using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Settings;
using Campus.Kernel.Settings;
using GdUnit4;
using Godot;
using KernelServices = Campus.Kernel.Registry.Kernel;

namespace Campus.Tests;

[TestSuite]
public sealed class ObserverCameraRuntimeTests
{
    [TestCase, TestCategory(TestReason.Regression), RequireGodotRuntime]
    public async Task UnhandledActionsOnlyStopAtTheirActualCameraOrRouterConsumer()
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        var flow = new TestAppFlow();
        var rules = new TestRuleset(defaultViewMode: ViewMode.Walk);
        var failure = new TestAppFailure();
        await using var settings = new SettingsStore(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json"));
        KernelServices.Services.Add<IAppFlow>(flow);
        KernelServices.Services.Add<IRuleset>(rules);
        KernelServices.Services.Add<IAppFailure>(failure);
        KernelServices.Services.Add<IWorldState>(new TestWorldState(new TestWeatherProcess(), rules));
        KernelServices.Services.Add<ITerrainPreparation>(new FlowTerrainPreparation());
        KernelServices.Services.Add<ITransitDriver>(new FlowTransitDriver());
        KernelServices.Services.Add<ISettingsStore>(settings);
        var camera = new ObserverCamera { ProcessMode = Node.ProcessModeEnum.Disabled };
        var controls = new ObserverControls(camera, camera.Tuning);
        try
        {
            tree.Root.AddChild(camera);
            controls.Initialize(null, automated: true);
            if (!camera.IsPlaying || camera.Mode != ViewMode.Walk)
                throw new InvalidOperationException("The regression did not enter the playing camera input branch.");
            foreach (string action in InputBindings.Actions)
            {
                if (action is "toggle_mode" || action == InputBindings.CursorAction) continue;
                var binding = InputMap.ActionGetEvents(action).OfType<InputEventKey>().Single();
                using var down = new InputEventKey
                { PhysicalKeycode = binding.PhysicalKeycode, Location = binding.Location, Pressed = true };
                using var up = new InputEventKey
                { PhysicalKeycode = binding.PhysicalKeycode, Location = binding.Location, Pressed = false };
                if (!down.IsActionPressed(action))
                    throw new InvalidOperationException("The physical binding did not reach its configured action.");
                bool expected = new ObserverInputState(camera.Tuning).Press(action).Consumed;
                if (action is "map" or "atlas_index" or "minimap" && expected)
                    throw new InvalidOperationException("A map action entered camera-owned input state.");
                controls.ObserveInput(down);
                if (controls.HandleUnhandled(down, flow) != expected)
                    throw new InvalidOperationException($"Camera consumption did not match its actual action: {action}");
                if (action == "move_forward" && controls.State.Move.Y <= 0)
                    throw new InvalidOperationException("The camera stopped accepting forward input.");
                controls.ObserveInput(up);
                if (controls.HandleUnhandled(up, flow) || controls.State.Move != System.Numerics.Vector2.Zero)
                    throw new InvalidOperationException("A release was swallowed or left movement held.");
            }
            using var move = new InputEventAction { Action = "move_forward", Pressed = true };
            using var map = new InputEventAction { Action = "map", Pressed = true };
            using (controls.LockInput(InputLockReason.Menu))
                if (controls.HandleUnhandled(move, flow) || controls.HandleUnhandled(map, flow)
                    || controls.State.Move != System.Numerics.Vector2.Zero)
                    throw new InvalidOperationException("Locked input entered camera state or consumed a foreign action.");
            bool lowerReached = false;
            using (controls.RegisterLayer("lower", 10, _ => { lowerReached = true; return false; }))
            using (controls.RegisterLayer("upper", 0, _ => true))
                if (!controls.HandleUnhandled(move, flow) || !controls.HandleUnhandled(map, flow)
                    || lowerReached || controls.State.Move != System.Numerics.Vector2.Zero)
                    throw new InvalidOperationException("A router-consumed action propagated or entered camera state.");
            using var mode = new InputEventAction { Action = "toggle_mode", Pressed = true };
            if (!controls.HandleUnhandled(mode, flow) || camera.Mode != ViewMode.Fly || failure.Errors.Count != 0)
                throw new InvalidOperationException("The camera stopped consuming and applying its mode action.");
        }
        finally
        {
            controls.SetFocused(false);
            camera.PrepareForExit();
            await camera.DrainForExitAsync();
            camera.Free();
        }
    }

    // Guards two facts: the camera's physics priority precedes the walk body's, and WalkLocomotion's
    // jump latch consumes one press exactly once whatever the physics steps per frame.
    // 相机提交路径由核心专项 CoreLifecycleDriver.ExerciseInput 的真实组合覆盖。
    [TestCase, TestCategory(TestReason.Regression), RequireGodotRuntime]
    public void CameraPhysicsPrecedesWalkAndLocomotionLatchesEachJumpPressOnceWhateverTheStepsPerFrame()
    {
        var camera = new ObserverCamera();
        var walk = new WalkController();
        var body = new CharacterBody3D();
        try
        {
            if (camera.ProcessPhysicsPriority >= walk.ProcessPhysicsPriority)
                throw new InvalidOperationException("Walk input is committed after the walk body's physics step that should use it.");
            var controls = new ObserverControls(camera, camera.Tuning);
            var locomotion = new WalkLocomotion(new WalkBodyCollision(body));
            var map = FlightTestSupport.CreateContext();
            var collision = new StreamLifecycleTestSupport.Collision();
            float dt = 1f / Engine.PhysicsTicksPerSecond;
            int Frame(int steps)
            {
                int jumps = 0;
                for (int step = 0; step < steps; step++)
                {
                    locomotion.SetInput(controls.TakeWalkInput());
                    locomotion.AdvanceJumpClock(dt);
                    locomotion.PrepareInput(dt, true, false, default);
                    if (locomotion.Integrate(dt, true, false, default, 0, default, default, map, collision, null).Jumped) jumps++;
                }
                return jumps;
            }
            controls.State.Press("jump");
            if (Frame(3) != 1) throw new InvalidOperationException("A held press in a multi-step frame did not jump exactly once.");
            controls.State.Release("jump");
            locomotion.ResetPlacement();
            controls.State.Press("jump");
            int zeroStep = Frame(0);
            controls.State.Release("jump");
            if (zeroStep != 0 || Frame(1) != 1) throw new InvalidOperationException("A tap in a frame without physics steps was lost.");
            locomotion.ResetPlacement();
            controls.State.Press("jump");
            controls.State.Release("jump");
            if (Frame(1) != 1 || Frame(1) != 0) throw new InvalidOperationException("A tap did not start exactly one jump.");
            locomotion.ResetPlacement();
            controls.State.Press("jump");
            if (Frame((int)(WalkTuning.HeldJumpInterval / dt) - 1) != 1)
                throw new InvalidOperationException("Holding jump repeated it before the held-jump interval.");
        }
        finally
        {
            body.Free();
            walk.Free();
            camera.Free();
        }
    }

    [TestCase, TestCategory(TestReason.Regression), RequireGodotRuntime]
    public void CursorLeasesAndRawHeldEventsRemainIndependentOfMovementLocks()
    {
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
            finally { camera.Free(); }
        }
    }

    [TestCase, TestCategory(TestReason.Regression), RequireGodotRuntime]
    public void WindowFocusNotificationsDriveCursorPolicyAfterAnUnfocusedStart()
    {
        var camera = new ObserverCamera();
        try
        {
            // The deferred first activation leaves the entry snapshot unfocused and sends no application focus-in.
            camera.ApplyWindowFocus(false);
            if (!camera.CursorVisible)
                throw new InvalidOperationException("The unfocused start did not show the cursor.");
            camera.Notification((int)Node.NotificationWMWindowFocusIn);
            if (camera.CursorVisible)
                throw new InvalidOperationException("Window focus-in did not return the cursor to capture.");
            var binding = InputMap.ActionGetEvents(InputBindings.CursorAction).OfType<InputEventKey>().Single();
            using var down = new InputEventKey
            {
                PhysicalKeycode = binding.PhysicalKeycode, Location = binding.Location, Pressed = true
            };
            camera._Input(down);
            camera.Notification((int)Node.NotificationWMWindowFocusOut);
            if (!camera.CursorVisible || camera.CursorHeld)
                throw new InvalidOperationException("Window focus-out did not release the cursor and clear its held action.");
        }
        finally
        {
            try { camera.ApplyWindowFocus(false); }
            finally { camera.Free(); }
        }
    }

    [TestCase, TestCategory(TestReason.Regression), TestCategory("NativeRender"), RequireGodotRuntime]
    public void InactiveDesktopCannotCaptureTheSessionCursor()
    {
        // The NativeRender runner creates a real window on its private desktop. Do not substitute
        // headless or fabricate the OS eligibility: this is the original failure's native condition.
        if (DisplayServer.GetName() != "Windows" || !DisplayServer.WindowIsFocused())
            throw new InvalidOperationException("The capture regression requires a locally focused native Windows window.");
        var capture = new ObserverMouseCapture();
        if (capture.IsInputDesktop)
            throw new InvalidOperationException("The capture regression must run on the isolated, inactive desktop.");
        if (Input.MouseMode != Input.MouseModeEnum.Visible)
            throw new InvalidOperationException("The isolated runner must start without native capture.");
        var camera = new ObserverCamera();
        var controls = new ObserverControls(camera, camera.Tuning);
        try
        {
            controls.Initialize(null, automated: true);
            controls.SetFocused(true);
            if (controls.CursorVisible || Input.MouseMode != Input.MouseModeEnum.Visible)
                throw new InvalidOperationException("The inactive desktop must keep look input enabled without capturing the session cursor.");
            using (controls.RequestCursor(InputLockReason.Menu))
                if (!controls.CursorVisible || Input.MouseMode != Input.MouseModeEnum.Visible)
                    throw new InvalidOperationException("A background cursor lease changed native capture.");
            controls.RefreshCapture();
            if (controls.CursorVisible || Input.MouseMode != Input.MouseModeEnum.Visible)
                throw new InvalidOperationException("Releasing a background cursor lease acquired native capture.");
        }
        finally
        {
            try { controls.SetFocused(false); }
            finally { camera.Free(); }
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
        var context = FlightTestSupport.CreateContext();
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
}
