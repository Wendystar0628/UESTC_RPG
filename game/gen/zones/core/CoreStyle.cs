using Campus.Contracts;
using Campus.Gen.Zones.Core.Framework;

namespace Campus.Gen.Zones.Core;

/// <summary>
/// Every tunable value of WP-16 lives here (ENG-TEAM §0.1: one place for the main session to review). Sizes are metres,
/// angles degrees. Values marked "review" are mainline initial values awaiting the main session's call; the rest come
/// from the cited documents. Colours are palette names only (ART-STYLE §8.5).
/// </summary>
public static partial class CoreStyle
{
    // ---- Ground surfaces this zone stands on. Mirrors WP-15 GroundSection (paving 0.15, lawn 0.10 above the terrain
    //      height, decal lift 4 mm); the zone cannot reference the ground assembly, keep the two in step. ----
    public const float PavingLift = 0.15f;
    public const float LawnLift = 0.10f;
    public const float DecalLift = 0.004f;
    /// <summary>Solids continue this far below the surface they stand on, so draping never opens a gap.</summary>
    public const float Sink = 0.1f;

    // ---- Building datum (WLD-TERRAIN §4.3) ----
    public const float FloorAbovePad = 0.15f;
    public const float WallBelowPad = 0.3f;
    public const float PlinthTop = 0.6f;

    // ---- Time plaza (MAP-QSH-CORE §2, MAP-QSH-TERRAIN §2) ----
    /// <summary>Axis bearing used only if the library stair feature is missing (P02 looks along 307°).</summary>
    public const float AxisBearingFallback = 307.5f;
    /// <summary>Ring steps: risers near 0.15 m, tread clamp(0.65 − 2R, 0.30, 0.40) = 0.35 m (WLD-TERRAIN §5.1).</summary>
    public const float StepRiserTarget = 0.15f;
    /// <summary>Below this terrain drop between rim and floor the plaza is flat and gets no ring steps (flat stub).</summary>
    public const float StepMinRise = 0.3f;
    /// <summary>Concentric stone bands (三道同心环带): (inner radius, outer radius) as fractions of the step-foot radius.</summary>
    public static readonly (float Inner, float Outer)[] Bands = [(0.905f, 0.94f), (0.62f, 0.645f), (0.335f, 0.355f)];
    /// <summary>Twelve hour marks on the outermost band (十二时辰): thin light granite bars across it; the four cardinal ones get a band-stone diamond outside.</summary>
    public const float HourMarkWidth = 0.16f;
    public const float HourDiamond = 0.32f;
    /// <summary>Central emblem (原创徽记, ART-STYLE §11) as a single-colour inlay: outer ring, octagon, eight traces.</summary>
    public const float EmblemRingInner = 2.05f, EmblemRingOuter = 2.3f;
    public const float EmblemOctagon = 1.25f, EmblemLine = 0.12f, EmblemTraceEnd = 1.75f, EmblemPad = 0.22f;

    /// <summary>Ramps on the axis' two flanks (NE and SW): 1:12, a 1.5 m landing every 0.75 m of rise, deck 2 m wide.</summary>
    public const float RampGrade = 1f / 12f;
    public const float RampMaxRunRise = 0.75f;
    public const float RampLanding = 1.5f;
    public const float RampWidth = 2.0f;
    /// <summary>Stone cheek walls along a ramp's two edges: width, and coping above the higher side.</summary>
    public const float CheekWidth = 0.3f, CheekCoping = 0.1f, CheekBevel = 0.02f;

    /// <summary>Flagpoles: national pole on the axis, blue (left) and school (right) poles beside it (review heights).</summary>
    public const float FlagSetback = 3.4f;          // plinth centre inward from the step foot
    public const float FlagSpacing = 4.2f;
    public const float FlagPoleHeight = 15f, FlagSidePoleHeight = 12.5f;
    public const float FlagPoleBase = 0.11f, FlagPoleTop = 0.06f;
    public const float FlagPlinthLength = 11.2f, FlagPlinthDepth = 1.5f, FlagPlinthHeight = 0.45f, FlagPlinthBevel = 0.03f;
    public const float FlagCollar = 0.24f, FlagCollarHeight = 0.35f, FlagFinial = 0.11f;
    /// <summary>Drooping flag at rest (SYS-MOTION §3.8 animates it later): hang length, fold count and fold depth.</summary>
    public const float FlagHang = 2.6f, FlagSideHang = 2.2f, FlagFoldDepth = 0.09f, FlagFoldWidth = 0.11f;
    public const int FlagFolds = 4;

    /// <summary>Outer ring of thin street lamps on the rim (初灯组, MAP-QSH-ADDITIONS §8.3); first lamp half a step off the axis.</summary>
    public const int RimLamps = 16;
    public const float RimLampOffset = 1.2f;        // outside the plaza edge, on the lawn
    public const float RimLampHeight = 4.2f, RimLampWidth = 0.25f;
    public const float RimLampLightY = 4.0f, RimLampEnergy = 1f, RimLampRange = 7f;
    public static readonly ChannelId RimLampChannel = new("lamp.first");
    public static readonly PaletteToken LampColor = new("lamp.warm");
    /// <summary>Flag uplights in the plaza floor (MAP-QSH-CORE §7), on the street-lamp curve.</summary>
    public static readonly ChannelId FlagLightChannel = new("lamp.street");
    public const float FlagLightEnergy = 0.8f, FlagLightRange = 6f;

    /// <summary>Benches under rim lamps, facing the plaza (design addition, core.overlay.json): lamp slots and offset.</summary>
    public static readonly int[] BenchLampSlots = [6, 8, 10];
    public const float BenchSideOffset = 1.2f;

    // ---- Sundial (MAP-QSH-CORE §3) ----
    /// <summary>Latitude of the campus: the gnomon points to true north at this elevation (身份特征, not tunable).</summary>
    public const float Latitude = 30.75f;
    public static readonly (float Radius, float Height)[] SundialTiers = [(2.4f, 0.2f), (1.9f, 0.2f), (1.4f, 0.2f)];
    public const float SundialTierBevel = 0.025f;
    public const float SundialPadRadius = 3.6f;     // granite pad around the tiers
    public const float SundialPedestal = 0.32f, SundialDialCentre = 1.35f;
    public const float SundialDial = 0.72f, SundialDialThick = 0.06f;
    public const float SundialRingInner = 0.72f, SundialRingOuter = 0.84f, SundialRingThick = 0.09f;
    public const float SundialGnomon = 0.022f, SundialGnomonLength = 1.5f;

    // ---- Tessellation per level ----
    public static int CircleSides(LodLevel lod) => lod switch
    {
        LodLevel.L0 => 96,
        LodLevel.L1 => 64,
        LodLevel.L2 => 40,
        LodLevel.L3 or LodLevel.L4 => 24,
        _ => throw new ArgumentOutOfRangeException(nameof(lod), lod, "Unknown LOD level."),
    };

    public static int SmallSides(LodLevel lod) => lod switch
    {
        LodLevel.L0 => 16,
        LodLevel.L1 => 12,
        LodLevel.L2 => 8,
        LodLevel.L3 or LodLevel.L4 => 6,
        _ => throw new ArgumentOutOfRangeException(nameof(lod), lod, "Unknown LOD level."),
    };

    // ---- Colours (palette families) ----
    /// <summary>Flag plinth, sundial tiers and pad: warm granite.</summary>
    public const string Granite = "stone.beige";
    /// <summary>
    /// Grey stone of the three bands: a pattern in the paving, not a line on it. Borrowed: of all registered colours its
    /// derived lit colour sits nearest the plaza granite (ground.curb) at about 0.06–0.07 lower OKLab L with chroma 0.015
    /// at 17:30 and noon; the building-class warm greys (wall.creamCoffee) derive to an apricot band at dusk.
    /// </summary>
    public const string BandStone = "equip.grey";
    /// <summary>Hour marks inlaid in the dark band: the light granite of the plaza.</summary>
    public const string MarkStone = "ground.curb";
    /// <summary>Single-colour emblem on a landscape carrier: gold (ART-STYLE §11 "景观石金").</summary>
    public const string EmblemGold = "uestc.gold";
    /// <summary>Polished stainless steel: poles, sundial ring and gnomon.</summary>
    public const string Steel = "roof.silverMetal";
    /// <summary>Bronze dial face (青铜色). No bronze entry is registered yet; nearest warm metal brown (review).</summary>
    public const string Bronze = "panel.brown";
    /// <summary>Lamp poles: grey aluminium.</summary>
    public const string LampMetal = "frame.alu";
    public const string FlagBlue = "uestc.blue";
    public const string FlagWhite = "wall.greyWhite";
    /// <summary>National flag red: borrowed from WP-12's fire-equipment red, the campus' one red (17:30 lit #C8473B).</summary>
    public const string FlagRed = "equip.fireRed";

    public static string DefaultFamily(CoreMaterial material) => material switch
    {
        CoreMaterial.Stone => Granite,
        CoreMaterial.Inlay => BandStone,
        CoreMaterial.Metal => Steel,
        CoreMaterial.Cloth => FlagWhite,
        CoreMaterial.Wall or CoreMaterial.Trim or CoreMaterial.Roof or CoreMaterial.Sheet => BuildingFamily(material),
        _ => throw new ArgumentOutOfRangeException(nameof(material), material, "Unknown core material."),
    };

    /// <summary>ToonSpec of a material class with a colour family; the non-colour fields define the class.</summary>
    public static ToonSpec Spec(CoreMaterial material, string family)
    {
        var spec = new ToonSpec(new(family + ".lit"), new(family + ".shade"));
        return material switch
        {
            CoreMaterial.Stone => spec with { Fx = ToonFx.BevelLift | ToonFx.Mottle, Wet = 0.6f },
            // No outline: inlays are part of the paving, so their edges are colour changes only.
            CoreMaterial.Inlay => spec with { Fx = ToonFx.DetailTiling, Line = LineClass.None, Wet = 0.6f },
            CoreMaterial.Metal => spec with { Fx = ToonFx.Rim | ToonFx.AnisoBand, Line = LineClass.Fine, Wet = 0.4f },
            CoreMaterial.Cloth => spec with { Translucency = 0.5f, Line = LineClass.Fine, Wet = 0.3f },
            CoreMaterial.Wall or CoreMaterial.Trim or CoreMaterial.Roof or CoreMaterial.Sheet => BuildingSpec(material, spec),
            _ => throw new ArgumentOutOfRangeException(nameof(material), material, "Unknown core material."),
        };
    }

    public static Paint Stone(string family) => new(CoreMaterial.Stone, family);
    public static Paint Inlay(string family) => new(CoreMaterial.Inlay, family, (byte)(VertexFlags.Decal | VertexFlags.NoShadow));
    public static Paint Metal(string family) => new(CoreMaterial.Metal, family);
    public static Paint Cloth(string family) => new(CoreMaterial.Cloth, family);

    /// <summary>Parameters of a catalogue part with a material of this zone (stubs draw their bounding box until the owner delivers).</summary>
    public static ToonSpec PartMaterial(string family) => Spec(CoreMaterial.Metal, family);
}
