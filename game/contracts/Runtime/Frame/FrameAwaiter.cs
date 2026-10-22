using Campus.Contracts.AppFlow;
using Godot;

namespace Campus.Contracts;

/// <summary>
/// 运行时各模块跨帧等待的唯一写法（等帧、等绘制、等补间）：等待挂在宿主节点和调用方的令牌上，令牌取消或宿主离树都以取消结束，结束时退订。
/// 原生 ToSignal 不能取消，宿主释放时只留下 "Object was deleted while awaiting a callback"，所以跨帧等待不用它。
/// Queries scene state at process boundaries; render signals carry only draw evidence.
/// </summary>
public static class FrameAwaiter
{
    public static async Task ProcessAsync(Node host, CancellationToken cancellationToken = default)
    {
        Enter(cancellationToken);
        var tree = host.GetTree();
        await UntilAsync(host, tree, signal => tree.ProcessFrame += signal, signal => tree.ProcessFrame -= signal, cancellationToken);
    }

    /// <summary>
    /// 等补间播完；没播完就结束时停掉补间，不让它在作用域外继续改属性。
    /// 补间被杀不发 Finished，所以补间要由宿主节点 CreateTween 绑定，随宿主离树以取消结束。
    /// </summary>
    public static async Task TweenAsync(Tween tween, Node host, CancellationToken cancellationToken)
    {
        Enter(cancellationToken);
        var finished = false;
        try
        {
            await UntilAsync(host, tween, signal => tween.Finished += signal, signal => tween.Finished -= signal, cancellationToken);
            finished = true;
        }
        finally
        {
            if (!finished && GodotObject.IsInstanceValid(tween)) tween.Kill();
        }
    }

    /// <summary>The project's Safe render mode submits and completes this pair on the main thread.</summary>
    public static async Task DrawAsync(Node host, CancellationToken cancellationToken = default)
    {
        Enter(cancellationToken);
        var submitted = false;
        // Never run a consumer inline inside RenderingServer's signal dispatch.
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void OnPreDraw() => submitted = true;
        void OnPostDraw() { if (submitted) completion.TrySetResult(); }
        void OnExit() => completion.TrySetCanceled();
        RenderingServer.Singleton.FramePreDraw += OnPreDraw;
        RenderingServer.Singleton.FramePostDraw += OnPostDraw;
        host.TreeExiting += OnExit;
        try { await completion.Task.WaitAsync(cancellationToken); }
        finally
        {
            RenderingServer.Singleton.FramePreDraw -= OnPreDraw;
            RenderingServer.Singleton.FramePostDraw -= OnPostDraw;
            if (GodotObject.IsInstanceValid(host)) host.TreeExiting -= OnExit;
        }
    }

    public static async Task StartupReadyDrawAsync(Node host, Func<bool> ready, TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        try { await ReadyDrawAsync(host, ready, timeout, cancellationToken); }
        catch (TimeoutException error)
        {
            throw AppStartupException.Stalled("Startup frame presentation stalled.", error);
        }
    }

    public static async Task ReadyDrawAsync(Node host, Func<bool> ready, TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        using var deadline = new CancellationTokenSource(timeout);
        using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, deadline.Token);
        try
        {
            while (true)
            {
                await ProcessAsync(host, lifetime.Token);
                lifetime.Token.ThrowIfCancellationRequested();
                // Resource processing can still emit draw signals while presentation is disabled.
                if (!RenderingServer.RenderLoopEnabled || !ready()) continue;
                await DrawAsync(host, lifetime.Token);
                await ProcessAsync(host, lifetime.Token);
                lifetime.Token.ThrowIfCancellationRequested();
                if (RenderingServer.RenderLoopEnabled && ready()) return;
            }
        }
        catch (OperationCanceledException error) when (deadline.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException("Frame presentation stalled.", error);
        }
    }

    private static void Enter(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!GodotThread.IsMainThread()) throw new InvalidOperationException("Frame waits begin on the Godot main thread.");
    }

    // 等 source 的一次信号：信号到即完成，令牌取消或宿主离树即取消；source 或宿主已释放就不再退订。
    private static async Task UntilAsync(Node host, GodotObject source, Action<Action> subscribe, Action<Action> unsubscribe,
        CancellationToken cancellationToken)
    {
        var completion = new TaskCompletionSource();
        void OnSignal() => completion.TrySetResult();
        void OnExit() => completion.TrySetCanceled();
        subscribe(OnSignal);
        host.TreeExiting += OnExit;
        try { await completion.Task.WaitAsync(cancellationToken); }
        finally
        {
            if (GodotObject.IsInstanceValid(source)) unsubscribe(OnSignal);
            if (GodotObject.IsInstanceValid(host)) host.TreeExiting -= OnExit;
        }
    }
}
