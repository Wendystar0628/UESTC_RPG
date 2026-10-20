using Campus.Gen.Geometry;
using Campus.Contracts;
using System.Numerics;
using Campus.Gen.Buildings.Plan;

namespace Campus.Gen.Buildings.Mesh;

/// <summary>A point of a section profile: s is the offset along the outward normal (negative = into the material), y the height.</summary>
public readonly record struct ProfilePoint(float S, float Y, float Chamfer = 0f, float Ao = 0f);

/// <summary>
/// A closed or open section profile, traversed so that the material lies on the right of travel in the (s, y) plane:
/// up the front, inward across the top, down the back, outward across a soffit. Segment normal = (dy, -ds).
/// A bevel segment takes its neighbours' normals at its two ends (custom bevel normals, ART-MODELING).
/// </summary>
public sealed class Profile
{
    public Profile(IReadOnlyList<ProfilePoint> points, IReadOnlyList<bool>? bevel = null)
    {
        if (points.Count < 2) throw new ArgumentException("A profile needs at least two points.", nameof(points));
        Points = points;
        Bevel = bevel ?? new bool[points.Count - 1];
        if (Bevel.Count != points.Count - 1) throw new ArgumentException("One bevel flag per segment.", nameof(bevel));
        var seg = new Vector2[points.Count - 1];
        for (var i = 0; i < seg.Length; i++)
        {
            var ds = points[i + 1].S - points[i].S;
            var dy = points[i + 1].Y - points[i].Y;
            seg[i] = VectorMath.Unit(new Vector2(dy, -ds), Vector2.UnitX, 1e-6f);
        }
        SegmentNormal = seg;
    }

    public IReadOnlyList<ProfilePoint> Points { get; }
    public IReadOnlyList<bool> Bevel { get; }
    /// <summary>(outward component, up component) of each segment's normal.</summary>
    public IReadOnlyList<Vector2> SegmentNormal { get; }

    /// <summary>Normal used at the start (end = false) or end (end = true) of segment i.</summary>
    public Vector2 NormalAt(int i, bool end)
    {
        if (!Bevel[i]) return SegmentNormal[i];
        if (!end) return i > 0 ? SegmentNormal[i - 1] : SegmentNormal[i];
        return i + 1 < SegmentNormal.Count ? SegmentNormal[i + 1] : SegmentNormal[i];
    }

    /// <summary>Builds a profile from points and the indices of segments that are bevels.</summary>
    public static Profile Of(IReadOnlyList<ProfilePoint> points, params int[] bevelSegments)
    {
        var flags = new bool[points.Count - 1];
        foreach (var b in bevelSegments) flags[b] = true;
        return new Profile(points, flags);
    }
}

/// <summary>A span [U0, U1] (skin u from the edge start) of one ring edge left open by a sweep, closed by end caps.</summary>
public readonly record struct SweepGap(int Edge, float U0, float U1);

/// <summary>Geometry primitives in plan rings and edge-local frames.</summary>
public static class Prims
{
    public static Vector3 W(Vector2 plan, float y) => new(plan.X, y, plan.Y);
    public static Vector3 W3(Vector2 planDir) => new(planDir.X, 0f, planDir.Y);

    /// <summary>A corner can take a plan chamfer: outside corner, turning by at least 20°, both edges long enough.</summary>
    public static bool Chamferable(PlanRing r, int corner, float c) =>
        c > 0f && r.Convex[corner] && r.TurnCos[corner] < 0.94f &&
        r.Length[(corner + r.Count - 1) % r.Count] > 2f * c + 0.1f && r.Length[corner] > 2f * c + 0.1f;

    /// <summary>Outline at offset s with plan chamfer c: two points per corner (equal where no chamfer applies).</summary>
    public static Vector2[] Outline(PlanRing r, float s, float c)
    {
        var o = new Vector2[2 * r.Count];
        for (var i = 0; i < r.Count; i++)
        {
            var m = r.Mitre(i, s);
            if (Chamferable(r, i, c))
            {
                o[2 * i] = m - r.Dir[(i + r.Count - 1) % r.Count] * c;
                o[2 * i + 1] = m + r.Dir[i] * c;
            }
            else
            {
                o[2 * i] = m;
                o[2 * i + 1] = m;
            }
        }
        return o;
    }

    /// <summary>
    /// Sweeps a section profile around a ring. Edge faces use the edge normal; plan-chamfer corner faces
    /// interpolate from the incoming to the outgoing edge normal. Segments of zero length or flipped edges are skipped.
    /// <paramref name="gaps"/> leave spans of edges open (non-overlapping), each closed by end caps.
    /// </summary>
    public static void Sweep(GeometrySink sink, in Face faceIn, PlanRing ring, Profile profile, IReadOnlyList<SweepGap>? gaps = null, Vector2? capClosure = null)
    {
        var face = faceIn;
        var pts = profile.Points;
        var outlines = new Vector2[pts.Count][];
        for (var j = 0; j < pts.Count; j++) outlines[j] = Outline(ring, pts[j].S, pts[j].Chamfer);
        var perim = new float[ring.Count + 1];
        for (var i = 0; i < ring.Count; i++) perim[i + 1] = perim[i] + ring.Length[i];
        var vAcc = new float[pts.Count];
        for (var j = 1; j < pts.Count; j++)
            vAcc[j] = vAcc[j - 1] + Vector2.Distance(new(pts[j].S, pts[j].Y), new(pts[j - 1].S, pts[j - 1].Y));
        for (var j = 0; j + 1 < pts.Count; j++)
        {
            var p0 = pts[j];
            var p1 = pts[j + 1];
            if (MathF.Abs(p0.S - p1.S) < 1e-5f && MathF.Abs(p0.Y - p1.Y) < 1e-5f) continue;
            var nStart = profile.NormalAt(j, false);
            var nEnd = profile.NormalAt(j, true);
            var o0 = outlines[j];
            var o1 = outlines[j + 1];
            for (var i = 0; i < ring.Count; i++)
            {
                var prev = (i + ring.Count - 1) % ring.Count;
                var next = (i + 1) % ring.Count;
                // Corner face a_i -> b_i.
                if (Vector2.DistanceSquared(o0[2 * i], o0[2 * i + 1]) > 1e-10f || Vector2.DistanceSquared(o1[2 * i], o1[2 * i + 1]) > 1e-10f)
                {
                    var na = ring.Normal[prev];
                    var nb = ring.Normal[i];
                    sink.Quad(face,
                        new V(W(o0[2 * i], p0.Y), N(na, nStart), new(perim[i], vAcc[j]), p0.Ao),
                        new V(W(o0[2 * i + 1], p0.Y), N(nb, nStart), new(perim[i], vAcc[j]), p0.Ao),
                        new V(W(o1[2 * i + 1], p1.Y), N(nb, nEnd), new(perim[i], vAcc[j + 1]), p1.Ao),
                        new V(W(o1[2 * i], p1.Y), N(na, nEnd), new(perim[i], vAcc[j + 1]), p1.Ao));
                }
                // Edge face b_i -> a_{i+1}.
                var b0 = o0[2 * i + 1];
                var a0 = o0[2 * next];
                var b1 = o1[2 * i + 1];
                var a1 = o1[2 * next];
                if (Vector2.Dot(a0 - b0, ring.Dir[i]) <= 1e-4f && Vector2.Dot(a1 - b1, ring.Dir[i]) <= 1e-4f) continue;
                var n = ring.Normal[i];
                var cuts = gaps?.Where(g => g.Edge == i).OrderBy(g => g.U0).ToArray() ?? [];
                if (cuts.Length > 0)
                {
                    // Pieces between the gaps; the cut lines are perpendicular to the edge.
                    var o = ring.Points[i];
                    var d = ring.Dir[i];
                    Vector2 At(Vector2 onLine, float u) => onLine + d * (u - Vector2.Dot(onLine - o, d));
                    var (s0, s1) = (b0, b1);
                    foreach (var g in cuts)
                    {
                        EdgeQuad(s0, At(s0, g.U0), s1, At(s1, g.U0));
                        (s0, s1) = (At(a0, g.U1), At(a1, g.U1));
                    }
                    EdgeQuad(s0, a0, s1, a1);
                    continue;
                }
                EdgeQuad(b0, a0, b1, a1);

                void EdgeQuad(Vector2 s0, Vector2 e0, Vector2 s1, Vector2 e1)
                {
                    if (Vector2.Dot(e0 - s0, ring.Dir[i]) <= 1e-4f && Vector2.Dot(e1 - s1, ring.Dir[i]) <= 1e-4f) return;
                    sink.Quad(face,
                        new V(W(s0, p0.Y), N(n, nStart), new(perim[i], vAcc[j]), p0.Ao),
                        new V(W(e0, p0.Y), N(n, nStart), new(perim[i + 1], vAcc[j]), p0.Ao),
                        new V(W(e1, p1.Y), N(n, nEnd), new(perim[i + 1], vAcc[j + 1]), p1.Ao),
                        new V(W(s1, p1.Y), N(n, nEnd), new(perim[i], vAcc[j + 1]), p1.Ao));
                }
            }
        }
        foreach (var gg in gaps ?? []) GapCaps(sink, face, ring, profile, gg, capClosure);
    }

    /// <summary>End caps of a swept section at a gap: the section polygon (plus an optional closing point on the wall).</summary>
    private static void GapCaps(GeometrySink sink, in Face face, PlanRing ring, Profile profile, SweepGap g, Vector2? closure)
    {
        var poly = profile.Points.Select(p => new Vector2(p.S, p.Y)).ToList();
        if (closure is { } c) poly.Add(c);
        var (cp, tris) = Triangulate(poly, []);
        if (tris.Length == 0) return;
        var f = EdgeFrame.Of(ring, g.Edge);
        var a = f.Along;
        sink.Polygon(face, cp.Select(p => new V(f.At(g.U0, p.X, p.Y), a, new(p.X, p.Y))).ToArray(), tris);
        sink.Polygon(face, cp.Select(p => new V(f.At(g.U1, p.X, p.Y), -a, new(p.X, p.Y))).ToArray(), tris);
    }

    /// <summary>Horizontal cap over the ring outline at offset s (with chamfers), facing up or down; holes optional.</summary>
    public static void Cap(GeometrySink sink, in Face face, PlanRing outer, IReadOnlyList<PlanRing> holes, float s, float c, float y, bool up)
    {
        var o = PlanRings.DeduplicateRing(Outline(outer, s, c));
        var hs = holes.Select(h => (IReadOnlyList<Vector2>)PlanRings.DeduplicateRing(Outline(h, s, c))).ToArray();
        CapPoints(sink, face, o, hs, y, up);
    }

    public static void CapPoints(GeometrySink sink, in Face face, IReadOnlyList<Vector2> outer, IReadOnlyList<IReadOnlyList<Vector2>> holes, float y, bool up)
    {
        var (points, tris) = Triangulate(outer, holes);
        if (tris.Length == 0) return;
        var n = up ? Vector3.UnitY : -Vector3.UnitY;
        var verts = points.Select(p => new V(W(p, y), n, p)).ToArray();
        sink.Polygon(face, verts, tris);
    }

    /// <summary>
    /// Strict earcut (parts kernel) first; it rejects touching or self-touching rings, in which case the
    /// unchecked earcut entry keeps the cap closed instead of dropping it.
    /// </summary>
    public static (Vector2[] Points, int[] Triangles) Triangulate(IReadOnlyList<Vector2> outer, IReadOnlyList<IReadOnlyList<Vector2>> holes)
    {
        // PolygonTriangulator orients the rings itself.
        var rings = new Campus.Contracts.PolygonRings(outer, holes);
        try
        {
            var t = Campus.Gen.Parts.Core.PolygonTriangulator.Triangulate(rings);
            return (t.Vertices, t.Indices);
        }
        catch (ArgumentException)
        {
            var t = Campus.Gen.Parts.Core.PolygonTriangulator.TriangulateUnchecked(rings);
            return (t.Vertices, t.Indices);
        }
    }

    private static Vector3 N(Vector2 plan, Vector2 section) =>
        new Vector3(plan.X * section.X, section.Y, plan.Y * section.X);

}

/// <summary>
/// Local frame of one facade edge: u along the edge from its start corner, s outward, y up.
/// Facade attachments (sills, frames, pilasters, canopies) are built in this frame.
/// </summary>
public sealed class EdgeFrame(Vector2 origin, Vector2 dir, Vector2 normal)
{
    public Vector2 Origin { get; } = origin;
    public Vector2 Dir { get; } = dir;
    public Vector2 Normal { get; } = normal;
    public Vector3 Along => Prims.W3(Dir);
    public Vector3 Out => Prims.W3(Normal);

    public static EdgeFrame Of(PlanRing r, int edge) => new(r.Points[edge], r.Dir[edge], r.Normal[edge]);

    public Vector3 At(float u, float s, float y) => new(Origin.X + Dir.X * u + Normal.X * s, y, Origin.Y + Dir.Y * u + Normal.Y * s);

    /// <summary>Axis-aligned box in the frame. The back face (s0) is omitted when it sits inside the wall.</summary>
    public void Box(GeometrySink sink, in Face face, float u0, float u1, float s0, float s1, float y0, float y1,
        bool back = false, bool bottom = true, float aoBottom = 0f)
    {
        if (u1 - u0 < 1e-4f || s1 - s0 < 1e-4f || y1 - y0 < 1e-4f) return;
        var o = Out;
        var a = Along;
        // front
        sink.Quad(face, V(u0, s1, y0, o, u0, y0, 0f), V(u1, s1, y0, o, u1, y0, 0f), V(u1, s1, y1, o, u1, y1, 0f), V(u0, s1, y1, o, u0, y1, 0f));
        // top
        sink.Quad(face, V(u0, s0, y1, Vector3.UnitY, u0, s0, 0f), V(u1, s0, y1, Vector3.UnitY, u1, s0, 0f), V(u1, s1, y1, Vector3.UnitY, u1, s1, 0f), V(u0, s1, y1, Vector3.UnitY, u0, s1, 0f));
        if (bottom)
            sink.Quad(face, V(u0, s0, y0, -Vector3.UnitY, u0, s0, aoBottom), V(u1, s0, y0, -Vector3.UnitY, u1, s0, aoBottom), V(u1, s1, y0, -Vector3.UnitY, u1, s1, 0f), V(u0, s1, y0, -Vector3.UnitY, u0, s1, 0f));
        // ends
        sink.Quad(face, V(u0, s0, y0, -a, s0, y0, 0f), V(u0, s1, y0, -a, s1, y0, 0f), V(u0, s1, y1, -a, s1, y1, 0f), V(u0, s0, y1, -a, s0, y1, 0f));
        sink.Quad(face, V(u1, s0, y0, a, s0, y0, 0f), V(u1, s1, y0, a, s1, y0, 0f), V(u1, s1, y1, a, s1, y1, 0f), V(u1, s0, y1, a, s0, y1, 0f));
        if (back)
            sink.Quad(face, V(u0, s0, y0, -o, u0, y0, 0f), V(u1, s0, y0, -o, u1, y0, 0f), V(u1, s0, y1, -o, u1, y1, 0f), V(u0, s0, y1, -o, u0, y1, 0f));
    }

    /// <summary>
    /// Sweeps an open section (s, y) along u from u0 to u1, with flat end caps (the section polygon closed
    /// along its back). Used for sills, canopies and short bands.
    /// </summary>
    public void Bar(GeometrySink sink, in Face face, float u0, float u1, Profile section, bool caps = true)
    {
        if (u1 - u0 < 1e-4f) return;
        var pts = section.Points;
        for (var j = 0; j + 1 < pts.Count; j++)
        {
            var p0 = pts[j];
            var p1 = pts[j + 1];
            if (MathF.Abs(p0.S - p1.S) < 1e-5f && MathF.Abs(p0.Y - p1.Y) < 1e-5f) continue;
            var n0 = section.NormalAt(j, false);
            var n1 = section.NormalAt(j, true);
            var nn0 = Out * n0.X + Vector3.UnitY * n0.Y;
            var nn1 = Out * n1.X + Vector3.UnitY * n1.Y;
            sink.Quad(face,
                new V(At(u0, p0.S, p0.Y), nn0, new(u0, p0.Y), p0.Ao), new V(At(u1, p0.S, p0.Y), nn0, new(u1, p0.Y), p0.Ao),
                new V(At(u1, p1.S, p1.Y), nn1, new(u1, p1.Y), p1.Ao), new V(At(u0, p1.S, p1.Y), nn1, new(u0, p1.Y), p1.Ao));
        }
        if (!caps) return;
        var poly = pts.Select(p => new Vector2(p.S, p.Y)).ToArray();
        var (cp, tris) = Prims.Triangulate(poly, []);
        if (tris.Length == 0) return;
        var left = cp.Select(p => new V(At(u0, p.X, p.Y), -Along, new(p.X, p.Y))).ToArray();
        var right = cp.Select(p => new V(At(u1, p.X, p.Y), Along, new(p.X, p.Y))).ToArray();
        sink.Polygon(face, left, tris);
        sink.Polygon(face, right, tris);
    }

    /// <summary>Vertical planar quad in the frame at offset s.</summary>
    public void Panel(GeometrySink sink, in Face face, float u0, float u1, float s, float y0, float y1, Vector3 normal,
        Vector2 uv0, Vector2 uv1, float ao = 0f)
    {
        if (u1 - u0 < 1e-4f || y1 - y0 < 1e-4f) return;
        sink.Quad(face,
            new V(At(u0, s, y0), normal, new(uv0.X, uv0.Y), ao), new V(At(u1, s, y0), normal, new(uv1.X, uv0.Y), ao),
            new V(At(u1, s, y1), normal, new(uv1.X, uv1.Y), ao), new V(At(u0, s, y1), normal, new(uv0.X, uv1.Y), ao));
    }

    private V V(float u, float s, float y, Vector3 n, float uvx, float uvy, float ao) => new(At(u, s, y), n, new(uvx, uvy), ao);
}
