using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Bake;
using Campus.Contracts.Perf;
using Campus.Contracts.Save;
using Campus.Contracts.Settings;
using Campus.Hud.Widgets;
using KernelRegistry = Campus.Kernel.Registry.Kernel;
using Godot;

namespace Campus.Hud.Pages;

/// <summary>逐行构造无独立编辑会话的设置正文；页面调度由 SettingsCard 持有。</summary>
internal sealed class SettingsCardContent
{
    private readonly UiKit _kit;
    private readonly HudSettings _settings;
    private readonly SettingsCardBindings _bindings;
    private readonly HudRequests _recording;

    public SettingsCardContent(UiKit kit, HudSettings settings, SettingsCardBindings bindings, HudRequests recording)
    {
        _kit = kit;
        _settings = settings;
        _bindings = bindings;
        _recording = recording;
    }

    public System.Collections.IEnumerable BuildGeneral(Control page)
    {
        foreach (var step in Group(page, "settings.group.interface",
        [
            HudRow(HudSettingDefinitions.Theme),
            HudRow(HudSettingDefinitions.Resident),
            HudRow(HudSettingDefinitions.Frost),
            HudRow(HudSettingDefinitions.ReduceTransparency),
            HudRow(HudSettingDefinitions.ReduceMotion),
            HudRow(HudSettingDefinitions.UiScale, Percent),
            Row(UiSettingDefinitions.AutoJump, ReadView(UiSettingDefinitions.AutoJump), value => WriteView(UiSettingDefinitions.AutoJump, value), null),
            HudRow(HudSettingDefinitions.SingleKeyShortcuts),
        ])) yield return step;

        foreach (var step in BuildStorage(page)) yield return step;
    }

    /// <summary>
    /// 画面页借用的 ui 节行：镜头与舒适组的视场与视角晃动、页尾性能信息组的开关。存储仍在 ui 节，
    /// 分组、改动标记与恢复默认由画面页按这些行做。Recording 建性能信息组的记录行与状态行，不是借用行。
    /// </summary>
    public (IReadOnlyList<SettingsUiRow> Comfort, IReadOnlyList<SettingsUiRow> Performance,
        Func<Control, (HBoxContainer Record, HBoxContainer Status)> Recording) GraphicsRows() =>
    (
        [
            UiRow(UiSettingDefinitions.FlyFov, ReadView(UiSettingDefinitions.FlyFov), value => WriteView(UiSettingDefinitions.FlyFov, value), Degrees),
            UiRow(UiSettingDefinitions.WalkFov, ReadView(UiSettingDefinitions.WalkFov), value => WriteView(UiSettingDefinitions.WalkFov, value), Degrees),
            UiRow(UiSettingDefinitions.ViewBobbing, ReadView(UiSettingDefinitions.ViewBobbing), value => WriteView(UiSettingDefinitions.ViewBobbing, value), null),
        ],
        [UiRow(HudSettingDefinitions.ShowPerf, () => _settings.Get(HudSettingDefinitions.ShowPerf), value => _settings.Set(HudSettingDefinitions.ShowPerf, value), null)],
        BuildRecording
    );

    internal const string RecordingStatusKey = "graphics.performance.status";

    /// <summary>
    /// 记录性能数据（ENG-PERF-DATA）：开关与状态只经记录服务读写，不碰设置存储，不算改动、不随恢复默认变。
    /// 开关显示记录意图；状态行订阅状态变化，页面可见时按引擎步长每秒重读一次已录时长；离树只退订与停止刷新。
    /// </summary>
    private (HBoxContainer Record, HBoxContainer Status) BuildRecording(Control group)
    {
        var recorder = KernelRegistry.Get<IPerformanceRecorder>();
        var label = UiKit.T(PerfSettingDefinitions.RecordPerformance.LabelKey);
        var record = _kit.Layout.SettingRow(group, label);
        var toggle = DipSwitch.Create(_kit, record, recorder.Snapshot.Requested);
        toggle.AccessibilityName = label;
        var statusRow = _kit.Layout.SettingRow(group, UiKit.T(RecordingStatusKey));
        var status = _kit.Text(statusRow, "", "data", "text.2");
        // 每次开关当场把意图交给服务，由 HUD 持有的请求队列逐个等完（HudRequests）；设置卡离树不丢弃已交出的请求与其失败。
        toggle.Toggled += on => _recording.Submit(() => recorder.SetRecordingAsync(on));
        void Show(PerfRecordingSnapshot snapshot)
        {
            toggle.Set(snapshot.Requested, notify: false);
            status.Text = RecordingStatus(snapshot);
            _kit.Recolor(status, snapshot.State == PerfRecordingState.Faulted ? "danger.text" : "text.2");
        }
        // 已录时长由服务按单调时钟现算；界面不取时钟（G4），只用引擎步长定刷新节拍。
        var since = 0.0;
        void Tick()
        {
            if (!status.IsVisibleInTree()) return;
            since += status.GetProcessDeltaTime();
            if (since < 1) return;
            since = 0;
            if (recorder.Snapshot is { State: PerfRecordingState.Recording } snapshot) Show(snapshot);
        }
        var tree = group.GetTree();
        recorder.StateChanged += Show;
        tree.ProcessFrame += Tick;
        status.TreeExiting += () =>
        {
            recorder.StateChanged -= Show;
            tree.ProcessFrame -= Tick;
        };
        Show(recorder.Snapshot);
        return (record, statusRow);
    }

    // 五态各一个文案键，故障按故障码取键，不显示异常文字；只有实际进入 Recording 才显示"记录中 · 时长"。
    private static string RecordingStatus(PerfRecordingSnapshot snapshot) => snapshot.State switch
    {
        PerfRecordingState.Off => UiKit.T(RecordingStatusKey + ".off"),
        PerfRecordingState.Starting => UiKit.T(RecordingStatusKey + ".starting"),
        PerfRecordingState.Recording => UiKit.T(RecordingStatusKey + ".recording", ("time", Elapsed(snapshot.RecordedSeconds))),
        PerfRecordingState.Stopping => UiKit.T(RecordingStatusKey + ".stopping"),
        PerfRecordingState.Faulted => UiKit.T(snapshot.Fault switch
        {
            PerfFaultCode.InvalidDictionary => "graphics.performance.fault.invalid_dictionary",
            PerfFaultCode.OpenFailed => "graphics.performance.fault.open_failed",
            PerfFaultCode.WriteFailed => "graphics.performance.fault.write_failed",
            PerfFaultCode.ProducerFailed => "graphics.performance.fault.producer_failed",
            PerfFaultCode.FinalizeFailed => "graphics.performance.fault.finalize_failed",
            _ => throw new InvalidOperationException($"Faulted recording without a fault code ({snapshot.Fault})."),
        }),
        _ => throw new InvalidOperationException("Unknown recording state."),
    };

    private static string Elapsed(double seconds)
    {
        var total = (long)Math.Floor(Math.Max(0, seconds));
        return total >= 3600 ? $"{total / 3600}:{total / 60 % 60:00}:{total % 60:00}" : $"{total / 60:00}:{total % 60:00}";
    }

    // 视场、视角晃动与自动跳在 ui 节里，HUD 建立之前就被读取（UiSettingDefinitions），改了立刻写盘，不经 HudSettings 的延迟写。
    private static Func<T> ReadView<T>(SettingDefinition<T> definition) where T : notnull =>
        () => (T)SettingValues.Read(KernelRegistry.Get<ISettingsStore>().Section("ui"), definition).Value;

    private static void WriteView<T>(SettingDefinition<T> definition, T value) where T : notnull
    {
        var store = KernelRegistry.Get<ISettingsStore>();
        SettingValues.Write(store.Section("ui"), definition.Key, value);
        store.TrySave();
    }

    private System.Collections.IEnumerable BuildStorage(Control page)
    {
        var storageGroup = _kit.Layout.SettingGroup(page, UiKit.T("settings.group.storage"));
        var storageRow = _kit.Layout.SettingRow(storageGroup, UiKit.T("settings.storage"));
        var storage = _kit.Text(storageRow, "", "data", "text.2");
        void SyncStorage()
        {
            var bake = KernelRegistry.Get<IBakeService>();
            var progress = bake.Progress;
            var state = progress.Total == 0 || progress.Done >= progress.Total ? UiKit.T("settings.storage.done")
                : UiKit.T("settings.storage.running", ("pct", $"{progress.Done * 100 / Math.Max(1, progress.Total)}"));
            storage.Text = $"{bake.CacheBytes / 1048576.0:0.0} MB · {state}";
        }
        SyncStorage();
        page.VisibilityChanged += SyncStorage;
        yield return null;
        var saveRow = _kit.Layout.SettingRow(storageGroup, UiKit.T("settings.save"));
        var save = _kit.Text(saveRow, "", "data", "text.2");
        void SyncSave()
        {
            var state = KernelRegistry.Get<ISaveService>().State;
            save.Text = state.ReadOnly switch
            {
                ReadOnlyReason.None => state.Slot is { } slot ? $"{slot.Value.ToUpperInvariant()} · {(state.Last == SaveStatus.Failed ? UiKit.T("settings.save.failed") : UiKit.T("settings.save.ok"))}" : UiKit.T("settings.save.none"),
                ReadOnlyReason.SaveOff => UiKit.T("settings.save.off"),
                ReadOnlyReason.OtherWindow => UiKit.T("settings.save.other_window"),
                _ => throw new InvalidOperationException("Unknown read-only reason."),
            };
            _kit.Recolor(save, state.Last == SaveStatus.Failed ? "danger.text" : "text.2");
        }
        SyncSave();
        page.VisibilityChanged += SyncSave;
    }

    public System.Collections.IEnumerable BuildSound(Control page)
    {
        // 音频设置是一整条记录经 ApplySettings 生效，定义里没有应用委托，所以每行写明取值与 with
        var audio = KernelRegistry.Get<IAudioService>();
        Action<Control> Sound<T>(SettingDefinition<T> definition, Func<AudioSettings, T> read, Func<AudioSettings, T, AudioSettings> apply,
            Func<float, string>? format = null) where T : notnull =>
            Row(definition, () => read(audio.Settings), value => audio.ApplySettings(apply(audio.Settings, value)), format);
        foreach (var step in Group(page, "sound.group.volume",
        [
            Sound(AudioSettingDefinitions.Master, s => s.Master, (s, v) => s with { Master = v }, Percent),
            Sound(AudioSettingDefinitions.Ambience, s => s.Ambience, (s, v) => s with { Ambience = v }, Percent),
            Sound(AudioSettingDefinitions.Foley, s => s.Foley, (s, v) => s with { Foley = v }, Percent),
            Sound(AudioSettingDefinitions.Ui, s => s.Ui, (s, v) => s with { Ui = v }, Percent),
            Sound(AudioSettingDefinitions.Music, s => s.Music, (s, v) => s with { Music = v }, Percent),
        ])) yield return step;
        foreach (var step in Group(page, "sound.group.calm",
        [
            Sound(AudioSettingDefinitions.UiSounds, s => s.UiSounds, (s, v) => s with { UiSounds = v }),
            Sound(AudioSettingDefinitions.HoverSounds, s => s.HoverSounds, (s, v) => s with { HoverSounds = v }),
            Sound(AudioSettingDefinitions.QuietMode, s => s.QuietMode, (s, v) => s with { QuietMode = v }),
            Sound(AudioSettingDefinitions.Mono, s => s.Mono, (s, v) => s with { Mono = v }),
            Sound(AudioSettingDefinitions.Sudden, s => (int)s.Sudden, (s, v) => s with { Sudden = (SuddenSoundMode)v }),
            Sound(AudioSettingDefinitions.Background, s => (int)s.Background, (s, v) => s with { Background = (BackgroundBehavior)v }),
        ])) yield return step;
    }

    private static string Percent(float value) => $"{value * 100:0}%";
    private static string Degrees(float value) => $"{value:0}°";

    public System.Collections.IEnumerable BuildKeys(Control page)
    {
        var group = _kit.Layout.SettingGroup(page, UiKit.T("settings.group.keys"));
        var menu = _kit.Layout.SettingRow(group, UiKit.T("keys.menu"));
        Parts.Keycap(_kit, menu, _bindings.KeyLabel("menu"));
        yield return null;
        foreach (var action in SettingsCardBindings.KeyboardActions())
        {
            var row = _kit.Layout.SettingRow(group, UiKit.T("rebind.action." + action));
            Parts.Keycap(_kit, row, _bindings.KeyLabel(action));
            yield return null;
        }
        _kit.Text(page, UiKit.T("keys.note"), "small", "text.2");
    }

    /// <summary>一组设置：组标题加逐行，每建一行让出一帧（由 SettingsCard 分帧调度）。</summary>
    private System.Collections.IEnumerable Group(Control page, string titleKey, IEnumerable<Action<Control>> rows)
    {
        var group = _kit.Layout.SettingGroup(page, UiKit.T(titleKey));
        foreach (var row in rows)
        {
            row(group);
            yield return null;
        }
    }

    private Action<Control> HudRow<T>(SettingDefinition<T> definition, Func<float, string>? format = null) where T : notnull =>
        Row(definition, () => _settings.Get(definition), value => _settings.Set(definition, value), format);

    private Action<Control> Row<T>(SettingDefinition<T> definition, Func<T> read, Action<T> write, Func<float, string>? format) where T : notnull =>
        parent => Build(parent, definition, read(), write, format);

    private SettingsUiRow UiRow<T>(SettingDefinition<T> definition, Func<T> read, Action<T> write, Func<float, string>? format) where T : notnull
    {
        Action<T>? sync = null;
        return new(definition.LabelKey,
            (parent, changed) =>
            {
                var (row, set) = Build(parent, definition, read(), value => { write(value); changed(); }, format);
                sync = set;
                return row;
            },
            () => !EqualityComparer<T>.Default.Equals(read(), definition.Default),
            () =>
            {
                write(definition.Default);
                sync?.Invoke(definition.Default);
            });
    }

    /// <summary>按定义生成一行：bool 是开关，带选项的 int 是下拉，带范围的 float 是推子；文案取定义的 LabelKey。返回行与不发事件的同步委托。</summary>
    private (HBoxContainer Row, Action<T> Set) Build<T>(Control parent, SettingDefinition<T> definition, T value, Action<T> write,
        Func<float, string>? format) where T : notnull
    {
        switch ((object)definition)
        {
            case SettingDefinition<bool> d:
                {
                    var (row, set) = Toggle(parent, d, (bool)(object)value, next => write((T)(object)next));
                    return (row, next => set((bool)(object)next));
                }
            case SettingDefinition<int> d:
                {
                    var (row, set) = Choice(parent, d, (int)(object)value, next => write((T)(object)next));
                    return (row, next => set((int)(object)next));
                }
            case SettingDefinition<float> d:
                {
                    var (row, set) = Slider(parent, d, (float)(object)value, next => write((T)(object)next),
                        format ?? throw new InvalidOperationException($"Slider setting '{d.Key}' needs a value format."));
                    return (row, next => set((float)(object)next));
                }
            default:
                throw new NotSupportedException($"Unsupported setting definition {definition.GetType()}.");
        }
    }

    private (HBoxContainer, Action<bool>) Toggle(Control parent, SettingDefinition<bool> definition, bool value, Action<bool> changed)
    {
        var row = _kit.Layout.SettingRow(parent, UiKit.T(definition.LabelKey));
        var control = DipSwitch.Create(_kit, row, value);
        control.AccessibilityName = UiKit.T(definition.LabelKey);
        control.Toggled += changed;
        return (row, next => control.Set(next, notify: false));
    }

    private (HBoxContainer, Action<int>) Choice(Control parent, SettingDefinition<int> definition, int value, Action<int> changed)
    {
        var options = definition.Options ?? throw new InvalidOperationException("Setting options are required.");
        int Index(int next)
        {
            var index = options.ToList().FindIndex(option => option.Value == next);
            return index >= 0 ? index : options.ToList().FindIndex(option => option.Value == definition.Default);
        }
        var row = _kit.Layout.SettingRow(parent, UiKit.T(definition.LabelKey));
        var control = Dropdown.Create(_kit, row, options.Select(option => UiKit.T(option.LabelKey)).ToArray(), Index(value));
        control.AccessibilityName = UiKit.T(definition.LabelKey);
        control.Changed += index => changed(options[index].Value);
        return (row, next => control.Select(Index(next), notify: false));
    }

    private (HBoxContainer, Action<float>) Slider(Control parent, SettingDefinition<float> definition, float value, Action<float> changed,
        Func<float, string> format)
    {
        var range = definition.Range ?? throw new InvalidOperationException("Setting range is required.");
        var row = _kit.Layout.SettingRow(parent, UiKit.T(definition.LabelKey));
        var control = Fader.Create(_kit, row, (float)range.Min, (float)range.Max, (float)range.Step, value, format: format);
        control.AccessibilityName = UiKit.T(definition.LabelKey);
        control.Changed += changed;
        return (row, next => control.Set(next, notify: false));
    }
}

/// <summary>
/// 别的页借用的一行 ui 节设置：Build 建行并在玩家改值后回调；Modified 比较定义的默认值；
/// Reset 写回默认并同步已建的控件（未建时只写值）。
/// </summary>
internal sealed record SettingsUiRow(string LabelKey, Func<Control, Action, HBoxContainer> Build, Func<bool> Modified, Action Reset);
