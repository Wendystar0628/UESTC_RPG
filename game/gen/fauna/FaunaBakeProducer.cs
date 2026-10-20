using Campus.Gen.World;
using System.Globalization;
using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Fauna;

namespace Campus.Gen.Fauna;

/// <summary>Produces only the requested owner tile from the registered source's frozen inputs.</summary>


[BakeProducer(BakeModuleIds.FaunaRuntime, Owner = "WP-25")]
public sealed class FaunaBakeProducer : IBakeProducer, IBakeContentConsumer
{
    public BakePayloadClass PayloadClass => BakePayloadClass.Product;
    public BakeProductKind ProductKind => BakeProductKind.Fauna;
    public BakeInputDependencies Inputs(BakeKey key) => Source.Module.BakeInputs;
    public bool UsesModule(IContentModule module) => module is IFaunaContentSource;

    private Lazy<IReadOnlyList<PreparedContentModule>>? _modules;

    public void Prepare(Lazy<IReadOnlyList<PreparedContentModule>> modules, LightInventoryDocument lighting) => _modules = modules;

    private PreparedContentModule Source =>
        (_modules ?? throw new InvalidOperationException("Fauna sources are not prepared.")).Value
            .Single(module => module.Module is IFaunaContentSource);

    public bool CanBuild(BakeKey key) => key.Module == BakeModuleIds.FaunaRuntime;

    public IReadOnlyList<BakeTileEntry> Tiles(BuildContext context)
    {
        var source = Source;
        if (source.Context.Map.Id != context.Map.Id) throw new ArgumentException("Fauna directory map mismatch.");
        return ((IFaunaContentSource)source.Module).FaunaTiles(source.Context)
            .OrderBy(pair => pair.Key.X).ThenBy(pair => pair.Key.Z)
            .Select(pair => new BakeTileEntry(Key(context.Map.Id, pair.Key), pair.Value)).ToArray();
    }

    public object Build(BakeKey key, BuildContext context)
    {
        if (!CanBuild(key) || key.Map != context.Map.Id) throw new ArgumentException("Invalid fauna bake key.");
        var tile = Tile(key);
        var source = Source;
        if (source.Context.Map.Id != key.Map) throw new ArgumentException("Fauna source map mismatch.");
        var content = ((IFaunaContentSource)source.Module).PrepareFauna(source.Context.WithWorld(context.World), tile);
        return new FaunaTileContent(key, tile, content);
    }

    private static BakeKey Key(MapId map, TileKey tile) =>
        new(map, BakeModuleIds.FaunaRuntime, FormattableString.Invariant($"L0/{tile.X}/{tile.Z}"));

    private static TileKey Tile(BakeKey key)
    {
        var parts = key.Detail.Split('/');
        if (parts.Length != 3 || parts[0] != "L0" ||
            !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var x) ||
            !int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var z) ||
            x is < -512 or > 511 || z is < -512 or > 511)
            throw new ArgumentException("Invalid fauna tile key.");
        return new(x, z);
    }
}
