using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Gen.Collision;

namespace Campus.Kernel.Collision;

/// <summary>Called only by WP-02b's StreamMount queue, once per visible frame at most; covered frames share the loading UI slice.</summary>
public sealed class CollisionMountWork : IStreamMountWork
{
    // Keep ordinary batches small; 250 remains the hard ceiling checked again at the physics boundary.
    public const int ShapesPerSlice = 32;
    private readonly ICollisionMountRuntime _world;
    private readonly CollisionTile _tile;
    private readonly long _worldRevision;
    private readonly IReadOnlyList<Collider>[] _batches;
    private int _next;
    private bool _started;
    public BakeKey Key => _tile.Key;
    public BakePriority Priority => BakePriority.Collision;
    public long Revision { get; }
    public StreamMountKind Kind => StreamMountKind.Collision;
    public bool Complete { get; private set; }
    public bool Cancelled { get; private set; }
    public string? Error { get; private set; }

    public CollisionMountWork(ICollisionMountRuntime world, CollisionTile tile, long revision)
    {
        _world = world; _tile = tile; Revision = revision; _worldRevision = world.Revision;
        _batches = CollisionGeometry.BodyBatches(tile.Colliders).SelectMany(batch => batch.Chunk(ShapesPerSlice))
            .Select(batch => (IReadOnlyList<Collider>)batch).ToArray();
    }

    public bool Step(double budgetMilliseconds)
    {
        if (Complete || Cancelled || Error is not null) return true;
        if (_world.Revision != _worldRevision || _world.Map != Key.Map)
        {
            Cancelled = true;
            return true;
        }
        if (budgetMilliseconds <= 0) return false;
        try
        {
            if (!_started) { _world.BeginTile(_tile.Tile); _started = true; }
            if (_next < _batches.Length) _world.MountBody(_tile.Tile, _batches[_next++]);
            if (_next == _batches.Length)
            {
                _world.CompleteTile(_tile.Tile); Complete = true;
            }
            return Complete;
        }
        catch (Exception error)
        {
            Error = error.Message;
            _world.FailTile(_tile.Tile, Error);
            throw;
        }
    }

    public void Cancel()
    {
        if (Cancelled || Complete) return;
        Cancelled = true;
        if (_started && _world.Revision == _worldRevision) _world.RemoveTile(_tile.Tile);
    }
}
