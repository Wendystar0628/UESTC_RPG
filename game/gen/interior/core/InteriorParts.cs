using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Parts;
using Campus.Gen.Geometry;
using Campus.Gen.Parts.Core;

namespace Campus.Gen.Interior;

/// <summary>Shared part mesh placement and optional collision from the same authored parameters.</summary>
public sealed class InteriorParts(GeometrySink mesh, InteriorCollision collision, bool draw)
{
    public void Add(PartGeometry geometry, Matrix4x4 at, byte flags = VertexFlag.Interior)
    {
        if (!draw) return;
        foreach (var surface in geometry.Meshes)
        {
            var values = surface.Flags is null ? new byte[surface.Position.Length / 3] : (byte[])surface.Flags.Clone();
            for (var i = 0; i < values.Length; i++) values[i] |= flags;
            mesh.Append(surface with { Flags = values }, at);
        }
    }

    public void Part<T>(IPartDef<T> definition, T parameters, Matrix4x4 at, bool solid = false) where T : notnull
    {
        if (draw) Add(definition.Lods[LodLevel.L0](parameters), at);
        if (!solid) return;
        foreach (var collider in definition.Collision(parameters))
        {
            collision.AddTransformed(collider, at);
        }
    }

    public void Plate(PolygonRings plan, float top, float thickness, ToonSpec material)
    {
        if (draw) Add(PartGeometryKernel.Extrude(plan, thickness, material, top - thickness), Matrix4x4.Identity);
    }
}
