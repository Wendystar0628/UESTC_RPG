using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Kernel.Frame;

namespace Campus.Kernel.Inspect;

/// <summary>Dictionary-owned flags and settling durations. Missing command-owner coverage stays explicit.</summary>
internal sealed class PerfContextCapture
{
    public static readonly string[] MissingCoverage =
    [
        "IAppFlow: enter request kind/slot and rejection are not published",
        "IMapService: prepare/travel request, begin and completion are not published",
        "ICameraRig: transit request target/rejection and input-lock reasons are not published; fly speed is a setting, not velocity",
        "IWorldState: clock/season/weather/flow command requests and rejection are not published",
        "IQualityService: Changed does not publish apply begin, terminal result or rejection",
        "ISaveService: load/new-game request and terminal result are not published",
        "IObserverState/IWalkController: mode/space changes have no owner notification; observed at published frame"
    ];
    private readonly Dictionary<string, long> _flags = new();
    private readonly Dictionary<string, double> _settling = new();
    private readonly HashSet<AppState> _appStates = [];
    private readonly int _resumeFrames;
    private readonly bool _requiredOwnerBoundariesAttached;
    private bool? _couldDraw;
    private int _resumeRemaining;
    private double _settleUntil, _qualityUntil;
    public ulong? ThumbnailFrame { get; set; }

    public PerfContextCapture(PerfFieldDictionary dictionary, bool requiredOwnerBoundariesAttached)
    {
        _requiredOwnerBoundariesAttached = requiredOwnerBoundariesAttached;
        foreach (var flag in dictionary.Snapshot["contextFlags"]!.AsArray())
            _flags.Add(flag!["name"]!.GetValue<string>(), flag["value"]!.GetValue<long>());
        var rules = dictionary.Snapshot["nonSteadyRules"]!;
        foreach (var state in rules["appStates"]!.AsArray()) _appStates.Add(Enum.Parse<AppState>(state!.GetValue<string>(), true));
        foreach (var rule in rules["rules"]!.AsArray())
        {
            string name = rule!["name"]!.GetValue<string>();
            _settling.Add(name, rule["settleMs"]!.GetValue<double>());
            if (name == "drawResume") _resumeFrames = rule["settleFrames"]!.GetValue<int>();
        }
    }

    public void Settle(string rule)
    {
        double until = FrameClock.MonotonicMilliseconds + _settling[rule];
        if (rule == "qualityApply") _qualityUntil = Math.Max(_qualityUntil, until);
        else _settleUntil = Math.Max(_settleUntil, until);
    }

    public (long Flags, bool Complete) Capture(in FrameSample frame, bool loading, WorldTransition transition, bool arriving)
    {
        long bits = (long)frame.Context.Activity;
        if (_appStates.Contains(frame.Context.State)) bits |= Flag("notPlaying");
        if (loading) bits |= Flag("loadingVisible");
        if (frame.Context.WindowFocused == false) bits |= Flag("windowUnfocused");
        if ((transition & (WorldTransition.Time | WorldTransition.Season)) != 0 || arriving)
            bits |= Flag("worldOrArrivalTransition");
        if (frame.MonotonicMilliseconds < _settleUntil) bits |= Flag("transitionSettling");
        if (frame.MonotonicMilliseconds < _qualityUntil) bits |= Flag("qualitySettling");
        if (ThumbnailFrame == frame.CollectionFrame) bits |= Flag("captureFrame");
        if (_couldDraw == false && frame.Context.WindowCanDraw) _resumeRemaining = _resumeFrames;
        _couldDraw = frame.Context.WindowCanDraw;
        if (_resumeRemaining > 0 && frame.Context.WindowCanDraw)
        {
            bits |= Flag("drawResuming");
            _resumeRemaining--;
        }
        // Optional world-command and input-lock telemetry does not gate non-steady classification.
        return (bits, _requiredOwnerBoundariesAttached);
    }
    private long Flag(string name) => _flags[name];
}
