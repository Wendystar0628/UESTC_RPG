using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Bake;
using Campus.Kernel.Bake;
using Campus.Kernel.Lod;
using Godot;
using N = System.Numerics;

namespace Campus.Kernel.Stream;

public partial class StreamDirector
{
    private IReadOnlyList<MapStreamDestination> _pinnedDestinations = Array.Empty<MapStreamDestination>();
    private PinnedTarget[] _pinnedTargets = [];
    private CancellationTokenSource? _pinnedCancellation;
    private IWorldModel? _pinnedWorld;
    public IReadOnlyList<MapStreamDestination> PinnedDestinations => _pinnedDestinations;

    public void SetPinnedDestinations(IReadOnlyList<MapStreamDestination> destinations)
    {
        ArgumentNullException.ThrowIfNull(destinations);
        if (_context is null && destinations.Count > 0)
            throw new InvalidOperationException("Pinned destinations require the mounted map.");
        CancelPinnedPreparation();
        foreach (var pending in _pending)
            if (pending.Request.PreloadOnly) pending.Pinned = false;
        _pinnedDestinations = Array.AsReadOnly(destinations.Distinct().ToArray());
        _replan = _settings.ReplanSeconds;
    }

    private void PollPinnedPreparation()
    {
        if (_context is null || _pinnedDestinations.Count == 0) return;
        if (_pinnedCancellation is not null && !ReferenceEquals(_pinnedWorld, _context.World)) CancelPinnedPreparation();
        if (_pinnedCancellation is null)
        {
            _pinnedCancellation = new(); _pinnedWorld = _context.World;
            _pinnedTargets = _pinnedDestinations.Select(destination => new PinnedTarget(destination,
                PreparePinnedTerrain(_terrain, _context, destination, _pinnedCancellation.Token))).ToArray();
        }
        foreach (var target in _pinnedTargets)
        {
            if (target.Completed || !target.Preparation.IsCompleted) continue;
            target.Completed = true;
            try { target.Preparation.GetAwaiter().GetResult(); }
            catch (Exception error) { target.Error = error.GetBaseException().Message; GD.PushError(target.Error); }
            _replan = _settings.ReplanSeconds;
        }
    }

    private static async Task PreparePinnedTerrain(ITerrainPreparation terrain, IMapContext context,
        MapStreamDestination destination, CancellationToken cancellation)
    {
        var point = new N.Vector2(destination.Pose.Position.X, destination.Pose.Position.Z);
        await terrain.PreparePoints(context, FormattableString.Invariant($"stream-pin/{point.X:R}/{point.Y:R}"), [point], cancellation);
        cancellation.ThrowIfCancellationRequested();
        switch (destination.Mode)
        {
            case ViewMode.Fly: break;
            case ViewMode.Walk:
                var reach = new N.Vector2(WalkRules.ArrivalCollisionRadius);
                await terrain.PrepareRegion(context, new(point - reach, point + reach), cancellation);
                cancellation.ThrowIfCancellationRequested();
                break;
            default: throw new ArgumentOutOfRangeException(nameof(destination));
        }
    }

    private void QueuePinned(List<StreamRequest> requests, float height)
    {
        foreach (var pending in _pending)
            if (pending.Request.PreloadOnly) pending.Pinned = false;
        foreach (var target in _pinnedTargets)
        {
            if (!target.Completed || target.Error is not null) continue;
            try
            {
                var destination = target.Destination;
                if (!target.Classified)
                {
                    var point = new N.Vector2(destination.Pose.Position.X, destination.Pose.Position.Z);
                    if (!_terrain.IsPrepared(_context!.World, point)) continue;
                    target.Space = destination.Space;
                    if (target.Space is null && _context.World.Spatial.MediumAt(destination.Pose.Position) is
                        { Kind: MediumKind.Interior, Feature: { } building } medium)
                        target.Space = new(building, checked((short)medium.Storey));
                    target.Classified = true;
                }
                _tree!.Select(destination.Pose.Position, 0, height, destination.Pose.FovDeg ?? 60, LodLevel.L0,
                    _predictionRequired, _predictionRefined);
                foreach (var tile in _predictionRequired)
                    foreach (var module in _catalog!.Modules(tile))
                        QueueCpuRead(BakeRequestKeys.Tile(_context!.Descriptor.Id, module, tile.Tile, tile.Level), tile, requests);
                SelectParticipants(destination.Pose, _predictionRequired, destination: false, live: false, preload: requests,
                    walkingDestination: WalkingDestination(destination.Pose, destination.Mode));
                _interiors!.Preload(target.Space, destination.Pose.Position, key => QueueCpuRead(key, StreamTile.Root, requests));
            }
            catch (Exception error) { target.Error = error.GetBaseException().Message; GD.PushError(target.Error); }
        }
    }

    private void QueueCpuRead(BakeKey key, StreamTile tile, List<StreamRequest> requests)
    {
        if (_issued.Contains(key))
        {
            foreach (var pending in _pending)
                if (pending.Request.PreloadOnly && pending.Request.Key == key && pending.Revision == _mount.Revision)
                    pending.Pinned = true;
            _bake.Ensure(key, BakePriority.KnownDestination); return;
        }
        for (var i = 0; i < requests.Count; i++)
            if (requests[i].Key == key)
            {
                requests[i] = requests[i] with { Priority = BakePriority.KnownDestination };
                return;
            }
        requests.Add(new(key, tile, BakePriority.KnownDestination, 0) { PreloadOnly = true });
    }

    private void PromotePreload(StreamRequest request, TileGroup group, ParticipantTile? participant)
    {
        for (var i = 0; i < _pending.Count; i++)
        {
            var pending = _pending[i];
            if (!pending.Request.PreloadOnly || pending.Request.Key != request.Key || pending.Revision != _mount.Revision) continue;
            _pending[i] = pending with { Request = request, Group = group, GeometryRevision = group.GeometryRevision, Participant = participant, Pinned = false };
            return;
        }
    }

    private void CancelPinnedPreparation()
    {
        if (_pinnedCancellation is { } cancellation)
        {
            _pinnedCancellation = null; cancellation.Cancel(); cancellation.Dispose();
        }
        foreach (var target in _pinnedTargets) _ = ObserveDiscardedTerrain(target.Preparation);
        _pinnedTargets = []; _pinnedWorld = null;
    }

    private sealed class PinnedTarget(MapStreamDestination destination, Task preparation)
    {
        public MapStreamDestination Destination { get; } = destination;
        public Task Preparation { get; } = preparation;
        public NavSpace? Space;
        public bool Completed, Classified;
        public string? Error;
    }
}
