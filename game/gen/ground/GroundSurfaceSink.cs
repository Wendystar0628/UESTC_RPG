using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Geometry;

namespace Campus.Gen.Ground;

/// <summary>Resolves ground paints and paving coordinates, then writes into the shared material accumulator.</summary>
internal sealed class GroundSurfaceSink(SurfaceMaterialCatalog? materials = null)
{
    private readonly SortedDictionary<int, GeometrySink> _sinks = [];
    public int Triangles => _sinks.Values.Sum(sink => sink.Triangles);

    public void Triangle(GroundColor color, byte flags, Vector3 a, Vector3 b, Vector3 c,
        Vector3 na, Vector3 nb, Vector3 nc, Vector3 facing, GroundFaceInput? input = null)
    {
        if (Vector3.Cross(b - a, c - a).LengthSquared() < 1e-14f) return;
        var material = input is { } face ? GroundPalette.MaterialOf(face.Surface, face.Role, color) : GroundPalette.MaterialOf(color);
        PavingFrame? frame = null;
        var gate = (Gate: 0f, Zone: 0f);
        if (input is { } paved)
        {
            var centre = new Vector2((a.X + b.X + c.X) / 3f, (a.Z + b.Z + c.Z) / 3f);
            frame = paved.PavingAt(centre);
            if (frame is not null) gate = paved.Context.Paving.GateAt(centre);
        }
        var spec = GroundPalette.Spec(material);
        var profile = frame?.Profile ?? GroundPaving.None;
        if (profile != GroundPaving.None)
            spec = spec with { SurfacePattern = (materials ?? throw new InvalidOperationException("Patterned ground requires the map's surface material catalogue."))
                .Get(GroundPaving.MaterialId(profile)) };
        var id = GroundPalette.Id(color);
        GeometryVertex Vertex(Vector3 point, Vector3 normal) => new(point, normal,
            frame?.Uv(new(point.X, point.Z)), frame is null ? null : new Vector2(gate.Gate, gate.Zone), ColorIndex: id, Flags: flags);
        // Content classes and shadow flags remain separate even when their GPU material is shared.
        var key = ((int)material << 8) | flags;
        if (!_sinks.TryGetValue(key, out var sink)) _sinks.Add(key, sink = new());
        sink.Tri(spec, Vertex(a, na), Vertex(b, nb), Vertex(c, nc), facing);
    }

    public void Quad(GroundColor color, byte flags, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal)
    {
        Triangle(color, flags, a, b, c, normal, normal, normal, normal);
        Triangle(color, flags, a, c, d, normal, normal, normal, normal);
    }

    public IReadOnlyList<GeometryBundle> Finish() => _sinks.Values.SelectMany(sink => sink.Finish()).ToArray();
}
