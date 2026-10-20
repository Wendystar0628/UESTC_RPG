using Campus.Contracts;
using Godot;

namespace Campus.Kernel.Renderer;

/// <summary>
/// One vertex encoder for frozen prototypes and ordinary geometry. Uploads one surface into an owned mesh;
/// it does not resolve materials, interpret instance policy, freeze mutable input, or read GPU buffers.
/// </summary>
public static class GeometrySurfaceUpload
{
    /// <summary>Build-time layout of the uncompressed frozen-prototype upload path.</summary>
    public static ulong FormatOf(GeometryBuffer source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return FormatOf(source.Uv is not null, source.Uv2 is not null, source.Custom0 is not null,
            source.Custom1 is not null, source.Custom2 is not null);
    }

    /// <summary>Build-time layout of the default dynamic-geometry upload path.</summary>
    public static ulong FormatOf(GeometryBundle source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return FormatOf(source.Uv is not null, source.Uv2 is not null, source.Custom0 is not null,
            source.Custom1 is not null, source.Custom2 is not null);
    }

    private static ulong FormatOf(bool uv, bool uv2, bool custom0, bool custom1, bool custom2)
    {
        // AddSurfaceFromArrays supplies an arbitrary tangent whenever normals are present.
        var format = (ulong)(RenderingServer.ArrayFormat.FlagFormatVersion2 | RenderingServer.ArrayFormat.FormatVertex |
            RenderingServer.ArrayFormat.FormatNormal | RenderingServer.ArrayFormat.FormatTangent |
            RenderingServer.ArrayFormat.FormatColor | RenderingServer.ArrayFormat.FormatIndex);
        if (uv) format |= (ulong)RenderingServer.ArrayFormat.FormatTexUV;
        if (uv2) format |= (ulong)RenderingServer.ArrayFormat.FormatTexUV2;
        if (custom0) Custom(RenderingServer.ArrayFormat.FormatCustom0, RenderingServer.ArrayFormat.FormatCustom0Shift);
        if (custom1) Custom(RenderingServer.ArrayFormat.FormatCustom1, RenderingServer.ArrayFormat.FormatCustom1Shift);
        if (custom2) Custom(RenderingServer.ArrayFormat.FormatCustom2, RenderingServer.ArrayFormat.FormatCustom2Shift);
        return format;

        void Custom(RenderingServer.ArrayFormat bit, RenderingServer.ArrayFormat shift) =>
            format |= (ulong)bit | ((ulong)Mesh.ArrayCustomFormat.RgbaFloat << (int)shift);
    }

    public static int Append(ArrayMesh target, GeometryBuffer source, PaletteCatalog catalog,
        PaletteToken? tint = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        return Append(target, new SurfaceView
        {
            Spec = source.Material.Spec,
            ColorIndices = source.ColorIndices,
            DecalBaseColorIndices = source.DecalBaseColorIndices,
            Position = source.Position,
            Normal = source.Normal,
            Index = source.Index,
            Ao = source.Ao,
            Uv = source.Uv,
            Uv2 = source.Uv2,
            Flags = source.Flags,
            Reserved = source.Reserved,
            Custom0 = source.Custom0,
            Custom1 = source.Custom1,
            Custom2 = source.Custom2,
            Lods = source.Lods.Select(lod => new LodView(lod.Error, lod.Index))
        }, tint, false, catalog);
    }

    /// <summary>Caller exclusively owns source arrays during this upload; no extra GeometryBuffer.Freeze occurs.</summary>
    public static int Append(ArrayMesh target, GeometryBundle source, PaletteCatalog catalog,
        PaletteToken? tint = null, ToonSpec? materialSpec = null,
        bool compressAttributes = false)
    {
        ArgumentNullException.ThrowIfNull(source);
        source.ValidateVertexData();
        if (materialSpec is not null && materialSpec.SurfacePattern != source.Material.Spec.SurfacePattern)
            throw new ArgumentException("A material override cannot remove or change the surface-pattern profile.", nameof(materialSpec));
        materialSpec?.ValidateSurfacePattern();
        return Append(target, new SurfaceView
        {
            Spec = materialSpec ?? source.Material.Spec,
            ColorIndices = Memory(source.ColorIndices),
            DecalBaseColorIndices = Memory(source.DecalBaseColorIndices),
            Position = source.Position,
            Normal = source.Normal,
            Index = source.Index,
            Ao = Memory(source.Ao),
            Uv = Memory(source.Uv),
            Uv2 = Memory(source.Uv2),
            Flags = Memory(source.Flags),
            Reserved = Memory(source.Reserved),
            Custom0 = Memory(source.Custom0),
            Custom1 = Memory(source.Custom1),
            Custom2 = Memory(source.Custom2),
            Lods = (source.Lods ?? []).Select(lod => new LodView(lod.Error, lod.Index))
        }, tint, compressAttributes, catalog);
    }

    private static int Append(ArrayMesh target, SurfaceView source,
        PaletteToken? tint, bool compressAttributes, PaletteCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(source.Spec);
        source.Spec.ValidateSurfacePattern();
        ArgumentNullException.ThrowIfNull(catalog);
        catalog.Validate(source.Spec);
        if (tint is { } token)
        {
            var index = catalog.Resolve(token);
            catalog.Validate(source.Spec with { Lit = token, Shade = catalog.Get(new PaletteIndex(index.Value + 1)).Token });
        }
        var count = source.Position.Length / 3;
        if (count < 3 || source.Index.Length == 0)
            throw new ArgumentException("A native surface needs at least three vertices and one triangle.", nameof(source));
        if (compressAttributes && source.Uv2 is not null)
            throw new ArgumentException("UV2 must remain fp32; compressed attributes cannot be requested.", nameof(compressAttributes));

        var levels = OrderedLevels(source);

        var positions = new Vector3[count];
        var normals = new Vector3[count];
        var colors = new Color[count];
        FillVertices(source, tint, catalog, positions, normals, colors);

        using var arrays = new Godot.Collections.Array();
        if (arrays.Resize((int)Mesh.ArrayType.Max) != Error.Ok)
            throw new InvalidOperationException("Unable to allocate surface array slots.");
        arrays[(int)Mesh.ArrayType.Vertex] = positions;
        arrays[(int)Mesh.ArrayType.Normal] = normals;
        arrays[(int)Mesh.ArrayType.Color] = colors;
        arrays[(int)Mesh.ArrayType.Index] = source.Index.ToArray();
        if (source.Uv is { } uv) arrays[(int)Mesh.ArrayType.TexUV] = Pairs(uv.Span);
        if (source.Uv2 is { } uv2) arrays[(int)Mesh.ArrayType.TexUV2] = Pairs(uv2.Span);
        var flags = compressAttributes ? Mesh.ArrayFormat.FlagCompressAttributes : (Mesh.ArrayFormat)0;
        SetCustom(Mesh.ArrayType.Custom0, source.Custom0, Mesh.ArrayFormat.FormatCustom0Shift);
        SetCustom(Mesh.ArrayType.Custom1, source.Custom1, Mesh.ArrayFormat.FormatCustom1Shift);
        SetCustom(Mesh.ArrayType.Custom2, source.Custom2, Mesh.ArrayFormat.FormatCustom2Shift);
        using var lods = new Godot.Collections.Dictionary();
        foreach (var lod in levels) lods[lod.Error] = lod.Index.ToArray();
        var surface = target.GetSurfaceCount();
        target.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays, lods: lods, flags: flags);
        if (target.GetSurfaceCount() != surface + 1)
            throw new InvalidOperationException("Godot rejected the complete geometry surface.");
        return surface;

        void SetCustom(Mesh.ArrayType channel, ReadOnlyMemory<float>? data, Mesh.ArrayFormat shift)
        {
            if (data is not { } values) return;
            arrays[(int)channel] = values.ToArray();
            flags |= (Mesh.ArrayFormat)((ulong)Mesh.ArrayCustomFormat.RgbaFloat << (int)shift);
        }
    }

    private static LodView[] OrderedLevels(SurfaceView source)
    {
        var levels = source.Lods.OrderBy(lod => lod.Error).ToArray();
        float? previousError = null;
        foreach (var lod in levels)
        {
            // Locked Godot's mesh_create_surface_data_from_arrays skips these entries; reject instead of silently losing them.
            if (!float.IsFinite(lod.Error) || lod.Error <= 0f || lod.Index.Length == 0 || lod.Index.Length >= source.Index.Length)
                throw new ArgumentException("A native LOD needs a positive error and fewer nonempty indices than the base surface.", nameof(source));
            if (previousError == lod.Error)
                throw new ArgumentException("Duplicate LOD error keys would discard an index buffer.", nameof(source));
            previousError = lod.Error;
        }
        return levels;
    }

    private static void FillVertices(SurfaceView source, PaletteToken? tint, PaletteCatalog catalog,
        Vector3[] positions, Vector3[] normals, Color[] colors)
    {
        var defaultId = default(PaletteIndex);
        if (tint.HasValue || source.ColorIndices is null)
            defaultId = catalog.Resolve(tint ?? source.Spec.Lit);
        var p = source.Position.Span;
        var n = source.Normal.Span;
        for (var i = 0; i < positions.Length; i++)
        {
            positions[i] = new(p[i * 3], p[i * 3 + 1], p[i * 3 + 2]);
            normals[i] = new(n[i * 3], n[i * 3 + 1], n[i * 3 + 2]);
            var color = tint.HasValue ? defaultId : source.ColorIndices is { } logical ? Registered(logical.Span[i])
                : defaultId;
            // Match WP-02's established AO byte truncation, then use the existing native UNORM8 input bias.
            var ao = (byte)Math.Clamp(At(source.Ao, i, 1f) * 255f, 0f, 255f);
            PaletteIndex? decalBase = source.DecalBaseColorIndices is { } backgrounds ? Registered(backgrounds.Span[i]) : null;
            var rgba = PaletteEncoding.EncodeColor(color, ao, At(source.Flags, i, (byte)0),
                At(source.Reserved, i, (byte)0), decalBase);
            colors[i] = new(PaletteEncoding.Unorm8Input(rgba.R), PaletteEncoding.Unorm8Input(rgba.G),
                PaletteEncoding.Unorm8Input(rgba.B), PaletteEncoding.Unorm8Input(rgba.A));
        }

        PaletteIndex Registered(ushort value)
        {
            var index = new PaletteIndex(value);
            _ = catalog.Get(index);
            return index;
        }
    }

    private static ReadOnlyMemory<T>? Memory<T>(T[]? values) => values is null
        ? (ReadOnlyMemory<T>?)null : new ReadOnlyMemory<T>(values);
    private static T At<T>(ReadOnlyMemory<T>? values, int index, T fallback) => values is { } memory ? memory.Span[index] : fallback;
    private static Vector2[] Pairs(ReadOnlySpan<float> values)
    {
        var result = new Vector2[values.Length / 2];
        for (var i = 0; i < result.Length; i++) result[i] = new(values[i * 2], values[i * 2 + 1]);
        return result;
    }

    private readonly record struct LodView(float Error, ReadOnlyMemory<int> Index);
    private sealed class SurfaceView
    {
        public required ToonSpec Spec { get; init; }
        public ReadOnlyMemory<ushort>? ColorIndices { get; init; }
        public ReadOnlyMemory<ushort>? DecalBaseColorIndices { get; init; }
        public required ReadOnlyMemory<float> Position { get; init; }
        public required ReadOnlyMemory<float> Normal { get; init; }
        public required ReadOnlyMemory<int> Index { get; init; }
        public ReadOnlyMemory<float>? Ao { get; init; }
        public ReadOnlyMemory<float>? Uv { get; init; }
        public ReadOnlyMemory<float>? Uv2 { get; init; }
        public ReadOnlyMemory<byte>? Flags { get; init; }
        public ReadOnlyMemory<byte>? Reserved { get; init; }
        public ReadOnlyMemory<float>? Custom0 { get; init; }
        public ReadOnlyMemory<float>? Custom1 { get; init; }
        public ReadOnlyMemory<float>? Custom2 { get; init; }
        public required IEnumerable<LodView> Lods { get; init; }
    }
}
