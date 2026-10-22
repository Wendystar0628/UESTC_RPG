using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Kernel.Diagnostics;
using Godot;

namespace Campus.Stage;

/// <summary>Retains terminal failures and joins the stage's unified shutdown.</summary>
internal sealed class StageStartupFailure(KernelStage stage)
{
    private readonly KernelStage _stage = stage;
    private readonly List<Exception> _failures = [];
    private string? _failurePhase;
    private bool _handlingFailure;
    private Task? _failureTask;
    internal bool Failed { get; private set; }

    internal Task Fail([FailureHandler("SYS-TITLE")] Exception error)
    {
        // Preserve the original error before any deferred main-thread work or cleanup.
        RecordFailure(error);
        return OnStageThread(() => FailOnStageThread(error));
    }

    private Task FailOnStageThread(Exception error)
    {
        Failed = true;
        _stage.MarkFailure();
        // Cleanup must not await its own failure task. Nested startup callbacks only record.
        if (!_stage.CanHandleFailure || _handlingFailure) return Task.CompletedTask;
        if (_failureTask is not null) return _failureTask;
        var completion = new TaskCompletionSource();
        _failureTask = completion.Task;
        _handlingFailure = true;
        try
        {
            try { _stage.FailBoot(error); }
            catch (Exception flowError) { RecordFailure(flowError); }
            try { _stage.CancelStartupCapture(); } catch (Exception cancelError) { RecordFailure(cancelError); }
        }
        finally { _handlingFailure = false; }
        _ = Handle();
        return _failureTask;

        async Task Handle()
        {
            try { await ((IAppExit)_stage).FinishQuit(2); completion.TrySetResult(); }
            catch (Exception shutdownError) { completion.TrySetException(shutdownError); }
        }
    }

    private void RecordFailure([FailureHandler("SYS-TITLE")] Exception error)
    {
        lock (_failures)
        {
            _failurePhase ??= _stage.Phase;
            var failures = new Queue<Exception>();
            failures.Enqueue(error);
            while (failures.TryDequeue(out var failure))
            {
                if (failure is AggregateException { InnerExceptions.Count: > 0 } aggregate)
                {
                    foreach (var inner in aggregate.InnerExceptions) failures.Enqueue(inner);
                    continue;
                }
                if (_failures.Any(existing => ReferenceEquals(existing, failure))) continue;
                _failures.Add(failure);
                DiagnosticLog.Error($"[SYS-TITLE][phase={_failurePhase}]", failure);
            }
        }
    }

    private static Task OnStageThread(Func<Task> action)
    {
        if (GodotThread.IsMainThread()) return action();
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Callable.From(async () =>
        {
            try { await action(); completion.TrySetResult(); }
            catch (Exception error) { completion.TrySetException(error); }
        }).CallDeferred();
        return completion.Task;
    }
}
