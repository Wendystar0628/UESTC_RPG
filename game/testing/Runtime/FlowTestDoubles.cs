using Campus.Contracts.Diagnostics;
using System.Collections.Concurrent;
using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Maps;
using Campus.Contracts.Save;
using Campus.Flow;
using Campus.Contracts.Settings;
using Campus.Gen.Tests.Support;
using Campus.Gen.World;

namespace Campus.Tests;

/// <summary>Records the actual diagnostic calls and keeps exception objects for fault-injection assertions.</summary>
internal sealed class TestDiagnosticLog : IDiagnosticLog
{
    private readonly ConcurrentQueue<(bool IsError, string Context, Exception? Error, string? Message)> _entries = new();
    public IReadOnlyList<Exception?> Errors => _entries.Where(entry => entry.IsError).Select(entry => entry.Error).ToArray();
    public IReadOnlyList<(bool IsError, string Context, Exception? Error, string? Message)> Entries => _entries.ToArray();
    public void Error(string context, Exception error) => _entries.Enqueue((true, context, error, null));
    public void Error(string context, string message) => _entries.Enqueue((true, context, null, message));
    public void Warning(string context, Exception error) => _entries.Enqueue((false, context, error, null));
    public void Warning(string context, string message) => _entries.Enqueue((false, context, null, message));
}

internal sealed class TestAppLoadingPresentation : IAppLoadingPresentation
{
    private readonly List<Lease> _leases = [];
    public bool Active => _leases.Count != 0;
    public bool IsCovered => Active;
    public float Progress { get; private set; }
    public int Hides { get; private set; }
    // Superseded leases release immediately; the final screen owns the visible fade.
    public Task? HideGate { get; set; }
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
        public async Task Hide()
        {
            if (_released) return;
            if (owner._leases.Count == 1)
            {
                if (owner.Progress != 1)
                    throw new InvalidOperationException("Loading hid before every item completed.");
                if (owner.HideGate is { } gate) await gate;
            }
            _released = true;
            owner._leases.Remove(this);
            if (!owner.Active) owner.Hides++;
        }
        public ValueTask DisposeAsync() => new(Hide());
    }
}
internal sealed class TestAppFrontispiecePresentation : IAppFrontispiecePresentation
{
    public bool Visible { get; private set; }
    public Task Show() { Visible = true; return Task.CompletedTask; }
    public Task Hide(float seconds) { Visible = false; return Task.CompletedTask; }
}
internal sealed class TestDestinationPins
{
    private readonly List<Lease> _leases = [];
    public IReadOnlyList<MapStreamDestination> Destinations => _leases.Select(lease => lease.Destination).Distinct().ToArray();
    public IDisposable Acquire(MapStreamDestination destination)
    {
        var lease = new Lease(this, destination); _leases.Add(lease); return lease;
    }
    private sealed class Lease(TestDestinationPins owner, MapStreamDestination destination) : IDisposable
    {
        public MapStreamDestination Destination => destination;
        public void Dispose() => owner._leases.Remove(this);
    }
}

internal sealed class FlowRuntimeTestStub(Func<bool> covered) : IMapRuntime, IMapStreamState
{
    private readonly TestDestinationPins _pins = new();
    public IReadOnlyList<MapStreamDestination> PinnedDestinations => _pins.Destinations;
    public IDisposable PinDestination(MapStreamDestination destination) => _pins.Acquire(destination);
    public bool PinsReady { get; set; }
    public bool PinnedDestinationReady(MapStreamDestination destination) => PinsReady && PinnedDestinations.Contains(destination);
    public void ClearDestination() { _arrival?.Cancel(); _arrival = null; ArrivalChanged?.Invoke(null); }
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
        var failure = error as AppStartupException ?? new("Map preparation failed.", error);
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
    public TestAppFrontispiecePresentation Title { get; } = new();
    public FlowRuntimeTestStub Runtime { get; }
    public FlowTransitDriver Transit { get; } = new();
    public List<string> Diagnostics { get; } = [];
    public FlowTestRig(bool completeArrivalImmediately = true)
    {
        World = new(new TestWeatherProcess(), Rules); Camera = new(World);
        Runtime = new(() => Loading.Active) { CompleteArrivalImmediately = completeArrivalImmediately };
    }
    public AppFlowService CreateFlow(ISaveService saves, IMapService maps, IAppExit exit, IWorldRestore? restore = null) =>
        new(Camera, Loading, Title, saves, maps, World, World, restore ?? World, exit, Runtime, Runtime,
            Transit, new FlowDiagnostics(Diagnostics));

    internal static TestMapService Maps()
    {
        var source = RealMapFixture.Entry(); var descriptor = source.Descriptor;
        return new()
        {
            Default = descriptor.Id, Current = descriptor.Id, Maps = [descriptor],
            Context = new TestMapContext(descriptor, new FakeWorld { Boundary = new([]) }),
            SpawnResolver = (_, spawn) => source.ResolveSpawn(spawn ?? descriptor.DefaultSpawn)
        };
    }

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

/// <summary>Flow owns the decision to drain; camera tests execute the real transit lifecycle.</summary>
internal sealed class FlowTransitDriver : ITransitDriver
{
    public bool IsReady => true;
    public Campus.Contracts.Save.ObserverSaveSnapshot? SaveSnapshot => null;
    public Task Draining { get; set; } = Task.CompletedTask;
    public Task CancelAsync() => Draining;
    public ITransitHandle Start(Pose from, Pose to, TransitOptions? options = null) =>
        throw new NotSupportedException("Flow does not start camera transit.");
    public ITransitHandle SwitchMode(Func<CancellationToken, Task<ModeSwitchResult>> apply) =>
        throw new NotSupportedException("Flow does not commit camera modes.");
}

internal sealed class FlowSaveTestStub(FlowTestRig rig) : TestSaveService
{
    private SlotId? _active;
    public int NewGames { get; private set; }
    public int Loads { get; private set; }
    public int Saves { get; private set; }
    public int Flushes { get; private set; }
    public SaveStatus InitialSaveStatus { get; set; } = SaveStatus.Saved;
    public SaveStatus FlushStatus { get; set; } = SaveStatus.Saved;
    public string? FlushError { get; set; }
    private SaveResult? _lastResult;
    public override SaveState State => new(Active, _lastResult?.Status, _lastResult?.Error, null, ReadOnlyReason.None);
    public TaskCompletionSource ArrivalStarted { get; } = new();
    private static readonly Pose Landing = new(new(120, 1.7f, -70), 20, 0, UiSettingDefinitions.WalkFov.Default);
    private SaveMeta Meta => new(Campus.Kernel.Save.Format.SaveContainer.CurrentFormat, new("s1"), SlotKind.Auto, 1, 1, 0, FlowTestRig.Maps().Default,
        17.5, 17.5, Season.Autumn, WeatherKind.Clear, ViewMode.Walk, Landing, null, null, null, 0, SaveReason.Test);
    public override IReadOnlyList<SlotInfo> Slots => [new(new("s1"), SlotKind.Auto, SlotHealth.Ok, Meta, 1)];
    public override SlotId? Continue => new("s1");
    public override SlotId? Active => _active;
    public override Task<SaveResult> SaveNow(SaveReason reason)
    {
        if (rig.Runtime.Arrival?.Done.IsCompletedSuccessfully != true) throw new InvalidOperationException("Save preceded arrival.");
        Saves++; return Task.FromResult(new SaveResult(InitialSaveStatus, Active, 1));
    }
    public override Task<LoadResult> NewGame(SlotId? slot, uint? seed = null, WorldPreset? start = null, NewGameBoot? boot = null)
    { NewGames++; return Restore(slot); }
    public override Task<LoadResult> Load(SlotId slot) { Loads++; return Restore(slot); }
    private async Task<LoadResult> Restore(SlotId? slot)
    {
        _active = slot; rig.Camera.Mode = ViewMode.Walk; rig.Camera.SetPose(Landing);
        var arrival = rig.Runtime.Arrive(Landing, ViewMode.Walk);
        ArrivalStarted.TrySetResult(); await arrival.Done;
        return new(LoadStatus.Loaded, slot, 1, []);
    }
    public override Task<SaveResult> FlushForQuit(SaveReason reason, TimeSpan timeout)
    { Flushes++; _lastResult = new(FlushStatus, Active, 1, FlushError); return Task.FromResult(_lastResult); }
}
