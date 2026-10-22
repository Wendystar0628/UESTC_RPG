using Campus.Contracts.Quality;
using KernelRegistry = Campus.Kernel.Registry.Kernel;
using Campus.Hud.Widgets;
using Godot;

namespace Campus.Hud.Pages;

// 画面设置仅经 IQualityService.Plan / Apply；未应用的计划在关闭页面后保留。
internal sealed class SettingsCardGraphics
{
    private readonly UiKit _kit;
    // 界面一侧的应用等待只有一份，归界面根持有、随 HUD 离树取消；关页不取消，未应用的计划要保留。
    private readonly HudTask _applying;
    private QualityProfile? _queued;   // 已明确请求应用的那份计划；之后才改、还没点"应用"的加载项不借它提交

    public SettingsCardGraphics(UiKit kit, HudTask applying)
    {
        _kit = kit;
        _applying = applying;
    }

    private readonly Dictionary<QualityKnob, float> _pending = [];
    private readonly Dictionary<QualityKnob, Action<float>> _knobSync = [];
    private Label? _applyStatus;
    private TechButton? _apply;
    private Dropdown? _preset;
    private VBoxContainer? _graphicsActions;
    private QualityProfile? _planned;

    public System.Collections.IEnumerable Build(Control page, Control pageActions)
    {
        var quality = KernelRegistry.Get<IQualityService>();
        var presets = Enum.GetValues<QualityPreset>();
        _preset = Dropdown.Create(_kit, page, presets.Select(preset => UiKit.T("quality.preset." + preset.ToString().ToLowerInvariant())).ToArray(),
            Array.IndexOf(presets, _planned?.Preset ?? quality.Requested.Preset));
        _preset.AccessibilityName = UiKit.T("settings.tab.graphics");
        _preset.Changed += index =>
        {
            if (presets[index] == QualityPreset.Custom) return;
            _pending.Clear();
            Plan(new QualityProfile(presets[index], new Dictionary<QualityKnob, float>()));
            SyncKnobs();
        };
        yield return null;
        foreach (var group in quality.Knobs.GroupBy(spec => spec.Group))
        {
            var content = _kit.Layout.SettingGroup(page, UiKit.T("quality.group." + group.Key));
            foreach (var spec in group)
            {
                var row = _kit.Layout.SettingRow(content, UiKit.T(spec.LabelKey));
                var editor = _kit.Layout.Stack(row, groups: false);
                KnobControl(editor, spec, RequestedValue(spec.Knob));
                if (spec.Apply is ApplyClass.UnderLoading or ApplyClass.Restart)
                    Parts.Tag(_kit, editor, UiKit.T(spec.Apply == ApplyClass.Restart ? "quality.tag.restart" : "quality.tag.loading"), spec.Apply == ApplyClass.UnderLoading);
                yield return null;
            }
        }
        _kit.Text(page, UiKit.T("quality.footnote"), "small", "text.2");
        _graphicsActions = _kit.Layout.Stack(pageActions, groups: false);
        _applyStatus = _kit.Text(_graphicsActions, "", "small", "text.2");
        _applyStatus.Visible = false;
        var actions = _kit.Layout.Flow(_graphicsActions);
        _apply = _kit.Button(actions, ButtonStyle.Primary, UiKit.T("quality.apply"), "button.primary", pressed: () => { if (_planned is { } plan) Request(plan); });
        _apply.OffReason = UiKit.T("quality.apply.nothing");
        _apply.Off = true;
        if (_planned is { } planned) ShowPlan(quality.Plan(planned));
    }

    private void KnobControl(Control parent, KnobSpec spec, float value)
    {
        if (spec.Values is { Count: > 0 } values)
        {
            int Index(float next) => Enumerable.Range(0, values.Count).MinBy(index => Math.Abs(values[index] - next));
            var control = Dropdown.Create(_kit, parent, values.Select(next => FormatKnob(spec, next)).ToArray(), Index(value));
            control.AccessibilityName = UiKit.T(spec.LabelKey);
            control.Changed += index => Change(spec.Knob, values[index]);
            _knobSync[spec.Knob] = next => control.Select(Index(next), notify: false);
            return;
        }
        var steps = spec.Step > 0 ? (int)Math.Round((spec.Max - spec.Min) / spec.Step) + 1 : 0;
        if (spec.Step >= 1 && steps >= 2)
        {
            var labels = Enumerable.Range(0, steps).Select(index => StepLabel(spec, steps, spec.Min + index * spec.Step)).ToArray();
            var control = Dropdown.Create(_kit, parent, labels, (int)Math.Round((value - spec.Min) / spec.Step));
            control.AccessibilityName = UiKit.T(spec.LabelKey);
            control.Changed += index => Change(spec.Knob, spec.Min + index * spec.Step);
            _knobSync[spec.Knob] = next => control.Select((int)Math.Round((next - spec.Min) / spec.Step), notify: false);
            return;
        }
        var step = spec.Step > 0 ? spec.Step : (spec.Max - spec.Min) / 20f;
        var fader = Fader.Create(_kit, parent, spec.Min, spec.Max, step, value, format: next => FormatKnob(spec, next));
        fader.AccessibilityName = UiKit.T(spec.LabelKey);
        fader.Changed += next => Change(spec.Knob, next);
        _knobSync[spec.Knob] = next => fader.Set(next, notify: false);
    }

    private float RequestedValue(QualityKnob knob)
    {
        if (_pending.TryGetValue(knob, out var pending)) return pending;
        var quality = KernelRegistry.Get<IQualityService>();
        if (_planned is not { } plan) return quality.Effective.Requested(knob);
        if (plan.Overrides.TryGetValue(knob, out var value)) return value;
        return plan.Preset == QualityPreset.Custom ? quality.Effective.Requested(knob) : quality.PresetValue(plan.Preset, knob);
    }

    private void SyncKnobs()
    {
        foreach (var (knob, set) in _knobSync) set(RequestedValue(knob));
    }

    private static string StepLabel(KnobSpec spec, int steps, float value)
    {
        var key = $"{spec.LabelKey}.{value:0}";
        if (spec.Knob == QualityKnob.FrameRateLimit && RefreshHz((int)value) is { } hz) return UiKit.T(key + ".hz", ("hz", $"{hz}"));
        return Campus.Hud.I18n.Tr.Has(key) ? UiKit.T(key) : steps == 2 ? UiKit.T(value > 0 ? "ui.common.on" : "ui.common.off") : $"{value:0}";
    }

    // 只有跟随刷新率这一档要在标签里带出实际赫兹数；读不到刷新率时只显示档名。
    private static int? RefreshHz(int code)
    {
        if (code != 2) return null;
        var rate = DisplayServer.ScreenGetRefreshRate(DisplayServer.WindowGetCurrentScreen());
        if (!float.IsFinite(rate) || rate < 24) return null;
        return (int)MathF.Floor(rate);
    }

    private static string FormatKnob(KnobSpec spec, float value) => spec.Unit switch
    {
        KnobUnit.Scale => $"{value:0.00}×",
        KnobUnit.Metres => $"{value:0} m",
        KnobUnit.Percent => $"{value * 100:0}%",
        KnobUnit.Number => $"{value:0.##}",
        _ => throw new ArgumentOutOfRangeException(nameof(spec), "Unknown knob unit."),
    };

    private void Change(QualityKnob knob, float value)
    {
        var quality = KernelRegistry.Get<IQualityService>();
        _pending[knob] = value;
        _preset?.Select((int)QualityPreset.Custom, notify: false);
        var basis = _planned ?? quality.Requested;
        var overrides = new Dictionary<QualityKnob, float>(basis.Overrides);
        // Changing a preset's knob keeps the other preset values even before its pending application.
        if (basis.Preset != quality.Requested.Preset && basis.Preset != QualityPreset.Custom)
            foreach (var spec in quality.Knobs) overrides[spec.Knob] = quality.PresetValue(basis.Preset, spec.Knob);
        foreach (var (changed, next) in _pending) overrides[changed] = next;
        Plan(new QualityProfile(QualityPreset.Custom, overrides));
    }

    private void Plan(QualityProfile next)
    {
        var plan = KernelRegistry.Get<IQualityService>().Plan(next);
        _planned = next;
        ShowPlan(plan);
        if (plan.Heaviest is not (ApplyClass.UnderLoading or ApplyClass.Restart) && plan.Rejected.Count == 0) Request(next);
    }

    private void ShowPlan(ApplyPlan plan)
    {
        if (_applyStatus is null || _apply is null) return;
        var screen = plan.Heaviest == ApplyClass.UnderLoading;
        var restart = plan.Heaviest == ApplyClass.Restart;
        _applyStatus.Text = plan.Deltas.Count == 0 && _pending.Count == 0 ? ""
            : UiKit.T(restart ? "quality.pending.restart" : screen ? "quality.pending.loading" : "quality.pending.now", ("n", $"{Math.Max(plan.Deltas.Count, _pending.Count)}"));
        _applyStatus.Visible = _applyStatus.Text.Length > 0;
        _apply.Off = plan.Rejected.Count > 0 || plan.Deltas.Count == 0;
    }

    // 应用由 IQualityService 持有（SYS-GRAPHICS）；等待期间再次请求只记下这次请求的计划，这次结束后再应用它，不并行开第二次。
    private void Request(QualityProfile profile)
    {
        _queued = profile;
        _applying.Start(ApplyQueued);
    }

    private async Task ApplyQueued(CancellationToken token)
    {
        ApplyResult result;
        QualityProfile applied;
        do
        {
            applied = _queued ?? throw new InvalidOperationException("Apply started without a requested plan.");
            _queued = null;
            var apply = KernelRegistry.Get<IQualityService>().Apply(applied, ApplyOrigin.User);
            try { result = await apply.WaitAsync(token); }
            // 画质服务卸下时取消排队中的请求：按任务自身的取消状态认，不算失败。
            catch (OperationCanceledException) when (apply.IsCanceled) { return; }
        }
        while (_queued is not null);
        // 应用期间又改出、还没提交的计划留着等"应用"，只刷新待应用提示。
        if (!ReferenceEquals(_planned, applied))
        {
            if (_planned is { } newer) ShowPlan(KernelRegistry.Get<IQualityService>().Plan(newer));
            return;
        }
        _pending.Clear();
        _planned = null;
        if (_applyStatus is null || _apply is null) return;
        _applyStatus.Text = result.Status switch
        {
            ApplyStatus.Applied => result.RestartPending ? UiKit.T("quality.result.restart") : "",
            ApplyStatus.Deferred => UiKit.T("quality.result.deferred"),
            ApplyStatus.Rejected => UiKit.T("quality.result.rejected"),
            _ => throw new InvalidOperationException("Unknown apply status."),
        };
        _applyStatus.Visible = _applyStatus.Text.Length > 0;
        _apply.Off = true;
        SyncKnobs();
    }

    public void Refresh()
    {
        if (_preset is null || _pending.Count > 0 || _planned is not null) return;
        _preset.Select((int)KernelRegistry.Get<IQualityService>().Requested.Preset, notify: false);
        SyncKnobs();
    }

    public void ShowActions(bool visible)
    {
        if (_graphicsActions is not null) _graphicsActions.Visible = visible;
    }

    public void Release()
    {
        _knobSync.Clear();
        _preset = null;
        _applyStatus = null;
        _apply = null;
        _graphicsActions = null;
    }
}
