using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.Ground;

/// <summary>
/// Collects triangles per ground material, writes the colour slot's palette column into each vertex's colour byte,
/// shares identical vertices and fixes winding so that front faces point along the intended normal (Godot uses
/// clockwise front faces). Output order is stable: material, then flags, then chunk.
/// </summary>
internal sealed class MeshSink
{
    private const int MaxVertices = 65536;
    private readonly SortedDictionary<int, List<Chunk>> _chunks = new();

    public int Triangles { get; private set; }

    public void Triangle(GroundColor color, byte flags, Vector3 a, Vector3 b, Vector3 c,
        Vector3 na, Vector3 nb, Vector3 nc, Vector3 facing, GroundFaceInput? input = null)
    {
        var cross = Vector3.Cross(b - a, c - a);
        if (cross.LengthSquared() < 1e-14f) return;
        if (Vector3.Dot(cross, facing) > 0f)
        {
            (b, c) = (c, b);
            (nb, nc) = (nc, nb);
        }
        // T03 resolves a selected surface profile and metre UVs here, while source/role/direction remain available.
        // Source IDs and directions never become GPU material or batching keys.
        var material = input is { } face ? GroundPalette.MaterialOf(face.Surface, face.Role, color) : GroundPalette.MaterialOf(color);
        var chunk = Current(material, flags, 3);
        var id = GroundPalette.Id(color);
        var ia = chunk.Vertex(a, na, id);
        var ib = chunk.Vertex(b, nb, id);
        var ic = chunk.Vertex(c, nc, id);
        chunk.Index.Add(ia);
        chunk.Index.Add(ib);
        chunk.Index.Add(ic);
        Triangles++;
    }

    public void Quad(GroundColor color, byte flags, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal)
    {
        Triangle(color, flags, a, b, c, normal, normal, normal, normal);
        Triangle(color, flags, a, c, d, normal, normal, normal, normal);
    }

    public IReadOnlyList<GeometryBundle> Finish()
    {
        var result = new List<GeometryBundle>();
        foreach (var (key, list) in _chunks)
        {
            var material = (GroundMaterial)(key >> 8);
            var flags = (byte)(key & 0xff);
            var spec = GroundPalette.Spec(material);
            foreach (var chunk in list)
            {
                if (chunk.Index.Count == 0) continue;
                var count = chunk.Position.Count / 3;
                var flagArray = new byte[count];
                Array.Fill(flagArray, flags);
                var bundle = new GeometryBundle(new ToonRef(spec), chunk.Position.ToArray(), chunk.Normal.ToArray(), chunk.Index.ToArray())
                {
                    ColorIds = chunk.Colors.ToArray(),
                    Flags = flagArray
                };
                result.Add(bundle);
            }
        }
        return result;
    }

    private Chunk Current(GroundMaterial material, byte flags, int needed)
    {
        var key = ((int)material << 8) | flags;
        if (!_chunks.TryGetValue(key, out var list)) _chunks.Add(key, list = [new Chunk()]);
        var chunk = list[^1];
        if (chunk.Position.Count / 3 + needed > MaxVertices)
        {
            chunk = new Chunk();
            list.Add(chunk);
        }
        return chunk;
    }

    private sealed class Chunk
    {
        private readonly Dictionary<(Vector3, Vector3, byte), int> _map = new();
        public List<float> Position { get; } = [];
        public List<float> Normal { get; } = [];
        public List<byte> Colors { get; } = [];
        public List<int> Index { get; } = [];

        public int Vertex(Vector3 p, Vector3 n, byte color)
        {
            if (_map.TryGetValue((p, n, color), out var index)) return index;
            index = Position.Count / 3;
            Position.Add(p.X); Position.Add(p.Y); Position.Add(p.Z);
            Normal.Add(n.X); Normal.Add(n.Y); Normal.Add(n.Z);
            Colors.Add(color);
            _map.Add((p, n, color), index);
            return index;
        }
    }
}
