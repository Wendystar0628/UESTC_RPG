using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Bake;
using Campus.Kernel.Lod;
using N = System.Numerics;

namespace Campus.Kernel.Stream;

internal sealed class StreamPins(StreamDirector host)
{
    private readonly StreamDirector _host = host;
    private IReadOnlyList<MapStreamDestination> _pinnedDestinations = Array.Empty<MapStreamDestination>();
    private PinnedTarget[] _pinnedTargets = [];
    private readonly List<PinnedTarget> _retired = [];
    private readonly HashSet<StreamTile> _required = [];
    internal bool IsIdle => _pinnedTargets.Length == 0 && _retired.Count == 0;
    internal int Preparations
    {
        get
        {
            var count = 0;
            foreach (var target in _pinnedTargets) if (!target.Completed) count++;
            return count;
        }
    }
    internal int Retirements => _retired.Count;
    internal IReadOnlyList<MapStreamDestination> PinnedDestinations => _pinnedDestinations;
    internal IReadOnlySet<StreamTile> Required => _required;

    internal IDisposable PinDestination(MapStreamDestination destination)
    {
        ArgumentNullException.ThrowIfNull(destination);
        var context = _host.Context ?? throw new InvalidOperationException("Pinned destinations require the mounted map.");
        var target = _pinnedTargets.FirstOrDefault(target => target.Destination == destination && Current(target));
        if (target is null)
        {
            var cancellation = new CancellationTokenSource();
            target = new(destination, _host.MapGeneration, context, cancellation,
                PreparePinnedTerrain(_host.Terrain, context, destination, cancellation.Token));
            _pinnedTargets = [.. _pinnedTargets, target];
            RefreshDestinations();
        }
        target.Requests++;
        return new PinLease(this, target);
    }

    private void Release(PinnedTarget target)
    {
        if (!Current(target) || target.Requests == 0) return;
        if (--target.Requests > 0) return;
        _pinnedTargets = _pinnedTargets.Where(active => !ReferenceEquals(active, target)).ToArray();
        Retire(target);
        RefreshDestinations();
    }

    private void RefreshDestinations()
    {
        _pinnedDestinations = Array.AsReadOnly(_pinnedTargets.Select(target => target.Destination).ToArray());
        _required.Clear();
        _host.Selection.RequestReplan();
    }

    internal void StopNewWork()
    {
        foreach (var target in _pinnedTargets) { target.Requests = 0; Retire(target); }
        _pinnedTargets = [];
        RefreshDestinations();
    }

    private bool Current(PinnedTarget target) => target.Generation == _host.MapGeneration &&
        ReferenceEquals(target.Context, _host.Context) && ReferenceEquals(target.World, _host.Context?.World);

    internal bool DestinationReady(MapStreamDestination destination)
    {
        if (_host.Failure is not null || _host.Context is null) return false;
        foreach (var target in _pinnedTargets)
        {
            if (target.Destination != destination || !Current(target)) continue;
            if (!target.Completed || !target.Selected || target.Failure is not null) return false;
            foreach (var tile in target.Required)
                if (!_host.Residency.IsReady(tile)) return false;
            if (!_host.Participants.ResourcesReady(target.Participants) || !_host.Interiors!.ResourcesReady(target.Interiors)) return false;
            return destination.Mode switch
            {
                ViewMode.Fly => true,
                ViewMode.Walk => _host.Collision.Ready(destination.CollisionFeet ?? destination.Pose.Position, WalkRules.ArrivalCollisionRadius),
                _ => throw new ArgumentOutOfRangeException(nameof(destination))
            };
        }
        return false;
    }

    internal void PollPinnedPreparation()
    {
        foreach (var target in _pinnedTargets)
        {
            if (target.Completed || !target.Preparation.IsCompleted) continue;
            target.Completed = true;
            try { target.Preparation.GetAwaiter().GetResult(); }
            catch (OperationCanceledException) when (target.Cancellation.IsCancellationRequested) { }
            catch (Exception error) { target.Failure = error; _host.RecordFailure(error); }
            _host.Selection.RequestReplan();
        }
    }

    private static async Task PreparePinnedTerrain(ITerrainPreparation terrain, IMapContext context,
        MapStreamDestination destination, CancellationToken cancellation)
    {
        var point = new N.Vector2(destination.Pose.Position.X, destination.Pose.Position.Z);
        var feet = destination.CollisionFeet ?? destination.Pose.Position;
        var center = new N.Vector2(feet.X, feet.Z);
        await terrain.PreparePoints(context, point == center ? [point] : [point, center], cancellation);
        cancellation.ThrowIfCancellationRequested();
        switch (destination.Mode)
        {
            case ViewMode.Fly: break;
            case ViewMode.Walk:
                var reach = new N.Vector2(WalkRules.ArrivalCollisionRadius);
                await terrain.PrepareRegion(context, new(center - reach, center + reach), cancellation);
                cancellation.ThrowIfCancellationRequested();
                break;
            default: throw new ArgumentOutOfRangeException(nameof(destination));
        }
    }

    internal void Select()
    {
        _required.Clear();
        _host.Interiors!.ResetPins();
        foreach (var target in _pinnedTargets)
        {
            if (!Current(target) || !target.Completed || target.Failure is not null) continue;
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
                _host.Selection.Tree!.Select(destination.Pose.Position, 0, destination.Pose.FovDeg ?? 60, LodLevel.L0,
                    target.Refined, target.Required, target.Refined);
                _required.UnionWith(target.Required);
                target.Participants.Clear(); target.Interiors.Clear();
                _host.Participants.SelectParticipants(destination.Pose, target.Required, destination: false, live: false,
                    pinned: target.Participants, walkingDestination: destination.CollisionFeet ?? StreamSelection.WalkingDestination(destination.Pose, destination.Mode));
                _host.Interiors.Pin(target.Space, destination.Pose.Position, target.Interiors);
                target.Selected = true;
            }
            catch (Exception error) { target.Failure = error; _host.RecordFailure(error); }
        }
    }

    private void Retire(PinnedTarget target)
    {
        _retired.Add(target); target.Cancellation.Cancel();
    }

    internal void PollRetired()
    {
        for (var i = _retired.Count - 1; i >= 0; i--)
        {
            var target = _retired[i];
            if (!target.Preparation.IsCompleted) continue;
            try { target.Preparation.GetAwaiter().GetResult(); }
            catch (OperationCanceledException) when (target.Cancellation.IsCancellationRequested) { }
            catch (Exception error) { _host.RecordFailure(error); }
            target.Cancellation.Dispose(); _retired.RemoveAt(i);
        }
    }

    internal async Task DrainAsync()
    {
        StopNewWork();
        foreach (var target in _retired)
        {
            try { await target.Preparation; }
            catch (OperationCanceledException) when (target.Cancellation.IsCancellationRequested) { }
            catch (Exception error) { _host.RecordFailure(error); }
            target.Cancellation.Dispose();
        }
        _retired.Clear();
    }

    private sealed class PinLease(StreamPins owner, PinnedTarget target) : IDisposable
    {
        private bool _released;
        public void Dispose()
        {
            if (_released) return;
            _released = true;
            owner.Release(target);
        }
    }

    private sealed class PinnedTarget(MapStreamDestination destination, long generation, IMapContext context,
        CancellationTokenSource cancellation, Task preparation)
    {
        public MapStreamDestination Destination { get; } = destination;
        public long Generation { get; } = generation;
        public IMapContext Context { get; } = context;
        public IWorldModel World { get; } = context.World;
        public CancellationTokenSource Cancellation { get; } = cancellation;
        public Task Preparation { get; } = preparation;
        public HashSet<StreamTile> Required { get; } = [];
        public HashSet<StreamTile> Refined { get; } = [];
        public HashSet<BakeKey> Participants { get; } = [];
        public HashSet<BakeKey> Interiors { get; } = [];
        public NavSpace? Space;
        public int Requests;
        public bool Completed, Classified, Selected;
        public Exception? Failure;
    }
}
