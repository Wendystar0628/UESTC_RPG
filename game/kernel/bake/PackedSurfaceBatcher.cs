using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.InteropServices;
using Campus.Contracts;
using Campus.Contracts.Bake;
using Format = Godot.RenderingServer.ArrayFormat;
using CustomFormat = Godot.RenderingServer.ArrayCustomFormat;

namespace Campus.Kernel.Bake;

/// <summary>Concatenates adjacent compatible opaque surfaces without decoding or changing any vertex bytes.</summary>
/// <remarks>
/// Only the pinned BakeSurfacePacker layouts are accepted. An unsupported surface is retained by reference and
/// ends the current batch. Global surface order and every source's vertex/triangle order remain unchanged.
/// This operates within one TileBundle's ordinary surfaces, never on MultiMesh prototypes or instance streams.
/// </remarks>
internal static class PackedSurfaceBatcher
{
    private const Format Required = Format.FlagFormatVersion2 | Format.FormatVertex | Format.FormatNormal |
        Format.FormatTangent | Format.FormatColor | Format.FormatTexUV | Format.FormatIndex;
    private readonly record struct Layout(int PositionStride, int NormalStride, int AttributeStride);

    public static IReadOnlyList<BakedSurface> Merge(IReadOnlyList<BakedSurface> surfaces)
    {
        var output = new List<BakedSurface>(surfaces.Count);
        var batch = new List<BakedSurface>();
        var vertices = 0;
        var layout = default(Layout);
        foreach (var surface in surfaces)
        {
            if (!TryLayout(surface, out var nextLayout))
            {
                Flush(); output.Add(surface); continue;
            }
            if (batch.Count != 0 && (!SameKey(batch[0], surface) || vertices + surface.VertexCount > GeometryVertexLimits.Maximum)) Flush();
            if (batch.Count == 0) layout = nextLayout;
            batch.Add(surface); vertices += surface.VertexCount;
        }
        Flush();
        return output.AsReadOnly();

        void Flush()
        {
            if (batch.Count == 0) return;
            output.Add(batch.Count == 1 ? batch[0] : Concatenate(batch, layout, vertices));
            batch.Clear(); vertices = 0;
        }
    }

    private static bool TryLayout(BakedSurface surface, out Layout layout)
    {
        layout = default;
        if (surface.Shader is not null || surface.Material.Spec.Kind is not (ToonKind.Static or ToonKind.Glass) ||
            surface.Primitive != BakedPrimitive.Triangles || surface.VertexCount <= 0 || surface.IndexCount <= 0 ||
            surface.SkinData.Length != 0 || surface.Lods.Count != 0) return false;
        var format = (Format)surface.Format;
        var uv2 = (format & Format.FormatTexUV2) != 0;
        var expected = Required | (uv2 ? Format.FormatTexUV2 : Format.FlagCompressAttributes);
        var attributes = uv2 ? 20 : 8;
        AddCustom(Format.FormatCustom0, Format.FormatCustom0Shift);
        AddCustom(Format.FormatCustom1, Format.FormatCustom1Shift);
        AddCustom(Format.FormatCustom2, Format.FormatCustom2Shift);
        // Exact equality rejects custom3, skin/bone flags, other custom encodings, version changes and unknown bits.
        if (format != expected) return false;
        var candidate = new Layout(uv2 ? 12 : 8, uv2 ? 8 : 4, attributes);
        if (surface.VertexData.Length != (long)surface.VertexCount * (candidate.PositionStride + candidate.NormalStride) ||
            surface.AttributeData.Length != (long)surface.VertexCount * candidate.AttributeStride ||
            surface.IndexData.Length != (long)surface.IndexCount * sizeof(ushort)) return false;
        layout = candidate;
        return true;

        void AddCustom(Format present, Format shift)
        {
            if ((format & present) == 0) return;
            expected |= present | (Format)((ulong)CustomFormat.RgbaFloat << (int)shift);
            attributes += 16;
        }
    }

    private static bool SameKey(BakedSurface a, BakedSurface b) =>
        a.Material == b.Material && a.Format == b.Format && a.Primitive == b.Primitive && a.Shader == b.Shader &&
        a.Variant == b.Variant && a.Reflect == b.Reflect && a.IgnoreOcclusionCulling == b.IgnoreOcclusionCulling &&
        BitsEqual(a.At, b.At) && BitsEqual(a.Bounds, b.Bounds) && BitsEqual(a.UvScale, b.UvScale) &&
        BitsEqual(a.Material.Spec.Band, b.Material.Spec.Band) && BitsEqual(a.Material.Spec.Wet, b.Material.Spec.Wet) &&
        BitsEqual(a.Material.Spec.Translucency, b.Material.Spec.Translucency);

    private static bool BitsEqual<T>(T a, T b) where T : unmanaged =>
        MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref a, 1)).SequenceEqual(
            MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref b, 1)));

    private static BakedSurface Concatenate(IReadOnlyList<BakedSurface> sources, Layout layout, int vertexCount)
    {
        var indexCount = sources.Aggregate(0, (total, source) => checked(total + source.IndexCount));
        var vertices = new byte[checked(vertexCount * (layout.PositionStride + layout.NormalStride))];
        var attributes = new byte[checked(vertexCount * layout.AttributeStride)];
        var indices = new byte[checked(indexCount * sizeof(ushort))];
        var vertexOffset = 0; var indexOffset = 0;
        foreach (var source in sources)
        {
            var positionBytes = source.VertexCount * layout.PositionStride;
            source.VertexData.Span[..positionBytes].CopyTo(vertices.AsSpan(vertexOffset * layout.PositionStride));
            source.VertexData.Span[positionBytes..].CopyTo(vertices.AsSpan(vertexCount * layout.PositionStride + vertexOffset * layout.NormalStride));
            source.AttributeData.Span.CopyTo(attributes.AsSpan(vertexOffset * layout.AttributeStride));
            for (var index = 0; index < source.IndexCount; index++)
            {
                var original = BinaryPrimitives.ReadUInt16LittleEndian(source.IndexData.Span[(index * sizeof(ushort))..]);
                BinaryPrimitives.WriteUInt16LittleEndian(indices.AsSpan((indexOffset + index) * sizeof(ushort)),
                    checked((ushort)(original + vertexOffset)));
            }
            vertexOffset += source.VertexCount; indexOffset += source.IndexCount;
        }
        var first = sources[0];
        return new(first.Material, first.Format, first.Primitive, vertexCount, indexCount, vertices, attributes, indices,
            first.Bounds, at: first.At, uvScale: first.UvScale, variant: first.Variant, ignoreOcclusionCulling: first.IgnoreOcclusionCulling)
        { Shader = first.Shader, Reflect = first.Reflect };
    }
}
