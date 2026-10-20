using System.Globalization;
using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.Geometry;

/// <summary>Ordinary VF-A/VF-B vertex; null optional attributes omit their channel. AO is visibility.</summary>
public readonly record struct GeometryVertex(Vector3 Position, Vector3 Normal, Vector2? Uv = null,
    Vector2? Uv2 = null, float? Ao = null, ushort? ColorIndex = null, byte? Flags = null, byte? Reserved = null, ushort? DecalBaseColorIndex = null);

/// <summary>One indexed clockwise triangle stream per material class, sharing identical vertices within each batch.</summary>
public sealed class GeometrySink(PaletteCatalog? palette = null, bool withUv2 = false)
{
    private readonly SortedDictionary<string, Stream> _streams = new(StringComparer.Ordinal);
    private readonly List<(string Key, GeometryBundle Mesh)> _done = [];
    public int Triangles { get; private set; }

    public ushort ColorIndex(PaletteToken lit) =>
        (palette ?? throw new InvalidOperationException("Per-vertex colours require a palette catalogue.")).Resolve(lit).Value;

    public void Tri(ToonSpec spec, in GeometryVertex a, in GeometryVertex b, in GeometryVertex c, Vector3? facing = null)
    {
        var s = Get(spec, 3, RequiredChannels(spec) | ChannelsOf(a) | ChannelsOf(b) | ChannelsOf(c));
        s.Tri(s.Vertex(a), s.Vertex(b), s.Vertex(c), facing ?? (a.Normal + b.Normal + c.Normal));
        Triangles++;
    }

    public void Quad(ToonSpec spec, in GeometryVertex a, in GeometryVertex b, in GeometryVertex c, in GeometryVertex d)
    {
        var s = Get(spec, 4, RequiredChannels(spec) | ChannelsOf(a) | ChannelsOf(b) | ChannelsOf(c) | ChannelsOf(d));
        var ia = s.Vertex(a); var ib = s.Vertex(b); var ic = s.Vertex(c); var id = s.Vertex(d);
        var normal = a.Normal + b.Normal + c.Normal + d.Normal;
        s.Tri(ia, ib, ic, normal);
        s.Tri(ia, ic, id, normal);
        Triangles += 2;
    }

    public void Polygon(ToonSpec spec, IReadOnlyList<GeometryVertex> vertices, IReadOnlyList<int> indices, bool? hasUv2 = null)
    {
        if (indices.Count % 3 != 0) throw new ArgumentException("A polygon needs complete triangles.", nameof(indices));
        var map = new Dictionary<int, int>();
        var channels = ChannelsOf(spec, vertices);
        if (hasUv2 is { } uv2) channels = uv2 ? channels | Channels.Uv2 : channels & ~Channels.Uv2;
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
            Triangles++;
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
        ushort? fallback = palette is null ? null : ColorIndex(mesh.Material.Spec.Lit);
        for (var i = 0; i < vertices.Length; i++)
            vertices[i] = new(Vector3.Transform(new(mesh.Position[3 * i], mesh.Position[3 * i + 1], mesh.Position[3 * i + 2]), transform),
                Vector3.TransformNormal(new(mesh.Normal[3 * i], mesh.Normal[3 * i + 1], mesh.Normal[3 * i + 2]), transform),
                mesh.Uv is { } uv ? new Vector2(uv[2 * i], uv[2 * i + 1]) : null,
                mesh.Uv2 is { } uv2 ? new Vector2(uv2[2 * i], uv2[2 * i + 1]) : null,
                mesh.Ao?[i], mesh.ColorIndices?[i] ?? fallback, mesh.Flags?[i], mesh.Reserved?[i], mesh.DecalBaseColorIndices?[i]);
        Polygon(mesh.Material.Spec, vertices, mesh.Index, mesh.Uv2 is not null);
    }

    public IReadOnlyList<GeometryBundle> Finish()
    {
        foreach (var (key, stream) in _streams) Flush(key, stream);
        _streams.Clear();
        return _done.OrderBy(x => x.Key, StringComparer.Ordinal).Select(x => x.Mesh).ToArray();
    }

    private bool UsesUv2(ToonSpec spec) => withUv2 || spec.Kind == ToonKind.Glass || spec.SurfacePattern is not null;

    private Channels RequiredChannels(ToonSpec spec) => UsesUv2(spec) ? Channels.Uv2 : Channels.None;

    private Channels ChannelsOf(ToonSpec spec, IReadOnlyList<GeometryVertex> vertices)
    {
        var channels = RequiredChannels(spec);
        foreach (var vertex in vertices) channels |= ChannelsOf(vertex);
        return channels;
    }

    private static Channels ChannelsOf(in GeometryVertex vertex)
    {
        var channels = Channels.None;
        if (vertex.Ao.HasValue) channels |= Channels.Ao;
        if (vertex.Uv.HasValue) channels |= Channels.Uv;
        if (vertex.Uv2.HasValue) channels |= Channels.Uv2;
        if (vertex.ColorIndex.HasValue) channels |= Channels.Colors;
        if (vertex.Flags.HasValue) channels |= Channels.Flags;
        if (vertex.Reserved.HasValue) channels |= Channels.Reserved;
        if (vertex.DecalBaseColorIndex.HasValue) channels |= Channels.DecalBase;
        return channels;
    }

    private Stream Get(ToonSpec spec, int extra, Channels channels)
    {
        var key = ClassKey(spec, (channels & Channels.Colors) != 0) + "|" + ((int)channels).ToString("D3", CultureInfo.InvariantCulture);
        if (!_streams.TryGetValue(key, out var stream)) _streams.Add(key, stream = new(spec, channels));
        if (stream.Vertices.Count + extra > GeometryVertexLimits.Batch)
        {
            Flush(key, stream);
            _streams[key] = stream = new(spec, channels);
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

    private void Flush(string key, Stream stream)
    {
        if (stream.Index.Count == 0) return;
        var vertices = stream.Vertices;
        var mesh = new GeometryBundle(new(stream.Spec), vertices.SelectMany(v => new[] { v.Position.X, v.Position.Y, v.Position.Z }).ToArray(),
            vertices.SelectMany(v => { var n = VectorMath.Unit(v.Normal, Vector3.UnitY, 1e-8f); return new[] { n.X, n.Y, n.Z }; }).ToArray(),
            [.. stream.Index], stream.Has(Channels.Ao) ? vertices.Select(v => v.Ao ?? 1f).ToArray() : null,
            stream.Has(Channels.Uv) ? vertices.SelectMany(v => new[] { v.Uv?.X ?? 0f, v.Uv?.Y ?? 0f }).ToArray() : null,
            stream.Has(Channels.Uv2) ? vertices.SelectMany(v => new[] { v.Uv2?.X ?? 0f, v.Uv2?.Y ?? 0f }).ToArray() : null)
        {
            ColorIndices = stream.Has(Channels.Colors) ? vertices.Select(v => v.ColorIndex ?? throw new InvalidOperationException("Missing vertex colour.")).ToArray() : null,
            DecalBaseColorIndices = stream.Has(Channels.DecalBase) ? vertices.Select(v => v.DecalBaseColorIndex ?? throw new InvalidOperationException("Missing decal base colour.")).ToArray() : null,
            Flags = stream.Has(Channels.Flags) ? vertices.Select(v => v.Flags ?? (byte)0).ToArray() : null,
            Reserved = stream.Has(Channels.Reserved) && !stream.Has(Channels.DecalBase) ? vertices.Select(v => v.Reserved ?? (byte)0).ToArray() : null,
        };
        _done.Add((key + "|" + _done.Count.ToString("D8", CultureInfo.InvariantCulture), mesh));
    }

    [Flags]
    private enum Channels { None = 0, Ao = 1, Uv = 2, Uv2 = 4, Colors = 8, Flags = 16, Reserved = 32, DecalBase = 64 }

    private sealed class Stream(ToonSpec spec, Channels channels)
    {
        private readonly Dictionary<GeometryVertex, int> _vertices = [];
        public ToonSpec Spec { get; } = spec;
        public bool Has(Channels channel) => (channels & channel) != 0;
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
