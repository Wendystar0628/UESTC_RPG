using System.Numerics;

namespace Campus.Gen.Buildings.Plan;

/// <summary>
/// Ear-clipping triangulation of a polygon with holes (holes bridged into the outer ring first).
/// Stand-in for the WP-09 earcut port: same contract (flat point list, triangle index triples),
/// used for roof slabs and caps. Output triangles are counter-clockwise in the (X, Z) plane;
/// the mesh sink orients every triangle against its intended normal, so callers need not care.
/// </summary>
public static class Triangulator
{
    /// <summary>Returns the combined vertex list and triangle indices into it.</summary>
    public static (Vector2[] Points, int[] Triangles) Triangulate(IReadOnlyList<Vector2> outer, IReadOnlyList<IReadOnlyList<Vector2>> holes)
    {
        var ring = new List<Vector2>(outer);
        if (PlanRing.SignedArea(ring) < 0f) ring.Reverse();
        var ordered = holes.Where(h => h.Count >= 3)
            .Select(h =>
            {
                var list = new List<Vector2>(h);
                if (PlanRing.SignedArea(list) > 0f) list.Reverse();
                return list;
            })
            .OrderByDescending(h => h.Max(p => p.X)).ThenByDescending(h => h.Max(p => p.Y))
            .ToList();
        foreach (var hole in ordered) Bridge(ring, hole);
        var tris = Clip(ring);
        return ([.. ring], tris);
    }

    // Eberly's hole elimination: connect the hole's rightmost vertex to a mutually visible outer vertex.
    private static void Bridge(List<Vector2> ring, List<Vector2> hole)
    {
        var mi = 0;
        for (var i = 1; i < hole.Count; i++)
            if (hole[i].X > hole[mi].X || hole[i].X == hole[mi].X && hole[i].Y < hole[mi].Y) mi = i;
        var m = hole[mi];
        var bestT = float.MaxValue;
        var edge = -1;
        var hit = Vector2.Zero;
        for (var i = 0; i < ring.Count; i++)
        {
            var a = ring[i];
            var b = ring[(i + 1) % ring.Count];
            if ((a.Y > m.Y) == (b.Y > m.Y) && a.Y != m.Y && b.Y != m.Y) continue;
            if (a.Y == b.Y) continue;
            var t = (m.Y - a.Y) / (b.Y - a.Y);
            if (t < 0f || t > 1f) continue;
            var x = a.X + t * (b.X - a.X);
            if (x < m.X - 1e-5f) continue;
            var d = x - m.X;
            if (d < bestT)
            {
                bestT = d;
                edge = i;
                hit = new Vector2(x, m.Y);
            }
        }
        int pi;
        if (edge < 0)
        {
            // Degenerate input: connect to the nearest outer vertex.
            pi = 0;
            for (var i = 1; i < ring.Count; i++)
                if (Vector2.DistanceSquared(ring[i], m) < Vector2.DistanceSquared(ring[pi], m)) pi = i;
        }
        else
        {
            var a = ring[edge];
            var b = ring[(edge + 1) % ring.Count];
            pi = a.X > b.X ? edge : (edge + 1) % ring.Count;
            var p = ring[pi];
            // Any reflex vertex inside triangle (m, hit, p) blocks visibility; choose the one with the smallest angle.
            var bestAngle = float.MaxValue;
            for (var i = 0; i < ring.Count; i++)
            {
                if (i == pi) continue;
                var v = ring[i];
                if (v.X < m.X) continue;
                if (!InTriangle(m, hit, p, v) && !InTriangle(m, p, hit, v)) continue;
                var dir = Vector2.Normalize(v - m);
                var angle = 1f - dir.X;
                if (angle < bestAngle || angle == bestAngle && Vector2.DistanceSquared(v, m) < Vector2.DistanceSquared(ring[pi], m))
                {
                    bestAngle = angle;
                    pi = i;
                }
            }
        }
        var splice = new List<Vector2>(hole.Count + 2);
        for (var k = 0; k <= hole.Count; k++) splice.Add(hole[(mi + k) % hole.Count]);
        splice.Add(ring[pi]);
        ring.InsertRange(pi + 1, splice);
    }

    private static int[] Clip(List<Vector2> pts)
    {
        var n = pts.Count;
        var result = new List<int>((n - 2) * 3);
        var idx = new List<int>(n);
        for (var i = 0; i < n; i++) idx.Add(i);
        var guard = 0;
        var i0 = 0;
        while (idx.Count > 3 && guard < n * n + 16)
        {
            guard++;
            var found = false;
            for (var step = 0; step < idx.Count; step++)
            {
                var k = (i0 + step) % idx.Count;
                var ia = idx[(k + idx.Count - 1) % idx.Count];
                var ib = idx[k];
                var ic = idx[(k + 1) % idx.Count];
                var a = pts[ia];
                var b = pts[ib];
                var c = pts[ic];
                var cross = PlanRing.Cross(b - a, c - b);
                if (cross <= 1e-7f) continue;
                var blocked = false;
                foreach (var j in idx)
                {
                    if (j == ia || j == ib || j == ic) continue;
                    var p = pts[j];
                    if (p == a || p == b || p == c) continue;
                    if (InTriangle(a, b, c, p)) { blocked = true; break; }
                }
                if (blocked) continue;
                result.Add(ia); result.Add(ib); result.Add(ic);
                idx.RemoveAt(k);
                i0 = k % Math.Max(1, idx.Count);
                found = true;
                break;
            }
            if (!found)
            {
                // Numerically stuck (collinear or touching bridge): drop the flattest vertex and continue.
                var worst = 0;
                var worstArea = float.MaxValue;
                for (var k = 0; k < idx.Count; k++)
                {
                    var a = pts[idx[(k + idx.Count - 1) % idx.Count]];
                    var b = pts[idx[k]];
                    var c = pts[idx[(k + 1) % idx.Count]];
                    var area = MathF.Abs(PlanRing.Cross(b - a, c - b));
                    if (area < worstArea) { worstArea = area; worst = k; }
                }
                idx.RemoveAt(worst);
            }
        }
        if (idx.Count == 3 && PlanRing.Cross(pts[idx[1]] - pts[idx[0]], pts[idx[2]] - pts[idx[1]]) > 1e-7f)
        {
            result.Add(idx[0]); result.Add(idx[1]); result.Add(idx[2]);
        }
        return [.. result];
    }

    private static bool InTriangle(Vector2 a, Vector2 b, Vector2 c, Vector2 p)
    {
        var d1 = PlanRing.Cross(b - a, p - a);
        var d2 = PlanRing.Cross(c - b, p - b);
        var d3 = PlanRing.Cross(a - c, p - c);
        return d1 >= -1e-7f && d2 >= -1e-7f && d3 >= -1e-7f;
    }
}
