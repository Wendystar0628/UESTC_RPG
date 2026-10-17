using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.Zones.DesignBoundary;

/// <summary>
/// Every tunable value of the campus boundary and its gates in one place, for review (WF-FLOW §4): materials as colour
/// names, the gate table, the fence and the gate dimensions. Coordinates are world (x, z) in metres from campus.json. Gate
/// frames come from the data: origin where the gate road's centreline crosses the boundary ring, local X along the ring
/// toward the guard-house side, local +Z out of the campus. Values marked "初值" await the main session's call. The ring
/// fence (30b) runs everywhere else, opened at the gates, the grates and the side gates (Ring/BoundaryPlan).
/// </summary>
public static class BoundaryTune
{
    public const int BudgetTrianglesL0 = 24_000;
    public const int BudgetTrianglesL1 = 16_000;
    public const int BudgetDrawCalls = 12;

    /// <summary>Materials (ART-STYLE §8 colour names only, no new names in 30a or 30b).</summary>
    public static class Look
    {
        private static ToonSpec S(string family, ToonFx fx = ToonFx.None, LineClass line = LineClass.Building, float wet = 0.6f) =>
            new(new(family + ".lit"), new(family + ".shade")) { Fx = fx, Line = line, Wet = wet };

        /// <summary>Gate piers, guard-house walls and the boundary's stone pillars: light grey stone, the south gate's family.</summary>
        public static readonly ToonSpec Stone = S("wall.greyWhite", ToonFx.BevelLift);
        /// <summary>Caps, joint bands, sills, the plaque frame.</summary>
        public static readonly ToonSpec StoneTrim = S("stone.beige", ToonFx.BevelLift, LineClass.Fine);
        /// <summary>Plinths, kerbs of the traffic island, the fence's low base course.</summary>
        public static readonly ToonSpec Granite = S("stone.beige", ToonFx.BevelLift);
        public static readonly ToonSpec RoofSlab = S("roof.grey", ToonFx.Mottle, LineClass.Building, 0.85f);
        /// <summary>
        /// Name plaque field on the gate pier (题字载体): a darker grey stone panel inside a beige frame. The characters wait for the
        /// scene text atlas (INV-4); the field stays blank until then.
        /// </summary>
        public static readonly ToonSpec PlaqueField = S("roof.grey", ToonFx.None, LineClass.Fine, 0.5f);
        /// <summary>Fence rails: deep grey steel, the north cluster fences' colour (MAP-QSH-NORTH §4.7, ART-AMBIENCE §0.1).</summary>
        public static readonly ToonSpec Steel = S("equip.fence", ToonFx.BevelLift, LineClass.Fine, 0.4f);
        /// <summary>Fence bars: same colour, no outline, so the 25 mm bars read as a veil, not hatching.</summary>
        public static readonly ToonSpec SteelBar = S("equip.fence", ToonFx.None, LineClass.None, 0.4f);
        /// <summary>Turnstile and barrier cabinets, window frames: brushed stainless.</summary>
        public static readonly ToonSpec Stainless = S("frame.alu", ToonFx.BevelLift, LineClass.Fine, 0.4f);
        /// <summary>Barrier arm and the waterside railing: light painted steel (MAP-QSH-WEST §10.4 浅色栏杆).</summary>
        public static readonly ToonSpec LightSteel = S("wall.greyWhite", ToonFx.None, LineClass.Fine, 0.4f);
        public static readonly ToonSpec LightBar = S("wall.greyWhite", ToonFx.None, LineClass.None, 0.4f);
        public static readonly ToonSpec Glass = new(new("glass.lit"), new("glass.shade"))
        { Kind = ToonKind.Glass, Fx = ToonFx.GlassStreak | ToonFx.Glare, Wet = 0 };
        public static readonly ToonSpec Door = S("frame.alu", ToonFx.BevelLift, LineClass.Fine, 0.4f);
        /// <summary>Clipped hedge inside the fence (MAP-QSH-ADDITIONS §2.11 方整绿篱): seasonal hedge colour, soft shading.</summary>
        public static readonly ToonSpec Hedge = new(new("hedge.lit"), new("hedge.shade"))
        { Normal = ToonNormal.Smooth, Steps = ShadeSteps.Soft, Line = LineClass.Tree };
    }

    /// <summary>
    /// Boundary fence (MAP-QSH-ADDITIONS §2.11, WLD-OUTSKIRTS §7.1): the north cluster fence's iron language — deep grey 25 mm
    /// bars at 0.14 m between a bottom and a top rail — hung between light grey 0.5 m stone pillars every ≤ 3.6 m, on a low
    /// granite base course; a 1.2 m clipped hedge inside where the strip allows. The fence's outer face sits on the ring.
    /// </summary>
    public static class Fence
    {
        /// <summary>Fence centreline inside the boundary ring (pillar half-width plus a little).</summary>
        public const float Inset = 0.3f;
        /// <summary>Ring vertices closer than this to the previous kept one are dropped from a fence line (no cramped pillars).</summary>
        public const float MinSegment = 1.2f;
        public const float PillarPitch = 3.6f, PillarSize = 0.5f, PillarHeight = 2.32f;
        public const float CapSize = 0.62f, CapHeight = 0.1f, CapTop = 0.05f;
        /// <summary>Last pillar of a run (收头): a little broader and taller, with a two-step cap.</summary>
        public const float EndPillarSize = 0.6f, EndPillarHeight = 2.45f;
        public const float BaseHeight = 0.2f, BaseWidth = 0.34f;
        public const float BottomRail = 0.26f, TopRail = 2.04f, RailSize = 0.05f, BarSize = 0.025f, BarPitch = 0.14f, BarTop = 2.17f;
        /// <summary>Hedge centre this far inside the fence centreline; 1.2 m high, 0.8 m wide.</summary>
        public const float HedgeOffset = 0.75f, HedgeHeight = 1.2f, HedgeWidth = 0.8f, HedgeBevel = 0.1f;
        /// <summary>Boundary collision: top 2.2 m, half thickness so it reaches the pillar faces (≤ 0.3 m beyond the fence).</summary>
        public const float ColliderTop = 2.2f, ColliderHalf = 0.27f;
    }

    /// <summary>
    /// Campus gate (WLD-OUTSKIRTS §7.1, MAP-QSH-NORTH §8, MAP-QSH-WEST §12): two stone piers flank the carriageway, the barrier
    /// arm is down, the turnstile bank beside the island is closed, and the guard house closes the line on the far side. The
    /// left pier (seen from the street) carries the name plaque. Local X along the ring toward the guard house, +Z outward.
    /// </summary>
    public static class Gate
    {
        /// <summary>Clearance beside the carriageway (road half width plus this) to the pier and island faces.</summary>
        public const float CarriageMargin = 0.05f;
        public const float PierSize = 0.95f, PierHeight = 4.3f, PierPlinth = 0.45f, PierZ = -0.55f;
        public const float PierCap = 0.32f, PierCapOver = 0.12f;
        /// <summary>Plaque on the left pier's street face: field width and height, bottom above the plinth, frame width.</summary>
        public const float PlaqueWidth = 0.6f, PlaqueHeight = 2.3f, PlaqueBottom = 0.95f, PlaqueFrame = 0.06f;
        public const float IslandWidth = 0.95f, IslandLength = 3.0f, IslandRise = 0.15f;
        public const float BarrierZ = -2.2f, BarrierCabinet = 0.36f, BarrierCabinetHeight = 1.05f, ArmY = 0.95f, ArmHeight = 0.1f, ArmDepth = 0.07f;
        public const int Passages = 2;
        public const float Passage = 1.0f, CabinetWidth = 0.26f, CabinetDepth = 1.5f, CabinetHeight = 1.02f, TurnstileZ = -0.85f;
        public const float FlapHeight = 0.55f, FlapBase = 0.38f;
        /// <summary>Guard house (值班室): flat-roofed grey stone room closing the line beyond the turnstiles.</summary>
        public const float HouseGap = 0.07f, HouseWidth = 4.0f, HouseDepth = 3.4f, HouseHeight = 3.3f, HouseFront = -0.1f;
        public const float HousePlinth = 0.45f, HouseRoof = 0.34f, HouseEave = 0.4f, WindowSill = 1.0f, WindowHead = 2.5f;
        /// <summary>Collision tops: turnstiles and arm at least 1.5 m; the gate's invisible top-up over the lane to 2.2 m (SYS-WALK §7).</summary>
        public const float LowTop = 1.5f, LaneTop = 2.2f;
    }

    /// <summary>
    /// One campus gate. <paramref name="Road"/> is the network road through it (its crossing with the ring is the frame origin).
    /// <paramref name="Closure"/>: the line beyond the guard house first runs straight out of the campus (local +Z) to the ring,
    /// where the ring fence takes over (the north gate, whose ring jogs outward there). <paramref name="Forecourt"/> names the
    /// overlay paving outside the line, when the ring leaves a pocket there; <paramref name="Bollards"/> stand on it (local x, z).
    /// <paramref name="Mirror"/> puts the guard house on the street view's left (local −X) when the right is taken (东南口:
    /// 走马河 leaves the campus 9 m to the right). <paramref name="Plaque"/>: the lone pier carries the name plaque (题字载体).
    /// <paramref name="Wicket"/>: local x (before mirroring) of a footway beside the lone pier, closed by its own turnstile bank
    /// (人行便门, MAP-QSH-ADDITIONS §2.11). <paramref name="Locker"/>: local (x, z) of a delivery locker inside, by the fence.
    /// </summary>
    public sealed record GateSpec(string Key, string Name, string Road, bool Closure, string? Forecourt, Vector2[] Bollards,
        bool Mirror = false, bool Plaque = true, float? Wicket = null, Vector2? Locker = null);

    /// <summary>
    /// 30a: the north gate (北门, `qsh:node:6784723438`) and the west gate (西门, the residential road 170 m before the 西门桥头
    /// bridge). 30b: 西北门 (the service road between them, `563777250`), 西二门 (水杉路, footways on both sides: the left one
    /// through a 便门 bank, the right one through the main bank) and 东南口 (the service road by 走马河's exit, house on the left).
    /// The service gates keep the composition without a plaque: they read as the campus's back doors (初值).
    /// </summary>
    public static readonly GateSpec[] Gates =
    [
        new("north", "北门", "qsh:source:way:723349795", Closure: true,
            "design:boundary-north-forecourt", [new(3.40f, 2.2f), new(3.58f, 5.2f), new(3.76f, 8.2f), new(3.94f, 11.2f)]),
        new("west", "西门", "qsh:source:way:544161581", Closure: false, null, []),
        new("northwest", "西北门", "qsh:source:way:563777250", Closure: false, null, [], Plaque: false),
        new("west2", "西二门", "qsh:source:way:544277137", Closure: false, null, [], Wicket: -6.7f, Locker: new(13.2f, -0.86f)),
        new("southeast", "东南口", "qsh:source:way:988857948", Closure: false, null, [], Mirror: true, Plaque: false),
    ];

    /// <summary>
    /// The south gate (WP-18, SouthTune.Gate): its two guard houses close the line, so the ring fence stops on their outer
    /// walls. Copied from SouthTune.Gate (HouseWest, HouseEast, HouseSize); keep in step if 18 moves them.
    /// </summary>
    public static class SouthGate
    {
        public static readonly Vector2 HouseWest = new(-532.535f, 719.865f), HouseEast = new(-510.945f, 735.3575f);
        public const float HouseSize = 5.6f;
    }

    /// <summary>
    /// Every other road that crosses the ring is closed by a side gate (常闭侧门): two end pillars of the fence on the kerbs and a
    /// pair of closed iron leaves in the fence's language (one leaf for a footway). Clearance beside the road edge to the pillar face.
    /// </summary>
    public static class SideGate
    {
        public const float Clear = 0.3f, Bottom = 0.08f, Rail = 0.05f, MidRail = 1.0f, Stile = 0.07f, Seam = 0.03f;
        /// <summary>Roads up to this width get a single pedestrian leaf.</summary>
        public const float FootwayWidth = 3f;
    }

    /// <summary>
    /// A road that runs across a corner of the ring outside the campus (the cycleway round the north tip): the fence follows its
    /// campus side, <paramref name="Clear"/> metres off its edge, instead of crossing it twice.
    /// </summary>
    public sealed record Detour(string Key, string Road, float Clear);

    public static readonly Detour[] Detours = [new("north-tip", "qsh:source:way:723349816", 0.9f)];

    /// <summary>
    /// River grate (拦污格栅, WLD-OUTSKIRTS §7.3, MAP-QSH-ADDITIONS §2.11) wherever water crosses the ring: deep grey flat bars
    /// across the full channel between the fence's end pillars on the two bank tops, from below the bed to 1.5 m above the bank,
    /// held by a top beam and a waist rail. Collision 1.5 m above the bank top, Boundary.
    /// </summary>
    public static class Grate
    {
        /// <summary>Opening beyond each water edge (the end pillar's centre stands on the bank top here).</summary>
        public const float Bank = 0.6f;
        public const float Top = 1.5f, Foot = 2.2f, BarWidth = 0.02f, BarDepth = 0.07f, BarPitch = 0.11f;
        public const float BeamHeight = 0.14f, BeamDepth = 0.16f, WaistY = 0.35f, WaistHeight = 0.08f;
        /// <summary>Water crossings closer than this along the ring are one channel.</summary>
        public const float Cluster = 30f;
        /// <summary>Step along the fence line when looking for water that meets the ring without crossing it.</summary>
        public const float Sample = 0.5f;
    }

    /// <summary>Hedge clearances: off road edges, off water, buildings and paving, trimmed back from the run ends.</summary>
    public static class HedgeClear
    {
        public const float Road = 0.4f, Area = 0.3f, Trim = 0.35f, MinLength = 3.0f, Sample = 0.5f;
        /// <summary>Ring runs shorter than this keep no hedge.</summary>
        public const float MinRun = 6f;
        /// <summary>Area kinds a hedge never stands in.</summary>
        public static readonly string[] Kinds = ["building", "water", "road", "parking", "sport"];
    }

    /// <summary>
    /// North waterside plaza (design:boundary-north-a05-plaza, 初值): 14 × 8.6 m of paving on the channel's east bank between the
    /// water and the east service road, over the end of cycleway 950008623. Local frame: t along the bank (toward 21.8°), n off
    /// the water edge toward the road. The paving is WP-15 ground from the overlay polygon; this site adds the light railing on
    /// the water side and two benches facing the water. The A05 kiosk is WP-13's on WP-48c's anchor: <see cref="Kiosk"/> is the
    /// spot the plaza keeps for it (4.2 m off the water, 5.8 m from the lane edge, facing 165°).
    /// </summary>
    public static class Plaza
    {
        public const string Feature = "design:boundary-north-a05-plaza";
        /// <summary>A point on the straight water edge and its direction (osm:way:687770522's east bank).</summary>
        public static readonly Vector2 EdgeOrigin = new(-75.21f, -778.91f);
        public static readonly Vector2 EdgeDirection = new(0.37100f, -0.92863f);
        public const float T0 = -25f, T1 = -11f, N0 = 0.1f, N1 = 8.7f;
        public const float PavingTop = 0.15f;
        /// <summary>Railing: 1.1 m, posts every ≤ 1.6 m, handrail and bottom rail, 20 mm bars at 0.12 m; collision 1.5 m.</summary>
        public const float RailN = 0.35f, RailHeight = 1.1f, PostPitch = 1.6f, PostSize = 0.06f, HandRail = 0.07f, BottomRail = 0.12f;
        public const float BarSize = 0.02f, BarPitch = 0.12f, RailColliderTop = 1.5f;
        public const float RailT0 = -24.6f, RailT1 = -11.4f;
        public static readonly Contracts.Parts.FurnitureParams BenchSize = new(1.8f, 0.8f, 0.62f);
        /// <summary>Bench centres (t, n); sitters face the water.</summary>
        public static readonly Vector2[] Benches = [new(-22.6f, 1.75f), new(-19.9f, 1.75f)];
        public static readonly Vector2 Kiosk = new(-13.5f, 4.2f);
        public const float KioskYaw = 165f;
    }
}
