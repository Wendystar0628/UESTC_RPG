using Campus.Contracts.Bake;
using Godot;

namespace Campus.Kernel.Stream;

/// <summary>Checks raw packed-buffer lengths against this engine ABI before native upload.</summary>
internal static class PackedSurfaceValidation
{
    public static void Validate(BakedSurface surface)
    {
        var format = (RenderingServer.ArrayFormat)surface.Format;
        var versionMask = (ulong)RenderingServer.ArrayFormat.FlagFormatVersionMask <<
            (int)RenderingServer.ArrayFormat.FlagFormatVersionShift;
        if ((surface.Format & versionMask) != (ulong)RenderingServer.ArrayFormat.FlagFormatCurrentVersion)
            throw new InvalidDataException("Packed surface uses an unsupported renderer format version.");
        var customBits = (ulong)RenderingServer.ArrayFormat.FormatCustomMask;
        var customMask = (customBits << (int)RenderingServer.ArrayFormat.FormatCustom0Shift) |
            (customBits << (int)RenderingServer.ArrayFormat.FormatCustom1Shift) |
            (customBits << (int)RenderingServer.ArrayFormat.FormatCustom2Shift);
        var allowed = RenderingServer.ArrayFormat.FormatVertex | RenderingServer.ArrayFormat.FormatNormal |
            RenderingServer.ArrayFormat.FormatTangent | RenderingServer.ArrayFormat.FormatColor |
            RenderingServer.ArrayFormat.FormatTexUV | RenderingServer.ArrayFormat.FormatTexUV2 |
            RenderingServer.ArrayFormat.FormatCustom0 | RenderingServer.ArrayFormat.FormatCustom1 |
            RenderingServer.ArrayFormat.FormatCustom2 | RenderingServer.ArrayFormat.FormatIndex |
            RenderingServer.ArrayFormat.FlagCompressAttributes | RenderingServer.ArrayFormat.FlagFormatCurrentVersion;
        if ((surface.Format & ~((ulong)allowed | customMask)) != 0)
            throw new InvalidDataException("Packed surface contains flags outside the static tile ABI.");
        var unsupported = RenderingServer.ArrayFormat.FlagUse2DVertices | RenderingServer.ArrayFormat.FlagUsesEmptyVertexArray |
            RenderingServer.ArrayFormat.FormatBones | RenderingServer.ArrayFormat.FormatWeights;
        if ((format & RenderingServer.ArrayFormat.FormatVertex) == 0 || (format & unsupported) != 0)
            throw new InvalidDataException("Static tile surface requires 3D positions and must not contain skinning.");
        if (((format & RenderingServer.ArrayFormat.FormatIndex) != 0) != (surface.IndexCount != 0))
            throw new InvalidDataException("Packed surface index flag and count disagree.");
        if ((format & RenderingServer.ArrayFormat.FormatTexUV2) != 0 &&
            (format & RenderingServer.ArrayFormat.FlagCompressAttributes) != 0)
            throw new InvalidDataException("Tile UV2 IDs require the uncompressed float32 VF-B layout.");
        ValidateCustom(format, RenderingServer.ArrayFormat.FormatCustom0, RenderingServer.ArrayFormat.FormatCustom0Shift);
        ValidateCustom(format, RenderingServer.ArrayFormat.FormatCustom1, RenderingServer.ArrayFormat.FormatCustom1Shift);
        ValidateCustom(format, RenderingServer.ArrayFormat.FormatCustom2, RenderingServer.ArrayFormat.FormatCustom2Shift);

        var vertices = (long)surface.VertexCount;
        var vertexStride = RenderingServer.MeshSurfaceGetFormatVertexStride(format, surface.VertexCount);
        var normalStride = RenderingServer.MeshSurfaceGetFormatNormalTangentStride(format, surface.VertexCount);
        RequireLength(surface.VertexData.Length, checked(vertices * ((long)vertexStride + normalStride)), "vertex/normal");
        RequireLength(surface.AttributeData.Length, checked(vertices * RenderingServer.MeshSurfaceGetFormatAttributeStride(format, surface.VertexCount)), "attribute");
        RequireLength(surface.SkinData.Length, checked(vertices * RenderingServer.MeshSurfaceGetFormatSkinStride(format, surface.VertexCount)), "skin");
        if (surface.IndexCount > 0 && RenderingServer.MeshSurfaceGetFormatIndexStride(format, surface.VertexCount) != sizeof(ushort))
            throw new InvalidDataException("Packed tile indices must use the engine's unsigned 16-bit index layout.");
        RequireLength(surface.IndexData.Length, checked((long)surface.IndexCount * sizeof(ushort)), "index");
        foreach (var lod in surface.Lods) RequireLength(lod.IndexData.Length, checked((long)lod.IndexCount * sizeof(ushort)), "LOD index");
        // These are raw CPU buffers, not .tbin compressed bytes or a measurement of allocated GPU memory.
    }
    private static void RequireLength(int actual, long expected, string buffer)
    {
        if (actual != expected) throw new InvalidDataException($"Packed {buffer} bytes: expected {expected}, found {actual}.");
    }
    private static void ValidateCustom(RenderingServer.ArrayFormat format, RenderingServer.ArrayFormat present, RenderingServer.ArrayFormat shift)
    {
        var code = ((ulong)format >> (int)shift) & (ulong)RenderingServer.ArrayFormat.FormatCustomMask;
        var expected = (format & present) != 0 ? (ulong)RenderingServer.ArrayCustomFormat.RgbaFloat : 0UL;
        if (code != expected) throw new InvalidDataException("Packed CUSTOM channel must use the tile ABI RGBA float32 layout.");
    }
}
