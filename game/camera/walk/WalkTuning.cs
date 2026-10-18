namespace Campus.Camera.Walk;

/// <summary>SYS-WALK baseline. Keep movement and comfort tuning together for the feel review.</summary>
public static class WalkTuning
{
    public const float SprintSpeed = 5.6f;
    public const float CrouchSpeed = 1.3f;
    public const float MaximumHorizontalSpeed = 12f;
    public const float SprintJumpBoost = 4f;
    public const float Gravity = 27.8f;
    public const float JumpSpeed = 8.35f;
    public const float TerminalFallSpeed = 50f;
    public const float GroundResponse = 12.1f;
    public const float AirResponse = 1.89f;
    public const float CrouchingHeight = 1.5f;
    public const float SnapLength = .5f;
    public const float SlowSlideAngleDegrees = 60f;
    public const float SlowSlideSpeed = 1.3f;
    public const float SafeMargin = .01f;
    public const float CoyoteSeconds = .1f;
    public const float JumpBufferSeconds = .12f;
    public const float HeldJumpInterval = .5f;
    public const float LandingLift = .3f;
    public const float LandingSpeed = 2f;
    public const float SprintFovMultiplier = 1.1f;
    public const float FovResponseSeconds = .2f;
    public const float StepUpCameraSeconds = .10f;
    public const float StepDownCameraSeconds = .06f;
    public const float MaximumStepCameraLag = .35f;
    public const float BobVerticalMetres = .03f;
    public const float BobLateralMetres = .015f;
    public const float BobRollDegrees = .4f;
    public const float BobSprintMultiplier = 1.3f;
    public const float BobCrouchMultiplier = .6f;
    public const float BobFullSpeed = 2f;
    public const float BobFadeSeconds = .2f;
    public const float StepFrequencyScale = 1.08f;
    public const float StepFrequencyPower = .54f;
    public const float MinimumStepFrequency = .9f;
    public const float MaximumStepFrequency = 3.6f;
    public const float SafetyRecordSeconds = .5f;
    public const float SafetyRecordSlopeDegrees = 20f;
    public const int SafetyPointCapacity = 16;
    public const float SaveRepairRadius = 50f;
    public const float SafetyReturnMinimumDistance = 3f;
    public const float SafetyReturnRepeatSeconds = 1f;
    public const float NearLandingDistance = 3f;
    public const float NearLandingSeconds = .5f;
    public const float DirectLandingHeight = 30f;
    public const float LandingSightDistance = 600f;
    public const int LandingSightHintIterations = 4;
    public const float BoundaryClearance = 1f;
    public const float BelowSupportLimit = 3f;
    public const float SaveSupportTolerance = .2f;
}
