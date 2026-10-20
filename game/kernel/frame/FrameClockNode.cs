using Campus.Contracts;
using Godot;

namespace Campus.Kernel.Frame;

[KernelNode("frame-clock")]
public partial class FrameClockNode : Node
{
    private static readonly StringName _shaderGlobalFadeClock = ShaderGlobals.FadeClock;

    private double _elapsed;
    public override void _Ready()
    {
        ProcessPriority = FramePriority.FadeClock;
        ProcessMode = ProcessModeEnum.Always;
        FrameClock.FadeSeconds = 0;
    }
    public override void _Process(double delta)
    {
        _elapsed = (_elapsed + delta) % WorldTime.AnimLoopSeconds;
        FrameClock.FadeSeconds = (float)_elapsed;
        RenderingServer.GlobalShaderParameterSet(_shaderGlobalFadeClock, FrameClock.FadeSeconds);
    }
}
