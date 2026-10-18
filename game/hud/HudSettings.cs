using Campus.Contracts;
using Campus.Contracts.Settings;

namespace Campus.Hud;

public enum ResidentTheme : byte { Dark, Follow }
public enum ReduceMotionMode : byte { System, On, Off }

public static class HudSettingDefinitions
{
    public static readonly SettingDefinition<int> Theme = new("theme", (int)UiThemeMode.Auto, "settings.theme", Options: [new((int)UiThemeMode.Auto, "settings.theme.auto"), new((int)UiThemeMode.Light, "settings.theme.light"), new((int)UiThemeMode.Dark, "settings.theme.dark")]);
    public static readonly SettingDefinition<int> Resident = new("resident", (int)ResidentTheme.Dark, "settings.resident", Options: [new((int)ResidentTheme.Dark, "settings.resident.dark"), new((int)ResidentTheme.Follow, "settings.resident.follow")]);
    public static readonly SettingDefinition<int> ReduceMotion = new("reduce_motion", (int)ReduceMotionMode.System, "settings.reduce_motion", Options: [new((int)ReduceMotionMode.System, "settings.follow_system"), new((int)ReduceMotionMode.On, "ui.common.on"), new((int)ReduceMotionMode.Off, "ui.common.off")]);
    public static readonly SettingDefinition<bool> ReduceTransparency = new("reduce_transparency", false, "settings.reduce_transparency");
    public static readonly SettingDefinition<bool> Frost = new("frost", true, "settings.frost");
    public static SettingDefinition<float> UiScale => UiSettingDefinitions.Scale;
    public static readonly SettingDefinition<bool> SingleKeyShortcuts = new("single_key_shortcuts", true, "settings.single_keys");
    public static readonly SettingDefinition<bool> ShowPerf = new("show_perf", false, "settings.show_perf");
}

/// <summary>
/// 界面的观者设置（UI-NAV），存 user://settings.json 的 ui 节（档案级，不进存档）。
/// 读写经 ISettingsStore（越界或类型不对用默认值）；改动后 0.5 s 写一次（SYS-SAVE-FORMAT）。
/// </summary>
[KernelService(typeof(HudSettings), Owner = "WP-35")]
public sealed class HudSettings
{
    private readonly ISettingsStore _store;
    private readonly ISettingsSection _section;
    private float _dirtyFor = -1;

    public HudSettings(ISettingsStore store)
    {
        _store = store;
        _section = store.Section("ui");
        Load();
    }

    public UiThemeMode Theme { get; private set; } = (UiThemeMode)HudSettingDefinitions.Theme.Default;
    public ResidentTheme Resident { get; private set; } = (ResidentTheme)HudSettingDefinitions.Resident.Default;
    public ReduceMotionMode ReduceMotion { get; private set; } = (ReduceMotionMode)HudSettingDefinitions.ReduceMotion.Default;
    public bool ReduceTransparency { get; private set; } = HudSettingDefinitions.ReduceTransparency.Default;
    public bool Frost { get; private set; } = HudSettingDefinitions.Frost.Default;
    public float UiScale { get; private set; } = HudSettingDefinitions.UiScale.Default;
    public bool SingleKeyShortcuts { get; private set; } = HudSettingDefinitions.SingleKeyShortcuts.Default;
    public bool ShowPerf { get; private set; } = HudSettingDefinitions.ShowPerf.Default;
    public long Rev { get; private set; }
    public event Action? Changed;

    public void SetTheme(UiThemeMode v) => Set(() => Theme = v, Theme != v);
    public void SetResident(ResidentTheme v) => Set(() => Resident = v, Resident != v);
    public void SetReduceMotion(ReduceMotionMode v) => Set(() => ReduceMotion = v, ReduceMotion != v);
    public void SetReduceTransparency(bool v) => Set(() => ReduceTransparency = v, ReduceTransparency != v);
    public void SetFrost(bool v) => Set(() => Frost = v, Frost != v);
    public void SetUiScale(float v) => Set(() => UiScale = Math.Clamp(v, (float)HudSettingDefinitions.UiScale.Range!.Min, (float)HudSettingDefinitions.UiScale.Range.Max), Math.Abs(UiScale - v) > 0.001f);
    public void SetSingleKeyShortcuts(bool v) => Set(() => SingleKeyShortcuts = v, SingleKeyShortcuts != v);
    public void SetShowPerf(bool v) => Set(() => ShowPerf = v, ShowPerf != v);

    private void Set(Action apply, bool changed)
    {
        if (!changed) return;
        apply();
        Rev++;
        _dirtyFor = 0;
        Changed?.Invoke();
    }

    /// <summary>HudRoot 每帧调用：改动后 0.5 s 写盘。</summary>
    public void Tick(double delta)
    {
        if (_dirtyFor < 0) return;
        _dirtyFor += (float)delta;
        if (_dirtyFor >= 0.5f) Flush();
    }

    /// <summary>关窗口、退出前同步写一次。</summary>
    public void Flush()
    {
        if (_dirtyFor < 0) return;
        _dirtyFor = -1;
        _section.Set(HudSettingDefinitions.Theme.Key, (int)Theme);
        _section.Set(HudSettingDefinitions.Resident.Key, (int)Resident);
        _section.Set(HudSettingDefinitions.ReduceMotion.Key, (int)ReduceMotion);
        _section.Set(HudSettingDefinitions.ReduceTransparency.Key, ReduceTransparency);
        _section.Set(HudSettingDefinitions.Frost.Key, Frost);
        _section.Set(HudSettingDefinitions.UiScale.Key, UiScale);
        _section.Set(HudSettingDefinitions.SingleKeyShortcuts.Key, SingleKeyShortcuts);
        _section.Set(HudSettingDefinitions.ShowPerf.Key, ShowPerf);
        _store.TrySave();
    }

    private void Load()
    {
        Theme = (UiThemeMode)_section.Get(HudSettingDefinitions.Theme);
        Resident = (ResidentTheme)_section.Get(HudSettingDefinitions.Resident);
        ReduceMotion = (ReduceMotionMode)_section.Get(HudSettingDefinitions.ReduceMotion);
        ReduceTransparency = _section.Get(HudSettingDefinitions.ReduceTransparency);
        Frost = _section.Get(HudSettingDefinitions.Frost);
        UiScale = _section.Get(HudSettingDefinitions.UiScale);
        SingleKeyShortcuts = _section.Get(HudSettingDefinitions.SingleKeyShortcuts);
        ShowPerf = _section.Get(HudSettingDefinitions.ShowPerf);
    }
}
