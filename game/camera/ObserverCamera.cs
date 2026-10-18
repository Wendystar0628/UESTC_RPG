using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Maps;
using Campus.Contracts.Save;
using Campus.Contracts.Settings;
using Godot;
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
    private IWalkController? _walk;
    private ITransitDriver? _transit;
    private Pose _pose;
    private ViewMode? _mode;
    private int _flySpeedStep;
    private bool _recoverTriggered;
    private bool _automated;
    private bool _worldResetPending = true;
    private IWorldState? _world;
    private Pose? _transitDestination;
    private TaskCompletionSource<ModeSwitchResult>? _walkEntry;
    private ObserverListener? _listener;
    private ISettingsSection? _uiSettings;
    private float _flyFovFrom;
    private float _flyFovTarget;
    private float _flyFovElapsed;

    private float FlyFov => _uiSettings?.Get(UiSettingDefinitions.FlyFov) ?? UiSettingDefinitions.FlyFov.Default;
    private float WalkFov => _uiSettings?.Get(UiSettingDefinitions.WalkFov) ?? UiSettingDefinitions.WalkFov.Default;
    public event Action? WalkEntryRefused;

    public FlightTuning Tuning { get; } = new();

    public ObserverCamera()
    {
        _flight = new(Tuning); _input = new(Tuning);
        _flySpeedStep = Tuning.DefaultSpeedStep;
        ProcessPriority = FramePriority.Camera;
    }

    public override void _Ready()
    {
        _flow = Services.Get<IAppFlow>(); _rules = Services.Get<IRuleset>();
        _world = Services.Get<IWorldState>();
        _world.InstanceChanged += OnWorldInstanceChanged;
        if (Services.Services.Entries.Any(e => e.Kind == "service" && e.Id == typeof(IWalkController).FullName && !e.IsStub))
            _walk = Services.Get<IWalkController>();
        _transit = Services.Get<ITransitDriver>();
        _automated = AppBoot.Options.Automated;
        var settings = _automated ? null : Services.Get<ISettingsStore>();
        _bindings.Initialize(settings);
        _uiSettings = settings?.Section("ui");
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
        if (_worldResetPending)
        { _worldResetPending = false; ResetHistory(); SetEdgeFog(0); }
        AdvanceInput(seconds);
        if (AdvanceWalkEntry()) { _flight.Reset(); SetEdgeFog(0); return; }
        if (!IsPlaying || _mode is null) { _flight.Reset(); SetEdgeFog(0); return; }
        if (Mode == ViewMode.Fly && ActiveTransit is null) AdvanceFlyFov(seconds);
        if (Mode == ViewMode.Walk && _walk is { ExitRequested: true })
        {
            _ = SetMode(ViewMode.Fly, ModeSwitchVia.Key);
            return;
        }
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
                    _flySpeedStep, _map.World.Spatial.GroundY(Pose.Position.X, Pose.Position.Z), seconds);
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
                _walk.SetInput(_locks.Locked ? default : new WalkInput(_input.Move, _input.Sprint,
                    _input.JumpHeld, _input.TakeJump(), _input.Crouch));
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
        CancelWalkEntry();
        ActiveTransit?.Cancel(); ActiveTransit = null; _transitDestination = null;
        ResetHistory();
        _bounds = null; _map = null; SetEdgeFog(0);
    }

    private bool IsPlaying => _flow?.IsPlaying == true;
    private void OnWorldInstanceChanged(long instance) { CancelWalkEntry(); _worldResetPending = true; }
    private void OnFlowChanged(AppState previous, AppState next)
    {
        _input.Clear(); _flight.Reset(); _walk?.SetInput(default); SetInterfaceMode(true); SetEdgeFog(0);
        ResetSkipHold(); _skipHintVisible = false;
        if (IsPlaying && Mode == ViewMode.Fly) _pose = Legalize(Pose);
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
        _wheel = 0; ResetSkipHold(); _skipHintVisible = false;
        _listener?.Sync(reset: true);
    }

    private void AdvanceFlyFov(float seconds)
    {
        float target = FlyFov;
        if (target != _flyFovTarget)
        {
            _flyFovFrom = _pose.FovDeg ?? target;
            _flyFovTarget = target;
            _flyFovElapsed = 0;
        }
        // Keep an explicitly supplied review/cinematic FOV until a mode or preference change.
        if (_flyFovElapsed >= Tuning.ModeFovSeconds) return;
        _flyFovElapsed = MathF.Min(_flyFovElapsed + seconds, Tuning.ModeFovSeconds);
        float t = ScalarMath.Smootherstep(_flyFovElapsed / Tuning.ModeFovSeconds);
        _pose = _pose with { FovDeg = _flyFovFrom + (_flyFovTarget - _flyFovFrom) * t };
    }
}
