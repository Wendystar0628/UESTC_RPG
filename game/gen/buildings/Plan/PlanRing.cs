using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Gen.World;

namespace Campus.Gen.Buildings.Plan;

/// <summary>
/// One cleaned footprint ring with per-edge frames. Plan coordinates are (world X, world Z).
/// Every ring keeps the building material on the left of travel, so the outward (air side) normal of
/// edge i is (d.Y, -d.X): outer rings run counter-clockwise (positive signed area), courtyard holes clockwise.
/// Corner i sits at Points[i], between edge i-1 (incoming) and edge i (outgoing).
/// </summary>
public sealed class PlanRing
{
    private PlanRing(Vector2[] points, bool hole)
    {
        Points = points;
        IsHole = hole;
        var n = points.Length;
        Dir = new Vector2[n];
        Normal = new Vector2[n];
        Length = new float[n];
        Convex = new bool[n];
        TurnCos = new float[n];
        for (var i = 0; i < n; i++)
        {
            var e = points[(i + 1) % n] - points[i];
            var len = e.Length();
            Length[i] = len;
            Dir[i] = e / len;
            Normal[i] = new Vector2(Dir[i].Y, -Dir[i].X);
        }
        for (var i = 0; i < n; i++)
        {
            var prev = Dir[(i + n - 1) % n];
            var next = Dir[i];
            Convex[i] = Cross(prev, next) > 0f;
            TurnCos[i] = Vector2.Dot(prev, next);
        }
    }

    public Vector2[] Points { get; }
    public bool IsHole { get; }
    public Vector2[] Dir { get; }
    public Vector2[] Normal { get; }
    public float[] Length { get; }
    /// <summary>True when corner i is an outside corner (left turn with the material on the left).</summary>
    public bool[] Convex { get; }
    /// <summary>Cosine between the incoming and outgoing edge directions at corner i (1 = straight).</summary>
    public float[] TurnCos { get; }
    public int Count => Points.Length;

    public float Perimeter
    {
        get
        {
            var sum = 0f;
            foreach (var l in Length) sum += l;
            return sum;
        }
    }

    public static float Cross(Vector2 a, Vector2 b) => a.X * b.Y - a.Y * b.X;

    /// <summary>Wraps already clean, correctly oriented points (for offset skins).</summary>
    public static PlanRing FromPoints(Vector2[] points, bool hole) => new(points, hole);

    /// <summary>
    /// Removes duplicate, collinear and sliver vertices, then orients the ring (outer CCW, hole CW).
    /// Returns null when fewer than three useful vertices remain or the area is negligible.
    /// </summary>
    public static PlanRing? Clean(IReadOnlyList<Vector2> input, bool hole, float minEdge = 0.35f, float collinearTolerance = 0.12f)
    {
        var pts = new List<Vector2>(input.Count);
        foreach (var p in input)
        {
            if (!float.IsFinite(p.X) || !float.IsFinite(p.Y)) return null;
            if (pts.Count == 0 || Vector2.DistanceSquared(pts[^1], p) > 0.0025f) pts.Add(p);
        }
        while (pts.Count > 1 && Vector2.DistanceSquared(pts[0], pts[^1]) <= 0.0025f) pts.RemoveAt(pts.Count - 1);
        // Iterate until stable: drop collinear vertices (deviation from the neighbour chord below the tolerance)
        // and merge vertices that close an edge shorter than minEdge.
        var changed = true;
        var guard = 0;
        while (changed && pts.Count >= 3 && guard++ < 64)
        {
            changed = false;
            for (var i = 0; i < pts.Count && pts.Count >= 3; i++)
            {
                var a = pts[(i + pts.Count - 1) % pts.Count];
                var p = pts[i];
                var b = pts[(i + 1) % pts.Count];
                var chord = b - a;
                var chordLen = chord.Length();
                var deviation = chordLen > 1e-4f ? MathF.Abs(Cross(chord, p - a)) / chordLen : 0f;
                var reverses = Vector2.Dot(p - a, b - p) < 0f && deviation < 0.05f;
                if (deviation < collinearTolerance && !reverses && chordLen > 1e-4f || reverses)
                {
                    pts.RemoveAt(i);
                    changed = true;
                    i--;
                    continue;
                }
                if (Vector2.Distance(p, b) < minEdge)
                {
                    // Keep whichever of the two endpoints preserves more area.
                    var c = pts[(i + 2) % pts.Count];
                    var keepP = MathF.Abs(Cross(c - p, a - p));
                    var keepB = MathF.Abs(Cross(c - b, a - b));
                    pts.RemoveAt(keepP >= keepB ? (i + 1) % pts.Count : i);
                    changed = true;
                    i--;
                }
            }
        }
        if (pts.Count < 3) return null;
        var area = (float)PlanGeometry.SignedArea(pts);
        if (MathF.Abs(area) < 4f) return null;
        if (area > 0f == hole) pts.Reverse();
        return new PlanRing([.. pts], hole);
    }

    /// <summary>Area-weighted centroid of an outer ring minus its holes.</summary>
    public static Vector2 Centroid(PlanRing outer, IReadOnlyList<PlanRing> holes)
    {
        double ax = 0d, az = 0d, aw = 0d;
        Accumulate(outer.Points);
        foreach (var h in holes) Accumulate(h.Points);
        return aw != 0d ? new Vector2((float)(ax / aw), (float)(az / aw)) : outer.Points[0];

        void Accumulate(Vector2[] r)
        {
            for (var i = 0; i < r.Length; i++)
            {
                var a = r[i];
                var b = r[(i + 1) % r.Length];
                var cross = (double)a.X * b.Y - (double)b.X * a.Y;
                ax += (a.X + b.X) * cross;
                az += (a.Y + b.Y) * cross;
                aw += 3d * cross;
            }
        }
    }

    /// <summary>Point inside the outer ring and outside every hole.</summary>
    public static bool InsideFootprint(PlanRing outer, IReadOnlyList<PlanRing> holes, Vector2 p)
    {
        if (!PlanGeometry.InsideByCrossings(outer.Points, p)) return false;
        foreach (var h in holes)
            if (PlanGeometry.InsideByCrossings(h.Points, p)) return false;
        return true;
    }

    /// <summary>Distance from p to the nearest edge of any ring.</summary>
    public static float DistanceToEdges(PlanRing outer, IReadOnlyList<PlanRing> holes, Vector2 p)
    {
        var best = Distance(outer, p);
        foreach (var h in holes) best = MathF.Min(best, Distance(h, p));
        return best;

        static float Distance(PlanRing r, Vector2 p)
        {
            var best = float.MaxValue;
            for (var i = 0; i < r.Count; i++)
            {
                var a = r.Points[i];
                var t = Math.Clamp(Vector2.Dot(p - a, r.Dir[i]), 0f, r.Length[i]);
                best = MathF.Min(best, Vector2.Distance(p, a + r.Dir[i] * t));
            }
            return best;
        }
    }

    /// <summary>
    /// Mitred offset of corner i along the outward normals by s (negative = into the material).
    /// Very sharp corners clamp the mitre so that the point never runs further than four times |s|.
    /// </summary>
    public Vector2 Mitre(int i, float s)
    {
        var n0 = Normal[(i + Count - 1) % Count];
        var n1 = Normal[i];
        var den = 1f + Vector2.Dot(n0, n1);
        var k = den > 0.125f ? 1f / den : 8f;
        return Points[i] + (n0 + n1) * (s * k);
    }

    /// <summary>
    /// Offsets the whole ring by s. Returns null when an edge would flip or the result self-intersects,
    /// so callers fall back to a simpler, still closed form.
    /// </summary>
    public Vector2[]? Offset(float s)
    {
        var result = new Vector2[Count];
        for (var i = 0; i < Count; i++) result[i] = Mitre(i, s);
        for (var i = 0; i < Count; i++)
        {
            var e = result[(i + 1) % Count] - result[i];
            if (Vector2.Dot(e, Dir[i]) <= 0.02f) return null;
        }
        return SelfIntersects(result) ? null : result;
    }

    public static bool SelfIntersects(IReadOnlyList<Vector2> r)
    {
        var n = r.Count;
        for (var i = 0; i < n; i++)
        {
            var a0 = r[i];
            var a1 = r[(i + 1) % n];
            for (var j = i + 2; j < n; j++)
            {
                if (i == 0 && j == n - 1) continue;
                var b0 = r[j];
                var b1 = r[(j + 1) % n];
                if (SegmentsCross(a0, a1, b0, b1)) return true;
            }
        }
        return false;
    }

    private static bool SegmentsCross(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
    {
        var d1 = Cross(b - a, c - a);
        var d2 = Cross(b - a, d - a);
        var d3 = Cross(d - c, a - c);
        var d4 = Cross(d - c, b - c);
        return (d1 > 0f) != (d2 > 0f) && (d3 > 0f) != (d4 > 0f);
    }
}
