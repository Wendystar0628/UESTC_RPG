using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using Campus.Gen.Geometry;
using Campus.Gen.Parts.Core;

namespace Campus.Gen.Interior;

public sealed partial class InteriorGeometry
{
    public void Add(PartGeometry geometry, Matrix4x4 at, byte flags = VertexFlag.Interior)
    {
        if (Draw) AddPart(geometry, at, flags);
    }

    public void Part<T>(IPartDef<T> definition, T parameters, Matrix4x4 at, bool solid = false) where T : notnull
    {
        if (Draw) Add(definition.Lods[LodLevel.L0](parameters), at);
        if (solid)
            foreach (var collider in definition.Collision(parameters))
                Colliders.Add(collider switch
                {
                    SlabCollider slab => slab with { Ring = slab.Ring.Select(p => GeometryPlacement.Plan(p, at)).ToArray(), Y = slab.Y + at.M42 },
                    SlopedWallCollider wall => wall with
                    {
                        A = GeometryPlacement.Plan(wall.A, at), B = GeometryPlacement.Plan(wall.B, at),
                        Y0a = wall.Y0a + at.M42, Y1a = wall.Y1a + at.M42,
                        Y0b = wall.Y0b + at.M42, Y1b = wall.Y1b + at.M42
                    },
                    _ => GeometryPlacement.Transform(collider, at)
                });
    }

    public void Floor(PolygonRings plan, float top, float thickness, ToonSpec material, FootMat foot)
    {
        if (Draw) Add(PartGeometryKernel.Extrude(plan, thickness, material, top - thickness), Matrix4x4.Identity);
        Support(plan, top, thickness, foot);
    }

    public void Support(PolygonRings plan, float top, float thickness, FootMat foot)
    {
        Floor(plan, top, thickness, foot);
    }

    public void Plate(PolygonRings plan, float top, float thickness, ToonSpec material)
    {
        if (Draw) Add(PartGeometryKernel.Extrude(plan, thickness, material, top - thickness), Matrix4x4.Identity);
    }

    public void Wall(Vector2 a, Vector2 b, float bottom, float top, float thickness, ToonSpec material,
        SurfaceTag tag = SurfaceTag.Wall, FootMat foot = FootMat.Concrete)
    {
        if (Draw)
        {
            var frame = PlanFrame.Toward(a, b);
            float length = Vector2.Distance(a, b);
            // Opening fills and short wall spans keep their actual dimensions; the shared shape fits the bevel to them.
            var geometry = PartShapes.Box(new(0, bottom, -thickness * 0.5f), new(length, top, thickness * 0.5f),
                PartStyleDefaults.TrimEdgeBevel, material, out var at);
            Add(geometry, at * frame.At(0, 0, 0));
        }
        Colliders.Add(new WallCollider(a, b, thickness * 0.5f, bottom, top, tag) { Foot = foot });
    }

    public void Floor(PolygonRings rings, float top, float thickness, FootMat foot)
    {
        var t = PolygonTriangulator.Triangulate(rings);
        for (var i = 0; i + 2 < t.Indices.Length; i += 3)
            Colliders.Add(new SlabCollider([t.Vertices[t.Indices[i]], t.Vertices[t.Indices[i + 1]], t.Vertices[t.Indices[i + 2]]],
                top, thickness, SurfaceTag.Ground) { Foot = foot });
    }

    private void AddPart(PartGeometry geometry, Matrix4x4 at, byte flags)
    {
        foreach (var surface in geometry.Meshes)
        {
            var values = surface.Flags is null ? new byte[surface.Position.Length / 3] : (byte[])surface.Flags.Clone();
            for (var i = 0; i < values.Length; i++) values[i] |= flags;
            Mesh.Append(surface with { Flags = values }, at);
        }
    }
}
