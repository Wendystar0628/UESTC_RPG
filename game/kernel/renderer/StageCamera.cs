using Campus.Contracts;
using Godot;

namespace Campus.Kernel.Renderer;

/// <summary>Owns the engine camera. Pose/input and explicit listener policy remain with WP-06.</summary>
public partial class StageCamera : Node
{
    public const float NearClip = .05f;
    public const float DefaultFarClip = 12000f;
    private ICameraRig? _rig;
    public Camera3D Camera { get; } = new()
    {
        Name = "ViewCamera",
        Current = true,
        TopLevel = true,
        PhysicsInterpolationMode = PhysicsInterpolationModeEnum.Off,
        KeepAspect = Camera3D.KeepAspectEnum.Height,
        Near = NearClip,
        Far = DefaultFarClip,
        Fov = 60f
    };

    public void Bind(ICameraRig rig) => _rig = rig;
    public override void _Ready()
    {
        ProcessPriority = FramePriority.CameraSync;
        AddChild(Camera);
    }
    public override void _Process(double delta) => Sync();
    public void Sync()
    {
        if (_rig is null) return;
        var pose = _rig.Pose;
        Camera.GlobalPosition = pose.Position.ToGodot();
        Camera.GlobalRotationDegrees = new(pose.PitchDeg, -pose.YawDeg, _rig.RollDeg);
        Camera.Fov = pose.FovDeg ?? 60f;
    }
}
