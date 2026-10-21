using System.Reflection;
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
    private IAppStartupFailurePresentation? _startupFailurePresentation;
    private StartupFailureSession? _startupFailureSession;
    internal bool Failed { get; private set; }
    internal string? Failure { get; private set; }

    internal void BindPresentation(ServiceRegistry registry)
    {
        // Resolve the declared UI capability; an existing service is not a display acknowledgement.
        if (registry.Entries.Any(entry => entry.Contract == typeof(IAppStartupFailurePresentation)))
            _startupFailurePresentation = registry.Get<IAppStartupFailurePresentation>();
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
                if (CanPresentStartupFailure() && _startupFailurePresentation is { } presentation)
                {
                    // Join the existing cleanup; its fade must not run after failure-page acknowledgement.
                    await _stage.ReleaseStartupPresentation();
                    if (CanPresentStartupFailure())
                    {
                        var session = new StartupFailureSession(this, DescribeStartupFailure(error));
                        _startupFailureSession = session;
                        var acknowledged = await presentation.Show(session);
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
            Flow.ShowTitle: true, Ui.Visible: not false } &&
        _startupFailurePresentation is Node node && GodotObject.IsInstanceValid(node) && node.IsInsideTree() && node.IsNodeReady();

    private AppStartupFailure DescribeStartupFailure(Exception error)
    {
        var cause = error.GetBaseException();
        var (code, reason) = cause switch
        {
            FileNotFoundException or DirectoryNotFoundException or TypeLoadException or ReflectionTypeLoadException or BadImageFormatException =>
                ("STARTUP_INSTALLATION", "启动所需的安装文件缺失或与程序不配套。"),
            TimeoutException => ("STARTUP_STALLED", "启动准备已停止推进，请重试。"),
            InvalidDataException => ("STARTUP_DATA_INVALID", "启动数据不完整或与程序不配套。"),
            _ when _failurePhase == "travel-boot" => ("STARTUP_MAP_PREPARATION", "校园地图未能准备完成。"),
            _ => ("STARTUP_UNEXPECTED", "启动时遇到错误，未能进入游戏。")
        };
        return new(reason, code, _failurePhase!, BuildInfo.Build, "user://logs");
    }

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
            Failure = new AggregateException("Stage failed.", _failures).GetBaseException().Message;
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
            if (restart && !_accepted) return Task.CompletedTask;
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
