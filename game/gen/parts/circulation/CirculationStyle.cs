using Campus.Contracts;
using Campus.Contracts.Audio;

namespace Campus.Gen.Parts.Circulation;

/// <summary>
/// Every tunable value of WP-11 lives here (WF-FLOW). Sizes are metres. Sources: ART-MODELING and §7.8 (stylised
/// proportions; human-scale anchors kept real), WLD-TERRAIN (step, landing and ramp rules), WLD-INTERIORS and
/// SYS-WALK (collision). Values marked "review" are mainline initial values awaiting the main session's call.
/// </summary>
public static class CirculationStyle
{
    // ---- Materials (core palette tokens; WP-11 has no extension segment yet, ART-STYLE) ----
    /// <summary>Railing posts, top rails and gate frames: grey aluminium.</summary>
    public static readonly ToonSpec Metal = new(new("frame.alu.lit"), new("frame.alu.shade")) { Fx = ToonFx.BevelLift };
    /// <summary>Balusters and bars: same colour, small-item line fade (ART-MODELING), no shadow (ART-MODELING).</summary>
    public static readonly ToonSpec MetalFine = new(new("frame.alu.lit"), new("frame.alu.shade")) { Line = LineClass.Fine };
    /// <summary>Round handrail tubes: smooth normals, curved-prop two-and-a-half steps (ART-STYLE).</summary>
    public static readonly ToonSpec MetalTube = new(new("frame.alu.lit"), new("frame.alu.shade"))
    { Normal = ToonNormal.Smooth, Steps = ShadeSteps.TwoHalf, Line = LineClass.Fine };
    /// <summary>Outdoor steps, cheek walls, ramps, platforms and planters: warm granite.</summary>
    public static readonly ToonSpec Stone = new(new("stone.beige.lit"), new("stone.beige.shade")) { Fx = ToonFx.BevelLift };
    /// <summary>Copings and trim strips: lighter stone with its own line class so the joint draws (ART-MODELING).</summary>
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

    // ---- Collision feet (SYS-AUDIO); used when the parameters leave Foot at Auto ----
    public const FootMat OutdoorStepFoot = FootMat.StonePaving;
    public const FootMat IndoorStairFoot = FootMat.Terrazzo;
    public const FootMat RampFoot = FootMat.Concrete;
    public const FootMat MetalFoot = FootMat.Metal;
    public const FootMat SlabFoot = FootMat.Concrete;

    // ---- Shared joints and bevels (ART-MODELING) ----
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

    // ---- Guard railings (ART-MODELING) ----
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

    // ---- Barrier collision (SYS-WALK rules 2–3, §13) ----
    public const float BarrierBelow = 1.0f;
    public const float BarrierMinHalfThickness = 0.05f;

    // ---- Outdoor steps (WLD-TERRAIN) ----
    /// <summary>Absolute riser floor for geometry; two such risers define the curb/steps boundary.</summary>
    public const float RiserMinAllowed = 0.08f;
    public const float StepsMinRise = 2 * RiserMinAllowed;
    public const float RiserMin = 0.12f, RiserMax = 0.16f, RiserTarget = 0.15f;
    public const float TreadBase = 0.65f, TreadMin = 0.30f, TreadMax = 0.40f;
    public const float LandingMin = 1.2f;
    public const float MainNetworkWidth = 2.4f, OtherWidth = 1.5f;
    /// <summary>Cheek wall width, its height above the nosing line and the flat pier at its foot (review).</summary>
    public const float CheekWidth = 0.3f, CheekRise = 0.15f, CheekPier = 0.3f;
    public const float CopingThickness = 0.06f, CopingOverhang = 0.02f;
    /// <summary>Side handrails appear from this total rise (review).</summary>
    public const float HandrailMinRise = 0.6f;
    /// <summary>Middle handrail when the clear width and the rise both reach these (WLD-TERRAIN, review).</summary>
    public const float MidHandrailMinClear = 2.0f, MidHandrailMinRise = 1.05f;
    /// <summary>Ramp colliders run this far below the lower level (WLD-INTERIORS item 3).</summary>
    public const float RampBury = 0.2f;
    public const float StepColliderThickness = 0.3f;

    // ---- Grass seat tiers (MAP-QSH-ADDITIONS: 草阶看台, 下沉草阶草坪) ----
    /// <summary>
    /// Concrete edge in front of each grass mat, the grass mat's rise over the concrete tread, the nosing chamfer.
    /// Edge 0.25 m and grass 0.03 m are settled (main-session review, 2026-10-16).
    /// </summary>
    public const float SeatEdge = 0.25f, SeatGrass = 0.03f, SeatNosingBevel = 0.02f;

    // ---- Ramps (WLD-TERRAIN) ----
    public const float RampMaxGrade = 1f / 12f, RampMinGrade = 0.05f;
    public const float RampMaxSegmentRise = 0.75f, RampLandingMin = 1.5f;
    public const float RampCurbWidth = 0.15f, RampCurbRise = 0.1f;

    // ---- Indoor stairs (WLD-INTERIORS, WP-51a plan options) ----
    public const float WaistThickness = 0.18f;
    public const float LandingThickness = 0.18f;
    public const float WellWidth = 0.2f;
    public const float StairLandingMin = 1.5f;
    public const float StringerWidth = 0.06f, StringerRise = 0.06f, StringerDepth = 0.32f;
    public const float BalusterInset = 0.06f;

    // ---- Balconies (ART-MODELING: prefer solid parapets) ----
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

    // ---- Iron gates (WLD-INTERIORS seals) ----
    public const float GateFrame = 0.08f, GateBar = 0.03f, GateBarPitch = 0.12f;

    // ---- Folding gates, lift doors, raised and tiered floors (WLD-INTERIORS; core palette tokens) ----
    /// <summary>Folding gate in grey-green paint (灰绿铁门, WLD-AMBIENCE) and in dark steel.</summary>
    public static readonly ToonSpec GateGreen = new(new("umbrella.graygreen.lit"), new("umbrella.graygreen.shade")) { Fx = ToonFx.BevelLift };
    public static readonly ToonSpec GateDark = new(new("equip.fence.lit"), new("equip.fence.shade")) { Fx = ToonFx.BevelLift };
    /// <summary>Lift door and jamb: brushed stainless, white paint, dark steel.</summary>
    public static readonly ToonSpec LiftSteel = new(new("roof.silverMetal.lit"), new("roof.silverMetal.shade")) { Fx = ToonFx.BevelLift };
    public static readonly ToonSpec LiftWhite = new(new("wall.greyWhite.lit"), new("wall.greyWhite.shade")) { Fx = ToonFx.BevelLift };
    /// <summary>Lift indicator and call-button plate, the floor's joints.</summary>
    public static readonly ToonSpec Insert = new(new("glass.lit"), new("glass.shade")) { Line = LineClass.Fine };
    public static readonly ToonSpec FloorWood = new(new("door.wood.lit"), new("door.wood.shade")) { Fx = ToonFx.BevelLift };
    public static readonly ToonSpec FloorGrey = new(new("equip.grey.lit"), new("equip.grey.shade")) { Fx = ToonFx.BevelLift };
    public const FootMat TileFoot = FootMat.Tile;
    public const FootMat WoodFoot = FootMat.WoodFloor;
    public const FootMat SteelFoot = FootMat.SteelPlate;

    // ---- Folding gate ----
    public const float FoldStile = 0.08f, FoldTopRail = 0.08f, FoldBottomRail = 0.05f, FoldTrack = 0.14f;
    public const float FoldBar = 0.03f, FoldBarThick = 0.03f, FoldCellWidth = 0.3f, FoldCellHeight = 0.4f;

    // ---- Lift door ----
    public const float LiftJamb = 0.14f, LiftHead = 0.2f, LiftSill = 0.03f, LiftGap = 0.008f, LiftRecess = 0.02f, LiftFrameRecess = 0.012f;

    // ---- Raised floor ----
    public const float RaisedPlate = 0.035f, RaisedSkirtInset = 0.03f, RaisedModule = 0.6f, RaisedJoint = 0.006f;
}
