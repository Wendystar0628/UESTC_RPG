using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Gen.World;

namespace Campus.Gen.Zones.DesignBoundary.Framework;

/// <summary>
/// The campus boundary ring (campus.json `boundary`, through <see cref="IWorldModel.Boundary"/>) with the plan queries the
/// gates and fences need: crossings, projections, arc positions and inset slices. Plain arithmetic and square roots
/// only (D-165). Index order follows the data; <see cref="Inward"/> gives the interior side of each edge.
/// </summary>
public sealed class BoundaryRing
{
    private readonly Vector2[] _p;
    private readonly float[] _arc;
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
        _arc = new float[_p.Length];
        for (var i = 1; i < _p.Length; i++) _arc[i] = _arc[i - 1] + Length(i - 1);
        Perimeter = _arc[^1] + Length(_p.Length - 1);
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
    public bool Contains(Vector2 p) => PlanGeometry.InsideByCrossings(_p, p);

    /// <summary>First crossing of a polyline with the ring: the point, the edge and the edge parameter.</summary>
    public (Vector2 Point, int Edge, float T)? Crossing(IReadOnlyList<Vector2> line)
    {
        for (var k = 0; k + 1 < line.Count; k++)
            for (var i = 0; i < _p.Length; i++)
                if (Intersect(line[k], line[k + 1], _p[i], _p[Wrap(i + 1)]) is { } hit) return (hit.Point, i, hit.U);
        return null;
    }

    /// <summary>Arc positions where the segment p→q crosses the ring, in order along the segment (a vertex hit counts once).</summary>
    public IReadOnlyList<float> Crossings(Vector2 p, Vector2 q)
    {
        var hits = new List<(float Along, float S)>();
        for (var i = 0; i < _p.Length; i++)
            if (Intersect(p, q, _p[i], _p[Wrap(i + 1)]) is { } hit)
            {
                var s = Arc(i, hit.U);
                if (hits.Any(h => MathF.Abs(h.Along - hit.S) * Vector2.Distance(p, q) < 0.05f)) continue;
                hits.Add((hit.S, s));
            }
        hits.Sort((a, b) => a.Along.CompareTo(b.Along));
        return [.. hits.Select(h => h.S)];
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

    /// <summary>Ring length; arc positions s run from vertex 0 in data order and wrap at this value.</summary>
    public float Perimeter { get; }

    /// <summary>Arc position of a point on edge i at parameter t.</summary>
    public float Arc(int edge, float t) => _arc[Wrap(edge)] + Length(edge) * t;

    /// <summary>Arc position of the ring point nearest p.</summary>
    public float Arc(Vector2 p)
    {
        var (edge, t) = Project(p);
        return Arc(edge, t);
    }

    /// <summary>An arc position folded into [0, Perimeter).</summary>
    public float Fold(float s)
    {
        var r = s % Perimeter;
        return r < 0 ? r + Perimeter : r;
    }

    /// <summary>Distance from a to b walking forward (increasing s) round the ring.</summary>
    public float Forward(float a, float b) => Fold(b - a);

    /// <summary>Edge and parameter of an arc position.</summary>
    public (int Edge, float T) Locate(float s)
    {
        s = Fold(s);
        var i = 0;
        while (i + 1 < _p.Length && _arc[i + 1] <= s) i++;
        return (i, Math.Clamp((s - _arc[i]) / Length(i), 0f, 1f));
    }

    public Vector2 At(float s)
    {
        var (edge, t) = Locate(s);
        return At(edge, t);
    }

    /// <summary>Unit direction of the ring at s (forward).</summary>
    public Vector2 Direction(float s)
    {
        var (a, b) = Edge(Locate(s).Edge);
        return Vector2.Normalize(b - a);
    }

    /// <summary>Unit inward normal of the ring at s.</summary>
    public Vector2 InwardAt(float s) => Inward(Locate(s).Edge);

    /// <summary>
    /// The ring from arc s0 forward for <paramref name="length"/> metres, offset <paramref name="inset"/> metres toward the
    /// interior (mitred at the vertices, the mitre clamped at twice the inset).
    /// </summary>
    public IReadOnlyList<Vector2> Slice(float s0, float length, float inset)
    {
        var (edge, t) = Locate(s0);
        var points = new List<Vector2> { At(edge, t) };
        var edges = new List<int> { edge };
        var left = length;
        var toVertex = Length(edge) * (1f - t);
        var i = edge;
        for (var guard = 0; left > toVertex + 1e-4f && guard <= _p.Length; guard++)
        {
            left -= toVertex;
            i = Wrap(i + 1);
            points.Add(_p[i]);
            edges.Add(i);
            toVertex = Length(i);
            t = 0f;
        }
        points.Add(At(i, Math.Min(1f, t + left / Length(i))));
        edges.Add(i);
        // A start or end on a vertex repeats it: keep the later copy, which carries the edge the slice continues on.
        for (var k = points.Count - 1; k > 0; k--)
            if (Vector2.Distance(points[k], points[k - 1]) < 0.01f) { points.RemoveAt(k - 1); edges.RemoveAt(k - 1); }
        var result = new Vector2[points.Count];
        for (var k = 0; k < points.Count; k++)
        {
            var before = Inward(k == 0 ? edges[k] : edges[k - 1]);
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
