using System.Numerics;
using Campus.Contracts;

namespace Campus.Systems.Weather.Precip;

/// <summary>
/// 天气向量 → 天气类全局参数（ENG-PERF；写入方 WP-45b，FramePriority.Motion，contract-5 第 5 条）。
/// 45a 只交出 WeatherVec，全局参数只在这里换算，所以两边不会各写一份。
/// </summary>
public static class WeatherGlobalsMap
{
    /// <summary>贴地雾的雾顶最高 12 m（SYS-WEATHER"贴地雾 0–12 m"）。</summary>
    public const float GroundFogTopM = 12f;

    /// <summary>weather_a = (云量, 阴度, 降水量, 降水类型 0 雨 → 1 雪)。</summary>
    public static Vector4 A(in WeatherVec w) => new(Unit(w.Cloud), Unit(w.Overcast), Unit(w.Precip), Unit(w.PrecipKind));

    /// <summary>weather_b = (湿润, 积水, 风暴度, 彩虹)。</summary>
    public static Vector4 B(in WeatherVec w) => new(Unit(w.Wet), Unit(w.Puddle), Unit(w.Storm), Unit(w.Rainbow));

    /// <summary>weather_fog = (雾密度倍率（晴为 1）, 贴地雾顶高 m, 贴地雾密度, 远雨幕)。远雨幕只在下雨时有。</summary>
    public static Vector4 Fog(in WeatherVec w)
    {
        var mult = float.IsFinite(w.Fog) && w.Fog > 0f ? w.Fog : 1f;
        var ground = Unit(w.GroundFog);
        return new(mult, GroundFogTopM * ground, ground, Unit(w.Precip) * (1f - Unit(w.PrecipKind)));
    }

    private static float Unit(float x) => float.IsFinite(x) ? Math.Clamp(x, 0f, 1f) : 0f;
}
