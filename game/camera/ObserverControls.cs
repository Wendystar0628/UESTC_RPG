using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Settings;
using Godot;
using KernelServices = Campus.Kernel.Registry.Kernel;

namespace Campus.Camera;

/// <summary>Input routing, capture leases, held controls and persistent physical bindings.</summary>
internal sealed class ObserverControls(ObserverCamera camera, FlightTuning tuning)
{
    private sealed record Layer(long Id, string Owner, int Priority, Func<InputActionEvent, bool> Handler);
    private readonly List<Layer> _layers = [];
    private readonly InputLocks _locks = new();
    private readonly InputLocks _cursorRequests = new();
    private readonly InputBindings _bindings = new();
    private readonly ObserverMouseCapture _mouseCapture = new();
    private long _nextLayer;
    private float _wheel;
    private bool _automated;
    // Godot-local focus governs delivered input events, including formal injection on a private desktop.
    // Native desktop/focus in ObserverMouseCapture alone governs OS capture and recorded WindowFocused.
    private bool _focused = true;

    public ObserverInputState State { get; } = new(tuning);
    public bool Locked => _locks.Locked;
    public InputDesktopState DesktopState => _mouseCapture.DesktopState;
    public bool CursorVisible { get; private set; }
    public bool CursorHeld { get; private set; }
    public event Action<bool>? CursorVisibilityChanged;
    private bool CursorRequested => CursorHeld || _cursorRequests.Locked;

    public void Initialize(ISettingsStore? settings, bool automated)
    {
        _automated = automated;
        _bindings.Initialize(settings);
        RefreshCapture();
    }

    public IDisposable RegisterLayer(string owner, int priority, Func<InputActionEvent, bool> handler)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        ArgumentNullException.ThrowIfNull(handler);
        var layer = new Layer(++_nextLayer, owner, priority, handler);
        _layers.Add(layer);
        _layers.Sort((a, b) => a.Priority != b.Priority ? a.Priority.CompareTo(b.Priority) : b.Id.CompareTo(a.Id));
        return new LayerLease(this, layer.Id);
    }

    public IDisposable LockInput(InputLockReason reason)
    {
        camera.ClearInputMotion();
        return _locks.Acquire(reason);
    }

    public IDisposable RequestCursor(InputLockReason reason)
    {
        var lease = _cursorRequests.Acquire(reason);
        RefreshCapture();
        return new CursorLease(this, lease);
    }

    public void SetFocused(bool focused)
    {
        _focused = focused;
        if (!focused) CursorHeld = false;
        RefreshCapture();
    }

    public void ResetCursorHeld()
    {
        CursorHeld = false;
        RefreshCapture();
    }

    public void RefreshCapture()
    {
        bool visible = !_focused || CursorRequested;
        bool changed = CursorVisible != visible;
        CursorVisible = visible;
        // Published cursor policy is not permission to touch the OS cursor; input event delivery
        // and native capture eligibility are separate on an inactive desktop.
        _mouseCapture.Refresh(!visible);
        if (changed) CursorVisibilityChanged?.Invoke(visible);
    }

    public bool TryRebind(string action, int physicalKey, int physicalLocation, out string? conflict)
    {
        bool changed = _bindings.Rebind(action, physicalKey, physicalLocation, out conflict);
        if (changed)
        {
            State.Clear();
            camera.ResetFlightMotion();
            ResetCursorHeld();
            SaveBindings();
        }
        return changed;
    }

    public int PhysicalKey(string action) => action == "menu" ? (int)Key.Escape : _bindings.PhysicalKey(action);
    private void SaveBindings()
    {
        if (!_automated) _ = _bindings.Save(KernelServices.Get<ISettingsStore>());
    }

    public void ResetWheel() => _wheel = 0;

    public void ObserveInput(InputEvent input)
    {
        // _Input runs before GUI routing, so a control cannot swallow the cursor release.
        if (input.IsAction(InputBindings.CursorAction) && !input.IsEcho())
        {
            CursorHeld = input.IsActionPressed(InputBindings.CursorAction);
            RefreshCapture();
        }
        // Releases always clear held state, including when a newly opened GUI consumes the release.
        foreach (string action in InputBindings.Actions)
            if (input.IsActionReleased(action)) State.Release(action);
    }

    public bool HandleUnhandled(InputEvent input, IAppFlow? flow)
    {
        if (flow is null || flow.State is AppState.Boot or AppState.Entering or AppState.Leaving) return false;
        if (input is InputEventMouseMotion motion) return HandleLook(motion);
        if (input is InputEventMouseButton mouse && mouse.Pressed && camera.IsPlaying && !Locked)
        {
            if (HandleMouseButton(mouse)) return true;
        }
        if (input.IsAction("menu")) return HandleMenu(input);
        if (!camera.IsPlaying) return HandlePresentation(input, flow.State);
        return HandleActions(input);
    }

    private bool HandleLook(InputEventMouseMotion motion)
    {
        if (!camera.IsPlaying || CursorVisible || Locked) return false;
        camera.CancelTransitForInput();
        var relative = motion.ScreenRelative;
        var pose = camera.Pose with
        {
            YawDeg = WrapYaw(camera.Pose.YawDeg + relative.X * tuning.DegreesPerMouseCount),
            PitchDeg = Math.Clamp(camera.Pose.PitchDeg - relative.Y * tuning.DegreesPerMouseCount,
                -tuning.PitchLimit, tuning.PitchLimit)
        };
        camera.ApplyLook(pose);
        return true;
    }

    private bool HandleMouseButton(InputEventMouseButton mouse)
    {
        if (CursorVisible) return false;
        if (mouse.ButtonIndex is not (MouseButton.WheelUp or MouseButton.WheelDown)) return false;
        _wheel += mouse.Factor * (mouse.ButtonIndex == MouseButton.WheelUp ? 1 : -1);
        int steps = (int)_wheel;
        if (steps != 0)
        {
            _wheel -= steps;
            camera.ChangeSpeed(steps);
        }
        return true;
    }

    private bool HandleMenu(InputEvent input)
    {
        bool pressed = input.IsActionPressed("menu");
        return Dispatch(new("menu", pressed, input.IsEcho()));
    }

    private bool HandlePresentation(InputEvent input, AppState state)
    {
        if (state is not (AppState.Title or AppState.Quitting)) return false;
        foreach (string action in new[] { "ui_accept", "ui_select", "ui_focus_next", "ui_focus_prev", "ui_left", "ui_right", "ui_up", "ui_down" })
            if (input.IsAction(action) && Dispatch(new(action, input.IsActionPressed(action, true), input.IsEcho())))
                return true;
        return false;
    }

    private bool HandleActions(InputEvent input)
    {
        bool handled = false;
        foreach (string action in InputBindings.Actions)
        {
            if (!input.IsAction(action)) continue;
            if (action == InputBindings.CursorAction) return true;
            bool pressed = input.IsActionPressed(action, true);
            bool echo = input.IsEcho();
            if (Dispatch(new(action, pressed, echo))) return true;
            if (Locked || !pressed || echo) continue;
            var (consumed, doubleSpace) = State.Press(action);
            if (action == "toggle_mode" || doubleSpace && camera.CanDoubleSpace())
            {
                camera.RequestMode(camera.Mode switch
                {
                    ViewMode.Fly => ViewMode.Walk,
                    ViewMode.Walk => ViewMode.Fly,
                    _ => throw new InvalidOperationException("Unknown view mode.")
                }, doubleSpace ? ModeSwitchVia.DoubleSpace : ModeSwitchVia.Key);
                return true;
            }
            if (!consumed) continue;
            if (action != "recover") camera.CancelTransitForInput();
            handled = true;
        }
        return handled;
    }

    private bool Dispatch(InputActionEvent action)
    {
        // Snapshot registrations because handling an action may open or close a page.
        foreach (var layer in _layers.ToArray())
            if (_layers.Contains(layer) && layer.Handler(action)) return true;
        return false;
    }

    private static float WrapYaw(float yaw) => (yaw % 360 + 360) % 360;

    private sealed class CursorLease(ObserverControls owner, IDisposable lease) : IDisposable
    {
        private IDisposable? _lease = lease;
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _lease, null) is not { } held) return;
            held.Dispose();
            owner.RefreshCapture();
        }
    }

    private sealed class LayerLease(ObserverControls owner, long id) : IDisposable
    {
        private ObserverControls? _owner = owner;
        public void Dispose() => Interlocked.Exchange(ref _owner, null)?._layers.RemoveAll(layer => layer.Id == id);
    }
}
