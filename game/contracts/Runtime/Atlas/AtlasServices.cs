using System.Numerics;
using System.Text.Json.Serialization;
using Campus.Contracts.Save;
using Campus.Contracts.Maps;

namespace Campus.Contracts.Atlas;
public enum UnlockState : byte { Hidden, Known, Unlocked }
public enum TravelStyle : byte { Teleport, Fly }                  // 默认 Teleport（雾幕瞬移），Fly 是"飞过去"
public enum TravelRefusal : byte { None, NotTravelable, Locked, Here, Busy, Unavailable }
// Unavailable：过场、开始页、换图雾幕期间
public enum PinShape : byte { Diamond, Circle, Square, Triangle, Star, Flag }

public sealed record CustomPin(int Id, Vector2 At, PinShape Shape, string? Note = null);   // Id：本图内递增，不复用；Note 观光期不开放

public interface IUnlockPolicy
{
    string Id { get; }                                            // "all-unlocked"、"proximity"
    UnlockState Initial(AtlasMarker m);
    UnlockState OnVisit(AtlasMarker m, UnlockState now);          // 进入判定圈时调用
}

public interface IAtlasService                                    // Kernel 服务（SYS-ATLAS-TRAVEL）；实现 IMapAware，切图时换底图和标记
{
    long Rev { get; }
    MapId Map { get; }
    MapRect PageRect { get; } // Atlas page extent from authored data; unavailable until the map is loaded.
    float ZoomMin { get; } // Authored pixels per metre in markers.json; requires a loaded map.
    float ZoomMax { get; }
    IReadOnlyList<AtlasMarker> Markers { get; }                   // 含运行时由室内索引生成的门口标记与自定义标记
    IReadOnlyList<AtlasRegion> Regions { get; }
    event Action? Changed;                                        // 主线程；换图、标记增删、解锁或追踪变化时发
    AtlasMarker? Find(MarkerId id);
    UnlockState StateOf(MarkerId id);
    string? RegionAt(float x, float z);                           // 8 m 区域栅格；返回地点 ID
    bool CanTravel(MarkerId id, TravelStyle style, out TravelRefusal why);
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
