using Campus.Camera.Cinematic;
using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Maps;
using Campus.Contracts.Save;
using Campus.Flow;
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

/// <summary>The parts an AppFlowService needs, wired the way the entry-flow tests share them.</summary>
internal sealed class FlowTestRig
{
    public TestRuleset Rules { get; } = TestRuleset.ForFlow();
    public TestWorldState World { get; }
    public TestCameraRig Camera { get; }
    public ImmediateVeil Veil { get; } = new();
    public CinematicTestPresentation Presentation { get; } = new();
    public CinematicRuntimeTestStub Runtime { get; }
    public CinematicPlayback Playback { get; }
    public TestCollisionWorld Collision { get; } = new();
    public List<string> Diagnostics { get; } = [];

    public FlowTestRig(bool completePrefetchImmediately = true)
    {
        World = new(new TestWeatherProcess(), Rules);
        Camera = new(World);
        Runtime = new(() => Presentation.IsCovered) { CompletePrefetchImmediately = completePrefetchImmediately };
        Playback = new(Camera, Veil, Rules, Presentation);
    }

    /// <summary>Builds the flow under test; restore defaults to the rig's world.</summary>
    public AppFlowService CreateFlow(ISaveService saves, IMapService maps, IAppExit exit, IWorldRestore? restore = null) =>
        new(Camera, Veil, saves, maps, World, World, Rules, new TestMotionSettings(), World, Collision, Playback,
            restore ?? World, exit, Runtime, Runtime)
        { DiagnosticSink = Diagnostics.Add };
}
