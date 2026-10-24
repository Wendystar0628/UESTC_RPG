using Campus.Contracts.AppFlow;

namespace Campus.Hud;

/// <summary>
/// 界面交给服务的一串请求的持有者：每次请求当场交给服务（服务串行处理），这里按顺序逐个等到落定，某次失败带原异常交 <see cref="IAppFailure"/> 后接着等后面的。
/// 持有者归 HUD、不归发起请求的页面：页面离树只退订与停止刷新，已交出的请求照常等完；应用退出由 HudRoot 经 <see cref="Waiting"/> 停接、排空。
/// 被调方持有的请求照常走完，取消不提前结束这里的等待；等待只交付失败，不碰界面。
/// </summary>
internal sealed class HudRequests(IAppFailure failure)
{
    private readonly IAppFailure _failure = failure ?? throw new ArgumentNullException(nameof(failure));
    private readonly Queue<Task> _requests = new();

    public HudTask Waiting { get; } = new(failure);

    /// <summary>退出停接后不再把新请求交给服务；正在等时由那一轮接着等新交的请求。</summary>
    public void Submit(Func<Task> send)
    {
        ArgumentNullException.ThrowIfNull(send);
        if (!Waiting.Accepting) return;
        Task request;
        // 同步抛出说明请求没交到服务手里，没有可等的任务：当场带原异常交失败接收方。
        try { request = send(); }
        catch (Exception error) { _failure.Report(error); return; }
        _requests.Enqueue(request);
        Waiting.Start(Drain);
    }

    private async Task Drain(CancellationToken _)
    {
        while (_requests.TryDequeue(out var request))
        {
            try { await request; }
            catch (Exception error) { _failure.Report(error); }
        }
    }
}
