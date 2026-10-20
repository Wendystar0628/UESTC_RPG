using System.Buffers.Binary;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Bake;
using Format = Godot.RenderingServer.ArrayFormat;

namespace Campus.Kernel.Bake;

/// <summary>Pure CPU encoder for the pinned renderer surface ABI. It creates no Godot resource or RID.</summary>
public static class BakeSurfacePacker
{
    /// <summary>Tint overrides every vertex with a registered logical Lit index.</summary>
    public static GeometryBundle ApplyTint(GeometryBundle mesh, PlacedPart part, PaletteCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        if (part.Tint is not { } token) return mesh;
        mesh.ValidateVertexData();
        var lit = catalog.Get(token);
        var material = new ToonRef(mesh.Material.Spec with
        {
            Lit = token,
            Shade = catalog.Get(new PaletteIndex(lit.Index.Value + 1)).Token
        });
        catalog.Validate(material.Spec);
        var colors = new ushort[mesh.Position.Length / 3];
        Array.Fill(colors, lit.Index.Value);
        return mesh with { Material = material, ColorIndices = colors };
    }

    public static BakedSurface Pack(GeometryBundle mesh, Box3 bounds, PaletteCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        mesh.ValidateVertexData();
        return Pack(View(mesh), bounds, catalog);
    }

    public static BakedSurface Pack(GeometryBuffer mesh, Box3 bounds, PaletteCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(mesh);
        ArgumentNullException.ThrowIfNull(catalog);
        return Pack(View(mesh), bounds, catalog);
    }

    private static PackInput View(GeometryBundle mesh) => new(mesh.Material, mesh.Shader,
        mesh.Position, mesh.Normal, mesh.Index, Borrow(mesh.Ao), Borrow(mesh.Uv), Borrow(mesh.Uv2),
        Borrow(mesh.Flags), Borrow(mesh.Reserved), Borrow(mesh.Custom0), Borrow(mesh.Custom1),
        Borrow(mesh.Custom2), (mesh.Lods ?? []).Select(lod => new LodInput(lod.Error, lod.Index)), mesh.Variant,
        mesh.Reflect, mesh.IgnoreOcclusionCulling, Borrow(mesh.ColorIndices), Borrow(mesh.DecalBaseColorIndices));

    private static PackInput View(GeometryBuffer mesh) => new(mesh.Material, mesh.Shader,
        mesh.Position, mesh.Normal, mesh.Index, mesh.Ao, mesh.Uv, mesh.Uv2, mesh.Flags, mesh.Reserved,
        mesh.Custom0, mesh.Custom1, mesh.Custom2, mesh.Lods.Select(lod => new LodInput(lod.Error, lod.Index)),
        mesh.Variant, mesh.Reflect, mesh.IgnoreOcclusionCulling, mesh.ColorIndices, mesh.DecalBaseColorIndices);

    private static BakedSurface Pack(PackInput mesh, Box3 bounds, PaletteCatalog catalog)
    {
        catalog.Validate(mesh.Material.Spec);
        var count = mesh.Position.Length / 3;
        GeometryVertexLimits.Require(count);
        if (count == 0) throw new InvalidDataException("A packed surface needs vertices.");
        var size = bounds.Size;
        if (!float.IsFinite(size.X) || !float.IsFinite(size.Y) || !float.IsFinite(size.Z) || size.X < 0 || size.Y < 0 || size.Z < 0)
            throw new InvalidDataException("Quantization bounds must be finite and ordered.");
        var compressed = mesh.Uv2 is null;
        var format = Format.FlagFormatVersion2 | Format.FormatVertex | Format.FormatNormal |
            Format.FormatTangent | Format.FormatColor | Format.FormatTexUV | Format.FormatIndex;
        if (compressed) format |= Format.FlagCompressAttributes;
        else format |= Format.FormatTexUV2;
        var customs = new[] { mesh.Custom0, mesh.Custom1, mesh.Custom2 };
        for (var channel = 0; channel < customs.Length; channel++)
            if (customs[channel] is not null) format |= (Format)((1UL << (6 + channel)) | (7UL << (13 + channel * 3)));
        var vertexStride = compressed ? 8 : 12;
        var normalStride = compressed ? 4 : 8;
        var attributeStride = (compressed ? 8 : 20) + customs.Count(c => c is not null) * 16;
        var vertices = new byte[checked((vertexStride + normalStride) * count)];
        var attributes = new byte[checked(attributeStride * count)];
        var uvScale = compressed ? UvScale(mesh.Uv) : Vector4.Zero;
        var lit = catalog.Resolve(mesh.Material.Spec.Lit);
        for (var i = 0; i < count; i++)
        {
            var p = new Vector3(mesh.Position.Span[i * 3], mesh.Position.Span[i * 3 + 1], mesh.Position.Span[i * 3 + 2]);
            if (!bounds.Contains(p)) throw new InvalidDataException("A vertex is outside the shared tile bounds.");
            var n = Normalize(new(mesh.Normal.Span[i * 3], mesh.Normal.Span[i * 3 + 1], mesh.Normal.Span[i * 3 + 2]));
            var vertex = vertices.AsSpan(i * vertexStride, vertexStride);
            var normal = vertices.AsSpan(count * vertexStride + i * normalStride, normalStride);
            PackVertexFrame(p, n, bounds, size, compressed, vertex, normal);
            var attribute = attributes.AsSpan(i * attributeStride, attributeStride);
            var color = mesh.ColorIndices is { } logical ? Registered(logical.Span[i])
                : lit;
            PaletteIndex? decalBase = mesh.DecalBaseColorIndices is { } backgrounds ? Registered(backgrounds.Span[i]) : null;
            var ao = (byte)Math.Clamp(Value(mesh.Ao, i, 1f) * 255f, 0f, 255f);
            var rgba = PaletteEncoding.EncodeColor(color, ao, Value(mesh.Flags, i, (byte)0),
                Value(mesh.Reserved, i, (byte)0), decalBase);
            attribute[0] = rgba.R; attribute[1] = rgba.G; attribute[2] = rgba.B; attribute[3] = rgba.A;
            var u = Value(mesh.Uv, i * 2, 0f); var v = Value(mesh.Uv, i * 2 + 1, 0f);
            var offset = 4;
            if (compressed)
            {
                if (uvScale != Vector4.Zero) { u = u / uvScale.X + .5f; v = v / uvScale.Y + .5f; }
                U16(attribute, offset, u); U16(attribute, offset + 2, v); offset += 4;
            }
            else
            {
                F32(attribute, offset, u); F32(attribute, offset + 4, v); offset += 8;
                // Window IDs, water metadata and surface-pattern gate/K pairs retain their fp32 values.
                F32(attribute, offset, mesh.Uv2!.Value.Span[i * 2]); F32(attribute, offset + 4, mesh.Uv2.Value.Span[i * 2 + 1]); offset += 8;
            }
            foreach (var custom in customs)
            {
                if (custom is null) continue;
                for (var component = 0; component < 4; component++) F32(attribute, offset + component * 4, custom.Value.Span[i * 4 + component]);
                offset += 16;
            }
        }
        return new(mesh.Material, (ulong)format, BakedPrimitive.Triangles, count, mesh.Index.Length,
            vertices, attributes, Indices(mesh.Index.Span), bounds,
            lods: mesh.Lods.Select(l => new BakedMeshLod(l.Error, l.Index.Length, Indices(l.Index.Span))).ToArray(),
            uvScale: uvScale, variant: mesh.Variant, ignoreOcclusionCulling: mesh.IgnoreOcclusionCulling)
        { Shader = mesh.Shader, Reflect = mesh.Reflect };

        PaletteIndex Registered(ushort value)
        {
            var index = new PaletteIndex(value);
            _ = catalog.Get(index);
            return index;
        }
    }

    private static void PackVertexFrame(Vector3 p, Vector3 n, Box3 bounds, Vector3 size,
        bool compressed, Span<byte> vertex, Span<byte> normal)
    {
        if (compressed)
        {
            var delta = p - bounds.Min;
            // A planar prototype may have a zero-width axis. Keep its exact bounds and encode that constant axis as zero.
            var unit = new Vector3(size.X == 0 ? 0 : delta.X / size.X,
                size.Y == 0 ? 0 : delta.Y / size.Y, size.Z == 0 ? 0 : delta.Z / size.Z);
            U16(vertex, 0, unit.X); U16(vertex, 2, unit.Y); U16(vertex, 4, unit.Z);
            var (axis, angle) = NormalRotation(n);
            var oct = Octahedral(axis);
            U16(normal, 0, oct.X); U16(normal, 2, oct.Y);
            U16(vertex, 6, Math.Clamp(angle / 3.14159265358979323846f * .5f + .5f, .500008f, 1f));
        }
        else
        {
            F32(vertex, 0, p.X); F32(vertex, 4, p.Y); F32(vertex, 8, p.Z);
            var oct = Octahedral(n);
            U16(normal, 0, oct.X); U16(normal, 2, oct.Y);
            var tangent = Normalize(Vector3.Cross(Math.Abs(n.Z) < .9f ? Vector3.UnitZ : Vector3.UnitY, n));
            var encoded = Octahedral(tangent);
            U16(normal, 4, encoded.X); U16(normal, 6, Math.Max(encoded.Y, 1f / 32767f) * .5f + .5f);
        }
    }

    private static Vector4 UvScale(ReadOnlyMemory<float>? uv)
    {
        if (uv is null) return Vector4.Zero;
        var values = uv.Value.Span;
        var unitRange = true;
        var x = 0f; var y = 0f;
        for (var i = 0; i < values.Length; i += 2)
        {
            x = Math.Max(x, Math.Abs(values[i])); y = Math.Max(y, Math.Abs(values[i + 1]));
            unitRange &= values[i] is >= 0f and <= 1f && values[i + 1] is >= 0f and <= 1f;
        }
        if (unitRange) return Vector4.Zero;
        return new(x == 0 ? 1 : x * 2, y == 0 ? 1 : y * 2, 0, 0);
    }
    private static (Vector3 Axis, float Angle) NormalRotation(Vector3 normal)
    {
        var planar = Math.Sqrt((double)normal.X * normal.X + (double)normal.Y * normal.Y);
        if (planar < 1e-12) return (Vector3.UnitX, normal.Z >= 0 ? 0 : 3.14159265358979323846f);
        return (new((float)(normal.Y / planar), (float)(-normal.X / planar), 0), MathF.Acos(Math.Clamp(normal.Z, -1f, 1f)));
    }
    private static Vector3 Normalize(Vector3 value)
    {
        var length = Math.Sqrt((double)value.X * value.X + (double)value.Y * value.Y + (double)value.Z * value.Z);
        if (!(length > 0) || !double.IsFinite(length)) throw new InvalidDataException("Surface normals must be finite and nonzero.");
        return new((float)(value.X / length), (float)(value.Y / length), (float)(value.Z / length));
    }
    private static Vector2 Octahedral(Vector3 value)
    {
        var scale = Math.Abs(value.X) + Math.Abs(value.Y) + Math.Abs(value.Z);
        var x = value.X / scale; var y = value.Y / scale;
        if (value.Z < 0) (x, y) = ((1 - Math.Abs(y)) * (x < 0 ? -1 : 1), (1 - Math.Abs(x)) * (y < 0 ? -1 : 1));
        return new(x * .5f + .5f, y * .5f + .5f);
    }
    private static void U16(Span<byte> target, int offset, float value) =>
        BinaryPrimitives.WriteUInt16LittleEndian(target[offset..], (ushort)Math.Clamp(value * 65535f, 0, 65535));
    private static void F32(Span<byte> target, int offset, float value) => BinaryPrimitives.WriteSingleLittleEndian(target[offset..], value);
    private static byte[] Indices(ReadOnlySpan<int> values)
    {
        var bytes = new byte[checked(values.Length * 2)];
        for (var i = 0; i < values.Length; i++) BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(i * 2), checked((ushort)values[i]));
        return bytes;
    }

    // The untyped null branch otherwise selects the array-to-ROM conversion and produces HasValue=true, Length=0.
    private static ReadOnlyMemory<T>? Borrow<T>(T[]? values) => values is null ? (ReadOnlyMemory<T>?)null : new ReadOnlyMemory<T>(values);
    private static T Value<T>(ReadOnlyMemory<T>? values, int index, T absentValue) => values is { } memory ? memory.Span[index] : absentValue;
    private readonly record struct LodInput(float Error, ReadOnlyMemory<int> Index);
    private sealed record PackInput(ToonRef Material, ShaderId? Shader, ReadOnlyMemory<float> Position,
        ReadOnlyMemory<float> Normal, ReadOnlyMemory<int> Index, ReadOnlyMemory<float>? Ao,
        ReadOnlyMemory<float>? Uv, ReadOnlyMemory<float>? Uv2,
        ReadOnlyMemory<byte>? Flags, ReadOnlyMemory<byte>? Reserved, ReadOnlyMemory<float>? Custom0,
        ReadOnlyMemory<float>? Custom1, ReadOnlyMemory<float>? Custom2, IEnumerable<LodInput> Lods,
        VariantTag? Variant, bool Reflect, bool IgnoreOcclusionCulling,
        ReadOnlyMemory<ushort>? ColorIndices, ReadOnlyMemory<ushort>? DecalBaseColorIndices);
}
