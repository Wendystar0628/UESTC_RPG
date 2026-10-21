using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Save;
using KernelServices = Campus.Kernel.Registry.Kernel;

namespace Campus.Camera;

/// <summary>The view section's settled-pose, mode-admission and speed projection policy.</summary>
internal static class ObserverViewSave
{
    public static ViewSaveData Capture(ObserverCamera camera, int flySpeedStep, ITransitDriver? transit,
        IWalkController? walk, IMapContext? map)
    {
        Pose settled = (camera.ActiveTransit is not null ? transit?.Destination : null) ?? (camera.Mode switch
        {
            ViewMode.Fly => camera.Pose,
            ViewMode.Walk => walk?.SettledEyePose ?? camera.Pose,
            _ => throw new InvalidOperationException("Unknown view mode.")
        });
        var medium = map?.World.Spatial.MediumAt(settled.Position);
        NavSpace? space = medium is { } current && current.Kind == MediumKind.Interior && current.Feature is { } feature
            ? new(feature, checked((short)current.Storey)) : null;
        return new(camera.Mode, settled with { FovDeg = null }, space, camera.SpeedStep, flySpeedStep);
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
        var pose = spawn.ViewPose(context.Mode);
        if (context.Mode == ViewMode.Fly)
            return new(context.Mode, pose, null, tuning.DefaultSpeedStep, tuning.DefaultSpeedStep);
        ObserverPresentation.ValidateFinite(pose);
        return new(context.Mode, pose, null, WalkRules.DefaultSpeedStep, tuning.DefaultSpeedStep);
    }
}
