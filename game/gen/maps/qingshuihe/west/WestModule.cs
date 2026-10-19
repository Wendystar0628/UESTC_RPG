using Campus.Contracts;
using Campus.Gen.Sites;

namespace Campus.Gen.Maps.Qingshuihe.West;

/// <summary>
/// WP-19 west zone (MAP-QSH-WEST): the sites listed in <see cref="WestSites.All"/>, filed into cells. Generic buildings stay
/// with WP-14 (their storeys and heights set by the world/west.json records); this module claims only the landmarks it builds
/// itself (六号科研楼's three entities) and adds identity pieces (the motto stone) next to them. As the zone's one window
/// metadata source (contract-29) it declares a complete grid set for every Facade-claimed feature, gathered from its sites.
/// Pure .NET: reads the world model and terrain only.
/// </summary>
[ContentModule("zone.west", Owner = "WP-19")]
public sealed class WestModule : ZoneModuleBase, IWindowMetadataSource, ILightMetadataSource, IContentSourceTraceProvider
{
    public override BakeInputDependencies BakeInputs { get; } = new(BakeInputKinds.World | BakeInputKinds.Windows, [], [SceneTextInputs.AtlasKey]);

    public WestModule() : this(WestSites.All) { }

    internal WestModule(IReadOnlyList<ZoneSite> sites) : base(sites,
        new(TrianglesL0: WestTune.BudgetTrianglesL0, TrianglesL1: WestTune.BudgetTrianglesL1, DrawCalls: WestTune.BudgetDrawCalls)) { }
}

/// <summary>
/// The west zone's sites in a fixed order. 19a: 六号科研楼 and the motto stone (the A10 plaza is overlay paving drawn by
/// WP-15). 19b: 四号's glass entrance boxes and lawn seat wall, 创新中心's stair shafts and main portal; the other 19b
/// buildings (五号、研究院、核磁、经管楼与 7482、荟文楼、6151/6154) are WP-14's, styled in world/west.json. 19c (托幼园、
/// 湿地与西门桥头、石刻) appends its own entries after these.
/// </summary>
public static class WestSites
{
    public static IReadOnlyList<ZoneSite> All { get; } = Array.AsReadOnly<ZoneSite>(
    [
        new Six.SixSite(),
        new Motto.MottoStoneSite(),
        new Four.FourSite(),
        new Four.RoofScienceSite(),
        new Innovation.InnovationSite(),
        new Innovation.ParkingSite(),
        new Kindergarten.KeyboardSite(),
        new Kindergarten.PocketGardenSite(),
        new Wetland.BoardwalkSite(),
        new Wetland.LeveeStepsSite(),
        new Bridgehead.ShelterSite(),
        new Bridgehead.CarvingSite(),
    ]);
}
