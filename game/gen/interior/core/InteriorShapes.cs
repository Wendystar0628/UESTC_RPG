using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Geometry;
using Campus.Gen.Parts.Core;

namespace Campus.Gen.Interior;

/// <summary>Interior surface primitives sharing the storey's ordinary geometry stream.</summary>
public sealed class InteriorShapes(GeometrySink mesh, bool draw, float slabThickness)
{
    private static Vector3 AtHeight(Vector2 p, float y) => new(p.X, y, p.Y);

    /// <summary>A box with plan centre c, length along unit u, depth across it, from y0 to y1.</summary>
    public void Box(in InteriorPaint paint, Vector2 c, Vector2 u, float length, float depth, float y0, float y1)
    {
        if (!draw) return;
        var v = new Vector2(-u.Y, u.X);
        float hl = length * 0.5f;
        float hd = depth * 0.5f;
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
        if (!draw || y1 - y0 < 1e-4f || ring.Count < 3) return;
        var centre = Vector2.Zero;
        foreach (var p in ring)
        {
            centre += p;
        }
        centre /= ring.Count;
        for (var i = 1; i + 1 < ring.Count; i++)
        {
            mesh.Tri(paint, AtHeight(ring[0], y1), AtHeight(ring[i], y1), AtHeight(ring[i + 1], y1), Vector3.UnitY);
            mesh.Tri(paint, AtHeight(ring[0], y0), AtHeight(ring[i], y0), AtHeight(ring[i + 1], y0), -Vector3.UnitY);
        }
        for (var i = 0; i < ring.Count; i++)
        {
            Vector2 a = ring[i];
            Vector2 b = ring[(i + 1) % ring.Count];
            var edge = b - a;
            if (edge.LengthSquared() < 1e-10f) continue;
            var n = Vector2.Normalize(new Vector2(edge.Y, -edge.X));
            if (Vector2.Dot(n, (a + b) * 0.5f - centre) < 0f) n = -n;
            mesh.Quad(paint, AtHeight(a, y0), AtHeight(b, y0), AtHeight(b, y1), AtHeight(a, y1), new Vector3(n.X, 0f, n.Y));
        }
    }

    /// <summary>A closed six-faced solid: bottom corners and top corners in the same order around (sloped stair parts).</summary>
    public void Hexa(in InteriorPaint paint, Vector3[] bottom, Vector3[] top)
    {
        if (!draw) return;
        var centre = Vector3.Zero;
        foreach (var p in bottom)
        {
            centre += p;
        }
        foreach (var p in top)
        {
            centre += p;
        }
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
        if (!draw) return;
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
        if (!draw) return;
        var geometry = PartShapes.Box(new(-depth * 0.5f, y0, -length * 0.5f),
            new(depth * 0.5f, y1, length * 0.5f), bevel, paint.Material, out var at);
        mesh.Add(paint, geometry, at * PlanFrame.Along(c, new Vector2(u.Y, -u.X)).At(0, 0, 0));
    }

    /// <summary>A round solid standing on y0 (cups, flasks, lamp bases): radius, height, sides.</summary>
    public void Cylinder(in InteriorPaint paint, Vector2 c, float radius, float y0, float y1, int sides = 12)
    {
        if (!draw) return;
        var profile = PartShapes.Creased(new(0, 0), new(radius, 0), new(radius, y1 - y0), new(0, y1 - y0));
        mesh.Add(paint, PartShapes.Lathe(profile, sides, paint.Material), Matrix4x4.CreateTranslation(c.X, y0, c.Y));
    }

    /// <summary>A flat convex polygon facing up (inlays lie 2 mm proud of the floor, ART-MODELING).</summary>
    public void Flat(in InteriorPaint paint, IReadOnlyList<Vector2> ring, float y)
    {
        if (!draw) return;
        for (var i = 1; i + 1 < ring.Count; i++)
        {
            mesh.Tri(paint, AtHeight(ring[0], y), AtHeight(ring[i], y), AtHeight(ring[i + 1], y), Vector3.UnitY);
        }
    }

    /// <summary>A horizontal plate of simple, non-touching polygon rings, facing up or down (ceilings are their underside only).</summary>
    public void Plate(in InteriorPaint paint, PolygonRings rings, float y, bool up)
    {
        if (!draw) return;
        var t = PolygonTriangulator.Triangulate(rings);
        var n = up ? Vector3.UnitY : -Vector3.UnitY;
        for (var i = 0; i + 2 < t.Indices.Length; i += 3)
        {
            mesh.Tri(paint, AtHeight(t.Vertices[t.Indices[i]], y), AtHeight(t.Vertices[t.Indices[i + 1]], y),
                AtHeight(t.Vertices[t.Indices[i + 2]], y), n);
        }
    }

    /// <summary>
    /// A floor slab: top and soffit plates and the edge faces of its holes (the void's white slab edge). Outer edges stand
    /// behind the shell's walls and their linings, so they get no faces that could share the wall's plane.
    /// </summary>
    public void Slab(PolygonRings rings, float top, in InteriorPaint upper, in InteriorPaint? lower, in InteriorPaint edge)
    {
        if (!draw) return;
        Plate(upper, rings, top, true);
        if (lower is { } soffit) Plate(soffit, rings, top - slabThickness, false);
        foreach (var hole in rings.Holes)
        {
            for (var i = 0; i < hole.Count; i++)
            {
                Vector2 a = hole[i];
                Vector2 b = hole[(i + 1) % hole.Count];
                var mid = (a + b) * 0.5f;
                var n = Vector2.Normalize(new Vector2(b.Y - a.Y, a.X - b.X));
                // Faces point into the hole: toward the hole's own centre.
                var centre = hole.Aggregate(Vector2.Zero, (s, p) => s + p) / hole.Count;
                if (Vector2.Dot(n, centre - mid) < 0f) n = -n;
                mesh.Quad(edge, AtHeight(a, top - slabThickness), AtHeight(b, top - slabThickness), AtHeight(b, top), AtHeight(a, top), new Vector3(n.X, 0f, n.Y));
            }
        }
    }

    private void Face(in InteriorPaint paint, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 centre)
    {
        var n = Vector3.Cross(b - a, c - a);
        if (n.LengthSquared() < 1e-12f) n = Vector3.Cross(c - a, d - a);
        if (n.LengthSquared() < 1e-12f) return;
        n = Vector3.Normalize(n);
        if (Vector3.Dot(n, (a + b + c + d) * 0.25f - centre) < 0f) n = -n;
        mesh.Quad(paint, a, b, c, d, n);
    }

    public void Steps(RampCollider ramp, ToonSpec material)
    {
        int count = ramp.Stair?.Count ?? throw new ArgumentException("Interior stair geometry requires authored step metadata.", nameof(ramp));
        for (int i = 0; i < count; i++)
        {
            float start = (float)i / count;
            float end = (float)(i + 1) / count;
            Vector3 a = Vector3.Lerp(ramp.P0, ramp.P3, start);
            Vector3 b = Vector3.Lerp(ramp.P1, ramp.P2, start);
            Vector3 c = Vector3.Lerp(ramp.P1, ramp.P2, end);
            Vector3 d = Vector3.Lerp(ramp.P0, ramp.P3, end);
            a.Y = b.Y = c.Y;
            StepPrism([a, b, c, d], ramp.Thick + (ramp.P3.Y - ramp.P0.Y) / count, material);
        }
    }

    private void StepPrism(Vector3[] top, float thickness, ToonSpec material)
    {
        var down = Vector3.UnitY * thickness;
        // Rings are clockwise when seen from +Y; reversed indices below match Godot's front-face winding.
        StepFace(top[3], top[2], top[1], top[0], material);
        StepFace(top[0] - down, top[1] - down, top[2] - down, top[3] - down, material);
        for (int i = 0; i < 4; i++)
        {
            int next = (i + 1) % 4;
            StepFace(top[i], top[next], top[next] - down, top[i] - down, material);
        }
    }

    private void StepFace(Vector3 a, Vector3 b, Vector3 c, Vector3 d, ToonSpec material)
    {
        Vector3 normal = Vector3.Normalize(Vector3.Cross(b - a, c - a));
        mesh.Quad(material, new(a, normal), new(b, normal), new(c, normal), new(d, normal));
    }
}
