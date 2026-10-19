using System.Numerics;
using Campus.Contracts.Save;

namespace Campus.Contracts.Maps;

public interface IMapContext                 // 纯数据；场景 RID 和物理空间留在 Godot 侧的 kernel/map
{
    MapDescriptor Descriptor { get; }
    /// <summary>The frozen map-relative input version used to create this context and its World.</summary>
    BuildInputSnapshot SourceInputs { get; }
    IWorldModel World { get; }               // 切图后失效，任何节点都不得跨图持有
    string DataRoot { get; }                 // "res://data/maps/qingshuihe/"
}
public enum MapTravelCause : byte { Boot, NewGame, LoadSave }
public sealed record MapTravelRequest(MapId Target, string? Spawn = null, Pose? Pose = null,
    ViewMode? Mode = null, MapTravelCause Cause = MapTravelCause.Boot);
public enum MapStage : byte { LoadData, Mount, Arrival, Spawn, Done }
public enum MapTravelResult : byte { Arrived, Rejected, Failed }
public interface IMapTransition
{
    MapId From { get; }
    MapId To { get; }
    MapTravelCause Cause { get; }
    MapStage Stage { get; }
    float Progress { get; }                  // 0–1，加载界面的真实加载进度（UI-VISUAL）
    Task<MapTravelResult> Done { get; }      // 主线程完成；不可取消
}
public interface IMapService                 // Kernel 服务（WP-46）
{
    long Rev { get; }                        // 每次 Spawn 完成 + 1
    MapId Default { get; }                   // index.json 的默认图
    IReadOnlyList<MapDescriptor> Maps { get; }
    IReadOnlyList<MapVisit> Visits { get; }   // All visited maps, including the current one; save enumerates per-map sections.
    MapId Current { get; }
    IMapContext? Context { get; }            // 启动加载完成前为 null
    IMapTransition? Transition { get; }
    event Action<MapId, MapTravelCause>? Arrived;   // Main thread, after successful Spawn; subscribers must not block the transition.
    /// <summary>
    /// Resolves authored spawn data from the initialized catalogue for the requested map, independently
    /// of Current/Context. Null selects that map's DefaultSpawn; named spawns precede its waypoints.
    /// This lookup does not load files, move the camera or legalize the destination against active-map bounds.
    /// </summary>
    /// <exception cref="InvalidOperationException">The catalogue has not been initialized.</exception>
    /// <exception cref="ArgumentException">The map or spawn/waypoint ID is unknown.</exception>
    MapSpawn ResolveSpawn(MapId map, string? spawn = null);
    IMapTransition Travel(MapTravelRequest r);   // 仅用于启动加载与同图的新游戏/读档落点；同图传送交给 ICameraRig.Transit 和 IAtlasService
}
public interface IMapAware                   // [KernelNode] 可选实现；雾幕全黑时按节点 ID 顺序调用
{
    void OnMapUnloading(MapId from);         // 释放本图的实例、纹理内容和缓存；之后不得再读旧的 IWorldModel
    void OnMapLoaded(IMapContext ctx);       // 读新图的数据，重建本图的状态
}
public sealed record MapVisit(MapId Map, Pose LastPose, ViewMode Mode);
