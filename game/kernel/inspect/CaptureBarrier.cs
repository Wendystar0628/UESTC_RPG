using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Maps;
using Campus.Kernel.Boot;
using Campus.Kernel.Frame;
using Campus.Kernel.Renderer;
using Godot;
using Services = Campus.Kernel.Registry.Kernel;

namespace Campus.Kernel.Inspect;

public readonly record struct PipelineCounts(double Surface, double Draw, double Canvas)
{
    public static PipelineCounts Read(bool canvas = true) => new(
        Performance.GetMonitor(Performance.Monitor.PipelineCompilationsSurface),
        Performance.GetMonitor(Performance.Monitor.PipelineCompilationsDraw),
        canvas ? Performance.GetMonitor(Performance.Monitor.PipelineCompilationsCanvas) : 0);
}

/// <summary>Requires settled world/weather and a producer-owned content predicate; fixed sleeps cannot satisfy this barrier.</summary>
public static class CaptureBarrier
{
    public static async Task WaitForReadyAsync(Node host, Func<bool> contentReady,
        CancellationToken cancellationToken = default, bool includeCanvas = true)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(contentReady);
        var world = Services.Get<IWorldState>();
        var weather = Services.Get<IWeatherProcess>();
        var bake = Services.Get<IBakeService>();
        var runtime = Services.Get<IMapRuntime>();
        var previous = PipelineCounts.Read(includeCanvas);
        var stable = 0;
        var settled = 0;
        var started = FrameClock.MonotonicMilliseconds;
        var lastProgress = started;
        var nextDiagnostic = started;
        var wasReady = false;
        var progress = (bake.Progress, runtime.ResidentReady, runtime.DestinationReady, runtime.WarmReady);
        while (settled < BootTuning.CaptureSettleFrames)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await host.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            cancellationToken.ThrowIfCancellationRequested();
            var current = PipelineCounts.Read(includeCanvas);
            var content = contentReady();
            var scrubbing = world.Scrubbing;
            var transition = world.Transition;
            var weatherTransitioning = weather.Transitioning;
            var ready = content && !scrubbing && transition == WorldTransition.None && !weatherTransitioning;
            var pipelineChanged = current != previous;
            var now = FrameClock.MonotonicMilliseconds;
            var nextProgress = (bake.Progress, runtime.ResidentReady, runtime.DestinationReady, runtime.WarmReady);
            if (pipelineChanged || ready != wasReady || nextProgress != progress) lastProgress = now;
            progress = nextProgress;
            if (!ready || pipelineChanged) { stable = 0; settled = 0; }
            else if (stable < BootTuning.CaptureStableFrames) stable++;
            else settled++;
            if (now >= nextDiagnostic)
            {
                GD.Print("[WP-01][phase=capture-readiness] " + Describe());
                nextDiagnostic = now + 10000;
            }
            if (now - lastProgress > BootTuning.StalledSeconds * 1000)
                throw new TimeoutException("Screenshot readiness stalled; " + Describe());
            previous = current;
            wasReady = ready;

            string Describe() =>
                $"frame={Engine.GetProcessFrames()},waitMs={now - started:F1},noProgressMs={now - lastProgress:F1}," +
                $"content={content},scrubbing={scrubbing},worldTransition={transition},weatherTransitioning={weatherTransitioning}," +
                $"ready={ready},stable={stable}/{BootTuning.CaptureStableFrames},settled={settled}/{BootTuning.CaptureSettleFrames}," +
                $"pipelineChanged={pipelineChanged},pipelineBefore={previous},pipelineNow={current},canvasIncluded={includeCanvas}," +
                $"instance={world.Instance},worldHours={world.WorldHours:R}; {DescribeProducers()}";
        }
    }

    private static string DescribeProducers()
    {
        try
        {
            var runtime = Services.Get<IMapRuntime>();
            var veil = Services.Get<IVeilState>();
            var presentation = Services.Get<IVeilPresentation>();
            var map = runtime is MapRuntime actual ? actual.DescribeReadiness()
                : $"provider={runtime.GetType().FullName},resident={runtime.ResidentReady},destination={runtime.DestinationReady},warm={runtime.WarmReady},error={runtime.Error ?? "none"}";
            return $"veil[opacity={veil.Opacity},reason={veil.Reason},covered={presentation.IsCovered}]; runtime[{map}]";
        }
        catch (Exception error) { return $"producerDiagnosticsUnavailable={error.GetBaseException().Message}"; }
    }
}
