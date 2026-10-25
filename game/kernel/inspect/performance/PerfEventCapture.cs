using Campus.Contracts;
using Campus.Contracts.Perf;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Maps;
using Campus.Contracts.Quality;
using Campus.Contracts.Save;
using Campus.Contracts.Bake;
using Campus.Kernel.Frame;
using Godot;
using KernelServices = Campus.Kernel.Registry.Kernel;

namespace Campus.Kernel.Inspect;

/// <summary>Reads owner state on the main thread; no input interception or inferred command acceptance.</summary>
internal sealed class PerfEventCapture : IDisposable
{
    private readonly PerfRecordingSession _session;
    private readonly PerfFieldDictionary _dictionary;
    private readonly PerfValueChannel _samples;
    private readonly IAppFlow _app = KernelServices.Get<IAppFlow>();
    private readonly IMapService _maps = KernelServices.Get<IMapService>();
    private readonly IMapRuntime _runtime = KernelServices.Get<IMapRuntime>();
    private readonly IWorldState _world = KernelServices.Get<IWorldState>();
    private readonly IObserverState _observer = KernelServices.Get<IObserverState>();
    private readonly ICameraRig _camera = KernelServices.Get<ICameraRig>();
    private readonly IWalkController _walk = KernelServices.Get<IWalkController>();
    private readonly IMediumState _medium = KernelServices.Get<IMediumState>();
    private readonly IUiSurfaceState _ui = KernelServices.Get<IUiSurfaceState>();
    private readonly IQualityService _quality = KernelServices.Get<IQualityService>();
    private readonly ISaveService _save = KernelServices.Get<ISaveService>();
    private readonly IAppLoadingPresentation _loading = KernelServices.Get<IAppLoadingPresentation>();
    private readonly IStreamStatistics _stream = KernelServices.Get<IStreamStatistics>();
    private readonly IBakeService _bake = KernelServices.Get<IBakeService>();
    private readonly Viewport _viewport;
    private readonly PerfContextCapture _context;
    private readonly PerfSaveCommandQueue _saveCommands = new();
    private IMapArrival? _arrival;
    private bool _mapBound, _arrivalFinished, _disposed;
    private double _nextSample;
    private Vector2I _windowSize;
    private AppActivity _activity;
    private ViewMode _mode;
    private NavSpace? _space;
    private MediumKind _mediumKind;
    private WorldTransition _transition;
    private Season _season;
    private WeatherSel _weather;
    private Campus.Contracts.Flow _flow;
    private ITransitHandle? _transit;
    private bool _transitFinished;
    private bool? _focus;
    private bool _loadingActive, _loadingCovered;
    private float _loadingProgress;
    private BakeCacheStatistics? _cacheStatistics;
    private DisplayServer.WindowMode _windowMode;
    private float _refresh;
    private long _checkedFrames, _missingPhases, _missingTiming;

    public PerfEventCapture(PerfRecordingSession session, PerfFieldDictionary dictionary,
        PerfValueChannel samples, Viewport viewport)
    {
        _session = session;
        _dictionary = dictionary;
        _samples = samples;
        _viewport = viewport;
        _context = new(dictionary);
        _mode = _observer.Mode; _space = _observer.Space; _mediumKind = _medium.Current.Kind;
        _activity = _app.Activity; _transition = _world.Transition; _season = _world.SeasonTarget;
        _weather = _world.Weather; _flow = _world.Flow;
        _loadingActive = _loading.Active; _loadingCovered = _loading.IsCovered;
        _loadingProgress = _loading.Progress;
        _windowMode = DisplayServer.WindowGetMode();
        _refresh = DisplayServer.ScreenGetRefreshRate(DisplayServer.WindowGetCurrentScreen());
    }

    public void Start()
    {
        _app.Changed += OnAppState;
        _app.PhaseReached += OnAppPhase;
        _app.EnterCommand += OnEnterCommand;
        _maps.PrepareCommand += OnPrepareCommand;
        _quality.ApplyCommand += OnApplyCommand;
        _save.LoadCommand += OnSaveCommand;
        _world.InstanceChanged += OnWorldInstance;
        _ui.Changed += OnUi;
        _quality.Changed += OnQuality;
        _save.SlotsChanged += OnSave;
        _viewport.SizeChanged += OnSize;
        // The stream owner is application-lived; keep old-revision retirement facts through map drain.
        _stream.TileChanged += OnStreamTileChanged;
        if (_maps.Context is not null) BindMap(initial: true);
        Emit("snapshot.initial", new
        {
            appState = _app.State.ToString(), activity = (int)_activity,
            map = _maps.Context?.Descriptor.Id.ToString(), worldInstance = _world.Instance,
            mode = _mode.ToString(), space = _space, medium = _mediumKind.ToString(),
            surfaces = (int)_ui.Open, settingsPage = _ui.SettingsPage,
            saveState = _save.State, saveBusy = _save.Busy,
            loadingActive = _loading.Active, loadingCovered = _loading.IsCovered,
            qualityRequested = PerfManifest.QualityValues(_quality, true), qualityEffective = PerfManifest.QualityValues(_quality, false),
            requiredCoverage = _context.RequiredCoverage,
            notAttached = _context.NotAttached,
            commandIntervalStart = "unknownBeforeRecording; existing operations are not replayed or assigned an operation number"
        });
        CaptureWindowSize(initial: true);
        _windowMode = DisplayServer.WindowGetMode();
        Emit("window.mode", new { mode = _windowMode.ToString(), observation = "current" });
        Emit("loading.state", new { active = _loadingActive, covered = _loadingCovered, progress = _loadingProgress });
        CaptureCacheStatistics();
    }

    public void BindMap(bool initial = false)
    {
        if (_mapBound || _disposed) return;
        _mapBound = true;
        _runtime.ArrivalChanged += OnArrival;
        _arrival = _runtime.Arrival;
        if (_arrival is not null) _arrival.ProgressChanged += OnArrivalProgress;
        _arrivalFinished = _arrival?.Done.IsCompleted ?? false;
        if (_arrival is { } arrival)
            Emit("map.arrival", new { phase = "current", destination = arrival.Destination,
                result = arrival.Done.IsCompleted ? Completion(arrival.Done) : "waiting",
                completed = arrival.Completed, total = arrival.Total });
        Emit("map.travel", new { phase = initial ? "current" : "loaded", map = _maps.Context?.Descriptor.Id.ToString(), requestedMap = (string?)null });
    }

    public void UnbindMap()
    {
        if (!_mapBound) return;
        try
        {
            CheckArrival();
            if (_arrival is not null && !_arrivalFinished)
                EmitArrival("detached", "mapUnloading");
            Emit("map.travel", new { phase = "unloading", map = _maps.Current.ToString(), requestedMap = (string?)null });
        }
        finally
        {
            _runtime.ArrivalChanged -= OnArrival;
            if (_arrival is not null) _arrival.ProgressChanged -= OnArrivalProgress;
            _arrival = null;
            _mapBound = false;
        }
    }

    public long OnFrame(in FrameSample frame)
    {
        DrainSaveCommands();
        CheckOwnerChanges(in frame);
        CheckArrival();
        CaptureCacheStatistics();
        _checkedFrames++;
        if (frame.Phases.Validity != FramePhaseValidity.All) _missingPhases++;
        if (frame.Context.WindowCanDraw && frame.MainViewport.DrewThisFrame != false &&
            (frame.MainViewport.CpuRenderMilliseconds is null || frame.MainViewport.GpuRenderMilliseconds is null)) _missingTiming++;
        if (frame.Gc.Gen0Collections != 0 || frame.Gc.Gen1Collections != 0 || frame.Gc.Gen2Collections != 0 || frame.Gc.PauseMilliseconds > 0)
            Emit("gc.detail", new { pauseMs = frame.Gc.PauseMilliseconds, gen0 = frame.Gc.Gen0Collections,
                gen1 = frame.Gc.Gen1Collections, gen2 = frame.Gc.Gen2Collections,
                allocatedBytes = frame.Gc.AllocatedBytes, managedHeapMiB = frame.Gc.ManagedHeapBytes / 1048576d }, frame.CollectionFrame, frame.MonotonicMilliseconds);
        return _context.Capture(in frame, _loading.Active, _world.Transition, _arrival is { Done.IsCompleted: false });
    }

    // Only housekeeping allocates; the ordinary SamplePublished path compares value snapshots.
    public void Poll()
    {
        double now = FrameClock.MonotonicMilliseconds;
        DrainSaveCommands();
        if (now < _nextSample) return;
        _nextSample = now + _dictionary.SampleIntervalMilliseconds;
        float refresh = DisplayServer.ScreenGetRefreshRate(DisplayServer.WindowGetCurrentScreen());
        if (_refresh != refresh) { _refresh = refresh; Emit("display.refresh", new { refreshHz = refresh > 0 ? (float?)refresh : null }); }
        bool live = _world.Instance != 0 && _maps.Context is not null;
        var p = _observer.Feet;
        var values = new Dictionary<string, object?>
        {
            ["xM"] = live ? p.X : null, ["yM"] = live ? p.Y : null, ["zM"] = live ? p.Z : null,
            ["speedMps"] = live && _observer.Mode == ViewMode.Walk ? _walk.Velocity.Length() : null,
            ["mode"] = live ? _observer.Mode.ToString() : null, ["medium"] = live ? _medium.Current.Kind.ToString() : null,
            ["building"] = live ? _observer.Space?.Building?.ToString() : null,
            ["floor"] = live && _observer.Space is { } space ? space.Floor.ToString(System.Globalization.CultureInfo.InvariantCulture) : null,
            ["appState"] = _app.State.ToString(), ["map"] = live ? _maps.Current.ToString() : null,
            ["worldHours"] = live ? _world.WorldHours : null, ["clockHours"] = live ? _world.Clock : null,
            ["season"] = live ? _world.SeasonTarget.ToString() : null,
            ["seasonFraction"] = live ? _world.SeasonPos - Math.Floor(_world.SeasonPos) : null,
            ["weather"] = live ? _world.Weather.Kind.ToString() : null, ["weatherIntensity"] = live ? _world.Weather.Intensity : null,
            ["flowRate"] = live ? _world.Flow.Rate : null, ["worldTransition"] = live ? (int)_world.Transition : null,
            ["workingSetMiB"] = System.Environment.WorkingSet / 1048576d,
            ["managedHeapMiB"] = GC.GetGCMemoryInfo().HeapSizeBytes / 1048576d,
            ["godotStaticMiB"] = Monitor(Performance.Monitor.MemoryStatic) / 1048576d,
            ["engineVideoMemoryMiB"] = Monitor(Performance.Monitor.RenderVideoMemUsed) / 1048576d,
            ["textureMemoryMiB"] = Monitor(Performance.Monitor.RenderTextureMemUsed) / 1048576d,
            ["bufferMemoryMiB"] = Monitor(Performance.Monitor.RenderBufferMemUsed) / 1048576d,
            ["objectCount"] = Monitor(Performance.Monitor.ObjectCount), ["nodeCount"] = Monitor(Performance.Monitor.ObjectNodeCount),
            ["orphanNodeCount"] = Monitor(Performance.Monitor.ObjectOrphanNodeCount),
            ["resourceCount"] = Monitor(Performance.Monitor.ObjectResourceCount), ["processCpuFraction"] = _session.ProcessCpuFraction
        };
        _samples.TryRow(new("samples", Engine.GetProcessFrames(), now, values));
        _samples.Flush();
        if (_checkedFrames != 0) Emit("measurement.selfcheck", new
        {
            frames = _checkedFrames, missingPhases = _missingPhases, missingTiming = _missingTiming,
            phaseUnavailableFraction = (double)_missingPhases / _checkedFrames,
            timingUnavailableFraction = (double)_missingTiming / _checkedFrames
        });
        _checkedFrames = _missingPhases = _missingTiming = 0;
    }

    private static double? Monitor(Performance.Monitor monitor)
    {
        double value = Performance.GetMonitor(monitor);
        return double.IsFinite(value) && value >= 0 ? value : null;
    }

    private void CheckOwnerChanges(in FrameSample frame)
    {
        var mode = DisplayServer.WindowGetMode();
        if (_windowMode != mode) { _windowMode = mode; Emit("window.mode", new { mode = mode.ToString(), observation = "changed" }); }
        if (_loadingActive != _loading.Active || _loadingCovered != _loading.IsCovered || _loadingProgress != _loading.Progress)
        {
            _loadingActive = _loading.Active; _loadingCovered = _loading.IsCovered;
            _loadingProgress = _loading.Progress;
            Emit("loading.state", new { active = _loadingActive, covered = _loadingCovered, progress = _loadingProgress });
        }
        if (_activity != frame.Context.Activity) { _activity = frame.Context.Activity; Emit("app.activity", new { activity = (int)_activity }); }
        if (_mode != _observer.Mode) { _mode = _observer.Mode; _context.Settle("transition"); Emit("view.mode", new { mode = _mode.ToString(), observation = "ownerState" }); }
        if (_space != _observer.Space) { _space = _observer.Space; Emit("view.space", new { space = _space }); }
        if (_mediumKind != _medium.Current.Kind) { _mediumKind = _medium.Current.Kind; Emit("view.medium", new { medium = _mediumKind.ToString() }); }
        if (_season != _world.SeasonTarget) { _season = _world.SeasonTarget; Emit("world.season", new { season = _season.ToString(), observation = "ownerTarget" }); }
        if (_weather != _world.Weather) { _weather = _world.Weather; Emit("world.weather", new { weather = _weather }); }
        if (_flow != _world.Flow) { _flow = _world.Flow; Emit("world.flow", new { rate = _flow.Rate }); }
        if (_transition != _world.Transition)
        {
            var previous = _transition;
            _transition = _world.Transition;
            if (((previous ^ _transition) & (WorldTransition.Time | WorldTransition.Season)) != 0) _context.Settle("transition");
            Emit("world.transition", new { transition = (int)_transition, worldHours = _world.WorldHours, clockHours = _world.Clock });
        }
        if (!ReferenceEquals(_transit, _camera.ActiveTransit))
        {
            CheckTransit();
            if (_transit is not null && !_transitFinished) Emit("transit", new { phase = "detached", kind = _transit.Kind.ToString(), result = "ownerReplaced" });
            _transit = _camera.ActiveTransit; _transitFinished = false;
            if (_transit is not null) Emit("transit", new { phase = "observed", kind = _transit.Kind.ToString(), result = (string?)null });
        }
        CheckTransit();
        if (_focus != frame.Context.WindowFocused) { _focus = frame.Context.WindowFocused; Emit("window.focus", new { focused = _focus }); }
    }

    private void CheckTransit()
    {
        if (_transit is null || _transitFinished || !_transit.Done.IsCompleted) return;
        _transitFinished = true;
        _context.Settle("transition");
        Emit("transit", new { phase = "end", kind = _transit.Kind.ToString(), result = _transit.Done.IsCompletedSuccessfully
            ? _transit.Done.Result.ToString() : Completion(_transit.Done) });
    }
    private void CheckArrival()
    {
        if (_arrival is null || _arrivalFinished || !_arrival.Done.IsCompleted) return;
        _arrivalFinished = true;
        _context.Settle("transition");
        EmitArrival("end", Completion(_arrival.Done));
    }
    // The map/camera owner retains the original failure; recording observes its task terminal status only.
    private static string Completion(Task task) => task.IsCanceled ? "cancelled" : task.IsFaulted ? "failed" : "ready";
    private void OnArrival(IMapArrival? arrival)
    {
        try
        {
            CheckArrival();
            if (_arrival is not null)
            {
                _arrival.ProgressChanged -= OnArrivalProgress;
                if (!_arrivalFinished) EmitArrival("detached", "ownerReplaced");
            }
            _arrival = arrival; _arrivalFinished = false;
            if (arrival is not null)
            {
                arrival.ProgressChanged += OnArrivalProgress;
                EmitArrival("begin", null);
            }
            CheckArrival();
        }
        catch (Exception error) { _session.AddFailure(PerfFaultCode.ProducerFailed, error); }
    }
    private void OnArrivalProgress()
    {
        if (_disposed || _arrival is null) return;
        try { EmitArrival("progress", null); }
        catch (Exception error) { _session.AddFailure(PerfFaultCode.ProducerFailed, error); }
    }
    private void EmitArrival(string phase, string? result)
    {
        if (_arrival is not { } arrival) return;
        Emit("map.arrival", new { phase, destination = arrival.Destination, result,
            completed = arrival.Completed, total = arrival.Total });
    }
    private void OnStreamTileChanged(StreamTileChange change)
    {
        if (_disposed) return;
        try
        {
            Emit("stream.tileChanged", new { key = change.Key, kind = change.Kind.ToString(),
                revision = change.Revision, change = change.Change.ToString(), bytes = change.Bytes },
                Engine.GetProcessFrames(), change.MonotonicMilliseconds);
        }
        catch (Exception error) { _session.AddFailure(PerfFaultCode.ProducerFailed, error); }
    }
    private void CaptureCacheStatistics()
    {
        var current = _bake.CacheStatistics;
        if (_cacheStatistics == current) return;
        string observation = _cacheStatistics is null ? "current" : "changed";
        _cacheStatistics = current;
        Emit("bake.cache", new { hits = current.Hits, misses = current.Misses, rebuilds = current.Rebuilds, observation });
    }
    private void OnAppState(AppState from, AppState to)
    {
        try
        {
            Emit("app.state", new { from = from.ToString(), to = to.ToString() });
        }
        catch (Exception error) { _session.AddFailure(PerfFaultCode.ProducerFailed, error); }
    }
    private void OnAppPhase(string phase) => AppPhase(phase, null, null);
    internal void AppPhase(string phase, ulong? frame, double? time)
    {
        if (_disposed) return;
        try
        {
            Emit("app.phase", new { phase }, frame, time);
        }
        catch (Exception error) { _session.AddFailure(PerfFaultCode.ProducerFailed, error); }
    }
    private void OnWorldInstance(long instance)
    {
        try
        {
            Emit("world.instance", new { instance });
        }
        catch (Exception error) { _session.AddFailure(PerfFaultCode.ProducerFailed, error); }
    }
    private void OnUi()
    {
        try
        {
            Emit("ui.surface", new { surfaces = (int)_ui.Open, settingsPage = _ui.SettingsPage });
        }
        catch (Exception error) { _session.AddFailure(PerfFaultCode.ProducerFailed, error); }
    }
    private void OnQuality(QualityChange change)
    {
        try
        {
            Emit("settings.quality", new { change, requested = PerfManifest.QualityValues(_quality, true),
                effective = PerfManifest.QualityValues(_quality, false), observation = "ownerChanged" });
        }
        catch (Exception error) { _session.AddFailure(PerfFaultCode.ProducerFailed, error); }
    }
    private void OnSave()
    {
        try
        {
            Emit("save.write", new { state = _save.State, busy = _save.Busy, active = _save.Active });
        }
        catch (Exception error) { _session.AddFailure(PerfFaultCode.ProducerFailed, error); }
    }
    private void OnEnterCommand(EnterCommandEvent value)
    {
        try
        {
            double time = FrameClock.MonotonicMilliseconds;
            bool startObserved = _context.Observe(in value, time);
            Emit("game.enter", new { owner = "IAppFlow", operation = value.Operation, phase = value.Phase,
                target = value.Target, result = value.Result, direct = value.Direct, errorKey = value.ErrorKey,
                startObserved }, time: time);
        }
        catch (Exception error) { _session.AddFailure(PerfFaultCode.ProducerFailed, error); }
    }
    private void OnPrepareCommand(MapPrepareEvent value)
    {
        try
        {
            double time = FrameClock.MonotonicMilliseconds;
            bool startObserved = _context.Observe(in value, time);
            Emit("map.prepare", new { owner = "IMapService", operation = value.Operation, phase = value.Phase,
                from = value.From, to = value.To, result = value.Result, startObserved }, time: time);
        }
        catch (Exception error) { _session.AddFailure(PerfFaultCode.ProducerFailed, error); }
    }
    private void OnApplyCommand(QualityApplyEvent value)
    {
        try
        {
            double time = FrameClock.MonotonicMilliseconds;
            bool startObserved = _context.Observe(in value, time);
            Emit("settings.apply", new { owner = "IQualityService", operation = value.Operation, phase = value.Phase,
                target = value.Target, origin = value.Origin, result = value.Result, startObserved }, time: time);
        }
        catch (Exception error) { _session.AddFailure(PerfFaultCode.ProducerFailed, error); }
    }
    // SaveLoading may resume off-thread: only timestamp and enqueue detached contract values here.
    private void OnSaveCommand(SaveLoadEvent value)
    {
        try
        {
            _saveCommands.Enqueue(value);
        }
        catch (Exception error) { _session.AddFailure(PerfFaultCode.ProducerFailed, error); }
    }
    private void DrainSaveCommands()
    {
        while (_saveCommands.TryDequeue(out var captured))
        {
            var value = captured.Value;
            bool startObserved = _context.Observe(in value, captured.Time);
            // A worker has no collection frame. Do not replace null with the drain's current frame.
            _session.TryEvent(new("save.load", captured.Frame, captured.Time, new
            {
                owner = "ISaveService", operation = value.Operation, phase = value.Phase,
                kind = value.Kind, requestedSlot = value.Slot, requestedGeneration = value.RequestedGeneration,
                file = value.File, result = value.Result, startObserved
            }));
        }
    }
    private void OnSize() => CaptureWindowSize(initial: false);

    private void CaptureWindowSize(bool initial)
    {
        try
        {
            var size = DisplayServer.WindowGetSize();
            if (!initial && size == _windowSize) return;
            _windowSize = size;
            Emit("window.size", new { widthPx = size.X, heightPx = size.Y, observation = initial ? "current" : "changed" });
        }
        catch (Exception error) { _session.AddFailure(PerfFaultCode.ProducerFailed, error); }
    }
    public void Readback(string phase, string purpose, ulong frame)
    {
        _context.Readback(purpose, frame);
        Emit("sync.readback", new { phase, purpose }, frame);
    }
    private void Emit(string name, object payload, ulong? frame = null, double? time = null) =>
        _session.TryEvent(new(name, frame ?? Engine.GetProcessFrames(), time ?? FrameClock.MonotonicMilliseconds, payload));

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try
        {
            try { CaptureCacheStatistics(); }
            catch (Exception error) { _session.AddFailure(PerfFaultCode.ProducerFailed, error); }
            UnbindMap();
            CheckTransit();
            if (_transit is not null && !_transitFinished)
                Emit("transit", new { phase = "detached", kind = _transit.Kind.ToString(), result = "recordingStopped" });
        }
        finally
        {
            _app.Changed -= OnAppState; _app.PhaseReached -= OnAppPhase;
            _stream.TileChanged -= OnStreamTileChanged;
            _app.EnterCommand -= OnEnterCommand; _maps.PrepareCommand -= OnPrepareCommand;
            _quality.ApplyCommand -= OnApplyCommand; _save.LoadCommand -= OnSaveCommand;
            _saveCommands.Stop();
            _world.InstanceChanged -= OnWorldInstance; _ui.Changed -= OnUi;
            _quality.Changed -= OnQuality; _save.SlotsChanged -= OnSave; _viewport.SizeChanged -= OnSize;
            try { DrainSaveCommands(); }
            finally
            {
                _samples.Stop();
            }
        }
    }
}

/// <summary>Save callbacks may run on workers; a BCL value queue keeps them away from scene state and SingleWriter sinks.</summary>
internal sealed class PerfSaveCommandQueue
{
    private readonly object _gate = new();
    internal readonly record struct Captured(SaveLoadEvent Value, double Time, ulong? Frame);
    private readonly Queue<Captured> _values = new();
    private bool _stopped;
    public void Enqueue(SaveLoadEvent value)
    {
        double time = FrameClock.MonotonicMilliseconds;
        ulong? frame = OS.GetThreadCallerId() == OS.GetMainThreadId() ? Engine.GetProcessFrames() : null;
        lock (_gate)
            if (!_stopped) _values.Enqueue(new(value, time, frame));
    }
    public bool TryDequeue(out Captured value)
    {
        lock (_gate) return _values.TryDequeue(out value);
    }
    public void Stop() { lock (_gate) _stopped = true; }
}
