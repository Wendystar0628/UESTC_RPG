using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Geometry;

namespace Campus.Gen.Interior.Shell.Box;

internal sealed class BoxInteriorMeshBuilder
{
    private readonly List<GeometryVertex> _vertices = [];
    private readonly List<int> _indices = [];

    public void AddSteps(RampCollider ramp)
    {
        int count = ramp.Stair?.Count ?? 24;
        for (int i = 0; i < count; i++)
        {
            float start = (float)i / count, end = (float)(i + 1) / count;
            Vector3 a = Vector3.Lerp(ramp.P0, ramp.P3, start), b = Vector3.Lerp(ramp.P1, ramp.P2, start);
            Vector3 c = Vector3.Lerp(ramp.P1, ramp.P2, end), d = Vector3.Lerp(ramp.P0, ramp.P3, end);
            a.Y = b.Y = c.Y;
            Prism([a, b, c, d], ramp.Thick + (ramp.P3.Y - ramp.P0.Y) / count);
        }
    }

    public void Add(Collider collider)
    {
        switch (collider)
        {
            case SlabCollider slab:
                Prism(slab.Ring.Select(p => new Vector3(p.X, slab.Y, p.Y)).ToArray(), slab.Thick);
                break;
            case WallCollider wall:
                Vector2 delta = wall.B - wall.A;
                Vector2 normal = Vector2.Normalize(new(-delta.Y, delta.X)) * wall.HalfT;
                Vector2[] ring = [wall.A - normal, wall.B - normal, wall.B + normal, wall.A + normal];
                Prism(ring.Select(p => new Vector3(p.X, wall.Y1, p.Y)).ToArray(), wall.Y1 - wall.Y0);
                break;
            case RampCollider ramp:
                Prism([ramp.P0, ramp.P1, ramp.P2, ramp.P3], ramp.Thick);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(collider), collider.GetType().Name, "Unsupported box interior mesh primitive.");
        }
    }

    private void Prism(Vector3[] top, float thickness)
    {
        var down = Vector3.UnitY * thickness;
        // Rings are clockwise when seen from +Y; reversed indices below match Godot's front-face winding.
        Quad(top[3], top[2], top[1], top[0]);
        Quad(top[0] - down, top[1] - down, top[2] - down, top[3] - down);
        for (int i = 0; i < 4; i++)
        {
            int next = (i + 1) % 4;
            Quad(top[i], top[next], top[next] - down, top[i] - down);
        }
    }

    private void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        int start = _vertices.Count;
        Vector3 normal = Vector3.Normalize(Vector3.Cross(b - a, c - a));
        foreach (var p in new[] { a, b, c, d })
            _vertices.Add(new(p, normal));
        _indices.AddRange([start, start + 2, start + 1, start, start + 3, start + 2]);
    }

    public IReadOnlyList<GeometryBundle> Build(ToonRef material)
    {
        var sink = new GeometrySink();
        sink.Polygon(material.Spec, _vertices, _indices);
        return sink.Finish().Select(mesh => mesh with { IgnoreOcclusionCulling = true }).ToArray();
    }
}
