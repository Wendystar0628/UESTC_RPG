using System.Globalization;
using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.Geometry;

/// <summary>Ordinary VF-A/VF-B vertex; null optional attributes omit their channel. AO is visibility.</summary>
public readonly record struct GeometryVertex(Vector3 Position, Vector3 Normal, Vector2? Uv = null,
    Vector2? Uv2 = null, float? Ao = null, ushort? ColorIndex = null, byte? Flags = null, byte? Reserved = null, ushort? DecalBaseColorIndex = null);

/// <summary>Present ordinary vertex channels, including the layout of an empty indexed surface.</summary>
[Flags]
public enum GeometryChannels { None = 0, Ao = 1, Uv = 2, Uv2 = 4, Colors = 8, Flags = 16, Reserved = 32, DecalBase = 64 }

/// <summary>One indexed clockwise triangle stream per material class, sharing identical vertices within each batch.</summary>
public sealed class GeometrySink(PaletteCatalog? palette = null, bool withUv2 = false)
{
    private readonly Dictionary<StreamKey, MaterialStream> _streams = [];
    private readonly List<(string Key, GeometryBundle Mesh)> _done = [];

    public ushort ColorIndex(PaletteToken lit) =>
        (palette ?? throw new InvalidOperationException("Per-vertex colours require a palette catalogue.")).Resolve(lit).Value;

    public void Tri(ToonSpec spec, in GeometryVertex a, in GeometryVertex b, in GeometryVertex c, Vector3? facing = null)
    {
        var s = Get(spec, 3, RequiredChannels(spec) | ChannelsOf(a) | ChannelsOf(b) | ChannelsOf(c));
        s.Tri(s.Vertex(a), s.Vertex(b), s.Vertex(c), facing ?? (a.Normal + b.Normal + c.Normal));
    }

    public void Quad(ToonSpec spec, in GeometryVertex a, in GeometryVertex b, in GeometryVertex c, in GeometryVertex d)
    {
        var s = Get(spec, 4, RequiredChannels(spec) | ChannelsOf(a) | ChannelsOf(b) | ChannelsOf(c) | ChannelsOf(d));
        var ia = s.Vertex(a); var ib = s.Vertex(b); var ic = s.Vertex(c); var id = s.Vertex(d);
        var normal = a.Normal + b.Normal + c.Normal + d.Normal;
        s.Tri(ia, ib, ic, normal);
        s.Tri(ia, ic, id, normal);
    }

    public void Polygon(ToonSpec spec, IReadOnlyList<GeometryVertex> vertices, IReadOnlyList<int> indices, bool? hasUv2 = null)
    {
        if (indices.Count % 3 != 0) throw new ArgumentException("A polygon needs complete triangles.", nameof(indices));
        var map = new Dictionary<int, int>();
        var channels = ChannelsOf(spec, vertices);
        if (hasUv2 is { } uv2) channels = uv2 ? channels | GeometryChannels.Uv2 : channels & ~GeometryChannels.Uv2;
        Stream? previous = null;
        for (var t = 0; t < indices.Count; t += 3)
        {
            var s = Get(spec, 3, channels);
            if (!ReferenceEquals(s, previous)) { map.Clear(); previous = s; }
            int Copy(int i)
            {
                if (!map.TryGetValue(i, out var target))
                {
                    target = s.Vertex(vertices[i]); map.Add(i, target);
                }
                return target;
            }
            int a = indices[t], b = indices[t + 1], c = indices[t + 2];
            s.Tri(Copy(a), Copy(b), Copy(c), vertices[a].Normal + vertices[b].Normal + vertices[c].Normal);
        }
    }

    /// <summary>Append ordinary geometry with its authored channel presence; custom and LOD formats keep their dedicated writer.</summary>
    public void Append(GeometryBundle mesh, in Matrix4x4 at)
    {
        if (mesh.Custom0 is not null || mesh.Custom1 is not null || mesh.Custom2 is not null ||
            mesh.Lods is not null || mesh.Shader is not null ||
            mesh.Variant is not null || mesh.Reflect || mesh.IgnoreOcclusionCulling)
            throw new ArgumentException("Specialised surface channels cannot enter an ordinary geometry stream.", nameof(mesh));
        var transform = at;
        var vertices = new GeometryVertex[mesh.Position.Length / 3];
        ushort? materialColor = palette is null ? null : ColorIndex(mesh.Material.Spec.Lit);
        for (var i = 0; i < vertices.Length; i++)
            vertices[i] = new(Vector3.Transform(new(mesh.Position[3 * i], mesh.Position[3 * i + 1], mesh.Position[3 * i + 2]), transform),
                Vector3.TransformNormal(new(mesh.Normal[3 * i], mesh.Normal[3 * i + 1], mesh.Normal[3 * i + 2]), transform),
                mesh.Uv is { } uv ? new Vector2(uv[2 * i], uv[2 * i + 1]) : null,
                mesh.Uv2 is { } uv2 ? new Vector2(uv2[2 * i], uv2[2 * i + 1]) : null,
                mesh.Ao?[i], mesh.ColorIndices?[i] ?? materialColor, mesh.Flags?[i], mesh.Reserved?[i], mesh.DecalBaseColorIndices?[i]);
        Polygon(mesh.Material.Spec, vertices, mesh.Index, mesh.Uv2 is not null);
    }

    public IReadOnlyList<GeometryBundle> Finish()
    {
        foreach (var stream in _streams.Values.OrderBy(stream => stream.SortKey, StringComparer.Ordinal)) Flush(stream);
        _streams.Clear();
        return _done.OrderBy(x => x.Key, StringComparer.Ordinal).Select(x => x.Mesh).ToArray();
    }

    /// <summary>
    /// Packs an authored indexed surface without welding, sorting, normalising normals or changing winding.
    /// Unused vertices and explicitly present empty channels are retained; custom channels are four floats per vertex.
    /// </summary>
    public static GeometryBundle Indexed(ToonSpec spec, IReadOnlyList<GeometryVertex> vertices, IReadOnlyList<int> index,
        GeometryChannels channels, IReadOnlyList<Vector4>? custom0 = null, IReadOnlyList<Vector4>? custom1 = null,
        IReadOnlyList<Vector4>? custom2 = null)
    {
        var bundle = Pack(spec, vertices, index, channels, normalizeNormals: false, custom0, custom1, custom2);
        bundle.ValidateVertexData();
        return bundle;
    }

    /// <summary>Packs geometric points in their authored order, including meshes that have not yet been divided into surfaces.</summary>
    public static float[] Positions(IReadOnlyList<Vector3> points)
    {
        var result = new float[checked(points.Count * 3)];
        for (var i = 0; i < points.Count; i++)
        {
            var point = points[i];
            result[3 * i] = point.X; result[3 * i + 1] = point.Y; result[3 * i + 2] = point.Z;
        }
        return result;
    }

    private static float[]? CustomCoordinates(IReadOnlyList<Vector4>? values)
    {
        if (values is null) return null;
        var result = new float[checked(values.Count * 4)];
        for (var i = 0; i < values.Count; i++)
        {
            var value = values[i];
            result[4 * i] = value.X; result[4 * i + 1] = value.Y;
            result[4 * i + 2] = value.Z; result[4 * i + 3] = value.W;
        }
        return result;
    }

    private bool UsesUv2(ToonSpec spec) => withUv2 || spec.Kind == ToonKind.Glass || spec.SurfacePattern is not null;

    private GeometryChannels RequiredChannels(ToonSpec spec) => UsesUv2(spec) ? GeometryChannels.Uv2 : GeometryChannels.None;

    private GeometryChannels ChannelsOf(ToonSpec spec, IReadOnlyList<GeometryVertex> vertices)
    {
        var channels = RequiredChannels(spec);
        foreach (var vertex in vertices) channels |= ChannelsOf(vertex);
        return channels;
    }

    private static GeometryChannels ChannelsOf(in GeometryVertex vertex)
    {
        var channels = GeometryChannels.None;
        if (vertex.Ao.HasValue) channels |= GeometryChannels.Ao;
        if (vertex.Uv.HasValue) channels |= GeometryChannels.Uv;
        if (vertex.Uv2.HasValue) channels |= GeometryChannels.Uv2;
        if (vertex.ColorIndex.HasValue) channels |= GeometryChannels.Colors;
        if (vertex.Flags.HasValue) channels |= GeometryChannels.Flags;
        if (vertex.Reserved.HasValue) channels |= GeometryChannels.Reserved;
        if (vertex.DecalBaseColorIndex.HasValue) channels |= GeometryChannels.DecalBase;
        return channels;
    }

    private Stream Get(ToonSpec spec, int extra, GeometryChannels channels)
    {
        var key = StreamKey.Of(spec, channels);
        if (!_streams.TryGetValue(key, out var material))
        {
            var sortKey = ClassKey(spec, (channels & GeometryChannels.Colors) != 0) + "|" + ((int)channels).ToString("D3", CultureInfo.InvariantCulture);
            material = new(sortKey, new(spec, channels));
            _streams.Add(key, material);
        }
        var stream = material.Current;
        if (stream.Vertices.Count + extra > GeometryVertexLimits.Batch)
        {
            Flush(material);
            material.Current = stream = new(spec, channels);
        }
        return stream;
    }

    private static string ClassKey(ToonSpec spec, bool color)
    {
        var c = CultureInfo.InvariantCulture;
        return string.Join("|", ((int)spec.Kind).ToString("D2", c), ((int)spec.Fx).ToString("D3", c),
            ((int)spec.Line).ToString(c), ((int)spec.Steps).ToString(c), ((int)spec.Normal).ToString(c),
            spec.Band.ToString("R", c), spec.Translucency.ToString("R", c), spec.Wet.ToString("R", c),
            spec.Deep?.Value ?? "-", spec.Backface?.Value ?? "-", spec.Emissive?.Value ?? "-",
            spec.SurfacePattern is { } p ? string.Join(",", p.Layer.ToString(c), p.PeriodMeters.X.ToString("R", c),
                p.PeriodMeters.Y.ToString("R", c), p.JointMeters.X.ToString("R", c), p.JointMeters.Y.ToString("R", c)) : "-",
            color ? "-" : spec.Lit.Value + "|" + spec.Shade.Value);
    }

    private void Flush(MaterialStream material)
    {
        var stream = material.Current;
        if (stream.Index.Count == 0) return;
        var channels = stream.Channels;
        if ((channels & GeometryChannels.DecalBase) != 0) channels &= ~GeometryChannels.Reserved;
        var mesh = Pack(stream.Spec, stream.Vertices, stream.Index, channels, normalizeNormals: true);
        _done.Add((material.SortKey + "|" + _done.Count.ToString("D8", CultureInfo.InvariantCulture), mesh));
    }

    private static GeometryBundle Pack(ToonSpec spec, IReadOnlyList<GeometryVertex> vertices,
        IReadOnlyList<int> index, GeometryChannels channels, bool normalizeNormals,
        IReadOnlyList<Vector4>? custom0 = null, IReadOnlyList<Vector4>? custom1 = null, IReadOnlyList<Vector4>? custom2 = null)
    {
        bool Has(GeometryChannels channel) => (channels & channel) != 0;
        var count = vertices.Count;
        var position = new float[checked(count * 3)];
        var normal = new float[position.Length];
        var ao = Has(GeometryChannels.Ao) ? new float[count] : null;
        var uv = Has(GeometryChannels.Uv) ? new float[checked(count * 2)] : null;
        var uv2 = Has(GeometryChannels.Uv2) ? new float[checked(count * 2)] : null;
        var colors = Has(GeometryChannels.Colors) ? new ushort[count] : null;
        var flags = Has(GeometryChannels.Flags) ? new byte[count] : null;
        var reserved = Has(GeometryChannels.Reserved) ? new byte[count] : null;
        var decalBase = Has(GeometryChannels.DecalBase) ? new ushort[count] : null;
        for (var i = 0; i < count; i++)
        {
            var vertex = vertices[i];
            var n = normalizeNormals ? VectorMath.Unit(vertex.Normal, 1e-8f) : vertex.Normal;
            position[3 * i] = vertex.Position.X; position[3 * i + 1] = vertex.Position.Y; position[3 * i + 2] = vertex.Position.Z;
            normal[3 * i] = n.X; normal[3 * i + 1] = n.Y; normal[3 * i + 2] = n.Z;
            if (ao is not null) ao[i] = vertex.Ao ?? 1f;
            if (uv is not null) { uv[2 * i] = vertex.Uv?.X ?? 0f; uv[2 * i + 1] = vertex.Uv?.Y ?? 0f; }
            if (uv2 is not null) { uv2[2 * i] = vertex.Uv2?.X ?? 0f; uv2[2 * i + 1] = vertex.Uv2?.Y ?? 0f; }
            if (colors is not null) colors[i] = vertex.ColorIndex ?? throw new InvalidOperationException("Missing vertex colour.");
            if (flags is not null) flags[i] = vertex.Flags ?? 0;
            if (reserved is not null) reserved[i] = vertex.Reserved ?? 0;
            if (decalBase is not null) decalBase[i] = vertex.DecalBaseColorIndex ?? throw new InvalidOperationException("Missing decal base colour.");
        }
        return new(new(spec), position, normal, [.. index], ao, uv, uv2)
        {
            ColorIndices = colors, Flags = flags, Reserved = reserved, DecalBaseColorIndices = decalBase,
            Custom0 = CustomCoordinates(custom0), Custom1 = CustomCoordinates(custom1), Custom2 = CustomCoordinates(custom2)
        };
    }

    // Float bits retain the original round-trip key's signed zero; all NaN spellings share its "NaN" key.
    private static int FloatKey(float value) => BitConverter.SingleToInt32Bits(float.IsNaN(value) ? float.NaN : value);

    private readonly record struct MaterialStyle(ToonKind Kind, ToonFx Fx, LineClass Line, ShadeSteps Steps,
        ToonNormal Normal, int Band, int Translucency, int Wet);
    private readonly record struct MaterialColors(PaletteToken? Lit, PaletteToken? Shade, PaletteToken? Deep,
        PaletteToken? Backface, ChannelId? Emissive);
    private readonly record struct PatternKey(ushort Layer, int PeriodX, int PeriodY, int JointX, int JointY);
    private readonly record struct StreamKey(MaterialStyle Style, MaterialColors Colors, PatternKey? Pattern, GeometryChannels Channels)
    {
        public static StreamKey Of(ToonSpec spec, GeometryChannels channels)
        {
            var color = (channels & GeometryChannels.Colors) != 0;
            return new(new(spec.Kind, spec.Fx, spec.Line, spec.Steps, spec.Normal, FloatKey(spec.Band),
                    FloatKey(spec.Translucency), FloatKey(spec.Wet)),
                new(color ? null : spec.Lit, color ? null : spec.Shade, spec.Deep, spec.Backface, spec.Emissive),
                spec.SurfacePattern is { } p ? new PatternKey(p.Layer, FloatKey(p.PeriodMeters.X), FloatKey(p.PeriodMeters.Y),
                    FloatKey(p.JointMeters.X), FloatKey(p.JointMeters.Y)) : null, channels);
        }
    }

    private sealed class MaterialStream(string sortKey, Stream current)
    {
        public string SortKey { get; } = sortKey;
        public Stream Current { get; set; } = current;
    }

    private sealed class Stream(ToonSpec spec, GeometryChannels channels)
    {
        private readonly Dictionary<GeometryVertex, int> _vertices = [];
        public ToonSpec Spec { get; } = spec;
        public GeometryChannels Channels { get; } = channels;
        public List<GeometryVertex> Vertices { get; } = [];
        public List<int> Index { get; } = [];
        public int Vertex(in GeometryVertex vertex)
        {
            if (_vertices.TryGetValue(vertex, out var index)) return index;
            index = Vertices.Count;
            Vertices.Add(vertex);
            _vertices.Add(vertex, index);
            return index;
        }

        public void Tri(int a, int b, int c, Vector3 normal)
        {
            if (GeometryWinding.Flip(Vertices[a].Position, Vertices[b].Position, Vertices[c].Position, normal)) (b, c) = (c, b);
            Index.Add(a); Index.Add(b); Index.Add(c);
        }
    }
}

/// <summary>Godot front faces are clockwise when viewed along the intended outward normal.</summary>
public static class GeometryWinding
{
    public static bool Flip(Vector3 a, Vector3 b, Vector3 c, Vector3 outward)
    {
        var cross = Vector3.Cross(b - a, c - a);
        return (double)cross.X * outward.X + (double)cross.Y * outward.Y + (double)cross.Z * outward.Z > 0;
    }
}
