using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Maps;
using Campus.Kernel.Boot;
using Campus.Kernel.Frame;
using Campus.Kernel.Stream;
using Godot;
using Services = Campus.Kernel.Registry.Kernel;

namespace Campus.Kernel.Inspect;

/// <summary>One drawn frame with the current pose's coarse outdoor content and applied world state.</summary>
public static class CaptureBarrier
{
    public static async Task WaitForReadyAsync(Node host, Pose pose, Func<bool> contentReady,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(contentReady);
        var world = Services.Get<IWorldState>();
        var weather = Services.Get<IWeatherProcess>();
        var bake = Services.Get<IBakeService>();
        var mapStream = Services.Get<IMapStreamState>();
        var stream = Services.Get<IStreamVisualFrame>();
        var rig = Services.Get<ICameraRig>();
        var started = FrameClock.MonotonicMilliseconds;
        var lastProgress = started;
        var nextDiagnostic = started;
        var progress = bake.Progress;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await host.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            cancellationToken.ThrowIfCancellationRequested();
            if (!host.IsInsideTree()) throw new OperationCanceledException("Capture stage left the tree.");
            if (rig.Pose != pose) throw new OperationCanceledException("Capture pose was replaced.");
            if (mapStream.Error is { } error) throw new InvalidOperationException("Capture stream failed: " + error);
            var content = contentReady();
            var scrubbing = world.Scrubbing;
            var transition = world.Transition;
            var weatherTransitioning = weather.Transitioning;
            var streamReady = stream.CaptureReady;
            var ready = content && streamReady && !scrubbing && transition == WorldTransition.None && !weatherTransitioning;
            if (ready) return;
            var now = FrameClock.MonotonicMilliseconds;
            var nextProgress = bake.Progress;
            if (nextProgress != progress) lastProgress = now;
            progress = nextProgress;
            if (now >= nextDiagnostic)
            {
                GD.Print("[WP-01][phase=capture-readiness] " + Describe());
                nextDiagnostic = now + 10000;
            }
            if (now - lastProgress > BootTuning.StalledSeconds * 1000)
                throw new TimeoutException("Screenshot readiness stalled; " + Describe());
            string Describe() =>
                $"frame={Engine.GetProcessFrames()},waitMs={now - started:F1},noProgressMs={now - lastProgress:F1}," +
                $"content={content},streamReady={streamReady},scrubbing={scrubbing},worldTransition={transition},weatherTransitioning={weatherTransitioning}," +
                $"pose={pose},instance={world.Instance},worldHours={world.WorldHours:R}";
        }
    }
}
