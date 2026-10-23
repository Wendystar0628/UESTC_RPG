using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Save;
using KernelServices = Campus.Kernel.Registry.Kernel;

namespace Campus.Camera;

/// <summary>The view section's settled-pose, mode-admission and speed projection policy.</summary>
internal static class ObserverViewSave
{
    public static ObserverSaveSnapshot Capture(ICameraRig camera, MapId map, int speedStep, int flySpeedStep,
        IWalkController? walk, IMapContext? context)
    {
        if (context is null || context.Descriptor.Id != map) throw new InvalidOperationException("Cannot capture an observer before its map is mounted.");
        var walking = walk?.CaptureSettled();
        Pose settled = camera.Mode switch
        {
            ViewMode.Fly => camera.Pose,
            ViewMode.Walk when walking is not null => walking.EyePose(),
            ViewMode.Walk => throw new InvalidOperationException("Walking persistence is unavailable."),
            _ => throw new InvalidOperationException("Unknown view mode.")
        };
        return new(map, new(camera.Mode, settled with { FovDeg = null }, SpaceAt(context, settled),
            speedStep, flySpeedStep), walking);
    }

    public static NavSpace? SpaceAt(IMapContext map, Pose pose)
    {
        var medium = map.World.Spatial.MediumAt(pose.Position);
        return medium.Kind == MediumKind.Interior && medium.Feature is { } feature
            ? new(feature, checked((short)medium.Storey)) : null;
    }

    public static ViewMode ValidateRestore(ViewSaveData data, IRuleset? rules, IWalkController? walk)
    {
        ViewMode mode = data.Mode switch
        {
            ViewMode.Fly => ViewMode.Fly,
            ViewMode.Walk => ViewMode.Walk,
            _ => throw new InvalidDataException("Unknown saved view mode.")
        };
        if (rules is null) throw new InvalidOperationException("Ruleset is unavailable during view restoration.");
        if (!rules.Current.ViewModes.Contains(mode))
            throw new InvalidDataException("The saved view mode is unavailable in the selected ruleset.");
        if (mode == ViewMode.Walk && walk is null)
            throw new InvalidOperationException("Restoring walk view requires game/camera/walk/ and its walk save section.");
        ObserverPresentation.ValidateFinite(data.Pose);
        // MapService awaited terrain preparation and projected this destination before restore.
        return mode;
    }

    // v1 keeps the active-speed projection on disk; walk restores its speed from its own section.
    public static int FlySpeed(ViewSaveData data, ViewMode mode) => mode switch
    {
        ViewMode.Fly => data.SpeedStep,
        ViewMode.Walk => data.FlySpeedStep,
        _ => throw new InvalidDataException("Unknown view mode.")
    };

    public static ViewSaveData CreateDefault(NewGameContext context, FlightTuning tuning)
    {
        // The ruleset selects the mode; the map catalogue resolves the authored spawn before mount.
        var spawn = KernelServices.Get<IMapService>().ResolveSpawn(context.Map);
        var pose = context.Boot?.Destination?.Pose ?? spawn.ViewPose(context.Mode);
        if (context.Mode == ViewMode.Fly)
            return new(context.Mode, pose, null, tuning.DefaultSpeedStep, tuning.DefaultSpeedStep);
        ObserverPresentation.ValidateFinite(pose);
        return new(context.Mode, pose, null, WalkRules.DefaultSpeedStep, tuning.DefaultSpeedStep);
    }
}
