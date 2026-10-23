using System.Text.Json.Serialization.Metadata;
using Campus.Contracts.Maps;

namespace Campus.Contracts.Save;
// MapId、MapVisit 由多地图契约定义，存档只引用。现在只有 new MapId("qingshuihe")

public enum SlotKind : byte { Auto }
public enum SaveScope : byte { Global, PerMap }        // PerMap 的段在文件里的键是 "map:<MapId>/<段 ID>"
public enum SaveReason : byte
{
    Quit,          // 关窗口、Alt+F4、菜单"退出"：AutoAcceptQuit = false，后台写完再 Quit()
    ToTitle,       // 返回开始页（SYS-TITLE）
    Interval,      // 漫游态累计 5 分钟
    NewGame,       // 新游戏交还控制：写这条世界线的第一代
    Test,          // 门禁（--save-at）
    Manual,        // 游戏内存档页立即保存
}
public enum SlotHealth : byte { Empty, Ok, Corrupt, Newer }

public sealed record SaveMeta(
    [property: DataVersion] int Format,                     // 容器格式号（SYS-SAVE-FORMAT）
    SlotId Slot, SlotKind Kind,
    ulong Gen,                      // 代号：存档位内严格递增；与头和文件名 g<gen>.sav 一致
    ulong Line,                     // 世界线号 = 这条世界线第一代的代号；回退只在同一条线内（SYS-SAVE）
    double PlaySeconds,             // 游玩时长：只在漫游态累加
    MapId Map, double WorldHours, double Clock, Season Season, WeatherKind Weather, ViewMode Mode,
    Pose Pose,                      // 镜头位姿，与 view 段相同；继续读档按它定终点（SYS-SAVE，SYS-TITLE）
    NavSpace? Space,                // 所在空间（楼与楼层，WLD-INTERIORS）；室外为 null
    [property: DataId] string? Region,                 // 区域 ID（SYS-ATLAS区域栅格）
    string? PlaceKey,               // 地点名的翻译键，存档卡摘要用；查不到时退回图名（SYS-SAVE）
    long SavedAtUnix,               // 系统时钟，只用于显示（SYS-SAVE，G4 例外）
    SaveReason Reason,
    [property: DataId] string Ruleset = "sightseeing", // RulesetId.Value（规则集块）；预留 "rpg"
    bool HasThumb = false);
public sealed record SlotInfo(SlotId Slot, SlotKind Kind, SlotHealth Health, SaveMeta? Meta, int Generations);

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class SaveSectionAttribute(string id) : RegistrationAttribute(RegistrationKind.Save, id, RegistrationLifetime.Global)
{   // 标在实现 ISaveSection<T> 的 [KernelService] 或 [KernelNode] 类上；注册器从 Kernel 取这个实例
    public override string IdentityScope => Scope.ToString();
    [DataVersion]
    public int Version { get; set; } = 1;         // 本段当前的 v；字段有任何增减都要加 1（SYS-SAVE）
    public SaveScope Scope { get; set; } = SaveScope.Global;
    public RestorePhase Phase { get; set; } = RestorePhase.Content;   // 恢复阶段（SYS-SAVE）
    public int Order { get; set; }                // 阶段内的恢复顺序，小的先（SYS-SAVE）；G5 查不重复
}
public enum RestorePhase : byte
{
    World,     // 换图之前：maps、world、weather；这一阶段结束时 Instance + 1
    Content,   // 换图之后：map:<id>/atlas 等
    Pose,      // 落位：view、walk；完整恢复终点后等待真实到达。
}

public readonly record struct CaptureContext(SaveReason Reason, MapId Map, bool Forced)
{
    /// <summary>The shared settled endpoint captured once for every section in this save.</summary>
    public required WorldSaveMoment Moment { get; init; }
}
// Forced：此刻不是安全时刻（退出，或顺延满 60 s），各段必须给出落定值（SYS-SAVE）
public sealed class RestoreContext
{
    public required MapId Map { get; init; }
    [DataVersion]
    public required int FromVersion { get; init; }   // 存档中本段的版本，只用于日志
    public required bool Missing { get; init; }      // 文件里没有这一段，传进来的是 CreateDefault 的值
    private readonly List<string> _repairs = [];
    public IReadOnlyList<string> Repairs => _repairs.AsReadOnly();
    public void Repair(string what)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(what);
        _repairs.Add(what);
    }
    // 修复要用的只读查询（安全点、活动范围、点位与标记是否存在）由 game/kernel/save/ 在 v1 里补全
}
/// <summary>Explicit fresh-world command-line initialization, committed by the normal new-game transaction.</summary>
public sealed record NewGameBoot(double AnimationTime, int FlowRate, WeatherBoot Weather, bool UseMapWeather)
{
    public MapId? Map { get; init; }
    public MapStreamDestination? Destination { get; init; }
}

public sealed record NewGameContext(uint Seed, MapId Map, WorldPreset Start, ViewMode Mode)
{
    public NewGameBoot? Boot { get; init; }
}
// Start defaults to the authored map world state; application entry keeps that clock.

public interface ISaveSection                         // 注册器只认这个非泛型接口；各包实现下面的泛型版
{
    JsonTypeInfo TypeInfo { get; }
    bool IsSettled => true;                           // 本段此刻是否落定；有一段为 false，就顺延（SYS-SAVE）
    object Capture(in CaptureContext c);
    void Restore(object dto, RestoreContext c);
    object CreateDefault(NewGameContext c);
}
public interface ISaveSection<T> : ISaveSection where T : class   // T：sealed record，只读，不含 Godot 类型（SYS-SAVE）
{
    new JsonTypeInfo<T> TypeInfo { get; }             // 来自本包的源生成 JsonSerializerContext
    new T Capture(in CaptureContext c);               // 主线程，FramePriority.Save 那一步；落定快照；全部段合计 p99 ≤ 0.3 ms
    void Restore(T dto, RestoreContext c);            // 主线程，在遮挡下按 Phase、Order 调用；立即生效，不做过渡
    new T CreateDefault(NewGameContext c);            // 新游戏，或文件里缺这一段
    JsonTypeInfo ISaveSection.TypeInfo => TypeInfo;
    object ISaveSection.Capture(in CaptureContext c) => Capture(in c);
    void ISaveSection.Restore(object dto, RestoreContext c) => Restore((T)dto, c);
    object ISaveSection.CreateDefault(NewGameContext c) => CreateDefault(c);
}

public enum SaveStatus : byte { Saved, Deferred, Disabled, Failed }
public sealed record SaveResult(SaveStatus Status, SlotId? Slot, ulong Gen, string? Error = null);
public enum LoadStatus : byte { Loaded, Empty, Corrupt, Failed }
public sealed record LoadResult(LoadStatus Status, SlotId? Slot, ulong Gen, IReadOnlyList<string> Repairs, string? Error = null);
public enum ReadOnlyReason : byte { None, SaveOff, OtherWindow }   // 不保存的原因：--save=off、另一个窗口持有锁（SYS-SAVE、SYS-SAVE）
public sealed record SaveState(                                     // 保存状态（SYS-SAVE）：大页被动显示
    SlotId? Slot,                   // 当前存档位
    SaveStatus? Last,               // 本次运行最近一次写盘的结果；还没写过为 null
    string? ErrorKey,               // Last 为 Failed 时的原因，文案键，如 "save.error.disk_full"
    long? LastSavedAtUnix,          // 当前存档位最后一次成功保存的时间（最新可读一代 meta 的 savedAtUnix），只用于显示
    ReadOnlyReason ReadOnly);

public interface ISaveService                         // Kernel 服务（game/kernel/save/）；不加 Autoload
{
    bool Enabled { get; }                             // --save=off，或 .lock 被别的进程占着时为 false：不写、不挪任何文件
    IReadOnlyList<SlotInfo> Slots { get; }            // 扉页后读好；s1–s3 恒为三项
    SlotId? Continue { get; }                         // 最近保存的可读存档位（SYS-SAVE）；没有就是 null
    SlotId? Active { get; }                           // 当前存档位：自动保存写到这里
    bool Busy { get; }                                // 后台写盘进行中
    SaveState State { get; }                          // 保存状态（SYS-SAVE）；值变时也发 SlotsChanged
    event Action? SlotsChanged;                       // 在主线程发出
    void Request(SaveReason r);                       // 自动请求合并、限频（两次尝试之间至少 30 s）、等安全时刻（SYS-SAVE）；只在漫游态有效；Quit、ToTitle、Manual 不走这里
    Task<SaveResult> SaveNow(SaveReason r);           // NewGame、Test、Manual；不等安全时刻，不受自动请求下限约束
    // Both loads restore every phase and complete only after the real endpoint is ready.
    Task<LoadResult> Load(SlotId s);
    Task<LoadResult> LoadFile(string path);           // 源文件只读；之后写到 --save 指定的存档位，没指定就不写
    // A null slot creates a complete temporary CLI world without activating or overwriting a save slot.
    Task<LoadResult> NewGame(SlotId? s, uint? seed = null, WorldPreset? start = null, NewGameBoot? boot = null);
    // seed 为 null 时取一次熵（G4 唯一的熵源例外）；start 为 null 时取本图的 newGameStart（SYS-SAVE）
    Task<SaveResult> FlushForQuit(SaveReason r, TimeSpan timeout);
    // r 只能是 Quit 或 ToTitle；旧代不受影响。保存失败或超时也照常离开，回标题失败后在开始页提示一次（SYS-SAVE-FLOW）
    byte[]? Thumbnail(SlotId s);                      // 最新可读一代的 WebP 字节，界面自己解码（SYS-SAVE）
}
