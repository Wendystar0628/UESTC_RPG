using System.Numerics;
using Campus.Gen.Zones.Common;
using static Campus.Gen.Zones.Common.ZoneLooks;
using Campus.Gen.Zones.DesignBoundary;
using Campus.Contracts;
using FacadeRules = Campus.Gen.Buildings.Rules.FacadeRules;

namespace Campus.Gen.Zones.North;

/// <summary>
/// Every tunable value of the north zone in one place, for review (WF-FLOW): materials as colour names, the cluster
/// table and the dimensions of each site. Coordinates are world (x, z) in metres in world/north.json; local frames are
/// described per block. Values marked "初值" are starting values awaiting the main session's call.
/// </summary>
public static class NorthTune
{
    public const int BudgetTrianglesL0 = 24_000;
    public const int BudgetTrianglesL1 = 24_000;
    public const int BudgetDrawCalls = 14;

    /// <summary>Materials (ART-STYLE colour names only).</summary>
    public static class Look
    {
        /// <summary>Guard house walls: the renovated dormitory warm yellow (N-22: "值班室按 N-02 的预设做新").</summary>
        public static readonly ToonSpec HouseWall = S("wall.dormYellow", ToonFx.BevelLift);
        /// <summary>Copings, sills, door surrounds, canopy fascias.</summary>
        public static readonly ToonSpec Trim = S("wall.greyWhite", ToonFx.BevelLift, LineClass.Fine);
        public static readonly ToonSpec Plinth = S("base.warmBrown", ToonFx.BevelLift);
        public static readonly ToonSpec RoofSlab = S("roof.grey", ToonFx.Mottle, LineClass.Building, 0.85f);
        /// <summary>Thresholds, islands, the hall landing and its steps: warm granite.</summary>
        public static readonly ToonSpec Granite = S("stone.beige", ToonFx.BevelLift);
        /// <summary>Fence posts and rails, barrier arms: deep grey steel (MAP-QSH-NORTH). 初值：借 WP-12 的围挡深灰。</summary>
        public static readonly ToonSpec Steel = S("equip.fence", ToonFx.BevelLift, LineClass.Fine, 0.4f);
        /// <summary>Fence bars: same colour, no outline, so the 25 mm bars read as a veil, not hatching (ART-AMBIENCE).</summary>
        public static readonly ToonSpec SteelBar = S("equip.fence", ToonFx.None, LineClass.None, 0.4f);
        /// <summary>Turnstile cabinets, barrier cabinets, scanner stalks: brushed stainless.</summary>
        public static readonly ToonSpec Stainless = S("frame.alu", ToonFx.BevelLift, LineClass.Fine, 0.4f);
        public static readonly ToonSpec Glass = ZoneLooks.Glass;
        /// <summary>Clipped hedge inside the fence: the seasonal hedge colour, soft vegetation shading.</summary>
        public static readonly ToonSpec Hedge = new(new("hedge.lit"), new("hedge.shade"))
        { Normal = ToonNormal.Smooth, Steps = ShadeSteps.Soft, Line = LineClass.Tree, Wet = WetSurface.PorousResponse };
        /// <summary>
        /// Open-lane arrow. 借用：`ground.turf`（WP-12 出口牌同样借它），`sign.green` 是发光色名、没有受光/影色对；
        /// WP-04 的发光着色与 `sign.exitGreen` 到位后换。
        /// </summary>
        public static readonly ToonSpec ArrowGreen = S("ground.turf", ToonFx.None, LineClass.None, 0.2f);
        /// <summary>Closed-lane cross. 借用：`maple`；WP-12 的 `equip.fireRed`（全校的红）进 main 后换。</summary>
        public static readonly ToonSpec CrossRed = S("maple", ToonFx.None, LineClass.None, 0.2f);
        public static readonly ToonSpec Wood = S("door.wood", ToonFx.BevelLift, LineClass.Fine);
        /// <summary>Canal-bank railing: light painted steel, the north-gate plaza's (WP-30 30a, MAP-QSH-WEST 浅色栏杆), so the two runs read as one.</summary>
        public static readonly ToonSpec LightSteel = S("wall.greyWhite", ToonFx.None, LineClass.Fine, 0.4f);
        /// <summary>Canal-bank railing bars: same colour, no outline.</summary>
        public static readonly ToonSpec LightBar = S("wall.greyWhite", ToonFx.None, LineClass.None, 0.4f);
    }

    /// <summary>
    /// One dormitory cluster (MAP-QSH-NORTH 数据定位 · 宿舍组团; MAP-QSH-RENOVATION N-22). Its OSM polygon is the fence line.
    /// The main gate point, whether that gate is open, and the wall presets come from world/north.json (D-239): the gate is
    /// <c>design:north-gate-{Key}</c> (props.open), the presets are the buildings' family and facade fields in world/north.json.
    /// </summary>
    /// <param name="Id">Cluster entity.</param>
    /// <param name="Key">Short key for site IDs and the world record's gate ID.</param>
    public sealed record Cluster(string Id, string Key);

    /// <summary>20a-20b2 clusters in site order: 学知苑, the international students' cluster, 硕丰苑 and 博翰苑.</summary>
    public static readonly Cluster[] Clusters =
    [
        // 20a: 学知苑 1-8 and the international students' cluster.
        new(NorthFeatureIds.ClusterXz1.Entity, "xz1"),
        new(NorthFeatureIds.ClusterXz2.Entity, "xz2"),
        new(NorthFeatureIds.ClusterXz3.Entity, "xz3"),
        new(NorthFeatureIds.ClusterXz4.Entity, "xz4"),
        new(NorthFeatureIds.ClusterXz5.Entity, "xz5"),
        new(NorthFeatureIds.ClusterXz6.Entity, "xz6"),
        new(NorthFeatureIds.ClusterXz7.Entity, "xz7"),
        new(NorthFeatureIds.ClusterXz8.Entity, "xz8"),
        new(NorthFeatureIds.International.Entity, "intl"),
        // 20b1: 硕丰苑.
        new(NorthFeatureIds.ClusterSf1.Entity, "sf1"),
        new(NorthFeatureIds.ClusterSf2.Entity, "sf2"),
        new(NorthFeatureIds.ClusterSf3.Entity, "sf3"),
        new(NorthFeatureIds.ClusterSf4.Entity, "sf4"),
        new(NorthFeatureIds.ClusterSf5.Entity, "sf5"),
        new(NorthFeatureIds.ClusterSf6.Entity, "sf6"),
        new(NorthFeatureIds.ClusterSf7.Entity, "sf7"),
        new(NorthFeatureIds.ClusterSf8.Entity, "sf8"),
        new(NorthFeatureIds.ClusterSf10.Entity, "sf10"),
        // 20b2: 博翰苑.
        new(NorthFeatureIds.ClusterBh1.Entity, "bh1"),
        new(NorthFeatureIds.ClusterBh2.Entity, "bh2"),
    ];

    /// <summary>Cluster fence (MAP-QSH-NORTH): 2.2 m see-through steel bars, a 1.2 m clipped hedge inside.</summary>
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
    /// Cluster gate at a road crossing (MAP-QSH-NORTH, MAP-QSH-RENOVATION N-22). Frame: origin on the fence line at the
    /// crossing, X along the fence, +Z out of the cluster. Vehicle lane centred on the road, the barrier cabinet on an
    /// island at +X, the face-scan turnstile bank beyond it, and at the main gate the guard house beyond the bank.
    /// </summary>
    public static class Gate
    {
        public const float LaneMargin = 0.4f, IslandWidth = 0.7f, CurbRise = 0.15f;
        public const int Passages = 2;
        /// <summary>Clear passage width: 1.1 m on the open route (WLD-INTERIORS), 1.0 m elsewhere.</summary>
        public const float Passage = 1.0f, OpenPassage = 1.1f;
        /// <summary>Cabinet: about 1.05 m tall; the collision top still goes to 1.5 m (§8: otherwise it can be jumped).</summary>
        public const float CabinetWidth = 0.24f, CabinetDepth = 1.4f, CabinetHeight = 1.05f;
        public const float WingHeight = 0.75f, WingBase = 0.3f, Indicator = 0.13f;
        public const float BarrierCabinet = 0.36f, BarrierHeight = 1.05f, ArmY = 0.95f, ArmDepth = 0.07f, ArmHeight = 0.1f;
        /// <summary>Guard house (值班室): small flat-roofed room straddling the fence line, window toward the turnstiles.</summary>
        public const float HouseWidth = 3.4f, HouseDepth = 3.0f, HouseHeight = 3.0f, HouseOut = 0.6f, HouseGap = 0.35f;
        public const float HousePlinth = FacadeRules.HutPlinth, HouseRoof = 0.3f, HouseEave = 0.35f;
        /// <summary>Footway crossings get a closed steel side gate between two piers.</summary>
        public const float SideGateHeight = 2.0f, PierSize = 0.3f;
        /// <summary>Footways narrower than this are side gates; wider roads get the vehicle gate.</summary>
        public const float VehicleRoad = 4f;
    }

    /// <summary>
    /// Inspection probes of 20a (MAP-QSH-NORTH 近景巡检点): Q1 outside 学知一组团's open gate at eye height (walk, K1),
    /// Q4 against 学知苑 1 栋's ground floor at 0.5 m (首层贴墙, N-01), Q6 45° down over the 学知一 courtyard from about 40 m.
    /// Review-camera input only: these fixed poses are not scene geometry.
    /// </summary>
    public static class Probes
    {
        /// <summary>The cluster whose main gate is the open walking route (overlay design:north-gate-xz1, props.open); Q1 and Q6 belong to it.</summary>
        public const string OpenCluster = "xz1";
        public static readonly Vector3 GateEye = new(100.4f, 3f + WalkRules.StandingEye, -468.9f);
        public static readonly Vector2 GateLook = new(0.934f, 0.358f);
        public static readonly Vector3 WallEye = new(130.7f, 3f + WalkRules.StandingEye, -482.7f);
        public static readonly Vector2 WallLook = new(-0.386f, -0.922f);
        public static readonly Vector3 CourtEye = new(146f, 3f + 30f, -431f);
        public static readonly Vector2 CourtLook = new(0.57f, -0.82f);
    }

    /// <summary>
    /// 学知一 courtyard stay point (20a-2, moved in 20a-3; D-185, MAP-QSH-ADDITIONS R6): one gently bowed seat wall at the
    /// courtyard walk (the cluster road's north-east leg toward 银杏大道), on its south-east side. It faces north-west across
    /// the walk to the hill (design:terrain:north-xz1-court-hill) and the lawn behind it; at its back is the lawn in front of
    /// the south dormitory, whose windows stay 10 m off. A paved pad fills the space between the wall and the walk's edge, so
    /// the seat is reached from the walk without crossing grass. Granite body sunk into the ground, a warm wood seat with a
    /// small overhang (ART-FIDELITY: chamfered, capped). The wall's plan points are the registered overlay polyline
    /// (D-239: start, mid, end); the bow is a quadratic curve through the mid point, bulging away from the walk so the wall
    /// gathers the view. 初值。
    /// </summary>
    public static class Court
    {
        public const int SeatSegments = 12;
        /// <summary>Stone body: width, height above the pad, chamfer, and how far it is sunk below the terrain.</summary>
        public const float BodyWidth = 0.42f, BodyHeight = 0.38f, BodyBevel = 0.025f, Sink = 0.1f;
        /// <summary>Wood seat on top: width (0.04 m overhang each side), thickness, chamfer; held back from each stone end.</summary>
        public const float SeatWidth = 0.5f, SeatThickness = 0.07f, SeatBevel = 0.015f, SeatHoldBack = 0.15f;
        /// <summary>Paver pad in front of the wall, out to a straight edge <see cref="PadReach"/> from the chord toward the walk,
        /// stopping 2 cm short of the walk's carriageway: 1.5 m deep at the wall ends, 2.0 m at the middle. Its top is at
        /// WP-15 paving level (GroundSection.PavingY, 0.15 m over the terrain, 5 cm above the lawn) plus <see cref="PadLift"/>,
        /// so where it laps over the kerb stone it covers it instead of flickering against it; the seat stands 0.45 m above it.</summary>
        public const float PadReach = 1.69f, PadThickness = 0.08f, PadLift = 0.01f;
        public static readonly ToonSpec Pad = new(new("ground.paver.lit"), new("ground.paver.shade")) { Fx = ToonFx.BevelLift, Line = LineClass.Fine, Wet = 0.6f };
    }

    /// <summary>
    /// Canal-bank railing along the 商业街 side of 红光右支渠北段 (20a-4; MAP-QSH-NORTH 碰撞体, MAP-QSH-TERRAIN: the bank is
    /// a stone wall with a railing, 1.05 m at least, collision top 1.5 m). It starts where the north-gate plaza's railing
    /// (WP-30 30a, design:boundary-north-a05-plaza, commit 04767c7) ends and runs south along the east bank to the street's far
    /// end. Frame as the plaza's, read from the registered bank selection: <c>t</c> along the straight water edge (toward bearing 21.8°, so the run south into the campus
    /// is toward negative t), <c>n</c> off the water toward the road. The railing language is the plaza's: 1.1 m, square posts at most 1.6 m apart, handrail and bottom
    /// rail, 20 mm bars at 0.12 m, an end post a size heavier with a cap. The plaza's end post at <see cref="JointT"/> is shared:
    /// this run starts flush on its campus-side face and carries on from its centre in collision, so there is neither gap nor overlap.
    /// 初值。
    /// </summary>
    public static class Bank
    {
        /// <summary>The plaza railing's campus-side end: <c>BoundaryTune.Plaza.RailT0</c> and <c>RailN</c>. Its end post is <see cref="JointPost"/> thick.</summary>
        public const float JointT = BoundaryTune.Plaza.RailT0, RailN = BoundaryTune.Plaza.RailN, JointPost = BoundaryTune.Plaza.EndPostSize;
        /// <summary>Where the run ends: 商业街's far end is at t −110.5; the end post stands just beyond it.</summary>
        public const float EndT = -115f;
        public static readonly float Height = new Campus.Contracts.Parts.RailingParams().Height;
        public const float PostPitch = 1.6f, PostSize = 0.06f, HandRail = 0.07f, BottomRail = 0.12f, BarSize = 0.02f, BarPitch = 0.12f;
        public const float ColliderHalf = 0.08f;
    }

    /// <summary>
    /// 硕丰苑十组团 rest deck (20b1; D-185 R6, MAP-QSH-ADDITIONS 休憩平台, registered as design:north-sf10-deck): the
    /// courtyard's one stay point, 12 × 8 m and 0.45 m high so its edge is the seat; deep grey steel frame, wood top in four
    /// boards around a square pit holding a clipped shrub. Long side along 33、32 栋, 20 m and more off every wall. The plan
    /// (centre, long axis, length, width) and the height are the registered overlay polygon (D-239). 初值。
    /// </summary>
    public static class Deck
    {
        public const float Sink = 0.1f;
        /// <summary>Wood top: thickness, overhang past the steel frame, chamfer, and the gap between boards.</summary>
        public const float Top = 0.07f, Overhang = 0.05f, TopBevel = 0.015f, Seam = 0.02f;
        /// <summary>Square pit in the middle: clear size, steel kerb width; the shrub stands this far above the deck.</summary>
        public const float Pit = 2.4f, Kerb = 0.08f, Shrub = 0.75f, ShrubBevel = 0.12f;
    }

    /// <summary>
    /// 学知苑 1 栋 front door toward the courtyard (MAP-QSH-NORTH, WLD-INTERIORS `xz1.hall`). The door
    /// centre and facing come from the interior index (EntranceDecl); the porch uses the registered facade selection.
    /// Frame: origin on the footprint (glass plane), X along the facade, +Z out to the courtyard.
    /// </summary>
    public static class Hall
    {
        /// <summary>Ground floor raised about 1.0 m above the courtyard (MAP-QSH-TERRAIN), reached by 6–8 risers.</summary>
        public const float FloorRaise = 1.0f, Tread = 0.33f;
        public const int MinRisers = 6, MaxRisers = 8;
        /// <summary>WP-14's wall face stands one reveal outside the footprint.</summary>
        public const float WallFace = 0.3f;
        /// <summary>Landing reaches past the 2.5 m teleport landing in front of the door (SYS-ATLAS).</summary>
        public const float LandingWidth = 6.4f, LandingDepth = 3.2f, StepsWidth = 3.2f;
        /// <summary>Vestibule block (门斗) with a through doorway; double glass leaves fixed open against the reveals.</summary>
        public const float VestibuleWidth = 3.6f, VestibuleDepth = 1.2f, VestibuleHeight = 3.4f, DoorWidth = 1.8f, DoorHeight = 2.5f;
        public const float CanopyDepth = 1.5f, CanopyThickness = 0.2f;
    }
}
