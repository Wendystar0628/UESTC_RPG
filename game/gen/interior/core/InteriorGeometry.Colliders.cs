using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Geometry;

namespace Campus.Gen.Interior;

public sealed partial class InteriorGeometry
{
    public void AddSteps(RampCollider ramp, ToonSpec material)
    {
        int count = ramp.Stair?.Count ?? throw new ArgumentException("Interior stair geometry requires authored step metadata.", nameof(ramp));
        for (int i = 0; i < count; i++)
        {
            float start = (float)i / count, end = (float)(i + 1) / count;
            Vector3 a = Vector3.Lerp(ramp.P0, ramp.P3, start), b = Vector3.Lerp(ramp.P1, ramp.P2, start);
            Vector3 c = Vector3.Lerp(ramp.P1, ramp.P2, end), d = Vector3.Lerp(ramp.P0, ramp.P3, end);
            a.Y = b.Y = c.Y;
            ColliderPrism([a, b, c, d], ramp.Thick + (ramp.P3.Y - ramp.P0.Y) / count, material);
        }
    }

    private void ColliderPrism(Vector3[] top, float thickness, ToonSpec material)
    {
        var down = Vector3.UnitY * thickness;
        // Rings are clockwise when seen from +Y; reversed indices below match Godot's front-face winding.
        ColliderQuad(top[3], top[2], top[1], top[0], material);
        ColliderQuad(top[0] - down, top[1] - down, top[2] - down, top[3] - down, material);
        for (int i = 0; i < 4; i++)
        {
            int next = (i + 1) % 4;
            ColliderQuad(top[i], top[next], top[next] - down, top[i] - down, material);
        }
    }

    private void ColliderQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, ToonSpec material)
    {
        Vector3 normal = Vector3.Normalize(Vector3.Cross(b - a, c - a));
        Mesh.Quad(material, new(a, normal), new(b, normal), new(c, normal), new(d, normal));
    }
}
