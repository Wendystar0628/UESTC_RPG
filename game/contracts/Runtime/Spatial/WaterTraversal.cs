using System.Numerics;

namespace Campus.Contracts;

// Default values must fail closed: an uninitialized result is unavailable, not confirmed dry or permitted.
public enum WaterQueryStatus : byte { NotReady, NoWater, Ready, Invalid }
public enum WaterAccess : byte { Forbidden, WadeOnly, SurfaceSwim }
public enum WaterBottomKind : byte { Terrain, Plane }
public enum WaterExitKind : byte { SoftBank, SubmergedSteps, PoolSteps }

/// <summary>Point and known support space; null space does not authorize ignoring vertical water-volume boundaries.</summary>
public readonly record struct WaterQuery(Vector3 Position, NavSpace? Space = null);

/// <summary>Resolved water facts at Position, from a finite volume and the current map's immutable catalog.</summary>
/// <remarks>ITerrain.WaterY is the level source. BottomY is not the water surface. SurfaceSwim requires an explicit SwimAreaId.</remarks>
public readonly record struct WaterSample(
    Vector3 Position, FeatureId Water, string VolumeId, string? SwimAreaId, NavSpace? Space,
    float SurfaceY, float BottomY, WaterBottomKind BottomKind, float MinY, float MaxY, WaterAccess Access)
{
    public float ColumnDepth => SurfaceY - BottomY;
    public float DepthAtPoint => MathF.Max(0, SurfaceY - Position.Y);

    public void Validate()
    {
        if (!float.IsFinite(Position.X) || !float.IsFinite(Position.Y) || !float.IsFinite(Position.Z) ||
            !float.IsFinite(SurfaceY) || !float.IsFinite(BottomY) || !float.IsFinite(MinY) || !float.IsFinite(MaxY))
            throw new ArgumentException("Water sample geometry must be finite.");
        if (string.IsNullOrWhiteSpace(Water.Value) || string.IsNullOrWhiteSpace(VolumeId))
            throw new ArgumentException("Ready water requires a water feature and a volume identity.");
        if (MinY >= MaxY || BottomY < MinY || BottomY > SurfaceY || SurfaceY > MaxY || Position.Y < MinY || Position.Y > MaxY)
            throw new ArgumentException("Water sample heights are inconsistent with its finite volume.");
        if (!float.IsFinite(ColumnDepth) || !float.IsFinite(DepthAtPoint))
            throw new ArgumentException("Water sample depth exceeds the finite numeric range.");
        _ = BottomKind switch
        {
            WaterBottomKind.Terrain or WaterBottomKind.Plane => 0,
            _ => throw new ArgumentOutOfRangeException(nameof(BottomKind))
        };
        _ = Access switch
        {
            WaterAccess.Forbidden or WaterAccess.WadeOnly => 0,
            WaterAccess.SurfaceSwim when !string.IsNullOrWhiteSpace(SwimAreaId) => 0,
            WaterAccess.SurfaceSwim => throw new ArgumentException("Swimming permission requires a declared swim area."),
            _ => throw new ArgumentOutOfRangeException(nameof(Access))
        };
    }
}

/// <summary>A query outcome that cannot attach a fabricated sample to missing or invalid data.</summary>
public readonly record struct WaterQueryResult
{
    public WaterQueryResult(WaterQueryStatus status, WaterSample? sample = null,
        float? segmentT = null, string? diagnosticCode = null)
    {
        switch (status)
        {
            case WaterQueryStatus.Ready:
                if (sample is not { } water) throw new ArgumentException("Ready water requires a sample.", nameof(sample));
                water.Validate();
                if (segmentT is float t && (!float.IsFinite(t) || t < 0 || t > 1))
                    throw new ArgumentOutOfRangeException(nameof(segmentT));
                break;
            case WaterQueryStatus.NotReady:
            case WaterQueryStatus.NoWater:
            case WaterQueryStatus.Invalid:
                if (sample.HasValue || segmentT.HasValue) throw new ArgumentException("Unavailable or absent water cannot carry a hit.");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(status));
        }
        Status = status; Sample = sample; SegmentT = segmentT; DiagnosticCode = diagnosticCode;
    }

    public WaterQueryStatus Status { get; }
    public WaterSample? Sample { get; }
    /// <summary>Trace intersection parameter in [0,1]; null for Sample or a non-ready result.</summary>
    public float? SegmentT { get; }
    public string? DiagnosticCode { get; }
}

/// <summary>One declared, physically walkable exit. Vertical pool ladders are not traversable in P0.</summary>
/// <remarks>This is a runtime query value; no authoring file format or path is established by this record.</remarks>
public sealed record WaterExitDecl(
    string Id, string VolumeId, WaterExitKind Kind, Box3 Approach,
    Pose DryFeet, NavSpace? DrySpace, float ClearWidth);

/// <summary>WP-08's current-map query. Data remains available independently of visual water residency.</summary>
public interface IWaterTraversalQuery
{
    MapId Map { get; }
    long DataRevision { get; }
    WaterQueryResult Sample(in WaterQuery query);
    /// <summary>Earliest valid volume intersection. A Ready result includes Sample.Position and SegmentT.</summary>
    /// <remarks>Segment intersection does not bypass solid collision or grant access to an undeclared swim area.</remarks>
    WaterQueryResult Trace(Vector3 from, Vector3 to, NavSpace? space = null);
    /// <summary>Stable distance-then-ordinal-ID order, writing at most buffer.Length nearest exits.</summary>
    /// <remarks>Ready with written=0 means no exit nearby. NotReady/Invalid must write zero entries.</remarks>
    WaterQueryStatus ExitsNear(in WaterQuery query, float radius, Span<WaterExitDecl> buffer, out int written);
}

/// <summary>WP-42 movement configuration, never supplied by the visible character model.</summary>
/// <remarks>
/// Initial values come from WaterMotionRules.DefaultProfile. Entry and exit use standing column depth,
/// not feet immersion. Exit also requires support, head clearance and a declared exit.
/// Swimming speeds are independent of scroll speed. Deep wading is allowed only in an approved swim area.
/// </remarks>
public sealed record WaterMotionProfile(
    WalkBodyShape SwimmingShape, float FeetBelowSurface, float EyeAboveFeet,
    float EnterColumnDepth, float ExitColumnDepth, float Speed, float SprintSpeed, float StrokeDistance);

/// <summary>Shared water movement rules. They classify depth and movement; they never grant water access.</summary>
public static class WaterMotionRules
{
    /// <summary>Wade-only permission requires a supported water column strictly shallower than this limit.</summary>
    public const float ShallowWadeDepth = .3f;
    public const float WadeSpeedMultiplier = .6f;
    private const float DefaultSwimSpeed = 1.4f;

    /// <summary>Approved initial physical profile; stroke distance advances from accepted horizontal travel.</summary>
    public static WaterMotionProfile DefaultProfile { get; } = new(
        SwimmingShape: new(.4f, .9f), FeetBelowSurface: .75f, EyeAboveFeet: .87f,
        EnterColumnDepth: 1.05f, ExitColumnDepth: .95f,
        Speed: DefaultSwimSpeed, SprintSpeed: 2.2f, StrokeDistance: DefaultSwimSpeed * 1.33f);
}

public enum WaterMotionEventKind : byte { Enter, Exit, Stroke }
/// <summary>A committed physical edge; measured normal/tangent speed in m/s supplies intensity, not artistic sound gain.</summary>
public readonly record struct WaterMotionEvent(
    WalkStamp Stamp, ushort Ordinal, WaterMotionEventKind Kind,
    FeatureId Water, string VolumeId, Vector3 Position, float FeetDepth, float NormalSpeed, float TangentSpeed);

public interface IWaterMotionEvents
{
    /// <summary>Main-thread synchronous publication after Current for the same tick. No publisher backlog.</summary>
    /// <remarks>Freeze emits no motion edges. Warp/restore/map changes silently rebuild contact; discard old continuity events.
    /// Legacy WaterEnter/Exit/Sink keep their prior meaning; each consumer must select one water-event path to avoid double playback.</remarks>
    event Action<WaterMotionEvent>? Emitted;
}
