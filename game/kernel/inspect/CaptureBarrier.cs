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
        var content = false;
        var materialReady = false;
        var samplesReleased = false;
        var veilOpacity = float.NaN;
        var loadingActive = false;
        try
        {
            // Arrival can finish on the last covered draw. Query outside rendering, then require a new draw.
            await FrameAwaiter.ReadyDrawAsync(host, Ready, TimeSpan.FromSeconds(BootTuning.StalledSeconds), cancellationToken);
        }
        catch (TimeoutException error)
        {
            throw new TimeoutException($"Screenshot presentation stalled; content={content},materialsReady={materialReady}," +
                $"samplesReleased={samplesReleased},veil={veilOpacity},loading={loadingActive}", error);
        }

        bool Ready()
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!host.IsInsideTree()) throw new OperationCanceledException("Capture stage left the tree.");
            if (rig.Pose != pose) throw new OperationCanceledException("Capture pose was replaced.");
            if (materials.Error is { } error) throw new InvalidOperationException("Capture material warmup failed: " + error);
            content = contentReady();
            materialReady = materials.IsReady;
            samplesReleased = materials.SamplesReleased;
            veilOpacity = veil.Opacity;
            loadingActive = loading.Active;
            return content && materialReady && samplesReleased && veilOpacity == 0f &&
                !loadingActive && !world.Scrubbing && world.Transition == WorldTransition.None && !weather.Transitioning;
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
        var timeout = TimeSpan.FromSeconds(BootTuning.StalledSeconds);
        using var deadline = new CancellationTokenSource(timeout);
        using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, deadline.Token);
        var content = false;
        var scrubbing = false;
        var transition = WorldTransition.None;
        var weatherTransitioning = false;
        var streamReady = false;
        var now = started;
        ulong frame = 0;
        long instance = 0;
        double worldHours = 0;
        try
        {
            while (true)
            {
                await FrameAwaiter.ProcessAsync(host, lifetime.Token);
                lifetime.Token.ThrowIfCancellationRequested();
                if (!host.IsInsideTree()) throw new OperationCanceledException("Capture stage left the tree.");
                if (rig.Pose != pose) throw new OperationCanceledException("Capture pose was replaced.");
                if (mapStream.Error is { } error) throw new InvalidOperationException("Capture stream failed: " + error);
                content = contentReady();
                scrubbing = world.Scrubbing;
                transition = world.Transition;
                weatherTransitioning = weather.Transitioning;
                streamReady = stream.ViewReady;
                var ready = content && streamReady && !scrubbing && transition == WorldTransition.None && !weatherTransitioning;
                if (ready)
                {
                    lifetime.Token.ThrowIfCancellationRequested();
                    deadline.CancelAfter(Timeout.InfiniteTimeSpan);
                    break;
                }
                now = FrameClock.MonotonicMilliseconds;
                frame = Engine.GetProcessFrames();
                instance = world.Instance;
                worldHours = world.WorldHours;
                var nextProgress = bake.Progress;
                if (nextProgress != progress) { lastProgress = now; deadline.CancelAfter(timeout); }
                progress = nextProgress;
                if (now >= nextDiagnostic)
                {
                    GD.Print("[WP-01][phase=capture-readiness] " + Describe());
                    nextDiagnostic = now + 10000;
                }
            }
        }
        catch (OperationCanceledException error) when (deadline.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException("Screenshot readiness stalled; " + Describe(), error);
        }
        // New view demand uses the production warmup cover; read only its released, presented frame.
        await WaitForPresentedAsync(host, pose, () => contentReady() && stream.ViewReady, cancellationToken);

        string Describe() =>
            $"frame={frame},waitMs={now - started:F1},noProgressMs={now - lastProgress:F1}," +
            $"content={content},streamReady={streamReady},scrubbing={scrubbing},worldTransition={transition},weatherTransitioning={weatherTransitioning}," +
            $"pose={pose},instance={instance},worldHours={worldHours:R}";
    }
}
