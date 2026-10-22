using Campus.Contracts.AppFlow;

namespace Campus.Hud;

/// <summary>
/// 界面发起的一次异步操作的持有者（STD-CODE「任务有主」）：同一时刻只跑一个，正在跑时再开返回 false，重复点击不叠第二个。
/// 作用域结束（HUD 离树）调 <see cref="Cancel"/>：按本持有者的令牌认取消、不算失败；其余失败带原异常交 <see cref="IAppFailure"/>，不重抛进引擎、不吞。
/// 被调方自己持有的操作照常走完，界面一侧只停等待；取消后操作体不得再碰界面。
/// </summary>
internal sealed class HudTask(IAppFailure failure)
{
    private readonly IAppFailure _failure = failure ?? throw new ArgumentNullException(nameof(failure));
    private CancellationTokenSource? _scope;
    private Task? _task;

    public bool Running => _task is { IsCompleted: false };

    public bool Start(Func<CancellationToken, Task> run)
    {
        ArgumentNullException.ThrowIfNull(run);
        if (Running) return false;
        _scope?.Dispose();
        _scope = new CancellationTokenSource();
        _task = Run(run, _scope.Token);
        return true;
    }

    public void Cancel() => _scope?.Cancel();

    // 续体经主线程同步上下文回来，Report 在主线程调用（IAppFailure 契约）。
    private async Task Run(Func<CancellationToken, Task> run, CancellationToken scope)
    {
        try { await run(scope); }
        catch (OperationCanceledException) when (scope.IsCancellationRequested) { }
        catch (Exception error) { _failure.Report(error); }
    }
}
