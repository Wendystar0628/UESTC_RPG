using System.Numerics;
using Campus.Contracts.Save;

// 多地图（照 SYS-MAPS，WP-46）；命名空间与平面地图的契约分开。MapId、WaypointRef 在基础类型里
namespace Campus.Contracts.Maps;

public enum MapKind : byte { Outdoor, Interior, Instance }       // 现在只有 Outdoor；精选室内属于所在的图（SYS-MAPS）
public enum MapTimeRule : byte { Shared, Fixed }                 // Fixed 预留，WP-34 现在只实现 Shared（SYS-MAPS）
public sealed record GeoFrame(double OriginLon, double OriginLat, int Epsg,
    Vector2 BoundaryMin, Vector2 BoundaryMax);                   // 从 campus.json 的 meta 读，不手写
public sealed record MapRect(Vector2 Min, Vector2 Max);          // 全图数据纹理的 xz 范围，边长为 64 m 的整数倍；清水河 ±1280 m（SYS-MAPS）
public sealed record RoamLimits(                                 // 旁观活动范围（SYS-VIEW）；公式与前沿线见 WLD-OUTSKIRTS
    float HeightFactor, float HeightStart, float MaxRadius,      // 0.9、30 m、3000 m：水平范围 = 前沿线 + 0.9 × max(0, h − 30 m)
    float FloorY, float CeilY,                                   // −28 m、2000 m
    float Buffer, float SoftFloorClearance,                      // 30 m 缓冲带；外围上空的软地板 = 屋顶 + 40 m
    float FallbackMargin)                                        // 没有外围数据的图：校界外扩 150 m
{
    public float PushRate { get; init; } = 1.5f;                 // WLD-OUTSKIRTS：缓冲带里沿法线推回的速率
    public float PushMax { get; init; } = 15f;                   //   推回上限
    public float EdgeFogMax { get; init; } = 1.6f;               //   边缘雾上限
}
public sealed record MapSpawn(string Id, Pose Fly, WaypointStand? Stand);   // 点位以外的出生点
public sealed record MapWorldRules(
    MapTimeRule Time,
    WorldPreset Default,                                         // 清水河：秋 17:30 晴
    LocalizedName DefaultName,                                   // "落金初灯"
    bool QuietUntilDusk,                                         // 默认状态保持晴到本季黄昏带结束（SYS-WEATHER）
    string WeatherProfile,                                       // game/data/weather/<剧本>/；清水河 "chengdu"
    string SunProfile = "default",                               // 美术日月轨迹（SYS-TIME）
    WorldPreset? Fixed = null)                                   // Time == Fixed 时进图钉住的状态（预留）
{
    public WorldPreset? NewGameStart { get; init; }              // SYS-TITLE：新游戏过场的起点，清水河秋 17:05 晴；null 时取 Default
}
public sealed record MapEnvironment(string Sky, string Backdrop, IReadOnlyList<FeatureId> ReflectLakes)   // 列表顺序即湖号
{
    public string? Outskirts { get; init; }                      // WLD-OUTSKIRTS："outskirts.json"；null 表示没有外围
}
public sealed record MapTitle(Pose T0, string CloudLayout);      // SYS-TITLE：开始页的 T0 镜头与云卡布局名
public sealed record MapPortal(string Id, Box3 Trigger, MapId Target, string TargetSpawn);              // 预留
public sealed record MapDescriptor(
    MapId Id, LocalizedName Name, MapKind Kind, int Version, uint Seed,
    GeoFrame Geo, Vector2 Center, Box3 Bounds, MapRect DataRect, RoamLimits Roam,
    // Center：校界形心，清水河约 (50, 77)，活动范围"离中心不超过 3 km"按它算；Bounds：箱庭包围盒；DataRect：全图数据纹理 = map_rect，清水河 ±1280 m（SYS-MAPS）
    TileKey TileMin, TileKey TileMax,
    string DefaultSpawn, IReadOnlyList<MapSpawn> Spawns,
    MapWorldRules World, MapEnvironment Env,
    IReadOnlyDictionary<string, string> Cinematics,              // 角色 → 过场 ID：enterNew、enterNewShort、enterContinue、leave（SYS-TITLE）
    IReadOnlyList<MapPortal> Portals)
{
    public MapTitle? Title { get; init; }                        // 没写时开始页用默认图的 T0
    public TerrainDefaults? TerrainDefaults { get; init; }
}

public interface IMapContext                 // 纯数据；场景 RID 和物理空间留在 Godot 侧的 kernel/map
{
    MapDescriptor Descriptor { get; }
    /// <summary>The frozen map-relative input version used to create this context and its World.</summary>
    BuildInputSnapshot SourceInputs { get; }
    IWorldModel World { get; }               // 切图后失效，任何节点都不得跨图持有
    string DataRoot { get; }                 // "res://data/maps/qingshuihe/"
}
public enum MapTravelCause : byte { Boot, NewGame, LoadSave, Ui, Portal, Teleport, Test }
public sealed record MapTravelRequest(MapId Target, string? Spawn = null, Pose? Pose = null,
    ViewMode? Mode = null, MapTravelCause Cause = MapTravelCause.Ui);
public enum MapStage : byte { VeilIn, Snapshot, Unload, LoadData, Mount, Warm, Spawn, VeilOut, Done }
public enum MapTravelResult : byte { Arrived, Rejected, Failed }
public interface IMapTransition
{
    MapId From { get; }
    MapId To { get; }
    MapTravelCause Cause { get; }
    MapStage Stage { get; }
    float Progress { get; }                  // 0–1，只给界面的等待灯用，不画进度条（UI-VISUAL）
    Task<MapTravelResult> Done { get; }      // 主线程完成；不可取消
}
public interface IMapService                 // Kernel 服务（WP-46）
{
    long Rev { get; }                        // 每次 Spawn 完成 + 1
    MapId Default { get; }                   // index.json 的默认图
    IReadOnlyList<MapDescriptor> Maps { get; }
    IReadOnlyList<MapVisit> Visits { get; }   // All visited maps, including the current one; save enumerates per-map sections.
    MapId Current { get; }
    IMapContext? Context { get; }            // Unload 到 Mount 之间为 null
    IMapTransition? Transition { get; }
    event Action<MapId, MapTravelCause>? Arrived;   // Main thread, after successful Spawn; subscribers must not block the transition.
    /// <summary>
    /// Resolves authored spawn data from the initialized catalogue for the requested map, independently
    /// of Current/Context. Null selects that map's DefaultSpawn; named spawns precede its waypoints.
    /// This lookup does not load files, move the camera or legalize the destination against active-map bounds.
    /// </summary>
    /// <exception cref="InvalidOperationException">The catalogue has not been initialized.</exception>
    /// <exception cref="ArgumentException">The map or spawn/waypoint ID is unknown.</exception>
    /// <exception cref="NotSupportedException">The implementation does not support catalogue spawn lookup.</exception>
    MapSpawn ResolveSpawn(MapId map, string? spawn = null) =>
        throw new NotSupportedException("This map service does not support catalogue spawn lookup.");
    IMapTransition Travel(MapTravelRequest r);   // 跨图切换走雾幕加载；同图内移动交给 ICameraRig.Transit 和 IAtlasService
}
public interface IMapAware                   // [KernelNode] 可选实现；雾幕全黑时按节点 ID 顺序调用
{
    void OnMapUnloading(MapId from);         // 释放本图的实例、纹理内容和缓存；之后不得再读旧的 IWorldModel
    void OnMapLoaded(IMapContext ctx);       // 读新图的数据，重建本图的状态
}
public sealed record MapVisit(MapId Map, Pose LastPose, ViewMode Mode);
// 存档只需记：MapId CurrentMap、各图的 MapVisit；段的格式由 SYS-SAVE定（按图的段名是 "map:<id>/…"）
// 其他契约里与多地图有关的部分（在 §3 世界状态块和全局着色器参数）：IWeatherProcess.Enter/Leave；FramePriority.Map；ResetDefault 即"回到本图默认状态"；ShaderGlobals.MapRect
