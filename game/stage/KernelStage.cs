using Campus.Kernel.Diagnostics;
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
    private bool _failed;
    private Task? _shutdownTask;
    private readonly CancellationTokenSource _captureLifetime = new();
    private ViewContentReadiness? _contentReadiness;
    private IAppLoadingLease? _failureLoading;
    private IFlowLease? _failureFlow;
    private readonly StageGateDriver _gate;
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
    public string? Failure { get; private set; }

    public KernelStage() => _gate = new(this);

    public override async void _Ready()
    {
        if (AppBoot.Failed) return;
        try
        {
            var registry = KernelServices.Services;
            registry.Discover(BuildInfo.ModuleAssemblies.Select(name => Assembly.Load(new AssemblyName(name))).SelectMany(assembly => assembly.GetTypes()));
            GD.Print($"Kernel registry: {registry.Entries.Count} declarations; build {BuildInfo.Build}");
            if (RegistryStageGate.TryRun(this)) return;
            if (AppBoot.Options.CpuBakeMode) { await RunBakeCommand(); return; }
            Compose();
            await StartWorld();
        }
        catch (OperationCanceledException) when (_closing) { }
        catch (Exception error) { Fail(error); }
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
        GD.Print($"[WP-01][map=-][phase=compose] build={BuildInfo.Build}; global objects={registry.Created.Count}");
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
            Diagnostics: registry.Get<Campus.Contracts.Diagnostics.IDiagnosticLog>()));
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
        IAppLoadingLease? bootLoading = null;
        if (AppBoot.Options.Flow.ShowTitle) await frontispiece.Show();
        else { bootLoading = registry.Get<IAppLoadingPresentation>().Show(); await bootLoading.Covered; }
        using var startupFlow = registry.Get<IWorldControl>().AcquireFlow(FlowPriority.Pause, 0);
        try { await StartWorld(frontispiece, bootLoading, startupFlow); }
        catch (OperationCanceledException) when (_closing) { throw; }
        catch
        {
            HoldFailure();
            throw;
        }
        finally { if (bootLoading is not null) await bootLoading.DisposeAsync(); }
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
        if (travelled != MapTravelResult.Arrived) throw new InvalidOperationException("Boot map travel failed.");
        await Initialize(BootPhase.AfterMapWarm);
        await BootWorldOverrides.ApplyQuality(options, registry.Get<IQualityService>());
        CheckStartupActive();
        Phase = "prepare-world";
        // The user's base flow is already set; only the initial warmup pause ends here.
        // Lifecycle checks also keep loaded world time fixed until their own test lease takes over.
        if (options.Capture.Gate is not ("save-close" or "save-resume")) startupFlow.Dispose();
        await appBoot.PrepareWorld();
        CheckStartupActive();
        await BootWorldOverrides.ApplyView(options, registry, appBoot.WillRestoreWorld, showTitle);
        CheckStartupActive();
        _view!.Sync();
        var rig = registry.Get<ICameraRig>();
        var arrival = _mapRuntime!.Arrive(rig.Pose, rig.Mode);
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
        Phase = "realtime";
        GD.Print($"[WP-01][map={maps.Current.Value}][phase=realtime] flow={_flow!.State}; startup complete");
        await _gate.OnRealtime();
    }

    private void CheckStartupActive()
    {
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
        var exitCode = await command.Run(CreateBootContext());
        if (!_closing) GetTree().Quit(exitCode);
    }

    private static BootContext CreateBootContext()
    {
        var options = AppBoot.Options;
        return new(options, options.SaveDirectory ?? Path.Combine(OS.GetUserDataDir(), "saves"), BuildInfo.Build);
    }

    public override void _Process(double delta)
    {
        if (!_closing && !_failed && _mapRuntime?.Failure is { } failure && failure is not OperationCanceledException)
            Fail(failure);
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
    private async Task CloseAsync()
    {
        try
        {
            if (_shutdownTask is not null) { await _shutdownTask; return; }
            _closing = true;
            _captureLifetime.Cancel();
            if (_cpuCommand) { GetTree().Quit(130); return; }
            if (_flow is null) { await FinishQuit(); return; }
            // All requests, including repeats, use the flow's shared quit task. It calls IAppExit
            // only after saving and the user's final decision; observing Quitting is not permission to exit.
            await _flow.Quit();
            if (_shutdownStarted) return;
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
        catch (Exception error) { _closing = false; Fail(error); }
    }
    public override void _ExitTree()
    {
        _closing = true;
        if (_flow is not null) _flow.Changed -= OnFlowChanged;
        _captureLifetime.Cancel();
        if (_failureLoading is not null) _ = _failureLoading.DisposeAsync();
        _failureFlow?.Dispose();
        _ = ObserveRegistryShutdown();
    }

    private static async Task ObserveRegistryShutdown()
    {
        try { await KernelServices.Services.ShutdownAsync(); }
        catch (Exception error) { DiagnosticLog.Error("[WP-01][phase=exit-tree]", error); }
    }

    public Task FinishQuit()
    {
        if (_shutdownTask is not null) return _shutdownTask;
        var completion = new TaskCompletionSource();
        _shutdownTask = completion.Task;
        _closing = true;
        _ = Finish();
        return _shutdownTask;

        async Task Finish()
        {
            try { await ShutdownAndQuit(0); completion.TrySetResult(); }
            catch (Exception error) { completion.TrySetException(error); }
        }
    }

    private async Task ShutdownAndQuit(int exitCode)
    {
        if (_shutdownStarted) return;
        _shutdownStarted = true;
        _ready = false;
        _statistics = null;
        Phase = "shutdown";
        var failures = new List<Exception>();
        try { await KernelServices.Services.ShutdownAsync(this, _gate.TraceClose); }
        catch (Exception error) { failures.Add(error); }
        _gate.TraceClose("shutdown-complete");
        foreach (var error in failures) DiagnosticLog.Error("[WP-01][phase=shutdown]", error);
        _gate.ReportShutdown();
        GetTree().Quit(failures.Count == 0 ? exitCode : 2);
    }
    private void Fail([FailureHandler("ENG-ARCH")] Exception error)
    {
        if (_failed) return;
        _failed = true;
        _ready = false;
        Failure = error.GetBaseException().Message;
        if (_flow is IAppBootFlow bootFlow) bootFlow.FailBoot(error);
        DiagnosticLog.Error($"[WP-01][phase={Phase}]", error);
        HoldFailure();
        // Automated runs must never turn an incomplete startup or a failed screenshot into success.
        if (_cpuCommand || AppBoot.Options.CaptureMode || !AppBoot.Options.Title) GetTree().Quit(2);
    }

    private void HoldFailure()
    {
        var loading = KernelServices.Services.Created.OfType<IAppLoadingPresentation>().SingleOrDefault();
        if (loading is not null)
        {
            try { _failureLoading ??= loading.Show(); }
            catch (Exception coverError) { DiagnosticLog.Error("[WP-01][phase=failure-cover]", coverError); }
        }
        var world = KernelServices.Services.Created.OfType<IWorldControl>().SingleOrDefault();
        if (world is not null)
        {
            try { _failureFlow ??= world.AcquireFlow(FlowPriority.AppFlow, 0); }
            catch (Exception flowError) { DiagnosticLog.Error("[WP-01][phase=failure-flow]", flowError); }
        }
    }

    Node IStageGateHost.Stage => this;
    IAppFlow IStageGateHost.Flow => _flow!;
    IMapRuntime IStageGateHost.MapRuntime => _mapRuntime!;
    ViewContentReadiness IStageGateHost.Readiness => _contentReadiness!;
    CancellationToken IStageGateHost.Lifetime => _captureLifetime.Token;
    bool IStageGateHost.Ready => _ready;
    bool IStageGateHost.Closing => _closing;
    void IStageGateHost.CheckActive() => CheckStartupActive();
    void IStageGateHost.SyncView() => _view!.Sync();
    Task IStageGateHost.Close() => CloseAsync();
    void IStageGateHost.Fail(Exception error) => Fail(error);
}
