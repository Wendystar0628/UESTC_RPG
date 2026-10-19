using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Kernel.Boot;
using Campus.Kernel.Lod;
using Godot;
using Services = Campus.Kernel.Registry.Kernel;
using N = System.Numerics;

namespace Campus.Kernel.Stream;

public partial class StreamDirector
{
    private IStreamTileParticipant[] _participants = [];
    private readonly N.Plane[] _demandFrustum = new N.Plane[6];
    private Box3[] _demandNearBuffer = [];
    private readonly List<BakeKey> _retiredAuxiliary = [];

    private void LoadParticipants(MapId map) => _participants = Services.Services.ForMap(map.Value)
        .Where(entry => (entry.Kind is "node" or "service" or "save") && typeof(IStreamTileParticipant).IsAssignableFrom(entry.Type))
        .GroupBy(entry => entry.Type)
        .Select(group => group.OrderBy(entry => entry.Kind == "node" ? 0 : 1).ThenBy(entry => entry.Id, StringComparer.Ordinal).First())
        .OrderBy(entry => entry.Id, StringComparer.Ordinal)
        .Select(entry => (IStreamTileParticipant)RuntimeComposition.Resolve(Services.Services, entry)).ToArray();

    private IStreamTileParticipant Participant(BakeKey key)
    {
        IStreamTileParticipant? result = null;
        foreach (var participant in _participants)
        {
            if (!participant.CanMount(key)) continue;
            if (result is not null) throw new InvalidOperationException($"Multiple stream participants own {key}.");
            result = participant;
        }
        return result ?? throw new InvalidOperationException($"No stream participant owns {key}.");
    }

    private void ValidateAuxiliary(ModuleTileCatalog catalog)
    {
        foreach (var auxiliary in catalog.Auxiliary)
        {
            var entry = auxiliary.Content;
            if (entry.Key.Map != _context!.Descriptor.Id)
                throw new InvalidDataException($"Invalid auxiliary directory key: {entry.Key}.");
            _ = Participant(entry.Key);
        }
    }

    private TileGroup GetGroup(StreamTile tile)
    {
        if (!_groups.TryGetValue(tile, out var group)) _groups.Add(tile, group = new(tile));
        return group;
    }

    private void ResetAuxiliaryDemand()
    {
        foreach (var group in _groups.Values)
            foreach (var item in group.Auxiliary.Values)
            { item.Need = StreamTileNeed.None; item.CurrentRequired = item.DestinationRequired = false; }
    }

    private void SelectAuxiliary(Pose pose, HashSet<StreamTile> required, bool destination, bool live, List<StreamRequest>? preload = null)
    {
        var camera = GetViewport().GetCamera3D()
            ?? throw new InvalidOperationException("Stream demand requires the existing view camera.");
        var projection = camera.GetCameraProjection();
        var transform = camera.GetCameraTransform();
        if (!live)
        {
            var basis = Basis.FromEuler(new(Mathf.DegToRad(pose.PitchDeg), -Mathf.DegToRad(pose.YawDeg), 0));
            var eye = new Vector3(pose.Position.X, pose.Position.Y, pose.Position.Z);
            transform = new(basis, eye + basis.X * camera.HOffset + basis.Y * camera.VOffset);
            if (camera.Projection == Camera3D.ProjectionType.Perspective && pose.FovDeg is { } fov)
                projection = Projection.CreatePerspective(fov, projection.GetAspect(), camera.Near, camera.Far,
                    camera.KeepAspect == Camera3D.KeepAspectEnum.Width);
        }
        for (var i = 0; i < _demandFrustum.Length; i++)
        {
            var plane = transform * projection.GetProjectionPlane((Projection.Planes)i);
            _demandFrustum[i] = new(new(plane.Normal.X, plane.Normal.Y, plane.Normal.Z), -plane.D);
        }
        if (_demandNearBuffer.Length < required.Count) Array.Resize(ref _demandNearBuffer, required.Count);
        var nearCount = 0;
        foreach (var tile in required)
            if (tile.Level == LodLevel.L0) _demandNearBuffer[nearCount++] = _tree!.Bounds(tile);
        var view = new StreamDemandView(pose, _demandFrustum, _demandNearBuffer.AsMemory(0, nearCount));
        ApplyAuxiliaryDemand(in view, destination, preload);
        // The real reflection bridge reuses its own scratch; consume it before querying the next source view.
        if (_reflections.TryGetDemand(in view, out var reflected)) ApplyAuxiliaryDemand(in reflected, destination, preload);
    }

    private void ApplyAuxiliaryDemand(in StreamDemandView view, bool destination, List<StreamRequest>? preload)
    {
        foreach (var auxiliary in _catalog!.Auxiliary)
        {
            var entry = auxiliary.Content;
            var tile = auxiliary.Tile;
            AuxiliaryTile? item = null;
            if (_groups.TryGetValue(tile, out var existing)) existing.Auxiliary.TryGetValue(entry.Key, out item);
            var participant = item?.Participant ?? Participant(entry.Key);
            var need = participant.Demand(in entry, in view, item is { Mounted: true });
            if (preload is not null)
            {
                switch (need)
                {
                    case StreamTileNeed.None:
                    case StreamTileNeed.Retain: break;
                    case StreamTileNeed.Required: QueueCpuRead(entry.Key, tile, preload); break;
                    default: throw new ArgumentOutOfRangeException(nameof(need));
                }
                continue;
            }
            switch (need)
            {
                case StreamTileNeed.None: continue;
                case StreamTileNeed.Retain:
                    if (item is not { Mounted: true }) continue;
                    break;
                case StreamTileNeed.Required:
                    if (item is null)
                    {
                        item = new(entry, participant);
                        GetGroup(tile).Auxiliary.Add(entry.Key, item);
                    }
                    if (destination) item.DestinationRequired = true;
                    else item.CurrentRequired = true;
                    break;
                default: throw new ArgumentOutOfRangeException(nameof(need));
            }
            if ((byte)need > (byte)item.Need) item.Need = need;
        }
    }

    private void QueueAuxiliary(List<StreamRequest> requests, Pose pose)
    {
        foreach (var group in _groups.Values)
            foreach (var (key, item) in group.Auxiliary)
            {
                if (item.Need != StreamTileNeed.Required) continue;
                var priority = item.DestinationRequired ? BakePriority.KnownDestination : BakePriority.Normal;
                if (item.Error is not null && item.DestinationRequired) _error ??= item.Error;
                if (!_issued.Contains(key))
                    requests.Add(new(key, group.Tile, priority, LodTree.Distance(pose.Position, item.Entry.Bounds) / Math.Max(1, _camera.Speed)));
                else
                {
                    PromotePreload(new(key, group.Tile, priority, 0), group, item);
                    if (priority == BakePriority.KnownDestination) _bake.Ensure(key, priority);
                }
            }
    }

    private bool IsCurrent(Pending pending) => pending.Group is { } group && IsCurrent(pending.Request.Tile, group, pending.Revision) &&
        (pending.Auxiliary is { } auxiliary
            ? group.Auxiliary.TryGetValue(pending.Request.Key, out var current) && ReferenceEquals(current, auxiliary)
            : group.GeometryRequested && group.GeometryRevision == pending.GeometryRevision);

    private bool IsCurrentGeometry(StreamTile tile, TileGroup group, long revision, long geometryRevision) =>
        IsCurrent(tile, group, revision) && group.GeometryRequested && group.GeometryRevision == geometryRevision;

    private void MountAuxiliary(Pending pending, AuxiliaryTile item)
    {
        try
        {
            var product = pending.Handle.Product ?? throw new InvalidDataException($"Bake returned no product for {pending.Request.Key}.");
            var priority = item.DestinationRequired ? BakePriority.KnownDestination : pending.Request.Priority;
            var work = item.Participant.CreateMount(pending.Request.Key, product, priority, pending.Revision);
            if (work.Key != pending.Request.Key || work.Revision != pending.Revision || work.Kind != StreamMountKind.Auxiliary)
            {
                work.Cancel();
                throw new InvalidDataException($"Auxiliary mount identity mismatch: {pending.Request.Key}.");
            }
            item.Work = new(work, () => IsCurrent(pending), () => item.Mounted = true,
                error => FailAuxiliary(pending.Group!, item, error));
            _mount.Enqueue(item.Work);
        }
        catch (Exception error) { FailAuxiliary(pending.Group!, item, error.GetBaseException().Message); }
    }

    private bool AuxiliaryReady(bool destination)
    {
        foreach (var group in _groups.Values)
            foreach (var (key, item) in group.Auxiliary)
                if ((destination ? item.DestinationRequired : item.CurrentRequired) &&
                    (item.Error is not null || !item.Mounted || !item.Participant.IsReady(key))) return false;
        return true;
    }

    private void PollAuxiliaryErrors()
    {
        foreach (var group in _groups.Values)
            foreach (var item in group.Auxiliary.Values)
                if (item.Need == StreamTileNeed.Required && item.Error is null && item.Participant.Error is { } error)
                    FailAuxiliary(group, item, error);
    }

    private void FailAuxiliary(TileGroup group, AuxiliaryTile item, string error)
    {
        Fail(group.Tile, error, item);
    }

    private void RetireAuxiliary()
    {
        foreach (var group in _groups.Values)
        {
            _retiredAuxiliary.Clear();
            foreach (var (key, item) in group.Auxiliary)
            {
                if (item.Need != StreamTileNeed.None) continue;
                // Keep mounted near content until geometry covering its owner is actually drawn.
                if (item.Mounted && !HasGeometryCoverage(group.Tile)) continue;
                ReleaseAuxiliary(key, item); _issued.Remove(key); _retiredAuxiliary.Add(key);
            }
            foreach (var key in _retiredAuxiliary) group.Auxiliary.Remove(key);
        }
        _retiredAuxiliary.Clear();
    }

    private bool HasGeometryCoverage(StreamTile tile)
    {
        // Auxiliary influence may reach the map from an owner outside the geometry quadtree.
        if (_tree is null || !_tree.Contains(tile)) return true;
        while (true)
        {
            if (_visibility.Drawn.Contains(tile) && IsReady(tile)) return true;
            if (tile == StreamTile.Root) return false;
            tile = tile.Parent;
        }
    }

    private static void ReleaseAuxiliary(BakeKey key, AuxiliaryTile item)
    {
        if (item.Mounted) item.Participant.Unmount(key);
        else item.Work?.Cancel();
        item.Mounted = false;
    }

    private void UnloadAuxiliary()
    {
        foreach (var group in _groups.Values)
            foreach (var (key, item) in group.Auxiliary) ReleaseAuxiliary(key, item);
        _participants = [];
    }

    private sealed class AuxiliaryTile(BakeTileEntry entry, IStreamTileParticipant participant)
    {
        public BakeTileEntry Entry { get; } = entry;
        public IStreamTileParticipant Participant { get; } = participant;
        public AuxiliaryMountWork? Work;
        public StreamTileNeed Need;
        public bool CurrentRequired, DestinationRequired, Mounted;
        public string? Error;
    }
}
