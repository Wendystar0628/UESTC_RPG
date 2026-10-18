using Campus.Contracts;
using Campus.Contracts.Quality;

namespace Campus.Kernel.Quality.Settings;

/// <summary>Pure state/validation; engine scheduling and persistence live in QualityService.</summary>
public sealed class QualityState
{
    public sealed record Snapshot(QualityProfile Profile, QualityPreset BasePreset, float[] Values);
    private readonly QualityCatalog _catalog;
    private float[] _values;
    private long _version;
    private QualityPreset _basePreset;
    public bool CaptureMode { get; set; }
    public float RefreshRate { get; set; } = 60;
    public QualityProfile Requested { get; private set; }
    public QualityView Effective { get; private set; }
    public float GrassRadius => _catalog.GrassRadius(_basePreset);

    public QualityState(QualityCatalog catalog)
    {
        _catalog = catalog;
        _basePreset = catalog.DefaultPreset;
        Requested = catalog.DefaultProfile;
        _values = catalog.Resolve(Requested, _basePreset);
        Effective = MakeView();
    }

    public ApplyPlan Plan(QualityProfile next)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(next.Overrides);
        if (!Enum.IsDefined(next.Preset) || next.Schema != 1) throw new ArgumentException("Unsupported quality profile.", nameof(next));
        var rejected = next.Overrides.Where(pair => !_catalog.IsValid(pair.Key, pair.Value)).Select(pair => pair.Key).Order().ToArray();
        var frozen = QualityCatalog.Freeze(next);
        if (rejected.Length != 0) return new(frozen, [], ApplyClass.Immediate, 0, 0, rejected);
        var values = _catalog.Resolve(frozen, _basePreset);
        var preset = frozen.Preset == QualityPreset.Custom ? _basePreset : frozen.Preset;
        var grassRangeChanged = _catalog.GrassRadius(preset) != GrassRadius;
        var deltas = QualityCatalog.All.Where(knob => values[(int)knob] != _values[(int)knob] ||
                knob == QualityKnob.FoliageDensity && grassRangeChanged)
            .Select(knob => new KnobDelta(knob, _values[(int)knob], values[(int)knob], _catalog[knob].Apply)).ToArray();
        var heaviest = deltas.Length == 0 ? ApplyClass.Immediate : deltas.Max(delta => delta.Apply);
        return new(frozen, Array.AsReadOnly(deltas), heaviest, 0, 0, []);
    }

    public QualityChange Commit(QualityProfile next, ApplyOrigin origin, bool veiled)
    {
        var plan = Plan(next);
        if (plan.Rejected.Count != 0) throw new ArgumentException("Cannot commit invalid quality values.", nameof(next));
        var values = _catalog.Resolve(next, _basePreset);
        var previousGrassRadius = GrassRadius;
        if (next.Preset != QualityPreset.Custom) _basePreset = next.Preset;
        var baselineOverrides = next.Overrides.Where(pair => pair.Key is QualityKnob.DisplayMode or QualityKnob.WindowSize)
            .ToDictionary(pair => pair.Key, pair => pair.Value);
        var baseline = _catalog.Resolve(new(_basePreset, baselineOverrides), _basePreset);
        var overrides = next.Overrides.Where(pair => pair.Value !=
            (pair.Key == QualityKnob.RenderScale ? baseline[(int)pair.Key] : _catalog.PresetValue(_basePreset, pair.Key)))
            .ToDictionary(pair => pair.Key, pair => pair.Value);
        Requested = QualityCatalog.Freeze(new(_basePreset, overrides));
        ulong mask = 0;
        // The fixed grass range belongs to the vegetation preset effect, including when all public knobs match.
        if (GrassRadius != previousGrassRadius) mask |= 1UL << (int)QualityKnob.FoliageDensity;
        foreach (var knob in QualityCatalog.All)
            if (_values[(int)knob] != values[(int)knob]) mask |= 1UL << (int)knob;
        _values = values;
        if (origin == ApplyOrigin.Startup) mask = (1UL << QualityCatalog.Count) - 1;
        if (mask != 0) _version++;
        Effective = MakeView();
        return new(_version, mask, origin, veiled);
    }

    public Snapshot Capture() => new(Requested, _basePreset, (float[])_values.Clone());

    public QualityChange Restore(Snapshot snapshot, ApplyOrigin origin, bool veiled)
    {
        Requested = snapshot.Profile;
        _basePreset = snapshot.BasePreset;
        _values = (float[])snapshot.Values.Clone();
        _version++;
        Effective = MakeView();
        // A consumer may have thrown after partially applying; replay every setting to restore it.
        return new(_version, (1UL << QualityCatalog.Count) - 1, origin, veiled);
    }

    private QualityView MakeView()
    {
        var cap = QualityCatalog.FrameLimit(_values[(int)QualityKnob.FrameCap], RefreshRate);
        return new(_version, Requested.Overrides.Count == 0 ? _basePreset : QualityPreset.Custom, _values,
            stepRate: WorldTime.StepRate(cap, CaptureMode));
    }
}
