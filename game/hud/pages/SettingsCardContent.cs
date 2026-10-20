using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Bake;
using Campus.Contracts.Save;
using Campus.Contracts.Settings;
using Campus.Hud.Style;
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
        var ui = _kit.Layout.SettingGroup(page, UiKit.T("settings.group.interface"));
        Choice(ui, HudSettingDefinitions.Theme, (int)_settings.Theme, value => _settings.SetTheme((UiThemeMode)value));
        yield return null;
        Choice(ui, HudSettingDefinitions.Resident, (int)_settings.Resident, value => _settings.SetResident((ResidentTheme)value));
        yield return null;
        Toggle(ui, HudSettingDefinitions.Frost, _settings.Frost, _settings.SetFrost);
        yield return null;
        Toggle(ui, HudSettingDefinitions.ReduceTransparency, _settings.ReduceTransparency, _settings.SetReduceTransparency);
        yield return null;
        Choice(ui, HudSettingDefinitions.ReduceMotion, (int)_settings.ReduceMotion, value => _settings.SetReduceMotion((ReduceMotionMode)value));
        yield return null;
        Slider(ui, HudSettingDefinitions.UiScale, _settings.UiScale, _settings.SetUiScale, value => $"{value * 100:0}%");
        yield return null;
        var store = KernelRegistry.Get<ISettingsStore>();
        var viewSettings = store.Section("ui");
        foreach (var definition in new[] { UiSettingDefinitions.FlyFov, UiSettingDefinitions.WalkFov })
        {
            Slider(ui, definition, viewSettings.Get(definition), value =>
            {
                viewSettings.Set(definition.Key, value);
                store.TrySave();
            }, value => $"{value:0}°");
            yield return null;
        }
        Choice(ui, UiSettingDefinitions.ViewBobbing, viewSettings.Get(UiSettingDefinitions.ViewBobbing), value =>
        {
            viewSettings.Set(UiSettingDefinitions.ViewBobbing.Key, value);
            store.TrySave();
        });
        yield return null;
        Toggle(ui, UiSettingDefinitions.AutoJump, viewSettings.Get(UiSettingDefinitions.AutoJump), value =>
        {
            viewSettings.Set(UiSettingDefinitions.AutoJump.Key, value);
            store.TrySave();
        });
        yield return null;
        Toggle(ui, HudSettingDefinitions.SingleKeyShortcuts, _settings.SingleKeyShortcuts, _settings.SetSingleKeyShortcuts);
        yield return null;
        Toggle(ui, HudSettingDefinitions.ShowPerf, _settings.ShowPerf, _settings.SetShowPerf);
        yield return null;

        foreach (var step in BuildStorage(page)) yield return step;
    }

    private System.Collections.IEnumerable BuildStorage(Control page)
    {
        var storageGroup = _kit.Layout.SettingGroup(page, UiKit.T("settings.group.storage"));
        var storageRow = _kit.Layout.SettingRow(storageGroup, UiKit.T("settings.storage"));
        var storage = _kit.Text(storageRow, "", "data", "text.2");
        var clear = _kit.Button(storageRow, ButtonStyle.Secondary, UiKit.T("settings.storage.clear"));
        void SyncStorage()
        {
            var bake = KernelRegistry.Get<IBakeService>();
            var progress = bake.Progress;
            var state = progress.Total == 0 || progress.Done >= progress.Total ? UiKit.T("settings.storage.done")
                : UiKit.T("settings.storage.running", ("pct", $"{progress.Done * 100 / Math.Max(1, progress.Total)}"));
            storage.Text = $"{bake.CacheBytes / 1048576.0:0.0} MB · {state}";
        }
        clear.Pressed += () => { KernelRegistry.Get<IBakeService>().Clear(); SyncStorage(); };
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
        var audio = KernelRegistry.Get<IAudioService>();
        var volume = _kit.Layout.SettingGroup(page, UiKit.T("sound.group.volume"));
        Slider(volume, AudioSettingDefinitions.Master, audio.Settings.Master, value => audio.ApplySettings(audio.Settings with { Master = value }), Percent);
        yield return null;
        Slider(volume, AudioSettingDefinitions.Ambience, audio.Settings.Ambience, value => audio.ApplySettings(audio.Settings with { Ambience = value }), Percent);
        yield return null;
        Slider(volume, AudioSettingDefinitions.Foley, audio.Settings.Foley, value => audio.ApplySettings(audio.Settings with { Foley = value }), Percent);
        yield return null;
        Slider(volume, AudioSettingDefinitions.Ui, audio.Settings.Ui, value => audio.ApplySettings(audio.Settings with { Ui = value }), Percent);
        yield return null;
        Slider(volume, AudioSettingDefinitions.Music, audio.Settings.Music, value => audio.ApplySettings(audio.Settings with { Music = value }), Percent);
        yield return null;
        var calm = _kit.Layout.SettingGroup(page, UiKit.T("sound.group.calm"));
        Toggle(calm, AudioSettingDefinitions.UiSounds, audio.Settings.UiSounds, value => audio.ApplySettings(audio.Settings with { UiSounds = value }));
        yield return null;
        Toggle(calm, AudioSettingDefinitions.HoverSounds, audio.Settings.HoverSounds, value => audio.ApplySettings(audio.Settings with { HoverSounds = value }));
        yield return null;
        Toggle(calm, AudioSettingDefinitions.QuietMode, audio.Settings.QuietMode, value => audio.ApplySettings(audio.Settings with { QuietMode = value }));
        yield return null;
        Toggle(calm, AudioSettingDefinitions.Mono, audio.Settings.Mono, value => audio.ApplySettings(audio.Settings with { Mono = value }));
        yield return null;
        Choice(calm, AudioSettingDefinitions.Sudden, (int)audio.Settings.Sudden, value => audio.ApplySettings(audio.Settings with { Sudden = (SuddenSoundMode)value }));
        yield return null;
        Choice(calm, AudioSettingDefinitions.Background, (int)audio.Settings.Background, value => audio.ApplySettings(audio.Settings with { Background = (BackgroundBehavior)value }));
        yield return null;
    }

    private static string Percent(float value) => $"{value * 100:0}%";

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

    public System.Collections.IEnumerable BuildLicenses(Control page)
    {
        var table = _kit.Layout.Add(page, new GridContainer
        {
            Columns = 2,
            SizeFlagsHorizontal = Godot.Control.SizeFlags.ExpandFill,
        }, UiThemeBuilder.CardGrid);
        foreach (var name in new[] { "self", "osm", "fonts", "godot" })
        {
            var title = _kit.Text(table, UiKit.T("licenses.name." + name), "small", "text.2");
            title.SizeFlagsStretchRatio = 1;
            title.SizeFlagsVertical = Godot.Control.SizeFlags.ShrinkBegin;
            var detail = _kit.Text(table, UiKit.T("licenses." + name), "small", "text.1");
            detail.SizeFlagsStretchRatio = 3;
            yield return null;
        }
    }

    private DipSwitch Toggle(Control parent, SettingDefinition<bool> definition, bool value, Action<bool> changed)
    {
        var row = _kit.Layout.SettingRow(parent, UiKit.T(definition.LabelKey));
        var control = DipSwitch.Create(_kit, row, value);
        control.AccessibilityName = UiKit.T(definition.LabelKey);
        control.Toggled += changed;
        return control;
    }

    private Dropdown Choice(Control parent, SettingDefinition<int> definition, int value, Action<int> changed)
    {
        var options = definition.Options ?? throw new InvalidOperationException("Setting options are required.");
        var selected = options.ToList().FindIndex(option => option.Value == value);
        if (selected < 0) selected = options.ToList().FindIndex(option => option.Value == definition.Default);
        var row = _kit.Layout.SettingRow(parent, UiKit.T(definition.LabelKey));
        var control = Dropdown.Create(_kit, row, options.Select(option => UiKit.T(option.LabelKey)).ToArray(), selected);
        control.AccessibilityName = UiKit.T(definition.LabelKey);
        control.Changed += index => changed(options[index].Value);
        return control;
    }

    private Fader Slider(Control parent, SettingDefinition<float> definition, float value, Action<float> changed, Func<float, string> format)
    {
        var range = definition.Range ?? throw new InvalidOperationException("Setting range is required.");
        var row = _kit.Layout.SettingRow(parent, UiKit.T(definition.LabelKey));
        var control = Fader.Create(_kit, row, (float)range.Min, (float)range.Max, (float)range.Step, value, format: format);
        control.AccessibilityName = UiKit.T(definition.LabelKey);
        control.Changed += changed;
        return control;
    }

}
