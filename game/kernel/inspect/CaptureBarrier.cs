using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Maps;
using Campus.Kernel.Boot;
using Campus.Kernel.Frame;
using Campus.Kernel.Renderer;
using Campus.Kernel.Stream;
using Godot;
using Services = Campus.Kernel.Registry.Kernel;

namespace Campus.Kernel.Inspect;

/// <summary>Readback barriers consuming the production view and destination presentation state.</summary>
public static class CaptureBarrier
{
    /// <summary>Draw the released indoor destination after its existing warmup cover has been withdrawn.</summary>
    public static async Task WaitForPresentedAsync(Node host, Pose pose, Func<bool> contentReady,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(contentReady);
        var rig = Services.Get<ICameraRig>();
        var materials = Services.Get<MaterialWarmup>();
        var veil = Services.Get<IVeilState>();
        var loading = Services.Get<IAppLoadingPresentation>();
        var world = Services.Get<IWorldState>();
        var weather = Services.Get<IWeatherProcess>();
        var started = FrameClock.MonotonicMilliseconds;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            // Arrival can complete after the last covered draw, before presentation processes the release.
            // Certify the state submitted to a new draw, then read back only that draw's completed viewport.
            await host.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePreDraw);
            var submittedReady = Ready();
            await host.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            if (Ready() && submittedReady) return;
            if (FrameClock.MonotonicMilliseconds - started > BootTuning.StalledSeconds * 1000)
                throw new TimeoutException($"Screenshot presentation stalled; veil={veil.Opacity},loading={loading.Active}; " +
                    materials.DescribeReadiness());
        }

        bool Ready()
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!host.IsInsideTree()) throw new OperationCanceledException("Capture stage left the tree.");
            if (rig.Pose != pose) throw new OperationCanceledException("Capture pose was replaced.");
            if (materials.Error is { } error) throw new InvalidOperationException("Capture material warmup failed: " + error);
            return contentReady() && materials.IsReady && materials.SamplesReleased && veil.Opacity == 0f &&
                !loading.Active && !world.Scrubbing && world.Transition == WorldTransition.None && !weather.Transitioning;
        }
    }

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
            var streamReady = stream.ViewReady;
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
