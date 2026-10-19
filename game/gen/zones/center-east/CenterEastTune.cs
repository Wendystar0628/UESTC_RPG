using static Campus.Gen.Zones.Common.ZoneLooks;
using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.Zones.CenterEast;

/// <summary>
/// Every tunable value of the center-east zone in one place, for review (WF-FLOW): materials as colour names, and
/// the modelling sizes of each site. Positions, outlines, storeys and heights that world/center-east.json
/// already holds are read from the world model by the IDs below (D-239); only inspection viewpoints remain here because probes have no world input. Local frames are described per block. Values marked "初值" are starting values awaiting the main
/// session's call. 21b–21d add their own nested classes after <see cref="Pinxue"/>.
/// </summary>
public static class CenterEastTune
{
    public const int BudgetTrianglesL0 = 60_000;
    public const int BudgetTrianglesL1 = 30_000;
    public const int BudgetDrawCalls = 16;

    /// <summary>
    /// Window numbering (contract-29): the light channel (LightChannelCatalog) and window-card type (WLD-INTERIORS: 3 corridor,
    /// 5 office, 6 lab) of each grid the zone declares. The research blocks follow the lab curve with W-LAB : W-OFF = 7 : 3
    /// (MAP-QSH-CENTER-EAST), 品学楼's frame beam and link bridges the teaching curve with corridor cards (初值).
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

    /// <summary>Materials (ART-STYLE colour names only).</summary>
    public static class Look
    {

        /// <summary>Renovated teaching-block frame and walls: grey-white (MAP-QSH-CENTER-EAST).</summary>
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
        public static readonly ToonSpec Soil = S("base.warmBrown", ToonFx.None, LineClass.None, WetSurface.PorousResponse);
        /// <summary>Planter beds behind the ramp seat walls: a low planted surface until WP-23 fills them with shrubs.</summary>
        public static readonly ToonSpec Bed = S("ground.grassDark", ToonFx.None, LineClass.None, WetSurface.PorousResponse);
        public static readonly ToonSpec RoofSlab = S("roof.grey", ToonFx.Mottle, LineClass.Building, 0.85f);
        /// <summary>Grey aluminium window frames and mullions (窗框灰铝).</summary>
        public static readonly ToonSpec Metal = S("frame.alu", ToonFx.BevelLift, LineClass.Fine);
        /// <summary>Research family trims (cornice, sills, portals, slab bands): grey-white against the warm stone.</summary>
        public static readonly ToonSpec LightTrim = S("wall.greyWhite", ToonFx.BevelLift, LineClass.Fine);
        /// <summary>Roof-garden timber frames (轻木构架).</summary>
        public static readonly ToonSpec Timber = S("panel.brown", ToonFx.BevelLift, LineClass.Fine);
        /// <summary>
        /// The two bronze figures use the registered bronze pair. Curved metal: two-and-a-half steps, rim and anisotropic
        /// band (ART-STYLE).
        /// </summary>
        public static readonly ToonSpec Bronze = new(new("bronze.lit"), new("bronze.shade"))
        {
            Fx = ToonFx.Rim | ToonFx.AnisoBand,
            Steps = ShadeSteps.TwoHalf,
            Normal = ToonNormal.Smooth,
            Wet = 0.4f,
        };
        /// <summary>林为干像's dark pedestal uses the registered polished black stone.</summary>
        public static readonly ToonSpec DarkStone = S("stone.black", ToonFx.BevelLift, LineClass.Building, 0.9f);
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
        /// <summary>文科楼's local yellow-green panels (#aaa961, ART-STYLE: only on this building).</summary>
        public static readonly ToonSpec YellowGreen = S("panel.yellowGreen", ToonFx.BevelLift);
        /// <summary>
        /// The green cloth wound round the friendship tree's trunk (绿布): borrows the turf green, a clear green that does not
        /// turn with the seasons (the grey-green fabric name read as grey on the trunk).
        /// </summary>
        public static readonly ToonSpec Cloth = S("ground.turf", ToonFx.None, LineClass.Fine, 0.8f) with { Normal = ToonNormal.Smooth };
        /// <summary>The granite landscape stone beside the friendship tree: pale grey, rounded body, smooth normals.</summary>
        public static readonly ToonSpec GreyStone = S("wall.greyWhite", ToonFx.None) with { Normal = ToonNormal.Smooth };
        /// <summary>The gold emblem inlaid in that stone (嵌金色原创徽记, ART-STYLE): curved metal, rim and anisotropic band.</summary>
        public static readonly ToonSpec Gold = new(new("uestc.gold.lit"), new("uestc.gold.shade"))
        {
            Fx = ToonFx.Rim | ToonFx.AnisoBand,
            Steps = ShadeSteps.TwoHalf,
            Normal = ToonNormal.Smooth,
            Wet = 0.3f,
        };
    }

    /// <summary>
    /// 品学楼 (MAP-QSH-CENTER-EAST). The A, B and C wings keep WP-14's facade rules on the +1.5 m platform
    /// (center-east.terrain.json); this zone builds the gate frame, the two link bridges, the forecourt, the fountain's
    /// stone tiers and the curved end bay.
    /// </summary>
    public static class Pinxue
    {
        /// <summary>Design level of the platform (MAP-QSH-TERRAIN): only for probe eye heights, which have no terrain to read.</summary>
        public const float PlatformY = 1.5f;

        /// <summary>
        /// Gate frame (§2.2), read from the bar part (FrameBar): origin at its north-west corner on A's south wall, X south along
        /// the west edge to B's north wall, local +Z west out to the forecourt; the bar lies at z −depth…0.
        /// Storeys follow A's main body (4 storeys, 16 m). The open passage is storeys 1–2, the beam bridge storeys 3–4 up to
        /// the wings' height, the link bridges storeys 2–3.
        /// </summary>
        public const int PassageStoreys = 2, BridgeFromStorey = 2, BridgeToStorey = 3;
        /// <summary>
        /// Inspection probes are fixed viewpoints (the module's Probes take no world), so their eyes are written out in world
        /// (x, z) with the plan direction they look along: Q1 under the beam's forecourt edge looking east at the soffit and
        /// plaque; Q4 0.5 m off the A hall's ground-floor glazing (door px.a.hall, bearing-216° facade) looking back at it.
        /// </summary>
        public static readonly Vector2 Q1Eye = new(160.46f, -64.12f), Q1Look = new(0.9899f, -0.1415f);
        public static readonly Vector2 Q4Eye = new(142.58f, -87.16f), Q4Look = new(0.5939f, -0.8046f);
        /// <summary>Four square columns, one row on the bar's centre line, three bays with the middle one widest (四柱三间, 初值).</summary>
        public const float ColumnSize = 2.4f, ColumnPlinth = 0.45f;
        public static readonly float[] ColumnX = [2.6f, 13.0f, 24.72f, 35.12f];
        /// <summary>Inscription plaque on the beam's forecourt face; the calligraphy waits for the scene text atlas (初值).</summary>
        public const float PlaqueWidth = 26f, PlaqueHeight = 3.0f, PlaqueProud = 0.08f;
        /// <summary>Soffit downlights over the passage (lit by WP-39 with the frame's light channel).</summary>
        public const float DownlightPitch = 3.2f, DownlightRadius = 0.16f;

        /// <summary>Link bridges A–C and B–C (§2.2): storeys 2–3 over the paths between the wings (初值).</summary>
        public const float BridgeColumnRadius = 0.3f;

        /// <summary>
        /// Low stepped cascade (§2.3; water by WP-24, WLD-WATER; 10:01 review): three nested rectangular pools of light
        /// stone, centred in the forecourt's 14.2 × 7.2 m lawn panel 22 m in front of the frame (the point FountainId), long
        /// side along the bar. Each tier's thin water sheet spills over its coping into the pool below. Halves are (along the
        /// bar, toward the forecourt); heights are above the lawn and stay under 0.9 m so the cascade lies low on the grass (初值).
        /// </summary>
        public static readonly Vector2 BasinHalf = new(5.5f, 2.5f);
        public const float BasinCoping = 0.35f, BasinRim = 0.30f, BasinWater = 0.24f;
        public static readonly Vector2 MiddleHalf = new(4.0f, 1.7f);
        public const float MiddleRim = 0.55f, MiddleWater = 0.49f;
        public static readonly Vector2 TopHalf = new(2.5f, 0.9f);
        public const float TopRim = 0.80f, TopWater = 0.74f;
        /// <summary>Coping width of the two upper tiers; every pool is this deep below its water line.</summary>
        public const float TierCoping = 0.25f, PoolDepth = 0.14f;
        public const float PanelKerbWidth = 0.3f, PanelKerbHeight = 0.15f;

        /// <summary>Square tree pits (方形树池), one at each polygon of TreePitsId, aligned to the gate frame.</summary>
        public const float TreePitSize = 1.8f, TreePitKerb = 0.22f, TreePitHeight = 0.42f, TreePitSoil = 0.32f;

        /// <summary>Round bed on the ramp (the forecourt's circular hole, the point RoundBedId): a 0.45 m granite seat ring round one tree.</summary>
        public const float RoundBedRadius = 3.45f, SeatWidth = 0.45f, SeatHeight = 0.45f;

        /// <summary>
        /// Ramp seat walls (MAP-QSH-TERRAIN): 0.45 m planter walls on both long sides of the 5 % ramp plaza. The registered
        /// polylines (RampWallNorthId, RampWallSouthId) run down the wall's centre line from the top of the ramp to the foot,
        /// the wall stepping per segment.
        /// </summary>
        public const float RampWallSegment = 6f;
        public const float RampWallThickness = 0.5f, RampBedWidth = 1.2f, RampBedKerb = 0.2f;

        /// <summary>
        /// Curved end of the right wing (§2.4: 右翼端部是一个弧形体量), read from the forecourt as the far right: a curved
        /// curtain-wall bay over B's diagonal south-west lobby (ArcLobby), bulging toward the river (初值). Its chord is the
        /// registered ArcEndId outline's ends, its height and storeys the lobby's. The entrance kit at the A-wing hall door
        /// px.a.hall (interiors.json) and at the side door under the passage is WP-13's (12:15 ruling).
        /// </summary>
        public static readonly Vector2 ArcBulge = new(-0.6f, 0.8f);
        public const float ArcSagitta = 3.5f;
    }

    /// <summary>
    /// 中部科研区 (MAP-QSH-CENTER-EAST): 基础实验大楼 (科 A) and 一号科研实验楼 (科 B, 科 C), claimed and built by this zone
    /// as the renovated research family: warm light stone frame over recessed glass, a glazed ground storey, windows
    /// grouped in pairs by giant pilasters, grey-white cornice and sills, dark plinth (MAP-QSH-RENOVATION E-02, E-03).
    /// All blocks share one plan direction; positions are in its local frame R (u along <see cref="Axis"/>, w = (−Axis.y,
    /// Axis.x), roughly south). Roof outlines and fittings registered in world/center-east.json (the three IDs below) are read
    /// back from the world model and put into R; doors, stairs and plant enclosures are read from the zone layout points.
    /// </summary>
    public static class Research
    {
        /// <summary>Registered roof dressings: 科 A's fence ring and tank, 科 B's attic ring and dishes, 科 C's garden ring.</summary>

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

        /// <summary>Hall heights to the coping: a tall storey each, the storey count read from the hall part (OSM: 1, 1, 2 levels).</summary>
        public const float HallStorey = 4.5f, HallAttic = 1.0f;
        public const float HallDoorWidth = 2.4f, HallDoorHeight = 2.7f, HallCanopyDepth = 1.8f;

        /// <summary>科 B's east wing rises one storey higher (§1.2 东段局部 6 层): a set-back attic storey over the east bar (the KeBRoofId ring).</summary>
        public const float AtticStorey = 4.2f;

        /// <summary>Stair penthouses on the roofs (B7 roof relief every 20–30 m), local centres; all 5.0 × 6.0 m, 3.4 m high.</summary>
        public static readonly Vector2 StairSize = new(5.0f, 6.0f);
        public const float StairHeight = 3.4f;

        /// <summary>Plant enclosures (屋面设备进围挡, MAP-QSH-ADDITIONS): dark perforated fence round three heat pumps.</summary>
        public static readonly Vector2 PlantSize = new(8.0f, 4.6f);
        public const float PlantFence = 1.8f;

        /// <summary>
        /// 科 A's roof fence and tower tank (MAP-QSH-ADDITIONS): a 2 m grey aluminium bar fence round the east wing's
        /// roof and a panel tank on a steel stand beside the stair penthouse; skyline only, the roof stays closed.
        /// </summary>
        public const float KeAFenceHeight = 2.0f, KeAFencePosts = 2.5f;
        public static readonly Vector3 KeATankSize = new(3.0f, 3.8f, 3.0f);

        /// <summary>
        /// 一号科研楼 antennas (§2.9 科研楼天线): white dishes on 科 B's attic roof, facing east to catch the sunrise; their spots
        /// and diameters are KeBRoofId's props.dishes. Roof garden (§2.9 屋顶实验花园) on 科 C's bar (the KeCGardenId ring): a light
        /// timber frame over rows of planters.
        /// </summary>
        public const float GardenPostPitch = 3.14f, GardenFrameHeight = 2.4f;
    }

    /// <summary>
    /// 中部科研区 monuments (MAP-QSH-CENTER-EAST): 林为干像 and 麦克斯韦像 on the statue lawn east of 科 B/C, the
    /// equation stone beside Maxwell and the 科研楼 stele by 银杏大道 between 科 A and 科 B. The OSM points move less than
    /// 4 m (a minor position adjustment): 麦克斯韦像 and the stele stand on theirs (read by Maxwell, Stele), 林为干像 on a moved spot below;
    /// the equation stone and Maxwell's granite apron are design additions in world/center-east.json.
    /// Facings are bearings (0 north, 90 east). The figures are sculpture volumes after 吴立人像 (18c1 review): clothes,
    /// posture and hair as masses, the face one smooth volume with no features or likeness (CORE-INDEX). Inscriptions wait
    /// for the scene text atlas (contract-26): the pedestal and stele faces stay blank, the equation stone carries four
    /// filled line blocks where the four equations go (初值).
    /// </summary>
    public static class Monuments
    {
        /// <summary>The paved plaza in front of 科 B's door; its tree island on the door's axis carries 林为干像.</summary>

        /// <summary>
        /// 林为干像 (§1.4): bronze bust in a suit on a dark pedestal at the east tip of the tree island, on 科 B's door axis
        /// (research frame w = −143.8, 0.9 m from the OSM point, so not the data's point), facing out along the axis to 银杏大道
        /// with the big tree behind. Bust scale 1.35 × a 2 m figure's head and shoulders.
        /// </summary>
        public const float LinFacing = 81.8f, LinScale = 1.35f;
        public const float LinPlinth = 0.25f, LinShaft = 1.2f, LinCap = 0.12f;
        /// <summary>Granite kerb round the tree island (the plaza's hole), like the 品学楼 lawn panel's.</summary>
        public const float IslandKerbWidth = 0.25f, IslandKerbTop = 0.2f;

        /// <summary>
        /// 麦克斯韦像 (§1.3): full-length bronze, frock coat, full beard, a scroll held to the chest, on a light grey granite
        /// pedestal; at the OSM point (Maxwell) near the lawn's east edge, turned south-east toward the plaza corner where R5
        /// arrives. Figure 1.25 × a 2 m model, about 2.5 m, as 吴立人像.
        /// </summary>
        public const float MaxwellFacing = 130f, MaxwellScale = 1.25f;
        /// <summary>Top of the pedestal above the ground (base slab, die, cap; 5 cm sunk).</summary>
        public const float MaxwellPedestalTop = 1.95f;

        /// <summary>
        /// Granite apron (stop point) round Maxwell and the stone, the ApronId rectangle (read in the research frame) at the
        /// lawn's east edge where footway 572670136 runs; a few centimetres proud of the lawn with a light kerb.
        /// </summary>
        /// <summary>Apron top above the terrain (2 cm over WP-15's paving level, 7 cm over the lawn) and its kerb.</summary>
        public const float ApronTop = 0.17f, ApronKerb = 0.2f;

        /// <summary>
        /// Equation stone (§1.3 圆浑的景石): at Maxwell's front right ((−2.8, 1.4) in his frame; the point StoneId), its
        /// dressed face turned to the R5 approach. Length × depth × height, and the face: centre height, distance in front
        /// of the stone's axis, tilt back from vertical.
        /// </summary>
        public const float StoneFacing = 160f;
        public static readonly Common.BoulderShape Stone = new(2.4f, 1.2f, 1.35f, 0.72f, 0.32f, 18f * (MathF.PI / 180f), -0.12f) { Rise = -0.22f };
        /// <summary>Inscription lines (bottom to top, the four equations): length, and the line height and pitch on the face.</summary>
        public static readonly float[] StoneLines = [0.95f, 0.78f, 0.42f, 0.66f];
        public const float StoneLineHeight = 0.07f, StoneLinePitch = 0.15f, StoneLineBottom = -0.24f;

        /// <summary>
        /// 科研楼碑 (§1.5): a rough-hewn white stone wider than it is tall, its top falling away to one end, set straight into
        /// the lawn at the 银杏大道 roadside: standing obliquely (斜立), face turned to the walker coming north from the bridge,
        /// leaning back a little. No dressed plinth, so it reads as a campus stone and not a headstone. Stands at the OSM
        /// point (Stele).
        /// </summary>
        public const float SteleFacing = 145f, SteleLean = 6f;
        public const float SteleThickness = 0.75f, SteleBevel = 0.12f, SteleSunk = 0.25f;
        /// <summary>Face outline (x across, y up from the sunk foot) of the stone, convex, with a roughly cut, sloping top.</summary>
        public static readonly Vector2[] SteleOutline =
            [new(-1.55f, 0f), new(1.5f, 0f), new(1.62f, 0.42f), new(1.5f, 0.92f), new(1.12f, 1.3f), new(0.5f, 1.56f), new(-0.2f, 1.74f), new(-0.82f, 1.78f), new(-1.32f, 1.5f), new(-1.62f, 0.8f)];

        /// <summary>WP-15's ground section above the terrain (GroundSection): lawns at 0.10 m, paving at 0.15 m.</summary>
    }

    /// <summary>
    /// 文科楼群 (MAP-QSH-CENTER-EAST), claimed whole and built by this zone from its five OSM parts: the tower in the east
    /// wing, three podium wings and the central hub that holds the glass-roofed atrium. Every block is a stack of rounded
    /// storeys: a white-grey slab-edge band at each floor and a continuous dark glass ribbon between (§3.2 白灰色楼板边，
    /// 夹着连续的黑灰玻璃带); the wings step back storey by storey toward their tips, each set-back a planted terrace behind
    /// a rail. Built "complete, moving in" (§3.3): closed, no site hoarding or scaffold anywhere. Plan points are world (x, z).
    /// </summary>
    public static class Wenke
    {
        /// <summary>The atrium roof, the door's forecourt paving and the walk to the road, registered in the overlay (read back by ID).</summary>

        /// <summary>
        /// Storeys per block (§3.2: tower about 15–16, podium 4–6 terraced). The tower's is null: the part's own 15 levels are
        /// read. The podium parts' OSM 4 levels are estimates, so the zone's own terraced counts stay: the south and west wings
        /// 6 at the hub stepping to 4 at their tips, the north wing 5 to 4, the hub <see cref="HubStoreys"/> (初值).
        /// </summary>
        public static readonly (string Part, int? Storeys)[] Blocks = [(CenterEastIds.Wenke.Tower, null), (CenterEastIds.Wenke.Hub, HubStoreys), (CenterEastIds.Wenke.South, 6), (CenterEastIds.Wenke.North, 5), (CenterEastIds.Wenke.West, 6)];
        public const int HubStoreys = 5;

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
        public static readonly float RailHeight = new Campus.Contracts.Parts.RailingParams().Height;
        public const float RailPosts = 1.8f;

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

        /// <summary>
        /// The yellow-green panels (§3.2 局部有黄绿面板): a vertical strip of opaque panels in place of glass on the tower's south
        /// face, from the storey over the podium roof to the top, centred on this point, over the atrium door (初值).
        /// </summary>
        public const float PanelStripHalf = 2.8f;
        public const int PanelStripFrom = 5;

        /// <summary>
        /// The atrium's glass roof (中间是一个钢网架玻璃顶的中庭; mainline shows its form only): a low elliptical glass vault on the
        /// hub roof, ribbed along meridians and two rings (初值). Its centre, long axis and semi-axes are CenterEastIds.Wenke.AtriumRoofId's props.
        /// </summary>
        public const float AtriumRise = 4.5f, AtriumCurb = 0.6f, AtriumRib = 0.14f;
        public const int AtriumRibs = 16;

        /// <summary>
        /// The atrium entrance (中庭入口, closed: §3.3) on the hub's south-east face between the tower and the south wing, facing
        /// the forecourt and the training hall: a two-storey glass porch framed in yellow-green, glass doors shut (门厅卡 by WP-51).
        /// The door's spot and facing are CenterEastIds.Wenke.ForecourtId's props.door and props.doorFacing.
        /// </summary>
        public const float PorchHalfWidth = 4.6f, PorchJamb = 0.9f, PorchDepth = 1.8f, PorchHead = 1.1f;
        public const float DoorWidth = 3.2f, DoorHeight = 3.0f;

        /// <summary>
        /// Forecourt paving in front of the door (CenterEastIds.Wenke.ForecourtId, read in the door frame: u along the face toward the tower, w out):
        /// a granite stop and the walk (CenterEastIds.Wenke.WalkId, a strip from its west end to the road junction 544161568 / 544161582; design addition).
        /// </summary>
        public const float PavingTop = 0.17f, PavingKerb = 0.2f;

        /// <summary>CenterEastIds.Wenke.Tower roof: a stair and lift penthouse with the plant fenced beside it (屋面设备进围挡).</summary>
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
        /// <summary>Trunk wrap (cloth over the WP-23 trunk): radius and its bottom and top above the ground.</summary>
        public const float WrapRadius = 0.25f, WrapBottom = 0.25f, WrapTop = 2.3f;
        /// <summary>Three stakes leaning in to the trunk: foot radius, height where they meet the trunk, pole radius, first bearing.</summary>
        public const float StakeFoot = 1.7f, StakeTop = 2.6f, StakeRadius = 0.055f, StakeBearing = 30f;

        /// <summary>The stone (the point StoneId): face bearing (toward the forecourt and the door, lit at sunset, R1) and shape.</summary>
        public const float StoneFacing = 300f;
        public static readonly Common.BoulderShape Stone = new(1.9f, 1.0f, 1.05f, 0.55f, 0.27f, 15f * (MathF.PI / 180f), -0.08f) { Rise = 0.15f };
        /// <summary>The emblem disc on the face: radius, centre height on the face, relief.</summary>
        public const float EmblemRadius = 0.21f, EmblemY = 0.02f, EmblemRelief = 0.02f;

        /// <summary>
        /// P14's proposed stand (WP-36 decides): south-east of the tree on the lawn, the tree, the stone, the tower and the shut
        /// atrium door to the north-west, the training hall and the moon clear at 65°.
        /// </summary>
        public static readonly Vector2 StandAt = new(429.0f, 141.5f);
        public const float StandFacing = 65f;
        /// <summary>Q2 looks from the stand at the atrium porch's door (a fixed point: the probe has no world to read).</summary>
        public static readonly Vector2 StandLookAt = new(400.26f, 114.97f);
    }
}
