using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Gen.Geometry;
using Campus.Gen.Parts.Core;

namespace Campus.Gen.Interior;

/// <summary>Storey assembly: shared ordinary mesh stream, matching floor/wall collision and final interior output.</summary>
public sealed class InteriorGeometry
{
    private readonly GeometrySink _mesh;

    public InteriorGeometry(PaletteCatalog? palette = null, bool draw = true, bool full = true,
        float slabThickness = 0.25f, Func<string, ToonSpec>? glowMaterial = null)
    {
        _mesh = new GeometrySink(palette);
        Draw = draw;
        Full = full;
        Shapes = new InteriorShapes(_mesh, draw, slabThickness);
        Collision = new InteriorCollision(slabThickness);
        Parts = new InteriorParts(_mesh, Collision, draw);
        Lights = new InteriorLights(draw, glowMaterial ?? MissingGlowMaterial);
    }

    public bool Draw { get; }
    public bool Full { get; }
    public InteriorShapes Shapes { get; }
    public InteriorCollision Collision { get; }
    public InteriorParts Parts { get; }
    public InteriorLights Lights { get; }

    public void Floor(PolygonRings plan, float top, float thickness, ToonSpec material, FootMat foot)
    {
        if (Draw) Parts.Add(PartSolids.Extrude(plan, thickness, material, top - thickness), Matrix4x4.Identity);
        Collision.Floor(plan, top, thickness, foot);
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
            Parts.Add(geometry, at * frame.At(0, 0, 0));
        }
        Collision.Wall(a, b, thickness * 0.5f, bottom, top, tag, foot);
    }

    public ContentOutput Finish()
    {
        var meshes = new List<GeometryBundle>();
        foreach (var bundle in _mesh.Finish())
        {
            meshes.Add(bundle with { IgnoreOcclusionCulling = true });
        }
        meshes.AddRange(Lights.Finish());
        return new ContentOutput(meshes, []);
    }

    private static ToonSpec MissingGlowMaterial(string family)
    {
        throw new InvalidOperationException("No emissive material was supplied.");
    }
}
