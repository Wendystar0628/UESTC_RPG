using Campus.Contracts;

namespace Campus.Gen.Maps.Qingshuihe.Core;

/// <summary>
/// Tunable values of the museum (16d-2, MAP-QSH-CORE; 8401 hall body and 8402 colonnade ring, one entity). Metres above
/// the museum floor (the pad + FloorAbovePad) unless noted; values marked "review" are mainline initial values awaiting
/// the main session's call. Colours are palette names only.
/// </summary>
public static class MuseumStyle
{
    // ---- Section: one tall gallery storey under a flat roof that runs out over the colonnade ring to the eave, the white
    //      central hall (序厅) rising from its middle under a low pyramid roof (review). ----
    /// <summary>Gallery storey: floor to the flat roof's top (BuildingFrame storeys). Not the data's (8401 two storeys 7.2 m, 8402 one storey 3.6 m): the gallery is one tall storey (D-239).</summary>
    public static readonly float[] MuseumStoreys = [5.4f];
    /// <summary>Colonnade ceiling, and the top of the granite gallery walls under it.</summary>
    public const float MuseumSoffit = 4.6f;
    /// <summary>The eave band rises this far above the flat roof (parapet), its coping on top.</summary>
    public const float MuseumUpstand = 0.45f;
    public const float MuseumFasciaThick = 0.4f;
    /// <summary>Central hall: plan size along and across the building's long axis (约 28×26 m, WLD-INTERIORS), eaves height.</summary>
    public const float MuseumHallLength = 28f, MuseumHallWidth = 26f, MuseumHallEaves = 11.4f;
    /// <summary>
    /// Low pyramid roof (低坡锥形屋顶, about 18°): rise from the eave to the small flat top, whose ring is the eave ring scaled by
    /// this. With the eaves at 11.4 m the hall and its roof show over the portico head from the south boardwalk.
    /// </summary>
    public const float MuseumRoofRise = 4.0f, MuseumApexScale = 0.12f;
    /// <summary>The hall's eave slab: out from the hall wall, and its thickness.</summary>
    public const float MuseumHallEaveOut = 0.7f, MuseumHallEaveThick = 0.35f;
    /// <summary>Base band of the hall walls where they rise from the flat roof.</summary>
    public const float MuseumHallBase = 0.35f;
    /// <summary>
    /// Every octagonal ring (walls, eave, platform) gets this chamfer at its corners: the octagon's ~47° folds would sit in
    /// the outline's critical band (ART-MODELING 第 6 条), two ~23° folds do not.
    /// </summary>
    public const float MuseumCornerChamfer = 0.35f;

    // ---- Colonnade ring (柱廊环, 8402): square columns along the platform edge carrying the eave band. ----
    /// <summary>Column centres in from the platform edge; the eave's outer face this far beyond the columns' outer faces.</summary>
    public const float ColumnInset = 0.9f, MuseumEaveOverhang = 0.3f;
    /// <summary>Square columns: shaft, base block and capital (review); bays at most this pitch.</summary>
    public const float ColumnSize = 0.6f, ColumnBase = 0.8f, ColumnBaseHeight = 0.3f, ColumnCap = 0.72f, ColumnCapHeight = 0.16f, ColumnBevel = 0.03f;
    public const float ColumnPitch = 4.8f;

    // ---- Portico (方柱门廊) on the south colonnade (MAP-QSH-CORE initial value: the boardwalk side): four larger
    //      columns and a raised head block (门头) over the bay, its face carrying a recessed name field (review). ----
    public const float PorticoColumn = 0.9f, PorticoColumnBase = 1.1f;
    /// <summary>Column centres either side of the door axis: inner pair, outer pair.</summary>
    public const float PorticoInner = 2.6f, PorticoOuter = 6.4f;
    /// <summary>Head block: half width, top above the floor, projection beyond the eave face.</summary>
    public const float PorticoHalf = 7.0f, PorticoTop = 7.2f, PorticoProject = 0.35f;
    /// <summary>Name field on the head's face (black calligraphy waits for the scene text atlas): margins and recess.</summary>
    public const float NameSideMargin = 0.7f, NameMargin = 0.45f, NameDepth = 0.05f;

    // ---- Granite gallery walls (带横槽的浅灰花岗岩外墙) ----
    /// <summary>Horizontal grooves (横向凹槽线脚): centre heights, height and depth.</summary>
    public static readonly float[] MuseumGrooves = [1.25f, 2.15f, 3.05f, 3.95f];
    public const float GrooveHeight = 0.07f, GrooveDepth = 0.035f;
    /// <summary>Main door bay under the portico: four 1.8 m cells to the bay head, the middle two the door up to its transom.</summary>
    public const int EntryCells = 4, EntryDoorCells = 2;
    public const float EntryCell = 1.8f, EntryHeight = 4.0f, EntryTransom = 3.0f;
    /// <summary>Lake front (朝湖立面): floor-to-head dark glass between two granite piers; the side door's cell, as a fraction from the north end.</summary>
    public const float LakePier = 1.2f, LakeModule = 1.75f, LakeDoorAt = 0.25f;
    /// <summary>Tall narrow slits on the other long granite faces (review).</summary>
    public const float MuseumSlitWidth = 0.7f, MuseumSlitSill = 0.9f, MuseumSlitPitch = 6f, MuseumSlitMargin = 4.5f;
    /// <summary>Clerestory ribbon round the hall (高窗): sill above the flat roof, height, module and end margin.</summary>
    public const float ClerestorySill = 1.4f, ClerestoryHeight = 3.2f, ClerestoryModule = 1.6f, ClerestoryMargin = 0.8f;
    /// <summary>Glass doors (WP-10): the main door four leaves, inner two open; the lake door two leaves, both open.</summary>
    public const int MuseumDoorLeaves = 4, LakeDoorLeaves = 2;

    // ---- Lights (灯走 interior.public): every museum window on the public-interior curve. ----
    public static readonly ChannelId MuseumWindowChannel = new("interior.public");
    /// <summary>Window cards: the gallery glass the lecture-hall card (原 168 报告厅), the clerestory the venue high window.</summary>
    public const byte CardLecture = 2, CardVenue = 13;

    // ---- Colours ----
    /// <summary>Light grey granite of the gallery walls: the zone's grey stone (see BandStone).</summary>
    public const string MuseumGranite = "equip.grey";
    /// <summary>White structure (白色体量): hall walls, columns, eave band, soffit, portico head.</summary>
    public const string MuseumWhite = "wall.greyWhite";
    /// <summary>Copings and sills, the library's light trim stone.</summary>
    public const string MuseumTrim = "wall";
    /// <summary>Dark granite: plinth, column bases, platform curb; grooves a mid grey.</summary>
    public const string MuseumDark = "roof.grey";
    /// <summary>The flat roof ring round the hall: a light grey, so the large roof seen from the library and from above stays clean.</summary>
    public const string MuseumFlatRoof = "ground.concrete";
    public const string MuseumGroove = "frame.alu";
    /// <summary>Dark frames for the dark glass (大面深色玻璃).</summary>
    public const string MuseumFrame = "roof.grey";
    /// <summary>The white low pyramid roof (白色坡顶, P16 looks over it).</summary>
    public const string MuseumRoof = "roof.silverMetal";
    /// <summary>Colonnade floor: light granite paving.</summary>
    public const string MuseumFloor = "ground.curb";
    /// <summary>Name field on the portico head: granite, lighter than the black calligraphy that will go on it.</summary>
    public const string MuseumNameField = "equip.grey";
    /// <summary>Width of the dark curb round the platform edge.</summary>
    public const float MuseumCurb = 0.45f;
}
