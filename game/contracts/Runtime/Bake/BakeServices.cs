
namespace Campus.Contracts.Bake;
public enum BakePriority : byte { Collision, KnownDestination, InteriorPrefetch, Normal, Background }
public enum BakeState : byte { Pending, Running, Ready, Failed }
public interface IBakeHandle
{
    BakeState State { get; }
    object? Product { get; }
    TileBundle? Tile => Product as TileBundle;
    CollisionTile? Collision => Product as CollisionTile;
    string? Error { get; }
}
public readonly record struct BakeProgress(int Total, int Done, string Stage);
public interface IBakeService
{
    IBakeHandle Ensure(BakeKey key, BakePriority priority);
    BakeProgress Progress { get; }
    long CacheBytes { get; }
    void Clear();
    void ClearFailures();
}
