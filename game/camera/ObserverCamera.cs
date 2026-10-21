using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Maps;
using Campus.Contracts.Save;
using Campus.Contracts.Settings;
using Godot;
using AppBoot = Campus.Kernel.Boot.Boot;
using KernelServices = Campus.Kernel.Registry.Kernel;
using N = System.Numerics;

namespace Campus.Camera;

[KernelService(typeof(ICameraRig))]
[KernelService(typeof(IObserverState))]
[KernelService(typeof(IViewControl))]
[KernelService(typeof(IInputRouter))]
[KernelService(typeof(ICameraPoseValidator))]
[KernelNode("observer-camera")]
[SaveSection("view", Version = 1, Phase = RestorePhase.Pose, Order = 60)]
public partial class ObserverCamera : Node, ICameraRig, IObserverState, IViewControl, IInputRouter,
    ICameraPoseValidator, IMapAware, IMapDataParticipant, ISaveSection<ViewSaveData>
{
    private readonly ObserverPresentation _presentation;
    private readonly ObserverFlight _flight;
    private readonly ObserverControls _controls;
    private IAppFlow? _flow;
    private IRuleset? _rules;
    private IWalkController? _walk;
    private ITransitDriver? _transit;
    private ViewMode? _mode;
    private bool _recoverTriggered;
    private bool _worldResetPending = true;
    private IWorldState? _world;
    private TaskCompletionSource<ModeSwitchResult>? _walkEntry;
    private ObserverListener? _listener;

    public event Action? WalkEntryRefused;
    public FlightTuning Tuning { get; } = new();
    internal bool IsPlaying => _flow?.IsPlaying == true;

    public ObserverCamera()
    {
        _presentation = new(Tuning);
        _flight = new(Tuning);
        _controls = new(this, Tuning);
        ProcessPriority = FramePriority.Camera;
    }

    public override void _Ready()
    {
        _flow = KernelServices.Get<IAppFlow>();
        _rules = KernelServices.Get<IRuleset>();
        _world = KernelServices.Get<IWorldState>();
        _flight.Initialize(KernelServices.Get<ITerrainPreparation>(), AppBoot.Options.Automated);
        _world.InstanceChanged += OnWorldInstanceChanged;
        if (KernelServices.Services.Query<IWalkController>(RegistrationKind.Service).Any(entry => !entry.IsStub))
            _walk = KernelServices.Get<IWalkController>();
        _transit = KernelServices.Get<ITransitDriver>();
        var settings = AppBoot.Options.Automated ? null : KernelServices.Get<ISettingsStore>();
        _controls.Initialize(settings, AppBoot.Options.Automated);
        _presentation.Bind(settings?.Section("ui"));
        Input.UseAccumulatedInput = false;
        _flow.Changed += OnFlowChanged;
        _listener = new ObserverListener { Observer = this };
        AddChild(_listener);
        SetInterfaceMode(true);
    }

    public override void _EnterTree()
    {
        if (_world is null) return;
        _world.InstanceChanged += OnWorldInstanceChanged;
        if (_flow is not null) _flow.Changed += OnFlowChanged;
        _worldResetPending = true;
    }

    public override void _ExitTree()
    {
        CancelWalkEntry();
        if (_flow is not null) _flow.Changed -= OnFlowChanged;
        if (_world is not null) _world.InstanceChanged -= OnWorldInstanceChanged;
        Input.MouseMode = Input.MouseModeEnum.Visible;
    }

    public override void _Process(double delta)
    {
        float seconds = (float)Math.Min(delta, Tuning.MaxFrameStep);
        if (!_flight.AdvanceFlowProjection(_presentation)) return;
        if (_worldResetPending)
        {
            _worldResetPending = false;
            ResetHistory();
            _flight.SetEdgeFog(0);
        }
        _controls.State.Advance(seconds);
        if (AdvanceWalkEntry()) { _flight.Reset(); _flight.SetEdgeFog(0); return; }
        if (!IsPlaying || _mode is null) { _flight.Reset(); _flight.SetEdgeFog(0); return; }
        if (Mode == ViewMode.Fly && ActiveTransit is null) _presentation.AdvanceFlyFov(seconds);
        if (Mode == ViewMode.Walk && _walk is { ExitRequested: true })
        {
            _ = SetMode(ViewMode.Fly, ModeSwitchVia.Key);
            return;
        }
        if (ActiveTransit is not null)
        {
            if (!ActiveTransit.Done.IsCompleted) { _flight.Reset(); _flight.SetEdgeFog(0); return; }
            ActiveTransit = null;
        }
        switch (Mode)
        {
            case ViewMode.Fly:
                if (_flight.Map is null) return;
                if (_controls.Locked) { _flight.Reset(); return; }
                _flight.Advance(Pose, _controls.State, seconds, _presentation);
                break;
            case ViewMode.Walk:
                AdvanceWalkInput();
                break;
            default: throw new InvalidOperationException("Unknown view mode.");
        }
    }

    private void AdvanceWalkInput()
    {
        _flight.SetEdgeFog(0);
        if (_walk is null)
            throw new InvalidOperationException("Walk mode requires the restored game/camera/walk/ controller.");
        var input = _controls.State;
        _walk.SetInput(_controls.Locked ? default : new WalkInput(input.Move, input.Sprint,
            input.JumpHeld, input.TakeJump(), input.Crouch));
        if (!_controls.Locked && input.RecoverHeld >= Tuning.ReturnHoldSeconds && !_recoverTriggered)
        {
            _recoverTriggered = true;
            if (!_walk.ReturnToSafety()) _ = SetMode(ViewMode.Fly, ModeSwitchVia.Key);
        }
        if (input.RecoverHeld == 0) _recoverTriggered = false;
    }

    public void OnMapLoaded(IMapContext context)
    {
        _flight.Mount(context);
        // MapService owns destination selection and applies its resolved pose after these callbacks.
        // Loading a map must not replace a preview/Title pose with its default spawn.
        ResetHistory();
        _flight.SetEdgeFog(0);
    }

    public Task Prepare(IMapContext context) => _flight.Prepare(context, _presentation.Value);
    public Task UnloadAsync(MapId from) => Task.CompletedTask;

    public void OnMapUnloading(MapId from)
    {
        CancelWalkEntry();
        CancelTransitForInput();
        ResetHistory();
        _flight.Unmount();
    }

    private void OnWorldInstanceChanged(long instance)
    {
        CancelWalkEntry();
        _worldResetPending = true;
    }

    private void OnFlowChanged(AppState previous, AppState next)
    {
        ClearInputMotion();
        SetInterfaceMode(true);
        _flight.SetEdgeFog(0);
        if (IsPlaying && Mode == ViewMode.Fly) _flight.BeginFlowProjection(Pose);
    }

    private void ResetHistory()
    {
        CancelTransitForInput();
        _flight.Reset();
        _controls.State.Clear();
        _walk?.SetInput(default);
        _flight.DiscardMotionProjection();
        _recoverTriggered = false;
        _controls.ResetWheel();
        _listener?.Sync(reset: true);
    }

    public float RollDeg => IsPlaying && _mode == ViewMode.Walk ? _walk?.EyeRollDeg ?? 0 : 0;
    // Application flow owns a presentation pose while the restored walk body stays frozen.
    public Pose Pose => !IsPlaying || _mode is null ? _presentation.Value : Mode switch
    {
        ViewMode.Fly => _presentation.Value,
        ViewMode.Walk => _walk?.EyePose ?? throw new InvalidOperationException("Walk mode requires its physical eye pose."),
        _ => throw new InvalidOperationException("Unknown view mode.")
    };
    public float Speed => _mode switch
    {
        null => 0,
        ViewMode.Fly => _flight.Velocity.Length(),
        ViewMode.Walk => _walk?.Velocity.Length() ?? 0,
        _ => throw new InvalidOperationException("Unknown view mode.")
    };
    // Early UI reads can precede _Ready; the ruleset is loaded before any runtime node.
    // The nullable field still prevents input/physics from treating this as an applied mode.
    public ViewMode Mode => _mode ?? (_rules ?? Campus.Kernel.Registry.Kernel.Get<IRuleset>()).Current.DefaultViewMode;
    public int SpeedStep => _mode is null ? Mode switch
    {
        ViewMode.Fly => Tuning.DefaultSpeedStep,
        ViewMode.Walk => WalkRules.DefaultSpeedStep,
        _ => throw new InvalidOperationException("Unknown default view mode.")
    } : Mode switch
    {
        ViewMode.Fly => _flight.SpeedStep,
        ViewMode.Walk => _walk?.SpeedStep ?? throw new InvalidOperationException("Walk mode requires its speed setting."),
        _ => throw new InvalidOperationException("Unknown view mode.")
    };
    public ITransitHandle? ActiveTransit { get; private set; }
    public float EdgeFog => _flight.EdgeFog;
    public N.Vector3 Feet => _mode switch
    {
        null => _presentation.Value.Position,
        ViewMode.Fly => Pose.Position,
        ViewMode.Walk => _walk?.BodyPose.Position ?? Pose.Position,
        _ => throw new InvalidOperationException("Unknown view mode.")
    };
    public N.Vector3 Eye => _mode switch
    {
        null => _presentation.Value.Position,
        ViewMode.Fly => Pose.Position,
        ViewMode.Walk => _walk?.ObserverEye ?? Pose.Position,
        _ => throw new InvalidOperationException("Unknown view mode.")
    };
    public float YawDeg => _mode switch
    {
        null => _presentation.Value.YawDeg,
        ViewMode.Fly => Pose.YawDeg,
        ViewMode.Walk => _walk?.BodyPose.YawDeg ?? Pose.YawDeg,
        _ => throw new InvalidOperationException("Unknown view mode.")
    };
    public NavSpace? Space => _mode switch
    {
        null => null,
        ViewMode.Fly => _flight.Map?.World.Spatial.MediumAt(Pose.Position) is { Kind: MediumKind.Interior, Feature: { } feature } medium
            ? new NavSpace(feature, checked((short)medium.Storey)) : null,
        ViewMode.Walk => _walk?.Space,
        _ => throw new InvalidOperationException("Unknown view mode.")
    };
    public bool IsSettled => _mode is not null && ActiveTransit is null && _walkEntry is null
        && _flight.IsSettled && (Mode switch
    {
        ViewMode.Fly => true,
        ViewMode.Walk => _walk?.IsSettled ?? false,
        _ => throw new InvalidOperationException("Unknown view mode.")
    });

    public void SetPose(Pose pose)
    {
        ObserverPresentation.ValidateFinite(pose);
        _flight.ClearProjection();
        _presentation.Set(pose, pose.FovDeg ?? _presentation.DefaultFov(Mode));
        _flight.Reset();
        _listener?.Sync(reset: true);
    }

    public Task<Pose> LegalizePrepared(Pose pose, CancellationToken cancellation = default) =>
        _flight.LegalizePrepared(pose, cancellation);
    public bool IsLegal(Pose pose, bool outsideBuffer = false) => _flight.IsLegal(pose, outsideBuffer);

    public ITransitHandle Transit(Pose to, TransitOptions? options = null)
    {
        if (_transit is null || !_transit.IsReady)
            throw new InvalidOperationException("The production transit driver is not ready.");
        ObserverPresentation.ValidateFinite(to);
        ActiveTransit?.Cancel();
        _controls.State.Clear(); _flight.Reset();
        ActiveTransit = _transit.Start(Pose, to, options);
        return ActiveTransit;
    }

    public Task<ModeSwitchResult> SetMode(ViewMode mode, ModeSwitchVia via)
    {
        _ = mode switch { ViewMode.Fly or ViewMode.Walk => 0, _ => throw new ArgumentOutOfRangeException(nameof(mode)) };
        if (_walkEntry is not null)
        {
            CancelWalkEntry();
            return Task.FromResult(ModeSwitchResult.Refused);
        }
        if (_rules is null || !_rules.Current.ViewModes.Contains(mode)) return Task.FromResult(ModeSwitchResult.Refused);
        if (mode == _mode) return Task.FromResult(ModeSwitchResult.Ok);
        ActiveTransit?.Cancel(); ActiveTransit = null;
        switch (mode)
        {
            case ViewMode.Walk:
                if (_walk is null) return Task.FromResult(ModeSwitchResult.Refused);
                var result = _walk.Enter(Pose);
                if (result == ModeSwitchResult.Refused)
                { WalkEntryRefused?.Invoke(); return Task.FromResult(result); }
                _walkEntry = new(TaskCreationOptions.RunContinuationsAsynchronously);
                _flight.Reset(); _controls.State.Clear(); _flight.SetEdgeFog(0);
                return _walkEntry.Task;
            case ViewMode.Fly:
                return SwitchToFly();
            default: throw new ArgumentOutOfRangeException(nameof(mode));
        }
    }

    private async Task<ModeSwitchResult> SwitchToFly()
    {
        bool landingRefused = _walk?.ExitRequested == true && !PlacementReadFailed();
        var map = _flight.Map;
        var origin = _walk is { Active: true } ? _walk.EyePose : Pose;
        var pose = await LegalizePrepared(origin);
        if (!ReferenceEquals(map, _flight.Map)) return ModeSwitchResult.Refused;
        N.Vector3 inheritedVelocity = _walk is { Active: true } body ? new(body.Velocity.X, 0, body.Velocity.Z) : default;
        _walk?.Leave();
        _presentation.BeginFly(pose);
        _mode = ViewMode.Fly; _flight.Reset(); _controls.State.Clear(); _flight.SetEdgeFog(0);
        _flight.ConstrainVelocity(inheritedVelocity);
        if (landingRefused) WalkEntryRefused?.Invoke();
        return ModeSwitchResult.Ok;
    }

    private bool AdvanceWalkEntry()
    {
        if (_walkEntry is not { } completion || _walk is null) return false;
        _walk.SetLook(_presentation.Value.YawDeg, _presentation.Value.PitchDeg);
        if (_walk.ExitRequested)
        {
            bool readFailed = PlacementReadFailed();
            _walkEntry = null;
            _walk.Leave();
            completion.TrySetResult(ModeSwitchResult.Refused);
            if (!readFailed) WalkEntryRefused?.Invoke();
            return false;
        }
        if (!_walk.CollisionReady) return true;
        _walkEntry = null;
        _mode = ViewMode.Walk;
        _flight.Reset(); _flight.SetEdgeFog(0);
        bool relocated = _walk.BodyPose.Position.X != _presentation.Value.Position.X || _walk.BodyPose.Position.Z != _presentation.Value.Position.Z;
        completion.TrySetResult(relocated ? ModeSwitchResult.Relocated : ModeSwitchResult.Ok);
        return false;
    }

    private void CancelWalkEntry()
    {
        if (_walkEntry is not { } completion) return;
        _walkEntry = null;
        _walk?.Leave();
        completion.TrySetResult(ModeSwitchResult.Refused);
    }

    private bool PlacementReadFailed() => _walk?.PlacementArrival?.Done is { IsFaulted: true } or { IsCanceled: true };

    public override void _Input(InputEvent input) => _controls.ObserveReleases(input);
    public override void _UnhandledInput(InputEvent input)
    {
        if (_controls.HandleUnhandled(input, _flow)) GetViewport().SetInputAsHandled();
    }

    public override void _Notification(int what)
    {
        if (what != NotificationApplicationFocusOut) return;
        ClearInputMotion();
        SetInterfaceMode(true);
    }

    public IDisposable RegisterLayer(string owner, int priority, Func<InputActionEvent, bool> handler) =>
        _controls.RegisterLayer(owner, priority, handler);
    public IDisposable LockInput(InputLockReason reason) => _controls.LockInput(reason);
    public bool InterfaceMode => _controls.InterfaceMode;
    public event Action<bool>? InterfaceModeChanged
    {
        add => _controls.InterfaceModeChanged += value;
        remove => _controls.InterfaceModeChanged -= value;
    }
    public void SetInterfaceMode(bool enabled) => _controls.SetInterfaceMode(enabled);
    public bool TryRebind(string action, int physicalKey, out string? conflict) =>
        _controls.TryRebind(action, physicalKey, out conflict);
    public int PhysicalKey(string action) => _controls.PhysicalKey(action);
    public void SaveBindings() => _controls.SaveBindings();

    internal void ClearInputMotion()
    {
        _controls.State.Clear();
        _flight.Reset();
        _walk?.SetInput(default);
    }

    internal void ResetFlightMotion() => _flight.Reset();

    internal void ApplyLook(Pose pose)
    {
        switch (Mode)
        {
            case ViewMode.Fly: _presentation.Look(pose); break;
            case ViewMode.Walk: _walk?.SetLook(pose.YawDeg, pose.PitchDeg); break;
            default: throw new InvalidOperationException("Unknown view mode.");
        }
    }

    internal bool CanDoubleSpace() => _flight.Map?.World.Spatial.MediumAt(Pose.Position).Kind != MediumKind.Underwater
        && (Mode switch
        {
            ViewMode.Fly => true,
            ViewMode.Walk => _walk is { CollisionReady: true },
            _ => throw new InvalidOperationException("Unknown view mode.")
        });

    internal void ChangeSpeed(int steps)
    {
        switch (Mode)
        {
            case ViewMode.Fly: _flight.ChangeSpeed(steps); break;
            case ViewMode.Walk:
                _walk?.SetSpeedStep(Math.Clamp(SpeedStep + steps, 0, WalkRules.SpeedStepCount - 1)); break;
            default: throw new InvalidOperationException("Unknown view mode.");
        }
    }

    internal void CancelTransitForInput()
    {
        ActiveTransit?.Cancel();
        ActiveTransit = null;
    }

    public System.Text.Json.Serialization.Metadata.JsonTypeInfo<ViewSaveData> TypeInfo => ViewJsonContext.Contract.ViewSaveData;

    public ViewSaveData Capture(in CaptureContext context) =>
        ObserverViewSave.Capture(this, _flight.SpeedStep, _transit, _walk, _flight.Map);

    public void Restore(ViewSaveData data, RestoreContext context)
    {
        ViewMode mode = ObserverViewSave.ValidateRestore(data, _rules, _walk);
        CancelTransitForInput();
        ResetHistory();
        _mode = mode;
        int flyStep = ObserverViewSave.FlySpeed(data, mode);
        _flight.SetSpeedStep(flyStep);
        if (_flight.SpeedStep != flyStep) context.Repair(mode == ViewMode.Fly ? "view.speedStep" : "view.flySpeedStep");
        if (mode == ViewMode.Fly) _walk?.Leave();
        SetPose(data.Pose);
        _flight.SetEdgeFog(0);
    }

    public ViewSaveData CreateDefault(NewGameContext context) => ObserverViewSave.CreateDefault(context, Tuning);
}
