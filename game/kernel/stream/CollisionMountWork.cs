using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Gen.Collision;
using Campus.Kernel.Collision;

namespace Campus.Kernel.Stream;

/// <summary>One complete body per shared queue step; physics publication follows the final body.</summary>
public sealed class CollisionMountWork : StreamMountWork
{
    // Keep ordinary batches small; 250 remains the hard ceiling at the physics boundary.
    public const int ShapesPerSlice = 32;
    private readonly ICollisionMountRuntime _world;
    private readonly CollisionTile _tile;
    private readonly long _worldRevision;
    private readonly IReadOnlyList<Collider>[] _batches;
    private int _next;
    private bool _started;

    public CollisionMountWork(ICollisionMountRuntime world, CollisionTile tile, long revision)
        : base(tile.Key, BakePriority.Collision, revision, StreamMountKind.Collision)
    {
        _world = world; _tile = tile; _worldRevision = world.Revision;
        _batches = CollisionGeometry.BodyBatches(tile.Colliders).SelectMany(batch => batch.Chunk(ShapesPerSlice))
            .Select(batch => (IReadOnlyList<Collider>)batch).ToArray();
    }

    protected override bool IsCurrent => _world.Revision == _worldRevision && _world.Map == Key.Map;

    protected override bool MountStep(double budgetMilliseconds)
    {
        if (!_started) { _world.BeginTile(_tile.Tile); _started = true; }
        if (_next < _batches.Length) _world.MountBody(_tile.Tile, _batches[_next++]);
        if (_next != _batches.Length) return false;
        _world.CompleteTile(_tile.Tile);
        return true;
    }

    protected override void Release()
    {
        if (_started && _world.Revision == _worldRevision) _world.RemoveTile(_tile.Tile);
    }

    protected override void Failed(Exception error)
    {
        if (_world.Revision == _worldRevision) _world.FailTile(_tile.Tile, error);
    }
}
