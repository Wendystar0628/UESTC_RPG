using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Geometry;

namespace Campus.Gen.Parts.Core;

/// <summary>Counterclockwise section in (lateral, up) metres. Optional vertex normals interpolate across chamfer bands.</summary>
public sealed record SweepProfile(IReadOnlyList<Vector2> Points, IReadOnlyList<Vector2>? Normals = null);

public static class PartSweeps
{
    private readonly record struct SectionEdge(Vector2 A, Vector2 B, Vector2 N0, Vector2 N1, Vector2 Outward, float V0, float V1);

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
    /// Outer joins exceeding miterLimit are beveled; inner joins retain the offset-edge intersection.
    /// Paths have no duplicate endpoint; closed=true joins last to first. Reversals and collapsed segments fail.
    /// Caller must keep the swept corridor free of self-overlap; this is not a solid-union operation.
    /// </summary>
    public static PartGeometry Sweep(SweepProfile profile, IReadOnlyList<Vector2> path, ToonSpec material, bool closed = false, float baseY = 0, float miterLimit = 4)
    {
        var cap = ValidateInput(profile, path, closed, baseY, miterLimit);
        var segmentCount = closed ? path.Count : path.Count - 1;
        var tangents = new Vector3[segmentCount]; var rights = new Vector3[segmentCount];
        for (var i = 0; i < segmentCount; i++)
        {
            var a = path[i]; var b = path[(i + 1) % path.Count];
            tangents[i] = VectorMath.Unit(new Vector3(b.X - a.X, 0, b.Y - a.Y));
            rights[i] = new(tangents[i].Z, 0, -tangents[i].X);
        }
        var miters = new Vector3[path.Count]; var bevelTurns = new int[path.Count];
        for (var i = 0; i < path.Count; i++)
        {
            if (!closed && i == 0) miters[i] = rights[0];
            else if (!closed && i == path.Count - 1) miters[i] = rights[^1];
            else
            {
                var a = rights[(i + segmentCount - 1) % segmentCount]; var b = rights[i % segmentCount];
                var denominator = 1 + VectorMath.Dot(a, b);
                if (denominator < 1e-8) throw new ArgumentException("A sweep cannot reverse direction at a join.", nameof(path));
                miters[i] = Vector3.Multiply(Vector3.Add(a, b), (float)(1 / denominator));
                if (VectorMath.Dot(miters[i], miters[i]) > (double)miterLimit * miterLimit + 1e-6)
                    bevelTurns[i] = Math.Sign((double)a.X * b.Z - (double)a.Z * b.X);
            }
        }
        // Like NTS bevel joins, connect the two outer offset endpoints, never a shortened miter.
        // The buffer library cannot loft arbitrary section heights/normals, so the section mesh stays here.
        Vector3 Position(int ring, Vector2 p, bool outgoing)
        {
            var lateral = bevelTurns[ring] * p.X > 0
                ? rights[outgoing ? ring % segmentCount : (ring + segmentCount - 1) % segmentCount]
                : miters[ring];
            return new(path[ring].X + lateral.X * p.X, baseY + p.Y, path[ring].Y + lateral.Z * p.X);
        }
        var edges = SectionEdges(profile, bevelTurns.Any(turn => turn != 0), out var sectionSplits);
        var writer = new PartShapeWriter(material);
        float distance = 0;
        for (var s = 0; s < segmentCount; s++)
        {
            var next = (s + 1) % path.Count; var delta = new Vector3(path[next].X - path[s].X, 0, path[next].Y - path[s].Y);
            var length = (float)Math.Sqrt(VectorMath.Dot(delta, delta));
            foreach (var (a, b, n0, n1, edge, v0, v1) in edges)
            {
                if (VectorMath.Dot(Vector3.Subtract(Position(next, a, false), Position(s, a, true)), tangents[s]) <= 0)
                    throw new ArgumentException("A sweep segment is too short for the section and adjacent miters.", nameof(path));
                Vector3 Normal(Vector2 n) => new(rights[s].X * n.X, n.Y, rights[s].Z * n.X);
                writer.Quad(writer.Vertex(Position(s, a, true), Normal(n0), new(distance, v0)), writer.Vertex(Position(s, b, true), Normal(n1), new(distance, v1)),
                    writer.Vertex(Position(next, b, false), Normal(n1), new(distance + length, v1)), writer.Vertex(Position(next, a, false), Normal(n0), new(distance + length, v0)), Normal(edge));
            }
            distance += length;
            if (bevelTurns[next] != 0)
            {
                var advance = VectorMath.Length(new Vector2(rights[next % segmentCount].X - rights[s].X, rights[next % segmentCount].Z - rights[s].Z)) * profile.Points.Max(p => Math.Abs(p.X));
                foreach (var (a, b, n0, n1, edge, v0, v1) in edges)
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

    private static TriangulatedPolygon ValidateInput(SweepProfile profile, IReadOnlyList<Vector2> path, bool closed, float baseY, float miterLimit)
    {
        ArgumentNullException.ThrowIfNull(profile); ArgumentNullException.ThrowIfNull(path);
        if (path.Count < (closed ? 3 : 2)) throw new ArgumentException("A sweep needs at least two points, or three for a closed path.", nameof(path));
        if (!float.IsFinite(baseY) || !float.IsFinite(miterLimit) || miterLimit < 1) throw new ArgumentOutOfRangeException(nameof(miterLimit));
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

    private static List<SectionEdge> SectionEdges(SweepProfile profile, bool splitCentre, out Dictionary<(int, int), Vector2> splits)
    {
        var edges = new List<SectionEdge>(); splits = [];
        for (var i = 0; i < profile.Points.Count; i++)
        {
            var j = (i + 1) % profile.Points.Count; var a = profile.Points[i]; var b = profile.Points[j];
            var edge = PlanOffsets.RightUnitNormal(a, b); var n0 = profile.Normals?[i] ?? edge; var n1 = profile.Normals?[j] ?? edge;
            var height = (float)Math.Sqrt(((double)b.X - a.X) * (b.X - a.X) + ((double)b.Y - a.Y) * (b.Y - a.Y));
            if (splitCentre && (double)a.X * b.X < 0)
            {
                // An inner/outer transition is a real corner of the join surface, also on sloping sections.
                var t = (double)a.X / ((double)a.X - b.X);
                var p = new Vector2(0, (float)(a.Y + ((double)b.Y - a.Y) * t));
                splits[(i, j)] = p; splits[(j, i)] = p;
                var normal = VectorMath.Unit(Vector2.Lerp(n0, n1, (float)t));
                var v = (float)(height * t);
                edges.Add(new(a, p, n0, normal, edge, 0, v)); edges.Add(new(p, b, normal, n1, edge, v, height));
            }
            else edges.Add(new(a, b, n0, n1, edge, 0, height));
        }
        return edges;
    }

    private static void AddCap(PartShapeWriter writer, TriangulatedPolygon cap, IReadOnlyDictionary<(int, int), Vector2> splits,
        Vector3 normal, Func<Vector2, Vector3> position)
    {
        var ids = new int[cap.Vertices.Length];
        for (var i = 0; i < ids.Length; i++) ids[i] = writer.Vertex(position(cap.Vertices[i]), normal, cap.Vertices[i]);
        for (var i = 0; i < cap.Indices.Length; i += 3)
        {
            var a = cap.Indices[i]; var b = cap.Indices[i + 1]; var c = cap.Indices[i + 2];
            if (!splits.ContainsKey((a, b)) && !splits.ContainsKey((b, c)) && !splits.ContainsKey((c, a)))
            { writer.Triangle(ids[a], ids[b], ids[c], normal); continue; }
            // Keep the existing cap triangles, splitting only their boundary edges to match the side strips.
            var boundary = new List<int>();
            foreach (var (from, to) in new[] { (a, b), (b, c), (c, a) })
            {
                boundary.Add(ids[from]);
                if (splits.TryGetValue((from, to), out var p)) boundary.Add(writer.Vertex(position(p), normal, p));
            }
            var centre = (cap.Vertices[a] + cap.Vertices[b] + cap.Vertices[c]) / 3;
            var centreId = writer.Vertex(position(centre), normal, centre);
            for (var j = 0; j < boundary.Count; j++) writer.Triangle(centreId, boundary[j], boundary[(j + 1) % boundary.Count], normal);
        }
    }
}
