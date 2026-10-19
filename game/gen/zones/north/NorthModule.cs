using Campus.Contracts;
using Campus.Gen.Zones.Common;

namespace Campus.Gen.Zones.North;

/// <summary>
/// WP-20 north living zone (MAP-QSH-NORTH): the sites listed in <see cref="NorthSites.All"/>, filed into cells.
/// Generic buildings (the dormitories among them) stay with WP-14; this module claims only the features it builds itself
/// and adds the zone's own parts next to the generic massing: cluster fences and gates, entrances, canteen and street
/// identity parts. Pure .NET: reads the world model, the interior index and the terrain only.
/// </summary>
[ContentModule("zone.north", Owner = "WP-20")]
public sealed class NorthModule : ZoneModuleBase, IWindowMetadataSource, IContentSourceTraceProvider
{
    public NorthModule() : this(NorthSites.All) { }

    internal NorthModule(IReadOnlyList<ZoneSite> sites)
        : base(sites, new(NorthTune.BudgetTrianglesL0, NorthTune.BudgetTrianglesL1, NorthTune.BudgetDrawCalls)) { }
}

/// <summary>
/// The north zone's sites in a fixed order. 20a: the 学知苑 and international students' cluster enclosures and
/// 学知苑 1 栋's open front door. 20b appends 硕丰苑 and 博翰苑 (clusters into <see cref="NorthTune.Clusters"/> plus their
/// own sites; the three blocks of WLD-INTERIORS are styled for WP-14, not built here); 20c appends the canteens, the shopping street, the student
/// centre and auditorium, the hospital, the covered walkway and the cultural landmarks, after these.
/// </summary>
public static class NorthSites
{
    public static IReadOnlyList<ZoneSite> All { get; } = Array.AsReadOnly<ZoneSite>(
    [
        // 20a
        .. NorthTune.Clusters.Select(c => (ZoneSite)new Clusters.ClusterSite(c)),
        new Xuezhi.HallEntranceSite(),
        // 20a-2
        new Xuezhi.CourtyardSite(),
        // 20b1 (its clusters are in NorthTune.Clusters above)
        new Shuofeng.RestDeckSite(),
        // 20b2 adds no site: 博翰's clusters are in NorthTune.Clusters above, the WLD-INTERIORS blocks go to WP-14
        // through the family and facade fields in world/north.json.
        // 20a-4: the canal-bank railing from the north-gate plaza's (WP-30) up the 商业街 side
        new Canal.CanalRailSite(),
        // 20c: identity geometry replaces the solid drum, link and arcade, and completes the food-court route.
        new Identity.AuditoriumSite(),
        new Identity.ActivityLobbySite(),
        new Identity.ActivityRoofSite(),
        new Identity.ShoppingStreetSite(),
        new Identity.EscalatorSite(NorthFeatureIds.EscalatorYinxing),
        new Identity.EscalatorSite(NorthFeatureIds.EscalatorSixueWest),
        new Identity.EscalatorSite(NorthFeatureIds.EscalatorSixueEast),
        new Identity.ChaoyangStairSite(),
        new Identity.CoveredWalkSite(),
        new Identity.CulturalStonesSite(),
        new Identity.IdentitySignsSite(),
    ]);
}
