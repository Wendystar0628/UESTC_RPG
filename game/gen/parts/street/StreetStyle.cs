using Campus.Contracts;
using Campus.Contracts.Audio;

namespace Campus.Gen.Parts.Street;

/// <summary>
/// Every tunable value of the WP-13 street parts in one place (ENG-TEAM §5): colours, material classes, sizes, lamp
/// markers and seating. Initial values follow ART-MODELING §4, §7.8, WLD-LIFE §4–§5, SYS-ATLAS §6, MAP-QSH-ADDITIONS
/// §2.13, §8 and WLD-INTERIORS §4; the Opus review sets the finals. Review colours are autumn 17:30 lit colours.
/// </summary>
public static class StreetStyle
{
    // ---- Colour families. WP-13 has no palette segment yet, so every name is borrowed (ART-STYLE §8.5) ----
    /// <summary>White paint: disc street lamp, open-sign face, sign lettering, white stone sphere.</summary>
    public const string PaintWhite = "wall.greyWhite";
    /// <summary>Grey aluminium: single-arm lamp, bench legs, sign poles and frames, lamp accent rings.</summary>
    public const string GreyMetal = "frame.alu";
    /// <summary>Stainless steel: bollards, etched plate on the atlas post.</summary>
    public const string Stainless = "roof.silverMetal";
    /// <summary>Dark grey steel (WP-12 blue-grey, lit #6E737B): bin frame, atlas kiosk and post, lamp housings, accent bands.</summary>
    public const string DarkSteel = "equip.fence";
    /// <summary>
    /// Black of the crash bollard's stripe (Opus review 13:46: the stripe read dark grey-blue in daylight). No registered pair is
    /// black, so this borrows the darkest neutral one, <c>glass</c> (autumn 17:30 lit #525B52, shade #313E4E; <see cref="DarkSteel"/>
    /// reads #6E737B). Proposed for bank 1: <c>hazard.black</c>, a warm near-black; switch the name here when it is registered.
    /// </summary>
    public const string HazardBlack = "glass";
    /// <summary>Light equipment grey (WP-12): bin rim band and the grey "other waste" door.</summary>
    public const string LightSteel = "equip.grey";
    /// <summary>Warm wood (WP-10, lit #A8744A): bench slats.</summary>
    public const string Wood = "door.wood";
    /// <summary>Lamp diffusers and lenses: warm cream, self-lit through ToonSpec.Emissive.</summary>
    public const string Diffuser = "curtain.cream";
    /// <summary>Atlas kiosk face stand-in until WP-48a's baked map lands: the warm off-white of the map's building blocks.</summary>
    public const string MapPaper = "wall";
    public const string StoneWhite = "wall.greyWhite";
    public const string StoneGrey = "ground.concrete";
    /// <summary>Dark stone and rubber: atlas-post base, entrance mat.</summary>
    public const string StoneDark = "ground.asphalt";
    public const string SafetyYellow = "ground.markingYellow";
    /// <summary>
    /// Street blue for the recyclable door and the shuttle-stop board (Opus review 10:07: <c>street.blue</c>, autumn 17:30 lit
    /// #4E7FB0, ids 448–449 in the 512-colour extension). The palette loader still reads schema 1 only, so until the second bank
    /// reaches the vertices this borrows the closest registered blue; switch the name here when street.blue is registered.
    /// </summary>
    public const string StreetBlue = "uestc.techblue";
    /// <summary>Sorting doors left to right (WLD-LIFE §4.1): other, recyclable, hazardous, kitchen. Red is the campus red.</summary>
    public static readonly string[] BinDoors = ["equip.grey", StreetBlue, "equip.fireRed", "ground.turf"];
    /// <summary>Shuttle-stop board ("蓝底白字"), the same street blue.</summary>
    public const string SignBlue = StreetBlue;
    /// <summary>Open-sign green band and arrow (WLD-INTERIORS §4: sign.green is an emissive constant, so the face borrows a surface pair).</summary>
    public const string SignGreen = "ground.turf";

    public static PaletteToken Lit(string family) => new(family + ".lit");
    public static PaletteToken Shade(string family) => new(family + ".shade");
    private static ToonSpec Pair(string family) => new(Lit(family), Shade(family));

    // ---- Material classes: the non-colour ToonSpec fields (ART-STYLE §2.1, §4.1, §5.2) ----
    /// <summary>Painted bodies and boxes: chamfer highlight, building line.</summary>
    public static ToonSpec Casing(string family) => Casing(Pair(family));
    public static ToonSpec Casing(ToonSpec colours) => new(colours.Lit, colours.Shade) { Fx = ToonFx.BevelLift, Wet = 0.5f };
    /// <summary>Round poles and curved metal: two-and-a-half steps, smooth normals, the anisotropic band of ART-STYLE §5.2.</summary>
    public static ToonSpec Pole(string family) => Pole(Pair(family));
    public static ToonSpec Pole(ToonSpec colours) => new(colours.Lit, colours.Shade) { Steps = ShadeSteps.TwoHalf, Normal = ToonNormal.Smooth, Fx = ToonFx.Rim | ToonFx.AnisoBand, Wet = 0.4f };
    /// <summary>Thin metal: bars, legs, frames.</summary>
    public static ToonSpec Metal(string family) => new(Lit(family), Shade(family)) { Fx = ToonFx.Rim | ToonFx.AnisoBand, Line = LineClass.Fine, Wet = 0.4f };
    /// <summary>Wood slats: chamfer highlight, fine line so the slat gaps read without thickening.</summary>
    public static ToonSpec Slat(string family) => new(Lit(family), Shade(family)) { Fx = ToonFx.BevelLift, Line = LineClass.Fine, Wet = 0.6f };
    /// <summary>Stone: chamfer highlight; round stone takes smooth normals and two-and-a-half steps.</summary>
    public static ToonSpec Stone(ToonSpec colours, bool round) => round
        ? new(colours.Lit, colours.Shade) { Steps = ShadeSteps.TwoHalf, Normal = ToonNormal.Smooth, Wet = 0.6f }
        : new(colours.Lit, colours.Shade) { Fx = ToonFx.BevelLift, Wet = 0.6f };
    /// <summary>Flat boards and sign faces: no chamfer lift.</summary>
    public static ToonSpec Board(string family) => new(Lit(family), Shade(family)) { Wet = 0.5f };
    /// <summary>Raised lettering and pictograms: fine line, out of the shadow pass.</summary>
    public static ToonSpec Ink(string family) => new(Lit(family), Shade(family)) { Line = LineClass.Fine, Wet = 0.5f };
    /// <summary>Self-lit lens on a light channel (ToonSpec.Emissive, ENG-PERF §10.3).</summary>
    public static ToonSpec Lens(string family, ChannelId channel) => new(Lit(family), Shade(family)) { Emissive = channel, Line = LineClass.Fine, Wet = 0.3f };
    /// <summary>Flat things lying on the ground (mat).</summary>
    public static ToonSpec Mat(string family) => new(Lit(family), Shade(family)) { Line = LineClass.Fine, Wet = 0.8f };

    /// <summary>
    /// A caller that sets <see cref="Campus.Contracts.Parts.BoxPartParams.Material"/> away from the record default chooses the
    /// body colour (stone spheres and bollards follow the site's stone or marking paint); otherwise the style colour stands.
    /// </summary>
    public static ToonSpec BodyColour(ToonSpec requested, string fallback) =>
        requested.Lit.Value == "wall.lit" ? Pair(fallback) : new(requested.Lit, requested.Shade);

    // ---- Shared ----
    public const int RoundSegments = 16;
    public const int RoundSegmentsSmall = 12;
    public const int RoundSegmentsFar = 8;
    public const float BoundsPad = 0.002f;
    /// <summary>Overlap that hides joints between touching pieces (ART-MODELING §7.4: 2–5 mm into the body).</summary>
    public const float Joint = 0.003f;
    /// <summary>Post colliders reach below grade (SYS-WALK §13: Y0 = lowest ground in the footprint − 0.1 m).</summary>
    public const float PostColliderSink = 0.1f;

    // ---- Lamp markers (MAP-QSH-ADDITIONS §8: grade 1 / 0.75 / 0.5, warm colour; schedule slots in lighting-schedule.json) ----
    public static readonly ChannelId FirstGroup = new("lamp.first");
    public static readonly ChannelId StreetGroup = new("lamp.street");
    public static readonly ChannelId Landscape = new("lamp.landscape");
    public static readonly ChannelId Signal = new("sign.signal");
    public static readonly PaletteToken LampWarm = new("lamp.warm");
    public const float GradeOneEnergy = 1f, GradeTwoEnergy = 0.75f, GradeThreeEnergy = 0.5f;
    /// <summary>Ground pool radii: grade 1 lamps at 25–30 m nearly join (50–70% cover), grade 2 at 30–40 m leave dark between.</summary>
    public const float GradeOneRange = 8.5f, GradeTwoRange = 7f;
    /// <summary>The marker sits this far under the lens, so the halo hugs the lamp head and never hides it (ART-FIDELITY §3.6).</summary>
    public const float MarkerDrop = 0.04f;

    // ---- Street lamp (WLD-LIFE §4.2; ART-MODELING §4: pole 0.2–0.28 m, head ×1.3–1.5) ----
    /// <summary>Base sleeve: the thicker foot of the pole, with its grey ring as the one accent band.</summary>
    public const float LampSleeveRadius = 0.15f;
    public const float LampSleeveHeight = 0.6f;
    public const float LampFlangeRadius = 0.2f;
    public const float LampFlangeHeight = 0.035f;
    public const float LampRingHeight = 0.05f;
    public const float LampPoleBaseRadius = 0.115f;
    public const float LampPoleTopRadius = 0.1f;
    /// <summary>Disc head: thickness, rim and the diffuser inset from the rim.</summary>
    public const float DiscHeadThickness = 0.2f;
    public const float DiscRimHeight = 0.045f;
    public const float DiscChamfer = 0.03f;
    public const float DiscLensInset = 0.06f;
    public const float DiscLensDrop = 0.016f;
    public const float DiscMinDiameter = 0.4f;
    /// <summary>Single-arm LED: arm tube, head box and its upward tilt.</summary>
    public const float ArmRadius = 0.045f;
    public const float ArmRiseDeg = 8f;
    public const float ArmHeadLength = 0.62f;
    public const float ArmHeadWidth = 0.26f;
    public const float ArmHeadThickness = 0.085f;
    public const float ArmMinReach = 0.35f;
    public const float ArmDrop = 0.22f;

    // ---- Entrance lamp (WLD-INTERIORS §4: one door lamp per open entrance, on the building's light channel) ----
    public const float DoorLampBackPlate = 0.02f;
    public const float DoorLampHousingRatio = 0.82f;
    public const float DoorLampLensRatio = 0.6f;
    public const float DoorLampRange = 2.4f;

    // ---- Bench (WLD-LIFE §4.7; seat 0.45 m is a human anchor, never scaled; ART-MODELING §4: 3–4 slats) ----
    public const float SeatHeight = 0.45f;
    public const float SeatDepth = 0.42f;
    public const int SeatSlats = 3;
    public const int BackSlats = 2;
    public const float SlatThickness = 0.035f;
    public const float SlatGap = 0.012f;
    public const float SlatBevel = 0.006f;
    public const float BackSlatHeight = 0.12f;
    public const float BackTiltDeg = 14f;
    public const float BackRise = 0.08f;
    public const float BenchLegInset = 0.24f;
    public const float BenchLeg = 0.05f;
    /// <summary>Seat pitch for the WP-54a seat places; edge margin keeps a sitter off the bench ends.</summary>
    public const float SeatPitch = 0.6f;
    public const float SeatEdgeMargin = 0.15f;
    public const FootMat BenchFoot = FootMat.WoodFloor;

    // ---- Four-stream sorting bin (WLD-LIFE §4.1; ART-MODELING §4: rounded box, a colour band round the mouth) ----
    public const float BinPlinth = 0.06f;
    public const float BinTop = 0.05f;
    public const float BinTopOverhang = 0.02f;
    public const float BinFrame = 0.05f;
    public const float BinMullion = 0.04f;
    public const float BinDoorProud = 0.012f;
    public const float BinHoodHeight = 0.07f;
    public const float BinHoodProud = 0.035f;
    public const float BinBevel = 0.02f;

    // ---- Bollard and stone sphere (WLD-LIFE §4.4; MAP-QSH-SOUTH §1.4) ----
    public const float BollardFlange = 0.015f;
    public const float BollardFlareHeight = 0.035f;
    public const int BollardSegments = 10;
    public const int BollardSegmentsFar = 6;
    public const float BollardBandHeight = 0.06f;
    public const float BollardBandDrop = 0.17f;
    /// <summary>Crash bollard: one broad black stripe, its top at this share of the height (黄黑相间: yellow, black, yellow).</summary>
    public const float CrashBandTop = 0.62f;
    public const float CrashBandHeight = 0.14f;
    public const float SphereSink = 0.012f;
    public const float SpherePlinthRatio = 0.62f;
    public const float SpherePlinthHeight = 0.03f;
    public const FootMat StoneFoot = FootMat.StonePaving;
    public const FootMat MetalFoot = FootMat.Metal;

    // ---- Shuttle stop (WLD-LIFE §4.3: blue board, white text, grey pole) ----
    public const float StopPoleRadius = 0.05f;
    public const float StopBoardHeight = 0.8f;
    public const float StopBoardTopGap = 0.12f;
    public const float StopBoardDepth = 0.05f;
    public const float StopBorder = 0.035f;
    public const string StopTitle = "班车";

    // ---- Atlas kiosk and post (SYS-ATLAS §6, MAP-QSH-ADDITIONS §2.13) ----
    public const float KioskPost = 0.12f;
    public const float KioskPostInset = 0.06f;
    public const float KioskCanopy = 0.08f;
    /// <summary>The canopy projects this far in front of the face (SYS-ATLAS §6: 0.6 m).</summary>
    public const float KioskOverhang = 0.6f;
    public const float KioskFaceWidth = 1.6f;
    public const float KioskFaceHeight = 1f;
    public const float KioskFaceSill = 0.9f;
    public const float KioskFrame = 0.045f;
    public const float KioskFrameDepth = 0.07f;
    public const float KioskLightRange = 1.6f;
    public const float PostSection = 0.2f;
    public const float PostSlantDeg = 30f;
    public const float PostPlinthHeight = 0.12f;
    public const float PostPlateWidth = 0.18f;
    public const float PostPlateHeight = 0.14f;
    public const float PostLampHeight = 0.85f;
    public const float PostLampRadius = 0.02f;
    /// <summary>Ground pool of the post lamp: about 1.5 m across (SYS-ATLAS §6).</summary>
    public const float PostLampRange = 0.75f;

    // ---- Open entrance (WLD-INTERIORS §4, WLD-LIFE §4.11) ----
    public const float MatBorder = 0.04f;
    public const float SignBandRatio = 0.38f;
    public const float SignFaceInset = 0.012f;
    public const float SignPost = 0.05f;
    public const float SignBaseHeight = 0.03f;
    /// <summary>Free-standing sign: the panel takes this share of the total height, at the top.</summary>
    public const float SignPanelRatio = 0.42f;
    public const string OpenTitle = "开放";

    // ---- Lettering (ART-MODELING §7.8: vector type is a part constant at every level; §7.5: printed as face geometry) ----
    /// <summary>Print quads float this far off their face (reverse-Z D32F separates 1–2 mm within 400 m, ART-MODELING §7.5).</summary>
    public const float PrintLift = 0.0015f;
    public const float CjkStroke = 0.1f;
}
