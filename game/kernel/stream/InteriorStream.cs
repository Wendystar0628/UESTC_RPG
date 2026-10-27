using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Maps;
using Campus.Kernel.Frame;
using Campus.Kernel.Renderer;
using Godot;
using N = System.Numerics;

namespace Campus.Kernel.Stream;

/// <summary>Destination/current-floor IF and adjacent IS use the same bake and mount paths as outdoor tiles.</summary>
internal sealed class InteriorStream(IBakeService bake, IStreamMountQueue mount, StreamMaterials materials,
    IReflectionWorld reflections, Action<PreparedTile> retire, bool captureMode, IStreamGeometryPreparation geometry,
    Action<StreamTileChange> changed, Action<PreparedTile> detach, Func<bool> detached, Func<double>? clock = null)
{
    private readonly Func<double> _clock = clock ?? (static () => FrameClock.MonotonicMilliseconds);
    private readonly Dictionary<BakeKey, Layer> _layers = [];
    private readonly List<Layer> _visibilityOrder = [];
    private readonly List<(Layer Layer, int Order)> _layerSnapshot = [];
    private readonly List<int> _storeyOrder = [];
    private readonly List<Layer> _draining = [];
    private readonly HashSet<BakeKey> _destination = [], _visible = [], _pinned = [];
    private IMapContext? _context;
    private readonly Dictionary<FeatureId, IReadOnlyList<int>> _atriumStoreys = [];
    public Exception? Failure { get; private set; }
    private readonly List<Exception> _failures = [];
    public int Preparations
    {
        get
        {
            var count = 0;
            foreach (var layer in _layers.Values) if (layer.Preparing is not null) count++;
            foreach (var layer in _draining) if (layer.Preparing is not null) count++;
            return count;
        }
    }
    public int Bakes
    {
        get
        {
            var count = 0;
            foreach (var layer in _layers.Values) if (layer.Owner is null && !layer.Finished) count++;
            foreach (var layer in _draining) if (layer.Owner is null && !layer.Finished) count++;
            return count;
        }
    }
    public int Retirements(bool unloading) => _draining.Count + (unloading ? _layers.Count : 0);
    public bool IsUnloaded => _context is null && _layers.Count == 0 && _draining.Count == 0;
    public IEnumerable<PreparedTile> Prepared => _layers.OrderBy(pair => pair.Key.Detail, StringComparer.Ordinal)
        .Select(pair => pair.Value.Prepared).OfType<PreparedTile>();
    public bool DestinationReady
    {
        get
        {
            if (Failure is not null || !VisibilityReady) return false;
            foreach (var key in _destination) if (!Ready(_layers[key])) return false;
            return true;
        }
    }
    public bool VisibilityReady
    {
        get
        {
            foreach (var layer in _layers.Values)
                if (layer.Visible != _visible.Contains(layer.Key) || _visible.Contains(layer.Key) && !Ready(layer)) return false;
            return true;
        }
    }
    public void Load(IMapContext context, BuildContext? generation = null)
    {
        _context = context; Failure = null; _failures.Clear(); _atriumStoreys.Clear();
        if (generation is null) return;
        foreach (var layout in generation.InteriorLayouts.Values)
            _atriumStoreys.Add(layout.Feature, layout.AtriumVisibleStoreys);
    }
    public void Prefetch(NavSpace? space, N.Vector3 position)
    {
        _destination.Clear();
        if (space is { Building: not null } value) Request(value, position, BakePriority.KnownDestination, _destination, true);
    }
    public void Select(CameraMedium medium, N.Vector3 position)
    {
        _visible.Clear();
        if (medium.Kind == MediumKind.Interior && medium.Feature is { } building)
            Request(new(building, checked((short)medium.Storey)), position, BakePriority.InteriorPrefetch, _visible, false);
    }
    public void ClearDestination()
    {
        _destination.Clear(); _visible.Clear();
    }
    public void ResetPins() => _pinned.Clear();
    public void Pin(NavSpace? space, N.Vector3 position, HashSet<BakeKey> keys)
    {
        if (space is { Building: not null } value)
        {
            Request(value, position, BakePriority.KnownDestination, keys, true);
            _pinned.UnionWith(keys);
        }
    }
    public bool ResourcesReady(IReadOnlySet<BakeKey> keys)
    {
        if (_context is null || Failure is not null) return false;
        foreach (var key in keys)
            if (!_pinned.Contains(key) || !_layers.TryGetValue(key, out var layer) || layer.Revision != mount.Revision || !Ready(layer)) return false;
        return true;
    }
    public void RefreshPriorities()
    {
        foreach (var layer in _layers.Values)
        {
            layer.Priority = _destination.Contains(layer.Key) || _pinned.Contains(layer.Key) ? BakePriority.KnownDestination :
                _visible.Contains(layer.Key) ? BakePriority.InteriorPrefetch : BakePriority.Background;
            if (layer.Work is { } work) work.Priority = layer.Priority;
            if (_destination.Contains(layer.Key) || _visible.Contains(layer.Key) || _pinned.Contains(layer.Key)) bake.Ensure(layer.Key, layer.Priority);
        }
    }
    private void Request(NavSpace space, N.Vector3 position, BakePriority priority, HashSet<BakeKey> target, bool blocking)
    {
        if (_context is null || space.Building is not { } building) return;
        var entry = _context.World.Interiors.Get(building);
        if (entry is null || !entry.Open || string.IsNullOrWhiteSpace(entry.ProviderId))
        {
            if (blocking) RecordFailure(new InvalidOperationException($"Interior destination is not an open registered building: {building.Value}."));
            return;
        }
        var area = _context.World.Interiors.AreaAt(building, position);
        if (area is null || !InteriorOpenPart.IsIndoor(area.Kind) || area.Index != space.Floor)
        {
            if (blocking) RecordFailure(new InvalidOperationException($"Interior destination is outside the open area: {building.Value}/{space.Floor}."));
            return;
        }
        var atrium = _atriumStoreys.GetValueOrDefault(building);
        _storeyOrder.Clear();
        foreach (var storey in entry.OpenStoreys)
            if (Math.Abs(storey - space.Floor) <= 1 || atrium is not null && atrium.Contains(space.Floor) && atrium.Contains(storey))
                _storeyOrder.Add(storey);
        _storeyOrder.Sort();
        foreach (var storey in _storeyOrder)
        {
            var detail = storey == space.Floor ? "IF" : "IS";
            var key = new BakeKey(_context.Descriptor.Id, "interior." + entry.ProviderId,
                FormattableString.Invariant($"{building.Value}/{storey}/{detail}"));
            target.Add(key);
            if (!_layers.ContainsKey(key)) _layers.Add(key, new(key, bake.Ensure(key, priority), mount.Revision, priority, building, storey));
            else if (priority == BakePriority.KnownDestination) bake.Ensure(key, priority);
        }
    }
    public void Poll(int availablePreparations, bool background = false)
    {
        SnapshotLayers();
        _layerSnapshot.Sort(static (a, b) =>
        {
            var priority = ((byte)a.Layer.Priority).CompareTo((byte)b.Layer.Priority);
            return priority != 0 ? priority : CompareDetail(a, b);
        });
        foreach (var (layer, _) in _layerSnapshot)
        {
            if ((layer.Priority == BakePriority.Background) != background) continue;
            if (layer.Preparing is not null || layer.Owner is not null || layer.Failure is not null) continue;
            if (layer.Handle.State is BakeState.Pending or BakeState.Running) continue;
            if (_context is null || layer.Revision != mount.Revision) { layer.Finished = true; continue; }
            if (layer.Handle.State == BakeState.Failed)
            {
                if (layer.Handle.Failure is { } error and not OperationCanceledException) Fail(layer, error);
                layer.Finished = true; continue;
            }
            if (layer.Handle.State != BakeState.Ready) throw new InvalidDataException("Unknown interior bake state.");
            if (availablePreparations <= 0) continue;
            if (layer.Handle.Tile is not { } bundle || bundle.Key != layer.Key)
            {
                Fail(layer, new InvalidDataException($"Interior bake returned the wrong product: {layer.Key}.")); continue;
            }
            layer.Owner = new PreparedTile(bundle);
            layer.Preparing = Initialize(layer.Owner, bundle);
            availablePreparations--;
        }
        _layerSnapshot.Clear();
    }
    private void SnapshotLayers()
    {
        _layerSnapshot.Clear();
        foreach (var layer in _layers.Values) _layerSnapshot.Add((layer, _layerSnapshot.Count));
    }
    private static int CompareDetail((Layer Layer, int Order) a, (Layer Layer, int Order) b)
    {
        var detail = StringComparer.Ordinal.Compare(a.Layer.Key.Detail, b.Layer.Key.Detail);
        return detail != 0 ? detail : a.Order.CompareTo(b.Order);
    }
    private async Task Initialize(PreparedTile owner, TileBundle bundle) => await geometry.PrepareAsync(owner, bundle).ConfigureAwait(false);

    public void HarvestPreparations(Rid scenario)
    {
        foreach (var layer in _layers.Values)
        {
            if (!layer.Finished && layer.Preparing is { IsCompleted: true } task)
            {
                if (task.IsCompletedSuccessfully)
                {
                    layer.Preparing = null;
                    layer.Prepared = layer.Owner;
                    if (_context is null || layer.Revision != mount.Revision)
                    { retire(layer.Owner!); layer.Owner = layer.Prepared = null; layer.Finished = true; }
                    else
                    {
                        layer.Work = new VisualMountWork(layer.Key, [layer.Prepared!], layer.Revision, scenario, layer.Priority, materials, reflections,
                            () => SetMounted(layer), () => ClearMounted(layer), error => Fail(layer, error), detach, detached);
                        mount.Enqueue(layer.Work);
                    }
                }
                else
                {
                    try { task.GetAwaiter().GetResult(); }
                    catch (OperationCanceledException) when (task.IsCanceled) { }
                    catch (Exception error) { Fail(layer, error); }
                    layer.Finished = true;
                }
            }
        }
        for (var i = _draining.Count - 1; i >= 0; i--)
        {
            var layer = _draining[i];
            if (!layer.Finished && layer.Preparing is { IsCompleted: true } pending)
            {
                layer.Finished = true;
                if (pending.IsCompletedSuccessfully)
                { layer.Preparing = null; retire(layer.Owner!); layer.Owner = layer.Prepared = null; }
                else
                    try { pending.GetAwaiter().GetResult(); }
                    catch (OperationCanceledException) when (pending.IsCanceled) { }
                    catch (Exception error) { Fail(layer, error); }
            }
            if (layer.Preparing is null && layer.Owner is null && layer.Handle.State is BakeState.Ready or BakeState.Failed)
            {
                if (layer.Handle.Failure is { } error and not OperationCanceledException) Fail(layer, error);
                _draining.RemoveAt(i);
            }
        }
        if (_context is null)
        {
            SnapshotLayers();
            foreach (var (layer, _) in _layerSnapshot)
                if (layer.Owner is null && (layer.Finished ||
                    layer.Preparing is null && layer.Handle.State is BakeState.Ready or BakeState.Failed)) _layers.Remove(layer.Key);
            _layerSnapshot.Clear();
        }
    }
    public void ApplyVisibility(double budgetMilliseconds)
    {
        var start = _clock();
        _visibilityOrder.Clear();
        foreach (var layer in _layers.Values) _visibilityOrder.Add(layer);
        _visibilityOrder.Sort(static (a, b) =>
        {
            var building = StringComparer.Ordinal.Compare(a.Building.Value, b.Building.Value);
            if (building != 0) return building;
            var storey = a.Storey.CompareTo(b.Storey);
            return storey != 0 ? storey : StringComparer.Ordinal.Compare(a.Key.Detail, b.Key.Detail);
        });
        for (var cursor = 0; cursor < _visibilityOrder.Count;)
        {
            var first = _visibilityOrder[cursor];
            var end = cursor + 1;
            while (end < _visibilityOrder.Count && _visibilityOrder[end].Building == first.Building &&
                _visibilityOrder[end].Storey == first.Storey) end++;
            var ready = true; var pending = false;
            for (var i = cursor; i < end; i++)
            {
                var layer = _visibilityOrder[i];
                var visible = _visible.Contains(layer.Key);
                if (visible && !Ready(layer)) ready = false;
                if (visible != layer.Visible) pending = true;
            }
            // Keep the old IF/IS until this floor's entire requested replacement can be shown.
            // I0 stays with the outdoor owner and retains its existing fallback behavior.
            if (ready && pending)
            {
                if (_clock() - start >= budgetMilliseconds) return;
                ApplyVisibilityGroup(cursor, end);
            }
            cursor = end;
        }
    }
    private void ApplyVisibilityGroup(int first, int end)
    {
        for (var i = first; i < end; i++)
        {
            var layer = _visibilityOrder[i];
            var visible = _visible.Contains(layer.Key);
            if (layer.Visible == visible) continue;
            // Zero is the shader's fully visible sentinel, rather than a zero-duration interpolation.
            layer.Prepared!.SetVisible(visible, visible && !captureMode ? FrameClock.FadeSeconds : 0);
            layer.Visible = visible;
        }
    }
    public void AdvanceRetention(double delta, System.Numerics.Vector3 camera)
    {
        if (_context is null) return;
        SnapshotLayers();
        _layerSnapshot.Sort(CompareDetail);
        foreach (var (layer, _) in _layerSnapshot)
        {
            if (layer.Visible || _visible.Contains(layer.Key) || _destination.Contains(layer.Key) || _pinned.Contains(layer.Key)) { layer.IdleSeconds = 0; continue; }
            layer.IdleSeconds += Math.Clamp(delta, 0, .1);
            var outside = false;
            if (_context.World.Features.TryGetValue(layer.Building, out var building))
            {
                var p = new System.Numerics.Vector2(camera.X, camera.Z);
                outside = building.Bounds.HorizontalDistanceSquared(new(p.X, 0, p.Y)) > 40 * 40;
            }
            if (!outside && layer.IdleSeconds < 20) continue;
            if (layer.Failure is not null) continue;
            RetireLayer(layer);
            _layers.Remove(layer.Key);
            if (layer.Preparing is not null || layer.Handle.State is BakeState.Pending or BakeState.Running) _draining.Add(layer);
        }
        _layerSnapshot.Clear();
    }
    private bool Ready(Layer layer) => layer.Failure is null && layer.Mounted && layer.Prepared is not null;
    private void SetMounted(Layer layer)
    {
        layer.Mounted = true;
        changed(new(layer.Key, StreamMountKind.Visual, layer.Revision, StreamTileChangeKind.Mounted,
            layer.Prepared!.Bytes, FrameClock.MonotonicMilliseconds));
    }
    private void ClearMounted(Layer layer)
    {
        if (!layer.Mounted) return;
        layer.Mounted = false;
        changed(new(layer.Key, StreamMountKind.Visual, layer.Revision, StreamTileChangeKind.Unmounted,
            layer.Prepared!.Bytes, FrameClock.MonotonicMilliseconds));
    }
    private void Fail(Layer layer, [FailureHandler("ENG-PERF-STREAM")] Exception error)
    {
        layer.Failure = error;
        RecordFailure(error);
    }
    private void RecordFailure([FailureHandler("ENG-PERF-STREAM")] Exception error)
    {
        if (error is AggregateException { InnerExceptions.Count: > 0 } aggregate)
        {
            foreach (var inner in aggregate.InnerExceptions) RecordFailure(inner);
            return;
        }
        if (_failures.Contains(error)) return;
        _failures.Add(error);
        Failure = _failures.Count == 1 ? error : new AggregateException("Interior stream failed.", _failures);
    }
    public void Unload()
    {
        _context = null; _destination.Clear(); _visible.Clear(); _pinned.Clear(); _atriumStoreys.Clear();
        foreach (var layer in _layers.Values)
        {
            try { RetireLayer(layer); }
            catch (Exception error) { Fail(layer, error); }
            _draining.Add(layer);
        }
        _layers.Clear(); _visibilityOrder.Clear(); _layerSnapshot.Clear(); _storeyOrder.Clear();
    }
    public async Task DrainAsync()
    {
        Unload();
        for (var i = _draining.Count - 1; i >= 0; i--)
        {
            var layer = _draining[i];
            try { await layer.Handle.Done; }
            catch (OperationCanceledException) when (layer.Handle.Done.IsCanceled) { }
            catch (Exception error) { Fail(layer, error); }
            if (layer.Preparing is { } task)
            {
                try { await task; }
                catch (OperationCanceledException) when (task.IsCanceled) { }
                catch (Exception error) { Fail(layer, error); }
                finally { layer.Preparing = null; }
            }
            try { RetireLayer(layer); }
            catch (Exception error) { Fail(layer, error); }
            _draining.RemoveAt(i);
        }
        if (_failures.Count == 1) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(_failures[0]).Throw();
        if (_failures.Count > 1) throw new AggregateException("Interior stream drain failed.", _failures);
    }
    private void RetireLayer(Layer layer)
    {
        if (layer.Preparing is not null || layer.Retiring) return;
        layer.Retiring = true;
        if (layer.Work is { } work) mount.Retire(work);
        else if (layer.Owner is { } owner) retire(owner);
    }
    private sealed class Layer(BakeKey key, IBakeHandle handle, long revision, BakePriority priority, FeatureId building, int storey)
    {
        public BakeKey Key { get; } = key;
        public IBakeHandle Handle { get; } = handle;
        public long Revision { get; } = revision;
        public BakePriority Priority { get; set; } = priority;
        public FeatureId Building { get; } = building;
        public int Storey { get; } = storey;
        public double IdleSeconds;
        public VisualMountWork? Work;
        public Task? Preparing;
        // Owner is retained before the worker starts; Prepared is published only after successful initialization.
        public PreparedTile? Owner;
        public PreparedTile? Prepared;
        public Exception? Failure;
        public bool Mounted, Visible, Finished, Retiring;
    }
}
