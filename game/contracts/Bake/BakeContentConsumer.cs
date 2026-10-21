namespace Campus.Contracts.Bake;

/// <summary>One content producer with its immutable prepared inputs and exact L0 tile index.</summary>
public sealed record PreparedContentModule([property: DataId] string Id, IContentModule Module, BuildContext Context, IReadOnlyList<TileKey> Tiles);

/// <summary>One fixed auxiliary product and its complete frozen world-space influence bounds.</summary>
public sealed record BakeTileEntry(BakeKey Key, Box3 Bounds);

/// <summary>The single prepared map directory shared by baking and streaming.</summary>
public sealed record PreparedBakeTiles(
    IReadOnlyDictionary<string, IReadOnlyList<TileKey>> Modules,
    IReadOnlyList<BakeTileEntry> Auxiliary);

/// <summary>A bake producer that consumes the prepared map's original content producers.</summary>
public interface IBakeContentConsumer
{
    void Prepare(Lazy<IReadOnlyList<PreparedContentModule>> modules, LightInventoryDocument lighting);
}
