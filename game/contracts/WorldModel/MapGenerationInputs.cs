using System.Collections.ObjectModel;
using Campus.Contracts.Parts;

namespace Campus.Contracts;

/// <summary>A captured registration and its detached declarations; workers never consult a registry.</summary>
public sealed class ContentModuleInput(IContentModule value, string codeHash, string owner, bool global)
{
    public IContentModule Value { get; } = value;
    public string CodeHash { get; } = codeHash;
    public string Owner { get; } = owner;
    public bool Global { get; } = global;
    public IReadOnlyList<Claim> Claims { get; } = Array.AsReadOnly(value.Claims.ToArray());
}

/// <summary>Map-filtered CPU objects and frozen assets captured before worker preparation.</summary>
public sealed class MapGenerationInputs
{
    public MapGenerationInputs(PaletteCatalog catalog, IReadOnlyDictionary<string, ContentModuleInput> modules,
        PartLodCatalog partLods, IReadOnlyDictionary<string, IInteriorProvider> interiorProviders,
        IReadOnlyDictionary<string, string> interiorCodeHashes,
        BuildInputSnapshot globalInputs, IReadOnlyList<IGroundMaskContributor> groundMaskContributors,
        Func<PlacedPart, IReadOnlyList<Collider>> partCollision)
    {
        PartCollision = partCollision;
        Catalog = catalog;
        Modules = new ReadOnlyDictionary<string, ContentModuleInput>(new SortedDictionary<string, ContentModuleInput>(
            modules.ToDictionary(pair => pair.Key, pair => pair.Value), StringComparer.Ordinal));
        PartLods = partLods;
        InteriorProviders = new ReadOnlyDictionary<string, IInteriorProvider>(new SortedDictionary<string, IInteriorProvider>(
            interiorProviders.ToDictionary(pair => pair.Key, pair => pair.Value), StringComparer.Ordinal));
        InteriorCodeHashes = new ReadOnlyDictionary<string, string>(new SortedDictionary<string, string>(
            interiorCodeHashes.ToDictionary(pair => pair.Key, pair => pair.Value), StringComparer.Ordinal));
        GlobalInputs = globalInputs;
        GroundMaskContributors = Array.AsReadOnly(groundMaskContributors.ToArray());
    }

    public Func<PlacedPart, IReadOnlyList<Collider>> PartCollision { get; }
    public PaletteCatalog Catalog { get; }
    public IReadOnlyDictionary<string, ContentModuleInput> Modules { get; }
    public PartLodCatalog PartLods { get; }
    public IReadOnlyDictionary<string, IInteriorProvider> InteriorProviders { get; }
    public IReadOnlyDictionary<string, string> InteriorCodeHashes { get; }
    public BuildInputSnapshot GlobalInputs { get; }
    public IReadOnlyList<IGroundMaskContributor> GroundMaskContributors { get; }
}
