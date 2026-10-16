using System.Globalization;
using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.Zones.Core.Framework;

/// <summary>
/// Material classes of the core zone. Colours vary per vertex (palette IDs); classes stay few so a piece costs a
/// handful of draw calls (ENG-PERF §1.6). The non-colour fields of each class live in <see cref="CoreStyle"/>.
/// </summary>
public enum CoreMaterial : byte
{
    /// <summary>Granite plinths, stone tiers and steps' companions: bevel lift, mottle.</summary>
    Stone,
    /// <summary>Flush inlays in paving (bands, emblem, hour marks): decal, no shadow, no outline.</summary>
    Inlay,
    /// <summary>Poles, rings, rods and bronze: rim and anisotropic band, fine line.</summary>
    Metal,
    /// <summary>Thin cloth (flags): translucent, fine line.</summary>
    Cloth,
}

/// <summary>How one face is painted: material class, colour family (palette name without .lit/.shade) and VF-A flags.</summary>
public readonly record struct Paint(CoreMaterial Material, string Family, byte Flags = 0);

/// <summary>VF-A flag bits (GeometryBundle.Flags).</summary>
public static class VertexFlags
{
    public const byte Decal = 4;
    public const byte NoShadow = 8;
}

/// <summary>
/// Collects world-space triangles per material class and emits contract GeometryBundles (≤ 65 000 vertices each).
/// With a palette registry, colours go into ColorIds and one bundle per class is enough; without one, bundles are
/// split per colour family so each bundle's Lit token carries its colour. Every triangle is wound against the
/// supplied normal here (Godot front faces are clockwise from outside), so callers may list corners in any order.
/// </summary>
public sealed class CoreMesh(PaletteRegistry? palette)
{
    public const int MaxVertices = 65000;

    private readonly SortedDictionary<string, Stream> _streams = new(StringComparer.Ordinal);
    private readonly List<(string Key, GeometryBundle Bundle)> _done = [];

    public int Triangles { get; private set; }

    public void Tri(in Paint p, Vector3 a, Vector3 b, Vector3 c, Vector3 n) => Tri(p, a, b, c, n, n, n);

    public void Tri(in Paint p, Vector3 a, Vector3 b, Vector3 c, Vector3 na, Vector3 nb, Vector3 nc)
    {
        if (Vector3.Cross(b - a, c - a).LengthSquared() <= 1e-14f) return;
        var s = Get(p, 3);
        var i = s.Count;
        s.Add(a, na, p); s.Add(b, nb, p); s.Add(c, nc, p);
        s.Tri(i, i + 1, i + 2, na + nb + nc);
        Triangles++;
    }

    public void Quad(in Paint p, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 n) => Quad(p, a, b, c, d, n, n, n, n);

    public void Quad(in Paint p, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 na, Vector3 nb, Vector3 nc, Vector3 nd)
    {
        Tri(p, a, b, c, na, nb, nc);
        Tri(p, a, c, d, na, nc, nd);
    }

    /// <summary>Adds local-coordinate geometry (WP-09 kernel or a part), transformed by a row-vector matrix.</summary>
    public void Add(in Paint p, PartGeometry geometry, Matrix4x4 at)
    {
        ArgumentNullException.ThrowIfNull(geometry);
        foreach (var g in geometry.Meshes)
            for (var t = 0; t + 2 < g.Index.Length; t += 3)
            {
                var (a, na) = Point(g, g.Index[t], at);
                var (b, nb) = Point(g, g.Index[t + 1], at);
                var (c, nc) = Point(g, g.Index[t + 2], at);
                Tri(p, a, b, c, na, nb, nc);
            }
    }

    public IReadOnlyList<GeometryBundle> Finish()
    {
        foreach (var (key, s) in _streams) Flush(key, s);
        _streams.Clear();
        return _done.OrderBy(x => x.Key, StringComparer.Ordinal).Select(x => x.Bundle).ToArray();
    }

    private static (Vector3 P, Vector3 N) Point(GeometryBundle g, int i, Matrix4x4 at)
    {
        var p = Vector3.Transform(new(g.Position[3 * i], g.Position[3 * i + 1], g.Position[3 * i + 2]), at);
        var n = Vector3.TransformNormal(new(g.Normal[3 * i], g.Normal[3 * i + 1], g.Normal[3 * i + 2]), at);
        var len = n.Length();
        return (p, len > 1e-8f ? n / len : Vector3.UnitY);
    }

    private Stream Get(in Paint p, int extra)
    {
        var family = palette is null ? p.Family : CoreStyle.DefaultFamily(p.Material);
        var key = ((int)p.Material).ToString("D2", CultureInfo.InvariantCulture) + "|" + family;
        if (!_streams.TryGetValue(key, out var s))
        {
            s = new Stream(p.Material, family, palette);
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
        var bundle = new GeometryBundle(new ToonRef(CoreStyle.Spec(s.Material, s.Family)), [.. s.Position], [.. s.Normal], [.. s.Index], [.. s.Ao])
        {
            ColorIds = s.ColorIds is { } ids ? [.. ids] : null,
            Flags = [.. s.Flags],
        };
        bundle.ValidateVertexData();
        _done.Add((key + "|" + _done.Count.ToString("D4", CultureInfo.InvariantCulture), bundle));
    }

    private sealed class Stream(CoreMaterial material, string family, PaletteRegistry? palette)
    {
        private readonly Dictionary<string, byte> _ids = new(StringComparer.Ordinal);
        public CoreMaterial Material { get; } = material;
        public string Family { get; } = family;
        public List<float> Position { get; } = [];
        public List<float> Normal { get; } = [];
        public List<float> Ao { get; } = [];
        public List<byte> Flags { get; } = [];
        public List<byte>? ColorIds { get; } = palette is null ? null : [];
        public List<int> Index { get; } = [];
        public int Count => Position.Count / 3;

        public void Clear()
        {
            Position.Clear(); Normal.Clear(); Ao.Clear(); Flags.Clear(); ColorIds?.Clear(); Index.Clear();
        }

        public void Add(Vector3 v, Vector3 n, in Paint p)
        {
            Position.Add(v.X); Position.Add(v.Y); Position.Add(v.Z);
            var len = n.Length();
            n = len > 1e-8f ? n / len : Vector3.UnitY;
            Normal.Add(n.X); Normal.Add(n.Y); Normal.Add(n.Z);
            Ao.Add(1f);
            Flags.Add(p.Flags);
            if (ColorIds is null) return;
            if (!_ids.TryGetValue(p.Family, out var id))
            {
                id = palette!.Get(new PaletteToken(p.Family + ".lit")).Id;
                _ids.Add(p.Family, id);
            }
            ColorIds.Add(id);
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
