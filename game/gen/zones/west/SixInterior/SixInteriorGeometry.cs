using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using Campus.Gen.Geometry;
using Campus.Gen.Parts.Core;
using Campus.Gen.Parts.Circulation;
using Campus.Gen.Zones.Common;

namespace Campus.Gen.Zones.West.SixInterior;

/// <summary>Storey output through the shared geometry kernel and catalogue parts.</summary>
internal sealed class SixInteriorGeometry(bool draw)
{
    private readonly PartBuilder _mesh = new();
    public List<Collider> Colliders { get; } = [];

    public void Add(PartGeometry geometry, Matrix4x4 at, byte flags = VertexFlag.Interior)
    {
        if (draw) _mesh.Add(geometry, at, flags);
    }

    public void Part<T>(IPartDef<T> definition, T parameters, Matrix4x4 at, bool solid = false) where T : notnull
    {
        if (draw) Add(definition.Lods[LodLevel.L0](parameters), at);
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
        if (draw) Add(PartGeometryKernel.Extrude(plan, thickness, material, top - thickness), Matrix4x4.Identity);
        Support(plan, top, thickness, foot);
    }

    public void Support(PolygonRings plan, float top, float thickness, FootMat foot)
    {
        var triangles = PolygonTriangulator.Triangulate(plan);
        for (var i = 0; i < triangles.Indices.Length; i += 3)
            Colliders.Add(new SlabCollider([triangles.Vertices[triangles.Indices[i]], triangles.Vertices[triangles.Indices[i + 1]],
                triangles.Vertices[triangles.Indices[i + 2]]], top, thickness, SurfaceTag.Ground) { Foot = foot });
    }

    public void Plate(PolygonRings plan, float top, float thickness, ToonSpec material)
    {
        if (draw) Add(PartGeometryKernel.Extrude(plan, thickness, material, top - thickness), Matrix4x4.Identity);
    }

    public void Wall(Vector2 a, Vector2 b, float bottom, float top, float thickness, ToonSpec material,
        SurfaceTag tag = SurfaceTag.Wall, FootMat foot = FootMat.Concrete)
    {
        if (draw)
        {
            var frame = PlanFrame.Toward(a, b);
            Add(PartGeometryKernel.BeveledBox(Vector2.Distance(a, b), top - bottom, thickness,
                PartStyleDefaults.TrimEdgeBevel, material), frame.At(Vector2.Distance(a, b) * 0.5f, bottom, 0));
        }
        Colliders.Add(new WallCollider(a, b, thickness * 0.5f, bottom, top, tag) { Foot = foot });
    }

    public void GlassGuard(Vector2 a, Vector2 b, float y, float height)
    {
        var frame = PlanFrame.Toward(a, b); var length = Vector2.Distance(a, b);
        var p = new RailingParams();
        if (draw)
        {
            var panel = CirculationStyle.PanelThickness;
            Add(PartGeometryKernel.BeveledBox(length, height - CirculationStyle.TopRailHeight - CirculationStyle.BottomRailClearance,
                panel, 0, PartMaterials.Glazing()), frame.At(length * 0.5f, y + CirculationStyle.BottomRailClearance, 0),
                VertexFlag.Interior | VertexFlag.NoShadow);
            Add(PartGeometryKernel.BeveledBox(length, CirculationStyle.TopRailHeight, p.Depth, CirculationStyle.RailBevel,
                CirculationStyle.Metal), frame.At(length * 0.5f, y + height - CirculationStyle.TopRailHeight, 0));
            var count = Math.Max(1, (int)MathF.Ceiling(length / p.PostSpacing));
            for (var i = 0; i <= count; i++)
                Add(PartGeometryKernel.BeveledBox(CirculationStyle.PostSize, height - CirculationStyle.TopRailHeight,
                    CirculationStyle.PostSize, CirculationStyle.PostBevel, CirculationStyle.Metal), frame.At(length * i / count, y, 0));
        }
        Colliders.Add(new WallCollider(a, b, p.Depth * 0.5f, y, y + WalkRules.BarrierHeight, SurfaceTag.Barrier) { Foot = FootMat.Glass });
    }

    public ContentOutput Finish()
    {
        if (!draw) return ContentOutput.Empty;
        var (min, max) = _mesh.Measure();
        return new(_mesh.Finish(new(min, max)).Meshes.Select(m => m with { IgnoreOcclusionCulling = true }).ToArray(), []);
    }
}
