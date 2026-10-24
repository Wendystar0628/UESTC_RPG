using Campus.Contracts.AppFlow;

namespace Campus.Hud;

/// <summary>
/// 界面发起的一次异步操作的持有者（STD-CODE「任务有主」）：同一时刻只跑一个，正在跑时再开返回 false，重复点击不叠第二个。
/// "正在跑"只认操作体自己的任务：体一结束即可再开，不等交付失败的续体晚一帧回来；先交意图再 Start 的排队写法因此不会落进无人等的空档。
/// 每次 Start 各有一层等待，等到它的操作体结束为止。
/// 作用域结束（HUD 离树）调 <see cref="Cancel"/>：按本持有者的令牌认取消、不算失败；其余失败带原异常交 <see cref="IAppFailure"/>，不重抛进引擎、不吞。
/// 被调方自己持有的操作照常走完，界面一侧只停等待；取消后操作体不得再碰界面。
/// </summary>
internal sealed class HudTask(IAppFailure failure)
{
    private readonly IAppFailure _failure = failure ?? throw new ArgumentNullException(nameof(failure));
    private CancellationTokenSource? _scope;
    private Task? _body;
    private Task? _observer;

    public bool Running => _body is { IsCompleted: false };

    public bool Start(Func<CancellationToken, Task> run)
    {
        ArgumentNullException.ThrowIfNull(run);
        if (Running) return false;
        var scope = new CancellationTokenSource();
        _scope = scope;
        _observer = Observe(run, scope);
        return true;
    }

    // 只取消还没等完的那一次；等完的作用域已随它的等待释放。
    public void Cancel()
    {
        if (_observer is { IsCompleted: false }) _scope!.Cancel();
    }

    // 续体经主线程同步上下文回来，Report 在主线程调用（IAppFailure 契约）；每次的令牌源归这一次的等待，等完即释放。
    // 操作体在第一次 await 之前就记下，Start 返回时 Running 已认它；同步抛出由这层等待照常交付。
    private async Task Observe(Func<CancellationToken, Task> run, CancellationTokenSource scope)
    {
        try { await (_body = run(scope.Token)); }
        catch (OperationCanceledException) when (scope.IsCancellationRequested) { }
        catch (Exception error) { _failure.Report(error); }
        finally { scope.Dispose(); }
    }
}
