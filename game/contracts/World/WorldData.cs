using System.Numerics;

namespace Campus.Contracts;

public enum Season { Spring, Summer, Autumn, Winter }
public enum ViewMode { Fly, Walk }
// AltDeg：日月美术轨迹上的实际高度角；Sun 为美术太阳实际高度，不是 h_eff 或阴影钳制高度。
public readonly record struct Body(Vector3 Dir, float AltDeg, float AzDeg, float Intensity);

// 天气（SYS-WEATHER）：界面的 8 格（SYS-WEATHER）对应下面 11 个内部状态
public enum WeatherKind : byte { Clear, FewClouds, Cloudy, Overcast, Fog, LightRain, Rain, Thunderstorm, AfterRain, Snow, AfterSnow }
public readonly record struct WeatherVec(       // 天气向量：每帧插值；所有天气表现只读它和它写出的全局参数
    float Cloud, float CloudType, float Cb,     // 云量、云型、积雨云量
    float Overcast, float Precip, float PrecipKind,   // 阴度、降水量、降水类型（0 雨 → 1 雪）
    float Wet, float Puddle,                    // 湿润、积水
    float Fog, float GroundFog,                 // 雾密度倍率（晴为 1）、贴地雾
    float Storm, float SnowAdd,                 // 风暴度、天气积雪量 0..1.5；树冠消费自行封顶 1
    float Rainbow, float Lightning);            // 彩虹、闪光
public sealed record WorldPreset(double Clock, Season Season, WeatherKind? Weather = null, float? Intensity = null);

public enum ChannelKind { Scalar, Vec2, PerInstance }
public enum ChannelRate { Continuous, Step12 }   // Step12：按 IWorldState.Step12 步进，即 floor(AnimTime·StepRate)/StepRate，StepRate 同 step_rate
public sealed record ChannelDecl(
    ChannelId Id, ChannelKind Kind, float Min, float Max, ChannelRate Rate,
    string? Source = null);                 // 由灯光时刻表或世界状态驱动，例如 "window.dorm"、"lamp.street"（SYS-TIME）

// 随季节、昼夜显隐的物体（雪人、校庆道旗、毕业行李箱等）：几何照常生成，由内核按掩码显隐，不重建格子
[Flags] public enum SeasonMask : byte { Spring = 1, Summer = 2, Autumn = 4, Winter = 8, All = 15 }
public enum NightRule { Any, Only, Never }
public readonly record struct VariantTag(SeasonMask Seasons = SeasonMask.All, NightRule Night = NightRule.Any);
