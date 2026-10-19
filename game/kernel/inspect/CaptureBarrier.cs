using Campus.Contracts;
using Campus.Kernel.Boot;
using Campus.Kernel.Frame;
using Godot;
using Services = Campus.Kernel.Registry.Kernel;

namespace Campus.Kernel.Inspect;

/// <summary>Readback barriers consuming the production view and destination presentation state.</summary>
public static class CaptureBarrier
{
    /// <summary>Draw the destination after the loading screen has disappeared.</summary>
    public static async Task WaitForPresentedAsync(Node host, Pose pose, Func<bool> contentReady,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(contentReady);
        var rig = Services.Get<ICameraRig>();
        var loading = Services.Get<IAppLoadingPresentation>();
        var world = Services.Get<IWorldState>();
        var weather = Services.Get<IWeatherProcess>();
        var content = false;
        var loadingActive = false;
        try
        {
            // Arrival can finish on the last covered draw. Query outside rendering, then require a new draw.
            await FrameAwaiter.ReadyDrawAsync(host, Ready, TimeSpan.FromSeconds(BootTuning.StalledSeconds), cancellationToken);
        }
        catch (TimeoutException error)
        {
            throw new TimeoutException($"Screenshot presentation stalled; content={content},loading={loadingActive}", error);
        }

        bool Ready()
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!host.IsInsideTree()) throw new OperationCanceledException("Capture stage left the tree.");
            if (rig.Pose != pose) throw new OperationCanceledException("Capture pose was replaced.");
            content = contentReady();
            loadingActive = loading.Active;
            return content && !loadingActive && !world.Scrubbing && world.Transition == WorldTransition.None && !weather.Transitioning;
        }
    }
}
