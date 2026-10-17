using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.Zones.West;

/// <summary>
/// Every tunable value of the west zone in one place, for review (WF-FLOW §4): materials as colour names, and the
/// dimensions of each site. Coordinates are world (x, z) in metres from the OSM footprints in campus.json. Values marked
/// "初值" are starting values awaiting the main session's call. 19b and 19c add their own nested classes after these.
/// </summary>
public static class WestTune
{
    public const int BudgetTrianglesL0 = 90_000;
    public const int BudgetTrianglesL1 = 40_000;
    public const int BudgetDrawCalls = 20;

    /// <summary>
    /// Window numbering (contract-29): 六号's stone blocks follow the lab curve (about 0.3 all night, SYS-TIME §6) with
    /// W-LAB : W-OFF = 7 : 3 (WLD-INTERIORS §5.1: 5 office, 6 lab), as the research family elsewhere; the zone stays quiet at night.
    /// </summary>
    public static class Windows
    {
        public static readonly ChannelId Research = new("window.lab");
        public const byte Office = 5, Lab = 6;
        /// <summary>Tenths of the windows that show an office card instead of a lab.</summary>
        public const int OfficeShare = 3;
    }

    /// <summary>Materials (ART-STYLE §8 colour names only).</summary>
    public static class Look
    {
        private static ToonSpec S(string family, ToonFx fx = ToonFx.None, LineClass line = LineClass.Building, float wet = 0.6f) =>
            new(new(family + ".lit"), new(family + ".shade")) { Fx = fx, Line = line, Wet = wet };

        /// <summary>六号's dry-hung stone: pale warm grey (浅暖灰石材, MAP-QSH-WEST §1.3).</summary>
        public static readonly ToonSpec Stone = S("stone.beige", ToonFx.BevelLift);
        /// <summary>Copings, sills, soffit fascias and column caps: grey-white, fine lines so trims do not hatch the facade.</summary>
        public static readonly ToonSpec Trim = S("wall.greyWhite", ToonFx.BevelLift, LineClass.Fine);
        /// <summary>The continuous louvre strip on every floor (深灰水平带) and the plinth: dark grey.</summary>
        public static readonly ToonSpec DarkGrey = S("roof.grey", ToonFx.BevelLift, LineClass.Fine, 0.8f);
        /// <summary>Louvre slats, mullions and door frames: grey aluminium.</summary>
        public static readonly ToonSpec Metal = S("frame.alu", ToonFx.BevelLift, LineClass.Fine);
        public static readonly ToonSpec RoofSlab = S("roof.grey", ToonFx.Mottle, LineClass.Building, 0.85f);
        public static readonly ToonSpec Glass = new(new("glass.lit"), new("glass.shade"))
        { Kind = ToonKind.Glass, Fx = ToonFx.GlassStreak | ToonFx.Glare, Wet = 0 };
        /// <summary>Entrance aprons and the stone plaza kerbs: light granite.</summary>
        public static readonly ToonSpec Granite = S("stone.beige", ToonFx.BevelLift, LineClass.Fine);
        /// <summary>The campus red (equip.fireRed, Opus review 10:07): 六号's emblem and the motto's lacquered characters.</summary>
        public static readonly ToonSpec Red = S("equip.fireRed", ToonFx.None, LineClass.None, 0.3f);
        /// <summary>The silver characters under the emblem: brushed metal with a rim and an anisotropic band (ART-STYLE §2).</summary>
        public static readonly ToonSpec Silver = S("roof.silverMetal", ToonFx.Rim | ToonFx.AnisoBand, LineClass.Fine, 0.5f);
        /// <summary>The motto stone: one rough cream-white boulder (米白粗粝块面石, MAP-QSH-WEST §2.2), faceted normals.</summary>
        public static readonly ToonSpec CreamStone = S("wall", ToonFx.BevelLift);
        /// <summary>
        /// 深蓝玻璃 (19b): 四号's entrance boxes, the campus tech blue as glass (streaked, glaring), so the boxes read blue and
        /// clear, not black. WP-04's glass.deep is an extension colour the byte palette path cannot carry yet; 初值, the main
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
        public static readonly ToonSpec Hedge = S("hedge", ToonFx.Mottle, LineClass.Tree, 0.5f);

        // ---- 19c: wetland, bridgehead and stone carving ----
        /// <summary>Boardwalk planks and handrails (栈道木板): warm light timber, mottled, soft wet darkening.</summary>
        public static readonly ToonSpec Deck = S("panel.brown", ToonFx.Mottle | ToonFx.BevelLift, LineClass.Fine, 0.7f);
        /// <summary>Boardwalk stringers, rail posts and piles: the same timber a step darker, so the deck edge reads as a clean line.</summary>
        public static readonly ToonSpec DeckDark = S("base.warmBrown", ToonFx.BevelLift, LineClass.Fine, 0.7f);
        /// <summary>The low boardwalk lamp's body (深灰色圆柱灯, MAP-QSH-ADDITIONS §2.5): dark grey, fine lines.</summary>
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
    /// 六号科研楼 (MAP-QSH-WEST §1), named by the OSM blocks A, B and C. Three claimed entities: A and B are their own entities;
    /// the C block, the two links and the north tower are the parts of qsh:relation:14153711 (the duplicate C shell
    /// qsh:way:779496166 is hidden in overlay/west.d/). Every block stands on a two-storey glass base set back under the
    /// stone (底层两层通高、向内退的深色玻璃幕墙), with deep square windows above and one dark louvre strip per floor.
    /// </summary>
    public static class Six
    {
        public const string EntityA = "qsh:way:779496168";
        public const string EntityB = "qsh:way:779496169";
        public const string EntityC = "qsh:relation:14153711";
        public const string PartA = "osm:way:779496168";
        public const string PartB = "osm:way:779496169";
        /// <summary>The C block with its courtyard (the source the 13:06 dedupe kept).</summary>
        public const string PartC = "osm:relation:10807930";
        /// <summary>B–C link: the 2F outdoor bridge over the drive-through (§1.6).</summary>
        public const string LinkBC = "osm:relation:14153711";
        /// <summary>A–B link: a two-storey glass link (门厅或连廊).</summary>
        public const string LinkAB = "osm:way:950008613";
        /// <summary>North tower: the stair and lift core, 7 storeys, 23.1 m.</summary>
        public const string Tower = "osm:way:779496167";

        /// <summary>The two-storey glass base (两层通高) and the depth it is set back behind the stone line.</summary>
        public const float BaseHeight = 9.0f;
        public const float BaseStorey = 4.5f;
        public const float Recess = 2.2f;
        /// <summary>Upper storeys above the base: A 9 floors in all (41.5 m), B and C 7 (32.5 m) (§1.1, 数据定位).</summary>
        public const int UpperA = 7, UpperB = 5, UpperC = 5;
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
        /// C 区东门 (WLD-INTERIORS §2.6, interiors.json six.c.east): door centre on the stone line, facing 77°, floor ±0 as the
        /// entrance record says. A glass vestibule reaches from the set-back glass out to the stone line; its doors stand open.
        /// </summary>
        public static readonly Vector2 EastDoor = new(-588.3f, 230f);
        public const float VestibuleWidth = 5.2f;
        public const float VestibuleHeight = 4.2f;
        public const float DoorWidth = 2.4f;
        public const float DoorHeight = 2.7f;
        /// <summary>
        /// The granite threshold apron in front of the open door, 3 cm proud of the forecourt paving (WP-15 draws paving at
        /// <see cref="PavingTop"/>; the interior floor is ±0 in interiors.json, so no risers); the forecourt itself is
        /// overlay/west.d/'s design:west-six-forecourt.
        /// </summary>
        public const float ApronDepth = 4.0f;
        /// <summary>Top of WP-15's paving (GroundSection.PavingY); the paving under the overhang sits 1 cm above it.</summary>
        public const float PavingTop = 0.15f;
        public const float ApronWidth = 7.6f;

        /// <summary>Shut entrances (glass doors, lobby card behind, DoorClosed): one on A, one on B, one on C's south-east face. 初值.</summary>
        public static readonly Vector2[] ShutDoors = [new(-804.19f, 160.62f), new(-728.73f, 217.39f), new(-601.64f, 275.48f)];
        public const float ShutDoorWidth = 2.0f;
        public const float ShutDoorHeight = 2.6f;

        /// <summary>
        /// The crown sign on C's east face (§1.2, ART-STYLE §11), centred on the face above the door: a solid stone panel over the
        /// top two storeys rising above the parapet, the red emblem at its head, a line of silver characters below and a thin
        /// English line under them. Text slots are blank until the scene text atlas (contract-26): 楼顶"基础与前沿研究院"加一行英文.
        /// </summary>
        public const float SignWidth = 21f;
        public const float SignRise = 3.4f;
        public const float SignDrop = 6.0f;
        public const float SignProud = 0.95f;
        public const float EmblemDiameter = 3.6f;
        public const float EmblemDepth = 0.18f;
        public const int SignCharacters = 8;
        public const float CharacterSize = 1.75f;
        public const float CharacterGap = 0.42f;
        public const float EnglishHeight = 0.32f;
        public const float EnglishLength = 15.6f;

        /// <summary>B–C link: deck at the first-floor line, roof at the base top, glass railing on its long sides.</summary>
        public const float LinkDeck = 4.5f;
        public const float LinkPier = 1.1f;
        public const float RailHeight = 1.1f;
        /// <summary>A–B link: two-storey glass with a stone fascia.</summary>
        public const float LinkFascia = 1.1f;
        /// <summary>North tower height (数据定位: 7 storeys, 23.1 m).</summary>
        public const float TowerHeight = 23.1f;

        /// <summary>The blocks' common plan axis (A's south wing, B's first wing; C's faces run square to it).</summary>
        public static readonly Vector2 Axis = new(0.80023f, 0.59969f);
        /// <summary>Roof plant enclosures (fenced, in the middle of a wing) on A, B and C, set out along <see cref="Axis"/>. 初值.</summary>
        public static readonly Vector2[] Plants = [new(-804.1f, 99.8f), new(-732.2f, 117.3f), new(-607.1f, 267.4f)];
        public static readonly Vector2 PlantSize = new(8f, 5f);
        public const float PlantFence = 2.0f;
    }

    /// <summary>
    /// 校训石 (MAP-QSH-WEST §2): one cream-white boulder lying on its side, about 2 m high and 6.6 m long, in front of
    /// 六号's C east door; its dressed face turns toward P10 (−515, 250) so the red characters read from the drop point.
    /// </summary>
    public static class Motto
    {
        public const string Entity = "qsh:node:11361252903";
        public static readonly Vector2 At = new(-554.76f, 233.83f);
        /// <summary>Bearing its inscribed face looks along (toward P10).</summary>
        public const float Facing = 112f;
        public const float Length = 6.6f;
        public const float Depth = 1.85f;
        public const float Height = 1.8f;
        /// <summary>Two lines "求实求真" / "大气大为" layout slots; blank until the scene text atlas (contract-26): 校训石正面"求实求真 大气大为".</summary>
        public const float CharacterSize = 0.56f;
        public const float CharacterPitch = 0.72f;
        public const float LineGap = 0.78f;
        /// <summary>Granite pad round the foot (石脚的分界线) and the low lawn-lamp spot on the P10 side, for WP-39.</summary>
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
    /// 四号科研楼 (MAP-QSH-WEST §6): the three courtyard blocks are WP-14's (family research, overlay/west.d/ style); this
    /// zone adds what the rules cannot: each block's entrance, a two-storey deep-blue glass box in a white door frame with a
    /// sign band (§6.3), shut (WLD-INTERIORS §5.2: glass doors, lobby card, DoorClosed); and the fan lawn's 0.45 m seat wall
    /// on its outer arc (MAP-QSH-TERRAIN §10.2; the lawn's dish is west.terrain.json's basin).
    /// </summary>
    public static class Four
    {
        public const string EntityA = "qsh:relation:11334255";
        public const string EntityB = "qsh:relation:11665641";
        public const string EntityC = "qsh:relation:11334253";

        /// <summary>
        /// Door centres on each block's stone line toward the lawn, and the bearing the box faces: A's south face, B's
        /// south-east face, C's east face (osm:relation:11334255 e2, 11665641 e1, 11334252 e1, mid-points). 初值.
        /// </summary>
        public static readonly (string Entity, string Key, Vector2 Door, float Facing)[] Boxes =
        [
            (EntityA, "a", new(-306.69f, -357.83f), 178.4f),
            (EntityB, "b", new(-359.35f, -322.52f), 128.4f),
            (EntityC, "c", new(-384.75f, -262.87f), 96.8f),
        ];

        /// <summary>Glass box: width along the face, how far it stands out, and its height (two storeys of WP-14's 4.2 m research storey).</summary>
        public const float GlassWidth = 7.2f, GlassDepth = 3.6f, GlassHeight = 8.4f;
        /// <summary>The white frame round it: member thickness, how far it stands proud of the glass, and the sign band on its head.</summary>
        public const float FrameMember = 0.65f, FrameProud = 0.55f, SignBand = 1.15f;
        /// <summary>Mullion pitch on the glass and the transom at the first-floor line.</summary>
        public const float MullionPitch = 1.2f, Transom = 4.2f;
        public const float DoorWidth = 2.4f, DoorHeight = 2.7f;
        /// <summary>Granite landing in front of the box, 3 cm proud of WP-15's paving.</summary>
        public const float LandingDepth = 2.4f, PavingTop = 0.15f;

        /// <summary>
        /// The fan lawn (osm:way:803693984–803693986): its outer arc is the lawn's edge along the ring road; the seat wall runs on
        /// these edges, inset toward the dish centre, so the radial paths pass through its gaps. Points run along the arc. 初值.
        /// </summary>
        public static readonly Vector2[][] SeatArcs =
        [
            [new(-335f, -211f), new(-344f, -220f), new(-352f, -230f), new(-357f, -241f), new(-360f, -252f)],
            [new(-361f, -259f), new(-360f, -271f), new(-359f, -282f), new(-357f, -291f), new(-353f, -300f)],
            [new(-343f, -316f), new(-334f, -324f), new(-326f, -330f), new(-320f, -333f), new(-315f, -334f), new(-311f, -334.5f)],
        ];
        /// <summary>The dish centre the arcs are inset toward (MAP-QSH-TERRAIN §10.2: about (−324, −275), radius 32, 0.45 deep).</summary>
        public static readonly Vector2 LawnCentre = new(-324f, -275f);
        public const float LawnRadius = 32f, LawnDepth = 0.45f;
        public const float SeatInset = 0.8f, SeatHeight = 0.45f, SeatWidth = 0.5f, SeatCap = 0.08f;
    }

    /// <summary>
    /// 创新中心 (MAP-QSH-WEST §3, MAP-QSH-RENOVATION W-01): the two six-storey bars and the two four-storey cross wings are WP-14's
    /// (research, with its rooftop plant enclosure and stair penthouses under the main coping); this zone builds the dark glass
    /// stair shafts where the wings meet the bars (§3.3) and the main entrance frame on the east wing's face to the parking
    /// square, with the slot for the red-brown calligraphy sign (§3.2) left blank until contract-26.
    /// </summary>
    public static class Innovation
    {
        public const string Entity = "qsh:relation:11032065";
        /// <summary>The cross wings' long axis (north to south).</summary>
        public static readonly Vector2 WingAxis = new(-0.5990f, 0.8007f);
        /// <summary>
        /// The four links between wings and bars (osm:way:795677485 / 795677486 necks): centre and the link's length along the
        /// wing axis between the two buildings. The glass shaft is a little wider than the neck. 初值.
        /// </summary>
        public static readonly (Vector2 Centre, float Length)[] Shafts =
        [
            (new(-591.9f, 77.2f), 6.2f),
            (new(-626.9f, 119.7f), 7.0f),
            (new(-553.4f, 106.4f), 6.6f),
            (new(-586.7f, 149.6f), 7.6f),
        ];
        public const float ShaftWidth = 5.2f, ShaftHeight = 19.4f, ShaftFin = 1.25f, ShaftLanding = 3.9f;

        /// <summary>
        /// Main entrance on the east wing's south-east face to the parking square (795677486 e8, 2 m north of its mid-point so the
        /// portal's glass screen covers WP-14's own door and canopy there).
        /// </summary>
        public static readonly Vector2 MainDoor = new(-564.70f, 134.12f);
        public const float MainFacing = 126.8f;
        public const float PortalWidth = 9.0f, PortalHeight = 6.2f, PortalDepth = 3.4f, PortalMember = 0.7f;
        /// <summary>The glass screen stands this far behind the portal's front, proud of anything WP-14 hangs on the wall.</summary>
        public const float ScreenBack = 0.25f;
        /// <summary>The sign slot on the portal head, for "创新中心" in red-brown calligraphy (blank until contract-26).</summary>
        public const float SignHeight = 1.3f;
        public const float DoorWidth = 2.6f, DoorHeight = 2.8f;
    }

    /// <summary>
    /// A10 湿地 (MAP-QSH-WEST §12 锚点表; 设计增补): a small paved plaza at the boardwalk's south mouth, where the wetland loop
    /// path meets the stone-bridge approach (−239.8, −155.1), on dry ground east of the low ground. The post stands on the
    /// plaza's east edge facing 250° over the reeds, so its warm lamp falls on the paving and leaves the water side dark.
    /// </summary>
    public static class A10
    {
        /// <summary>Plaza centre and its long axis (bearing 250°), 9.8 × 7 m with chamfered corners.</summary>
        public static readonly Vector2 Centre = new(-236.5f, -158.1f);
        public static readonly Vector2 Post = new(-232.46f, -159.57f);
        public const float Facing = 250f;
        public static readonly Vector2[] Plaza =
            [new(-241.01f, -156.46f), new(-240.7f, -160.29f), new(-234.31f, -162.62f), new(-232.35f, -161.31f), new(-231.25f, -158.31f), new(-231.92f, -156.04f), new(-238.31f, -153.72f)];
        /// <summary>The plaza grown by 1.5 m: the hole left in the wetland low ground, so the plaza and its rim stay at 0 m.</summary>
        public static readonly Vector2[] Dry =
            [new(-242.51f, -155.91f), new(-242.0f, -161.52f), new(-234.2f, -164.36f), new(-231.05f, -162.42f), new(-229.55f, -158.29f), new(-230.72f, -154.78f), new(-238.51f, -151.94f)];
    }

    /// <summary>
    /// 湿地栈道 (MAP-QSH-WEST §10.2, §12; 19c): a timber boardwalk through the wetland low ground east of the levee and the wetland
    /// stream, from the A10 plaza (south mouth) to the footway at the 西门车行桥's east head (north mouth), on the main network.
    /// No OSM line: the route is drawn here (初值), kept 10 m and more inside the low ground's east edge, swinging west to the
    /// stream once (the turn, with the pebble wading beach), and passing P11's spot (−193, −300) so the 250° view opens over
    /// the reeds. The bridges (湿地石桥 692240860, 西门车行桥) and the canal banks are WP-28's (r005); this route touches neither.
    /// </summary>
    public static class Wetland
    {
        /// <summary>The 湿地公园 entity: the feature its seeds and probe name.</summary>
        public const string Entity = "qsh:node:11020008575";
        /// <summary>Route control points (world x, z), south to north; smoothed by two Chaikin passes (ends kept). 初值.</summary>
        public static readonly Vector2[] Route =
        [
            new(-236.6f, -162.4f), new(-233.5f, -176f), new(-236.0f, -190f), new(-236.4f, -203f), new(-233.0f, -216f),
            new(-221.5f, -238f), new(-205.5f, -262f), new(-195.0f, -287f), new(-193.0f, -312f), new(-198.0f, -340f),
            new(-206.0f, -361f), new(-211.5f, -372.9f),
        ];
        /// <summary>Deck width, plank thickness, and the deck top over the paving level at the two mouths (flush, a 3 cm lip).</summary>
        public const float Width = 2.4f, Plank = 0.09f, Lip = 0.03f, PavingTop = 0.15f;
        /// <summary>WP-15 draws grass this high over the terrain (GroundSection.GrassY): the beach and the pebbles sit on it.</summary>
        public const float GrassTop = 0.10f;
        /// <summary>Granite sill across each mouth, along the route.</summary>
        public const float Sill = 0.6f;
        /// <summary>Stringers along both edges (收边): width, depth below the deck top.</summary>
        public const float StringerWidth = 0.09f, StringerDepth = 0.30f;
        /// <summary>Railing (SYS-WALK §7: waterside rails ≥ 1.05 m, barrier top 1.5 m): posts, handrail, one mid rail.</summary>
        public const float RailHeight = 1.05f, RailBarrier = 1.5f, PostSize = 0.10f, PostPitch = 2.0f, RailInset = 0.06f;
        public const float HandrailWidth = 0.14f, HandrailDepth = 0.07f, MidRail = 0.5f, MidRailSize = 0.05f;
        /// <summary>Piles under the deck: pitch along the route, radius, how deep they go into the mud.</summary>
        public const float PilePitch = 3.0f, PileRadius = 0.085f, PileSink = 0.4f;
        /// <summary>Colliders: the deck in 2 m slabs (SYS-WALK §13), this thick under the deck top.</summary>
        public const float SlabLength = 2.0f, SlabThick = 0.2f;

        /// <summary>
        /// Low boardwalk lamps (低位步道灯, §10.1, MAP-QSH-ADDITIONS §2.5 adapted to a deck): dark grey round columns 0.62 m high
        /// on the deck edge away from the stream, a frosted band under the cap throwing light down on the planks; every 16 m.
        /// Both water sides stay dark for the fireflies; the emitter is landscape grade (lamp.landscape) with a short pool. 初值.
        /// </summary>
        public const float LampPitch = 16f, LampStart = 6f, LampHeight = 0.62f, LampRadius = 0.07f, LampInset = 0.22f, LampEnergy = 0.3f, LampRange = 3.5f;

        /// <summary>
        /// 涉水卵石滩 (§12 涉水区): on the stream's east soft bank, below the stone bridge (downstream, north) at the boardwalk's
        /// turn; about 12.5 m along the stream, from 0.9 m inside the water edge to the deck. A gap in the rail with timber steps
        /// leads down to it. Its wading polygon is <see cref="WadeRing"/> (registration pending: the stream is a channel op,
        /// which has no wade list; Astra decides, max-116 19c). 初值.
        /// </summary>
        public static readonly Vector2 BeachCentre = new(-240.4f, -203.2f);
        /// <summary>The beach's half axes along and across the stream, and its long axis (the stream's line there).</summary>
        public const float BeachHalfLength = 6.3f, BeachHalfWidth = 2.7f;
        public static readonly Vector2 BeachAxis = new(0.0054f, -1f);
        /// <summary>The rail gap and its steps down to the beach (a stoop of timber blocks), centred on the route opposite the beach.</summary>
        public const float BeachGap = 2.2f, StepTread = 0.36f;
        /// <summary>Pebbles strewn on the beach (L0 only): count and their radius range.</summary>
        public const int Pebbles = 46;
        public const float PebbleMin = 0.07f, PebbleMax = 0.26f;
        /// <summary>The wading polygon (水深不到 0.3 m): the beach's wet half, from the bank top into the stream.</summary>
        public static readonly Vector2[] WadeRing =
            [new(-241.2f, -196.9f), new(-243.1f, -197.6f), new(-243.4f, -203.2f), new(-243.1f, -208.8f), new(-241.2f, -209.5f)];

        /// <summary>Q2 (walk, K1): P11's spot on the deck, eye height, looking 250° over the reeds.</summary>
        public static readonly Vector2 P11 = new(-193.4f, -300f);
        public const float P11Bearing = 250f;
    }

    /// <summary>
    /// 渠堤台阶 (19a 接续; MAP-QSH-TERRAIN §6.3 "短陡、长缓"): where the stone-bridge footway (692240861) and the lake-mouth
    /// path (544161583) leave the +0.9 levee top, a flight of stone garden steps runs down the levee's 1:3 east flank to the 0 m
    /// path (the 5 % ramps at the levee's two ends are the long way). Built from the terrain itself: the flight spans the flank
    /// between the crest edge and the slope foot along the path, and is left out where the ground has no rise (flat stub).
    /// </summary>
    public static class LeveeSteps
    {
        /// <summary>Junction on the levee centreline and a point down the path (world x, z).</summary>
        public static readonly (string Key, Vector2 Top, Vector2 Toward)[] Junctions =
        [
            ("stone-bridge", new(-296.28f, -164.94f), new(-283.7f, -160.1f)),
            ("lake-mouth", new(-304.0f, -154.9f), new(-285.8f, -147.8f)),
        ];
        /// <summary>Levee crest half width and side run per metre of rise (west.terrain.json design:terrain:west-levee: width 4, 1:3).</summary>
        public const float CrestHalf = 2.0f, SideRun = 3.0f, LeveeRise = 0.9f;
        /// <summary>Outer flight width (path 2.5 m plus cheek walls), the target riser, and the least rise worth a flight.</summary>
        public const float Width = 3.1f, Riser = 0.15f, MinRise = 0.3f;
    }

    /// <summary>
    /// 西门桥头候车亭 (MAP-QSH-WEST §10.3, §11 天气要点): the OSM shelter footprint qsh:way:604832425 (14 × 3.2 m, west of the
    /// bridgehead service road) built here instead of WP-14's box: a raised granite platform, slim aluminium posts along the
    /// back, a thin flat roof cantilevered over the platform with front and back eaves (rain curtains hang from both), a glass
    /// back screen, a timber bench, a downlight strip under the roof. Open to the road (east). The stop sign is WP-13's.
    /// </summary>
    public static class Shelter
    {
        public const string Entity = "qsh:way:604832425";
        /// <summary>The back line (west edge of the footprint), south end then north end; the platform opens to its right (the road).</summary>
        public static readonly Vector2 BackA = new(-214.98f, -417.82f), BackB = new(-210.09f, -430.99f);
        /// <summary>Platform depth from the back line toward the road, its rise over the ground and its margin past the roof ends.</summary>
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
    /// 国防科技园石刻 (MAP-QSH-WEST §9.1): one rough-hewn white lying stone in the green wedge north of the bridgehead junction,
    /// its inscribed face turned south-south-west toward the junction (bearing 200°), so the low evening sun rakes it and it reads from the
    /// west gate road. The inscription "国防科技园" (墨绿行书) is left blank until the scene text atlas (contract-26). The
    /// WP-28 bridge-head stone (design:waterfront-stone-ximen, −238.4, −411.9) stands 14 m away across the road.
    /// </summary>
    public static class Carving
    {
        public const string Entity = "qsh:node:6784744448";
        public static readonly Vector2 At = new(-247.0f, -423.0f);
        public const float Facing = 200f;
        /// <summary>Scale of the motto stone's faceted body: about 3.0 × 1.15 m in plan and 1.1 m high.</summary>
        public static readonly Vector3 Scale = new(0.45f, 0.62f, 0.62f);
        /// <summary>Its box collider (SYS-WALK §13): length, depth and height.</summary>
        public const float Length = 3.0f, Depth = 1.2f, Height = 1.1f;
    }
}
