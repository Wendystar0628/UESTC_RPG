using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Geometry;

namespace Campus.Gen.Vegetation;

/// <summary>The mother model's authored vertex channels and card accounting; array packing belongs to GeometrySink.</summary>
internal sealed class VegetationPrototypeGeometry(bool withUv)
{
    private readonly List<GeometryVertex> _vertices = [];
    private readonly List<Vector4> _c0 = [];
    private readonly List<Vector4> _c1 = [];
    private readonly List<Vector4> _c2 = [];
    private readonly List<int> _index = [];
    public int VertexCount => _vertices.Count;
    public int Triangles => _index.Count / 3;
    public int Cards { get; set; }

    public int Add(Vector3 p, Vector3 n, float ao, ushort color, byte spare, Vector4 c0, Vector4 c1, Vector4 c2, Vector2 uv)
    {
        _vertices.Add(new(p, n, withUv ? uv : null, Ao: Math.Clamp(ao, 0f, 1f), ColorIndex: color, Reserved: spare));
        _c0.Add(c0); _c1.Add(c1); _c2.Add(c2);
        return _vertices.Count - 1;
    }

    public void Tri(int a, int b, int c) => _index.AddRange([a, b, c]);

    public Box3 Bounds()
    {
        if (_vertices.Count == 0) return new Box3(Vector3.Zero, Vector3.Zero);
        var min = new Vector3(float.MaxValue);
        var max = new Vector3(float.MinValue);
        foreach (var vertex in _vertices)
        {
            min = Vector3.Min(min, vertex.Position);
            max = Vector3.Max(max, vertex.Position);
        }
        return new Box3(min, max);
    }

    public GeometryBundle Bundle(ToonSpec spec, byte flags)
    {
        var channels = GeometryChannels.Ao | GeometryChannels.Colors | GeometryChannels.Reserved;
        if (withUv) channels |= GeometryChannels.Uv;
        if (flags != 0) channels |= GeometryChannels.Flags;
        var vertices = flags == 0 ? _vertices : _vertices.Select(v => v with { Flags = flags }).ToList();
        return GeometrySink.Indexed(spec, vertices, _index, channels, _c0, _c1, _c2);
    }
}
