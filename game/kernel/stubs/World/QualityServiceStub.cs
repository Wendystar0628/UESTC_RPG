using System.Collections.ObjectModel;
using Campus.Contracts;
using Campus.Contracts.Quality;

namespace Campus.Kernel.Stubs;

/// <summary>D0 quality service: fixed Fine/DgpuMid readouts, with no engine mutation or persistence.</summary>
[KernelService(typeof(IQualityService), Owner = "WP-00", IsStub = true)]
public sealed class QualityServiceStub : IQualityService
{
    // Enum-valued knobs follow the order listed in SYS-GRAPHICS section 3.
    // WindowSize exposes one placeholder (1080p) until WP-32 enumerates the display.
    // Values for resolution-dependent presets below use the 1080p column.
    private static readonly float[][] Presets =
    [
        [1, 0, 1, 1, 0, 0.59f, 0.2f, 1, 4, 0, 600, 0, 1, 1, 1, 1, 0.7f, 2, 0, 0.75f, 12, 4, 0.5f, 0, 0, 0, 1],
        [1, 0, 1, 1, 0, 0.67f, 0.2f, 1, 8, 0, 1000, 1, 1, 1, 1, 1, 0.8f, 1.5f, 0, 1, 20, 3, 0.75f, 0, 0, 0, 1],
        [1, 0, 1, 1, 0, 1, 0.2f, 0, 16, 1, 1600, 1, 0, 0, 1, 1, 0.9f, 1, 1, 1, 20, 2, 1, 0, 0, 0, 1],
        [1, 0, 1, 3, 0, 1, 0.2f, 0, 16, 2, 1600, 1, 0, 0, 1, 1, 1, 1, 1, 1, 20, 2, 1, 0, 0, 0, 1],
    ];

    public DeviceTier Tier => DeviceTier.DgpuMid;
    public QualityPreset Recommended => QualityPreset.Fine;
    public QualityProfile Requested { get; } = new(QualityPreset.Fine,
        new ReadOnlyDictionary<QualityKnob, float>(new Dictionary<QualityKnob, float>()), false);
    public QualityView Effective { get; } = new(1, QualityPreset.Fine, DeviceTier.DgpuMid,
        false, Presets[(int)QualityPreset.Fine]);
    public IReadOnlyList<KnobSpec> Knobs { get; } = Array.AsReadOnly<KnobSpec>(
    [
        Spec(QualityKnob.DisplayMode, "WP-32", ApplyClass.UnderVeil, KnobKind.Performance, 0, 2, 1, 0),
        Spec(QualityKnob.WindowSize, "WP-32", ApplyClass.UnderVeil, KnobKind.Performance, 0, 0, 1, 0),
        Spec(QualityKnob.VSync, "WP-32", ApplyClass.Immediate, KnobKind.Performance, 0, 1, 1, 0),
        Spec(QualityKnob.FrameCap, "WP-32", ApplyClass.Immediate, KnobKind.Performance, 0, 4, 1, 0),
        Spec(QualityKnob.Vrr, "WP-32", ApplyClass.Immediate, KnobKind.Performance, 0, 1, 1, 0),
        Spec(QualityKnob.RenderScale, "WP-32", ApplyClass.UnderVeil, KnobKind.Performance, 0.5f, 1.25f, 0, 0.5f),
        Spec(QualityKnob.FsrSharpness, "WP-32", ApplyClass.UnderVeil, KnobKind.Performance, 0, 2, 0.1f, 0),
        Spec(QualityKnob.AntiAliasing, "WP-05", ApplyClass.UnderVeil, KnobKind.Performance, 0, 1, 1, 1, higherIsBetter: false),
        Spec(QualityKnob.Anisotropy, "WP-04", ApplyClass.UnderVeil, KnobKind.Performance, 2, 16, 0, 2),
        Spec(QualityKnob.ShadowQuality, "WP-03", ApplyClass.Immediate, KnobKind.Floor, 0, 2, 1, 0, adaptive: true),
        Spec(QualityKnob.ShadowDistance, "WP-03", ApplyClass.Immediate, KnobKind.Performance, 600, 1600, 0, 600, adaptive: true),
        Spec(QualityKnob.SmallCasters, "WP-03", ApplyClass.UnderVeil, KnobKind.Performance, 0, 1, 1, 0),
        Spec(QualityKnob.GlowQuality, "WP-05", ApplyClass.Immediate, KnobKind.Performance, 0, 1, 1, 1, higherIsBetter: false, adaptive: true),
        Spec(QualityKnob.TiltShift, "WP-05", ApplyClass.Immediate, KnobKind.StyleDefault, 0, 2, 1, 2, higherIsBetter: false),
        Spec(QualityKnob.LensFlare, "WP-05", ApplyClass.Immediate, KnobKind.StyleDefault, 0, 1, 1, 0),
        Spec(QualityKnob.LightShafts, "WP-05", ApplyClass.Immediate, KnobKind.StyleDefault, 0, 1, 1, 0),
        Spec(QualityKnob.DetailScale, "WP-02", ApplyClass.Amortized, KnobKind.Performance, 0.7f, 1, 0.1f, 0.7f, adaptive: true),
        Spec(QualityKnob.MeshLodThreshold, "WP-02", ApplyClass.Immediate, KnobKind.Performance, 1, 2, 0.5f, 2, higherIsBetter: false, adaptive: true),
        Spec(QualityKnob.GeometricLeaves, "WP-23", ApplyClass.Immediate, KnobKind.Performance, 0, 1, 1, 0),
        Spec(QualityKnob.FoliageDensity, "WP-23", ApplyClass.Immediate, KnobKind.Performance, 0.75f, 1, 0.25f, 0.75f, adaptive: true),
        Spec(QualityKnob.GrassRadius, "WP-23", ApplyClass.Immediate, KnobKind.Performance, 12, 20, 8, 12),
        Spec(QualityKnob.ReflectionInterval, "WP-24", ApplyClass.Immediate, KnobKind.Floor, 2, 4, 1, 4, higherIsBetter: false, adaptive: true),
        Spec(QualityKnob.ParticleDensity, "WP-45", ApplyClass.Immediate, KnobKind.Performance, 0.5f, 1, 0.25f, 0.5f, adaptive: true),
        Spec(QualityKnob.Adaptive, "WP-32", ApplyClass.Immediate, KnobKind.Performance, 0, 1, 1, 0),
        Spec(QualityKnob.GraphicsApi, "WP-32", ApplyClass.Restart, KnobKind.Performance, 0, 1, 1, 0),
        Spec(QualityKnob.GpuIndex, "WP-32", ApplyClass.Restart, KnobKind.Performance, 0, 0, 1, 0),
        Spec(QualityKnob.Vignette, "WP-05", ApplyClass.Immediate, KnobKind.StyleDefault, 0, 1, 1, 0),
    ]);

    // Fixed effective values never produce a change notification in the stub.
    public event Action<QualityChange>? Changed { add { } remove { } }

    public float PresetValue(QualityPreset p, QualityKnob k)
    {
        if (!Enum.IsDefined(k)) throw new ArgumentOutOfRangeException(nameof(k));
        var index = p switch
        {
            QualityPreset.Smooth => 0,
            QualityPreset.Balanced => 1,
            QualityPreset.Fine => 2,
            QualityPreset.Ultra => 3,
            QualityPreset.Custom => throw new ArgumentException("Custom has no independent preset table.", nameof(p)),
            _ => throw new ArgumentOutOfRangeException(nameof(p)),
        };
        return Presets[index][(int)k];
    }

    public ApplyPlan Plan(QualityProfile next)
    {
        ArgumentNullException.ThrowIfNull(next);
        return new ApplyPlan(next, Array.Empty<KnobDelta>(), ApplyClass.Immediate, false,
            0, 0, Array.Empty<QualityKnob>());
    }

    public Task<ApplyResult> Apply(QualityProfile next, ApplyOrigin origin)
    {
        ArgumentNullException.ThrowIfNull(next);
        if (!Enum.IsDefined(origin)) throw new ArgumentOutOfRangeException(nameof(origin));
        if (origin is ApplyOrigin.Adaptive)
            throw new ArgumentException("Adaptive is a notification origin, not an apply request.", nameof(origin));
        return Task.FromResult(new ApplyResult(ApplyStatus.Applied, Effective, false, false,
            Array.Empty<QualityKnob>()));
    }

    public void ConfirmDisplay() { }

    private static KnobSpec Spec(QualityKnob knob, string owner, ApplyClass apply, KnobKind kind,
        float min, float max, float step, float worst, bool higherIsBetter = true, bool adaptive = false) =>
        new(knob, owner, apply, kind, min, max, step, worst, higherIsBetter, adaptive,
            "quality." + System.Text.Json.JsonNamingPolicy.SnakeCaseLower.ConvertName(knob.ToString()));
}
