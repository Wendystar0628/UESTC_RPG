using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Kernel.Renderer;
using Godot;
using N = System.Numerics;

namespace Campus.Camera.Walk;

/// <summary>Physics tick, foot alternation, queued placement events and movement telemetry.</summary>
internal sealed class WalkMovementTelemetry(CharacterBody3D body, WalkBodyCollision collisionBody) : IWalkTelemetry
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
    public bool NextFootRight => _nextFootRight;
    public void BeginTick()
    {
        _telemetryTick++;
        FlushPlacementEvents();
    }
    public void QueueTeleport() => _teleportPending = true;
    public void QueueTakeoff(bool active) => _takeoffPending |= active;
    public void ClearTouchdown() => _touchdownPending = false;
    public void QueueTouchdown() => _touchdownPending = true;
    public void PublishWaterEnter(float depth) => _waterEnter?.Invoke(_telemetryTick, depth);
    public void PublishWaterExit() => _waterExit?.Invoke(_telemetryTick);
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

    public void ResetGait(float yaw)
    {
        _stepPhase = .5f;
        _nextFootRight = false;
        _telemetryYaw = yaw;
    }

    private void FlushPlacementEvents()
    {
        if (_teleportPending) { _teleportPending = false; Teleport?.Invoke(_telemetryTick); }
        if (_takeoffPending) { _takeoffPending = false; ModeTakeoff?.Invoke(_telemetryTick); }
    }

    public void PublishMovement(WalkMotionSample sample, float dt, bool wasGrounded, bool jumped, bool stepped, N.Vector3 incomingVelocity, float previousSpeed)
    {
        if (jumped) Jumped?.Invoke(_telemetryTick, WalkTuning.JumpSpeed);
        float distance = N.Vector2.Distance(new(sample.Feet.X, sample.Feet.Z), new(sample.PreviousFeet.X, sample.PreviousFeet.Z));
        float speed = distance / dt;
        if (sample.Grounded && !wasGrounded)
        {
            _stepPhase = .5f;
            Landed?.Invoke(_telemetryTick, MathF.Max(0, -incomingVelocity.Y), sample.Feet);
            if (_touchdownPending) { _touchdownPending = false; ModeTouchdown?.Invoke(_telemetryTick); }
        }
        else if (sample.Grounded && !jumped && distance > 0)
        {
            _stepPhase += dt * WalkMath.StepFrequency(speed);
            while (_stepPhase >= 1)
            {
                _stepPhase -= 1;
                PublishFootfall(speed);
                _nextFootRight = !_nextFootRight;
            }
        }
        float rise = sample.Feet.Y - sample.PreviousFeet.Y;
        if (sample.Grounded && wasGrounded && !jumped && MathF.Abs(rise) > WalkTuning.SafeMargin
            && MathF.Abs(rise) <= WalkRules.StepHeight + WalkTuning.SafeMargin
            && (stepped || body.GetFloorNormal().Y >= 1f - WalkTuning.SafeMargin))
        {
            if (rise > 0) StepUp?.Invoke(_telemetryTick, rise);
            else StepDown?.Invoke(_telemetryTick, -rise);
        }
        if (sample.Grounded && previousSpeed > speed) Skid?.Invoke(_telemetryTick, previousSpeed - speed);
        float turn = Angles.SignedDelta(_telemetryYaw, sample.Yaw);
        if (sample.Grounded && distance <= WalkTuning.SafeMargin && turn != 0) TurnInPlace?.Invoke(_telemetryTick, turn);
        _telemetryYaw = sample.Yaw;
        for (int i = 0; i < body.GetSlideCollisionCount(); i++)
        {
            var hit = body.GetSlideCollision(i);
            var normal = hit.GetNormal();
            if (normal.Y >= MathF.Cos(body.FloorMaxAngle)) continue;
            var velocity = incomingVelocity.ToGodot();
            float normalSpeed = velocity.Dot(normal);
            var tangent = velocity - normal * normalSpeed;
            var tag = hit.GetCollider() is CollisionObject3D collider ? WalkBodyCollision.CollisionTag(collider.CollisionLayer) : SurfaceTag.Wall;
            WallContact?.Invoke(new(_telemetryTick, hit.GetColliderId(), hit.GetColliderShapeIndex(),
                hit.GetPosition().ToNumerics(), normal.ToNumerics(), MathF.Abs(normalSpeed), tangent.Length(),
                sample.Input.Move.LengthSquared() > 0, tag));
        }
    }

    private void PublishFootfall(float speed)
    {
        if (collisionBody.SampleFootfall(_telemetryTick, _nextFootRight, speed) is { } footfall) Footfall?.Invoke(footfall);
    }

}
