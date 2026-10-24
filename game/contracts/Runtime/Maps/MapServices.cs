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
/// <summary>Loading-only publication capability. Runtime map consumers read IMapContext only.</summary>
public interface IMapInteriorPreparation
{
    /// <summary>Publish exactly once, after cold generation or resident restoration and before map callbacks.</summary>
    void PublishInteriors(IReadOnlyList<InteriorIndexEntry> entries);
}
public enum MapStage : byte { LoadData, Mount, Done }
public enum MapTravelResult : byte { Arrived, Failed }
public interface IMapTransition
{
    MapId From { get; }
    MapId To { get; }
    MapStage Stage { get; }
    float Progress { get; }                  // 0–1，加载界面的真实加载进度（UI-VISUAL）
    /// <summary>The original terminal failure, published before Done; null for success, rejection or cancellation.</summary>
    Exception? Failure { get; }
    Task<MapTravelResult> Done { get; }      // 主线程完成；不可取消
}
public interface IMapService                 // Kernel 服务（SYS-MAPS）
{
    /// <summary>Command-owner value boundaries; original failures stay on their existing channel.</summary>
    event Action<MapPrepareEvent>? PrepareCommand;
    long Rev { get; }                        // 地图基础挂载完成 + 1
    MapId Default { get; }                   // index.json 的默认图
    IReadOnlyList<MapDescriptor> Maps { get; }
    IReadOnlyList<MapVisit> Visits { get; }   // All visited maps, including the current one; save enumerates per-map sections.
    MapId Current { get; }
    IMapContext? Context { get; }            // 启动加载完成前为 null
    IMapTransition? Transition { get; }
    /// <summary>
    /// Resolves authored spawn data from the initialized catalogue for the requested map, independently
    /// of Current/Context. Null selects that map's DefaultSpawn; named spawns precede its waypoints.
    /// This lookup does not load files, move the camera or legalize the destination against active-map bounds.
    /// </summary>
    /// <exception cref="InvalidOperationException">The catalogue has not been initialized.</exception>
    /// <exception cref="ArgumentException">The map or spawn/waypoint ID is unknown.</exception>
    MapSpawn ResolveSpawn(MapId map, string? spawn = null);
    /// <summary>Loads and mounts the map foundation without selecting a camera destination. Same-map calls reuse it.</summary>
    Task Prepare(MapId map);
    /// <summary>Rejects new preparation and cancels its publication, retaining every started task for DrainAsync.</summary>
    void StopNewWork();
    /// <summary>Observes all started data preparation before its inputs/services can be released; retains real failures.</summary>
    Task DrainAsync();
}
public interface IMapAware                   // [KernelNode] 可选实现；雾幕全黑时按节点 ID 顺序调用
{
    void OnMapUnloading(MapId from);         // 释放本图的实例、纹理内容和缓存；之后不得再读旧的 IWorldModel
    void OnMapLoaded(IMapContext ctx);       // 读新图的数据，重建本图的状态
}
public sealed record MapVisit(MapId Map, Pose LastPose, ViewMode Mode);

public enum MapPreparePhase : byte { Started, Joined, Reused, Rejected, Completed }
public readonly record struct MapPrepareEvent(
    long Operation, MapPreparePhase Phase, MapId From, MapId To,
    MapTravelResult? Result);
