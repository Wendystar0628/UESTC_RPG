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
/// Glass data a placed part surface takes over from the building (WLD-INTERIORS §5.1): its pane UVs in [0, 1) move
/// into window cell <see cref="Cell"/> and UV2 becomes the building's (BaseId, channel slot + 32 × TotalColumns).
/// </summary>
public readonly record struct PartGlass(Vector2 Cell, Vector2 Uv2);

/// <summary>
/// Collects triangles per material class and emits contract GeometryBundles (≤ 65 000 vertices each).
/// Every triangle is wound against its intended normal here, so primitives can list corners in any order.
/// A material class is a ToonSpec without its colours: the generator's own faces and placed WP-10 part surfaces
/// of the same class share one stream. With a palette registry, colours go into ColorIds and one bundle per class
/// is enough; without one, bundles are split per colour family so the Lit token of each bundle carries the colour.
/// </summary>
public sealed class MeshSink(IColorIds? colorIds)
{
    public const int MaxVertices = 65000;

    private readonly SortedDictionary<string, Stream> _streams = new(StringComparer.Ordinal);
    private readonly List<(string Key, GeometryBundle Bundle)> _done = [];

    public int Triangles { get; private set; }

    /// <summary>
    /// Appends one part surface (part frame) under the row-vector placement <paramref name="at"/>, a proper rigid
    /// motion, so the part's winding holds. AO (visibility), flags and the glass spare byte pass through unchanged;
    /// <paramref name="glass"/> renumbers a glass surface into the building's window grid. With
    /// <paramref name="behind"/>, triangles lying wholly at part z ≥ behind (inside the building, behind opaque
    /// glass) are left out.
    /// </summary>
    public void Append(GeometryBundle part, in Matrix4x4 at, PartGlass? glass = null, float? behind = null)
    {
        var count = part.Position.Length / 3;
        if (count == 0) return;
        if (count > MaxVertices) throw new ArgumentException("A part surface has too many vertices for one stream.", nameof(part));
        var index = part.Index;
        if (behind is { } zCut)
        {
            var front = new List<int>(index.Length);
            for (var t = 0; t + 2 < index.Length; t += 3)
                if (part.Position[3 * index[t] + 2] < zCut || part.Position[3 * index[t + 1] + 2] < zCut || part.Position[3 * index[t + 2] + 2] < zCut)
                {
                    front.Add(index[t]); front.Add(index[t + 1]); front.Add(index[t + 2]);
                }
            if (front.Count == 0) return;
            index = [.. front];
        }
        var spec = part.Material.Spec;
        var s = Get(spec, count);
        var renumber = glass is not null && spec.Kind == ToonKind.Glass;
        // Vertices are taken in first-use order, so a culled surface keeps only what its triangles reference.
        var map = new int[count];
        Array.Fill(map, -1);
        foreach (var i in index)
        {
            if (map[i] < 0)
            {
                map[i] = s.Count;
                float x = part.Position[3 * i], y = part.Position[3 * i + 1], z = part.Position[3 * i + 2];
                float nx = part.Normal[3 * i], ny = part.Normal[3 * i + 1], nz = part.Normal[3 * i + 2];
                var p = new Vector3(x * at.M11 + y * at.M21 + z * at.M31 + at.M41, x * at.M12 + y * at.M22 + z * at.M32 + at.M42,
                    x * at.M13 + y * at.M23 + z * at.M33 + at.M43);
                var n = new Vector3(nx * at.M11 + ny * at.M21 + nz * at.M31, nx * at.M12 + ny * at.M22 + nz * at.M32,
                    nx * at.M13 + ny * at.M23 + nz * at.M33);
                var uv = part.Uv is { } u ? new Vector2(u[2 * i], u[2 * i + 1]) : Vector2.Zero;
                var uv2 = part.Uv2 is { } u2 ? new Vector2(u2[2 * i], u2[2 * i + 1]) : Vector2.Zero;
                if (renumber)
                {
                    uv += glass!.Value.Cell;
                    uv2 = glass.Value.Uv2;
                }
                s.AddRaw(p, n, uv, uv2, part.Ao?[i] ?? 1f, part.Flags?[i] ?? 0, part.Reserved?[i] ?? 0, spec.Lit);
            }
            s.Index.Add(map[i]);
        }
        Triangles += index.Length / 3;
    }

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

    private Stream Get(in Face f, int extra) =>
        Get(Materials.Spec(f.Kind, colorIds is null ? f.Family : Materials.DefaultFamily(f.Kind)), extra);

    private Stream Get(ToonSpec spec, int extra)
    {
        var key = ClassKey(spec) + (colorIds is null ? "|" + spec.Lit.Value : "");
        if (!_streams.TryGetValue(key, out var s))
        {
            s = new Stream(spec, spec.Kind == ToonKind.Glass, colorIds);
            _streams.Add(key, s);
        }
        if (s.Count + extra > MaxVertices)
        {
            Flush(key, s);
            s.Clear();
        }
        return s;
    }

    /// <summary>Everything of a ToonSpec except its colours, written culture-invariantly: the material class.</summary>
    private static string ClassKey(ToonSpec spec)
    {
        var c = System.Globalization.CultureInfo.InvariantCulture;
        return string.Join("|",
            ((int)spec.Kind).ToString("D2", c), ((int)spec.Fx).ToString("D3", c), ((int)spec.Line).ToString(c),
            ((int)spec.Steps).ToString(c), ((int)spec.Normal).ToString(c), spec.Band.ToString("R", c),
            spec.Translucency.ToString("R", c), spec.Wet.ToString("R", c), spec.Deep?.Value ?? "-", spec.Backface?.Value ?? "-",
            spec.Emissive?.Value ?? "-");
    }

    private void Flush(string key, Stream s)
    {
        if (s.Index.Count == 0) return;
        var bundle = new GeometryBundle(new ToonRef(s.Spec), [.. s.Position], [.. s.Normal], [.. s.Index],
            [.. s.Ao], [.. s.Uv], s.HasUv2 ? [.. s.Uv2] : null)
        {
            ColorIds = s.ColorIds is { } ids ? [.. ids] : null,
            Flags = [.. s.Flags],
            Reserved = [.. s.Spare],
        };
        _done.Add((key + "|" + _done.Count.ToString("D4", System.Globalization.CultureInfo.InvariantCulture), bundle));
    }

    private sealed class Stream(ToonSpec spec, bool hasUv2, IColorIds? colorIds)
    {
        private readonly Dictionary<string, byte> _idCache = new(StringComparer.Ordinal);
        /// <summary>The bundle's material: the first spec of the class (its colours are the fallback when ColorIds exist).</summary>
        public ToonSpec Spec { get; } = spec;
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

        public void Add(in V v, in Face f) =>
            AddRaw(v.P, v.N, v.Uv, f.Uv2, 1f - Math.Clamp(v.Ao, 0f, 1f), f.Flags, f.Spare, Materials.Lit(f.Family));

        /// <summary>One vertex with visibility AO (1 = open), as stored in the bundle.</summary>
        public void AddRaw(Vector3 p, Vector3 normal, Vector2 uv, Vector2 uv2, float visibility, byte flags, byte spare, PaletteToken lit)
        {
            Position.Add(p.X); Position.Add(p.Y); Position.Add(p.Z);
            var n = normal;
            var len = n.Length();
            n = len > 1e-8f ? n / len : Vector3.UnitY;
            Normal.Add(n.X); Normal.Add(n.Y); Normal.Add(n.Z);
            Uv.Add(uv.X); Uv.Add(uv.Y);
            if (HasUv2) { Uv2.Add(uv2.X); Uv2.Add(uv2.Y); }
            Ao.Add(visibility);
            Flags.Add(flags);
            Spare.Add(spare);
            if (ColorIds is not null)
            {
                if (!_idCache.TryGetValue(lit.Value, out var id))
                {
                    id = colorIds!.Id(lit);
                    _idCache.Add(lit.Value, id);
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
