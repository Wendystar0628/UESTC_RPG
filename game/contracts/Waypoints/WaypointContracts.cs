using System.Numerics;

namespace Campus.Contracts;

// 点位（照 UI-NAV 冻结，数据在 game/data/maps/<地图>/waypoints.json）
public readonly record struct LocalizedName(string Zh, string En);
public enum WaypointGroup { Overview, Core, South, West, North, East, Edge }
public sealed record WaypointStand(Vector3 Feet, float YawDeg);
public sealed record WaypointRecommend(double Clock, Season Season, WeatherKind? Weather = null);
// Weather 为 null 时按晴钉住；应用时转成 WorldPreset 调 IWorldControl.Apply（UI-NAV、SYS-WEATHER）
public sealed record Waypoint(
    [property: DataId] string Id,                               // "P00" 起（UI-NAV）；室内点位 P16–P23（WLD-INTERIORS），以后从 P24 往后编；
                                             // 只在本图内唯一，跨图引用用 WaypointRef
    LocalizedName Name, WaypointGroup Group,
    Pose Fly,                                // 旁观位姿
    WaypointStand? Stand,                    // 行走落点；没有就是"仅旁观"
    EntryPath? Entry,
    WaypointRecommend Recommend);
