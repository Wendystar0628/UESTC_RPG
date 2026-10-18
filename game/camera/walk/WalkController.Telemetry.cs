using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Kernel.Renderer;
using Godot;
using N = System.Numerics;

namespace Campus.Camera.Walk;

public partial class WalkController : IWalkTelemetry
{
    private ulong _telemetryTick;
    private float _stepPhase;
    private bool _nextFootRight;
    private bool _teleportPending;
    private bool _takeoffPending;
    private bool _touchdownPending;
    private float _telemetryYaw;
    private Action<ulong, float>? _waterEnter;
    private Action<ulong>? _waterExit;
    private Action<ulong>? _sink;
    private Action<ulong, float>? _elevatorDoor;

    public float StepPhase => _stepPhase;
    public event Action<FootfallInfo>? Footfall;
    public event Action<ulong, float>? Jumped;
    public event Action<ulong, float, N.Vector3>? Landed;
    public event Action<ulong, float>? StepUp;
    public event Action<ulong, float>? StepDown;
    public event Action<ulong, float>? Skid;
    public event Action<ContactInfo>? WallContact;
    // Traversal owners publish these when their physical state is committed, using this tick and phase.
    public event Action<ulong, float> WaterEnter { add => _waterEnter += value; remove => _waterEnter -= value; }
    public event Action<ulong> WaterExit { add => _waterExit += value; remove => _waterExit -= value; }
    public event Action<ulong> Sink { add => _sink += value; remove => _sink -= value; }
    public event Action<ulong, float> ElevatorDoor { add => _elevatorDoor += value; remove => _elevatorDoor -= value; }
    public event Action<ulong, float>? TurnInPlace;
    public event Action<ulong>? ModeTouchdown;
    public event Action<ulong>? ModeTakeoff;
    public event Action<ulong>? Teleport;

    private void ResetGait()
    {
        _stepPhase = .5f;
        _nextFootRight = false;
        _telemetryYaw = _yaw;
        ResetComfort();
    }

    private void FlushPlacementEvents()
    {
        if (_teleportPending) { _teleportPending = false; Teleport?.Invoke(_telemetryTick); }
        if (_takeoffPending) { _takeoffPending = false; ModeTakeoff?.Invoke(_telemetryTick); }
    }

    private void PublishMovement(float dt, bool wasGrounded, bool jumped, bool stepped, N.Vector3 incomingVelocity, float previousSpeed)
    {
        if (jumped) Jumped?.Invoke(_telemetryTick, WalkTuning.JumpSpeed);
        float distance = N.Vector2.Distance(new(_feet.X, _feet.Z), new(_previousFeet.X, _previousFeet.Z));
        float speed = distance / dt;
        if (Grounded && !wasGrounded)
        {
            _stepPhase = .5f;
            Landed?.Invoke(_telemetryTick, MathF.Max(0, -incomingVelocity.Y), _feet);
            if (_touchdownPending) { _touchdownPending = false; ModeTouchdown?.Invoke(_telemetryTick); }
        }
        else if (Grounded && !jumped && distance > 0)
        {
            _stepPhase += dt * WalkMath.StepFrequency(speed);
            while (_stepPhase >= 1)
            {
                _stepPhase -= 1;
                PublishFootfall(speed);
                _nextFootRight = !_nextFootRight;
            }
        }
        float rise = _feet.Y - _previousFeet.Y;
        if (Grounded && wasGrounded && !jumped && MathF.Abs(rise) > WalkTuning.SafeMargin
            && MathF.Abs(rise) <= WalkRules.StepHeight + WalkTuning.SafeMargin
            && (stepped || GetFloorNormal().Y >= 1f - WalkTuning.SafeMargin))
        {
            if (rise > 0) StepUp?.Invoke(_telemetryTick, rise);
            else StepDown?.Invoke(_telemetryTick, -rise);
        }
        if (Grounded && previousSpeed > speed) Skid?.Invoke(_telemetryTick, previousSpeed - speed);
        float turn = Angles.SignedDelta(_telemetryYaw, _yaw);
        if (Grounded && distance <= WalkTuning.SafeMargin && turn != 0) TurnInPlace?.Invoke(_telemetryTick, turn);
        _telemetryYaw = _yaw;
        for (int i = 0; i < GetSlideCollisionCount(); i++)
        {
            var hit = GetSlideCollision(i);
            var normal = hit.GetNormal();
            if (normal.Y >= MathF.Cos(FloorMaxAngle)) continue;
            var velocity = incomingVelocity.ToGodot();
            float normalSpeed = velocity.Dot(normal);
            var tangent = velocity - normal * normalSpeed;
            var tag = hit.GetCollider() is CollisionObject3D collider ? CollisionTag(collider.CollisionLayer) : SurfaceTag.Wall;
            WallContact?.Invoke(new(_telemetryTick, hit.GetColliderId(), hit.GetColliderShapeIndex(),
                hit.GetPosition().ToNumerics(), normal.ToNumerics(), MathF.Abs(normalSpeed), tangent.Length(),
                _input.Move.LengthSquared() > 0, tag));
        }
    }

    private void PublishFootfall(float speed)
    {
        if (!Probe(GlobalPosition + Vector3.Up * WalkTuning.SafeMargin,
            Vector3.Down * (WalkTuning.SnapLength + WalkTuning.SafeMargin))) return;
        var normal = _probeResult.GetCollisionNormal();
        var tag = _probeResult.GetCollider() is CollisionObject3D collider ? CollisionTag(collider.CollisionLayer) : SurfaceTag.Ground;
        Footfall?.Invoke(new(_telemetryTick, _nextFootRight ? FootSide.Right : FootSide.Left,
            _probeResult.GetCollisionPoint().ToNumerics(), speed, normal.ToNumerics(),
            _probeResult.GetColliderId(), _probeResult.GetColliderShape(), tag == SurfaceTag.Stair,
            MathF.Acos(Math.Clamp(normal.Y, -1, 1)) * (180f / float.Pi)));
    }

    private static SurfaceTag CollisionTag(uint layers) => layers switch
    {
        1u << (int)SurfaceTag.Ground => SurfaceTag.Ground,
        1u << (int)SurfaceTag.Stair => SurfaceTag.Stair,
        1u << (int)SurfaceTag.Barrier => SurfaceTag.Barrier,
        1u << (int)SurfaceTag.Wall => SurfaceTag.Wall,
        1u << (int)SurfaceTag.Prop => SurfaceTag.Prop,
        1u << (int)SurfaceTag.Water => SurfaceTag.Water,
        1u << (int)SurfaceTag.Invisible => SurfaceTag.Invisible,
        1u << (int)SurfaceTag.Boundary => SurfaceTag.Boundary,
        1u << (int)SurfaceTag.DoorClosed => SurfaceTag.DoorClosed,
        _ => throw new InvalidOperationException("Walking contact has no registered surface layer.")
    };
}
