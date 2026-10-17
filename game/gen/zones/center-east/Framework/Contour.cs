using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Parts.Core;

namespace Campus.Gen.Zones.CenterEast.Framework;

/// <summary>
/// Plan rings for rounded, terraced massing (the 21c liberal-arts group): half-plane clips for set-back storeys, corner
/// fillets, miter offsets, cells of equal arclength for ribbon glazing, and slabs and ring walls whose sides keep smooth
/// normals round the curves so a rounded floor edge shades as one band (ART-MODELING). Rings are canonical: outside
/// on the right of the path (<see cref="PlanMath.OutsideRight"/>), so an edge a→b faces out along (b − a) turned to (dz, −dx).
/// Scalar arithmetic and DetMath only.
/// </summary>
public static class Contour
{
    /// <summary>Corners turning less than this keep one shared normal on a slab side; sharper ones split.</summary>
    private const float SmoothCos = 0.766f;

    /// <summary>The ring kept where (p − at)·dir ≤ keep (dir need not be unit), canonical. Concave rings stay one ring when the line cuts them once.</summary>
    public static Vector2[] Clip(IReadOnlyList<Vector2> ring, Vector2 at, Vector2 dir, float keep)
    {
        ArgumentNullException.ThrowIfNull(ring);
        var n = Unit(dir);
        var output = new List<Vector2>();
        for (var i = 0; i < ring.Count; i++)
        {
            Vector2 a = ring[i], b = ring[(i + 1) % ring.Count];
            float da = Dot(a - at, n) - keep, db = Dot(b - at, n) - keep;
            if (da <= 0) output.Add(a);
            if ((da < 0 && db > 0) || (da > 0 && db < 0)) output.Add(a + (b - a) * (da / (da - db)));
        }
        return Clean(output, 0.05f);
    }

    /// <summary>Canonical ring without repeated points or straight-through vertices.</summary>
    public static Vector2[] Clean(IReadOnlyList<Vector2> ring, float minEdge)
    {
        ArgumentNullException.ThrowIfNull(ring);
        var points = new List<Vector2>();
        foreach (var p in ring)
            if (points.Count == 0 || Vector2.Distance(points[^1], p) >= minEdge) points.Add(p);
        while (points.Count > 1 && Vector2.Distance(points[0], points[^1]) < minEdge) points.RemoveAt(points.Count - 1);
        for (var changed = true; changed && points.Count > 3;)
        {
            changed = false;
            for (var i = 0; i < points.Count && points.Count > 3; i++)
            {
                Vector2 p = points[(i + points.Count - 1) % points.Count], q = points[i], r = points[(i + 1) % points.Count];
                if (MathF.Abs(Cross(q - p, r - q)) < 1e-4f * Vector2.Distance(p, r) && Dot(q - p, r - q) > 0)
                {
                    points.RemoveAt(i);
                    changed = true;
                }
            }
        }
        return PlanMath.OutsideRight(points);
    }

    /// <summary>
    /// Rounds every corner turning more than <paramref name="minTurnDegrees"/> with a parabolic fillet of up to
    /// <paramref name="radius"/>, each tangent run limited to 45 % of the shorter adjacent edge; corners for which
    /// <paramref name="pinned"/> answers true (where the ring meets a neighbouring block) stay sharp.
    /// </summary>
    public static Vector2[] Fillet(IReadOnlyList<Vector2> ring, float radius, float minTurnDegrees, Func<Vector2, bool> pinned)
    {
        ArgumentNullException.ThrowIfNull(ring);
        ArgumentNullException.ThrowIfNull(pinned);
        var points = new List<Vector2>();
        var count = ring.Count;
        for (var i = 0; i < count; i++)
        {
            Vector2 p = ring[(i + count - 1) % count], q = ring[i], r = ring[(i + 1) % count];
            Vector2 din = q - p, dout = r - q;
            float lin = din.Length(), lout = dout.Length();
            din /= lin; dout /= lout;
            var turn = MathF.Abs(DetMath.Atan2(Cross(din, dout), Dot(din, dout))) * (180f / MathF.PI);
            if (turn < minTurnDegrees || pinned(q)) { points.Add(q); continue; }
            // Tangent run of a circular fillet: r·tan(θ/2), tan(θ/2) = sinθ / (1 + cosθ).
            var half = MathF.Abs(Cross(din, dout)) / (1f + Dot(din, dout));
            var run = MathF.Min(radius * half, 0.45f * MathF.Min(lin, lout));
            Vector2 s = q - din * run, e = q + dout * run;
            var segments = Math.Max(2, (int)MathF.Ceiling(turn / 12f));
            for (var k = 0; k <= segments; k++)
            {
                var t = k / (float)segments;
                points.Add(s * ((1 - t) * (1 - t)) + q * (2 * t * (1 - t)) + e * (t * t));
            }
        }
        return Clean(points, 0.02f);
    }

    /// <summary>Miter offset, outward for positive <paramref name="d"/>; the miter is held to 2.5 |d| at sharp corners.</summary>
    public static Vector2[] Offset(IReadOnlyList<Vector2> ring, float d)
    {
        ArgumentNullException.ThrowIfNull(ring);
        var count = ring.Count;
        var output = new Vector2[count];
        for (var i = 0; i < count; i++)
        {
            Vector2 p = ring[(i + count - 1) % count], q = ring[i], r = ring[(i + 1) % count];
            Vector2 n0 = Outward(p, q), n1 = Outward(q, r);
            var m = (n0 + n1) / (1f + Dot(n0, n1));
            var length = m.Length();
            if (length > 2.5f) m *= 2.5f / length;
            output[i] = q + m * d;
        }
        return output;
    }

    /// <summary>Outward unit normal of a canonical ring's edge a→b.</summary>
    public static Vector2 Outward(Vector2 a, Vector2 b) => Unit(new(b.Y - a.Y, a.X - b.X));

    public static float Perimeter(IReadOnlyList<Vector2> ring)
    {
        var length = 0f;
        for (var i = 0; i < ring.Count; i++) length += Vector2.Distance(ring[i], ring[(i + 1) % ring.Count]);
        return length;
    }

    /// <summary>
    /// The ring cut into <c>round(perimeter / pitch)</c> cells of equal arclength from its first point: each cell is the
    /// polyline it covers (the cut points and the ring vertices between them), in ring order.
    /// </summary>
    public static IReadOnlyList<Vector2[]> Cells(IReadOnlyList<Vector2> ring, float pitch)
    {
        ArgumentNullException.ThrowIfNull(ring);
        // The closed path with its running length: point i of the ring sits at s[i], the ring's start again at s[n].
        var n = ring.Count;
        var s = new float[n + 1];
        for (var i = 0; i < n; i++) s[i + 1] = s[i] + Vector2.Distance(ring[i], ring[(i + 1) % n]);
        var total = s[n];
        var count = Math.Max(1, (int)MathF.Round(total / pitch));
        var cells = new Vector2[count][];
        for (var c = 0; c < count; c++)
        {
            float s0 = total * c / count, s1 = c == count - 1 ? total : total * (c + 1) / count;
            var points = new List<Vector2> { At(s0) };
            for (var i = 1; i < n; i++)
                if (s[i] > s0 + 1e-3f && s[i] < s1 - 1e-3f) points.Add(ring[i]);
            points.Add(At(s1));
            cells[c] = [.. points];
        }
        return cells;

        Vector2 At(float d)
        {
            var i = 0;
            while (i < n - 1 && s[i + 1] < d) i++;
            var length = s[i + 1] - s[i];
            return length > 1e-6f ? Vector2.Lerp(ring[i], ring[(i + 1) % n], Math.Clamp((d - s[i]) / length, 0f, 1f)) : ring[i];
        }
    }

    /// <summary>Point and outward normal halfway along a cell's polyline.</summary>
    public static (Vector2 Point, Vector2 Out) Middle(IReadOnlyList<Vector2> cell)
    {
        var total = 0f;
        for (var i = 0; i + 1 < cell.Count; i++) total += Vector2.Distance(cell[i], cell[i + 1]);
        var walked = 0f;
        for (var i = 0; i + 1 < cell.Count; i++)
        {
            var length = Vector2.Distance(cell[i], cell[i + 1]);
            if (walked + length >= total * 0.5f && length > 1e-4f)
                return (Vector2.Lerp(cell[i], cell[i + 1], (total * 0.5f - walked) / length), Outward(cell[i], cell[i + 1]));
            walked += length;
        }
        return (cell[0], Outward(cell[0], cell[^1]));
    }

    /// <summary>
    /// A horizontal slab on a canonical ring from y0 to y1: sides with smooth normals round gentle corners, flat caps, and
    /// a <paramref name="chamfer"/> along the top outer edge (0 for none); without <paramref name="caps"/> only the sides.
    /// </summary>
    public static PartGeometry Slab(IReadOnlyList<Vector2> ring, float y0, float y1, ToonSpec material, float chamfer = 0f, bool caps = true)
    {
        ArgumentNullException.ThrowIfNull(ring);
        var m = new Shapes.Writer(material);
        var count = ring.Count;
        chamfer = MathF.Min(chamfer, (y1 - y0) * 0.4f);
        var top = chamfer > 0 ? Offset(ring, -chamfer) : [.. ring];
        var sideTop = y1 - chamfer;
        var up = Vector3.UnitY;
        for (var i = 0; i < count; i++)
        {
            var j = (i + 1) % count;
            Vector2 a = ring[i], b = ring[j];
            var edge = Outward(a, b);
            var outward = new Vector3(edge.X, 0, edge.Y);
            var (na, nb) = (Normal(ring, i, edge), Normal(ring, j, edge));
            m.Quad(m.Vertex(new(a.X, y0, a.Y), na, new(0, 0)), m.Vertex(new(b.X, y0, b.Y), nb, new(1, 0)),
                m.Vertex(new(b.X, sideTop, b.Y), nb, new(1, 1)), m.Vertex(new(a.X, sideTop, a.Y), na, new(0, 1)), outward);
            if (chamfer > 0)
                m.Quad(m.Vertex(new(a.X, sideTop, a.Y), Solids.Unit(na + up), new(0, 0)), m.Vertex(new(b.X, sideTop, b.Y), Solids.Unit(nb + up), new(1, 0)),
                    m.Vertex(new(top[j].X, y1, top[j].Y), Solids.Unit(nb + up), new(1, 1)), m.Vertex(new(top[i].X, y1, top[i].Y), Solids.Unit(na + up), new(0, 1)), outward + up);
        }
        if (caps)
        {
            Cap(m, top, y1, 1f);
            Cap(m, ring, y0, -1f);
        }
        return m.Finish();
    }

    /// <summary>A ring wall (parapet) standing on y0: outer face on the ring, inner face <paramref name="thickness"/> inside, a flat top.</summary>
    public static PartGeometry Wall(IReadOnlyList<Vector2> ring, float thickness, float y0, float y1, ToonSpec material)
    {
        ArgumentNullException.ThrowIfNull(ring);
        var m = new Shapes.Writer(material);
        var inner = Offset(ring, -thickness);
        var count = ring.Count;
        var up = Vector3.UnitY;
        for (var i = 0; i < count; i++)
        {
            var j = (i + 1) % count;
            var edge = Outward(ring[i], ring[j]);
            var outward = new Vector3(edge.X, 0, edge.Y);
            var (na, nb) = (Normal(ring, i, edge), Normal(ring, j, edge));
            Vector2 a = ring[i], b = ring[j], c = inner[i], d = inner[j];
            m.Quad(m.Vertex(new(a.X, y0, a.Y), na, new(0, 0)), m.Vertex(new(b.X, y0, b.Y), nb, new(1, 0)),
                m.Vertex(new(b.X, y1, b.Y), nb, new(1, 1)), m.Vertex(new(a.X, y1, a.Y), na, new(0, 1)), outward);
            m.Quad(m.Vertex(new(c.X, y0, c.Y), -na, new(0, 0)), m.Vertex(new(d.X, y0, d.Y), -nb, new(1, 0)),
                m.Vertex(new(d.X, y1, d.Y), -nb, new(1, 1)), m.Vertex(new(c.X, y1, c.Y), -na, new(0, 1)), -outward);
            m.Quad(m.Vertex(new(a.X, y1, a.Y), up, new(0, 0)), m.Vertex(new(b.X, y1, b.Y), up, new(1, 0)),
                m.Vertex(new(d.X, y1, d.Y), up, new(1, 1)), m.Vertex(new(c.X, y1, c.Y), up, new(0, 1)), up);
        }
        return m.Finish();
    }

    /// <summary>Side normal at ring vertex i for the edge being drawn: shared across a gentle corner, the edge's own at a sharp one.</summary>
    private static Vector3 Normal(IReadOnlyList<Vector2> ring, int i, Vector2 edge)
    {
        var count = ring.Count;
        Vector2 n0 = Outward(ring[(i + count - 1) % count], ring[i]), n1 = Outward(ring[i], ring[(i + 1) % count]);
        var n = Dot(n0, n1) >= SmoothCos ? Unit(n0 + n1) : edge;
        return new(n.X, 0, n.Y);
    }

    private static void Cap(Shapes.Writer m, IReadOnlyList<Vector2> ring, float y, float sign)
    {
        var polygon = PolygonTriangulator.Triangulate(new PolygonRings(ring));
        var normal = new Vector3(0, sign, 0);
        var ids = new int[polygon.Vertices.Length];
        for (var i = 0; i < ids.Length; i++) ids[i] = m.Vertex(new(polygon.Vertices[i].X, y, polygon.Vertices[i].Y), normal, polygon.Vertices[i]);
        for (var i = 0; i < polygon.Indices.Length; i += 3) m.Triangle(ids[polygon.Indices[i]], ids[polygon.Indices[i + 1]], ids[polygon.Indices[i + 2]], normal);
    }

    private static float Dot(Vector2 a, Vector2 b) => a.X * b.X + a.Y * b.Y;

    private static float Cross(Vector2 a, Vector2 b) => a.X * b.Y - a.Y * b.X;

    private static Vector2 Unit(Vector2 v)
    {
        var length = MathF.Sqrt(v.X * v.X + v.Y * v.Y);
        return length > 1e-9f ? v / length : Vector2.UnitX;
    }
}
