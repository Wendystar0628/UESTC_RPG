using Campus.Contracts;

namespace Campus.Gen.Maps.Qingshuihe.Core;

/// <summary>Authored time-plaza, flag and sundial values; metres, degrees and palette family names.</summary>
public static class PlazaStyle
{
    // ---- Time plaza (MAP-QSH-CORE, MAP-QSH-TERRAIN) ----
    /// <summary>Axis bearing used only if the library stair feature is missing (P02 looks along 307°).</summary>
    public const float AxisBearingFallback = 307.5f;

    /// <summary>Below this terrain drop between rim and floor the plaza is flat and gets no ring steps (flat stub).</summary>
    public const float StepMinRise = 0.3f;

    /// <summary>Concentric stone bands (三道同心环带): (inner radius, outer radius) as fractions of the step-foot radius.</summary>
    public static readonly (float Inner, float Outer)[] Bands = [(0.905f, 0.94f), (0.62f, 0.645f), (0.335f, 0.355f)];

    /// <summary>Twelve hour marks on the outermost band (十二时辰): thin light granite bars across it; the four cardinal ones get a band-stone diamond outside.</summary>
    public const float HourMarkWidth = 0.16f;

    public const float HourDiamond = 0.32f;

    /// <summary>Central emblem (原创徽记, ART-STYLE) as a single-colour inlay: outer ring, octagon, eight traces.</summary>
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

    /// <summary>Drooping flag at rest (SYS-MOTION animates it later): hang length, fold count and fold depth.</summary>
    public const float FlagHang = 2.6f, FlagSideHang = 2.2f, FlagFoldDepth = 0.09f, FlagFoldWidth = 0.11f;

    public const int FlagFolds = 4;

    /// <summary>Outer ring of thin street lamps on the rim (初灯组, MAP-QSH-ADDITIONS); first lamp half a step off the axis.</summary>
    public const int RimLamps = 16;

    public const float RimLampOffset = 1.2f;        // outside the plaza edge, on the lawn

    public const float RimLampHeight = 4.2f, RimLampWidth = 0.25f;

    public const float RimLampLightY = 4.0f, RimLampEnergy = 1f, RimLampRange = 7f;

    public static readonly ChannelId RimLampChannel = new("lamp.first");

    public static readonly PaletteToken LampColor = new("lamp.warm");

    /// <summary>Flag uplights in the plaza floor (MAP-QSH-CORE), on the street-lamp curve.</summary>
    public static readonly ChannelId FlagLightChannel = new("lamp.street");

    public const float FlagLightEnergy = 0.8f, FlagLightRange = 6f;

    /// <summary>Benches under rim lamps, facing the plaza (design addition, world/core.json): lamp slots and offset.</summary>
    public static readonly int[] BenchLampSlots = [6, 8, 10];

    public const float BenchSideOffset = 1.2f;

    // ---- Sundial (MAP-QSH-CORE) ----
    public static readonly (float Radius, float Height)[] SundialTiers = [(2.4f, 0.2f), (1.9f, 0.2f), (1.4f, 0.2f)];

    public const float SundialTierBevel = 0.025f;

    public const float SundialPadRadius = 3.6f;     // granite pad around the tiers

    public const float SundialPedestal = 0.32f, SundialDialCentre = 1.35f;

    public const float SundialDial = 0.72f, SundialDialThick = 0.06f;

    public const float SundialRingInner = 0.72f, SundialRingOuter = 0.84f, SundialRingThick = 0.09f;

    public const float SundialGnomon = 0.022f, SundialGnomonLength = 1.5f;

    /// <summary>Hour marks inlaid in the dark band: the light granite of the plaza.</summary>
    public const string MarkStone = "ground.curb";

    /// <summary>Single-colour emblem on a landscape carrier: gold (ART-STYLE "景观石金").</summary>
    public const string EmblemGold = "uestc.gold";

    /// <summary>Bronze dial face (青铜色), from the registered bronze family.</summary>
    public const string Bronze = "bronze";

    /// <summary>Lamp poles: grey aluminium.</summary>
    public const string LampMetal = "frame.alu";

    public const string FlagBlue = "uestc.blue";

    /// <summary>National flag red: borrowed from WP-12's fire-equipment red, the campus' one red (17:30 lit #C8473B).</summary>
    public const string FlagRed = "equip.fireRed";
}
