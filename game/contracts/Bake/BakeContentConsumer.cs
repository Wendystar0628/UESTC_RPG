namespace Campus.Contracts.Bake;

/// <summary>One content producer with its immutable prepared inputs and exact L0 tile index.</summary>
public sealed record PreparedContentModule(string Id, IContentModule Module, BuildContext Context, IReadOnlyList<TileKey> Tiles);

/// <summary>A bake producer that consumes the prepared map's original content producers.</summary>
public interface IBakeContentConsumer
{
    void Prepare(Lazy<IReadOnlyList<PreparedContentModule>> modules);
}
