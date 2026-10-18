using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Cinematic;
using Campus.Contracts.Maps;
using Campus.Contracts.Save;

namespace Campus.Flow;

public sealed partial class AppFlowService(ICameraRig camera, IVeilService veil, ISaveService saves,
    IMapService maps, IWorldState world, IWorldControl control, IRuleset rules,
    IMotionSettings motion, IViewControl view, ICinematicPlayback playback,
    IWorldRestore restore, IAppExit appExit, IMapRuntime mapRuntime,
    ICinematicSettings? settings = null) : IAppFlow
{
    internal Action<string>? DiagnosticSink { get; init; }
    private IDisposable? _input;
    private Task<EnterResult>? _entry;
    private Task? _skip;
    private Task? _quit;
    private Task? _leaving;
    private bool _leaveSaved;
    private Pose _leavePose;
    private IMapArrival? _titleArrival;
    private bool TitlePrepared => _titleArrival is { } arrival &&
        ReferenceEquals(mapRuntime.Arrival, arrival) && arrival.Done.IsCompletedSuccessfully;
    private SlotId? _preview;
    private bool _livePreview;
    private bool _previewNeedsVeil;
    private Pose _destination;
    private ViewMode _destinationMode;
    // WP-06's default until the settings-owned camera preference contract is available.
    private readonly float _userFov = FlowTuning.DefaultUserFov;
    private double? _flowTarget;
    private bool _newTimeline;
    private bool _finishing;
    private bool _quitting;
    private bool _skipped;
    private EnterRequest? _request;
    private CinematicDef? _definition;
    private CinematicPlaybackStart? _playbackStart;
    private EnterPath? _path;
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
    public EnterPath? Path => State switch
    {
        AppState.Entering => _path,
        AppState.Boot or AppState.Title or AppState.Playing or AppState.Leaving or AppState.Quitting => null,
        _ => throw new InvalidOperationException("Unknown application state."),
    };
    public bool Skippable => State switch
    {
        AppState.Entering => !_finishing && !_quitting && _path is EnterPath.Cinematic or EnterPath.ShortCinematic,
        AppState.Boot or AppState.Title or AppState.Playing or AppState.Leaving or AppState.Quitting => false,
        _ => throw new InvalidOperationException("Unknown application state."),
    };

    public Task<EnterResult> Enter(EnterRequest request)
    {
        if (!In(AppState.Title) || maps.Context is null && !restore.RestoreFaulted) return Task.FromResult(EnterResult.Rejected);
        var slot = request.Kind switch
        {
            EnterKind.NewGame => request.Slot,
            EnterKind.Continue => request.Slot ?? saves.Continue,
            EnterKind.LoadSlot => request.Slot,
            _ => throw new ArgumentOutOfRangeException(nameof(request)),
        };
        if (slot is null || !saves.Slots.Any(s => s.Slot == slot)) return Task.FromResult(EnterResult.Rejected);
        if (request.Kind is EnterKind.Continue or EnterKind.LoadSlot && !Readable(Info(slot.Value))) return Task.FromResult(EnterResult.Rejected);
        _request = request with { Slot = slot }; _finishing = false; _quitting = false; _skipped = false;
        LastError = null;
        _faultHold = null;
        _newTimeline = false; _flowTarget = null; _skip = null;
        _definition = null; _playbackStart = null; _path = null;
        _resumeEndpoint = null; _resumeRequested = false;
        HoldInput(); Change(AppState.Entering);
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
            if (restore.RestoreFaulted) return await RecoverEntry(newGame);
            HoldCinematicFlow();
            var context = Context(); var catalog = new CinematicCatalog(context);
            var meta = newGame ? null : Info(_request.Slot!.Value)?.Meta;
            _destination = meta?.Pose ?? maps.ResolveSpawn(context.Descriptor.Id).Fly;
            _destinationMode = meta?.Mode ?? rules.Current.DefaultViewMode;
            var mode = _boot?.CinematicMode ?? settings?.Mode ?? CinematicMode.Full;
            var role = mode switch
            {
                CinematicMode.Full => newGame ? "enterNew" : "enterContinue",
                CinematicMode.Short => newGame ? "enterNewShort" : "enterContinue",
                CinematicMode.Off => newGame ? "enterNew" : "enterContinue",
                _ => throw new InvalidOperationException("Unknown cinematic setting."),
            };
            _definition = _cinematicOverride is { } forced ? catalog.Read(forced) : catalog.Find(role);
            _cinematicOverride = null;
            _playbackStart = new(TitlePose(), camera.Pose, _destination, _userFov, context.Descriptor.Id);
            if (!newGame && _definition is not null) _destination = playback.ResolvePose(_definition.End.Pose, _playbackStart, maps);
            _path = mode switch { CinematicMode.Full or CinematicMode.Off => EnterPath.Cinematic, CinematicMode.Short => EnterPath.ShortCinematic, _ => throw new InvalidOperationException("Unknown cinematic setting.") };
            if (_definition?.Kind is CinematicKind.EnterNewShort) _path = EnterPath.ShortCinematic;
            var direct = mode is CinematicMode.Off || motion.ReduceMotion || _definition is null || !rules.Current.CinematicStates.Contains(AppState.Entering)
                || meta?.Space?.Building is not null || meta is not null && meta.Map != maps.Current || !newGame && _previewNeedsVeil;
            if (!newGame && context.World.Spatial.MediumAt(_destination.Position).Kind is MediumKind.Interior or MediumKind.Underground or MediumKind.Underwater) direct = true;
            if (direct)
            {
                _path = EnterPath.Veil; _skipped = true;
                await CoverFlow(motion.ReduceMotion ? FlowTuning.ReducedMotionSeconds : FlowTuning.CoverSeconds);
                if (newGame) await StartNewTimeline(); else await RestoreRequested();
                if (newGame) await AdvanceToDefault();
                _finishing = true;
            }
            else
            {
                await playback.Play(_definition!, _playbackStart, maps, context.World, State, ExecuteWorldCue);
                _finishing = true;
                if (_skip is not null) await _skip;
            }
            await FinishEntry(newGame, direct || _skipped);
            return EnterResult.Entered;
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            CaptureFailure(error);
            if (restore.RestoreFaulted) { await HoldRestoreFailure(); return EnterResult.Failed; }
            HoldInput(); HoldTitleFlow(); HoldCinematicFlow();
            await CoverFlow(0);
            playback.Clear();
            if (maps.Context is not null) camera.SetPose(TitlePose());
            if (!_quitting)
            {
                Change(AppState.Title);
                // The title menu can recover above the cover. Readiness for the
                // failed destination cannot authorize revealing the T0 camera.
            }
            return EnterResult.Failed;
        }
        finally { _finishing = false; }
    }
    private async Task ExecuteWorldCue(WorldCue cue, bool skipping)
    {
        switch (cue.Cue)
        {
            case WorldCueKind.NewTimeline: await StartNewTimeline(); break;
            case WorldCueKind.RestoreSlot: await RestoreRequested(); break;
            case WorldCueKind.FlowTo:
                _flowTarget = cue.Clock ?? throw new InvalidDataException("Flow cue has no clock.");
                if (!AtClock(_flowTarget.Value)) CinematicFlow.FlowTo(_flowTarget.Value, skipping ? FlowTuning.SkipFlowRate : cue.Rate!.Value);
                if (skipping) await FinishFlow(true);
                break;
            case WorldCueKind.SetFlow:
                if (skipping) await FinishFlow(true);
                var rate = cue.Rate ?? throw new InvalidDataException("Flow cue has no rate.");
                if (_definition is not null && cue.T < _definition.Duration) CinematicFlow.SetRate(rate);
                break;
            default: throw new InvalidDataException("Unknown world cue.");
        }
    }
    private async Task StartNewTimeline()
    {
        if (_newTimeline) return;
        await playback.WaitForPresentedCoverage();
        // Save owns the destination descriptor and the entire restore transaction,
        // including recovery when the previous map Context has already unloaded.
        var result = await saves.NewGame(_request!.Slot!.Value, seed: _boot?.Seed);
        RequireLoaded(result); _newTimeline = true; _preview = null; _livePreview = false;
        RequireHealthyWorld();
        HoldCinematicFlow();
        // NewGame restores the view default as well (D-242): walking on the spawn's stand is the landing.
        // The cinematic still owns the camera until handoff.
        _destination = camera.Pose; _destinationMode = camera.Mode;
        if (_playbackStart is not null) camera.SetPose(_playbackStart.Current);
    }
    private async Task RestoreRequested()
    {
        var slot = _request!.Slot!.Value;
        await playback.WaitForPresentedCoverage();
        if (restore.RestoreFaulted || _preview != slot || (!saves.HasPendingPose && !_livePreview))
        {
            using var readinessWait = veil.ObserveReadinessWait();
            var result = await saves.Load(slot, LoadScope.Preview); RequireLoaded(result);
            _preview = slot; _livePreview = false;
            _previewNeedsVeil = result.Repairs.Count > 0;
            if (_previewNeedsVeil && playback.IsPlaying) Skip();
        }
        RequireHealthyWorld();
        HoldCinematicFlow();
        _destination = Info(slot)?.Meta?.Pose ?? _destination;
        // Preview restores World/Content only. Pose stays pending throughout the
        // authored descent and is completed at the final, covered handoff.
        _resumeRequested = true;
    }
    private async Task AdvanceToDefault()
    {
        _flowTarget = Context().Descriptor.World.Default.Clock;
        await FinishFlow(true);
    }
    private async Task FinishFlow(bool fast)
    {
        RequireHealthyWorld();
        if (_flowTarget is not { } target || AtClock(target)) return;
        if (fast) CinematicFlow.FlowTo(target, FlowTuning.SkipFlowRate);
        await playback.WaitUntil(() => AtClock(target), FlowTuning.WorldEndpointTimeoutSeconds, "World timeline did not reach its endpoint.");
    }
    private bool AtClock(double target) => Math.Abs(world.Clock - target) <= FlowTuning.ClockTolerance;
    private async Task FinishEntry(bool newGame, bool underVeil)
    {
        RequireHealthyWorld();
        if (newGame && !_newTimeline) throw new InvalidDataException("New-game timeline did not initialize the world.");
        await FinishFlow(_skipped);
        if (!newGame)
        {
            if (!_resumeRequested) throw new InvalidOperationException("Continue timeline did not request its saved world.");
            await CoverFlow(underVeil ? 0 : FlowTuning.CoverSeconds);
            underVeil = true;
            await PrepareResumeEndpoint();
            RequirePreparedResume();
        }
        else
        {
            // The endpoint's actual destination must be warm before its first
            // playable draw, including a skip or a missing cinematic file.
            await CoverFlow(underVeil ? 0 : FlowTuning.CoverSeconds);
            underVeil = true;
        }
        camera.SetPose(_destination);
        if (newGame) await PreparePlayableDestination();
        RequireHealthyWorld();
        playback.Clear();
        CommitEntryFlow(newGame);
        if (_quitting) { ReleaseFlow(); return; }
        if (underVeil) await RevealFlow();
        RequireHealthyWorld();
        ReleaseFlow();
        Change(AppState.Playing); ReleaseInput();
        if (newGame) await saves.SaveNow(SaveReason.NewGame);
    }
    public void Skip()
    {
        if (!Skippable || _skip is not null) return;
        _skipped = true; _finishing = true;
        _skip = SkipCore();
    }
    private async Task SkipCore()
    {
        playback.Pause();
        try
        {
            await CoverFlow(FlowTuning.CoverSeconds);
            await playback.FinishWorld();
        }
        catch (Exception error) when (error is not OutOfMemoryException) { playback.Abort(error); }
    }
    public Task ReturnToTitle()
    {
        if (restore.RestoreFaulted) return HoldRestoreFailure();
        if (!In(AppState.Playing)) return Task.CompletedTask;
        _leaveSaved = false; _titleArrival = null; _leavePose = camera.Pose; HoldInput(); Change(AppState.Leaving);
        _leaving = ReturnToTitleCore(); return _leaving;
    }
    private async Task ReturnToTitleCore()
    {
        try { await LeaveCore(); }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            CaptureFailure(error);
            if (restore.RestoreFaulted) { await HoldRestoreFailure(); return; }
            await CoverFlow(0);
            playback.Clear();
            if (!_leaveSaved)
            {
                camera.SetPose(_leavePose); ReleaseFlow();
                if (!_quitting) { Change(AppState.Playing); ReleaseInput(); }
            }
            else
            {
                if (maps.Context is not null) camera.SetPose(TitlePose());
                HoldTitleFlow(); ReleaseCinematicFlow();
                if (!_quitting) Change(AppState.Title);
            }
            if (!_quitting && (!_leaveSaved || TitlePrepared) && mapRuntime.Arrival?.Done.IsCompletedSuccessfully == true)
                await RevealFlow();
        }
    }
    private async Task LeaveCore()
    {
        var saved = await saves.FlushForQuit(SaveReason.ToTitle, TimeSpan.FromSeconds(FlowTuning.SaveTimeoutSeconds));
        if (restore.RestoreFaulted) { await HoldRestoreFailure(); return; }
        if (!CanLeave(saved.Status)) { Change(AppState.Playing); ReleaseInput(); return; }
        _leaveSaved = true;
        _preview = saves.Active; _livePreview = true; _previewNeedsVeil = false;
        HoldTitleFlow(); HoldCinematicFlow();
        var context = Context(); var catalog = new CinematicCatalog(context);
        var definition = _cinematicOverride is { } forced ? catalog.Read(forced) : catalog.Find("leave");
        _cinematicOverride = null;
        if (definition is not null && !motion.ReduceMotion && !_quitting && rules.Current.CinematicStates.Contains(AppState.Leaving))
        {
            var start = new CinematicPlaybackStart(TitlePose(), camera.Pose, TitlePose(), _userFov, context.Descriptor.Id);
            await playback.Play(definition, start, maps, context.World, State, ExecuteLeaveCue);
            await CoverFlow(0);
            playback.Clear();
        }
        else await CoverFlow(FlowTuning.CoverSeconds);
        if (!TitlePrepared) await PrepareTitleDestination();
        camera.SetPose(TitlePose()); ReleaseCinematicFlow();
        // This is the existing world instance: do not reload it just to display T0.
        // Quit from Title never flushes; the playable pose remains in the saved metadata.
        if (!_quitting)
        {
            Change(AppState.Title);
            await RevealFlow();
        }
    }
    private async Task ExecuteLeaveCue(WorldCue cue, bool skipping)
    {
        switch (cue.Cue)
        {
            case WorldCueKind.SetFlow:
                CinematicFlow.SetRate(cue.Rate!.Value);
                if (cue.Rate == 0) await PrepareTitleDestination();
                break;
            case WorldCueKind.FlowTo: CinematicFlow.FlowTo(cue.Clock!.Value, cue.Rate!.Value); break;
            case WorldCueKind.NewTimeline: case WorldCueKind.RestoreSlot: throw new InvalidDataException("A return-to-title timeline cannot replace the world.");
            default: throw new InvalidDataException("Unknown world cue.");
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
            _skipped = true; _finishing = true;
            _skip ??= SkipCore(); await _skip;
            if (_entry is not null) save = await _entry == EnterResult.Entered;
        }
        if (previous == AppState.Leaving && _leaving is not null)
        {
            if (playback.IsPlaying) { await CoverFlow(0); await playback.FinishWorld(); }
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
                _quitting = false; Change(AppState.Playing); ReleaseInput();
                await RevealFlow();
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
    private void HoldInput() => _input ??= camera.LockInput(InputLockReason.Cinematic);
    private void ReleaseInput() { if (restore.RestoreFaulted) return; _input?.Dispose(); _input = null; }
    private void Change(AppState state) { if (state is AppState.Playing) RequireHealthyWorld(); if (State == state) return; var old = State; State = state; Rev++; Changed?.Invoke(old, state); }
}
