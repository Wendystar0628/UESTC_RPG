using System.Text.Json.Serialization.Metadata;
using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Audio;
using Campus.Contracts.Maps;
using Campus.Contracts.Save;
using Campus.Contracts.Settings;
using Campus.Contracts.Startup;
using Campus.Kernel.Renderer;
using Godot;
using N = System.Numerics;
using AppBoot = Campus.Kernel.Boot.Boot;
using KernelServices = Campus.Kernel.Registry.Kernel;

namespace Campus.Camera.Walk;

/// <summary>Feet-origin physical host and orchestration for independent walking responsibilities.</summary>
[KernelService(typeof(IWalkController))]
[KernelService(typeof(IWalkTelemetry))]
[KernelNode("walk-controller")]
[SaveSection("walk", Version = 1, Phase = RestorePhase.Pose, Order = 61)]
public partial class WalkController : CharacterBody3D, IWalkController, IWalkTelemetry, IMapAware, ISaveSection<WalkSave>, IViewContentReadiness
{
    private readonly WalkBodyCollision _bodyCollision;
    private readonly WalkLocomotion _locomotion;
    private readonly WalkMovementTelemetry _telemetry;
    private readonly WalkComfort _comfort;
    private readonly WalkSwimming _swimming;
    private readonly WalkPlacement _placement;
    private readonly WalkTerrainPreparation _terrain = new();
    private readonly WalkPersistence _persistence;
    private IMapContext? _map;
    private ICollisionWorld? _collision;
    private IWorldState? _world;
    private IViewControl? _view;
    private IAppFlow? _appFlow;
    private ISettingsSection? _uiSettings;
    private bool _appFrozen;
    private N.Vector3 _feet;
    private N.Vector3 _previousFeet;
    private float _yaw;
    private float _pitch;
    private bool _worldResetPending = true;

    public bool Active => _placement.HasPlacement && _view?.Mode == ViewMode.Walk;
    public bool ExitRequested => _placement.ExitRequested;
    public bool Grounded { get; private set; }
    public bool CollisionReady { get; private set; }
    public IMapArrival? PlacementArrival => _placement.Arrival;
    public int SpeedStep => _locomotion.SpeedStep;
    public int SafetyReturnCount => _placement.SafetyReturnCount;
    public bool IsSettled => !_terrain.LandingPointsPending && (!Active || (!_swimming.Active && Grounded && CollisionReady
        && !_comfort.Descending && !ExitRequested && _map is not null && !WalkSafetyQueries.InWater(_map.World.Spatial, _feet)));
    public bool IsReady(Pose pose) => !_terrain.LandingPointsPending && !_persistence.HistoryPending;
    public Exception? Failure => _terrain.Failure;
    public Pose BodyPose => new(_feet, _yaw, _pitch);
    public N.Vector3 ObserverEye => _feet + N.Vector3.UnitY * EyeHeight;
    N.Vector3 IWalkController.Velocity => Velocity.ToNumerics();
    public Pose SettledEyePose => new(ResolveSettledFeet().Position + N.Vector3.UnitY * ShoreEyeHeight, _yaw, _pitch, _comfort.Fov);
    public float EyeRollDeg => _comfort.EyeRollDeg(_appFrozen || _placement.EntryFrom is not null || _comfort.Descending
        || _placement.NeedsCheck || !Active);
    private float EyeHeight => _swimming.Active ? WaterMotionRules.FeetBelowSurface + WaterMotionRules.EyeAboveSurface : ShoreEyeHeight;
    private float ShoreEyeHeight => _locomotion.Crouching ? WalkRules.CrouchingEye : WalkRules.StandingEye;
    private WalkMotionSample MotionSample => new(_feet, _previousFeet, _yaw, Grounded, _locomotion.Input, _locomotion.Crouching);

    public NavSpace? Space
    {
        get
        {
            if (_map is null || !_terrain.IsPrepared(_feet)) return null;
            var medium = _map.World.Spatial.MediumAt(_feet + N.Vector3.UnitY * EyeHeight);
            return medium.Kind switch
            {
                MediumKind.Interior when medium.Feature is { } building => new(building, (short)medium.Storey),
                MediumKind.Air or MediumKind.Interior or MediumKind.Underground or MediumKind.Underwater or MediumKind.FogSea => null,
                _ => throw new ArgumentOutOfRangeException(nameof(medium))
            };
        }
    }

    public Pose EyePose
    {
        get
        {
            if (_placement.EntryFrom is { } pending) return pending with { YawDeg = _yaw, PitchDeg = _pitch };
            if (_appFrozen) return EyeAt(_feet);
            if (_comfort.Descending) return _comfort.DescentEye(_feet, EyeHeight, _yaw, _pitch);
            return _comfort.RenderEye(MotionSample, EyeHeight, _pitch);
        }
    }

    public WalkController()
    {
        _bodyCollision = new(this);
        _locomotion = new(_bodyCollision);
        _telemetry = new(this, _bodyCollision);
        _comfort = new(this, _telemetry);
        _swimming = new(_bodyCollision, () => CollisionReady = false, RequestExit);
        _placement = new(this, () => Space);
        _persistence = new(_placement, _terrain);
        Name = "WalkController";
        ProcessPriority = FramePriority.Camera;
        PhysicsInterpolationMode = PhysicsInterpolationModeEnum.Off;
        MotionMode = MotionModeEnum.Grounded;
        UpDirection = Vector3.Up;
        CollisionLayer = 0;
        CollisionMask = WalkBodyCollision.CollisionLayers;
        FloorMaxAngle = WalkRules.FloorAngleDegrees * (float.Pi / 180f);
        FloorSnapLength = WalkTuning.SnapLength;
        FloorConstantSpeed = true;
        FloorStopOnSlope = true;
        FloorBlockOnWall = true;
        SafeMargin = WalkTuning.SafeMargin;
        MaxSlides = 4;
    }

    public override void _Ready()
    {
        ResolveServices();
        _uiSettings = AppBoot.Options.Automated ? null : KernelServices.Get<ISettingsStore>().Section("ui");
        _comfort.BindSettings(_uiSettings);
        _world!.InstanceChanged += OnWorldInstanceChanged;
        _bodyCollision.Initialize();
        _placement.Attach(this);
        _previousFeet = _feet = GlobalPosition.ToNumerics();
    }

    public override void _EnterTree()
    {
        if (!_bodyCollision.Initialized) return;
        _world!.InstanceChanged += OnWorldInstanceChanged;
        _placement.Attach(this);
        _worldResetPending = true;
    }

    public override void _ExitTree()
    {
        if (_world is not null) _world.InstanceChanged -= OnWorldInstanceChanged;
        _placement.Detach();
    }

    private void OnWorldInstanceChanged(long instance)
    {
        _worldResetPending = true;
        ResetSwimming();
        ResetGait();
        _placement.WorldChanged();
    }

    private void ResolveServices()
    {
        _collision ??= KernelServices.Get<ICollisionWorld>();
        _terrain.ResolveService();
        _swimming.ResolveService();
        _world ??= KernelServices.Get<IWorldState>();
        _view ??= KernelServices.Get<IViewControl>();
        _map = KernelServices.Get<IMapService>().Context;
        _terrain.BindMap(_map);
        _placement.Bind(_map, _collision);
    }

    public ModeSwitchResult Enter(Pose from)
    {
        ValidatePose(from);
        ResolveServices();
        if (_map is null) return ModeSwitchResult.Refused;
        _appFrozen = false;
        _locomotion.Enter();
        ApplyHeight();
        Place(from, WalkTuning.LandingLift);
        _placement.BeginEntry(from);
        _terrain.PrepareEntry(from);
        _comfort.UseEntryFov(from);
        return ModeSwitchResult.Ok;
    }

    public void Leave()
    {
        _telemetry.QueueTakeoff(Active);
        ResetSwimming();
        ResetGait();
        _appFrozen = false;
        _locomotion.ClearInput();
        _comfort.CancelDescent();
        _placement.Leave();
        _terrain.CancelEntry();
        Velocity = Vector3.Zero;
        Grounded = false;
        CollisionReady = false;
    }

    private void RequestExit()
    {
        ResetSwimming();
        ResetGait();
        _placement.Fail();
        _locomotion.ClearInput();
        _comfort.CancelDescent();
        _terrain.CancelEntry();
        Velocity = Vector3.Zero;
        Grounded = false;
        CollisionReady = false;
    }

    public void SetLook(float yawDeg, float pitchDeg)
    {
        if (!float.IsFinite(yawDeg) || !float.IsFinite(pitchDeg)) throw new ArgumentOutOfRangeException(nameof(yawDeg));
        _yaw = yawDeg % 360f;
        _pitch = Math.Clamp(pitchDeg, -89f, 89f);
    }

    public void SetInput(WalkInput input) => _locomotion.SetInput(input);
    public void SetSpeedStep(int step) => _locomotion.SetSpeedStep(step);
    public void Warp(Pose feet) { Place(feet, 0); _telemetry.QueueTeleport(); }

    private void Place(Pose feet, float lift, float searchRadius = float.PositiveInfinity)
    {
        ValidatePose(feet);
        SetLook(feet.YawDeg, feet.PitchDeg);
        ResetSwimming();
        ResetGait();
        _telemetry.ClearTouchdown();
        _placement.Begin(feet, lift, searchRadius);
        _feet = _previousFeet = feet.Position;
        if (IsInsideTree()) GlobalPosition = _feet.ToGodot();
        else Position = _feet.ToGodot();
        if (IsInsideTree()) ResetPhysicsInterpolation();
        Velocity = Vector3.Zero;
        Grounded = false;
        CollisionReady = false;
        _locomotion.ResetPlacement();
        _comfort.ClearDescent();
        _comfort.UseWalkFov();
    }

    public override void _PhysicsProcess(double delta)
    {
        _telemetry.BeginTick();
        if (_terrain.LandingPointsPending)
        {
            if (!_terrain.CompleteLandingPoints()) return;
            _persistence.ValidateRestoredHistory(_map!);
        }
        StepPhysics(delta);
    }

    private void StepPhysics(double delta)
    {
        if ((!Active && _placement.EntryFrom is null) || !_bodyCollision.Initialized || _map is null || _collision is null || ExitRequested) return;
        if (!_terrain.TerrainReady(_feet, _bodyCollision.Radius)) { _previousFeet = _feet; return; }
        if (!PrepareEntryCandidate()) return;
        // Resolve only after the node is live, never while constructing the flow/save/view dependency graph.
        _appFlow ??= KernelServices.Get<IAppFlow>();
        CollisionReady = _collision.Ready(_feet, _bodyCollision.Radius);
        if (_placement.NeedsCheck && !ResolvePlacement()) { CollisionReady = false; _previousFeet = _feet; return; }
        // The camera commits Walk only after the candidate has passed real placement admission.
        if (!Active || FreezeUnlessPlaying()) return;
        float dt = (float)delta;
        if (!(dt > 0) || !float.IsFinite(dt)) return;
        if (!PrepareMotion(dt)) return;
        if (!_swimming.ReadWater(_feet, out var water)) { FreezeWaterMotion(); return; }
        UpdateSwimming(water);
        _locomotion.AdvanceJumpClock(dt);
        _placement.AdvanceSafetyClocks(dt);
        bool wasGrounded = Grounded;
        if (_locomotion.PrepareInput(dt, wasGrounded, _swimming.Active, _feet)) ApplyHeight();
        var plan = _locomotion.Integrate(dt, wasGrounded, _swimming.Active, _feet, _yaw,
            Velocity.ToNumerics(), water, _map, _collision, _uiSettings);
        IntegrateMotion(dt, wasGrounded, plan);
    }

    private bool PrepareEntryCandidate()
    {
        if (!_placement.EntryCandidatePending) return true;
        if (!_terrain.CompleteEntry()) return false;
        _placement.CompleteEntryCandidate();
        var origin = _placement.EntryFrom!.Value;
        var candidate = _placement.EntryCandidate(origin);
        if (candidate.Position != origin.Position) MovePlacementFocus(candidate);
        return true;
    }

    private bool FreezeUnlessPlaying()
    {
        var runtimeVelocity = Velocity.ToNumerics();
        _appFrozen = WalkMath.FreezeUnlessPlaying(_appFlow!.IsPlaying, _feet, ref _previousFeet, ref runtimeVelocity);
        if (!_appFrozen) return false;
        _comfort.Reset();
        Velocity = runtimeVelocity.ToGodot();
        _locomotion.Freeze();
        return true;
    }

    private bool PrepareMotion(float dt)
    {
        // Prepare the actual one-tick motion envelope before water, safety or region queries.
        float travel = Math.Max(WalkTuning.SprintSpeed * WalkRules.SpeedMultiplier(SpeedStep)
            + WalkTuning.SprintJumpBoost * WalkRules.SpeedMultiplier(SpeedStep), Velocity.Length()) * dt;
        if (!_terrain.TerrainReady(_feet, travel + _bodyCollision.Radius)) { _previousFeet = _feet; return false; }
        if (_worldResetPending)
        {
            _worldResetPending = false;
            ResetGait();
            _previousFeet = _feet;
            if (!_comfort.Descending && _placement.EntryFrom is null) _comfort.UseWalkFov();
        }
        // Ready covers the current 64 m tile plus eight neighbours. Never integrate into an unmounted tile.
        if (!CollisionReady) { _previousFeet = _feet; _comfort.Reset(); return false; }
        if (_comfort.Descending)
        {
            if (!_comfort.AdvanceDescent(dt)) return false;
            _previousFeet = _feet;
            ResetGait();
            _telemetry.QueueTouchdown();
            Velocity = Vector3.Down * WalkTuning.LandingSpeed;
        }
        return true;
    }

    private void IntegrateMotion(float dt, bool wasGrounded, WalkVelocityPlan plan)
    {
        var velocity = plan.Velocity;
        Span<BoundaryHit> boundaries = stackalloc BoundaryHit[8];
        int boundaryCount = _collision!.NearbyBoundaries(_feet, _bodyCollision.Radius, _bodyCollision.Height, boundaries);
        velocity = WalkMath.SlowAtBoundaries(velocity, boundaries[..boundaryCount]);
        var requested = new Vector3(velocity.X * dt, 0, velocity.Z * dt);
        requested = _swimming.LimitRegions(requested, _feet, _map!, _collision);
        if (!CollisionReady || ExitRequested) { FreezeWaterMotion(); return; }
        if (_locomotion.Crouching && wasGrounded && !plan.Jumped) requested = _bodyCollision.LimitCrouchEdge(requested);
        velocity.X = requested.X / dt;
        velocity.Z = requested.Z / dt;
        var next = _feet + new N.Vector3(requested.X, 0, requested.Z);
        if (!_collision.Ready(next, _bodyCollision.Radius)) { CollisionReady = false; _previousFeet = _feet; return; }
        if (!_swimming.ReadWater(next, out _)) { FreezeWaterMotion(); return; }
        _previousFeet = _feet;
        Velocity = velocity.ToGodot();
        FloorSnapLength = _swimming.Active || plan.Jumped ? 0 : WalkTuning.SnapLength;
        MoveAndSlide();
        bool stepped = false;
        if (!_swimming.Active && wasGrounded && !plan.Jumped && IsOnWall()) stepped = _bodyCollision.TryStep(requested, _previousFeet.ToGodot());
        if (stepped) Velocity = new(velocity.X, 0, velocity.Z);
        if (IsOnCeiling()) Velocity = new(Velocity.X, MathF.Min(0, Velocity.Y), Velocity.Z);
        if (!_swimming.Active) _bodyCollision.LimitSteepSlide();
        Grounded = !_swimming.Active && (IsOnFloor() || stepped);
        _feet = GlobalPosition.ToNumerics();
        if (!_swimming.ReadWater(_feet, out var water))
        {
            _feet = _previousFeet;
            GlobalPosition = _feet.ToGodot();
            FreezeWaterMotion();
            return;
        }
        UpdateSwimming(water);
        if (_placement.NeedsRecovery(_feet, water)) { Recover(); return; }
        _telemetry.PublishMovement(MotionSample, dt, wasGrounded, plan.Jumped, stepped, velocity, plan.PreviousSpeed);
        _comfort.AdvanceComfort(MotionSample, _map!, dt, wasGrounded, plan.Jumped, stepped, plan.Sprint);
        if (_placement.ShouldRecordSafety(Grounded))
            _placement.RecordSafety(BodyPose, Grounded, _placement.IsSafeRecordPoint(MotionSample, _swimming.Active,
                CollisionReady, _bodyCollision.Height));
        _comfort.AdvanceFov(dt, plan.Sprint, Grounded, requested);
    }

    private void UpdateSwimming(WaterQueryResult result)
    {
        bool changed = _swimming.Update(result, _feet);
        if (changed)
        {
            _locomotion.ChangeSwimming();
            Grounded = false;
            MotionMode = _swimming.Active ? MotionModeEnum.Floating : MotionModeEnum.Grounded;
            ApplyHeight();
            _comfort.Reset();
        }
        if (_swimming.Active)
        {
            var water = result.Sample!.Value;
            _feet.Y = water.SurfaceY - WaterMotionRules.FeetBelowSurface;
            GlobalPosition = _feet.ToGodot();
            Velocity = new(Velocity.X, 0, Velocity.Z);
            Grounded = false;
            if (changed)
            {
                _previousFeet.Y = _feet.Y;
                _telemetry.PublishWaterEnter(water.SurfaceY - water.BottomY);
            }
        }
        else if (changed) _telemetry.PublishWaterExit();
    }

    private void ResetSwimming()
    {
        _swimming.Reset();
        MotionMode = MotionModeEnum.Grounded;
        ApplyHeight();
    }

    private void ApplyHeight() => _bodyCollision.ApplyHeight(_swimming.Active, _locomotion.Crouching);

    private void ResetGait()
    {
        _telemetry.ResetGait(_yaw);
        _comfort.Reset();
    }

    private void FreezeWaterMotion()
    {
        _previousFeet = _feet;
        Velocity = Vector3.Zero;
        _comfort.Reset();
    }
    private Pose EyeAt(N.Vector3 feet) => new(feet + N.Vector3.UnitY * EyeHeight, _yaw, _pitch, _comfort.Fov);
    private Pose ResolveSettledFeet() => _placement.ResolveSettledFeet(Active, IsSettled, BodyPose);

    private bool ResolvePlacement()
    {
        var resolution = _placement.Resolve(_yaw, _pitch, _bodyCollision.Height);
        if (ExitRequested) { RequestExit(); return false; }
        switch (resolution.Status)
        {
            case WalkPlacementStatus.Pending:
                return false;
            case WalkPlacementStatus.Refocused:
                MovePlacementFocus(resolution.Target);
                if (resolution.Relocated) _placement.MarkRelocated();
                return false;
            case WalkPlacementStatus.Resolved:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(resolution));
        }
        _feet = _previousFeet = resolution.Feet;
        ResetGait();
        GlobalPosition = _feet.ToGodot();
        ResetPhysicsInterpolation();
        _placement.CommitResolution();
        CollisionReady = true;
        _placement.WarnIfRepaired();
        if (resolution.DescentFrom is { } from) _comfort.BeginDescent(from, resolution.Target.Position, EyeHeight);
        else
        {
            if (resolution.Relocated) _telemetry.QueueTeleport();
            _comfort.ClearDescent();
            _comfort.UseWalkFov();
            _locomotion.ClearInput();
        }
        _placement.FinishResolution();
        Velocity = resolution.DescentFrom is not null ? Vector3.Down * WalkTuning.LandingSpeed : Vector3.Zero;
        return true;
    }

    private void MovePlacementFocus(Pose target)
    {
        _placement.MoveFocus(target);
        _feet = _previousFeet = target.Position;
        ResetGait();
        GlobalPosition = _feet.ToGodot();
        ResetPhysicsInterpolation();
        Velocity = Vector3.Zero;
        Grounded = false;
        CollisionReady = false;
    }

    public bool ReturnToSafety() => Active && _map is not null && _appFlow?.IsPlaying == true
        && (_appFlow.Activity & AppActivity.MapTransition) == 0 && Recover(manual: true);

    private bool Recover(bool manual = false)
    {
        if (_placement.RecoveryTarget(BodyPose, _bodyCollision.Height, manual) is not { } target)
        { RequestExit(); return false; }
        Place(target, 0);
        _placement.CommitRecovery();
        return true;
    }

    private static void ValidatePose(Pose pose)
    {
        if (!VectorMath.Finite(pose.Position) || !float.IsFinite(pose.YawDeg) || !float.IsFinite(pose.PitchDeg))
            throw new ArgumentOutOfRangeException(nameof(pose));
    }

    public void OnMapUnloading(MapId from)
    {
        Leave();
        _map = null;
        _placement.Unload();
        _terrain.Unload();
        _persistence.Unload();
    }

    public void OnMapLoaded(IMapContext ctx)
    {
        _map = ctx;
        _terrain.ResolveService();
        _terrain.BindMap(ctx);
        _placement.Bind(ctx, _collision!);
        _terrain.PrepareAuthoredLandings(ctx);
    }

    public JsonTypeInfo<WalkSave> TypeInfo => _persistence.TypeInfo;
    public WalkSave Capture(in CaptureContext context)
    {
        RequireMap(context.Map);
        return _persistence.Capture(ResolveSettledFeet(), _locomotion.Crouching, SpeedStep);
    }

    public void Restore(WalkSave dto, RestoreContext context)
    {
        ArgumentNullException.ThrowIfNull(dto);
        RequireMap(context.Map);
        _persistence.RestoreHistory(dto, context, _map!);
        _locomotion.RestoreCrouch(dto.Crouching);
        ApplyHeight();
        _locomotion.RestoreSpeedStep(dto.SpeedStep);
        if (SpeedStep != dto.SpeedStep) context.Repair("walk: speed step clamped");
        Place(_persistence.RestoreFeet(dto, context), 0, WalkTuning.SaveRepairRadius);
    }

    public WalkSave CreateDefault(NewGameContext context) => _persistence.CreateDefault(context);

    private void RequireMap(MapId targetMap)
    {
        ResolveServices();
        var maps = KernelServices.Get<IMapService>();
        if (maps.Current == targetMap && _map?.Descriptor.Id == targetMap) return;
        RequestExit();
        throw new InvalidOperationException($"Cannot restore or capture walk feet before map {targetMap.Value} is mounted.");
    }

    public float StepPhase => _telemetry.StepPhase;
    public event Action<FootfallInfo>? Footfall { add => _telemetry.Footfall += value; remove => _telemetry.Footfall -= value; }
    public event Action<ulong, float>? Jumped { add => _telemetry.Jumped += value; remove => _telemetry.Jumped -= value; }
    public event Action<ulong, float, N.Vector3>? Landed { add => _telemetry.Landed += value; remove => _telemetry.Landed -= value; }
    public event Action<ulong, float>? StepUp { add => _telemetry.StepUp += value; remove => _telemetry.StepUp -= value; }
    public event Action<ulong, float>? StepDown { add => _telemetry.StepDown += value; remove => _telemetry.StepDown -= value; }
    public event Action<ulong, float>? Skid { add => _telemetry.Skid += value; remove => _telemetry.Skid -= value; }
    public event Action<ContactInfo>? WallContact { add => _telemetry.WallContact += value; remove => _telemetry.WallContact -= value; }
    public event Action<ulong, float> WaterEnter { add => _telemetry.WaterEnter += value; remove => _telemetry.WaterEnter -= value; }
    public event Action<ulong> WaterExit { add => _telemetry.WaterExit += value; remove => _telemetry.WaterExit -= value; }
    public event Action<ulong, float>? TurnInPlace { add => _telemetry.TurnInPlace += value; remove => _telemetry.TurnInPlace -= value; }
    public event Action<ulong>? ModeTouchdown { add => _telemetry.ModeTouchdown += value; remove => _telemetry.ModeTouchdown -= value; }
    public event Action<ulong>? ModeTakeoff { add => _telemetry.ModeTakeoff += value; remove => _telemetry.ModeTakeoff -= value; }
    public event Action<ulong>? Teleport { add => _telemetry.Teleport += value; remove => _telemetry.Teleport -= value; }
}
