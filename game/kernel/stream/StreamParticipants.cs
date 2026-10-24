using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Kernel.Bake;
using Campus.Kernel.Collision;
using Campus.Kernel.Diagnostics;
using Campus.Kernel.Frame;
using Campus.Kernel.Lod;
using Campus.Kernel.Renderer;
using Campus.Kernel.Registry;
using Godot;
using N = System.Numerics;
using TileGroup = Campus.Kernel.Stream.StreamResidency.TileGroup;
using Pending = Campus.Kernel.Stream.StreamPreparation.Pending;

namespace Campus.Kernel.Stream;

internal sealed class StreamParticipants(StreamDirector host, ServiceRegistry registry, Camera3D viewCamera)
{
    private readonly StreamDirector _host = host;
    private readonly ServiceRegistry _registry = registry;
    private readonly Camera3D _viewCamera = viewCamera;
    private IStreamTileParticipant[] _participants = [];
    private ParticipantEntry[] _participantEntries = [];
    private readonly N.Plane[] _demandFrustum = new N.Plane[6];
    private Box3[] _demandNearBuffer = [];
    private readonly List<BakeKey> _retiredParticipants = [];

    internal void LoadParticipants(MapId map) => _participants = _registry.RuntimeInstances<IStreamTileParticipant>(map).ToArray();

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

    internal void LoadParticipantEntries(ModuleTileCatalog catalog)
    {
        var entries = new List<ParticipantEntry>(catalog.Auxiliary.Count);
        foreach (var auxiliary in catalog.Auxiliary)
        {
            var entry = auxiliary.Content;
            if (entry.Key.Map != _host.Context!.Descriptor.Id)
                throw new InvalidDataException($"Invalid auxiliary directory key: {entry.Key}.");
            entries.Add(new(auxiliary.Tile, entry, Participant(entry.Key)));
        }
        var map = _host.Context!.Descriptor;
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

    internal void ResetParticipantDemand()
    {
        foreach (var group in _host.Residency.Groups.Values)
            foreach (var item in group.Participants.Values)
            { item.Need = StreamTileNeed.None; item.CurrentRequired = item.DestinationRequired = item.PinnedRequired = false; }
    }

    internal void SelectParticipants(Pose pose, HashSet<StreamTile> required, bool destination, bool live,
        HashSet<BakeKey>? pinned = null, N.Vector3? walkingCenter = null, N.Vector3? walkingDestination = null)
    {
        var camera = _viewCamera;
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
            if (tile.Level == LodLevel.L0) _demandNearBuffer[nearCount++] = _host.Selection.Tree!.Bounds(tile);
        var view = new StreamDemandView(pose, _demandFrustum, _demandNearBuffer.AsMemory(0, nearCount))
        { WalkingCenter = walkingCenter, WalkingDestination = walkingDestination, ResourcePreload = pinned is not null };
        ApplyParticipantDemand(in view, destination, pinned);
        // The real reflection bridge reuses its own scratch; consume it before querying the next source view.
        if (_host.Reflections.TryGetDemand(in view, out var reflected))
        {
            reflected = reflected with { ResourcePreload = pinned is not null };
            ApplyParticipantDemand(in reflected, destination, pinned);
        }
    }

    private void ApplyParticipantDemand(in StreamDemandView view, bool destination, HashSet<BakeKey>? pinned)
    {
        foreach (var payload in _participantEntries)
        {
            var entry = payload.Content;
            var tile = payload.Tile;
            ParticipantTile? item = null;
            if (_host.Residency.Groups.TryGetValue(tile, out var existing)) existing.Participants.TryGetValue(entry.Key, out item);
            var participant = payload.Participant;
            // Collision keeps an in-flight tile inside its release envelope, just as a completed tile.
            var resident = participant.Kind == StreamMountKind.Collision ? item is not null : item is { Mounted: true };
            var need = participant.Demand(in entry, in view, resident);
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
                        _host.Residency.GetGroup(tile).Participants.Add(entry.Key, item);
                    }
                    if (pinned is not null) { item.PinnedRequired = true; pinned.Add(entry.Key); }
                    else if (destination) item.DestinationRequired = true;
                    else item.CurrentRequired = true;
                    if (pinned is null) item.KeepForCoverage = true;
                    break;
                default: throw new ArgumentOutOfRangeException(nameof(need));
            }
            if ((byte)need > (byte)item.Need) item.Need = need;
        }
    }

    internal void QueueParticipants(List<StreamRequest> requests, Pose pose)
    {
        foreach (var group in _host.Residency.Groups.Values)
            foreach (var (key, item) in group.Participants)
            {
                if (item.Need == StreamTileNeed.None) continue;
                var priority = ParticipantPriority(item);
                if (!_host.Preparation.IsIssued(key))
                    requests.Add(new(key, group.Tile, priority, MathF.Sqrt(item.Entry.Bounds.DistanceSquared(pose.Position)) / Math.Max(1, _host.Camera.Speed)));
                else
                {
                    _host.Bake.Ensure(key, priority);
                }
            }
    }

    internal static BakePriority ParticipantPriority(ParticipantTile item) => !item.CurrentRequired && !item.DestinationRequired
        ? BakePriority.Background : item.Participant.Kind switch
    {
        StreamMountKind.Collision => BakePriority.Collision,
        StreamMountKind.Auxiliary => item.DestinationRequired ? BakePriority.KnownDestination : BakePriority.Normal,
        StreamMountKind.Visual => throw new InvalidOperationException("Visual geometry is not a tile participant."),
        _ => throw new ArgumentOutOfRangeException(nameof(item))
    };

    internal void MountParticipant(Pending pending, ParticipantTile item)
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
            item.Work = new(work, () => _host.Residency.IsCurrent(pending), () =>
                {
                    item.Mounted = true;
                    _host.RecordTileChange(new(work.Key, work.Kind, work.Revision, StreamTileChangeKind.Mounted,
                        null, FrameClock.MonotonicMilliseconds));
                },
                error => FailParticipant(pending.Group, item, error), () => ParticipantPriority(item));
            _host.Mount.Enqueue(item.Work);
        }
        catch (Exception error) { FailParticipant(pending.Group, item, error); }
    }

    internal bool ParticipantsReady(bool destination)
    {
        foreach (var group in _host.Residency.Groups.Values)
            foreach (var (key, item) in group.Participants)
                if ((destination ? item.DestinationRequired : item.CurrentRequired) &&
                    (item.Failure is not null || !item.Mounted || !item.Participant.IsReady(key))) return false;
        return true;
    }

    internal bool ResourcesReady(IReadOnlySet<BakeKey> keys)
    {
        var found = 0;
        foreach (var group in _host.Residency.Groups.Values)
            foreach (var (key, item) in group.Participants)
            {
                if (!keys.Contains(key)) continue;
                if (!item.PinnedRequired || item.Failure is not null || !item.Mounted || !item.Participant.IsReady(key)) return false;
                found++;
            }
        return found == keys.Count;
    }

    internal void PollParticipantErrors()
    {
        // A producer's failure belongs to the map lifetime, independent of any tile's demand.
        foreach (var participant in _participants)
            if (participant.Failure is { } error) _host.RecordFailure(error);
    }

    private void FailParticipant(TileGroup group, ParticipantTile item, [FailureHandler("ENG-PERF-STREAM")] Exception error)
    {
        _host.Fail(group.Tile, error, item);
    }

    internal void RetireParticipants()
    {
        foreach (var group in _host.Residency.Groups.Values)
        {
            _retiredParticipants.Clear();
            foreach (var (key, item) in group.Participants)
            {
                if (item.Need != StreamTileNeed.None) continue;
                // Keep mounted near content until geometry covering its owner is actually drawn.
                if (_host.Selection.Destination is not null && item.Participant.Kind == StreamMountKind.Auxiliary && item.KeepForCoverage && item.Mounted && !_host.Residency.HasGeometryCoverage(group.Tile)) continue;
                ReleaseParticipant(key, item); _host.Preparation.Forget(key); _retiredParticipants.Add(key);
            }
            foreach (var key in _retiredParticipants) group.Participants.Remove(key);
        }
        _retiredParticipants.Clear();
    }

    private void ReleaseParticipant(BakeKey key, ParticipantTile item)
    {
        Exception? failure = null;
        var unmounted = false;
        try { item.Work?.Cancel(); } catch (Exception error) { failure = RetainedFailure.Append(failure, error); }
        try { item.Participant.Unmount(key); unmounted = true; }
        catch (Exception error) { failure = RetainedFailure.Append(failure, error); }
        var wasMounted = item.Mounted;
        item.Mounted = false;
        try
        {
            if (unmounted && wasMounted)
            {
            var work = item.Work ?? throw new InvalidOperationException("Mounted participant has no work identity.");
            _host.RecordTileChange(new(work.Key, work.Kind, work.Revision, StreamTileChangeKind.Unmounted,
                null, FrameClock.MonotonicMilliseconds));
        }
        }
        catch (Exception error) { failure = RetainedFailure.Append(failure, error); }
        if (failure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
    }

    internal void UnloadParticipants()
    {
        foreach (var group in _host.Residency.Groups.Values)
            foreach (var (key, item) in group.Participants)
                try { ReleaseParticipant(key, item); }
                catch (Exception error) { _host.RecordFailure(error); }
        _participants = [];
        _participantEntries = [];
    }

    private readonly record struct ParticipantEntry(StreamTile Tile, BakeTileEntry Content, IStreamTileParticipant Participant);

    internal sealed class ParticipantTile(BakeTileEntry entry, IStreamTileParticipant participant)
    {
        public BakeTileEntry Entry { get; } = entry;
        public IStreamTileParticipant Participant { get; } = participant;
        public ParticipantMountWork? Work;
        public StreamTileNeed Need;
        public bool CurrentRequired, DestinationRequired, PinnedRequired, KeepForCoverage, Mounted;
        public Exception? Failure;
    }
}
