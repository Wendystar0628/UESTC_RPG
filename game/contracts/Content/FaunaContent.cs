using Campus.Contracts.Bake;

namespace Campus.Contracts.Fauna;

public enum FaunaKind : byte { Cat, Pigeon, Sparrow, Swan, Crow }

/// <summary>Latched display states produced for each animal site.</summary>
public enum FaunaSet : byte { Day, Night, Rain }

/// <summary>Prepared geometry and the bounds shared by every display state of one site.</summary>
public sealed record FaunaSiteContent(string Key, FaunaKind Kind, Box3 Bounds, IReadOnlyList<ContentOutput> Sets);

public sealed record FaunaContent(IReadOnlyList<FaunaSiteContent> Sites, IReadOnlyList<string> Log);

public sealed record FaunaTileContent(BakeKey Key, TileKey Tile, FaunaContent Content);

/// <summary>Optional capability of the map's registered fauna content module.</summary>
public interface IFaunaContentSource : IContentModule
{
    /// <summary>Unique owner tiles with the union of their sites' complete Day/Night/Rain bounds.</summary>
    IReadOnlyDictionary<TileKey, Box3> FaunaTiles(BuildContext context);
    FaunaContent PrepareFauna(BuildContext context, TileKey tile);
}
