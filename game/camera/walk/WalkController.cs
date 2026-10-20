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

/// <summary>Feet-origin cylinder; input ownership and camera orientation remain with WP-06.</summary>
[KernelService(typeof(IWalkController))]
[KernelService(typeof(IWalkTelemetry))]
[KernelNode("walk-controller")]
[SaveSection("walk", Version = 1, Phase = RestorePhase.Pose, Order = 61)]
public partial class WalkController : CharacterBody3D, IWalkController, IMapAware, ISaveSection<WalkSave>, IViewContentReadiness
{
    // WP-43 assigns one layer per SurfaceTag. Water is semantic, never a blocking surface.
    private const uint BlockingLayers = (1u << (int)SurfaceTag.Ground) | (1u << (int)SurfaceTag.Stair)
        | (1u << (int)SurfaceTag.Barrier) | (1u << (int)SurfaceTag.Wall) | (1u << (int)SurfaceTag.Prop)
        | (1u << (int)SurfaceTag.Invisible) | (1u << (int)SurfaceTag.Boundary) | (1u << (int)SurfaceTag.DoorClosed);
    private const uint NoStepLayers = (1u << (int)SurfaceTag.Barrier) | (1u << (int)SurfaceTag.Boundary)
        | (1u << (int)SurfaceTag.DoorClosed);
    private readonly PhysicsTestMotionParameters3D _probe = new();
    private readonly PhysicsTestMotionResult3D _probeResult = new();
    private readonly CylinderShape3D _cylinder = new() { Radius = WalkRules.Radius, Height = WalkRules.StandingHeight };
    private readonly CylinderShape3D _standingProbe = new() { Radius = WalkRules.Radius, Height = WalkRules.StandingHeight };
    private readonly PhysicsShapeQueryParameters3D _clearance = new();
    private WalkPlacementAdmission? _admission;
    private CollisionShape3D? _shape;
    private IMapContext? _map;
    private ITerrainPreparation? _terrain;
    private Task? _terrainPreparation;
    private Task? _entryTerrainPreparation;
    private Task? _landingPointsPreparation;
    private bool _entryCandidatePending;
    private ICollisionWorld? _collision;
    private IWorldState? _world;
    private IViewControl? _view;
    private IAppFlow? _appFlow;
    private ISettingsSection? _uiSettings;
    private bool _appFrozen;
    private WalkInput _input;
    private N.Vector3 _feet;
    private N.Vector3 _previousFeet;
    private float _yaw;
    private float _pitch;
    private bool _crouching;
    private bool _restoredCrouch;
    private bool _jumpQueued;
    private float _coyote;
    private float _jumpBuffer;
    private float _sinceJump = WalkTuning.HeldJumpInterval;
    private float _sinceSafe;
    private readonly WalkSafePoints _safePoints = new();
    private readonly List<Pose> _placementAlternatives = [];
    private int _placementAlternative;
    private IMapRuntime? _mapRuntime;
    private IMapArrival? _placementArrival;
    private bool _placementRelocated;
    private float _placementSearchRadius;
    private bool _entrySightPending;
    private float _sinceReturn = float.PositiveInfinity;
    private int _returnSkip;
    private Pose _placementTarget;
    private bool _hasPlacement;
    private bool _placementNeedsCheck;
    private float _placementLift;
    private Pose? _entryFrom;
    private bool _safetyFailed;
    private float _fov = UiSettingDefinitions.WalkFov.Default;
    private Pose _descentFrom;
    private float _descentDuration;
    private float _descentElapsed;
    private bool _worldResetPending = true;

    public bool Active => _hasPlacement && _view?.Mode == ViewMode.Walk;
    public bool ExitRequested => _safetyFailed;
    public bool Grounded { get; private set; }
    public bool CollisionReady { get; private set; }
    public IMapArrival? PlacementArrival => _placementArrival;
    public int SpeedStep { get; private set; } = WalkRules.DefaultSpeedStep;
    public int SafetyReturnCount { get; private set; }
    public bool IsSettled => _landingPointsPreparation is null && (!Active || (!_swimming && Grounded && CollisionReady
        && _descentDuration <= 0 && !_safetyFailed && _map is not null && !WalkSafetyQueries.InWater(_map.World.Spatial, _feet)));
    public bool IsReady(Pose pose) => _landingPointsPreparation is null && _historyRestore is null;
    public string? Error => _landingPointsPreparation?.Exception?.GetBaseException().Message;
    public Pose BodyPose => new(_feet, _yaw, _pitch);
    public N.Vector3 ObserverEye => _feet + N.Vector3.UnitY * EyeHeight;
    N.Vector3 IWalkController.Velocity => Velocity.ToNumerics();

    public Pose SettledEyePose => new(ResolveSettledFeet().Position + N.Vector3.UnitY * ShoreEyeHeight, _yaw, _pitch, _fov);
    public NavSpace? Space
    {
        get
        {
            if (_map is null || _terrain is null || !_terrain.IsPrepared(_map.World, new(_feet.X, _feet.Z))) return null;
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
            if (_entryFrom is { } pending) return pending with { YawDeg = _yaw, PitchDeg = _pitch };
            if (_appFrozen) return EyeAt(_feet);
            if (_descentDuration > 0)
            {
                float t = ScalarMath.Smootherstep(_descentElapsed / _descentDuration);
                var destination = _feet + N.Vector3.UnitY * EyeHeight;
                var position = N.Vector3.Lerp(_descentFrom.Position, destination, t);
                float fromFov = _descentFrom.FovDeg ?? UiSettingDefinitions.FlyFov.Default;
                return new(position, _yaw, _pitch, fromFov + (WalkFov - fromFov) * t);
            }
            return RenderEye();
        }
    }
    private float EyeHeight => _swimming ? WaterMotionRules.FeetBelowSurface + WaterMotionRules.EyeAboveSurface
        : ShoreEyeHeight;
    private float ShoreEyeHeight => _crouching ? WalkRules.CrouchingEye : WalkRules.StandingEye;
    private float WalkFov => _uiSettings?.Get(UiSettingDefinitions.WalkFov) ?? UiSettingDefinitions.WalkFov.Default;

    public WalkController()
    {
        Name = "WalkController";
        ProcessPriority = FramePriority.Camera;
        PhysicsInterpolationMode = PhysicsInterpolationModeEnum.Off;
        MotionMode = MotionModeEnum.Grounded;
        UpDirection = Vector3.Up;
        CollisionLayer = 0;
        CollisionMask = BlockingLayers;
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
        _world!.InstanceChanged += OnWorldInstanceChanged;
        _shape = new CollisionShape3D { Shape = _cylinder, Position = Vector3.Up * (_cylinder.Height * .5f) };
        AddChild(_shape);
        _probe.Margin = WalkTuning.SafeMargin;
        _probe.MaxCollisions = 4;
        _probe.RecoveryAsCollision = false;
        _clearance.CollisionMask = BlockingLayers;
        _clearance.CollideWithAreas = false;
        _clearance.CollideWithBodies = true;
        _clearance.Margin = 0;
        _clearance.Exclude = [GetRid()];
        _admission = new(this);
        _previousFeet = _feet = GlobalPosition.ToNumerics();
    }

    public override void _EnterTree()
    {
        if (_shape is null) return;
        _world!.InstanceChanged += OnWorldInstanceChanged;
        _admission = new(this);
        _worldResetPending = true;
    }

    public override void _ExitTree()
    {
        if (_world is not null) _world.InstanceChanged -= OnWorldInstanceChanged;
        _admission?.Dispose();
        _admission = null;
    }

    private void OnWorldInstanceChanged(long instance)
    {
        _worldResetPending = true;
        ResetSwimming();
        ResetGait();
        _safePoints.Clear();
        _placementArrival = null;
    }

    private void ResolveServices()
    {
        _collision ??= KernelServices.Get<ICollisionWorld>();
        _terrain ??= KernelServices.Get<ITerrainPreparation>();
        _water ??= KernelServices.Get<IWaterTraversalQuery>();
        _world ??= KernelServices.Get<IWorldState>();
        _view ??= KernelServices.Get<IViewControl>();
        _map = KernelServices.Get<IMapService>().Context;
    }

    public ModeSwitchResult Enter(Pose from)
    {
        ValidatePose(from);
        ResolveServices();
        if (_map is null) return ModeSwitchResult.Refused;
        _appFrozen = false;
        _safetyFailed = false;
        _crouching = false;
        _restoredCrouch = false;
        ApplyHeight();
        Place(from, WalkTuning.LandingLift);
        _entryFrom = from;
        _entryCandidatePending = true;
        var end = from.Position + Angles.Forward(from.YawDeg, from.PitchDeg) * WalkTuning.LandingSightDistance;
        var begin = new N.Vector2(from.Position.X, from.Position.Z);
        var finish = new N.Vector2(end.X, end.Z);
        _entryTerrainPreparation = _terrain!.PrepareRegion(_map, new(N.Vector2.Min(begin, finish), N.Vector2.Max(begin, finish)));
        _fov = from.FovDeg ?? UiSettingDefinitions.FlyFov.Default;
        return ModeSwitchResult.Ok;
    }

    public void Leave()
    {
        _takeoffPending |= Active;
        ResetSwimming();
        ResetGait();
        _appFrozen = false;
        _input = default;
        _jumpQueued = false;
        _descentDuration = 0;
        _placementArrival = null;
        _entrySightPending = false;
        _entryFrom = null;
        _entryCandidatePending = false; _entryTerrainPreparation = null;
        Velocity = Vector3.Zero;
        Grounded = false;
        CollisionReady = false;
    }

    private void RequestExit()
    {
        ResetSwimming();
        ResetGait();
        _safetyFailed = true;
        _input = default;
        _jumpQueued = false;
        _descentDuration = 0;
        _entryFrom = null;
        _entryCandidatePending = false; _entryTerrainPreparation = null;
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

    public void SetInput(WalkInput input)
    {
        if (!float.IsFinite(input.Move.X) || !float.IsFinite(input.Move.Y)) throw new ArgumentOutOfRangeException(nameof(input));
        if (input.Crouch) _restoredCrouch = false;
        _jumpQueued |= input.JumpPressed;
        _input = input;
    }

    public void SetSpeedStep(int step)
    {
        _ = WalkRules.SpeedMultiplier(step);
        SpeedStep = step;
    }

    public void Warp(Pose feet)
    {
        Place(feet, 0);
        _teleportPending = true;
    }

    private void Place(Pose feet, float lift, float searchRadius = float.PositiveInfinity)
    {
        ValidatePose(feet);
        SetLook(feet.YawDeg, feet.PitchDeg);
        ResetSwimming();
        ResetGait();
        _touchdownPending = false;
        _placementTarget = feet;
        _hasPlacement = true;
        _feet = _previousFeet = _placementTarget.Position;
        if (IsInsideTree()) GlobalPosition = _feet.ToGodot();
        else Position = _feet.ToGodot();
        if (IsInsideTree()) ResetPhysicsInterpolation();
        Velocity = Vector3.Zero;
        Grounded = false;
        CollisionReady = false;
        _coyote = _jumpBuffer = 0;
        _sinceJump = WalkTuning.HeldJumpInterval;
        _sinceSafe = 0;
        _jumpQueued = false;
        _input = default;
        _descentFrom = default;
        _descentDuration = _descentElapsed = 0;
        _placementNeedsCheck = true;
        _placementLift = lift;
        _placementSearchRadius = searchRadius;
        _placementArrival = null;
        _placementRelocated = false;
        _entrySightPending = false;
        _placementAlternatives.Clear();
        _placementAlternative = -1;
        _entryFrom = null;
        _safetyFailed = false;
        _fov = WalkFov;
    }

    public override void _PhysicsProcess(double delta)
    {
        _telemetryTick++;
        FlushPlacementEvents();
        if (_landingPointsPreparation is { } landingPoints)
        {
            if (!landingPoints.IsCompleted) return;
            landingPoints.GetAwaiter().GetResult(); _landingPointsPreparation = null;
            ValidateRestoredHistory();
        }
        StepPhysics(delta);
    }

    private void StepPhysics(double delta)
    {
        if ((!Active && _entryFrom is null) || _shape is null || _map is null || _collision is null || _safetyFailed) return;
        if (!TerrainReady(_feet, _cylinder.Radius)) { _previousFeet = _feet; return; }
        if (_entryCandidatePending)
        {
            if (_entryTerrainPreparation is not { IsCompleted: true }) return;
            _entryTerrainPreparation.GetAwaiter().GetResult(); _entryTerrainPreparation = null;
            _entryCandidatePending = false;
            var origin = _entryFrom!.Value;
            var candidate = EntryCandidate(origin);
            if (candidate.Position != origin.Position) MovePlacementFocus(candidate);
        }
        // Resolve only after the node is live, never while constructing the flow/save/view dependency graph.
        _appFlow ??= KernelServices.Get<IAppFlow>();
        CollisionReady = _collision.Ready(_feet, _cylinder.Radius);
        if (_placementNeedsCheck)
        {
            if (!ResolvePlacement()) { CollisionReady = false; _previousFeet = _feet; return; }
        }
        // The camera commits Walk only after the candidate has passed real placement admission.
        if (!Active) return;
        var runtimeVelocity = Velocity.ToNumerics();
        _appFrozen = WalkMath.FreezeUnlessPlaying(_appFlow.IsPlaying, _feet, ref _previousFeet, ref runtimeVelocity);
        if (_appFrozen)
        {
            ResetComfort();
            Velocity = runtimeVelocity.ToGodot();
            _input = default;
            _jumpQueued = false;
            _jumpBuffer = 0;
            return;
        }
        float dt = (float)delta;
        if (!(dt > 0) || !float.IsFinite(dt)) return;
        // Prepare the actual one-tick motion envelope before water, safety or region queries.
        float travel = Math.Max(WalkTuning.SprintSpeed * WalkRules.SpeedMultiplier(SpeedStep)
            + WalkTuning.SprintJumpBoost * WalkRules.SpeedMultiplier(SpeedStep), Velocity.Length()) * dt;
        if (!TerrainReady(_feet, travel + _cylinder.Radius)) { _previousFeet = _feet; return; }
        if (_worldResetPending)
        {
            _worldResetPending = false;
            ResetGait();
            _previousFeet = _feet;
            if (_descentDuration <= 0 && _entryFrom is null) _fov = WalkFov;
        }
        // Ready covers the current 64 m tile plus eight neighbours. Never integrate into an unmounted tile.
        if (!CollisionReady) { _previousFeet = _feet; ResetComfort(); return; }
        if (_descentDuration > 0)
        {
            _descentElapsed += dt;
            if (_descentElapsed < _descentDuration) { ResetComfort(); return; }
            _descentDuration = 0;
            _fov = WalkFov;
            _previousFeet = _feet;
            ResetGait();
            _touchdownPending = true;
            Velocity = Vector3.Down * WalkTuning.LandingSpeed;
        }
        if (!ReadWater(_feet, out var water)) { FreezeWaterMotion(); return; }
        UpdateSwimming(water);
        _sinceJump += dt;
        _sinceSafe += dt;
        _sinceReturn += dt;
        bool wasGrounded = Grounded;
        _coyote = wasGrounded ? WalkTuning.CoyoteSeconds : MathF.Max(0, _coyote - dt);
        _jumpBuffer = _jumpQueued ? WalkTuning.JumpBufferSeconds : MathF.Max(0, _jumpBuffer - dt);
        _jumpQueued = false;
        if (!_swimming) UpdateCrouch();
        float multiplier = WalkRules.SpeedMultiplier(SpeedStep);
        bool sprint = !_swimming && _input.Sprint && !_crouching && _input.Move.Y > 0;
        float speed = _swimming ? WaterMotionRules.SwimSpeed
            : (_crouching ? WalkTuning.CrouchSpeed : sprint ? WalkTuning.SprintSpeed : WalkRules.WalkSpeed) * multiplier;
        if (!_swimming && (_map.World.Spatial.WalkClassAt(_feet.X, _feet.Z) == WalkClass.Wade
            || (wasGrounded && water.Sample is { } wade && _feet.Y < wade.SurfaceY))) speed *= WaterMotionRules.WadeSpeedMultiplier;
        var direction = WalkMath.Direction(_input.Move, _yaw);
        float previousSpeed = new N.Vector2(Velocity.X, Velocity.Z).Length();
        var velocity = _swimming ? direction * speed
            : WalkMath.HorizontalVelocity(Velocity.ToNumerics(), direction * speed, wasGrounded, dt);
        bool jump = !_swimming && _coyote > 0 && (_jumpBuffer > 0 || (_input.JumpHeld && _sinceJump >= WalkTuning.HeldJumpInterval)
            || (wasGrounded && WantsAutoJump(velocity, dt)));
        if (jump)
        {
            velocity.Y = WalkTuning.JumpSpeed;
            if (sprint) velocity += WalkMath.Direction(new(0, 1), _yaw) * (WalkTuning.SprintJumpBoost * multiplier);
            velocity = WalkMath.LimitHorizontal(velocity);
            _coyote = _jumpBuffer = _sinceJump = 0;
        }
        else if (!_swimming) velocity.Y = MathF.Max(-WalkTuning.TerminalFallSpeed, velocity.Y - WalkTuning.Gravity * dt);
        Span<BoundaryHit> boundaries = stackalloc BoundaryHit[8];
        int boundaryCount = _collision.NearbyBoundaries(_feet, _cylinder.Radius, _cylinder.Height, boundaries);
        velocity = WalkMath.SlowAtBoundaries(velocity, boundaries[..boundaryCount]);
        var requested = new Vector3(velocity.X * dt, 0, velocity.Z * dt);
        requested = LimitRegions(requested);
        if (!CollisionReady || _safetyFailed) { FreezeWaterMotion(); return; }
        if (_crouching && wasGrounded && !jump) requested = LimitCrouchEdge(requested);
        velocity.X = requested.X / dt;
        velocity.Z = requested.Z / dt;
        var next = _feet + new N.Vector3(requested.X, 0, requested.Z);
        if (!_collision.Ready(next, _cylinder.Radius)) { CollisionReady = false; _previousFeet = _feet; return; }
        if (!ReadWater(next, out _)) { FreezeWaterMotion(); return; }
        _previousFeet = _feet;
        Velocity = velocity.ToGodot();
        FloorSnapLength = _swimming || jump ? 0 : WalkTuning.SnapLength;
        MoveAndSlide();
        bool stepped = false;
        if (!_swimming && wasGrounded && !jump && IsOnWall()) stepped = TryStep(requested, _previousFeet.ToGodot());
        if (stepped) Velocity = new(velocity.X, 0, velocity.Z);
        if (IsOnCeiling()) Velocity = new(Velocity.X, MathF.Min(0, Velocity.Y), Velocity.Z);
        if (!_swimming) LimitSteepSlide();
        Grounded = !_swimming && (IsOnFloor() || stepped);
        _feet = GlobalPosition.ToNumerics();
        if (!ReadWater(_feet, out water))
        {
            _feet = _previousFeet;
            GlobalPosition = _feet.ToGodot();
            FreezeWaterMotion();
            return;
        }
        UpdateSwimming(water);
        if (NeedsRecovery(water)) { Recover(); return; }
        PublishMovement(dt, wasGrounded, jump, stepped, velocity, previousSpeed);
        AdvanceComfort(dt, wasGrounded, jump, stepped, sprint);
        if (Grounded && _sinceSafe >= WalkTuning.SafetyRecordSeconds && IsSafeRecordPoint())
        {
            _safePoints.Record(BodyPose);
            _sinceSafe = 0;
        }
        float desiredFov = WalkFov * (sprint && Grounded && requested.LengthSquared() > 0 ? WalkTuning.SprintFovMultiplier : 1f);
        _fov += (desiredFov - _fov) * (1f - MathF.Exp(-dt / WalkTuning.FovResponseSeconds));
    }

    private void UpdateCrouch()
    {
        bool desired = _input.Crouch || _restoredCrouch || (_crouching && !CanStand());
        if (_crouching == desired) return;
        _crouching = desired;
        ApplyHeight();
    }

    private void ApplyHeight()
    {
        _cylinder.Radius = _swimming ? WaterMotionRules.SwimmingShape.Radius : WalkRules.Radius;
        _cylinder.Height = _swimming ? WaterMotionRules.SwimmingShape.Height
            : _crouching ? WalkTuning.CrouchingHeight : WalkRules.StandingHeight;
        if (_shape is not null) _shape.Position = Vector3.Up * (_cylinder.Height * .5f);
    }

    private bool CanStand()
    {
        // Existing crouch volume is already collision-resolved. Query only the added crown, so a sloped
        // supporting floor does not falsely count as an overhead obstruction during the full-cylinder query.
        float extra = WalkRules.StandingHeight - WalkTuning.CrouchingHeight;
        _standingProbe.Height = extra;
        _clearance.Shape = _standingProbe;
        _clearance.Transform = new(Basis.Identity,
            (_feet + N.Vector3.UnitY * (WalkTuning.CrouchingHeight + extra * .5f)).ToGodot());
        return GetWorld3D().DirectSpaceState.IntersectShape(_clearance, 1).Count == 0;
    }

    private bool Probe(Vector3 from, Vector3 motion)
    {
        _probe.From = new(Basis.Identity, from);
        _probe.Motion = motion;
        return PhysicsServer3D.BodyTestMotion(GetRid(), _probe, _probeResult);
    }

    private Vector3 LimitCrouchEdge(Vector3 motion)
    {
        var from = GlobalPosition;
        if (HasSupport(from + motion)) return motion;
        // Axis fallback lets the player slide along a ledge instead of dropping over it.
        var zOnly = new Vector3(0, 0, motion.Z);
        if (HasSupport(from + zOnly)) return zOnly;
        var xOnly = new Vector3(motion.X, 0, 0);
        return HasSupport(from + xOnly) ? xOnly : Vector3.Zero;
    }

    private bool HasSupport(Vector3 feet) => Probe(feet + Vector3.Up * WalkTuning.SafeMargin,
        Vector3.Down * (WalkTuning.SnapLength + WalkTuning.SafeMargin))
        && _probeResult.GetCollisionNormal().Y >= MathF.Cos(FloorMaxAngle);

    private bool TryStep(Vector3 requested, Vector3 before)
    {
        if (requested.LengthSquared() < .0000001f) return false;
        for (int i = 0; i < GetSlideCollisionCount(); i++)
        {
            var hit = GetSlideCollision(i);
            if (hit.GetNormal().Y < MathF.Cos(FloorMaxAngle) && hit.GetCollider() is CollisionObject3D blocker
                && (blocker.CollisionLayer & NoStepLayers) != 0) return false;
        }
        var current = GlobalPosition;
        var actual = current - before;
        var remaining = new Vector3(requested.X - actual.X, 0, requested.Z - actual.Z);
        if (remaining.LengthSquared() < .0000001f || remaining.Dot(requested) <= 0) return false;
        var lift = Vector3.Up * WalkRules.StepHeight;
        if (Probe(current, lift)) lift = _probeResult.GetTravel();
        if (lift.Y <= WalkTuning.SafeMargin) return false;
        var raised = current + lift;
        if (Probe(raised, remaining)) return false;
        var advanced = raised + remaining;
        if (!Probe(advanced, Vector3.Down * (lift.Y + WalkTuning.SnapLength))) return false;
        if (_probeResult.GetCollisionNormal().Y < MathF.Cos(FloorMaxAngle)) return false;
        if (_probeResult.GetCollider() is CollisionObject3D floor && (floor.CollisionLayer & NoStepLayers) != 0) return false;
        var destination = advanced + _probeResult.GetTravel();
        if (destination.Y - before.Y > WalkRules.StepHeight + WalkTuning.SafeMargin) return false;
        GlobalPosition = destination;
        FloorSnapLength = 0;
        Velocity = new(Velocity.X, 0, Velocity.Z);
        return true;
    }

    private void LimitSteepSlide()
    {
        for (int i = 0; i < GetSlideCollisionCount(); i++)
        {
            var normal = GetSlideCollision(i).GetNormal();
            if (normal.Y < MathF.Cos(WalkTuning.SlowSlideAngleDegrees * (float.Pi / 180f)) || normal.Y >= MathF.Cos(FloorMaxAngle)) continue;
            if (Velocity.Y >= 0) continue;
            var downhill = (Vector3.Down - normal * Vector3.Down.Dot(normal)).Normalized();
            float along = Velocity.Dot(downhill);
            if (along > WalkTuning.SlowSlideSpeed) Velocity -= downhill * (along - WalkTuning.SlowSlideSpeed);
        }
    }

    private Pose EyeAt(N.Vector3 feet) => new(feet + N.Vector3.UnitY * EyeHeight, _yaw, _pitch, _fov);

    private static void ValidatePose(Pose pose)
    {
        if (!VectorMath.Finite(pose.Position) || !float.IsFinite(pose.YawDeg) || !float.IsFinite(pose.PitchDeg))
            throw new ArgumentOutOfRangeException(nameof(pose));
    }

    public void OnMapUnloading(MapId from)
    {
        Leave();
        _map = null;
        _safePoints.Clear();
        _placementAlternatives.Clear();
        _placementArrival = null;
        _entryFrom = null;
        _placementNeedsCheck = false;
        _hasPlacement = false;
        _terrainPreparation = null; _entryTerrainPreparation = null; _landingPointsPreparation = null;
        _historyRestore = null;
        _sinceSafe = 0;
    }

    public void OnMapLoaded(IMapContext ctx)
    {
        _map = ctx;
        _terrain ??= KernelServices.Get<ITerrainPreparation>();
        var points = ctx.Descriptor.Spawns.Where(s => s.Stand is not null)
            .Select(s => new N.Vector2(s.Stand!.Feet.X, s.Stand.Feet.Z)).ToArray();
        _landingPointsPreparation = _terrain.PreparePoints(ctx, "walk.authored-landings", points);
    }
}
