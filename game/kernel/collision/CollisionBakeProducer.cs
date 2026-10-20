using Campus.Gen.World;
using System.Globalization;
using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Gen.Collision;
using Campus.Gen.BakeInput;
using Campus.Kernel.Registry;
using KernelServices = Campus.Kernel.Registry.Kernel;

namespace Campus.Kernel.Collision;

/// <summary>Consumes prepared map content and snapshots part generators on the main thread.</summary>
// Registration activates with the formal Boot composition.




[BakeProducer("collision.world")]
public sealed class CollisionBakeProducer : IBakeProducer, IBakeContentConsumer
{
    public bool UsesModule(IContentModule module) => true;
    public BakePayloadClass PayloadClass => BakePayloadClass.Tile;
    public BakeProductKind ProductKind => BakeProductKind.Collision;
    public BakeInputDependencies Inputs(BakeKey key) => BakeInputDependencies.World;

    public const string Module = "collision.world";
    private readonly (Registration Registration, object Instance)[] _parts;
    private Lazy<IReadOnlyList<PreparedContentModule>>? _modules;

    public CollisionBakeProducer()
    {
        var registry = KernelServices.Services;
        // Content, interior and producer instances are owned by the map scope.
        _parts = registry.Query<IPartDef>(RegistrationKind.Part)
            .Select(r => (r, registry.Create(r.Type))).ToArray();
    }

    public bool CanBuild(BakeKey key) => key.Module == Module && key.Detail.StartsWith("C/", StringComparison.Ordinal);

    public IReadOnlyList<BakeTileEntry> Tiles(BuildContext context) => [];

    public void Prepare(Lazy<IReadOnlyList<PreparedContentModule>> modules, LightInventoryDocument lighting) => _modules = modules;

    public object Build(BakeKey key, BuildContext context, string inputHash)
    {
        if (key.Map != context.Map.Id || !CanBuild(key)) throw new ArgumentException("Collision bake key does not match its map.");
        var tile = ParseTile(key.Detail);
        var sources = _parts.Where(s => string.IsNullOrEmpty(s.Registration.Map) || s.Registration.Map == key.Map.Value);
        var partCollision = new PartCollisionBinding(sources
            .Select(source => new KeyValuePair<PartId, IPartDef>(new(source.Registration.Id), (IPartDef)source.Instance)));
        var builder = new CollisionTileBuilder(
            (_modules ?? throw new InvalidOperationException("Collision content modules have not completed map preparation.")).Value,
            partCollision.Collision);
        var colliders = builder.Build(context, tile);
        var origin = TileGrid.Origin(tile);
        var bounds = new Box3(new(origin.X, context.Map.Bounds.Min.Y, origin.Y), new(origin.X + TileGrid.BaseEdge, context.Map.Bounds.Max.Y, origin.Y + TileGrid.BaseEdge));
        return new CollisionTile(key, tile, bounds, colliders);
    }

    public static BakeKey Key(MapId map, TileKey tile) => new(map, Module, FormattableString.Invariant($"C/{tile.X}/{tile.Z}"));
    public static TileKey ParseTile(string detail)
    {
        var parts = detail.AsSpan();
        var separator = parts.Length >= 2 ? parts[2..].IndexOf('/') + 2 : -1;
        if (parts.Length < 2 || parts[0] != 'C' || parts[1] != '/' || separator < 2 ||
            !int.TryParse(parts[2..separator], NumberStyles.Integer, CultureInfo.InvariantCulture, out var x) ||
            !int.TryParse(parts[(separator + 1)..], NumberStyles.Integer, CultureInfo.InvariantCulture, out var z))
            throw new ArgumentException("Invalid collision tile key.");
        return new(x, z);
    }
}
