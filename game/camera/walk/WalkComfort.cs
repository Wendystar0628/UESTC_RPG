using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Settings;
using Godot;
using N = System.Numerics;

namespace Campus.Camera.Walk;

/// <summary>Stair smoothing, gait presentation, FOV response.</summary>
internal sealed class WalkComfort(CharacterBody3D body, WalkMovementTelemetry telemetry)
{
    private float _stepCameraOffset;
    private float _previousStepCameraOffset;
    private float _bobWeight;
    private N.Vector3 _bobOffset;
    private N.Vector3 _previousBobOffset;
    private float _bobRoll;
    private float _previousBobRoll;

    private ISettingsSection? _uiSettings;
    private float _fov = UiSettingDefinitions.WalkFov.Default;

    public float Fov => _fov;
    private float WalkFov => _uiSettings?.Get(UiSettingDefinitions.WalkFov) ?? UiSettingDefinitions.WalkFov.Default;
    private static float RenderFraction => (float)Math.Clamp(Engine.GetPhysicsInterpolationFraction(), 0, 1);
    public void BindSettings(ISettingsSection? settings) => _uiSettings = settings;
    public void UseWalkFov() => _fov = WalkFov;
    public float EyeRollDeg(bool suppressed) => suppressed ? 0 : float.Lerp(_previousBobRoll, _bobRoll, RenderFraction);

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

    public void Reset()
    {
        _stepCameraOffset = _previousStepCameraOffset = 0;
        _bobWeight = _bobRoll = _previousBobRoll = 0;
        _bobOffset = _previousBobOffset = N.Vector3.Zero;
    }

    public Pose RenderEye(WalkMotionSample sample, float eyeHeight, float pitch)
    {
        float fraction = RenderFraction;
        var feet = N.Vector3.Lerp(sample.PreviousFeet, sample.Feet, fraction);
        feet.Y += float.Lerp(_previousStepCameraOffset, _stepCameraOffset, fraction);
        feet += N.Vector3.Lerp(_previousBobOffset, _bobOffset, fraction);
        return new(feet + N.Vector3.UnitY * eyeHeight, sample.Yaw, pitch, _fov);
    }

    public void AdvanceComfort(WalkMotionSample sample, IMapContext map, float dt, bool wasGrounded, bool jumped, bool stepped, bool sprint)
    {
        _previousStepCameraOffset = _stepCameraOffset;
        float rise = sample.Feet.Y - sample.PreviousFeet.Y;
        bool smallGroundStep = sample.Grounded && wasGrounded && !jumped
            && MathF.Abs(rise) <= WalkRules.StepHeight + WalkTuning.SafeMargin
            && (stepped || body.GetFloorNormal().Y >= 1f - WalkTuning.SafeMargin);
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
        float speed = N.Vector2.Distance(new(sample.Feet.X, sample.Feet.Z), new(sample.PreviousFeet.X, sample.PreviousFeet.Z)) / dt;
        float strength = BobStrength;
        float target = sample.Grounded && !jumped && !WalkSafetyQueries.InWater(map!.World.Spatial, sample.Feet)
            ? Math.Clamp(speed / WalkTuning.BobFullSpeed, 0, 1) : 0;
        _bobWeight += Math.Clamp(target - _bobWeight, -dt / WalkTuning.BobFadeSeconds, dt / WalkTuning.BobFadeSeconds);
        float scale = strength * _bobWeight * (sample.Crouching ? WalkTuning.BobCrouchMultiplier
            : sprint ? WalkTuning.BobSprintMultiplier : 1f);
        var local = WalkMath.BobOffset(telemetry.StepPhase, telemetry.NextFootRight, scale);
        var right = WalkMath.Direction(new(1, 0), sample.Yaw);
        _bobOffset = right * local.X + N.Vector3.UnitY * local.Y;
        _bobRoll = local.Z;
    }

    public void AdvanceFov(float dt, bool sprint, bool grounded, Vector3 requested)
    {
        float desiredFov = WalkFov * (sprint && grounded && requested.LengthSquared() > 0 ? WalkTuning.SprintFovMultiplier : 1f);
        _fov += (desiredFov - _fov) * (1f - MathF.Exp(-dt / WalkTuning.FovResponseSeconds));
    }
}
