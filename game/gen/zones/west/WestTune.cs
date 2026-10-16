using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.Zones.West;

/// <summary>
/// Every tunable value of the west zone in one place, for review (ENG-TEAM §0.1): materials as colour names, and the
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
    }

    /// <summary>
    /// 六号科研楼 (MAP-QSH-WEST §1), named by the OSM blocks A, B and C. Three claimed entities: A and B are their own entities;
    /// the C block, the two links and the north tower are the parts of qsh:relation:14153711 (the duplicate C shell
    /// qsh:way:779496166 is hidden in west.overlay.json). Every block stands on a two-storey glass base set back under the
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
        /// west.overlay.json's design:west-six-forecourt.
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
        public const float Depth = 1.7f;
        public const float Height = 2.05f;
        /// <summary>Two lines "求实求真" / "大气大为" layout slots; blank until the scene text atlas (contract-26): 校训石正面"求实求真 大气大为".</summary>
        public const float CharacterSize = 0.56f;
        public const float CharacterPitch = 0.72f;
        public const float LineGap = 0.78f;
        /// <summary>Granite pad round the foot (石脚的分界线) and the low lawn-lamp spot on the P10 side, for WP-39.</summary>
        public const float PadMargin = 0.5f;
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
}
