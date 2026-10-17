using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Save;

namespace Campus.Camera.Walk;

/// <summary>Spatial safety is available before native collision tiles mount; water depth is never inferred from support height.</summary>
public static class WalkSafetyQueries
{
    public static WalkSave CreateDefault(IMapService maps, NewGameContext context) =>
        new(DefaultFeet(maps, context.Map), false, WalkTuning.DefaultSpeedStep, []);

    public static Pose DefaultFeet(IMapService maps, MapId targetMap)
    {
        // The catalogue also resolves waypoints. Never substitute the first named spawn or map centre.
        var spawn = maps.ResolveSpawn(targetMap);
        if (spawn.Stand is { } stand)
        {
            if (!WalkMath.Finite(stand.Feet) || !float.IsFinite(stand.YawDeg))
                throw new InvalidDataException($"Walk default for {targetMap.Value} has an invalid authored stand.");
            // Authored feet may be read before Travel mounts the target. Restore checks target support later.
            return new(stand.Feet, stand.YawDeg, 0);
        }
        if (maps.Current != targetMap || maps.Context is not { } context || context.Descriptor.Id != targetMap)
            throw new InvalidOperationException($"Walk default for {targetMap.Value} has no stand and its world is not mounted.");
        if (!WalkMath.Finite(spawn.Fly.Position) || !float.IsFinite(spawn.Fly.YawDeg) || !float.IsFinite(spawn.Fly.PitchDeg))
            throw new InvalidDataException($"Walk default for {targetMap.Value} has an invalid fly pose.");
        var spatial = context.World.Spatial;
        float ground = spatial.GroundY(spawn.Fly.Position.X, spawn.Fly.Position.Z);
        if (float.IsFinite(ground))
        {
            var projected = new Vector3(spawn.Fly.Position.X, ground, spawn.Fly.Position.Z);
            var bounds = context.Descriptor.Bounds;
            var farthest = Vector3.Max(Vector3.Abs(projected - bounds.Min), Vector3.Abs(projected - bounds.Max));
            float maximum = MathF.Max(WalkTuning.SafetySearchRadius, farthest.Length());
            if (float.IsFinite(maximum))
            {
                // Panorama defaults can be outside the walking boundary. This initializes an inactive
                // section, so broaden the real safe-point lookup up to the authored map bounds.
                // Ordinary mode entry and recovery retain their separate 50 m limit.
                for (float radius = WalkTuning.SafetySearchRadius; ; radius = MathF.Min(maximum, radius * 2))
                {
                    if (spatial.NearestSafe(projected, radius) is { } safe && SupportedDryFeet(spatial, safe))
                        return new(safe, spawn.Fly.YawDeg, Math.Clamp(spawn.Fly.PitchDeg, -89f, 89f));
                    if (radius >= maximum) break;
                }
            }
        }
        throw new InvalidOperationException($"Walk default for {targetMap.Value} has no supported dry safety point.");
    }

    public static bool InWater(ISpatialIndex spatial, Vector3 feet) =>
        spatial.MediumAt(feet).Kind == MediumKind.Underwater;

    public static bool SupportedDryFeet(ISpatialIndex spatial, Vector3 feet)
    {
        if (!WalkMath.Finite(feet) || !WalkMath.CanWalk(spatial.WalkClassAt(feet.X, feet.Z)) || InWater(spatial, feet)) return false;
        return HasSupportAtFeet(spatial, feet);
    }

    public static IReadOnlyList<Pose> RestoreSafePoints(ISpatialIndex? spatial, IReadOnlyList<Pose>? points, RestoreContext context)
    {
        var valid = (points ?? []).Where(p => spatial is not null && float.IsFinite(p.YawDeg)
            && float.IsFinite(p.PitchDeg) && SupportedDryFeet(spatial, p.Position))
            .Take(WalkTuning.SafePointCapacity).ToArray();
        if (valid.Length != (points?.Count ?? 0)) context.Repair("walk: invalid or unsupported safety-ring entries removed");
        return valid;
    }

    public static bool NeedsRecovery(ISpatialIndex spatial, Vector3 feet)
    {
        if (!WalkMath.Finite(feet)) return true;
        var classification = spatial.WalkClassAt(feet.X, feet.Z);
        if (classification == WalkClass.Outside) return true;
        var medium = spatial.MediumAt(feet);
        // Query the feet, not the eye. Underwater.Depth is distance below the actual water surface.
        // The riverbed may support the cylinder while its feet are already too deep below that surface.
        if (classification != WalkClass.Wade && medium.Kind == MediumKind.Underwater
            && (!float.IsFinite(medium.Depth) || medium.Depth > WalkTuning.WaterRecoveryDepth)) return true;
        float ground = spatial.GroundY(feet.X, feet.Z);
        if (feet.Y < ground - WalkTuning.BelowSupportLimit) return true;
        if (WalkMath.CanWalk(classification)) return false;
        // A bridge may share a coarse water cell. It is dry only when the actual feet are above water
        // and the spatial support agrees with their height; an airborne point does not qualify.
        if (classification == WalkClass.Water && medium.Kind != MediumKind.Underwater && HasSupportAtFeet(spatial, feet)) return false;
        const int samples = 8;
        for (int i = 0; i < samples; i++)
        {
            float angle = i * (2f * float.Pi / samples);
            float x = feet.X + MathF.Cos(angle) * WalkTuning.ForbiddenDistanceLimit;
            float z = feet.Z + MathF.Sin(angle) * WalkTuning.ForbiddenDistanceLimit;
            if (WalkMath.CanWalk(spatial.WalkClassAt(x, z))) return false;
        }
        return true;
    }

    private static bool HasSupportAtFeet(ISpatialIndex spatial, Vector3 feet)
    {
        var support = spatial.SupportY(feet.X, feet.Z, feet.Y + WalkTuning.SaveSupportTolerance);
        return support is { } y && float.IsFinite(y) && MathF.Abs(y - feet.Y) <= WalkTuning.SaveSupportTolerance;
    }
}
