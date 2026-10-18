using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.Parts.Core;

/// <summary>Minimal clockwise mesh writer for shapes the kernel has no constructor for (lathes, tetrahedra, flat quads).</summary>
public sealed class MeshWriter(ToonSpec material)
{
    private readonly List<float> _position = [];
    private readonly List<float> _normal = [];
    private readonly List<float> _uv = [];
    private readonly List<int> _index = [];

    public int Vertex(Vector3 p, Vector3 n, Vector2 uv)
    {
        _position.Add(p.X); _position.Add(p.Y); _position.Add(p.Z);
        _normal.Add(n.X); _normal.Add(n.Y); _normal.Add(n.Z);
        _uv.Add(uv.X); _uv.Add(uv.Y);
        return _position.Count / 3 - 1;
    }

    public void Triangle(int a, int b, int c, Vector3 outward)
    {
        Vector3 P(int i) => new(_position[3 * i], _position[3 * i + 1], _position[3 * i + 2]);
        var pa = P(a); var pb = P(b); var pc = P(c);
        var u = new Vector3(pb.X - pa.X, pb.Y - pa.Y, pb.Z - pa.Z); var v = new Vector3(pc.X - pa.X, pc.Y - pa.Y, pc.Z - pa.Z);
        var cross = new Vector3(u.Y * v.Z - u.Z * v.Y, u.Z * v.X - u.X * v.Z, u.X * v.Y - u.Y * v.X);
        var direction = (double)cross.X * outward.X + (double)cross.Y * outward.Y + (double)cross.Z * outward.Z;
        if (!double.IsFinite(direction) || direction == 0) throw new ArgumentException("A face is degenerate.");
        // Godot's front face is clockwise when viewed from outside.
        _index.Add(a); _index.Add(direction < 0 ? b : c); _index.Add(direction < 0 ? c : b);
    }

    public PartGeometry Finish()
    {
        if (_index.Count == 0) throw new InvalidOperationException("A shape must contain geometry.");
        float[] p = [.. _position];
        var min = new Vector3(p[0], p[1], p[2]); var max = min;
        for (var i = 0; i < p.Length; i += 3)
        {
            min = new(Math.Min(min.X, p[i]), Math.Min(min.Y, p[i + 1]), Math.Min(min.Z, p[i + 2]));
            max = new(Math.Max(max.X, p[i]), Math.Max(max.Y, p[i + 1]), Math.Max(max.Z, p[i + 2]));
        }
        var mesh = new GeometryBundle(new ToonRef(material), p, [.. _normal], [.. _index], Uv: [.. _uv]);
        mesh.ValidateVertexData();
        return new([mesh], new(min, max));
    }
}
