using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.Zones.CenterEast;

/// <summary>
/// Every tunable value of the center-east zone in one place, for review (WF-FLOW §4): materials as colour names, and
/// the dimensions of each site. Coordinates are world (x, z) in metres from the OSM footprints in campus.json; local frames
/// are described per block. Values marked "初值" are starting values awaiting the main session's call. 21b–21d add their
/// own nested classes after <see cref="Pinxue"/>.
/// </summary>
public static class CenterEastTune
{
    public const int BudgetTrianglesL0 = 60_000;
    public const int BudgetTrianglesL1 = 30_000;
    public const int BudgetDrawCalls = 16;

    /// <summary>
    /// Window numbering (contract-29): the light channel (LightChannelCatalog) and window-card type (WLD-INTERIORS §5.1: 3 corridor,
    /// 5 office, 6 lab) of each grid the zone declares. The research blocks follow the lab curve with W-LAB : W-OFF = 7 : 3
    /// (MAP-QSH-CENTER-EAST §1.2), 品学楼's frame beam and link bridges the teaching curve with corridor cards (初值).
    /// </summary>
    public static class Windows
    {
        public static readonly ChannelId Research = new("window.lab"), Teaching = new("window.teach");
        public const byte Classroom = 1, Corridor = 3, Office = 5, Lab = 6;
        /// <summary>Tenths of the research windows that show an office card instead of a lab.</summary>
        public const int OfficeShare = 3;
        /// <summary>文科楼 (teaching curve): classrooms, with this many tenths of its windows offices (初值).</summary>
        public const int WenkeOfficeShare = 4;
    }

    /// <summary>Materials (ART-STYLE §8 colour names only).</summary>
    public static class Look
    {
        private static ToonSpec S(string family, ToonFx fx = ToonFx.None, LineClass line = LineClass.Building, float wet = 0.6f) =>
            new(new(family + ".lit"), new(family + ".shade")) { Fx = fx, Line = line, Wet = wet };

        /// <summary>Renovated teaching-block frame and walls: grey-white (MAP-QSH-CENTER-EAST §2.8).</summary>
        public static readonly ToonSpec Wall = S("wall.greyWhite", ToonFx.BevelLift);
        /// <summary>Copings, bands, plaque frames: light beige stone, fine lines so trims do not hatch the facade.</summary>
        public static readonly ToonSpec Trim = S("stone.beige", ToonFx.BevelLift, LineClass.Fine);
        /// <summary>Spandrel stone and the inscription plaque ground (浅米灰石材).</summary>
        public static readonly ToonSpec Stone = S("stone.beige", ToonFx.BevelLift);
        /// <summary>Tree-pit kerbs, seat walls, bed kerbs: light granite.</summary>
        public static readonly ToonSpec Granite = S("stone.beige", ToonFx.BevelLift, LineClass.Fine);
        /// <summary>Plinths and the cascade fountain: dark grey granite (深灰勒脚、深色花岗岩).</summary>
        public static readonly ToonSpec DarkGranite = S("roof.grey", ToonFx.BevelLift, LineClass.Fine, 0.8f);
        public static readonly ToonSpec Paving = S("ground.paver", ToonFx.None, LineClass.None);
        /// <summary>Tree-pit and planter soil: the horizontal face that catches leaves, petals and snow (MAP-QSH-ADDITIONS R3).</summary>
        public static readonly ToonSpec Soil = S("base.warmBrown", ToonFx.None, LineClass.None);
        /// <summary>Planter beds behind the ramp seat walls: a low planted surface until WP-23 fills them with shrubs.</summary>
        public static readonly ToonSpec Bed = S("ground.grassDark", ToonFx.None, LineClass.None);
        public static readonly ToonSpec RoofSlab = S("roof.grey", ToonFx.Mottle, LineClass.Building, 0.85f);
        /// <summary>Grey aluminium window frames and mullions (窗框灰铝).</summary>
        public static readonly ToonSpec Metal = S("frame.alu", ToonFx.BevelLift, LineClass.Fine);
        public static readonly ToonSpec Glass = new(new("glass.lit"), new("glass.shade"))
        { Kind = ToonKind.Glass, Fx = ToonFx.GlassStreak | ToonFx.Glare, Wet = 0 };
        /// <summary>Research family trims (cornice, sills, portals, slab bands): grey-white against the warm stone.</summary>
        public static readonly ToonSpec LightTrim = S("wall.greyWhite", ToonFx.BevelLift, LineClass.Fine);
        /// <summary>Roof-garden timber frames (轻木构架).</summary>
        public static readonly ToonSpec Timber = S("panel.brown", ToonFx.BevelLift, LineClass.Fine);
        /// <summary>
        /// The two bronze figures (§1.3–§1.4), after 吴立人像's accepted borrowing (18c1 review 1): the palette is full, so the
        /// deep bronze borrows tree.bark until the 320–321 bronze pair is registered; the verdigris in the recesses (ART-STYLE
        /// §10 青铜像) waits for that pair too. Curved metal: two-and-a-half steps, rim and anisotropic band (ART-STYLE §2, §5).
        /// </summary>
        public static readonly ToonSpec Bronze = new(new("tree.bark.lit"), new("tree.bark.shade"))
        {
            Fx = ToonFx.Rim | ToonFx.AnisoBand,
            Steps = ShadeSteps.TwoHalf,
            Normal = ToonNormal.Smooth,
            Wet = 0.4f,
        };
        /// <summary>林为干像's dark pedestal (深色基座): the darkest neutral name, as 吴立人像's black stone (322–323 later).</summary>
        public static readonly ToonSpec DarkStone = S("glass", ToonFx.BevelLift, LineClass.Building, 0.9f);
        /// <summary>麦克斯韦像's light grey granite pedestal (浅灰花岗岩) and the stele's base.</summary>
        public static readonly ToonSpec PaleGranite = S("wall.greyWhite", ToonFx.BevelLift);
        /// <summary>The 科研楼 stele's rough-hewn white stone (粗凿白石, the campus stone-carving family).</summary>
        public static readonly ToonSpec WhiteStone = S("wall", ToonFx.BevelLift);
        /// <summary>The equation stone (圆浑的景石): warm natural stone with smooth normals, no bevel lift on a curved body.</summary>
        public static readonly ToonSpec FieldStone = S("stone.beige", ToonFx.None) with { Normal = ToonNormal.Smooth };
        /// <summary>Filled lines of a carved inscription: the darkest neutral (ink green-grey), no outline round each line.</summary>
        public static readonly ToonSpec Ink = S("glass", ToonFx.None, LineClass.None, 0.9f);
        /// <summary>文科楼's ribbon mullions: dark frames so the glass between the floor bands reads as one continuous dark band.</summary>
        public static readonly ToonSpec DarkFrame = S("glass", ToonFx.None, LineClass.Fine, 0.8f);
        /// <summary>文科楼's local yellow-green panels (#aaa961, ART-STYLE §8: only on this building).</summary>
        public static readonly ToonSpec YellowGreen = S("panel.yellowGreen", ToonFx.BevelLift);
        /// <summary>
        /// The green cloth wound round the friendship tree's trunk (绿布): borrows the turf green, a clear green that does not
        /// turn with the seasons (the grey-green fabric name read as grey on the trunk).
        /// </summary>
        public static readonly ToonSpec Cloth = S("ground.turf", ToonFx.None, LineClass.Fine, 0.8f) with { Normal = ToonNormal.Smooth };
        /// <summary>The granite landscape stone beside the friendship tree: pale grey, rounded body, smooth normals.</summary>
        public static readonly ToonSpec GreyStone = S("wall.greyWhite", ToonFx.None) with { Normal = ToonNormal.Smooth };
        /// <summary>The gold emblem inlaid in that stone (嵌金色原创徽记, ART-STYLE §11): curved metal, rim and anisotropic band.</summary>
        public static readonly ToonSpec Gold = new(new("uestc.gold.lit"), new("uestc.gold.shade"))
        {
            Fx = ToonFx.Rim | ToonFx.AnisoBand,
            Steps = ShadeSteps.TwoHalf,
            Normal = ToonNormal.Smooth,
            Wet = 0.3f,
        };
    }

    /// <summary>
    /// 品学楼 (MAP-QSH-CENTER-EAST §2). The A, B and C wings keep WP-14's facade rules on the +1.5 m platform
    /// (center-east.terrain.json); this zone builds the gate frame, the two link bridges, the forecourt, the fountain's
    /// stone tiers and the curved end bay.
    /// </summary>
    public static class Pinxue
    {
        /// <summary>The 品学楼 entity: its parts are the gate frame bar and the A–C, B–C link bridges.</summary>
        public const string Complex = "qsh:relation:11043355";
        public const string WingA = "qsh:relation:11043352";
        public const string WingB = "qsh:relation:11043353";
        public const string FrameBar = "osm:way:797290174";
        public const string BridgeAC = "osm:way:797290183";
        public const string BridgeBC = "osm:way:797290182";
        /// <summary>The forecourt (门框前广场) and its holes: the lawn panel round the fountain, the round bed on the ramp.</summary>
        public const string Forecourt = "qsh:source:relation:11043356";
        /// <summary>Design level of the platform (MAP-QSH-TERRAIN §10): only for probe eye heights, which have no terrain to read.</summary>
        public const float PlatformY = 1.5f;

        /// <summary>
        /// Gate frame (§2.2). Frame: origin at the bar's north-west corner on A's south wall, X south along the bar to B's
        /// north wall (37.7 m), local +Z west out to the forecourt; the bar is 6.9 m deep (z −6.9…0).
        /// </summary>
        public static readonly Vector2 FrameNorthWest = new(161.82f, -79.33f);
        public static readonly Vector2 FrameSouthWest = new(167.16f, -41.98f);
        public const float FrameDepth = 6.9f;
        /// <summary>A and B: 4 storeys, 16 m. The open passage is storeys 1–2, the beam bridge storeys 3–4.</summary>
        public const float StoreyHeight = 4.0f;
        public const float BeamBottom = 8.0f, BeamTop = 16.0f;
        /// <summary>Four square columns, one row on the bar's centre line, three bays with the middle one widest (四柱三间, 初值).</summary>
        public const float ColumnSize = 2.4f, ColumnPlinth = 0.45f;
        public static readonly float[] ColumnX = [2.6f, 13.0f, 24.72f, 35.12f];
        /// <summary>Inscription plaque on the beam's forecourt face; the calligraphy waits for the scene text atlas (初值).</summary>
        public const float PlaqueWidth = 26f, PlaqueHeight = 3.0f, PlaqueProud = 0.08f;
        /// <summary>Soffit downlights over the passage (lit by WP-39 with the frame's light channel).</summary>
        public const float DownlightPitch = 3.2f, DownlightRadius = 0.16f;

        /// <summary>Link bridges A–C and B–C (§2.2): storeys 2–3 over the paths between the wings (初值).</summary>
        public const float BridgeBottom = 4.0f, BridgeTop = 12.0f, BridgeColumnRadius = 0.3f;

        /// <summary>
        /// Low stepped cascade (§2.3; water by WP-24, WLD-WATER §9; 10:01 review): three nested rectangular pools of light
        /// stone, centred in the forecourt's 14.2 × 7.2 m lawn panel 22 m in front of the frame, long side along the bar.
        /// Each tier's thin water sheet spills over its coping into the pool below. Halves are (along the bar, toward the
        /// forecourt); heights are above the lawn and stay under 0.9 m so the cascade lies low on the grass (初值).
        /// </summary>
        public static readonly Vector2 Fountain = new(142.38f, -58.10f);
        public static readonly Vector2 BasinHalf = new(5.5f, 2.5f);
        public const float BasinCoping = 0.35f, BasinRim = 0.30f, BasinWater = 0.24f;
        public static readonly Vector2 MiddleHalf = new(4.0f, 1.7f);
        public const float MiddleRim = 0.55f, MiddleWater = 0.49f;
        public static readonly Vector2 TopHalf = new(2.5f, 0.9f);
        public const float TopRim = 0.80f, TopWater = 0.74f;
        /// <summary>Coping width of the two upper tiers; every pool is this deep below its water line.</summary>
        public const float TierCoping = 0.25f, PoolDepth = 0.14f;
        public const float PanelKerbWidth = 0.3f, PanelKerbHeight = 0.15f;

        /// <summary>Square tree pits (方形树池) in two rows either side of the axis, in the frame's local (x, z).</summary>
        public static readonly float[] TreePitRowsX = [4.0f, 33.8f];
        public static readonly float[] TreePitZ = [9f, 15f, 21f, 27f, 33f];
        public const float TreePitSize = 1.8f, TreePitKerb = 0.22f, TreePitHeight = 0.42f, TreePitSoil = 0.32f;

        /// <summary>Round bed on the ramp (the forecourt's circular hole): a 0.45 m granite seat ring round one tree.</summary>
        public static readonly Vector2 RoundBed = new(101.68f, -53.13f);
        public const float RoundBedRadius = 3.45f, SeatWidth = 0.45f, SeatHeight = 0.45f;

        /// <summary>
        /// Ramp seat walls (MAP-QSH-TERRAIN §10.2): 0.45 m planter walls on both long sides of the 5 % ramp plaza,
        /// frame-local x of each wall's plaza face, from z0 (top of the ramp) to z1 (foot), stepping per segment.
        /// </summary>
        public static readonly float[] RampWallX = [3.6f, 33.7f];
        public const float RampWallZ0 = 47f, RampWallZ1 = 77f, RampWallSegment = 6f;
        public const float RampWallThickness = 0.5f, RampBedWidth = 1.2f, RampBedKerb = 0.2f;

        /// <summary>
        /// Curved end of the right wing (§2.4: 右翼端部是一个弧形体量), read from the forecourt as the far right: a curved
        /// curtain-wall bay over B's diagonal south-west lobby (osm:way:797290169), bulging toward the river (初值).
        /// </summary>
        public static readonly Vector2 ArcFrom = new(156.26f, 29.09f);
        public static readonly Vector2 ArcTo = new(168.75f, 38.45f);
        public static readonly Vector2 ArcBulge = new(-0.6f, 0.8f);
        public const float ArcSagitta = 3.5f, ArcHeight = 19.5f;
        public const int ArcStoreys = 5;

        /// <summary>
        /// A-wing hall entrance px.a.hall (interiors.json): door centre and the diagonal facade it sits in (the Q4 probe
        /// stands there). The entrance kit at this door and at the side door under the passage is WP-13's (12:15 ruling).
        /// </summary>
        public static readonly Vector2 HallDoor = new(146.5f, -84.9f);
        public static readonly Vector2 HallFacadeFrom = new(140.64f, -89.21f);
        public static readonly Vector2 HallFacadeTo = new(152.44f, -80.5f);
    }

    /// <summary>
    /// 中部科研区 (MAP-QSH-CENTER-EAST §1): 基础实验大楼 (科 A) and 一号科研实验楼 (科 B, 科 C), claimed and built by this zone
    /// as the renovated research family: warm light stone frame over recessed glass, a glazed ground storey, windows
    /// grouped in pairs by giant pilasters, grey-white cornice and sills, dark plinth (MAP-QSH-RENOVATION E-02, E-03).
    /// All blocks share one plan direction; positions are in its local frame R (u along <see cref="Axis"/>, w = (−Axis.y,
    /// Axis.x), roughly south), read from the OSM footprints in campus.json.
    /// </summary>
    public static class Research
    {
        public const string KeA = "qsh:relation:11127418";
        public const string KeB = "qsh:relation:11127417";
        public const string KeC = "qsh:way:806848385";
        /// <summary>Parts: the コ-shaped main block, the west and middle blocks with their link strips, and the lens halls.</summary>
        public const string KeAMain = "osm:way:806848386", KeAWest = "osm:way:806848387", KeAHall = "osm:way:806848388";
        public const string KeBMain = "osm:way:806848375", KeBMiddle = "osm:way:806848378", KeBWest = "osm:way:806848379", KeBHall = "osm:way:806848381";
        public const string KeCBar = "osm:way:806848376", KeCLink = "osm:way:806848377", KeCHall = "osm:way:806848380";

        public static readonly Vector2 Axis = new(0.9898f, -0.1423f);

        /// <summary>
        /// Five storeys everywhere (§1.2: OSM's 6 and 1 levels are wrong): a 4.8 m glazed ground storey and four of 4.2 m,
        /// cornice and parapet to 22.9 m. The ground floor stands 0.45 m above the pad, three risers at each entrance.
        /// </summary>
        public const float GroundStorey = 4.8f, Storey = 4.2f, FloorLevel = 0.45f;
        public const int Storeys = 5;
        public const float Bay = 3.9f, Pier = 1.1f, GroundSill = 1.0f, GroundWindow = 3.2f, Sill = 0.95f, Window = 2.4f;
        public const int PilasterEvery = 2;
        /// <summary>Edges at least this long that face away from the building's courtyards get the pilasters.</summary>
        public const float FrontMin = 20f, CourtyardReach = 45f;

        /// <summary>
        /// Entrance portal on a block face (§1.2 门厅朝东): a 7.2 m opening cut through the two lower storeys up to the
        /// second-floor sill (a two-storey glazed lobby bay that reads from the road), framed by grey-white jambs and a
        /// deep head that is also the canopy; glass doors with mullioned glazing round them on the glass plane; three
        /// risers out to the pad.
        /// </summary>
        public const float PortalHalfWidth = 3.6f, PortalJamb = 1.0f, PortalDepth = 1.6f, PortalHead = 1.2f;
        public const int PortalStoreys = 2;
        public const float DoorWidth = 3.0f, DoorHeight = 3.0f, TreadDepth = 0.38f;

        /// <summary>科 A's door on the east face, where the footway from 银杏大道 arrives (local u, w).</summary>
        public static readonly Vector2 KeADoor = new(37.2f, -34.2f);
        /// <summary>科 B's door on the east face, on the axis of its lens hall, facing the statue lawn (初值).</summary>
        public static readonly Vector2 KeBDoor = new(72.1f, -143.8f);

        /// <summary>
        /// Lens halls (透镜形弧厅): a glass hall whose back stands against a block and whose front is one circular arc.
        /// Outline: the local polyline Back, closed by an arc from its last point to its first, bulging Sagitta metres away
        /// from it. The OSM outlines carry small hooks at the arc ends that this mainline smooths away (初值).
        /// </summary>
        public static readonly Vector2[] KeAHallBack = [new(18.2f, -26.4f), new(26.0f, -26.4f), new(26.0f, -49.2f), new(18.2f, -49.2f)];
        public const float KeAHallSagitta = 3.9f;
        public static readonly Vector2[] KeBHallBack = [new(50.0f, -133.2f), new(60.0f, -133.2f), new(60.0f, -154.3f), new(50.0f, -154.3f)];
        public const float KeBHallSagitta = 3.2f;
        /// <summary>科 C's two-storey hall between C's bar and B's north arm, its arc facing the statue lawn: C 区大厅 and its door.</summary>
        public static readonly Vector2[] KeCHallBack = [new(66.0f, -179.0f), new(58.7f, -179.0f), new(58.7f, -213.6f), new(72.0f, -213.6f)];
        public const float KeCHallSagitta = 9.5f;
        /// <summary>Hall heights to the coping: A and B one tall storey, C two storeys (OSM: 1, 1, 2 levels).</summary>
        public const float HallStorey = 4.5f, HallAttic = 1.0f;
        public const float HallDoorWidth = 2.4f, HallDoorHeight = 2.7f, HallCanopyDepth = 1.8f;

        /// <summary>科 B's east wing rises one storey higher (§1.2 东段局部 6 层): a set-back attic storey over the east bar.</summary>
        public static readonly Vector2 KeBAtticMin = new(60.0f, -177.9f), KeBAtticMax = new(70.9f, -107.4f);
        public const float AtticStorey = 4.2f;

        /// <summary>Stair penthouses on the roofs (B7 roof relief every 20–30 m), local centres; all 5.0 × 6.0 m, 3.4 m high.</summary>
        public static readonly Vector2[] KeAStairs = [new(-38.0f, -63.3f), new(-38.0f, -9.6f), new(31.6f, -60.5f), new(-50.5f, -39.0f)];
        public static readonly Vector2[] KeBStairs = [new(-40.0f, -170.4f), new(-40.0f, -116.3f), new(-0.8f, -144.4f), new(-54.5f, -144.7f)];
        public static readonly Vector2[] KeCStairs = [new(5.0f, -222.7f), new(0.0f, -197.8f)];
        public static readonly Vector2 StairSize = new(5.0f, 6.0f);
        public const float StairHeight = 3.4f;

        /// <summary>Plant enclosures (屋面设备进围挡, MAP-QSH-ADDITIONS §2.9): dark perforated fence round three heat pumps.</summary>
        public static readonly Vector2[] KeAPlant = [new(0.0f, -63.3f)];
        public static readonly Vector2[] KeBPlant = [new(10.0f, -116.3f)];
        public static readonly Vector2[] KeCPlant = [new(60.0f, -222.7f)];
        public static readonly Vector2 PlantSize = new(8.0f, 4.6f);
        public const float PlantFence = 1.8f;

        /// <summary>
        /// 科 A's roof fence and tower tank (MAP-QSH-ADDITIONS §2.16): a 2 m grey aluminium bar fence round the east wing's
        /// roof and a panel tank on a steel stand beside the stair penthouse; skyline only, the roof stays closed.
        /// </summary>
        public static readonly Vector2 KeAFenceMin = new(27.4f, -70.8f), KeAFenceMax = new(35.8f, -2.0f);
        public const float KeAFenceHeight = 2.0f, KeAFencePosts = 2.5f;
        public static readonly Vector2 KeATank = new(31.6f, -53.6f);
        public static readonly Vector3 KeATankSize = new(3.0f, 3.8f, 3.0f);

        /// <summary>一号科研楼 antennas (§2.9 科研楼天线): white dishes on 科 B's attic roof, facing east to catch the sunrise (初值).</summary>
        public static readonly Vector2[] KeBDishes = [new(65.5f, -168.0f), new(65.5f, -158.0f), new(65.5f, -149.0f)];
        public static readonly float[] KeBDishDiameters = [2.4f, 3.0f, 1.8f];

        /// <summary>一号科研楼 roof garden (§2.9 屋顶实验花园) on 科 C's bar: a light timber frame over rows of planters.</summary>
        public static readonly Vector2 KeCGardenMin = new(21.5f, -229.4f), KeCGardenMax = new(43.5f, -216.0f);
        public const float GardenPostPitch = 3.14f, GardenFrameHeight = 2.4f;
    }

    /// <summary>
    /// 中部科研区 monuments (MAP-QSH-CENTER-EAST §1.3–§1.5): 林为干像 and 麦克斯韦像 on the statue lawn east of 科 B/C, the
    /// equation stone beside Maxwell and the 科研楼 stele by 银杏大道 between 科 A and 科 B. The OSM points move less than
    /// 4 m (minor, no reshape); the equation stone and Maxwell's granite apron are design additions in overlay/center-east.d/.
    /// Facings are bearings (0 north, 90 east). The figures are sculpture volumes after 吴立人像 (18c1 review): clothes,
    /// posture and hair as masses, the face one smooth volume with no features or likeness (CORE-INDEX §3). Inscriptions wait
    /// for the scene text atlas (contract-26): the pedestal and stele faces stay blank, the equation stone carries four
    /// filled line blocks where the four equations go (初值).
    /// </summary>
    public static class Monuments
    {
        public const string Lin = "qsh:node:5261487375", Maxwell = "qsh:node:7878394896", Stele = "qsh:node:7878386482";
        /// <summary>The paved plaza in front of 科 B's door; its tree island on the door's axis carries 林为干像.</summary>
        public const string Plaza = "qsh:source:relation:11583698";

        /// <summary>
        /// 林为干像 (§1.4): bronze bust in a suit on a dark pedestal at the east tip of the tree island, on 科 B's door axis
        /// (research frame w = −143.8, 0.9 m from the OSM point), facing out along the axis to 银杏大道 with the big tree behind.
        /// Bust scale 1.35 × a 2 m figure's head and shoulders.
        /// </summary>
        public static readonly Vector2 LinAt = new(89.80f, -158.19f);
        public const float LinFacing = 81.8f, LinScale = 1.35f;
        public const float LinPlinth = 0.25f, LinShaft = 1.2f, LinCap = 0.12f;
        /// <summary>Granite kerb round the tree island (the plaza's hole), like the 品学楼 lawn panel's.</summary>
        public const float IslandKerbWidth = 0.25f, IslandKerbTop = 0.2f;

        /// <summary>
        /// 麦克斯韦像 (§1.3): full-length bronze, frock coat, full beard, a scroll held to the chest, on a light grey granite
        /// pedestal; at the OSM point near the lawn's east edge, turned south-east toward the plaza corner where R5 arrives.
        /// Figure 1.25 × a 2 m model, about 2.5 m, as 吴立人像.
        /// </summary>
        public static readonly Vector2 MaxwellAt = new(93.84f, -188.77f);
        public const float MaxwellFacing = 130f, MaxwellScale = 1.25f;
        /// <summary>Top of the pedestal above the ground (base slab, die, cap; 5 cm sunk).</summary>
        public const float MaxwellPedestalTop = 1.95f;

        /// <summary>
        /// Granite apron (stop point) round Maxwell and the stone, research-frame rectangle (u, w) from the lawn's east edge
        /// where footway 572670136 runs; a few centimetres proud of the lawn with a light kerb.
        /// </summary>
        public static readonly Vector2 ApronMin = new(116.2f, -176.4f), ApronMax = new(124.4f, -167.6f);
        /// <summary>Apron top above the terrain (2 cm over WP-15's paving level, 7 cm over the lawn) and its kerb.</summary>
        public const float ApronTop = 0.17f, ApronKerb = 0.2f;

        /// <summary>
        /// Equation stone (§1.3 圆浑的景石): at Maxwell's front right ((−2.8, 1.4) in his frame), its dressed face turned to
        /// the R5 approach. Length × depth × height, and the face: centre height, distance in front of the stone's axis, tilt
        /// back from vertical.
        /// </summary>
        public static readonly Vector2 StoneAt = new(93.11f, -185.73f);
        public const float StoneFacing = 160f;
        public static readonly Framework.BoulderShape Stone = new(2.4f, 1.2f, 1.35f, 0.72f, 0.32f, 18f * (MathF.PI / 180f), -0.12f) { Rise = -0.22f };
        /// <summary>Inscription lines (bottom to top, the four equations): length, and the line height and pitch on the face.</summary>
        public static readonly float[] StoneLines = [0.95f, 0.78f, 0.42f, 0.66f];
        public const float StoneLineHeight = 0.07f, StoneLinePitch = 0.15f, StoneLineBottom = -0.24f;

        /// <summary>
        /// 科研楼碑 (§1.5): a rough-hewn white stone wider than it is tall, its top falling away to one end, set straight into
        /// the lawn at the 银杏大道 roadside: standing obliquely (斜立), face turned to the walker coming north from the bridge,
        /// leaning back a little. No dressed plinth, so it reads as a campus stone and not a headstone.
        /// </summary>
        public static readonly Vector2 SteleAt = new(65.44f, -79.81f);
        public const float SteleFacing = 145f, SteleLean = 6f;
        public const float SteleThickness = 0.75f, SteleBevel = 0.12f, SteleSunk = 0.25f;
        /// <summary>Face outline (x across, y up from the sunk foot) of the stone, convex, with a roughly cut, sloping top.</summary>
        public static readonly Vector2[] SteleOutline =
            [new(-1.55f, 0f), new(1.5f, 0f), new(1.62f, 0.42f), new(1.5f, 0.92f), new(1.12f, 1.3f), new(0.5f, 1.56f), new(-0.2f, 1.74f), new(-0.82f, 1.78f), new(-1.32f, 1.5f), new(-1.62f, 0.8f)];

        /// <summary>WP-15's ground section above the terrain (GroundSection): lawns at 0.10 m, paving at 0.15 m.</summary>
        public const float LawnLevel = 0.10f, PavingLevel = 0.15f;
    }

    /// <summary>
    /// 文科楼群 (MAP-QSH-CENTER-EAST §3), claimed whole and built by this zone from its five OSM parts: the tower in the east
    /// wing, three podium wings and the central hub that holds the glass-roofed atrium. Every block is a stack of rounded
    /// storeys: a white-grey slab-edge band at each floor and a continuous dark glass ribbon between (§3.2 白灰色楼板边，
    /// 夹着连续的黑灰玻璃带); the wings step back storey by storey toward their tips, each set-back a planted terrace behind
    /// a rail. Built "complete, moving in" (§3.3): closed, no site hoarding or scaffold anywhere. Plan points are world (x, z).
    /// </summary>
    public static class Wenke
    {
        public const string Entity = "qsh:way:883187548";
        public const string Tower = "osm:way:1431090636", South = "osm:way:1431090637", Hub = "osm:way:1431090638", North = "osm:way:1431090639", West = "osm:way:1431090640";

        /// <summary>
        /// Storeys per block (§3.2: tower about 15–16, podium 4–6 terraced): the tower 15, the south and west wings 6 at the hub
        /// stepping to 4 at their tips, the north wing 5 to 4, the hub 5 (初值).
        /// </summary>
        public static readonly (string Part, int Storeys)[] Blocks = [(Tower, 15), (Hub, 5), (South, 6), (North, 5), (West, 6)];

        /// <summary>One storey height everywhere so the floor bands of touching blocks line up; a taller ground storey.</summary>
        public const float GroundStorey = 5.1f, Storey = 3.9f;
        /// <summary>
        /// Floor band: from <see cref="SlabBelow"/> under each floor to <see cref="Upstand"/> over it, standing
        /// <see cref="BandOut"/> proud of the glass line; dark plinth under the ground-storey glass.
        /// </summary>
        public const float SlabBelow = 0.25f, Upstand = 0.55f, BandOut = 0.45f, BandChamfer = 0.05f, Plinth = 0.5f;
        /// <summary>Roof: the top band reaches <see cref="RoofBand"/> over the roof level, its parapet <see cref="Parapet"/>.</summary>
        public const float RoofBand = 0.35f, Parapet = 1.15f, ParapetThickness = 0.3f;
        /// <summary>Terraces and podium roofs are planted (退台屋面是绿化露台): the soil stands this far in from the band face.</summary>
        public const float TerraceRim = 0.6f, TerraceSoil = 0.12f;
        public const float RailHeight = 1.1f, RailPosts = 1.8f;

        /// <summary>Corner rounding of every storey outline (楼体带圆角): fillet radius and the smallest corner turn it rounds.</summary>
        public const float CornerRadius = 4.5f, MinTurn = 10f;
        /// <summary>Ribbon glazing: mullions at about this pitch along the glass line; each cell is one numbered window.</summary>
        public const float MullionPitch = 1.8f, MullionWidth = 0.08f, MullionDepth = 0.16f;
        /// <summary>The dark core behind the ribbon stands this far inside the glass line.</summary>
        public const float CoreInset = 0.6f;

        /// <summary>
        /// Set-backs (逐层退台): from storey From up, a block keeps only the side of the line through At where the distance toward
        /// Toward stays within Keep metres. The hub follows the north wing so its narrow strip along that wing steps with it (初值).
        /// </summary>
        public static readonly (string Part, int From, Vector2 At, Vector2 Toward, float Keep)[] SetBacks =
        [
            (South, 4, new(380f, 135f), new(-0.124f, 0.992f), 50f),
            (South, 5, new(380f, 135f), new(-0.124f, 0.992f), 28f),
            (West, 4, new(330f, 120f), new(0.17f, 0.985f), 42f),
            (West, 4, new(318f, 106f), new(-0.86f, -0.5f), 28f),
            (West, 5, new(330f, 120f), new(0.17f, 0.985f), 22f),
            (West, 5, new(318f, 106f), new(-0.86f, -0.5f), 12f),
            (North, 4, new(350f, 72f), new(-0.7f, -0.7f), 22f),
            (Hub, 4, new(350f, 72f), new(-0.7f, -0.7f), 22f),
        ];

        /// <summary>
        /// The yellow-green panels (§3.2 局部有黄绿面板): a vertical strip of opaque panels in place of glass on the tower's south
        /// face, from the storey over the podium roof to the top, centred on this point, over the atrium door (初值).
        /// </summary>
        public static readonly Vector2 PanelStrip = new(397.0f, 101.9f);
        public const float PanelStripHalf = 2.8f;
        public const int PanelStripFrom = 5;

        /// <summary>
        /// The atrium's glass roof (中间是一个钢网架玻璃顶的中庭; mainline shows its form only): a low elliptical glass vault on the
        /// hub roof, semi-axes along and across <see cref="AtriumAxis"/>, ribbed along meridians and two rings (初值).
        /// </summary>
        public static readonly Vector2 AtriumCentre = new(357f, 112f), AtriumAxis = new(0.819f, -0.574f), AtriumHalf = new(18f, 11.5f);
        public const float AtriumRise = 4.5f, AtriumCurb = 0.6f, AtriumRib = 0.14f;
        public const int AtriumRibs = 16;

        /// <summary>
        /// The atrium entrance (中庭入口, closed: §3.3) on the hub's south-east face between the tower and the south wing, facing
        /// the forecourt and the training hall: a two-storey glass porch framed in yellow-green, glass doors shut (门厅卡 by WP-51).
        /// </summary>
        public static readonly Vector2 DoorAt = new(400.26f, 114.97f);
        public const float DoorFacing = 142.6f;
        public const float PorchHalfWidth = 4.6f, PorchJamb = 0.9f, PorchDepth = 1.8f, PorchHead = 1.1f;
        public const float DoorWidth = 3.2f, DoorHeight = 3.0f;

        /// <summary>
        /// Forecourt paving in front of the door, in the door frame (u along the face toward the tower, w out): a granite stop
        /// and a 3.6 m walk from its east side to the 544161568 / 544161582 junction (design addition).
        /// </summary>
        public static readonly Vector2 ForecourtMin = new(-10f, 0.4f), ForecourtMax = new(10f, 13f);
        public static readonly Vector2 WalkFrom = new(413.06f, 115.25f), WalkTo = new(448.5f, 124.0f);
        public const float WalkWidth = 3.6f, PavingTop = 0.17f, PavingKerb = 0.2f;

        /// <summary>Tower roof: a stair and lift penthouse with the plant fenced beside it (屋面设备进围挡).</summary>
        public static readonly Vector3 PenthouseSize = new(11f, 4.2f, 7f);
        public static readonly Vector2 PlantSize = new(9f, 5f);
        public const float PlantFence = 1.8f;
    }

    /// <summary>
    /// 友谊树 (§3.4): a newly planted big camphor (WP-23 plants it at the registered point) held by three timber stakes, its
    /// trunk wound in green cloth, and a granite landscape stone beside it with the gold original emblem on its dressed face.
    /// On the forecourt lawn south of the walk, where P14's stand looks out at 65° to the training hall (初值).
    /// </summary>
    public static class Friendship
    {
        public static readonly Vector2 TreeAt = new(424.0f, 136.0f);
        /// <summary>Trunk wrap (cloth over the WP-23 trunk): radius and its bottom and top above the ground.</summary>
        public const float WrapRadius = 0.25f, WrapBottom = 0.25f, WrapTop = 2.3f;
        /// <summary>Three stakes leaning in to the trunk: foot radius, height where they meet the trunk, pole radius, first bearing.</summary>
        public const float StakeFoot = 1.7f, StakeTop = 2.6f, StakeRadius = 0.055f, StakeBearing = 30f;

        /// <summary>The stone: position, face bearing (toward the forecourt and the door, lit at sunset, R1) and shape.</summary>
        public static readonly Vector2 StoneAt = new(419.6f, 133.2f);
        public const float StoneFacing = 300f;
        public static readonly Framework.BoulderShape Stone = new(1.9f, 1.0f, 1.05f, 0.55f, 0.27f, 15f * (MathF.PI / 180f), -0.08f) { Rise = 0.15f };
        /// <summary>The emblem disc on the face: radius, centre height on the face, relief.</summary>
        public const float EmblemRadius = 0.21f, EmblemY = 0.02f, EmblemRelief = 0.02f;

        /// <summary>
        /// P14's proposed stand (WP-36 decides): south-east of the tree on the lawn, the tree, the stone, the tower and the shut
        /// atrium door to the north-west, the training hall and the moon clear at 65°.
        /// </summary>
        public static readonly Vector2 StandAt = new(429.0f, 141.5f);
        public const float StandFacing = 65f;
    }
}
