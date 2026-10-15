using System;
using System.Numerics;
using System.Threading.Tasks;

namespace Campus.Contracts;

public interface IMotionSettings
{
    bool ReduceMotion { get; }
    event Action? Changed;
}

// 时间环的天色光谱：WP-37 实现；HudModel 在换季、换天气模式时取一次，不逐帧
public interface IPaletteProbe
{
    // 24 个整点的太阳侧与背阳侧地平线天色平均值，sRGB 0–1；长度必须恰为 24，否则抛 ArgumentException。
    // 已含 weather 修正，尚未乘界面的 0.7 彩度；换季转场期间调用方传 IWorldState.SeasonTarget。
    void SampleHorizon(Season season, WeatherVec weather, Span<Vector3> srgb24);
}

// 雾幕：WP-01 实现（雾幕层由 WP-05 画），转场、跳转、传送、换季、画质、回位、过场、切图共用一层
public enum VeilReason : byte { Boot, Transit, Teleport, Season, Quality, Respawn, Cinematic, MapTravel }   // 在 Campus.Contracts，不在音频命名空间
public enum VeilContent : byte { Fog, Frontispiece }    // 雾色；扉页插画（SYS-TITLE §1.3）
public interface IVeilState { float Opacity { get; } VeilReason Reason { get; } }   // 只读；音频据此压低 World 组（SYS-AUDIO §7.2）
public interface IVeilService : IVeilState             // 草案：签名由 WP-01 在 v1 里补全
{
    Task Cover(VeilReason reason, float seconds, VeilContent content = VeilContent.Fog);   // 盖满时完成
    void HoldUntil(VeilReason reason, Func<bool> ready, float maxWaitSeconds);            // 保持不透明直到条件满足（SYS-TITLE §10 的 hold）
    Task Reveal(VeilReason reason, float seconds);
}

// 其他包补的只读状态（SYS-AUDIO §19）
public readonly record struct LightningFlash(double RealSeconds, float AzDeg, float ElDeg, float DistanceKm,
    int Pulses, float Peak, bool Visible);              // Visible = false：减少动态效果时只发事件、不画
public interface ILightningFeed { event Action<LightningFlash>? Flash; }     // WP-40：远雷的节拍（SYS-AUDIO §6.1）
public interface IChannelReader { float Value(ChannelId id); }               // WP-03 / WP-39：灯光通道的当前值
