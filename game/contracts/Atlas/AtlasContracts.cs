using System.Numerics;
using System.Text.Json.Serialization;
using Campus.Contracts.Save;
using Campus.Contracts.Maps;

namespace Campus.Contracts.Atlas;

public readonly record struct MarkerId(string Value);
// 本图内唯一："A03"（锚点）、"P05"（观景点）、"door:<入口 ID>"（可进入的室内）、"route:R1"、
// "lm:<FeatureId>"（地标）、"fac:<FeatureId>"（设施）、"pin:<十进制整数>"（自定义）
public enum MarkerKind : byte { Anchor, Viewpoint, Interior, Route, Landmark, Facility, Custom, Quest /* RPG 预留 */ }
public enum AnchorForm : byte { Kiosk, Post }                     // 导览亭、导览桩

public sealed record TeleportSpec(Pose Fly, WaypointStand? Stand, EntryPath? Entry = null);   // Stand 为 null：仅旁观

public sealed record AtlasMarker(
    MarkerId Id, MarkerKind Kind, LocalizedName Name,
    Vector2 At,                         // 地图坐标 (x, z)，单位 m
    string Icon,                        // UI-VISUAL图标体系里的名字
    int Priority, byte MinZoomBand,     // 缩放档 0–4（SYS-ATLAS）
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
