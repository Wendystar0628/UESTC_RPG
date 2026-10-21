using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Geometry;

namespace Campus.Gen.Parts.Core;

/// <summary>
/// Shared plan joins for solid sections, including paths whose Y varies. NTS buffer joins do not retain a
/// section's heights and normals, so both loft writers consume this plan construction and keep their own shading.
/// </summary>
internal sealed class PathJoins
{
    internal readonly record struct SectionEdge(int Source, Vector2 A, Vector2 B, Vector2 N0, Vector2 N1, Vector2 Outward, float V0, float V1);

    private readonly IReadOnlyList<Vector3> _path;
    private readonly int[] _turns;
    private readonly float[] _innerScales;
    internal Vector3[] Tangents { get; }
    internal Vector3[] Rights { get; }
    internal Vector3[] Miters { get; }
    internal int[] BevelTurns { get; }
    internal bool SplitCentre { get; }

    internal PathJoins(IReadOnlyList<Vector3> path, bool closed, float lateralMin, float lateralMax, float miterLimit)
    {
        if (path.Count < (closed ? 3 : 2)) throw new ArgumentException("A sweep needs two points, or three for a closed path.", nameof(path));
        if (!float.IsFinite(miterLimit) || miterLimit < 1) throw new ArgumentOutOfRangeException(nameof(miterLimit));
        _path = path;
        var segments = closed ? path.Count : path.Count - 1;
        Tangents = new Vector3[segments]; Rights = new Vector3[segments];
        var lengths = new double[segments];
        for (var s = 0; s < segments; s++)
        {
            var a = path[s]; var b = path[(s + 1) % path.Count];
            if (!VectorMath.Finite(a) || !VectorMath.Finite(b)) throw new ArgumentException("Path coordinates must be finite.", nameof(path));
            var delta = new Vector3(b.X - a.X, 0, b.Z - a.Z);
            lengths[s] = Math.Sqrt(VectorMath.Dot(delta, delta));
            Tangents[s] = VectorMath.Unit(delta);
            Rights[s] = new(Tangents[s].Z, 0, -Tangents[s].X);
        }
        Miters = new Vector3[path.Count]; BevelTurns = new int[path.Count];
        _turns = new int[path.Count]; _innerScales = new float[path.Count];
        var innerReaches = new double[path.Count];
        for (var i = 0; i < path.Count; i++)
        {
            _innerScales[i] = 1;
            if (!closed && i == 0) { Miters[i] = Rights[0]; continue; }
            if (!closed && i == path.Count - 1) { Miters[i] = Rights[^1]; continue; }
            var incoming = (i + segments - 1) % segments; var outgoing = i % segments;
            var a = Rights[incoming]; var b = Rights[outgoing];
            var denominator = 1 + VectorMath.Dot(a, b);
            if (denominator < 1e-8) throw new ArgumentException("A sweep cannot reverse direction at a join.", nameof(path));
            Miters[i] = Vector3.Multiply(Vector3.Add(a, b), (float)(1 / denominator));
            _turns[i] = Math.Sign((double)a.X * b.Z - (double)a.Z * b.X);
            var miterSquared = VectorMath.Dot(Miters[i], Miters[i]);
            if (miterSquared > (double)miterLimit * miterLimit + 1e-6) BevelTurns[i] = _turns[i];
            var innerExtent = _turns[i] > 0 ? Math.Max(0, -lateralMin) : _turns[i] < 0 ? Math.Max(0, lateralMax) : 0;
            innerReaches[i] = innerExtent * Math.Sqrt(miterSquared);
        }
        // Allocate each segment's available length between the two requested inner intersections. Using their
        // radial reaches keeps their longitudinal cutbacks strictly inside that allocation; the whole segment
        // remains present even when both joins are limited. Open ends request no cutback, so an isolated join
        // can use its full adjacent segment. Ordinary intersections that fit retain their exact construction.
        for (var i = 0; i < path.Count; i++)
        {
            if (innerReaches[i] == 0) continue;
            var previous = (i + path.Count - 1) % path.Count; var next = (i + 1) % path.Count;
            var incoming = (i + segments - 1) % segments; var outgoing = i % segments;
            _innerScales[i] = (float)Math.Min(1, Math.Min(lengths[incoming] / (innerReaches[previous] + innerReaches[i]),
                lengths[outgoing] / (innerReaches[i] + innerReaches[next])));
        }
        SplitCentre = BevelTurns.Any(turn => turn != 0) || _innerScales.Any(scale => scale < 1);
    }

    internal Vector3 Position(int ring, Vector2 p, bool outgoing)
    {
        var lateral = BevelTurns[ring] * p.X > 0
            ? Rights[outgoing ? ring % Rights.Length : (ring + Rights.Length - 1) % Rights.Length]
            : _turns[ring] * p.X < 0 ? Vector3.Multiply(Miters[ring], _innerScales[ring]) : Miters[ring];
        return new(_path[ring].X + lateral.X * p.X, _path[ring].Y + p.Y, _path[ring].Z + lateral.Z * p.X);
    }

    internal static bool OtherDiagonal(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 outward)
    {
        // Limiting an inner ring can make a section strip concave. Keep its boundary order and choose
        // the interior diagonal; independently flipping the two triangles would duplicate that diagonal.
        var ac0 = VectorMath.TriangleOrientation(a, b, c, outward);
        var ac1 = VectorMath.TriangleOrientation(a, c, d, outward);
        if (ac0 * ac1 >= 0) return false;
        return VectorMath.TriangleOrientation(a, b, d, outward) * VectorMath.TriangleOrientation(b, c, d, outward) > 0;
    }

    internal static List<SectionEdge> SectionEdges(SweepProfile profile, bool splitCentre, out Dictionary<(Vector2, Vector2), Vector2> splits)
    {
        var edges = new List<SectionEdge>(); splits = [];
        for (var i = 0; i < profile.Points.Count; i++)
        {
            var j = (i + 1) % profile.Points.Count; var a = profile.Points[i]; var b = profile.Points[j];
            var edge = PlanOffsets.RightUnitNormal(a, b); var n0 = profile.Normals?[i] ?? edge; var n1 = profile.Normals?[j] ?? edge;
            var height = (float)Math.Sqrt(((double)b.X - a.X) * (b.X - a.X) + ((double)b.Y - a.Y) * (b.Y - a.Y));
            if (splitCentre && (double)a.X * b.X < 0)
            {
                // Split the actual inner/outer transition, including on sloping section edges.
                var t = (double)a.X / ((double)a.X - b.X);
                var p = new Vector2(0, (float)(a.Y + ((double)b.Y - a.Y) * t));
                splits[(a, b)] = p; splits[(b, a)] = p;
                // Opposite end normals describe the two sides of a flat closing edge (a clipped hedge's foot).
                // Its inserted point takes that edge's outward normal; the two directions have no smooth interpolation.
                var normal = n0 == -n1 ? edge : VectorMath.Unit(Vector2.Lerp(n0, n1, (float)t));
                var v = (float)(height * t);
                edges.Add(new(i, a, p, n0, normal, edge, 0, v)); edges.Add(new(i, p, b, normal, n1, edge, v, height));
            }
            else edges.Add(new(i, a, b, n0, n1, edge, 0, height));
        }
        return edges;
    }

    internal static (IReadOnlyList<Vector2> Points, IReadOnlyList<int> Indices) SplitCap(IReadOnlyList<Vector2> points,
        IReadOnlyList<int> indices, IReadOnlyDictionary<(Vector2, Vector2), Vector2> splits)
    {
        if (splits.Count == 0) return (points, indices);
        var vertices = points.ToList(); var triangles = new List<int>();
        for (var i = 0; i < indices.Count; i += 3)
        {
            int a = indices[i], b = indices[i + 1], c = indices[i + 2];
            if (!splits.ContainsKey((points[a], points[b])) && !splits.ContainsKey((points[b], points[c])) && !splits.ContainsKey((points[c], points[a])))
            { triangles.Add(a); triangles.Add(b); triangles.Add(c); continue; }
            // Preserve the original triangles and split only their boundary edges, also after Earcut reverses a ring.
            var boundary = new List<int>();
            foreach (var (from, to) in new[] { (a, b), (b, c), (c, a) })
            {
                boundary.Add(from);
                if (splits.TryGetValue((points[from], points[to]), out var p))
                { boundary.Add(vertices.Count); vertices.Add(p); }
            }
            var centre = vertices.Count; vertices.Add((points[a] + points[b] + points[c]) / 3);
            for (var j = 0; j < boundary.Count; j++)
            { triangles.Add(centre); triangles.Add(boundary[j]); triangles.Add(boundary[(j + 1) % boundary.Count]); }
        }
        return (vertices, triangles);
    }
}
