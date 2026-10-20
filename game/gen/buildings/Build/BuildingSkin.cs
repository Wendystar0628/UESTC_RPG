using Campus.Gen.Geometry;
using Campus.Contracts;
using Campus.Gen.Buildings.Mesh;
using Campus.Gen.Buildings.Model;
using Campus.Gen.Buildings.Plan;

namespace Campus.Gen.Buildings.Build;

/// <summary>LOD chamfers, profile sweeps and ordered facade frames on one building's skin rings.</summary>
internal sealed class BuildingSkin(BuildingPlan plan, LodLevel lod, GeometrySink sink)
{
    public bool Full => lod == LodLevel.L0;
    public float Chamfer => Full || lod == LodLevel.L1 ? BuildingPlan.WallChamfer : 0f;

    private float ProfileChamfer(float s) => s == 0f ? Chamfer : s > 0.1f ? (Chamfer > 0f ? 0.08f : 0f) : s > 0f ? (Chamfer > 0f ? 0.04f : 0f) : 0f;

    public ProfilePoint Point(float s, float y, float ao = 0f) => new(s, y, ProfileChamfer(s), ao);

    public void Sweep(in Face face, Profile profile)
    {
        foreach (var ring in plan.SkinRings) Prims.Sweep(sink, face, ring, profile);
    }

    public IEnumerable<(int Fi, EdgeFrame F, FacadeLayout L, PlanRing Skin)> Facades()
    {
        var fi = 0;
        for (var r = 0; r < plan.RingCount; r++)
        {
            var skin = plan.SkinAt(r);
            for (var e = 0; e < skin.Count; e++, fi++) yield return (fi, EdgeFrame.Of(skin, e), plan.Facades[fi], skin);
        }
    }
}
