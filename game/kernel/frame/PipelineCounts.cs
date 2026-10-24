using Campus.Contracts;
using Godot;

namespace Campus.Kernel.Frame;

internal static class PipelineCounts
{
    internal static FramePipelineCounts Read() => new(
        (long)Performance.GetMonitor(Performance.Monitor.PipelineCompilationsCanvas),
        (long)Performance.GetMonitor(Performance.Monitor.PipelineCompilationsMesh),
        (long)Performance.GetMonitor(Performance.Monitor.PipelineCompilationsSurface),
        (long)Performance.GetMonitor(Performance.Monitor.PipelineCompilationsDraw),
        (long)Performance.GetMonitor(Performance.Monitor.PipelineCompilationsSpecialization));
}
