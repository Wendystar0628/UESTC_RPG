using System.Numerics;
using Campus.Gen.Geometry;

namespace Campus.Gen.Water;

/// <summary>Flat surface of one water body with its per-vertex shore distance and flow.</summary>
public sealed record WaterMesh(
    WaterBody Body,
    float[] Position,   // x, y, z per vertex
    float[] Shore,      // distance to the nearest shore (m), clamped to WaterParams.SdfClamp
    float[] Flow,       // flow velocity (vx, vz) in m/s; zero on lakes
    int[] Index)        // triangles, front face up (D0 winding: (b - a) x (c - a) points down)
{
    public int VertexCount => Position.Length / 3;
    public int TriangleCount => Index.Length / 3;
}

/// <summary>
/// Meshes a water polygon on a grid of WaterParams.MeshStep: cells fully inside water are plain quads, cells the shore
/// passes through are cut into trapezoids along the shore. The shore distance is stored per vertex, so the three hard
/// colour bands and the foam line (WLD-WATER) need no mask texture. Pure and deterministic (D-165).
/// </summary>
public static class WaterMesher
{
    private readonly record struct Edge(Vector2 A, Vector2 B)
    {
        public float ZMin => MathF.Min(A.Y, B.Y);
        public float ZMax => MathF.Max(A.Y, B.Y);
        public bool Spans(float z) => (A.Y <= z && z < B.Y) || (B.Y <= z && z < A.Y);
        public float XAt(float z)
        {
            if (z == A.Y) return A.X;
            if (z == B.Y) return B.X;
            return A.X + (z - A.Y) * (B.X - A.X) / (B.Y - A.Y);
        }
    }

    private readonly record struct Shore(Vector2 A, Vector2 B);

    public static WaterMesh Build(WaterBody body, IReadOnlyList<WaterBody> all, WaterParams? parameters = null)
    {
        ArgumentNullException.ThrowIfNull(body);
        ArgumentNullException.ThrowIfNull(all);
        var p = parameters ?? WaterParams.Default;
        var edges = Edges(body);
        var builder = new Builder(body.SurfaceY);
        Tessellate(body, edges, p.MeshStep, p.SliverFraction, builder);
        var shores = ShoreSegments(body, all, p);
        var count = builder.VertexCount;
        var shore = new float[count];
        var flow = new float[count * 2];
        for (var i = 0; i < count; i++)
        {
            var v = new Vector2(builder.Positions[3 * i], builder.Positions[3 * i + 2]);
            var best = float.MaxValue;
            float sxx = 0f, sxz = 0f, szz = 0f;
            foreach (var s in shores)
            {
                var d = PlanGeometry.SegmentDistanceSquared(v, s.A, s.B);
                if (d < best) best = d;
                if (body.Kind != WaterKind.Channel || d > FlowReach * FlowReach) continue;
                // Orientation tensor of the nearby banks: the long banks outweigh an end cap or a joint.
                var e = s.B - s.A;
                var len = e.Length();
                if (len <= 1e-6f) continue;
                var u = e / len;
                var w = len / (1f + d / (FlowFalloff * FlowFalloff));
                sxx += w * u.X * u.X;
                sxz += w * u.X * u.Y;
                szz += w * u.Y * u.Y;
            }
            shore[i] = shores.Count == 0 ? p.SdfClamp : MathF.Min(MathF.Sqrt(best), p.SdfClamp);
            var tangent = PrincipalAxis(sxx, sxz, szz);
            if (body.Kind == WaterKind.Channel && tangent.LengthSquared() > 1e-12f)
            {
                var t = Vector2.Normalize(tangent);
                // WLD-WATER: every channel flows north to south (+z), east-west pieces flow east (+x).
                if (MathF.Abs(t.Y) >= 0.25f ? t.Y < 0f : t.X < 0f) t = -t;
                flow[2 * i] = t.X * p.FlowSpeed;
                flow[2 * i + 1] = t.Y * p.FlowSpeed;
            }
        }
        return new(body, builder.Positions.ToArray(), shore, flow, builder.Indices.ToArray());
    }

    private const float FlowReach = 15f;
    private const float FlowFalloff = 6f;

    /// <summary>Major eigenvector of the symmetric tensor [[a, b], [b, c]], unnormalised; zero when isotropic.</summary>
    private static Vector2 PrincipalAxis(float a, float b, float c)
    {
        var half = (a - c) * 0.5f;
        var lambda = (a + c) * 0.5f + MathF.Sqrt(half * half + b * b);
        var u = new Vector2(b, lambda - a);
        var w = new Vector2(lambda - c, b);
        return u.LengthSquared() >= w.LengthSquared() ? u : w;
    }

    private static List<Edge> Edges(WaterBody body)
    {
        var edges = new List<Edge>();
        AddRing(body.Rings.Outer);
        foreach (var hole in body.Rings.Holes) AddRing(hole);
        return edges;

        void AddRing(IReadOnlyList<Vector2> ring)
        {
            for (int i = 0, j = ring.Count - 1; i < ring.Count; j = i++)
                if (ring[i] != ring[j]) edges.Add(new(ring[j], ring[i]));
        }
    }

    // ----------------------------------------------------------------------------------------------------------------
    // Tessellation
    // ----------------------------------------------------------------------------------------------------------------

    private static void Tessellate(WaterBody body, List<Edge> edges, float step, float sliver, Builder builder)
    {
        var c0x = (int)MathF.Floor(body.Min.X / step);
        var c1x = (int)MathF.Floor(body.Max.X / step);
        var c0z = (int)MathF.Floor(body.Min.Y / step);
        var c1z = (int)MathF.Floor(body.Max.Y / step);
        var width = c1x - c0x + 1;
        var height = c1z - c0z + 1;
        var boundary = new bool[width * height];
        foreach (var e in edges)
        {
            var ax = (int)MathF.Floor(MathF.Min(e.A.X, e.B.X) / step) - c0x;
            var bx = (int)MathF.Floor(MathF.Max(e.A.X, e.B.X) / step) - c0x;
            var az = (int)MathF.Floor(e.ZMin / step) - c0z;
            var bz = (int)MathF.Floor(e.ZMax / step) - c0z;
            for (var cz = Math.Max(az - 1, 0); cz <= Math.Min(bz + 1, height - 1); cz++)
                for (var cx = Math.Max(ax - 1, 0); cx <= Math.Min(bx + 1, width - 1); cx++)
                {
                    var min = new Vector2((cx + c0x) * step, (cz + c0z) * step);
                    if (SegmentTouchesRect(e.A, e.B, min, min + new Vector2(step))) boundary[cz * width + cx] = true;
                }
        }

        // Extra vertices that boundary runs put on the walls of neighbouring inside cells, keyed by wall column.
        var wallRows = new Dictionary<(int Wall, int Row), List<float>>();
        for (var cz = 0; cz < height; cz++)
        {
            var z0 = (cz + c0z) * step;
            var z1 = z0 + step;
            var cx = 0;
            while (cx < width)
            {
                if (!boundary[cz * width + cx]) { cx++; continue; }
                var start = cx;
                while (cx < width && boundary[cz * width + cx]) cx++;
                var rows = TessellateRun(edges, (start + c0x) * step, (cx + c0x) * step, z0, z1, step, start + c0x, cx + c0x, builder);
                wallRows[(start + c0x, cz + c0z)] = rows;
                wallRows[(cx + c0x, cz + c0z)] = rows;
            }
        }

        for (var cz = 0; cz < height; cz++)
            for (var cx = 0; cx < width; cx++)
            {
                if (boundary[cz * width + cx]) continue;
                var x0 = (cx + c0x) * step;
                var z0 = (cz + c0z) * step;
                var centre = new Vector2(x0 + step * 0.5f, z0 + step * 0.5f);
                if (!body.Contains(centre)) continue;
                wallRows.TryGetValue((cx + c0x, cz + c0z), out var left);
                wallRows.TryGetValue((cx + c0x + 1, cz + c0z), out var right);
                InsideCell(x0, z0, step, left, right, builder);
            }
    }

    /// <summary>Cuts a horizontal run of shore cells into trapezoids; returns the rows it used (for neighbour walls).</summary>
    private static List<float> TessellateRun(List<Edge> edges, float xa, float xb, float z0, float z1, float step,
        int firstWall, int endWall, Builder builder)
    {
        // Rows: the cell borders and every ring vertex inside the run. Ring vertices lying on a row are pinned into
        // that row, so a vertex where two intervals meet (an island's tip) is shared by the bands above and below.
        var rows = new List<float> { z0, z1 };
        var pins = new List<Vector2>();
        foreach (var e in edges)
        {
            if (e.ZMax < z0 || e.ZMin > z1 || MathF.Max(e.A.X, e.B.X) < xa || MathF.Min(e.A.X, e.B.X) > xb) continue;
            Pin(e.A);
            Pin(e.B);
        }
        rows.Sort();
        var unique = new List<float>(rows.Count);
        foreach (var z in rows)
            if (unique.Count == 0 || z - unique[^1] > 1e-5f) unique.Add(z);
        unique[^1] = z1;
        var rowPins = new List<float>[unique.Count];
        for (var r = 0; r < unique.Count; r++) rowPins[r] = [];
        foreach (var v in pins)
        {
            var r = unique.BinarySearch(v.Y);
            if (r < 0) { r = ~r; if (r == unique.Count || (r > 0 && v.Y - unique[r - 1] < unique[r] - v.Y)) r--; }
            if (!rowPins[r].Contains(v.X)) rowPins[r].Add(v.X);
        }

        var active = new List<(float X, Edge E)>();
        for (var r = 0; r + 1 < unique.Count; r++)
        {
            var zb = unique[r];
            var zt = unique[r + 1];
            var zm = (zb + zt) * 0.5f;
            active.Clear();
            foreach (var e in edges)
                if (e.Spans(zm)) active.Add((e.XAt(zm), e));
            active.Sort((a, b) => a.X.CompareTo(b.X));
            var inside = false;
            var i = 0;
            for (; i < active.Count && active[i].X < xa; i++) inside = !inside;
            Edge? left = null;
            for (; i <= active.Count; i++)
            {
                var atEnd = i == active.Count || active[i].X >= xb;
                if (atEnd)
                {
                    if (inside) Trapezoid(left, null, xa, xb, zb, zt, rowPins[r], rowPins[r + 1], firstWall, endWall, step, builder);
                    break;
                }
                if (inside) Trapezoid(left, active[i].E, xa, xb, zb, zt, rowPins[r], rowPins[r + 1], firstWall, endWall, step, builder);
                else left = active[i].E;
                inside = !inside;
            }
        }
        return unique;

        void Pin(Vector2 v)
        {
            if (v.Y < z0 || v.Y > z1 || v.X < xa || v.X > xb) return;
            pins.Add(v);
            if (v.Y > z0 && v.Y < z1) rows.Add(v.Y);
        }
    }

    /// <summary>
    /// Triangulates one band of an interval between two rows. Each row carries its corners, every inner cell wall
    /// and every pinned ring vertex inside it; the two rows are zipped together, which is valid for any trapezoid.
    /// </summary>
    private static void Trapezoid(Edge? left, Edge? right, float xa, float xb, float zb, float zt,
        List<float> pinsBottom, List<float> pinsTop, int firstWall, int endWall, float step, Builder builder)
    {
        var bottom = Row(left?.XAt(zb) ?? xa, right?.XAt(zb) ?? xb, pinsBottom);
        var top = Row(left?.XAt(zt) ?? xa, right?.XAt(zt) ?? xb, pinsTop);
        var b = new int[bottom.Count];
        var t = new int[top.Count];
        for (var k = 0; k < bottom.Count; k++) b[k] = builder.Vertex(bottom[k], zb);
        for (var k = 0; k < top.Count; k++) t[k] = builder.Vertex(top[k], zt);
        int i = 0, j = 0;
        while (i < b.Length - 1 || j < t.Length - 1)
        {
            if (j == t.Length - 1 || (i < b.Length - 1 && bottom[i + 1] <= top[j + 1]))
            {
                builder.Triangle(b[i], b[i + 1], t[j]);
                i++;
            }
            else
            {
                builder.Triangle(b[i], t[j + 1], t[j]);
                j++;
            }
        }

        List<float> Row(float l, float r, List<float> pins)
        {
            const float eps = 1e-3f;
            var xs = new List<float> { l };
            for (var w = firstWall + 1; w < endWall; w++)
            {
                var g = w * step;
                if (g > l + eps && g < r - eps) xs.Add(g);
            }
            foreach (var x in pins)
                if (x > l + eps && x < r - eps) xs.Add(x);
            xs.Sort();
            var row = new List<float>(xs.Count + 1);
            foreach (var x in xs)
                if (row.Count == 0 || x - row[^1] > eps) row.Add(x);
            if (r - row[^1] > eps) row.Add(r);
            return row;
        }
    }

    private static void InsideCell(float x0, float z0, float step, List<float>? left, List<float>? right, Builder builder)
    {
        var x1 = x0 + step;
        var z1 = z0 + step;
        var extraLeft = Inner(left, z0, z1);
        var extraRight = Inner(right, z0, z1);
        if (extraLeft.Count == 0 && extraRight.Count == 0)
        {
            builder.Quad(x0, x1, x1, x0, z0, z1);
            return;
        }
        // Fan from the centre over the cell outline, so wall vertices shared with shore runs leave no T-junction.
        var ring = new List<Vector2> { new(x0, z0), new(x1, z0) };
        foreach (var z in extraRight) ring.Add(new(x1, z));
        ring.Add(new(x1, z1));
        ring.Add(new(x0, z1));
        for (var i = extraLeft.Count - 1; i >= 0; i--) ring.Add(new(x0, extraLeft[i]));
        var c = builder.Vertex(x0 + step * 0.5f, z0 + step * 0.5f);
        var outline = ring.Select(v => builder.Vertex(v.X, v.Y)).ToArray();
        for (var i = 0; i < outline.Length; i++) builder.Triangle(c, outline[i], outline[(i + 1) % outline.Length]);
    }

    private static List<float> Inner(List<float>? rows, float z0, float z1)
    {
        var result = new List<float>();
        if (rows is null) return result;
        foreach (var z in rows)
            if (z > z0 + 1e-5f && z < z1 - 1e-5f) result.Add(z);
        return result;
    }

    // ----------------------------------------------------------------------------------------------------------------
    // Shore distance
    // ----------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Shore pieces of this body and of nearby bodies. Edges are cut into pieces of at most 2 m; a piece is not shore
    /// when water continues on its outer side (river mouths, joints between OSM river pieces).
    /// </summary>
    private static List<Shore> ShoreSegments(WaterBody body, IReadOnlyList<WaterBody> all, WaterParams p)
    {
        var shores = new List<Shore>();
        foreach (var other in all)
        {
            if (other != body && !Near(body, other, p.NeighbourReach)) continue;
            foreach (var e in Edges(other))
            {
                var length = Vector2.Distance(e.A, e.B);
                var pieces = Math.Max(1, (int)MathF.Ceiling(length / 2f));
                for (var k = 0; k < pieces; k++)
                {
                    var a = Vector2.Lerp(e.A, e.B, (float)k / pieces);
                    var b = Vector2.Lerp(e.A, e.B, (float)(k + 1) / pieces);
                    if (IsShore(other, all, a, b, p.OpenEdgeProbe)) shores.Add(new(a, b));
                }
            }
        }
        return shores;
    }

    private static bool Near(WaterBody a, WaterBody b, float reach) =>
        a.Min.X - reach <= b.Max.X && a.Max.X + reach >= b.Min.X && a.Min.Y - reach <= b.Max.Y && a.Max.Y + reach >= b.Min.Y;

    private static bool IsShore(WaterBody owner, IReadOnlyList<WaterBody> all, Vector2 a, Vector2 b, float probe)
    {
        var mid = (a + b) * 0.5f;
        var t = b - a;
        var n = Vector2.Normalize(new Vector2(-t.Y, t.X));
        if (owner.Contains(mid + n * 0.05f)) n = -n;
        var outside = mid + n * probe;
        foreach (var other in all)
            if (other != owner && other.Contains(outside)) return false;
        return true;
    }

    private static bool SegmentTouchesRect(Vector2 a, Vector2 b, Vector2 min, Vector2 max)
    {
        // Liang-Barsky on the closed rectangle.
        float t0 = 0f, t1 = 1f;
        var d = b - a;
        return Clip(-d.X, a.X - min.X) && Clip(d.X, max.X - a.X) && Clip(-d.Y, a.Y - min.Y) && Clip(d.Y, max.Y - a.Y);

        bool Clip(float q, float r)
        {
            if (q == 0f) return r >= 0f;
            var t = r / q;
            if (q < 0f) { if (t > t1) return false; if (t > t0) t0 = t; }
            else { if (t < t0) return false; if (t < t1) t1 = t; }
            return true;
        }
    }

    private sealed class Builder(float y)
    {
        private readonly Dictionary<(int, int), int> _weld = [];
        public List<float> Positions { get; } = [];
        public List<int> Indices { get; } = [];
        public int VertexCount => Positions.Count / 3;

        /// <summary>Vertices are welded by exact position, so neighbouring pieces share them.</summary>
        public int Vertex(float x, float z)
        {
            var key = (BitConverter.SingleToInt32Bits(x == 0f ? 0f : x), BitConverter.SingleToInt32Bits(z == 0f ? 0f : z));
            if (_weld.TryGetValue(key, out var index)) return index;
            Positions.Add(x); Positions.Add(y); Positions.Add(z);
            _weld.Add(key, VertexCount - 1);
            return VertexCount - 1;
        }

        /// <summary>Trapezoid with bottom row (xb0..xb1 at zb) and top row (xt0..xt1 at zt).</summary>
        public void Quad(float xb0, float xb1, float xt1, float xt0, float zb, float zt)
        {
            var a = Vertex(xb0, zb);
            var b = Vertex(xb1, zb);
            var c = Vertex(xt1, zt);
            var d = Vertex(xt0, zt);
            Triangle(a, b, c);
            Triangle(a, c, d);
        }

        public void Triangle(int a, int b, int c)
        {
            float ax = Positions[3 * a], az = Positions[3 * a + 2];
            float bx = Positions[3 * b], bz = Positions[3 * b + 2];
            float cx = Positions[3 * c], cz = Positions[3 * c + 2];
            // y of (b - a) x (c - a); zero area triangles are dropped.
            var cross = (bz - az) * (cx - ax) - (bx - ax) * (cz - az);
            if (MathF.Abs(cross) <= 1e-7f) return;
            if (cross > 0f) (b, c) = (c, b);
            Indices.Add(a); Indices.Add(b); Indices.Add(c);
        }
    }
}
