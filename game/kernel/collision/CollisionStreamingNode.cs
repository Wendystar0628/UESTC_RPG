using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Maps;
using Campus.Gen.Collision;
using Godot;
using N = System.Numerics;
using Services = Campus.Kernel.Registry.Kernel;

namespace Campus.Kernel.Collision;

/// <summary>Requests C tiles at Stream; WP-02b alone executes their work at StreamMount.</summary>
[KernelNode("collision.streaming", Owner = "WP-43")]
public partial class CollisionStreamingNode : Node3D, IMapAware
{
    public const float LoadRadius = 96f;
    public const float UnloadRadius = 160f;
    private readonly Dictionary<TileKey, IBakeHandle> _pending = [];
    private readonly Dictionary<TileKey, CollisionMountWork> _mounting = [];
    private readonly HashSet<TileKey> _resident = [];
    private readonly HashSet<TileKey> _failed = [];
    private IMapContext? _context;
    private CollisionWorld? _world;
    private IBakeService? _bake;
    private IStreamMountQueue? _queue;
    private IObserverState? _observer;
    private IMapStreamState? _streamState;
    private N.Vector3? _lastPrefetchTarget;
    private double _untilSchedule;
    private bool _walking;

    public override void _Ready()
    {
        ProcessPriority = FramePriority.Stream;
        _world = Services.Get<ICollisionWorld>() as CollisionWorld ?? throw new InvalidOperationException("WP-43 collision service was not registered.");
        _bake = Services.Get<IBakeService>();
        _queue = Services.Get<IStreamMountQueue>();
        _observer = Services.Get<IObserverState>();
        _streamState = Services.Get<IMapStreamState>();
    }

    public void OnMapLoaded(IMapContext context)
    {
        if (_world is null) throw new InvalidOperationException("Collision node must enter the tree before its map loads.");
        Reset();
        _context = context;
        _world.Attach(this, context);
        _untilSchedule = 0;
    }

    public void OnMapUnloading(MapId from) { if (_context?.Descriptor.Id == from) Reset(); }
    public override void _ExitTree() => Reset();

    public override void _Process(double delta)
    {
        if (_context is null || _world is null || _bake is null || _queue is null || _observer is null) return;
        var walk = IsWalk(_observer.Mode);
        // Destination retains the last result after arrival. Only an outstanding request
        // may add a second collision residency center beside the current observer.
        var destination = _streamState is { HasPendingDestination: true } stream ? stream.Destination : null;
        var target = destination is not null && IsWalk(destination.Mode) ? destination.Pose.Position : (N.Vector3?)null;
        if (target != _lastPrefetchTarget) { _lastPrefetchTarget = target; _untilSchedule = 0; }
        if (!walk && target is null)
        {
            if (_walking)
            {
                ClearTiles();
                _world.Unload();
                _world.Attach(this, _context);
            }
            _walking = false;
            return;
        }
        if (!_walking) _untilSchedule = 0;
        _walking = true;
        _untilSchedule -= delta;
        var centers = walk ? (target is { } known ? new[] { _observer.Feet, known } : new[] { _observer.Feet }) : new[] { target!.Value };
        if (_untilSchedule <= 0) { Schedule(centers); _untilSchedule = 0.1; }
        foreach (var pair in _pending.OrderBy(p => DistanceSquared(p.Key, centers[0])).ThenBy(p => p.Key.X).ThenBy(p => p.Key.Z).ToArray())
        {
            if (pair.Value.State == BakeState.Failed)
            {
                _world.FailTile(pair.Key, pair.Value.Error ?? "Collision bake failed.");
                // Keep a failed handle to avoid creating an unbounded retry loop.
                continue;
            }
            if (pair.Value.State != BakeState.Ready) continue;
            if (pair.Value.Product is not CollisionTile product || product.Key != CollisionBakeProducer.Key(_context.Descriptor.Id, pair.Key) || product.Tile != pair.Key)
                throw new InvalidOperationException("Collision bake product identity mismatch.");
            var work = new CollisionMountWork(_world, product, _queue.Revision);
            _mounting.Add(pair.Key, work);
            _queue.Enqueue(work);
            _pending.Remove(pair.Key);
        }
        foreach (var pair in _mounting.Where(p => p.Value.Complete || p.Value.Cancelled || p.Value.Error is not null).ToArray())
        {
            if (pair.Value.Complete) _resident.Add(pair.Key);
            if (pair.Value.Error is not null) _failed.Add(pair.Key);
            _mounting.Remove(pair.Key);
        }
    }

    private void Schedule(IReadOnlyList<N.Vector3> centers)
    {
        var map = _context!.Descriptor;
        foreach (var center in centers)
        {
            var min = CollisionGeometry.TileAt(center - new N.Vector3(LoadRadius, 0, LoadRadius));
            var max = CollisionGeometry.TileAt(center + new N.Vector3(LoadRadius, 0, LoadRadius));
            var wanted = new List<TileKey>();
            for (var x = Math.Max(min.X, map.TileMin.X); x <= Math.Min(max.X, map.TileMax.X); x++)
                for (var z = Math.Max(min.Z, map.TileMin.Z); z <= Math.Min(max.Z, map.TileMax.Z); z++)
                {
                    var tile = new TileKey(x, z);
                    if (DistanceSquared(tile, center) <= LoadRadius * LoadRadius) wanted.Add(tile);
                }
            foreach (var tile in wanted.OrderBy(t => DistanceSquared(t, center)).ThenBy(t => t.X).ThenBy(t => t.Z))
                if (!_pending.ContainsKey(tile) && !_mounting.ContainsKey(tile) && !_resident.Contains(tile) && !_failed.Contains(tile))
                    _pending.Add(tile, _bake!.Ensure(CollisionBakeProducer.Key(map.Id, tile), BakePriority.Collision));
        }
        bool Outside(TileKey tile) => centers.All(center => DistanceSquared(tile, center) > UnloadRadius * UnloadRadius);
        foreach (var tile in _pending.Keys.Where(Outside).ToArray()) _pending.Remove(tile);
        foreach (var tile in _mounting.Keys.Where(Outside).ToArray())
        { _mounting[tile].Cancel(); _world!.RemoveTile(tile); _mounting.Remove(tile); }
        foreach (var tile in _resident.Where(Outside).ToArray())
        { _world!.RemoveTile(tile); _resident.Remove(tile); }
    }

    private void ClearTiles()
    {
        foreach (var work in _mounting.Values) work.Cancel();
        _pending.Clear(); _mounting.Clear(); _resident.Clear(); _failed.Clear();
    }
    private void Reset()
    {
        ClearTiles(); _world?.Unload(); _context = null; _walking = false; _lastPrefetchTarget = null;
    }
    private static float DistanceSquared(TileKey tile, N.Vector3 feet)
    {
        var origin = CollisionGeometry.Origin(tile);
        var dx = MathF.Max(MathF.Max(origin.X - feet.X, feet.X - origin.X - 64), 0);
        var dz = MathF.Max(MathF.Max(origin.Z - feet.Z, feet.Z - origin.Z - 64), 0);
        return dx * dx + dz * dz;
    }
    private static bool IsWalk(ViewMode mode) => mode switch { ViewMode.Walk => true, ViewMode.Fly => false, _ => throw new InvalidOperationException("Unknown observer mode.") };
}
