using System.Text.Json.Serialization.Metadata;
using Campus.Camera.Walk;
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
        return new(Mode, settled with { FovDeg = null }, space, _speedStep, _flySpeedStep);
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
        _speedStep = Math.Clamp(data.SpeedStep,
            mode switch { ViewMode.Fly => 1, ViewMode.Walk => 0, _ => throw new InvalidDataException("Unknown view mode.") },
            mode switch { ViewMode.Fly => Tuning.SpeedSteps, ViewMode.Walk => 4, _ => throw new InvalidDataException("Unknown view mode.") });
        _flySpeedStep = Math.Clamp(data.FlySpeedStep, 1, Tuning.SpeedSteps);
        if (_speedStep != data.SpeedStep) context.Repair("view.speedStep");
        if (_flySpeedStep != data.FlySpeedStep) context.Repair("view.flySpeedStep");
        if (mode == ViewMode.Walk) _walkSpeedStep = _speedStep;
        else _walk?.Leave();
        SetPose(pose); SetEdgeFog(0);
    }

    public ViewSaveData CreateDefault(NewGameContext context)
    {
        // D-242: a new game hands control back walking on the default spawn's stand. The catalogue resolves
        // authored spawns before waypoints, including a target map that has not mounted yet; Restore
        // legalizes the pose against the mounted map, as for every saved walk pose.
        var spawn = Services.Get<IMapService>().ResolveSpawn(context.Map);
        var stand = spawn.Stand ?? throw new InvalidDataException(
            $"Default spawn of {context.Map.Value} has no walk stand.");
        var eye = new Pose(stand.Feet + N.Vector3.UnitY * WalkTuning.StandingEye, stand.YawDeg, 0);
        ValidateFinite(eye);
        return new(ViewMode.Walk, eye, null, WalkTuning.DefaultSpeedStep, Tuning.DefaultSpeedStep);
    }
}
