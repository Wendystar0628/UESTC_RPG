namespace Campus.Contracts;

/// <summary>Shared walking dimensions and collision rules, in metres unless named otherwise.</summary>
public static class WalkRules
{
    public const float Radius = .3f;
    public const float StandingHeight = 1.8f;
    public const float StepHeight = .35f;
    public const float FloorAngleDegrees = 45f;
    public const float BarrierHeight = 1.5f;
    public const float BoundaryHeight = 2.2f;
    public const float BuildingCollisionHeight = 300f;
    public const float DefaultDoorHeight = 2.1f;
    public const float MinimumDoorHeight = 2f;
    public const float UnknownEntranceCollisionHeight = 3.2f;
    public const float OutdoorFlightMaxRise = 1.5f;
}
