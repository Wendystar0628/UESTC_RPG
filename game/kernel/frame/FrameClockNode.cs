using Campus.Contracts;
using Godot;

namespace Campus.Kernel.Frame;

[KernelNode("frame-clock")]
[KernelService(typeof(IFrameTiming))]
public partial class FrameClockNode : Node, IFrameTiming
{
    private static readonly StringName _shaderGlobalFadeClock = ShaderGlobals.FadeClock;

    private readonly Func<double> _clock;
    private double? _previousProcessMilliseconds;
    private double _elapsed;

    public FrameClockNode() : this(static () => FrameClock.MonotonicMilliseconds) { }
    internal FrameClockNode(Func<double> clock) => _clock = clock;

    public double? PreviousFrameMilliseconds { get; private set; }

    public override void _Ready()
    {
        ProcessPriority = FramePriority.FadeClock;
        ProcessMode = ProcessModeEnum.Always;
        _previousProcessMilliseconds = null;
        PreviousFrameMilliseconds = null;
        FrameClock.FadeSeconds = 0;
    }
    public override void _Process(double delta)
    {
        var now = _clock();
        PreviousFrameMilliseconds = _previousProcessMilliseconds is { } previous ? now - previous : null;
        _previousProcessMilliseconds = now;

        _elapsed = (_elapsed + delta) % WorldTime.AnimLoopSeconds;
        FrameClock.FadeSeconds = (float)_elapsed;
        RenderingServer.GlobalShaderParameterSet(_shaderGlobalFadeClock, FrameClock.FadeSeconds);
    }
}
