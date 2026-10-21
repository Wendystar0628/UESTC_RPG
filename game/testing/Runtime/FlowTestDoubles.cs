using Campus.Contracts.Diagnostics;
using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Maps;
using Campus.Contracts.Save;
using Campus.Flow;

namespace Campus.Tests;

internal sealed class TestAppLoadingPresentation : IAppLoadingPresentation
{
    private readonly List<Lease> _leases = [];
    public bool Active => _leases.Count != 0;
    public bool IsCovered => Active;
    public float Progress { get; private set; }
    public int Hides { get; private set; }
    public IAppLoadingLease Show(EnterRequest? request = null)
    {
        if (!Active) Progress = 0;
        var lease = new Lease(this);
        _leases.Add(lease);
        return lease;
    }
    private sealed class Lease(TestAppLoadingPresentation owner) : IAppLoadingLease
    {
        private bool _released;
        public Task Covered => Task.CompletedTask;
        public void Report(int completed, int total)
        {
            if (!_released) owner.Progress = Math.Max(owner.Progress, total > 0 ? (float)completed / total : 0);
        }
        public Task Hide()
        {
            if (_released) return Task.CompletedTask;
            if (owner._leases.Count == 1 && owner.Progress != 1)
                throw new InvalidOperationException("Loading hid before every item completed.");
            _released = true;
            owner._leases.Remove(this);
            if (!owner.Active) owner.Hides++;
            return Task.CompletedTask;
        }
        public ValueTask DisposeAsync() => new(Hide());
    }
}
internal sealed class TestAppTitlePresentation : IAppTitlePresentation
{
    public bool Visible { get; private set; }
    public void SetVisible(bool visible) => Visible = visible;
}
internal sealed class FlowRuntimeTestStub(Func<bool> covered) : IMapRuntime, IMapStreamState
{
    public IReadOnlyList<MapStreamDestination> PinnedDestinations { get; private set; } = [];
    public void SetPinnedDestinations(IReadOnlyList<MapStreamDestination> destinations) => PinnedDestinations = destinations.ToArray();
    public bool IsUnloaded => false;
    public Task DrainAsync() => Task.CompletedTask;
    public bool ResidentReady => true;
    public bool DestinationReady => Arrival?.Done.IsCompletedSuccessfully == true;
    public MapStreamDestination? Destination => Arrival?.Destination;
    public bool HasPendingDestination => Arrival is { Done.IsCompleted: false };
    public void Prefetch(Pose destination, ViewMode mode, NavSpace? space = null, System.Numerics.Vector3? collisionFeet = null) =>
        throw new InvalidOperationException("Flow preparation must retain real arrival ownership.");
    public int Arrivals { get; private set; }
    public bool CompleteArrivalImmediately { get; set; } = true;
    public event Action<IMapArrival?>? ArrivalChanged;
    private Exception? _failure;
    public Exception? Failure
    {
        get => _failure;
        set { _failure = value; _arrival?.TryComplete(); }
    }
    public AppStartupException ReportFailure([FailureHandler("SYS-MAPS")] Exception error)
    {
        var failure = error as AppStartupException ?? new(AppStartupFailureCodes.MapPreparation, "Map preparation failed.", error);
        Failure = failure;
        return failure;
    }
    private ArrivalStub? _arrival;
    public IMapArrival? Arrival => _arrival;
    public IMapArrival Arrive(Pose destination, ViewMode mode, NavSpace? space = null, System.Numerics.Vector3? collisionFeet = null)
    {
        _arrival?.Cancel(); Arrivals++;
        _arrival = new(this, new(destination, mode, space, collisionFeet), CompleteArrivalImmediately);
        ArrivalChanged?.Invoke(_arrival); _arrival.TryComplete(); return _arrival;
    }
    private bool IsCovered() => covered();
    public void CompleteArrival() { if (_arrival is { } arrival) { arrival.Ready = true; arrival.TryComplete(); } }
    private sealed class ArrivalStub(FlowRuntimeTestStub owner, MapStreamDestination destination, bool ready) : IMapArrival
    {
        private readonly TaskCompletionSource _done = new();
        public bool Ready { get; set; } = ready;
        public MapStreamDestination Destination => destination;
        public Task Done => _done.Task;
        public int Completed => _done.Task.IsCompletedSuccessfully ? Total : 0;
        public int Total => 1;
        public event Action? ProgressChanged;
        public void Cancel() => _done.TrySetCanceled();
        public void TryComplete()
        {
            if (owner.Failure is { } error) _done.TrySetException(error);
            else if (Ready && owner.IsCovered()) { _done.TrySetResult(); ProgressChanged?.Invoke(); }
        }
    }
}
/// <summary>The shared service fixture for application flow behavior.</summary>
internal sealed class FlowTestRig
{
    public TestRuleset Rules { get; } = TestRuleset.ForFlow();
    public TestWorldState World { get; }
    public TestCameraRig Camera { get; }
    public TestAppLoadingPresentation Loading { get; } = new();
    public TestAppTitlePresentation Title { get; } = new();
    public FlowRuntimeTestStub Runtime { get; }
    public List<string> Diagnostics { get; } = [];
    public FlowTestRig(bool completeArrivalImmediately = true)
    {
        World = new(new TestWeatherProcess(), Rules); Camera = new(World);
        Runtime = new(() => Loading.Active) { CompleteArrivalImmediately = completeArrivalImmediately };
    }
    public AppFlowService CreateFlow(ISaveService saves, IMapService maps, IAppExit exit, IWorldRestore? restore = null) =>
        new(Camera, Loading, Title, saves, maps, World, World, World, restore ?? World, exit, Runtime, Runtime,
            new FlowDiagnostics(Diagnostics));

    private sealed class FlowDiagnostics(List<string> messages) : IDiagnosticLog
    {
        public void Error(string context, Exception error) => Error(context, error.ToString());
        public void Error(string context, string message) => messages.Add(context + " " + message);
        public void Warning(string context, Exception error) => Warning(context, error.ToString());
        public void Warning(string context, string message) => messages.Add(context + " " + message);
    }
}
internal sealed class FlowTerrainPreparation : ITerrainPreparation, ICameraPoseValidator
{
    public bool IsLegal(Pose pose, bool outsideBuffer) => true;
    public Task<Pose> LegalizePrepared(Pose pose, CancellationToken cancellation = default) => Task.FromResult(pose);
    public bool IsPrepared(IWorldModel world, System.Numerics.Vector2 point) => true;
    public Task PrepareRegion(IMapContext context, MapRect region, CancellationToken cancellation = default) => Task.CompletedTask;
    public Task PreparePoints(IMapContext context, IReadOnlyList<System.Numerics.Vector2> points,
        CancellationToken cancellation = default) => Task.CompletedTask;
}
