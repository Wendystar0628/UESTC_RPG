using Campus.Contracts.Quality;

namespace Campus.Kernel.Quality.Settings;

/// <summary>Pure state/validation; engine scheduling and persistence live in QualityService.</summary>
public sealed class QualityState
{
    public sealed record Snapshot(QualityProfile Profile, QualityPreset BasePreset, float[] Effective,
        float[] Requested, bool[] Locked, float?[] Ceilings);
    private readonly QualityCatalog _catalog;
    private float[] _effective;
    private float[] _requested;
    private readonly bool[] _locked = new bool[QualityCatalog.Count];
    private readonly float?[] _ceilings = new float?[QualityCatalog.Count];
    private long _version;
    private QualityPreset _basePreset = QualityPreset.Fine;
    public DeviceTier Tier { get; set; } = DeviceTier.DgpuMid;
    public bool CaptureMode { get; set; }
    public float RefreshRate { get; set; } = 60;
    public QualityProfile Requested { get; private set; } = QualityCatalog.Profile(QualityPreset.Fine);
    public QualityView Effective { get; private set; }
    public bool RestartPending => QualityCatalog.All.Any(knob => _catalog[knob].Apply == ApplyClass.Restart && _requested[(int)knob] != _effective[(int)knob]);

    public QualityState(QualityCatalog catalog)
    {
        _catalog = catalog;
        _requested = catalog.Resolve(Requested, _basePreset);
        _effective = (float[])_requested.Clone();
        Effective = MakeView();
    }

    public ApplyPlan Plan(QualityProfile next)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(next.Overrides);
        if (!Enum.IsDefined(next.Preset) || next.Schema != 1) throw new ArgumentException("Unsupported quality profile.", nameof(next));
        var rejected = next.Overrides.Where(pair => !_catalog.IsValid(pair.Key, pair.Value)).Select(pair => pair.Key).Order().ToArray();
        var frozen = QualityCatalog.Freeze(next);
        if (rejected.Length != 0) return new(frozen, [], ApplyClass.Immediate, false, 0, 0, rejected);
        var values = _catalog.Resolve(frozen, _basePreset);
        var deltas = QualityCatalog.All.Where(knob => values[(int)knob] != _effective[(int)knob])
            .Select(knob => new KnobDelta(knob, _effective[(int)knob], values[(int)knob], _catalog[knob].Apply)).ToArray();
        var heaviest = deltas.Length == 0 ? ApplyClass.Immediate : deltas.Max(delta => delta.Apply);
        return new(frozen, Array.AsReadOnly(deltas), heaviest,
            deltas.Any(delta => IsDisplay(delta.Knob)), 0, 0, []);
    }

    public QualityChange Commit(QualityProfile next, ApplyOrigin origin, bool veiled)
    {
        var plan = Plan(next);
        if (plan.Rejected.Count != 0) throw new ArgumentException("Cannot commit invalid quality values.", nameof(next));
        var values = _catalog.Resolve(next, _basePreset);
        if (origin is ApplyOrigin.User or ApplyOrigin.Startup)
        {
            // Choosing a clean preset resets its manual locks; individual edits retain them.
            if (next.Overrides.Count == 0 || next.Preset != _basePreset && next.Preset != QualityPreset.Custom)
            {
                Array.Clear(_locked);
                Array.Clear(_ceilings);
            }
            foreach (var knob in next.Overrides.Keys.Order()) _locked[(int)knob] = true;
        }
        if (next.Preset != QualityPreset.Custom) _basePreset = next.Preset;
        var baselineOverrides = next.Overrides.Where(pair => pair.Key is QualityKnob.DisplayMode or QualityKnob.WindowSize)
            .ToDictionary(pair => pair.Key, pair => pair.Value);
        var baseline = _catalog.Resolve(new(_basePreset, baselineOverrides, next.Adaptive), _basePreset);
        var overrides = next.Overrides.Where(pair => pair.Key != QualityKnob.Adaptive && pair.Value !=
            (pair.Key == QualityKnob.RenderScale ? baseline[(int)pair.Key] : _catalog.PresetValue(_basePreset, pair.Key)))
            .ToDictionary(pair => pair.Key, pair => pair.Value);
        Requested = QualityCatalog.Freeze(new(_basePreset, overrides, values[(int)QualityKnob.Adaptive] != 0));
        ulong mask = 0;
        foreach (var knob in QualityCatalog.All)
        {
            if (_catalog[knob].Apply == ApplyClass.Restart && origin != ApplyOrigin.Startup)
            {
                // Notify queued UI requests that the restart choice was accepted; the effective engine value stays unchanged.
                if (_requested[(int)knob] != values[(int)knob]) mask |= 1UL << (int)knob;
                continue;
            }
            var value = Merge(knob, values[(int)knob], Requested.Adaptive);
            if (_effective[(int)knob] == value) continue;
            _effective[(int)knob] = value;
            mask |= 1UL << (int)knob;
        }
        _requested = values;
        if (origin == ApplyOrigin.Startup) mask = (1UL << QualityCatalog.Count) - 1;
        if (mask != 0) _version++;
        Effective = MakeView();
        return new(_version, mask, origin, veiled);
    }

    public QualityProfile RestoreDisplay(QualityProfile before)
    {
        var original = _catalog.Resolve(before, before.Preset == QualityPreset.Custom ? _basePreset : before.Preset);
        var current = Requested.Overrides.ToDictionary(pair => pair.Key, pair => pair.Value);
        foreach (var knob in QualityCatalog.All.Where(IsDisplay)) current[knob] = original[(int)knob];
        return new(Requested.Preset, current, Requested.Adaptive);
    }

    public Snapshot Capture() => new(Requested, _basePreset, (float[])_effective.Clone(),
        (float[])_requested.Clone(), (bool[])_locked.Clone(), (float?[])_ceilings.Clone());

    public QualityChange Restore(Snapshot snapshot, bool veiled)
    {
        Requested = snapshot.Profile;
        _basePreset = snapshot.BasePreset;
        _effective = (float[])snapshot.Effective.Clone();
        _requested = (float[])snapshot.Requested.Clone();
        snapshot.Locked.CopyTo(_locked, 0);
        snapshot.Ceilings.CopyTo(_ceilings, 0);
        _version++;
        Effective = MakeView();
        // A consumer may have thrown after partially applying; replay every setting to restore it.
        return new(_version, (1UL << QualityCatalog.Count) - 1, ApplyOrigin.Revert, veiled);
    }

    /// <summary>The future feedback controller supplies ceilings here; it cannot touch style toggles or locked items.</summary>
    public QualityChange SetAdaptiveCeilings(IReadOnlyDictionary<QualityKnob, float> ceilings)
    {
        foreach (var pair in ceilings.OrderBy(pair => pair.Key))
        {
            var spec = _catalog[pair.Key];
            if (!spec.InAdaptiveLadder || spec.Apply is not (ApplyClass.Immediate or ApplyClass.Amortized) ||
                !_catalog.IsValid(pair.Key, pair.Value)) throw new ArgumentException($"Invalid adaptive ceiling: {pair.Key}", nameof(ceilings));
        }
        foreach (var pair in ceilings.OrderBy(pair => pair.Key))
        {
            var previous = _ceilings[(int)pair.Key];
            var higher = _catalog[pair.Key].HigherIsBetter;
            _ceilings[(int)pair.Key] = previous is null ? pair.Value : higher ? Math.Min(previous.Value, pair.Value) : Math.Max(previous.Value, pair.Value);
        }
        ulong mask = 0;
        foreach (var knob in QualityCatalog.All)
        {
            if (_catalog[knob].Apply == ApplyClass.Restart) continue;
            var merged = Merge(knob, _requested[(int)knob], Requested.Adaptive);
            if (merged == _effective[(int)knob]) continue;
            _effective[(int)knob] = merged;
            mask |= 1UL << (int)knob;
        }
        if (mask != 0) _version++;
        Effective = MakeView();
        return new(_version, mask, ApplyOrigin.Adaptive, false);
    }

    private float Merge(QualityKnob knob, float requested, bool adaptive)
    {
        if (!adaptive || _locked[(int)knob] || _ceilings[(int)knob] is not { } ceiling) return requested;
        var spec = _catalog[knob];
        return spec.HigherIsBetter ? Math.Max(spec.WorstAllowed, Math.Min(requested, ceiling)) :
            Math.Min(spec.WorstAllowed, Math.Max(requested, ceiling));
    }

    private QualityView MakeView()
    {
        var cap = QualityCatalog.FrameLimit(_effective[(int)QualityKnob.FrameCap], _effective[(int)QualityKnob.Vrr] != 0, RefreshRate);
        return new(_version, Requested.Overrides.Count == 0 ? _basePreset : QualityPreset.Custom, Tier,
            _effective[(int)QualityKnob.Adaptive] != 0, _effective, _requested, _locked,
            QualityCatalog.All.Select(knob => _catalog[knob].Apply != ApplyClass.Restart && _effective[(int)knob] != _requested[(int)knob]).ToArray(),
            stepRate: !CaptureMode && cap == 30 ? 15 : 12);
    }

    public static bool IsDisplay(QualityKnob knob) => knob is QualityKnob.DisplayMode or QualityKnob.WindowSize or QualityKnob.RenderScale;
}
