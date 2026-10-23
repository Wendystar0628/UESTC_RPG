namespace Campus.Contracts.Save;

/// <summary>The view section's single DTO, shared with asynchronous restore preparation.</summary>
public sealed record ViewSaveData(ViewMode Mode, Pose Pose, NavSpace? Space, int SpeedStep, int FlySpeedStep);

/// <summary>Walking persistence data; its disk shape and version are owned by the walk save section.</summary>
public sealed record WalkSaveData(Pose Feet, bool Crouching, int SpeedStep, IReadOnlyList<Pose> SafePoints)
{
    public Pose EyePose() => Feet with
    {
        Position = Feet.Position + System.Numerics.Vector3.UnitY *
            (Crouching ? WalkRules.CrouchingEye : WalkRules.StandingEye), FovDeg = null
    };
}

/// <summary>One immutable observer endpoint shared by maps, view, walk and metadata in a Save frame.</summary>
public sealed record ObserverSaveSnapshot(MapId Map, ViewSaveData View, WalkSaveData? Walk);
