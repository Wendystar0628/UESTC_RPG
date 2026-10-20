using System.Numerics;

namespace Campus.Gen.Water;

/// <summary>Exact-position welds and nondegenerate clockwise triangles of the water tessellation.</summary>
internal sealed class WaterTopology(float y)
{
    private readonly Dictionary<(int, int), int> _weld = [];
    public List<Vector3> Vertices { get; } = [];
    public List<int> Indices { get; } = [];
    public int VertexCount => Vertices.Count;

    /// <summary>Vertices are welded by exact position, so neighbouring pieces share them.</summary>
    public int Vertex(float x, float z)
    {
        var key = (BitConverter.SingleToInt32Bits(x == 0f ? 0f : x), BitConverter.SingleToInt32Bits(z == 0f ? 0f : z));
        if (_weld.TryGetValue(key, out var index)) return index;
        Vertices.Add(new(x, y, z));
        _weld.Add(key, VertexCount - 1);
        return VertexCount - 1;
    }

    /// <summary>Trapezoid with bottom row (xb0..xb1 at zb) and top row (xt0..xt1 at zt).</summary>
    public void Quad(float xb0, float xb1, float xt1, float xt0, float zb, float zt)
    {
        var a = Vertex(xb0, zb);
        var b = Vertex(xb1, zb);
        var c = Vertex(xt1, zt);
        var d = Vertex(xt0, zt);
        Triangle(a, b, c);
        Triangle(a, c, d);
    }

    public void Triangle(int a, int b, int c)
    {
        float ax = Vertices[a].X, az = Vertices[a].Z;
        float bx = Vertices[b].X, bz = Vertices[b].Z;
        float cx = Vertices[c].X, cz = Vertices[c].Z;
        // y of (b - a) x (c - a); zero area triangles are dropped.
        var cross = (bz - az) * (cx - ax) - (bx - ax) * (cz - az);
        if (MathF.Abs(cross) <= 1e-7f) return;
        if (cross > 0f) (b, c) = (c, b);
        Indices.Add(a); Indices.Add(b); Indices.Add(c);
    }
}
