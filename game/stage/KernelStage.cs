using Campus.Gates;
using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Bake;
using Campus.Contracts.Maps;
using Campus.Contracts.Quality;
using Campus.Contracts.Save;
using Campus.Contracts.Settings;
using Campus.Contracts.Startup;
using Campus.Kernel.Bake;
using Campus.Kernel.Boot;
using Campus.Kernel.Frame;
using Campus.Kernel.Inspect;
using Campus.Kernel.Registry;
using Campus.Kernel.Renderer;
using Campus.Kernel.Stream;
using Godot;
using AppBoot = Campus.Kernel.Boot.Boot;
using KernelServices = Campus.Kernel.Registry.Kernel;

namespace Campus.Stage;

/// <summary>The production composition root. Map data, world state and flow are supplied exclusively through services.</summary>
public partial class KernelStage : Node3D, IAppExit, IStageGateHost
{
    private StageCamera? _view;
    private FrameStatistics? _statistics;
    private IAppFlow? _flow;
    private IMapRuntime? _mapRuntime;
    private bool _ready;
    private bool _closing;
    private bool _cpuCommand;
    private bool _shutdownStarted;
    private Task? _shutdownTask;
    private Task? _closeTask;
    private Task<int>? _bakeCommandTask;
    private SceneTree? _sceneTree;
    private bool _treeExited;
    private bool _shutdownComplete;
    private bool _quitIssued;
    private int _exitCode;
    private IAppLoadingLease? _bootLoading;
    private Task? _bootLoadingDisposal;
    private IFlowLease? _startupFlow;
    private Task? _backgroundPreparation;
    private bool _startupFinished;
    private bool _realtimeStarted;
    private readonly CancellationTokenSource _captureLifetime = new();
    private ViewContentReadiness? _contentReadiness;
    private GameCaptureNode.CaptureDestination? _startupCapture;
    private readonly StageGateDriver _gate;
    private readonly StageStartupFailure _startupFailure;
    private readonly StageStartupTrace _startupTrace = new();
    public KernelStage()
    {
        _startupFailure = new(this);
        _gate = new(this);
    }

    public override async void _Ready()
    {
        if (AppBoot.Failed) return;
        _sceneTree = GetTree();
        Task? startup = null;
        try
        {
            var registry = KernelServices.Services;
            IRequiredResources resources = new RequiredResources();
            registry.Add(resources);
            var codeFingerprints = new BuildCodeIndex(resources);
            registry.Add(codeFingerprints);
            registry.Discover(StageModules.LoadModuleTypes(), codeFingerprints.Owner);
            GD.Print($"Kernel registry: {registry.Entries.Count} declarations; build {BuildInfo.Build}");
            if (RegistryStageGate.TryRun()) { await FinishQuit(); return; }
            if (AppBoot.Options.CpuBakeMode) { await RunBakeCommand(); return; }
            Compose();
            await (startup = StartWorld());
        }
        catch (OperationCanceledException) when (_captureLifetime.IsCancellationRequested && startup?.IsCanceled == true) { }
        catch (Exception error) { await _startupFailure.Fail(error); }
    }

    private void Compose()
    {
        var registry = KernelServices.Services;
        var view = CreateStagePresentation(registry);
        registry.InstantiateServices();
        BindHudDependencies(registry);
        BindMapDependencies(registry, view.Camera);
        AttachGlobalNodes(registry);
        BindRuntimeServices(registry, view);
        _statistics = new(GetViewport());
        _statistics.Enable();
        DisplayServer.WindowSetTitle("电光年华");
        GD.Print($"[ENG-ARCH][map=-][phase=compose] build={BuildInfo.Build}; global objects={registry.Created.Count}");
    }

    private StageCamera CreateStagePresentation(ServiceRegistry registry)
    {
        ProcessPriority = FramePriority.Stage;
        var view = new StageCamera();
        _view = view;
        AddChild(view);
        var environment = new WorldEnvironment
        {
            Name = "WorldEnvironment",
            Environment = new Godot.Environment
            {
                BackgroundMode = Godot.Environment.BGMode.Color,
                BackgroundColor = RenderingServer.GetDefaultClearColor(),
                TonemapMode = Godot.Environment.ToneMapper.Linear,
                AmbientLightSource = Godot.Environment.AmbientSource.Disabled,
                ReflectedLightSource = Godot.Environment.ReflectionSource.Disabled,
                FogEnabled = false,
                VolumetricFogEnabled = false
            }
        };
        AddChild(environment);
        var ui = new CanvasLayer { Name = "Ui", Layer = 10, Visible = AppBoot.Options.Ui.Visible ?? true };
        AddChild(ui);
        CapturePresentation.Bind(ui);
        registry.Add(view.Camera);
        registry.Add(environment);
        registry.Add(environment.Environment);
        registry.Add(ui);
        registry.Add<IAppExit>(this);
        return view;
    }

    private static void BindHudDependencies(ServiceRegistry registry)
    {
        registry.Get<Campus.Hud.HudRoot>().Bind(new Campus.Hud.HudRootDependencies(
            Settings: registry.Get<Campus.Hud.HudSettings>(),
            Model: registry.Get<Campus.Hud.HudModel>(),
            Motion: registry.Get<Campus.Hud.Motion.MotionPolicy>(),
            WorldControl: registry.Get<IWorldControl>(),
            Flow: registry.Get<IAppFlow>(),
            Exit: registry.Get<IAppExit>(),
            Audio: registry.Get<Campus.Contracts.Audio.IAudioService>(),
            PaletteCatalog: registry.Get<IPaletteCatalogSource>(),
            ShaderMaterials: registry.Get<IShaderMaterialSource>(),
            ThemeClock: registry.Get<Campus.Hud.Style.UiThemeClock>(),
            InputRouter: registry.Get<IInputRouter>(),
            ViewControl: registry.Get<IViewControl>(),
            Capture: registry.Get<IGameCapture>(),
            Maps: registry.Get<IMapService>(),
            WorldState: registry.Get<IWorldState>(),
            Saves: registry.Get<ISaveService>(),
            PaletteProbe: registry.Get<IPaletteProbe>(),
            Diagnostics: registry.Get<Campus.Contracts.Diagnostics.IDiagnosticLog>(),
            Resources: registry.Get<IRequiredResources>()));
    }

    private static void BindMapDependencies(ServiceRegistry registry, Camera3D viewCamera)
    {
        var resources = registry.Get<IRequiredResources>();
        registry.Get<PipelineWarmup>().Bind(registry.Get<IToonMaterialProvider>(), registry.Get<IShaderMaterialSource>(),
            () => BakePipelineCatalog.Read(resources), TimeSpan.FromSeconds(BootTuning.StalledSeconds));
        ((MapRuntime)registry.Create(typeof(MapRuntime))).Bind(new(registry,
            registry.Get<IMapStreamState>(), registry.Get<IBakeService>(), registry.Get<ICollisionWorld>(),
            registry.Get<IRenderReadiness>(), registry.Get<ICameraRig>(),
            registry.Get<ITerrainPreparation>(), viewCamera));
        ((StreamDirector)registry.Create(typeof(StreamDirector))).Bind(new(registry,
            registry.Get<IBakeContextSource>(), registry.Get<IBakeService>(), registry.Get<IStreamMountQueue>(),
            registry.Get<ICameraRig>(), registry.Get<IObserverState>(), registry.Get<IWorldState>(),
            registry.Get<ICollisionWorld>(), registry.Get<IMapService>(), registry.Get<ITerrainPreparation>(),
            registry.Get<IReflectionWorld>(), registry.Get<IToonMaterialProvider>(), registry.Get<IShaderMaterialSource>(),
            viewCamera, AppBoot.Options.CaptureMode, registry.Get<IStreamGeometryPreparation>()));
    }

    private void AttachGlobalNodes(ServiceRegistry registry)
    {
        // Build the complete batch, attach every node, then invoke lifecycle hooks. An fx.* consumer
        // must not depend on its instance-pool host sorting ahead of it; _Ready only prepares structure.
        var globalNodes = registry.Query<Node>(RegistrationKind.Node).Where(entry => entry.Lifetime == RegistrationLifetime.Global && entry.Map is null)
            .OrderBy(entry => entry.Id, StringComparer.Ordinal)
            .Select(entry => registry.Create(entry.Type) as Node
                ?? throw new InvalidOperationException($"KernelNode must inherit Node: {entry.Id}."))
            .ToArray();
        foreach (var node in globalNodes) AddChild(node);
        if (globalNodes.Any(node => !node.IsInsideTree()))
            throw new InvalidOperationException("Every global kernel node must enter the tree before startup callbacks.");
        _ = registry.RuntimeInstances<object>();
    }

    private void BindRuntimeServices(ServiceRegistry registry, StageCamera view)
    {
        _startupFailure.BindPresentation(registry);
        _flow = registry.Get<IAppFlow>();
        _flow.PhaseReached += phase => StageStartupTrace.TraceFlowPhase(registry, phase);
        if (!ReferenceEquals(_flow, registry.Get<IAppBootFlow>()))
            throw new InvalidOperationException("IAppFlow and IAppBootFlow must resolve to the same instance.");
        _flow.Changed += OnFlowChanged;
        _mapRuntime = registry.Get<IMapRuntime>();
        view.Bind(registry.Get<ICameraRig>());
    }

    internal bool CanHandleFailure => !_treeExited && !_shutdownStarted;
    internal bool CanKeepFailurePresentation => !_closing && CanHandleFailure;
    internal Task ShutdownCompletion => _shutdownTask ?? _closeTask
        ?? throw new InvalidOperationException("No close operation owns this invalidated startup session.");

    internal void MarkFailure()
    {
        _ready = false;
        _exitCode = Math.Max(_exitCode, 2);
    }

    internal void FailBoot(Exception error)
    {
        if (_flow is IAppBootFlow bootFlow) bootFlow.FailBoot(error);
    }

    internal void CancelStartupCapture() => _captureLifetime.Cancel();

    internal Task ReleaseStartupPresentation()
    {
        ReleaseStartupFlow();
        return DisposeBootLoading();
    }

    private async Task StartWorld()
    {
        var registry = KernelServices.Services;
        var options = AppBoot.Options;
        var staticUiCapture = options.CaptureMode && options.Title && options.World.Pose is null &&
            options.Capture.Gate is null && options.Capture.Shots is ["ui"];
        var showTitle = options.Flow.ShowTitle &&
            (!options.CaptureMode && !options.Automated || staticUiCapture || options.Capture.Gate == "save-close");
        var frontispiece = registry.Get<IAppFrontispiecePresentation>();
        var participants = registry.RuntimeInstances<IBootParticipant>().OrderBy(item => item.Order)
            .ThenBy(item => item.Id, StringComparer.Ordinal).ToArray();
        if (participants.GroupBy(item => item.Id, StringComparer.Ordinal).Any(group => group.Count() != 1))
            throw new InvalidOperationException("Boot participant IDs must be unique.");
        var context = CreateBootContext();
        var failures = new List<Exception>();
        try
        {
            _startupFlow = registry.Get<IWorldControl>().AcquireFlow(FlowPriority.Pause, 0);
            // The fixed image has no catalogue, save metadata or world dependency.
            if (showTitle) await frontispiece.Show();
            await InitializeBootPhase(BootPhase.BeforeMap, participants, context);
            // Quality has no active-map or world dependency. Its effective settings must precede pipeline submission.
            await InitializeBootPhase(BootPhase.AfterMapWarm, participants, context);
            await BootWorldOverrides.ApplyQuality(options, registry.Get<IQualityService>());
            registry.Get<PipelineWarmup>().SetCanvasFont(registry.Created.OfType<Campus.Hud.HudRoot>().Single().CanvasFont);
            Phase = "prepare-flow";
            var appBoot = registry.Get<IAppBootFlow>();
            var target = await appBoot.PrepareBoot(options.Flow with { ShowTitle = showTitle });
            var maps = registry.Get<IMapService>();
            // Both tasks start on the engine thread. Neither requires an active world instance.
            var mapPreparation = maps.Prepare(target);
            var globalPreparation = PrepareGlobal(registry, participants, context);
            _backgroundPreparation = appBoot.PrepareBackground(mapPreparation, globalPreparation);
            _ = ObserveBackgroundFailure(appBoot);
            if (showTitle)
            {
                CheckStartupActive();
                await appBoot.OpenTitle();
                var titleInputReady = TraceTitleInputReady(appBoot);
                if (staticUiCapture)
                {
                    await registry.Created.OfType<GameCaptureNode>().Single().CaptureStaticUiAsync(this, _captureLifetime.Token);
                    await CloseAsync();
                    return;
                }
                await _backgroundPreparation.WaitAsync(_captureLifetime.Token);
                await titleInputReady;
                CheckStartupActive();
                _contentReadiness = ViewContentReadiness.Capture(this);
                Phase = "background-ready";
            }
            else await StartDirectWorld(registry, options, appBoot, maps, _backgroundPreparation);
            CheckStartupActive();
            _startupFinished = true;
            _startupFailure.CompleteBoot();
            if (_flow!.IsPlaying) await CompletePlayingStartup();
        }
        catch (OperationCanceledException error) when (_captureLifetime.IsCancellationRequested &&
            (error.CancellationToken == _captureLifetime.Token || _backgroundPreparation?.IsCanceled == true)) { failures.Add(error); }
        catch (Exception error)
        {
            failures.Add(error);
            _ = _startupFailure.Fail(error);
        }
        finally
        {
            try { ReleaseStartupFlow(); } catch (Exception error) { failures.Add(error); _ = _startupFailure.Fail(error); }
            try { await DisposeBootLoading(); } catch (Exception error) { failures.Add(error); _ = _startupFailure.Fail(error); }
        }
        if (failures.Count == 1) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count > 0) throw new AggregateException("Stage startup and presentation cleanup failed.", failures);
        if (_flow!.State == AppState.Title) await _gate.OnTitleReady();
    }

    private async Task InitializeBootPhase(BootPhase phase, IBootParticipant[] participants, BootContext context)
    {
        Phase = phase.ToString();
        foreach (var participant in participants)
        {
            CheckStartupActive();
            var initialization = participant.Initialize(phase, context);
            try { await initialization; }
            catch (OperationCanceledException) when (_captureLifetime.IsCancellationRequested && initialization.IsCanceled)
            { throw new OperationCanceledException(_captureLifetime.Token); }
        }
        CheckStartupActive();
    }

    private async Task PrepareGlobal(ServiceRegistry registry, IBootParticipant[] participants, BootContext context)
    {
        var pipelines = registry.Get<IPipelinePreparation>().PrepareAsync(_captureLifetime.Token);
        var jit = InitializeBootPhase(BootPhase.BeforeReveal, participants, context);
        await Task.WhenAll(pipelines, jit);
    }

    private async Task StartDirectWorld(ServiceRegistry registry, BootRunOptions options, IAppBootFlow appBoot,
        IMapService maps, Task backgroundPreparation)
    {
        _bootLoading = registry.Get<IAppLoadingPresentation>().Show();
        await _bootLoading.Covered;
        await appBoot.MapReady.WaitAsync(_captureLifetime.Token);
        CheckStartupActive();
        WorldPreset? start = null; NewGameBoot? boot = null;
        if (!options.HasLoad) (start, boot) = await BootWorldOverrides.PrepareFreshEntry(options, registry);
        var entering = appBoot.EnterDirect(start, boot);
        // The entry owns its own opaque lease before the temporary startup lease is released.
        _bootLoading.Report(1, 1);
        await DisposeBootLoading();
        var arrival = await entering;
        CheckStartupActive();
        _view!.Sync();
        var rig = registry.Get<ICameraRig>();
        _contentReadiness = ViewContentReadiness.Capture(this);
        _startupCapture = options.World.Pose is not { } poseId ? null : new GameCaptureNode.CaptureDestination(
            poseId, rig.Mode, maps.Context!, registry.Get<IWorldState>().Instance, arrival, rig.Pose);
        await backgroundPreparation;
    }

    private async Task ObserveBackgroundFailure(IAppBootFlow boot)
    {
        try { await boot.BackgroundReady; }
        catch (OperationCanceledException) when (boot.BackgroundReady.IsCanceled) { }
        catch (Exception error) { await _startupFailure.Fail(error); }
    }

    private async Task CompletePlayingStartup()
    {
        if (_realtimeStarted || !_startupFinished || _flow?.IsPlaying != true) return;
        _realtimeStarted = true;
        _statistics!.MarkRealtime();
        _ready = true;
        Phase = "realtime";
        GD.Print($"[ENG-ARCH][map={KernelServices.Get<IMapService>().Current.Value}][phase=realtime] flow={_flow.State}; startup complete");
        try { await _gate.OnRealtime(); }
        catch (Exception error) { await _startupFailure.Fail(error); }
    }

    private void ReleaseStartupFlow()
    {
        var flow = _startupFlow;
        _startupFlow = null;
        flow?.Dispose();
    }

    private Task DisposeBootLoading()
    {
        if (_bootLoadingDisposal is not null) return _bootLoadingDisposal;
        var loading = _bootLoading;
        _bootLoading = null;
        return _bootLoadingDisposal = loading?.DisposeAsync().AsTask() ?? Task.CompletedTask;
    }

    private void CheckStartupActive()
    {
        _captureLifetime.Token.ThrowIfCancellationRequested();
    }

    private string _phase = "compose";
    public string Phase
    {
        get => _phase;
        private set
        {
            _phase = value;
            StageStartupTrace.TracePhase(value);
        }
    }

    private async Task TraceTitleInputReady(IAppBootFlow boot)
    {
        try
        {
            await FrameAwaiter.ProcessAsync(this, _captureLifetime.Token);
            CheckStartupActive();
            StageStartupTrace.TraceTitleInputReady(boot);
        }
        catch (OperationCanceledException error) when (_captureLifetime.IsCancellationRequested && error.CancellationToken == _captureLifetime.Token) { }
        catch (Exception error) { await _startupFailure.Fail(error); }
    }

    private void OnFlowChanged(AppState before, AppState after)
    {
        // Playing is published immediately before ReleaseInput, with no await between them.
        // Observe after that stack unwinds, without waiting for the first save to finish.
        if (before == AppState.Entering && after == AppState.Playing)
        {
            Callable.From(TraceInputReady).CallDeferred();
            if (_startupFinished) _ = CompletePlayingStartup();
        }
    }

    private void TraceInputReady()
    {
        if (_closing || _flow?.State != AppState.Playing) return;
        StageStartupTrace.TraceInputReady();
    }

    private async Task RunBakeCommand()
    {
        _cpuCommand = true;
        Phase = "bake-command";
        // CPU batches deliberately bypass the visual composition root and map preparation.
        // Only the requested command and its declared constructor dependencies are resolved.
        var command = KernelServices.Get<IBakeCommand>();
        if (command is Node node) AddChild(node);
        _bakeCommandTask = command.Run(CreateBootContext());
        var exitCode = await _bakeCommandTask;
        await FinishQuit(exitCode);
    }

    private static BootContext CreateBootContext()
    {
        var options = AppBoot.Options;
        return new(options, options.SaveDirectory ?? Path.Combine(OS.GetUserDataDir(), "saves"), BuildInfo.Build);
    }

    public override void _Process(double delta)
    {
        if (!_closing && !_startupFailure.Failed && _mapRuntime?.Failure is { } failure && failure is not OperationCanceledException)
            _ = _startupFailure.Fail(failure);
        _statistics?.Sample();
        _startupTrace.TraceFrame(_statistics);
        if (_flow is IAppBootFlow boot && _flow.State is AppState.Title) _startupTrace.TracePreloadReady(boot);
        _gate.OnProcess();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest)
        {
            _gate.OnCloseRequest();
            _ = CloseAsync();
        }
    }
    private Task CloseAsync()
    {
        if (_shutdownTask is not null) return _shutdownTask;
        if (_closeTask is not null) return _closeTask;
        var completion = new TaskCompletionSource();
        _closeTask = completion.Task;
        _ = Close();
        return completion.Task;

        async Task Close()
        {
            try
            {
                await CloseCoreAsync();
                if (!_closing) _closeTask = null;
                completion.TrySetResult();
            }
            catch (Exception error) { completion.TrySetException(error); }
        }
    }

    private async Task CloseCoreAsync()
    {
        try
        {
            if (_shutdownTask is not null) { await _shutdownTask; return; }
            _closing = true;
            _captureLifetime.Cancel();
            if (_startupFailure.Failed) { await FinishQuit(2); return; }
            if (_cpuCommand) { await FinishQuit(130); return; }
            if (_flow is null) { await FinishQuit(); return; }
            // All requests, including repeats, use the flow's shared quit task. It calls IAppExit
            // only after saving and the user's final decision; observing Quitting is not permission to exit.
            await _flow.Quit();
            if (_shutdownStarted) { await _shutdownTask!; return; }
            switch (_flow.State)
            {
                case AppState.Quitting: throw new InvalidOperationException("Quit completed without the IAppExit shutdown bridge.");
                case AppState.Playing:
                case AppState.Title: _closing = false; break;
                case AppState.Boot:
                case AppState.Entering:
                case AppState.Leaving:
                default: throw new InvalidOperationException("Quit completed without a final quit or resume decision.");
            }
        }
        catch (Exception error) { await _startupFailure.Fail(error); }
    }

    public override void _ExitTree()
    {
        _treeExited = true;
        if (AppBoot.Failed || _shutdownComplete) return;
        // Leaving the tree first destroys the lifetime required by async cleanup. Reject that
        // ownership violation synchronously; never start a replacement shutdown on a dead stage.
        var error = new InvalidOperationException("KernelStage left the tree before its awaited shutdown completed.");
        _ = _startupFailure.Fail(error);
        QuitEngine();
        throw error;
    }

    public Task FinishQuit(int exitCode = 0) => BeginShutdownAndQuit(exitCode);

    private Task BeginShutdownAndQuit(int exitCode)
    {
        _exitCode = Math.Max(_exitCode, exitCode);
        if (_shutdownTask is not null) return _shutdownTask;
        if (_treeExited) throw new InvalidOperationException("Stage shutdown must begin before leaving the tree.");
        var completion = new TaskCompletionSource();
        // Publish before cancellation or shutdown can reenter this boundary.
        _shutdownTask = completion.Task;
        _shutdownStarted = true;
        _closing = true;
        _startupFailure.InvalidateSession();
        _ = Finish();
        return _shutdownTask;

        async Task Finish()
        {
            try { await ShutdownAndQuit(); }
            catch (Exception error)
            {
                _ = _startupFailure.Fail(error);
                QuitEngine();
            }
            finally { completion.TrySetResult(); }
        }
    }

    private async Task ShutdownAndQuit()
    {
        _ready = false;
        _statistics = null;
        var failures = new List<Exception>();
        try { Phase = "shutdown"; } catch (Exception error) { failures.Add(error); }
        try { _captureLifetime.Cancel(); } catch (Exception error) { failures.Add(error); }
        try { if (_flow is not null) _flow.Changed -= OnFlowChanged; } catch (Exception error) { failures.Add(error); }
        // The command owns its own baking resources; a close request must let its finally blocks
        // finish before the registry can release its dependencies and the engine can quit.
        if (_bakeCommandTask is not null)
            try
            {
                var commandExit = await _bakeCommandTask;
                if (commandExit != 0) _exitCode = Math.Max(_exitCode, commandExit);
            }
            catch (Exception error) { failures.Add(error); }
        try { await KernelServices.Services.SealMapPreparationAsync(); } catch (Exception error) { failures.Add(error); }
        if (_backgroundPreparation is not null)
            try { await _backgroundPreparation; }
            catch (OperationCanceledException) when (_captureLifetime.IsCancellationRequested && _backgroundPreparation.IsCanceled) { }
            catch (Exception error) { failures.Add(error); }
        try { ReleaseStartupFlow(); } catch (Exception error) { failures.Add(error); }
        try { await DisposeBootLoading(); } catch (Exception error) { failures.Add(error); }
        try { await KernelServices.Services.ShutdownAsync(this, _gate.TraceClose); }
        catch (Exception error) { failures.Add(error); }
        try { _gate.TraceClose("shutdown-complete"); } catch (Exception error) { failures.Add(error); }
        try { _gate.ReportShutdown(); } catch (Exception error) { failures.Add(error); }
        if (failures.Count > 0) _ = _startupFailure.Fail(new AggregateException("Stage cleanup failed.", failures));
        _shutdownComplete = true;
        QuitEngine();
    }

    private void QuitEngine()
    {
        if (_quitIssued) return;
        _quitIssued = true;
        (_sceneTree ?? throw new InvalidOperationException("Stage has no live scene-tree owner.")).Quit(_exitCode);
    }

    Node IStageGateHost.Stage => this;
    IAppFlow IStageGateHost.Flow => _flow!;
    IMapRuntime IStageGateHost.MapRuntime => _mapRuntime!;
    ViewContentReadiness IStageGateHost.Readiness => _contentReadiness!;
    GameCaptureNode.CaptureDestination? IStageGateHost.StartupCapture => _startupCapture;
    CancellationToken IStageGateHost.Lifetime => _captureLifetime.Token;
    bool IStageGateHost.Ready => _ready;
    bool IStageGateHost.Closing => _closing;
    void IStageGateHost.CheckActive() => CheckStartupActive();
    void IStageGateHost.SyncView() => _view!.Sync();
    Task IStageGateHost.Close() => CloseAsync();
    void IStageGateHost.Fail(Exception error) => _ = _startupFailure.Fail(error);
}
