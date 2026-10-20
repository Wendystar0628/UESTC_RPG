using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Maps;
using Campus.Gen.Collision;
using Campus.Kernel.Collision;
using Godot;
using KernelServices = Campus.Kernel.Registry.Kernel;

namespace Campus.Kernel.Stream;

/// <summary>Collision payload owner; StreamDirector alone schedules and retires collision tiles.</summary>
[KernelNode("collision.streaming")]
public partial class CollisionTileParticipant : Node3D, IMapAware, IStreamTileParticipant
{
    public const float LoadRadius = 96f;
    public const float UnloadRadius = 160f;
    private IMapContext? _context;
    private ICollisionMountRuntime? _world;
    public StreamMountKind Kind => StreamMountKind.Collision;
    public string? Error => _world?.Error;

    public override void _Ready() => _world = KernelServices.Get<ICollisionMountRuntime>();

    public void OnMapLoaded(IMapContext context)
    {
        if (_world is null) throw new InvalidOperationException("Collision node must enter the tree before its map loads.");
        _world.Attach(this, context);
        _context = context;
    }

    public void OnMapUnloading(MapId from) { if (_context?.Descriptor.Id == from) Reset(); }
    public override void _ExitTree() => Reset();

    public bool CanMount(BakeKey key) => key.Module == CollisionBakeProducer.Module && key.Detail.StartsWith("C/", StringComparison.Ordinal);

    public StreamTileNeed Demand(in BakeTileEntry entry, in StreamDemandView view, bool resident)
    {
        if (!CanMount(entry.Key)) return StreamTileNeed.None;
        var distance = float.PositiveInfinity;
        if (view.WalkingCenter is { } current) distance = entry.Bounds.HorizontalDistanceSquared(current);
        if (view.WalkingDestination is { } destination) distance = MathF.Min(distance, entry.Bounds.HorizontalDistanceSquared(destination));
        if (distance <= LoadRadius * LoadRadius) return StreamTileNeed.Required;
        return resident && distance <= UnloadRadius * UnloadRadius ? StreamTileNeed.Retain : StreamTileNeed.None;
    }

    public IStreamMountWork CreateMount(BakeKey key, object product, BakePriority priority, long revision)
    {
        if (!CanMount(key) || _context?.Descriptor.Id != key.Map || product is not CollisionTile tile ||
            tile.Key != key || tile.Tile != CollisionBakeProducer.ParseTile(key.Detail))
            throw new ArgumentException("Collision payload does not match its tile request.");
        return new CollisionMountWork(_world!, tile, revision);
    }

    public bool IsReady(BakeKey key) => Error is null && _world is { } world && world.Map == key.Map &&
        world.IsTileReady(CollisionBakeProducer.ParseTile(key.Detail));

    public void Unmount(BakeKey key)
    {
        if (_world is { } world && world.Map == key.Map) world.RemoveTile(CollisionBakeProducer.ParseTile(key.Detail));
    }

    private void Reset() { _world?.Unload(); _context = null; }
}
