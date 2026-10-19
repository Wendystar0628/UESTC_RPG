using System.Globalization;
using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Bake;
using Campus.Contracts.Maps;
using Campus.Contracts.Quality;
using Campus.Contracts.Save;
using Campus.Contracts.Settings;
using Campus.Contracts.Startup;
using Campus.Kernel.Boot;
using Campus.Kernel.Frame;
using Campus.Kernel.Inspect;
using Campus.Kernel.Registry;
using Campus.Kernel.Renderer;
using Godot;
using AppBoot = Campus.Kernel.Boot.Boot;
using Services = Campus.Kernel.Registry.Kernel;

namespace Campus.Stage;

/// <summary>The production composition root. Map data, world state and flow are supplied exclusively through services.</summary>
public partial class KernelStage : Node3D, IAppExit, IStageGateHost
{
    private StageCamera? _view;
    private readonly List<object> _runtimeObjects = [];
    private FrameStatistics? _statistics;
    private IAppFlow? _flow;
    private IMapRuntime? _mapRuntime;
    private IMapLifetime? _mapLifetime;
    private bool _ready;
    private bool _closing;
    private bool _cpuCommand;
    private bool _shutdownStarted;
    private Task? _shutdownTask;
    private readonly CancellationTokenSource _captureLifetime = new();
    private ViewContentReadiness? _contentReadiness;
    private IVeilLease? _failureVeil;
    private IFlowLease? _failureFlow;
    private readonly StageGateDriver _gate;
    private string _phase = "compose";
    public string Phase
    {
        get => _phase;
        private set
        {
            _phase = value;
            RegistryGateRunner.TraceBootPhase(value);
            GD.Print($"STARTUP_PHASE {value} {FrameClock.MonotonicMilliseconds:R}");
        }
    }
    public string? Failure { get; private set; }

    public KernelStage() => _gate = new(this);

    public override async void _Ready()
    {
        if (AppBoot.Failed) return;
        try
        {
            if (RegistryGateRunner.TryRun(this)) return;
            if (AppBoot.Options.CpuBakeMode) { await RunBakeCommand(); return; }
            Compose();
            await StartWorld();
        }
        catch (OperationCanceledException) when (_closing) { }
        catch (Exception error) { Fail(error); }
    }

    private void Compose()
    {
        var registry = Services.Services;
        ProcessPriority = FramePriority.Save + 1;
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
        registry.InstantiateServices();
        // Build the complete batch, attach every node, then invoke lifecycle hooks. An fx.* consumer
        // must not depend on its instance-pool host sorting ahead of it; _Ready only prepares structure.
        var globalNodes = registry.Entries.Where(entry => entry.Kind == "node" && string.IsNullOrEmpty(entry.Map))
            .OrderBy(entry => entry.Id, StringComparer.Ordinal)
            .Select(entry => registry.Create(entry.Type) as Node
                ?? throw new InvalidOperationException($"KernelNode must inherit Node: {entry.Id}."))
            .ToArray();
        foreach (var node in globalNodes) AddChild(node);
        if (globalNodes.Any(node => !node.IsInsideTree()))
            throw new InvalidOperationException("Every global kernel node must enter the tree before startup callbacks.");
        foreach (var entry in registry.Entries.Where(entry => string.IsNullOrEmpty(entry.Map) && entry.Kind is "node" or "service" or "save")
            .OrderBy(entry => entry.Id, StringComparer.Ordinal))
        {
            var instance = RuntimeComposition.Resolve(registry, entry);
            if (!_runtimeObjects.Any(item => ReferenceEquals(item, instance))) _runtimeObjects.Add(instance);
        }
        _flow = registry.Get<IAppFlow>();
        if (!ReferenceEquals(_flow, registry.Get<IAppBootFlow>()))
            throw new InvalidOperationException("IAppFlow and IAppBootFlow must resolve to the same instance.");
        _flow.Changed += OnFlowChanged;
        _mapRuntime = registry.Get<IMapRuntime>();
        _mapLifetime = registry.Get<IMapLifetime>();
        view.Bind(registry.Get<ICameraRig>());
        _statistics = new(GetViewport());
        _statistics.Enable();
        DisplayServer.WindowSetTitle("电光年华");
        GD.Print($"[WP-01][map=-][phase=compose] build={BuildInfo.Build}; global objects={_runtimeObjects.Count}");
    }

    private async Task StartWorld()
    {
        var registry = Services.Services;
        using var bootVeil = registry.Get<IVeilService>().Cover(VeilReason.Boot, 0,
            AppBoot.Options.Title ? VeilContent.Frontispiece : VeilContent.Fog);
        using var startupFlow = registry.Get<IWorldControl>().AcquireFlow(FlowPriority.Pause, 0);
        try { await StartWorld(bootVeil, startupFlow); }
        catch (OperationCanceledException) when (_closing) { throw; }
        catch
        {
            HoldFailure();
            throw;
        }
    }

    private async Task StartWorld(IVeilLease bootVeil, IFlowLease startupFlow)
    {
        var options = AppBoot.Options;
        var showTitle = options.Flow.ShowTitle;
        var registry = Services.Services;
        var participants = _runtimeObjects.Distinct(ReferenceEqualityComparer.Instance).OfType<IBootParticipant>().OrderBy(item => item.Order)
            .ThenBy(item => item.Id, StringComparer.Ordinal).ToArray();
        if (participants.GroupBy(item => item.Id, StringComparer.Ordinal).Any(group => group.Count() != 1))
            throw new InvalidOperationException("Boot participant IDs must be unique.");
        var context = CreateBootContext();
        var veil = registry.Get<IVeilService>();
        await bootVeil.Covered;
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        CheckStartupActive();
        if (!registry.Get<IVeilPresentation>().IsCovered)
            throw new InvalidOperationException("Startup has no fully opaque viewport veil.");
        var shaderCompilation = options.Title && !context.CaptureMode
            ? StartupShaderCompilation.Capture(registry.Get<ISettingsStore>()) : null;
        // One observation owns the whole boot preparation; nested save/map work must not start a 1.5 s scope.
        using var readinessWait = veil.ObserveReadinessWait(options.Title
            ? VeilWaitKind.BootFrontispiece : VeilWaitKind.Readiness,
            firstShaderCompilation: shaderCompilation?.FirstShaderCompilation == true);
        await Initialize(BootPhase.BeforeMap);
        Phase = "prepare-flow";
        var appBoot = registry.Get<IAppBootFlow>();
        var target = await appBoot.PrepareBoot(options.Flow);
        CheckStartupActive();
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
        Phase = "reveal";
        readinessWait.Dispose(); // Preparation ended; the authored reveal duration is not loading.
        shaderCompilation?.RecordSuccess();
        _statistics!.MarkRealtime();
        await bootVeil.Reveal(showTitle ? BootTuning.TitleRevealSeconds : BootTuning.WorldRevealSeconds);
        CheckStartupActive();
        await appBoot.CompleteBoot();
        if (_flow!.State == AppState.Title)
            GD.Print($"STARTUP_PHASE title-ready {FrameClock.MonotonicMilliseconds:R}");
        await _gate.RunNamedCinematic();
        _ready = true;
        Phase = "realtime";
        GD.Print($"[WP-01][map={maps.Current.Value}][phase=realtime] flow={_flow!.State}; startup complete");
        await _gate.OnRealtime();

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
        GD.Print($"STARTUP_PHASE input-ready {FrameClock.MonotonicMilliseconds:R} mode={Services.Get<ICameraRig>().Mode}");
    }

    private async Task RunBakeCommand()
    {
        _cpuCommand = true;
        Phase = "bake-command";
        // CPU batches deliberately bypass the visual composition root and map-travel warmup.
        // Only the requested command and its declared constructor dependencies are resolved.
        var command = Services.Get<IBakeCommand>();
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
        _statistics?.Sample();
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
        _failureVeil?.Dispose();
        _failureFlow?.Dispose();
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
        Phase = "shutdown-map";
        _gate.TraceClose(Phase);
        var failures = new List<Exception>();
        var textures = _runtimeObjects.OfType<IGlobalDataTextureRegistry>().SingleOrDefault();
        try
        {
            if (_mapLifetime is { } lifetime)
            {
                var maps = Services.Get<IMapService>();
                await lifetime.ShutdownAsync(maps.Context?.Descriptor.Id ?? maps.Current);
            }
        }
        catch (Exception error) { failures.Add(error); }

        // Saving has allowed departure and map consumers have drained. Only CPU bake bodies may outlive teardown.
        try { _runtimeObjects.OfType<Campus.Kernel.Bake.BakeService>().SingleOrDefault()?.StopForExit(); }
        catch (Exception error) { failures.Add(error); }

        Phase = "shutdown-nodes";
        _gate.TraceClose(Phase);
        var children = GetChildren();
        Node[] owned;
        // Array<Node> is not IDisposable; its explicit untyped conversion provides deterministic disposal.
        using (var nativeChildren = (Godot.Collections.Array)children)
            owned = children.ToArray();
        foreach (var node in owned)
        {
            try { node.SetProcess(false); node.SetPhysicsProcess(false); }
            catch (Exception error) { failures.Add(error); }
        }
        // Reverse attachment releases presentation consumers before the stage camera/environment.
        foreach (var node in owned.Reverse())
            try
            {
                RemoveChild(node); node.Free();
                _gate.TraceClose("node:" + node.GetType().Name);
            }
            catch (Exception error) { failures.Add(error); }
        _gate.TraceClose("shutdown-services");
        foreach (var instance in _runtimeObjects)
        {
            if (instance is GodotObject || ReferenceEquals(instance, textures) || instance is not IDisposable disposable) continue;
            try { disposable.Dispose(); } catch (Exception error) { failures.Add(error); }
        }
        _runtimeObjects.Clear();
        Phase = "shutdown-textures";
        _gate.TraceClose(Phase);
        try { if (textures is not null) await textures.ShutdownAsync(); }
        catch (Exception error) { failures.Add(error); }
        _gate.TraceClose("shutdown-complete");
        foreach (var error in failures) GD.PushError("[WP-01][phase=shutdown] " + error);
        _gate.ReportShutdown();
        GetTree().Quit(failures.Count == 0 ? exitCode : 2);
    }
    private void Fail(Exception error)
    {
        _ready = false;
        Failure = error.GetBaseException().Message;
        GD.PushError($"[WP-01][phase={Phase}] {error}");
        HoldFailure();
        // Automated runs must never turn an incomplete startup or a failed screenshot into success.
        if (_cpuCommand || AppBoot.Options.CaptureMode || !AppBoot.Options.Title) GetTree().Quit(2);
    }

    private void HoldFailure()
    {
        var veil = _runtimeObjects.OfType<IVeilService>().SingleOrDefault();
        if (veil is not null)
        {
            try { _failureVeil ??= veil.Cover(VeilReason.Boot, 0, AppBoot.Options.Title ? VeilContent.Frontispiece : VeilContent.Fog); }
            catch (Exception coverError) { GD.PushError($"[WP-01][phase=failure-cover] {coverError}"); }
        }
        var world = _runtimeObjects.OfType<IWorldControl>().SingleOrDefault();
        if (world is not null)
        {
            try { _failureFlow ??= world.AcquireFlow(FlowPriority.Cinematic, 0); }
            catch (Exception flowError) { GD.PushError($"[WP-01][phase=failure-flow] {flowError}"); }
        }
    }

    Node IStageGateHost.Stage => this;
    IAppFlow IStageGateHost.Flow => _flow!;
    IMapRuntime IStageGateHost.MapRuntime => _mapRuntime!;
    ViewContentReadiness IStageGateHost.Readiness => _contentReadiness!;
    CancellationToken IStageGateHost.Lifetime => _captureLifetime.Token;
    bool IStageGateHost.Ready => _ready;
    bool IStageGateHost.Closing => _closing;
    string IStageGateHost.Phase { set => Phase = value; }
    void IStageGateHost.CheckActive() => CheckStartupActive();
    void IStageGateHost.SyncView() => _view!.Sync();
    Task IStageGateHost.Close() => CloseAsync();
    void IStageGateHost.Fail(Exception error) => Fail(error);
}
