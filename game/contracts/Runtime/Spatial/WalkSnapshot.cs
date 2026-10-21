using System.Numerics;
using Campus.Contracts.Audio;

namespace Campus.Contracts;

/// <summary>Physical publication identity. Continuity changes on non-continuous placement; Tick shares IWalkTelemetry's counter.</summary>
public readonly record struct WalkStamp(MapId Map, long MapRevision, long WorldInstance, ulong Continuity, ulong Tick);

public enum WalkPresence : byte { Absent, Pending, Live }
public enum WalkMotion : byte { Ground, Air, Wade }
public enum WalkPosture : byte { Standing, Crouching }
[Flags]
public enum WalkHold : ushort
{
    None = 0, CollisionNotReady = 1,
    MapTransition = 4, PhysicsPaused = 8, PlacementInvalid = 16
}
public enum WalkTransition : byte { None = 0, SafetyReturn = 2, Elevator = 3 }
public enum WalkReset : byte
{
    None, ModeChanged, Teleport, SafetyReturn, Elevator, MapChanged, Restore, NewGame, Resume, DataReload
}

/// <summary>The controller's actual upright cylinder, independent of the visible character model.</summary>
public readonly record struct WalkBodyShape(float Radius, float Height);
public readonly record struct WalkSupport(Vector3 Point, Vector3 Normal, SurfaceTag Tag, NavSpace? Space);

/// <summary>game/camera/walk/'s committed physical truth, published on the main thread at the end of a fixed physics step.</summary>
/// <remarks>
/// Feet is the active cylinder's bottom centre; PhysicalEye is not interpolated. Velocity is actual accepted motion,
/// not input intent or a camera-arm velocity; a relocation never becomes a large velocity.
/// Mode/Presence gate all position, shape, support and phase fields. Absent/Pending values are not usable physical evidence.
/// StepPhase is the same phi as IWalkTelemetry. NextFoot is Left/Right or null.
/// Body visibility is not a field: disabling the model must not change physical perception or the simulation.
/// </remarks>
public readonly record struct WalkSnapshot(
    WalkStamp Stamp,
    ViewMode Mode,
    WalkPresence Presence,
    Vector3 Feet,
    Vector3 PhysicalEye,
    float YawDeg,
    float PitchDeg,
    Vector3 Velocity,
    WalkMotion Motion,
    WalkPosture Posture,
    WalkBodyShape Shape,
    WalkHold Hold,
    WalkTransition Transition,
    WalkReset Reset,
    bool Grounded,
    NavSpace? Space,
    WalkSupport? Support,
    float StepPhase,
    FootSide? NextFoot)
{
    /// <summary>Only a validated Walk body in Playing is perceptible; the producer encodes that admission in Presence.</summary>
    public bool Perceptible => Mode switch
    {
        ViewMode.Fly => false,
        ViewMode.Walk => Presence == WalkPresence.Live,
        _ => throw new ArgumentOutOfRangeException(nameof(Mode))
    };

    /// <summary>Whether a consumer may interpolate continuous values from the immediately previous publication.</summary>
    /// <remarks>Discrete state, space and shape are always read from Current.</remarks>
    public bool CanInterpolateFrom(in WalkSnapshot previous) =>
        Perceptible && previous.Perceptible &&
        Stamp.Map == previous.Stamp.Map && Stamp.MapRevision == previous.Stamp.MapRevision &&
        Stamp.WorldInstance == previous.Stamp.WorldInstance && Stamp.Continuity == previous.Stamp.Continuity &&
        Stamp.Tick > 0 && previous.Stamp.Tick == Stamp.Tick - 1 &&
        Reset == WalkReset.None && previous.Reset == WalkReset.None &&
        Hold == WalkHold.None && previous.Hold == WalkHold.None &&
        Posture == previous.Posture && Shape == previous.Shape;
}

public interface IWalkSnapshot
{
    WalkSnapshot Previous { get; }
    WalkSnapshot Current { get; }
    /// <summary>Same fraction as the controller's render eye. Return 1 when CanInterpolateFrom is false.</summary>
    /// <remarks>On discontinuity the producer immediately sets Previous=Current, including Warp called outside a physics step.</remarks>
    float Fraction { get; }
}
