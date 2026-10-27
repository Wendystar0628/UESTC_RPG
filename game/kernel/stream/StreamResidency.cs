using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Kernel.Diagnostics;
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
    private readonly HashSet<StreamTile> _wanted = [];
    private readonly List<StreamTile> _tileOrder = [];
    private StreamMaterials _materials = null!;
    private IStreamReleaseWork? _materialRetirement;
    private readonly Queue<PreparedTile> _reflectionRefresh = [];
    private readonly Queue<PreparedTile> _retiringBundles = [];
    private readonly Queue<PreparedTile> _detachPending = [];
    private int _reflectionCursor;
    private bool _reflectionDirty = true;
    private Rid _reflectionScenario;
    private Material? _reflectionMaterial;
    internal IReadOnlyDictionary<StreamTile, TileGroup> Groups => _groups;
    internal StreamMaterials Materials => _materials;
    internal bool IsIdle => _groups.Count == 0 && _retiringBundles.Count == 0 && _detachPending.Count == 0 && (_materials?.IsEmpty ?? true);
    internal bool CanReleaseRetiredInstances => _detachPending.Count == 0;
    internal bool CanActivateVisual
    {
        get
        {
            if (_retiringBundles.Count != 0) return false;
            foreach (var group in _groups.Values) if (group.RetiringGeometry) return false;
            return true;
        }
    }
    internal int Retirements
    {
        get
        {
            if (_host.Context is not null) return 0;
            var count = 0;
            foreach (var group in _groups.Values) if (!group.RetiringGeometry) count++;
            return count;
        }
    }
    internal int MountedTiles
    {
        get
        {
            var count = 0;
            foreach (var group in _groups.Values) if (group.Mounted) count++;
            return count;
        }
    }
    internal long ResidentBytes
    {
        get
        {
            var bytes = 0L;
            foreach (var group in _groups.Values)
                foreach (var bundle in group.Bundles) bytes += bundle.Bytes;
            return bytes;
        }
    }
    internal bool RootMounted => _groups.TryGetValue(StreamTile.Root, out var root) && root.Failure is null && !root.RetiringGeometry && root.Mounted;

    internal TileGroup GetGroup(StreamTile tile)
    {
        if (!_groups.TryGetValue(tile, out var group)) _groups.Add(tile, group = new(tile));
        return group;
    }

    internal void SetMounted(TileGroup group, long revision)
    {
        group.Mounted = true;
        foreach (var bundle in group.Bundles)
            _host.RecordTileChange(new(bundle.Key, StreamMountKind.Visual, revision,
                StreamTileChangeKind.Mounted, bundle.Bytes, FrameClock.MonotonicMilliseconds));
    }

    internal void ClearMounted(TileGroup group, long revision)
    {
        if (!group.Mounted) return;
        group.Mounted = false;
        foreach (var bundle in group.Bundles)
            _host.RecordTileChange(new(bundle.Key, StreamMountKind.Visual, revision,
                StreamTileChangeKind.Unmounted, bundle.Bytes, FrameClock.MonotonicMilliseconds));
    }

    internal bool IsReady(StreamTile tile) => _groups.TryGetValue(tile, out var group) && group.GeometryRequested && group.Failure is null && group.Mounted;

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
        _wanted.Clear();
        _wanted.UnionWith(_host.Selection.Required); _wanted.UnionWith(_host.Selection.DestinationRequired); _wanted.UnionWith(_host.Pins.Required);
        _tileOrder.Clear();
        foreach (var tile in _wanted) _tileOrder.Add(tile);
        _tileOrder.Sort();
        foreach (var tile in _tileOrder)
        {
            var group = GetGroup(tile);
            if (group.RetiringGeometry) continue;
            if (!group.GeometryRequested)
            {
                group.GeometryRequested = true; group.Expected = _host.Preparation.Catalog!.Modules(tile).Count;
                group.Mounted = group.Expected == 0; // Empty quadrants are valid geometry coverage.
                foreach (var module in _host.Preparation.Catalog.Modules(tile))
                {
                    var key = BakeRequestKeys.Tile(_host.Context!.Descriptor.Id, module, tile.Tile, tile.Level);
                    // Catalog modules are ordinally ordered; keep these identities for this geometry lifetime.
                    group.MountKey ??= key;
                    group.Keys.Add(key);
                }
            }
            var priority = tile == StreamTile.Root || _host.Selection.DestinationRequired.Contains(tile) || _host.Pins.Required.Contains(tile) ? BakePriority.KnownDestination :
                _host.Selection.Required.Contains(tile) ? BakePriority.Normal : BakePriority.Background;
            group.Priority = priority;
            if (group.Work is { } work) work.Priority = priority;
            var distance = MathF.Sqrt(_host.Selection.Tree!.Bounds(tile).DistanceSquared(pose.Position));
            var toTile = _host.Selection.Tree!.Bounds(tile).Center - pose.Position;
            var approach = toTile.LengthSquared() > 0 ? N.Vector3.Dot(_host.Selection.Velocity, N.Vector3.Normalize(toTile)) : 0;
            var radius = _host.Selection.Settings.Distances[Math.Min((int)tile.Level, 3)];
            var neededIn = (distance - radius) / Math.Max(1, approach);
            foreach (var key in group.Keys)
            {
                if (!_host.Preparation.IsIssued(key)) requests.Add(new(key, tile, priority, neededIn));
                // Re-Ensure promotes an already issued background request without restarting it.
                else
                {
                    _host.Bake.Ensure(key, priority);
                }
            }
        }
    }

    internal void RetireUnneeded()
    {
        _host.Participants.RetireParticipants();
        _tileOrder.Clear();
        foreach (var tile in _groups.Keys)
            if (tile != StreamTile.Root && !_host.Selection.Required.Contains(tile) && !_host.Selection.DestinationRequired.Contains(tile) &&
                !_host.Pins.Required.Contains(tile) && !_host.Selection.Visibility.Drawn.Contains(tile)) _tileOrder.Add(tile);
        _tileOrder.Sort();
        foreach (var tile in _tileOrder)
        {
            var group = _groups[tile];
            if (!group.GeometryRequested && group.Participants.Count > 0) continue;
            RetireGeometry(group);
        }
    }

    private void RetireGeometry(TileGroup group)
    {
        if (group.RetiringGeometry) return;
        group.RetiringGeometry = true;
        group.GeometryRequested = false;
        if (group.Work is { } work) _host.Mount.Retire(work);
        else foreach (var bundle in group.Bundles) QueueRetirement(bundle);
    }

    private void HarvestRetiredGroups()
    {
        foreach (var group in _groups.Values)
        {
            if (!group.RetiringGeometry || HasUnreleasedBundle(group)) continue;
            ClearMounted(group, group.Work?.Revision ?? _host.Mount.Revision);
            foreach (var key in group.Keys) _host.Preparation.Forget(key);
            group.Bundles.Clear(); group.Keys.Clear(); group.Work = null; group.MountKey = null;
            group.Expected = 0; group.Queued = group.Mounted = group.RetiringGeometry = false;
            group.GeometryRevision++; group.Failure = null;
        }
        _tileOrder.Clear();
        foreach (var (tile, group) in _groups)
            if (!group.GeometryRequested && !group.RetiringGeometry && group.Bundles.Count == 0 && group.Participants.Count == 0) _tileOrder.Add(tile);
        foreach (var tile in _tileOrder) _groups.Remove(tile);
    }

    private static bool HasUnreleasedBundle(TileGroup group)
    {
        foreach (var bundle in group.Bundles) if (!bundle.IsDisposed) return true;
        return false;
    }

    internal void QueueRetirement(PreparedTile bundle)
    {
        QueueDetach(bundle);
        _retiringBundles.Enqueue(bundle);
        _host.Mount.Retire(new PreparedRetirement(bundle, this));
        _materials.Forget(bundle.Draws);
    }

    internal void QueueDetach(PreparedTile bundle)
    {
        bundle.BeginRetirement();
        if (!_detachPending.Contains(bundle)) _detachPending.Enqueue(bundle);
    }

    internal void ReleaseRetired(double budgetMilliseconds)
    {
        var start = FrameClock.MonotonicMilliseconds;
        if (_detachPending.TryPeek(out var pending))
            do
            {
                if (pending.DetachOne()) _detachPending.Dequeue();
                if (FrameClock.MonotonicMilliseconds - start >= budgetMilliseconds) break;
            } while (_detachPending.TryPeek(out pending));
        _host.Participants.HarvestRetired();
        HarvestRetiredGroups();
        var count = _retiringBundles.Count;
        for (var index = 0; index < count; index++)
        {
            var bundle = _retiringBundles.Dequeue();
            if (!bundle.IsDisposed) _retiringBundles.Enqueue(bundle);
        }
        if (_host.Context is null) Unload();
    }

    private sealed class PreparedRetirement(PreparedTile bundle, StreamResidency residency) : IStreamReleaseWork
    {
        public bool ReleaseStep(double budgetMilliseconds)
        {
            try
            {
                var start = FrameClock.MonotonicMilliseconds;
                if (!residency.CanReleaseRetiredInstances) return false;
                do { if (bundle.ReleaseOne()) return true; }
                while (FrameClock.MonotonicMilliseconds - start < budgetMilliseconds);
                return false;
            }
            catch (Exception error)
            {
                try { residency._host.RecordFailure(error); }
                catch (Exception reporting) { throw RetainedFailure.Append(error, reporting); }
                throw;
            }
        }
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
            _tileOrder.Clear();
            foreach (var tile in _groups.Keys) _tileOrder.Add(tile);
            _tileOrder.Sort();
            foreach (var tile in _tileOrder)
                foreach (var bundle in _groups[tile].Bundles) _reflectionRefresh.Enqueue(bundle);
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
    internal bool IsCurrent(Pending pending) => IsCurrent(pending.Request.Tile, pending.Group, pending.Revision) &&
        (pending.Participant is { } participant
            ? pending.Group.Participants.TryGetValue(pending.Request.Key, out var current) && ReferenceEquals(current, participant)
            : pending.Group.GeometryRequested && pending.Group.GeometryRevision == pending.GeometryRevision);

    internal void InitializeMaterials(IToonMaterialProvider provider, IShaderMaterialSource shaders) => _materials = new(provider, shaders);
    internal void Load() { _reflectionDirty = true; _materialRetirement = null; }
    internal void DetachMap()
    {
        foreach (var group in _groups.Values)
            try { RetireGeometry(group); }
            catch (Exception error) { _host.RecordFailure(error); }
    }
    internal void Unload()
    {
        DetachMap();
        _reflectionRefresh.Clear(); _reflectionCursor = 0; _reflectionMaterial = null; _reflectionScenario = default;
        // Bindings remain borrowed until every queued owner, including late preparation, is empty.
        if (_groups.Count == 0 && _retiringBundles.Count == 0 && _host.Preparation.IsIdle && (_host.Interiors?.IsUnloaded ?? true))
            if (_materials is { IsEmpty: false } && _materialRetirement is null)
            {
                _materialRetirement = new MaterialRetirement(_materials, _host.RecordFailure);
                _host.Mount.Retire(_materialRetirement);
            }
    }
    private sealed class MaterialRetirement(StreamMaterials materials, Action<Exception> failed) : IStreamReleaseWork
    {
        public bool ReleaseStep(double budgetMilliseconds)
        {
            try
            {
                var start = FrameClock.MonotonicMilliseconds;
                do { if (materials.ReleaseOne()) return true; }
                while (FrameClock.MonotonicMilliseconds - start < budgetMilliseconds);
                return false;
            }
            catch (Exception error)
            {
                try { failed(error); }
                catch (Exception reporting) { throw RetainedFailure.Append(error, reporting); }
                throw;
            }
        }
    }
    internal void RequireMaterialsDrained()
    {
        if (_materials is { IsEmpty: false }) throw new InvalidOperationException("Stream material bindings must drain before exit.");
    }
    internal sealed class TileGroup(StreamTile tile)
    {
        public StreamTile Tile { get; } = tile;
        public int Expected;
        public bool GeometryRequested;
        public long GeometryRevision;
        public Dictionary<BakeKey, ParticipantTile> Participants { get; } = [];
        public List<PreparedTile> Bundles { get; } = [];
        public List<BakeKey> Keys { get; } = [];
        public BakeKey? MountKey;
        public VisualMountWork? Work;
        public bool Queued, Mounted, RetiringGeometry;
        public BakePriority Priority;
        public Exception? Failure;
    }

}
