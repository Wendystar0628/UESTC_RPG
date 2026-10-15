using System.Numerics;
using System.Text.Json.Serialization;
using Campus.Contracts;

namespace Campus.Systems.Nightlight.Schedule;

// game/data/maps/<地图>/lighting-schedule.json 的数据模型（SYS-TIME §6、MAP-QSH-ADDITIONS §8、ENG-PERF §10.3）。
// 用 ContractJson.Options 读写：字段 camelCase，向量写成数组。槽位号与 ChannelId 一一对应，32 个槽位全部登记。

public enum ChannelClass : byte
{
    Window,     // L = 亮窗比例 = clamp(dark(h_eff) × curve(钟点), 0, cap)，再取阴雨天的白天下限
    Lamp,       // L = 保留比例 × (1 + Δ)；每盏灯另按 lamp_table 里的太阳高度阈值与 h_eff 比较
    Cue,        // sys.cue：平时 (0, 0)；过场覆盖时 (1, 目标太阳高度)
    PoolGain,   // sys.poolGain：(IColorScriptFrame.LampPoolGain, 0)
    Reserved,   // 预留：恒为 (0, Δmin)
}

/// <summary>开灯阈值的分组：每盏灯的阈值在 [ToDeg, FromDeg] 里按哈希分布；两者 ≥ 89° 表示常亮。</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ThresholdGroup(float FromDeg, float ToDeg);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ChannelSpec(int Slot, ChannelId Id, ChannelClass Kind)
{
    /// <summary>按美术钟点的关键帧 (钟点 h, 值)，钟点递增、在 [0, 24) 内，首尾跨 24 h 线性衔接。</summary>
    public IReadOnlyList<Vector2> Curve { get; init; } = [];
    /// <summary>窗灯类的上限（窗灯总体 0.85，图书馆 0.9，外围住宅 0.5）。</summary>
    public float Cap { get; init; } = 0.85f;
    /// <summary>窗灯类的天黑程度 dark = smoothstep(Dark.X, Dark.Y, h_eff)，X &gt; Y；null 表示与天色无关（恒为 1）。</summary>
    public Vector2? Dark { get; init; }
    /// <summary>窗灯类是否取阴雨天的白天下限（SYS-WEATHER §2）。</summary>
    public bool DayFloor { get; init; }
    /// <summary>灯具类的开灯阈值分组名（groups 里的键）。</summary>
    public string? Group { get; init; }
    /// <summary>这条通道的灯默认用的色名（灯具标记没给颜色时）。</summary>
    public PaletteToken? Color { get; init; }
    public string? Note { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record EffectiveSunSpec(float OvercastDeg = 6f, float PrecipDeg = 4f);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record DeltaSpec(float Min = 0.005f, float Max = 0.15f, float Seconds = 0.3f);

/// <summary>阴雨天白天窗灯的下限：阴天 0.25、雨天 0.3（SYS-WEATHER §2），按阴度、降水量的两段 smoothstep 淡入。</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record DayFloorSpec(float Overcast = 0.25f, float Rain = 0.3f,
    Vector2? OvercastRamp = null, Vector2? RainRamp = null, Vector2? OccupancyRamp = null);

/// <summary>过场"初灯追镜头"（SYS-TITLE §3.3）：LampCueDef.Group 到通道的映射，以及接管这些灯的槽位。</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CueSpec(ChannelId Slot, ChannelId SysSlot, IReadOnlyDictionary<string, ChannelId> Groups);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record LightingScheduleFile(
    int Format,
    MapId Map,
    EffectiveSunSpec Heff,
    DeltaSpec Delta,
    DayFloorSpec DayFloor,
    IReadOnlyDictionary<string, ThresholdGroup> Groups,
    IReadOnlyList<ChannelSpec> Channels,
    CueSpec Cue)
{
    public const int ChannelCount = 32;
    public const int CurrentFormat = 1;
    public string? Note { get; init; }
}
