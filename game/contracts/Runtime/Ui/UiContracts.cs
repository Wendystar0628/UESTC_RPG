using System;
using System.Numerics;
using System.Threading.Tasks;

namespace Campus.Contracts;

public interface IMotionSettings
{
    bool ReduceMotion { get; }
    event Action? Changed;
}

// 时间环的天色光谱：WP-37 实现；界面从服务源读取季节与天气，在相关选择变化时取一次，不逐帧
public interface IPaletteProbe
{
    // 24 个整点的太阳侧与背阳侧地平线天色平均值，sRGB 0–1；长度必须恰为 24，否则抛 ArgumentException。
    // 已含 weather 修正，尚未乘界面的 0.7 彩度；换季转场期间调用方传 IWorldState.SeasonTarget。
    void SampleHorizon(Season season, WeatherVec weather, Span<Vector3> srgb24);
}

// 其他包补的只读状态（SYS-AUDIO）
public readonly record struct LightningFlash(double RealSeconds, float AzDeg, float ElDeg, float DistanceKm,
    int Pulses, float Peak, bool Visible);              // Visible = false：减少动态效果时只发事件、不画
public interface ILightningFeed { event Action<LightningFlash>? Flash; }     // WP-40：远雷的节拍（SYS-AUDIO）
public interface IChannelReader { float Value(ChannelId id); }               // WP-03 / WP-39：灯光通道的当前值
