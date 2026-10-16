using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.Zones.DesignBoundary.Framework;

/// <summary>
/// The campus boundary ring (campus.json `boundary`, through <see cref="IWorldModel.Boundary"/>) with the plan queries the
/// gates and fences need: crossings, projections, walks along the ring and inset copies. Plain arithmetic and square roots
/// only (D-165). Index order follows the data; <see cref="Inward"/> gives the interior side of each edge.
/// </summary>
public sealed class BoundaryRing
{
    private readonly Vector2[] _p;
    private readonly float _side;

    public BoundaryRing(IReadOnlyList<Vector2> outer)
    {
        ArgumentNullException.ThrowIfNull(outer);
        var points = new List<Vector2>(outer.Count);
        foreach (var p in outer)
            if (points.Count == 0 || Vector2.Distance(points[^1], p) > 0.05f) points.Add(p);
        if (points.Count > 1 && Vector2.Distance(points[0], points[^1]) <= 0.05f) points.RemoveAt(points.Count - 1);
        if (points.Count < 3) throw new ArgumentException("The boundary ring needs at least three points.", nameof(outer));
        _p = [.. points];
        // Interior side: test a point just off the longest edge's midpoint.
        var longest = 0;
        for (var i = 1; i < _p.Length; i++) if (Length(i) > Length(longest)) longest = i;
        var (a, b) = Edge(longest);
        var d = Vector2.Normalize(b - a);
        _side = Contains((a + b) * 0.5f + new Vector2(d.Y, -d.X) * 0.5f) ? 1f : -1f;
    }

    public int Count => _p.Length;

    public Vector2 this[int i] => _p[Wrap(i)];

    public (Vector2 A, Vector2 B) Edge(int i) => (_p[Wrap(i)], _p[Wrap(i + 1)]);

    /// <summary>Unit normal of edge i toward the interior.</summary>
    public Vector2 Inward(int i)
    {
        var (a, b) = Edge(i);
        var d = Vector2.Normalize(b - a);
        return new Vector2(d.Y, -d.X) * _side;
    }

    /// <summary>Even-odd point in ring.</summary>
    public bool Contains(Vector2 p)
    {
        var inside = false;
        for (int i = 0, j = _p.Length - 1; i < _p.Length; j = i++)
        {
            var a = _p[i];
            var b = _p[j];
            if ((a.Y > p.Y) != (b.Y > p.Y) && p.X < (b.X - a.X) * (p.Y - a.Y) / (b.Y - a.Y) + a.X) inside = !inside;
        }
        return inside;
    }

    /// <summary>First crossing of a polyline with the ring: the point, the edge and the edge parameter.</summary>
    public (Vector2 Point, int Edge, float T)? Crossing(IReadOnlyList<Vector2> line)
    {
        for (var k = 0; k + 1 < line.Count; k++)
            for (var i = 0; i < _p.Length; i++)
                if (Intersect(line[k], line[k + 1], _p[i], _p[Wrap(i + 1)]) is { } hit) return (hit.Point, i, hit.U);
        return null;
    }

    /// <summary>First crossing of the ray p + dir·s (s > 0) with the ring.</summary>
    public (Vector2 Point, int Edge, float T)? Ray(Vector2 p, Vector2 dir, float reach)
    {
        (Vector2 Point, int Edge, float T)? best = null;
        var bestS = float.MaxValue;
        for (var i = 0; i < _p.Length; i++)
            if (Intersect(p, p + dir * reach, _p[i], _p[Wrap(i + 1)]) is { } hit && hit.S * reach < bestS && hit.S > 1e-4f)
            {
                bestS = hit.S * reach;
                best = (hit.Point, i, hit.U);
            }
        return best;
    }

    /// <summary>Nearest point of the ring to p: edge index and parameter.</summary>
    public (int Edge, float T) Project(Vector2 p)
    {
        var best = (Edge: 0, T: 0f);
        var bestD = float.MaxValue;
        for (var i = 0; i < _p.Length; i++)
        {
            var (a, b) = Edge(i);
            var ab = b - a;
            var t = Math.Clamp(Vector2.Dot(p - a, ab) / ab.LengthSquared(), 0f, 1f);
            var d = Vector2.DistanceSquared(p, a + ab * t);
            if (d < bestD) { bestD = d; best = (i, t); }
        }
        return best;
    }

    public Vector2 At(int edge, float t)
    {
        var (a, b) = Edge(edge);
        return a + (b - a) * t;
    }

    /// <summary>
    /// The ring from (edge, t) for <paramref name="length"/> metres, in the direction that moves away from
    /// <paramref name="away"/>, offset <paramref name="inset"/> metres toward the interior (mitred at corners).
    /// </summary>
    public IReadOnlyList<Vector2> Walk(int edge, float t, Vector2 away, float length, float inset)
    {
        var start = At(edge, t);
        var forward = Vector2.Distance(At(edge, Math.Min(1f, t + 0.5f / Length(edge))), away);
        var backward = Vector2.Distance(At(edge, Math.Max(0f, t - 0.5f / Length(edge))), away);
        var step = forward >= backward ? 1 : -1;
        var points = new List<Vector2> { start };
        var edges = new List<int> { edge };
        var left = length;
        var cursor = start;
        var i = edge;
        // Next vertex in the walking direction: edge end when stepping forward, edge start when stepping back.
        var vertex = step > 0 ? Wrap(i + 1) : i;
        for (var guard = 0; left > 1e-4f && guard < _p.Length; guard++)
        {
            var target = _p[vertex];
            var d = Vector2.Distance(cursor, target);
            if (d >= left)
            {
                points.Add(cursor + (target - cursor) * (left / d));
                edges.Add(i);
                break;
            }
            left -= d;
            cursor = target;
            points.Add(cursor);
            i = step > 0 ? vertex : Wrap(vertex - 1);
            edges.Add(i);
            vertex = step > 0 ? Wrap(vertex + 1) : Wrap(vertex - 1);
        }
        // A start on a vertex repeats it: keep the later copy, which carries the edge the walk continues on.
        for (var k = points.Count - 1; k > 0; k--)
            if (Vector2.Distance(points[k], points[k - 1]) < 0.01f) { points.RemoveAt(k - 1); edges.RemoveAt(k - 1); }
        // Offset: each point moves along the mean inward normal of the edges meeting there (a mitre, clamped).
        var result = new Vector2[points.Count];
        for (var k = 0; k < points.Count; k++)
        {
            var before = Inward(edges[Math.Max(0, k - 1)] == edges[k] || k == 0 ? edges[k] : edges[k - 1]);
            var after = Inward(edges[k]);
            var n = before + after;
            var len = n.Length();
            n = len > 1e-4f ? n / len : after;
            var cos = Vector2.Dot(n, after);
            result[k] = points[k] + n * (inset / Math.Max(cos, 0.5f));
        }
        return result;
    }

    private float Length(int i) => Vector2.Distance(_p[Wrap(i)], _p[Wrap(i + 1)]);

    private int Wrap(int i) => ((i % _p.Length) + _p.Length) % _p.Length;

    private static (Vector2 Point, float S, float U)? Intersect(Vector2 p, Vector2 q, Vector2 a, Vector2 b)
    {
        var r = q - p;
        var s = b - a;
        var den = r.X * s.Y - r.Y * s.X;
        if (MathF.Abs(den) < 1e-9f) return null;
        var ap = a - p;
        var t = (ap.X * s.Y - ap.Y * s.X) / den;
        var u = (ap.X * r.Y - ap.Y * r.X) / den;
        // A small tolerance keeps a crossing that lands exactly on a shared vertex (road node = ring vertex).
        const float eps = 1e-5f;
        if (t < -eps || t > 1 + eps || u < -eps || u > 1 + eps) return null;
        return (p + r * t, Math.Clamp(t, 0f, 1f), Math.Clamp(u, 0f, 1f));
    }
}
