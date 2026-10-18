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
    public static readonly float[] RenderScales = [0.5f, 0.59f, 0.67f, 0.77f, 1f];
    public static readonly (int Width, int Height)[] WindowSizes =
        [(1920, 1080), (1280, 720), (1600, 900), (2560, 1440), (3840, 2160)];
    private static readonly QualityPreset[] Presets = [QualityPreset.Balanced, QualityPreset.Fine, QualityPreset.Ultra];
    private readonly float[][] _presets;
    private readonly KnobSpec[] _specs;
    public IReadOnlyList<KnobSpec> Knobs { get; }
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
            Spec(QualityKnob.RenderScale, "WP-32", ApplyClass.UnderVeil, KnobKind.Performance, 0.5f, 1, 0, 0.5f),
            Spec(QualityKnob.AntiAliasing, "WP-05", ApplyClass.UnderVeil, KnobKind.Performance, 0, 1, 1, 1, false),
            Spec(QualityKnob.ShadowQuality, "WP-03", ApplyClass.Immediate, KnobKind.Floor, 0, 2, 1, 0),
            Spec(QualityKnob.ShadowDistance, "WP-03", ApplyClass.Immediate, KnobKind.Performance, 600, 1600, 0, 600),
            Spec(QualityKnob.FoliageDensity, "WP-23", ApplyClass.Immediate, KnobKind.Performance, 0.75f, 1, 0.25f, 0.75f),
            Spec(QualityKnob.ReflectionInterval, "WP-24", ApplyClass.Immediate, KnobKind.Floor, 2, 4, 1, 4, false),
            Spec(QualityKnob.ParticleDensity, "WP-45b", ApplyClass.Immediate, KnobKind.Performance, 0.5f, 1, 0.25f, 0.5f),
        ];
        Knobs = Array.AsReadOnly(_specs);
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.GetProperty("schema").GetInt32() != 1)
            throw new InvalidDataException("Unsupported quality preset schema.");
        var presets = document.RootElement.GetProperty("presets");
        _presets = new float[Presets.Length][];
        foreach (var preset in Presets)
        {
            var row = presets.GetProperty(preset.ToString().ToLowerInvariant());
            if (row.EnumerateObject().Count() != Count) throw new InvalidDataException($"Each preset must contain all {Count} knobs.");
            var values = _presets[(int)preset] = new float[Count];
            foreach (var knob in All)
            {
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
        if (knob != QualityKnob.RenderScale) return _presets[(int)preset][(int)knob];
        return ScaleForOutput(preset, OutputWidth, OutputHeight);
    }

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
        return knob switch
        {
            QualityKnob.RenderScale => RenderScales.Any(scale => Math.Abs(scale - value) < 0.0001f),
            QualityKnob.ShadowDistance => value is 600 or 1000 or 1600,
            _ => true,
        };
    }

    public float[] Resolve(QualityProfile profile, QualityPreset customBase)
    {
        var preset = profile.Preset == QualityPreset.Custom ? customBase : profile.Preset;
        var values = All.Select(knob => PresetValue(preset, knob)).ToArray();
        foreach (var pair in profile.Overrides.OrderBy(pair => pair.Key)) values[(int)pair.Key] = pair.Value;
        if (!PreserveOutputSize && values[(int)QualityKnob.DisplayMode] == 0 && !profile.Overrides.ContainsKey(QualityKnob.RenderScale))
        {
            var size = WindowSizes[(int)values[(int)QualityKnob.WindowSize]];
            values[(int)QualityKnob.RenderScale] = ScaleForOutput(preset, size.Width, size.Height);
        }
        return values;
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
        float min, float max, float step, float worst, bool higher = true) =>
        new(knob, owner, apply, kind, min, max, step, worst, higher, "quality." + Name(knob));
}
