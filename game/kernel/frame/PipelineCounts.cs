using Godot;

namespace Campus.Kernel.Frame;

public readonly record struct PipelineCounts(double Surface, double Draw, double Canvas)
{
    public static PipelineCounts Read(bool canvas = true) => new(
        Performance.GetMonitor(Performance.Monitor.PipelineCompilationsSurface),
        Performance.GetMonitor(Performance.Monitor.PipelineCompilationsDraw),
        canvas ? Performance.GetMonitor(Performance.Monitor.PipelineCompilationsCanvas) : 0);
}
