namespace Campus.Flow;

/// <summary>Application-flow timing and handoff rules; timeline choreography remains in map cinematic data.</summary>
public static class FlowTuning
{
    public const float CoverSeconds = 0.4f;
    public const float RevealSeconds = 0.5f;
    public const float ReducedMotionSeconds = 0.5f;
    public const double SkipHoldSeconds = 0.8;
    public const int SkipFlowRate = 1440;
    public const double SaveTimeoutSeconds = 5;
    public const double WorldEndpointTimeoutSeconds = 65;
    public const double ClockTolerance = 0.000001;
}
