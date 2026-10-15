using Campus.Contracts;
using Godot;

namespace Campus.Hud;

public enum UiThemeMode : byte { Auto, Light, Dark }
public enum ResidentTheme : byte { Dark, Follow }
public enum ReduceMotionMode : byte { System, On, Off }

/// <summary>
/// 界面的观者设置（UI-NAV §4.6），存 user://settings.cfg（档案级，不进存档）。
/// 读：先按文本查 Object( 等写法，只接受 bool、int、float、String，越界或类型不对用默认值；
/// 写：EncodeToText → .tmp → Flush(true) → File.Replace（留 .bak），改动后 0.5 s 写一次（SYS-SAVE §8）。
/// </summary>
[KernelService(typeof(HudSettings), Owner = "WP-35")]
public sealed class HudSettings
{
    public const string FilePath = "user://settings.cfg";
    private const string Section = "ui";
    private static readonly string[] Unsafe = ["Object(", "Resource(", "SubResource(", "ExtResource("];

    private float _dirtyFor = -1;

    public HudSettings() => Load();

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
        var cfg = new ConfigFile();
        cfg.SetValue(Section, "theme", (int)Theme);
        cfg.SetValue(Section, "resident", (int)Resident);
        cfg.SetValue(Section, "reduce_motion", (int)ReduceMotion);
        cfg.SetValue(Section, "reduce_transparency", ReduceTransparency);
        cfg.SetValue(Section, "frost", Frost);
        cfg.SetValue(Section, "ui_scale", UiScale);
        cfg.SetValue(Section, "single_key_shortcuts", SingleKeyShortcuts);
        var path = ProjectSettings.GlobalizePath(FilePath);
        var tmp = path + ".tmp";
        try
        {
            using (var stream = new FileStream(tmp, FileMode.Create, System.IO.FileAccess.Write, FileShare.None))
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(cfg.EncodeToText());
                stream.Write(bytes);
                stream.Flush(true);
            }
            if (File.Exists(path)) File.Replace(tmp, path, path + ".bak");
            else File.Move(tmp, path);
        }
        catch (IOException error) { GD.PushWarning($"settings.cfg not written: {error.Message}"); }
        catch (UnauthorizedAccessException error) { GD.PushWarning($"settings.cfg not written: {error.Message}"); }
    }

    private void Load()
    {
        var path = ProjectSettings.GlobalizePath(FilePath);
        var cfg = Read(path) ?? Read(path + ".bak");
        if (cfg is null) return;
        Theme = (UiThemeMode)ReadInt(cfg, "theme", 0, 2, (int)Theme);
        Resident = (ResidentTheme)ReadInt(cfg, "resident", 0, 1, (int)Resident);
        ReduceMotion = (ReduceMotionMode)ReadInt(cfg, "reduce_motion", 0, 2, (int)ReduceMotion);
        ReduceTransparency = ReadBool(cfg, "reduce_transparency", ReduceTransparency);
        Frost = ReadBool(cfg, "frost", Frost);
        UiScale = ReadFloat(cfg, "ui_scale", 0.75f, 1.5f, UiScale);
        SingleKeyShortcuts = ReadBool(cfg, "single_key_shortcuts", SingleKeyShortcuts);
    }

    private static ConfigFile? Read(string path)
    {
        if (!File.Exists(path)) return null;
        string text;
        try { text = File.ReadAllText(path); }
        catch (IOException) { return null; }
        if (Unsafe.Any(token => text.Contains(token, StringComparison.Ordinal))) return null;
        var cfg = new ConfigFile();
        return cfg.Parse(text) == Error.Ok ? cfg : null;
    }

    private static int ReadInt(ConfigFile cfg, string key, int min, int max, int fallback)
    {
        var v = cfg.GetValue(Section, key, fallback);
        return v.VariantType == Variant.Type.Int && v.AsInt32() >= min && v.AsInt32() <= max ? v.AsInt32() : fallback;
    }

    private static float ReadFloat(ConfigFile cfg, string key, float min, float max, float fallback)
    {
        var v = cfg.GetValue(Section, key, fallback);
        if (v.VariantType is not (Variant.Type.Float or Variant.Type.Int)) return fallback;
        var f = (float)v.AsDouble();
        return float.IsFinite(f) && f >= min && f <= max ? f : fallback;
    }

    private static bool ReadBool(ConfigFile cfg, string key, bool fallback)
    {
        var v = cfg.GetValue(Section, key, fallback);
        return v.VariantType == Variant.Type.Bool ? v.AsBool() : fallback;
    }
}
