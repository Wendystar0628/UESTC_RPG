using System.Numerics;
using System.Collections.ObjectModel;
using Campus.Contracts.Maps;

namespace Campus.Contracts;

[Flags]
public enum Aspects { Massing = 1, Facade = 2, Roof = 4, Interior = 8, Ground = 16, Planting = 32, Props = 64 }
public abstract record Claim(Aspects Aspects);
public sealed record FeatureClaim(FeatureId Feature, Aspects Aspects) : Claim(Aspects);
public sealed record AreaClaim(ZoneId Zone, Aspects Aspects) : Claim(Aspects);

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ContentModuleAttribute(string id) : RegistrationAttribute(RegistrationKind.Content, id, RegistrationLifetime.Map)
{ }

[Flags]
public enum BakeInputKinds
{
    None = 0, World = 1, Windows = 2, Vegetation = 4, Furniture = 8,
    Backdrop = 16, ReflectLakes = 32, OutskirtsBlocks = 64, Lighting = 128
}

/// <summary>Effective source dependencies declared by a CPU producer. File keys are relative to map data or game/data.</summary>
public sealed record BakeInputDependencies(BakeInputKinds Kinds, IReadOnlyList<string> MapFiles, IReadOnlyList<string> GlobalFiles)
{
    public static BakeInputDependencies World { get; } = new(BakeInputKinds.World, [], []);
}

public interface IContentModule
{
    BakeInputDependencies BakeInputs { get; }
    BakeInputDependencies CollisionInputs { get; }
    IReadOnlyList<Claim> Claims { get; }
    /// <summary>Enumerates conservative occupied L0 cells on the 64-metre grid from source ranges, without generating products or querying terrain heights, regardless of requested rendering LOD.</summary>
    IEnumerable<TileKey> Tiles(BuildContext ctx);
    /// <summary>
    /// For L0-L3, tile is the level's index: floor(worldXZ / (64 &lt;&lt; (int)lod)), including negative coordinates.
    /// L4 is the module's entire-map product and always uses TileKey(0, 0).
    /// </summary>
    ContentOutput Build(BuildContext ctx, TileKey tile, LodLevel lod);
    /// <summary>Returns world-coordinate collision primitives. The kernel localizes them to the C tile exactly once.</summary>
    IReadOnlyList<Collider> Collide(BuildContext ctx, TileKey tile) => [];
}

public sealed class BuildContext
{
    /// <summary>Remembers a pure derived value for this frozen context; factories execute once, including null results.</summary>
    public T Memo<T>(object key, Func<T> create) => DerivedMemo.ForOwner(this, key, create);

    public required IWorldModel World { get; init; }
    public required MapDescriptor Map { get; init; }
    public required uint WorldSeed { get; init; }
    /// <summary>Single immutable map-wide campus window allocation, prepared before window metadata or geometry; no-window contexts supply an explicit empty snapshot.</summary>
    public required WindowNumberingSnapshot WindowNumbering { get; init; }
    /// <summary>Final resident interior layouts restored with the map's cached metadata.</summary>
    public IReadOnlyDictionary<FeatureId, InteriorLayout> InteriorLayouts { get; init; } =
        new ReadOnlyDictionary<FeatureId, InteriorLayout>(new Dictionary<FeatureId, InteriorLayout>());
    /// <summary>Map-filtered immutable part LOD capabilities, captured before worker publication. Null means not supplied, never complete or universally supported.</summary>
    public Parts.PartLodCatalog? PartLods { get; init; }
    private IReadOnlyDictionary<string, IInteriorProvider> _interiorProviders =
        new ReadOnlyDictionary<string, IInteriorProvider>(new SortedDictionary<string, IInteriorProvider>(StringComparer.Ordinal));
    /// <summary>
    /// This map's isolated interior instances, keyed by original provider ID. The initializer copies and sorts the mapping.
    /// Configure providers before publishing the context; never reconfigure a provider while this map's jobs can still run.
    /// Maps without open interiors supply an empty mapping. Workers never fall back to the global registry.
    /// </summary>
    public IReadOnlyDictionary<string, IInteriorProvider> InteriorProviders
    {
        get => _interiorProviders;
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            var snapshot = new SortedDictionary<string, IInteriorProvider>(StringComparer.Ordinal);
            foreach (var pair in value)
            {
                if (string.IsNullOrWhiteSpace(pair.Key)) throw new ArgumentException("Interior provider IDs must be non-empty.", nameof(value));
                ArgumentNullException.ThrowIfNull(pair.Value);
                snapshot.Add(pair.Key, pair.Value);
            }
            _interiorProviders = new ReadOnlyDictionary<string, IInteriorProvider>(snapshot);
        }
    }
    /// <summary>Complete immutable palette snapshot shared by generation and packing; all entries belong in the bake fingerprint.</summary>
    public required PaletteCatalog Catalog { get; init; }
    /// <summary>
    /// Ownership query supplied from the current map's formally resolved claims ledger, after map filtering
    /// and conflict checks. Never infer ownership from module names, directories or feature-ID prefixes.
    /// Returns null when the requested feature/aspect has no registered owner.
    /// </summary>
    public Func<FeatureId, Aspects, string?> ClaimedBy { get; init; } = (_, _) => null;
    /// <summary>Caller-preloaded immutable bytes keyed by forward-slash paths relative to DataRoot; generators never perform IO.</summary>
    public IReadOnlyDictionary<string, ReadOnlyMemory<byte>> SourceInputs { get; init; } =
        new ReadOnlyDictionary<string, ReadOnlyMemory<byte>>(new Dictionary<string, ReadOnlyMemory<byte>>(StringComparer.Ordinal));
    /// <summary>Inputs relative to game/data, frozen once by the asset loader and shared across contexts. No IO or per-tile copying.</summary>
    public BuildInputSnapshot GlobalInputs { get; init; } = BuildInputSnapshot.Empty;
    /// <summary>Read-only map-filtered optional content capabilities. The caller validates exclusive channel claims before any painting.</summary>
    public IReadOnlyList<IGroundMaskContributor> GroundMaskContributors { get; init; } = [];
    /// <summary>Fingerprint of this producer and its shared generation dependencies, computed by the caller.</summary>
    public required string CodeHash { get; init; }
    /// <summary>Retains this producer's frozen inputs while using its prepared cold-asset world.</summary>
    public BuildContext WithWorld(IWorldModel world) => ReferenceEquals(World, world) ? this : new()
    {
        World = world, Map = Map, WorldSeed = WorldSeed, WindowNumbering = WindowNumbering, InteriorLayouts = InteriorLayouts,
        PartLods = PartLods, InteriorProviders = InteriorProviders, Catalog = Catalog, ClaimedBy = ClaimedBy,
        SourceInputs = SourceInputs, GlobalInputs = GlobalInputs, GroundMaskContributors = GroundMaskContributors,
        CodeHash = CodeHash
    };
    public Rng Seeded(FeatureId feature, PartId part, string purpose) =>
        new(StableHash.Hash32(WorldSeed, feature.Value, part.Value, purpose));
    public ToonRef Toon(ToonSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        return new(spec);
    }
}

public sealed record ContentOutput(
    IReadOnlyList<GeometryBundle> Meshes,
    IReadOnlyList<PlacedPart> Parts,
    IReadOnlyList<LightEmitter>? Emitters = null,
    IReadOnlyList<Footprint2D>? Occupancy = null)
{
    public static ContentOutput Empty { get; } = new([], []);
    /// <summary>Deterministic in-memory diagnostics. The collector validates and reports them separately from bake artifacts and hashes.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public IReadOnlyList<BuildDiagnostic> Diagnostics { get; init; } = [];
    /// <summary>Static instance streams sharing frozen prototypes. Consumers must pack/mount these or explicitly reject them.</summary>
    public IReadOnlyList<GeometryInstanceStream> Instances { get; init; } = [];
    public IReadOnlyList<EntranceDecl> Entrances { get; init; } = [];
    public IReadOnlyList<WindowFacadeDecl> Windows { get; init; } = [];
    public IReadOnlyList<WindowDecl> LooseWindows { get; init; } = [];
    /// <summary>Direct world-coordinate seat declarations sharing the collision source; no placement transform is pending.</summary>
    public IReadOnlyList<SeatDecl> Seats { get; init; } = [];
}
