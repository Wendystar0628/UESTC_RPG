using Campus.Kernel.Renderer;
using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Kernel.Collision;
using Campus.Kernel.Lod;
using Godot;
using KernelServices = Campus.Kernel.Registry.Kernel;
using N = System.Numerics;

namespace Campus.Kernel.Stream;

public partial class StreamDirector
{
    private IStreamTileParticipant[] _participants = [];
    private ParticipantEntry[] _participantEntries = [];
    private readonly N.Plane[] _demandFrustum = new N.Plane[6];
    private Box3[] _demandNearBuffer = [];
    private readonly List<BakeKey> _retiredParticipants = [];

    private void LoadParticipants(MapId map) => _participants = KernelServices.Services.RuntimeInstances<IStreamTileParticipant>(map).ToArray();

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

    private void LoadParticipantEntries(ModuleTileCatalog catalog)
    {
        var entries = new List<ParticipantEntry>(catalog.Auxiliary.Count);
        foreach (var auxiliary in catalog.Auxiliary)
        {
            var entry = auxiliary.Content;
            if (entry.Key.Map != _context!.Descriptor.Id)
                throw new InvalidDataException($"Invalid auxiliary directory key: {entry.Key}.");
            entries.Add(new(auxiliary.Tile, entry, Participant(entry.Key)));
        }
        var map = _context!.Descriptor;
        for (var x = map.TileMin.X; x <= map.TileMax.X; x++)
            for (var z = map.TileMin.Z; z <= map.TileMax.Z; z++)
            {
                var tile = new TileKey(x, z);
                var origin = TileGrid.Origin(tile);
                var bounds = new Box3(new(origin.X, map.Bounds.Min.Y, origin.Y),
                    new(origin.X + TileGrid.BaseEdge, map.Bounds.Max.Y, origin.Y + TileGrid.BaseEdge));
                var entry = new BakeTileEntry(CollisionBakeProducer.Key(map.Id, tile), bounds);
                entries.Add(new(new(tile, LodLevel.L0), entry, Participant(entry.Key)));
            }
        _participantEntries = entries.ToArray();
    }

    private TileGroup GetGroup(StreamTile tile)
    {
        if (!_groups.TryGetValue(tile, out var group)) _groups.Add(tile, group = new(tile));
        return group;
    }

    private void ResetParticipantDemand()
    {
        foreach (var group in _groups.Values)
            foreach (var item in group.Participants.Values)
            { item.Need = StreamTileNeed.None; item.CurrentRequired = item.DestinationRequired = false; }
    }

    private void SelectParticipants(Pose pose, HashSet<StreamTile> required, bool destination, bool live,
        List<StreamRequest>? preload = null, N.Vector3? walkingCenter = null, N.Vector3? walkingDestination = null)
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
        CameraFrustum.Fill(transform, projection, _demandFrustum);
        if (_demandNearBuffer.Length < required.Count) Array.Resize(ref _demandNearBuffer, required.Count);
        var nearCount = 0;
        foreach (var tile in required)
            if (tile.Level == LodLevel.L0) _demandNearBuffer[nearCount++] = _tree!.Bounds(tile);
        var view = new StreamDemandView(pose, _demandFrustum, _demandNearBuffer.AsMemory(0, nearCount))
        { WalkingCenter = walkingCenter, WalkingDestination = walkingDestination };
        ApplyParticipantDemand(in view, destination, preload);
        // The real reflection bridge reuses its own scratch; consume it before querying the next source view.
        if (_reflections.TryGetDemand(in view, out var reflected)) ApplyParticipantDemand(in reflected, destination, preload);
    }

    private void ApplyParticipantDemand(in StreamDemandView view, bool destination, List<StreamRequest>? preload)
    {
        foreach (var payload in _participantEntries)
        {
            var entry = payload.Content;
            var tile = payload.Tile;
            ParticipantTile? item = null;
            if (_groups.TryGetValue(tile, out var existing)) existing.Participants.TryGetValue(entry.Key, out item);
            var participant = payload.Participant;
            // Collision keeps an in-flight tile inside its release envelope, just as a completed tile.
            var resident = participant.Kind == StreamMountKind.Collision ? item is not null : item is { Mounted: true };
            var need = participant.Demand(in entry, in view, resident);
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
                    if (item is null) continue;
                    break;
                case StreamTileNeed.Required:
                    if (item is null)
                    {
                        item = new(entry, participant);
                        GetGroup(tile).Participants.Add(entry.Key, item);
                    }
                    if (destination) item.DestinationRequired = true;
                    else item.CurrentRequired = true;
                    break;
                default: throw new ArgumentOutOfRangeException(nameof(need));
            }
            if ((byte)need > (byte)item.Need) item.Need = need;
        }
    }

    private void QueueParticipants(List<StreamRequest> requests, Pose pose)
    {
        foreach (var group in _groups.Values)
            foreach (var (key, item) in group.Participants)
            {
                if (item.Need == StreamTileNeed.None) continue;
                var priority = ParticipantPriority(item);
                if (item.Error is not null && (item.DestinationRequired || priority == BakePriority.Collision)) _error ??= item.Error;
                if (!_issued.Contains(key))
                    requests.Add(new(key, group.Tile, priority, MathF.Sqrt(item.Entry.Bounds.DistanceSquared(pose.Position)) / Math.Max(1, _camera.Speed)));
                else
                {
                    PromotePreload(new(key, group.Tile, priority, 0), group, item);
                    if (priority is BakePriority.Collision or BakePriority.KnownDestination) _bake.Ensure(key, priority);
                }
            }
    }

    private static BakePriority ParticipantPriority(ParticipantTile item) => item.Participant.Kind switch
    {
        StreamMountKind.Collision => BakePriority.Collision,
        StreamMountKind.Auxiliary => item.DestinationRequired ? BakePriority.KnownDestination : BakePriority.Normal,
        StreamMountKind.Visual => throw new InvalidOperationException("Visual geometry is not a tile participant."),
        _ => throw new ArgumentOutOfRangeException(nameof(item))
    };

    private bool IsCurrent(Pending pending) => pending.Group is { } group && IsCurrent(pending.Request.Tile, group, pending.Revision) &&
        (pending.Participant is { } participant
            ? group.Participants.TryGetValue(pending.Request.Key, out var current) && ReferenceEquals(current, participant)
            : group.GeometryRequested && group.GeometryRevision == pending.GeometryRevision);

    private bool IsCurrentGeometry(StreamTile tile, TileGroup group, long revision, long geometryRevision) =>
        IsCurrent(tile, group, revision) && group.GeometryRequested && group.GeometryRevision == geometryRevision;

    private void MountParticipant(Pending pending, ParticipantTile item)
    {
        try
        {
            var product = pending.Handle.Product ?? throw new InvalidDataException($"Bake returned no product for {pending.Request.Key}.");
            var priority = ParticipantPriority(item);
            var work = item.Participant.CreateMount(pending.Request.Key, product, priority, pending.Revision);
            if (work.Key != pending.Request.Key || work.Revision != pending.Revision || work.Kind != item.Participant.Kind)
            {
                work.Cancel();
                throw new InvalidDataException($"Participant mount identity mismatch: {pending.Request.Key}.");
            }
            item.Work = new(work, () => IsCurrent(pending), () => item.Mounted = true,
                error => FailParticipant(pending.Group!, item, error));
            _mount.Enqueue(item.Work);
        }
        catch (Exception error) { FailParticipant(pending.Group!, item, error.GetBaseException().Message); }
    }

    private bool ParticipantsReady(bool destination)
    {
        foreach (var group in _groups.Values)
            foreach (var (key, item) in group.Participants)
                if ((destination ? item.DestinationRequired : item.CurrentRequired) &&
                    (item.Error is not null || !item.Mounted || !item.Participant.IsReady(key))) return false;
        return true;
    }

    private void PollParticipantErrors()
    {
        foreach (var group in _groups.Values)
            foreach (var item in group.Participants.Values)
                if (item.Need == StreamTileNeed.Required && item.Error is null && item.Participant.Error is { } error)
                    FailParticipant(group, item, error);
    }

    private void FailParticipant(TileGroup group, ParticipantTile item, string error)
    {
        Fail(group.Tile, error, item);
    }

    private void RetireParticipants()
    {
        foreach (var group in _groups.Values)
        {
            _retiredParticipants.Clear();
            foreach (var (key, item) in group.Participants)
            {
                if (item.Need != StreamTileNeed.None) continue;
                // Keep mounted near content until geometry covering its owner is actually drawn.
                if (item.Participant.Kind == StreamMountKind.Auxiliary && item.Mounted && !HasGeometryCoverage(group.Tile)) continue;
                ReleaseParticipant(key, item); _issued.Remove(key); _retiredParticipants.Add(key);
            }
            foreach (var key in _retiredParticipants) group.Participants.Remove(key);
        }
        _retiredParticipants.Clear();
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

    private static void ReleaseParticipant(BakeKey key, ParticipantTile item)
    {
        item.Work?.Cancel();
        item.Participant.Unmount(key);
        item.Mounted = false;
    }

    private void UnloadParticipants()
    {
        foreach (var group in _groups.Values)
            foreach (var (key, item) in group.Participants) ReleaseParticipant(key, item);
        _participants = [];
        _participantEntries = [];
    }

    private readonly record struct ParticipantEntry(StreamTile Tile, BakeTileEntry Content, IStreamTileParticipant Participant);

    private sealed class ParticipantTile(BakeTileEntry entry, IStreamTileParticipant participant)
    {
        public BakeTileEntry Entry { get; } = entry;
        public IStreamTileParticipant Participant { get; } = participant;
        public ParticipantMountWork? Work;
        public StreamTileNeed Need;
        public bool CurrentRequired, DestinationRequired, Mounted;
        public string? Error;
    }
}
