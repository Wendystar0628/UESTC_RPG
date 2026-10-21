using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Geometry;

namespace Campus.Gen.Parts.Core;

/// <summary>Counterclockwise section in (lateral, up) metres. Optional vertex normals interpolate across chamfer bands.</summary>
public sealed record SweepProfile(IReadOnlyList<Vector2> Points, IReadOnlyList<Vector2>? Normals = null);

public static class PartSweeps
{
    public const float DefaultMiterLimit = 4;

    /// <summary>Closed rectangular section with a single chamfer at each corner and flat principal-face normals.</summary>
    public static SweepProfile BeveledRectangleProfile(float width, float height, float bevel)
    {
        GeometryMath.Positive(width, nameof(width)); GeometryMath.Positive(height, nameof(height));
        PartSolids.ValidateBevel(bevel, Math.Min(width, height) * 0.5f);
        var x = width * 0.5f;
        return bevel == 0
            ? new([new(-x, 0), new(x, 0), new(x, height), new(-x, height)])
            : new([new(-x + bevel, 0), new(x - bevel, 0), new(x, bevel), new(x, height - bevel), new(x - bevel, height), new(-x + bevel, height), new(-x, height - bevel), new(-x, bevel)],
                [new(0, -1), new(0, -1), new(1, 0), new(1, 0), new(0, 1), new(0, 1), new(-1, 0), new(-1, 0)]);
    }

    /// <summary>
    /// Sweeps a solid section along an X/Z polyline. Lateral positive is right of the path.
    /// Outer joins exceeding miterLimit are beveled; inner intersections are bounded by adjacent segment lengths.
    /// Paths have no duplicate endpoint; closed=true joins last to first. Reversals and collapsed segments fail.
    /// Caller must keep the swept corridor free of self-overlap; this is not a solid-union operation.
    /// </summary>
    public static PartGeometry Sweep(SweepProfile profile, IReadOnlyList<Vector2> path, ToonSpec material, bool closed = false, float baseY = 0, float miterLimit = DefaultMiterLimit)
    {
        var cap = ValidateInput(profile, path, closed, baseY);
        var joins = new PathJoins(path.Select(p => new Vector3(p.X, baseY, p.Y)).ToArray(), closed,
            profile.Points.Min(p => p.X), profile.Points.Max(p => p.X), miterLimit);
        var segmentCount = joins.Tangents.Length;
        var tangents = joins.Tangents; var rights = joins.Rights; var miters = joins.Miters; var bevelTurns = joins.BevelTurns;
        Vector3 Position(int ring, Vector2 p, bool outgoing) => joins.Position(ring, p, outgoing);
        var edges = PathJoins.SectionEdges(profile, joins.SplitCentre, out var sectionSplits);
        var writer = new PartShapeWriter(material);
        float distance = 0;
        for (var s = 0; s < segmentCount; s++)
        {
            var next = (s + 1) % path.Count; var delta = new Vector3(path[next].X - path[s].X, 0, path[next].Y - path[s].Y);
            var length = (float)Math.Sqrt(VectorMath.Dot(delta, delta));
            foreach (var (_, a, b, n0, n1, edge, v0, v1) in edges)
            {
                Vector3 Normal(Vector2 n) => new(rights[s].X * n.X, n.Y, rights[s].Z * n.X);
                var pa = Position(s, a, true); var pb = Position(s, b, true);
                var pc = Position(next, b, false); var pd = Position(next, a, false);
                var ai = writer.Vertex(pa, Normal(n0), new(distance, v0)); var bi = writer.Vertex(pb, Normal(n1), new(distance, v1));
                var ci = writer.Vertex(pc, Normal(n1), new(distance + length, v1)); var di = writer.Vertex(pd, Normal(n0), new(distance + length, v0));
                if (PathJoins.OtherDiagonal(pa, pb, pc, pd, Normal(edge)))
                { writer.Triangle(ai, bi, di, Normal(edge)); writer.Triangle(bi, ci, di, Normal(edge)); }
                else writer.Quad(ai, bi, ci, di, Normal(edge));
            }
            distance += length;
            if (bevelTurns[next] != 0)
            {
                var advance = VectorMath.Length(new Vector2(rights[next % segmentCount].X - rights[s].X, rights[next % segmentCount].Z - rights[s].Z)) * profile.Points.Max(p => Math.Abs(p.X));
                foreach (var (_, a, b, n0, n1, edge, v0, v1) in edges)
                {
                    if (bevelTurns[next] * a.X <= 0 && bevelTurns[next] * b.X <= 0) continue;
                    Vector3 Normal(Vector2 n) => VectorMath.Unit(new Vector3(miters[next].X * n.X, n.Y, miters[next].Z * n.X));
                    int Vertex(Vector2 p, bool outgoing, Vector2 normal, float v) =>
                        writer.Vertex(Position(next, p, outgoing), Normal(normal), new(distance + (outgoing ? advance : 0), v));
                    var ai = Vertex(a, false, n0, v0); var bi = Vertex(b, false, n1, v1);
                    if (a.X == 0) writer.Triangle(ai, bi, Vertex(b, true, n1, v1), Normal(edge));
                    else if (b.X == 0) writer.Triangle(ai, bi, Vertex(a, true, n0, v0), Normal(edge));
                    else writer.Quad(ai, bi, Vertex(b, true, n1, v1), Vertex(a, true, n0, v0), Normal(edge));
                }
                distance += advance;
            }
        }
        if (!closed)
        {
            for (var end = 0; end < 2; end++)
            {
                var ring = end == 0 ? 0 : path.Count - 1; var normal = end == 0 ? Vector3.Multiply(tangents[0], -1) : tangents[^1];
                AddCap(writer, cap, sectionSplits, normal, p => Position(ring, p, true));
            }
        }
        return writer.Finish();
    }

    private static TriangulatedPolygon ValidateInput(SweepProfile profile, IReadOnlyList<Vector2> path, bool closed, float baseY)
    {
        ArgumentNullException.ThrowIfNull(profile); ArgumentNullException.ThrowIfNull(path);
        if (path.Count < (closed ? 3 : 2)) throw new ArgumentException("A sweep needs at least two points, or three for a closed path.", nameof(path));
        if (!float.IsFinite(baseY)) throw new ArgumentOutOfRangeException(nameof(baseY));
        var cap = PolygonTriangulator.Triangulate(new(profile.Points));
        if (!cap.Vertices.SequenceEqual(profile.Points)) throw new ArgumentException("Section must be counterclockwise with no repeated or collinear vertices.", nameof(profile));
        if (profile.Normals is not null)
        {
            if (profile.Normals.Count != profile.Points.Count) throw new ArgumentException("A section normal is required for each point.", nameof(profile));
            foreach (var n in profile.Normals)
                if (!VectorMath.Finite(n) || Math.Abs((double)n.X * n.X + (double)n.Y * n.Y - 1) > 1e-5)
                    throw new ArgumentException("Section normals must be finite unit vectors.", nameof(profile));
        }
        return cap;
    }

    private static void AddCap(PartShapeWriter writer, TriangulatedPolygon cap, IReadOnlyDictionary<(Vector2, Vector2), Vector2> splits,
        Vector3 normal, Func<Vector2, Vector3> position)
    {
        var (points, indices) = PathJoins.SplitCap(cap.Vertices, cap.Indices, splits);
        var ids = new int[points.Count];
        for (var i = 0; i < ids.Length; i++) ids[i] = writer.Vertex(position(points[i]), normal, points[i]);
        for (var i = 0; i < indices.Count; i += 3) writer.Triangle(ids[indices[i]], ids[indices[i + 1]], ids[indices[i + 2]], normal);
    }
}
