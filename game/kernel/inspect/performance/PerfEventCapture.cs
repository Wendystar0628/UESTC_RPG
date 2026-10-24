using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Maps;
using Campus.Contracts.Quality;
using Campus.Contracts.Save;
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
    private readonly Viewport _viewport;
    private readonly PerfContextCapture _context;
    private readonly PerfSaveCommandQueue _saveCommands = new();
    private IMapArrival? _arrival;
    private bool _mapBound, _arrivalFinished, _disposed;
    private double _nextSample, _lastCpuAt;
    private TimeSpan? _lastCpu;
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
        _context = new(dictionary, requiredOwnerBoundariesAttached: true);
        _mode = _observer.Mode; _space = _observer.Space; _mediumKind = _medium.Current.Kind;
        _activity = _app.Activity; _transition = _world.Transition; _season = _world.SeasonTarget;
        _weather = _world.Weather; _flow = _world.Flow;
        _loadingActive = _loading.Active; _loadingCovered = _loading.IsCovered;
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
        _save.LoadCommand += _saveCommands.Enqueue;
        _world.InstanceChanged += OnWorldInstance;
        _ui.Changed += OnUi;
        _quality.Changed += OnQuality;
        _save.SlotsChanged += OnSave;
        _viewport.SizeChanged += OnSize;
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
            requiredCoverage = _context.RequiredCoverage, requiredMissing = _context.RequiredMissing,
            notAttached = _context.NotAttached,
            commandIntervalStart = "unknownBeforeRecording; existing operations are not replayed or assigned an operation number"
        });
        OnSize();
    }

    public void BindMap(bool initial = false)
    {
        if (_mapBound || _disposed) return;
        _mapBound = true;
        _runtime.ArrivalChanged += OnArrival;
        _arrival = _runtime.Arrival;
        _arrivalFinished = _arrival?.Done.IsCompleted ?? false;
        if (_arrival is { } arrival)
            Emit("map.arrival", new { phase = "current", destination = arrival.Destination,
                result = arrival.Done.IsCompleted ? Completion(arrival.Done) : "waiting" });
        Emit("map.travel", new { phase = initial ? "current" : "loaded", map = _maps.Context?.Descriptor.Id.ToString(), requestedMap = (string?)null });
    }

    public void UnbindMap()
    {
        if (!_mapBound) return;
        try
        {
            CheckArrival();
            if (_arrival is not null && !_arrivalFinished)
                Emit("map.arrival", new { phase = "detached", destination = _arrival.Destination, result = "mapUnloading" });
            Emit("map.travel", new { phase = "unloading", map = _maps.Current.ToString(), requestedMap = (string?)null });
        }
        finally
        {
            _runtime.ArrivalChanged -= OnArrival;
            _arrival = null;
            _mapBound = false;
        }
    }

    public (long Flags, bool Complete) OnFrame(in FrameSample frame)
    {
        DrainSaveCommands(frame.CollectionFrame, frame.MonotonicMilliseconds);
        CheckOwnerChanges(in frame);
        CheckArrival();
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
        DrainSaveCommands(Engine.GetProcessFrames(), now);
        if (now < _nextSample) return;
        _nextSample = now + _dictionary.SampleIntervalMilliseconds;
        var cpu = System.Environment.CpuUsage.TotalTime;
        double? cpuFraction = _lastCpu is { } previous && now > _lastCpuAt
            ? Math.Clamp((cpu - previous).TotalMilliseconds / (now - _lastCpuAt) / System.Environment.ProcessorCount, 0, 1) : null;
        _lastCpu = cpu; _lastCpuAt = now;
        var mode = DisplayServer.WindowGetMode();
        if (_windowMode != mode) { _windowMode = mode; Emit("window.mode", new { mode = mode.ToString() }); }
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
            ["workingSetMiB"] = System.Environment.WorkingSet / 1048576d, ["privateMiB"] = null,
            ["managedHeapMiB"] = GC.GetGCMemoryInfo().HeapSizeBytes / 1048576d,
            ["godotStaticMiB"] = Monitor(Performance.Monitor.MemoryStatic) / 1048576d,
            ["engineVideoMemoryMiB"] = Monitor(Performance.Monitor.RenderVideoMemUsed) / 1048576d,
            ["textureMemoryMiB"] = Monitor(Performance.Monitor.RenderTextureMemUsed) / 1048576d,
            ["bufferMemoryMiB"] = Monitor(Performance.Monitor.RenderBufferMemUsed) / 1048576d,
            ["objectCount"] = Monitor(Performance.Monitor.ObjectCount), ["nodeCount"] = Monitor(Performance.Monitor.ObjectNodeCount),
            ["orphanNodeCount"] = Monitor(Performance.Monitor.ObjectOrphanNodeCount),
            ["resourceCount"] = Monitor(Performance.Monitor.ObjectResourceCount), ["processCpuFraction"] = cpuFraction
        };
        _samples.TryRow(new("samples", Engine.GetProcessFrames(), now, values));
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
        if (_loadingActive != _loading.Active || _loadingCovered != _loading.IsCovered)
        {
            _loadingActive = _loading.Active; _loadingCovered = _loading.IsCovered;
            Emit("loading.state", new { active = _loadingActive, covered = _loadingCovered });
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
        Emit("map.arrival", new { phase = "end", destination = _arrival.Destination, result = Completion(_arrival.Done) });
    }
    // The map/camera owner retains the original failure; recording observes its task terminal status only.
    private static string Completion(Task task) => task.IsCanceled ? "cancelled" : task.IsFaulted ? "failed" : "ready";
    private void OnArrival(IMapArrival? arrival)
    {
        CheckArrival();
        if (_arrival is not null && !_arrivalFinished) Emit("map.arrival", new { phase = "detached", destination = _arrival.Destination, result = "ownerReplaced" });
        _arrival = arrival; _arrivalFinished = false;
        if (arrival is not null) Emit("map.arrival", new { phase = "begin", destination = arrival.Destination, result = (string?)null });
        CheckArrival();
    }
    private void OnAppState(AppState from, AppState to)
    {
        Emit("app.state", new { from = from.ToString(), to = to.ToString() });
    }
    private void OnAppPhase(string phase) => Emit("app.phase", new { phase });
    private void OnWorldInstance(long instance) => Emit("world.instance", new { instance });
    private void OnUi() => Emit("ui.surface", new { surfaces = (int)_ui.Open, settingsPage = _ui.SettingsPage });
    private void OnQuality(QualityChange change)
    {
        Emit("settings.quality", new { change, requested = PerfManifest.QualityValues(_quality, true),
            effective = PerfManifest.QualityValues(_quality, false), observation = "ownerChanged" });
    }
    private void OnSave() => Emit("save.write", new { state = _save.State, busy = _save.Busy, active = _save.Active });
    private void OnEnterCommand(EnterCommandEvent value)
    {
        double time = FrameClock.MonotonicMilliseconds;
        bool startObserved = _context.Observe(in value, time);
        Emit("game.enter", new { owner = "IAppFlow", operation = value.Operation, phase = value.Phase,
            target = value.Target, result = value.Result, direct = value.Direct, errorKey = value.ErrorKey,
            startObserved }, time: time);
    }
    private void OnPrepareCommand(MapPrepareEvent value)
    {
        double time = FrameClock.MonotonicMilliseconds;
        bool startObserved = _context.Observe(in value, time);
        Emit("map.prepare", new { owner = "IMapService", operation = value.Operation, phase = value.Phase,
            from = value.From, to = value.To, result = value.Result, startObserved }, time: time);
    }
    private void OnApplyCommand(QualityApplyEvent value)
    {
        double time = FrameClock.MonotonicMilliseconds;
        bool startObserved = _context.Observe(in value, time);
        Emit("settings.apply", new { owner = "IQualityService", operation = value.Operation, phase = value.Phase,
            target = value.Target, origin = value.Origin, result = value.Result, startObserved }, time: time);
    }
    // SaveLoading resumes off-thread. Its callback only copies a contract value into this independent queue.
    // The main sampling point supplies observation time/frame; it never claims the original worker timestamp.
    private void DrainSaveCommands(ulong frame, double time)
    {
        while (_saveCommands.TryDequeue(out var value))
        {
            bool startObserved = _context.Observe(in value, time);
            Emit("save.load", new { owner = "ISaveService", operation = value.Operation, phase = value.Phase,
                kind = value.Kind, requestedSlot = value.Slot, requestedGeneration = value.RequestedGeneration,
                file = value.File, result = value.Result, startObserved, timing = "mainThreadObservation" }, frame, time);
        }
    }
    private void OnSize()
    {
        var size = _viewport.GetVisibleRect().Size;
        Emit("window.size", new { widthPx = size.X, heightPx = size.Y });
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
            UnbindMap();
            CheckTransit();
            if (_transit is not null && !_transitFinished)
                Emit("transit", new { phase = "detached", kind = _transit.Kind.ToString(), result = "recordingStopped" });
        }
        finally
        {
            _app.Changed -= OnAppState; _app.PhaseReached -= OnAppPhase;
            _app.EnterCommand -= OnEnterCommand; _maps.PrepareCommand -= OnPrepareCommand;
            _quality.ApplyCommand -= OnApplyCommand; _save.LoadCommand -= _saveCommands.Enqueue;
            _saveCommands.Stop();
            _world.InstanceChanged -= OnWorldInstance; _ui.Changed -= OnUi;
            _quality.Changed -= OnQuality; _save.SlotsChanged -= OnSave; _viewport.SizeChanged -= OnSize;
            try { DrainSaveCommands(Engine.GetProcessFrames(), FrameClock.MonotonicMilliseconds); }
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
    private readonly Queue<SaveLoadEvent> _values = new();
    private bool _stopped;
    public void Enqueue(SaveLoadEvent value)
    {
        lock (_gate)
            if (!_stopped) _values.Enqueue(value);
    }
    public bool TryDequeue(out SaveLoadEvent value)
    {
        lock (_gate) return _values.TryDequeue(out value);
    }
    public void Stop() { lock (_gate) _stopped = true; }
}
