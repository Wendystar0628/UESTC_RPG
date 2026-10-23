using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Bake;
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

    public SettingsCardContent(UiKit kit, HudSettings settings, SettingsCardBindings bindings)
    {
        _kit = kit;
        _settings = settings;
        _bindings = bindings;
    }

    public System.Collections.IEnumerable BuildGeneral(Control page)
    {
        var store = KernelRegistry.Get<ISettingsStore>();
        var view = store.Section("ui");
        // 视场、视角晃动与自动跳这四项在 ui 节里，HUD 建立之前就被读取（UiSettingDefinitions），改了立刻写盘，不经 HudSettings 的延迟写
        Action<Control> View<T>(SettingDefinition<T> definition, Func<float, string>? format = null) where T : notnull =>
            Row(definition, () => (T)SettingValues.Read(view, definition).Value, value =>
            {
                SettingValues.Write(view, definition.Key, value);
                store.TrySave();
            }, format);
        foreach (var step in Group(page, "settings.group.interface",
        [
            HudRow(HudSettingDefinitions.Theme),
            HudRow(HudSettingDefinitions.Resident),
            HudRow(HudSettingDefinitions.Frost),
            HudRow(HudSettingDefinitions.ReduceTransparency),
            HudRow(HudSettingDefinitions.ReduceMotion),
            HudRow(HudSettingDefinitions.UiScale, Percent),
            View(UiSettingDefinitions.FlyFov, Degrees),
            View(UiSettingDefinitions.WalkFov, Degrees),
            View(UiSettingDefinitions.ViewBobbing),
            View(UiSettingDefinitions.AutoJump),
            HudRow(HudSettingDefinitions.SingleKeyShortcuts),
        ])) yield return step;

        foreach (var step in BuildStorage(page)) yield return step;
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

    /// <summary>画面页末尾的性能信息组：仍存 ui 节的 show_perf，经 HudSettings 即时生效。</summary>
    public System.Collections.IEnumerable BuildPerformance(Control page) =>
        Group(page, "settings.group.perf", [HudRow(HudSettingDefinitions.ShowPerf)]);

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

    /// <summary>按定义生成一行：bool 是开关，带选项的 int 是下拉，带范围的 float 是推子；文案取定义的 LabelKey，值在建行时读。</summary>
    private Action<Control> Row<T>(SettingDefinition<T> definition, Func<T> read, Action<T> write, Func<float, string>? format) where T : notnull =>
        (object)definition switch
        {
            SettingDefinition<bool> d => parent => Toggle(parent, d, (bool)(object)read(), value => write((T)(object)value)),
            SettingDefinition<int> d => parent => Choice(parent, d, (int)(object)read(), value => write((T)(object)value)),
            SettingDefinition<float> d => parent => Slider(parent, d, (float)(object)read(), value => write((T)(object)value),
                format ?? throw new InvalidOperationException($"Slider setting '{d.Key}' needs a value format.")),
            _ => throw new NotSupportedException($"Unsupported setting definition {definition.GetType()}."),
        };

    private void Toggle(Control parent, SettingDefinition<bool> definition, bool value, Action<bool> changed)
    {
        var row = _kit.Layout.SettingRow(parent, UiKit.T(definition.LabelKey));
        var control = DipSwitch.Create(_kit, row, value);
        control.AccessibilityName = UiKit.T(definition.LabelKey);
        control.Toggled += changed;
    }

    private void Choice(Control parent, SettingDefinition<int> definition, int value, Action<int> changed)
    {
        var options = definition.Options ?? throw new InvalidOperationException("Setting options are required.");
        var selected = options.ToList().FindIndex(option => option.Value == value);
        if (selected < 0) selected = options.ToList().FindIndex(option => option.Value == definition.Default);
        var row = _kit.Layout.SettingRow(parent, UiKit.T(definition.LabelKey));
        var control = Dropdown.Create(_kit, row, options.Select(option => UiKit.T(option.LabelKey)).ToArray(), selected);
        control.AccessibilityName = UiKit.T(definition.LabelKey);
        control.Changed += index => changed(options[index].Value);
    }

    private void Slider(Control parent, SettingDefinition<float> definition, float value, Action<float> changed, Func<float, string> format)
    {
        var range = definition.Range ?? throw new InvalidOperationException("Setting range is required.");
        var row = _kit.Layout.SettingRow(parent, UiKit.T(definition.LabelKey));
        var control = Fader.Create(_kit, row, (float)range.Min, (float)range.Max, (float)range.Step, value, format: format);
        control.AccessibilityName = UiKit.T(definition.LabelKey);
        control.Changed += changed;
    }
}
