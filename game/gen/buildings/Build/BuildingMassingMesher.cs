using Campus.Gen.Geometry;
using Campus.Gen.Buildings.Mesh;
using Campus.Gen.Buildings.Model;

namespace Campus.Gen.Buildings.Build;

/// <summary>Far colour-banded massing and flat panes, sharing the actual roof edge and slab with the near roof.</summary>
internal sealed class BuildingMassingMesher(BuildingPlan plan, GeometrySink sink, BuildingSurfaces surfaces,
    BuildingSkin shell, FacadeOpenings openings, BuildingRoofMesher roof)
{
    public void Build(bool withEdge, bool withWindows)
    {
        MassWalls(withEdge);
        if (withWindows) FlatWindows();
    }

    // ---------------------------------------------------------------------------------------------
    // L2-L4: massing with colour bands; windows as flat panes (same numbers and hashes as near).
    // ---------------------------------------------------------------------------------------------
    private void MassWalls(bool withEdge)
    {
        var o = plan.Profile.PlinthProjection;
        var top = plan.PlinthTopY;
        ProfilePoint Z(float s, float y) => new(s, y);
        var plinth = Profile.Of([Z(o, plan.SunkY), Z(o, top), Z(0f, top)]);
        for (var r = 0; r < plan.RingCount; r++)
            Prims.Sweep(sink, surfaces.Plinth, plan.SkinAt(r), plinth);
        if (withEdge)
        {
            var yr = plan.RoofY;
            if (plan.Profile.RoofEdge == Rules.RoofEdge.Cornice)
            {
                var proj = plan.Small ? 0.22f : 0.34f;
                var ctop = yr + (plan.Small ? 0.05f : 0.10f);
                shell.Sweep(surfaces.Wall, Profile.Of([Z(0f, top), Z(0f, plan.WallTopY)]));
                shell.Sweep(surfaces.Trim, Profile.Of([Z(0f, plan.WallTopY), Z(proj, plan.WallTopY), Z(proj, ctop), Z(0.12f, ctop)]));
                shell.Sweep(surfaces.Wall, Profile.Of([Z(0.12f, ctop), Z(0.12f, plan.EdgeTopY - 0.15f)]));
                EdgeCap(0.12f);
            }
            else
            {
                shell.Sweep(surfaces.Wall, Profile.Of([Z(0f, top), Z(0f, plan.WallTopY)]));
                EdgeCap(0f);
            }
        }
        else
        {
            shell.Sweep(surfaces.Wall, Profile.Of([Z(0f, top), Z(0f, plan.EdgeTopY)]));
            Prims.Cap(sink, surfaces.Roof, plan.SkinOuter, plan.SkinHoles, 0f, 0f, plan.EdgeTopY, up: true);
        }
    }

    /// <summary>L2 roof edge: the same coping as near (the part's far level) and the roof slab.</summary>
    private void EdgeCap(float s0)
    {
        roof.BuildCoping(s0, plan.EdgeTopY - 0.15f, plan.EdgeTopY);
        roof.BuildSlab();
    }

    private void FlatWindows()
    {
        foreach (var (fi, f, layout, _) in shell.Facades())
            foreach (var o in plan.Openings[fi])
                openings.FlatGlass(f, layout, o, 0.012f);
    }
}
