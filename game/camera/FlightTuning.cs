namespace Campus.Camera;

/// <summary>All flight feel values in one place for the visual director's review. Distances are metres.</summary>
public sealed record FlightTuning
{
    public int SpeedSteps { get; init; } = 16;
    public int DefaultSpeedStep { get; init; } = 9;
    public float MinSpeed { get; init; } = .5f;
    public float MaxSpeed { get; init; } = 200f;
    public float GroundSpeed { get; init; } = 6f;
    public float HeightSpeedFactor { get; init; } = .5f;
    public float SprintMultiplier { get; init; } = 2f;
    public float Acceleration { get; init; } = 20f;
    public float Deceleration { get; init; } = 15f;
    public float FlyFov { get; init; } = 60f;
    public float PitchLimit { get; init; } = 89f;
    public float SensitivityPercent { get; init; } = 100f;
    public float DoublePressSeconds { get; init; } = .35f;
    public float DoubleSpaceSeconds { get; init; } = .3f;
    public float SpaceTapSeconds { get; init; } = .2f;
    public float ModeSwitchCooldown { get; init; } = .5f;
    public float ReturnHoldSeconds { get; init; } = .6f;
    public float SkipHoldSeconds { get; init; } = .8f;
    public float FogTimeConstant { get; init; } = .5f;
    public float MaxFrameStep { get; init; } = .1f;

    public float SpeedAt(int step) => MinSpeed * MathF.Pow(MaxSpeed / MinSpeed,
        (Math.Clamp(step, 1, SpeedSteps) - 1f) / (SpeedSteps - 1));

    public float DegreesPerMouseCount
    {
        get
        {
            float f = .6f * (Math.Clamp(SensitivityPercent, 0, 200) / 200f) + .2f;
            return .15f * 8f * f * f * f;
        }
    }
}
