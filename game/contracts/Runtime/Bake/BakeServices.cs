
namespace Campus.Contracts.Bake;
public enum BakePriority : byte { Collision, KnownDestination, InteriorPrefetch, Normal, Background }
public enum BakeState : byte { Pending, Running, Ready, Failed }
public interface IBakeHandle
{
    BakeState State { get; }
    object? Product { get; }
    TileBundle? Tile => Product as TileBundle;
    CollisionTile? Collision => Product as CollisionTile;
    Exception? Failure { get; }
    Task Done { get; }
}
public readonly record struct BakeProgress(int Total, int Done, string Stage);
/// <summary>Application-lifetime successful decode, normal miss and published rebuild counters.</summary>
public readonly record struct BakeCacheStatistics(long Hits, long Misses, long Rebuilds);

public interface IBakeService
{
    BakeCacheStatistics CacheStatistics { get; }
    IBakeHandle Ensure(BakeKey key, BakePriority priority);
    BakeProgress Progress { get; }
    Exception? Failure { get; }
    long CacheBytes { get; }
}
