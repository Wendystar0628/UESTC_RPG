using System.Numerics;
using System.Text.Json.Serialization;
using Campus.Contracts.Audio;

namespace Campus.Contracts;

// 室内（照 WLD-INTERIORS §15：IInteriorProvider 按层提供；只开放 WLD-INTERIORS §2 的开放清单，
// 以后加一栋楼只在 interiors.json 加一项、再注册一个提供者或给通用提供者加一条楼型配置，不改契约，也不改流式架构，WLD-INTERIORS §14）
public enum InteriorDetail : byte { Proxy = 0, Shell = 1, Full = 2 }   // I0 兜底、IS 只留壳、IF 完整
public enum ZoneKind : byte { Room, Corridor, Stair, Atrium, Lobby, Wc, Service, Gallery }
public enum PortalKind : byte { Door, StairOpening, AtriumOpening, Window, Sealed, ElevatorStop }   // Sealed：封口
public sealed record StoreyInfo(int Index, float FloorY, float Height, IReadOnlyList<Vector2> Footprint);
public sealed record ZoneVolume(int Storey, int Id, ZoneKind Kind, IReadOnlyList<Vector2> Ring,
    ChannelId LightChannel, uint LightHash, bool Open);                     // Open：属于开放范围
public sealed record PortalDecl(int Storey, int ZoneA, int ZoneB, Vector3 A, Vector3 B, float Height, PortalKind Kind);
public sealed record EntranceDecl(string Id, Vector3 Outside, Vector3 Inside, float ClearWidth, bool Open)
{   // 开放楼的入口门固定敞开，作为"能进"的信号，不随观者开合（WLD-INTERIORS §4）；
    // Outside：门外落点（门外 2.5 m），供地图传送（SYS-ATLAS §7.3）；Inside：门内 3 m 的点
    public Vector3 Center { get; init; }       // 门中心（SYS-ATLAS 的室内门口标记、G18 的 ≤ 0.5 m 比对，WLD-INTERIORS §15）
    public float YawDeg { get; init; }         // 门外法线的方位角，0 = 北、90 = 东，与点位偏航同一约定（WLD-INTERIORS §2.17）
    /// <summary>Absolute world Y of the doorway top in metres; zero means unknown, never a relative height.</summary>
    public float HeadY { get; init; }
}
public sealed record ElevatorDecl(string Id, int StoreyA, int StoreyB, Box3 CabA, Box3 CabB);   // 关门换位两站（WLD-INTERIORS §3）

// 会动的门（参数以 WLD-INTERIORS §4.1 为准）：开放室内里能进的房门、开放路线上的楼梯间防火门和走廊玻璃门，
// 观者走近自动推开，走过去后由闭门器缓缓关上，带开门和关门声；不加交互键（UI-NAV 的"没有可以点选的东西"不变）。
// 推开 0.55 s 到 90°（一拍二步进）；门扇朝观者前进的方向开、离观者而去，两个方向都能开，方向在运行时定；
// 观者离开扫掠区 1.5 m 以外再过 0.8 s 回关：前 80° 用 2.5–3.5 s（防火门 3.5 s），最后 10° 快速落锁；回关途中观者再走近就立即重新推开。
// 门扇转动和停在开位时都没有碰撞，关好（开度 < 2°）之后才有碰撞。开放楼的入口外门不在此列（固定敞开），锁着的门（DoorClosed）也不在此列
public enum DoorLeafKind : byte { RoomDoor, FireDoor, GlassSwing }      // 木质房门、楼梯间防火门、玻璃平开门：决定门体的 Foot 和开关门声
public sealed record DoorLeafDecl(string Id, int Storey, int ZoneA, int ZoneB,
    Vector3 Hinge, float ClosedYawDeg, float OpenDeg, float Width, float Height, DoorLeafKind Kind,
    float TriggerM = 1.5f);                  // 静止时的触发半径；运动时放大为 max(TriggerM, v × 0.75 s)，保证观者到门洞前门已开过 75°（WLD-INTERIORS §4.1）；
                                             // OpenDeg 只给开到的角度，不给方向
public sealed record InteriorLayout(FeatureId Feature, IReadOnlyList<StoreyInfo> Storeys,
    IReadOnlyList<ZoneVolume> Zones, IReadOnlyList<PortalDecl> Portals, IReadOnlyList<EntranceDecl> Entrances,
    IReadOnlyList<ElevatorDecl> Elevators, IReadOnlyList<int> OpenStoreys, IReadOnlyList<int> AtriumVisibleStoreys,
    Box3 Bounds, uint PlanHash)
{
    public IReadOnlyList<DoorLeafDecl> Doors { get; init; } = [];          // 会动的门扇
}

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class InteriorProviderAttribute(string id) : Attribute
{
    public string Id { get; } = id;
    public string Owner { get; set; } = "";
    public string Map { get; set; } = "";    // SYS-MAPS §7
}
public interface IInteriorProvider           // gen 里实现，纯函数；注册即开放（G19c 核对开放清单 = 注册的提供者 = 门开着的楼）
{
    IReadOnlyList<FeatureId> FeatureIds { get; }
    int Priority { get; }
    bool Walkable => true;
    InteriorLayout Layout(BuildContext ctx, FeatureId id);
    ContentOutput BuildStorey(BuildContext ctx, FeatureId id, int storey, InteriorDetail detail);
    IReadOnlyList<Collider> CollisionStorey(BuildContext ctx, FeatureId id, int storey);   // 门扇不进烘焙碰撞，见 IInteriorDoors
    NavFloorGraph Nav(BuildContext ctx, FeatureId id, int storey) => NavFloorGraph.Empty(id, storey);   // WLD-INTERIORS §13
    AcousticsDecl Acoustics(BuildContext ctx, FeatureId id) => AcousticsDecl.Empty;   // 房间与门户按层声明（SYS-AUDIO §19）；没写时退到声学网格
}
// ContentOutput 里室内层的实例一律带 IgnoreOcclusionCulling 的标记，室内遮挡体单独成表（WP-33 管理显隐，WLD-INTERIORS §15）

// 室内索引：WP-51 烘焙，WP-08 随空间索引加载
public sealed record InteriorIndexEntry(FeatureId Feature, IReadOnlyList<StoreyInfo> Storeys,
    IReadOnlyList<EntranceDecl> Entrances, bool Open, string? ProviderId);
public interface IInteriorIndex
{
    InteriorIndexEntry? Get(FeatureId id);
    IReadOnlyList<EntranceDecl> EntrancesNear(Vector3 p, float r);       // 入口预取、地图图层（SYS-ATLAS 的"可进入的室内"）、G19
}

// 门的运行时（Godot 侧，WP-07 的 InteriorDirector 实现，FramePriority.Medium 那一步推进，WLD-INTERIORS §4.1）
public enum DoorPhase : byte { Closed, Opening, Open, Closing }
public readonly record struct DoorEvent(FeatureId Building, string DoorId, DoorPhase Phase, Vector3 Position, ulong Tick);
public interface IInteriorDoors             // Kernel 服务；只读
{
    DoorPhase PhaseOf(FeatureId building, string doorId);
    float OpenOf(FeatureId building, string doorId);   // 门扇开度 0–1；声学门户的开度读同一个值（AcousticPortalDecl.Door，SYS-AUDIO §10.4）
    event Action<DoorEvent>? Changed;        // 阶段变化时发：WP-53d 放开门、关门、落锁声（进入 Opening、Closed 时；停在开位和回关途中无声），
                                             // WP-43 在 Closed 时启用、离开 Closed 时停用这扇门的运行时门体（StaticBody3D，只切 CollisionLayer，不增删形状）
}
// 规则：门扇转动只由观者位置触发，与钟点、季节、天气无关，所以"可走性与时刻无关"（WLD-INTERIORS §10、ENG-BUDGET §7）不受影响；
// 运行时门体不进烘焙碰撞，G19e 的碰撞哈希不含它；门在 Closed 时也只是一块薄盒，观者走近就先推开，不会挡路；
// 读档、切图、换实例时所有门直接落到 Closed，不放声音；门的状态不进存档
