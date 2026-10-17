using System.Numerics;
using System.Text.Json.Serialization;
using Campus.Contracts.Save;
using Campus.Contracts.Maps;

// 平面地图（照 SYS-ATLAS §11，WP-48c；Campus.Gen.* 不得引用 IAtlasService）
// 自定义标记的 ID 是按存档递增的整数（Guid.NewGuid 被 G4 禁用）
namespace Campus.Contracts.Atlas;

public readonly record struct MarkerId(string Value);
// 本图内唯一："A03"（锚点）、"P05"（观景点）、"door:<入口 ID>"（可进入的室内）、"route:R1"、
// "lm:<FeatureId>"（地标）、"fac:<FeatureId>"（设施）、"pin:<十进制整数>"（自定义）
public enum MarkerKind : byte { Anchor, Viewpoint, Interior, Route, Landmark, Facility, Custom, Quest /* RPG 预留 */ }
public enum AnchorForm : byte { Kiosk, Post }                     // 导览亭、导览桩
public enum UnlockState : byte { Hidden, Known, Unlocked }
public enum TravelStyle : byte { Teleport, Fly }                  // 默认 Teleport（雾幕瞬移），Fly 是"飞过去"
public enum TravelRefusal : byte { None, NotTravelable, Locked, Here, Busy, Unavailable }
// Unavailable：过场、开始页、换图雾幕期间
public enum PinShape : byte { Diamond, Circle, Square, Triangle, Star, Flag }

public sealed record TeleportSpec(Pose Fly, WaypointStand? Stand, EntryPath? Entry = null);   // Stand 为 null：仅旁观

public sealed record AtlasMarker(
    MarkerId Id, MarkerKind Kind, LocalizedName Name,
    Vector2 At,                         // 地图坐标 (x, z)，单位 m
    string Icon,                        // UI-VISUAL图标体系里的名字
    int Priority, byte MinZoomBand,     // 缩放档 0–4（SYS-ATLAS §4.2）
    string? Region,                     // 区域 ID（labels.json）
    TeleportSpec? Teleport = null,      // 只有 Anchor、Viewpoint、Interior、Route 可以非空
    FeatureId? Feature = null,          // 锚点：design:anchor:Axx；地标、设施：实体；室内：楼
    string? WaypointId = null,          // Viewpoint：引用 waypoints.json，不复制位姿
    string? RouteId = null,
    AnchorForm? Form = null)
{
    // 由 Atlas 加载时从关联点位发布；无点位时分组和推荐均为空。
    public WaypointGroup? Group { get; init; }
    [JsonIgnore]
    public WaypointRecommend? Recommend { get; init; }
}

public sealed record AtlasRegion(string Id, LocalizedName Name, string? Parent);   // 大区的 Parent 为 null
public sealed record AtlasLayer(string Id, LocalizedName Name, float YMin, float YMax);   // 预留：楼层图

public sealed record CustomPin(int Id, Vector2 At, PinShape Shape, string? Note = null);   // Id：本图内递增，不复用；Note 观光期不开放

public interface IUnlockPolicy
{
    string Id { get; }                                            // "all-unlocked"、"proximity"
    UnlockState Initial(AtlasMarker m);
    UnlockState OnVisit(AtlasMarker m, UnlockState now);          // 进入判定圈时调用
}

public interface IAtlasService                                    // Kernel 服务（WP-48c）；实现 IMapAware，切图时换底图和标记
{
    long Rev { get; }
    MapId Map { get; }
    MapRect PageRect { get; } // Atlas page extent from authored data; unavailable until the map is loaded.
    IReadOnlyList<AtlasMarker> Markers { get; }                   // 含运行时由室内索引生成的门口标记与自定义标记
    IReadOnlyList<AtlasRegion> Regions { get; }
    event Action? Changed;                                        // 主线程；换图、标记增删、解锁或追踪变化时发
    AtlasMarker? Find(MarkerId id);
    UnlockState StateOf(MarkerId id);
    string? RegionAt(float x, float z);                           // 8 m 区域栅格；返回地点 ID
    bool CanTravel(MarkerId id, TravelStyle style, out TravelRefusal why);
    void Prefetch(MarkerId id);                                   // 同一时间只保留一个目的地（UI-NAV §7.5）
    ITransitHandle? Travel(MarkerId id, TravelStyle style);       // 同图只调 ICameraRig.Transit；目标在别的图时才调 IMapService.Travel；拒绝时返回 null
    MarkerId? Tracked { get; }
    void Track(MarkerId? id);                                     // null 取消；同一时间只有一个
    IReadOnlyList<CustomPin> Pins { get; }                        // 当前图
    CustomPin? AddPin(Vector2 at, PinShape shape);                // 满 50 返回 null
    void MovePin(int id, Vector2 at);
    void SetPinShape(int id, PinShape shape);
    void RemovePin(int id);
    string PolicyId { get; }                                      // 取自规则集的 UnlockPolicy（观光为 "all-unlocked"）；"proximity" 留给 RPG；
                                                                  // 要按图覆盖时再给 map.json 加字段
}

// 存档段（SYS-SAVE的 ISaveSection；段 ID 按图展开为 "map:<id>/atlas"）
public sealed record AtlasSave(
    string Policy,                                                // "all-unlocked"
    IReadOnlyList<CustomPin> Pins,
    int NextPinId,                                                // 下一个自定义标记的 ID，只增不减
    string? Tracked,                                              // MarkerId.Value
    IReadOnlyDictionary<string, UnlockState> Unlock,             // 只存与策略初值不同的项；观光期为空
    IReadOnlyList<string> VisitedAnchors);
// [SaveSection("atlas", Owner = "WP-48c", Version = 1, Scope = SaveScope.PerMap, Phase = RestorePhase.Content, Order = 40)]（SYS-SAVE §2）
// 相机契约里与传送有关的部分（TransitPreference、TransitOptions.Prefer、TransitKind.Teleport）在 §3 相机块
