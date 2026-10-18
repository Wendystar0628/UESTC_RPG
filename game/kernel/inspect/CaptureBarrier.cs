using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Maps;
using Campus.Kernel.Boot;
using Campus.Kernel.Frame;
using Campus.Kernel.Renderer;
using Godot;
using Services = Campus.Kernel.Registry.Kernel;

namespace Campus.Kernel.Inspect;

/// <summary>Requires the current arrival, settled world/weather and producer content; fixed sleeps cannot satisfy this barrier.</summary>
public static class CaptureBarrier
{
    public static async Task WaitForReadyAsync(Node host, ViewContentReadiness producers, IMapArrival arrival, Func<bool> contentReady,
        CancellationToken cancellationToken = default, bool includeCanvas = true)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(producers);
        ArgumentNullException.ThrowIfNull(arrival);
        ArgumentNullException.ThrowIfNull(contentReady);
        var world = Services.Get<IWorldState>();
        var weather = Services.Get<IWeatherProcess>();
        var bake = Services.Get<IBakeService>();
        var runtime = Services.Get<IMapRuntime>();
        var rig = Services.Get<ICameraRig>();
        using var cancellation = cancellationToken.Register(arrival.Cancel);
        var previous = PipelineCounts.Read(includeCanvas);
        var stable = 0;
        var settled = 0;
        var started = FrameClock.MonotonicMilliseconds;
        var lastProgress = started;
        var nextDiagnostic = started;
        var wasReady = false;
        var progress = (bake.Progress, arrival.Done.Status);
        while (settled < BootTuning.CaptureSettleFrames)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await host.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            cancellationToken.ThrowIfCancellationRequested();
            if (!host.IsInsideTree()) throw new OperationCanceledException("Capture stage left the tree.");
            if (arrival.Done.IsCompleted) await arrival.Done;
            if (!ReferenceEquals(runtime.Arrival, arrival))
                throw new OperationCanceledException("Capture arrival was replaced.");
            var current = PipelineCounts.Read(includeCanvas);
            var content = contentReady();
            content &= arrival.Done.IsCompletedSuccessfully && producers.IsReady(rig.Pose);
            var scrubbing = world.Scrubbing;
            var transition = world.Transition;
            var weatherTransitioning = weather.Transitioning;
            var ready = content && !scrubbing && transition == WorldTransition.None && !weatherTransitioning;
            var pipelineChanged = current != previous;
            var now = FrameClock.MonotonicMilliseconds;
            var nextProgress = (bake.Progress, arrival.Done.Status);
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
                $"instance={world.Instance},worldHours={world.WorldHours:R}," +
                $"contentProducers=[{producers.Describe(rig.Pose)}]; {DescribeProducers()}";
        }
    }

    private static string DescribeProducers()
    {
        try
        {
            var lifetime = Services.Get<IMapLifetime>();
            var veil = Services.Get<IVeilState>();
            var presentation = Services.Get<IVeilPresentation>();
            var map = lifetime.DescribeReadiness();
            return $"veil[opacity={veil.Opacity},reason={veil.Reason},covered={presentation.IsCovered}]; runtime[{map}]";
        }
        catch (Exception error) { return $"producerDiagnosticsUnavailable={error.GetBaseException().Message}"; }
    }
}
