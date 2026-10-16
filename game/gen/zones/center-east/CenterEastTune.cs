using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.Zones.CenterEast;

/// <summary>
/// Every tunable value of the center-east zone in one place, for review (ENG-TEAM §0.1): materials as colour names, and
/// the dimensions of each site. Coordinates are world (x, z) in metres from the OSM footprints in campus.json; local frames
/// are described per block. Values marked "初值" are starting values awaiting the main session's call. 21b–21d add their
/// own nested classes after <see cref="Pinxue"/>.
/// </summary>
public static class CenterEastTune
{
    public const int BudgetTrianglesL0 = 60_000;
    public const int BudgetTrianglesL1 = 30_000;
    public const int BudgetDrawCalls = 16;

    /// <summary>Materials (ART-STYLE §8 colour names only).</summary>
    public static class Look
    {
        private static ToonSpec S(string family, ToonFx fx = ToonFx.None, LineClass line = LineClass.Building, float wet = 0.6f) =>
            new(new(family + ".lit"), new(family + ".shade")) { Fx = fx, Line = line, Wet = wet };

        /// <summary>Renovated teaching-block frame and walls: grey-white (MAP-QSH-CENTER-EAST §2.8).</summary>
        public static readonly ToonSpec Wall = S("wall.greyWhite", ToonFx.BevelLift);
        /// <summary>Copings, bands, plaque frames: light beige stone, fine lines so trims do not hatch the facade.</summary>
        public static readonly ToonSpec Trim = S("stone.beige", ToonFx.BevelLift, LineClass.Fine);
        /// <summary>Spandrel stone and the inscription plaque ground (浅米灰石材).</summary>
        public static readonly ToonSpec Stone = S("stone.beige", ToonFx.BevelLift);
        /// <summary>Tree-pit kerbs, seat walls, bed kerbs: light granite.</summary>
        public static readonly ToonSpec Granite = S("stone.beige", ToonFx.BevelLift, LineClass.Fine);
        /// <summary>Plinths and the cascade fountain: dark grey granite (深灰勒脚、深色花岗岩).</summary>
        public static readonly ToonSpec DarkGranite = S("roof.grey", ToonFx.BevelLift, LineClass.Fine, 0.8f);
        public static readonly ToonSpec Paving = S("ground.paver", ToonFx.None, LineClass.None);
        /// <summary>Tree-pit and planter soil: the horizontal face that catches leaves, petals and snow (MAP-QSH-ADDITIONS R3).</summary>
        public static readonly ToonSpec Soil = S("base.warmBrown", ToonFx.None, LineClass.None);
        /// <summary>Planter beds behind the ramp seat walls: a low planted surface until WP-23 fills them with shrubs.</summary>
        public static readonly ToonSpec Bed = S("ground.grassDark", ToonFx.None, LineClass.None);
        public static readonly ToonSpec RoofSlab = S("roof.grey", ToonFx.Mottle, LineClass.Building, 0.85f);
        /// <summary>Grey aluminium window frames and mullions (窗框灰铝).</summary>
        public static readonly ToonSpec Metal = S("frame.alu", ToonFx.BevelLift, LineClass.Fine);
        public static readonly ToonSpec Glass = new(new("glass.lit"), new("glass.shade"))
        { Kind = ToonKind.Glass, Fx = ToonFx.GlassStreak | ToonFx.Glare, Wet = 0 };
    }

    /// <summary>
    /// 品学楼 (MAP-QSH-CENTER-EAST §2). The A, B and C wings keep WP-14's facade rules on the +1.5 m platform
    /// (center-east.terrain.json); this zone builds the gate frame, the two link bridges, the forecourt, the fountain's
    /// stone tiers and the curved end bay.
    /// </summary>
    public static class Pinxue
    {
        /// <summary>The 品学楼 entity: its parts are the gate frame bar and the A–C, B–C link bridges.</summary>
        public const string Complex = "qsh:relation:11043355";
        public const string WingA = "qsh:relation:11043352";
        public const string WingB = "qsh:relation:11043353";
        public const string FrameBar = "osm:way:797290174";
        public const string BridgeAC = "osm:way:797290183";
        public const string BridgeBC = "osm:way:797290182";
        /// <summary>The forecourt (门框前广场) and its holes: the lawn panel round the fountain, the round bed on the ramp.</summary>
        public const string Forecourt = "qsh:source:relation:11043356";
        /// <summary>Design level of the platform (MAP-QSH-TERRAIN §10): only for probe eye heights, which have no terrain to read.</summary>
        public const float PlatformY = 1.5f;

        /// <summary>
        /// Gate frame (§2.2). Frame: origin at the bar's north-west corner on A's south wall, X south along the bar to B's
        /// north wall (37.7 m), local +Z west out to the forecourt; the bar is 6.9 m deep (z −6.9…0).
        /// </summary>
        public static readonly Vector2 FrameNorthWest = new(161.82f, -79.33f);
        public static readonly Vector2 FrameSouthWest = new(167.16f, -41.98f);
        public const float FrameDepth = 6.9f;
        /// <summary>A and B: 4 storeys, 16 m. The open passage is storeys 1–2, the beam bridge storeys 3–4.</summary>
        public const float StoreyHeight = 4.0f;
        public const float BeamBottom = 8.0f, BeamTop = 16.0f;
        /// <summary>Four square columns, one row on the bar's centre line, three bays with the middle one widest (四柱三间, 初值).</summary>
        public const float ColumnSize = 2.4f, ColumnPlinth = 0.45f;
        public static readonly float[] ColumnX = [2.6f, 13.0f, 24.72f, 35.12f];
        /// <summary>Inscription plaque on the beam's forecourt face; the calligraphy waits for the scene text atlas (初值).</summary>
        public const float PlaqueWidth = 26f, PlaqueHeight = 3.0f, PlaqueProud = 0.08f;
        /// <summary>Soffit downlights over the passage (lit by WP-39 with the frame's light channel).</summary>
        public const float DownlightPitch = 3.2f, DownlightRadius = 0.16f;

        /// <summary>Link bridges A–C and B–C (§2.2): storeys 2–3 over the paths between the wings (初值).</summary>
        public const float BridgeBottom = 4.0f, BridgeTop = 12.0f, BridgeColumnRadius = 0.3f;

        /// <summary>
        /// Cascade fountain stone (§2.3; water by WP-24, WLD-WATER §9): three dark granite tiers on the frame's axis,
        /// centred in the forecourt's rectangular lawn panel 18 m in front of the frame. Heights are above the forecourt.
        /// </summary>
        public static readonly Vector2 Fountain = new(142.38f, -58.10f);
        public const float BasinRadius = 2.85f, BasinWall = 0.35f, BasinRim = 0.55f, BasinWater = 0.45f;
        public const float MiddleRadius = 1.75f, MiddleRim = 1.42f, MiddleWater = 1.36f;
        public const float TopRadius = 0.9f, TopRim = 2.12f, TopWater = 2.06f, SpoutTop = 2.4f;
        public const float PanelKerbWidth = 0.3f, PanelKerbHeight = 0.15f;

        /// <summary>Square tree pits (方形树池) in two rows either side of the axis, in the frame's local (x, z).</summary>
        public static readonly float[] TreePitRowsX = [4.0f, 33.8f];
        public static readonly float[] TreePitZ = [9f, 15f, 21f, 27f, 33f];
        public const float TreePitSize = 1.8f, TreePitKerb = 0.22f, TreePitHeight = 0.42f, TreePitSoil = 0.32f;

        /// <summary>Round bed on the ramp (the forecourt's circular hole): a 0.45 m granite seat ring round one tree.</summary>
        public static readonly Vector2 RoundBed = new(101.68f, -53.13f);
        public const float RoundBedRadius = 3.45f, SeatWidth = 0.45f, SeatHeight = 0.45f;

        /// <summary>
        /// Ramp seat walls (MAP-QSH-TERRAIN §10.2): 0.45 m planter walls on both long sides of the 5 % ramp plaza,
        /// frame-local x of each wall's plaza face, from z0 (top of the ramp) to z1 (foot), stepping per segment.
        /// </summary>
        public static readonly float[] RampWallX = [3.6f, 33.7f];
        public const float RampWallZ0 = 47f, RampWallZ1 = 77f, RampWallSegment = 6f;
        public const float RampWallThickness = 0.5f, RampBedWidth = 1.2f, RampBedKerb = 0.2f;

        /// <summary>
        /// Curved end of the right wing (§2.4: 右翼端部是一个弧形体量), read from the forecourt as the far right: a curved
        /// curtain-wall bay over B's diagonal south-west lobby (osm:way:797290169), bulging toward the river (初值).
        /// </summary>
        public static readonly Vector2 ArcFrom = new(156.26f, 29.09f);
        public static readonly Vector2 ArcTo = new(168.75f, 38.45f);
        public static readonly Vector2 ArcBulge = new(-0.6f, 0.8f);
        public const float ArcSagitta = 3.5f, ArcHeight = 19.5f;
        public const int ArcStoreys = 5;

        /// <summary>A-wing hall entrance px.a.hall (interiors.json): door centre and the diagonal facade it sits in.</summary>
        public static readonly Vector2 HallDoor = new(146.5f, -84.9f);
        public static readonly Vector2 HallFacadeFrom = new(140.64f, -89.21f);
        public static readonly Vector2 HallFacadeTo = new(152.44f, -80.5f);
        /// <summary>Side door into A under the passage (WLD-INTERIORS §2.12), on A's wall at the bar's centre line (初值).</summary>
        public const float SideDoorZ = -3.45f;
    }
}
