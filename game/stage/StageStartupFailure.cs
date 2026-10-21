using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Startup;
using Campus.Kernel.Diagnostics;
using Campus.Kernel.Registry;
using Godot;
using AppBoot = Campus.Kernel.Boot.Boot;

namespace Campus.Stage;

/// <summary>Owns failure records, the single startup presentation attempt and its retry/quit decision.</summary>
internal sealed class StageStartupFailure(KernelStage stage)
{
    private readonly KernelStage _stage = stage;
    private readonly List<Exception> _failures = [];
    private string? _failurePhase;
    private bool _handlingFailure;
    private bool _bootCompleted;
    private Task? _failureTask;
    private IAppStartupFailurePresentation _startupFailurePresentation = null!;
    private bool _presentationBound;
    private StartupFailureSession? _startupFailureSession;
    internal bool Failed { get; private set; }

    internal void BindPresentation(ServiceRegistry registry)
    {
        _startupFailurePresentation = registry.Get<IAppStartupFailurePresentation>();
        _presentationBound = true;
    }

    internal void CompleteBoot() => _bootCompleted = true;
    internal void InvalidateSession() => _startupFailureSession?.Invalidate();

    internal Task Fail([FailureHandler("ENG-ARCH")] Exception error)
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
            try
            {
                if (_presentationBound && CanPresentStartupFailure())
                {
                    // Join the existing cleanup; its fade must not run after failure-page acknowledgement.
                    await _stage.ReleaseStartupPresentation();
                    if (CanPresentStartupFailure())
                    {
                        var session = new StartupFailureSession(this, DescribeStartupFailure(error));
                        _startupFailureSession = session;
                        var acknowledged = await _startupFailurePresentation.Show(session);
                        if (acknowledged && CanPresentStartupFailure())
                        {
                            session.Accept();
                            completion.TrySetResult();
                            return;
                        }
                    }
                }
            }
            catch (Exception presentationError) { RecordFailure(presentationError); }
            try { await ((IAppExit)_stage).FinishQuit(2); completion.TrySetResult(); }
            catch (Exception shutdownError) { completion.TrySetException(shutdownError); }
        }
    }

    private bool CanPresentStartupFailure() => !_bootCompleted && _stage.CanKeepFailurePresentation &&
        _stage.IsInsideTree() && DisplayServer.GetName() != "headless" &&
        AppBoot.Options is { Mode: BootRunMode.Interactive, Automated: false, Title: true,
            Flow.ShowTitle: true, Ui.Visible: not false };

    private AppStartupFailure DescribeStartupFailure(Exception error) =>
        new(SourceCode(error), _failurePhase!, BuildInfo.Build, "user://logs");

    private static string SourceCode(Exception error) =>
        error is AppStartupException startup ? startup.Code : AppStartupFailureCodes.Unexpected;

    private void RecordFailure(Exception error)
    {
        lock (_failures)
        {
            _failurePhase ??= _stage.Phase;
            IEnumerable<Exception> failures = error is AggregateException aggregate ? aggregate.Flatten().InnerExceptions : [error];
            foreach (var failure in failures)
            {
                if (_failures.Any(existing => ReferenceEquals(existing, failure))) continue;
                _failures.Add(failure);
                DiagnosticLog.Error($"[ENG-ARCH][phase={_failurePhase}]", failure);
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

    private sealed class StartupFailureSession(StageStartupFailure owner, AppStartupFailure failure) : IAppStartupFailureSession
    {
        private bool _accepted;
        private Task? _decision;
        public AppStartupFailure Failure { get; } = failure;
        public void Accept() => _accepted = true;
        public void Invalidate() => _accepted = false;
        public Task Retry() => OnStageThread(() => Decide(restart: true));
        public Task Quit() => OnStageThread(() => Decide(restart: false));

        private Task Decide(bool restart)
        {
            if (_decision is not null) return _decision;
            if (!ReferenceEquals(owner._startupFailureSession, this) || !owner._stage.CanKeepFailurePresentation)
                return owner._stage.ShutdownCompletion;
            if (restart && !_accepted)
                throw new InvalidOperationException("Startup retry requires presentation acknowledgement.");
            var completion = new TaskCompletionSource();
            _decision = completion.Task;
            _accepted = false;
            _ = Finish();
            return _decision;

            async Task Finish()
            {
                try
                {
                    // Godot's filtered GetCmdlineArgs loses consumed engine flags and the user separator.
                    // Preserve the original process argv without parsing or reconstructing either argument set.
                    if (restart) OS.SetRestartOnExit(true, System.Environment.GetCommandLineArgs()[1..]);
                }
                catch (Exception restartError) { owner.RecordFailure(restartError); }
                try { await ((IAppExit)owner._stage).FinishQuit(2); completion.TrySetResult(); }
                catch (Exception shutdownError) { completion.TrySetException(shutdownError); }
            }
        }
    }
}
