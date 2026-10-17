using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;

namespace Campus.Gen.Parts.Street;

/// <summary>
/// Every tunable value of the WP-13 street parts in one place (WF-FLOW §4): colours, material classes, sizes, lamp
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

    // ---- 13c2b colours (all borrowed, ART-STYLE §8.5) ----
    /// <summary>Low-saturation body colours of parked cars (WLD-LIFE §3.3), picked by the placer through the part's Material.</summary>
    public static readonly string[] CarBodies = ["wall.greyWhite", "umbrella.grayblue", "roof.grey", "umbrella.beige", "umbrella.graygreen"];
    /// <summary>Car glass: the dark sky-mirroring glass of the windows (the windows reflect the sky through the shader's glass class).</summary>
    public const string CarGlass = "glass";
    /// <summary>Tyres and grips: the darkest neutral, the same near-black the crash bollard borrows (switches with <see cref="HazardBlack"/>).</summary>
    public const string Tyre = HazardBlack;
    /// <summary>Unlit lamps of a parked car (WLD-LIFE §3.4: car lights never on): clear head lamps and red tail lamps.</summary>
    public const string HeadLamp = "wall.greyWhite";
    public const string TailLamp = "equip.fireRed";
    /// <summary>Shared bicycle (WLD-LIFE §3.5, D-066): milk-white frame, electronic blue on the down tube and the number plate only, dark grey basket and lock box.</summary>
    public const string BikeWhite = "wall";
    public const string BikeBlue = "uestc.blue";
    public const string BikeDark = DarkSteel;
    /// <summary>Vending machine (WLD-LIFE §4.5): white body, blue pick-up panel; variation 1 is the low-saturation odd one of a group.</summary>
    public const string VendingWhite = "wall.greyWhite";
    public const string VendingOdd = "umbrella.graygreen";
    /// <summary>Goods behind the window: a few clean, quiet colours, no brands, no lettering on the mainline.</summary>
    public static readonly string[] Goods = ["equip.fireRed", "ground.markingYellow", "ground.turf", StreetBlue, "umbrella.beige", "wall.greyWhite"];
    /// <summary>Cold-white screens and display backs (WLD-LIFE §4.5: they glow at night, counted with the shop signs).</summary>
    public const string ScreenWhite = "wall.greyWhite";
    public static readonly ChannelId ShopGroup = new("sign.shop");
    /// <summary>Paint of the shared-bicycle bay's frame on the ground (white road marking).</summary>
    public const string GroundPaint = "ground.marking";
    /// <summary>Kerb ramp concrete and its yellow tactile warning strip (盲道提示砖).</summary>
    public const string KerbConcrete = "ground.curb";
    public const string TactileYellow = "ground.markingYellow";

    // ---- Parked car (WLD-LIFE §3.3–§3.4, §5: one box, Metal; Opus review 13c2b 21:05) ----
    /// <summary>The body's underside stands this high between the wheels (no skirt: the ground shows under the car).</summary>
    public const float CarClearance = 0.15f;
    /// <summary>Wheels: 0.65 m across, set this far in from the body side, under arches this much wider than the tyre.</summary>
    public const float WheelRadius = 0.325f;
    public const float WheelWidth = 0.2f;
    public const float WheelInset = 0.025f;
    public const float ArchRadius = 0.37f;
    /// <summary>The arch's straight foot from the axle height down to the underside spans this much along the car.</summary>
    public const float ArchFoot = 0.03f;
    /// <summary>Light hub disc on the tyre's outer face, as a share of the wheel radius.</summary>
    public const float HubRatio = 0.6f;
    /// <summary>Mirrors reach the car's full width; the body side stands this far inside it.</summary>
    public const float CarMirrorOut = 0.07f;
    /// <summary>Chamfers of the lower body: the shoulder along the belt and the hood and boot edges, and the sill tucked under the doors.</summary>
    public const float CarShoulder = 0.05f;
    public const float CarSillTuck = 0.06f;
    /// <summary>
    /// The glasshouse stands this far in from the body side at the belt (a ledge that parts the belt line from the windows) and leans
    /// in by this slope (dx/dy); its roof edge is rounded by this chamfer.
    /// </summary>
    public const float CarLedge = 0.11f;
    public const float CarTumblehome = 0.22f;
    public const float CarRoofEdge = 0.07f;
    /// <summary>
    /// Windows: panes lie this proud of the glasshouse; a body-colour strip this tall runs under the side glass, a rail this deep
    /// over it, the A and C pillars and the windscreen surround are this wide and the B pillar this wide.
    /// </summary>
    public const float PaneProud = 0.006f;
    public const float WindowSill = 0.045f;
    public const float WindowRail = 0.045f;
    public const float PillarA = 0.045f;
    public const float PillarB = 0.09f;
    /// <summary>Bumpers stand this proud of the nose and the tail, so a line parts them from the bonnet and the boot lid.</summary>
    public const float CarBumper = 0.014f;
    /// <summary>The side glass starts this far behind the windscreen's foot (the mirror sail).</summary>
    public const float MirrorSail = 0.3f;

    /// <summary>
    /// The two body shapes. Heights are metres (the roof is the part's Height); positions along the car are shares of its length:
    /// the cowl (windscreen foot) and the roof's front from the nose, the roof's rear and the rear window's foot from the tail, the
    /// axles from their ends. Nose and Tail round the ends (along the car, off the top, off the sides, off the underside). The B
    /// pillar stands at its share of the roof; the rear side glass ends <c>RearGlassTop</c> ahead of the roof's rear at its top and
    /// <c>RearGlassFoot</c> ahead of it at its sill (negative: behind).
    /// </summary>
    public static CarShape Saloon { get; } = new(Belt: 0.93f, HoodNose: 0.77f, TailTop: 0.92f, Cowl: 0.3f, RoofFront: 0.49f, RoofRear: 0.267f, RearBase: 0.144f,
        FrontAxle: 0.204f, RearAxle: 0.196f, Nose: new(0.3f, 0.16f, 0.18f, 0.07f), Tail: new(0.24f, 0.14f, 0.14f, 0.07f), BPillar: 0.42f, RearGlassTop: 0.1f, RearGlassFoot: -0.12f);

    public static CarShape Hatch { get; } = new(Belt: 0.95f, HoodNose: 0.79f, TailTop: 0.95f, Cowl: 0.305f, RoofFront: 0.495f, RoofRear: 0.075f, RearBase: 0.035f,
        FrontAxle: 0.2f, RearAxle: 0.165f, Nose: new(0.27f, 0.16f, 0.17f, 0.07f), Tail: new(0.12f, 0.09f, 0.12f, 0.07f), BPillar: 0.4f, RearGlassTop: 0.32f, RearGlassFoot: 0.12f);

    // ---- Shared bicycle (WLD-LIFE §3.5; ART-MODELING §7.8: 300–600 / 60 a bike; no collision, §5) ----
    public const float BikeWheelRadius = 0.33f;
    public const float BikeTyre = 0.028f;
    public const float BikeTube = 0.022f;
    public const float BikeSaddleY = 0.86f;
    public const float BikeBarY = 1.02f;

    // ---- Vending machine and delivery locker (WLD-LIFE §4.5; §5: one box, Metal) ----
    public const float VendingPlinth = 0.06f;
    public const float VendingCanopy = 0.05f;
    public const float VendingCanopyOut = 0.16f;
    /// <summary>The window holds this many shelves of this many goods.</summary>
    public const int VendingShelves = 3;
    public const int VendingGoods = 5;
    /// <summary>Compressor hum (WLD-LIFE §4.12, SYS-AUDIO §4.2): only within 4 m. The set waits for WP-53b's sound-set registry.</summary>
    public const string VendingHumSet = "amb.vending.hum";
    public const float VendingHumDb = 42f;
    public const float VendingHumRadius = 4f;
    public const int LockerColumns = 3;
    public const int LockerRows = 5;

    // ---- Drinking fountain (MAP-QSH-ADDITIONS §2.13: brushed steel, two push spouts, a drip tray, a "直饮水" plate, a green light) ----
    public const float FountainTop = 0.05f;
    public const string FountainTitle = "直饮水";

    // ---- Signpost (MAP-QSH-ADDITIONS §2.13: two dark grey posts 2.6 m, 3–5 arrow boards 0.9 × 0.2 m, white text with pinyin) ----
    public const float SignpostPost = 0.09f;
    public const float SignpostBoardLength = 0.9f;
    public const float SignpostBoardHeight = 0.2f;
    public const float SignpostBoardDepth = 0.04f;
    public const float SignpostBoardGap = 0.06f;
    public const float SignpostTip = 0.12f;

    // ---- Shared-bicycle bay (WLD-LIFE §3.5: a painted frame on the ground and a low standing sign, number and bicycle icon) ----
    public const float BayLine = 0.08f;
    public const float BaySlot = 0.6f;
    public const float BaySignWidth = 0.42f;
    public const float BaySignHeight = 1.25f;
    public const float BaySignPanel = 0.46f;

    // ---- Kerb ramp (丹桂路 junctions, 13c2b): a dropped kerb, 1:8 at most across the verge edge, flared sides ----
    /// <summary>The kerb ramp's public catalogue ID.</summary>
    public const string KerbRampId = PartCatalog.CurbRamp;
    public const float KerbRampFlare = 0.45f;
    public const float TactileDepth = 0.3f;

    // ---- Lettering (ART-MODELING §7.8: vector type is a part constant at every level; §7.5: printed as face geometry) ----
    /// <summary>Print quads float this far off their face (reverse-Z D32F separates 1–2 mm within 400 m, ART-MODELING §7.5).</summary>
    public const float PrintLift = 0.0015f;
    public const float CjkStroke = 0.1f;
}
