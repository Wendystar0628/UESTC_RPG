using Campus.Contracts;
using Campus.Contracts.Audio;
using static Campus.Gen.Sites.ZoneLooks;

namespace Campus.Gen.Maps.Qingshuihe.Waterfront;

/// <summary>
/// Every tunable value of the WP-28 bridges in one place (WF-FLOW). Lengths in metres; heights are relative to the
/// carriageway surface at the bridge ends (the road level, WLD-TERRAIN: "桥面标高等于两端路面").
/// </summary>
public static class WaterfrontTune
{
    /// <summary>Kind of a world bridge registration (world/waterfront.json): a point marker whose props name the network lines.</summary>
    public const string BridgeKind = "bridge-deck";
    /// <summary>Overlay kind of a bridge-head lying stone (MAP-QSH-ADDITIONS 桥头卧石).</summary>
    public const string StoneKind = "bridge-stone";

    // ---- Registered hard-bank sample (MAP-QSH-WATER) ----
    public const string BankKind = "bank", HardBankForm = "hard";
    public const string BankSampleId = "design:waterfront-bank-library-south-sample";
    public const float BankCopingWidth = 0.35f, BankCopingThickness = 0.15f, BankCopingOverhang = 0.03f;
    public const float BankWetHeight = 0.2f, BankCopingBevel = 0.015f;
    public const float BankRiser = 0.15f, BankTread = 0.35f, BankSubmersion = 0.15f;
    public const float BankPieceLength = 2f, BankJoinTolerance = 0.02f;
    /// <summary>Plan envelope for water-access steps, bounded by the terrain's supported vertical range without sampling it.</summary>
    public const float BankExitReach = (TerrainSampling.MaximumHeight - TerrainSampling.MinimumHeight + BankSubmersion + 2 * BankRiser) * BankTread / BankRiser;
    public const float BankEndpointTolerance = Campus.Gen.World.WorldFeatureGeometry.SelectionEndpointTolerance;
    public const string BankWet = "ground.bankWet";
    public static ToonSpec BankWallSpec => ConcreteSpec;
    public static ToonSpec BankWetSpec => BankWallSpec with { Lit = new(BankWet + ".lit"), Shade = new(BankWet + ".shade") };

    // ---- Deck section ----
    /// <summary>Default sidewalk on a carriage bridge that has no own footway lines (西门车行桥).</summary>
    public const float DefaultSidewalk = 1.5f;
    /// <summary>Kerb stone between carriageway and sidewalk.</summary>
    public const float KerbWidth = 0.18f, KerbBevel = 0.015f;
    /// <summary>Surfacing layer (asphalt, paving) on the slab.</summary>
    public const float Surfacing = 0.06f;
    /// <summary>Deck top (carriageway) to soffit.</summary>
    public const float SlabDepth = 0.55f;
    /// <summary>The stone parapet base overhangs the concrete fascia by this much: the drip and shadow line of the deck edge.</summary>
    public const float BaseOverhang = 0.04f;

    // ---- Railing (MAP-QSH-ADDITIONS 护栏细部: 灰白石望柱、两道不锈钢细横管) ----
    /// <summary>Continuous stone base (地栿) under the posts: width and rise above the walking surface.</summary>
    public const float BaseWidth = 0.34f, BaseRise = 0.14f, BaseDrop = 0.12f, BaseBevel = 0.02f;
    /// <summary>Post: 0.25 m square, top at the railing part's height above the walking surface (ART-FIDELITY 第 6 条, SYS-WALK 第 1 条).</summary>
    public const float PostSize = 0.25f, PostBevel = 0.02f, PostSpacing = 2.0f;
    public static readonly float PostTop = new Campus.Contracts.Parts.RailingParams().Height;
    /// <summary>Plain square post head (望柱头做成素方，不雕兽): a slightly wider cap.</summary>
    public const float CapSize = 0.29f, CapHeight = 0.08f, CapBevel = 0.015f;
    /// <summary>Bridge-head posts at the four deck corners.</summary>
    public const float HeadSize = 0.40f, HeadTop = 1.25f, HeadCap = 0.46f, HeadCapHeight = 0.10f;
    /// <summary>Two stainless rails (φ48), through the posts; they cast no shadow (ART-MODELING 小件不投影).</summary>
    public const float RailRadius = 0.024f, RailLow = 0.48f, RailHigh = 0.90f;
    public const int RailSegments = 10;

    // ---- Abutments ----
    public const float BankSearchReach = 12f;
    /// <summary>Abutment wall under each deck end at the bank line, set back onto the bank, down below any bed.</summary>
    public const float AbutmentThick = 0.7f, AbutmentSetback = 0.15f, AbutmentBottom = -3.6f, AbutmentBevel = 0.03f;
    /// <summary>The deck rests this far onto the bank behind each abutment (搭板); the rest of a long network bridge line is ground.</summary>
    public const float BearingMargin = 0.9f;

    // ---- Bridge-head stone (桥头卧石) ----
    public const float StoneLength = 1.8f, StoneDepth = 0.75f, StoneHeight = 0.62f, StoneBevel = 0.06f;

    // ---- Collision (SYS-WALK; MAP-QSH-ADDITIONS WP-28 行) ----
    /// <summary>Deck ramps are cut every 2 m along the bridge; each is a sloped box this thick.</summary>
    public const float RampStep = 2.0f, RampThick = 0.3f;
    public const FootMat CarriageFoot = FootMat.Asphalt, WalkFoot = FootMat.StonePaving, RailFoot = FootMat.Metal, StoneFoot = FootMat.StonePaving;

    // ---- Colours (palette names only; MAP-QSH-ADDITIONS 第 2 条) ----
    public const string StoneWhite = "wall.greyWhite";
    public const string Stainless = "roof.silverMetal";
    public const string Concrete = "ground.concrete";
    public const string Asphalt = "ground.asphalt";
    public const string Paver = "ground.paver";
    public const string Kerb = "ground.curb";

    // ---- Material classes, matched to the WP-15 ground so deck and approach read as one surface ----
    public static ToonSpec AsphaltSpec => S(Asphalt, ToonFx.Mottle, LineClass.Building, 1f);
    public static ToonSpec PaverSpec => S(Paver, ToonFx.DetailTiling | ToonFx.Mottle, LineClass.Building);
    public static ToonSpec KerbSpec => S(Kerb, ToonFx.BevelLift | ToonFx.DetailTiling, LineClass.Building);
    public static ToonSpec StoneSpec => S(StoneWhite, ToonFx.BevelLift, LineClass.Building);
    public static ToonSpec ConcreteSpec => S(Concrete, ToonFx.BevelLift, LineClass.Building, 0.7f);
    public static ToonSpec RailSpec => S(Stainless, ToonFx.Rim | ToonFx.AnisoBand, LineClass.Fine, 0.4f) with { Steps = ShadeSteps.TwoHalf, Normal = ToonNormal.Smooth };

    // ---- Budget (PLAN-WP: WP-28 takes 1 % / 1 % from the building share; measured on the 28a bridges) ----

}
