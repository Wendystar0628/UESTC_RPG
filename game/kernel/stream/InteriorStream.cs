using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Maps;
using Campus.Kernel.Frame;
using Campus.Kernel.Renderer;
using Godot;
using AppBoot = Campus.Kernel.Boot.Boot;

namespace Campus.Kernel.Stream;

/// <summary>Destination/current-floor IF and adjacent IS use the same bake and mount paths as outdoor tiles.</summary>
internal sealed class InteriorStream(IBakeService bake, IStreamMountQueue mount, StreamMaterials materials,
    IReflectionWorld reflections, Action<PreparedTile> retire)
{
    private readonly Dictionary<BakeKey, Layer> _layers = [];
    private readonly List<Layer> _draining = [];
    private readonly HashSet<BakeKey> _destination = [], _visible = [];
    private IMapContext? _context;
    private readonly Dictionary<FeatureId, IReadOnlyList<int>> _atriumStoreys = [];
    public string? Error { get; private set; }
    public int Preparations => _layers.Values.Concat(_draining).Count(layer => layer.Preparing is not null);
    public bool IsUnloaded => _context is null && _layers.Count == 0 && _draining.Count == 0;
    public IEnumerable<PreparedTile> Prepared => _layers.OrderBy(pair => pair.Key.Detail, StringComparer.Ordinal)
        .Select(pair => pair.Value.Prepared).OfType<PreparedTile>();
    public bool DestinationReady => Error is null && _destination.All(key => Ready(_layers[key]));
    public void Load(IMapContext context, BuildContext? generation = null)
    {
        _context = context; Error = null; _atriumStoreys.Clear();
        if (generation is null) return;
        foreach (var layout in generation.InteriorLayouts.Values)
            _atriumStoreys.Add(layout.Feature, layout.AtriumVisibleStoreys);
    }
    public void Prefetch(NavSpace? space)
    {
        _destination.Clear(); Error = null;
        if (space is { Building: not null } value) Request(value, BakePriority.KnownDestination, _destination, true);
    }
    public void Select(CameraMedium medium)
    {
        _visible.Clear();
        if (medium.Kind == MediumKind.Interior && medium.Feature is { } building)
            Request(new(building, checked((short)medium.Storey)), BakePriority.InteriorPrefetch, _visible, false);
    }
    public void Preload(NavSpace? space, Action<BakeKey> enqueue)
    {
        if (space is { Building: not null } value) Request(value, BakePriority.KnownDestination, null, false, enqueue);
    }
    private void Request(NavSpace space, BakePriority priority, HashSet<BakeKey>? target, bool blocking, Action<BakeKey>? preload = null)
    {
        if (_context is null || space.Building is not { } building) return;
        var entry = _context.World.Interiors.Get(building);
        if (entry is null || !entry.Open || string.IsNullOrWhiteSpace(entry.ProviderId))
        {
            if (blocking) Error = $"Interior destination is not an open registered building: {building.Value}.";
            return;
        }
        if (!entry.OpenStoreys.Contains(space.Floor))
        {
            if (blocking) Error = $"Interior destination storey is not open: {building.Value}/{space.Floor}.";
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
            if (blocking && _layers[key].Error is { } error) Error ??= error;
        }
    }
    public void Poll(int availablePreparations)
    {
        foreach (var layer in _layers.Values.OrderBy(layer => layer.Priority).ThenBy(layer => layer.Key.Detail, StringComparer.Ordinal))
        {
            if (layer.Preparing is not null || layer.Prepared is not null || layer.Error is not null) continue;
            if (layer.Handle.State is BakeState.Pending or BakeState.Running) continue;
            if (_context is null || layer.Revision != mount.Revision) { layer.Finished = true; continue; }
            if (layer.Handle.State == BakeState.Failed)
            {
                Fail(layer, layer.Handle.Error ?? $"Interior bake failed: {layer.Key}."); continue;
            }
            if (layer.Handle.State != BakeState.Ready) throw new InvalidDataException("Unknown interior bake state.");
            if (availablePreparations <= 0) continue;
            if (layer.Handle.Tile is not { } bundle || bundle.Key != layer.Key)
            {
                Fail(layer, $"Interior bake returned the wrong product: {layer.Key}."); continue;
            }
            layer.Preparing = Task.Factory.StartNew(() =>
            {
                Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;
                return PreparedTile.Create(bundle);
            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
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
                    if (_context is null || layer.Revision != mount.Revision) { retire(task.Result); layer.Finished = true; }
                    else
                    {
                        layer.Prepared = task.Result;
                        layer.Work = new VisualMountWork([task.Result], layer.Revision, scenario, layer.Priority, materials, reflections,
                            () => layer.Mounted = true, () => layer.Mounted = false, error => Fail(layer, error));
                        mount.Enqueue(layer.Work);
                    }
                }
                else { Fail(layer, task.Exception?.GetBaseException().Message ?? "Interior GPU preparation failed."); layer.Finished = true; }
            }
            if (!allowVisibility || layer.Prepared is null || !layer.Mounted) continue;
            var visible = _visible.Contains(layer.Key) && Ready(layer);
            if (layer.Visible == visible) continue;
            layer.Visible = visible;
            // Zero is the shader's fully visible sentinel, rather than a zero-duration interpolation.
            layer.Prepared.SetVisible(visible, visible && !AppBoot.Options.CaptureMode ? FrameClock.FadeSeconds : 0);
        }
        for (var i = _draining.Count - 1; i >= 0; i--)
        {
            var layer = _draining[i];
            if (layer.Preparing is { IsCompleted: true } pending)
            {
                layer.Preparing = null;
                if (pending.IsCompletedSuccessfully) retire(pending.Result);
                else _ = pending.Exception; // Observe the obsolete task; it cannot poison a new layer's state.
            }
            if (layer.Preparing is null && layer.Handle.State is BakeState.Ready or BakeState.Failed) _draining.RemoveAt(i);
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
                var nearest = System.Numerics.Vector2.Clamp(p, new(building.Bounds.Min.X, building.Bounds.Min.Z), new(building.Bounds.Max.X, building.Bounds.Max.Z));
                outside = System.Numerics.Vector2.DistanceSquared(p, nearest) > 40 * 40;
            }
            if (!outside && layer.IdleSeconds < 20) continue;
            layer.Work?.Abandon();
            if (layer.Prepared is { } prepared) { retire(prepared); layer.Prepared = null; }
            _layers.Remove(layer.Key);
            if (layer.Preparing is not null || layer.Handle.State is BakeState.Pending or BakeState.Running) _draining.Add(layer);
        }
    }
    private bool Ready(Layer layer) => layer.Error is null && layer.Mounted && layer.Prepared is { } prepared &&
        prepared.Draws.All(materials.IsReady);
    private void Fail(Layer layer, string error)
    {
        layer.Error = error;
        if (_destination.Contains(layer.Key)) Error ??= error;
        GD.PushError(error);
    }
    public void Unload()
    {
        _context = null; _destination.Clear(); _visible.Clear(); _atriumStoreys.Clear(); Error = null;
        foreach (var layer in _layers.Values)
        {
            layer.Work?.Abandon();
            layer.Prepared?.Dispose(); layer.Prepared = null; layer.Mounted = false;
            _draining.Add(layer);
        }
        _layers.Clear();
    }
    public void Shutdown()
    {
        Unload();
        foreach (var layer in _layers.Values.Concat(_draining))
            if (layer.Preparing is { } task)
                try { task.GetAwaiter().GetResult().Dispose(); } catch (Exception error) { GD.PushError(error.Message); }
        _layers.Clear(); _draining.Clear();
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
        public string? Error;
        public bool Mounted, Visible, Finished;
    }
}
