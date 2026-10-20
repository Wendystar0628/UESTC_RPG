using Campus.Contracts;
using Campus.Contracts.Bake;

namespace Campus.Kernel.Bake;

/// <summary>CPU geometry buffers and prototypes with their bounds and instance-value checks.</summary>
internal static class TileGeometrySection
{
    internal static void Prototype(BinaryWriter writer, GeometryPrototype prototype, BakeKey key)
    {
        TileBinaryFields.Text(writer, prototype.Id); TileBinaryFields.Box(writer, prototype.Bounds); writer.Write(prototype.Surfaces.Count);
        foreach (var surface in prototype.Surfaces) Geometry(writer, surface, key);
    }

    internal static GeometryPrototype Prototype(BinaryReader reader, BakeKey key)
    {
        var id = TileBinaryFields.Text(reader, 1024); var bounds = TileBinaryFields.Box(reader);
        var surfaces = new GeometryBuffer[TileBinaryFields.Count(reader)];
        for (var i = 0; i < surfaces.Length; i++)
        {
            surfaces[i] = GeometryBuffer.Freeze(Geometry(reader, key));
        }
        return new(id, surfaces, bounds);
    }

    internal static void Bounds(Box3 bounds)
    {
        if (!VectorMath.Finite(bounds.Min) || !VectorMath.Finite(bounds.Max) ||
            bounds.Min.X > bounds.Max.X || bounds.Min.Y > bounds.Max.Y || bounds.Min.Z > bounds.Max.Z)
            throw new InvalidDataException("Invalid auxiliary bounds.");
    }

    internal static void Finite(ReadOnlySpan<float> values)
    {
        foreach (var value in values)
            if (!float.IsFinite(value)) throw new InvalidDataException("Non-finite auxiliary instance record.");
    }

    internal static void Geometry(BinaryWriter writer, GeometryBuffer surface, BakeKey key)
    {
        TileBinaryFields.Json(writer, new GeometryMetadata(surface.Material, surface.Variant, surface.Shader,
            surface.Reflect, surface.IgnoreOcclusionCulling));
        TileBinaryFields.Channel(writer, surface.Position.Span, key); TileBinaryFields.Channel(writer, surface.Normal.Span, key); TileBinaryFields.Channel(writer, surface.Index.Span, key);
        TileBinaryFields.OptionalChannel(writer, surface.Ao, key); TileBinaryFields.OptionalChannel(writer, surface.Uv, key); TileBinaryFields.OptionalChannel(writer, surface.Uv2, key);
        TileBinaryFields.OptionalChannel(writer, surface.ColorIndices, key); TileBinaryFields.OptionalChannel(writer, surface.DecalBaseColorIndices, key);
        TileBinaryFields.OptionalChannel(writer, surface.Flags, key); TileBinaryFields.OptionalChannel(writer, surface.Reserved, key);
        TileBinaryFields.OptionalChannel(writer, surface.Custom0, key); TileBinaryFields.OptionalChannel(writer, surface.Custom1, key); TileBinaryFields.OptionalChannel(writer, surface.Custom2, key);
        writer.Write(surface.Lods.Count);
        foreach (var lod in surface.Lods) { writer.Write(lod.Error); TileBinaryFields.Channel(writer, lod.Index.Span, key); }
    }

    internal static GeometryBundle Geometry(BinaryReader reader, BakeKey key)
    {
        var meta = TileBinaryFields.Json<GeometryMetadata>(reader);
        var position = TileBinaryFields.Channel<float>(reader, key); var normal = TileBinaryFields.Channel<float>(reader, key); var indices = TileBinaryFields.Channel<int>(reader, key);
        var ao = TileBinaryFields.OptionalChannel<float>(reader, key); var uv = TileBinaryFields.OptionalChannel<float>(reader, key); var uv2 = TileBinaryFields.OptionalChannel<float>(reader, key);
        var colors = TileBinaryFields.OptionalChannel<ushort>(reader, key); var decal = TileBinaryFields.OptionalChannel<ushort>(reader, key);
        var flags = TileBinaryFields.OptionalChannel<byte>(reader, key); var reserved = TileBinaryFields.OptionalChannel<byte>(reader, key);
        var custom0 = TileBinaryFields.OptionalChannel<float>(reader, key); var custom1 = TileBinaryFields.OptionalChannel<float>(reader, key); var custom2 = TileBinaryFields.OptionalChannel<float>(reader, key);
        var lods = new MeshLod[TileBinaryFields.Count(reader, 32)];
        for (var i = 0; i < lods.Length; i++) lods[i] = new(reader.ReadSingle(), TileBinaryFields.Channel<int>(reader, key));
        var surface = new GeometryBundle(meta.Material, position, normal, indices, ao, uv, uv2, lods, meta.Variant)
        {
            ColorIndices = colors, DecalBaseColorIndices = decal, Flags = flags, Reserved = reserved,
            Custom0 = custom0, Custom1 = custom1, Custom2 = custom2,
            Shader = meta.Shader, Reflect = meta.Reflect, IgnoreOcclusionCulling = meta.IgnoreOcclusionCulling
        };
        surface.ValidateVertexData();
        return surface;
    }

    private sealed record GeometryMetadata(ToonRef Material, VariantTag? Variant, ShaderId? Shader,
        bool Reflect, bool IgnoreOcclusionCulling);

}
