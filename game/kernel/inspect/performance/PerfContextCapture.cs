using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Maps;
using Campus.Contracts.Quality;
using Campus.Contracts.Save;
using Campus.Kernel.Frame;

namespace Campus.Kernel.Inspect;

/// <summary>Dictionary-owned flags and settling durations; command intervals are scoped to their owner and operation.</summary>
internal sealed class PerfContextCapture
{
    private readonly record struct OperationState(string Rule, bool Active, bool Started);
    private readonly Dictionary<string, long> _flags = new();
    private readonly Dictionary<string, double> _settling = new();
    private readonly Dictionary<string, long> _ruleFlags = new();
    private readonly Dictionary<string, string> _eventRules = new();
    private readonly Dictionary<(string Owner, long Operation), OperationState> _operations = new();
    private readonly HashSet<AppState> _appStates = [];
    private readonly HashSet<ulong> _thumbnailFrames = [];
    private readonly int _resumeFrames;
    private bool? _couldDraw;
    private int _resumeRemaining;
    private double _settleUntil, _qualityUntil;
    public string[] RequiredCoverage { get; }
    public string[] RequiredMissing { get; }
    public string[] NotAttached { get; }

    public PerfContextCapture(PerfFieldDictionary dictionary)
    {
        foreach (var flag in dictionary.Snapshot["contextFlags"]!.AsArray())
            _flags.Add(flag!["name"]!.GetValue<string>(), flag["value"]!.GetValue<long>());
        var rules = dictionary.Snapshot["nonSteadyRules"]!;
        RequiredCoverage = rules["requiredCoverage"]!.AsArray().Select(item => item!.GetValue<string>()).ToArray();
        RequiredMissing = [];
        NotAttached = rules["notAttached"]!.AsArray().Select(item => item!.GetValue<string>()).ToArray();
        foreach (var state in rules["appStates"]!.AsArray()) _appStates.Add(Enum.Parse<AppState>(state!.GetValue<string>(), true));
        foreach (var rule in rules["rules"]!.AsArray())
        {
            string name = rule!["name"]!.GetValue<string>();
            _settling.Add(name, rule["settleMs"]!.GetValue<double>());
            foreach (var eventName in rule["events"]!.AsArray()) _eventRules.Add(eventName!.GetValue<string>(), name);
            if (rule["activeFlag"] is { } activeFlag) _ruleFlags.Add(name, Flag(activeFlag.GetValue<string>()));
            if (name == "drawResume") _resumeFrames = rule["settleFrames"]!.GetValue<int>();
        }
    }

    public bool Observe(in EnterCommandEvent value, double time) => value.Phase switch
    {
        EnterCommandPhase.Started => Open("game.enter", value.Operation, "newGameOrLoad", true, true),
        EnterCommandPhase.Rejected => Close("game.enter", value.Operation, time, false),
        EnterCommandPhase.Completed => Close("game.enter", value.Operation, time, true),
        EnterCommandPhase.Faulted => Close("game.enter", value.Operation, time, true),
        _ => throw new ArgumentOutOfRangeException(nameof(value))
    };

    public bool Observe(in SaveLoadEvent value, double time) => value.Phase switch
    {
        SaveLoadPhase.Queued => Open("save.load", value.Operation, "newGameOrLoad", true, false),
        SaveLoadPhase.Started => Open("save.load", value.Operation, "newGameOrLoad", true, true),
        SaveLoadPhase.Rejected => Close("save.load", value.Operation, time, false),
        SaveLoadPhase.Completed => Close("save.load", value.Operation, time, true),
        SaveLoadPhase.Canceled => Close("save.load", value.Operation, time, true),
        SaveLoadPhase.Faulted => Close("save.load", value.Operation, time, true),
        _ => throw new ArgumentOutOfRangeException(nameof(value))
    };

    public bool Observe(in MapPrepareEvent value, double time) => value.Phase switch
    {
        MapPreparePhase.Started => Open("map.prepare", value.Operation, "transition", true, true),
        MapPreparePhase.Joined => _operations.ContainsKey(("map.prepare", value.Operation)),
        MapPreparePhase.Reused => Close("map.prepare", value.Operation, time, false),
        MapPreparePhase.Rejected => Close("map.prepare", value.Operation, time, false),
        MapPreparePhase.Completed => Close("map.prepare", value.Operation, time, true),
        _ => throw new ArgumentOutOfRangeException(nameof(value))
    };

    public bool Observe(in QualityApplyEvent value, double time) => value.Phase switch
    {
        QualityApplyPhase.Queued => Open("settings.apply", value.Operation, "qualityApply", false, false),
        QualityApplyPhase.Started => Open("settings.apply", value.Operation, "qualityApply", true, true),
        QualityApplyPhase.Rejected => Close("settings.apply", value.Operation, time, false),
        QualityApplyPhase.Completed => Close("settings.apply", value.Operation, time, true),
        QualityApplyPhase.Superseded => Close("settings.apply", value.Operation, time, false),
        QualityApplyPhase.Canceled => Close("settings.apply", value.Operation, time, true, requireStarted: true),
        QualityApplyPhase.Faulted => Close("settings.apply", value.Operation, time, true, requireStarted: true),
        _ => throw new ArgumentOutOfRangeException(nameof(value))
    };

    private bool Open(string owner, long operation, string rule, bool active, bool started)
    {
        _operations[(owner, operation)] = new(rule, active, started);
        return true;
    }

    private bool Close(string owner, long operation, double time, bool settle, bool requireStarted = false)
    {
        bool observed = _operations.Remove((owner, operation), out var state);
        if (settle && (!requireStarted || observed && state.Started))
            Settle(observed ? state.Rule : _eventRules[owner], time);
        return observed;
    }

    public void Settle(string rule, double? time = null)
    {
        double until = (time ?? FrameClock.MonotonicMilliseconds) + _settling[rule];
        if (rule == "qualityApply") _qualityUntil = Math.Max(_qualityUntil, until);
        else _settleUntil = Math.Max(_settleUntil, until);
    }

    public void Readback(string purpose, ulong collectionFrame)
    {
        if (purpose == "saveThumbnail") _thumbnailFrames.Add(collectionFrame);
    }

    public (long Flags, bool Complete) Capture(in FrameSample frame, bool loading, WorldTransition transition, bool arriving)
    {
        long bits = (long)frame.Context.Activity;
        foreach (var operation in _operations)
            if (operation.Value.Active) bits |= _ruleFlags[operation.Value.Rule];
        if (_appStates.Contains(frame.Context.State)) bits |= Flag("notPlaying");
        if (loading) bits |= Flag("loadingVisible");
        if (frame.Context.WindowFocused == false) bits |= Flag("windowUnfocused");
        if ((transition & (WorldTransition.Time | WorldTransition.Season)) != 0 || arriving)
            bits |= Flag("worldOrArrivalTransition");
        if (frame.MonotonicMilliseconds < _settleUntil) bits |= Flag("transitionSettling");
        if (frame.MonotonicMilliseconds < _qualityUntil) bits |= Flag("qualitySettling");
        if (_thumbnailFrames.Remove(frame.CollectionFrame)) bits |= Flag("captureFrame");
        if (_couldDraw == false && frame.Context.WindowCanDraw) _resumeRemaining = _resumeFrames;
        _couldDraw = frame.Context.WindowCanDraw;
        if (_resumeRemaining > 0 && frame.Context.WindowCanDraw)
        {
            bits |= Flag("drawResuming");
            _resumeRemaining--;
        }
        return (bits, RequiredMissing.Length == 0);
    }
    private long Flag(string name) => _flags[name];
}
