using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Parts.Core;

namespace Campus.Gen.Interior;

/// <summary>Self-lit part streams grouped by family, with interior and no-shadow output flags.</summary>
public sealed class InteriorLights(bool draw, Func<string, ToonSpec> material)
{
    private static Vector3 AtHeight(Vector2 p, float y) => new(p.X, y, p.Y);

    private readonly SortedDictionary<string, PartShapeWriter> _glow = new(StringComparer.Ordinal);

    /// <summary>A self-lit box on the library light channel (downlights, lantern shades, lamp pools, circuit lines).</summary>
    public void Box(string family, Vector2 c, Vector2 u, float length, float depth, float y0, float y1)
    {
        var v = new Vector2(-u.Y, u.X);
        float hl = length * 0.5f;
        float hd = depth * 0.5f;
        Prism(family, [c - u * hl - v * hd, c + u * hl - v * hd, c + u * hl + v * hd, c - u * hl + v * hd], y0, y1);
    }

    /// <summary>A self-lit vertical prism over a convex plan ring (lamp pools, shade mouths).</summary>
    public void Prism(string family, IReadOnlyList<Vector2> ring, float y0, float y1)
    {
        if (!draw || ring.Count < 3 || y1 - y0 < 1e-4f) return;
        var area = 0f;
        for (var i = 1; i + 1 < ring.Count; i++)
        {
            Vector2 p = ring[i] - ring[0];
            Vector2 q = ring[i + 1] - ring[0];
            area += p.X * q.Y - p.Y * q.X;
        }
        if (MathF.Abs(area) < 1e-8f) return;
        if (!_glow.TryGetValue(family, out var found))
        {
            found = new PartShapeWriter(material(family));
            _glow.Add(family, found);
        }
        PartShapeWriter writer = found;
        var centre = Vector2.Zero;
        foreach (var p in ring)
        {
            centre += p;
        }
        centre /= ring.Count;
        for (var i = 1; i + 1 < ring.Count; i++)
        {
            GlowTriangle(writer, AtHeight(ring[0], y1), AtHeight(ring[i], y1), AtHeight(ring[i + 1], y1), Vector3.UnitY);
            GlowTriangle(writer, AtHeight(ring[0], y0), AtHeight(ring[i], y0), AtHeight(ring[i + 1], y0), -Vector3.UnitY);
        }
        for (var i = 0; i < ring.Count; i++)
        {
            Vector2 a = ring[i];
            Vector2 b = ring[(i + 1) % ring.Count];
            if (Vector2.DistanceSquared(a, b) < 1e-10f) continue;
            var n = Vector2.Normalize(new Vector2(b.Y - a.Y, a.X - b.X));
            if (Vector2.Dot(n, (a + b) * 0.5f - centre) < 0f) n = -n;
            var n3 = new Vector3(n.X, 0f, n.Y);
            GlowTriangle(writer, AtHeight(a, y0), AtHeight(b, y0), AtHeight(b, y1), n3);
            GlowTriangle(writer, AtHeight(a, y0), AtHeight(b, y1), AtHeight(a, y1), n3);
        }
    }

    private static void GlowTriangle(PartShapeWriter writer, Vector3 a, Vector3 b, Vector3 c, Vector3 n)
    {
        if (Vector3.Cross(b - a, c - a).LengthSquared() <= 1e-14f) return;
        writer.Triangle(writer.Vertex(a, n, Vector2.Zero), writer.Vertex(b, n, Vector2.UnitX), writer.Vertex(c, n, Vector2.One), n);
    }

    public IReadOnlyList<GeometryBundle> Finish()
    {
        var meshes = new List<GeometryBundle>();
        foreach (var writer in _glow.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => pair.Value))
        {
            foreach (var bundle in writer.Finish().Meshes)
            {
                var flags = new byte[bundle.Position.Length / 3];
                Array.Fill(flags, (byte)(VertexFlag.Interior | VertexFlag.NoShadow));
                var lit = bundle with { Flags = flags, IgnoreOcclusionCulling = true };
                lit.ValidateVertexData();
                meshes.Add(lit);
            }
        }
        return meshes;
    }
}
