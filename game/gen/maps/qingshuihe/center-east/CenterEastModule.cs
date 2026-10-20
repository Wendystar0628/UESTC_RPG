using Campus.Gen.World;
using Campus.Gen.Parts.Roof;
using Campus.Gen.Parts.Openings;
using Campus.Gen.Parts.Circulation;
using Campus.Contracts.Bake;
using Campus.Contracts;
using Campus.Gen.Sites;

namespace Campus.Gen.Maps.Qingshuihe.CenterEast;

/// <summary>Center-east sites share zone routing, geometry publication and window metadata.</summary>

[ContentModule("zone.center-east")]
public sealed class CenterEastModule : ZoneModuleBase, IWindowMetadataSource, IContentSourceTraceProvider
{

    public override BakeInputDependencies BakeInputs { get; } = new(BakeInputKinds.World | BakeInputKinds.Windows, [], [SceneTextInputs.AtlasKey]);

    public CenterEastModule() : this(CenterEastSites.All) { }

    internal CenterEastModule(IReadOnlyList<ZoneSite> sites) : base(sites) { }
}

/// <summary>
/// The center-east zone's sites in a fixed order. 21a: 品学楼 (gate frame and link bridges, forecourt and cascade stone,
/// curved end bay); 21b1: the three research buildings of 中部科研区 (科 A, 科 B, 科 C); 21b2: the statue lawn (林为干像,
/// 麦克斯韦像 and the equation stone) and the 科研楼 stele (the 银杏大道 bridge is WP-28's); 21c: 文科楼群 (tower, terraced
/// podium, atrium) and its forecourt with the friendship tree's stakes and stone. 21d (training hall, courts, east edge)
/// appends its own entries after these.
/// </summary>
public static class CenterEastSites
{
    public static IReadOnlyList<ZoneSite> All { get; } = Array.AsReadOnly<ZoneSite>(
    [
        new Pinxue.PinxueFrameSite(),
        new Pinxue.PinxueForecourtSite(),
        new Pinxue.PinxueArcEndSite(),
        new Research.KeASite(),
        new Research.KeBSite(),
        new Research.KeCSite(),
        new Research.StatueLawnSite(),
        new Research.ResearchSteleSite(),
        new Wenke.WenkeSite(),
        new Wenke.WenkeForecourtSite(),
        new Sports.TrainingHallSite(),
        new Sports.CourtSites(),
        new Sports.PoolSite(),
        new Sports.PoolRoomsSite(),
        new Sports.SportsFurnitureSite(),
    ]);
}
