using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Geometry;

namespace Campus.Gen.Parts.Core;

/// <summary>Indexed construction of kernel shapes; publication and surface splitting belong to GeometrySink.</summary>
public sealed class MeshWriter(ToonSpec material)
{
    private readonly List<GeometryVertex> _vertices = [];
    private readonly List<int> _indices = [];

    public int Vertex(Vector3 p, Vector3 n, Vector2 uv, float visibility = 1f)
    {
        if (!VectorMath.Finite(p) || !VectorMath.Finite(n)) throw new ArgumentException("Coordinates must be finite.");
        _vertices.Add(new(p, n, uv, Ao: visibility));
        return _vertices.Count - 1;
    }

    public Vector3 Normal(int i) => _vertices[i].Normal;

    public void Triangle(int a, int b, int c, Vector3 outward)
    {
        var flip = GeometryWinding.Flip(_vertices[a].Position, _vertices[b].Position, _vertices[c].Position, outward);
        _indices.Add(a); _indices.Add(flip ? c : b); _indices.Add(flip ? b : c);
    }

    public void Quad(int a, int b, int c, int d, Vector3 outward)
    { Triangle(a, b, c, outward); Triangle(a, c, d, outward); }

    public PartGeometry Finish()
    {
        if (_indices.Count == 0) throw new InvalidOperationException("A shape must contain geometry.");
        var min = _vertices[0].Position; var max = min;
        foreach (var vertex in _vertices)
        { min = Vector3.Min(min, vertex.Position); max = Vector3.Max(max, vertex.Position); }
        var sink = new GeometrySink();
        sink.Polygon(material, _vertices, _indices);
        return new(sink.Finish(), new(min, max));
    }
}
