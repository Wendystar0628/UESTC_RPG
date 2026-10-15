using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Buildings.Rules;

namespace Campus.Gen.Buildings.Mesh;

/// <summary>Token to palette ID resolution (the generator converts colour names to IDs at build time, ART-STYLE §8.5).</summary>
public interface IColorIds
{
    byte Id(PaletteToken lit);
}

public sealed class RegistryColorIds(PaletteRegistry registry) : IColorIds
{
    public byte Id(PaletteToken lit) => registry.Get(lit).Id;
}

/// <summary>
/// One vertex: position, normal, UV (metres along the facade, or window pane coordinates on glass) and occlusion.
/// Occlusion is 0 on open faces and grows on narrow recesses; the bundle stores visibility Ao = 1 - occlusion
/// (contract: missing Ao means 1, the toon threshold push is 0.3 × (1 - Ao)).
/// </summary>
public readonly record struct V(Vector3 P, Vector3 N, Vector2 Uv, float Ao = 0f);

/// <summary>Per-face attributes written into the VF-A COLOR bytes and the VF-B UV2.</summary>
public readonly record struct Face(MaterialKind Kind, string Family, byte Flags = 0, byte Spare = 0, Vector2 Uv2 = default);

public static class VertexFlags
{
    public const byte Interior = 1;
    public const byte Outskirts = 2;
    public const byte Decal = 4;
    public const byte NoShadow = 8;
}

/// <summary>
/// Collects triangles per material class and emits contract GeometryBundles (≤ 65 000 vertices each).
/// Every triangle is wound against its intended normal here, so primitives can list corners in any order.
/// With a palette registry, colours go into ColorIds and one bundle per class is enough; without one,
/// bundles are split per colour family so the Lit token of each bundle carries the colour.
/// </summary>
public sealed class MeshSink(IColorIds? colorIds)
{
    public const int MaxVertices = 65000;

    private readonly SortedDictionary<string, Stream> _streams = new(StringComparer.Ordinal);
    private readonly List<(string Key, GeometryBundle Bundle)> _done = [];

    public int Triangles { get; private set; }

    public void Quad(in Face f, in V a, in V b, in V c, in V d)
    {
        var s = Get(f, 4);
        var i = s.Count;
        s.Add(a, f); s.Add(b, f); s.Add(c, f); s.Add(d, f);
        var reference = a.N + b.N + c.N + d.N;
        s.Tri(i, i + 1, i + 2, reference);
        s.Tri(i, i + 2, i + 3, reference);
        Triangles += 2;
    }

    public void Tri(in Face f, in V a, in V b, in V c)
    {
        var s = Get(f, 3);
        var i = s.Count;
        s.Add(a, f); s.Add(b, f); s.Add(c, f);
        s.Tri(i, i + 1, i + 2, a.N + b.N + c.N);
        Triangles++;
    }

    /// <summary>Planar polygon from a triangulation (points share one normal).</summary>
    public void Polygon(in Face f, IReadOnlyList<V> verts, IReadOnlyList<int> triangles)
    {
        if (verts.Count > MaxVertices) throw new ArgumentException("Polygon has too many vertices for one surface.", nameof(verts));
        var s = Get(f, verts.Count);
        var i0 = s.Count;
        var reference = Vector3.Zero;
        foreach (var v in verts) { s.Add(v, f); reference += v.N; }
        for (var t = 0; t + 2 < triangles.Count; t += 3)
        {
            s.Tri(i0 + triangles[t], i0 + triangles[t + 1], i0 + triangles[t + 2], reference);
            Triangles++;
        }
    }

    public IReadOnlyList<GeometryBundle> Finish()
    {
        foreach (var (key, s) in _streams) Flush(key, s);
        _streams.Clear();
        return _done.OrderBy(x => x.Key, StringComparer.Ordinal).Select(x => x.Bundle).ToArray();
    }

    private Stream Get(in Face f, int extra)
    {
        var family = colorIds is null ? f.Family : Materials.DefaultFamily(f.Kind);
        var key = ((int)f.Kind).ToString("D2", System.Globalization.CultureInfo.InvariantCulture) + "|" + family;
        if (!_streams.TryGetValue(key, out var s))
        {
            s = new Stream(f.Kind, family, f.Kind == MaterialKind.Glass, colorIds);
            _streams.Add(key, s);
        }
        if (s.Count + extra > MaxVertices)
        {
            Flush(key, s);
            s.Clear();
        }
        return s;
    }

    private void Flush(string key, Stream s)
    {
        if (s.Index.Count == 0) return;
        var spec = Materials.Spec(s.Kind, s.Family);
        var bundle = new GeometryBundle(new ToonRef(spec), [.. s.Position], [.. s.Normal], [.. s.Index],
            [.. s.Ao], [.. s.Uv], s.HasUv2 ? [.. s.Uv2] : null)
        {
            ColorIds = s.ColorIds is { } ids ? [.. ids] : null,
            Flags = [.. s.Flags],
            Reserved = [.. s.Spare],
        };
        _done.Add((key + "|" + _done.Count.ToString("D4", System.Globalization.CultureInfo.InvariantCulture), bundle));
    }

    private sealed class Stream(MaterialKind kind, string family, bool hasUv2, IColorIds? colorIds)
    {
        private readonly Dictionary<string, byte> _idCache = new(StringComparer.Ordinal);
        public MaterialKind Kind { get; } = kind;
        public string Family { get; } = family;
        public bool HasUv2 { get; } = hasUv2;
        public List<float> Position { get; } = [];
        public List<float> Normal { get; } = [];
        public List<float> Uv { get; } = [];
        public List<float> Uv2 { get; } = [];
        public List<float> Ao { get; } = [];
        public List<byte> Flags { get; } = [];
        public List<byte> Spare { get; } = [];
        public List<byte>? ColorIds { get; } = colorIds is null ? null : [];
        public List<int> Index { get; } = [];
        public int Count => Position.Count / 3;

        public void Clear()
        {
            Position.Clear(); Normal.Clear(); Uv.Clear(); Uv2.Clear(); Ao.Clear();
            Flags.Clear(); Spare.Clear(); ColorIds?.Clear(); Index.Clear();
        }

        public void Add(in V v, in Face f)
        {
            Position.Add(v.P.X); Position.Add(v.P.Y); Position.Add(v.P.Z);
            var n = v.N;
            var len = n.Length();
            n = len > 1e-8f ? n / len : Vector3.UnitY;
            Normal.Add(n.X); Normal.Add(n.Y); Normal.Add(n.Z);
            Uv.Add(v.Uv.X); Uv.Add(v.Uv.Y);
            if (HasUv2) { Uv2.Add(f.Uv2.X); Uv2.Add(f.Uv2.Y); }
            Ao.Add(1f - Math.Clamp(v.Ao, 0f, 1f));
            Flags.Add(f.Flags);
            Spare.Add(f.Spare);
            if (ColorIds is not null)
            {
                if (!_idCache.TryGetValue(f.Family, out var id))
                {
                    id = colorIds!.Id(Materials.Lit(f.Family));
                    _idCache.Add(f.Family, id);
                }
                ColorIds.Add(id);
            }
        }

        // Godot treats clockwise triangles (seen from the front) as front faces: cross(b - a, c - a) points away from the viewer.
        public void Tri(int a, int b, int c, Vector3 reference)
        {
            var pa = P(a);
            var g = Vector3.Cross(P(b) - pa, P(c) - pa);
            if (Vector3.Dot(g, reference) > 0f) (b, c) = (c, b);
            Index.Add(a); Index.Add(b); Index.Add(c);
        }

        private Vector3 P(int i) => new(Position[3 * i], Position[3 * i + 1], Position[3 * i + 2]);
    }
}
