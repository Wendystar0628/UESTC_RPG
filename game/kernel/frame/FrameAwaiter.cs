using Campus.Contracts.AppFlow;
using Godot;

namespace Campus.Kernel.Frame;

/// <summary>Queries scene state at process boundaries; render signals carry only draw evidence.</summary>
public static class FrameAwaiter
{
    public static async Task ProcessAsync(Node host, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!GodotThread.IsMainThread()) throw new InvalidOperationException("Frame waits begin on the Godot main thread.");
        var tree = host.GetTree();
        var completion = new TaskCompletionSource();
        void OnProcess() => completion.TrySetResult();
        void OnExit() => completion.TrySetCanceled();
        tree.ProcessFrame += OnProcess;
        host.TreeExiting += OnExit;
        try { await completion.Task.WaitAsync(cancellationToken); }
        finally
        {
            if (GodotObject.IsInstanceValid(tree)) tree.ProcessFrame -= OnProcess;
            if (GodotObject.IsInstanceValid(host)) host.TreeExiting -= OnExit;
        }
    }

    /// <summary>The project's Safe render mode submits and completes this pair on the main thread.</summary>
    public static async Task DrawAsync(Node host, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!GodotThread.IsMainThread()) throw new InvalidOperationException("Frame waits begin on the Godot main thread.");
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
}
