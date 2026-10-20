using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Gen.Geometry;
using Campus.Gen.Parts.Core;

namespace Campus.Gen.Interior;

/// <summary>Storey collision collection, floor triangulation and part collision placement.</summary>
public sealed class InteriorCollision(float slabThickness)
{
    private readonly List<Collider> _items = [];

    public IReadOnlyList<Collider> Items => _items;

    public void Add(Collider collider) => _items.Add(collider);

    public void Floor(PolygonRings rings, float top, FootMat foot)
    {
        Floor(rings, top, slabThickness, foot);
    }

    public void Floor(PolygonRings rings, float top, float thickness, FootMat foot)
    {
        var triangles = PolygonTriangulator.Triangulate(rings);
        for (var i = 0; i + 2 < triangles.Indices.Length; i += 3)
        {
            Add(new SlabCollider([
                triangles.Vertices[triangles.Indices[i]],
                triangles.Vertices[triangles.Indices[i + 1]],
                triangles.Vertices[triangles.Indices[i + 2]]], top, thickness, SurfaceTag.Ground) { Foot = foot });
        }
    }

    public void Wall(Vector2 a, Vector2 b, float halfThickness, float y0, float y1, SurfaceTag tag, FootMat foot)
    {
        Add(new WallCollider(a, b, halfThickness, y0, y1, tag) { Foot = foot });
    }

    public void Solid(Vector2 c, Vector2 u, float length, float depth, float y0, float y1, SurfaceTag tag, FootMat foot)
    {
        var at = PlanFrame.Along(c, new Vector2(u.Y, -u.X)).At(0, (y0 + y1) * 0.5f, 0);
        Add(new BoxCollider(at, new(depth * 0.5f, (y1 - y0) * 0.5f, length * 0.5f), tag) { Foot = foot });
    }

    public void AddTransformed(Collider collider, Matrix4x4 at)
    {
        Add(collider switch
        {
            SlabCollider slab => slab with
            {
                Ring = slab.Ring.Select(p => GeometryPlacement.Plan(p, at)).ToArray(),
                Y = slab.Y + at.M42
            },
            SlopedWallCollider wall => wall with
            {
                A = GeometryPlacement.Plan(wall.A, at),
                B = GeometryPlacement.Plan(wall.B, at),
                Y0a = wall.Y0a + at.M42,
                Y1a = wall.Y1a + at.M42,
                Y0b = wall.Y0b + at.M42,
                Y1b = wall.Y1b + at.M42
            },
            _ => GeometryPlacement.Transform(collider, at)
        });
    }
}
