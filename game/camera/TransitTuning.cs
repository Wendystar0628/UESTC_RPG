namespace Campus.Camera;

/// <summary>Destination settling after a loading-screen teleport (SYS-ATLAS-TRAVEL).</summary>
public static class TransitTuning
{
    /// <summary>Walking also waits for grounded feet, at most this long after the destination is ready.</summary>
    public const double WalkSettleSeconds = 1.5;
}
