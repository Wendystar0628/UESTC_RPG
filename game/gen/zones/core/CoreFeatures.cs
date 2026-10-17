using Campus.Contracts;

namespace Campus.Gen.Zones.Core;

/// <summary>
/// Entity IDs of the core zone (MAP-QSH-CORE "数据定位"). The `qsh:` prefix is the data pipeline's, not a map ID.
/// Design additions carry the `design:core-` IDs registered in overlay/core.d/.
/// </summary>
public static class CoreFeatures
{
    public static readonly FeatureId TimePlaza = new("qsh:way:552968505");
    /// <summary>Grand stair base of the library (8398): the axis runs from the plaza centre to it.</summary>
    public static readonly FeatureId LibraryStair = new("qsh:way:806848398");
    public static readonly FeatureId Library = new("qsh:way:806848396");
    public static readonly FeatureId Museum = new("qsh:way:806848402");
    /// <summary>品·悦吧 store front (8407) against the podium's north-east end: the 1F cafe door (lib.cafe) opens beside it.</summary>
    public static readonly FeatureId CafeStore = new("qsh:way:806848407");

    /// <summary>16e: the library's north blocks this zone builds (WLD-INTERIORS), each its own entity.</summary>
    public static readonly FeatureId NorthAnnex = new("qsh:way:806848403");
    public static readonly FeatureId CourtWing = new("qsh:way:806848406");
    public static readonly FeatureId LowBlock = new("qsh:way:806848408");
    public static readonly FeatureId NorthWestLink = new("qsh:way:806848409");
    public static readonly FeatureId NorthWestJoint = new("qsh:way:806848410");
    /// <summary>The north-east connector and bridge (8404, 8405, 16f): here only as blocks the north walls stand against.</summary>
    public static readonly FeatureId NorthEastConnector = new("qsh:way:806848404");
    public static readonly FeatureId NorthEastBridge = new("qsh:way:806848405");

    /// <summary>Ring steps of the sunken plaza: registered so the axis corridor's steps interval names a real structure.</summary>
    public static readonly FeatureId PlazaRingSteps = new("design:core-plaza-ring-steps");
    /// <summary>Sundial (real, not in OSM): registered with its position in overlay/core.d/.</summary>
    public static readonly FeatureId Sundial = new("design:core-sundial");
    /// <summary>Benches under the plaza's rim lamps (MAP-QSH-CORE "本区设计增补"): design:core-plaza-bench-1, -2, ….</summary>
    public const string PlazaBenchPrefix = "design:core-plaza-bench-";

    /// <summary>Source parts of the library entity (WorldFeaturePart.SourceId, MAP-QSH-CORE "数据定位").</summary>
    public const string LibraryCore = "osm:way:806848395";
    public const string LibraryWings = "osm:way:806848396";
    public const string LibraryPodium = "osm:way:806848397";
    public const string LibraryTowerNe = "osm:way:806848391";
    /// <summary>The south-east corner block is the front square tower facing the time plaza (正面方塔).</summary>
    public const string LibraryTowerSe = "osm:way:806848392";
    public const string LibraryTowerNw = "osm:way:806848393";
    public const string LibraryTowerSw = "osm:way:806848394";

    /// <summary>Source parts of the museum entity: the hall body (8401) and the colonnade ring round it (8402, its hole is 8401).</summary>
    public const string MuseumHall = "osm:way:806848401";
    public const string MuseumRing = "osm:way:806848402";
}
