using Campus.Contracts;
using Campus.Kernel.Renderer;
using Godot;

namespace Campus.Camera;

/// <summary>The explicit listener follows the observer eye after camera pose updates.</summary>
internal partial class ObserverListener : AudioListener3D
{
    internal ObserverCamera? Observer { get; init; }

    public ObserverListener()
    {
        Name = "ObserverListener";
        TopLevel = true;
        PhysicsInterpolationMode = PhysicsInterpolationModeEnum.Off;
        ProcessPriority = FramePriority.Listener;
    }

    public override void _Ready()
    {
        Sync(reset: true);
        MakeCurrent();
    }

    public override void _Process(double delta) => Sync();
    public override void _ExitTree() => ClearCurrent();

    internal void Sync(bool reset = false)
    {
        if (Observer is null) return;
        GlobalPosition = Observer.Eye.ToGodot();
        GlobalRotationDegrees = new(Observer.Pose.PitchDeg, -Observer.Pose.YawDeg, 0f);
        if (reset) ResetPhysicsInterpolation();
    }
}
