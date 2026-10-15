using Campus.Contracts;
using Campus.Contracts.Audio;

namespace Campus.Gen.Parts.Circulation;

/// <summary>
/// Every tunable value of WP-11 lives here (ENG-TEAM §5). Sizes are metres. Sources: ART-MODELING §2 and §7.8 (stylised
/// proportions; human-scale anchors kept real), WLD-TERRAIN §5 (step, landing and ramp rules), WLD-INTERIORS §10 and
/// SYS-WALK §7, §13 (collision). Values marked "review" are mainline initial values awaiting the main session's call.
/// </summary>
public static class CirculationStyle
{
    // ---- Materials (core palette tokens; WP-11 has no extension segment yet, ART-STYLE §8.5) ----
    /// <summary>Railing posts, top rails and gate frames: grey aluminium.</summary>
    public static readonly ToonSpec Metal = new(new("frame.alu.lit"), new("frame.alu.shade")) { Fx = ToonFx.BevelLift };
    /// <summary>Balusters and bars: same colour, small-item line fade (ART-MODELING §7.8), no shadow (ART-MODELING §7.4).</summary>
    public static readonly ToonSpec MetalFine = new(new("frame.alu.lit"), new("frame.alu.shade")) { Line = LineClass.Fine };
    /// <summary>Round handrail tubes: smooth normals, curved-prop two-and-a-half steps (ART-STYLE §2.1).</summary>
    public static readonly ToonSpec MetalTube = new(new("frame.alu.lit"), new("frame.alu.shade"))
    { Normal = ToonNormal.Smooth, Steps = ShadeSteps.TwoHalf, Line = LineClass.Fine };
    /// <summary>Outdoor steps, cheek walls, ramps, platforms and planters: warm granite.</summary>
    public static readonly ToonSpec Stone = new(new("stone.beige.lit"), new("stone.beige.shade")) { Fx = ToonFx.BevelLift };
    /// <summary>Copings and trim strips: lighter stone with its own line class so the joint draws (ART-MODELING §7.4).</summary>
    public static readonly ToonSpec Trim = new(new("wall.greyWhite.lit"), new("wall.greyWhite.shade")) { Line = LineClass.Fine, Fx = ToonFx.BevelLift };
    /// <summary>Indoor stair flights and landings: light terrazzo-like grey white.</summary>
    public static readonly ToonSpec StairBody = new(new("wall.greyWhite.lit"), new("wall.greyWhite.shade")) { Fx = ToonFx.BevelLift };
    /// <summary>Planter soil.</summary>
    public static readonly ToonSpec Soil = new(new("base.warmBrown.lit"), new("base.warmBrown.shade")) { Line = LineClass.None };
    /// <summary>Grass mats on seat tiers: the seasonal lawn colour.</summary>
    public static readonly ToonSpec Grass = new(new("grass.lit"), new("grass.shade")) { Line = LineClass.None };
    /// <summary>Clipped shrub cushion in planters: seasonal hedge colour, soft vegetation shading.</summary>
    public static readonly ToonSpec Shrub = new(new("hedge.lit"), new("hedge.shade"))
    { Normal = ToonNormal.Smooth, Steps = ShadeSteps.Soft, Line = LineClass.Tree };

    // ---- Collision feet (SYS-AUDIO §9.3); used when the parameters leave Foot at Auto ----
    public const FootMat OutdoorStepFoot = FootMat.StonePaving;
    public const FootMat IndoorStairFoot = FootMat.Terrazzo;
    public const FootMat RampFoot = FootMat.Concrete;
    public const FootMat MetalFoot = FootMat.Metal;
    public const FootMat SlabFoot = FootMat.Concrete;

    // ---- Shared joints and bevels (ART-MODELING §7.2, §7.4) ----
    /// <summary>Different pieces sink this far into each other instead of sharing a face.</summary>
    public const float Joint = 0.005f;
    /// <summary>Outer faces of overlaid trims stay this far inside the host face so no two faces are coplanar.</summary>
    public const float FaceInset = 0.002f;
    /// <summary>Step and landing nosing chamfer, R 10–20 mm.</summary>
    public const float NosingBevel = 0.015f;
    public const float RailBevel = 0.012f;
    public const float PostBevel = 0.01f;
    public const float SlabBevel = 0.02f;
    /// <summary>Solids continue this far below their base so terrain noise never opens a gap.</summary>
    public const float Sink = 0.1f;

    // ---- Guard railings (ART-MODELING §2, §7.8) ----
    /// <summary>Top rail height (thickness); width comes from the Depth parameter (0.08–0.15).</summary>
    public const float TopRailHeight = 0.07f;
    public const float PostSize = 0.09f;
    public const float BalusterSize = 0.04f;
    /// <summary>Stylised baluster pitch 0.25–0.3 m; below 4 px per period the far levels drop them (review).</summary>
    public const float BalusterPitch = 0.27f;
    public const float BottomRailClearance = 0.1f;
    public const float BottomRailHeight = 0.05f;
    /// <summary>Variation 1: solid infill panel thickness.</summary>
    public const float PanelThickness = 0.03f;
    /// <summary>Variation 2: number of intermediate horizontal bars.</summary>
    public const int HorizontalBars = 3;

    // ---- Handrails on stairs, steps and ramps ----
    public const float HandrailHeight = 0.9f;
    public const float HandrailRadius = 0.025f;
    /// <summary>Radius of the round knob that caps each free handrail end.</summary>
    public const float HandrailKnob = 0.03f;
    public const int HandrailSegments = 12;
    public const float HandrailPostSize = 0.05f;
    public const float HandrailPostPitch = 1.4f;
    /// <summary>Horizontal run of a handrail past the top nosing.</summary>
    public const float HandrailExtension = 0.25f;

    // ---- Barrier collision (SYS-WALK §7 rules 2–3, §13) ----
    public const float BarrierTop = 1.5f;
    public const float BarrierBelow = 1.0f;
    public const float BarrierMinHalfThickness = 0.05f;

    // ---- Outdoor steps (WLD-TERRAIN §5.1–§5.2) ----
    public const float RiserMin = 0.12f, RiserMax = 0.16f, RiserTarget = 0.15f;
    public const float TreadBase = 0.65f, TreadMin = 0.30f, TreadMax = 0.40f;
    public const float FlightMaxRise = 1.5f;
    public const float LandingMin = 1.2f;
    public const float MainNetworkWidth = 2.4f, OtherWidth = 1.5f;
    /// <summary>Cheek wall width, its height above the nosing line and the flat pier at its foot (review).</summary>
    public const float CheekWidth = 0.3f, CheekRise = 0.15f, CheekPier = 0.3f;
    public const float CopingThickness = 0.06f, CopingOverhang = 0.02f;
    /// <summary>Side handrails appear from this total rise (review).</summary>
    public const float HandrailMinRise = 0.6f;
    /// <summary>Middle handrail when the clear width and the rise both reach these (WLD-TERRAIN §5.1, review).</summary>
    public const float MidHandrailMinClear = 2.0f, MidHandrailMinRise = 1.05f;
    /// <summary>Ramp colliders run this far below the lower level (WLD-INTERIORS §10 item 3).</summary>
    public const float RampBury = 0.2f;
    public const float StepColliderThickness = 0.3f;

    // ---- Grass seat tiers (MAP-QSH-ADDITIONS §2.6: 草阶看台, 下沉草阶草坪) ----
    /// <summary>Concrete edge in front of each grass mat, the grass mat's rise over the concrete tread, the nosing chamfer.</summary>
    public const float SeatEdge = 0.25f, SeatGrass = 0.03f, SeatNosingBevel = 0.02f;

    // ---- Ramps (WLD-TERRAIN §5.1, §5.3) ----
    public const float RampMaxGrade = 1f / 12f, RampMinGrade = 0.05f;
    public const float RampMaxSegmentRise = 0.75f, RampLandingMin = 1.5f;
    public const float RampCurbWidth = 0.15f, RampCurbRise = 0.1f;

    // ---- Indoor stairs (WLD-INTERIORS §10, WP-51a plan options) ----
    public const float WaistThickness = 0.18f;
    public const float LandingThickness = 0.18f;
    public const float WellWidth = 0.2f;
    public const float StairLandingMin = 1.5f;
    public const float StringerWidth = 0.06f, StringerRise = 0.06f, StringerDepth = 0.32f;
    public const float BalusterInset = 0.06f;
    public const float StairBarrierAbove = 1.5f;

    // ---- Balconies (ART-MODELING §2: prefer solid parapets) ----
    public const float BalconySlab = 0.15f;
    public const float ParapetThickness = 0.1f;
    public const float BalconyCoping = 0.06f;
    /// <summary>Variation 1: height of the solid upstand under the metal railing.</summary>
    public const float BalconyUpstand = 0.3f;

    // ---- Planters ----
    public const float PlanterRimWidth = 0.06f, PlanterRimHeight = 0.05f, PlanterRimOverhang = 0.015f;
    public const float PlanterPlinth = 0.05f, PlanterPlinthInset = 0.02f;
    public const float SoilDrop = 0.05f;
    public const float ShrubHeight = 0.28f, ShrubGap = 0.01f, GroundcoverHeight = 0.08f;

    // ---- Drying rails ----
    public const float DryingBarDiameter = 0.035f, DryingBarPitch = 0.3f, DryingBracketThickness = 0.02f;

    // ---- Iron gates (WLD-INTERIORS §10 seals) ----
    public const float GateFrame = 0.08f, GateBar = 0.03f, GateBarPitch = 0.12f;
}
