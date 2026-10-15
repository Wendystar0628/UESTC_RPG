using System.Numerics;
using Campus.Contracts.Maps;

namespace Campus.Contracts;

[Flags]
public enum Aspects { Massing = 1, Facade = 2, Roof = 4, Interior = 8, Ground = 16, Planting = 32, Props = 64 }
public abstract record Claim(Aspects Aspects);
public sealed record FeatureClaim(FeatureId Feature, Aspects Aspects) : Claim(Aspects);
public sealed record AreaClaim(ZoneId Zone, Aspects Aspects) : Claim(Aspects);

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ContentModuleAttribute(string id) : Attribute
{
    public string Id { get; } = id;
    public string Owner { get; set; } = "";
    public string Map { get; set; } = "";
}

/// <summary>Declared upper bounds for generated L0/L1 triangles and draw calls.</summary>
public sealed record ModuleBudget(int TrianglesL0, int TrianglesL1, int DrawCalls);
public sealed record PartBudget(int L0, int L1);
public sealed record LightEmitter(ChannelId Channel, Vector3 Position, PaletteToken Color, float Energy, float Range);
public sealed record Footprint2D(FeatureId Feature, PolygonRings Rings, float MinY = 0f, float MaxY = 0f);
public sealed record InspectionProbe(string Id, FeatureId Feature, Pose Pose, float Distance);

public interface IContentModule
{
    IReadOnlyList<Claim> Claims { get; }
    ModuleBudget Budget { get; }
    IEnumerable<TileKey> Tiles(BuildContext ctx);
    ContentOutput Build(BuildContext ctx, TileKey tile, LodLevel lod);
    IReadOnlyList<Collider> Collide(BuildContext ctx, TileKey tile) => [];
    IReadOnlyList<ChannelDecl> Channels => [];
    IReadOnlyList<InspectionProbe> Probes => [];
}

public sealed class BuildContext
{
    public required IWorldModel World { get; init; }
    public required MapDescriptor Map { get; init; }
    public required uint WorldSeed { get; init; }
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
    // VF-A COLOR RGBA8: ColorIds, quantized Ao, Flags, Reserved. Each byte array has one entry per vertex.
    // Missing ColorIds uses Material.Spec.Lit; missing Ao means 1; missing Flags and Reserved mean 0.
    public byte[]? ColorIds { get; init; }
    // Bit values: 1 interior, 2 outskirts, 4 decal, 8 no shadow. Upper four bits remain reserved.
    public byte[]? Flags { get; init; }
    public byte[]? Reserved { get; init; }
    // VF-C channels: card centre/h2, branch anchor/h3, cluster centre/reserved; all are xyzw per vertex.
    public float[]? Custom0 { get; init; }
    public float[]? Custom1 { get; init; }
    public float[]? Custom2 { get; init; }

    /// <summary>Checks array dimensions, finite values, AO range, flag bits, and triangle indices; throws ArgumentException on invalid data.</summary>
    public void ValidateVertexData()
    {
        ArgumentNullException.ThrowIfNull(Position);
        ArgumentNullException.ThrowIfNull(Normal);
        ArgumentNullException.ThrowIfNull(Index);
        if (Position.Length % 3 != 0) throw new ArgumentException("Position must have 3 floats per vertex.", nameof(Position));
        var count = Position.Length / 3;
        RequireLength(Normal, Position.Length, nameof(Normal));
        RequireLength(Ao, count, nameof(Ao));
        RequireLength(Uv, checked(count * 2), nameof(Uv));
        RequireLength(Uv2, checked(count * 2), nameof(Uv2));
        RequireLength(ColorIds, count, nameof(ColorIds));
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
        RequireIndices(Index, count, nameof(Index));
        if (Lods is not null)
            foreach (var lod in Lods)
            {
                if (lod is null || !float.IsFinite(lod.Error) || lod.Error < 0f)
                    throw new ArgumentException("Each mesh LOD needs a finite, non-negative error.", nameof(Lods));
                RequireIndices(lod.Index, count, nameof(Lods));
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
