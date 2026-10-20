using Campus.Gen.World;
using System.Globalization;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Vegetation;

namespace Campus.Gen.Vegetation;

/// <summary>Fixed tile payloads from the original source and its exact prepared context.</summary>

[BakeProducer("vegetation.runtime")]
public sealed class VegetationBakeProducer : IBakeProducer, IBakeContentConsumer
{
    public BakePayloadClass PayloadClass => BakePayloadClass.Product;
    public BakeProductKind ProductKind => BakeProductKind.Vegetation;
    public BakeInputDependencies Inputs(BakeKey key) => Source.Module.BakeInputs;
    public bool UsesModule(IContentModule module) => module is IVegetationRuntimeSource;

    private Lazy<IReadOnlyList<PreparedContentModule>>? _modules;

    public void Prepare(Lazy<IReadOnlyList<PreparedContentModule>> modules, LightInventoryDocument lighting) => _modules = modules;

    private PreparedContentModule Source =>
        (_modules ?? throw new InvalidOperationException("Vegetation sources are not prepared.")).Value
            .Single(m => m.Module is IVegetationRuntimeSource);

    public bool CanBuild(BakeKey key) => key.Module is BakeModuleIds.VegetationTrees or BakeModuleIds.VegetationGrass;

    public IReadOnlyList<BakeTileEntry> Tiles(BuildContext context)
    {
        var source = Source;
        var prepared = source.Context.Memo(context.World, () => source.Context.WithWorld(context.World));
        if (prepared.Map.Id != context.Map.Id) throw new InvalidOperationException("Vegetation directory map mismatch.");
        var producer = (IVegetationRuntimeSource)source.Module;
        var height = VegetationInputs.Species(prepared).Species.Max(s => s.HeightMax) * Planting.MaximumTreeScale;
        foreach (var feature in prepared.World.Features.Values)
            if (feature.Kind.Value == Planting.DesignTreeKind && feature.Height is { } authored) height = MathF.Max(height, authored);
        var entries = new List<BakeTileEntry>();
        foreach (var tile in source.Tiles)
            entries.Add(new(Key(prepared.Map.Id, BakeModuleIds.VegetationTrees, tile),
                Bounds(prepared, tile, height + VegetationPrototypes.SwayPad)));
        foreach (var tile in producer.GrassTiles(prepared))
            entries.Add(new(Key(prepared.Map.Id, BakeModuleIds.VegetationGrass, tile),
                Bounds(prepared, tile, GrassClumps.Reach(GrassStyle.Tall) * GrassField.MaximumScale + GrassClumps.SwayPad)));
        return entries.AsReadOnly();
    }

    public object Build(BakeKey key, BuildContext context, string inputHash)
    {
        if (!CanBuild(key) || key.Map != context.Map.Id) throw new ArgumentException("Invalid vegetation key.");
        var tile = Tile(key);
        var source = Source;
        if (source.Context.Map.Id != key.Map) throw new InvalidOperationException("Vegetation source map mismatch.");
        var producer = (IVegetationRuntimeSource)source.Module;
        var prepared = source.Context.Memo(context.World, () => source.Context.WithWorld(context.World));
        var mothers = key.Module == BakeModuleIds.VegetationTrees
            ? producer.PrepareTrees(prepared, tile) : producer.PrepareGrass(prepared, tile);
        return new VegetationTileContent(key, tile, mothers);
    }

    private static Box3 Bounds(BuildContext context, TileKey tile, float pad)
    {
        var origin = TileGrid.Origin(tile);
        return new(new Vector3(origin.X - pad, context.Map.Bounds.Min.Y - pad, origin.Y - pad),
            new Vector3(origin.X + TileGrid.BaseEdge + pad, context.Map.Bounds.Max.Y + pad, origin.Y + TileGrid.BaseEdge + pad));
    }

    private static BakeKey Key(MapId map, string module, TileKey tile) =>
        new(map, module, FormattableString.Invariant($"L0/{tile.X}/{tile.Z}"));

    private static TileKey Tile(BakeKey key)
    {
        var parts = key.Detail.Split('/');
        if (parts.Length != 3 || parts[0] != "L0" ||
            !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var x) ||
            !int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var z))
            throw new ArgumentException("Invalid vegetation tile key.");
        return new(x, z);
    }
}
