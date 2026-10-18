using System.Text.Json.Serialization.Metadata;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Save;
using N = System.Numerics;
using Services = Campus.Kernel.Registry.Kernel;

namespace Campus.Camera;

// ISaveSection<ViewSaveData>: the "view" save section.
public partial class ObserverCamera
{
    public JsonTypeInfo<ViewSaveData> TypeInfo => ViewJsonContext.Contract.ViewSaveData;

    public ViewSaveData Capture(in CaptureContext context)
    {
        Pose settled = (ActiveTransit is not null ? _transit?.Destination ?? _transitDestination : null) ?? (Mode switch
        {
            ViewMode.Fly => Pose,
            ViewMode.Walk => _walk?.SettledEyePose ?? Pose,
            _ => throw new InvalidOperationException("Unknown view mode.")
        });
        var medium = _map?.World.Spatial.MediumAt(settled.Position);
        NavSpace? space = medium is { } current && current.Kind == MediumKind.Interior && current.Feature is { } feature
            ? new(feature, checked((short)current.Storey)) : null;
        return new(Mode, settled with { FovDeg = null }, space, SpeedStep, _flySpeedStep);
    }

    public void Restore(ViewSaveData data, RestoreContext context)
    {
        ViewMode mode = data.Mode switch
        {
            ViewMode.Fly => ViewMode.Fly,
            ViewMode.Walk => ViewMode.Walk,
            _ => throw new InvalidDataException("Unknown saved view mode.")
        };
        if (_rules is null) throw new InvalidOperationException("Ruleset is unavailable during view restoration.");
        if (!_rules.Current.ViewModes.Contains(mode))
            throw new InvalidDataException("The saved view mode is unavailable in the selected ruleset.");
        if (mode == ViewMode.Walk && _walk is null)
            throw new InvalidOperationException("Restoring walk view requires WP-42 and its walk save section.");
        Pose pose = Legalize(data.Pose);
        if (pose.Position != data.Pose.Position) context.Repair("view.pose");
        ActiveTransit?.Cancel(); ActiveTransit = null; _transitDestination = null;
        ResetHistory();
        _mode = mode;
        // v1 keeps the active-speed projection on disk. Only a fly view restores it here;
        // walk/Pose61 restores the sole walking speed from its own section.
        int flyStep = mode switch
        {
            ViewMode.Fly => data.SpeedStep,
            ViewMode.Walk => data.FlySpeedStep,
            _ => throw new InvalidDataException("Unknown view mode.")
        };
        _flySpeedStep = Math.Clamp(flyStep, 1, Tuning.SpeedSteps);
        if (_flySpeedStep != flyStep) context.Repair(mode == ViewMode.Fly ? "view.speedStep" : "view.flySpeedStep");
        if (mode == ViewMode.Fly) _walk?.Leave();
        SetPose(pose); SetEdgeFog(0);
    }

    public ViewSaveData CreateDefault(NewGameContext context)
    {
        // The ruleset selects the mode; the map catalogue resolves the authored spawn before mount.
        var spawn = Services.Get<IMapService>().ResolveSpawn(context.Map);
        if (context.Mode == ViewMode.Fly)
            return new(context.Mode, spawn.Fly, null, Tuning.DefaultSpeedStep, Tuning.DefaultSpeedStep);
        if (context.Mode != ViewMode.Walk) throw new InvalidDataException("Invalid default observer mode.");
        var stand = spawn.Stand ?? throw new InvalidDataException(
            $"Default spawn of {context.Map.Value} has no walk stand.");
        var eye = new Pose(stand.Feet + N.Vector3.UnitY * WalkRules.StandingEye, stand.YawDeg, 0);
        ValidateFinite(eye);
        return new(context.Mode, eye, null, WalkRules.DefaultSpeedStep, Tuning.DefaultSpeedStep);
    }
}
