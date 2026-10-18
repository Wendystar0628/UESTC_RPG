using Campus.Contracts;
using Campus.Gen.Zones.Core.Framework;

namespace Campus.Gen.Zones.Core;

/// <summary>
/// Tunable values of the library shell (16b, MAP-QSH-CORE, MAP-QSH-LIBRARY). Metres and degrees; values marked
/// "review" are mainline initial values awaiting the main session's call. Colours are palette names only.
/// </summary>
public static partial class CoreStyle
{
    // ---- Storeys and heights. The 1F floor is the pad + FloorAbovePad, and 1F is 4.95 m so the 2F floor lands on
    //      pad + 5.1 like the stair top and interiors.json's lib.main door (MAP-QSH-LIBRARY: 5F 18.6, roof 23.1).
    //      Not read from the data (D-239): its ring height is 22.5 m and podium 4.5 m, 0.6 m under this section. ----
    public static readonly float[] LibraryStoreys = [4.95f, 4.5f, 4.5f, 4.5f, 4.5f];
    /// <summary>
    /// The front square tower (SE, 正面方塔) has one storey more than the ring (约 6 层, MAP-QSH-CORE); the data's 6 and 27 m
    /// stay unread for the same reason as the table (this tower tops out 0.6 m above its 27 m).
    /// </summary>
    public const int FrontTowerStoreys = 6;
    /// <summary>The other three corner blocks rise this far above the ring roof (stair overrun, review).</summary>
    public const float TowerRise = 1.6f;
    /// <summary>5F set back from the wing faces (顶层退台一圈, review); the strip in front is the 5F terrace.</summary>
    public const float TopSetback = 2.4f;

    // ---- Parapets, copings, bands ----
    public const float ParapetHeight = 1.1f, ParapetThick = 0.3f;
    public const float TowerCrown = 0.75f;
    public const float CopingHeight = 0.14f, CopingOverhang = 0.06f, CopingBevel = 0.025f;
    /// <summary>String course at each floor line of the ring: a shallow stone band (横向线脚).</summary>
    public const float CourseHeight = 0.22f, CourseProject = 0.06f;
    /// <summary>Plinth band (勒脚) proud of the wall, up to the level top line PadY + PlinthTop (WLD-TERRAIN).</summary>
    public const float PlinthProject = 0.05f;

    // ---- Openings ----
    /// <summary>Reveal depth from the wall face to the glass plane; frame and mullion faces; sill.</summary>
    public const float Reveal = 0.28f, FrameFace = 0.07f, FrameDepth = 0.05f, MullionFace = 0.06f;
    public const float SillProject = 0.06f, SillHeight = 0.1f;
    /// <summary>Ring storeys 2F–4F: one long thin ribbon per storey on every non-tower face (横窗, MAP-QSH-CORE).</summary>
    public const float RibbonSill = 0.95f, RibbonHeight = 1.75f, RibbonModule = 1.5f, RibbonMargin = 1.3f;
    /// <summary>Podium 1F: taller ribbon with a transom.</summary>
    public const float PodiumSill = 0.7f, PodiumHeight = 3.0f, PodiumModule = 1.8f, PodiumTransom = 2.25f;
    /// <summary>Set-back 5F: a row of square windows under the eave (一排方形小窗).</summary>
    public const float SquareSill = 1.25f, SquareSize = 1.15f, SquarePitch = 2.3f;
    /// <summary>Corner towers: full-height narrow vertical slits on the outer face (转角面竖缝窗, review count 7).</summary>
    public const int TowerSlits = 7;
    public const float SlitWidth = 0.45f, SlitPitch = 1.9f, SlitTopGap = 1.6f;
    /// <summary>Front tower: the glass slot (玻璃槽), recessed, with four mullions; door bay at 2F.</summary>
    public const float SlotWidth = 4.8f, SlotDepth = 0.75f, SlotTopGap = 1.4f, SlotDoorHeight = 3.1f;
    public const int SlotMullions = 4;
    /// <summary>
    /// 1F cafe door (lib.cafe, review): two podium modules of the podium face beside the 品·悦吧 store front, open from the
    /// floor up to the ribbon's transom line, a pier between it and the rest of the ribbon.
    /// </summary>
    public const int CafeDoorCells = 2;
    public const float CafeDoorPier = 0.9f;
    /// <summary>Granite apron at the cafe door, level with its threshold: depth out from the face, margin past each jamb.</summary>
    public const float CafeApronDepth = 2.4f, CafeApronMargin = 0.6f;
    /// <summary>
    /// Paved link from the cafe apron to the nearest footway (16d-2): width, reach, how far it starts inside the apron and runs
    /// onto the walk, the walk width when the network has none; pavers and edging band as WP-15 lays a footway (review).
    /// </summary>
    public const float CafeWalkWidth = 1.8f, CafeWalkReach = 15f, CafeWalkInset = 0.3f, CafeWalkOverlap = 0.05f, CafeWalkFallbackWidth = 2f;
    public const float CafeWalkEdging = 0.12f;
    public const string CafeWalkPaving = "ground.paver", CafeWalkEdge = "ground.curb";
    /// <summary>Faces shorter than this get no openings (tower returns, short notches).</summary>
    public const float MinWindowFace = 3.2f;

    // ---- 5F eave: a canted sheet-metal band leaning back over the set-back wall (灰色金属斜檐, review) ----
    public const float EaveOverhang = 0.9f, EaveDrop = 0.55f, EaveRun = 1.7f, EaveRise = 0.95f, EaveThick = 0.18f;

    // ---- Roof ----
    /// <summary>Skylight across flats (采光顶对边约 34–35 m, WLD-INTERIORS) and its low cone (rise / circumradius, libraryRoof.ts).</summary>
    public const float SkylightAcross = 34.5f, SkylightRiseRatio = 0.15f, SkylightCurb = 0.8f, SkylightCurbWidth = 0.6f;
    public const float SkylightRingRadius = 1.4f;
    public const float RibWidth = 0.32f, RibDepth = 0.34f, GridBar = 0.09f, GridDepth = 0.12f, EdgeBeam = 0.26f;
    /// <summary>Rows of the triangular steel grid between two ribs, per level.</summary>
    public static int SkylightRows(LodLevel lod) => lod switch
    {
        LodLevel.L0 => 6,
        LodLevel.L1 => 4,
        LodLevel.L2 => 2,
        LodLevel.L3 or LodLevel.L4 => 0,
        _ => throw new ArgumentOutOfRangeException(nameof(lod), lod, "Unknown LOD level."),
    };
    /// <summary>Curved metal roofs alternating with the flat ring roof (浅灰弧形金属小坡顶): radial barrels at the face centres.</summary>
    public const float VaultLength = 15f, VaultWidth = 6.6f, VaultRise = 1.9f, VaultCurb = 0.35f;
    /// <summary>Two fenced plant enclosures on the ring roof (review): centre radius, size, fence height.</summary>
    public const float PlantRadius = 29f, PlantLength = 7f, PlantDepth = 4.5f, PlantFence = 1.8f;
    /// <summary>Raised mechanical box north of the skylight (两排约 12 扇小窗, W-MECH).</summary>
    public const float MechLength = 19f, MechDepth = 7.5f, MechHeight = 4.4f;
    public const int MechWindows = 12;
    public const float MechWindow = 0.85f, MechRow0 = 0.9f, MechRow1 = 2.55f;

    // ---- Window lights (VF-B) ----
    /// <summary>Light channel of every library window; the base comes from the map's allocation (LibraryShell.WindowKey).</summary>
    public static readonly ChannelId LibraryWindowChannel = new("window.library");
    /// <summary>Window cards (WLD-INTERIORS): reading rooms use the classroom card, tower slits the stair card.</summary>
    public const byte CardReading = 1, CardStair = 4, CardMech = 15;

    // ---- Colours ----
    /// <summary>Beige stone of the ring and towers (米色石材).</summary>
    public const string LibraryWall = "stone.beige";
    /// <summary>Copings, string courses, sills, plinth band: the lighter warm stone.</summary>
    public const string LibraryTrim = "wall";
    /// <summary>Dark grey flat roofs (深灰平屋面) and terraces.</summary>
    public const string LibraryRoof = "roof.grey";
    /// <summary>Podium roof: the 2F terrace and stair landing, light granite paving.</summary>
    public const string LibraryTerrace = "ground.curb";
    /// <summary>Light grey curved metal roofs (浅灰弧形金属).</summary>
    public const string LibraryVault = "roof.silverMetal";
    /// <summary>Grey metal eave (灰色金属斜檐) and window frames.</summary>
    public const string LibraryEave = "frame.alu";
    public const string LibraryFrame = "frame.alu";
    /// <summary>Skylight ribs and grid: light steel so the eight-petal grid reads against the dark roof.</summary>
    public const string SkylightSteel = "wall.greyWhite";

    private static string BuildingFamily(CoreMaterial material) => material switch
    {
        CoreMaterial.Wall => LibraryWall,
        CoreMaterial.Trim => LibraryTrim,
        CoreMaterial.Roof => LibraryRoof,
        CoreMaterial.Sheet => LibraryVault,
        _ => throw new ArgumentOutOfRangeException(nameof(material), material, "Not a building material."),
    };

    private static ToonSpec BuildingSpec(CoreMaterial material, ToonSpec spec) => material switch
    {
        CoreMaterial.Wall => spec with { Fx = ToonFx.BevelLift },
        CoreMaterial.Trim => spec with { Fx = ToonFx.BevelLift, Line = LineClass.Fine },
        CoreMaterial.Roof => spec with { Fx = ToonFx.Mottle, Wet = 0.85f },
        CoreMaterial.Sheet => spec with { Fx = ToonFx.Rim | ToonFx.AnisoBand, Wet = 0.5f },
        _ => throw new ArgumentOutOfRangeException(nameof(material), material, "Not a building material."),
    };

    public static Paint Wall(string family) => new(CoreMaterial.Wall, family);
    public static Paint Trim(string family) => new(CoreMaterial.Trim, family);
    public static Paint Roof(string family) => new(CoreMaterial.Roof, family);
    public static Paint Sheet(string family) => new(CoreMaterial.Sheet, family);

    /// <summary>Window glass (toon_glass with window cards); casts shadow, it closes the hollow shell.</summary>
    public static ToonSpec WindowGlass => Campus.Gen.Zones.Common.ZoneLooks.Glass;
    /// <summary>See-through glass of the skylight and the front tower slot (paths P1 and P2, toon_glass_near).</summary>
    public static ToonSpec ClearGlass { get; } = WindowGlass with { Kind = ToonKind.GlassNear };
}
