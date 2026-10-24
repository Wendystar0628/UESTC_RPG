using Campus.Contracts.Perf;
using Campus.Contracts.Quality;
using Campus.Hud.I18n;
using Campus.Hud.Style;
using Campus.Hud.Widgets;
using Godot;
using KernelRegistry = Campus.Kernel.Registry.Kernel;

namespace Campus.Hud.Pages;

/// <summary>
/// 画面页（UI-NAV、SYS-GRAPHICS）：页首是效果预设分段与恢复默认，下面按 KnobScope 分组列出目录里的单项，
/// 镜头与舒适组另收 ui 节的视场与视角晃动，页尾是性能信息组；底部说明栏显示最近悬停或聚焦的一行。
/// 画面设置只经 IQualityService.Plan / Apply，请求带明确的编辑意图：预设分段发 SelectPreset，恢复默认发 RestoreDefaults，
/// 单项控件发 EditKnobs；同一份待应用计划里玩家明确改过的项累加进 EditedKnobs。未应用的计划在关闭页面后保留。
/// 性能信息组末尾的记录行与状态行只接说明栏，不亮改动标记、不随恢复默认变（SYS-GRAPHICS）。
/// </summary>
internal sealed class SettingsCardGraphics
{
    private const int ShortOptionChars = 8;    // 三档以内且每档不超过这么多字才用分段，否则用下拉
    private const float DimmedAlpha = 0.45f;   // 有禁用原因（ApplyPlan.Notes）的行，控件压暗

    private readonly UiKit _kit;
    // 界面一侧的应用等待只有一份，归界面根持有、随 HUD 离树取消；关页不取消，未应用的计划要保留。
    private readonly HudTask _applying;
    private QualityRequest? _queued;   // 已明确请求应用的那份计划；之后才改、还没点"应用"的加载项不借它提交
    private QualityRequest? _planned;  // 未应用的计划：意图、玩家改过的项与它们的取值，提交前一直保留
    private QualityProfile? _next;     // _planned 经 Plan 得出的完整配置（ApplyPlan.Next），控件与标记按它显示

    private readonly List<Row> _rows = [];
    private IReadOnlyList<KnobNote> _notes = [];
    private Row? _current;
    private Segmented? _preset;
    private TechButton? _reset, _apply;
    private SettingNoteBar? _note;
    private Label? _applyStatus;
    private VBoxContainer? _graphicsActions;
    private bool _armed;

    public SettingsCardGraphics(UiKit kit, HudTask applying)
    {
        _kit = kit;
        _applying = applying;
    }

    private static IQualityService Quality => KernelRegistry.Get<IQualityService>();

    /// <summary>一行：画质单项（Spec）、借来的 ui 节行（Ui），或两者都没有的预设行与记录行（Immediate 标出即时生效）。</summary>
    private sealed class Row(string labelKey)
    {
        public string LabelKey { get; } = labelKey;
        public bool Immediate { get; init; }
        public KnobSpec? Spec { get; init; }
        public SettingsUiRow? Ui { get; init; }
        public Control? Editor { get; init; }
        public Action<float>? Sync { get; init; }
        public Control? Marker { get; set; }
    }

    public System.Collections.IEnumerable Build(Control page, Control pageActions,
        (IReadOnlyList<SettingsUiRow> Comfort, IReadOnlyList<SettingsUiRow> Performance,
            Func<Control, (HBoxContainer Record, HBoxContainer Status)> Recording) uiRows)
    {
        // 页底在滚动区外：说明栏、待应用提示与"应用"。
        _graphicsActions = _kit.Layout.Stack(pageActions, groups: false);
        _note = SettingNoteBar.Create(_kit, _graphicsActions);
        _applyStatus = _kit.Text(_graphicsActions, "", "small", "text.2");
        _applyStatus.Visible = false;
        var actions = _kit.Layout.Flow(_graphicsActions);
        _apply = _kit.Button(actions, ButtonStyle.Primary, UiKit.T("quality.apply"), "button.primary", pressed: () => { if (_planned is { } plan) Request(plan); });
        _apply.OffReason = UiKit.T("quality.apply.nothing");
        _apply.Off = true;
        ShowNote();
        BuildPreset(page);
        yield return null;
        // 按作用域分组：契约里追加的单项按它的 Scope 自动进对应的组。
        var knobs = Quality.Knobs;
        foreach (var scope in Enum.GetValues<KnobScope>())
        {
            var specs = knobs.Where(spec => spec.Scope == scope).ToArray();
            var borrowed = scope == KnobScope.Comfort ? uiRows.Comfort : [];
            if (specs.Length == 0 && borrowed.Count == 0) continue;
            var group = _kit.Layout.SettingGroup(page, UiKit.T("quality.group." + scope.ToString().ToLowerInvariant()));
            foreach (var row in borrowed)
            {
                BorrowedRow(group, row);
                yield return null;
            }
            foreach (var spec in specs)
            {
                KnobRow(group, spec);
                yield return null;
            }
        }
        var performance = _kit.Layout.SettingGroup(page, UiKit.T("settings.group.perf"));
        foreach (var row in uiRows.Performance)
        {
            BorrowedRow(performance, row);
            yield return null;
        }
        var (record, status) = uiRows.Recording(performance);
        Track(record, new Row(PerfSettingDefinitions.RecordPerformance.LabelKey) { Immediate = true }, marker: false);
        Track(status, new Row(SettingsCardContent.RecordingStatusKey), marker: false);
        yield return null;
        _kit.Text(page, UiKit.T("quality.footnote"), "small", "text.2");
    }

    private void BuildPreset(Control page)
    {
        var presets = Enum.GetValues<QualityPreset>();
        var group = _kit.Layout.SettingGroup(page, UiKit.T("quality.group.preset"));
        var box = _kit.Layout.Row(group);
        _preset = Segmented.Create(_kit, box, presets.Select(preset => UiKit.T("quality.preset." + preset.ToString().ToLowerInvariant())).ToArray());
        _preset.AccessibilityName = UiKit.T("quality.preset");
        // "自定义"只显示状态：改动任一效果项后由页面选中，不能点。
        _preset.SetOff(Array.IndexOf(presets, QualityPreset.Custom), true, UiKit.T("quality.preset.custom.off"));
        _preset.Changed += index =>
        {
            if (presets[index] == QualityPreset.Custom) UpdateState();
            else ChoosePreset(presets[index]);
        };
        _reset = _kit.Button(box, ButtonStyle.Secondary, UiKit.T("quality.reset"), pressed: ResetPressed);
        _reset.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        _reset.OffReason = UiKit.T("quality.reset.off");
        _reset.FocusExited += Disarm;
        _reset.MouseExited += Disarm;
        Track(box, new Row("quality.preset"), marker: false, slot: false);
    }

    private void KnobRow(Control group, KnobSpec spec)
    {
        var box = _kit.Layout.SettingRow(group, UiKit.T(spec.LabelKey));
        var editor = _kit.Layout.Stack(box, groups: false);
        var sync = KnobControl(editor, spec, RequestedValue(spec.Knob));
        if (spec.Apply is ApplyClass.UnderLoading or ApplyClass.Restart)
            Parts.Tag(_kit, editor, UiKit.T(spec.Apply == ApplyClass.Restart ? "quality.tag.restart" : "quality.tag.loading"), spec.Apply == ApplyClass.UnderLoading);
        Track(box, new Row(spec.LabelKey) { Spec = spec, Editor = editor, Sync = sync });
    }

    private void BorrowedRow(Control group, SettingsUiRow ui) =>
        Track(ui.Build(group, UpdateState), new Row(ui.LabelKey) { Ui = ui });

    // 每行：名称前留改动标记的位置（不带标记的行只留空位与上下行对齐，预设行没有名称、不留），悬停或聚焦行内任一控件时说明栏换成这一行。
    private void Track(HBoxContainer box, Row row, bool marker = true, bool slot = true)
    {
        if (slot)
        {
            var diameter = _kit.Tokens.F("metrics", "via", "core");
            var holder = _kit.Layout.Add(box, new Control { CustomMinimumSize = new Vector2(diameter, diameter), SizeFlagsVertical = Control.SizeFlags.ShrinkCenter });
            box.MoveChild(holder, 0);
            if (marker)
            {
                var dot = _kit.Box(holder, new UiFx(Fill: "primary.line", Flags: UiFlags.Circle));
                dot.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
                dot.Visible = false;
                row.Marker = dot;
            }
        }
        box.MouseFilter = Control.MouseFilterEnum.Pass;
        void Point()
        {
            _current = row;
            ShowNote();
        }
        box.MouseEntered += Point;
        WatchFocus(box, Point);
        _rows.Add(row);
    }

    private static void WatchFocus(Control node, Action entered)
    {
        if (node.FocusMode != Control.FocusModeEnum.None) node.FocusEntered += entered;
        foreach (var child in node.GetChildren())
            if (child is Control control) WatchFocus(control, entered);
    }

    // 控件只按目录选（取值表、步长与档名），不按项特判：两档且没有档名 → 开关；三档以内且档名都短 → 分段；其余离散 → 下拉；连续 → 推子。
    private Action<float> KnobControl(Control parent, KnobSpec spec, float value)
    {
        var name = UiKit.T(spec.LabelKey);
        if (Options(spec) is not { } options)
        {
            var step = spec.Step > 0 ? spec.Step : (spec.Max - spec.Min) / 20f;
            var fader = Fader.Create(_kit, parent, spec.Min, spec.Max, step, value, format: next => FormatKnob(spec, next));
            fader.AccessibilityName = name;
            fader.Changed += next => Change(spec.Knob, next);
            return next => fader.Set(next, notify: false);
        }
        int Index(float next) => Enumerable.Range(0, options.Count).MinBy(index => Math.Abs(options[index] - next));
        if (options.Count == 2 && !options.Any(option => OptionKey(spec, option) is { } key && Tr.Has(key)))
        {
            var toggle = DipSwitch.Create(_kit, parent, Index(value) == 1);
            toggle.AccessibilityName = name;
            toggle.Toggled += on => Change(spec.Knob, options[on ? 1 : 0]);
            return next => toggle.Set(Index(next) == 1, notify: false);
        }
        var labels = options.Select(option => OptionLabel(spec, option)).ToArray();
        if (options.Count <= 3 && labels.All(label => label.Length <= ShortOptionChars))
        {
            var segments = Segmented.Create(_kit, parent, labels, selected: Index(value));
            segments.AccessibilityName = name;
            segments.Changed += index => Change(spec.Knob, options[index]);
            return next => segments.Select(Index(next), notify: false);
        }
        var dropdown = Dropdown.Create(_kit, parent, labels, Index(value));
        dropdown.AccessibilityName = name;
        dropdown.Changed += index => Change(spec.Knob, options[index]);
        return next => dropdown.Select(Index(next), notify: false);
    }

    private static IReadOnlyList<float>? Options(KnobSpec spec)
    {
        if (spec.Values is { Count: > 0 } values) return values;
        if (spec.Step < 1) return null;
        var steps = (int)Math.Round((spec.Max - spec.Min) / spec.Step) + 1;
        return steps >= 2 ? Enumerable.Range(0, steps).Select(index => spec.Min + index * spec.Step).ToArray() : null;
    }

    // 枚举类的项按序号编码成 float，档名键是 LabelKey.序号；非整数取值没有档名。
    private static string? OptionKey(KnobSpec spec, float value) =>
        value == MathF.Round(value) ? $"{spec.LabelKey}.{value:0}" : null;

    private static string OptionLabel(KnobSpec spec, float value)
    {
        if (OptionKey(spec, value) is not { } key || !Tr.Has(key)) return FormatKnob(spec, value);
        // 有 .hz 变体的档在标签里带出实际刷新率；读不到刷新率时只显示档名。
        return Tr.Has(key + ".hz") && RefreshHz() is { } hz ? UiKit.T(key + ".hz", ("hz", $"{hz}")) : UiKit.T(key);
    }

    private static int? RefreshHz()
    {
        var rate = DisplayServer.ScreenGetRefreshRate(DisplayServer.WindowGetCurrentScreen());
        if (!float.IsFinite(rate) || rate < 24) return null;
        return (int)MathF.Floor(rate);
    }

    private static string FormatKnob(KnobSpec spec, float value) => spec.Unit switch
    {
        KnobUnit.Scale => $"{value * 100:0}%",
        KnobUnit.Metres => $"{value:0} m",
        KnobUnit.Percent => $"{value * 100:0}%",
        KnobUnit.Number => $"{value:0.##}",
        _ => throw new ArgumentOutOfRangeException(nameof(spec), "Unknown knob unit."),
    };

    private static KnobSpec Spec(IQualityService quality, QualityKnob knob) => quality.Knobs.First(spec => spec.Knob == knob);

    private QualityProfile Shown(IQualityService quality) => _next ?? quality.Requested;

    // 有待应用计划时按服务给出的 ApplyPlan.Next 显示，界面不自己推算意图的效果。
    private float RequestedValue(QualityKnob knob) => RequestedValue(Quality, _next, knob);

    private static float RequestedValue(IQualityService quality, QualityProfile? planned, QualityKnob knob)
    {
        if (planned is not { } next) return quality.Effective.Requested(knob);
        if (next.Overrides.TryGetValue(knob, out var value)) return value;
        if (next.Preset == QualityPreset.Custom) return quality.Effective.Requested(knob);
        return quality.PresetValue(next.Preset, knob);
    }

    private static float DefaultValue(IQualityService quality, QualityKnob knob) =>
        quality.Default.Overrides.TryGetValue(knob, out var value) ? value : quality.PresetValue(quality.Default.Preset, knob);

    // 改动标记的参照：效果项比所选预设（"自定义"从哪来），其余比默认值。
    private static float Reference(IQualityService quality, QualityProfile shown, KnobSpec spec) =>
        spec.Scope == KnobScope.Effect && shown.Preset != QualityPreset.Custom ? quality.PresetValue(shown.Preset, spec.Knob) : DefaultValue(quality, spec.Knob);

    private static bool IsCustom(IQualityService quality, QualityProfile profile) => profile.Preset == QualityPreset.Custom ||
        profile.Overrides.Any(pair => Spec(quality, pair.Key).Scope == KnobScope.Effect && pair.Value != quality.PresetValue(profile.Preset, pair.Key));

    private void SyncKnobs()
    {
        foreach (var row in _rows)
            if (row is { Spec: { } spec, Sync: { } sync }) sync(RequestedValue(spec.Knob));
    }

    /// <summary>预设选中态、改动标记、禁用压暗、恢复默认可否与说明栏，都按当前显示的计划重算。</summary>
    private void UpdateState()
    {
        var quality = Quality;
        var shown = Shown(quality);
        _preset?.Select(Array.IndexOf(Enum.GetValues<QualityPreset>(), IsCustom(quality, shown) ? QualityPreset.Custom : shown.Preset), notify: false);
        var modified = shown.Preset != quality.Default.Preset;
        foreach (var row in _rows)
        {
            var changed = row.Spec is { } spec ? RequestedValue(spec.Knob) != Reference(quality, shown, spec) : row.Ui?.Modified() ?? false;
            if (row.Marker is { } marker) marker.Visible = changed;
            if (row.Editor is { } editor) editor.Modulate = new Color(1, 1, 1, Reason(row.Spec) is null ? 1 : DimmedAlpha);
            modified |= changed;
        }
        if (_reset is not null) _reset.Off = !modified;
        if (!modified) Disarm();
        ShowNote();
    }

    private string? Reason(KnobSpec? spec)
    {
        if (spec is null) return null;
        foreach (var note in _notes)
            if (note.Knob == spec.Knob) return UiKit.T(note.ReasonKey);
        return null;
    }

    private void ShowNote()
    {
        if (_note is null) return;
        if (_current is not { } row)
        {
            _note.Show(new("", "", UiKit.T("quality.note.idle")));
            return;
        }
        var spec = row.Spec;
        var condition = spec?.Apply switch
        {
            null => row.Ui is null && !row.Immediate ? "" : UiKit.T("quality.apply.immediate"),
            ApplyClass.Immediate or ApplyClass.Amortized => UiKit.T("quality.apply.immediate"),
            ApplyClass.UnderLoading => UiKit.T("quality.apply.loading"),
            ApplyClass.Restart => UiKit.T("quality.apply.restart"),
            _ => throw new InvalidOperationException("Unknown apply class."),
        };
        _note.Show(new(UiKit.T(row.LabelKey), condition, UiKit.T(row.LabelKey + ".hint"),
            spec is null ? null : Resolution(spec), Reason(spec), spec?.Load));
    }

    // 渲染精度这类倍率项写出"内部 → 输出"：输出是窗口的物理像素，内部按请求的倍率换算；文案键在目录项的 LabelKey 下。
    private string? Resolution(KnobSpec spec)
    {
        var upscaled = spec.LabelKey + ".upscaled";
        if (spec.Unit != KnobUnit.Scale || !Tr.Has(upscaled) || _note is null) return null;
        var scale = RequestedValue(spec.Knob);
        var output = _note.Root.GetWindow().Size;
        var outputText = $"{output.X}×{output.Y}";
        if (scale >= 1) return UiKit.T(spec.LabelKey + ".native", ("output", outputText));
        var inner = $"{(int)MathF.Round(output.X * scale)}×{(int)MathF.Round(output.Y * scale)}";
        return UiKit.T(upscaled, ("internal", inner), ("output", outputText));
    }

    private void ChoosePreset(QualityPreset preset)
    {
        Disarm();
        Plan(PresetRequest(Quality, _planned, _next, preset));
        SyncKnobs();
    }

    // 选预设由服务重置效果基底、保留独立偏好；本计划里玩家改过的非效果项继续算编辑，改过的效果项让位给预设。
    // 待应用的是恢复默认时，它复位的独立项也算玩家的选择：全部非效果项连同计划取值并进编辑，否则服务会沿用已生效的独立偏好（DECISION-322）。
    internal static QualityRequest PresetRequest(IQualityService quality, QualityRequest? pending, QualityProfile? planned, QualityPreset preset)
    {
        IEnumerable<QualityKnob> carried = pending is null ? [] : pending.Intent == QualityEditIntent.RestoreDefaults ? quality.Knobs.Select(spec => spec.Knob) : pending.EditedKnobs;
        var edited = carried.Where(knob => Spec(quality, knob).Scope != KnobScope.Effect).ToArray();
        var values = edited.ToDictionary(knob => knob, knob => RequestedValue(quality, planned, knob));
        return new QualityRequest(new QualityProfile(preset, values), QualityEditIntent.SelectPreset, edited);
    }

    internal static QualityRequest ResetRequest(IQualityService quality) => new(quality.Default, QualityEditIntent.RestoreDefaults, []);

    // 单项编辑沿用待应用计划的意图（选预设、恢复默认之后再改的项叠在其上），没有计划时是 EditKnobs；编辑项只增不减。
    private void Change(QualityKnob knob, float value)
    {
        Disarm();
        var basis = _planned ?? new QualityRequest(Quality.Requested, QualityEditIntent.EditKnobs, []);
        var overrides = new Dictionary<QualityKnob, float>(basis.Profile.Overrides) { [knob] = value };
        var edited = basis.EditedKnobs.Contains(knob) ? basis.EditedKnobs : [.. basis.EditedKnobs, knob];
        Plan(new QualityRequest(basis.Profile with { Overrides = overrides }, basis.Intent, edited));
    }

    // 恢复默认要按两次：第一次只把按钮换成确认文案，移开指针或焦点即撤销；第二次复位画质与本页的 ui 节行。
    private void ResetPressed()
    {
        if (_reset is null) return;
        if (!_armed)
        {
            _armed = true;
            _reset.Text = UiKit.T("quality.reset.confirm");
            return;
        }
        Disarm();
        foreach (var row in _rows) row.Ui?.Reset();
        Plan(ResetRequest(Quality));
        SyncKnobs();
    }

    private void Disarm()
    {
        if (!_armed) return;
        _armed = false;
        if (_reset is not null) _reset.Text = UiKit.T("quality.reset");
    }

    private void Plan(QualityRequest next)
    {
        var plan = Quality.Plan(next);
        _planned = next;
        ShowPlan(plan);
        if (plan.Heaviest is not (ApplyClass.UnderLoading or ApplyClass.Restart) && plan.Rejected.Count == 0) Request(next);
    }

    private void ShowPlan(ApplyPlan plan)
    {
        _next = plan.Next;
        _notes = plan.Notes;
        if (_applyStatus is not null && _apply is not null)
        {
            var screen = plan.Heaviest == ApplyClass.UnderLoading;
            var restart = plan.Heaviest == ApplyClass.Restart;
            _applyStatus.Text = plan.Deltas.Count == 0 ? ""
                : UiKit.T(restart ? "quality.pending.restart" : screen ? "quality.pending.loading" : "quality.pending.now", ("n", $"{plan.Deltas.Count}"));
            _applyStatus.Visible = _applyStatus.Text.Length > 0;
            _apply.Off = plan.Rejected.Count > 0 || plan.Deltas.Count == 0;
        }
        UpdateState();
    }

    private void ShowRequested()
    {
        SyncKnobs();
        _notes = Quality.Plan(new QualityRequest(Quality.Requested, QualityEditIntent.EditKnobs, [])).Notes;
        UpdateState();
    }

    // 应用由 IQualityService 持有（SYS-GRAPHICS）；等待期间再次请求只记下这次请求的计划，这次结束后再应用它，不并行开第二次。
    private void Request(QualityRequest request)
    {
        _queued = request;
        _applying.Start(ApplyQueued);
    }

    private async Task ApplyQueued(CancellationToken token)
    {
        ApplyResult result;
        QualityRequest applied;
        do
        {
            applied = _queued ?? throw new InvalidOperationException("Apply started without a requested plan.");
            _queued = null;
            var apply = Quality.Apply(applied, ApplyOrigin.User);
            try { result = await apply.WaitAsync(token); }
            // 画质服务卸下时取消排队中的请求：按任务自身的取消状态认，不算失败。
            catch (OperationCanceledException) when (apply.IsCanceled) { return; }
        }
        while (_queued is not null);
        // 应用期间又改出、还没提交的计划留着等"应用"，只刷新待应用提示。
        if (!ReferenceEquals(_planned, applied))
        {
            if (_planned is { } newer) ShowPlan(Quality.Plan(newer));
            return;
        }
        _planned = null;
        _next = null;
        if (_applyStatus is not null && _apply is not null)
        {
            _applyStatus.Text = result.Status switch
            {
                ApplyStatus.Applied => result.RestartPending ? UiKit.T("quality.result.restart") : "",
                ApplyStatus.Deferred => UiKit.T("quality.result.deferred"),
                ApplyStatus.Rejected => UiKit.T("quality.result.rejected"),
                _ => throw new InvalidOperationException("Unknown apply status."),
            };
            _applyStatus.Visible = _applyStatus.Text.Length > 0;
            _apply.Off = true;
        }
        ShowRequested();
    }

    public void Refresh()
    {
        if (_planned is { } plan) ShowPlan(Quality.Plan(plan));
        else ShowRequested();
    }

    public void ShowActions(bool visible)
    {
        if (_graphicsActions is not null) _graphicsActions.Visible = visible;
    }

    public void Release()
    {
        _rows.Clear();
        _current = null;
        _armed = false;
        _preset = null;
        _reset = null;
        _apply = null;
        _note = null;
        _applyStatus = null;
        _graphicsActions = null;
    }
}
