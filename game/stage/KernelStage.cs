using System.Globalization;
using System.Reflection;
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
    private readonly bool _startupProfiling = System.Environment.GetEnvironmentVariable("UESTC_STARTUP_PROFILE") == "1";
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
    private readonly CancellationTokenSource _captureLifetime = new();
    private ViewContentReadiness? _contentReadiness;
    private GameCaptureNode.CaptureDestination? _startupCapture;
    private readonly StageGateDriver _gate;
    private readonly StageStartupFailure _startupFailure;
    private string _phase = "compose";
    public string Phase
    {
        get => _phase;
        private set
        {
            _phase = value;
            RegistryStageGate.TraceBootPhase(value);
            GD.Print(FormattableString.Invariant($"STARTUP_PHASE {value} {FrameClock.MonotonicMilliseconds:R} frame={Engine.GetProcessFrames()}"));
        }
    }

    public KernelStage()
    {
        _startupFailure = new(this);
        _gate = new(this);
    }

    public override async void _Ready()
    {
        if (AppBoot.Failed) return;
        _sceneTree = GetTree();
        try
        {
            var registry = KernelServices.Services;
            registry.Add<IRequiredResources>(new RequiredResources());
            registry.Discover(LoadModuleTypes());
            GD.Print($"Kernel registry: {registry.Entries.Count} declarations; build {BuildInfo.Build}");
            if (RegistryStageGate.TryRun()) { await FinishQuit(); return; }
            if (AppBoot.Options.CpuBakeMode) { await RunBakeCommand(); return; }
            Compose();
            await StartWorld();
        }
        catch (OperationCanceledException) when (_closing || _startupFailure.Failed) { }
        catch (Exception error) { await _startupFailure.Fail(error); }
    }

    private static IEnumerable<Type> LoadModuleTypes()
    {
        foreach (var name in BuildInfo.ModuleAssemblies)
        {
            Type[] types;
            try { types = Assembly.Load(new AssemblyName(name)).GetTypes(); }
            catch (Exception error)
            { throw new AppStartupException(AppStartupFailureCodes.Installation, $"Cannot load module assembly: {name}.", error); }
            foreach (var type in types) yield return type;
        }
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
        registry.Get<PipelineWarmup>().Bind(registry.Get<IToonMaterialProvider>(), registry.Get<IShaderMaterialSource>(),
            BakePipelineCatalog.Read, TimeSpan.FromSeconds(BootTuning.StalledSeconds));
        ((MapRuntime)registry.Create(typeof(MapRuntime))).Bind(new(registry,
            registry.Get<IMapStreamState>(), registry.Get<IBakeService>(), registry.Get<ICollisionWorld>(),
            registry.Get<IRenderReadiness>(), registry.Get<IPipelinePreparation>(), registry.Get<ICameraRig>(),
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
        _flow.PhaseReached += static phase =>
            GD.Print(FormattableString.Invariant($"STARTUP_PHASE {phase} {FrameClock.MonotonicMilliseconds:R} frame={Engine.GetProcessFrames()}"));
        if (!ReferenceEquals(_flow, registry.Get<IAppBootFlow>()))
            throw new InvalidOperationException("IAppFlow and IAppBootFlow must resolve to the same instance.");
        _flow.Changed += OnFlowChanged;
        _mapRuntime = registry.Get<IMapRuntime>();
        view.Bind(registry.Get<ICameraRig>());
    }

    private async Task StartWorld()
    {
        var registry = KernelServices.Services;
        var frontispiece = registry.Get<IAppFrontispiecePresentation>();
        var failures = new List<Exception>();
        try
        {
            if (AppBoot.Options.Flow.ShowTitle) await frontispiece.Show();
            else { _bootLoading = registry.Get<IAppLoadingPresentation>().Show(); await _bootLoading.Covered; }
            CheckStartupActive();
            _startupFlow = registry.Get<IWorldControl>().AcquireFlow(FlowPriority.Pause, 0);
            await StartWorld(frontispiece, _bootLoading, _startupFlow);
        }
        catch (OperationCanceledException error) when (_closing || _startupFailure.Failed) { failures.Add(error); }
        catch (Exception error)
        {
            failures.Add(error);
            // Publish the original failure before any cleanup await can race a window-close exit.
            _ = _startupFailure.Fail(error);
        }
        finally
        {
            try { ReleaseStartupFlow(); } catch (Exception error) { failures.Add(error); _ = _startupFailure.Fail(error); }
            try { await DisposeBootLoading(); } catch (Exception error) { failures.Add(error); _ = _startupFailure.Fail(error); }
        }
        if (failures.Count == 1) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count > 0) throw new AggregateException("Stage startup and presentation cleanup failed.", failures);
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

    private async Task StartWorld(IAppFrontispiecePresentation frontispiece, IAppLoadingLease? bootLoading, IFlowLease startupFlow)
    {
        var options = AppBoot.Options;
        var showTitle = options.Flow.ShowTitle;
        var registry = KernelServices.Services;
        var participants = registry.RuntimeInstances<IBootParticipant>().OrderBy(item => item.Order)
            .ThenBy(item => item.Id, StringComparer.Ordinal).ToArray();
        if (participants.GroupBy(item => item.Id, StringComparer.Ordinal).Any(group => group.Count() != 1))
            throw new InvalidOperationException("Boot participant IDs must be unique.");
        var context = CreateBootContext();
        CheckStartupActive();
        if (!registry.Get<IAppScreenCoverage>().IsCovered)
            throw new InvalidOperationException("Startup has no fully opaque application presentation.");
        await Initialize(BootPhase.BeforeMap);
        registry.Get<PipelineWarmup>().SetCanvasFont(registry.Created.OfType<Campus.Hud.HudRoot>().Single().CanvasFont);
        Phase = "prepare-flow";
        var appBoot = registry.Get<IAppBootFlow>();
        var target = await appBoot.PrepareBoot(options.Flow);
        CheckStartupActive();
        // Interface, sound and save metadata are ready; no map work delays menu interaction.
        if (showTitle) await appBoot.OpenTitle();
        var maps = registry.Get<IMapService>();
        if (!appBoot.WillRestoreWorld)
        {
            Phase = "initialize-world";
            var initialMap = maps.Maps.Single(map => map.Id == target);
            BootWorldOverrides.InitializeFreshWorld(options, registry, initialMap);
        }
        Phase = "travel-boot";
        // First classification measures the map's default overview (V1); PrepareWorld places T0 afterwards.
        var travel = maps.Travel(new MapTravelRequest(target, options.Flow.Spawn,
            null, ViewMode.Fly, MapTravelCause.Boot));
        var travelled = await travel.Done;
        CheckStartupActive();
        if (travelled != MapTravelResult.Arrived)
        {
            if (travel.Failure is { } failure) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
            throw new InvalidOperationException("Boot map travel was rejected without a preparation failure.");
        }
        await Initialize(BootPhase.AfterMapWarm);
        await BootWorldOverrides.ApplyQuality(options, registry.Get<IQualityService>());
        CheckStartupActive();
        Phase = "prepare-world";
        // The user's base flow is already set; only the initial warmup pause ends here.
        // Lifecycle checks also keep loaded world time fixed until their own test lease takes over.
        if (options.Capture.Gate is not ("save-close" or "save-resume")) startupFlow.Dispose();
        await appBoot.PrepareWorld();
        CheckStartupActive();
        var poseId = await BootWorldOverrides.ApplyView(options, registry, appBoot.WillRestoreWorld, showTitle);
        CheckStartupActive();
        _view!.Sync();
        var rig = registry.Get<ICameraRig>();
        var arrival = _mapRuntime!.Arrive(rig.Pose, rig.Mode);
        _startupCapture = poseId is null ? null : new GameCaptureNode.CaptureDestination(
            poseId, rig.Mode, maps.Context!, registry.Get<IWorldState>().Instance, arrival, rig.Pose);
        await Initialize(BootPhase.BeforeReveal);
        _contentReadiness = ViewContentReadiness.Capture(this);
        Phase = "warm-final-view";
        await _contentReadiness.WaitForReadyAsync(this, arrival, () =>
        {
            CheckStartupActive();
            return true;
        }, _captureLifetime.Token);
        CheckStartupActive();
        await RevealBoot(frontispiece, bootLoading, appBoot, showTitle, maps);

        async Task Initialize(BootPhase phase)
        {
            Phase = phase.ToString();
            foreach (var participant in participants)
            {
                CheckStartupActive();
                await participant.Initialize(phase, context);
            }
            CheckStartupActive();
        }
    }

    private async Task RevealBoot(IAppFrontispiecePresentation frontispiece, IAppLoadingLease? bootLoading,
        IAppBootFlow appBoot, bool showTitle, IMapService maps)
    {
        Phase = "reveal";
        _statistics!.MarkRealtime();
        // Retire the boot-only image independently of foreground loading ownership. A player
        // who already entered remains covered by their loading lease while joining this work.
        if (showTitle && _flow!.State != AppState.Title) await frontispiece.Hide(0);
        if (bootLoading is not null) { bootLoading.Report(1, 1); await bootLoading.Hide(); }
        await appBoot.CompleteBoot();
        if (showTitle) await frontispiece.Hide(BootTuning.TitleRevealSeconds);
        CheckStartupActive();
        _ready = true;
        _startupFailure.CompleteBoot();
        Phase = "realtime";
        GD.Print($"[ENG-ARCH][map={maps.Current.Value}][phase=realtime] flow={_flow!.State}; startup complete");
        await _gate.OnRealtime();
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

    private void CheckStartupActive()
    {
        if (_startupFailure.Failed) throw new OperationCanceledException("Startup stopped by the terminal stage failure.");
        if (_closing || _shutdownStarted) throw new OperationCanceledException("Startup stopped by the exit request.");
    }

    private void OnFlowChanged(AppState before, AppState after)
    {
        // Playing is published immediately before ReleaseInput, with no await between them.
        // Observe after that stack unwinds, without waiting for the first save to finish.
        if (before == AppState.Entering && after == AppState.Playing)
            Callable.From(TraceInputReady).CallDeferred();
    }

    private void TraceInputReady()
    {
        if (_closing || _flow?.State != AppState.Playing) return;
        GD.Print(FormattableString.Invariant($"STARTUP_PHASE input-ready {FrameClock.MonotonicMilliseconds:R} frame={Engine.GetProcessFrames()} mode={KernelServices.Get<ICameraRig>().Mode}"));
    }

    private async Task RunBakeCommand()
    {
        _cpuCommand = true;
        Phase = "bake-command";
        // CPU batches deliberately bypass the visual composition root and map-travel warmup.
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
        if (_startupProfiling && _statistics is { } statistics)
        {
            var frame = statistics.Current;
            GD.Print(FormattableString.Invariant($"STARTUP_FRAME {FrameClock.MonotonicMilliseconds:R} frame={Engine.GetProcessFrames()} rendererCpuMs={frame.CpuRenderMilliseconds:R} rendererGpuMs={frame.GpuRenderMilliseconds:R}"));
        }
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
