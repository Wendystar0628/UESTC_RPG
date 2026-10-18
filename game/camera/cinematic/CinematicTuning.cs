namespace Campus.Camera.Cinematic;

/// <summary>Camera path sampling and playback presentation rules.</summary>
public static class CinematicTuning
{
    public const float CurveBakeInterval = 0.5f;
    public const float MaxYawRate = 45f;
    public const float MaxPitchRate = 30f;
    public const float ApproachBack = 180f;
    public const float ApproachRise = 220f;
    public const float HighDestination = 300f;
    public const float TransitClearance = 20f;
    public const float TransitHeightFactor = 0.18f;
    public const int TransitSamples = 16;
    public const double PresentationReadyTimeoutSeconds = 5;
}
