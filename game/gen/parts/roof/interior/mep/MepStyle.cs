using Campus.Contracts;

namespace Campus.Gen.Parts.Roof.Mep;

/// <summary>
/// Every tunable value of the interior equipment and sign parts (WLD-INTERIORS §7.5 group D) in one place (WF-FLOW §4).
/// Sizes follow the real products (ART-MODELING §2: human-scale anchors are not enlarged); Opus review sets the finals.
/// <para>All vertices carry the interior flag (shade from palette row 2, lit from row 3, WLD-INTERIORS §8.2) and the
/// no-shadow flag (indoors only walls and slabs cast, WLD-INTERIORS §8.3). Wall- and ceiling-mounted parts are visual only;
/// floor-standing units return one prop box.</para>
/// </summary>
public static class MepStyle
{
    public const byte Flags = VertexFlag.Interior | VertexFlag.NoShadow;

    // ---- Colour families: registered names only (ART-STYLE §8.5). Interior names (WLD-INTERIORS §8.4) are not registered yet. ----
    /// <summary>Off-white plastic and enamel: air-conditioner casings, dispensers, sign housings, switch plates, fans.</summary>
    public const string White = RoofStyle.CasingWhite;
    /// <summary>Brushed stainless: boiler cabinet, purifier, hydrant frame variant.</summary>
    public const string Stainless = RoofStyle.Steel;
    /// <summary>Grey aluminium: taps, trims, drip trays, brackets, door-plate variant.</summary>
    public const string Aluminium = RoofStyle.Aluminium;
    /// <summary>
    /// Dark inserts: tap niches, outlets, valve heads and hoses, door-plate text. Never black (ART-STYLE §4.2). Borrowed from the core
    /// table (Opus review): `glass` (#354F59) is the closest dark near-neutral in ids 64–117, OKLab ΔE 0.037 from a neutral L 0.40
    /// (the next, `roof.grey`, is 0.173); the blue-grey fence steel read as blue under interior light.
    /// </summary>
    public const string Dark = RoofStyle.Glass;
    /// <summary>Display windows and screens.</summary>
    public const string Screen = RoofStyle.Glass;
    /// <summary>Light equipment grey: fan blades variant, keys.</summary>
    public const string Grey = RoofStyle.EquipmentGrey;
    /// <summary>Hydrant door glass, read as an opaque dark pane (no window-light data, so not the glass shader).</summary>
    public const string Glass = RoofStyle.Glass;
    /// <summary>Warm wood: door-plate variant (WP-10's door wood).</summary>
    public const string Wood = "door.wood";
    /// <summary>Bottled water: light aqua (WP-24's near-water colour) until an interior name exists.</summary>
    public const string Water = "water.near";
    /// <summary>
    /// Fire red (消防红, ids 254/255 in ext/WP-12.json): lit #C8473B at autumn 17:30, intrinsic #A7424B solved back from it with
    /// WP-37's deriver. Hydrant frame and door, extinguishers, the scald lettering; WP-13's outdoor hydrants reference the same
    /// name. One of the three saturated interior colours (WLD-INTERIORS §8.4).
    /// </summary>
    public const string FireRed = "equip.fireRed";
    /// <summary>
    /// Exit-sign face green. BORROWED: `ground.turf` (#789E5E), the registered surface pair closest to the review's #3FA56B
    /// (OKLab ΔE 0.052). The face's own name `sign.exitGreen` (#3FA56B, emissive segment 40–63) comes with max-015 (WP-04),
    /// together with the Emissive shading; switch to it then.
    /// </summary>
    public const string ExitGreen = "ground.turf";
    /// <summary>Always-on small signal lights: exit signs, gate arrows (lighting-schedule slot 22, group "always").</summary>
    public static readonly ChannelId SignalChannel = new("sign.signal");
    /// <summary>Glow colour handed to the lamp inventory for the exit sign (ART-STYLE §8.1).</summary>
    public static readonly PaletteToken SignalGlow = new("sign.green");

    public static ToonSpec Casing(string family) => RoofStyle.Casing(family);
    public static ToonSpec Metal(string family) => RoofStyle.Metal(family);
    public static ToonSpec Pipe(string family) => RoofStyle.Pipe(RoofStyle.Casing(family));
    public static ToonSpec Curved(string family) => RoofStyle.Curved(family);
    /// <summary>Small inserts (displays, outlets, keys, text): fine line class so they fade with the small-part rule.</summary>
    public static ToonSpec Insert(string family) => RoofStyle.Grille(family);
    /// <summary>Self-lit sign face and its lettering, driven by the signal channel (ToonSpec.Emissive).</summary>
    public static ToonSpec Lit(string family) => RoofStyle.Grille(family) with { Emissive = SignalChannel };

    // ---- Shared ----
    public const float Bevel = 0.012f;
    public const float SmallBevel = 0.004f;
    /// <summary>How far applied details stand proud of the face they sit on; bodies are inset by this much so details stay in bounds.</summary>
    public const float Relief = 0.003f;
    /// <summary>Stroke width of lettering, as a share of the glyph height (CJK; Latin and digits use LatinStroke).</summary>
    public const float CjkStroke = 0.1f;
    public const float LatinStroke = 0.13f;

    // ---- Boiler (电开水器, WLD-INTERIORS §8.5: stainless cabinet, "小心烫伤" in red) ----
    public const float BoilerFoot = 0.08f;
    public const float BoilerTapReach = 0.075f;
    /// <summary>Tap outlet height as a share of the cabinet height (about 0.75 m on a 1.2 m cabinet).</summary>
    public const float BoilerTapRatio = 0.62f;
    public const float BoilerTrayDrop = 0.24f;
    public const float BoilerPanelHeight = 0.16f;
    public const string BoilerWarning = "小心烫伤";
    public const float BoilerWarningHeight = 0.045f;

    // ---- Water dispenser ----
    public const float DispenserBottleRatio = 0.4f;
    public const float DispenserBottleMax = 0.46f;
    public const float DispenserNiche = 0.04f;

    // ---- Hydrant box (长方形红框玻璃门, WLD-INTERIORS §8.5) ----
    public const float HydrantFrame = 0.05f;
    public const float HydrantFrameDepth = 0.035f;
    public const float HydrantGlassRecess = 0.012f;
    public const string HydrantLabel = "消火栓";
    public const float HydrantLabelRatio = 0.09f;

    // ---- Extinguisher (4 kg dry powder) ----
    public const float ExtinguisherValveRatio = 0.17f;
    public const float ExtinguisherRadiusRatio = 0.42f;

    // ---- Exit sign ("安全出口 EXIT", Chinese above English, WLD-INTERIORS §8.5) ----
    public const float ExitBorder = 0.012f;
    public const float ExitFaceProud = 0.004f;
    /// <summary>Exit lettering stroke weight relative to plain labels: the sign is read across a stairwell.</summary>
    public const float ExitWeight = 1.35f;
    public const string ExitChinese = "安全出口";
    public const string ExitLatin = "EXIT";
    /// <summary>Glow radius handed to the lamp inventory: the 0.4 m flat green halo on the stair wall (WLD-INTERIORS §8.6).</summary>
    public const float ExitGlowRange = 0.4f;
    public const float ExitGlowEnergy = 0.5f;

    // ---- Door plate ----
    public const float RoomSignHeader = 0.22f;
    public const float RoomSignTextRatio = 0.42f;

    // ---- Wall air-conditioner ----
    public const float AcOutletDepth = 0.26f;
    public const float AcDisplayWidth = 0.09f;

    // ---- Ceiling fan (1.2–1.4 m sweep, 0.3–0.5 m downrod) ----
    public const float FanMotorRadius = 0.1f;
    public const float FanMotorHeight = 0.11f;
    public const float FanCanopyRadius = 0.06f;
    public const float FanCanopyHeight = 0.06f;
    public const float FanRodRadius = 0.012f;
    public const float FanBladeWidth = 0.12f;
    public const float FanBladeThickness = 0.012f;
    public const float FanBladePitchDeg = 10f;

    // ---- Switch plate (86 type) and card reader ----
    public const float KeyGap = 0.0015f;
}
