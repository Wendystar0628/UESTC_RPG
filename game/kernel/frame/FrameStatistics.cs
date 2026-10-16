using Campus.Kernel.Inspect;
using Godot;

namespace Campus.Kernel.Frame;

public readonly record struct FrameSample(long Frame, double CpuRenderMilliseconds, double GpuRenderMilliseconds,
    double DrawCalls, double Primitives, long ManagedBytes, double GcPauseMilliseconds, PipelineCounts Pipelines);

/// <summary>Viewport timings are render measurements, not estimates of total gameplay CPU time.</summary>
public sealed class FrameStatistics(Viewport viewport)
{
    private long _frame;
    private TimeSpan _gcPause = GC.GetTotalPauseDuration();
    public FrameSample Current { get; private set; }
    public PipelineCounts? RealtimeBaseline { get; private set; }

    public void Enable() => RenderingServer.ViewportSetMeasureRenderTime(viewport.GetViewportRid(), true);
    public void MarkRealtime() => RealtimeBaseline = PipelineCounts.Read();
    public void Sample()
    {
        var pause = GC.GetTotalPauseDuration();
        var viewportRid = viewport.GetViewportRid();
        Current = new(++_frame,
            RenderingServer.ViewportGetMeasuredRenderTimeCpu(viewportRid),
            RenderingServer.ViewportGetMeasuredRenderTimeGpu(viewportRid),
            Performance.GetMonitor(Performance.Monitor.RenderTotalDrawCallsInFrame),
            Performance.GetMonitor(Performance.Monitor.RenderTotalPrimitivesInFrame),
            GC.GetTotalMemory(false), (pause - _gcPause).TotalMilliseconds, PipelineCounts.Read());
        _gcPause = pause;
    }
}
