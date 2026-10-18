using Campus.Camera.Cinematic;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Kernel.Renderer;

namespace Campus.Tests;

internal sealed class CinematicTestPresentation : IVeilPresentation
{
    public bool IsCovered { get; private set; }
    public void Present(in VeilFrame frame) => IsCovered = frame.Opacity >= 1;
    public void Draw(CinematicPlayback playback, IVeilService veil)
    {
        Present(new(veil.Opacity, VeilContent.Fog, null));
        playback.FramePresented();
    }
}

internal sealed class CinematicRuntimeTestStub(Func<bool> covered) : IMapRuntime, IMapStreamState
{
    bool IMapStreamState.IsUnloaded => false;
    public int Prefetches { get; private set; }
    public int Warms { get; private set; }
    public bool ResidentReady { get; private set; } = true;
    public bool DestinationReady { get; private set; }
    public bool CompletePrefetchImmediately { get; set; } = true;
    public bool WarmReady { get; private set; } = true;
    public string? Error { get; set; }
    public MapStreamDestination? Destination { get; private set; }
    public void Mount(IMapContext context) { ResidentReady = true; }
    public void Prefetch(Pose destination, ViewMode mode, NavSpace? space = null)
    {
        Prefetches++; Destination = new(destination, mode, space); DestinationReady = CompletePrefetchImmediately; WarmReady = false;
    }
    public void BeginWarm()
    {
        if (!covered()) throw new InvalidOperationException("Warm samples require an actually covered test frame.");
        Warms++; WarmReady = DestinationReady;
    }
    public void CompleteDestination() { DestinationReady = true; WarmReady = Warms > 0; }
}

internal static class CinematicTestFrames
{
    public static async Task Complete(Task operation, CinematicPlayback playback,
        CinematicTestPresentation presentation, IVeilService veil)
    {
        for (var frame = 0; frame < 100 && !operation.IsCompleted; frame++)
        {
            presentation.Draw(playback, veil);
            playback.Tick(1d / 60);
            await Task.Yield();
        }
        if (!operation.IsCompleted) throw new InvalidOperationException("Cinematic operation did not finish within its synthetic frame budget.");
        await operation;
    }
}
