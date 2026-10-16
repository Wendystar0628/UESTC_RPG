using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.Zones.DesignBoundary;

/// <summary>
/// Every tunable value of the campus boundary and its gates in one place, for review (ENG-TEAM §0.1): materials as colour
/// names, the gate table, the fence and the gate dimensions. Coordinates are world (x, z) in metres from campus.json. Gate
/// frames come from the data: origin where the gate road's centreline crosses the boundary ring, local X along the ring
/// toward the guard-house side, local +Z out of the campus. Values marked "初值" await the main session's call.
/// </summary>
public static class BoundaryTune
{
    public const int BudgetTrianglesL0 = 24_000;
    public const int BudgetTrianglesL1 = 16_000;
    public const int BudgetDrawCalls = 12;

    /// <summary>Materials (ART-STYLE §8 colour names only, no new names in 30a).</summary>
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
    /// One fence wing of a gate. Side −1 starts at the left pier's back corner, +1 at the guard house's far wall; the wing then
    /// follows the ring away from the gate for Length metres. A Closure wing first runs straight out of the campus (local +Z)
    /// from the house's front corner to the ring, standing a pillar there, and then follows the ring.
    /// </summary>
    public sealed record Wing(int Side, float Length, bool Closure, bool Hedge);

    /// <summary>
    /// One campus gate. <paramref name="Road"/> is the network road through it (its crossing with the ring is the frame origin);
    /// <paramref name="Forecourt"/> names the overlay paving outside the line, when the ring leaves a pocket there;
    /// <paramref name="Bollards"/> stand on it (local x, z) to keep cars off.
    /// </summary>
    public sealed record GateSpec(string Key, string Name, string Road, Wing[] Wings, string? Forecourt, Vector2[] Bollards);

    /// <summary>
    /// 30a: the north gate (北门, `qsh:node:6784723438`) and the west gate (西门, the residential road 170 m before the 西门桥头
    /// bridge). North: the ring jogs at the gate — inside, the road runs along the ring on its north-east kerb, so the left wing
    /// follows the ring inward to its corner at the channel and a few metres on; outside, the road hugs the ring on its south-west
    /// side, leaving an 8 × 13 m pocket of campus between the gate line and 天润路: the forecourt, closed by a wing running out to
    /// 天润路 and turning south-west along it. West: the ring is straight; both wings follow it, hedge inside.
    /// </summary>
    public static readonly GateSpec[] Gates =
    [
        new("north", "北门", "qsh:source:way:723349795",
        [new(-1, 32f, Closure: false, Hedge: false), new(1, 12f, Closure: true, Hedge: true)],
            "design:boundary-north-forecourt", [new(3.40f, 2.2f), new(3.58f, 5.2f), new(3.76f, 8.2f), new(3.94f, 11.2f)]),
        new("west", "西门", "qsh:source:way:544161581",
        [new(-1, 15f, Closure: false, Hedge: true), new(1, 15f, Closure: false, Hedge: true)], null, []),
    ];

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
