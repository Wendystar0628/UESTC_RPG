using System.Numerics;

namespace Campus.Contracts;

// An uninitialized result is unavailable, not confirmed dry or permitted.
public enum WaterQueryStatus : byte { NotReady, NoWater, Ready, Invalid }

/// <summary>Water surface and terrain bottom at the queried XZ point, with explicit swimming permission.</summary>
public readonly record struct WaterSample(float SurfaceY, float BottomY, bool CanSwim);

public readonly record struct WaterQueryResult
{
    public WaterQueryResult(WaterQueryStatus status, WaterSample? sample = null, string? diagnosticCode = null)
    {
        switch (status)
        {
            case WaterQueryStatus.Ready:
                if (sample is not { } water) throw new ArgumentException("Ready water requires a sample.", nameof(sample));
                if (!float.IsFinite(water.SurfaceY) || !float.IsFinite(water.BottomY) || water.BottomY > water.SurfaceY)
                    throw new ArgumentException("Water sample heights must be finite and ordered.", nameof(sample));
                break;
            case WaterQueryStatus.NotReady:
            case WaterQueryStatus.NoWater:
            case WaterQueryStatus.Invalid:
                if (sample.HasValue) throw new ArgumentException("Unavailable or absent water cannot carry a sample.", nameof(sample));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(status));
        }
        Status = status;
        Sample = sample;
        DiagnosticCode = diagnosticCode;
    }

    public WaterQueryStatus Status { get; }
    public WaterSample? Sample { get; }
    public string? DiagnosticCode { get; }
}

/// <summary>Current-map water query, available independently of visible water residency. Point is XZ.</summary>
public interface IWaterTraversalQuery
{
    WaterQueryResult Sample(Vector2 point);
}

/// <summary>Shared water movement rules; swimming permission comes from the water query.</summary>
public static class WaterMotionRules
{
    public const float ShallowWadeDepth = .3f;
    public const float WadeSpeedMultiplier = .6f;
    public static readonly WalkBodyShape SwimmingShape = new(.4f, .9f);
    public const float FeetBelowSurface = .75f;
    public const float EyeAboveSurface = .12f;
    public const float EnterColumnDepth = 1.05f;
    public const float ExitColumnDepth = .95f;
    public const float SwimSpeed = 1.4f;
}
