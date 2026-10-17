using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.Zones.North;

/// <summary>
/// Every tunable value of the north zone in one place, for review (WF-FLOW §4): materials as colour names, the cluster
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
        /// <summary>Canal-bank railing: light painted steel, the north-gate plaza's (WP-30 30a, MAP-QSH-WEST §10.4 浅色栏杆), so the two runs read as one.</summary>
        public static readonly ToonSpec LightSteel = S("wall.greyWhite", ToonFx.None, LineClass.Fine, 0.4f);
        /// <summary>Canal-bank railing bars: same colour, no outline.</summary>
        public static readonly ToonSpec LightBar = S("wall.greyWhite", ToonFx.None, LineClass.None, 0.4f);
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
    /// 六组团 B 型（原型），其余在暖米黄与浅奶咖之间分配，相邻组团尽量不同色；竖带一律陶红。
    /// 20b1 clusters: 硕丰苑 (MAP-QSH-NORTH §4.4): 1–20 栋 the renovated stone-beige master's dormitories, 21–28 and 30–33 栋
    /// the newer grey-white ones, with the two small annexes 9803、9804 of their clusters.
    /// 20b2 clusters: 博翰苑 (MAP-QSH-RENOVATION §8.3, §9): 一组团 1–6 栋 in the master's language, walled in cream coffee
    /// to stand apart from the stone-beige 硕丰五 and grey-white 硕丰十 beside them; 二组团 7–8 栋 and the same-batch
    /// 留学生 2 栋 (2015), cream coffee with warm-white window bands. 博翰苑 9 栋 stands inside 硕丰十's fence (<see cref="Singles"/>).
    /// Main gates of 硕丰 and 博翰 (main session 18:26): the vehicle gate nearest the highest-grade road beside the cluster
    /// (丹桂路 and 银杏大道 over the other 6 m campus roads over 5 m service roads), guard house on the right of the way in;
    /// every other gate closed. Under that rule 20b2 moved 硕丰六's main gate to its west gate on 丹桂路 and 硕丰七's to its
    /// north gate (the road out to 丹桂路); 三、十 keep their design drives (no road enters them), and 博翰二 gets one (its only
    /// road in runs along 8 栋's gable, leaving no room for the house on the right).
    /// 20a-4 (main session 20:58) extends the rule to 学知二、六、八 and the international students' cluster, whose main gates
    /// moved to the vehicle gate nearest 银杏大道 / 丹桂路: 学知二 to its east gate (6 m off 银杏大道), 学知六 to its south-east
    /// gate (6 m off), 学知八 to its north-west gate (31 m from 银杏大道, the nearest); the international cluster's one road in leaves the
    /// house a dormitory corner on its right, so it gets a design drive in its south-east corner (design:north-drive-intl, 21 m
    /// from 丹桂路). 学知一's gate is the open walking route (P13, A11, WP-31c) and stays; 三、四、五、七 already follow the rule.
    /// </summary>
    public static readonly Cluster[] Clusters =
    [
        new("qsh:way:596904641", "xz1", "学知一组团", new(107.8f, -466.0f), true, Facades.ACreamCoffee,
            ["qsh:way:217855218", "qsh:way:217855220", "qsh:way:217855221"]),
        new("qsh:way:603368217", "xz2", "学知二组团", new(268.6f, -383.3f), false, Facades.ADormYellow,
            ["qsh:way:217855223", "qsh:way:217855224", "qsh:way:217855225", "qsh:way:217855226"]),
        new("qsh:way:596904652", "xz3", "学知三组团", new(364.2f, -424.1f), false, Facades.ADormYellow,
            ["qsh:way:217855227", "qsh:way:217855228", "qsh:way:217855207", "qsh:way:217855208"]),
        new("qsh:way:596904653", "xz4", "学知四组团", new(365.8f, -417.5f), false, Facades.ACreamCoffee,
            ["qsh:way:217855209", "qsh:way:217855210", "qsh:way:217855211", "qsh:way:217855212"]),
        new("qsh:way:596904654", "xz5", "学知五组团", new(447.4f, -348.2f), false, Facades.ADormYellow,
            ["qsh:way:217855213", "qsh:way:217855214", "qsh:way:217855215", "qsh:way:217855216"]),
        new("qsh:way:596904646", "xz6", "学知六组团", new(154.8f, -643.3f), false, Facades.B,
            ["qsh:way:217855219", "qsh:way:217855217", "qsh:way:217855222"]),
        new("qsh:way:596904651", "xz7", "学知七组团", new(198.8f, -613.9f), false, Facades.ADormYellow,
            ["qsh:way:544160373", "qsh:way:544136508", "qsh:way:544136509"]),
        new("qsh:way:596904662", "xz8", "学知八组团", new(201.3f, -606.8f), false, Facades.ACreamCoffee,
            ["qsh:way:544160372", "qsh:way:449705142", "qsh:way:544136507"]),
        new("qsh:way:848026371", "intl", "留学生组团", new(48.0f, -838.5f), false, Facades.New, []),
        // 20b1: 硕丰苑, all gates closed.
        new("qsh:way:596904664", "sf1", "硕丰一组团", new(6.0f, -606.6f), false, Facades.MasterStoneBeige,
            ["qsh:way:217855292", "qsh:way:217855293", "qsh:way:217855294", "qsh:way:217855295"]),
        new("qsh:way:596904663", "sf2", "硕丰二组团", new(-2.1f, -491.8f), false, Facades.MasterStoneBeige,
            ["qsh:way:217855296", "qsh:way:217855297", "qsh:way:217855298", "qsh:way:217855299"]),
        new("qsh:way:596904658", "sf3", "硕丰三组团", new(389.6f, -568.0f), false, Facades.MasterStoneBeige,
            ["qsh:way:596904656", "qsh:way:596904657", "qsh:way:217855287"]),
        new("qsh:way:596904660", "sf4", "硕丰四组团", new(431.2f, -481.2f), false, Facades.MasterStoneBeige,
            ["qsh:way:217855288", "qsh:way:217855289", "qsh:way:217855290", "qsh:way:217855291"]),
        new("qsh:way:596904659", "sf5", "硕丰五组团", new(533.8f, -352.5f), false, Facades.MasterStoneBeige,
            ["qsh:way:217855254", "qsh:way:217855253", "qsh:way:217855255", "qsh:way:217855252", "qsh:way:217855250"]),
        new("qsh:way:848026374", "sf6", "硕丰六组团", new(102.1f, -905.5f), false, Facades.MasterGreyWhite,
            ["qsh:way:741650454", "qsh:way:741650453", "qsh:way:741650452", "qsh:way:779499804"]),
        new("qsh:way:725234159", "sf7", "硕丰七组团", new(101.8f, -816.7f), false, Facades.MasterGreyWhite,
            ["qsh:way:779499805", "qsh:way:741650451", "qsh:way:741650450"]),
        new("qsh:way:848026373", "sf8", "硕丰八组团", new(41.8f, -848.9f), false, Facades.MasterGreyWhite,
            ["qsh:way:741650456", "qsh:way:741650455", "qsh:way:779499803"]),
        new("qsh:way:1061419119", "sf10", "硕丰苑十组团", new(662.0f, -345.4f), false, Facades.MasterGreyWhite,
            ["qsh:way:1466169449", "qsh:way:1466169451", "qsh:way:1466169450", "qsh:way:1466169448"]),
        // 20b2: 博翰苑, all gates closed. 一组团's one road in opens onto 217855167; 二组团's one road in runs along 8 栋's
        // gable, leaving no room for the guard house on the right, so its main gate is on the design drive from 217855186.
        new("qsh:way:596904655", "bh1", "博翰一组团", new(463.4f, -216.4f), false, Facades.MasterCreamCoffee,
            ["qsh:way:217855243", "qsh:way:217855234", "qsh:way:217855232", "qsh:way:217855233", "qsh:way:217855248",
             "qsh:way:217855245", "qsh:way:217855239"]),
        new("qsh:way:596904661", "bh2", "博翰二组团", new(620.4f, -194.7f), false, Facades.MasterCreamCoffeeWhite,
            ["qsh:way:544136505", "qsh:way:544136504", "qsh:way:544160370"]),
    ];

    /// <summary>A building styled on its own, outside any cluster's building list.</summary>
    public sealed record StyledBuilding(string Id, string Family, string Facade, string Note);

    /// <summary>
    /// Buildings handed to WP-14 with a family and a preset but no cluster of their own (20b2): 博翰苑 9 栋, the three
    /// blocks of WLD-INTERIORS §17, and (20a-4) 5232、5561、9693, all closed — shell, window cards and the simplified interior only (INV-11); WP-14 keeps
    /// its generic door, shut, so no entrance is registered. Families pick the window cards (WLD-INTERIORS §5.1).
    /// </summary>
    public static readonly StyledBuilding[] Singles =
    [
        new("qsh:way:544136503", "dormitory", Facades.MasterCreamCoffeeWhite, "博翰苑 9 栋，在硕丰苑十组团的围栏里，按博翰苑配色"),
        new("qsh:way:949704499", "service", Facades.AnnexService, "无名 4499，社区活动室，2 层（数据记 teaching，改 service）"),
        new("qsh:way:217855247", "utility", Facades.AnnexUtility, "无名 5247，配电房、水泵房，1 层，后勤小楼"),
        new("qsh:way:683646735", "utility", Facades.AnnexHospital, "无名 6735，校医院附属或设备用房，2 层，外墙同校医院"),
        // 20a-4 (main session): the three small blocks beside 博翰一组团 take 博翰's language; 5561 and 9693 are dormitories (data family unknown).
        new("qsh:way:570145232", "service", Facades.AnnexService, "无名 5232，商业服务小楼，2 层，数据家族 service，外墙取博翰苑浅奶咖"),
        new("qsh:way:550365561", "dormitory", Facades.MasterCreamCoffee, "无名 5561，3 层宿舍，数据家族 unknown 改 dormitory，博翰一组团旁，按博翰一组团的老楼翻新配色"),
        new("qsh:way:544269693", "dormitory", Facades.MasterCreamCoffee, "无名 9693，2 层宿舍，数据家族 unknown 改 dormitory，同上"),
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
        /// <summary>硕士楼，硕丰苑 1–20 栋老楼翻新：灰米石材墙、灰铝框、无竖带（§4.4，N-09–N-13）。</summary>
        public const string MasterStoneBeige = "dorm.master.stoneBeige";
        /// <summary>硕士楼语言的新楼，硕丰苑 21–28、30–33 栋：灰白墙、灰米石材窗间板与线脚，"墙色更新、更干净"（§4.4、§4.11）。</summary>
        public const string MasterGreyWhite = "dorm.master.greyWhite";
        /// <summary>硕士楼语言，博翰苑 1–6 栋老楼翻新：浅奶咖墙、暖白线脚、无竖带（MAP-QSH-RENOVATION N-14、§8.3）。</summary>
        public const string MasterCreamCoffee = "dorm.master.creamCoffee";
        /// <summary>博翰苑 7–9 栋与留学生 2 栋（2015）：浅奶咖墙垛夹暖白窗带，更新、更干净（§4.4、§4.11）。</summary>
        public const string MasterCreamCoffeeWhite = "dorm.master.creamCoffeeWhite";
        /// <summary>附属小楼（WLD-INTERIORS §17）：4499 社区活动室、5247 配电房水泵房取博翰苑浅奶咖，6735 取校医院的浅灰。</summary>
        public const string AnnexService = "annex.service.creamCoffee", AnnexUtility = "annex.utility.creamCoffee", AnnexHospital = "annex.utility.greyWhite";
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
    /// 学知一 courtyard stay point (20a-2, moved in 20a-3; D-185, MAP-QSH-ADDITIONS §0 R6): one gently bowed seat wall at the
    /// courtyard walk (the cluster road's north-east leg toward 银杏大道), on its south-east side. It faces north-west across
    /// the walk to the hill (design:terrain:north-xz1-court-hill) and the lawn behind it; at its back is the lawn in front of
    /// the south dormitory, whose windows stay 10 m off. A paved pad fills the space between the wall and the walk's edge, so
    /// the seat is reached from the walk without crossing grass. Granite body sunk into the ground, a warm wood seat with a
    /// small overhang (ART-FIDELITY §0: chamfered, capped). Plan points are world (x, z); the bow is a quadratic curve through
    /// <see cref="SeatMid"/>, bulging away from the walk so the wall gathers the view. 初值。
    /// </summary>
    public static class Court
    {
        /// <summary>The courtyard walk the pad joins: 学知一组团 inner road, 5 m.</summary>
        public const string Walk = "qsh:source:way:552816773";
        public static readonly Vector2 SeatStart = new(212.25f, -455.74f), SeatMid = new(215.95f, -458.18f), SeatEnd = new(219.0f, -461.39f);
        public const int SeatSegments = 12;
        /// <summary>Stone body: width, height above the pad, chamfer, and how far it is sunk below the terrain.</summary>
        public const float BodyWidth = 0.42f, BodyHeight = 0.38f, BodyBevel = 0.025f, Sink = 0.1f;
        /// <summary>Wood seat on top: width (0.04 m overhang each side), thickness, chamfer; held back from each stone end.</summary>
        public const float SeatWidth = 0.5f, SeatThickness = 0.07f, SeatBevel = 0.015f, SeatHoldBack = 0.15f;
        /// <summary>Paver pad in front of the wall, out to a straight edge <see cref="PadReach"/> from the chord toward the walk,
        /// stopping 2 cm short of the walk's carriageway: 1.5 m deep at the wall ends, 2.0 m at the middle. Its top is at
        /// WP-15 paving level (GroundSection.PavingY, 0.15 m over the terrain, 5 cm above the lawn) plus <see cref="PadLift"/>,
        /// so where it laps over the kerb stone it covers it instead of flickering against it; the seat stands 0.45 m above it.</summary>
        public const float PadReach = 1.69f, PadThickness = 0.08f, PavingLevel = 0.15f, PadLift = 0.01f;
        public static readonly ToonSpec Pad = new(new("ground.paver.lit"), new("ground.paver.shade")) { Fx = ToonFx.BevelLift, Line = LineClass.Fine, Wet = 0.6f };
    }

    /// <summary>
    /// Canal-bank railing along the 商业街 side of 红光右支渠北段 (20a-4; MAP-QSH-NORTH §8 碰撞体, MAP-QSH-TERRAIN §6: the bank is
    /// a stone wall with a railing, 1.05 m at least, collision top 1.5 m). It starts where the north-gate plaza's railing
    /// (WP-30 30a, design:boundary-north-a05-plaza, commit 04767c7) ends and runs south along the east bank to the street's far
    /// end. Frame as the plaza's: <c>t</c> along the straight water edge (toward bearing 21.8°, so the run south into the campus
    /// is toward negative t), <c>n</c> off the water toward the road. The railing language is the plaza's: 1.1 m, square posts at most 1.6 m apart, handrail and bottom
    /// rail, 20 mm bars at 0.12 m, an end post a size heavier with a cap. The plaza's end post at <see cref="JointT"/> is shared:
    /// this run starts flush on its campus-side face and carries on from its centre in collision, so there is neither gap nor overlap.
    /// 初值。
    /// </summary>
    public static class Bank
    {
        /// <summary>A point on the straight water edge and its direction: the plaza's (<c>BoundaryTune.Plaza.EdgeOrigin</c>, <c>EdgeDirection</c>).</summary>
        public static readonly Vector2 EdgeOrigin = new(-75.21f, -778.91f), EdgeDirection = new(0.37100f, -0.92863f);
        /// <summary>The plaza railing's campus-side end: <c>BoundaryTune.Plaza.RailT0</c> and <c>RailN</c>. Its end post is <see cref="JointPost"/> thick.</summary>
        public const float JointT = -24.6f, RailN = 0.35f, JointPost = 0.08f;
        /// <summary>Where the run ends: 商业街's far end is at t −110.5; the end post stands just beyond it.</summary>
        public const float EndT = -115f;
        /// <summary>Same as the plaza: the railing stands on a 0.15 m paved walk; with the real bank top this follows the ground.</summary>
        public const float PavingTop = 0.15f;
        public const float Height = 1.1f, PostPitch = 1.6f, PostSize = 0.06f, HandRail = 0.07f, BottomRail = 0.12f, BarSize = 0.02f, BarPitch = 0.12f;
        public const float ColliderTop = 1.5f, ColliderHalf = 0.08f;
    }

    /// <summary>
    /// 硕丰苑十组团 rest deck (20b1; D-185 R6, MAP-QSH-ADDITIONS §2.1 休憩平台, registered as design:north-sf10-deck): the
    /// courtyard's one stay point, 12 × 8 m and 0.45 m high so its edge is the seat; deep grey steel frame, wood top in four
    /// boards around a square pit holding a clipped shrub. Long side along 33、32 栋, 20 m and more off every wall. 初值。
    /// </summary>
    public static class Deck
    {
        public const string Feature = "design:north-sf10-deck";
        public static readonly Vector2 Centre = new(716f, -354f), Along = new(-0.5736f, 0.8192f);
        public const float Length = 12f, Width = 8f, Height = 0.45f, Sink = 0.1f;
        /// <summary>Wood top: thickness, overhang past the steel frame, chamfer, and the gap between boards.</summary>
        public const float Top = 0.07f, Overhang = 0.05f, TopBevel = 0.015f, Seam = 0.02f;
        /// <summary>Square pit in the middle: clear size, steel kerb width; the shrub stands this far above the deck.</summary>
        public const float Pit = 2.4f, Kerb = 0.08f, Shrub = 0.75f, ShrubBevel = 0.12f;
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
