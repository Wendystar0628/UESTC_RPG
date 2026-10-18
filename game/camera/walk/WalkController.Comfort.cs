using Campus.Contracts;
using Campus.Contracts.Settings;
using Campus.Kernel.Renderer;
using Godot;
using N = System.Numerics;

namespace Campus.Camera.Walk;

public partial class WalkController
{
    private float _stepCameraOffset;
    private float _previousStepCameraOffset;
    private float _bobWeight;
    private N.Vector3 _bobOffset;
    private N.Vector3 _previousBobOffset;
    private float _bobRoll;
    private float _previousBobRoll;

    public float EyeRollDeg => _appFrozen || _entryFrom is not null || _descentDuration > 0
        || _placementNeedsCheck || !Active ? 0 : float.Lerp(_previousBobRoll, _bobRoll, RenderFraction);

    private static float RenderFraction => (float)Math.Clamp(Engine.GetPhysicsInterpolationFraction(), 0, 1);

    private float BobStrength
    {
        get
        {
            var definition = UiSettingDefinitions.ViewBobbing;
            int value = _uiSettings?.Int(definition.Key, definition.Default, int.MinValue, int.MaxValue) ?? definition.Default;
            var options = definition.Options!;
            for (int i = 0; i < options.Count; i++)
                if (options[i].Value == value) return value / 100f;
            return definition.Default / 100f;
        }
    }

    private void ResetComfort()
    {
        _stepCameraOffset = _previousStepCameraOffset = 0;
        _bobWeight = _bobRoll = _previousBobRoll = 0;
        _bobOffset = _previousBobOffset = N.Vector3.Zero;
    }

    private Pose RenderEye()
    {
        float fraction = RenderFraction;
        var feet = N.Vector3.Lerp(_previousFeet, _feet, fraction);
        feet.Y += float.Lerp(_previousStepCameraOffset, _stepCameraOffset, fraction);
        return EyeAt(feet + N.Vector3.Lerp(_previousBobOffset, _bobOffset, fraction));
    }

    private void AdvanceComfort(float dt, bool wasGrounded, bool jumped, bool stepped, bool sprint)
    {
        _previousStepCameraOffset = _stepCameraOffset;
        float rise = _feet.Y - _previousFeet.Y;
        bool smallGroundStep = Grounded && wasGrounded && !jumped
            && MathF.Abs(rise) <= WalkRules.StepHeight + WalkTuning.SafeMargin
            && (stepped || GetFloorNormal().Y >= 1f - WalkTuning.SafeMargin);
        if (smallGroundStep)
        {
            _stepCameraOffset = Math.Clamp(_stepCameraOffset - rise,
                -WalkTuning.MaximumStepCameraLag, WalkTuning.MaximumStepCameraLag);
            float seconds = _stepCameraOffset < 0 ? WalkTuning.StepUpCameraSeconds : WalkTuning.StepDownCameraSeconds;
            _stepCameraOffset *= MathF.Exp(-dt / seconds);
        }
        else _stepCameraOffset = _previousStepCameraOffset = 0;

        _previousBobOffset = _bobOffset;
        _previousBobRoll = _bobRoll;
        float speed = N.Vector2.Distance(new(_feet.X, _feet.Z), new(_previousFeet.X, _previousFeet.Z)) / dt;
        float strength = BobStrength;
        float target = Grounded && !jumped && !WalkSafetyQueries.InWater(_map!.World.Spatial, _feet)
            ? Math.Clamp(speed / WalkTuning.BobFullSpeed, 0, 1) : 0;
        _bobWeight += Math.Clamp(target - _bobWeight, -dt / WalkTuning.BobFadeSeconds, dt / WalkTuning.BobFadeSeconds);
        float scale = strength * _bobWeight * (_crouching ? WalkTuning.BobCrouchMultiplier
            : sprint ? WalkTuning.BobSprintMultiplier : 1f);
        var local = WalkMath.BobOffset(_stepPhase, _nextFootRight, scale);
        var right = WalkMath.Direction(new(1, 0), _yaw);
        _bobOffset = right * local.X + N.Vector3.UnitY * local.Y;
        _bobRoll = local.Z;
    }

    private bool WantsAutoJump(N.Vector3 horizontalVelocity, float dt)
    {
        if (_crouching || _input.Move.LengthSquared() == 0 || _sinceJump < WalkTuning.HeldJumpInterval
            || !(_uiSettings?.Get(UiSettingDefinitions.AutoJump) ?? UiSettingDefinitions.AutoJump.Default)) return false;
        var motion = new Vector3(horizontalVelocity.X * dt, 0, horizontalVelocity.Z * dt);
        if (motion.LengthSquared() <= WalkTuning.SafeMargin * WalkTuning.SafeMargin
            || !Probe(GlobalPosition, motion) || _probeResult.GetCollisionNormal().Y >= MathF.Cos(FloorMaxAngle)
            || ProbeHasNoStepSurface()) return false;
        float apex = WalkTuning.JumpSpeed * WalkTuning.JumpSpeed / (2f * WalkTuning.Gravity);
        var lift = Vector3.Up * apex;
        if (Probe(GlobalPosition, lift)) return false;
        var ahead = motion.Normalized() * (WalkRules.Radius * 2f + motion.Length());
        var raised = GlobalPosition + lift;
        if (Probe(raised, ahead)) return false;
        var advanced = raised + ahead;
        if (!Probe(advanced, Vector3.Down * apex)
            || _probeResult.GetCollisionNormal().Y < MathF.Cos(FloorMaxAngle) || ProbeHasNoStepSurface()) return false;
        var destination = (advanced + _probeResult.GetTravel()).ToNumerics();
        float height = destination.Y - _feet.Y;
        if (height <= WalkRules.StepHeight + WalkTuning.SafeMargin || height > apex
            || !_collision!.Ready(destination, WalkRules.Radius)
            || !WalkSafetyQueries.AllowsFeet(_map!.World.Spatial, destination)) return false;
        _standingProbe.Height = WalkRules.StandingHeight;
        _clearance.Shape = _standingProbe;
        _clearance.Transform = new(Basis.Identity,
            (destination + N.Vector3.UnitY * (WalkRules.StandingHeight * .5f + WalkTuning.SafeMargin)).ToGodot());
        return GetWorld3D().DirectSpaceState.IntersectShape(_clearance, 1).Count == 0;
    }

    private bool ProbeHasNoStepSurface()
    {
        for (int i = 0; i < _probeResult.GetCollisionCount(); i++)
            if (_probeResult.GetCollider(i) is CollisionObject3D collider && (collider.CollisionLayer & NoStepLayers) != 0)
                return true;
        return false;
    }
}
