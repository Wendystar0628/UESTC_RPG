using Campus.Contracts;
using Campus.Kernel.Boot;
using Godot;

namespace Campus.Kernel.Renderer;

/// <summary>Owns the engine camera. Pose/input and explicit listener policy remain with WP-06.</summary>
public partial class StageCamera : Node
{
    private ICameraRig? _rig;
    public Camera3D Camera { get; } = new()
    {
        Name = "ViewCamera",
        Current = true,
        TopLevel = true,
        PhysicsInterpolationMode = PhysicsInterpolationModeEnum.Off,
        KeepAspect = Camera3D.KeepAspectEnum.Height,
        Near = BootTuning.CameraNear,
        Far = BootTuning.CameraFar,
        Fov = 60f
    };

    public void Bind(ICameraRig rig) => _rig = rig;
    public override void _Ready()
    {
        ProcessPriority = FramePriority.Camera + 1;
        AddChild(Camera);
    }
    public override void _Process(double delta) => Sync();
    public void Sync()
    {
        if (_rig is null) return;
        var pose = _rig.Pose;
        Camera.GlobalPosition = pose.Position.ToGodot();
        Camera.GlobalRotationDegrees = new(pose.PitchDeg, -pose.YawDeg, 0f);
        Camera.Fov = pose.FovDeg ?? 60f;
    }
}
