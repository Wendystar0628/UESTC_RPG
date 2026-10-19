using Campus.Contracts;
using Campus.Gen.World;

namespace Campus.Gen.Zones.Core;

/// <summary>
/// Entity IDs of the core zone (MAP-QSH-CORE "数据定位"). The `qsh:` prefix is the data pipeline's, not a map ID.
/// Design additions carry the `design:core-` IDs registered in world/core.json.
/// </summary>
public static class CoreIds
{
    private const string OsmPrefix = "qsh";
    private static readonly OsmFeatureIds LibraryIdentity = OsmFeatureIds.Way(OsmPrefix, 806848396);
    private static readonly OsmFeatureIds MuseumIdentity = OsmFeatureIds.Way(OsmPrefix, 806848402);
    public static readonly FeatureId TimePlaza = new(OsmFeatureIds.Way(OsmPrefix, 552968505).Entity);
    /// <summary>Grand stair base of the library (8398): the axis runs from the plaza centre to it.</summary>
    public static readonly FeatureId LibraryStair = new(OsmFeatureIds.Way(OsmPrefix, 806848398).Entity);
    public static readonly FeatureId Library = new(LibraryIdentity.Entity);
    public static readonly FeatureId Museum = new(MuseumIdentity.Entity);
    /// <summary>品·悦吧 store front (8407) against the podium's north-east end: the 1F cafe door (lib.cafe) opens beside it.</summary>
    public static readonly FeatureId CafeStore = new(OsmFeatureIds.Way(OsmPrefix, 806848407).Entity);

    /// <summary>16e: the library's north blocks this zone builds (WLD-INTERIORS), each its own entity.</summary>
    public static readonly FeatureId NorthAnnex = new(OsmFeatureIds.Way(OsmPrefix, 806848403).Entity);
    public static readonly FeatureId CourtWing = new(OsmFeatureIds.Way(OsmPrefix, 806848406).Entity);
    public static readonly FeatureId LowBlock = new(OsmFeatureIds.Way(OsmPrefix, 806848408).Entity);
    public static readonly FeatureId NorthWestLink = new(OsmFeatureIds.Way(OsmPrefix, 806848409).Entity);
    public static readonly FeatureId NorthWestJoint = new(OsmFeatureIds.Way(OsmPrefix, 806848410).Entity);
    /// <summary>The north-east connector and bridge (8404, 8405, 16f): here only as blocks the north walls stand against.</summary>
    public static readonly FeatureId NorthEastConnector = new(OsmFeatureIds.Way(OsmPrefix, 806848404).Entity);
    public static readonly FeatureId NorthEastBridge = new(OsmFeatureIds.Way(OsmPrefix, 806848405).Entity);

    /// <summary>Ring steps of the sunken plaza: registered so the axis corridor's steps interval names a real structure.</summary>
    public static readonly FeatureId PlazaRingSteps = new("design:core-plaza-ring-steps");
    /// <summary>Sundial (real, not in OSM): registered with its position in world/core.json.</summary>
    public static readonly FeatureId Sundial = new("design:core-sundial");
    /// <summary>Benches under the plaza's rim lamps (MAP-QSH-CORE "本区设计增补"): design:core-plaza-bench-1, -2, ….</summary>
    public const string PlazaBenchPrefix = "design:core-plaza-bench-";

    /// <summary>Source parts of the library entity (WorldFeaturePart.SourceId, MAP-QSH-CORE "数据定位").</summary>
    public static readonly string LibraryCore = OsmFeatureIds.Way(OsmPrefix, 806848395).Part;
    public static readonly string LibraryWings = LibraryIdentity.Part;
    public static readonly string LibraryPodium = OsmFeatureIds.Way(OsmPrefix, 806848397).Part;
    public static readonly string LibraryTowerNe = OsmFeatureIds.Way(OsmPrefix, 806848391).Part;
    /// <summary>The south-east corner block is the front square tower facing the time plaza (正面方塔).</summary>
    public static readonly string LibraryTowerSe = OsmFeatureIds.Way(OsmPrefix, 806848392).Part;
    public static readonly string LibraryTowerNw = OsmFeatureIds.Way(OsmPrefix, 806848393).Part;
    public static readonly string LibraryTowerSw = OsmFeatureIds.Way(OsmPrefix, 806848394).Part;

    /// <summary>Source parts of the museum entity: the hall body (8401) and the colonnade ring round it (8402, its hole is 8401).</summary>
    public static readonly string MuseumHall = OsmFeatureIds.Way(OsmPrefix, 806848401).Part;
    public static readonly string MuseumRing = MuseumIdentity.Part;

    public const string LibraryRoofFace = "design:core-library-roof-face";
    public const string LibraryFrontFace = "design:core-library-front-face";
    public const string LibraryCafeFace = "design:core-library-cafe-face";
    public const string LibraryLanding = "design:core-library-landing";
    public const string AnnexDoorFace = "design:core-annex-door-face";
    public const string GardenPergolaFace = "design:core-garden-pergola-face";
    public const string GardenColonnadeFace = "design:core-garden-colonnade-face";
    public static readonly string[] LibraryWingSetbacks =
        ["design:core-library-wing-setback-1", "design:core-library-wing-setback-2",
         "design:core-library-wing-setback-3", "design:core-library-wing-setback-4"];
    public const string MuseumSouthFace = "design:core-museum-south-face";
    public const string MuseumWestFace = "design:core-museum-west-face";
    public const string MuseumNorthFace = "design:core-museum-north-face";
    public const string MuseumEastFace = "design:core-museum-east-face";
    public static readonly (string Body, string Ring)[] MuseumColonnadeEdges =
    [
        ("design:core-museum-body-edge-1", "design:core-museum-ring-edge-1"),
        (MuseumEastFace, "design:core-museum-ring-edge-2"),
        ("design:core-museum-body-edge-3", "design:core-museum-ring-edge-3"),
        (MuseumSouthFace, "design:core-museum-ring-edge-4"),
        ("design:core-museum-body-edge-5", "design:core-museum-ring-edge-5"),
        (MuseumWestFace, "design:core-museum-ring-edge-6"),
        ("design:core-museum-body-edge-7", "design:core-museum-ring-edge-7"),
        (MuseumNorthFace, "design:core-museum-ring-edge-8"),
    ];
    public const string GardenPalmPrefix = "design:core-quiet-garden-palm-";
    public static readonly string GinkgoAvenue = OsmFeatureIds.Way(OsmPrefix, 544688571).Source;
    public const string GinkgoWalkWest = "design:core-ginkgo-walk-west";
    public const string GinkgoWalkEast = "design:core-ginkgo-walk-east";
    public static readonly string[] GinkgoWalks = [GinkgoWalkWest, GinkgoWalkEast];
    public static readonly (string Hidden, string Walk, string Kept)[] GinkgoSidewalks =
    [
        (OsmFeatureIds.Way(OsmPrefix, 544161548).Source, GinkgoWalkWest, "design:core-ginkgo-sidewalk-west-bridge"),
        (OsmFeatureIds.Way(OsmPrefix, 544161558).Source, GinkgoWalkEast, "design:core-ginkgo-sidewalk-east-bridge"),
    ];
    // OSM side paths whose end reached into the east walk's paving: hidden whole, re-added cut back to 2.5 m off its centre line.
    public static readonly (string Hidden, string Kept)[] GinkgoSpurs =
    [
        (OsmFeatureIds.Way(OsmPrefix, 572770116).Source, "design:core-ginkgo-spur-572770116"),
        (OsmFeatureIds.Way(OsmPrefix, 572770120).Source, "design:core-ginkgo-spur-572770120"),
    ];
}
