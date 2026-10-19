using Campus.Contracts;

namespace Campus.Camera;

// ICameraPoseValidator and the finite-pose guard shared by SetPose and Transit.
public partial class ObserverCamera
{
    public async Task<Pose> LegalizePrepared(Pose pose, CancellationToken cancellation = default)
    {
        ValidateFinite(pose);
        return _bounds is null ? pose : pose with
        {
            Position = await _bounds.ProjectPrepared(pose.Position,
                _terrain ??= Campus.Kernel.Registry.Kernel.Get<ITerrainPreparation>(), cancellation)
        };
    }

    public bool IsLegal(Pose pose, bool outsideBuffer = false)
    {
        try { ValidateFinite(pose); } catch (ArgumentOutOfRangeException) { return false; }
        return _bounds?.IsLegal(pose.Position, outsideBuffer) ?? false;
    }

    private static void ValidateFinite(Pose pose)
    {
        if (!float.IsFinite(pose.Position.X) || !float.IsFinite(pose.Position.Y) || !float.IsFinite(pose.Position.Z)
            || !float.IsFinite(pose.YawDeg) || !float.IsFinite(pose.PitchDeg)
            || pose.FovDeg is { } fov && (!float.IsFinite(fov) || fov <= 0 || fov >= 180))
            throw new ArgumentOutOfRangeException(nameof(pose));
    }
}
