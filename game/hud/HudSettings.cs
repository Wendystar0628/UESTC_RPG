using Campus.Contracts;
using Campus.Contracts.Settings;

namespace Campus.Hud;

public enum UiThemeMode : byte { Auto, Light, Dark }
public enum ResidentTheme : byte { Dark, Follow }
public enum ReduceMotionMode : byte { System, On, Off }

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

    public UiThemeMode Theme { get; private set; } = UiThemeMode.Auto;
    public ResidentTheme Resident { get; private set; } = ResidentTheme.Dark;
    public ReduceMotionMode ReduceMotion { get; private set; } = ReduceMotionMode.System;
    public bool ReduceTransparency { get; private set; }
    public bool Frost { get; private set; } = true;
    public float UiScale { get; private set; } = 1f;
    public bool SingleKeyShortcuts { get; private set; } = true;
    public long Rev { get; private set; }
    public event Action? Changed;

    public void SetTheme(UiThemeMode v) => Set(() => Theme = v, Theme != v);
    public void SetResident(ResidentTheme v) => Set(() => Resident = v, Resident != v);
    public void SetReduceMotion(ReduceMotionMode v) => Set(() => ReduceMotion = v, ReduceMotion != v);
    public void SetReduceTransparency(bool v) => Set(() => ReduceTransparency = v, ReduceTransparency != v);
    public void SetFrost(bool v) => Set(() => Frost = v, Frost != v);
    public void SetUiScale(float v) => Set(() => UiScale = Math.Clamp(v, 0.75f, 1.5f), Math.Abs(UiScale - v) > 0.001f);
    public void SetSingleKeyShortcuts(bool v) => Set(() => SingleKeyShortcuts = v, SingleKeyShortcuts != v);

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
        _section.Set("theme", (int)Theme);
        _section.Set("resident", (int)Resident);
        _section.Set("reduce_motion", (int)ReduceMotion);
        _section.Set("reduce_transparency", ReduceTransparency);
        _section.Set("frost", Frost);
        _section.Set("ui_scale", UiScale);
        _section.Set("single_key_shortcuts", SingleKeyShortcuts);
        _store.TrySave();
    }

    private void Load()
    {
        Theme = (UiThemeMode)_section.Int("theme", (int)Theme, 0, 2);
        Resident = (ResidentTheme)_section.Int("resident", (int)Resident, 0, 1);
        ReduceMotion = (ReduceMotionMode)_section.Int("reduce_motion", (int)ReduceMotion, 0, 2);
        ReduceTransparency = _section.Bool("reduce_transparency", ReduceTransparency);
        Frost = _section.Bool("frost", Frost);
        UiScale = _section.Float("ui_scale", UiScale, 0.75f, 1.5f);
        SingleKeyShortcuts = _section.Bool("single_key_shortcuts", SingleKeyShortcuts);
    }
}
