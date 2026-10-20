using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Bake;

namespace Campus.Kernel.Bake;

/// <summary>Render tile records and packed surfaces within a bounded physical part.</summary>
internal static class TileRenderSection
{
    internal const byte GeometryKind = 1;

    internal static void RenderTile(BinaryWriter writer, TileBundleParts.Part part)
    {
        TileBundleParts.Validate(part);
        var tile = part.Tile;
        writer.Write(TileRenderSection.GeometryKind); TileBinaryFields.Key(writer, tile.Key); TileBinaryFields.Json(writer, tile.Tile); writer.Write((byte)tile.Level); TileBinaryFields.Json(writer, tile.Bounds);
        TileBinaryFields.Json(writer, part.Location);
        writer.Write(tile.Surfaces.Count);
        foreach (var surface in tile.Surfaces) WriteSurface(writer, surface);
        writer.Write(tile.MultiMeshes.Count);
        foreach (var multi in tile.MultiMeshes)
        {
            WriteSurface(writer, multi.Surface); writer.Write(multi.InstanceCount); TileBinaryFields.Bytes(writer, multi.InstanceData); TileBinaryFields.Json(writer, multi.Bounds);
        }
        TileBinaryFields.Text(writer, tile.StatisticsJson);
    }

    internal static TileBundleParts.Part RenderTile(BinaryReader reader, BakeKey key)
    {
        var tile = TileBinaryFields.Json<TileKey>(reader);
        var level = (LodLevel)reader.ReadByte();
        if (!Enum.IsDefined(level)) throw new InvalidDataException("Unknown tile LOD.");
        var bounds = TileBinaryFields.Json<Box3>(reader);
        var location = TileBinaryFields.Json<TileBundleParts.Location>(reader);
        var surfaces = new BakedSurface[TileBinaryFields.Count(reader)];
        for (var i = 0; i < surfaces.Length; i++) surfaces[i] = Surface(reader);
        var multis = new BakedMultiMesh[TileBinaryFields.Count(reader)];
        for (var i = 0; i < multis.Length; i++)
        {
            var surface = Surface(reader); var count = reader.ReadInt32(); var data = TileBinaryFields.Bytes(reader); var box = TileBinaryFields.Json<Box3>(reader);
            multis[i] = new(surface, count, data, box);
        }
        return new(new(key, tile, level, bounds, surfaces, multis, TileBinaryFields.Statistics(reader)), location);
    }

    internal static void WriteSurface(BinaryWriter writer, BakedSurface surface)
    {
        TileBinaryFields.Json(writer, new SurfaceMetadata(surface.Material, surface.Format, surface.Primitive, surface.VertexCount,
            surface.IndexCount, surface.Bounds, surface.At, surface.UvScale, surface.Variant, surface.IgnoreOcclusionCulling, surface.Shader, surface.Reflect));
        TileBinaryFields.Bytes(writer, surface.VertexData); TileBinaryFields.Bytes(writer, surface.AttributeData); TileBinaryFields.Bytes(writer, surface.IndexData); TileBinaryFields.Bytes(writer, surface.SkinData);
        writer.Write(surface.Lods.Count);
        foreach (var lod in surface.Lods) { writer.Write(lod.Error); writer.Write(lod.IndexCount); TileBinaryFields.Bytes(writer, lod.IndexData); }
    }

    private static BakedSurface Surface(BinaryReader reader)
    {
        var meta = TileBinaryFields.Json<SurfaceMetadata>(reader);
        var vertices = TileBinaryFields.Bytes(reader); var attributes = TileBinaryFields.Bytes(reader); var indices = TileBinaryFields.Bytes(reader); var skin = TileBinaryFields.Bytes(reader);
        var lods = new BakedMeshLod[TileBinaryFields.Count(reader, 32)];
        for (var i = 0; i < lods.Length; i++) lods[i] = new(reader.ReadSingle(), reader.ReadInt32(), TileBinaryFields.Bytes(reader));
        if (meta.Shader is { } shader && !Enum.IsDefined(shader)) throw new InvalidDataException("Unknown surface shader ID.");
        return new(meta.Material, meta.Format, meta.Primitive, meta.VertexCount, meta.IndexCount,
            vertices, attributes, indices, meta.Bounds, meta.At, lods, skin, meta.UvScale, meta.Variant, meta.IgnoreOcclusionCulling)
        { Shader = meta.Shader, Reflect = meta.Reflect };
    }

    private sealed record SurfaceMetadata(ToonRef Material, ulong Format, BakedPrimitive Primitive, int VertexCount,
        int IndexCount, Box3 Bounds, Matrix4x4 At, Vector4 UvScale, VariantTag? Variant, bool IgnoreOcclusionCulling,
        ShaderId? Shader, bool Reflect);

}
