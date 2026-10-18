using Campus.Contracts;
using Campus.Kernel.Renderer;
using Godot;

namespace Campus.Kernel.Frame;

[KernelNode("frame-clock", Owner = "WP-01")]
public partial class FrameClockNode : Node
{
    private static readonly StringName _shaderGlobalFadeClock = ShaderGlobals.FadeClock;

    private double _elapsed;
    public override void _Ready()
    {
        ProcessPriority = FramePriority.Map - 1;
        ProcessMode = ProcessModeEnum.Always;
        FrameClock.FadeSeconds = 0;
    }
    public override void _Process(double delta)
    {
        _elapsed = (_elapsed + delta) % 480d;
        FrameClock.FadeSeconds = (float)_elapsed;
        RenderingServer.GlobalShaderParameterSet(_shaderGlobalFadeClock, FrameClock.FadeSeconds);
    }
}
