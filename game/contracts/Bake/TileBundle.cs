using System.Buffers.Binary;
using System.Numerics;
using System.Text.Json;

namespace Campus.Contracts.Bake;

/// <summary>CPU topology names. The engine adapter explicitly maps these to its own primitive enum.</summary>
public enum BakedPrimitive : byte { Points, Lines, LineStrip, Triangles, TriangleStrip }

public sealed class BakedMeshLod
{
    public BakedMeshLod(float error, int indexCount, ReadOnlyMemory<byte> indexData)
    {
        if (!float.IsFinite(error) || error < 0f) throw new ArgumentOutOfRangeException(nameof(error));
        BakeData.ValidateIndexLength(indexCount, indexData.Length);
        Error = error;
        IndexCount = indexCount;
        IndexData = indexData.ToArray();
    }
    public float Error { get; }
    public int IndexCount { get; }
    /// <summary>Little-endian unsigned 16-bit triangle indices. The constructor copies its input.</summary>
    public ReadOnlyMemory<byte> IndexData { get; }
}

/// <summary>
/// Packed CPU surface. Format is an opaque renderer ABI bitmask, never an engine object or resource ID.
/// Buffers and LOD lists are copied at construction and exposed read-only. Consumers must not cast away their read-only views.
/// Positions use the supplied shared quantization Bounds; At is applied after decoding (identity for world-coordinate surfaces).
/// </summary>
public sealed class BakedSurface
{
    public BakedSurface(ToonRef material, ulong format, BakedPrimitive primitive,
        int vertexCount, int indexCount, ReadOnlyMemory<byte> vertexData,
        ReadOnlyMemory<byte> attributeData, ReadOnlyMemory<byte> indexData, Box3 bounds,
        Matrix4x4? at = null, IReadOnlyList<BakedMeshLod>? lods = null,
        ReadOnlyMemory<byte> skinData = default, Vector4? uvScale = null,
        VariantTag? variant = null, bool ignoreOcclusionCulling = false)
    {
        ArgumentNullException.ThrowIfNull(material.Spec);
        if (!Enum.IsDefined(primitive)) throw new ArgumentOutOfRangeException(nameof(primitive));
        if (vertexCount is < 0 or > 65536) throw new ArgumentOutOfRangeException(nameof(vertexCount));
        if (vertexCount > 0 && vertexData.IsEmpty) throw new ArgumentException("A non-empty surface needs vertex bytes.", nameof(vertexData));
        BakeData.ValidateIndexLength(indexCount, indexData.Length);
        BakeData.ValidateIndices(indexData.Span, vertexCount);
        if (primitive == BakedPrimitive.Triangles && (indexCount == 0 ? vertexCount : indexCount) % 3 != 0)
            throw new ArgumentException("Triangle topology requires a complete set of triples.", nameof(indexCount));
        BakeData.ValidateBounds(bounds);
        var transform = at ?? Matrix4x4.Identity;
        BakeData.ValidateTransform(transform);
        var scale = uvScale ?? Vector4.One;
        if (!float.IsFinite(scale.X) || !float.IsFinite(scale.Y) || !float.IsFinite(scale.Z) || !float.IsFinite(scale.W))
            throw new ArgumentException("UV scale must be finite.", nameof(uvScale));
        var lodArray = (lods ?? []).ToArray();
        foreach (var lod in lodArray)
        {
            ArgumentNullException.ThrowIfNull(lod);
            if (primitive != BakedPrimitive.Triangles || lod.IndexCount % 3 != 0)
                throw new ArgumentException("Mesh LOD indices require complete triangle triples.", nameof(lods));
            BakeData.ValidateIndices(lod.IndexData.Span, vertexCount);
        }
        Material = material; Format = format; Primitive = primitive;
        VertexCount = vertexCount; IndexCount = indexCount;
        VertexData = vertexData.ToArray(); AttributeData = attributeData.ToArray();
        IndexData = indexData.ToArray(); SkinData = skinData.ToArray();
        Bounds = bounds; At = transform; UvScale = scale;
        Lods = Array.AsReadOnly(lodArray); Variant = variant;
        IgnoreOcclusionCulling = ignoreOcclusionCulling;
    }
    public ToonRef Material { get; }
    public ulong Format { get; }
    public BakedPrimitive Primitive { get; }
    public int VertexCount { get; }
    public int IndexCount { get; }
    public ReadOnlyMemory<byte> VertexData { get; }
    public ReadOnlyMemory<byte> AttributeData { get; }
    public ReadOnlyMemory<byte> SkinData { get; }
    public ReadOnlyMemory<byte> IndexData { get; }
    public Box3 Bounds { get; }
    public Matrix4x4 At { get; }
    /// <summary>Producer-supplied packed UV scale; defaults to identity for formats without scaled UVs.</summary>
    public Vector4 UvScale { get; }
    public IReadOnlyList<BakedMeshLod> Lods { get; }
    public VariantTag? Variant { get; }
    public bool IgnoreOcclusionCulling { get; }
    public long ByteCount => (long)VertexData.Length + AttributeData.Length + SkinData.Length + IndexData.Length + Lods.Sum(lod => (long)lod.IndexData.Length);
}

/// <summary>
/// One packed prototype surface and a static instance stream. Each 64-byte record is 16 little-endian float32 values:
/// renderer Transform3D's 12 values followed by four custom-data values. No per-instance colour block is stored.
/// Prototype At must be identity; transforms are already supplied by the instance stream.
/// </summary>
public sealed class BakedMultiMesh
{
    public const int InstanceStrideBytes = 64;
    public BakedMultiMesh(BakedSurface surface, int instanceCount, ReadOnlyMemory<byte> instanceData, Box3 bounds)
    {
        ArgumentNullException.ThrowIfNull(surface);
        if (surface.At != Matrix4x4.Identity) throw new ArgumentException("MultiMesh prototype At must be identity.", nameof(surface));
        if (instanceCount < 0 || (long)instanceCount * InstanceStrideBytes != instanceData.Length)
            throw new ArgumentException("Instance data must contain exactly 64 bytes per instance.", nameof(instanceData));
        for (var offset = 0; offset < instanceData.Length; offset += sizeof(float))
            if (!float.IsFinite(BinaryPrimitives.ReadSingleLittleEndian(instanceData.Span.Slice(offset, sizeof(float)))))
                throw new ArgumentException("Instance data contains a non-finite float.", nameof(instanceData));
        BakeData.ValidateBounds(bounds);
        Surface = surface; InstanceCount = instanceCount;
        InstanceData = instanceData.ToArray(); Bounds = bounds;
    }
    public BakedSurface Surface { get; }
    public int InstanceCount { get; }
    public ReadOnlyMemory<byte> InstanceData { get; }
    public Box3 Bounds { get; }
}

/// <summary>Stored geometry counts and raw packed-buffer bytes, before file headers, statistics JSON and compression.</summary>
public sealed record TileBundleStats(long Vertices, long Triangles, int Surfaces, long Bytes, long Instances);

/// <summary>
/// One module's CPU render product for one tile and semantic LOD. All applicable modules are aggregated by the streamer.
/// L0-L3 tile coordinates are floor(worldXZ / (64 &lt;&lt; level)); the L4 entire-map product uses (0, 0).
/// Parts are expanded before construction; no object Params, engine resources, handles or ownership callbacks are stored.
/// Collision level C uses CollisionTile separately. This value does not require Dispose.
/// </summary>
public sealed class TileBundle
{
    public TileBundle(BakeKey key, TileKey tile, LodLevel level, Box3 bounds,
        IReadOnlyList<BakedSurface> surfaces, IReadOnlyList<BakedMultiMesh>? multiMeshes = null,
        string statisticsJson = "{}")
    {
        ArgumentNullException.ThrowIfNull(surfaces);
        if (!Enum.IsDefined(level)) throw new ArgumentOutOfRangeException(nameof(level));
        if (level == LodLevel.L4 && tile != new TileKey(0, 0))
            throw new ArgumentException("L4 is an entire-map product and must use TileKey(0, 0).", nameof(tile));
        BakeData.ValidateKey(key); BakeData.ValidateBounds(bounds); BakeData.ValidateStatistics(statisticsJson);
        var surfaceArray = surfaces.ToArray();
        var instanceArray = (multiMeshes ?? []).ToArray();
        foreach (var surface in surfaceArray) ArgumentNullException.ThrowIfNull(surface);
        foreach (var instances in instanceArray) ArgumentNullException.ThrowIfNull(instances);
        Key = key; Tile = tile; Level = level; Bounds = bounds;
        Surfaces = Array.AsReadOnly(surfaceArray); MultiMeshes = Array.AsReadOnly(instanceArray);
        StatisticsJson = statisticsJson;
        var allSurfaces = surfaceArray.Concat(instanceArray.Select(instances => instances.Surface)).ToArray();
        Stats = new(allSurfaces.Sum(surface => (long)surface.VertexCount), allSurfaces.Sum(TriangleCount),
            allSurfaces.Length,
            allSurfaces.Sum(surface => surface.ByteCount) + instanceArray.Sum(instances => (long)instances.InstanceData.Length),
            instanceArray.Sum(instances => (long)instances.InstanceCount));
    }
    public BakeKey Key { get; }
    public TileKey Tile { get; }
    public LodLevel Level { get; }
    public Box3 Bounds { get; }
    public IReadOnlyList<BakedSurface> Surfaces { get; }
    public IReadOnlyList<BakedMultiMesh> MultiMeshes { get; }
    public TileBundleStats Stats { get; }
    public string StatisticsJson { get; }

    private static long TriangleCount(BakedSurface surface)
    {
        var count = surface.IndexCount == 0 ? surface.VertexCount : surface.IndexCount;
        return surface.Primitive switch
        {
            BakedPrimitive.Points or BakedPrimitive.Lines or BakedPrimitive.LineStrip => 0,
            BakedPrimitive.Triangles => count / 3,
            BakedPrimitive.TriangleStrip => Math.Max(0, count - 2),
            _ => throw new ArgumentOutOfRangeException(nameof(surface))
        };
    }
}

/// <summary>
/// Independent collision level C product. Existing collider records contain mutable arrays, so construction and
/// Colliders reads take defensive snapshots. A consumer should retain one returned snapshot while mounting it.
/// Collider parameters are local to the 64-metre collision cell (SYS-WALK section 13).
/// Its body origin is (Tile.X * 64, 0, Tile.Z * 64); Bounds is world metadata and is not the origin.
/// </summary>
public sealed class CollisionTile
{
    private readonly Collider[] _colliders;
    public CollisionTile(BakeKey key, TileKey tile, Box3 bounds, IReadOnlyList<Collider> colliders, string statisticsJson = "{}")
    {
        ArgumentNullException.ThrowIfNull(colliders);
        BakeData.ValidateKey(key); BakeData.ValidateBounds(bounds); BakeData.ValidateStatistics(statisticsJson);
        Key = key; Tile = tile; Bounds = bounds;
        _colliders = colliders.Select(BakeData.CopyCollider).ToArray();
        StatisticsJson = statisticsJson;
    }
    public BakeKey Key { get; }
    public TileKey Tile { get; }
    public Box3 Bounds { get; }
    public int ColliderCount => _colliders.Length;
    public IReadOnlyList<Collider> Colliders => Array.AsReadOnly(_colliders.Select(BakeData.CopyCollider).ToArray());
    public string StatisticsJson { get; }
}

internal static class BakeData
{
    internal static void ValidateKey(BakeKey key)
    {
        if (string.IsNullOrWhiteSpace(key.Map.Value) || string.IsNullOrWhiteSpace(key.Module) || string.IsNullOrWhiteSpace(key.Detail))
            throw new ArgumentException("BakeKey needs a map, module and detail.", nameof(key));
    }
    internal static void ValidateBounds(Box3 bounds)
    {
        var min = bounds.Min; var max = bounds.Max;
        if (!float.IsFinite(min.X) || !float.IsFinite(min.Y) || !float.IsFinite(min.Z) ||
            !float.IsFinite(max.X) || !float.IsFinite(max.Y) || !float.IsFinite(max.Z) ||
            min.X > max.X || min.Y > max.Y || min.Z > max.Z)
            throw new ArgumentException("Bounds must be finite and ordered.", nameof(bounds));
    }
    internal static void ValidateTransform(Matrix4x4 at)
    {
        float[] values = [at.M11, at.M12, at.M13, at.M14, at.M21, at.M22, at.M23, at.M24,
            at.M31, at.M32, at.M33, at.M34, at.M41, at.M42, at.M43, at.M44];
        if (values.Any(value => !float.IsFinite(value))) throw new ArgumentException("Transform must be finite.", nameof(at));
    }
    internal static void ValidateIndexLength(int indexCount, int length)
    {
        if (indexCount < 0 || (long)indexCount * sizeof(ushort) != length)
            throw new ArgumentException("Index data must contain exactly two bytes per unsigned 16-bit index.", nameof(indexCount));
    }
    internal static void ValidateIndices(ReadOnlySpan<byte> bytes, int vertexCount)
    {
        for (var offset = 0; offset < bytes.Length; offset += sizeof(ushort))
            if (BinaryPrimitives.ReadUInt16LittleEndian(bytes.Slice(offset, sizeof(ushort))) >= vertexCount)
                throw new ArgumentException("An index is outside the surface vertex range.", nameof(bytes));
    }
    internal static void ValidateStatistics(string statisticsJson)
    {
        ArgumentNullException.ThrowIfNull(statisticsJson);
        using var document = JsonDocument.Parse(statisticsJson);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
            throw new ArgumentException("StatisticsJson must be a JSON object.", nameof(statisticsJson));
    }
    internal static Collider CopyCollider(Collider collider)
    {
        ArgumentNullException.ThrowIfNull(collider);
        return collider switch
        {
            WallCollider wall => wall with { },
            RampCollider ramp => ramp with { },
            PostCollider post => post with { },
            BoxCollider box => box with { },
            SlabCollider slab => slab with { Ring = Array.AsReadOnly(slab.Ring.ToArray()) },
            HeightFieldCollider height => height with
            {
                HeightsCm = height.HeightsCm.ToArray(),
                FootMatHalfMeter = height.FootMatHalfMeter?.ToArray(),
                Patches = height.Patches is null ? null : Array.AsReadOnly(height.Patches.ToArray())
            },
            SlopedWallCollider slope => slope with { },
            _ => throw new ArgumentException($"Unsupported collider type '{collider.GetType().Name}'.", nameof(collider))
        };
    }
}
