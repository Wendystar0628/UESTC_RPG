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
    IReflectionWorld reflections, Action<PreparedTile> retire, bool captureMode, IStreamGeometryPreparation geometry)
{
    private readonly Dictionary<BakeKey, Layer> _layers = [];
    private readonly List<Layer> _draining = [];
    private readonly HashSet<BakeKey> _destination = [], _visible = [];
    private IMapContext? _context;
    private readonly Dictionary<FeatureId, IReadOnlyList<int>> _atriumStoreys = [];
    public Exception? Failure { get; private set; }
    private readonly List<Exception> _failures = [];
    public int Preparations => _layers.Values.Concat(_draining).Count(layer => layer.Preparing is not null);
    public bool IsUnloaded => _context is null && _layers.Count == 0 && _draining.Count == 0;
    public IEnumerable<PreparedTile> Prepared => _layers.OrderBy(pair => pair.Key.Detail, StringComparer.Ordinal)
        .Select(pair => pair.Value.Prepared).OfType<PreparedTile>();
    public bool DestinationReady => Failure is null && _destination.All(key => Ready(_layers[key]));
    public void Load(IMapContext context, BuildContext? generation = null)
    {
        _context = context; Failure = null; _failures.Clear(); _atriumStoreys.Clear();
        if (generation is null) return;
        foreach (var layout in generation.InteriorLayouts.Values)
            _atriumStoreys.Add(layout.Feature, layout.AtriumVisibleStoreys);
    }
    public void Prefetch(NavSpace? space, N.Vector3 position)
    {
        _destination.Clear(); Failure = null;
        if (space is { Building: not null } value) Request(value, position, BakePriority.KnownDestination, _destination, true);
    }
    public void Select(CameraMedium medium, N.Vector3 position)
    {
        _visible.Clear();
        if (medium.Kind == MediumKind.Interior && medium.Feature is { } building)
            Request(new(building, checked((short)medium.Storey)), position, BakePriority.InteriorPrefetch, _visible, false);
    }
    public void Preload(NavSpace? space, N.Vector3 position, Action<BakeKey> enqueue)
    {
        if (space is { Building: not null } value) Request(value, position, BakePriority.KnownDestination, null, false, enqueue);
    }
    private void Request(NavSpace space, N.Vector3 position, BakePriority priority, HashSet<BakeKey>? target, bool blocking, Action<BakeKey>? preload = null)
    {
        if (_context is null || space.Building is not { } building) return;
        var entry = _context.World.Interiors.Get(building);
        if (entry is null || !entry.Open || string.IsNullOrWhiteSpace(entry.ProviderId))
        {
            if (blocking) FailDestination(new InvalidOperationException($"Interior destination is not an open registered building: {building.Value}."));
            return;
        }
        var area = _context.World.Interiors.AreaAt(building, position);
        if (area is null || !InteriorOpenPart.IsIndoor(area.Kind) || area.Index != space.Floor)
        {
            if (blocking) FailDestination(new InvalidOperationException($"Interior destination is outside the open area: {building.Value}/{space.Floor}."));
            return;
        }
        var atrium = _atriumStoreys.GetValueOrDefault(building);
        foreach (var storey in entry.OpenStoreys.Where(storey => Math.Abs(storey - space.Floor) <= 1 ||
                     atrium is not null && atrium.Contains(space.Floor) && atrium.Contains(storey)).Order())
        {
            var detail = storey == space.Floor ? "IF" : "IS";
            var key = new BakeKey(_context.Descriptor.Id, "interior." + entry.ProviderId,
                FormattableString.Invariant($"{building.Value}/{storey}/{detail}"));
            if (preload is not null) { preload(key); continue; }
            target?.Add(key);
            if (!_layers.ContainsKey(key)) _layers.Add(key, new(key, bake.Ensure(key, priority), mount.Revision, priority, building));
            else if (priority == BakePriority.KnownDestination) bake.Ensure(key, priority);
            if (blocking && _layers[key].Failure is { } error) Failure ??= error;
        }
    }
    public void Poll(int availablePreparations)
    {
        foreach (var layer in _layers.Values.OrderBy(layer => layer.Priority).ThenBy(layer => layer.Key.Detail, StringComparer.Ordinal))
        {
            if (layer.Preparing is not null || layer.Prepared is not null || layer.Failure is not null) continue;
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
            layer.Preparing = geometry.PrepareAsync(bundle);
            availablePreparations--;
        }
    }
    public void Finish(Rid scenario, bool allowVisibility)
    {
        foreach (var layer in _layers.Values)
        {
            if (layer.Preparing is { IsCompleted: true } task)
            {
                layer.Preparing = null;
                if (task.IsCompletedSuccessfully)
                {
                    layer.Prepared = task.Result;
                    if (_context is null || layer.Revision != mount.Revision)
                    { retire(layer.Prepared); layer.Prepared = null; layer.Finished = true; }
                    else
                    {
                        layer.Prepared = task.Result;
                        layer.Work = new VisualMountWork([task.Result], layer.Revision, scenario, layer.Priority, materials, reflections,
                            () => layer.Mounted = true, () => layer.Mounted = false, error => Fail(layer, error));
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
            if (!allowVisibility || layer.Prepared is null || !layer.Mounted) continue;
            var visible = _visible.Contains(layer.Key) && Ready(layer);
            if (layer.Visible == visible) continue;
            layer.Visible = visible;
            // Zero is the shader's fully visible sentinel, rather than a zero-duration interpolation.
            layer.Prepared.SetVisible(visible, visible && !captureMode ? FrameClock.FadeSeconds : 0);
        }
        for (var i = _draining.Count - 1; i >= 0; i--)
        {
            var layer = _draining[i];
            if (layer.Preparing is { IsCompleted: true } pending)
            {
                layer.Preparing = null;
                if (pending.IsCompletedSuccessfully)
                { layer.Prepared = pending.Result; retire(layer.Prepared); layer.Prepared = null; }
                else
                    try { pending.GetAwaiter().GetResult(); }
                    catch (OperationCanceledException) when (pending.IsCanceled) { }
                    catch (Exception error) { Fail(layer, error); }
            }
            if (layer.Preparing is null && layer.Handle.State is BakeState.Ready or BakeState.Failed)
            {
                if (layer.Handle.Failure is { } error and not OperationCanceledException) Fail(layer, error);
                _draining.RemoveAt(i);
            }
        }
        if (_context is null)
            foreach (var key in _layers.Where(pair => pair.Value.Finished ||
                         pair.Value.Preparing is null && pair.Value.Handle.State is BakeState.Ready or BakeState.Failed)
                     .Select(pair => pair.Key).ToArray()) _layers.Remove(key);
    }
    public void AdvanceRetention(double delta, System.Numerics.Vector3 camera)
    {
        if (_context is null) return;
        foreach (var layer in _layers.Values.OrderBy(layer => layer.Key.Detail, StringComparer.Ordinal).ToArray())
        {
            if (_visible.Contains(layer.Key) || _destination.Contains(layer.Key)) { layer.IdleSeconds = 0; continue; }
            layer.IdleSeconds += Math.Clamp(delta, 0, .1);
            var outside = false;
            if (_context.World.Features.TryGetValue(layer.Building, out var building))
            {
                var p = new System.Numerics.Vector2(camera.X, camera.Z);
                outside = building.Bounds.HorizontalDistanceSquared(new(p.X, 0, p.Y)) > 40 * 40;
            }
            if (!outside && layer.IdleSeconds < 20) continue;
            layer.Work?.Abandon();
            if (layer.Prepared is { } prepared) { retire(prepared); layer.Prepared = null; }
            _layers.Remove(layer.Key);
            if (layer.Preparing is not null || layer.Handle.State is BakeState.Pending or BakeState.Running) _draining.Add(layer);
        }
    }
    private bool Ready(Layer layer) => layer.Failure is null && layer.Mounted && layer.Prepared is not null;
    private void FailDestination(Exception error)
    {
        Failure = error;
        if (!_failures.Contains(error)) _failures.Add(error);
    }
    private void Fail(Layer layer, Exception error)
    {
        layer.Failure = error;
        if (_destination.Contains(layer.Key)) Failure ??= error;
        if (!_failures.Contains(error)) _failures.Add(error);
    }
    public void Unload()
    {
        _context = null; _destination.Clear(); _visible.Clear(); _atriumStoreys.Clear();
        foreach (var layer in _layers.Values)
        {
            try { layer.Work?.Abandon(); }
            catch (Exception error) { Fail(layer, error); }
            _draining.Add(layer);
        }
        _layers.Clear();
    }
    public async Task DrainAsync()
    {
        Unload();
        foreach (var layer in _draining)
        {
            try { await layer.Handle.Done; }
            catch (OperationCanceledException) when (layer.Handle.Done.IsCanceled) { }
            catch (Exception error) { Fail(layer, error); }
            if (layer.Preparing is { } task)
            {
                try { layer.Prepared = await task; }
                catch (OperationCanceledException) when (task.IsCanceled) { }
                catch (Exception error) { Fail(layer, error); }
                finally { layer.Preparing = null; }
            }
            try { layer.Prepared?.Dispose(); }
            catch (Exception error) { Fail(layer, error); }
            layer.Prepared = null; layer.Mounted = false;
        }
        _draining.Clear();
        if (_failures.Count == 1) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(_failures[0]).Throw();
        if (_failures.Count > 1) throw new AggregateException("Interior stream drain failed.", _failures);
    }
    private sealed class Layer(BakeKey key, IBakeHandle handle, long revision, BakePriority priority, FeatureId building)
    {
        public BakeKey Key { get; } = key;
        public IBakeHandle Handle { get; } = handle;
        public long Revision { get; } = revision;
        public BakePriority Priority { get; } = priority;
        public FeatureId Building { get; } = building;
        public double IdleSeconds;
        public VisualMountWork? Work;
        public Task<PreparedTile>? Preparing;
        public PreparedTile? Prepared;
        public Exception? Failure;
        public bool Mounted, Visible, Finished;
    }
}
