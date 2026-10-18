using System.Numerics;
using Campus.Contracts.Save;

// 开始页与过场（照 SYS-TITLE，WP-49a 实现）
namespace Campus.Contracts.AppFlow;

public enum AppState : byte { Boot, Title, Entering, Playing, Leaving, Quitting }
public enum EnterKind : byte { NewGame, Continue, LoadSlot }
public enum CinematicMode : byte { Full, Short, Off }            // 设置项"开场过场"（SYS-TITLE）
public enum EnterPath : byte { Cinematic, ShortCinematic, Veil } // 本次实际走的路径；减少动态效果时为 Veil
public enum EnterResult : byte { Entered, Rejected, Failed }     // Failed：回到 Title 并提示原因
public sealed record EnterRequest(EnterKind Kind, SlotId? Slot = null);   // SlotId 见存档块；NewGame 的 Slot 由开始页选定

public interface IAppFlow                     // Kernel 服务（WP-49a），主线程；本身不逐帧运行，不直接读写任何 user:// 文件
{
    AppState State { get; }                   // Boot → Title → Entering → Playing → Leaving → Title；任何状态都能走到 Quitting；
                                              // 退出时保存明确失败、观者选"返回游戏"，Quitting 回到 Playing（SYS-SAVE，D-171）
    long Rev { get; }
    event Action<AppState, AppState>? Changed; // (旧, 新)，值变才发
    EnterPath? Path { get; }                   // 只在 Entering 时有值
    bool Skippable { get; }                    // Entering 且还没开始收尾
    Task<EnterResult> Enter(EnterRequest r);   // 只在 Title 接受
    Task ReturnToTitle();                      // 只在 Playing 接受；先保存（SYS-SAVE）
    void Skip();                               // 输入层在按住 Esc 0.8 s 后调用；跳过后的终态与完整播放一致（SYS-TITLE）
    Task Quit();                               // 任何状态（SYS-TITLE）
}

// 过场数据的记录：game/contracts/Runtime/Cinematic/（命名空间 Campus.Contracts.Cinematic），WP-00 合入；
// CinematicDef、CameraSeg、WorldCue、MaskSeg、CaptionCue、ExtraCue、AudioCue、CinematicEnd 的字段与 SYS-TITLE 的 JSON 一一对应，
// 数据放在 game/data/maps/<地图>/cinematics/<id>.json，format 只增；遇到不认识的轨由 SYS-TITLE 的检查报错，不静默忽略。
// 现在没有"角色轨"
// CinematicPlayer 不是契约：它是 WP-49a 在 game/camera/cinematic 里的 [KernelNode]，ProcessPriority = FramePriority.Camera（−1100），
// 只在 Title、Entering、Leaving 状态里做事；镜头只经 ICameraRig.SetPose，世界只经 ISaveService 和 IWorldControl（FlowTo、SetFlow），
// 黑边、字幕、标题卡经 WP-35d 的放映层接口，雾幕经 IVeilService，音频经 IAudioService.Cine（SYS-TITLE）
