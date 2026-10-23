using System.Numerics;
using Campus.Contracts.Save;

// 开始页与加载流程（照 SYS-TITLE，game/flow/ 实现）
namespace Campus.Contracts.AppFlow;

public enum AppState : byte { Boot, Title, Entering, Playing, Leaving, Quitting }
[Flags] public enum AppActivity : byte { None = 0, MapTransition = 1, CameraTransition = 2, SaveWrite = 4, SeasonTransition = 8 }
public enum EnterKind : byte { NewGame, Continue, LoadSlot }
public enum EnterResult : byte { Entered, Rejected, Failed }     // Failed：回到 Title 并提示原因
public sealed record EnterRequest(EnterKind Kind, SlotId? Slot = null);   // SlotId 见存档块；NewGame 的 Slot 由开始页选定

public interface IAppFlow                     // Kernel 服务（game/flow/），主线程；本身不逐帧运行，不直接读写任何 user:// 文件
{
    AppState State { get; }                   // Boot → Title → Entering → Playing → Leaving → Title；任何状态都能走到 Quitting；
                                              // 保存失败或超时照常离开；Quitting 等真实排空与资源释放（SYS-SAVE-FLOW）
    long Rev { get; }
    bool IsPlaying { get; }
    string? LastError { get; }                // 最近流程失败的本地化文案键；原异常交失败载体与诊断日志，不向界面返回异常消息
    // Read directly from the owning services on every access; callers select the reasons relevant to their operation.
    AppActivity Activity { get; }
    event Action<AppState, AppState>? Changed; // (旧, 新)，值变才发
    event Action<string>? PhaseReached;       // Main-thread milestones; the runtime supplies its own clock.
    Task<EnterResult> Enter(EnterRequest r);   // 只在 Title 接受
    Task ReturnToTitle();                      // 只在 Playing 接受；尝试保存后离开，失败与超时不拦；最终写盘结果读 ISaveService.State/SlotsChanged（SYS-SAVE-FLOW）
    Task Quit();                               // 任何状态；尝试保存后仍等待真实关闭完成（SYS-TITLE）
}
