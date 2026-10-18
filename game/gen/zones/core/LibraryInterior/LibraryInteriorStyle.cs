using Campus.Contracts;
using Campus.Gen.Zones.Core.Framework;
using ZoneLooks = Campus.Gen.Zones.Common.ZoneLooks;

namespace Campus.Gen.Zones.Core.LibraryInterior;

/// <summary>
/// Tunable values of the library interior (WP-17, MAP-QSH-LIBRARY). Metres; "review" marks mainline initial values awaiting
/// the main session's call. Storey heights, the skylight, the 5F set-back and the window holes come from the shell
/// (<see cref="Library.LibraryPlan"/>, <see cref="Library.LibraryShell"/>), never from here.
/// Colours are palette names only. No interior colours are registered yet (WLD-INTERIORS-BUILD, WP-04), so each one
/// borrows the nearest registered entry (review).
/// </summary>
internal static class LibraryInteriorStyle
{
    // ---- Atrium and gallery rings (MAP-QSH-LIBRARY 中庭与回廊). The ring outside the topmost gallery is the skylight edge,
    //      so the void is the skylight across flats minus one ring on each side. ----
    public const float GalleryWidth = 4f;
    /// <summary>Corner cut along each edge of the void and the ring seals: splits the octagon's 45° folds (ART-MODELING).</summary>
    public const float OctagonChamfer = 0.3f;
    public const float SlabThick = 0.25f;
    /// <summary>Ceilings stop this far under the shell's roof planes so the two never share a plane.</summary>
    public const float CeilingGap = 0.02f;
    /// <summary>Beige ring beam round the skylight opening (外圈厚重的米白八角环梁): width and drop below the ceiling.</summary>
    public const float RingBeamWidth = 0.6f, RingBeamDrop = 1.0f;

    /// <summary>Atrium balustrade: 1.2 m visual (临开敞中庭), white curb and cap, red-brown wood infill; collision 1.5 m.</summary>
    public const float RailHeight = 1.2f, RailCap = 0.07f, RailCapWidth = 0.16f, RailCurb = 0.15f, RailThick = 0.12f, RailInfill = 0.05f;

    /// <summary>Square columns just behind the void corners, under the galleries (review).</summary>
    public const float ColumnSize = 0.8f, ColumnSetback = 0.25f;

    /// <summary>Inner lining of the shell's exterior walls: from this far behind the wall face to this far (the glass sits at 0.28).</summary>
    public const float LiningOut = 0.3f, LiningIn = 0.36f;

    /// <summary>Bookshelf seals (书架墙): shelf height, depth, unit width, boards, and the wood-faced wall behind them.</summary>
    public const float ShelfHeight = 2.3f, ShelfDepth = 0.4f, ShelfUnit = 0.9f, ShelfPanel = 0.03f, SealBack = 0.1f;
    public const int ShelfBoards = 5;

    /// <summary>1F openings in the hall edge: the four corridor mouths and the A/D sector fronts (前 6–8 m).</summary>
    public const float CorridorWidth = 4.8f, CorridorMouth = 3f, SectorDepth = 7f, SectorInset = 0.5f;

    /// <summary>
    /// The 1F↔2F twin stair (八字楼梯): two flights per side with a landing, 0.28 m treads; the riser is the 1F height over
    /// 2 × <see cref="FlightRisers"/>. Width, waist under the open upper flight, stringer and handrail (review).
    /// </summary>
    public const int FlightRisers = 15;
    public const float Tread = 0.28f, StairWidth = 2.2f, StairLanding = 1.5f, Waist = 0.2f, Stringer = 0.12f, StairRail = 1.1f;
    /// <summary>Faces (relative to the front) the two flights rise toward: W (3) and N (5), the far side of the hall.</summary>
    public static readonly int[] StairFaces = [3, 5];

    /// <summary>5F west aisle (西走廊) and the reading band along the west windows (P16).</summary>
    public const float AisleWidth = 3f, BandDepth = 6f, BandEndMargin = 1.2f;

    /// <summary>Star ceiling of the galleries (星空顶棚): downlight pitch and dot size.</summary>
    public const float StarPitch = 1.2f, StarSize = 0.07f;

    // ---- Colours (borrowed registered entries, review) ----
    /// <summary>Light grey polished stone of the atrium floor and the galleries.</summary>
    public const string Floor = "wall.greyWhite";
    /// <summary>Black strips in the atrium floor, parallel to the entrance axis.</summary>
    public const string FloorStrip = "stone.black";
    public const string Carpet = "roof.grey";
    /// <summary>Dark grey gallery ceilings (星空顶棚).</summary>
    public const string StarCeiling = "stone.black";
    public const string CeilingWhite = "wall";
    public const string WallWhite = "wall";
    public const string RingBeam = "stone.beige";
    /// <summary>Red-brown wood: shelves, wood-faced walls, balustrade infill.</summary>
    public const string Wood = "door.wood";
    public const string LightWood = "panel.brown";
    public const string DarkWood = "glass.tea";
    /// <summary>The red reading table of the visual focus: deep red-brown wood (红桌，可改深红褐木).</summary>
    public const string RedTable = "band.ochre";
    public const string ColumnBlue = "uestc.blue";
    public const string CircuitBlue = "uestc.techblue";
    public const string Gold = "uestc.gold";
    public const string ChairOrange = "uestc.gold";
    public const string ChairPeacock = "street.blue";
    public const string Sofa = "wall";
    public const string BarrelLime = "panel.yellowGreen";
    public const string LampGreen = "panel.yellowGreen";
    public const string Steel = "roof.silverMetal";
    public const string Frame = "frame.alu";
    public const string Paper = "wall";
    public const string BoardBlue = "uestc.techblue";
    public const string DoorGrey = "equip.grey";
    /// <summary>Self-lit downlights, lantern shades and lamp pools on the library light channel.</summary>
    public const string LampGlow = "wall.dormYellow";
    public static readonly string[] Books =
        ["band.terracotta", "street.blue", "curtain.cream", "panel.paleGold", "glass.deep", "band.ochre", "panel.yellowGreen", "wall.greyWhite", "uestc.techblue"];
    public static readonly string[] Exhibits = ["panel.paleGold", "band.terracotta", "glass.deep", "curtain.cream"];

    /// <summary>The library's light channel (window.library): low by day, full after sunset, off after 22:30.</summary>
    public static ChannelId LightChannel => CoreStyle.LibraryWindowChannel;

    /// <summary>VF-A interior bit (GeometryBundle.Flags): interior shade rows, no rain wetting.</summary>
    public const byte InteriorFlag = 1;

    public static Paint Wall(string family) => new(CoreMaterial.Wall, family, InteriorFlag);
    public static Paint Trim(string family) => new(CoreMaterial.Trim, family, InteriorFlag);
    public static Paint Metal(string family) => new(CoreMaterial.Metal, family, InteriorFlag);
    public static Paint Stone(string family) => new(CoreMaterial.Stone, family, InteriorFlag);
    public static Paint Inlay(string family) => new(CoreMaterial.Inlay, family, (byte)(InteriorFlag | VertexFlags.Decal | VertexFlags.NoShadow));
    public static ToonSpec Glow(string family) => ZoneLooks.S(family, line: LineClass.Fine, wet: 0f) with { Emissive = LightChannel };
}
