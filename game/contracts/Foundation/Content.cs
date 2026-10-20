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

/// <summary>Declared upper bounds for generated L0/L1 triangles and draw calls.</summary>
public sealed record ModuleBudget(int TrianglesL0, int TrianglesL1, int DrawCalls);
public sealed record PartBudget(int L0, int L1);
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
public sealed record LightEmitter(ChannelId Channel, Vector3 Position, PaletteToken Color, float Energy, float Range);
public sealed record Footprint2D(FeatureId Feature, PolygonRings Rings, float MinY = 0f, float MaxY = 0f);
public sealed record InspectionProbe(string Id, FeatureId Feature, Pose Pose, float Distance);

public interface IContentModule
{
    BakeInputDependencies BakeInputs { get; }
    BakeInputDependencies CollisionInputs { get; }
    IReadOnlyList<Claim> Claims { get; }
    ModuleBudget Budget { get; }
    /// <summary>Enumerates conservative occupied L0 cells on the 64-metre grid from source ranges, without generating products or querying terrain heights, regardless of requested rendering LOD.</summary>
    IEnumerable<TileKey> Tiles(BuildContext ctx);
    /// <summary>
    /// For L0-L3, tile is the level's index: floor(worldXZ / (64 &lt;&lt; (int)lod)), including negative coordinates.
    /// L4 is the module's entire-map product and always uses TileKey(0, 0).
    /// </summary>
    ContentOutput Build(BuildContext ctx, TileKey tile, LodLevel lod);
    /// <summary>Returns world-coordinate collision primitives. The kernel localizes them to the C tile exactly once.</summary>
    IReadOnlyList<Collider> Collide(BuildContext ctx, TileKey tile) => [];
    IReadOnlyList<ChannelDecl> Channels => [];
    IReadOnlyList<InspectionProbe> Probes => [];
}

public sealed class BuildContext
{
    private readonly Dictionary<(Type Type, object Key), Lazy<object?>> _memo = [];

    /// <summary>Remembers a pure derived value for this frozen context; factories execute once, including null results.</summary>
    public T Memo<T>(object key, Func<T> create)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(create);
        Lazy<object?> value;
        lock (_memo)
        {
            var entry = (typeof(T), key);
            if (!_memo.TryGetValue(entry, out value!))
            {
                value = new Lazy<object?>(() => create(), LazyThreadSafetyMode.ExecutionAndPublication);
                _memo.Add(entry, value);
            }
        }
        return (T)value.Value!;
    }

    public required IWorldModel World { get; init; }
    public required MapDescriptor Map { get; init; }
    public required uint WorldSeed { get; init; }
    /// <summary>Single immutable map-wide campus window allocation, prepared before window metadata or geometry. Null is for legacy/no-window fixtures only.</summary>
    public WindowNumberingSnapshot? WindowNumbering { get; init; }
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
    /// Empty preserves legacy contexts without open interiors. Workers must not fall back to the global registry.
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
    public Func<FeatureId, Aspects, WpId?> ClaimedBy { get; init; } = (_, _) => null;
    /// <summary>Caller-preloaded immutable bytes keyed by forward-slash paths relative to DataRoot; generators never perform IO.</summary>
    public IReadOnlyDictionary<string, ReadOnlyMemory<byte>> SourceInputs { get; init; } =
        new ReadOnlyDictionary<string, ReadOnlyMemory<byte>>(new Dictionary<string, ReadOnlyMemory<byte>>(StringComparer.Ordinal));
    /// <summary>Inputs relative to game/data, frozen once by the asset loader and shared across contexts. No IO or per-tile copying.</summary>
    public BuildInputSnapshot GlobalInputs { get; init; } = BuildInputSnapshot.Empty;
    /// <summary>Read-only map-filtered optional content capabilities. The caller validates exclusive channel claims before any painting.</summary>
    public IReadOnlyList<IGroundMaskContributor> GroundMaskContributors { get; init; } = [];
    /// <summary>Fingerprint of this producer and its shared generation dependencies, computed by the caller.</summary>
    public string CodeHash { get; init; } = "";
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

public sealed record MeshLod(float Error, int[] Index)
{
    /// <summary>Only removes complete independent items; never use for a continuous surface.</summary>
    public static MeshLod Prefix(float error, int[] index, int count)
    {
        ArgumentNullException.ThrowIfNull(index);
        if (count < 0 || count > index.Length || count % 3 != 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Index prefix must contain complete triangles.");
        return new(error, index[..count]);
    }
}

/// <summary>Input VF-A bits; packed colour-bank bits are owned by the encoder.</summary>
public static class VertexFlag
{
    public const byte Interior = 1;
    public const byte Outskirts = 2;
    public const byte Decal = 4;
    public const byte NoShadow = 8;
}

/// <summary>Unsigned-16-bit surface capacity and the generator's smaller batch target.</summary>
public static class GeometryVertexLimits
{
    public const int Maximum = ushort.MaxValue + 1;
    public const int Batch = 65000;

    public static void Require(int count)
    {
        if (count < 0 || count > Maximum) throw new ArgumentOutOfRangeException(nameof(count));
    }
}

/// <summary>
/// Construct the existing positional surface, set optional per-vertex arrays with an object initializer,
/// then call ValidateVertexData before packing. Positions/normals use 3 floats, UV/UV2 use 2,
/// and CUSTOM0/1/2 each use 4 floats per vertex. Arrays remain caller-owned and must not mutate while packing.
/// </summary>
public sealed record GeometryBundle(
    ToonRef Material,
    float[] Position,
    float[] Normal,
    int[] Index,
    float[]? Ao = null,
    float[]? Uv = null,
    float[]? Uv2 = null,
    IReadOnlyList<MeshLod>? Lods = null,
    VariantTag? Variant = null)
{
    public bool IgnoreOcclusionCulling { get; init; }
    /// <summary>Null uses ToonRef as before. Otherwise the host obtains the approved shader material and shares it per ShaderId within that host.</summary>
    public ShaderId? Shader { get; init; }
    /// <summary>Opt-in reflection-world proxy, mounted and released with the owning tile. Does not create engine instances in generators.</summary>
    public bool Reflect { get; init; }
    // VF-A COLOR RGBA8: logical colour encoded by PaletteEncoding, quantized Ao, Flags, Reserved.
    // Missing ColorIndices uses Material.Spec.Lit; missing Ao means 1; missing Flags and Reserved mean 0.
    /// <summary>Logical indices 0..511, one per vertex, validated against the complete catalog by the consumer.</summary>
    public ushort[]? ColorIndices { get; init; }
    /// <summary>Encoding-2 decal background indices 0..511, one per vertex, only on an all-decal surface.
    /// Requires Flags' decal bit on every vertex and Reserved=null; the packer writes A's low byte and B's bit 5.
    /// Glass/window types and all other Reserved meanings stay unchanged.</summary>
    public ushort[]? DecalBaseColorIndices { get; init; }
    // Input bit values: 1 interior, 2 outskirts, 4 decal, 8 no shadow. Upper input bits remain reserved.
    // Encoding-2 bank bits are produced by the packer, never supplied through this array.
    public byte[]? Flags { get; init; }
    // VF-B glass: WindowCardEncoding.TypeMask (low 7 bits) carries 0 unspecified; 1-15 W-CLS, W-LEC, W-COR,
    // W-STR, W-OFF, W-LAB, W-CLEAN, W-DORM, W-WC, W-CAN, W-SHOP, W-MED, W-GYM, W-POOL, W-MECH.
    // 16 = lobby card (closed entrance glass, WLD-INTERIORS). PhysicalCurtainFlag (0x80)
    // means this LOD actually contains a physical curtain; suppress only the card's window screen layer.
    // This occupies COLOR's fourth (Reserved) byte; non-glass surfaces retain their existing semantics.
    public byte[]? Reserved { get; init; }
    // VF-B glass: floor(Uv) is (building-grid column, row), fract(Uv) is the position in that cell.
    // A facade contributes ColumnStart + local column; blank bays and courtyard-ring bays count too.
    // Uv2.x is the shared building BaseId (an integer up to 2^24); Uv2.y = channel slot + 32 * TotalColumns.
    // Window ID = BaseId + row * TotalColumns + building-grid column. All building parts participate in
    // ordinal (FeatureId, part SourceId) numbering, including claimed buildings, so every producer agrees.
    // UV2 MUST retain fp32 components through packing/cache/GPU upload: half/UNorm16 loses integer IDs.
    // Flags bit 2 marks outskirts windows (dedicated IDs >= 65536), which skip window_override lookup.
    // Water shader payload instead uses Uv=(flow vx,vz) m/s, Uv2=(shore distance m, reflection lake or -1),
    // Flags bit 8 for no shadow, upward normals, and optional ColorIndices falling back to water.far.lit.
    // VF-C channels: card centre/h2, branch anchor/h3, cluster centre/reserved; all are xyzw per vertex.
    public float[]? Custom0 { get; init; }
    public float[]? Custom1 { get; init; }
    public float[]? Custom2 { get; init; }

    // With Material.Spec.SurfacePattern: Uv is continuous, directed/anchored metres and Uv2 is
    // (gate, fidelity K). Both UV2 components are constant on every base/LOD triangle and retain fp32.
    // No channel is reinterpreted when the optional profile is absent.

    /// <summary>Checks the current logical-index vertex layout; invalid data throws ArgumentException.</summary>
    public void ValidateVertexData()
    {
        ArgumentNullException.ThrowIfNull(Material.Spec);
        Material.Spec.ValidateSurfacePattern();
        ArgumentNullException.ThrowIfNull(Position);
        ArgumentNullException.ThrowIfNull(Normal);
        ArgumentNullException.ThrowIfNull(Index);
        if (Position.Length % 3 != 0) throw new ArgumentException("Position must have 3 floats per vertex.", nameof(Position));
        var count = Position.Length / 3;
        GeometryVertexLimits.Require(count);
        RequireLength(Normal, Position.Length, nameof(Normal));
        RequireLength(Ao, count, nameof(Ao));
        RequireLength(Uv, checked(count * 2), nameof(Uv));
        RequireLength(Uv2, checked(count * 2), nameof(Uv2));
        RequireLength(ColorIndices, count, nameof(ColorIndices));
        RequireLength(DecalBaseColorIndices, count, nameof(DecalBaseColorIndices));
        RequireLength(Flags, count, nameof(Flags));
        RequireLength(Reserved, count, nameof(Reserved));
        RequireLength(Custom0, checked(count * 4), nameof(Custom0));
        RequireLength(Custom1, checked(count * 4), nameof(Custom1));
        RequireLength(Custom2, checked(count * 4), nameof(Custom2));
        RequireFinite(Position, nameof(Position));
        RequireFinite(Normal, nameof(Normal));
        RequireFinite(Uv, nameof(Uv));
        RequireFinite(Uv2, nameof(Uv2));
        RequireFinite(Custom0, nameof(Custom0));
        RequireFinite(Custom1, nameof(Custom1));
        RequireFinite(Custom2, nameof(Custom2));
        if (Ao is not null && Ao.Any(value => !float.IsFinite(value) || value < 0f || value > 1f))
            throw new ArgumentException("Ao values must be finite and in [0, 1].", nameof(Ao));
        if (Flags is not null && Flags.Any(value => (value & 0xf0) != 0))
            throw new ArgumentException("Flags may only use interior=1, outskirts=2, decal=4 and no-shadow=8.", nameof(Flags));
        if (ColorIndices is not null && ColorIndices.Any(value => value >= PaletteLayout.Capacity))
            throw new ArgumentException("ColorIndices must be in [0, 511].", nameof(ColorIndices));
        if (DecalBaseColorIndices is not null)
        {
            if (Reserved is not null || Flags is null || Flags.Any(value => (value & VertexFlag.Decal) == 0))
                throw new ArgumentException("DecalBaseColorIndices requires an all-decal surface and Reserved=null.", nameof(DecalBaseColorIndices));
            if (DecalBaseColorIndices.Any(value => value >= PaletteLayout.Capacity))
                throw new ArgumentException("DecalBaseColorIndices must be in [0, 511].", nameof(DecalBaseColorIndices));
        }
        RequireIndices(Index, count, nameof(Index));
        if (Lods is not null)
            foreach (var lod in Lods)
            {
                if (lod is null || !float.IsFinite(lod.Error) || lod.Error < 0f)
                    throw new ArgumentException("Each mesh LOD needs a finite, non-negative error.", nameof(Lods));
                RequireIndices(lod.Index, count, nameof(Lods));
            }
        ValidateSurfacePatternVertices(count);
    }

    private void ValidateSurfacePatternVertices(int count)
    {
        if (Material.Spec.SurfacePattern is null) return;
        if (Shader is not null || Variant is not null ||
            Custom0 is not null || Custom1 is not null || Custom2 is not null || DecalBaseColorIndices is not null)
            throw new ArgumentException("Surface patterns cannot reinterpret shader, variant, decal or custom payloads.");
        if (Flags is not null && Flags.Any(value => (value & (VertexFlag.Interior | VertexFlag.Decal)) != 0))
            throw new ArgumentException("Surface patterns require ordinary outdoor, non-decal faces.", nameof(Flags));
        if (Uv is null || Uv2 is null)
            throw new ArgumentException("Surface patterns require metre UV and explicit gate/K UV2 on every vertex.");
        for (var vertex = 0; vertex < count; vertex++)
        {
            var gate = Uv2[vertex * 2];
            var zone = Uv2[vertex * 2 + 1];
            if (gate is not (0f or 1f) || zone < 0f || zone > 5f || zone != (int)zone ||
                (gate == 1f && zone is not (1f or 2f or 3f)))
                throw new ArgumentException("Surface UV2 requires gate=0/1, integer K=0..5; enabled faces require K1..K3.", nameof(Uv2));
        }
        CheckTriangles(Index);
        if (Lods is not null)
            foreach (var lod in Lods) CheckTriangles(lod.Index);

        void CheckTriangles(int[] indices)
        {
            for (var i = 0; i < indices.Length; i += 3)
            {
                var first = indices[i] * 2;
                for (var corner = 1; corner < 3; corner++)
                {
                    var other = indices[i + corner] * 2;
                    if (Uv2[first] != Uv2[other] || Uv2[first + 1] != Uv2[other + 1])
                        throw new ArgumentException("Surface gate/K must be constant on every base and LOD triangle.", nameof(Uv2));
                }
            }
        }
    }

    private static void RequireLength(Array? values, int expected, string name)
    {
        if (values is not null && values.Length != expected)
            throw new ArgumentException($"{name} has {values.Length} values; expected {expected}.", name);
    }
    private static void RequireFinite(float[]? values, string name)
    {
        if (values is not null && values.Any(value => !float.IsFinite(value)))
            throw new ArgumentException($"{name} contains a non-finite value.", name);
    }
    private static void RequireIndices(int[]? indices, int count, string name)
    {
        if (indices is null || indices.Length % 3 != 0 || indices.Any(index => index < 0 || index >= count))
            throw new ArgumentException($"{name} must contain complete triangles with indices in [0, {count}).", name);
    }
}

/// <summary>Local-coordinate mesh surfaces produced by one parameterized part.</summary>
public sealed record PartGeometry(IReadOnlyList<GeometryBundle> Meshes, Box3 Bounds);
