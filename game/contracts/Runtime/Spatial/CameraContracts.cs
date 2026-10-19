using System.Numerics;
using System.Text.Json.Serialization;
using Campus.Contracts.Audio;

namespace Campus.Contracts;

public interface ICameraRig
{
    Pose Pose { get; }
    /// <summary>Presentation-only roll, zero for explicit poses and outside live walking.</summary>
    float RollDeg { get; }
    float Speed { get; }
    ViewMode Mode { get; }                   // Fly 由 WP-06 驱动，Walk 由 WP-42 驱动
    void SetPose(Pose p);                    // 只用于启动/地图出生、过渡的旁观落位、审查/测试，以及 Title、Entering、Leaving 的过场展示；
                                             // Playing+Walk 的镜头直接读行走眼位，SetPose 不移动身体，也不写行走位姿
    ITransitHandle Transit(Pose to, TransitOptions? o = null);   // 点位跳转与同图传送（WP-36，UI-NAV、SYS-ATLAS），可被输入打断
    IDisposable LockInput(InputLockReason reason);   // 输入占用按原因计数：没有任何占用时才接受观者输入；Dispose 解除这一次占用
    ITransitHandle? ActiveTransit { get; }   // 进行中的跳转或传送，没有时为 null（SYS-AUDIO）
    float EdgeFog { get; }                   // 与全局参数 edge_fog 同一个值（WLD-OUTSKIRTS），音频据此压低远处的声音
}
public enum InputLockReason : byte { Atlas, AppFlow, MapTravel, Menu, Rebind } // Append new reasons only.
public enum TransitPreference : byte { Auto, Teleport }   // Auto 按距离选滑移、弧线或雾幕弧线（UI-NAV）；Teleport 是地图传送（SYS-ATLAS）
public sealed record TransitOptions(EntryPath? Entry = null, Vector3? LandOn = null /* 行走落点 */)
{
    public TransitPreference Prefer { get; init; } = TransitPreference.Auto;
}
public enum TransitKind { Slide, Arc, Veil, Teleport }   // Teleport：地图传送的雾幕瞬移，雾幕盖满 → 移位 → 等目的地就绪 → 散开（SYS-ATLAS）
public enum TransitResult { Arrived, Cancelled }
public interface ITransitHandle
{
    TransitKind Kind { get; }
    float Progress { get; }
    void Cancel();
    Task<TransitResult> Done { get; }        // 在主线程完成
}

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
