using System.Text.Json.Serialization.Metadata;
using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Maps;
using Campus.Contracts.Save;
using Godot;
using N = System.Numerics;
using AppBoot = Campus.Kernel.Boot.Boot;
using Services = Campus.Kernel.Registry.Kernel;

namespace Campus.Camera;

[KernelService(typeof(ICameraRig), Owner = "WP-06")]
[KernelService(typeof(IObserverState), Owner = "WP-06")]
[KernelService(typeof(IViewControl), Owner = "WP-06")]
[KernelService(typeof(IInputRouter), Owner = "WP-06")]
[KernelService(typeof(ICameraPoseValidator), Owner = "WP-06")]
[KernelService(typeof(ISkipInputState), Owner = "WP-06")]
[KernelNode("observer-camera", Owner = "WP-06")]
[SaveSection("view", Owner = "WP-06", Version = 1, Phase = RestorePhase.Pose, Order = 60)]
public partial class ObserverCamera : Node, ICameraRig, IObserverState, IViewControl, IInputRouter, ISkipInputState,
    ICameraPoseValidator, IMapAware, ISaveSection<ViewSaveData>
{
    private readonly InputLocks _locks = new();
    private readonly InputBindings _bindings = new();
    private readonly FlightMotion _flight;
    private readonly ObserverInputState _input;
    private IMapContext? _map;
    private FlightBounds? _bounds;
    private IAppFlow? _flow;
    private IRuleset? _rules;
    private ISaveService? _saves;
    private IWalkController? _walk;
    private ITransitDriver? _transit;
    private Pose _pose;
    private ViewMode _mode = ViewMode.Fly;
    private int _speedStep;
    private int _flySpeedStep;
    private int _walkSpeedStep = 2;
    private bool _recoverTriggered;
    private bool _automated;
    private long _worldInstance = long.MinValue;
    private IWorldState? _world;
    private Pose? _transitDestination;
    private ObserverListener? _listener;

    public FlightTuning Tuning { get; } = new();
    public Pose Pose => _pose;
    public float Speed => _mode switch
    {
        ViewMode.Fly => _flight.Velocity.Length(),
        ViewMode.Walk => _walk?.Velocity.Length() ?? 0,
        _ => throw new InvalidOperationException("Unknown view mode.")
    };
    public ViewMode Mode => _mode;
    public int SpeedStep => _speedStep;
    public ITransitHandle? ActiveTransit { get; private set; }
    public float EdgeFog { get; private set; }
    public N.Vector3 Feet => Mode switch
    {
        ViewMode.Fly => Pose.Position,
        ViewMode.Walk => _walk?.BodyPose.Position ?? Pose.Position,
        _ => throw new InvalidOperationException("Unknown view mode.")
    };
    public N.Vector3 Eye => Mode switch
    {
        ViewMode.Fly => Pose.Position,
        ViewMode.Walk => _walk?.ObserverEye ?? Pose.Position,
        _ => throw new InvalidOperationException("Unknown view mode.")
    };
    public float YawDeg => Mode switch
    {
        ViewMode.Fly => Pose.YawDeg,
        ViewMode.Walk => _walk?.BodyPose.YawDeg ?? Pose.YawDeg,
        _ => throw new InvalidOperationException("Unknown view mode.")
    };
    public NavSpace? Space => Mode switch
    {
        ViewMode.Fly => _map?.World.Spatial.MediumAt(Pose.Position) is { Kind: MediumKind.Interior, Feature: { } feature } medium
            ? new NavSpace(feature, checked((short)medium.Storey)) : null,
        ViewMode.Walk => _walk?.Space,
        _ => throw new InvalidOperationException("Unknown view mode.")
    };
    public bool IsSettled => ActiveTransit is null && (Mode switch
    {
        ViewMode.Fly => true,
        ViewMode.Walk => _walk?.IsSettled ?? false,
        _ => throw new InvalidOperationException("Unknown view mode.")
    });
    public JsonTypeInfo<ViewSaveData> TypeInfo => ViewJsonContext.Contract.ViewSaveData;

    public ObserverCamera()
    {
        _flight = new(Tuning); _input = new(Tuning);
        _speedStep = _flySpeedStep = Tuning.DefaultSpeedStep;
        ProcessPriority = FramePriority.Camera;
    }

    public override void _Ready()
    {
        _flow = Services.Get<IAppFlow>(); _rules = Services.Get<IRuleset>();
        _saves = Services.Get<ISaveService>(); _world = Services.Get<IWorldState>();
        if (Services.Services.Entries.Any(e => e.Kind == "service" && e.Id == typeof(IWalkController).FullName && !e.IsStub))
            _walk = Services.Get<IWalkController>();
        _transit = Services.Get<ITransitDriver>();
        _automated = new[] { "gate", "shots", "bench", "frames", "inspect" }.Any(AppBoot.Arguments.ContainsKey);
        _bindings.Initialize(!_automated);
        Input.UseAccumulatedInput = false;
        _flow.Changed += OnFlowChanged;
        _listener = new ObserverListener { Observer = this };
        AddChild(_listener);
        SetInterfaceMode(true);
    }

    public override void _ExitTree()
    {
        if (_flow is not null) _flow.Changed -= OnFlowChanged;
        Input.MouseMode = Input.MouseModeEnum.Visible;
    }

    public override void _Process(double delta)
    {
        float seconds = (float)Math.Min(delta, Tuning.MaxFrameStep);
        if (_world is not null && _world.Instance != _worldInstance)
        { _worldInstance = _world.Instance; ResetHistory(); SetEdgeFog(0); }
        AdvanceInput(seconds);
        if (!IsPlaying) { _flight.Reset(); SetEdgeFog(0); return; }
        if (ActiveTransit is not null)
        {
            if (!ActiveTransit.Done.IsCompleted) { _flight.Reset(); SetEdgeFog(0); return; }
            ActiveTransit = null; _transitDestination = null;
        }
        switch (Mode)
        {
            case ViewMode.Fly:
                if (_map is null || _bounds is null) return;
                if (_locks.Locked) { _flight.Reset(); return; }
                var move = _input.Move;
                var displacement = _flight.Step(Pose, new(move.X, _input.Vertical, move.Y), _input.Sprint,
                    _speedStep, _map.World.Spatial.GroundY(Pose.Position.X, Pose.Position.Z), seconds);
                var filtered = _bounds.FilterVelocity(Pose.Position, displacement, out float fog);
                _flight.ConstrainVelocity(_bounds.FilterVelocity(Pose.Position, _flight.Velocity, out _));
                var position = _bounds.Project(Pose.Position + filtered);
                _pose = Pose with { Position = position };
                var medium = _map.World.Spatial.MediumAt(position).Kind;
                if (_automated || medium is MediumKind.Interior or MediumKind.Underground or MediumKind.Underwater) fog = 0;
                SetEdgeFog(fog + (EdgeFog - fog) * MathF.Exp(-seconds / Tuning.FogTimeConstant));
                break;
            case ViewMode.Walk:
                SetEdgeFog(0);
                if (_walk is null)
                    throw new InvalidOperationException("Walk mode requires the restored WP-42 controller.");
                if (!_walk.Active)
                {
                    _ = SetMode(ViewMode.Fly, ModeSwitchVia.Key);
                    return;
                }
                _walk.SetInput(_locks.Locked ? default : new WalkInput(_input.Move, _input.Sprint,
                    _input.JumpHeld, _input.TakeJump(), _input.Crouch));
                _pose = _walk.EyePose;
                if (!_locks.Locked && _input.RecoverHeld >= Tuning.ReturnHoldSeconds && !_recoverTriggered)
                {
                    _recoverTriggered = true;
                    if (!_walk.ReturnToSafety()) _ = SetMode(ViewMode.Fly, ModeSwitchVia.Key);
                }
                if (_input.RecoverHeld == 0) _recoverTriggered = false;
                break;
            default: throw new InvalidOperationException("Unknown view mode.");
        }
    }

    public void SetPose(Pose pose)
    {
        ValidateFinite(pose);
        _pose = pose with { FovDeg = pose.FovDeg ?? Tuning.FlyFov };
        _flight.Reset();
        _listener?.Sync(reset: true);
    }

    public IDisposable LockInput(InputLockReason reason)
    {
        _input.Clear(); _flight.Reset(); _walk?.SetInput(default);
        var lease = _locks.Acquire(reason);
        SetInterfaceMode(true);
        return lease;
    }

    public ITransitHandle Transit(Pose to, TransitOptions? options = null)
    {
        if (_transit is null || !_transit.IsReady)
            throw new InvalidOperationException("The production transit driver is not ready.");
        ValidateFinite(to);
        if (!IsLegal(to)) throw new ArgumentOutOfRangeException(nameof(to), "Transit destination is outside the reachable volume.");
        ActiveTransit?.Cancel();
        _input.Clear(); _flight.Reset();
        _transitDestination = to;
        ActiveTransit = _transit.Start(Pose, to, options);
        return ActiveTransit;
    }

    public Task<ModeSwitchResult> SetMode(ViewMode mode, ModeSwitchVia via)
    {
        _ = mode switch { ViewMode.Fly or ViewMode.Walk => 0, _ => throw new ArgumentOutOfRangeException(nameof(mode)) };
        if (_rules is null || !_rules.Current.ViewModes.Contains(mode)) return Task.FromResult(ModeSwitchResult.Refused);
        if (mode == Mode) return Task.FromResult(ModeSwitchResult.Ok);
        ActiveTransit?.Cancel(); ActiveTransit = null; _transitDestination = null;
        ModeSwitchResult result;
        N.Vector3 inheritedVelocity = default;
        switch (mode)
        {
            case ViewMode.Walk:
                if (_walk is null) return Task.FromResult(ModeSwitchResult.Refused);
                result = _walk.Enter(Pose);
                if (result == ModeSwitchResult.Refused) return Task.FromResult(result);
                _flySpeedStep = _speedStep; _speedStep = _walkSpeedStep;
                _walk.SetSpeedStep(_walkSpeedStep);
                _pose = _walk.EyePose;
                break;
            case ViewMode.Fly:
                _pose = (_walk is { Active: true } ? _walk.EyePose : Pose) with { FovDeg = Tuning.FlyFov };
                if (_walk is { Active: true }) inheritedVelocity = new(_walk.Velocity.X, 0, _walk.Velocity.Z);
                _walk?.Leave(); _speedStep = _flySpeedStep;
                _pose = Legalize(_pose);
                result = ModeSwitchResult.Ok;
                break;
            default: throw new ArgumentOutOfRangeException(nameof(mode));
        }
        _mode = mode; _flight.Reset(); _input.Clear(); SetEdgeFog(0);
        _flight.ConstrainVelocity(inheritedVelocity);
        _saves?.Request(SaveReason.Settled);
        return Task.FromResult(result);
    }

    public void OnMapLoaded(IMapContext context)
    {
        _map = context;
        _bounds = new(context, context.World.Boundary);
        // MapService owns destination selection and applies its resolved pose after these callbacks.
        // In particular, loading a map must not replace a preview/Title pose with its default spawn.
        ResetHistory(); SetEdgeFog(0);
    }
    public void OnMapUnloading(MapId from)
    {
        ActiveTransit?.Cancel(); ActiveTransit = null; _transitDestination = null;
        ResetHistory();
        _bounds = null; _map = null; SetEdgeFog(0);
    }
    public Pose Legalize(Pose pose)
    {
        ValidateFinite(pose);
        return _bounds is null ? pose : pose with { Position = _bounds.Project(pose.Position) };
    }
    public bool IsLegal(Pose pose, bool outsideBuffer = false)
    {
        try { ValidateFinite(pose); } catch (ArgumentOutOfRangeException) { return false; }
        return _bounds?.IsLegal(pose.Position, outsideBuffer) ?? false;
    }

    public ViewSaveData Capture(in CaptureContext context)
    {
        Pose settled = (ActiveTransit is not null ? _transit?.Destination ?? _transitDestination : null) ?? (Mode switch
        {
            ViewMode.Fly => Pose,
            ViewMode.Walk => _walk?.SettledEyePose ?? Pose,
            _ => throw new InvalidOperationException("Unknown view mode.")
        });
        var medium = _map?.World.Spatial.MediumAt(settled.Position);
        NavSpace? space = medium is { } current && current.Kind == MediumKind.Interior && current.Feature is { } feature
            ? new(feature, checked((short)current.Storey)) : null;
        return new(Mode, settled with { FovDeg = null }, space, _speedStep, _flySpeedStep);
    }
    public void Restore(ViewSaveData data, RestoreContext context)
    {
        ViewMode mode = data.Mode switch
        {
            ViewMode.Fly => ViewMode.Fly,
            ViewMode.Walk => ViewMode.Walk,
            _ => throw new InvalidDataException("Unknown saved view mode.")
        };
        if (_rules is null) throw new InvalidOperationException("Ruleset is unavailable during view restoration.");
        if (!_rules.Current.ViewModes.Contains(mode))
            throw new InvalidDataException("The saved view mode is unavailable in the selected ruleset.");
        if (mode == ViewMode.Walk && _walk is null)
            throw new InvalidOperationException("Restoring walk view requires WP-42 and its walk save section.");
        Pose pose = Legalize(data.Pose);
        if (pose.Position != data.Pose.Position) context.Repair("view.pose");
        ActiveTransit?.Cancel(); ActiveTransit = null; _transitDestination = null;
        ResetHistory();
        _mode = mode;
        _speedStep = Math.Clamp(data.SpeedStep,
            mode switch { ViewMode.Fly => 1, ViewMode.Walk => 0, _ => throw new InvalidDataException("Unknown view mode.") },
            mode switch { ViewMode.Fly => Tuning.SpeedSteps, ViewMode.Walk => 4, _ => throw new InvalidDataException("Unknown view mode.") });
        _flySpeedStep = Math.Clamp(data.FlySpeedStep, 1, Tuning.SpeedSteps);
        if (_speedStep != data.SpeedStep) context.Repair("view.speedStep");
        if (_flySpeedStep != data.FlySpeedStep) context.Repair("view.flySpeedStep");
        if (mode == ViewMode.Walk) _walkSpeedStep = _speedStep;
        else _walk?.Leave();
        SetPose(pose); SetEdgeFog(0);
    }
    public ViewSaveData CreateDefault(NewGameContext context)
    {
        // The map catalogue resolves authored spawns before waypoints, including a target map that
        // has not mounted yet. Current-map bounds must never project another map's default pose.
        var spawn = Services.Get<IMapService>().ResolveSpawn(context.Map);
        ValidateFinite(spawn.Fly);
        var pose = _map?.Descriptor.Id == context.Map ? Legalize(spawn.Fly) : spawn.Fly;
        return new(ViewMode.Fly, pose with { FovDeg = null }, null,
            Tuning.DefaultSpeedStep, Tuning.DefaultSpeedStep);
    }

    private bool IsPlaying => _flow?.State switch
    {
        AppState.Playing => true,
        null or AppState.Boot or AppState.Title or AppState.Entering or AppState.Leaving or AppState.Quitting => false,
        _ => throw new InvalidOperationException("Unknown application state.")
    };
    private void OnFlowChanged(AppState previous, AppState next)
    {
        _input.Clear(); _flight.Reset(); _walk?.SetInput(default); SetInterfaceMode(true); SetEdgeFog(0);
        _menuHeld = false; _menuSeconds = 0; _skipSent = false;
        _skipHintVisible = false;
        if (next == AppState.Playing && Mode == ViewMode.Fly) _pose = Legalize(Pose);
    }
    private void SetEdgeFog(float value)
    {
        EdgeFog = Math.Clamp(value, 0, 1);
        RenderingServer.GlobalShaderParameterSet(ShaderGlobals.EdgeFog, EdgeFog);
    }
    private void ResetHistory()
    {
        ActiveTransit?.Cancel(); ActiveTransit = null; _transitDestination = null;
        _flight.Reset(); _input.Clear(); _walk?.SetInput(default);
        _recoverTriggered = false;
        _wheel = 0; _menuHeld = false; _menuSeconds = 0; _skipSent = false;
        _skipHintVisible = false;
        _listener?.Sync(reset: true);
    }
    private static void ValidateFinite(Pose pose)
    {
        if (!float.IsFinite(pose.Position.X) || !float.IsFinite(pose.Position.Y) || !float.IsFinite(pose.Position.Z)
            || !float.IsFinite(pose.YawDeg) || !float.IsFinite(pose.PitchDeg)
            || pose.FovDeg is { } fov && (!float.IsFinite(fov) || fov <= 0 || fov >= 180))
            throw new ArgumentOutOfRangeException(nameof(pose));
    }
}
