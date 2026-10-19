using Campus.Contracts;
using Campus.Gen.Geometry;
using Campus.Gen.Sites;

namespace Campus.Gen.Maps.Qingshuihe.South;

/// <summary>South-zone routing and window metadata over the shared site module.</summary>
[ContentModule("zone.south", Owner = "WP-18")]
public sealed class SouthModule : ZoneModuleBase, IWindowMetadataSource, IContentSourceTraceProvider
{
    public SouthModule() : this(SouthSites.All) { }

    internal SouthModule(IReadOnlyList<ZoneSite> sites) : base(sites,
        new(SouthTune.BudgetTrianglesL0, SouthTune.BudgetTrianglesL1, SouthTune.BudgetDrawCalls),
        GeometryMergePolicy.WindowChannels) { }
}

/// <summary>
/// The south zone's sites in a fixed order. 18a: gate and entrance axis, main building, forest park; 18b: Xuanbang, Liren,
/// east lake; 18c: the gym and natatorium, the sports platform, the stand, the west lawn by the innovation centre, the ordinary
/// buildings' probes (their massing stays WP-14's) and the plaza inside the gate.
/// </summary>
public static class SouthSites
{
    public static IReadOnlyList<ZoneSite> All { get; } = Array.AsReadOnly<ZoneSite>(
    [
        new Gate.SouthGateSite(),
        new MainBuilding.EntranceAxisSite(),
        new MainBuilding.MainBuildingSite(),
        new ForestPark.ForestParkSite(),
        new Xuanbang.XuanbangSite(),
        new Liren.LirenSite(),
        new EastLake.EastLakeSite(),
        new Sports.GymSite(),
        new Sports.NatatoriumSite(),
        new Sports.PlatformSite(),
        new Sports.GrandstandSite(),
        new Innovation.WestLawnSite(),
        new Ordinary.OrdinaryBuildingsSite(),
        new Gate.GatePlazaSite(),
    ]);
}
