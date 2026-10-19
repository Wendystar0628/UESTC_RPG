using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Maps;
using Campus.Contracts.Save;
using Campus.Contracts.Settings;

namespace Campus.Flow;

public sealed partial class AppFlowService(ICameraRig camera, IAppLoadingPresentation loading, IAppTitlePresentation titlePresentation, ISaveService saves,
    IMapService maps, IWorldState world, IWorldControl control, IViewControl view,
    IWorldRestore restore, IAppExit appExit, IMapRuntime mapRuntime, IMapStreamState stream,
    ISettingsStore? profileSettings = null) : IAppFlow
{
    internal Action<string>? DiagnosticSink { get; init; }
    private IDisposable? _input;
    private Task<EnterResult>? _entry;
    private Task? _quit;
    private Task? _leaving;
    private bool _leaveSaved;
    private Pose _leavePose;
    private IMapArrival? _titleArrival;
    private bool TitlePrepared => _titleArrival is { } arrival &&
        ReferenceEquals(mapRuntime.Arrival, arrival) && arrival.Done.IsCompletedSuccessfully;
    private SlotId? _preview;
    private bool _livePreview;
    private Pose _destination;
    private ViewMode _destinationMode;
    private readonly ISettingsSection? _cameraSettings = profileSettings?.Section("ui");
    private float UserFov(ViewMode mode)
    {
        var definition = mode switch
        {
            ViewMode.Fly => UiSettingDefinitions.FlyFov,
            ViewMode.Walk => UiSettingDefinitions.WalkFov,
            _ => throw new ArgumentOutOfRangeException(nameof(mode))
        };
        return _captureMode ? definition.Default : _cameraSettings?.Get(definition) ?? definition.Default;
    }
    private bool _newTimeline;
    private bool _quitting;
    private EnterRequest? _request;
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
    public event Action<string>? PhaseReached;

    public Task<EnterResult> Enter(EnterRequest request)
    {
        if (!In(AppState.Title)) return Task.FromResult(EnterResult.Rejected);
        var slot = request.Kind switch
        {
            EnterKind.NewGame => request.Slot,
            EnterKind.Continue => request.Slot ?? saves.Continue,
            EnterKind.LoadSlot => request.Slot,
            _ => throw new ArgumentOutOfRangeException(nameof(request)),
        };
        if (slot is null || !saves.Slots.Any(s => s.Slot == slot)) return Task.FromResult(EnterResult.Rejected);
        if (request.Kind is EnterKind.Continue or EnterKind.LoadSlot && !Readable(Info(slot.Value))) return Task.FromResult(EnterResult.Rejected);
        _request = request with { Slot = slot }; _quitting = false;
        LastError = null; _faultHold = null; _newTimeline = false;
        _resumeEndpoint = null; _resumeRequested = false;
        HoldInput(); HoldLoadingFlow(); Change(AppState.Entering);
        if (request.Kind is EnterKind.NewGame) MarkPhase("new-game-enter");
        _entry = EnterCore(); return _entry;
    }
    private async Task<EnterResult> EnterCore()
    {
        try
        {
            var newGame = _request!.Kind switch
            {
                EnterKind.NewGame => true,
                EnterKind.Continue or EnterKind.LoadSlot => false,
                _ => throw new ArgumentOutOfRangeException(nameof(_request)),
            };
            await BeginLoading(_request, newGame);
            // A faulted world's NewGame transaction owns its recovery preparation.
            if (!newGame || !restore.RestoreFaulted) await WorldReady;
            if (newGame) await StartNewTimeline(); else await RestoreRequested();
            RequireHealthyWorld();
            if (!newGame) { await PrepareResumeEndpoint(); RequirePreparedResume(); }
            else await PreparePlayableDestination();
            // The real destination now owns residency; keep the pins through its terrain preparation.
            stream.SetPinnedDestinations([]);
            if (newGame)
            {
                // The first generation belongs to this prepared Walk endpoint.
                var saved = await saves.SaveNow(SaveReason.NewGame);
                if (!CanLeave(saved.Status)) throw new IOException(saved.Error ?? "The new game could not be saved.");
                CompleteSavePreparation();
            }
            RequireHealthyWorld();
            if (_quitting) { CompleteLoadingProgress(); ReleaseFlow(); return EnterResult.Entered; }
            await FinishLoading();
            RequireHealthyWorld();
            ReleaseFlow(); Change(AppState.Playing); ReleaseInput();
            return EnterResult.Entered;
        }
        catch (OperationCanceledException) when (_quitting) { return EnterResult.Rejected; }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            CaptureFailure(error);
            if (restore.RestoreFaulted) { await HoldRestoreFailure(); return EnterResult.Failed; }
            await RecoverTitle();
            return EnterResult.Failed;
        }
    }
    private async Task StartNewTimeline()
    {
        if (_newTimeline) return;
        // Save owns the complete restore transaction and keeps the existing same-map geometry.
        var result = await saves.NewGame(_request!.Slot!.Value, seed: _boot?.Seed);
        RequireLoaded(result); _newTimeline = true; _preview = null; _livePreview = false;
        RequireHealthyWorld(); HoldLoadingFlow();
        _destination = camera.Pose; _destinationMode = camera.Mode;
        CompleteWorldPreparation();
    }
    private async Task RestoreRequested()
    {
        var slot = _request!.Slot!.Value;
        if (restore.RestoreFaulted || _preview != slot || (!saves.HasPendingPose && !_livePreview))
        {
            var result = await saves.Load(slot, LoadScope.Preview); RequireLoaded(result);
            _preview = slot; _livePreview = false;
        }
        RequireHealthyWorld(); HoldLoadingFlow();
        _destination = Info(slot)?.Meta?.Pose ?? camera.Pose;
        _destinationMode = Info(slot)?.Meta?.Mode ?? camera.Mode;
        _resumeRequested = true;
        CompleteWorldPreparation();
    }
    public Task ReturnToTitle()
    {
        if (restore.RestoreFaulted) return HoldRestoreFailure();
        if (!In(AppState.Playing)) return Task.CompletedTask;
        _leaveSaved = false; _titleArrival = null; _leavePose = camera.Pose;
        HoldInput(); HoldLoadingFlow(); Change(AppState.Leaving);
        _leaving = ReturnToTitleCore(); return _leaving;
    }
    private async Task ReturnToTitleCore()
    {
        try
        {
            await BeginLoading(null, save: true);
            var saved = await saves.FlushForQuit(SaveReason.ToTitle, TimeSpan.FromSeconds(FlowTuning.SaveTimeoutSeconds));
            if (restore.RestoreFaulted) { await HoldRestoreFailure(); return; }
            if (!CanLeave(saved.Status))
            {
                CompleteWorldPreparation(); CompleteSavePreparation();
                await PreparePlayableDestination(); await FinishLoading();
                ReleaseFlow(); Change(AppState.Playing); ReleaseInput(); return;
            }
            _leaveSaved = true; CompleteSavePreparation();
            _preview = saves.Active; _livePreview = true;
            // Keep this instance and its saved playable pose; only the presented destination changes.
            HoldTitleFlow(); CompleteWorldPreparation();
            await PrepareTitleDestination();
            if (!_quitting) { await FinishLoading(); ReleaseLoadingFlow(); Change(AppState.Title); }
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            CaptureFailure(error);
            if (restore.RestoreFaulted) { await HoldRestoreFailure(); return; }
            if (!_leaveSaved)
            {
                camera.SetPose(_leavePose);
                await PreparePlayableDestination(); await FinishLoading(); ReleaseFlow();
                if (!_quitting) { Change(AppState.Playing); ReleaseInput(); }
            }
            else await RecoverTitle();
        }
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
        var previous = State; _quitting = true; HoldInput();
        _worldReady.TrySetCanceled();
        if (restore.RestoreFaulted)
        {
            // Save returns Deferred for a faulted restore: intentionally preserve the old generation.
            if (await QuitFaultedWorld()) await appExit.FinishQuit();
            return;
        }
        var save = previous switch
        {
            AppState.Boot or AppState.Title => false,
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
            save = !_leaveSaved;
        }
        Change(AppState.Quitting);
        if (restore.RestoreFaulted)
        {
            if (await QuitFaultedWorld()) await appExit.FinishQuit();
            return;
        }
        if (save)
        {
            var result = await saves.FlushForQuit(SaveReason.Quit, TimeSpan.FromSeconds(FlowTuning.SaveTimeoutSeconds));
            if (!CanLeave(result.Status))
            {
                if (restore.RestoreFaulted) { _quitting = false; await HoldRestoreFailure(); return; }
                _quitting = false;
                if (loading.Active) await FinishLoading();
                ReleaseFlow(); Change(AppState.Playing); ReleaseInput();
                return;
            }
        }
        // The stage owns resource disposal and engine shutdown. It must never
        // close merely because Changed announced the start of Quitting.
        await appExit.FinishQuit();
    }
    private IMapContext Context() => maps.Context ?? throw new InvalidOperationException("No map is loaded.");
    public string? LastError { get; private set; }
    private void CaptureFailure(Exception error)
    {
        LastError = error.Message;
        var details = $"[WP-49a][map:{maps.Current.Value}][flow:{State}] {error.ToString()}";
        // Plain fixture execution supplies a managed sink. Runtime keeps the same
        // full exception diagnostics without requiring tests to initialize Godot.
        if (DiagnosticSink is { } sink) { sink(details); return; }
        if (restore.RestoreFaulted) Godot.GD.Print(details);
        else Godot.GD.PushError(details);
    }
    private Pose TitlePose() => Context().Descriptor.Title?.T0 ?? maps.Maps.FirstOrDefault(m => m.Id == maps.Default)?.Title?.T0 ?? maps.ResolveSpawn(Context().Descriptor.Id).Fly;
    private SlotInfo? Info(SlotId slot) => saves.Slots.FirstOrDefault(s => s.Slot == slot);
    private static bool Readable(SlotInfo? slot) => slot?.Health switch
    {
        SlotHealth.Ok => slot.Meta is not null,
        SlotHealth.Empty or SlotHealth.Corrupt or null => false,
        _ => throw new InvalidOperationException("Unknown slot health."),
    };
    private static bool Loaded(LoadStatus status) => status switch { LoadStatus.Loaded => true, LoadStatus.Empty or LoadStatus.Corrupt or LoadStatus.Failed => false, _ => throw new ArgumentOutOfRangeException(nameof(status)) };
    private static void RequireLoaded(LoadResult result) { if (!Loaded(result.Status)) throw new InvalidDataException(result.Error ?? "The requested world could not be restored."); }
    private static bool CanLeave(SaveStatus status) => status switch { SaveStatus.Saved or SaveStatus.Deferred or SaveStatus.Disabled => true, SaveStatus.Failed => false, _ => throw new ArgumentOutOfRangeException(nameof(status)) };
    private bool In(AppState expected) => State switch { AppState.Boot or AppState.Title or AppState.Entering or AppState.Playing or AppState.Leaving or AppState.Quitting => State == expected, _ => throw new InvalidOperationException("Unknown application state.") };
    private void HoldInput() => _input ??= camera.LockInput(InputLockReason.AppFlow);
    private void ReleaseInput() { if (restore.RestoreFaulted) return; _input?.Dispose(); _input = null; }
    private void Change(AppState state)
    {
        if (state is AppState.Playing) RequireHealthyWorld();
        if (State == state) return;
        var old = State;
        if (old is AppState.Title) saves.SlotsChanged -= RefreshTitlePrefetch;
        State = state; Rev++;
        switch (state)
        {
            case AppState.Title:
                saves.SlotsChanged += RefreshTitlePrefetch;
                RefreshTitlePrefetch();
                break;
            case AppState.Entering: break;
            case AppState.Boot: case AppState.Playing: case AppState.Leaving: case AppState.Quitting:
                stream.SetPinnedDestinations([]);
                break;
            default: throw new ArgumentOutOfRangeException(nameof(state));
        }
        Changed?.Invoke(old, state);
    }
    private void RefreshTitlePrefetch()
    {
        if (State is not AppState.Title || restore.RestoreFaulted || maps.Context is null)
        { stream.SetPinnedDestinations([]); return; }
        var destinations = new List<MapStreamDestination>(2);
        if (maps.Default == maps.Current)
            destinations.Add(new(maps.ResolveSpawn(maps.Default).ViewPose(ViewMode.Walk), ViewMode.Walk));
        if (saves.Continue is { } slot && Info(slot) is { Health: SlotHealth.Ok, Meta: { } meta } && meta.Map == maps.Current)
            destinations.Add(new(meta.Pose, meta.Mode, meta.Space));
        stream.SetPinnedDestinations(destinations);
    }
}
