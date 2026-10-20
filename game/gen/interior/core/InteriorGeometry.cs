using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Gen.Parts.Core;
using Campus.Contracts.Parts;

namespace Campus.Gen.Interior;

/// <summary>
/// Shared storey geometry, emitted through one ordinary geometry stream, and colliders built from the same numbers. <see cref="Draw"/> off skips every mesh write, so a
/// collision-only build does no meshing; <see cref="Full"/> on adds furniture and traces (IF), off keeps the shell (IS).
/// </summary>
public sealed partial class InteriorGeometry(PaletteCatalog? palette = null)
{
    private readonly SortedDictionary<string, PartShapeWriter> _glow = new(StringComparer.Ordinal);

    public GeometrySink Mesh { get; } = new(palette);
    public List<Collider> Colliders { get; } = [];
    public bool Draw { get; init; } = true;
    public bool Full { get; init; } = true;

    public float SlabThickness { get; init; } = 0.25f;
    public Func<string, ToonSpec> GlowMaterial { get; init; } = _ => throw new InvalidOperationException("No emissive material was supplied.");

    private static Vector3 V(Vector2 p, float y) => new(p.X, y, p.Y);

    /// <summary>A box with plan centre c, length along unit u, depth across it, from y0 to y1.</summary>
    public void Box(in InteriorPaint paint, Vector2 c, Vector2 u, float length, float depth, float y0, float y1)
    {
        if (!Draw) return;
        var v = new Vector2(-u.Y, u.X);
        float hl = length * 0.5f, hd = depth * 0.5f;
        Prism(paint, [c - u * hl - v * hd, c + u * hl - v * hd, c + u * hl + v * hd, c - u * hl + v * hd], y0, y1);
    }

    /// <summary>A box between two plan points, depth across the run.</summary>
    public void Run(in InteriorPaint paint, Vector2 a, Vector2 b, float depth, float y0, float y1)
    {
        var d = b - a;
        var length = d.Length();
        if (length < 1e-3f) return;
        Box(paint, (a + b) * 0.5f, d / length, length, depth, y0, y1);
    }

    /// <summary>A closed vertical prism over a convex plan ring (any winding).</summary>
    public void Prism(in InteriorPaint paint, IReadOnlyList<Vector2> ring, float y0, float y1)
    {
        if (!Draw || y1 - y0 < 1e-4f || ring.Count < 3) return;
        var centre = Vector2.Zero;
        foreach (var p in ring) centre += p;
        centre /= ring.Count;
        for (var i = 1; i + 1 < ring.Count; i++)
        {
            Mesh.Tri(paint, V(ring[0], y1), V(ring[i], y1), V(ring[i + 1], y1), Vector3.UnitY);
            Mesh.Tri(paint, V(ring[0], y0), V(ring[i], y0), V(ring[i + 1], y0), -Vector3.UnitY);
        }
        for (var i = 0; i < ring.Count; i++)
        {
            Vector2 a = ring[i], b = ring[(i + 1) % ring.Count];
            var edge = b - a;
            if (edge.LengthSquared() < 1e-10f) continue;
            var n = Vector2.Normalize(new Vector2(edge.Y, -edge.X));
            if (Vector2.Dot(n, (a + b) * 0.5f - centre) < 0f) n = -n;
            Mesh.Quad(paint, V(a, y0), V(b, y0), V(b, y1), V(a, y1), new Vector3(n.X, 0f, n.Y));
        }
    }

    /// <summary>A closed six-faced solid: bottom corners and top corners in the same order around (sloped stair parts).</summary>
    public void Hexa(in InteriorPaint paint, Vector3[] bottom, Vector3[] top)
    {
        if (!Draw) return;
        var centre = Vector3.Zero;
        foreach (var p in bottom) centre += p;
        foreach (var p in top) centre += p;
        centre /= 8f;
        Face(paint, bottom[0], bottom[1], bottom[2], bottom[3], centre);
        Face(paint, top[0], top[1], top[2], top[3], centre);
        for (var i = 0; i < 4; i++)
        {
            var k = (i + 1) % 4;
            Face(paint, bottom[i], bottom[k], top[k], top[i], centre);
        }
    }

    /// <summary>A square bar between two points (lamp arms, rods), its cross-section turned to stay level.</summary>
    public void Bar(in InteriorPaint paint, Vector3 a, Vector3 b, float width)
    {
        if (!Draw) return;
        var axis = b - a;
        if (axis.LengthSquared() < 1e-8f) return;
        axis = Vector3.Normalize(axis);
        var side = MathF.Abs(axis.Y) > 0.95f ? Vector3.UnitX : Vector3.Normalize(Vector3.Cross(Vector3.UnitY, axis));
        var up = Vector3.Normalize(Vector3.Cross(axis, side));
        side *= width * 0.5f;
        up *= width * 0.5f;
        Hexa(paint, [a - side - up, a + side - up, a + side + up, a - side + up], [b - side - up, b + side - up, b + side + up, b - side + up]);
    }

    /// <summary>A chamfered box for the pieces seen up close (tables, plaque, columns): local length along u.</summary>
    public void Beveled(in InteriorPaint paint, Vector2 c, Vector2 u, float length, float depth, float y0, float y1, float bevel)
    {
        if (!Draw) return;
        var geometry = PartShapes.Box(new(-depth * 0.5f, y0, -length * 0.5f),
            new(depth * 0.5f, y1, length * 0.5f), bevel, paint.Material, out var at);
        Mesh.Add(paint, geometry, at * PlanFrame.Along(c, new Vector2(u.Y, -u.X)).At(0, 0, 0));
    }

    /// <summary>A round solid standing on y0 (cups, flasks, lamp bases): radius, height, sides.</summary>
    public void Cylinder(in InteriorPaint paint, Vector2 c, float radius, float y0, float y1, int sides = 12)
    {
        if (!Draw) return;
        Mesh.Add(paint, PartShapes.Lathe(PartShapes.Creased(new(0, 0), new(radius, 0), new(radius, y1 - y0), new(0, y1 - y0)), sides, paint.Material), Matrix4x4.CreateTranslation(c.X, y0, c.Y));
    }

    /// <summary>A flat convex polygon facing up (inlays lie 2 mm proud of the floor, ART-MODELING).</summary>
    public void Flat(in InteriorPaint paint, IReadOnlyList<Vector2> ring, float y)
    {
        if (!Draw) return;
        for (var i = 1; i + 1 < ring.Count; i++) Mesh.Tri(paint, V(ring[0], y), V(ring[i], y), V(ring[i + 1], y), Vector3.UnitY);
    }

    /// <summary>A horizontal plate of any polygon with holes, facing up or down (ceilings are their underside only).</summary>
    public void Plate(in InteriorPaint paint, PolygonRings rings, float y, bool up)
    {
        if (!Draw) return;
        var t = PolygonTriangulator.TriangulateUnchecked(rings);
        var n = up ? Vector3.UnitY : -Vector3.UnitY;
        for (var i = 0; i + 2 < t.Indices.Length; i += 3)
            Mesh.Tri(paint, V(t.Vertices[t.Indices[i]], y), V(t.Vertices[t.Indices[i + 1]], y), V(t.Vertices[t.Indices[i + 2]], y), n);
    }

    /// <summary>
    /// A floor slab: top and soffit plates and the edge faces of its holes (the void's white slab edge). Outer edges stand
    /// behind the shell's walls and their linings, so they get no faces that could share the wall's plane.
    /// </summary>
    public void Slab(PolygonRings rings, float top, in InteriorPaint upper, in InteriorPaint? lower, in InteriorPaint edge)
    {
        if (!Draw) return;
        Plate(upper, rings, top, true);
        if (lower is { } soffit) Plate(soffit, rings, top - SlabThickness, false);
        foreach (var hole in rings.Holes)
            for (var i = 0; i < hole.Count; i++)
            {
                Vector2 a = hole[i], b = hole[(i + 1) % hole.Count];
                var mid = (a + b) * 0.5f;
                var n = Vector2.Normalize(new Vector2(b.Y - a.Y, a.X - b.X));
                // Faces point into the hole: toward the hole's own centre.
                var centre = hole.Aggregate(Vector2.Zero, (s, p) => s + p) / hole.Count;
                if (Vector2.Dot(n, centre - mid) < 0f) n = -n;
                Mesh.Quad(edge, V(a, top - SlabThickness), V(b, top - SlabThickness), V(b, top), V(a, top), new Vector3(n.X, 0f, n.Y));
            }
    }

    /// <summary>Walkable floor collision: one convex slab per triangle of the polygon.</summary>
    public void Floor(PolygonRings rings, float top, FootMat foot) => Floor(rings, top, SlabThickness, foot);

    public void Wall(Vector2 a, Vector2 b, float halfThickness, float y0, float y1, SurfaceTag tag, FootMat foot) =>
        Colliders.Add(new WallCollider(a, b, halfThickness, y0, y1, tag) { Foot = foot });

    /// <summary>A box collider with plan centre c, length along u, depth across, from y0 to y1.</summary>
    public void Solid(Vector2 c, Vector2 u, float length, float depth, float y0, float y1, SurfaceTag tag, FootMat foot) =>
        Colliders.Add(new BoxCollider(PlanFrame.Along(c, new Vector2(u.Y, -u.X)).At(0, (y0 + y1) * 0.5f, 0), new(depth * 0.5f, (y1 - y0) * 0.5f, length * 0.5f), tag) { Foot = foot });

    /// <summary>A self-lit box on the library light channel (downlights, lantern shades, lamp pools, circuit lines).</summary>
    public void Glow(string family, Vector2 c, Vector2 u, float length, float depth, float y0, float y1)
    {
        var v = new Vector2(-u.Y, u.X);
        float hl = length * 0.5f, hd = depth * 0.5f;
        Glow(family, [c - u * hl - v * hd, c + u * hl - v * hd, c + u * hl + v * hd, c - u * hl + v * hd], y0, y1);
    }

    /// <summary>A self-lit vertical prism over a convex plan ring (lamp pools, shade mouths).</summary>
    public void Glow(string family, IReadOnlyList<Vector2> ring, float y0, float y1)
    {
        if (!Draw || ring.Count < 3 || y1 - y0 < 1e-4f) return;
        var area = 0f;
        for (var i = 1; i + 1 < ring.Count; i++)
        {
            Vector2 p = ring[i] - ring[0], q = ring[i + 1] - ring[0];
            area += p.X * q.Y - p.Y * q.X;
        }
        if (MathF.Abs(area) < 1e-8f) return;
        if (!_glow.TryGetValue(family, out var found))
        {
            found = new PartShapeWriter(GlowMaterial(family));
            _glow.Add(family, found);
        }
        PartShapeWriter writer = found;
        var centre = Vector2.Zero;
        foreach (var p in ring) centre += p;
        centre /= ring.Count;
        for (var i = 1; i + 1 < ring.Count; i++)
        {
            GlowTriangle(writer, V(ring[0], y1), V(ring[i], y1), V(ring[i + 1], y1), Vector3.UnitY);
            GlowTriangle(writer, V(ring[0], y0), V(ring[i], y0), V(ring[i + 1], y0), -Vector3.UnitY);
        }
        for (var i = 0; i < ring.Count; i++)
        {
            Vector2 a = ring[i], b = ring[(i + 1) % ring.Count];
            if (Vector2.DistanceSquared(a, b) < 1e-10f) continue;
            var n = Vector2.Normalize(new Vector2(b.Y - a.Y, a.X - b.X));
            if (Vector2.Dot(n, (a + b) * 0.5f - centre) < 0f) n = -n;
            var n3 = new Vector3(n.X, 0f, n.Y);
            GlowTriangle(writer, V(a, y0), V(b, y0), V(b, y1), n3);
            GlowTriangle(writer, V(a, y0), V(b, y1), V(a, y1), n3);
        }
    }

    private static void GlowTriangle(PartShapeWriter writer, Vector3 a, Vector3 b, Vector3 c, Vector3 n)
    {
        if (Vector3.Cross(b - a, c - a).LengthSquared() <= 1e-14f) return;
        writer.Triangle(writer.Vertex(a, n, Vector2.Zero), writer.Vertex(b, n, Vector2.UnitX), writer.Vertex(c, n, Vector2.One), n);
    }

    /// <summary>The storey's bundles: every one is an interior surface that skips occlusion culling (WLD-INTERIORS).</summary>
    public ContentOutput Finish()
    {
        var meshes = new List<GeometryBundle>();
        foreach (var bundle in Mesh.Finish()) meshes.Add(bundle with { IgnoreOcclusionCulling = true });
        foreach (var writer in _glow.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => pair.Value))
        foreach (var bundle in writer.Finish().Meshes)
        {
            var flags = new byte[bundle.Position.Length / 3];
            Array.Fill(flags, (byte)(VertexFlag.Interior | VertexFlag.NoShadow));
            var lit = bundle with { Flags = flags, IgnoreOcclusionCulling = true };
            lit.ValidateVertexData();
            meshes.Add(lit);
        }
        return new ContentOutput(meshes, []);
    }

    private void Face(in InteriorPaint paint, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 centre)
    {
        var n = Vector3.Cross(b - a, c - a);
        if (n.LengthSquared() < 1e-12f) n = Vector3.Cross(c - a, d - a);
        if (n.LengthSquared() < 1e-12f) return;
        n = Vector3.Normalize(n);
        if (Vector3.Dot(n, (a + b + c + d) * 0.25f - centre) < 0f) n = -n;
        Mesh.Quad(paint, a, b, c, d, n);
    }
}
