using Campus.Contracts;

namespace Campus.Gen.Zones.Core;

/// <summary>
/// Entity IDs of the core zone (MAP-QSH-CORE "数据定位"). The `qsh:` prefix is the data pipeline's, not a map ID.
/// Design additions carry the `design:core-` IDs registered in core.overlay.json.
/// </summary>
public static class CoreFeatures
{
    public static readonly FeatureId TimePlaza = new("qsh:way:552968505");
    /// <summary>Grand stair base of the library (8398): the axis runs from the plaza centre to it.</summary>
    public static readonly FeatureId LibraryStair = new("qsh:way:806848398");
    public static readonly FeatureId Library = new("qsh:way:806848396");
    public static readonly FeatureId Museum = new("qsh:way:806848402");

    /// <summary>Ring steps of the sunken plaza: registered so the axis corridor's steps interval names a real structure.</summary>
    public static readonly FeatureId PlazaRingSteps = new("design:core-plaza-ring-steps");
    /// <summary>Sundial (real, not in OSM): registered with its position in core.overlay.json.</summary>
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
}
