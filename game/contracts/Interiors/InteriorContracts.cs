using System.Numerics;
using System.Text.Json.Serialization;
using Campus.Contracts.Audio;

namespace Campus.Contracts;

// 室内（照 WLD-INTERIORS：IInteriorProvider 按层提供；只开放 WLD-INTERIORS 的开放清单，
// 以后加一栋楼只在 interiors.json 加一项、再注册一个提供者或给通用提供者加一条楼型配置，不改契约，也不改流式架构，WLD-INTERIORS）
public enum InteriorDetail : byte { Proxy = 0, Shell = 1, Full = 2 }   // I0 兜底、IS 只留壳、IF 完整
public enum ZoneKind : byte { Room, Corridor, Stair, Atrium, Lobby, Wc, Service, Gallery }
public enum PortalKind : byte { Door, StairOpening, AtriumOpening, Window, Sealed, ElevatorStop }   // Sealed：封口
public sealed record StoreyInfo(int Index, float FloorY, float Height, IReadOnlyList<Vector2> Footprint);
public sealed record ZoneVolume(int Storey, int Id, ZoneKind Kind, IReadOnlyList<Vector2> Ring,
    ChannelId LightChannel, uint LightHash, bool Open);                     // Open：属于开放范围
public sealed record PortalDecl(int Storey, int ZoneA, int ZoneB, Vector3 A, Vector3 B, float Height, PortalKind Kind);
public sealed record EntranceDecl(string Id, Vector3 Outside, Vector3 Inside, float ClearWidth, bool Open)
{   // 开放楼的入口门固定敞开，作为"能进"的信号，不随观者开合（WLD-INTERIORS）；
    // Outside：门外落点（门外 2.5 m），供地图传送（SYS-ATLAS）；Inside：门内 3 m 的点
    public Vector3 Center { get; init; }       // 门中心（SYS-ATLAS 的室内门口标记、G18 的 ≤ 0.5 m 比对，WLD-INTERIORS）
    public float YawDeg { get; init; }         // 门外法线的方位角，0 = 北、90 = 东，与点位偏航同一约定（WLD-INTERIORS）
    /// <summary>Absolute world Y of the doorway top in metres; zero means unknown, never a relative height.</summary>
    public float HeadY { get; init; }
}
public sealed record ElevatorDecl(string Id, int StoreyA, int StoreyB, Box3 CabA, Box3 CabB);   // 关门换位两站（WLD-INTERIORS）

// 会动的门（参数以 WLD-INTERIORS 为准）：开放室内里能进的房门、开放路线上的楼梯间防火门和走廊玻璃门，
// 观者走近自动推开，走过去后由闭门器缓缓关上，带开门和关门声；不加交互键（UI-NAV 的"没有可以点选的东西"不变）。
// 推开 0.55 s 到 90°（一拍二步进）；门扇朝观者前进的方向开、离观者而去，两个方向都能开，方向在运行时定；
// 观者离开扫掠区 1.5 m 以外再过 0.8 s 回关：前 80° 用 2.5–3.5 s（防火门 3.5 s），最后 10° 快速落锁；回关途中观者再走近就立即重新推开。
// 门扇转动和停在开位时都没有碰撞，关好（开度 < 2°）之后才有碰撞。开放楼的入口外门不在此列（固定敞开），锁着的门（DoorClosed）也不在此列
public enum DoorLeafKind : byte { RoomDoor, FireDoor, GlassSwing }      // 木质房门、楼梯间防火门、玻璃平开门：决定门体的 Foot 和开关门声
public sealed record DoorLeafDecl(string Id, int Storey, int ZoneA, int ZoneB,
    Vector3 Hinge, float ClosedYawDeg, float OpenDeg, float Width, float Height, DoorLeafKind Kind,
    float TriggerM = 1.5f);                  // 静止时的触发半径；运动时放大为 max(TriggerM, v × 0.75 s)，保证观者到门洞前门已开过 75°（WLD-INTERIORS）；
                                             // OpenDeg 只给开到的角度，不给方向
public sealed record InteriorLayout(FeatureId Feature, IReadOnlyList<StoreyInfo> Storeys,
    IReadOnlyList<ZoneVolume> Zones, IReadOnlyList<PortalDecl> Portals, IReadOnlyList<EntranceDecl> Entrances,
    IReadOnlyList<ElevatorDecl> Elevators, IReadOnlyList<int> OpenStoreys, IReadOnlyList<int> AtriumVisibleStoreys,
    Box3 Bounds, uint PlanHash)
{
    public IReadOnlyList<DoorLeafDecl> Doors { get; init; } = [];          // 会动的门扇
}

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class InteriorProviderAttribute(string id) : RegistrationAttribute(RegistrationKind.Interior, id, RegistrationLifetime.Map)
{ }
/// <summary>Configure a fresh map-owned provider from the authoritative catalog before publishing it to workers.</summary>
public interface IInteriorCatalogConfigurable
{
    void Configure(InteriorCatalogConfiguration configuration);
}
public interface IInteriorProvider           // gen 里实现；加载期配置后 Layout/Build/Collision/Nav 为纯函数；目录声明才开放（G19c 核对开放清单 = 注册的提供者 = 门开着的楼）
{
    BakeInputDependencies BakeInputs { get; }
    BakeInputDependencies CollisionInputs { get; }
    IReadOnlyList<FeatureId> FeatureIds { get; }
    int Priority { get; }
    bool Walkable => true;
    InteriorLayout Layout(BuildContext ctx, FeatureId id);
    ContentOutput BuildStorey(BuildContext ctx, FeatureId id, int storey, InteriorDetail detail);
    IReadOnlyList<Collider> CollisionStorey(BuildContext ctx, FeatureId id, int storey);   // 门扇不进烘焙碰撞，见 IInteriorDoors
    NavFloorGraph Nav(BuildContext ctx, FeatureId id, int storey) => NavFloorGraph.Empty(id, storey);   // WLD-INTERIORS
    AcousticsDecl Acoustics(BuildContext ctx, FeatureId id) => AcousticsDecl.Empty;   // 房间与门户按层声明（SYS-AUDIO）；没写时退到声学网格
}
// ContentOutput 里室内层的实例一律带 IgnoreOcclusionCulling 的标记，室内遮挡体单独成表（WP-33 管理显隐，WLD-INTERIORS）

// 室内索引：Storeys 保留整栋楼的空间层；OpenStoreys 只列正式提供者按目录开放的层，不可由 Storeys 推断。
public sealed record InteriorIndexEntry(FeatureId Feature, IReadOnlyList<StoreyInfo> Storeys,
    IReadOnlyList<EntranceDecl> Entrances, bool Open, string? ProviderId,
    [property: JsonRequired] IReadOnlyList<int> OpenStoreys);
public interface IInteriorIndex
{
    InteriorIndexEntry? Get(FeatureId id);
    IReadOnlyList<EntranceDecl> EntrancesNear(Vector3 p, float r);       // 入口预取、地图图层（SYS-ATLAS 的"可进入的室内"）、G19
}
