using Godot;

namespace Campus.Stage;

/// <summary>Owns the production stage's coalesced close/shutdown tasks and final process exit code.</summary>
internal sealed class StageShutdown(KernelStage stage)
{
    private readonly KernelStage _stage = stage;
    private Task? _shutdownTask;
    private Task? _closeTask;
    internal bool Started => _shutdownTask is not null;
    internal bool Complete { get; private set; }
    internal int ExitCode { get; private set; }
    internal Task? Pending => _shutdownTask;

    internal void MarkFailure() => ExitCode = 2;
    internal void MarkComplete() => Complete = true;
    internal void IncludeExitCode(int exitCode) => ExitCode = _stage.HasFailed ? 2 : Math.Max(ExitCode, exitCode);
    internal void RequestClose() => Close();
    internal void Request(int exitCode) => FinishQuit(exitCode);

    internal Task Close()
    {
        if (_shutdownTask is not null) return _shutdownTask;
        if (_closeTask is not null) return _closeTask;
        var start = new TaskCompletionSource();
        _closeTask = RunClose(start.Task);
        Callable.From(() => start.SetResult()).CallDeferred();
        return _closeTask;
    }

    private async Task RunClose(Task start)
    {
        try
        {
            await start;
            await _stage.CloseCoreAsync();
            if (!_stage.Closing) _closeTask = null;
        }
        catch (Exception error) { _stage.Report(error); }
    }

    internal Task FinishQuit(int exitCode)
    {
        IncludeExitCode(exitCode);
        if (_shutdownTask is not null) return _shutdownTask;
        if (_stage.HasTreeExited) throw new InvalidOperationException("Stage shutdown must begin before leaving the tree.");
        var start = new TaskCompletionSource();
        // Publish before cancellation or shutdown can reenter this boundary. Starting on the
        // next engine stack also lets startup publish the task that shutdown must drain.
        _shutdownTask = Finish(start.Task);
        _stage.BeginClosing();
        Callable.From(() => start.SetResult()).CallDeferred();
        return _shutdownTask;
    }

    private async Task Finish(Task start)
    {
        try
        {
            await start;
            await _stage.ShutdownAndQuit();
        }
        catch (Exception error)
        {
            _stage.Report(error);
            _stage.QuitEngine();
        }
    }
}
