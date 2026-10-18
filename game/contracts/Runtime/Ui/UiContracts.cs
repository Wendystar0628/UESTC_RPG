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
public enum VeilContent : byte { Fog, Frontispiece }    // 雾色；扉页插画（SYS-TITLE）
// Fixed policies from UI-VISUAL and SYS-TITLE. Callers do not choose delays.
public enum VeilWaitKind : byte { Readiness, BootFrontispiece }
public interface IVeilState
{
    float Opacity { get; }
    VeilReason Reason { get; }
    // True means a live readiness observation has reached its policy delay: show the waiting indicator.
    bool Waiting { get; }
}   // 只读；音频据此压低 World 组（SYS-AUDIO）
public interface IVeilLease : IDisposable
{
    Task Covered { get; }                  // 本租约要求的遮挡已经盖满
    bool IsCurrent { get; }                // 雾幕服务选中的持有者；不用 Reason 推断所有权
    // Releases only this lease. Other covers remain; the final release fades out over seconds.
    Task Reveal(float seconds);
}
public interface IVeilService : IVeilState
{
    // Ownership is returned synchronously, before awaiting Covered. Dispose releases immediately and is idempotent.
    IVeilLease Cover(VeilReason reason, float seconds, VeilContent content = VeilContent.Fog);
    // Observe actual readiness work only. Main-thread begin; Dispose may run on a worker and is idempotent.
    // Does not cover/reveal, change Reason, evaluate Ready, cancel work, or impose any timeout.
    // Each scope owns only itself; simultaneous live scopes aggregate with OR.
    IDisposable ObserveReadinessWait(VeilWaitKind kind = VeilWaitKind.Readiness);
}

// 其他包补的只读状态（SYS-AUDIO）
public readonly record struct LightningFlash(double RealSeconds, float AzDeg, float ElDeg, float DistanceKm,
    int Pulses, float Peak, bool Visible);              // Visible = false：减少动态效果时只发事件、不画
public interface ILightningFeed { event Action<LightningFlash>? Flash; }     // WP-40：远雷的节拍（SYS-AUDIO）
public interface IChannelReader { float Value(ChannelId id); }               // WP-03 / WP-39：灯光通道的当前值
