using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Maps;
using Campus.Contracts.Bake;
using Campus.Kernel.Maps;
using Campus.Kernel.Registry;
using Campus.Kernel.Renderer;
using Godot;

namespace Campus.Kernel.Boot;

public sealed record MapRuntimeDependencies(ServiceRegistry Owner, IMapStreamState Stream, IBakeService Bake,
    ICollisionWorld Collision, IRenderReadiness Renderer,
    ICameraRig Camera, ITerrainPreparation Terrain, Camera3D ViewCamera);

/// <summary>Scene-side map lifetime. The map service owns transition ordering; this host owns engine nodes.</summary>
[KernelService(typeof(IMapRuntime))]
[KernelService(typeof(IMapLifetime))]
[KernelNode("map-runtime")]
public partial class MapRuntime : Node, IMapRuntime, IMapLifetime
{
    private IMapStreamState? _stream;
    private IBakeService? _bake;
    private ICollisionWorld? _collision;
    private IRenderReadiness? _renderer;
    private ServiceRegistry? _owner;
    private Camera3D? _viewCamera;
    private readonly CancellationTokenSource _exit = new();
    private MapId? _loaded;
    private Task? _preparing;
    private bool _mountPrepared;
    private TaskCompletionSource? _mountCompletion;
    private ArrivalRun? _arrival;
    private ICameraRig? _camera;
    private ITerrainPreparation? _terrain;
    private IMapContext? _context;
    private Task? _shutdown;
    private bool _stopping;
    private long _mapGeneration;
    private AppStartupException? _failure;
    private readonly List<Exception> _failures = [];
    private readonly List<Task> _arrivalPreparations = [];
    private ViewContentReadiness? _contentReadiness;
    private object[] _receivers = [];
    private bool ResidentReady => _mountPrepared && _stream?.ResidentReady == true && _renderer?.Pending == false && Failure is null;
    private bool DestinationReady => _mountPrepared && _stream?.DestinationReady == true &&
        Failure is null && _arrival is { Prepared: true } arrival && (arrival.Destination.Mode switch
        {
            ViewMode.Fly => true,
            ViewMode.Walk => _collision!.Ready(arrival.Destination.CollisionFeet ?? arrival.Destination.Pose.Position, WalkRules.ArrivalCollisionRadius),
            _ => throw new InvalidOperationException("Unknown arrival view mode.")
        });
    public IMapArrival? Arrival => _arrival;
    public event Action<IMapArrival?>? ArrivalChanged;
    public Exception? Failure => _failure;

    private void PollFailures()
    {
        if (_stream?.Failure is { } stream) ReportFailure(stream);
        if (_bake?.Failure is { } bake) ReportFailure(bake);
        if (_renderer?.Failure is { } rendering) ReportFailure(rendering);
        if (_collision?.Failure is { } collision) ReportFailure(collision);
        if (_contentReadiness is { } content)
            foreach (var producer in content.Producers)
                if (producer.Failure is { } failure) ReportFailure(failure);
    }

    public AppStartupException ReportFailure([FailureHandler("SYS-MAPS")] Exception error)
    {
        // Classify only at publication; keep each original producer and cleanup cause intact.
        if (_failure is { } published && ReferenceEquals(error, published)) return published;
        var causes = new Queue<Exception>();
        causes.Enqueue(error);
        var added = false;
        while (causes.TryDequeue(out var cause))
        {
            if (cause is AggregateException { InnerExceptions.Count: > 0 } aggregate)
            {
                foreach (var inner in aggregate.InnerExceptions) causes.Enqueue(inner);
                continue;
            }
            // An empty aggregate is itself a failure, even when nested beside another cause.
            if (_failures.Contains(cause)) continue;
            _failures.Add(cause);
            added = true;
        }
        if (!added && _failure is { } existing) return existing;
        if (_failures.Count == 1 && _failures[0] is AppStartupException startup)
            return _failure = startup;
        var code = _failures.OfType<AppStartupException>().FirstOrDefault()?.Code ?? AppStartupFailureCodes.MapPreparation;
        var diagnostic = _failures.Count == 1 ? _failures[0] : new AggregateException("Map runtime failed.", _failures);
        return _failure = new AppStartupException(code, "Map preparation failed.", diagnostic);
    }

    public void Bind(MapRuntimeDependencies dependencies)
    {
        ArgumentNullException.ThrowIfNull(dependencies);
        if (IsInsideTree() || _owner is not null) throw new InvalidOperationException("Map runtime binds once before entering the tree.");
        _owner = dependencies.Owner;
        _stream = dependencies.Stream; _bake = dependencies.Bake; _collision = dependencies.Collision;
        _renderer = dependencies.Renderer; _camera = dependencies.Camera;
        _terrain = dependencies.Terrain; _viewCamera = dependencies.ViewCamera;
    }

    public override void _Ready()
    {
        if (_owner is null) throw new InvalidOperationException("Map runtime dependencies must be bound before entering the tree.");
        ProcessPriority = FramePriority.MapRuntime;
    }

    /// <summary>Stage exit only: stop producers, drain preparation and release every map-owned resource.</summary>
    public Task ShutdownAsync(MapId map)
    {
        if (_shutdown is null || _shutdown.IsCompleted && _loaded.HasValue) _shutdown = ShutdownMap(map);
        return _shutdown;
    }

    private async Task ShutdownMap(MapId from)
    {
        try { StopNewWork(); } catch (Exception error) { ReportFailure(error); }
        _mountPrepared = false;
        _contentReadiness = null;
        _mapGeneration++;
        await ReleaseAfterPreparation(from, _preparing);
        PollFailures();
        if (Failure is { } failure) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
    }

    private async Task ReleaseAfterPreparation(MapId from, Task? preparation)
    {
        // Each phase runs even when an earlier phase failed. Await actual producer tasks before destroying their data.
        if (preparation is not null)
            try { await preparation; }
            catch (OperationCanceledException) when (_exit.IsCancellationRequested && preparation.IsCanceled) { }
            catch (Exception error) { ReportFailure(error); }
        foreach (var arrival in _arrivalPreparations)
            try { await arrival; }
            catch (OperationCanceledException) when (_exit.IsCancellationRequested && arrival.IsCanceled) { }
            catch (Exception error) { ReportFailure(error); }
        _arrivalPreparations.Clear();
        // Keep identities captured at preparation: never resolve new transient receivers during teardown.
        foreach (var aware in _receivers.OfType<IMapAware>())
            try { aware.OnMapUnloading(from); }
            catch (Exception error) { ReportFailure(error); }
        if (_stream is not null)
            try { await _stream.DrainAsync(); }
            catch (Exception error) { ReportFailure(error); }
        // A failed native release still owns map inputs and bindings; a later explicit shutdown resumes the drain.
        if (_stream?.IsUnloaded == false) return;
        foreach (var participant in _receivers.OfType<IMapDataParticipant>())
            try { await participant.UnloadAsync(from); }
            catch (Exception error) { ReportFailure(error); }
        try { await _owner!.ReleaseMapAsync(from); }
        catch (Exception error) { ReportFailure(error); }
        finally
        {
            _loaded = null; _context = null; _preparing = null; _receivers = [];
        }
    }

    public Task Mount(IMapContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (_owner is null) throw new InvalidOperationException("Map runtime dependencies have not been bound.");
        if (_stopping) throw new InvalidOperationException("The map runtime is shutting down.");
        if (_loaded.HasValue || _preparing is not null)
            throw new InvalidOperationException("The map runtime mounts once at startup.");
        _failure = null; _failures.Clear();
        _mountPrepared = false;
        _mountCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _contentReadiness = null;
        try
        {
            var camera = _viewCamera!;
            camera.Far = BackdropInputs.CameraFar(context.Descriptor, context.SourceInputs, StageCamera.DefaultFarClip, camera.Near);
            _loaded = context.Descriptor.Id;
            _context = context;
            _preparing = PrepareMap(context, ++_mapGeneration);
        }
        catch (Exception error)
        {
            var failure = ReportFailure(error);
            _mountCompletion.TrySetException(failure);
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
        }
        return _mountCompletion.Task;
    }

    private async Task PrepareMap(IMapContext context, long generation)
    {
        Task? preparation = null;
        try
        {
            // Keep the participant chain in registry order: map-data consumers may depend on earlier producers.
            var receivers = _receivers = _owner!.RuntimeInstances<object>(context.Descriptor.Id).ToArray();
            var participants = PrepareParticipants(context, generation, receivers);
            preparation = participants;
            await preparation;
            if (generation != _mapGeneration) return;
            foreach (var aware in receivers.OfType<IMapAware>())
            {
                aware.OnMapLoaded(context);
            }
            _contentReadiness = ViewContentReadiness.Capture(GetParent());
            _mountPrepared = true;
        }
        catch (OperationCanceledException) when (_exit.IsCancellationRequested && preparation?.IsFaulted != true) { }
        catch (Exception error)
        {
            // Await exposes only one cause; the combined task retains every independently failed branch.
            var failure = Campus.Kernel.Diagnostics.RetainedFailure.Append(preparation?.Exception, error);
            FailArrival(ReportFailure(failure));
            if (!ReferenceEquals(failure, error))
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
            throw;
        }
    }

    private async Task PrepareParticipants(IMapContext context, long generation, object[] receivers)
    {
        foreach (var participant in receivers.OfType<IMapDataParticipant>())
        {
            await participant.Prepare(context);
            if (generation != _mapGeneration) return;
        }
    }


    public void ClearDestination()
    {
        _arrival?.Cancel(); _arrival = null;
        _stream!.ClearDestination();
        ArrivalChanged?.Invoke(null);
    }

    public IMapArrival Arrive(Pose destination, ViewMode mode, NavSpace? space = null, System.Numerics.Vector3? collisionFeet = null)
    {
        if (_stopping) throw new InvalidOperationException("The map runtime is shutting down.");
        if (!_mountPrepared) throw new InvalidOperationException("A map must be prepared before arrival.");
        _arrival?.Cancel();
        var arrival = new ArrivalRun(new(destination, mode, space, collisionFeet));
        _arrival = arrival;
        ArrivalChanged?.Invoke(arrival);
        _arrivalPreparations.Add(PrepareArrival(arrival, _context ?? throw new InvalidOperationException("Arrival requires the mounted map context."), _mapGeneration));
        return arrival;
    }

    private async Task PrepareArrival(ArrivalRun arrival, IMapContext context, long generation)
    {
        var cancellation = arrival.Token;
        try
        {
            var destination = arrival.Destination;
            var feet = destination.CollisionFeet ?? destination.Pose.Position;
            var point = new System.Numerics.Vector2(feet.X, feet.Z);
            var eyePoint = new System.Numerics.Vector2(destination.Pose.Position.X, destination.Pose.Position.Z);
            await _terrain!.PreparePoints(context, point == eyePoint ? [point] : [point, eyePoint], cancellation);
            cancellation.ThrowIfCancellationRequested();
            if (generation != _mapGeneration || !ReferenceEquals(_arrival, arrival)) return;
            arrival.MarkCompleted(ArrivalStep.TerrainPoint);
            if (destination.Mode == ViewMode.Walk)
            {
                var reach = new System.Numerics.Vector2(WalkRules.ArrivalCollisionRadius);
                await _terrain.PrepareRegion(context, new(point - reach, point + reach), cancellation);
                cancellation.ThrowIfCancellationRequested();
                if (generation != _mapGeneration || !ReferenceEquals(_arrival, arrival)) return;
                arrival.MarkCompleted(ArrivalStep.TerrainRegion);
            }
            if (_stopping || generation != _mapGeneration || !ReferenceEquals(_arrival, arrival) || arrival.Done.IsCompleted) return;
            // Prefetch may classify the destination medium; all destination readers start after preparation.
            _stream!.Prefetch(destination.Pose, destination.Mode, destination.Space, destination.CollisionFeet);
            arrival.Prepared = true;
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        catch (Exception error)
        {
            // Replaced/cancelled arrivals cannot publish readiness, but their genuine failures still belong to this map lifetime.
            var failure = ReportFailure(error);
            if (!_stopping && generation == _mapGeneration && ReferenceEquals(_arrival, arrival) && !arrival.Done.IsCompleted)
                FailArrival(failure);
        }
        finally { arrival.FinishPreparation(); }
    }

    public string DescribeReadiness() =>
        $"map={_loaded?.Value ?? "none"},mountPrepared={_mountPrepared},preparation={_preparing?.Status}," +
        $"stopping={_stopping},resident={ResidentReady},destination={DestinationReady},terrainPrepared={_arrival?.Prepared},failure={Failure?.Message ?? "none"}; " +
        $"stream[resident={_stream?.ResidentReady},destination={_stream?.DestinationReady},unloaded={_stream?.IsUnloaded}," +
        $"target={_stream?.Destination},failure={_stream?.Failure?.Message ?? "none"}]; " +
        $"dynamic[pending={_renderer?.Pending},error={_renderer?.Failure?.Message ?? "none"}]";

    public override void _Process(double delta)
    {
        PollFailures();
        if (_stopping) return;
        if (Failure is { } failure)
        {
            FailArrival(failure);
            return;
        }
        if (ResidentReady) _mountCompletion?.TrySetResult();
        var arrival = _arrival;
        if (arrival is null || arrival.Done.IsCompleted || !arrival.Prepared) return;
        try
        {
            if (ResidentReady) arrival.MarkCompleted(ArrivalStep.Resident);
            if (_stream!.DestinationReady) arrival.MarkCompleted(ArrivalStep.Destination);
            if (arrival.Destination.Mode == ViewMode.Walk &&
                _collision!.Ready(arrival.Destination.CollisionFeet ?? arrival.Destination.Pose.Position, WalkRules.ArrivalCollisionRadius))
                arrival.MarkCompleted(ArrivalStep.Collision);
            // Visible producers still certify the presented camera, including the current indoor floor.
            var contentReady = _contentReadiness?.IsReady(_camera!.Pose) == true;
            if (contentReady) arrival.MarkCompleted(ArrivalStep.Content);
            if (ResidentReady && DestinationReady && contentReady) arrival.Complete();
        }
        catch (Exception error)
        {
            FailArrival(ReportFailure(error));
        }
    }

    private void FailArrival([FailureHandler("SYS-MAPS")] Exception error)
    {
        _mountCompletion?.TrySetException(error);
        _arrival?.Fail(error);
    }

    public void StopNewWork()
    {
        _stopping = true;
        PollFailures();
        if (Failure is { } failure)
            try { FailArrival(failure); } catch (Exception error) { ReportFailure(error); }
        try { _exit.Cancel(); } catch (Exception error) { ReportFailure(error); }
        _mountCompletion?.TrySetCanceled();
        if (_arrival is { } arrival)
        {
            try { arrival.Cancel(); } catch (Exception error) { ReportFailure(error); }
            _arrival = null;
            try { ArrivalChanged?.Invoke(null); } catch (Exception error) { ReportFailure(error); }
        }
    }
    public override void _ExitTree()
    {
        StopNewWork();
        _mapGeneration++;
        _exit.Dispose();
    }

    [Flags]
    private enum ArrivalStep
    {
        None = 0, TerrainPoint = 1, Resident = 2, Destination = 4, Content = 8,
        TerrainRegion = 64, Collision = 128
    }

    private sealed class ArrivalRun(MapStreamDestination destination) : IMapArrival
    {
        private readonly TaskCompletionSource _done = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly CancellationTokenSource _cancellation = new();
        private readonly ArrivalStep _required = ArrivalStep.TerrainPoint | ArrivalStep.Resident |
            ArrivalStep.Destination | ArrivalStep.Content |
            (destination.Mode switch
            {
                ViewMode.Fly => ArrivalStep.None,
                ViewMode.Walk => ArrivalStep.TerrainRegion | ArrivalStep.Collision,
                _ => throw new ArgumentOutOfRangeException(nameof(destination.Mode))
            });
        private ArrivalStep _completed;
        private bool _preparationFinished;
        public CancellationToken Token => _cancellation.Token;
        public bool Prepared { get; set; }
        public MapStreamDestination Destination => destination;
        public Task Done => _done.Task;
        public int Completed => Done.IsCompletedSuccessfully ? Total : System.Numerics.BitOperations.PopCount((uint)_completed);
        public int Total => System.Numerics.BitOperations.PopCount((uint)_required);
        public event Action? ProgressChanged;
        public void MarkCompleted(ArrivalStep step)
        {
            if (Done.IsCompleted || (_required & step) == 0 || (_completed & step) != 0) return;
            _completed |= step;
            ProgressChanged?.Invoke();
        }
        public void Cancel()
        {
            if (!_done.TrySetCanceled()) return;
            try { _cancellation.Cancel(); } finally { ReleaseCancellation(); }
        }
        public void Complete()
        {
            if (!_done.TrySetResult()) return;
            ReleaseCancellation();
            ProgressChanged?.Invoke();
        }
        public void FinishPreparation() { _preparationFinished = true; ReleaseCancellation(); }
        private void ReleaseCancellation() { if (_preparationFinished && Done.IsCompleted) _cancellation.Dispose(); }
        public void Fail(Exception error)
        {
            if (!_done.TrySetException(error)) return;
            try { _cancellation.Cancel(); } finally { ReleaseCancellation(); }
        }
    }

}
