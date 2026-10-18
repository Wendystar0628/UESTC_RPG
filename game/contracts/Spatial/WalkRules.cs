namespace Campus.Contracts;

/// <summary>Shared walking dimensions, speed steps and collision rules.</summary>
public static class WalkRules
{
    public const float Radius = .3f;
    public const float ArrivalCollisionRadius = 96f;
    public const float StandingHeight = 1.8f;
    public const float StandingEye = 1.62f;
    public const float CrouchingEye = 1.27f;
    public const float StepHeight = .35f;
    public const float FloorAngleDegrees = 45f;
    public const float BarrierHeight = 1.5f;
    public const float BoundaryHeight = 2.2f;
    public const float BoundarySlowdownDistance = .6f;
    public const float BuildingCollisionHeight = 300f;
    public const float DefaultDoorHeight = 2.1f;
    public const float MinimumDoorHeight = 2f;
    public const float UnknownEntranceCollisionHeight = 3.2f;
    public const float OutdoorFlightMaxRise = 1.5f;
    public const float WalkSpeed = 3.6f;
    public const int DefaultSpeedStep = 2;
    public const int SpeedStepCount = 5;

    public static float SpeedMultiplier(int step) => step switch
    {
        0 => .5f,
        1 => .7f,
        2 => 1f,
        3 => 1.4f,
        4 => 2f,
        _ => throw new ArgumentOutOfRangeException(nameof(step))
    };
}
