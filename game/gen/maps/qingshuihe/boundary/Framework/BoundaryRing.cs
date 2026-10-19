using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Gen.World;

namespace Campus.Gen.Maps.Qingshuihe.Boundary.Framework;

/// <summary>
/// The campus boundary ring (world/geo.json `boundary`, through <see cref="IWorldModel.Boundary"/>) with the plan queries the
/// gates and fences need: crossings, projections, arc positions and inset slices, sharing geometry queries with
/// <see cref="PlanGeometry"/>. Index order follows the data; <see cref="Inward"/> gives the interior side of each edge.
/// </summary>
public sealed class BoundaryRing
{
    private readonly Vector2[] _p;
    private readonly float[] _arc;
    private readonly float _side;

    public BoundaryRing(IReadOnlyList<Vector2> outer)
    {
        ArgumentNullException.ThrowIfNull(outer);
        _p = PlanGeometry.DeduplicateRing(outer, 0.05f * 0.05f);
        if (_p.Length < 3) throw new ArgumentException("The boundary ring needs at least three points.", nameof(outer));
        _arc = new float[_p.Length];
        for (var i = 1; i < _p.Length; i++) _arc[i] = _arc[i - 1] + Length(i - 1);
        Perimeter = _arc[^1] + Length(_p.Length - 1);
        // Winding determines which side of every boundary edge is inside.
        _side = PlanGeometry.SignedArea(_p) < 0 ? 1f : -1f;
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
                if (PlanLines.SegmentHit(line[k], line[k + 1], _p[i], _p[Wrap(i + 1)], endpointTolerance: 1e-5f) is { } hit) return (hit.Point, i, hit.U);
        return null;
    }

    /// <summary>Arc positions where the segment p→q crosses the ring, in order along the segment (a vertex hit counts once).</summary>
    public IReadOnlyList<float> Crossings(Vector2 p, Vector2 q)
    {
        var hits = new List<(float Along, float S)>();
        for (var i = 0; i < _p.Length; i++)
            if (PlanLines.SegmentHit(p, q, _p[i], _p[Wrap(i + 1)], endpointTolerance: 1e-5f) is { } hit)
            {
                var s = Arc(i, hit.U);
                if (hits.Any(h => MathF.Abs(h.Along - hit.T) * Vector2.Distance(p, q) < 0.05f)) continue;
                hits.Add((hit.T, s));
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
            if (PlanLines.SegmentHit(p, p + dir * reach, _p[i], _p[Wrap(i + 1)], endpointTolerance: 1e-5f) is { } hit && hit.T * reach < bestS && hit.T > 1e-4f)
            {
                bestS = hit.T * reach;
                best = (hit.Point, i, hit.U);
            }
        return best;
    }

    /// <summary>Nearest point of the ring to p: edge index and parameter.</summary>
    public (int Edge, float T) Project(Vector2 p) =>
        Locate((float)PlanLines.Project(_p, p, closed: true).Station);

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

}
