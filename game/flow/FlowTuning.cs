namespace Campus.Flow;

/// <summary>Playback review knobs; timeline choreography remains in map cinematic data.</summary>
public static class FlowTuning
{
    public const float CoverSeconds = 0.4f;
    public const float RevealSeconds = 0.5f;
    public const float ReducedMotionSeconds = 0.5f;
    public const double SkipHoldSeconds = 0.8;
    public const int SkipFlowRate = 1440;
    public const float CurveBakeInterval = 0.5f;
    public const float MaxYawRate = 45f;
    public const float MaxPitchRate = 30f;
    public const float DefaultUserFov = 60f;
    public const float ApproachBack = 180f;
    public const float ApproachRise = 220f;
    public const float HighDestination = 300f;
    public const float TransitClearance = 20f;
    public const float TransitHeightFactor = 0.18f;
    public const int TransitSamples = 16;
    public const double SaveTimeoutSeconds = 5;
    public const double WorldEndpointTimeoutSeconds = 65;
    public const double PresentationReadyTimeoutSeconds = 5;
    public const double DestinationWaitSeconds = 1.2;
    public const double WalkWaitSeconds = 1.5;
    public const double ClockTolerance = 0.000001;
}
