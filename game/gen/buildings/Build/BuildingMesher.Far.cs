using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Buildings.Mesh;
using Campus.Gen.Buildings.Model;
using Campus.Gen.Buildings.Plan;
using Campus.Gen.Buildings.Rules;
using Campus.Gen.World;

namespace Campus.Gen.Buildings.Build;

public sealed partial class BuildingMesher
{
    // ---------------------------------------------------------------------------------------------
    // L2–L4: massing with colour bands; windows as flat panes (same numbers and hashes as near).
    // ---------------------------------------------------------------------------------------------
    private void MassWalls(bool withEdge)
    {
        var o = _prof.PlinthProjection;
        var top = _p.PlinthTopY;
        ProfilePoint Z(float s, float y) => new(s, y);
        var plinth = Profile.Of([Z(o, _p.SunkY), Z(o, top), Z(0f, top)]);
        for (var r = 0; r < _p.RingCount; r++)
            Prims.Sweep(_sink, _plinth, _p.SkinAt(r), plinth);
        if (withEdge)
        {
            var yr = _p.RoofY;
            if (_prof.RoofEdge == Rules.RoofEdge.Cornice)
            {
                var proj = _p.Small ? 0.22f : 0.34f;
                var ctop = yr + (_p.Small ? 0.05f : 0.10f);
                SweepAll(_wall, Profile.Of([Z(0f, top), Z(0f, _p.WallTopY)]));
                SweepAll(_trim, Profile.Of([Z(0f, _p.WallTopY), Z(proj, _p.WallTopY), Z(proj, ctop), Z(0.12f, ctop)]));
                SweepAll(_wall, Profile.Of([Z(0.12f, ctop), Z(0.12f, _p.EdgeTopY - 0.15f)]));
                EdgeCap(0.12f);
            }
            else
            {
                SweepAll(_wall, Profile.Of([Z(0f, top), Z(0f, _p.WallTopY)]));
                EdgeCap(0f);
            }
        }
        else
        {
            SweepAll(_wall, Profile.Of([Z(0f, top), Z(0f, _p.EdgeTopY)]));
            Prims.Cap(_sink, _roof, _p.SkinOuter, _p.SkinHoles, 0f, 0f, _p.EdgeTopY, up: true);
        }
    }

    /// <summary>L2 roof edge: the same coping as near (the part's far level) and the roof slab.</summary>
    private void EdgeCap(float s0)
    {
        Coping(s0, _p.EdgeTopY - 0.15f, _p.EdgeTopY);
        Roof();
    }

    private void FlatWindows()
    {
        foreach (var (fi, f, layout, _) in FacadeFrames())
            foreach (var o in _p.Openings[fi])
                _openings.FlatGlass(f, layout, o, 0.012f);
    }
}
