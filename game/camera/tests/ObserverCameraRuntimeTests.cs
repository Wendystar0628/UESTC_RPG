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
    public void WindowFocusNotificationsAloneDriveTheCursorAfterAnUnfocusedStart()
    {
        var originalMouseMode = Input.MouseMode;
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
            try { camera.Free(); }
            finally { Input.MouseMode = originalMouseMode; }
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
}
