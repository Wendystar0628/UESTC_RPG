using Campus.Contracts;
using Campus.Kernel.Boot;
using Godot;
using KernelServices = Campus.Kernel.Registry.Kernel;

namespace Campus.Kernel.Inspect;

/// <summary>Readback barriers consuming the production view and destination presentation state.</summary>
public static class CaptureBarrier
{
    /// <summary>Draw the destination after the loading screen has disappeared.</summary>
    public static async Task WaitForPresentedAsync(Node host, Func<bool> contentReady,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(contentReady);
        var loading = KernelServices.Get<IAppLoadingPresentation>();
        var world = KernelServices.Get<IWorldState>();
        var weather = KernelServices.Get<IWeatherProcess>();
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
            content = contentReady();
            loadingActive = loading.Active;
            return content && !loadingActive && !world.Scrubbing && world.Transition == WorldTransition.None && !weather.Transitioning;
        }
    }
}
