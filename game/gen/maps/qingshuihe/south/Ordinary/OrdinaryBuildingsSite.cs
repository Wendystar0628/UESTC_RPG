using Campus.Gen.Geometry;
using Campus.Contracts;
using Campus.Gen.Sites;

namespace Campus.Gen.Maps.Qingshuihe.South.Ordinary;

/// <summary>
/// The south zone's ordinary buildings stay with WP-14's facade rules (MAP-QSH-SOUTH): the innovation centre, the Kechuang
/// canteen, the orphan blocks 6152, 6194 and 6195, and buildings 1–8 with the public toilet (the sports platform and the stand are
/// <see cref="Sports.PlatformSite"/>'s and <see cref="Sports.GrandstandSite"/>'s).
/// Their storeys, heights and families are set in world/south.json; this site claims nothing and builds nothing, it only
/// registers the zone's inspection probes on them (MAP-QSH-SOUTH Q5, Q6, Q8 (a)), from their OSM footprints.
/// </summary>
public sealed class OrdinaryBuildingsSite : ZoneSite
{
    public static readonly string InnovationA = SouthIds.InnovationA.Entity, InnovationC = SouthIds.InnovationC.Entity, Building4 = SouthIds.Building4.Entity;

    public override string Id => "18c.ordinary";

    public override IReadOnlyList<Campus.Contracts.Maps.MapRect> Ranges(IWorldModel world) => [];

    public override void Build(SiteBuilder b) { }
}
