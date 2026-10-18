using System.Numerics;
using Campus.Contracts.Save;

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
    float FallbackMargin);                                       // 没有外围数据的图：校界外扩 150 m
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
