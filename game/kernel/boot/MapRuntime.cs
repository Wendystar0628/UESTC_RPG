using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Kernel.Maps;
using Campus.Kernel.Registry;
using Campus.Kernel.Renderer;
using Godot;
using Services = Campus.Kernel.Registry.Kernel;

namespace Campus.Kernel.Boot;

/// <summary>Scene-side map lifetime. The map service owns transition ordering; this host owns engine nodes.</summary>
[KernelService(typeof(IMapRuntime), Owner = "WP-01")]
[KernelService(typeof(IMapLifetime), Owner = "WP-01")]
[KernelNode("map-runtime", Owner = "WP-01")]
public partial class MapRuntime : Node, IMapRuntime, IMapLifetime
{
    private readonly List<(Type Type, Node Node)> _scoped = [];
    private IMapStreamState? _stream;
    private ICollisionWorld? _collision;
    private IRenderReadiness? _renderer;
    private MaterialWarmup? _materials;
    private MapId? _loaded;
    private Task? _preparing;
    private bool _mountPrepared;
    private TaskCompletionSource? _mountCompletion;
    private ArrivalRun? _arrival;
    private ICameraRig? _camera;
    private ITerrainPreparation? _terrain;
    private IMapContext? _context;
    private IVeilService? _veil;
    private IVeilLease? _readinessCover;
    private IVeilPresentation? _presentation;
    private IAppLoadingPresentation? _loading;
    private ulong _warmFrame;
    private long _warmDraw;
    private long? _readyDraw;
    private ulong _coveredDrawFrame;
    private bool _reuseWarmth;
    private Task? _shutdown;
    private bool _stopping;
    private long _mapGeneration;
    private string? _error;
    private bool _warming;
    private bool _samplesStarted;
    private long _drawFrames;
    private long _lastWarmDraw;
    private int _stable;
    private PipelineCounts _previous;
    private ViewContentReadiness? _contentReadiness;
    private bool ResidentReady => _mountPrepared && _stream?.ResidentReady == true && _renderer?.Pending == false && Error is null;
    private bool DestinationReady => _mountPrepared && _stream?.DestinationReady == true &&
        _renderer?.PipelineReady == true && Error is null && _arrival is { Prepared: true } arrival && (arrival.Destination.Mode switch
        {
            ViewMode.Fly => true,
            ViewMode.Walk => _collision!.Ready(arrival.Destination.Pose.Position, WalkRules.ArrivalCollisionRadius),
            _ => throw new InvalidOperationException("Unknown arrival view mode.")
        });
    private bool WarmReady { get; set; }
    public IMapArrival? Arrival => _arrival;
    public event Action<IMapArrival?>? ArrivalChanged;
    private bool IsCovered => _loading?.IsCovered == true || _presentation?.IsCovered == true;
    private string? Error => _error ?? _materials?.Error ?? _stream?.Error ?? _renderer?.Error ?? _collision?.Error;

    public override void _Ready()
    {
        ProcessPriority = FramePriority.StreamMount + 1;
        _stream = Services.Get<IMapStreamState>();
        _collision = Services.Get<ICollisionWorld>();
        _renderer = Services.Get<IRenderReadiness>();
        _materials = Services.Get<MaterialWarmup>();
        _camera = Services.Get<ICameraRig>();
        _terrain = Services.Get<ITerrainPreparation>();
        _veil = Services.Get<IVeilService>();
        _presentation = Services.Get<IVeilPresentation>();
        _loading = Services.Get<IAppLoadingPresentation>();
        RenderingServer.Singleton.FramePostDraw += CountDraw;
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
        _warming = false;
        WarmReady = false;
        _materials?.End(reset: true);
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
            var receivers = Instances(from).ToArray();
            foreach (var aware in receivers.OfType<IMapAware>())
                try { aware.OnMapUnloading(from); }
                catch (Exception error) { ReportFailure(from, "map-aware-unload", aware.GetType().FullName, error); failures.Add(error); }
            foreach (var participant in receivers.OfType<IMapDataParticipant>())
                try { participant.Unload(from); }
                catch (Exception error) { ReportFailure(from, "map-data-unload", participant.GetType().FullName, error); failures.Add(error); }
            foreach (var (type, node) in _scoped)
            {
                Services.Services.Release(type);
                node.GetParent()?.RemoveChild(node);
                node.QueueFree();
            }
            _scoped.Clear();
            _loaded = null;
            _context = null;
        }
        catch (Exception error) { ReportFailure(from, "map-unload", null, error); failures.Add(error); }
        finally
        {
            _preparing = null;
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
            var camera = Services.Get<Camera3D>();
            camera.Far = BackdropInputs.CameraFar(context.Descriptor, context.SourceInputs, BootTuning.CameraFar, camera.Near);
            var nodes = Services.Services.ForMap(context.Descriptor.Id.Value)
                .Where(entry => entry.Kind == "node" && !string.IsNullOrEmpty(entry.Map))
                .OrderBy(entry => entry.Id, StringComparer.Ordinal)
                .Select(entry => (entry.Type, Node: Services.Services.Create(entry.Type) as Node
                    ?? throw new InvalidOperationException($"KernelNode does not inherit Node: {entry.Id}.")))
                .ToArray();
            _scoped.AddRange(nodes);
            foreach (var (_, node) in nodes) GetParent().AddChild(node);
            if (nodes.Any(item => !item.Node.IsInsideTree()))
                throw new InvalidOperationException("Every map kernel node must enter the tree before map callbacks.");
            _loaded = context.Descriptor.Id;
            _context = context;
            _preparing = PrepareMap(context, ++_mapGeneration);
        }
        catch (Exception error)
        {
            _error = error.GetBaseException().Message;
            _mountCompletion.TrySetException(error);
            ReportFailure(context.Descriptor.Id, "map-node-mount", null, error);
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
            var receivers = Instances(context.Descriptor.Id).ToArray();
            foreach (var participant in receivers.OfType<IMapDataParticipant>())
            {
                phase = "map-data-prepare";
                receiver = participant.GetType().FullName;
                await participant.Prepare(context);
                if (generation != _mapGeneration) return;
            }
            foreach (var aware in receivers.OfType<IMapAware>())
            {
                phase = "map-aware-load";
                receiver = aware.GetType().FullName;
                aware.OnMapLoaded(context);
            }
            _contentReadiness = ViewContentReadiness.Capture(GetParent());
            _mountPrepared = true;
        }
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
        var arrival = new ArrivalRun(new(destination, mode, space), CancelArrival);
        _arrival = arrival;
        StartWarmup();
        ArrivalChanged?.Invoke(arrival);
        _ = PrepareArrival(arrival, _context ?? throw new InvalidOperationException("Arrival requires the mounted map context."), _mapGeneration);
        return arrival;
    }

    private void StartWarmup()
    {
        // Every warm cycle needs a later real draw; new samples also require presented coverage.
        WarmReady = false;
        _warming = true;
        _samplesStarted = false;
        _stable = 0;
        _warmFrame = Engine.GetProcessFrames();
        _warmDraw = _drawFrames;
        _readyDraw = null;
        _lastWarmDraw = _drawFrames;
        _previous = PipelineCounts.Read();
        _reuseWarmth = _materials!.IsReady;
        if (!_reuseWarmth) _materials.End(reset: true);
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

    private void CancelArrival(ArrivalRun arrival)
    {
        if (!ReferenceEquals(_arrival, arrival)) return;
        _warming = false;
        WarmReady = false;
        _readyDraw = null;
        _materials?.End();
        ReleaseReadinessCover();
    }

    public string DescribeReadiness() =>
        $"map={_loaded?.Value ?? "none"},mountPrepared={_mountPrepared},preparation={_preparing?.Status}," +
        $"stopping={_stopping},resident={ResidentReady},destination={DestinationReady},warm={WarmReady}," +
        $"warming={_warming},terrainPrepared={_arrival?.Prepared},samplesStarted={_samplesStarted},stable={_stable}/{BootTuning.WarmFrames}," +
        $"drawFrames={_drawFrames},lastWarmDraw={_lastWarmDraw},pipeline={_previous},error={Error ?? "none"}; " +
        $"stream[resident={_stream?.ResidentReady},destination={_stream?.DestinationReady},unloaded={_stream?.IsUnloaded}," +
        $"target={_stream?.Destination},error={_stream?.Error ?? "none"}]; " +
        $"dynamic[pending={_renderer?.Pending},pipelineReady={_renderer?.PipelineReady},error={_renderer?.Error ?? "none"}]; " +
        $"materials[{_materials?.DescribeReadiness() ?? "missing"}]";
    public override void _Process(double delta)
    {
        if (_stopping) return;
        if (Error is { } failure)
        {
            var hasWaiter = _mountCompletion is { Task.IsCompleted: false } || _arrival is { Done.IsCompleted: false };
            if (_warming || hasWaiter)
            {
                var error = new InvalidOperationException(failure);
                FailArrival(error);
                // Formal requests propagate their fault through their tasks; autonomous warming has no awaiting caller.
                if (!hasWaiter) ReportFailure(_loaded ?? default, "view-warmup", null, error);
            }
            return;
        }
        if (ResidentReady) _mountCompletion?.TrySetResult();
        try { AdvanceWarmup(); }
        catch (Exception error)
        {
            _error = error.GetBaseException().Message;
            FailArrival(error);
            ReportFailure(_loaded ?? default, "view-warmup", null, error);
        }
    }

    private void FailArrival(Exception error)
    {
        _mountCompletion?.TrySetException(error);
        _arrival?.Fail(error);
        _warming = false;
        WarmReady = false;
        _readyDraw = null;
        _samplesStarted = false;
        _materials?.End();
        ReleaseReadinessCover();
    }

    private void AdvanceWarmup()
    {
        var arrival = _arrival is { Done.IsCompleted: false } pending ? pending : null;
        if (!_warming)
        {
            // Revealed views can register new materials/layouts without an arrival (ordinary movement or camera changes).
            // The provider revision invalidates one cached batch; idle frames never take repeated snapshots.
            if (!_mountPrepared || arrival is not null || !_materials!.HasPendingWarm) return;
            StartWarmup();
        }
        if (arrival is not null)
        {
            if (!arrival.Prepared) return;
            if (_stream!.DestinationReady) arrival.MarkCompleted(ArrivalStep.Destination);
            if (arrival.Destination.Mode == ViewMode.Walk &&
                _collision!.Ready(arrival.Destination.Pose.Position, WalkRules.ArrivalCollisionRadius))
                arrival.MarkCompleted(ArrivalStep.Collision);
            if (ContentReady) arrival.MarkCompleted(ArrivalStep.Content);
        }
        if (!ResidentReady) { _readyDraw = null; return; }
        arrival?.MarkCompleted(ArrivalStep.Resident);
        if (_loading?.Active == true && !_loading.IsCovered) return;
        if (!_samplesStarted)
        {
            // Existing warm pipelines keep the same-map fast path. Newly required variants must be sampled.
            if (_reuseWarmth && _renderer!.PipelineReady && !_materials!.HasPendingWarm)
            {
                if (_materials!.SamplesReleased) FinishWarmupAfterDraw(arrival);
                return;
            }
            // The warm cycle owns only its temporary cover; external transit/boot covers stay with their callers.
            if (!IsCovered && _loading?.Active != true)
                _readinessCover ??= _veil!.Cover(VeilReason.Transit, 0f);
            if (_coveredDrawFrame <= _warmFrame || !IsCovered) return;
            _reuseWarmth = false;
            _materials!.Begin();
            _samplesStarted = true;
        }
        if (_coveredDrawFrame <= _warmFrame || !IsCovered) return;
        if (_drawFrames == _lastWarmDraw) return;
        _lastWarmDraw = _drawFrames;
        var counts = PipelineCounts.Read();
        _stable = ResidentReady && _materials!.IsReady && _materials.SamplesReleased && counts == _previous ? _stable + 1 : 0;
        _previous = counts;
        if (_stable >= BootTuning.WarmFrames) FinishWarmupAfterDraw(arrival);
    }

    private void FinishWarmupAfterDraw(ArrivalRun? arrival)
    {
        if (!_renderer!.PipelineReady)
        {
            _readyDraw = null;
            return;
        }
        arrival?.MarkCompleted(ArrivalStep.Pipeline);
        if (!ContentReady || (arrival is not null && !DestinationReady))
        {
            _readyDraw = null;
            return;
        }
        // The final evidence must be a draw submitted after this destination's barriers became ready.
        _readyDraw ??= _drawFrames;
        if (_drawFrames > _readyDraw.Value && _drawFrames > _warmDraw) CompleteWarmup(arrival);
    }

    // Visible-content producers apply the presented camera. Stream/collision separately certify the arrival destination.
    private bool ContentReady => _contentReadiness?.IsReady(_camera!.Pose) == true;

    private void ReleaseReadinessCover()
    {
        _readinessCover?.Dispose();
        _readinessCover = null;
    }

    private void CompleteWarmup(ArrivalRun? arrival)
    {
        WarmReady = true;
        _materials!.End();
        _warming = false;
        ReleaseReadinessCover();
        arrival?.Complete();
    }

    private void CountDraw()
    {
        _drawFrames++;
        if (IsCovered) _coveredDrawFrame = Engine.GetProcessFrames();
    }

    public void StopNewWork()
    {
        _stopping = true;
        _mountCompletion?.TrySetCanceled();
        if (_arrival is { } arrival)
        {
            arrival.Cancel();
            _arrival = null;
            ArrivalChanged?.Invoke(null);
        }
        _warming = false;
        WarmReady = false;
        _readyDraw = null;
        _samplesStarted = false;
        _reuseWarmth = false;
        _stable = 0;
        _materials?.End(reset: true);
        ReleaseReadinessCover();
    }
    public override void _ExitTree()
    {
        StopNewWork();
        _mapGeneration++;
        RenderingServer.Singleton.FramePostDraw -= CountDraw;
        if (_materials is not null && GodotObject.IsInstanceValid(_materials)) _materials.End(reset: true);
    }

    [Flags]
    private enum ArrivalStep
    {
        None = 0, TerrainPoint = 1, Resident = 2, Destination = 4, Content = 8, Pipeline = 16, Draw = 32,
        TerrainRegion = 64, Collision = 128
    }

    private sealed class ArrivalRun(MapStreamDestination destination, Action<ArrivalRun> onCancel) : IMapArrival
    {
        private readonly TaskCompletionSource _done = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly CancellationTokenSource _cancellation = new();
        private readonly ArrivalStep _required = ArrivalStep.TerrainPoint | ArrivalStep.Resident |
            ArrivalStep.Destination | ArrivalStep.Content | ArrivalStep.Pipeline | ArrivalStep.Draw |
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
            onCancel(this);
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

    private static IEnumerable<object> Instances(MapId map) => Services.Services.ForMap(map.Value)
        .Where(entry => entry.Kind is "node" or "service" or "save")
        .GroupBy(entry => entry.Type)
        .Select(group => group.OrderBy(entry => entry.Kind == "node" ? 0 : 1).ThenBy(entry => entry.Id, StringComparer.Ordinal).First())
        .OrderBy(entry => entry.Id, StringComparer.Ordinal)
        .Select(entry =>
        {
            try { return RuntimeComposition.Resolve(Services.Services, entry); }
            catch (Exception error)
            {
                throw new InvalidOperationException($"Map runtime cannot resolve {entry.Kind} '{entry.Id}' ({entry.Type.FullName}).", error);
            }
        }).Distinct(ReferenceEqualityComparer.Instance);
}
