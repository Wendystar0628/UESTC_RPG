using System.Globalization;
using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Gen.Collision;
using Campus.Pipeline.Map;
using Campus.Kernel.Registry;
using Services = Campus.Kernel.Registry.Kernel;

namespace Campus.Kernel.Collision;

/// <summary>Snapshots content/part generators on the main thread. Interiors come only from each captured map context.</summary>
// Registration activates with the formal Boot composition.
[BakeProducer("collision.world", Owner = "WP-43")]
public sealed class CollisionBakeProducer : IBakeProducer
{
    public const string Module = "collision.world";
    private readonly (Registration Registration, object Instance)[] _sources;

    public CollisionBakeProducer()
    {
        var registry = Services.Services;
        // Interior instances belong to one MapBuild, not to the global service registry.
        _sources = registry.Entries.Where(r => r.Kind is "content" or "part")
            .OrderBy(r => r.Kind, StringComparer.Ordinal).ThenBy(r => r.Id, StringComparer.Ordinal)
            .Select(r => (r, registry.Create(r.Type))).ToArray();
    }

    public bool CanBuild(BakeKey key) => key.Module == Module && key.Detail.StartsWith("C/", StringComparison.Ordinal);

    public object Build(BakeKey key, BuildContext context)
    {
        if (key.Map != context.Map.Id || !CanBuild(key)) throw new ArgumentException("Collision bake key does not match its map.");
        var tile = ParseTile(key.Detail);
        var sources = _sources.Where(s => string.IsNullOrEmpty(s.Registration.Map) || s.Registration.Map == key.Map.Value).ToArray();
        var partCollision = new PartCollisionBinding(sources.Where(source => source.Registration.Kind == "part")
            .Select(source => new KeyValuePair<PartId, IPartDef>(new(source.Registration.Id), (IPartDef)source.Instance)));
        var builder = new CollisionTileBuilder(
            sources.Where(s => s.Registration.Kind == "content").Select(s => new CollisionModule(s.Registration.Id, (IContentModule)s.Instance)).ToArray(),
            partCollision.Collision);
        var colliders = builder.Build(context, tile);
        var origin = TileGrid.Origin(tile);
        var bounds = new Box3(new(origin.X, context.Map.Bounds.Min.Y, origin.Y), new(origin.X + TileGrid.BaseEdge, context.Map.Bounds.Max.Y, origin.Y + TileGrid.BaseEdge));
        return new CollisionTile(key, tile, bounds, colliders);
    }

    public static BakeKey Key(MapId map, TileKey tile) => new(map, Module, FormattableString.Invariant($"C/{tile.X}/{tile.Z}"));
    public static TileKey ParseTile(string detail)
    {
        var parts = detail.Split('/');
        if (parts.Length != 3 || parts[0] != "C" || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var x) ||
            !int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var z)) throw new ArgumentException("Invalid collision tile key.");
        return new(x, z);
    }
}
