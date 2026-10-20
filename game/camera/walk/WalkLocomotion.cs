using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Settings;
using N = System.Numerics;

namespace Campus.Camera.Walk;

/// <summary>Input latches, crouch, walking speed and jump integration.</summary>
internal sealed class WalkLocomotion(WalkBodyCollision collisionBody)
{
    private WalkInput _input;
    private bool _crouching;
    private bool _restoredCrouch;
    private bool _jumpQueued;
    private float _coyote;
    private float _jumpBuffer;
    private float _sinceJump = WalkTuning.HeldJumpInterval;

    public WalkInput Input => _input;
    public bool Crouching => _crouching;
    public int SpeedStep { get; private set; } = WalkRules.DefaultSpeedStep;

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

    public void Enter()
    {
        _crouching = false;
        _restoredCrouch = false;
    }

    public void RestoreCrouch(bool crouching)
    {
        _crouching = _restoredCrouch = crouching;
    }

    public void RestoreSpeedStep(int step) => SpeedStep = Math.Clamp(step, 0, WalkRules.SpeedStepCount - 1);

    public void ClearInput()
    {
        _input = default;
        _jumpQueued = false;
    }

    public void Freeze()
    {
        ClearInput();
        _jumpBuffer = 0;
    }

    public void ResetPlacement()
    {
        _coyote = _jumpBuffer = 0;
        _sinceJump = WalkTuning.HeldJumpInterval;
        ClearInput();
    }

    public void ChangeSwimming()
    {
        _crouching = _restoredCrouch = false;
        _coyote = _jumpBuffer = 0;
        _jumpQueued = false;
    }

    public void AdvanceJumpClock(float dt) => _sinceJump += dt;

    public bool PrepareInput(float dt, bool wasGrounded, bool swimming, N.Vector3 feet)
    {
        _coyote = wasGrounded ? WalkTuning.CoyoteSeconds : MathF.Max(0, _coyote - dt);
        _jumpBuffer = _jumpQueued ? WalkTuning.JumpBufferSeconds : MathF.Max(0, _jumpBuffer - dt);
        _jumpQueued = false;
        if (swimming) return false;
        bool desired = _input.Crouch || _restoredCrouch || (_crouching && !collisionBody.CanStand(feet));
        if (_crouching == desired) return false;
        _crouching = desired;
        return true;
    }

    public WalkVelocityPlan Integrate(float dt, bool wasGrounded, bool swimming, N.Vector3 feet,
        float yaw, N.Vector3 currentVelocity, WaterQueryResult water, IMapContext map,
        ICollisionWorld collision, ISettingsSection? settings)
    {
        float multiplier = WalkRules.SpeedMultiplier(SpeedStep);
        bool sprint = !swimming && _input.Sprint && !_crouching && _input.Move.Y > 0;
        float speed = swimming ? WaterMotionRules.SwimSpeed
            : (_crouching ? WalkTuning.CrouchSpeed : sprint ? WalkTuning.SprintSpeed : WalkRules.WalkSpeed) * multiplier;
        if (!swimming && (map.World.Spatial.WalkClassAt(feet.X, feet.Z) == WalkClass.Wade
            || (wasGrounded && water.Sample is { } wade && feet.Y < wade.SurfaceY))) speed *= WaterMotionRules.WadeSpeedMultiplier;
        var direction = WalkMath.Direction(_input.Move, yaw);
        float previousSpeed = new N.Vector2(currentVelocity.X, currentVelocity.Z).Length();
        var velocity = swimming ? direction * speed
            : WalkMath.HorizontalVelocity(currentVelocity, direction * speed, wasGrounded, dt);
        bool jump = !swimming && _coyote > 0 && (_jumpBuffer > 0 || (_input.JumpHeld && _sinceJump >= WalkTuning.HeldJumpInterval)
            || (wasGrounded && collisionBody.WantsAutoJump(velocity, dt, feet, _input, _crouching, _sinceJump, settings, map, collision)));
        if (jump)
        {
            velocity.Y = WalkTuning.JumpSpeed;
            if (sprint) velocity += WalkMath.Direction(new(0, 1), yaw) * (WalkTuning.SprintJumpBoost * multiplier);
            velocity = WalkMath.LimitHorizontal(velocity);
            _coyote = _jumpBuffer = _sinceJump = 0;
        }
        else if (!swimming) velocity.Y = MathF.Max(-WalkTuning.TerminalFallSpeed, velocity.Y - WalkTuning.Gravity * dt);
        return new(velocity, jump, sprint, previousSpeed);
    }
}

internal readonly record struct WalkVelocityPlan(N.Vector3 Velocity, bool Jumped, bool Sprint, float PreviousSpeed);

/// <summary>Read-only movement observation for gait and camera presentation; never a writable runtime state.</summary>
internal readonly record struct WalkMotionSample(N.Vector3 Feet, N.Vector3 PreviousFeet, float Yaw,
    bool Grounded, WalkInput Input, bool Crouching);
