using Campus.Contracts.AppFlow;
using Campus.Contracts.Save;

namespace Campus.Kernel.Save;

/// <summary>Owns play time, automatic request safety and attempt spacing (SYS-SAVE-FLOW).</summary>
internal sealed class SaveSchedule(SaveService owner)
{
    internal const double IntervalSeconds = 300;
    internal const double SafetyDelaySeconds = 60;
    internal const double AttemptSpacingSeconds = 30;
    private readonly SaveService _owner = owner;
    private SaveReason? _request;
    private double _requestAge, _interval, _attemptAge = AttemptSpacingSeconds;
    private int _frame;
    private bool _saveAtDone;
    internal double PlaySeconds { get; private set; }

    internal void Tick(double delta)
    {
        _frame++;
        var dt = Math.Clamp(double.IsFinite(delta) ? delta : 0, 0, .1);
        _attemptAge = Math.Min(AttemptSpacingSeconds, _attemptAge + dt);
        if (_request.HasValue) _requestAge += dt;
        if (Playing && !_owner.Loading.Restoring && !_owner.Loading.RestoreFaulted)
        {
            PlaySeconds += dt; _interval += dt;
            _owner.Writing.Tick();
            if (_interval >= IntervalSeconds && _request is null) Request(SaveReason.Interval);
        }
        if (_owner.Options.SaveAtFrame is { } at && _frame >= at && !_saveAtDone && Safe)
        { _saveAtDone = true; _owner.Writing.StartSave(SaveReason.Test, forced: true, ownsFailure: true); }
        if (_request is { } reason && !_owner.Busy && _attemptAge >= AttemptSpacingSeconds &&
            (Safe || _requestAge >= SafetyDelaySeconds) && !_owner.Loading.Restoring &&
            Playing && _owner.Runtime.World.Instance != 0)
        {
            var forced = !Safe;
            // Keep an automatic request pending until a successful write; failures retry at the same spacing.
            _requestAge = 0;
            _owner.Writing.StartSave(reason, forced, ownsFailure: true);
        }
    }
    internal void ClearRequest() { _request = null; _requestAge = 0; }
    internal void Attempted() => _attemptAge = 0;
    internal void Saved() { _interval = 0; ClearRequest(); }
    internal void Restored(double playSeconds)
    {
        ClearRequest(); _interval = 0; _attemptAge = AttemptSpacingSeconds; PlaySeconds = playSeconds;
    }

    internal bool Playing => _owner.Runtime.AppFlow.IsPlaying;
    internal bool MapTransitioning => (_owner.Runtime.AppFlow.Activity & AppActivity.MapTransition) != 0;

    internal bool Safe
    {
        get
        {
            if (_owner.Loading.RestoreFaulted || _owner.Loading.Restoring || _owner.Runtime.World.Instance == 0 || !Playing || _owner.Runtime.Loading.Active || MapTransitioning) return false;
            foreach (var registration in _owner.Sections.Registered) if (!registration.Section.IsSettled) return false;
            return true;
        }
    }

    /// <summary>Timed saves are the 5-minute Interval (and the gate's Test); other reasons never schedule a save (SYS-SAVE-FLOW).</summary>
    internal void Request(SaveReason reason)
    {
        if (!Enum.IsDefined(reason)) throw new ArgumentOutOfRangeException(nameof(reason));
        if (reason is SaveReason.Quit or SaveReason.ToTitle) throw new ArgumentException("Use FlushForQuit.", nameof(reason));
        if (reason is not (SaveReason.Interval or SaveReason.Test)) return;
        if (_owner.Stopping || !_owner.Initialized || !_owner.Enabled || _owner.Active is null || _owner.Runtime.World.Instance == 0 || _owner.Loading.Restoring || _owner.Loading.RestoreFaulted ||
            !Playing || MapTransitioning) return;
        if (_request is null) _requestAge = 0;
        _request = reason;
    }

}
