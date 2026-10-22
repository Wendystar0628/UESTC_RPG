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

    /// <summary>HudSettings 持有的全部项，按此顺序读入与写回；加一项设置只在本类写定义并登记到这里。</summary>
    public static readonly IReadOnlyList<object> All =
        [Theme, Resident, ReduceMotion, ReduceTransparency, Frost, UiScale, SingleKeyShortcuts, ShowPerf];
}

/// <summary>
/// 按定义的值类型分派到 ISettingsSection 的类型化读写；界面的设置项只有 bool、int、float 三类。
/// 契约的 Get 扩展按类型重载，泛型表要在这里收成一处分派。
/// </summary>
internal static class SettingValues
{
    public static (string Key, object Value) Read(ISettingsSection section, object definition) => definition switch
    {
        SettingDefinition<bool> d => (d.Key, (object)section.Get(d)),
        SettingDefinition<int> d => (d.Key, (object)section.Get(d)),
        SettingDefinition<float> d => (d.Key, (object)section.Get(d)),
        _ => throw new NotSupportedException($"Unsupported setting definition {definition.GetType()}."),
    };

    public static void Write(ISettingsSection section, string key, object value)
    {
        switch (value)
        {
            case bool b: section.Set(key, b); break;
            case int i: section.Set(key, i); break;
            case float f: section.Set(key, f); break;
            default: throw new NotSupportedException($"Unsupported setting value {value.GetType()} for '{key}'.");
        }
    }
}

/// <summary>
/// 界面的观者设置（UI-NAV），存 user://settings.json 的 ui 节（档案级，不进存档）。
/// 按 HudSettingDefinitions.All 表读写，读经 ISettingsStore（越界或类型不对用默认值）；改动后 0.5 s 写一次（SYS-SAVE-FORMAT）。
/// </summary>
[KernelService(typeof(HudSettings))]
public sealed class HudSettings
{
    private readonly ISettingsStore _store;
    private readonly ISettingsSection _section;
    private readonly Dictionary<string, object> _values = new(StringComparer.Ordinal);
    private readonly List<string> _keys = [];
    private float _dirtyFor = -1;

    public HudSettings(ISettingsStore store)
    {
        _store = store;
        _section = store.Section("ui");
        foreach (var definition in HudSettingDefinitions.All)
        {
            var (key, value) = SettingValues.Read(_section, definition);
            _values.Add(key, value);
            _keys.Add(key);
        }
    }

    public long Rev { get; private set; }
    public event Action? Changed;

    public T Get<T>(SettingDefinition<T> definition) => (T)_values[definition.Key];

    /// <summary>设置页的改动：浮点按定义的范围钳制；值变了才计版本、通知并安排写盘。</summary>
    public void Set<T>(SettingDefinition<T> definition, T value) where T : notnull
    {
        object next = value is float f && definition.Range is { } range ? (object)Math.Clamp(f, (float)range.Min, (float)range.Max) : value;
        if (_values[definition.Key].Equals(next)) return;
        _values[definition.Key] = next;
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

    /// <summary>关窗口、退出前同步写一次；按表的顺序写，文件里的键序固定。</summary>
    public void Flush()
    {
        if (_dirtyFor < 0) return;
        _dirtyFor = -1;
        foreach (var key in _keys) SettingValues.Write(_section, key, _values[key]);
        _store.TrySave();
    }
}
