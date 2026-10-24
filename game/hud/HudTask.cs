using Campus.Contracts.AppFlow;

namespace Campus.Hud;

/// <summary>
/// 界面发起的一次异步操作的持有者（STD-CODE「任务有主」）：同一时刻只跑一个，正在跑时再开返回 false，重复点击不叠第二个。
/// "正在跑"只认操作体自己的任务：体一结束即可再开，不等交付失败的续体晚一帧回来；先交意图再 Start 的排队写法因此不会落进无人等的空档。
/// 每次 Start 各有一层等待，等到它的操作体结束、交付完为止；持有者记着全部还没交付的等待，取消与排空按全部来，不只认最后一次。
/// 作用域结束调 <see cref="Cancel"/>（HUD 离树）或先 <see cref="Stop"/> 再等 <see cref="DrainAsync"/>（应用退出，ENG-ARCH「关闭顺序」）：
/// 按每次自己的令牌认取消、不算失败；其余失败带原异常交 <see cref="IAppFailure"/>，不重抛进引擎、不吞。
/// 被调方自己持有的操作照常走完，界面一侧只停等待；取消后操作体不得再碰界面。
/// </summary>
internal sealed class HudTask(IAppFailure failure)
{
    private readonly IAppFailure _failure = failure ?? throw new ArgumentNullException(nameof(failure));
    private readonly List<Wait> _waits = [];
    private Task? _body;
    private bool _stopped;

    public bool Running => _body is { IsCompleted: false };
    public bool Accepting => !_stopped;

    public bool Start(Func<CancellationToken, Task> run)
    {
        ArgumentNullException.ThrowIfNull(run);
        if (_stopped || Running) return false;
        var scope = new CancellationTokenSource();
        Task body;
        // 同步抛出时操作体已经结束，没有可等的任务：当场带原异常交失败接收方，这次算已开过。
        try { body = run(scope.Token); }
        catch (Exception error)
        {
            scope.Dispose();
            _failure.Report(error);
            return true;
        }
        // 操作体在第一次 await 之前就记下，Start 返回时 Running 已认它。
        _body = body;
        var wait = new Wait(body, scope);
        _waits.Add(wait);
        wait.Observer = Observe(wait);
        return true;
    }

    /// <summary>取消全部还没交付的等待；体已结束、续体还没回来的当场交付，不留到界面释放之后。</summary>
    public void Cancel()
    {
        foreach (var wait in _waits.ToArray())
        {
            wait.Scope.Cancel();
            if (wait.Body.IsCompleted) Settle(wait);
        }
    }

    /// <summary>停接新的 Start 并取消已接受的等待；只在应用退出时调，之后不再开（封口没有解封）。</summary>
    public void Stop()
    {
        _stopped = true;
        Cancel();
    }

    /// <summary>等全部已接受的等待交付完失败或取消；等待本身不抛，失败已交 IAppFailure。</summary>
    public Task DrainAsync() => Task.WhenAll(_waits.Select(wait => wait.Observer!));

    // 续体经主线程同步上下文回来，Report 在主线程调用（IAppFailure 契约）；这里只等体结束，结果统一由 Settle 读出。
    private async Task Observe(Wait wait)
    {
        await wait.Body.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing | ConfigureAwaitOptions.ContinueOnCapturedContext);
        Settle(wait);
    }

    // 每次等待只交付一次：续体与 Cancel 谁先到谁交付；每次的令牌源归这一次的等待，交付完即释放。
    private void Settle(Wait wait)
    {
        if (!_waits.Remove(wait)) return;
        try { wait.Body.GetAwaiter().GetResult(); }
        catch (OperationCanceledException) when (wait.Scope.IsCancellationRequested) { }
        catch (Exception error) { _failure.Report(error); }
        finally { wait.Scope.Dispose(); }
    }

    private sealed class Wait(Task body, CancellationTokenSource scope)
    {
        public Task Body { get; } = body;
        public CancellationTokenSource Scope { get; } = scope;
        public Task? Observer { get; set; }
    }
}
