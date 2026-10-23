using System.Collections.ObjectModel;
using System.Text.Json;
using Campus.Contracts.Quality;

namespace Campus.Kernel.Quality.Settings;

/// <summary>SYS-GRAPHICS values. Enum encodings match the published D0 catalog.</summary>
public sealed class QualityCatalog
{
    public static readonly QualityKnob[] All = Enum.GetValues<QualityKnob>();
    public static readonly int Count = All.Length;
    public static readonly IReadOnlyList<float> RenderScales = Array.AsReadOnly<float>([0.5f, 0.59f, 0.67f, 0.77f, 1f]);
    private static readonly IReadOnlyList<float> ShadowDistances = Array.AsReadOnly<float>([600, 1000, 1600]);
    public static readonly (int Width, int Height)[] WindowSizes =
        [(1920, 1080), (1280, 720), (1600, 900), (2560, 1440), (3840, 2160)];
    private static readonly QualityPreset[] Presets = [QualityPreset.Balanced, QualityPreset.Fine, QualityPreset.Ultra];
    private readonly float[][] _presets;
    private readonly float[] _defaults;
    private readonly float[] _grassRadii;
    private readonly KnobSpec[] _specs;
    public IReadOnlyList<KnobSpec> Knobs { get; }
    public QualityPreset DefaultPreset { get; }
    public float FoliageTransitionSeconds { get; }
    public QualityProfile DefaultProfile => new(DefaultPreset, new ReadOnlyDictionary<QualityKnob, float>(
        _specs.Where(spec => spec.Scope != KnobScope.Effect).ToDictionary(spec => spec.Knob, spec => _defaults[(int)spec.Knob])));

    public QualityCatalog(string json)
    {
        _specs =
        [
            Spec(QualityKnob.DisplayMode, ApplyClass.UnderLoading, KnobKind.Performance, KnobScope.Display, KnobUnit.Number, 0, 2, 1, 0),
            Spec(QualityKnob.WindowSize, ApplyClass.UnderLoading, KnobKind.Performance, KnobScope.Display, KnobUnit.Number, 0, WindowSizes.Length - 1, 1, 0),
            Spec(QualityKnob.VSync, ApplyClass.Immediate, KnobKind.Performance, KnobScope.Display, KnobUnit.Number, 0, 1, 1, 0),
            Spec(QualityKnob.FrameRateLimit, ApplyClass.Immediate, KnobKind.Performance, KnobScope.Display, KnobUnit.Number, 0, 2, 1, 0),
            Spec(QualityKnob.RenderScale, ApplyClass.UnderLoading, KnobKind.Performance, KnobScope.Clarity, KnobUnit.Scale,
                RenderScales[0], RenderScales[^1], 0, RenderScales[0], values: RenderScales),
            Spec(QualityKnob.AntiAliasing, ApplyClass.UnderLoading, KnobKind.Performance, KnobScope.Clarity, KnobUnit.Number, 0, 1, 1, 1, false),
            Spec(QualityKnob.ShadowQuality, ApplyClass.UnderLoading, KnobKind.Floor, KnobScope.Effect, KnobUnit.Number, 0, 2, 1, 0),
            Spec(QualityKnob.ShadowDistance, ApplyClass.Immediate, KnobKind.Performance, KnobScope.Effect, KnobUnit.Metres,
                ShadowDistances[0], ShadowDistances[^1], 0, ShadowDistances[0], values: ShadowDistances),
            Spec(QualityKnob.FoliageDensity, ApplyClass.Immediate, KnobKind.Performance, KnobScope.Effect, KnobUnit.Percent, 0.75f, 1, 0.25f, 0.75f),
            Spec(QualityKnob.ReflectionInterval, ApplyClass.Immediate, KnobKind.Floor, KnobScope.Effect, KnobUnit.Number, 2, 4, 1, 4, false),
            Spec(QualityKnob.ParticleDensity, ApplyClass.Immediate, KnobKind.Performance, KnobScope.Effect, KnobUnit.Percent, 0.5f, 1, 0.25f, 0.5f),
            Spec(QualityKnob.FsrSharpness, ApplyClass.UnderLoading, KnobKind.Performance, KnobScope.Clarity, KnobUnit.Number,
                QualityRules.FsrSharpnessMin, QualityRules.FsrSharpnessMax, 0.1f, QualityRules.FsrSharpnessMin),
            Spec(QualityKnob.Vignette, ApplyClass.Immediate, KnobKind.StyleDefault, KnobScope.Comfort, KnobUnit.Number, 0, 1, 1, 0),
        ];
        Knobs = Array.AsReadOnly(_specs);
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.GetProperty("schema").GetInt32() != 2)
            throw new InvalidDataException("Unsupported quality preset schema.");
        if (!document.RootElement.TryGetProperty("defaultPreset", out var defaultPreset) ||
            defaultPreset.ValueKind != JsonValueKind.String ||
            !Presets.Any(preset => preset.ToString().ToLowerInvariant() == defaultPreset.GetString()))
            throw new InvalidDataException("Quality defaultPreset must name a supported preset.");
        DefaultPreset = Presets.Single(preset => preset.ToString().ToLowerInvariant() == defaultPreset.GetString());
        FoliageTransitionSeconds = document.RootElement.GetProperty("foliageTransitionSeconds").GetSingle();
        if (!float.IsFinite(FoliageTransitionSeconds) || FoliageTransitionSeconds is < 0.5f or > 1f)
            throw new InvalidDataException("Foliage transition must take 0.5–1 seconds.");
        var defaults = document.RootElement.GetProperty("defaults");
        _defaults = new float[Count];
        foreach (var spec in _specs.Where(spec => spec.Scope != KnobScope.Effect))
        {
            var value = defaults.GetProperty(Name(spec.Knob)).GetSingle();
            if (!IsValid(spec.Knob, value)) throw new InvalidDataException($"Invalid default value: {spec.Knob}={value}.");
            _defaults[(int)spec.Knob] = value;
        }
        if (defaults.EnumerateObject().Count() != _specs.Count(spec => spec.Scope != KnobScope.Effect))
            throw new InvalidDataException("Quality defaults must contain exactly the non-effect knobs.");
        var presets = document.RootElement.GetProperty("presets");
        _presets = new float[Presets.Length][];
        _grassRadii = new float[Presets.Length];
        foreach (var preset in Presets)
        {
            var row = presets.GetProperty(preset.ToString().ToLowerInvariant());
            if (row.EnumerateObject().Count() != _specs.Count(spec => spec.Scope == KnobScope.Effect) + 1)
                throw new InvalidDataException("Each preset must contain exactly the effect knobs and grass_radius.");
            var grassRadius = row.GetProperty("grass_radius").GetSingle();
            if (!float.IsFinite(grassRadius) || grassRadius <= 0)
                throw new InvalidDataException($"Invalid preset grass radius: {grassRadius}.");
            _grassRadii[(int)preset] = grassRadius;
            var values = _presets[(int)preset] = new float[Count];
            foreach (var knob in All)
            {
                var value = this[knob].Scope == KnobScope.Effect ? row.GetProperty(Name(knob)).GetSingle() : _defaults[(int)knob];
                if (!IsValid(knob, value)) throw new InvalidDataException($"Invalid preset value: {knob}={value}.");
                values[(int)knob] = value;
            }
        }
    }

    public KnobSpec this[QualityKnob knob] => Enum.IsDefined(knob) ? _specs[(int)knob] : throw new ArgumentOutOfRangeException(nameof(knob));
    public static string Name(QualityKnob knob) => JsonNamingPolicy.SnakeCaseLower.ConvertName(knob.ToString());
    public static QualityProfile Profile(QualityPreset preset) => new(preset,
        new ReadOnlyDictionary<QualityKnob, float>(new Dictionary<QualityKnob, float>()));
    public static QualityProfile Freeze(QualityProfile profile) => profile with
    {
        Overrides = new ReadOnlyDictionary<QualityKnob, float>(profile.Overrides.ToDictionary(pair => pair.Key, pair => pair.Value)),
    };

    public float PresetValue(QualityPreset preset, QualityKnob knob)
    {
        if (preset is < QualityPreset.Balanced or > QualityPreset.Ultra) throw new ArgumentOutOfRangeException(nameof(preset));
        _ = this[knob];
        return _presets[(int)preset][(int)knob];
    }

    // Fixed by the base preset; custom single-item overrides keep that preset's grass range.
    public float GrassRadius(QualityPreset preset) => preset is >= QualityPreset.Balanced and <= QualityPreset.Ultra
        ? _grassRadii[(int)preset] : throw new ArgumentOutOfRangeException(nameof(preset));

    public bool IsValid(QualityKnob knob, float value)
    {
        if (!Enum.IsDefined(knob) || !float.IsFinite(value)) return false;
        var spec = this[knob];
        if (value < spec.Min || value > spec.Max) return false;
        if (spec.HigherIsBetter ? value < spec.WorstAllowed : value > spec.WorstAllowed) return false;
        if (spec.Step > 0 && Math.Abs((value - spec.Min) / spec.Step - Math.Round((value - spec.Min) / spec.Step)) > 0.0001) return false;
        return spec.Values is null || spec.Values.Contains(value);
    }

    public float[] ResolveRequested(QualityProfile profile, QualityPreset customBase)
    {
        var preset = profile.Preset == QualityPreset.Custom ? customBase : profile.Preset;
        var values = All.Select(knob => PresetValue(preset, knob)).ToArray();
        foreach (var pair in profile.Overrides.OrderBy(pair => pair.Key)) values[(int)pair.Key] = pair.Value;
        return values;
    }

    // The only current dependency: sharpening has no effect without the spatial upscaler.
    private static bool SharpnessRequiresUpscaling(float[] requested) => requested[(int)QualityKnob.RenderScale] >= 1;

    public float[] Resolve(QualityProfile profile, QualityPreset customBase)
    {
        var values = ResolveRequested(profile, customBase);
        if (SharpnessRequiresUpscaling(values)) values[(int)QualityKnob.FsrSharpness] = 0;
        return values;
    }

    public IReadOnlyList<KnobNote> Notes(QualityProfile profile, QualityPreset customBase) =>
        SharpnessRequiresUpscaling(ResolveRequested(profile, customBase))
            ? Array.AsReadOnly<KnobNote>([new(QualityKnob.FsrSharpness, "quality.fsr_sharpness.requires_upscaling", 0)]) : [];

    private static KnobLoad Load(QualityKnob knob) => knob switch
    {
        QualityKnob.DisplayMode or QualityKnob.VSync or QualityKnob.FrameRateLimit => new(0, 0, 0),
        QualityKnob.WindowSize => new(2, 2, 1),
        QualityKnob.RenderScale => new(2, 2, 0),
        QualityKnob.AntiAliasing => new(1, 1, 0),
        QualityKnob.ShadowQuality => new(2, 2, 1),
        QualityKnob.ShadowDistance => new(2, 0, 1),
        QualityKnob.FoliageDensity => new(1, 0, 2),
        QualityKnob.ReflectionInterval => new(2, 1, 1),
        QualityKnob.ParticleDensity => new(1, 0, 1),
        QualityKnob.FsrSharpness or QualityKnob.Vignette => new(1, 0, 0),
        _ => throw new ArgumentOutOfRangeException(nameof(knob)),
    };

    private static KnobSpec Spec(QualityKnob knob, ApplyClass apply, KnobKind kind, KnobScope scope,
        KnobUnit unit, float min, float max, float step, float worst, bool higher = true, IReadOnlyList<float>? values = null) =>
        new(knob, apply, kind, min, max, step, worst, higher, "quality." + Name(knob), unit) { Scope = scope, Load = Load(knob), Values = values };
}
