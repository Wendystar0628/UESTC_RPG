using Campus.Contracts.Quality;
using Campus.Hud.Widgets;
using Godot;

namespace Campus.Hud.Pages;

// 画面页（SYS-GRAPHICS：只经 IQualityService.Plan / Apply）；没应用的改动跨开关保留
public partial class SettingsCard
{
    private readonly Dictionary<QualityKnob, float> _pending = [];
    private Label? _applyStatus;
    private TechButton? _apply;
    private Segmented? _preset;
    private static readonly (string Key, QualityKnob[] Knobs)[] Groups =
    [
        ("display", [QualityKnob.DisplayMode, QualityKnob.VSync, QualityKnob.FrameCap, QualityKnob.Vrr]),
        ("clarity", [QualityKnob.RenderScale, QualityKnob.FsrSharpness, QualityKnob.AntiAliasing, QualityKnob.Anisotropy]),
        ("lighting", [QualityKnob.ShadowQuality, QualityKnob.ShadowDistance, QualityKnob.SmallCasters]),
        ("post", [QualityKnob.GlowQuality, QualityKnob.TiltShift, QualityKnob.LensFlare, QualityKnob.LightShafts, QualityKnob.Vignette]),
        ("scene", [QualityKnob.DetailScale, QualityKnob.MeshLodThreshold, QualityKnob.GeometricLeaves, QualityKnob.FoliageDensity, QualityKnob.GrassRadius]),
        ("effects", [QualityKnob.ReflectionInterval, QualityKnob.ParticleDensity, QualityKnob.Adaptive]),
        ("advanced", [QualityKnob.GraphicsApi, QualityKnob.GpuIndex]),
    ];

    private float BuildGraphics(Control page, float width)
    {
        IQualityService quality;
        try { quality = Svc<IQualityService>(); }
        catch (InvalidOperationException) { _kit.Text(page, UiKit.T("settings.graphics.unavailable"), "small", "text.2", new Vector2(8, 8)); return 48; }
        float y = 8;
        _preset = Segmented.Create(_kit, page, new Vector2(8, y), [112, 112, 114, 112, 122],
            [UiKit.T("quality.preset.smooth"), UiKit.T("quality.preset.balanced"), UiKit.T("quality.preset.fine"), UiKit.T("quality.preset.ultra"), UiKit.T("quality.preset.custom")],
            false, selected: (int)(_planned?.Preset ?? quality.Requested.Preset));
        _preset.Changed += i =>
        {
            if ((QualityPreset)i == QualityPreset.Custom) return;
            _pending.Clear();
            Plan(new QualityProfile((QualityPreset)i, new Dictionary<QualityKnob, float>(), quality.Requested.Adaptive));
        };
        y += 46;
        var rec = UiKit.T("quality.recommended", ("preset", UiKit.T("quality.preset." + PresetKey(quality.Recommended))), ("gpu", RenderingServer.GetVideoAdapterName()));
        _kit.Text(page, rec, "small", "text.2", new Vector2(8, y), width: width - 8);
        y += 34;
        var specs = quality.Knobs.ToDictionary(s => s.Knob);
        foreach (var (group, knobs) in Groups)
        {
            Parts.GroupHeader(_kit, page, new Vector2(8, y), width, UiKit.T("quality.group." + group));
            y += 24;
            foreach (var knob in knobs)
            {
                if (!specs.TryGetValue(knob, out var spec)) continue;
                Row(page, y, UiKit.T(spec.LabelKey));
                KnobControl(page, new Vector2(ControlX, y + 2), spec, _pending.TryGetValue(knob, out var pending) ? pending : quality.Effective.Requested(knob));
                if (spec.Apply is ApplyClass.UnderVeil or ApplyClass.Restart)
                    Parts.Tag(_kit, page, new Vector2(width - 48, y + 8), UiKit.T(spec.Apply == ApplyClass.Restart ? "quality.tag.restart" : "quality.tag.veil"), spec.Apply == ApplyClass.UnderVeil);
                y += 40;
            }
            y += 12;
        }
        _kit.Text(page, UiKit.T("quality.footnote"), "small", "text.2", new Vector2(8, y), width: width - 8, wrap: true);
        y += 52;
        _applyStatus = _kit.Text(page, "", "small", "text.2", new Vector2(8, y + 12));
        _apply = _kit.Button(page, ButtonStyle.Primary, UiKit.T("quality.apply"), new Rect2(width - 152 + 8, y, 152, 44), "button.primary", 20,
            pressed: () => _ = ApplyPending());
        _apply.OffReason = UiKit.T("quality.apply.nothing");
        _apply.Off = true;
        if (_planned is { } planned) ShowPlan(quality.Plan(planned));   // 上次关卡时没应用的改动
        return y + 60;
    }

    private void KnobControl(Control page, Vector2 at, KnobSpec spec, float value)
    {
        var steps = spec.Step > 0 ? (int)Math.Round((spec.Max - spec.Min) / spec.Step) + 1 : 0;
        if (spec.Step >= 1 && steps is >= 2 and <= 5)
        {
            var labels = new string[steps];
            var widths = new float[steps];
            for (var i = 0; i < steps; i++)
            {
                var v = spec.Min + i * spec.Step;
                var key = $"{spec.LabelKey}.{v:0}";
                labels[i] = Campus.Hud.I18n.Tr.Has(key) ? UiKit.T(key) : steps == 2 ? UiKit.T(v > 0 ? "ui.common.on" : "ui.common.off") : $"{v:0}";
                widths[i] = steps <= 3 ? 72 : 56;
            }
            var seg = Segmented.Create(_kit, page, at, widths, labels, false, selected: (int)Math.Round((value - spec.Min) / spec.Step));
            seg.Changed += i => Change(spec.Knob, spec.Min + i * spec.Step);
            return;
        }
        var step = spec.Step > 0 ? spec.Step : (spec.Max - spec.Min) / 20f;
        var fader = Fader.Create(_kit, page, at + new Vector2(6, 0), 200, spec.Min, spec.Max, step, value, false, v => FormatKnob(spec, v));
        fader.Changed += v => Change(spec.Knob, v);
    }

    private static string FormatKnob(KnobSpec spec, float v) => spec.Knob switch
    {
        QualityKnob.RenderScale => $"{v:0.00}×",
        QualityKnob.ShadowDistance => $"{v:0} m",
        QualityKnob.GrassRadius => $"{v:0} m",
        QualityKnob.FoliageDensity or QualityKnob.ParticleDensity => $"{v * 100:0}%",
        _ => $"{v:0.##}",
    };

    private void Change(QualityKnob knob, float value)
    {
        IQualityService quality;
        try { quality = Svc<IQualityService>(); }
        catch (InvalidOperationException) { return; }
        _pending[knob] = value;
        _preset?.Select((int)QualityPreset.Custom, notify: false);
        var overrides = new Dictionary<QualityKnob, float>(quality.Requested.Overrides);
        foreach (var (k, v) in _pending) overrides[k] = v;
        Plan(new QualityProfile(QualityPreset.Custom, overrides, quality.Requested.Adaptive));
    }

    private QualityProfile? _planned;

    private void Plan(QualityProfile next)
    {
        var plan = Svc<IQualityService>().Plan(next);
        _planned = next;
        ShowPlan(plan);
        if (plan.Heaviest is not (ApplyClass.UnderVeil or ApplyClass.Restart) && plan.Rejected.Count == 0) _ = ApplyPending();
    }

    private void ShowPlan(ApplyPlan plan)
    {
        if (_applyStatus is null || _apply is null) return;
        var veil = plan.Heaviest == ApplyClass.UnderVeil;
        var restart = plan.Heaviest == ApplyClass.Restart;
        _applyStatus.Text = plan.Deltas.Count == 0 && _pending.Count == 0 ? ""
            : UiKit.T(restart ? "quality.pending.restart" : veil ? "quality.pending.veil" : "quality.pending.now", ("n", $"{Math.Max(plan.Deltas.Count, _pending.Count)}"));
        _apply.Off = plan.Rejected.Count > 0;
    }

    private async Task ApplyPending()
    {
        if (_planned is not { } next) return;
        var result = await Svc<IQualityService>().Apply(next, ApplyOrigin.User);
        _pending.Clear();
        _planned = null;
        if (_applyStatus is null || _apply is null) return;   // 等应用的这段时间里卡已经关了，或画面页还没填
        _applyStatus.Text = result.Status switch
        {
            ApplyStatus.Applied => result.RestartPending ? UiKit.T("quality.result.restart") : "",
            ApplyStatus.Deferred => UiKit.T("quality.result.deferred"),
            ApplyStatus.Rejected => UiKit.T("quality.result.rejected"),
            _ => throw new InvalidOperationException("Unknown apply status."),
        };
        _apply.Off = true;
    }

    private void RefreshGraphics()
    {
        if (_preset is null || _pending.Count > 0) return;
        try { _preset.Select((int)Svc<IQualityService>().Requested.Preset, notify: false); }
        catch (InvalidOperationException) { }
    }

    private static string PresetKey(QualityPreset p) => p switch
    {
        QualityPreset.Smooth => "smooth",
        QualityPreset.Balanced => "balanced",
        QualityPreset.Fine => "fine",
        QualityPreset.Ultra => "ultra",
        QualityPreset.Custom => "custom",
        _ => throw new ArgumentOutOfRangeException(nameof(p)),
    };
}
