using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Maps;
using Campus.Contracts.Save;
using Campus.Kernel.Renderer;
using Godot;
using N = System.Numerics;
using Services = Campus.Kernel.Registry.Kernel;

namespace Campus.Camera.Walk;

/// <summary>Feet-origin cylinder; input ownership and camera orientation remain with WP-06.</summary>
[KernelService(typeof(IWalkController), Owner = "WP-42")]
[KernelNode("walk-controller", Owner = "WP-42")]
[SaveSection("walk", Owner = "WP-42", Version = 1, Phase = RestorePhase.Pose, Order = 61)]
public partial class WalkController : CharacterBody3D, IWalkController, IMapAware, ISaveSection<WalkSave>
{
    // WP-43 assigns one layer per SurfaceTag. Water is semantic, never a blocking surface.
    private const uint BlockingLayers = (1u << (int)SurfaceTag.Ground) | (1u << (int)SurfaceTag.Stair)
        | (1u << (int)SurfaceTag.Barrier) | (1u << (int)SurfaceTag.Wall) | (1u << (int)SurfaceTag.Prop)
        | (1u << (int)SurfaceTag.Invisible) | (1u << (int)SurfaceTag.Boundary) | (1u << (int)SurfaceTag.DoorClosed);
    private const uint NoStepLayers = (1u << (int)SurfaceTag.Barrier) | (1u << (int)SurfaceTag.Boundary)
        | (1u << (int)SurfaceTag.DoorClosed);
    private readonly WalkSafetyRing _safe = new();
    private readonly PhysicsTestMotionParameters3D _probe = new();
    private readonly PhysicsTestMotionResult3D _probeResult = new();
    private readonly CylinderShape3D _cylinder = new() { Radius = WalkTuning.Radius, Height = WalkTuning.StandingHeight };
    private readonly CylinderShape3D _standingProbe = new() { Radius = WalkTuning.Radius, Height = WalkTuning.StandingHeight };
    private readonly PhysicsShapeQueryParameters3D _clearance = new();
    private WalkPlacementAdmission? _admission;
    private CollisionShape3D? _shape;
    private IMapContext? _map;
    private ICollisionWorld? _collision;
    private IWorldState? _world;
    private IAppFlow? _appFlow;
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
    private float _sinceReturn = WalkTuning.ReturnRetrySeconds;
    private int _returnSkip;
    private bool _placementNeedsCheck;
    private float _placementLift;
    private bool _safetyFailed;
    private N.Vector3? _rejectedLanding;
    private float _fov = WalkTuning.WalkFov;
    private Pose _descentFrom;
    private float _descentDuration;
    private float _descentElapsed;
    private long _worldInstance = -1;

    public bool Active { get; private set; }
    public bool Grounded { get; private set; }
    public bool CollisionReady { get; private set; }
    public int SpeedStep { get; private set; } = WalkTuning.DefaultSpeedStep;
    public int SafetyReturnCount { get; private set; }
    public bool IsSettled => !Active || (Grounded && CollisionReady && _descentDuration <= 0 && !_safetyFailed
        && _map is not null && !WalkSafetyQueries.InWater(_map.World.Spatial, _feet));
    public Pose BodyPose => new(_feet, _yaw, _pitch);
    public N.Vector3 ObserverEye => _feet + N.Vector3.UnitY * EyeHeight;
    N.Vector3 IWalkController.Velocity => Velocity.ToNumerics();

    public Pose SettledEyePose => EyeAt(ResolveSettledFeet().Position);
    public NavSpace? Space
    {
        get
        {
            if (_map is null) return null;
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
            if (_appFrozen) return EyeAt(_feet);
            if (_descentDuration > 0)
            {
                float t = WalkMath.SmoothStep(_descentElapsed / _descentDuration);
                var destination = _feet + N.Vector3.UnitY * EyeHeight;
                var position = N.Vector3.Lerp(_descentFrom.Position, destination, t);
                // Height approaches the landing point logarithmically; the final .3 m uses real physics.
                float height = _descentFrom.Position.Y - destination.Y;
                if (height > 0) position.Y = destination.Y + MathF.Exp(MathF.Log(1f + height) * (1f - t)) - 1f;
                return new(position, _yaw, _pitch, _fov);
            }
            float fraction = (float)Math.Clamp(Engine.GetPhysicsInterpolationFraction(), 0, 1);
            return EyeAt(N.Vector3.Lerp(_previousFeet, _feet, fraction));
        }
    }
    private float EyeHeight => _crouching ? WalkTuning.CrouchingEye : WalkTuning.StandingEye;

    public WalkController()
    {
        Name = "WalkController";
        ProcessPriority = FramePriority.Camera;
        PhysicsInterpolationMode = PhysicsInterpolationModeEnum.Off;
        MotionMode = MotionModeEnum.Grounded;
        UpDirection = Vector3.Up;
        CollisionLayer = 0;
        CollisionMask = BlockingLayers;
        FloorMaxAngle = WalkTuning.FloorAngleDegrees * (float.Pi / 180f);
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

    public override void _ExitTree()
    {
        _admission?.Dispose();
        _admission = null;
    }

    private void ResolveServices()
    {
        _collision ??= Services.Get<ICollisionWorld>();
        _world ??= Services.Get<IWorldState>();
        _map = Services.Get<IMapService>().Context;
    }

    public ModeSwitchResult Enter(Pose from)
    {
        ValidatePose(from);
        ResolveServices();
        if (_map is null) return ModeSwitchResult.Refused;
        var target = FindLanding(from, out var landingCandidate);
        bool relocated = false;
        if (target is null)
        {
            target = _map.World.Spatial.NearestSafe(landingCandidate, WalkTuning.SafetySearchRadius);
            relocated = true;
        }
        if (target is not { } feet || !ValidSurface(feet)) return ModeSwitchResult.Refused;
        Active = true;
        _appFrozen = false;
        _safetyFailed = false;
        _crouching = false;
        _restoredCrouch = false;
        ApplyHeight();
        Place(new(feet, from.YawDeg, from.PitchDeg), WalkTuning.LandingLift);
        _descentFrom = from;
        float height = from.Position.Y - (feet.Y + EyeHeight);
        float distance = N.Vector3.Distance(from.Position, feet + N.Vector3.UnitY * EyeHeight);
        _descentDuration = distance > 3f ? WalkMath.DescentSeconds(MathF.Max(height, distance)) : 0;
        _descentElapsed = 0;
        Velocity = Vector3.Down * WalkTuning.LandingSpeed;
        return relocated ? ModeSwitchResult.Relocated : ModeSwitchResult.Ok;
    }

    public void Leave()
    {
        Active = false;
        _appFrozen = false;
        _input = default;
        _jumpQueued = false;
        _descentDuration = 0;
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
        _ = WalkTuning.SpeedMultiplier(step);
        SpeedStep = step;
    }

    public void Warp(Pose feet) => Place(feet, 0);

    private void Place(Pose feet, float lift)
    {
        ValidatePose(feet);
        SetLook(feet.YawDeg, feet.PitchDeg);
        var support = _map is { } map ? WalkPlacementAdmission.ResolveSupport(map.World.Spatial, feet.Position) : null;
        _feet = _previousFeet = (support ?? feet.Position) + N.Vector3.UnitY * lift;
        if (IsInsideTree()) GlobalPosition = _feet.ToGodot();
        else Position = _feet.ToGodot();
        Velocity = Vector3.Zero;
        Grounded = false;
        CollisionReady = false;
        _coyote = _jumpBuffer = 0;
        _sinceJump = WalkTuning.HeldJumpInterval;
        _jumpQueued = false;
        _descentDuration = _descentElapsed = 0;
        _placementNeedsCheck = true;
        _placementLift = lift;
        _safetyFailed = false;
        _fov = WalkTuning.WalkFov;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!Active || _shape is null || _map is null || _collision is null || _safetyFailed) return;
        // Resolve only after the node is live, never while constructing the flow/save/view dependency graph.
        _appFlow ??= Services.Get<IAppFlow>();
        CollisionReady = _collision.Ready(_feet, WalkTuning.Radius);
        var runtimeVelocity = Velocity.ToNumerics();
        _appFrozen = WalkMath.FreezeForAppState(_appFlow.State, _feet, ref _previousFeet, ref runtimeVelocity);
        if (_appFrozen)
        {
            Velocity = runtimeVelocity.ToGodot();
            _input = default;
            _jumpQueued = false;
            _jumpBuffer = 0;
            return;
        }
        float dt = (float)delta;
        if (!(dt > 0) || !float.IsFinite(dt)) return;
        if (_world is { } world && world.Instance != _worldInstance)
        {
            _worldInstance = world.Instance;
            _previousFeet = _feet;
            _fov = WalkTuning.WalkFov;
        }
        // Ready covers the current 64 m tile plus eight neighbours. Never integrate into an unmounted tile.
        if (!CollisionReady) { _previousFeet = _feet; return; }
        if (_placementNeedsCheck)
        {
            _placementNeedsCheck = false;
            if (!_admission!.Accept(_map.World.Spatial, _collision, _feet,
                _crouching ? WalkTuning.CrouchingHeight : WalkTuning.StandingHeight, _placementLift))
            {
                _rejectedLanding = _feet;
                Recover(false);
                return;
            }
        }
        if (_descentDuration > 0)
        {
            _descentElapsed += dt;
            if (_descentElapsed < _descentDuration) return;
            _descentDuration = 0;
            _previousFeet = _feet;
            Velocity = Vector3.Down * WalkTuning.LandingSpeed;
        }
        _sinceReturn += dt;
        _sinceJump += dt;
        _sinceSafe += dt;
        bool wasGrounded = Grounded;
        _coyote = wasGrounded ? WalkTuning.CoyoteSeconds : MathF.Max(0, _coyote - dt);
        _jumpBuffer = _jumpQueued ? WalkTuning.JumpBufferSeconds : MathF.Max(0, _jumpBuffer - dt);
        _jumpQueued = false;
        UpdateCrouch();
        float multiplier = WalkTuning.SpeedMultiplier(SpeedStep);
        bool sprint = _input.Sprint && !_crouching && _input.Move.Y > 0;
        float speed = (_crouching ? WalkTuning.CrouchSpeed : sprint ? WalkTuning.SprintSpeed : WalkTuning.WalkSpeed) * multiplier;
        if (_map.World.Spatial.WalkClassAt(_feet.X, _feet.Z) == WalkClass.Wade) speed *= .6f;
        var direction = WalkMath.Direction(_input.Move, _yaw);
        var velocity = WalkMath.HorizontalVelocity(Velocity.ToNumerics(), direction * speed, wasGrounded, dt);
        bool jump = _coyote > 0 && (_jumpBuffer > 0 || (_input.JumpHeld && _sinceJump >= WalkTuning.HeldJumpInterval));
        if (jump)
        {
            velocity.Y = WalkTuning.JumpSpeed;
            if (sprint) velocity += WalkMath.Direction(new(0, 1), _yaw) * (WalkTuning.SprintJumpBoost * multiplier);
            velocity = WalkMath.LimitHorizontal(velocity);
            _coyote = _jumpBuffer = _sinceJump = 0;
        }
        else velocity.Y = MathF.Max(-WalkTuning.TerminalFallSpeed, velocity.Y - WalkTuning.Gravity * dt);
        var requested = new Vector3(velocity.X * dt, 0, velocity.Z * dt);
        if (_crouching && wasGrounded && !jump) requested = LimitCrouchEdge(requested);
        velocity.X = requested.X / dt;
        velocity.Z = requested.Z / dt;
        var next = _feet + new N.Vector3(requested.X, 0, requested.Z);
        if (!_collision.Ready(next, WalkTuning.Radius)) { CollisionReady = false; _previousFeet = _feet; return; }
        _previousFeet = _feet;
        Velocity = velocity.ToGodot();
        FloorSnapLength = jump ? 0 : WalkTuning.SnapLength;
        MoveAndSlide();
        bool stepped = false;
        if (wasGrounded && !jump && IsOnWall()) stepped = TryStep(requested, _previousFeet.ToGodot());
        if (stepped) Velocity = new(velocity.X, 0, velocity.Z);
        if (IsOnCeiling()) Velocity = new(Velocity.X, MathF.Min(0, Velocity.Y), Velocity.Z);
        LimitSteepSlide();
        Grounded = IsOnFloor() || stepped;
        _feet = GlobalPosition.ToNumerics();
        if (NeedsRecovery()) { Recover(false); return; }
        if (Grounded) _rejectedLanding = null;
        if (Grounded && _sinceSafe >= WalkTuning.SafetyRecordSeconds && IsSafeRecordPoint())
        {
            _safe.Add(BodyPose);
            _sinceSafe = 0;
        }
        float desiredFov = WalkTuning.WalkFov * (sprint && Grounded && requested.LengthSquared() > 0 ? WalkTuning.SprintFovMultiplier : 1f);
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
        _cylinder.Height = _crouching ? WalkTuning.CrouchingHeight : WalkTuning.StandingHeight;
        if (_shape is not null) _shape.Position = Vector3.Up * (_cylinder.Height * .5f);
    }

    private bool CanStand()
    {
        // Existing crouch volume is already collision-resolved. Query only the added crown, so a sloped
        // supporting floor does not falsely count as an overhead obstruction during the full-cylinder query.
        float extra = WalkTuning.StandingHeight - WalkTuning.CrouchingHeight;
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
        var lift = Vector3.Up * WalkTuning.StepHeight;
        if (Probe(current, lift)) lift = _probeResult.GetTravel();
        if (lift.Y <= WalkTuning.SafeMargin) return false;
        var raised = current + lift;
        if (Probe(raised, remaining)) return false;
        var advanced = raised + remaining;
        if (!Probe(advanced, Vector3.Down * (lift.Y + WalkTuning.SnapLength))) return false;
        if (_probeResult.GetCollisionNormal().Y < MathF.Cos(FloorMaxAngle)) return false;
        if (_probeResult.GetCollider() is CollisionObject3D floor && (floor.CollisionLayer & NoStepLayers) != 0) return false;
        var destination = advanced + _probeResult.GetTravel();
        if (destination.Y - before.Y > WalkTuning.StepHeight + WalkTuning.SafeMargin) return false;
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

    private N.Vector3? FindLanding(Pose from, out N.Vector3 candidate)
    {
        var spatial = _map!.World.Spatial;
        float ground = spatial.GroundY(from.Position.X, from.Position.Z);
        candidate = new(from.Position.X, ground, from.Position.Z);
        if (from.Position.Y - ground > 30f)
        {
            float pitch = from.PitchDeg * (float.Pi / 180f);
            var direction = WalkMath.Direction(new(0, 1), from.YawDeg) * MathF.Cos(pitch);
            direction.Y = MathF.Sin(pitch);
            for (float distance = 2; distance <= 600; distance += 2)
            {
                var point = from.Position + direction * distance;
                float? y = spatial.SupportY(point.X, point.Z, point.Y + 2f);
                if (y is { } floor && point.Y <= floor)
                {
                    candidate = new(point.X, floor, point.Z);
                    return WalkMath.CanWalk(spatial.WalkClassAt(point.X, point.Z)) ? candidate : null;
                }
            }
        }
        var support = WalkPlacementAdmission.ResolveSupport(spatial, from.Position);
        if (support is { } known) candidate = known;
        return support;
    }

    private bool ValidSurface(N.Vector3 feet)
    {
        if (_map is null || !WalkMath.Finite(feet)) return false;
        return WalkPlacementAdmission.ResolveSupport(_map.World.Spatial, feet) is not null;
    }

    private bool NeedsRecovery() => WalkSafetyQueries.NeedsRecovery(_map!.World.Spatial, _feet);

    private bool IsSafeRecordPoint()
    {
        if (!Grounded || _map is null || _collision is null || !ValidSavedFeet(_feet)) return false;
        if (GetFloorNormal().Y < MathF.Cos(WalkTuning.SafetyRecordSlopeDegrees * (float.Pi / 180f))) return false;
        var spatial = _map.World.Spatial;
        if (spatial.WalkClassAt(_feet.X, _feet.Z) is not (WalkClass.Walk or WalkClass.Interior)) return false;
        // Conservative radial samples exclude water, cliffs, forbidden cells and boundaries without a new contract.
        ReadOnlySpan<N.Vector2> offsets = [new(1, 0), new(-1, 0), new(0, 1), new(0, -1),
            new(.7071f, .7071f), new(-.7071f, .7071f), new(.7071f, -.7071f), new(-.7071f, -.7071f)];
        foreach (var direction in offsets)
        {
            var check = _feet + new N.Vector3(direction.X, 0, direction.Y) * WalkTuning.BoundaryClearance;
            if (spatial.WalkClassAt(check.X, check.Z) is not (WalkClass.Walk or WalkClass.Interior)) return false;
            var support = spatial.SupportY(check.X, check.Z, _feet.Y + .2f);
            if (support is null || MathF.Abs(support.Value - _feet.Y) > .2f) return false;
            var hit = _collision.Sweep(_feet + N.Vector3.UnitY * WalkTuning.SafeMargin,
                check + N.Vector3.UnitY * WalkTuning.SafeMargin, WalkTuning.Radius, _cylinder.Height);
            if (hit is { Tag: SurfaceTag.Boundary }) return false;
            if (hit is { } obstruction && obstruction.T * WalkTuning.BoundaryClearance < WalkTuning.SafetyClearance)
                return false;
        }
        return true;
    }

    public bool ReturnToSafety() => Active && Recover(true);

    private bool Recover(bool manual)
    {
        if (_map is null) return false;
        if (_sinceReturn >= WalkTuning.ReturnRetrySeconds) _returnSkip = 0;
        var target = WalkPlacementAdmission.Recover(_map.World.Spatial, _safe, BodyPose, manual, _returnSkip, _rejectedLanding);
        if (target is not { } destination)
        {
            _safetyFailed = true;
            Active = false;
            CollisionReady = false;
            Velocity = Vector3.Zero;
            return false;
        }
        _returnSkip = _safe.IndexOf(destination) + 1;
        Warp(destination);
        _sinceReturn = 0;
        SafetyReturnCount++;
        return true;
    }

    private Pose ResolveSettledFeet()
    {
        if (IsSettled && ValidSavedFeet(_feet)) return BodyPose;
        var recorded = _safe.Nearest(_feet, 0, 0, p => ValidSavedFeet(p.Position));
        if (recorded is { } safe) return safe with { YawDeg = _yaw, PitchDeg = _pitch };
        if (_map?.World.Spatial.NearestSafe(_feet, WalkTuning.SafetySearchRadius) is { } global && ValidSavedFeet(global))
            return new(global, _yaw, _pitch);
        var landing = _feet - N.Vector3.UnitY * WalkTuning.LandingLift;
        if (_descentDuration > 0 && ValidSavedFeet(landing)) return new(landing, _yaw, _pitch);
        if (ValidSavedFeet(_feet)) return BodyPose;
        if (!Active && _map is { } map && DefaultFeet(map.Descriptor.Id) is { } start && ValidSavedFeet(start.Position)) return start;
        throw new InvalidOperationException("Cannot capture a walk position without supported, dry feet.");
    }

    private Pose EyeAt(N.Vector3 feet) => new(feet + N.Vector3.UnitY * EyeHeight, _yaw, _pitch, _fov);

    private static void ValidatePose(Pose pose)
    {
        if (!WalkMath.Finite(pose.Position) || !float.IsFinite(pose.YawDeg) || !float.IsFinite(pose.PitchDeg))
            throw new ArgumentOutOfRangeException(nameof(pose));
    }

    public void OnMapUnloading(MapId from)
    {
        Leave();
        _map = null;
        _safe.Clear();
        _rejectedLanding = null;
        _sinceSafe = 0;
    }

    public void OnMapLoaded(IMapContext ctx) => _map = ctx;
}
