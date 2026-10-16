using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.Zones.North;

/// <summary>
/// Every tunable value of the north zone in one place, for review (ENG-TEAM §0.1): materials as colour names, the cluster
/// table and the dimensions of each site. Coordinates are world (x, z) in metres from campus.json; local frames are
/// described per block. Values marked "初值" are starting values awaiting the main session's call.
/// </summary>
public static class NorthTune
{
    public const int BudgetTrianglesL0 = 24_000;
    public const int BudgetTrianglesL1 = 24_000;
    public const int BudgetDrawCalls = 14;

    /// <summary>Materials (ART-STYLE §8 colour names only).</summary>
    public static class Look
    {
        private static ToonSpec S(string family, ToonFx fx = ToonFx.None, LineClass line = LineClass.Building, float wet = 0.6f) =>
            new(new(family + ".lit"), new(family + ".shade")) { Fx = fx, Line = line, Wet = wet };

        /// <summary>Guard house walls: the renovated dormitory warm yellow (N-22: "值班室按 N-02 的预设做新").</summary>
        public static readonly ToonSpec HouseWall = S("wall.dormYellow", ToonFx.BevelLift);
        /// <summary>Copings, sills, door surrounds, canopy fascias.</summary>
        public static readonly ToonSpec Trim = S("wall.greyWhite", ToonFx.BevelLift, LineClass.Fine);
        public static readonly ToonSpec Plinth = S("base.warmBrown", ToonFx.BevelLift);
        public static readonly ToonSpec RoofSlab = S("roof.grey", ToonFx.Mottle, LineClass.Building, 0.85f);
        /// <summary>Thresholds, islands, the hall landing and its steps: warm granite.</summary>
        public static readonly ToonSpec Granite = S("stone.beige", ToonFx.BevelLift);
        /// <summary>Fence posts and rails, barrier arms: deep grey steel (MAP-QSH-NORTH §4.7, §8). 初值：借 WP-12 的围挡深灰。</summary>
        public static readonly ToonSpec Steel = S("equip.fence", ToonFx.BevelLift, LineClass.Fine, 0.4f);
        /// <summary>Fence bars: same colour, no outline, so the 25 mm bars read as a veil, not hatching (ART-AMBIENCE §0.1).</summary>
        public static readonly ToonSpec SteelBar = S("equip.fence", ToonFx.None, LineClass.None, 0.4f);
        /// <summary>Turnstile cabinets, barrier cabinets, scanner stalks: brushed stainless.</summary>
        public static readonly ToonSpec Stainless = S("frame.alu", ToonFx.BevelLift, LineClass.Fine, 0.4f);
        public static readonly ToonSpec Glass = new(new("glass.lit"), new("glass.shade"))
        { Kind = ToonKind.Glass, Fx = ToonFx.GlassStreak | ToonFx.Glare, Wet = 0 };
        /// <summary>Clipped hedge inside the fence: the seasonal hedge colour, soft vegetation shading.</summary>
        public static readonly ToonSpec Hedge = new(new("hedge.lit"), new("hedge.shade"))
        { Normal = ToonNormal.Smooth, Steps = ShadeSteps.Soft, Line = LineClass.Tree };
        /// <summary>
        /// Open-lane arrow. 借用：`ground.turf`（WP-12 出口牌同样借它），`sign.green` 是发光色名、没有受光/影色对；
        /// WP-04 的发光着色与 `sign.exitGreen` 到位后换。
        /// </summary>
        public static readonly ToonSpec ArrowGreen = S("ground.turf", ToonFx.None, LineClass.None, 0.2f);
        /// <summary>Closed-lane cross. 借用：`maple`；WP-12 的 `equip.fireRed`（全校的红）进 main 后换。</summary>
        public static readonly ToonSpec CrossRed = S("maple", ToonFx.None, LineClass.None, 0.2f);
        public static readonly ToonSpec Wood = S("door.wood", ToonFx.BevelLift, LineClass.Fine);
    }

    /// <summary>One dormitory cluster of this sub-ticket (MAP-QSH-NORTH 数据定位 · 宿舍组团; MAP-QSH-RENOVATION N-22).</summary>
    /// <param name="Id">Cluster entity (its OSM polygon is the fence line).</param>
    /// <param name="Key">Short key for site IDs and overlay IDs.</param>
    /// <param name="MainGate">Near the road crossing that gets the guard house; the nearest crossing within 15 m wins.</param>
    /// <param name="Open">Walk mode: the main gate's wings retracted, arrows green (WLD-INTERIORS §2.8, §4).</param>
    /// <param name="Facade">Wall preset handed to WP-14 through the overlay's StyleOp (MAP-QSH-RENOVATION §2.1, §11 初值).</param>
    public sealed record Cluster(string Id, string Key, string Name, Vector2 MainGate, bool Open, string Facade, string[] Buildings);

    /// <summary>
    /// 20a clusters: 学知苑 1–8 and the international students' cluster. Wall presets (§11 初值): 二组团 A 型暖米黄（原型），
    /// 六组团 B 型（原型），其余在暖米黄与浅奶咖之间分配，相邻组团尽量不同色；竖带一律陶红。20b appends 硕丰、博翰.
    /// </summary>
    public static readonly Cluster[] Clusters =
    [
        new("qsh:way:596904641", "xz1", "学知一组团", new(107.8f, -466.0f), true, Facades.ACreamCoffee,
            ["qsh:way:217855218", "qsh:way:217855220", "qsh:way:217855221"]),
        new("qsh:way:603368217", "xz2", "学知二组团", new(184.3f, -407.9f), false, Facades.ADormYellow,
            ["qsh:way:217855223", "qsh:way:217855224", "qsh:way:217855225", "qsh:way:217855226"]),
        new("qsh:way:596904652", "xz3", "学知三组团", new(364.2f, -424.1f), false, Facades.ADormYellow,
            ["qsh:way:217855227", "qsh:way:217855228", "qsh:way:217855207", "qsh:way:217855208"]),
        new("qsh:way:596904653", "xz4", "学知四组团", new(365.8f, -417.5f), false, Facades.ACreamCoffee,
            ["qsh:way:217855209", "qsh:way:217855210", "qsh:way:217855211", "qsh:way:217855212"]),
        new("qsh:way:596904654", "xz5", "学知五组团", new(447.4f, -348.2f), false, Facades.ADormYellow,
            ["qsh:way:217855213", "qsh:way:217855214", "qsh:way:217855215", "qsh:way:217855216"]),
        new("qsh:way:596904646", "xz6", "学知六组团", new(67.4f, -601.4f), false, Facades.B,
            ["qsh:way:217855219", "qsh:way:217855217", "qsh:way:217855222"]),
        new("qsh:way:596904651", "xz7", "学知七组团", new(198.8f, -613.9f), false, Facades.ADormYellow,
            ["qsh:way:544160373", "qsh:way:544136508", "qsh:way:544136509"]),
        new("qsh:way:596904662", "xz8", "学知八组团", new(281.6f, -609.8f), false, Facades.ACreamCoffee,
            ["qsh:way:544160372", "qsh:way:449705142", "qsh:way:544136507"]),
        new("qsh:way:848026371", "intl", "留学生组团", new(-47.0f, -805.1f), false, Facades.New, []),
    ];

    /// <summary>Facade preset keys written into north.overlay.json StyleOps (family "dormitory"); WP-14 reads them once it honours StyleOp.Facade.</summary>
    public static class Facades
    {
        /// <summary>本科 A 型：宿舍暖米黄墙配陶红竖向墙带（MAP-QSH-NORTH §4.2）。</summary>
        public const string ADormYellow = "dorm.a.dormYellow";
        /// <summary>本科 A 型的另一组团色：浅奶咖墙配陶红竖向墙带（§4.5）。</summary>
        public const string ACreamCoffee = "dorm.a.creamCoffee";
        /// <summary>本科 B 型：米白竖横框、灰粉或浅砂窗间板、浅粉砂阳台栏板（§4.3）。</summary>
        public const string B = "dorm.b";
        /// <summary>2015 年以后的新楼，按区文档新楼外观（MAP-QSH-RENOVATION §9），不写 StyleOp。</summary>
        public const string New = "";
    }

    /// <summary>Cluster fence (MAP-QSH-NORTH §4.7, §8): 2.2 m see-through steel bars, a 1.2 m clipped hedge inside.</summary>
    public static class Fence
    {
        /// <summary>The fence runs this far inside the cluster polygon (OSM traces the kerb), more where a road runs alongside.</summary>
        public const float Inset = 1.5f, MaxInset = 6f, RoadClearance = 0.8f;
        /// <summary>Maximum post pitch; the polygon is resampled at this spacing.</summary>
        public const float PostPitch = 2.4f;
        public const float Height = 2.2f, PostSize = 0.08f, RailSize = 0.05f, BarSize = 0.025f, BarPitch = 0.14f;
        public const float BottomRail = 0.12f, TopRail = 2.04f;
        public const float HedgeOffset = 0.75f, HedgeHeight = 1.2f, HedgeWidth = 0.8f, HedgeBevel = 0.1f;
        /// <summary>Road width when the network has none (service road 5 m in this zone's data).</summary>
        public const float DefaultRoadWidth = 4f;
    }

    /// <summary>
    /// Cluster gate at a road crossing (MAP-QSH-NORTH §4.7, MAP-QSH-RENOVATION N-22). Frame: origin on the fence line at the
    /// crossing, X along the fence, +Z out of the cluster. Vehicle lane centred on the road, the barrier cabinet on an
    /// island at +X, the face-scan turnstile bank beyond it, and at the main gate the guard house beyond the bank.
    /// </summary>
    public static class Gate
    {
        public const float LaneMargin = 0.4f, IslandWidth = 0.7f, CurbRise = 0.15f;
        public const int Passages = 2;
        /// <summary>Clear passage width: 1.1 m on the open route (WLD-INTERIORS §10), 1.0 m elsewhere.</summary>
        public const float Passage = 1.0f, OpenPassage = 1.1f;
        /// <summary>Cabinet: about 1.05 m tall; the collision top still goes to 1.5 m (§8: otherwise it can be jumped).</summary>
        public const float CabinetWidth = 0.24f, CabinetDepth = 1.4f, CabinetHeight = 1.05f, ColliderTop = 1.5f;
        public const float WingHeight = 0.75f, WingBase = 0.3f, Indicator = 0.13f;
        public const float BarrierCabinet = 0.36f, BarrierHeight = 1.05f, ArmY = 0.95f, ArmDepth = 0.07f, ArmHeight = 0.1f;
        /// <summary>Guard house (值班室): small flat-roofed room straddling the fence line, window toward the turnstiles.</summary>
        public const float HouseWidth = 3.4f, HouseDepth = 3.0f, HouseHeight = 3.0f, HouseOut = 0.6f, HouseGap = 0.35f;
        public const float HousePlinth = 0.45f, HouseRoof = 0.3f, HouseEave = 0.35f;
        /// <summary>Footway crossings get a closed steel side gate between two piers.</summary>
        public const float SideGateHeight = 2.0f, PierSize = 0.3f;
        /// <summary>Footways narrower than this are side gates; wider roads get the vehicle gate.</summary>
        public const float VehicleRoad = 4f;
    }

    /// <summary>
    /// Terrace levels of north.terrain.json (MAP-QSH-TERRAIN §7.2, §7.3), mirrored here for the authored heights that sit
    /// on them. The terrain file is the only source of the ground; these constants never replace a HeightAt query.
    /// </summary>
    public static class Terrain
    {
        public const float Upper = 3.0f, Lower = 1.5f;
    }

    /// <summary>
    /// Inspection probes of 20a (MAP-QSH-NORTH 近景巡检点): Q1 outside 学知一组团's open gate at eye height (walk, K1),
    /// Q4 against 学知苑 1 栋's ground floor at 0.5 m (首层贴墙, N-01), Q6 45° down over the 学知一 courtyard from about 40 m.
    /// All three stand on the upper terrace (MAP-QSH-TERRAIN §7.5): eye heights are terrace + 1.62 m, so they float 3 m
    /// above the flat stub until the WP-08 evaluator lands.
    /// </summary>
    public static class Probes
    {
        public static readonly Vector3 GateEye = new(100.4f, Terrain.Upper + 1.62f, -468.9f);
        public static readonly Vector2 GateLook = new(0.934f, 0.358f);
        public static readonly Vector3 WallEye = new(130.7f, Terrain.Upper + 1.62f, -482.7f);
        public static readonly Vector2 WallLook = new(-0.386f, -0.922f);
        public static readonly Vector3 CourtEye = new(146f, Terrain.Upper + 30f, -431f);
        public static readonly Vector2 CourtLook = new(0.57f, -0.82f);
    }

    /// <summary>
    /// 学知一 courtyard stay point (20a-2; D-185, MAP-QSH-ADDITIONS §0 R6): one gently bowed seat wall on the lawn in front of
    /// 学知苑 1 栋's east wing, 8 m off its sunset-facing wall (front face toward about 235°, R1), facing the courtyard, the
    /// open gate and the hill (design:terrain:north-xz1-court-hill). Granite body sunk into the ground, a warm wood seat with
    /// a small overhang (ART-FIDELITY §0: chamfered, capped). Plan points are world (x, z); the bow is a quadratic curve
    /// through <see cref="SeatMid"/>. 初值。
    /// </summary>
    public static class Court
    {
        public static readonly Vector2 SeatStart = new(159.35f, -478.81f), SeatMid = new(162.44f, -475.48f), SeatEnd = new(164.55f, -471.46f);
        public const int SeatSegments = 12;
        /// <summary>Stone body: width, height above the apron, chamfer, and how far it is sunk below the terrain.</summary>
        public const float BodyWidth = 0.42f, BodyHeight = 0.38f, BodyBevel = 0.025f, Sink = 0.1f;
        /// <summary>Wood seat on top: width (0.04 m overhang each side), thickness, chamfer; held back from each stone end.</summary>
        public const float SeatWidth = 0.5f, SeatThickness = 0.07f, SeatBevel = 0.015f, SeatHoldBack = 0.15f;
        /// <summary>Paver apron on the courtyard side where feet rest; its top is flush with WP-15 paving (GroundSection.PavingY,
        /// 0.15 m over the terrain, 5 cm above the lawn), and the seat stands 0.45 m above it.</summary>
        public const float ApronWidth = 1.5f, ApronThickness = 0.08f, PavingLevel = 0.15f;
        public static readonly ToonSpec Apron = new(new("ground.paver.lit"), new("ground.paver.shade")) { Fx = ToonFx.BevelLift, Line = LineClass.Fine, Wet = 0.6f };
    }

    /// <summary>
    /// 学知苑 1 栋 front door toward the courtyard (MAP-QSH-NORTH §4.12, WLD-INTERIORS §2.8, §2.17 `xz1.hall`). The door
    /// centre and facing come from the interior index (EntranceDecl); the porch sits on the facade edge nearest to it.
    /// Frame: origin on the footprint (glass plane), X along the facade, +Z out to the courtyard.
    /// </summary>
    public static class Hall
    {
        public const string Feature = "qsh:way:217855218";
        /// <summary>Ground floor raised about 1.0 m above the courtyard (MAP-QSH-TERRAIN §7.4), reached by 6–8 risers.</summary>
        public const float FloorRaise = 1.0f, Tread = 0.33f;
        public const int MinRisers = 6, MaxRisers = 8;
        /// <summary>WP-14's wall face stands one reveal outside the footprint.</summary>
        public const float WallFace = 0.3f;
        /// <summary>Landing reaches past the 2.5 m teleport landing in front of the door (SYS-ATLAS §5).</summary>
        public const float LandingWidth = 6.4f, LandingDepth = 3.2f, StepsWidth = 3.2f;
        /// <summary>Vestibule block (门斗) with a through doorway; double glass leaves fixed open against the reveals.</summary>
        public const float VestibuleWidth = 3.6f, VestibuleDepth = 1.2f, VestibuleHeight = 3.4f, DoorWidth = 1.8f, DoorHeight = 2.5f;
        public const float CanopyDepth = 1.5f, CanopyThickness = 0.2f;
    }
}
