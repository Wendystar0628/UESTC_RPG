using System.Numerics;
using System.Text.Json.Serialization;
using Campus.Contracts.Audio;

namespace Campus.Contracts;

// 相机（WP-06）与介质呈现（WP-07），详见 SYS-VIEW
public readonly record struct Pose(Vector3 Position, float YawDeg, float PitchDeg, float? FovDeg = null);

public interface ICameraRig
{
    Pose Pose { get; }
    float Speed { get; }
    ViewMode Mode { get; }                   // Fly 由 WP-06 驱动，Walk 由 WP-42 驱动
    void SetPose(Pose p);                    // 用于出生位置和测试构建，没有过渡动画；另用于 Title、Entering、Leaving 状态下
                                             // CinematicPlayer 的逐帧驱动，这三个状态里 WP-06 的相机不积分输入（SYS-TITLE §7.4）
    ITransitHandle Transit(Pose to, TransitOptions? o = null);   // 点位跳转与同图传送（WP-36，UI-NAV §7、SYS-ATLAS §7），可被输入打断
    IDisposable LockInput(InputLockReason reason);   // 输入占用按原因计数：没有任何占用时才接受观者输入；Dispose 解除这一次占用
    ITransitHandle? ActiveTransit { get; }   // 进行中的跳转或传送，没有时为 null（SYS-AUDIO §19）
    float EdgeFog { get; }                   // 与全局参数 edge_fog 同一个值（WLD-OUTSKIRTS §8.3），音频据此压低远处的声音
}
public enum InputLockReason : byte { Atlas, Cinematic, MapTravel, Menu, Rebind }   // 大地图（SYS-ATLAS §4.1）、开始页与过场的交还控制（SYS-TITLE §7.3）、
                                                                                   // 换图雾幕（SYS-MAPS §4.2）；Menu：设置卡、存档页、数据手册、确认对话框，Rebind：改键捕获（UI-NAV §5）；
                                                                                   // 只在末尾追加（以后加对话、剧情过场）
public enum EntryPath { Skylight, Door, WaterSurface }
public enum TransitPreference : byte { Auto, Teleport }   // Auto 按距离选滑移、弧线或雾幕弧线（UI-NAV §7.1）；Teleport 是地图传送（SYS-ATLAS §11）
public sealed record TransitOptions(EntryPath? Entry = null, Vector3? LandOn = null /* 行走落点 */)
{
    public TransitPreference Prefer { get; init; } = TransitPreference.Auto;
}
public enum TransitKind { Slide, Arc, Veil, Teleport }   // Teleport：地图传送的雾幕瞬移，雾幕盖满 → 移位 → 等目的地就绪 → 散开（SYS-ATLAS §7.2）
public enum TransitResult { Arrived, Cancelled }
public interface ITransitHandle
{
    TransitKind Kind { get; }
    float Progress { get; }
    void Cancel();
    Task<TransitResult> Done { get; }        // 在主线程完成
}

public static class PoseMath
{
    public static Pose PoseFacing(Vector3 target, float azDeg, float elDeg, float dist)
    {
        if (!float.IsFinite(dist) || dist < 0) throw new ArgumentOutOfRangeException(nameof(dist));
        if (!float.IsFinite(azDeg) || !float.IsFinite(elDeg)) throw new ArgumentOutOfRangeException(nameof(azDeg));
        float az = azDeg * (float.Pi / 180f), el = elDeg * (float.Pi / 180f);
        var forward = new Vector3(DetMath.Sin(az) * DetMath.Cos(el), -DetMath.Sin(el), -DetMath.Cos(az) * DetMath.Cos(el));
        return new(target - forward * dist, azDeg, -elDeg);
    }
}

public enum MediumKind { Air, Interior, Underground, Underwater, FogSea }
public readonly record struct CameraMedium(  // 值类型，每帧查询不分配内存
    MediumKind Kind,
    FeatureId? Feature = null,               // Interior：所在楼；Underwater：水体
    int Storey = 0,                          // Interior
    float WallDist = 0,                      // Interior
    float Depth = 0);                        // Underground、Underwater、FogSea

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class MediumPresenterAttribute(string id) : Attribute { public string Id { get; } = id; public string Owner { get; set; } = ""; }
public interface IMediumPresenter            // Godot 侧实现（主线程）
{
    IReadOnlyList<MediumKind> Kinds { get; }
    void Enter(IRuntimeContext rt, in CameraMedium m);
    PostOverride Update(IRuntimeContext rt, in CameraMedium m, float blend);
    void Exit(IRuntimeContext rt);
}
public interface IMediumState { CameraMedium Current { get; } MediumKind From { get; } MediumKind To { get; } float Blend { get; } }
// WP-07 实现，与全局参数 medium 同源的只读值；音频、界面读（SYS-AUDIO §19）
