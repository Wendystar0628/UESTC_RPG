using System.Numerics;
using Campus.Contracts.Save;

// 开始页与加载流程（照 SYS-TITLE，WP-49a 实现）
namespace Campus.Contracts.AppFlow;

public enum AppState : byte { Boot, Title, Entering, Playing, Leaving, Quitting }
[Flags] public enum AppActivity : byte { None = 0, MapTransition = 1, CameraTransition = 2, SaveWrite = 4, SeasonTransition = 8 }
public enum EnterKind : byte { NewGame, Continue, LoadSlot }
public enum EnterResult : byte { Entered, Rejected, Failed }     // Failed：回到 Title 并提示原因
public sealed record EnterRequest(EnterKind Kind, SlotId? Slot = null);   // SlotId 见存档块；NewGame 的 Slot 由开始页选定

public interface IAppFlow                     // Kernel 服务（WP-49a），主线程；本身不逐帧运行，不直接读写任何 user:// 文件
{
    AppState State { get; }                   // Boot → Title → Entering → Playing → Leaving → Title；任何状态都能走到 Quitting；
                                              // 退出时保存明确失败、观者选"返回游戏"，Quitting 回到 Playing（SYS-SAVE，D-171）
    long Rev { get; }
    bool IsPlaying { get; }
    string? LastError { get; }
    // Read directly from the owning services on every access; callers select the reasons relevant to their operation.
    AppActivity Activity { get; }
    event Action<AppState, AppState>? Changed; // (旧, 新)，值变才发
    event Action<string>? PhaseReached;       // Main-thread milestones; the runtime supplies its own clock.
    Task<EnterResult> Enter(EnterRequest r);   // 只在 Title 接受
    Task ReturnToTitle();                      // 只在 Playing 接受；先保存（SYS-SAVE）
    Task Quit();                               // 任何状态（SYS-TITLE）
}
