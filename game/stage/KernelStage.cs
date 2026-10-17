using System.Globalization;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Bake;
using Campus.Contracts.Maps;
using Campus.Contracts.Quality;
using Campus.Contracts.Save;
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
public partial class KernelStage : Node3D, IAppExit
{
    private StageCamera? _view;
    private readonly List<object> _runtimeObjects = [];
    private FrameStatistics? _statistics;
    private IAppFlow? _flow;
    private IMapRuntime? _mapRuntime;
    private bool _ready;
    private bool _closing;
    private bool _finishing;
    private bool _cpuCommand;
    private bool _shutdownStarted;
    private Task? _shutdownTask;
    private readonly CancellationTokenSource _captureLifetime = new();
    private ViewContentReadiness? _contentReadiness;
    private int _frames;
    private string _phase = "compose";
    public string Phase
    {
        get => _phase;
        private set
        {
            _phase = value;
            if (AppBoot.Value("gate") is "save-close" or "save-resume")
                GD.Print($"CORE_BOOT_PHASE {value} {FrameClock.MonotonicMilliseconds:R}");
        }
    }
    public string? Failure { get; private set; }

    public override async void _Ready()
    {
        if (AppBoot.Failed) return;
        try
        {
            if (AppBoot.Value("gate") == "registry") { RunRegistryGate(); return; }
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
        var ui = new CanvasLayer { Name = "Ui", Layer = 10, Visible = AppBoot.Value("ui", "1") == "1" };
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
        _mapRuntime = registry.Get<IMapRuntime>();
        view.Bind(registry.Get<ICameraRig>());
        _statistics = new(GetViewport());
        _statistics.Enable();
        DisplayServer.WindowSetTitle("电光年华");
        GD.Print($"[WP-01][map=-][phase=compose] build={BuildInfo.Build}; global objects={_runtimeObjects.Count}");
    }

    private async Task StartWorld()
    {
        var options = AppBoot.Options;
        var showTitle = options.Title && options.Value("cine") != "leave";
        var registry = Services.Services;
        var participants = _runtimeObjects.Distinct(ReferenceEqualityComparer.Instance).OfType<IBootParticipant>().OrderBy(item => item.Order)
            .ThenBy(item => item.Id, StringComparer.Ordinal).ToArray();
        if (participants.GroupBy(item => item.Id, StringComparer.Ordinal).Any(group => group.Count() != 1))
            throw new InvalidOperationException("Boot participant IDs must be unique.");
        var context = CreateBootContext();
        var veil = registry.Get<IVeilService>();
        await veil.Cover(VeilReason.Boot, 0, options.Title ? VeilContent.Frontispiece : VeilContent.Fog);
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        CheckStartupActive();
        if (!registry.Get<IVeilPresentation>().IsCovered)
            throw new InvalidOperationException("Startup has no fully opaque viewport veil.");
        // One observation owns the whole boot preparation; nested save/map work must not start a 1.5 s scope.
        using var readinessWait = veil.ObserveReadinessWait(options.Title
            ? VeilWaitKind.BootFrontispiece : VeilWaitKind.Readiness);
        await Initialize(BootPhase.BeforeMap);
        Phase = "prepare-flow";
        var appBoot = registry.Get<IAppBootFlow>();
        var target = await appBoot.PrepareBoot(new BootFlowRequest(
            ShowTitle: showTitle,
            Slot: options.Has("slot") ? new SlotId("s" + options.Value("slot").TrimStart('s')) : null,
            LoadFile: options.Has("load") ? options.Value("load") : null,
            Map: options.Has("map") ? new MapId(options.Value("map")) : null,
            Spawn: options.Has("spawn") ? options.Value("spawn") : null,
            Cinematic: options.Has("cine") ? options.Value("cine") : null,
            Seed: options.Values.TryGetValue("weather-seed", out var seed) ? uint.Parse(seed, CultureInfo.InvariantCulture)
                : options.Values.TryGetValue("seed", out seed) ? uint.Parse(seed, CultureInfo.InvariantCulture) : null));
        CheckStartupActive();
        var maps = registry.Get<IMapService>();
        int? initialFlow = null;
        if (!appBoot.WillRestoreWorld)
        {
            Phase = "initialize-world";
            var initialMap = maps.Maps.Single(map => map.Id == target);
            initialFlow = BootWorldOverrides.InitializeFreshWorld(options, registry, initialMap);
        }
        Phase = "travel-boot";
        // First classification measures the map's default overview (V1); PrepareWorld places T0 afterwards.
        var travel = maps.Travel(new MapTravelRequest(target, options.Value("spawn") is { Length: > 0 } spawn ? spawn : null,
            null, ViewMode.Fly, MapTravelCause.Boot));
        var travelled = await travel.Done;
        CheckStartupActive();
        if (travelled != MapTravelResult.Arrived) throw new InvalidOperationException("Boot map travel failed.");
        await Initialize(BootPhase.AfterMapWarm);
        await BootWorldOverrides.ApplyQuality(options, registry.Get<IQualityService>());
        CheckStartupActive();
        Phase = "prepare-world";
        // Preview can capture its desired resume rate, while the initial warmup itself stayed paused.
        if (initialFlow is { } flowRate) registry.Get<IWorldControl>().SetFlow(flowRate);
        await appBoot.PrepareWorld();
        CheckStartupActive();
        await BootWorldOverrides.ApplyView(options, registry, appBoot.WillRestoreWorld, showTitle);
        CheckStartupActive();
        _view!.Sync();
        _mapRuntime!.Prefetch(registry.Get<ICameraRig>().Pose, registry.Get<ICameraRig>().Mode);
        _mapRuntime.BeginWarm();
        await Initialize(BootPhase.BeforeReveal);
        _contentReadiness = ViewContentReadiness.Capture(this);
        Phase = "warm-final-view";
        await CaptureBarrier.WaitForReadyAsync(this, _contentReadiness, () =>
        {
            CheckStartupActive();
            if (_mapRuntime.Error is { } error) throw new InvalidOperationException(error);
            return _mapRuntime.ResidentReady && _mapRuntime.DestinationReady && _mapRuntime.WarmReady;
        });
        CheckStartupActive();
        Phase = "reveal";
        readinessWait.Dispose(); // Preparation ended; the authored reveal duration is not loading.
        _statistics!.MarkRealtime();
        await veil.Reveal(VeilReason.Boot, showTitle ? BootTuning.TitleRevealSeconds : BootTuning.WorldRevealSeconds);
        CheckStartupActive();
        await appBoot.CompleteBoot();
        if (options.Has("cine"))
        {
            Phase = "named-cinematic";
            await RunRequestedCinematic(options.Value("cine"));
            CheckStartupActive();
        }
        _ready = true;
        Phase = "realtime";
        GD.Print($"[WP-01][map={maps.Current.Value}][phase=realtime] flow={_flow!.State}; startup complete");
        if (options.Value("gate") is "save-close" or "save-resume")
        {
            await RunCoreLifecycle(options.Value("gate"));
            return;
        }
        if (!options.Has("gate") && options.Has("shots") && !options.Has("frames")) { _finishing = true; _ = FinishRun(); }

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

    private async Task RunRequestedCinematic(string name)
    {
        var saves = Services.Get<ISaveService>();
        switch (name)
        {
            case "enter_new":
            case "enter_new_short":
                var options = AppBoot.Options;
                SlotId? selected = options.Has("save") && options.Value("save") != "off"
                    ? new SlotId("s" + options.Value("save"))
                    : options.Has("slot") ? new SlotId("s" + options.Value("slot").TrimStart('s')) : null;
                var slot = selected ?? saves.Slots.FirstOrDefault(info => info.Health == SlotHealth.Empty)?.Slot
                    ?? throw new InvalidOperationException("Named new-game playback requires an empty or explicitly selected slot.");
                if (await _flow!.Enter(new(EnterKind.NewGame, slot)) != EnterResult.Entered)
                    throw new InvalidOperationException("Named new-game playback failed.");
                break;
            case "enter_continue":
                if (await _flow!.Enter(new(EnterKind.Continue, saves.Active ?? saves.Continue)) != EnterResult.Entered)
                    throw new InvalidOperationException("Named continue playback requires a readable slot.");
                break;
            case "leave":
                await _flow!.ReturnToTitle();
                if (_flow.State != AppState.Title) throw new InvalidOperationException("Named return-to-title playback failed.");
                break;
            default: throw new ArgumentException("Unknown named cinematic: " + name);
        }
    }

    private void CheckStartupActive()
    {
        if (_closing || _shutdownStarted) throw new OperationCanceledException("Startup stopped by the exit request.");
    }

    private void RunRegistryGate()
    {
        var report = RegistryGateRunner.Run(Services.Services, ProjectSettings.GetSetting("campus/maps/index").AsString(),
            AppBoot.Value("map"), Read, [typeof(IRuleset), typeof(IRulesetActivation), typeof(PaletteCatalog), typeof(IAppExit), typeof(Camera3D), typeof(WorldEnvironment), typeof(Godot.Environment), typeof(CanvasLayer)]);
        GD.Print("G5_REGISTRY_RESULT " + JsonSerializer.Serialize(report, ContractJson.Options));
        GetTree().Quit();
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
        return new(options.Values, options.ExplicitArguments,
            options.Value("save-dir", Path.Combine(OS.GetUserDataDir(), "saves")), BuildInfo.Build, options.CaptureMode);
    }

    public override void _Process(double delta)
    {
        _statistics?.Sample();
        if (!_ready || _closing || _finishing) return;
        _frames++;
        if (int.TryParse(AppBoot.Value("save-at"), NumberStyles.None, CultureInfo.InvariantCulture, out var saveAt) && _frames == saveAt)
            Services.Get<ISaveService>().Request(SaveReason.Test);
        if (!AppBoot.Options.Has("gate") && int.TryParse(AppBoot.Value("frames"), NumberStyles.None, CultureInfo.InvariantCulture, out var limit) && _frames >= limit)
        { _finishing = true; _ = FinishRun(); }
    }

    private async Task FinishRun()
    {
        try
        {
            if (AppBoot.Options.Has("shots"))
                await CaptureSequence.RunAsync(this, _contentReadiness!, AppBoot.Options, _view!.Sync, () =>
                {
                    CheckStartupActive();
                    return _ready &&
                        _mapRuntime!.ResidentReady && _mapRuntime.DestinationReady && _mapRuntime.WarmReady;
                }, _captureLifetime.Token, pose =>
                {
                    if (!Services.Get<ICameraPoseValidator>().IsLegal(pose, outsideBuffer: true))
                        throw new ArgumentException("Capture pose is outside the reachable space or inside its buffer.");
                });
            await CloseAsync();
        }
        catch (OperationCanceledException) when (_closing) { }
        catch (Exception error) { Fail(error); }
    }
    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest)
        {
            if (_coreAwaitingClose) { _coreAwaitingClose = false; _coreCloseAt = FrameClock.MonotonicMilliseconds; }
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
    public override void _ExitTree() { _closing = true; _captureLifetime.Cancel(); }

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
        TraceCoreClose(Phase);
        var failures = new List<Exception>();
        var textures = _runtimeObjects.OfType<IGlobalDataTextureRegistry>().SingleOrDefault();
        try
        {
            if (_mapRuntime is MapRuntime runtime) runtime.StopNewWork();
            if (_mapRuntime is not null && !_mapRuntime.IsUnloaded)
            {
                var maps = Services.Get<IMapService>();
                _mapRuntime.Unload(maps.Context?.Descriptor.Id ?? maps.Current);
                while (!_mapRuntime.IsUnloaded)
                    await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                if (_mapRuntime.Error is { } error) throw new InvalidOperationException("Map shutdown failed: " + error);
            }
        }
        catch (Exception error) { failures.Add(error); }

        Phase = "shutdown-nodes";
        TraceCoreClose(Phase);
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
                TraceCoreClose("node:" + node.GetType().Name);
            }
            catch (Exception error) { failures.Add(error); }
        TraceCoreClose("shutdown-services");
        foreach (var instance in _runtimeObjects)
        {
            if (instance is GodotObject || ReferenceEquals(instance, textures) || instance is not IDisposable disposable) continue;
            try { disposable.Dispose(); } catch (Exception error) { failures.Add(error); }
        }
        _runtimeObjects.Clear();
        Phase = "shutdown-textures";
        TraceCoreClose(Phase);
        try { if (textures is not null) await textures.ShutdownAsync(); }
        catch (Exception error) { failures.Add(error); }
        TraceCoreClose("shutdown-complete");
        foreach (var error in failures) GD.PushError("[WP-01][phase=shutdown] " + error);
        if (_coreCloseAt is { } closeAt)
            GD.Print($"CORE_SHUTDOWN_MS {FrameClock.MonotonicMilliseconds - closeAt:R}");
        GetTree().Quit(failures.Count == 0 ? exitCode : 2);
    }
    private void Fail(Exception error)
    {
        _ready = false;
        Failure = error.GetBaseException().Message;
        GD.PushError($"[WP-01][phase={Phase}] {error}");
        var veil = _runtimeObjects.OfType<IVeilService>().SingleOrDefault();
        if (veil is not null)
        {
            try { _ = veil.Cover(VeilReason.Boot, 0, AppBoot.Options.Title ? VeilContent.Frontispiece : VeilContent.Fog); }
            catch (Exception coverError) { GD.PushError($"[WP-01][phase=failure-cover] {coverError}"); }
        }
        // Automated runs must never turn an incomplete startup or a failed screenshot into success.
        if (_cpuCommand || AppBoot.Options.CaptureMode || !AppBoot.Options.Title) GetTree().Quit(2);
    }
    private static byte[] Read(string path)
    {
        var bytes = Godot.FileAccess.GetFileAsBytes(path);
        if (bytes.Length == 0) throw new FileNotFoundException($"Missing required data: {path}");
        return bytes;
    }
}
