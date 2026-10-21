using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Sites;
using static Campus.Gen.Sites.ZoneLooks;
using FacadeRules = Campus.Gen.Buildings.Rules.FacadeRules;

namespace Campus.Gen.Maps.Qingshuihe.West;

/// <summary>
/// Every tunable value of the west zone in one place, for review (WF-FLOW): materials as colour names, and the
/// dimensions of each site. Footprints, storeys and entity positions are read from the world model by ID (D-239); the
/// authored positions and routes are read from world/west.json. Values marked
/// "初值" are starting values awaiting the main session's call. 19b and 19c add their own nested classes after these.
/// </summary>
public static class WestTune
{

    /// <summary>
    /// Window numbering (contract-29): 六号's stone blocks follow the lab curve (about 0.3 all night, SYS-TIME) with
    /// W-LAB : W-OFF = 7 : 3 (WLD-INTERIORS: 5 office, 6 lab), as the research family elsewhere; the zone stays quiet at night.
    /// </summary>
    public static class Windows
    {
        public static readonly ChannelId Research = new("window.lab");
        public const byte Office = 5, Lab = 6;
        /// <summary>Tenths of the windows that show an office card instead of a lab.</summary>
        public const int OfficeShare = 3;
    }

    /// <summary>Materials (ART-STYLE colour names only).</summary>
    public static class Look
    {
        /// <summary>六号's dry-hung stone: pale warm grey (浅暖灰石材, MAP-QSH-WEST).</summary>
        public static readonly ToonSpec Stone = S("stone.beige", ToonFx.BevelLift);
        /// <summary>Copings, sills, soffit fascias and column caps: grey-white, fine lines so trims do not hatch the facade.</summary>
        public static readonly ToonSpec Trim = S("wall.greyWhite", ToonFx.BevelLift, LineClass.Fine);
        /// <summary>The continuous louvre strip on every floor (深灰水平带) and the plinth: dark grey.</summary>
        public static readonly ToonSpec DarkGrey = S("roof.grey", ToonFx.BevelLift, LineClass.Fine, 0.8f);
        /// <summary>Louvre slats, mullions and door frames: grey aluminium.</summary>
        public static readonly ToonSpec Metal = S("frame.alu", ToonFx.BevelLift, LineClass.Fine);
        public static readonly ToonSpec RoofSlab = S("roof.grey", ToonFx.Mottle, LineClass.Building, 0.85f);
        public static readonly ToonSpec Glass = ZoneLooks.Glass;
        /// <summary>Entrance aprons and the stone plaza kerbs: light granite.</summary>
        public static readonly ToonSpec Granite = S("stone.beige", ToonFx.BevelLift, LineClass.Fine);
        /// <summary>The campus red (equip.fireRed, Opus review 10:07): 六号's emblem and the motto's lacquered characters.</summary>
        public static readonly ToonSpec Red = S("equip.fireRed", ToonFx.None, LineClass.None, 0.3f);
        /// <summary>The silver characters under the emblem: brushed metal with a rim and an anisotropic band (ART-STYLE).</summary>
        public static readonly ToonSpec Silver = S("roof.silverMetal", ToonFx.Rim | ToonFx.AnisoBand, LineClass.Fine, 0.5f);
        /// <summary>The motto stone: one rough cream-white boulder (米白粗粝块面石, MAP-QSH-WEST), faceted normals.</summary>
        public static readonly ToonSpec CreamStone = S("wall", ToonFx.BevelLift);
        /// <summary>
        /// 深蓝玻璃 (19b): 四号's entrance boxes, the campus tech blue as glass (streaked, glaring), so the boxes read blue and
        /// clear, not black. game/style/'s glass.deep is an extension colour the byte palette path cannot carry yet; 初值, the main
        /// session may move both glasses to it once extension indices resolve end to end.
        /// </summary>
        public static readonly ToonSpec BlueGlass = new(new("uestc.techblue.lit"), new("uestc.techblue.shade"))
        { Kind = ToonKind.Glass, Fx = ToonFx.GlassStreak | ToonFx.Glare, Wet = 0 };
        /// <summary>深色玻璃 (19b): 创新中心's stair shafts, the campus glass (dark teal grey).</summary>
        public static readonly ToonSpec DarkGlass = Glass;
        /// <summary>White door frames (白色门框) and seat-wall caps: grey-white with lifted bevels.</summary>
        public static readonly ToonSpec WhiteFrame = S("wall.greyWhite", ToonFx.BevelLift);
        /// <summary>The motto stone's pebble bed: rounded river pebbles in pale paver grey, mottled.</summary>
        public static readonly ToonSpec Pebble = S("ground.paver", ToonFx.Mottle, LineClass.Fine, 0.9f);
        /// <summary>Low clipped planting round the motto stone's bed (hedge), soft tree lines.</summary>
        public static readonly ToonSpec Hedge = S("hedge", ToonFx.Mottle, LineClass.Tree, WetSurface.PorousResponse);

        // ---- 19c: wetland, bridgehead and stone carving ----
        /// <summary>Boardwalk planks and handrails (栈道木板): warm light timber, mottled, soft wet darkening.</summary>
        public static readonly ToonSpec Deck = S("panel.brown", ToonFx.Mottle | ToonFx.BevelLift, LineClass.Fine, 0.7f);
        /// <summary>Boardwalk stringers, rail posts and piles: the same timber a step darker, so the deck edge reads as a clean line.</summary>
        public static readonly ToonSpec DeckDark = S("base.warmBrown", ToonFx.BevelLift, LineClass.Fine, 0.7f);
        /// <summary>The low boardwalk lamp's body (深灰色圆柱灯, MAP-QSH-ADDITIONS): dark grey, fine lines.</summary>
        public static readonly ToonSpec LampBody = S("roof.grey", ToonFx.BevelLift, LineClass.Fine, 0.6f);
        /// <summary>Frosted lamp lenses: pale grey-white; the light itself is the module's lamp emitter (contract-12).</summary>
        public static readonly ToonSpec LampLens = S("wall.greyWhite", ToonFx.None, LineClass.None, 0.2f);
        /// <summary>候车亭 roof and fascia: clean grey-white with lifted bevels.</summary>
        public static readonly ToonSpec ShelterRoof = S("wall.greyWhite", ToonFx.BevelLift, LineClass.Building, 0.6f);
        /// <summary>A thin tech-blue band along the shelter's fascia (信息科技浅蓝的校园蓝, used sparingly).</summary>
        public static readonly ToonSpec ShelterBand = S("uestc.techblue", ToonFx.None, LineClass.Fine, 0.4f);
        /// <summary>The shelter's raised platform and kerb: light granite.</summary>
        public static readonly ToonSpec ShelterSlab = S("stone.beige", ToonFx.BevelLift | ToonFx.Mottle, LineClass.Fine, 0.85f);
        /// <summary>The 国防科技园 carving: a rough-hewn white lying stone (粗凿的白色卧石, §9.1), faceted.</summary>
        public static readonly ToonSpec WhiteStone = S("wall", ToonFx.BevelLift | ToonFx.Mottle);
    }

    /// <summary>
    /// 六号科研楼 (MAP-QSH-WEST), named by the OSM blocks A, B and C. Three claimed entities: A and B are their own entities;
    /// the C block, the two links and the north tower are the parts of qsh:relation:14153711 (the duplicate C shell
    /// qsh:way:779496166 is hidden in world/west.json). Every block stands on a two-storey glass base set back under the
    /// stone (底层两层通高、向内退的深色玻璃幕墙), with deep square windows above and one dark louvre strip per floor.
    /// </summary>
    public static class Six
    {
        public static readonly string EntityA = WestIds.Way779496168.Entity;
        public static readonly string EntityB = WestIds.Way779496169.Entity;
        public static readonly string EntityC = WestIds.Relation14153711.Entity;
        public static readonly string PartA = WestIds.Way779496168.Part;
        public static readonly string PartB = WestIds.Way779496169.Part;
        /// <summary>The C block with its courtyard (the source the 13:06 dedupe kept).</summary>
        public static readonly string PartC = WestIds.Relation10807930.Part;
        /// <summary>B–C link: the 2F outdoor bridge over the drive-through (§1.6).</summary>
        public static readonly string LinkBC = WestIds.Relation14153711.Part;
        /// <summary>A–B link: a two-storey glass link (门厅或连廊).</summary>
        public static readonly string LinkAB = WestIds.Way950008613.Part;
        /// <summary>North tower: the stair and lift core, 7 storeys, 23.1 m.</summary>
        public static readonly string Tower = WestIds.Way779496167.Part;

        /// <summary>The two-storey glass base (两层通高) and the depth it is set back behind the stone line.</summary>
        public const float BaseHeight = 9.0f;
        public const float BaseStorey = 4.5f;
        public const float Recess = 2.2f;
        /// <summary>
        /// Storeys of the glass base. A and B take their upper floors from the world model (storeys minus this: A 9 → 7, B 7 → 5;
        /// D-239).
        /// </summary>
        public const int BaseStoreys = 2;
        /// <summary>
        /// C's upper floors: 5, 7 in all, 32.5 m (§1.1, 数据定位). Not read from data: the OSM block osm:relation:10807930 carries
        /// 4 floors and 24 m, and the entity's storeys are the tower's.
        /// </summary>
        public const int UpperC = 5;
        public const float Storey = 4.4f;
        public const float Bay = 3.9f;
        public const float Pier = 1.0f;
        /// <summary>Deep reveals: the stone face stands this far in front of the glass (深凹的方窗).</summary>
        public const float PierDepth = 0.55f;
        public const float Sill = 0.9f;
        public const float Window = 2.5f;
        /// <summary>Base columns under the stone: every Nth pier, square, in the stone line.</summary>
        public const int ColumnEvery = 2;
        public const float ColumnSize = 0.75f;
        /// <summary>Base glass: mullion pitch and the transom at the first-floor line.</summary>
        public const float MullionPitch = 1.5f;

        /// <summary>
        /// C 区东门 (WLD-INTERIORS): the entrance record of C (interiors.json) gives the door centre on the stone line, facing 77°,
        /// floor ±0; the site reads it from the world model. A glass vestibule reaches from the set-back glass out to the stone
        /// line; its doors stand open.
        /// </summary>
        public const string EastDoorId = "six.c.east";
        public const float VestibuleWidth = 5.2f;
        public const float VestibuleHeight = 4.2f;
        public const float DoorWidth = 2.4f;
        public const float DoorHeight = 2.7f;
        /// <summary>
        /// The granite threshold apron in front of the open door, 3 cm proud of the forecourt paving (game/gen/ground/ draws paving at
        /// <see cref="PavingTop"/>; the interior floor is ±0 in interiors.json, so no risers); the forecourt itself is
        /// world/west.json's design:west-six-forecourt.
        /// </summary>
        public const float ApronDepth = 4.0f;
        /// <summary>Top of game/gen/ground/'s paving (GroundSection.PavingY); the paving under the overhang sits 1 cm above it.</summary>
        public const float PavingTop = GroundLevels.Paving;
        public const float ApronWidth = 7.6f;

        /// <summary>Shut entrances (glass doors, lobby card behind, DoorClosed): one on A, one on B, one on C's south-east face. 初值.</summary>
        public static Vector2[] ShutDoors(IWorldModel world) => [world.Feature(WestIds.SixShutDoor1).Point(), world.Feature(WestIds.SixShutDoor2).Point(), world.Feature(WestIds.SixShutDoor3).Point()];
        public const float ShutDoorWidth = 2.0f;
        public const float ShutDoorHeight = 2.6f;

        /// <summary>
        /// The crown sign on C's east face (§1.2, ART-STYLE), centred on the face above the door: a solid stone panel over the
        /// top two storeys rising above the parapet, the red emblem at its head and silver Chinese lettering below.
        /// </summary>
        public const float SignWidth = 21f;
        public const float SignRise = 3.4f;
        public const float SignDrop = 6.0f;
        public const float SignProud = 0.95f;
        public const float EmblemDiameter = 3.6f;
        public const float EmblemDepth = 0.18f;
        public const float CharacterSize = 1.75f;

        /// <summary>B–C link: deck at the first-floor line, roof at the base top, glass railing on its long sides.</summary>
        public const float LinkDeck = 4.5f;
        public const float LinkPier = 1.1f;
        public static readonly float RailHeight = new Campus.Contracts.Parts.RailingParams().Height;
        /// <summary>A–B link: two-storey glass with a stone fascia.</summary>
        public const float LinkFascia = 1.1f;
        /// <summary>
        /// North tower height (数据定位: 7 storeys, 23.1 m). Not read from data: the OSM part (osm:way:779496167) estimates 27.3 m
        /// at 3.9 m a storey, but a stair core's flights are lower than a research storey.
        /// </summary>
        public const float TowerHeight = 23.1f;

        /// <summary>The blocks' common plan axis (A's south wing, B's first wing; C's faces run square to it).</summary>
        public static readonly Vector2 Axis = new(0.80023f, 0.59969f);
        /// <summary>Roof plant enclosures (fenced, in the middle of a wing) on A, B and C, set out along <see cref="Axis"/>. 初值.</summary>
        public static Vector2[] Plants(IWorldModel world) => [world.Feature(WestIds.SixPlant1).Point(), world.Feature(WestIds.SixPlant2).Point(), world.Feature(WestIds.SixPlant3).Point()];
        public static readonly Vector2 PlantSize = new(8f, 5f);
        public const float PlantFence = 2.0f;
    }

    /// <summary>
    /// 校训石 (MAP-QSH-WEST): one cream-white boulder lying on its side, about 2 m high and 6.6 m long, in front of
    /// 六号's C east door; its dressed face turns toward P10 (−515, 250) so the red characters read from the drop point.
    /// </summary>
    public static class Motto
    {
        /// <summary>The stone's node; it stands at the node's anchor in the world model.</summary>
        public static readonly string Entity = WestIds.Node11361252903.Entity;
        /// <summary>Bearing its inscribed face looks along (toward P10).</summary>
        public const float Facing = 112f;
        public const float Length = 6.6f;
        public const float Depth = 1.85f;
        public const float Height = 1.8f;
        /// <summary>Two scene-text lines "求实求真" / "大气大为" on the stone face.</summary>
        public const float CharacterSize = 0.56f;
        public const float LineGap = 0.78f;
        /// <summary>Granite pad round the foot (石脚的分界线) and the low lawn-lamp spot on the P10 side, for game/systems/nightlight/.</summary>
        public const float PadMargin = 0.5f;

        /// <summary>
        /// 19b (Opus review 21:28): the stone no longer stands straight on the paving. It lies in an oval bed of river pebbles
        /// held by a low granite kerb, with a crescent of low clipped planting round its back and ends; the front stays open
        /// so the face reads from P10 and Q1. Margins of the kerb's centreline beyond the stone's half extents. 初值.
        /// </summary>
        public const float BedMarginX = 1.25f, BedMarginZ = 1.05f;
        public const float KerbWidth = 0.22f, KerbHeight = 0.16f;
        /// <summary>The pebble bed's crown above the paving (a low dome).</summary>
        public const float PebbleCrown = 0.14f;
        /// <summary>The planting crescent: its width, height, its sweep either side of straight back (degrees) and its gap to the kerb.</summary>
        public const float HedgeWidth = 0.55f, HedgeHeight = 0.42f, HedgeSweep = 128f, HedgeGap = 0.12f;
    }

    /// <summary>
    /// 四号科研楼 (MAP-QSH-WEST): the three courtyard blocks are game/gen/buildings/'s (research family set in world/west.json); this
    /// zone adds what the rules cannot: each block's entrance, a two-storey deep-blue glass box in a white door frame with a
    /// sign band (§6.3), shut (WLD-INTERIORS: glass doors, lobby card, DoorClosed); and the fan lawn's 0.45 m seat wall
    /// on its outer arc (MAP-QSH-TERRAIN; the lawn's dish is west.terrain.json's basin).
    /// </summary>
    public static class Four
    {
        public static readonly string EntityA = WestIds.Relation11334255.Entity;
        public static readonly string EntityB = WestIds.Relation11665641.Entity;
        public static readonly string EntityC = WestIds.Relation11334253.Entity;

        /// <summary>
        /// Door centres on each block's stone line toward the lawn, and the bearing the box faces: A's south face, B's
        /// south-east face, C's east face (osm:relation:11334255 e2, 11665641 e1, 11334252 e1, mid-points). 初值.
        /// </summary>
        public static (string Entity, string Key, Vector2 Door, float Facing)[] Boxes(IWorldModel world) =>
        [
            (EntityA, "a", world.Feature(WestIds.FourDoorA).Point(), world.Feature(WestIds.FourDoorA).Number("facing")),
            (EntityB, "b", world.Feature(WestIds.FourDoorB).Point(), world.Feature(WestIds.FourDoorB).Number("facing")),
            (EntityC, "c", world.Feature(WestIds.FourDoorC).Point(), world.Feature(WestIds.FourDoorC).Number("facing")),
        ];

        /// <summary>Glass box: width along the face, how far it stands out, and its height (two storeys of game/gen/buildings/'s 4.2 m research storey).</summary>
        public const float GlassWidth = 7.2f, GlassDepth = 3.6f, GlassHeight = 2 * FacadeRules.ResearchStorey;
        /// <summary>The white frame round it: member thickness, how far it stands proud of the glass, and the sign band on its head.</summary>
        public const float FrameMember = 0.65f, FrameProud = 0.55f, SignBand = 1.15f;
        /// <summary>Mullion pitch on the glass and the transom at the first-floor line.</summary>
        public const float MullionPitch = 1.2f, Transom = FacadeRules.ResearchStorey;
        public const float DoorWidth = 2.4f, DoorHeight = 2.7f;
        /// <summary>Granite landing in front of the box, 3 cm proud of game/gen/ground/'s paving.</summary>
        public const float LandingDepth = 2.4f, PavingTop = GroundLevels.Paving;

        /// <summary>
        /// The fan lawn (osm:way:803693984–803693986): its outer arc is the lawn's edge along the ring road; the seat wall runs on
        /// these edges, inset toward the dish centre, so the radial paths pass through its gaps. Points run along the arc. 初值.
        /// </summary>
        public static Vector2[][] SeatArcs(IWorldModel world) => [world.Feature(WestIds.FourSeatArc1).Line().ToArray(), world.Feature(WestIds.FourSeatArc2).Line().ToArray(), world.Feature(WestIds.FourSeatArc3).Line().ToArray()];
        /// <summary>The dish centre the arcs are inset toward (MAP-QSH-TERRAIN: about (−324, −275), radius 32, 0.45 deep).</summary>
        public static Vector2 LawnCentre(IWorldModel world) => ((TerrainCircle)((TerrainBasinOp)world.TerrainOperations[WestIds.FourFanLawn]).Shape).Center;
        public const float LawnRadius = 32f, LawnDepth = 0.45f;
        public const float SeatInset = 0.8f, SeatHeight = 0.45f, SeatWidth = 0.5f, SeatCap = 0.08f;
    }

    /// <summary>
    /// 创新中心 (MAP-QSH-WEST, MAP-QSH-RENOVATION W-01): the two six-storey bars and the two four-storey cross wings are game/gen/buildings/'s
    /// (research, with its rooftop plant enclosure and stair penthouses under the main coping); this zone builds the dark glass
    /// stair shafts where the wings meet the bars (§3.3) and the main entrance frame on the east wing's face to the parking
    /// square, with the red-brown entrance sign.
    /// </summary>
    public static class Innovation
    {
        public static readonly string Entity = WestIds.Relation11032065.Entity;
        /// <summary>The cross wings' long axis (north to south).</summary>
        public static readonly Vector2 WingAxis = new(-0.5990f, 0.8007f);
        /// <summary>
        /// The four links between wings and bars (osm:way:795677485 / 795677486 necks): centre and the link's length along the
        /// wing axis between the two buildings. The glass shaft is a little wider than the neck. 初值.
        /// </summary>
        public static (Vector2 Centre, float Length)[] Shafts(IWorldModel world) => [(world.Feature(WestIds.InnovationShaft1).Point(), world.Feature(WestIds.InnovationShaft1).Number("length")), (world.Feature(WestIds.InnovationShaft2).Point(), world.Feature(WestIds.InnovationShaft2).Number("length")), (world.Feature(WestIds.InnovationShaft3).Point(), world.Feature(WestIds.InnovationShaft3).Number("length")), (world.Feature(WestIds.InnovationShaft4).Point(), world.Feature(WestIds.InnovationShaft4).Number("length"))];
        public const float ShaftWidth = 5.2f, ShaftHeight = 19.4f, ShaftFin = 1.25f, ShaftLanding = 3.9f;

        /// <summary>
        /// Main entrance on the east wing's south-east face to the parking square (795677486 e8, 2 m north of its mid-point so the
        /// portal's glass screen covers game/gen/buildings/'s own door and canopy there).
        /// </summary>
        public static Vector2 MainDoor(IWorldModel world) => world.Feature(WestIds.InnovationDoor).Point();
        public const float MainFacing = 126.8f;
        public const float PortalWidth = 9.0f, PortalHeight = 6.2f, PortalDepth = 3.4f, PortalMember = 0.7f;
        /// <summary>The glass screen stands this far behind the portal's front, proud of anything game/gen/buildings/ hangs on the wall.</summary>
        public const float ScreenBack = 0.25f;
        /// <summary>The entrance sign panel for "创新中心".</summary>
        public const float SignHeight = 1.3f;
        public const float DoorWidth = 2.6f, DoorHeight = 2.8f;
    }

    /// <summary>
    /// 湿地栈道 (MAP-QSH-WEST; 19c): a timber boardwalk through the wetland low ground east of the levee and the wetland
    /// stream, from the A10 plaza (south mouth; design:west-a10-plaza in world/west.json) to the footway at the 西门车行桥's east head (north mouth), on the main network.
    /// No OSM line: the route is authored in world/west.json (初值), kept 10 m and more inside the low ground's east edge, swinging west to the
    /// stream once (the turn, with the pebble wading beach), and passing P11's spot (−193, −300) so the 250° view opens over
    /// the reeds. The bridges (湿地石桥 692240860, 西门车行桥) and the canal banks are game/gen/maps/qingshuihe/waterfront/'s (r005); this route touches neither.
    /// </summary>
    public static class Wetland
    {
        /// <summary>The 湿地公园 entity: the feature its seeds and probe name.</summary>
        public static readonly string Entity = WestIds.Node11020008575.Entity;
        /// <summary>Route control points (world x, z), south to north; smoothed by two Chaikin passes (ends kept). 初值.</summary>
        public static Vector2[] Route(IWorldModel world) => world.Feature(WestIds.BoardwalkRoute).Line().ToArray();
        /// <summary>Deck width, plank thickness, and the deck top over the paving level at the two mouths (flush, a 3 cm lip).</summary>
        public const float Width = 2.4f, Plank = 0.09f, Lip = 0.03f, PavingTop = GroundLevels.Paving;
        /// <summary>game/gen/ground/ draws grass this high over the terrain (GroundSection.GrassY): the beach and the pebbles sit on it.</summary>
        public const float GrassTop = GroundLevels.Lawn;
        /// <summary>Granite sill across each mouth, along the route.</summary>
        public const float Sill = 0.6f;
        /// <summary>Stringers along both edges (收边): width, depth below the deck top.</summary>
        public const float StringerWidth = 0.09f, StringerDepth = 0.30f;
        /// <summary>Railing (SYS-WALK: barrier top 1.5 m; height is the railing part's): posts, handrail, one mid rail.</summary>
        public static readonly float RailHeight = new Campus.Contracts.Parts.RailingParams().Height;
        public const float PostSize = 0.10f, PostPitch = 2.0f, RailInset = 0.06f;
        public const float HandrailWidth = 0.14f, HandrailDepth = 0.07f, MidRail = 0.5f, MidRailSize = 0.05f;
        /// <summary>Piles under the deck: pitch along the route, radius, how deep they go into the mud.</summary>
        public const float PilePitch = 3.0f, PileRadius = 0.085f, PileSink = 0.4f;
        /// <summary>Colliders: the deck in 2 m slabs (SYS-WALK), this thick under the deck top.</summary>
        public const float SlabLength = 2.0f, SlabThick = 0.2f;

        /// <summary>
        /// Low boardwalk lamps (低位步道灯, §10.1, MAP-QSH-ADDITIONS adapted to a deck): dark grey round columns 0.62 m high
        /// on the deck edge away from the stream, a frosted band under the cap throwing light down on the planks; every 16 m.
        /// Both water sides stay dark for the fireflies; the emitter is landscape grade (lamp.landscape) with a short pool. 初值.
        /// </summary>
        public const float LampPitch = 16f, LampStart = 6f, LampHeight = 0.62f, LampRadius = 0.07f, LampInset = 0.22f, LampEnergy = 0.3f, LampRange = 3.5f;

        /// <summary>
        /// 涉水卵石滩 (§12 涉水区): on the stream's east soft bank, below the stone bridge (downstream, north) at the boardwalk's
        /// turn; about 12.5 m along the stream, from 0.9 m inside the water edge to the deck. A gap in the rail with timber steps
        /// leads down to it. Its wading polygon is <see cref="WadeRing"/> (read from the layout overlay; terrain wading is registered separately). 初值.
        /// </summary>
        public static Vector2 BeachCentre(IWorldModel world) => world.Feature(WestIds.WetlandBeach).Point();
        /// <summary>The beach's half axes along and across the stream, and its long axis (the stream's line there).</summary>
        public const float BeachHalfLength = 6.3f, BeachHalfWidth = 2.7f;
        public static readonly Vector2 BeachAxis = new(0.0054f, -1f);
        /// <summary>The rail gap and its steps down to the beach (a stoop of timber blocks), centred on the route opposite the beach.</summary>
        public const float BeachGap = 2.2f, StepTread = 0.36f;
        /// <summary>Pebbles strewn on the beach (L0 only): count and their radius range.</summary>
        public const int Pebbles = 46;
        public const float PebbleMin = 0.07f, PebbleMax = 0.26f;
        /// <summary>The wading polygon (水深不到 0.3 m): the beach's wet half, from the bank top into the stream.</summary>
        public static IReadOnlyList<Vector2> WadeRing(IWorldModel world) => world.Feature(WestIds.WetlandWade).Outer();

        /// <summary>Q2 (walk, K1): P11's spot on the deck, eye height, looking 250° over the reeds.</summary>
        public static readonly Vector2 P11 = new(-193.4f, -300f);
        public const float P11Bearing = 250f;
    }

    /// <summary>
    /// 渠堤台阶 (19a 接续; MAP-QSH-TERRAIN "短陡、长缓"): where the stone-bridge footway (692240861) and the lake-mouth
    /// path (544161583) leave the +0.9 levee top, a flight of stone garden steps runs down the levee's 1:3 east flank to the 0 m
    /// path (the 5 % ramps at the levee's two ends are the long way). Built from the terrain itself: the flight spans the flank
    /// between the crest edge and the slope foot along the path, and is left out where the ground has no rise (flat stub).
    /// </summary>
    public static class LeveeSteps
    {
        /// <summary>Junction on the levee centreline and a point down the path (world x, z).</summary>
        public static (string Key, Vector2 Top, Vector2 Toward)[] Junctions(IWorldModel world) =>
        [
            ("stone-bridge", world.Feature(WestIds.LeveeStoneBridge).PointPair().From, world.Feature(WestIds.LeveeStoneBridge).PointPair().To),
            ("lake-mouth", world.Feature(WestIds.LeveeLakeMouth).PointPair().From, world.Feature(WestIds.LeveeLakeMouth).PointPair().To),
        ];
        /// <summary>Levee crest half width and side run per metre of rise (west.terrain.json design:terrain:west-levee: width 4, 1:3).</summary>
        public static TerrainSlopeOp Levee(IWorldModel world) => (TerrainSlopeOp)world.TerrainOperations[WestIds.Levee];
        public static float CrestHalf(IWorldModel world) => (float)Levee(world).Width!.Value * 0.5f;
        public static float SideRun(IWorldModel world) => float.Parse(Levee(world).Side.Fill.AsSpan(2), System.Globalization.CultureInfo.InvariantCulture);
        public static float LeveeRise(IWorldModel world) => (float)Levee(world).Y0;
        /// <summary>Outer flight width (path 2.5 m plus cheek walls), the target riser, and the least rise worth a flight.</summary>
        public const float Width = 3.1f, Riser = 0.15f, MinRise = 0.3f;
    }

    /// <summary>
    /// 西门桥头候车亭 (MAP-QSH-WEST 天气要点): the OSM shelter footprint qsh:way:604832425 (14 × 3.2 m, west of the
    /// bridgehead service road; its west edge is the back line, read from the world model) built here instead of game/gen/buildings/'s
    /// box: a raised granite platform, slim aluminium posts along the
    /// back, a thin flat roof cantilevered over the platform with front and back eaves (rain curtains hang from both), a glass
    /// back screen, a timber bench, a downlight strip under the roof. Open to the road (east). The stop sign is game/gen/furniture/'s.
    /// </summary>
    public static class Shelter
    {
        public static readonly string Entity = WestIds.Way604832425.Entity;
        /// <summary>Platform depth from the back line (the footprint's west edge) toward the road, its rise over the ground and its margin past the roof ends.</summary>
        public const float Depth = 3.25f, PlatformRise = 0.18f, PlatformMargin = 0.35f;
        /// <summary>Posts along the back and their section; roof underside over the platform, slab, overhangs, fascia band.</summary>
        public const int Posts = 5;
        public const float PostSize = 0.12f, Clear = 2.75f, RoofThick = 0.16f, RoofFront = 0.45f, RoofBack = 0.35f, Band = 0.05f;
        /// <summary>Glass back screen and the two end screens: foot gap, top, and how far the end screens reach forward.</summary>
        public const float ScreenFoot = 0.15f, ScreenTop = 2.35f, EndScreen = 1.4f;
        /// <summary>Bench: seat height, depth, and its gap in front of the back screen; it spans the inner bays.</summary>
        public const float SeatHeight = 0.45f, SeatDepth = 0.42f, SeatOff = 0.12f;
        /// <summary>Roof downlight strip (候车亭顶灯): street-grade emitters, one per bay, a soft pool on the platform. 初值.</summary>
        public const float LightEnergy = 0.45f, LightRange = 4.5f;
    }

    /// <summary>
    /// 国防科技园石刻 (MAP-QSH-WEST): one rough-hewn white lying stone in the green wedge north of the bridgehead junction,
    /// its inscribed face turned south-south-west toward the junction (bearing 200°), so the low evening sun rakes it and it reads from the
    /// west gate road. The inscription "国防科技园" uses the shared scene-text atlas. The
    /// game/gen/maps/qingshuihe/waterfront/ bridge-head stone (design:waterfront-stone-ximen, −238.4, −411.9) stands 14 m away across the road.
    /// </summary>
    public static class Carving
    {
        /// <summary>The carving's node; the stone lies at the node's anchor in the world model.</summary>
        public static readonly string Entity = WestIds.Node6784744448.Entity;
        public const float Facing = 200f;
        /// <summary>Scale of the motto stone's faceted body: about 3.0 × 1.15 m in plan and 1.1 m high.</summary>
        public static readonly Vector3 Scale = new(0.45f, 0.62f, 0.62f);
        /// <summary>Its box collider (SYS-WALK): length, depth and height.</summary>
        public const float Length = 3.0f, Depth = 1.2f, Height = 1.1f;
    }
}
