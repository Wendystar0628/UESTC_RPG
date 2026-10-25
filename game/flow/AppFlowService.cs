using Campus.Contracts.Diagnostics;
using Campus.Kernel.Diagnostics;
using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Maps;
using Campus.Contracts.Save;
using Campus.Contracts.Settings;
using Campus.Contracts.Startup;

namespace Campus.Flow;

[KernelService(typeof(IAppFlow))]
[KernelService(typeof(IAppBootFlow))]
public sealed class AppFlowService : IAppFlow, IAppBootFlow, IBootParticipant, IDisposable
{
    private readonly ICameraRig camera;
    private readonly ITransitDriver transit;
    private readonly ISaveService saves;
    private readonly IMapService maps;
    private readonly IWorldState world;
    private readonly IWorldRestore restore;
    private readonly IAppExit appExit;
    private readonly AppFlowResources _resources;
    private readonly AppFlowContinuation _continuation;
    private readonly AppFlowStartup _startup;
    private readonly AppFlowRecovery _recovery;
    private Task<EnterResult>? _entry;
    private readonly IMapRuntime _mapRuntime;
    private Task? _quit;
    private Task? _leaving;
    private bool _leaveSaveCompleted;
    private Pose _leavePose;
    private bool _quitting;
    private EnterRequest? _request;
    private bool _directEntry;
    private WorldPreset? _directStart;
    private NewGameBoot? _directBoot;
    private long _enterOperation;
    private readonly CommandNotification _commands = new();
    private readonly Queue<(AppState Previous, AppState Current)> _stateNotifications = new();

    public AppFlowService(ICameraRig camera, IAppLoadingPresentation loading, IAppFrontispiecePresentation frontispiece,
        ISaveService saves, IMapService maps, IWorldState world, IWorldControl control,
        IWorldRestore restore, IAppExit appExit, IMapRuntime mapRuntime, IMapStreamState stream,
        ITransitDriver transit, IDiagnosticLog diagnostics, ISettingsStore? profileSettings = null)
    {
        _mapRuntime = mapRuntime;
        this.camera = camera; this.transit = transit; this.saves = saves; this.maps = maps;
        this.world = world; this.restore = restore; this.appExit = appExit;
        _resources = new(camera, restore, control, loading, frontispiece, mapRuntime, MarkPhase);
        _continuation = new(camera, saves, maps, restore, mapRuntime, stream,
            _resources, profileSettings);
        _recovery = new(maps, restore, mapRuntime, _resources, () => _quitting, CompleteTitle, diagnostics.Error);
        _startup = new(maps, _continuation, _resources);
    }
    public AppState State { get; private set; } = AppState.Boot;
    public bool IsPlaying => State switch
    {
        AppState.Playing => true,
        AppState.Boot or AppState.Title or AppState.Entering or AppState.Leaving or AppState.Quitting => false,
        _ => throw new InvalidOperationException("Unknown application state."),
    };
    public AppActivity Activity =>
        (maps.Transition is { Done.IsCompleted: false } ? AppActivity.MapTransition : AppActivity.None) |
        (camera.ActiveTransit is { Done.IsCompleted: false } ? AppActivity.CameraTransition : AppActivity.None) |
        (saves.Busy ? AppActivity.SaveWrite : AppActivity.None) |
        ((world.Transition & WorldTransition.Season) != 0 ? AppActivity.SeasonTransition : AppActivity.None);
    public long Rev { get; private set; }
    public event Action<AppState, AppState>? Changed;
    public event Action<AppState, AppState>? StateAdvancing;
    public event Action<string>? PhaseReached;
    public event Action<EnterCommandEvent>? EnterCommand;

    public Task<EnterResult> Enter(EnterRequest request)
    {
        var operation = ++_enterOperation;
        if (!In(AppState.Title)) return RejectEnter(operation, request);
        if (request.Gen is not null && request.Kind is not EnterKind.LoadSlot) return RejectEnter(operation, request);
        var slot = request.Kind switch
        {
            EnterKind.NewGame => request.Slot,
            EnterKind.Continue => request.Slot ?? saves.Continue,
            EnterKind.LoadSlot => request.Slot,
            _ => throw new ArgumentOutOfRangeException(nameof(request)),
        };
        if (slot is null || _continuation.Info(slot.Value) is not { } info) return RejectEnter(operation, request);
        if (request.Kind is EnterKind.Continue or EnterKind.LoadSlot && !AppFlowContinuation.Readable(info, request.Gen)) return RejectEnter(operation, request);
        _request = request with { Slot = slot }; _quitting = false; _directEntry = false;
        _recovery.BeginEntry(); _continuation.BeginEntry(_request);
        return StartEntry(operation, _request, false);
    }
    private Task<EnterResult> RejectEnter(long operation, EnterRequest target)
    {
        _commands.Publish(EnterCommand, new EnterCommandEvent(operation, EnterCommandPhase.Rejected, target, EnterResult.Rejected, false, null));
        return Task.FromResult(EnterResult.Rejected);
    }
    public async Task<IMapArrival> EnterDirect(WorldPreset? start = null, NewGameBoot? boot = null)
    {
        if (!In(AppState.Boot) || _startup.ShowTitle)
            throw new InvalidOperationException("Direct entry requires an explicit boot entry intent.");
        var operation = ++_enterOperation;
        var request = _startup.Request;
        _directEntry = true; _directStart = start; _directBoot = boot;
        _request = new(request.LoadFile is not null || request.Slot is not null ? EnterKind.LoadSlot : EnterKind.NewGame, request.Slot ?? (request.LoadFile is null ? _startup.WriteSlot : null));
        _recovery.BeginEntry(); _continuation.BeginEntry(_request);
        if (await StartEntry(operation, _request, true) != EnterResult.Entered)
            throw Failure ?? new InvalidOperationException("Direct entry failed.");
        return _mapRuntime.Arrival ?? throw new InvalidOperationException("Direct entry has no prepared destination.");
    }
    private Task<EnterResult> StartEntry(long operation, EnterRequest target, bool direct)
    {
        var done = new TaskCompletionSource<EnterResult>(); _entry = done.Task;
        _ = RunEntry(operation, target, direct, done);
        return done.Task;
    }
    private async Task RunEntry(long operation, EnterRequest target, bool direct, TaskCompletionSource<EnterResult> done)
    {
        try { done.TrySetResult(await EnterCore(operation, target, direct)); }
        catch (Exception error) { done.TrySetException(error); }
    }
    private async Task<EnterResult> EnterCore(long operation, EnterRequest target, bool direct)
    {
        var joiningBackground = false;
        EnterResult? outcome = null;
        try
        {
            _resources.HoldInput();
            if (!restore.RestoreFaulted) _resources.HoldLoadingFlow();
            Change(AppState.Entering);
            PublishStateChanges();
            _commands.Publish(EnterCommand, new EnterCommandEvent(operation, EnterCommandPhase.Started, target, null, direct, null));
            if (!direct && target.Kind is EnterKind.NewGame) MarkPhase("new-game-enter");
            var newGame = _request!.Kind switch
            {
                EnterKind.NewGame => true,
                EnterKind.Continue or EnterKind.LoadSlot => false,
                _ => throw new ArgumentOutOfRangeException(nameof(_request)),
            };
            await _resources.BeginLoading(_request, newGame && _request.Slot is not null);
            // A faulted world's NewGame transaction owns its recovery preparation.
            if (!newGame || !restore.RestoreFaulted)
            {
                joiningBackground = true;
                await BackgroundReady;
                joiningBackground = false;
            }
            if (newGame) await _continuation.StartNewTimeline(_request.Slot, _startup.Seed, _directEntry ? _directStart : null, _directEntry ? _directBoot : null);
            else if (_directEntry && _startup.Request.LoadFile is { } file)
            {
                AppFlowContinuation.RequireLoaded(await saves.LoadFile(file));
                _resources.CompleteWorldPreparation();
            }
            else await _continuation.RestoreRequested(_request.Slot!.Value, _request.Gen);
            _continuation.RequireHealthyWorld();
            await _continuation.PreparePlayableDestination(_startup.CaptureMode);
            // The real destination now owns residency; keep the pins through its terrain preparation.
            _continuation.ReleaseTitlePins();
            if (newGame && _request.Slot is not null)
            {
                // The first generation belongs to this prepared Walk endpoint.
                var saved = await saves.SaveNow(SaveReason.NewGame);
                if (!AppFlowContinuation.InitialSaveAccepted(saved.Status)) throw new IOException(saved.Error ?? "The new game could not be saved.");
                _resources.CompleteSavePreparation();
            }
            _continuation.RequireHealthyWorld();
            if (_quitting) { _resources.CompleteLoadingProgress(); _resources.ReleaseFlow(); outcome = EnterResult.Entered; return EnterResult.Entered; }
            await _resources.FinishLoading();
            _continuation.RequireHealthyWorld();
            _resources.ReleaseFlow(); Change(AppState.Playing); _resources.ReleaseInput();
            PublishStateChanges();
            outcome = EnterResult.Entered;
            return EnterResult.Entered;
        }
        catch (OperationCanceledException) when (_quitting && joiningBackground && BackgroundReady.IsCanceled)
        { outcome = EnterResult.Rejected; return EnterResult.Rejected; }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            _recovery.CaptureFailure(error, State);
            if (_directEntry) { outcome = EnterResult.Failed; return EnterResult.Failed; }
            if (restore.RestoreFaulted) await _recovery.HoldRestoreFailure();
            else if (error is not AppStartupException && !BackgroundReady.IsFaulted && !BackgroundReady.IsCanceled && _mapRuntime.Failure is null)
                await _recovery.RecoverTitle();
            outcome = EnterResult.Failed;
            return EnterResult.Failed;
        }
        finally
        {
            PublishStateChanges();
            _commands.Publish(EnterCommand, new EnterCommandEvent(operation, outcome.HasValue ? EnterCommandPhase.Completed : EnterCommandPhase.Faulted,
                target, outcome, direct, LastError));
        }
    }
    public Task ReturnToTitle()
    {
        if (restore.RestoreFaulted) return _recovery.HoldRestoreFailure();
        if (!In(AppState.Playing)) return Task.CompletedTask;
        _recovery.BeginEntry();
        _leaveSaveCompleted = false; _leavePose = camera.Pose;
        var done = new TaskCompletionSource(); _leaving = done.Task;
        _ = RunReturnToTitle(done);
        return done.Task;
    }
    private async Task RunReturnToTitle(TaskCompletionSource done)
    {
        try { await ReturnToTitleCore(); done.TrySetResult(); }
        catch (Exception error) { done.TrySetException(error); }
    }
    private async Task ReturnToTitleCore()
    {
        try
        {
            _resources.HoldInput(); _resources.HoldLoadingFlow(); Change(AppState.Leaving);
            PublishStateChanges();
            await transit.CancelAsync();
            _leavePose = camera.Pose;
            await _resources.BeginLoading(null, save: true);
            await FlushBeforeLeaving(SaveReason.ToTitle, FlowTuning.TitleSaveTimeoutSeconds);
            _leaveSaveCompleted = true;
            if (restore.RestoreFaulted) { await _recovery.HoldRestoreFailure(); return; }
            _resources.CompleteSavePreparation();
            _resources.HoldTitleFlow();
            _mapRuntime.ClearDestination(); restore.EndInstance();
            await CompleteTitle();
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            _recovery.CaptureFailure(error, State);
            if (restore.RestoreFaulted) { await _recovery.HoldRestoreFailure(); return; }
            if (!_leaveSaveCompleted)
            {
                camera.SetPose(_leavePose);
                await _continuation.PreparePlayableDestination(_startup.CaptureMode); await _resources.FinishLoading(); _resources.ReleaseFlow();
                if (!_quitting) { Change(AppState.Playing); _resources.ReleaseInput(); }
            }
            else await _recovery.RecoverTitle();
        }
        finally { PublishStateChanges(); }
    }
    private async Task CompleteTitle(bool keepBoot = false)
    {
        try
        {
            await _resources.ShowTitle();
            if (_quitting) return;
            if (!keepBoot) Change(AppState.Title);
            PublishStateChanges();
            await _resources.FinishLoading();
            _resources.ReleaseLoadingFlow();
        }
        finally { PublishStateChanges(); }
    }
    public Task Quit()
    {
        if (_quit is not null && (!_quit.IsCompleted || In(AppState.Quitting))) return _quit;
        // Publish before invoking any state event or service: Changed handlers and
        // a concurrent WM_CLOSE must observe the exact same pending operation.
        var done = new TaskCompletionSource(); _quit = done.Task;
        _ = RunQuit(done);
        return done.Task;
    }
    private async Task RunQuit(TaskCompletionSource done)
    {
        try { await QuitCore(); done.TrySetResult(); }
        catch (Exception error) { done.TrySetException(error); }
    }
    private async Task QuitCore()
    {
        var previous = State; _quitting = true; _resources.HoldInput();
        _startup.Cancel();
        if (restore.RestoreFaulted)
        {
            // Save returns Deferred for a faulted restore: intentionally preserve the old generation.
            await transit.CancelAsync();
            await QuitFaultedWorld(); await appExit.FinishQuit();
            return;
        }
        var save = previous switch
        {
            AppState.Boot => false,
            AppState.Title => true,
            AppState.Entering or AppState.Playing => true,
            AppState.Leaving => false,
            AppState.Quitting => false,
            _ => throw new InvalidOperationException("Unknown application state."),
        };
        if (previous == AppState.Entering)
        {
            if (_entry is not null) save = await _entry == EnterResult.Entered;
        }
        if (previous == AppState.Leaving && _leaving is not null)
        {
            await _leaving;
            save = true;
        }
        await transit.CancelAsync();
        // ToTitle may have returned after detaching its immutable snapshot while disk I/O continues.
        save |= saves.Busy;
        Change(AppState.Quitting);
        PublishStateChanges();
        if (restore.RestoreFaulted)
        {
            await QuitFaultedWorld(); await appExit.FinishQuit();
            return;
        }
        if (save)
            await FlushBeforeLeaving(SaveReason.Quit, FlowTuning.SaveTimeoutSeconds);
        // The stage owns resource disposal and engine shutdown. It must never
        // close merely because Changed announced the start of Quitting.
        await appExit.FinishQuit();
    }
    internal Exception? Failure => _recovery.Failure;
    public string? LastError => _recovery.LastError;
    private bool In(AppState expected) => State switch { AppState.Boot or AppState.Title or AppState.Entering or AppState.Playing or AppState.Leaving or AppState.Quitting => State == expected, _ => throw new InvalidOperationException("Unknown application state.") };
    private void Change(AppState state)
    {
        if (state is AppState.Playing) _continuation.RequireHealthyWorld();
        if (State == state) return;
        var old = State;
        if (old is AppState.Title) _continuation.StopTitlePrefetch();
        State = state; Rev++;
        _continuation.OnStateChange(state);
        StateAdvancing?.Invoke(old, state);
        _stateNotifications.Enqueue((old, state));
    }
    private void PublishStateChanges()
    {
        while (_stateNotifications.TryDequeue(out var state))
            _commands.Publish(Changed, state.Previous, state.Current);
    }
    private void MarkPhase(string phase) => _commands.Publish(PhaseReached, phase);

    public Task MapReady => _startup.MapReady;
    public Task BackgroundReady => _startup.BackgroundReady;
    public bool NewGamePreloadReady => _continuation.NewGamePreloadReady;
    public string Id => "app-flow";
    public int Order => 100;
    public void FailBoot(Exception error) => _startup.Fail(error);
    public Task Initialize(BootPhase phase, BootContext context) => _startup.Initialize(phase, context);
    public Task<MapId> PrepareBoot(BootFlowRequest request)
    {
        if (!In(AppState.Boot)) throw new InvalidOperationException("Boot preparation is only accepted during Boot.");
        return Task.FromResult(_startup.Select(request));
    }
    public Task PrepareBackground(Task mapPreparation, Task globalPreparation) => _startup.PrepareBackground(mapPreparation, globalPreparation);
    public Task OpenTitle()
    {
        if (!In(AppState.Boot) || !_startup.ShowTitle)
            throw new InvalidOperationException("Title can open only after title boot preparation.");
        _resources.HoldTitleFlow();
        Change(AppState.Title);
        MarkPhase("title-ready");
        PublishStateChanges();
        return Task.CompletedTask;
    }
    private async Task QuitFaultedWorld()
    {
        await _recovery.HoldRestoreFailure(); Change(AppState.Quitting);
        PublishStateChanges();
        await FlushBeforeLeaving(SaveReason.Quit, FlowTuning.SaveTimeoutSeconds);
        _recovery.RetainRestoreError();
    }
    private async Task FlushBeforeLeaving(SaveReason reason, double timeoutSeconds)
    {
        try
        {
            // SYS-SAVE-FLOW: failures and bounded save waits never veto leaving.
            // Save owns detached writes and publishes their eventual result through State/SlotsChanged.
            // FinishQuit still waits for the real resource drain after this bounded save attempt.
            await saves.FlushForQuit(reason, TimeSpan.FromSeconds(timeoutSeconds));
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            _recovery.CaptureFailure(error, State);
        }
    }
    public void Dispose()
    {
        var failures = new List<Exception>();
        _startup.ObserveReadinessCompletion();
        try { _continuation.Dispose(); } catch (Exception error) { failures.Add(error); }
        try { _resources.Dispose(); } catch (Exception error) { failures.Add(error); }
        if (_commands.TakeFailure() is { } notification) failures.Add(notification);
        if (failures.Count > 0) throw new AggregateException("Application flow cleanup failed.", failures);
    }
}
