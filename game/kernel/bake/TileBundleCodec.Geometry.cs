using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Campus.Contracts;
using Campus.Contracts.Bake;

namespace Campus.Kernel.Bake;

public static partial class TileBundleCodec
{
    // Fixed CPU channels retain the original fp32/int values; only the small material metadata is JSON.
    private static void Geometry(BinaryWriter writer, GeometryBuffer surface, BakeKey key)
    {
        Json(writer, new GeometryMetadata(surface.Material, surface.Variant, surface.Shader,
            surface.Reflect, surface.IgnoreOcclusionCulling));
        Channel(writer, surface.Position.Span, key); Channel(writer, surface.Normal.Span, key); Channel(writer, surface.Index.Span, key);
        OptionalChannel(writer, surface.Ao, key); OptionalChannel(writer, surface.Uv, key); OptionalChannel(writer, surface.Uv2, key);
        OptionalChannel(writer, surface.ColorIndices, key); OptionalChannel(writer, surface.DecalBaseColorIndices, key);
        OptionalChannel(writer, surface.Flags, key); OptionalChannel(writer, surface.Reserved, key);
        OptionalChannel(writer, surface.Custom0, key); OptionalChannel(writer, surface.Custom1, key); OptionalChannel(writer, surface.Custom2, key);
        writer.Write(surface.Lods.Count);
        foreach (var lod in surface.Lods) { writer.Write(lod.Error); Channel(writer, lod.Index.Span, key); }
    }

    private static GeometryBundle Geometry(BinaryReader reader, BakeKey key)
    {
        var meta = Json<GeometryMetadata>(reader);
        var position = Channel<float>(reader, key); var normal = Channel<float>(reader, key); var indices = Channel<int>(reader, key);
        var ao = OptionalChannel<float>(reader, key); var uv = OptionalChannel<float>(reader, key); var uv2 = OptionalChannel<float>(reader, key);
        var colors = OptionalChannel<ushort>(reader, key); var decal = OptionalChannel<ushort>(reader, key);
        var flags = OptionalChannel<byte>(reader, key); var reserved = OptionalChannel<byte>(reader, key);
        var custom0 = OptionalChannel<float>(reader, key); var custom1 = OptionalChannel<float>(reader, key); var custom2 = OptionalChannel<float>(reader, key);
        var lods = new MeshLod[Count(reader, 32)];
        for (var i = 0; i < lods.Length; i++) lods[i] = new(reader.ReadSingle(), Channel<int>(reader, key));
        var surface = new GeometryBundle(meta.Material, position, normal, indices, ao, uv, uv2, lods, meta.Variant)
        {
            ColorIndices = colors, DecalBaseColorIndices = decal, Flags = flags, Reserved = reserved,
            Custom0 = custom0, Custom1 = custom1, Custom2 = custom2,
            Shader = meta.Shader, Reflect = meta.Reflect, IgnoreOcclusionCulling = meta.IgnoreOcclusionCulling
        };
        surface.ValidateVertexData();
        return surface;
    }

    private static void Channel<T>(BinaryWriter writer, ReadOnlySpan<T> values, BakeKey key) where T : unmanaged
    {
        var bytes = MemoryMarshal.AsBytes(values);
        var maximum = BakeCacheStore.MaximumPayload(key);
        if (bytes.Length > maximum) throw new InvalidDataException($"CPU channel {key.Map.Value}/{key.Module}/{key.Detail}: {bytes.Length} bytes exceeds limit {maximum}.");
        writer.Write(values.Length);
        writer.Write(bytes);
    }

    private static T[] Channel<T>(BinaryReader reader, BakeKey key) where T : unmanaged
    {
        var count = reader.ReadInt32();
        var bytes = (long)count * Unsafe.SizeOf<T>();
        var maximum = BakeCacheStore.MaximumPayload(key);
        var remaining = reader.BaseStream.Length - reader.BaseStream.Position;
        if (count < 0 || bytes > maximum || bytes > remaining)
            throw new InvalidDataException($"CPU channel {key.Map.Value}/{key.Module}/{key.Detail}: {bytes} bytes, remaining={remaining}, limit={maximum}.");
        var values = new T[count];
        reader.BaseStream.ReadExactly(MemoryMarshal.AsBytes(values.AsSpan()));
        return values;
    }

    private static void OptionalChannel<T>(BinaryWriter writer, ReadOnlyMemory<T>? values, BakeKey key) where T : unmanaged
    {
        writer.Write(values.HasValue);
        if (values is { } memory) Channel(writer, memory.Span, key);
    }
    private static T[]? OptionalChannel<T>(BinaryReader reader, BakeKey key) where T : unmanaged => reader.ReadBoolean() ? Channel<T>(reader, key) : null;

    private static void Box(BinaryWriter writer, Box3 bounds)
    {
        writer.Write(bounds.Min.X); writer.Write(bounds.Min.Y); writer.Write(bounds.Min.Z);
        writer.Write(bounds.Max.X); writer.Write(bounds.Max.Y); writer.Write(bounds.Max.Z);
    }
    private static Box3 Box(BinaryReader reader) => new(
        new(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()),
        new(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()));

    private sealed record GeometryMetadata(ToonRef Material, VariantTag? Variant, ShaderId? Shader,
        bool Reflect, bool IgnoreOcclusionCulling);
}
