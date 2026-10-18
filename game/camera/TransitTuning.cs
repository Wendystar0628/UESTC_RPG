namespace Campus.Camera;

/// <summary>Veil teleport timing (SYS-ATLAS-TRAVEL).</summary>
public static class TransitTuning
{
    public const float CoverSeconds = 0.3f;
    public const float RevealSeconds = 0.6f;
    /// <summary>Frames held after readiness so the pipeline and reflections settle before the veil lifts.</summary>
    public const int MinSettleFrames = 2;
    /// <summary>Walking also waits for grounded feet, at most this long after the destination is ready.</summary>
    public const double WalkSettleSeconds = 1.5;
}
