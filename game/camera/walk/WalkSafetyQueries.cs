using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Save;

namespace Campus.Camera.Walk;

/// <summary>Pure position hints and registered region rules; native support is resolved after collision mounts.</summary>
public static class WalkSafetyQueries
{
    public static WalkSave CreateDefault(IMapService maps, NewGameContext context) =>
        new(DefaultFeet(maps, context.Map), false, WalkRules.DefaultSpeedStep, []);

    public static Pose DefaultFeet(IMapService maps, MapId targetMap)
    {
        var spawn = maps.ResolveSpawn(targetMap);
        if (spawn.Stand is { } stand)
        {
            var pose = new Pose(stand.Feet, stand.YawDeg, 0);
            if (!ValidPose(pose)) throw new InvalidDataException($"Walk default for {targetMap.Value} has an invalid authored stand.");
            return pose;
        }
        if (!ValidPose(spawn.Fly)) throw new InvalidDataException($"Walk default for {targetMap.Value} has an invalid fly pose.");
        // A fly-only default is a pending ray origin, never a claim that its feet have support.
        return spawn.Fly;
    }

    public static bool ValidPose(Pose pose) => VectorMath.Finite(pose.Position)
        && float.IsFinite(pose.YawDeg) && float.IsFinite(pose.PitchDeg);

    public static bool InWater(ISpatialIndex spatial, Vector3 feet) =>
        spatial.MediumAt(feet).Kind == MediumKind.Underwater;

    public static bool AllowsFeet(ISpatialIndex spatial, Vector3 feet) => spatial.WalkClassAt(feet.X, feet.Z) switch
    {
        WalkClass.Walk or WalkClass.Interior or WalkClass.Wade => true,
        WalkClass.Water => !InWater(spatial, feet),
        WalkClass.Building or WalkClass.Forbidden or WalkClass.Outside or WalkClass.Steep => false,
        _ => throw new ArgumentOutOfRangeException(nameof(feet))
    };

    public static bool NeedsRecovery(ISpatialIndex spatial, Vector3 feet)
    {
        if (!VectorMath.Finite(feet) || !AllowsFeet(spatial, feet)) return true;
        var medium = spatial.MediumAt(feet);
        if (spatial.WalkClassAt(feet.X, feet.Z) != WalkClass.Wade && medium.Kind == MediumKind.Underwater
            && (!float.IsFinite(medium.Depth) || medium.Depth > WaterMotionRules.ShallowWadeDepth)) return true;
        return feet.Y < spatial.GroundY(feet.X, feet.Z) - WalkTuning.BelowSupportLimit;
    }
}
