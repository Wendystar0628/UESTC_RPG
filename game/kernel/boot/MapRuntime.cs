using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Kernel.Maps;
using Campus.Kernel.Bake;
using Campus.Kernel.Registry;
using Campus.Kernel.Renderer;
using Godot;
using KernelServices = Campus.Kernel.Registry.Kernel;

namespace Campus.Kernel.Boot;

/// <summary>Scene-side map lifetime. The map service owns transition ordering; this host owns engine nodes.</summary>
[KernelService(typeof(IMapRuntime))]
[KernelService(typeof(IMapLifetime))]
[KernelNode("map-runtime")]
public partial class MapRuntime : Node, IMapRuntime, IMapLifetime
{
    private IMapStreamState? _stream;
    private ICollisionWorld? _collision;
    private IRenderReadiness? _renderer;
    private PipelineWarmup? _pipelines;
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
    private string? _error;
    private ViewContentReadiness? _contentReadiness;
    private object[] _receivers = [];
    private bool ResidentReady => _mountPrepared && _stream?.ResidentReady == true && _renderer?.Pending == false && Error is null;
    private bool DestinationReady => _mountPrepared && _stream?.DestinationReady == true &&
        Error is null && _arrival is { Prepared: true } arrival && (arrival.Destination.Mode switch
        {
            ViewMode.Fly => true,
            ViewMode.Walk => _collision!.Ready(arrival.Destination.Pose.Position, WalkRules.ArrivalCollisionRadius),
            _ => throw new InvalidOperationException("Unknown arrival view mode.")
        });
    public IMapArrival? Arrival => _arrival;
    public event Action<IMapArrival?>? ArrivalChanged;
    private string? Error => _error ?? _stream?.Error ?? _renderer?.Error ?? _collision?.Error;

    public override void _Ready()
    {
        ProcessPriority = FramePriority.MapRuntime;
        _stream = KernelServices.Get<IMapStreamState>();
        _collision = KernelServices.Get<ICollisionWorld>();
        _renderer = KernelServices.Get<IRenderReadiness>();
        _pipelines = KernelServices.Get<PipelineWarmup>();
        _camera = KernelServices.Get<ICameraRig>();
        _terrain = KernelServices.Get<ITerrainPreparation>();
    }

    /// <summary>Stage exit only: stop producers, drain preparation and release every map-owned resource.</summary>
    public Task ShutdownAsync(MapId map) => _shutdown ??= ShutdownMap(map);

    private async Task ShutdownMap(MapId from)
    {
        StopNewWork();
        _error = null;
        _mountPrepared = false;
        _contentReadiness = null;
        _mapGeneration++;
        await ReleaseAfterPreparation(from, _preparing);
        while (_stream?.IsUnloaded == false)
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        if (Error is { } error) throw new InvalidOperationException("Map shutdown failed: " + error);
    }

    private async Task ReleaseAfterPreparation(MapId from, Task? preparation)
    {
        var failures = new List<Exception>();
        try
        {
            // Drain preparation before releasing its data, so a late CPU result cannot republish an unloaded map.
            if (preparation is not null) await preparation;
            // Keep the load order and identities; teardown must not resolve fresh transient receivers.
            var receivers = _receivers;
            foreach (var aware in receivers.OfType<IMapAware>())
                try { aware.OnMapUnloading(from); }
                catch (Exception error) { ReportFailure(from, "map-aware-unload", aware.GetType().FullName, error); failures.Add(error); }
            foreach (var participant in receivers.OfType<IMapDataParticipant>())
                try { participant.Unload(from); }
                catch (Exception error) { ReportFailure(from, "map-data-unload", participant.GetType().FullName, error); failures.Add(error); }
        }
        catch (Exception error) { ReportFailure(from, "map-unload", null, error); failures.Add(error); }
        finally
        {
            try { await KernelServices.Services.ReleaseMapAsync(from); }
            catch (Exception error) { ReportFailure(from, "map-scope-release", null, error); failures.Add(error); }
            _loaded = null;
            _context = null;
            _preparing = null;
            _receivers = [];
            if (failures.Count > 0) _error = string.Join("; ", failures.Select(error => error.GetBaseException().Message));
        }
    }

    public Task Mount(IMapContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (_stopping) throw new InvalidOperationException("The map runtime is shutting down.");
        if (_loaded.HasValue || _preparing is not null)
            throw new InvalidOperationException("The map runtime mounts once at startup.");
        _error = null;
        _mountPrepared = false;
        _mountCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _contentReadiness = null;
        try
        {
            var camera = KernelServices.Get<Camera3D>();
            camera.Far = BackdropInputs.CameraFar(context.Descriptor, context.SourceInputs, StageCamera.DefaultFarClip, camera.Near);
            _loaded = context.Descriptor.Id;
            _context = context;
            _preparing = PrepareMap(context, ++_mapGeneration);
        }
        catch (Exception error)
        {
            _error = error.GetBaseException().Message;
            _mountCompletion.TrySetException(error);
            ReportFailure(context.Descriptor.Id, "map-mount", null, error);
            throw;
        }
        return _mountCompletion.Task;
    }

    private async Task PrepareMap(IMapContext context, long generation)
    {
        var phase = "map-instances";
        string? receiver = null;
        try
        {
            // Snapshot by reference before callbacks; producers prepare first, then consumers see one complete map.
            var receivers = _receivers = Instances(context.Descriptor.Id).ToArray();
            foreach (var participant in receivers.OfType<IMapDataParticipant>())
            {
                phase = "map-data-prepare";
                receiver = participant.GetType().FullName;
                await participant.Prepare(context);
                if (generation != _mapGeneration) return;
            }
            // Bake participants have prepared this map's build-time combination catalogue.
            // Submit it once while the caller's frontispiece/loading screen owns coverage.
            phase = "pipeline-precompile";
            receiver = typeof(PipelineWarmup).FullName;
            await _pipelines!.PrepareAsync(BakePipelineCatalog.Read(),
                TimeSpan.FromSeconds(BootTuning.StalledSeconds), _exit.Token);
            if (generation != _mapGeneration) return;
            foreach (var aware in receivers.OfType<IMapAware>())
            {
                phase = "map-aware-load";
                receiver = aware.GetType().FullName;
                aware.OnMapLoaded(context);
            }
            _contentReadiness = ViewContentReadiness.Capture(GetParent());
            _mountPrepared = true;
        }
        catch (OperationCanceledException) when (_exit.IsCancellationRequested) { }
        catch (Exception error)
        {
            ReportFailure(context.Descriptor.Id, phase, receiver, error);
            if (generation == _mapGeneration) _error = error.GetBaseException().Message;
        }
    }

    public IMapArrival Arrive(Pose destination, ViewMode mode, NavSpace? space = null)
    {
        if (_stopping) throw new InvalidOperationException("The map runtime is shutting down.");
        if (!_mountPrepared) throw new InvalidOperationException("A map must be prepared before arrival.");
        _arrival?.Cancel();
        var arrival = new ArrivalRun(new(destination, mode, space));
        _arrival = arrival;
        ArrivalChanged?.Invoke(arrival);
        _ = PrepareArrival(arrival, _context ?? throw new InvalidOperationException("Arrival requires the mounted map context."), _mapGeneration);
        return arrival;
    }

    private async Task PrepareArrival(ArrivalRun arrival, IMapContext context, long generation)
    {
        var cancellation = arrival.Token;
        try
        {
            var destination = arrival.Destination;
            var point = new System.Numerics.Vector2(destination.Pose.Position.X, destination.Pose.Position.Z);
            var product = FormattableString.Invariant($"arrival/{point.X:R}/{point.Y:R}");
            await _terrain!.PreparePoints(context, product, [point], cancellation);
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
            _stream!.Prefetch(destination.Pose, destination.Mode, destination.Space);
            arrival.Prepared = true;
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        catch (Exception error)
        {
            if (_stopping || generation != _mapGeneration || !ReferenceEquals(_arrival, arrival) || arrival.Done.IsCompleted) return;
            FailArrival(error);
        }
    }

    public string DescribeReadiness() =>
        $"map={_loaded?.Value ?? "none"},mountPrepared={_mountPrepared},preparation={_preparing?.Status}," +
        $"stopping={_stopping},resident={ResidentReady},destination={DestinationReady},terrainPrepared={_arrival?.Prepared},error={Error ?? "none"}; " +
        $"stream[resident={_stream?.ResidentReady},destination={_stream?.DestinationReady},unloaded={_stream?.IsUnloaded}," +
        $"target={_stream?.Destination},error={_stream?.Error ?? "none"}]; " +
        $"dynamic[pending={_renderer?.Pending},error={_renderer?.Error ?? "none"}]";

    public override void _Process(double delta)
    {
        if (_stopping) return;
        if (Error is { } failure)
        {
            FailArrival(new InvalidOperationException(failure));
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
                _collision!.Ready(arrival.Destination.Pose.Position, WalkRules.ArrivalCollisionRadius))
                arrival.MarkCompleted(ArrivalStep.Collision);
            // Visible producers still certify the presented camera, including the current indoor floor.
            var contentReady = _contentReadiness?.IsReady(_camera!.Pose) == true;
            if (contentReady) arrival.MarkCompleted(ArrivalStep.Content);
            if (ResidentReady && DestinationReady && contentReady) arrival.Complete();
        }
        catch (Exception error)
        {
            _error = error.GetBaseException().Message;
            FailArrival(error);
            ReportFailure(_loaded ?? default, "view-arrival", null, error);
        }
    }

    private void FailArrival(Exception error)
    {
        _mountCompletion?.TrySetException(error);
        _arrival?.Fail(error);
    }

    public void StopNewWork()
    {
        _stopping = true;
        _exit.Cancel();
        _mountCompletion?.TrySetCanceled();
        if (_arrival is { } arrival)
        {
            arrival.Cancel();
            _arrival = null;
            ArrivalChanged?.Invoke(null);
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
            _cancellation.Cancel(); _cancellation.Dispose();
        }
        public void Complete()
        {
            if (!_done.TrySetResult()) return;
            _cancellation.Dispose();
            ProgressChanged?.Invoke();
        }
        public void Fail(Exception error)
        {
            if (!_done.TrySetException(error)) return;
            _cancellation.Cancel(); _cancellation.Dispose();
        }
    }

    private static void ReportFailure(MapId map, string phase, string? receiver, Exception error) =>
        GD.PushError($"[WP-01][map={map.Value}][phase={phase}][receiver={receiver ?? "-"}] {error}");

    private static IEnumerable<object> Instances(MapId map) => KernelServices.Services.RuntimeInstances<object>(map);
}
