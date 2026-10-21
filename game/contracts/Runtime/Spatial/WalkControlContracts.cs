using System.Numerics;
using Campus.Contracts.Maps;

namespace Campus.Contracts;

/// <summary>GUI and input-lock filtered snapshot. Move.X is right and Move.Y is forward; default is idle.</summary>
/// <remarks>JumpPressed is a press edge: the producer must not repeat it across physics steps.</remarks>
public readonly record struct WalkInput(Vector2 Move, bool Sprint, bool JumpHeld, bool JumpPressed, bool Crouch);

/// <summary>game/camera/walk/ physical controller. It consumes snapshots and never depends on ICameraRig.</summary>
public interface IWalkController
{
    /// <summary>Interpolated render eye pose, including current eye height and stair smoothing.</summary>
    Pose EyePose { get; }
    /// <summary>Presentation-only lateral gait roll; never part of physical look or a saved pose.</summary>
    float EyeRollDeg { get; }
    /// <summary>Uninterpolated physical eye position, including current standing or crouching eye height.</summary>
    Vector3 ObserverEye { get; }
    /// <summary>Uninterpolated physical feet pose. Yaw and pitch retain the current look direction.</summary>
    Pose BodyPose { get; }
    Vector3 Velocity { get; }
    bool Grounded { get; }
    /// <summary>Derived directly from IViewControl.Mode; the camera alone owns mode changes.</summary>
    bool Active { get; }
    /// <summary>Terminal placement failure: physics is frozen until the camera handles the exit request.</summary>
    bool ExitRequested { get; }
    bool CollisionReady { get; }
    /// <summary>The actual arrival handle owned or adopted by the current placement, never an independent readiness flag.</summary>
    IMapArrival? PlacementArrival { get; }
    NavSpace? Space { get; }
    /// <summary>Zero-based speed setting, 0 through 4; 2 is the default 1x speed.</summary>
    int SpeedStep { get; }
    /// <summary>Counts explicit or exceptional safety returns, not ordinary entry or warps.</summary>
    int SafetyReturnCount { get; }
    /// <summary>Grounded, outside water, and not in a safety return or other unsettled move.</summary>
    bool IsSettled { get; }
    /// <summary>Pure settled snapshot, resolved from the same feet destination as the walk save section.</summary>
    Pose SettledEyePose { get; }

    /// <summary>Prepares a legal feet destination from an eye pose before the camera commits walk mode.</summary>
    /// <remarks>The result describes destination selection. Physics stays frozen until collision is ready.</remarks>
    ModeSwitchResult Enter(Pose from);
    /// <summary>Clears motion and pending input as the camera leaves walk mode; never changes mode itself.</summary>
    void Leave();
    void SetLook(float yawDeg, float pitchDeg);
    void SetInput(WalkInput input);
    /// <summary>Accepts 0 through 4; rejects other values.</summary>
    void SetSpeedStep(int step);
    /// <summary>Returns false and freezes physics when no legal safety destination is available.</summary>
    bool ReturnToSafety();
    /// <summary>Production feet placement for travel, elevators and restore; preserves Active.</summary>
    /// <remarks>Clears velocity, interpolation history and pending jump timing. Await collision before physics resumes.</remarks>
    void Warp(Pose feet);
}
