namespace Campus.Contracts.Fauna;

public enum FaunaKind : byte { Cat, Pigeon, Sparrow, Swan, Crow }

/// <summary>Latched display states produced for each animal site.</summary>
public enum FaunaSet : byte { Day, Night, Rain }

/// <summary>Prepared geometry and the bounds shared by every display state of one site.</summary>
public sealed record FaunaSiteContent(string Key, FaunaKind Kind, Box3 Bounds, IReadOnlyList<ContentOutput> Sets);

public sealed record FaunaContent(IReadOnlyList<FaunaSiteContent> Sites, IReadOnlyList<string> Log);

/// <summary>Optional capability of the map's registered fauna content module.</summary>
public interface IFaunaContentSource : IContentModule
{
    FaunaContent PrepareFauna(BuildContext context);
}
