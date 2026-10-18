using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Camera.Walk;
using Godot;

namespace Campus.Camera;

public partial class ObserverCamera
{
    private sealed record Layer(long Id, string Owner, int Priority, Func<InputActionEvent, bool> Handler);
    private readonly List<Layer> _layers = [];
    private long _nextLayer;
    private float _wheel;
    private bool _menuHeld;
    private float _menuSeconds;
    private bool _skipSent;
    private bool _skipHintVisible;
    // ISkipInputState: hold-menu skip of the Entering sequence.
    public float Progress => _flow?.State == AppState.Entering && _menuHeld
        ? Math.Clamp(_menuSeconds / Tuning.SkipHoldSeconds, 0, 1) : 0;
    public bool HintVisible => _flow?.State == AppState.Entering && _skipHintVisible;

    public IDisposable RegisterLayer(string owner, int priority, Func<InputActionEvent, bool> handler)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        ArgumentNullException.ThrowIfNull(handler);
        var layer = new Layer(++_nextLayer, owner, priority, handler);
        _layers.Add(layer);
        _layers.Sort((a, b) => a.Priority != b.Priority ? a.Priority.CompareTo(b.Priority) : b.Id.CompareTo(a.Id));
        return new LayerLease(this, layer.Id);
    }

    public override void _Input(InputEvent input)
    {
        if (_flow?.State == AppState.Entering) _skipHintVisible = true;
        // Releases always clear held state, including when a newly opened GUI consumes the release.
        foreach (string action in InputBindings.Actions)
            if (input.IsActionReleased(action)) _input.Release(action);
        if (input.IsActionReleased("menu")) ResetSkipHold();
    }

    public override void _UnhandledInput(InputEvent input)
    {
        if (_flow is null) return;
        if (_flow.State == AppState.Entering)
        {
            if (input.IsActionPressed("menu")) { _menuHeld = true; GetViewport().SetInputAsHandled(); }
            return;
        }
        if (input is InputEventMouseMotion motion)
        {
            if (!IsPlaying || InterfaceMode || _locks.Locked) return;
            CancelTransitForInput();
            var relative = motion.ScreenRelative;
            var pose = Pose with
            {
                YawDeg = WrapYaw(Pose.YawDeg + relative.X * Tuning.DegreesPerMouseCount),
                PitchDeg = Math.Clamp(Pose.PitchDeg - relative.Y * Tuning.DegreesPerMouseCount,
                    -Tuning.PitchLimit, Tuning.PitchLimit)
            };
            switch (Mode)
            {
                case ViewMode.Fly: _pose = pose; break;
                case ViewMode.Walk: _walk?.SetLook(pose.YawDeg, pose.PitchDeg); break;
                default: throw new InvalidOperationException("Unknown view mode.");
            }
            GetViewport().SetInputAsHandled();
            return;
        }
        if (input is InputEventMouseButton mouse && mouse.Pressed && IsPlaying && !_locks.Locked)
        {
            if (mouse.ButtonIndex == MouseButton.Left)
            {
                if (!Dispatch(new("scene_click", true, false))) SetInterfaceMode(false);
                GetViewport().SetInputAsHandled(); return;
            }
            if (mouse.ButtonIndex is MouseButton.WheelUp or MouseButton.WheelDown)
            {
                _wheel += mouse.Factor * (mouse.ButtonIndex == MouseButton.WheelUp ? 1 : -1);
                int steps = (int)_wheel;
                if (steps != 0) { _wheel -= steps; ChangeSpeed(steps); }
                GetViewport().SetInputAsHandled(); return;
            }
        }
        if (input.IsAction("menu"))
        {
            bool pressed = input.IsActionPressed("menu");
            if (Dispatch(new("menu", pressed, input.IsEcho()))) { GetViewport().SetInputAsHandled(); return; }
            if (pressed && IsPlaying && !_locks.Locked)
            { SetInterfaceMode(!InterfaceMode); GetViewport().SetInputAsHandled(); }
            return;
        }
        if (!IsPlaying)
        {
            if (_flow.State is AppState.Title or AppState.Quitting)
                foreach (string action in new[] { "ui_accept", "ui_select", "ui_focus_next", "ui_focus_prev", "ui_left", "ui_right", "ui_up", "ui_down" })
                    if (input.IsAction(action) && Dispatch(new(action, input.IsActionPressed(action, true), input.IsEcho())))
                    { GetViewport().SetInputAsHandled(); break; }
            return;
        }
        bool handled = false;
        foreach (string action in InputBindings.Actions)
        {
            if (!input.IsAction(action)) continue;
            bool pressed = input.IsActionPressed(action, true);
            bool echo = input.IsEcho();
            if (Dispatch(new(action, pressed, echo))) { handled = true; break; }
            if (_locks.Locked || !pressed || echo) continue;
            bool doubleSpace = _input.Press(action);
            if (action == "toggle_mode" || doubleSpace && CanDoubleSpace())
            {
                _ = SetMode(Mode switch
                {
                    ViewMode.Fly => ViewMode.Walk,
                    ViewMode.Walk => ViewMode.Fly,
                    _ => throw new InvalidOperationException("Unknown view mode.")
                }, doubleSpace ? ModeSwitchVia.DoubleSpace : ModeSwitchVia.Key);
                handled = true; break;
            }
            if (action.StartsWith("move_", StringComparison.Ordinal) || action is "jump" or "crouch" or "sprint")
                CancelTransitForInput();
            handled = true;
        }
        if (handled) GetViewport().SetInputAsHandled();
    }

    public override void _Notification(int what)
    {
        if (what != NotificationApplicationFocusOut) return;
        _input.Clear(); _flight.Reset(); _walk?.SetInput(default);
        ResetSkipHold();
        SetInterfaceMode(true);
    }

    private void ResetSkipHold() { _menuHeld = false; _menuSeconds = 0; _skipSent = false; }

    private bool Dispatch(InputActionEvent action)
    {
        // Inactive registered layers return false. The first active consumer stops propagation;
        // snapshot registrations because handling an action may open or close a page.
        foreach (var layer in _layers.ToArray())
            if (_layers.Contains(layer) && layer.Handler(action)) return true;
        return false;
    }
    private void AdvanceInput(float seconds)
    {
        _input.Advance(seconds);
        if (_flow?.State == AppState.Entering && _menuHeld && !_skipSent)
        {
            _menuSeconds += seconds;
            if (_menuSeconds >= Tuning.SkipHoldSeconds && _flow.Skippable) { _skipSent = true; _flow.Skip(); }
        }
    }
    private bool CanDoubleSpace() => _map?.World.Spatial.MediumAt(Pose.Position).Kind != MediumKind.Underwater
        && (Mode switch
        {
            ViewMode.Fly => true,
            ViewMode.Walk => _walk is { CollisionReady: true },
            _ => throw new InvalidOperationException("Unknown view mode.")
        });
    private void ChangeSpeed(int steps)
    {
        switch (Mode)
        {
            case ViewMode.Fly: _flySpeedStep = Math.Clamp(_flySpeedStep + steps, 1, Tuning.SpeedSteps); break;
            case ViewMode.Walk:
                _walk?.SetSpeedStep(Math.Clamp(SpeedStep + steps, 0, WalkTuning.SpeedStepCount - 1)); break;
            default: throw new InvalidOperationException("Unknown view mode.");
        }
    }
    private void CancelTransitForInput()
    {
        ActiveTransit?.Cancel(); ActiveTransit = null; _transitDestination = null;
    }
    private static float WrapYaw(float yaw) => (yaw % 360 + 360) % 360;
    private sealed class LayerLease(ObserverCamera owner, long id) : IDisposable
    {
        private ObserverCamera? _owner = owner;
        public void Dispose() => Interlocked.Exchange(ref _owner, null)?._layers.RemoveAll(layer => layer.Id == id);
    }
}
