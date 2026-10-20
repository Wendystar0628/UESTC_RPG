using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Kernel.Frame;
using Campus.Kernel.Lod;
using Campus.Kernel.Renderer;
using Godot;
using N = System.Numerics;
using ParticipantTile = Campus.Kernel.Stream.StreamParticipants.ParticipantTile;
using Pending = Campus.Kernel.Stream.StreamPreparation.Pending;

namespace Campus.Kernel.Stream;

internal sealed class StreamResidency(StreamDirector host)
{
    private readonly StreamDirector _host = host;
    private readonly Dictionary<StreamTile, TileGroup> _groups = [];
    private StreamMaterials _materials = null!;
    private readonly Queue<PreparedTile> _reflectionRefresh = [];
    private readonly Queue<PreparedTile> _retiringBundles = [];
    private readonly Queue<PreparedTile> _detachPending = [];
    private int _reflectionCursor;
    private bool _reflectionDirty = true;
    private Rid _reflectionScenario;
    private Material? _reflectionMaterial;
    internal IReadOnlyDictionary<StreamTile, TileGroup> Groups => _groups;
    internal StreamMaterials Materials => _materials;
    internal bool IsIdle => _retiringBundles.Count == 0;
    internal bool CanActivateVisual => _retiringBundles.Count == 0 && _detachPending.Count == 0;
    internal int MountedTiles => _groups.Values.Count(group => group.Mounted);
    internal long ResidentBytes => _groups.Values.Sum(group => group.Bundles.Sum(bundle => bundle.Bytes));
    internal bool RootMounted => _groups.TryGetValue(StreamTile.Root, out var root) && root.Error is null && root.Mounted;

    internal TileGroup GetGroup(StreamTile tile)
    {
        if (!_groups.TryGetValue(tile, out var group)) _groups.Add(tile, group = new(tile));
        return group;
    }

    internal bool IsReady(StreamTile tile) => _groups.TryGetValue(tile, out var group) && group.GeometryRequested && group.Error is null && group.Mounted;

    internal bool IsCurrentGeometry(StreamTile tile, TileGroup group, long revision, long geometryRevision) =>
        IsCurrent(tile, group, revision) && group.GeometryRequested && group.GeometryRevision == geometryRevision;

    internal bool HasGeometryCoverage(StreamTile tile)
    {
        // Auxiliary influence may reach the map from an owner outside the geometry quadtree.
        if (_host.Selection.Tree is null || !_host.Selection.Tree.Contains(tile)) return true;
        while (true)
        {
            if (_host.Selection.Visibility.Drawn.Contains(tile) && IsReady(tile)) return true;
            if (tile == StreamTile.Root) return false;
            tile = tile.Parent;
        }
    }

    internal void QueueGeometryRequests(List<StreamRequest> requests, Pose pose)
    {
        var wanted = _host.Selection.Required.Concat(_host.Selection.DestinationRequired).Distinct().Order().ToArray();
        foreach (var tile in wanted)
        {
            var group = GetGroup(tile);
            if (!group.GeometryRequested)
            {
                group.GeometryRequested = true; group.Expected = _host.Preparation.Catalog!.Modules(tile).Count;
                group.Mounted = group.Expected == 0; // Empty quadrants are valid geometry coverage.
            }
            var priority = tile == StreamTile.Root || _host.Selection.DestinationRequired.Contains(tile) ? BakePriority.KnownDestination : BakePriority.Normal;
            if (group.Error is not null && priority == BakePriority.KnownDestination) _host.RecordError(group.Error);
            group.Priority = priority;
            var distance = MathF.Sqrt(_host.Selection.Tree!.Bounds(tile).DistanceSquared(pose.Position));
            var toTile = _host.Selection.Tree!.Bounds(tile).Center - pose.Position;
            var approach = toTile.LengthSquared() > 0 ? N.Vector3.Dot(_host.Selection.Velocity, N.Vector3.Normalize(toTile)) : 0;
            var radius = _host.Selection.Settings.Distances[Math.Min((int)tile.Level, 3)];
            var neededIn = (distance - radius) / Math.Max(1, approach);
            foreach (var module in _host.Preparation.Catalog!.Modules(tile))
            {
                var key = BakeRequestKeys.Tile(_host.Context!.Descriptor.Id, module, tile.Tile, tile.Level);
                group.Keys.Add(key);
                if (!_host.Preparation.IsIssued(key)) requests.Add(new(key, tile, priority, neededIn));
                // Re-Ensure promotes an already issued background request without restarting it.
                else
                {
                    _host.Preparation.PromotePreload(new(key, tile, priority, neededIn), group, null);
                    if (priority == BakePriority.KnownDestination) _host.Bake.Ensure(key, priority);
                }
            }
        }
    }

    internal void RetireUnneeded()
    {
        _host.Participants.RetireParticipants();
        foreach (var tile in _groups.Keys.Where(tile => tile != StreamTile.Root && !_host.Selection.Required.Contains(tile) &&
                     !_host.Selection.DestinationRequired.Contains(tile) && !_host.Selection.Visibility.Drawn.Contains(tile)).Order().ToArray())
        {
            var group = _groups[tile];
            if (!group.GeometryRequested && group.Participants.Count > 0) continue;
            group.Work?.Abandon(); group.Work = null;
            foreach (var bundle in group.Bundles) QueueRetirement(bundle);
            foreach (var key in group.Keys) _host.Preparation.Forget(key);
            group.Bundles.Clear(); group.Keys.Clear(); group.Expected = 0;
            group.Queued = group.Mounted = group.GeometryRequested = false; group.GeometryRevision++;
            group.Error = null;
            if (group.Participants.Count == 0) _groups.Remove(tile);
        }
    }

    internal void QueueRetirement(PreparedTile bundle)
    {
        _materials.Forget(bundle.Draws); bundle.BeginRetirement();
        _retiringBundles.Enqueue(bundle); _detachPending.Enqueue(bundle);
    }

    internal void ReleaseRetired(double budgetMilliseconds)
    {
        var start = FrameClock.MonotonicMilliseconds;
        while (_detachPending.TryPeek(out var pending) && FrameClock.MonotonicMilliseconds - start < budgetMilliseconds)
            if (pending.DetachOne()) _detachPending.Dequeue();
        if (_detachPending.Count > 0) return;
        while (_retiringBundles.TryPeek(out var bundle) && FrameClock.MonotonicMilliseconds - start < budgetMilliseconds)
            if (bundle.ReleaseOne()) _retiringBundles.Dequeue();
    }

    internal void OnReflectionChanged() => _reflectionDirty = true;

    internal void RefreshReflectionWorld(double budgetMilliseconds)
    {
        var scenario = _host.Reflections.Scenario;
        var material = scenario.IsValid ? _host.Reflections.ProxyMaterial : null;
        if (_reflectionDirty || scenario != _reflectionScenario || !ReferenceEquals(material, _reflectionMaterial))
        {
            _reflectionDirty = false; _reflectionScenario = scenario; _reflectionMaterial = material;
            _reflectionRefresh.Clear(); _reflectionCursor = 0;
            foreach (var group in _groups.OrderBy(pair => pair.Key).Select(pair => pair.Value))
                foreach (var bundle in group.Bundles) _reflectionRefresh.Enqueue(bundle);
            if (_host.Interiors is not null) foreach (var bundle in _host.Interiors.Prepared) _reflectionRefresh.Enqueue(bundle);
        }
        var start = FrameClock.MonotonicMilliseconds;
        while (_reflectionRefresh.TryPeek(out var bundle) && FrameClock.MonotonicMilliseconds - start < budgetMilliseconds)
        {
            if (bundle.IsDisposed || bundle.IsRetiring || _reflectionCursor >= bundle.Instances.Count)
            {
                _reflectionRefresh.Dequeue(); _reflectionCursor = 0; continue;
            }
            var index = _reflectionCursor++;
            bundle.RefreshReflectionOne(index, scenario, material);
        }
    }

    internal bool IsCurrent(StreamTile tile, TileGroup group, long revision) => _host.Context is not null && revision == _host.Mount.Revision &&
        _groups.TryGetValue(tile, out var current) && ReferenceEquals(current, group);
    internal bool IsCurrent(Pending pending) => pending.Group is { } group && IsCurrent(pending.Request.Tile, group, pending.Revision) &&
        (pending.Participant is { } participant
            ? group.Participants.TryGetValue(pending.Request.Key, out var current) && ReferenceEquals(current, participant)
            : group.GeometryRequested && group.GeometryRevision == pending.GeometryRevision);

    internal void InitializeMaterials(IToonMaterialProvider provider, IShaderMaterialSource shaders) => _materials = new(provider, shaders);
    internal void Load() => _reflectionDirty = true;
    internal void DetachMap()
    {
        // Detach the entire retiring scene before FreeRid flushes dirty instances.
        foreach (var bundle in _groups.Values.SelectMany(group => group.Bundles)
                     .Concat(_host.Interiors?.Prepared ?? []).Concat(_retiringBundles).Distinct()) bundle.DetachAll();
    }
    internal void Unload()
    {
        foreach (var group in _groups.Values) foreach (var bundle in group.Bundles) bundle.Dispose();
        _groups.Clear();
        while (_retiringBundles.TryDequeue(out var retired)) retired.Dispose();
        _detachPending.Clear();
        _reflectionRefresh.Clear(); _reflectionCursor = 0; _reflectionMaterial = null; _reflectionScenario = default;
        _materials.Dispose();
    }
    internal void Shutdown()
    {
        foreach (var retiringBundle in _retiringBundles) retiringBundle.DetachAll();
        while (_retiringBundles.TryDequeue(out var retired)) retired.Dispose();
        _detachPending.Clear();
    }
    internal void DisposeMaterials() => _materials?.Dispose();
    internal sealed class TileGroup(StreamTile tile)
    {
        public StreamTile Tile { get; } = tile;
        public int Expected;
        public bool GeometryRequested;
        public long GeometryRevision;
        public Dictionary<BakeKey, ParticipantTile> Participants { get; } = [];
        public List<PreparedTile> Bundles { get; } = [];
        public HashSet<BakeKey> Keys { get; } = [];
        public VisualMountWork? Work;
        public bool Queued, Mounted;
        public BakePriority Priority;
        public string? Error;
    }

}
