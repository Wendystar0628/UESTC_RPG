using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.Parts.Core;

internal static class GeometryMath
{
    internal static void Positive(float value, string name)
    {
        if (!float.IsFinite(value) || value <= 0) throw new ArgumentOutOfRangeException(name, "A dimension must be finite and positive.");
    }
    internal static Vector3 Point(Vector2 p, Vector3 origin, Vector3 u, Vector3 v, Vector3 axis, float h) =>
        new(origin.X + u.X * p.X + v.X * p.Y + axis.X * h,
            origin.Y + u.Y * p.X + v.Y * p.Y + axis.Y * h,
            origin.Z + u.Z * p.X + v.Z * p.Y + axis.Z * h);
}

internal sealed class GeometryWriter(ToonSpec material)
{
    private readonly List<Vector3> positions = [];
    private readonly List<Vector3> normals = [];
    private readonly List<Vector2> uvs = [];
    private readonly List<int> indices = [];
    private readonly List<float> ambientVisibility = [];

    // GeometryBundle.Ao is visibility: 1 is unoccluded, 0 is fully occluded.
    internal int Vertex(Vector3 position, Vector3 normal, Vector2 uv, float visibility = 1)
    {
        if (!VectorMath.Finite(position)) throw new ArgumentException("Coordinates must be finite.", nameof(position));
        if (!VectorMath.Finite(normal)) throw new ArgumentException("Coordinates must be finite.", nameof(normal));
        if (positions.Count >= 65536) throw new ArgumentException("A part surface exceeds 65,536 vertices; split it before baking.");
        positions.Add(position); normals.Add(normal); uvs.Add(uv); ambientVisibility.Add(visibility);
        return positions.Count - 1;
    }
    internal void Triangle(int a, int b, int c, Vector3 outward)
    {
        var direction = VectorMath.TriangleOrientation(positions[a], positions[b], positions[c], outward);
        if (!double.IsFinite(direction) || direction == 0) throw new ArgumentException("A face is degenerate.");
        // Godot's front face is clockwise when viewed from outside.
        indices.Add(a); indices.Add(direction < 0 ? b : c); indices.Add(direction < 0 ? c : b);
    }
    internal void Quad(int a, int b, int c, int d, Vector3 outward)
    { Triangle(a, b, c, outward); Triangle(a, c, d, outward); }

    internal PartGeometry Finish()
    {
        ArgumentNullException.ThrowIfNull(material);
        if (positions.Count == 0) throw new InvalidOperationException("A part must contain geometry.");
        var p = new float[positions.Count * 3]; var n = new float[p.Length]; var uv = new float[positions.Count * 2];
        var min = positions[0]; var max = min;
        for (var i = 0; i < positions.Count; i++)
        {
            var point = positions[i]; var normal = normals[i];
            p[3 * i] = point.X; p[3 * i + 1] = point.Y; p[3 * i + 2] = point.Z;
            n[3 * i] = normal.X; n[3 * i + 1] = normal.Y; n[3 * i + 2] = normal.Z;
            uv[2 * i] = uvs[i].X; uv[2 * i + 1] = uvs[i].Y;
            min = new(Math.Min(min.X, point.X), Math.Min(min.Y, point.Y), Math.Min(min.Z, point.Z));
            max = new(Math.Max(max.X, point.X), Math.Max(max.Y, point.Y), Math.Max(max.Z, point.Z));
        }
        var mesh = new GeometryBundle(new(material), p, n, indices.ToArray(), ambientVisibility.ToArray(), uv);
        mesh.ValidateVertexData();
        return new([mesh], new(min, max));
    }
}
