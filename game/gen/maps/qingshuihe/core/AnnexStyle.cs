using Campus.Contracts;

namespace Campus.Gen.Maps.Qingshuihe.Core;

/// <summary>
/// Tunable values of the library's north blocks (16e, MAP-QSH-CORE, WLD-INTERIORS): the north annex 8403,
/// the courtyard wing 8406, the low block 8408 (its roof is the 3F G terrace), the north-west link 8409 and its joint 8410,
/// and the 宁静苑 courtyard garden. Metres and degrees; "review" marks mainline initial values awaiting the main session.
/// The blocks speak the library's language: the same beige stone, the same floor lines (storey table), plinth band,
/// string courses, parapet coping and reveals, so they read as one building with it.
/// </summary>
public static class AnnexStyle
{
    // ---- Storeys: the library's own height table, so every floor line and string course runs on from the ring. The data's
    //      heights (3.5 m a storey, 14 m for 8403) are 4 m lower than this table, so heights are not read from it (D-239);
    //      the storey count of each block is the data's (AnnexSite). ----

    // ---- 8403 punched windows: one bay grid on every floor so the windows stack (行政与业务用房, review). ----
    /// <summary>Bay pitch along a face; window width (two cells, one mullion) and the margin kept at each face end.</summary>
    public const float AnnexBay = 3.6f, AnnexWindow = 2.3f, AnnexMargin = 1.4f;
    /// <summary>Upper storeys: sill and height. Ground storey: a taller window with a transom, like the library podium.</summary>
    public const float AnnexSill = 0.95f, AnnexWindowHeight = 1.9f;
    public const float AnnexGroundSill = 0.75f, AnnexGroundHeight = 2.9f, AnnexGroundTransom = 2.2f;

    /// <summary>
    /// The closed door of the north annex on its long street face (门玻璃是门厅卡, MAP-QSH-INTERIORS-LIBRARY): width, height of the
    /// opening and of the door below its transom light; a flat stone canopy over it.
    /// </summary>
    public const float AnnexDoorWidth = 2.4f, AnnexDoorHeight = 3.3f, AnnexDoorTop = 2.5f;
    public const float AnnexCanopyWidth = 2.85f, AnnexCanopyDepth = 1.6f, AnnexCanopyThick = 0.22f, AnnexCanopyGap = 0.15f;

    /// <summary>Stone pilasters on 8403's long fronts: every so many bays, width and projection (review).</summary>
    public const int AnnexPilasterEvery = 4;
    public const float AnnexPilaster = 0.75f, AnnexPilasterProject = 0.16f;

    /// <summary>Rainwater pipes (雨水管, Q5) at both ends of each long ground face of 8403: size, stand-off, inset from the end.</summary>
    public const float PipeSize = 0.13f, PipeOff = 0.04f, PipeInset = 0.45f, PipeMinFace = 12f;

    /// <summary>Roof units of 8403 seen from the 3F terrace (北辅楼屋顶设备): count, size, fence height.</summary>
    public const int AnnexRoofUnits = 3;
    public const float AnnexUnitLength = 2.2f, AnnexUnitDepth = 1.6f, AnnexUnitHeight = 1.1f;

    // ---- 宁静苑 (MAP-QSH-CORE): closed courtyard, seen from the 1F north windows and the 3F G terrace. ----
    /// <summary>Hexagonal timber pavilion (六角木亭): circumradius of the post ring, plinth, eave and apex heights, overhang.</summary>
    public const float PavilionRadius = 2.0f, PavilionPlinth = 0.36f, PavilionPlinthOut = 0.55f;
    public const float PavilionEave = 2.85f, PavilionApex = 4.55f, PavilionOverhang = 0.9f, PavilionFascia = 0.16f;
    public const float PavilionPost = 0.2f, PavilionBeam = 0.22f, PavilionRail = 0.45f, PavilionFinial = 0.5f;
    /// <summary>Red timber pergola (红色木花架) along the courtyard's long side: share of that side, depth, height, members.</summary>
    public const float PergolaShare = 0.62f, PergolaInset = 1.3f, PergolaDepth = 2.6f, PergolaHeight = 2.7f;
    public const float PergolaPost = 0.2f, PergolaPostPitch = 2.4f, PergolaBeam = 0.24f, PergolaRafter = 0.09f, PergolaRafterPitch = 0.55f;
    public const float PergolaOverhang = 0.35f;
    /// <summary>Granite colonnade (花岗岩柱廊) along the library's north wall: inset, column size and pitch, height, lintel.</summary>
    public const float ColonnadeInset = 1.8f, ColonnadeColumn = 0.42f, ColonnadePitch = 2.7f, ColonnadeHeight = 3.3f;
    public const float ColonnadeLintel = 0.45f, ColonnadePlinth = 0.18f, ColonnadeMargin = 1.4f;
    /// <summary>Timber-edged planters (木边花池) with clipped shrubs; the two palms (棕榈) stand in them, planted by game/gen/vegetation/.</summary>
    public const float PlanterLength = 3.6f, PlanterWidth = 2.0f, PlanterHeight = 0.42f, PlanterEdge = 0.14f;
    public const float PlanterGap = 1.6f;
    public const float ShrubHeight = 0.5f, ShrubWidth = 0.6f;
    /// <summary>Cut granite paving of the court (stepping path is the same stone, one tone lighter).</summary>
    public const float CourtInset = 0.05f;

    // ---- Colours: the library's plus the garden timbers (palette names only, ART-STYLE). ----
    /// <summary>Red timber of the pergola: the campus' warm terracotta red (red wood, not the signal red), review.</summary>
    public const string PergolaRed = "band.terracotta";
    /// <summary>Pavilion posts, beams and planter edges: warm natural timber (game/gen/parts/openings/'s door wood).</summary>
    public const string Timber = "door.wood";
    /// <summary>Pavilion roof: the library's dark grey roof.</summary>
    public const string PavilionRoof = "roof.grey";
    /// <summary>Granite of the colonnade: the museum's grey granite (equip.grey), so the zone's granite is one stone.</summary>
    public const string ColonnadeGranite = "equip.grey";
    public const string CourtPaving = "ground.paver";
    public const string CourtPath = "ground.curb";
    public const string PlanterSoil = "base.warmBrown";

    /// <summary>Window cards (WLD-INTERIORS): offices for 8403, reading for the court blocks, corridor for the link.</summary>
    public const byte CardOffice = 5, CardCorridor = 3;
}

/// <summary>Role of one north block of the library (MAP-QSH-CORE "数据定位").</summary>
public enum AnnexKind
{
    /// <summary>8403: the 4F L-shaped north annex (offices, stacks), punched windows and a closed door on the street side.</summary>
    NorthAnnex,
    /// <summary>8406: the narrow 3F wing along the annex's inner face, ribbon windows over the courtyard.</summary>
    CourtWing,
    /// <summary>8408: the 2F low block; its roof is the library's 3F G terrace.</summary>
    LowBlock,
    /// <summary>8409: the 4F, 3 m north-west link to the annex's west end.</summary>
    Link,
    /// <summary>8410: the link's joint against the library's north-west tower.</summary>
    Joint,
}
