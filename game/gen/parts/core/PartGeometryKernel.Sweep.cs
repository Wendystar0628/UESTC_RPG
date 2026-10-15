using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.Parts.Core;

/// <summary>Counterclockwise section in (lateral, up) metres. Optional vertex normals interpolate across chamfer bands.</summary>
public sealed record SweepProfile(IReadOnlyList<Vector2> Points, IReadOnlyList<Vector2>? Normals = null);

public static partial class PartGeometryKernel
{
    /// <summary>Closed rectangular section with a single chamfer at each corner and flat principal-face normals.</summary>
    public static SweepProfile BeveledRectangleProfile(float width, float height, float bevel)
    {
        GeometryMath.Positive(width, nameof(width)); GeometryMath.Positive(height, nameof(height));
        ValidateBevel(bevel, Math.Min(width, height) * 0.5f);
        var x = width * 0.5f;
        return bevel == 0
            ? new([new(-x, 0), new(x, 0), new(x, height), new(-x, height)])
            : new([new(-x + bevel, 0), new(x - bevel, 0), new(x, bevel), new(x, height - bevel), new(x - bevel, height), new(-x + bevel, height), new(-x, height - bevel), new(-x, bevel)],
                [new(0, -1), new(0, -1), new(1, 0), new(1, 0), new(0, 1), new(0, 1), new(-1, 0), new(-1, 0)]);
    }

    /// <summary>
    /// Sweeps a solid section along an X/Z polyline, with exact miter joins. Lateral positive is right of the path.
    /// Paths have no duplicate endpoint; closed=true joins last to first. Reversals and over-limit miters fail.
    /// Caller must keep the swept corridor free of self-overlap; this is not a solid-union operation.
    /// </summary>
    public static PartGeometry Sweep(SweepProfile profile, IReadOnlyList<Vector2> path, ToonSpec material, bool closed = false, float baseY = 0, float miterLimit = 4)
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
                if (!float.IsFinite(n.X) || !float.IsFinite(n.Y) || Math.Abs((double)n.X * n.X + (double)n.Y * n.Y - 1) > 1e-5)
                    throw new ArgumentException("Section normals must be finite unit vectors.", nameof(profile));
        }
        var segmentCount = closed ? path.Count : path.Count - 1;
        var tangents = new Vector3[segmentCount]; var rights = new Vector3[segmentCount];
        for (var i = 0; i < segmentCount; i++)
        {
            var a = path[i]; var b = path[(i + 1) % path.Count];
            tangents[i] = GeometryMath.Unit(new(b.X - a.X, 0, b.Y - a.Y));
            rights[i] = new(tangents[i].Z, 0, -tangents[i].X);
        }
        var miters = new Vector3[path.Count];
        for (var i = 0; i < path.Count; i++)
        {
            if (!closed && i == 0) miters[i] = rights[0];
            else if (!closed && i == path.Count - 1) miters[i] = rights[^1];
            else
            {
                var a = rights[(i + segmentCount - 1) % segmentCount]; var b = rights[i % segmentCount];
                var denominator = 1 + GeometryMath.Dot(a, b);
                if (denominator < 1e-8) throw new ArgumentException("A sweep cannot reverse direction at a join.", nameof(path));
                miters[i] = GeometryMath.Scale(GeometryMath.Add(a, b), (float)(1 / denominator));
                if (GeometryMath.Dot(miters[i], miters[i]) > (double)miterLimit * miterLimit + 1e-6)
                    throw new ArgumentException("A sweep join exceeds the miter limit.", nameof(path));
            }
        }
        Vector3 Position(int ring, Vector2 p) => new(path[ring].X + miters[ring].X * p.X, baseY + p.Y, path[ring].Y + miters[ring].Z * p.X);
        var writer = new GeometryWriter(material);
        float distance = 0;
        for (var s = 0; s < segmentCount; s++)
        {
            var next = (s + 1) % path.Count; var delta = new Vector3(path[next].X - path[s].X, 0, path[next].Y - path[s].Y);
            var length = (float)Math.Sqrt(GeometryMath.Dot(delta, delta));
            for (var i = 0; i < profile.Points.Count; i++)
            {
                var j = (i + 1) % profile.Points.Count; var a = profile.Points[i]; var b = profile.Points[j];
                if (GeometryMath.Dot(GeometryMath.Sub(Position(next, a), Position(s, a)), tangents[s]) <= 0)
                    throw new ArgumentException("A sweep segment is too short for the section and adjacent miters.", nameof(path));
                var edge = EdgeNormal(a, b); var n0 = profile.Normals?[i] ?? edge; var n1 = profile.Normals?[j] ?? edge;
                Vector3 Normal(Vector2 n) => new(rights[s].X * n.X, n.Y, rights[s].Z * n.X);
                var height = (float)Math.Sqrt(((double)b.X - a.X) * (b.X - a.X) + ((double)b.Y - a.Y) * (b.Y - a.Y));
                writer.Quad(writer.Vertex(Position(s, a), Normal(n0), new(distance, 0)), writer.Vertex(Position(s, b), Normal(n1), new(distance, height)),
                    writer.Vertex(Position(next, b), Normal(n1), new(distance + length, height)), writer.Vertex(Position(next, a), Normal(n0), new(distance + length, 0)), Normal(edge));
            }
            distance += length;
        }
        if (!closed)
        {
            for (var end = 0; end < 2; end++)
            {
                var ring = end == 0 ? 0 : path.Count - 1; var normal = end == 0 ? GeometryMath.Scale(tangents[0], -1) : tangents[^1];
                var ids = new int[cap.Vertices.Length];
                for (var i = 0; i < ids.Length; i++) ids[i] = writer.Vertex(Position(ring, cap.Vertices[i]), normal, cap.Vertices[i]);
                for (var i = 0; i < cap.Indices.Length; i += 3) writer.Triangle(ids[cap.Indices[i]], ids[cap.Indices[i + 1]], ids[cap.Indices[i + 2]], normal);
            }
        }
        return writer.Finish();
    }
}
