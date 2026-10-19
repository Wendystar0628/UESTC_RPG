using System.Collections.ObjectModel;
using System.Text.Json;
using Campus.Contracts.Quality;
using Campus.Kernel.Frame;

namespace Campus.Kernel.Quality.Settings;

/// <summary>SYS-GRAPHICS values. Enum encodings match the published D0 catalog.</summary>
public sealed class QualityCatalog
{
    public const int Count = 11;
    public static readonly QualityKnob[] All = Enum.GetValues<QualityKnob>();
    public static readonly IReadOnlyList<float> RenderScales = Array.AsReadOnly<float>([0.5f, 0.59f, 0.67f, 0.77f, 1f]);
    private static readonly IReadOnlyList<float> ShadowDistances = Array.AsReadOnly<float>([600, 1000, 1600]);
    public static readonly (int Width, int Height)[] WindowSizes =
        [(1920, 1080), (1280, 720), (1600, 900), (2560, 1440), (3840, 2160)];
    private static readonly QualityPreset[] Presets = [QualityPreset.Balanced, QualityPreset.Fine, QualityPreset.Ultra];
    private readonly float[][] _presets;
    private readonly float[] _grassRadii;
    private readonly KnobSpec[] _specs;
    public IReadOnlyList<KnobSpec> Knobs { get; }
    public QualityPreset DefaultPreset { get; }
    public float FoliageTransitionSeconds { get; }
    public QualityProfile DefaultProfile => Profile(DefaultPreset);
    public int OutputWidth { get; set; } = 1920;
    public int OutputHeight { get; set; } = 1080;
    public bool PreserveOutputSize { get; set; }

    public QualityCatalog(string json)
    {
        _specs =
        [
            Spec(QualityKnob.DisplayMode, "WP-32", ApplyClass.UnderVeil, KnobKind.Performance, 0, 2, 1, 0),
            Spec(QualityKnob.WindowSize, "WP-32", ApplyClass.UnderVeil, KnobKind.Performance, 0, WindowSizes.Length - 1, 1, 0),
            Spec(QualityKnob.VSync, "WP-32", ApplyClass.Immediate, KnobKind.Performance, 0, 1, 1, 0),
            Spec(QualityKnob.FrameCap, "WP-32", ApplyClass.Immediate, KnobKind.Performance, 0, 4, 1, 0),
            Spec(QualityKnob.RenderScale, "WP-32", ApplyClass.UnderVeil, KnobKind.Performance,
                RenderScales[0], RenderScales[^1], 0, RenderScales[0], values: RenderScales),
            Spec(QualityKnob.AntiAliasing, "WP-05", ApplyClass.UnderVeil, KnobKind.Performance, 0, 1, 1, 1, false),
            Spec(QualityKnob.ShadowQuality, "WP-03", ApplyClass.Immediate, KnobKind.Floor, 0, 2, 1, 0),
            Spec(QualityKnob.ShadowDistance, "WP-03", ApplyClass.Immediate, KnobKind.Performance,
                ShadowDistances[0], ShadowDistances[^1], 0, ShadowDistances[0], values: ShadowDistances),
            Spec(QualityKnob.FoliageDensity, "WP-32", ApplyClass.Immediate, KnobKind.Performance, 0.75f, 1, 0.25f, 0.75f),
            Spec(QualityKnob.ReflectionInterval, "WP-24", ApplyClass.Immediate, KnobKind.Floor, 2, 4, 1, 4, false),
            Spec(QualityKnob.ParticleDensity, "WP-45b", ApplyClass.Immediate, KnobKind.Performance, 0.5f, 1, 0.25f, 0.5f),
        ];
        Knobs = Array.AsReadOnly(_specs);
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.GetProperty("schema").GetInt32() != 1)
            throw new InvalidDataException("Unsupported quality preset schema.");
        if (!document.RootElement.TryGetProperty("defaultPreset", out var defaultPreset) ||
            defaultPreset.ValueKind != JsonValueKind.String ||
            !Presets.Any(preset => preset.ToString().ToLowerInvariant() == defaultPreset.GetString()))
            throw new InvalidDataException("Quality defaultPreset must name a supported preset.");
        DefaultPreset = Presets.Single(preset => preset.ToString().ToLowerInvariant() == defaultPreset.GetString());
        FoliageTransitionSeconds = document.RootElement.GetProperty("foliageTransitionSeconds").GetSingle();
        if (!float.IsFinite(FoliageTransitionSeconds) || FoliageTransitionSeconds is < 0.5f or > 1f)
            throw new InvalidDataException("Foliage transition must take 0.5–1 seconds.");
        var presets = document.RootElement.GetProperty("presets");
        _presets = new float[Presets.Length][];
        _grassRadii = new float[Presets.Length];
        foreach (var preset in Presets)
        {
            var row = presets.GetProperty(preset.ToString().ToLowerInvariant());
            if (row.EnumerateObject().Count() != Count) throw new InvalidDataException($"Each preset must contain all {Count - 1} data-driven knobs and grass_radius.");
            var grassRadius = row.GetProperty("grass_radius").GetSingle();
            if (!float.IsFinite(grassRadius) || grassRadius <= 0)
                throw new InvalidDataException($"Invalid preset grass radius: {grassRadius}.");
            _grassRadii[(int)preset] = grassRadius;
            var values = _presets[(int)preset] = new float[Count];
            foreach (var knob in All)
            {
                // Render scale is derived from the output pixel budget, with manual overrides applied by Resolve.
                if (knob == QualityKnob.RenderScale) continue;
                var value = row.GetProperty(Name(knob)).GetSingle();
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
        if (knob == QualityKnob.AntiAliasing) return Resolve(Profile(preset), preset)[(int)knob];
        if (knob != QualityKnob.RenderScale) return _presets[(int)preset][(int)knob];
        return ScaleForOutput(preset, OutputWidth, OutputHeight);
    }

    // Fixed by the base preset; custom single-item overrides keep that preset's grass range.
    public float GrassRadius(QualityPreset preset) => preset is >= QualityPreset.Balanced and <= QualityPreset.Ultra
        ? _grassRadii[(int)preset] : throw new ArgumentOutOfRangeException(nameof(preset));

    private static float ScaleForOutput(QualityPreset preset, int width, int height)
    {
        var pixels = (double)Math.Max(1, width) * Math.Max(1, height);
        var cap = preset switch
        {
            QualityPreset.Balanced => KernelBudgets.BalancedInternalPixels,
            QualityPreset.Fine => KernelBudgets.FineInternalPixels,
            QualityPreset.Ultra => pixels >= 8_000_000 ? KernelBudgets.UltraInternalPixelsAt4k : KernelBudgets.UltraInternalPixels,
            _ => throw new ArgumentOutOfRangeException(nameof(preset)),
        };
        var target = Math.Min(1, Math.Sqrt(cap / pixels));
        return RenderScales.MinBy(scale => Math.Abs(scale - target));
    }

    public bool IsValid(QualityKnob knob, float value)
    {
        if (!Enum.IsDefined(knob) || !float.IsFinite(value)) return false;
        var spec = this[knob];
        if (value < spec.Min || value > spec.Max) return false;
        if (spec.HigherIsBetter ? value < spec.WorstAllowed : value > spec.WorstAllowed) return false;
        if (spec.Step > 0 && Math.Abs((value - spec.Min) / spec.Step - Math.Round((value - spec.Min) / spec.Step)) > 0.0001) return false;
        return spec.Values is null || spec.Values.Contains(value);
    }

    public float[] Resolve(QualityProfile profile, QualityPreset customBase)
    {
        var preset = profile.Preset == QualityPreset.Custom ? customBase : profile.Preset;
        var values = All.Select(knob => knob == QualityKnob.AntiAliasing
            ? _presets[(int)preset][(int)knob] : PresetValue(preset, knob)).ToArray();
        foreach (var pair in profile.Overrides.OrderBy(pair => pair.Key)) values[(int)pair.Key] = pair.Value;
        if (!PreserveOutputSize && values[(int)QualityKnob.DisplayMode] == 0 && !profile.Overrides.ContainsKey(QualityKnob.RenderScale))
        {
            var size = WindowSizes[(int)values[(int)QualityKnob.WindowSize]];
            values[(int)QualityKnob.RenderScale] = ScaleForOutput(preset, size.Width, size.Height);
        }
        // The project's supported FSR1 path uses FXAA; native rendering keeps the chosen AA.
        if (values[(int)QualityKnob.RenderScale] < 1)
            values[(int)QualityKnob.AntiAliasing] = 1;
        return values;
    }

    public QualityProfile Normalize(QualityProfile profile, QualityPreset customBase)
    {
        var preset = profile.Preset == QualityPreset.Custom ? customBase : profile.Preset;
        var values = Resolve(profile, customBase);
        var overrides = profile.Overrides.ToDictionary(pair => pair.Key, pair => pair.Value);
        var aa = QualityKnob.AntiAliasing;
        var requested = overrides.GetValueOrDefault(aa, PresetValue(preset, aa));
        if (requested != values[(int)aa]) overrides[aa] = values[(int)aa];
        return profile with { Overrides = new ReadOnlyDictionary<QualityKnob, float>(overrides) };
    }

    public static int FrameLimit(float encoded, float refresh)
    {
        var hz = float.IsFinite(refresh) && refresh >= 24 ? refresh : 60;
        return encoded switch
        {
            0 => 30,
            1 => 60,
            2 => (int)Math.Floor(hz / 2),
            3 => (int)Math.Floor(hz),
            4 => 0,
            _ => throw new ArgumentOutOfRangeException(nameof(encoded)),
        };
    }

    private static KnobSpec Spec(QualityKnob knob, string owner, ApplyClass apply, KnobKind kind,
        float min, float max, float step, float worst, bool higher = true, IReadOnlyList<float>? values = null) =>
        new(knob, owner, apply, kind, min, max, step, worst, higher, "quality." + Name(knob)) { Values = values };
}
