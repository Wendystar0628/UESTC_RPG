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

internal sealed class CinematicRuntimeTestStub(Func<bool> covered) : IMapRuntime
{
    public int Arrivals { get; private set; }
    public bool CompleteArrivalImmediately { get; set; } = true;
    private string? _error;
    public string? Error
    {
        get => _error;
        set { _error = value; _arrival?.TryComplete(); }
    }
    private ArrivalStub? _arrival;
    public IMapArrival? Arrival => _arrival;
    public IMapArrival Arrive(Pose destination, ViewMode mode, NavSpace? space = null)
    {
        _arrival?.Cancel();
        Arrivals++;
        _arrival = new(this, new(destination, mode, space), CompleteArrivalImmediately);
        return _arrival;
    }
    public void CompleteArrival() { if (_arrival is { } arrival) { arrival.Ready = true; arrival.TryComplete(); } }

    private sealed class ArrivalStub(CinematicRuntimeTestStub owner, MapStreamDestination destination, bool ready) : IMapArrival
    {
        private readonly TaskCompletionSource _done = new();
        public bool Ready { get; set; } = ready;
        public MapStreamDestination Destination => destination;
        public Task Done { get { TryComplete(); return _done.Task; } }
        public void Cancel() => _done.TrySetCanceled();
        public void TryComplete()
        {
            if (owner.Error is { } error) _done.TrySetException(new InvalidOperationException(error));
            else if (Ready && owner.IsCovered()) _done.TrySetResult();
        }
    }
    private bool IsCovered() => covered();
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
    public List<string> Diagnostics { get; } = [];

    public FlowTestRig(bool completeArrivalImmediately = true)
    {
        World = new(new TestWeatherProcess(), Rules);
        Camera = new(World);
        Runtime = new(() => Presentation.IsCovered) { CompleteArrivalImmediately = completeArrivalImmediately };
        Playback = new(Camera, Veil, Rules, Presentation, new CinematicTerrainPreparation());
    }

    /// <summary>Builds the flow under test; restore defaults to the rig's world.</summary>
    public AppFlowService CreateFlow(ISaveService saves, IMapService maps, IAppExit exit, IWorldRestore? restore = null) =>
        new(Camera, Veil, saves, maps, World, World, Rules, new TestMotionSettings(), World, Playback,
            restore ?? World, exit, Runtime)
        { DiagnosticSink = Diagnostics.Add };
}

internal sealed class CinematicTerrainPreparation : ITerrainPreparation, ICameraPoseValidator
{
    public bool IsLegal(Pose pose, bool outsideBuffer) => true;
    public Task<Pose> LegalizePrepared(Pose pose, CancellationToken cancellation = default) => Task.FromResult(pose);
    public bool IsPrepared(IWorldModel world, System.Numerics.Vector2 point) => true;
    public Task PrepareRegion(IMapContext context, MapRect region, CancellationToken cancellation = default) => Task.CompletedTask;
    public Task PreparePoints(IMapContext context, string product, IReadOnlyList<System.Numerics.Vector2> points,
        CancellationToken cancellation = default) => Task.CompletedTask;
}
