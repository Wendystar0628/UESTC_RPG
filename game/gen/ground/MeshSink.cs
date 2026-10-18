using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.Ground;

/// <summary>
/// Collects triangles per ground material, writes the colour slot's palette column into each vertex's colour byte,
/// shares identical vertices and fixes winding so that front faces point along the intended normal (Godot uses
/// clockwise front faces). Output order is stable: material, then paving profile, then flags, then chunk.
/// Patterned plaza faces (Q-015) carry continuous metre UVs in their paving frame and UV2 = (gate, K), constant on each
/// triangle; vertices are shared only between triangles with the same UV2.
/// </summary>
internal sealed class MeshSink(SurfaceMaterialCatalog? materials = null)
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
        // The surface profile and metre UVs are resolved here, while source/role/direction remain available.
        // Source IDs and directions never become GPU material or batching keys: only the profile does.
        var material = input is { } face ? GroundPalette.MaterialOf(face.Surface, face.Role, color) : GroundPalette.MaterialOf(color);
        PavingFrame? frame = null;
        var gate = (Gate: 0f, Zone: 0f);
        if (input is { } paved)
        {
            var centre = new Vector2((a.X + b.X + c.X) / 3f, (a.Z + b.Z + c.Z) / 3f);
            frame = paved.PavingAt(centre);
            if (frame is not null) gate = paved.Context.Paving.GateAt(centre);
        }
        var chunk = Current(material, frame?.Profile ?? GroundPaving.None, flags, 3);
        var id = GroundPalette.Id(color);
        var ia = chunk.Vertex(a, na, id, frame, gate);
        var ib = chunk.Vertex(b, nb, id, frame, gate);
        var ic = chunk.Vertex(c, nc, id, frame, gate);
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
            var material = (GroundMaterial)(key >> 16);
            var profile = (byte)((key >> 8) & 0xff);
            var flags = (byte)(key & 0xff);
            var spec = GroundPalette.Spec(material);
            if (profile != GroundPaving.None)
                spec = spec with { SurfacePattern = (materials ?? throw new InvalidOperationException("Patterned ground requires the map's surface material catalogue."))
                    .Get(GroundPaving.MaterialId(profile)) };
            foreach (var chunk in list)
            {
                if (chunk.Index.Count == 0) continue;
                var count = chunk.Position.Count / 3;
                var flagArray = new byte[count];
                Array.Fill(flagArray, flags);
                var bundle = new GeometryBundle(new ToonRef(spec), chunk.Position.ToArray(), chunk.Normal.ToArray(), chunk.Index.ToArray())
                {
                    ColorIndices = chunk.Colors.ToArray(),
                    Flags = flagArray,
                    Uv = profile != GroundPaving.None ? chunk.Uv.ToArray() : null,
                    Uv2 = profile != GroundPaving.None ? chunk.Uv2.ToArray() : null
                };
                result.Add(bundle);
            }
        }
        return result;
    }

    private Chunk Current(GroundMaterial material, byte profile, byte flags, int needed)
    {
        var key = ((int)material << 16) | (profile << 8) | flags;
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
        private readonly Dictionary<(Vector3, Vector3, ushort, float, float, PavingFrame?), int> _map = new();
        public List<float> Position { get; } = [];
        public List<float> Normal { get; } = [];
        public List<ushort> Colors { get; } = [];
        public List<float> Uv { get; } = [];
        public List<float> Uv2 { get; } = [];
        public List<int> Index { get; } = [];

        /// <summary>One chunk holds one profile: either every vertex has a frame (UV, UV2) or none does.</summary>
        public int Vertex(Vector3 p, Vector3 n, ushort color, PavingFrame? frame, (float Gate, float Zone) gate)
        {
            var key = (p, n, color, gate.Gate, gate.Zone, frame);
            if (_map.TryGetValue(key, out var index)) return index;
            index = Position.Count / 3;
            Position.Add(p.X); Position.Add(p.Y); Position.Add(p.Z);
            Normal.Add(n.X); Normal.Add(n.Y); Normal.Add(n.Z);
            Colors.Add(color);
            if (frame is not null)
            {
                var uv = frame.Uv(new(p.X, p.Z));
                Uv.Add(uv.X); Uv.Add(uv.Y);
                Uv2.Add(gate.Gate); Uv2.Add(gate.Zone);
            }
            _map.Add(key, index);
            return index;
        }
    }
}
