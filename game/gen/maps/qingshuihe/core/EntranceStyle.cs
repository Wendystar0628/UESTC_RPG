using Campus.Contracts;
using Campus.Gen.Maps.Qingshuihe.Core.Framework;

namespace Campus.Gen.Maps.Qingshuihe.Core;

/// <summary>
/// Tunable values of the library's grand stair (8398) and its 2F front door (16c, MAP-QSH-CORE, WLD-TERRAIN).
/// Metres; values marked "review" are mainline initial values awaiting the main session's call. Colours are palette
/// names only.
/// </summary>
public static class EntranceStyle
{
    // ---- Plan across the door axis (w = 0 on the front tower's door, symmetric): main stair, lawn terraces, side stairs,
    //      outer guard walls. The total, 2 × 13.78 m, is the OSM base's 27.7 m width (review); the world/core.json geometry of 8398
    //      (27.56 m) is drawn from these widths, so they stay here and the world record follows them (DECISION-239). ----
    /// <summary>Central grand stair, outer width including its cheek walls (two slot widths, review).</summary>
    public const float MainStairWidth = 9.0f;
    /// <summary>Lawn terrace (草坪台地) between the main stair and each side stair.</summary>
    public const float TerraceWidth = 4.78f;
    /// <summary>Side stair (侧梯) on each flank, outer width including cheeks (review).</summary>
    public const float SideStairWidth = 4.2f;
    /// <summary>Solid stone guard wall outside each side stair, its top at the railing part's height over the walking line (SYS-WALK).</summary>
    public const float GuardWallWidth = 0.3f;
    public static readonly float GuardHeight = new Campus.Contracts.Parts.RailingParams().Height;
    /// <summary>Top landing depth in front of the podium edge (the OSM base's 3.45 m back strip).</summary>
    public const float TopLandingDepth = 3.45f;

    // ---- Section (WLD-TERRAIN): risers near 0.15, flights of at most 1.5 m rise, one deep middle landing
    //      between the two runs (两跑) and short rest landings inside each run (review). ----
    public const float RestLanding = 1.2f, MiddleLanding = 2.8f;
    /// <summary>The stair rises from the axis paving: terrain plus <see cref="GroundLevels.Paving"/> at its foot.</summary>
    public const float StairFootLift = GroundLevels.Paving;

    // ---- Terraces: retaining walls with a coping, lawn tops, clipped hedge borders (the guard on every open edge). ----
    public const float TerraceCoping = 0.1f, TerraceCopingProject = 0.05f;
    /// <summary>Lawn top below the terrace coping (soil and turf sit a little inside the walls).</summary>
    public const float LawnDrop = 0.06f;
    /// <summary>Clipped hedges on the terraces, rows of soft cushions: width, height, set-in from the coping.</summary>
    public const float HedgeWidth = 0.8f, HedgeHeight = 0.72f, HedgeInset = 0.08f;
    /// <summary>Stone coursing on the guard walls' outer faces: string-course heights above the ground, thickness, projection.</summary>
    public static readonly float[] GuardCourses = [2.6f, 4.3f];
    public const float CourseThickness = 0.08f, CourseRelief = 0.025f;
    /// <summary>Plinth band (勒脚) at the foot of the lower terrace fronts and the guard walls, above the ground.</summary>
    public const float StairPlinth = 0.45f;

    // ---- Front door in the glass slot (MAP-QSH-CORE): four-leaf glass door over the slot's three middle cells,
    //      the outer two leaves fixed side lights, the inner two fixed open at 90° (WLD-INTERIORS). ----
    public const int DoorCells = 3;
    public const int DoorLeaves = 4;
    public const float DoorOpenDeg = 90f;
    /// <summary>Door frame depth on the slot's glass plane.</summary>
    public const float DoorDepth = 0.12f;
    /// <summary>Service doors (检修铁门, closed) in the two outer guard walls, into the plant rooms under the stair.</summary>
    public const float ServiceDoorWidth = 1.2f, ServiceDoorHeight = 2.2f, ServiceDoorS = 6.2f;

    // ---- Entrance porch frame over the 2F front door (门斗式外框, 16d, review): a flat canopy across the whole front tower
    //      face on two side piers standing on the terrace, its soffit a metre over the door transom. The light frame round
    //      a shaded porch is what reads as "the door" at mid range under the 25 m glass tower. ----
    public const float CanopyDepth = 2.8f, CanopyGap = 1.0f, CanopyThick = 0.7f, CanopyPier = 0.45f;
    /// <summary>Drip strip under the fascia's front edge (L0): height and projection.</summary>
    public const float CanopyDrip = 0.05f;
    /// <summary>The frame is the grey metal of the 5F eave (灰色金属), so it stands off the beige stone in shade and at noon.</summary>
    public const string CanopyFamily = LibraryStyle.LibraryEave;

    // ---- Colours ----
    /// <summary>Treads, landings and cheek walls: light granite, the podium terrace's paving.</summary>
    public const string StairStone = "ground.curb";
    /// <summary>Retaining and guard walls: the library's beige stone; copings the library trim.</summary>
    public const string StairWall = "stone.beige";
    public const string StairTrim = "wall";
    public const string TerraceLawn = "grass";
    /// <summary>The one dark purple-red band (红花檵木) on the lower terrace's front edge; the other borders box hedge.</summary>
    public const string LoropetalumBand = "loropetalum";
    public const string BoxHedge = "hedge";

    public static Paint Lawn(string family) => new(CoreMaterial.Lawn, family);
    public static Paint Foliage(string family) => new(CoreMaterial.Foliage, family);
}
