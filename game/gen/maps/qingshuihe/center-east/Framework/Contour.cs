using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Parts.Core;
using Campus.Gen.World;
using Campus.Gen.Sites;

namespace Campus.Gen.Maps.Qingshuihe.CenterEast.Framework;

/// <summary>
/// Plan rings for rounded, terraced massing (the 21c liberal-arts group): half-plane clips for set-back storeys, corner
/// fillets, miter offsets, cells of equal arclength for ribbon glazing, and slabs and ring walls whose sides keep smooth
/// normals round the curves so a rounded floor edge shades as one band (ART-MODELING). Rings are canonical: outside
/// on the right of the path (<see cref="PlanMath.OutsideRight"/>), so an edge a→b faces out along (b − a) turned to (dz, −dx).
/// Scalar arithmetic and standard math.
/// </summary>
public static class Contour
{
    /// <summary>Corners turning less than this keep one shared normal on a slab side; sharper ones split.</summary>
    private const float SmoothCos = 0.766f;

    /// <summary>The ring kept where (p − at)·dir ≤ keep (dir need not be unit), canonical. Concave rings stay one ring when the line cuts them once.</summary>
    public static Vector2[] Clip(IReadOnlyList<Vector2> ring, Vector2 at, Vector2 dir, float keep)
    {
        ArgumentNullException.ThrowIfNull(ring);
        var n = VectorMath.Unit(dir);
        var output = new List<Vector2>();
        for (var i = 0; i < ring.Count; i++)
        {
            Vector2 a = ring[i], b = ring[(i + 1) % ring.Count];
            float da = Vector2.Dot(a - at, n) - keep, db = Vector2.Dot(b - at, n) - keep;
            if (da <= 0) output.Add(a);
            if ((da < 0 && db > 0) || (da > 0 && db < 0)) output.Add(a + (b - a) * (da / (da - db)));
        }
        return PlanRings.CleanRing(output, minEdge: 0.05f, collinearTolerance: 1e-4f) ?? throw new InvalidOperationException("Setback leaves no contour.");
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
            var turn = MathF.Abs(MathF.Atan2(PlanRings.CrossFloat(din, dout), Vector2.Dot(din, dout))) * (180f / MathF.PI);
            if (turn < minTurnDegrees || pinned(q)) { points.Add(q); continue; }
            // Tangent run of a circular fillet: r·tan(θ/2), tan(θ/2) = sinθ / (1 + cosθ).
            var half = MathF.Abs(PlanRings.CrossFloat(din, dout)) / (1f + Vector2.Dot(din, dout));
            var run = MathF.Min(radius * half, 0.45f * MathF.Min(lin, lout));
            Vector2 s = q - din * run, e = q + dout * run;
            var segments = Math.Max(2, (int)MathF.Ceiling(turn / 12f));
            for (var k = 0; k <= segments; k++)
            {
                var t = k / (float)segments;
                points.Add(s * ((1 - t) * (1 - t)) + q * (2 * t * (1 - t)) + e * (t * t));
            }
        }
        return PlanRings.CleanRing(points, minEdge: 0.02f, collinearTolerance: 1e-4f) ?? throw new InvalidOperationException("Rounded outline has no contour.");
    }

    /// <summary>Equal arc-length facade cells, retaining each source vertex inside its cell.</summary>
    public static IReadOnlyList<Vector2[]> Cells(IReadOnlyList<Vector2> ring, float pitch)
    {
        var total = PlanLines.Length(ring, closed: true);
        var count = Math.Max(1, (int)Math.Round(total / pitch));
        var stations = new double[ring.Count];
        for (var i = 1; i < ring.Count; i++) stations[i] = stations[i - 1] + Vector2.Distance(ring[i - 1], ring[i]);
        return Enumerable.Range(0, count).Select(c =>
        {
            var start = total * c / count;
            var end = total * (c + 1) / count;
            return new[] { PlanLines.Sample(ring, start, closed: true).Point }
                .Concat(Enumerable.Range(1, ring.Count - 1).Where(i => stations[i] > start + 1e-3 && stations[i] < end - 1e-3).Select(i => ring[i]))
                .Append(PlanLines.Sample(ring, end, closed: true).Point).ToArray();
        }).ToArray();
    }

    /// <summary>Point and outward normal halfway along a glazing cell.</summary>
    public static (Vector2 Point, Vector2 Out) Middle(IReadOnlyList<Vector2> cell)
    {
        var sample = PlanLines.Sample(cell, PlanLines.Length(cell) * 0.5);
        return (sample.Point, new(sample.Tangent.Y, -sample.Tangent.X));
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
        var top = chamfer > 0 ? PlanOffsets.MitreOffset(ring, -chamfer, closed: true, limit: 2.5f) : [.. ring];
        var sideTop = y1 - chamfer;
        var up = Vector3.UnitY;
        for (var i = 0; i < count; i++)
        {
            var j = (i + 1) % count;
            Vector2 a = ring[i], b = ring[j];
            var edge = PlanOffsets.RightUnitNormal(a, b);
            var outward = new Vector3(edge.X, 0, edge.Y);
            var (na, nb) = (Normal(ring, i, edge), Normal(ring, j, edge));
            m.Quad(m.Vertex(new(a.X, y0, a.Y), na, new(0, 0)), m.Vertex(new(b.X, y0, b.Y), nb, new(1, 0)),
                m.Vertex(new(b.X, sideTop, b.Y), nb, new(1, 1)), m.Vertex(new(a.X, sideTop, a.Y), na, new(0, 1)), outward);
            if (chamfer > 0)
                m.Quad(m.Vertex(new(a.X, sideTop, a.Y), VectorMath.Unit(na + up, minimumLength: 1e-12f), new(0, 0)), m.Vertex(new(b.X, sideTop, b.Y), VectorMath.Unit(nb + up, minimumLength: 1e-12f), new(1, 0)),
                    m.Vertex(new(top[j].X, y1, top[j].Y), VectorMath.Unit(nb + up, minimumLength: 1e-12f), new(1, 1)), m.Vertex(new(top[i].X, y1, top[i].Y), VectorMath.Unit(na + up, minimumLength: 1e-12f), new(0, 1)), outward + up);
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
        var inner = PlanOffsets.MitreOffset(ring, -thickness, closed: true, limit: 2.5f);
        var count = ring.Count;
        var up = Vector3.UnitY;
        for (var i = 0; i < count; i++)
        {
            var j = (i + 1) % count;
            var edge = PlanOffsets.RightUnitNormal(ring[i], ring[j]);
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
        Vector2 n0 = PlanOffsets.RightUnitNormal(ring[(i + count - 1) % count], ring[i]), n1 = PlanOffsets.RightUnitNormal(ring[i], ring[(i + 1) % count]);
        var n = Vector2.Dot(n0, n1) >= SmoothCos ? VectorMath.Unit(n0 + n1) : edge;
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

}
