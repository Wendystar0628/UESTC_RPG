using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Bake;
using Campus.Kernel.Lod;
using Godot;
using N = System.Numerics;

namespace Campus.Kernel.Stream;

internal sealed class StreamPins(StreamDirector host)
{
    private readonly StreamDirector _host = host;
    private IReadOnlyList<MapStreamDestination> _pinnedDestinations = Array.Empty<MapStreamDestination>();
    private PinnedTarget[] _pinnedTargets = [];
    private CancellationTokenSource? _pinnedCancellation;
    private IWorldModel? _pinnedWorld;
    internal IReadOnlyList<MapStreamDestination> PinnedDestinations => _pinnedDestinations;

    private readonly HashSet<StreamTile> _predictionRequired = [], _predictionRefined = [];

    internal void SetPinnedDestinations(IReadOnlyList<MapStreamDestination> destinations)
    {
        ArgumentNullException.ThrowIfNull(destinations);
        if (_host.Context is null && destinations.Count > 0)
            throw new InvalidOperationException("Pinned destinations require the mounted map.");
        CancelPinnedPreparation();
        _host.Preparation.ClearPins();
        _pinnedDestinations = Array.AsReadOnly(destinations.Distinct().ToArray());
        _host.Selection.RequestReplan();
    }

    internal void PollPinnedPreparation()
    {
        if (_host.Context is null || _pinnedDestinations.Count == 0) return;
        if (_pinnedCancellation is not null && !ReferenceEquals(_pinnedWorld, _host.Context.World)) CancelPinnedPreparation();
        if (_pinnedCancellation is null)
        {
            _pinnedCancellation = new(); _pinnedWorld = _host.Context.World;
            _pinnedTargets = _pinnedDestinations.Select(destination => new PinnedTarget(destination,
                PreparePinnedTerrain(_host.Terrain, _host.Context, destination, _pinnedCancellation.Token))).ToArray();
        }
        foreach (var target in _pinnedTargets)
        {
            if (target.Completed || !target.Preparation.IsCompleted) continue;
            target.Completed = true;
            try { target.Preparation.GetAwaiter().GetResult(); }
            catch (Exception error) { target.Error = error.GetBaseException().Message; GD.PushError(target.Error); }
            _host.Selection.RequestReplan();
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

    internal void QueuePinned(List<StreamRequest> requests, float height)
    {
        _host.Preparation.ClearPins();
        foreach (var target in _pinnedTargets)
        {
            if (!target.Completed || target.Error is not null) continue;
            try
            {
                var destination = target.Destination;
                if (!target.Classified)
                {
                    var point = new N.Vector2(destination.Pose.Position.X, destination.Pose.Position.Z);
                    if (!_host.Terrain.IsPrepared(_host.Context!.World, point)) continue;
                    target.Space = destination.Space;
                    if (target.Space is null && _host.Context.World.Spatial.MediumAt(destination.Pose.Position) is
                        { Kind: MediumKind.Interior, Feature: { } building } medium)
                        target.Space = new(building, checked((short)medium.Storey));
                    target.Classified = true;
                }
                _host.Selection.Tree!.Select(destination.Pose.Position, 0, height, destination.Pose.FovDeg ?? 60, LodLevel.L0,
                    _predictionRequired, _predictionRefined);
                foreach (var tile in _predictionRequired)
                    foreach (var module in _host.Preparation.Catalog!.Modules(tile))
                        _host.Preparation.QueueCpuRead(BakeRequestKeys.Tile(_host.Context!.Descriptor.Id, module, tile.Tile, tile.Level), tile, requests);
                _host.Participants.SelectParticipants(destination.Pose, _predictionRequired, destination: false, live: false, preload: requests,
                    walkingDestination: StreamSelection.WalkingDestination(destination.Pose, destination.Mode));
                _host.Interiors!.Preload(target.Space, destination.Pose.Position, key => _host.Preparation.QueueCpuRead(key, StreamTile.Root, requests));
            }
            catch (Exception error) { target.Error = error.GetBaseException().Message; GD.PushError(target.Error); }
        }
    }

    private void CancelPinnedPreparation()
    {
        if (_pinnedCancellation is { } cancellation)
        {
            _pinnedCancellation = null; cancellation.Cancel(); cancellation.Dispose();
        }
        foreach (var target in _pinnedTargets) _ = StreamSelection.ObserveDiscardedTerrain(target.Preparation);
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
