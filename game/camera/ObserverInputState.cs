using System.Numerics;

namespace Campus.Camera;

/// <summary>Only unhandled presses enter this state; releases are always observed to prevent stuck keys.</summary>
public sealed class ObserverInputState(FlightTuning tuning)
{
    private readonly HashSet<string> _held = new(StringComparer.Ordinal);
    private float _forwardGap = float.MaxValue;
    private float _spaceGap = float.MaxValue;
    private float _spaceHeld;
    private float _modeCooldown;
    private bool _spaceWasShort;
    private bool _doubleSprint;
    private bool _jump;
    public bool SprintToggle { get; set; }
    private bool _sprintLatched;
    public float RecoverHeld { get; private set; }
    public bool Sprint => _doubleSprint || (SprintToggle ? _sprintLatched : Held("sprint"));
    public bool JumpHeld => Held("jump") || Held("move_up");
    public bool Crouch => Held("crouch") || Held("move_down");
    public Vector2 Move => new((Held("move_right") ? 1 : 0) - (Held("move_left") ? 1 : 0),
        (Held("move_forward") ? 1 : 0) - (Held("move_backward") ? 1 : 0));
    public float Vertical => (Held("move_up") ? 1 : 0) - (Held("move_down") ? 1 : 0);

    public void Advance(float seconds)
    {
        _forwardGap = Math.Min(10, _forwardGap + seconds);
        _spaceGap = Math.Min(10, _spaceGap + seconds);
        _modeCooldown = Math.Max(0, _modeCooldown - seconds);
        if (Held("move_up")) _spaceHeld += seconds;
        RecoverHeld = Held("recover") ? RecoverHeld + seconds : 0;
        if (Move == Vector2.Zero) _sprintLatched = false;
    }

    public bool Press(string action)
    {
        if (!_held.Add(action)) return false;
        if (action == "move_forward")
        { _doubleSprint = _forwardGap <= tuning.DoublePressSeconds; _forwardGap = 0; }
        if (action == "sprint" && SprintToggle) _sprintLatched = !_sprintLatched;
        if (action == "jump") _jump = true;
        if (action != "move_up") return false;
        bool doubleSpace = _spaceWasShort && _modeCooldown <= 0 && _spaceGap <= tuning.DoubleSpaceSeconds;
        _spaceGap = doubleSpace ? 10 : 0;
        _spaceHeld = 0;
        _spaceWasShort = false;
        if (doubleSpace) _modeCooldown = tuning.ModeSwitchCooldown;
        return doubleSpace;
    }

    public void Release(string action)
    {
        _held.Remove(action);
        if (action == "move_forward") _doubleSprint = false;
        if (action == "recover") RecoverHeld = 0;
        if (action == "move_up") _spaceWasShort = _spaceHeld < tuning.SpaceTapSeconds;
    }

    public bool TakeJump() { bool value = _jump; _jump = false; return value; }
    public IReadOnlyList<string> CaptureHeldActions() => _held.Order(StringComparer.Ordinal).ToArray();
    public void Clear()
    {
        _held.Clear(); _doubleSprint = false; _sprintLatched = false; _jump = false;
        RecoverHeld = 0; _forwardGap = 10; _spaceGap = 10;
        _spaceWasShort = false; _spaceHeld = 0;
    }
    private bool Held(string action) => _held.Contains(action);
}
