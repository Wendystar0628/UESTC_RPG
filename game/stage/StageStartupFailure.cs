using Campus.Contracts;
using Campus.Kernel.Diagnostics;
using Godot;

namespace Campus.Stage;

/// <summary>Retains terminal failures and requests unified shutdown after the reporting stack returns.</summary>
internal sealed class StageStartupFailure(KernelStage stage)
{
    private readonly KernelStage _stage = stage;
    private readonly List<Exception> _failures = [];
    private string? _failurePhase;
    private bool _failureScheduled;
    internal bool Failed { get; private set; }

    internal void Report([FailureHandler("ENG-ARCH")] Exception error)
    {
        ArgumentNullException.ThrowIfNull(error);
        if (!GodotThread.IsMainThread())
            throw new InvalidOperationException("Application failures must be delivered on the engine thread.");
        RecordFailure(error);
        Failed = true;
        _stage.MarkFailure();
        if (!_stage.CanHandleFailure || _failureScheduled) return;
        _failureScheduled = true;
        // Let the reporting owner publish its task before cancellation resumes its continuations.
        Callable.From(() => HandleFailure(error)).CallDeferred();
    }

    private void HandleFailure(Exception error)
    {
        if (!_stage.CanHandleFailure) return;
        try { _stage.FailBoot(error); }
        catch (Exception flowError) { Report(flowError); }
        try { _stage.CancelStartupCapture(); }
        catch (Exception cancelError) { Report(cancelError); }
        _stage.RequestFailureShutdown();
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
}
