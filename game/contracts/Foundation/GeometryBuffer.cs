using System.Numerics;

namespace Campus.Contracts;

/// <summary>Immutable CPU surface snapshot. Freeze mutable generation output once at the prototype/asset boundary, not once per tile.</summary>
/// <remarks>No engine resources are owned here. Consumers must not cast read-only memory back to mutable storage.</remarks>
public sealed class GeometryBuffer
{
    private GeometryBuffer(GeometryBundle source, int encoding)
    {
        source.ValidateVertexData(encoding);
        PaletteEncodingVersion = encoding;
        Material = source.Material; Shader = source.Shader;
        Position = source.Position.ToArray(); Normal = source.Normal.ToArray(); Index = source.Index.ToArray();
        Ao = Copy(source.Ao); Uv = Copy(source.Uv); Uv2 = Copy(source.Uv2);
        ColorIds = Copy(source.ColorIds); ColorIndices = Copy(source.ColorIndices);
        DecalBaseColorIndices = Copy(source.DecalBaseColorIndices); Flags = Copy(source.Flags); Reserved = Copy(source.Reserved);
        Custom0 = Copy(source.Custom0); Custom1 = Copy(source.Custom1); Custom2 = Copy(source.Custom2);
        Lods = Array.AsReadOnly((source.Lods ?? []).Select(lod => new GeometryLodBuffer(lod.Error, lod.Index)).ToArray());
        Variant = source.Variant; Reflect = source.Reflect; IgnoreOcclusionCulling = source.IgnoreOcclusionCulling;
    }

    public static GeometryBuffer Freeze(GeometryBundle source, int paletteEncodingVersion = PaletteLayout.LegacyEncodingVersion)
    {
        ArgumentNullException.ThrowIfNull(source);
        return new(source, paletteEncodingVersion);
    }

    public int PaletteEncodingVersion { get; }
    public ToonRef Material { get; }
    public ShaderId? Shader { get; }
    public ReadOnlyMemory<float> Position { get; }
    public ReadOnlyMemory<float> Normal { get; }
    public ReadOnlyMemory<int> Index { get; }
    public ReadOnlyMemory<float>? Ao { get; }
    public ReadOnlyMemory<float>? Uv { get; }
    public ReadOnlyMemory<float>? Uv2 { get; }
    public ReadOnlyMemory<byte>? ColorIds { get; }
    public ReadOnlyMemory<ushort>? ColorIndices { get; }
    public ReadOnlyMemory<ushort>? DecalBaseColorIndices { get; }
    public ReadOnlyMemory<byte>? Flags { get; }
    public ReadOnlyMemory<byte>? Reserved { get; }
    public ReadOnlyMemory<float>? Custom0 { get; }
    public ReadOnlyMemory<float>? Custom1 { get; }
    public ReadOnlyMemory<float>? Custom2 { get; }
    public IReadOnlyList<GeometryLodBuffer> Lods { get; }
    public VariantTag? Variant { get; }
    public bool Reflect { get; }
    public bool IgnoreOcclusionCulling { get; }
    private static ReadOnlyMemory<T>? Copy<T>(T[]? values) => values is null
        ? (ReadOnlyMemory<T>?)null : new ReadOnlyMemory<T>(values.ToArray());
}

public sealed class GeometryLodBuffer
{
    public GeometryLodBuffer(float error, ReadOnlyMemory<int> index)
    {
        if (!float.IsFinite(error) || error < 0f) throw new ArgumentOutOfRangeException(nameof(error));
        if (index.Length % 3 != 0) throw new ArgumentException("LOD indices must contain complete triangles.", nameof(index));
        foreach (int value in index.Span)
            if (value < 0) throw new ArgumentException("LOD indices must be nonnegative.", nameof(index));
        Error = error; Index = index.ToArray();
    }
    public float Error { get; }
    public ReadOnlyMemory<int> Index { get; }
}

/// <summary>One local-coordinate prototype shared by near buckets, shadow proxies and static tile instance streams.</summary>
/// <remarks>Surfaces are immutable and retained by reference. Their order fixes surface/material slot identity, not transparent GPU draw ordering.</remarks>
public sealed class GeometryPrototype
{
    public GeometryPrototype(string id, IReadOnlyList<GeometryBuffer> surfaces, Box3 bounds)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(surfaces);
        if (surfaces.Count == 0) throw new ArgumentException("A prototype requires at least one surface.", nameof(surfaces));
        foreach (var surface in surfaces) ArgumentNullException.ThrowIfNull(surface);
        if (!Finite(bounds.Min) || !Finite(bounds.Max) || bounds.Min.X > bounds.Max.X || bounds.Min.Y > bounds.Max.Y || bounds.Min.Z > bounds.Max.Z)
            throw new ArgumentException("Prototype bounds must be finite and ordered.", nameof(bounds));
        Id = id; Surfaces = Array.AsReadOnly(surfaces.ToArray()); Bounds = bounds;
    }
    public string Id { get; }
    public IReadOnlyList<GeometryBuffer> Surfaces { get; }
    public Box3 Bounds { get; }
    private static bool Finite(Vector3 p) => float.IsFinite(p.X) && float.IsFinite(p.Y) && float.IsFinite(p.Z);
}

/// <summary>One CPU-generated static stream. The prototype is shared; only this stream's mutable instance input is frozen once.</summary>
/// <remarks>Prototype coordinates are local. Instance transforms map them to world coordinates; the tile renderer adds no second tile translation.
/// Bounds is the world-space union including shader displacement. No engine visibility/shadow policy is encoded in CustomData.</remarks>
public sealed class GeometryInstanceStream
{
    public GeometryInstanceStream(string id, GeometryPrototype prototype, ReadOnlyMemory<float> data, Box3 bounds)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(prototype);
        if (data.Length % InstanceStreamLayout.FloatsPerInstance != 0)
            throw new ArgumentException("An instance stream must contain complete 16-float records.", nameof(data));
        foreach (float value in data.Span)
            if (!float.IsFinite(value)) throw new ArgumentException("Instance stream contains non-finite data.", nameof(data));
        if (!float.IsFinite(bounds.Min.X) || !float.IsFinite(bounds.Min.Y) || !float.IsFinite(bounds.Min.Z) ||
            !float.IsFinite(bounds.Max.X) || !float.IsFinite(bounds.Max.Y) || !float.IsFinite(bounds.Max.Z) ||
            bounds.Min.X > bounds.Max.X || bounds.Min.Y > bounds.Max.Y || bounds.Min.Z > bounds.Max.Z)
            throw new ArgumentException("Instance bounds must be finite and ordered.", nameof(bounds));
        Id = id; Prototype = prototype; Data = data.ToArray(); Bounds = bounds;
    }
    public string Id { get; }
    public GeometryPrototype Prototype { get; }
    public ReadOnlyMemory<float> Data { get; }
    public int Count => Data.Length / InstanceStreamLayout.FloatsPerInstance;
    public Box3 Bounds { get; }
}

public static class InstanceStreamLayout
{
    public const int FloatsPerInstance = 16;
    public const int BytesPerInstance = 64;
    /// <summary>Writes the existing renderer row-major 3x4 transform plus custom vec4; System.Numerics uses row-vector matrices.</summary>
    public static void Write(Span<float> target, Matrix4x4 at, Vector4 custom)
    {
        if (target.Length != FloatsPerInstance) throw new ArgumentException("Expected one complete instance record.", nameof(target));
        if (at.M14 != 0 || at.M24 != 0 || at.M34 != 0 || at.M44 != 1)
            throw new ArgumentException("An instance transform must be affine.", nameof(at));
        Span<float> record = stackalloc float[FloatsPerInstance]
        {
            at.M11, at.M21, at.M31, at.M41,
            at.M12, at.M22, at.M32, at.M42,
            at.M13, at.M23, at.M33, at.M43,
            custom.X, custom.Y, custom.Z, custom.W
        };
        foreach (float value in record)
            if (!float.IsFinite(value)) throw new ArgumentException("Instance values must be finite.", nameof(at));
        record.CopyTo(target);
    }
}
