using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Parts;
using static Campus.Gen.Sites.ZoneLooks;
using FacadeRules = Campus.Gen.Buildings.Rules.FacadeRules;

namespace Campus.Gen.Maps.Qingshuihe.South;

/// <summary>
/// Every tunable value of the south zone in one place, for review (WF-FLOW): materials as colour names, and the shaping
/// dimensions of each site. Positions, outlines, storey counts, heights and sizes the map data already carries are read from the
/// world model by feature ID (D-239), not copied here; what stays is the zone's own shaping, with a note where a value is near
/// the data but kept for a reason. Local frames are described per block. Values marked "初值" are starting values awaiting the
/// main session's call.
/// </summary>
public static class SouthTune
{
    public const int BudgetTrianglesL0 = 90_000;
    public const int BudgetTrianglesL1 = 45_000;
    public const int BudgetDrawCalls = 18;

    /// <summary>Materials (ART-STYLE colour names only).</summary>
    public static class Look
    {
        /// <summary>Main building and gate: light grey-white stone (MAP-QSH-SOUTH).</summary>
        public static readonly ToonSpec Stone = S("wall.greyWhite", ToonFx.BevelLift);
        /// <summary>Copings, column bases and capitals, string courses, name-wall slabs.</summary>
        public static readonly ToonSpec StoneTrim = S("stone.beige", ToonFx.BevelLift, LineClass.Fine);
        /// <summary>Plinths and step bodies.</summary>
        public static readonly ToonSpec Granite = S("stone.beige", ToonFx.BevelLift);
        public static readonly ToonSpec Paving = S("ground.paver", ToonFx.None, LineClass.None);
        public static readonly ToonSpec Concrete = S("ground.concrete", ToonFx.None, LineClass.Fine);
        public static readonly ToonSpec RoofSlab = S("roof.grey", ToonFx.Mottle, LineClass.Building, 0.85f);
        public static readonly ToonSpec Metal = S("frame.alu", ToonFx.BevelLift, LineClass.Fine);
        /// <summary>Peacock-house steel, flagpole, barrier arms: painted white.</summary>
        public static readonly ToonSpec WhiteSteel = S("wall.greyWhite", ToonFx.None, LineClass.Fine);
        /// <summary>Cage wire: no outline, so the 2.5 cm mesh reads as a light veil instead of dark hatching.</summary>
        public static readonly ToonSpec WhiteWire = S("wall.greyWhite", ToonFx.None, LineClass.None);
        public static readonly ToonSpec Wood = S("panel.brown", ToonFx.BevelLift, LineClass.Fine);
        public static readonly ToonSpec Marking = S("ground.markingYellow", ToonFx.None, LineClass.None);

        /// <summary>Xuanbang: beige-grey stone walls, piers, columns and the clock tower (MAP-QSH-SOUTH).</summary>
        public static readonly ToonSpec Beige = S("stone.beige", ToonFx.BevelLift);
        /// <summary>Xuanbang cornices, sills, capitals, dentils and pediment frames: the lighter stone, fine line.</summary>
        public static readonly ToonSpec BeigeTrim = S("wall.greyWhite", ToonFx.BevelLift, LineClass.Fine);
        /// <summary>Dark grey window frames (§5.6) with no outline, so mullions do not hatch the facade at distance (18a review).</summary>
        public static readonly ToonSpec DarkFrame = S("roof.grey", ToonFx.None, LineClass.None);
        /// <summary>Electron-tube dome (§5.9): dark grey metal; no other surface uses it, so the dome stays a bundle of its own.</summary>
        public static readonly ToonSpec DomeMetal = S("roof.grey", ToonFx.BevelLift, LineClass.Building, 0.8f);
        /// <summary>Clock dial bezels, hour marks and pivots.</summary>
        public static readonly ToonSpec DialRim = S("roof.grey", ToonFx.BevelLift, LineClass.Fine, 0.7f);
        /// <summary>Clock dial faces; the night backlight channel comes with the lighting list.</summary>
        public static readonly ToonSpec Dial = S("wall.greyWhite", ToonFx.None, LineClass.Fine);
        /// <summary>Liren: white throughout (§6.2), trims in WP-14's warm white so A, B and the gateway read as one building.</summary>
        public static readonly ToonSpec White = S("wall.greyWhite", ToonFx.BevelLift);
        public static readonly ToonSpec WhiteTrim = S("wall", ToonFx.BevelLift, LineClass.Fine);
        public static readonly ToonSpec GreyPlinth = S("roof.grey", ToonFx.BevelLift);
        /// <summary>East lake hard bank (§4.3): brick-red retaining wall, red sandstone coping, white stone balusters (fine line).</summary>
        public static readonly ToonSpec BrickRed = S("brick", ToonFx.Mottle);
        public static readonly ToonSpec Sandstone = S("band.terracotta", ToonFx.BevelLift);
        public static readonly ToonSpec Baluster = S("wall.greyWhite", ToonFx.None, LineClass.Fine);
        /// <summary>Steps and treads with no per-riser outline (18a review): they read by lit treads and shaded risers.</summary>
        public static readonly ToonSpec StepStone = S("stone.beige", ToonFx.BevelLift, LineClass.None);
        /// <summary>Rowing boats (§4.6): warm wood outside, lighter boards inside.</summary>
        public static readonly ToonSpec BoatWood = S("door.wood", ToonFx.BevelLift, LineClass.Fine);
        public static readonly ToonSpec BoatInside = S("panel.brown", ToonFx.None, LineClass.Fine);
        /// <summary>
        /// Wu Liren statue, registered deep bronze. Curved metal: two-and-a-half steps, rim and anisotropic band (ART-STYLE).
        /// </summary>
        public static readonly ToonSpec Bronze = new(new("bronze.lit"), new("bronze.shade"))
        {
            Fx = ToonFx.Rim | ToonFx.AnisoBand,
            Steps = ShadeSteps.TwoHalf,
            Normal = ToonNormal.Smooth,
            Wet = 0.4f,
        };
        /// <summary>Polished black stone pedestal (ART-STYLE).</summary>
        public static readonly ToonSpec DarkStone = S("stone.black", ToonFx.BevelLift, LineClass.Building, 0.9f);
        /// <summary>Hexagonal pavilion (§4.5): red-brown columns, dark grey tiles.</summary>
        public static readonly ToonSpec RedTimber = S("band.terracotta", ToonFx.BevelLift, LineClass.Fine);
        public static readonly ToonSpec Tiles = S("roof.grey", ToonFx.Mottle, LineClass.Building, 0.85f);

        /// <summary>
        /// Gym (§7.1): the silver-grey roof and the bulging rib ring are one curved metal, two-and-a-half steps with rim and
        /// anisotropic band like the statue's bronze (ART-STYLE); smooth normals so the drum reads round, not faceted.
        /// </summary>
        public static readonly ToonSpec Silver = new(new("roof.silverMetal.lit"), new("roof.silverMetal.shade"))
        {
            Fx = ToonFx.Rim | ToonFx.AnisoBand,
            Steps = ShadeSteps.TwoHalf,
            Normal = ToonNormal.Smooth,
            Wet = 0.85f,
        };
        /// <summary>The gym's registered ochre-brown base band, smooth round.</summary>
        public static readonly ToonSpec Ochre = S("band.ochre", ToonFx.Mottle) with { Normal = ToonNormal.Smooth };
        /// <summary>The gym's tea-coloured curtain wall and entrance screen.</summary>
        public static readonly ToonSpec TeaGlass = Glass with { Lit = new("glass.tea.lit"), Shade = new("glass.tea.shade") };
        /// <summary>Brown horizontal louvres in front of the tea glass (§7.1).</summary>
        public static readonly ToonSpec Louvre = S("panel.brown", ToonFx.None, LineClass.Fine) with { Normal = ToonNormal.Smooth };
        /// <summary>Light blue-grey roof vent caps (§7.1): WP-12's equipment grey.</summary>
        public static readonly ToonSpec Vent = S("equip.grey", ToonFx.None, LineClass.Fine) with { Normal = ToonNormal.Smooth };
        /// <summary>Natatorium (§7.2): the low white dome, read by its two light steps (ART-STYLE), smooth normals.</summary>
        public static readonly ToonSpec DomeWhite = S("wall.greyWhite") with { Normal = ToonNormal.Smooth };
        /// <summary>
        /// The light concrete apron round the natatorium's foot (18c2 review: the ochre-red ring read as a running track next to
        /// the stadium): the kerb's light grey, no outline.
        /// </summary>
        public static readonly ToonSpec Apron = S("ground.curb", ToonFx.Mottle, LineClass.None);
        /// <summary>
        /// Sports platform (§7.6): the deck's top paving and joints, and the warm sand shop block under it (§7.4). The soffit,
        /// fascia and blank sign boards take <see cref="White"/>, the columns <see cref="DomeWhite"/>, so the venue cells stay
        /// inside the draw-call budget.
        /// </summary>
        public static readonly ToonSpec DeckPaving = S("ground.paver", ToonFx.Mottle, LineClass.None);
        public static readonly ToonSpec DeckJoint = S("ground.concrete", ToonFx.None, LineClass.None);
        public static readonly ToonSpec Sand = S("tile.canteen", ToonFx.Mottle);
        /// <summary>
        /// Stand (§7.3): one light concrete throughout, seats new and low in chroma: muted yellow in the middle, pale lilac grey to
        /// the sides (the palette has no seat colours: the yellow marking and the grey-pink umbrella are borrowed), the low seats
        /// opposite dark grey. The canopy is new silver-white panels (the gym's curved metal) with a pale gold middle section.
        /// </summary>
        public static readonly ToonSpec StandConcrete = S("ground.curb", ToonFx.BevelLift);
        /// <summary>Rows read by lit treads and shaded risers, with no outline per row (the 18a review's hatching on the grand steps).</summary>
        public static readonly ToonSpec TierRiser = S("ground.curb", ToonFx.None, LineClass.None);
        public static readonly ToonSpec SeatYellow = S("ground.markingYellow", ToonFx.None, LineClass.None);
        public static readonly ToonSpec SeatLilac = S("umbrella.graypink", ToonFx.None, LineClass.None);
        public static readonly ToonSpec SeatDark = S("roof.grey", ToonFx.None, LineClass.None);
        public static readonly ToonSpec CanopyGold = new(new("panel.paleGold.lit"), new("panel.paleGold.shade"))
        {
            Fx = ToonFx.Rim | ToonFx.AnisoBand,
            Steps = ShadeSteps.TwoHalf,
            Normal = ToonNormal.Smooth,
            Wet = 0.85f,
        };
    }

    /// <summary>
    /// Window numbering (contract-29): the light channel (LightChannelCatalog) and window-card type (WLD-INTERIORS, 1 classroom,
    /// 3 corridor, 4 stair, 5 office, 6 lab, 13 gym, 14 pool) of each grid the zone declares (初值). Xuanbang follows the research
    /// curve, Liren the teaching curve (MAP-QSH-SOUTH), the porch glass of the main building and Xuanbang the public-hall
    /// channel, the gate's guard houses the all-night channel (manned, §1.2), the two venues the venue-light channel.
    /// </summary>
    public static class Windows
    {
        public static readonly ChannelId Research = new("window.lab"), Teaching = new("window.teach"), Hall = new("interior.public");
        public static readonly ChannelId AllNight = new("window.allnight"), Venue = new("window.venue"), General = new("window.general");
        public const byte Classroom = 1, Corridor = 3, Stair = 4, Office = 5, Lab = 6, Gym = 13, Pool = 14;
    }

    /// <summary>
    /// South gate (MAP-QSH-SOUTH). Frame: origin midway between the two guard houses, X from the west house to the east
    /// house (bearing ≈ 126°), local +Z outward to the street (≈ 216°), so the campus side is −Z. The frame, the houses' places and
    /// the islands' places and sizes are read from the relation's footprints (SouthGateSite.Layout).
    /// </summary>
    public static class Gate
    {
        public const float IslandRise = 0.15f;

        /// <summary>The guard house is 5.6 m square: its footprint is 6.0 × 6.2 m, but the house is drawn with a stone plinth and roof eave round it.</summary>
        public const float HouseHeight = 3.4f, HousePlinth = FacadeRules.HutPlinth;
        public const float HouseRoof = 0.36f, HouseEave = 0.45f;
        public const float HouseWindowSill = FacadeRules.HutWindowSill, HouseWindowHead = 2.6f;

        /// <summary>Stone pillars beside the carriageway, on the islands' street ends (初值).</summary>
        public const float PillarSize = 1.3f, PillarHeight = 6.4f, PillarZ = 1.6f;

        public const float BarrierZ = -1.4f, BarrierHeight = 0.95f, BarrierGap = 0.25f;
        /// <summary>Turnstile row: cabinet width and depth, flap height; gaps stay below 0.5 m (SYS-WALK).</summary>
        public const float TurnstileZ = -0.6f, CabinetWidth = 0.26f, CabinetDepth = 1.5f, CabinetHeight = 1.02f;
        public const int TurnstilePassages = 2;

        /// <summary>Name wall (§1.3): low granite wall left of the gate seen from the street, one slab per character (初值).</summary>
        public const float NameWallX0 = -33.5f, NameWallX1 = -19.5f, NameWallZ = 5.2f;
        public const float NameWallHeight = 1.35f, NameWallThickness = 0.7f, NameSlab = 1.0f, NameSlabPitch = 1.25f;
        public const int NameCharacters = 6;

        /// <summary>Forecourt (§1.4): stone spheres, yellow-black bollards, yellow no-parking grid.</summary>
        public const float SphereRowZ = 4.4f, SpherePitch = 1.7f, SphereSize = 0.6f;
        public const float GridX0 = -4.5f, GridX1 = 4.75f, GridZ0 = 1.2f, GridZ1 = 9.0f, GridPitch = 1.6f, GridLine = 0.15f;
    }

    /// <summary>
    /// The plaza inside the gate (design:south-a01-plaza, 初值): its 18 × 12 m paving is the overlay polygon, drawn by WP-15. Local
    /// frame from <see cref="Gate.GatePlazaSite.FrameOf"/>: x across toward the north-west lawn (±6 m), z along the axis toward the
    /// campus (±9 m). A planter across the far end and two benches in front of it, facing back toward the kiosk and the gate.
    /// </summary>
    public static class Plaza
    {
        /// <summary>Top of WP-15's paving above the ground (GroundSection.PavingY): furniture stands on it.</summary>
        public const float PavingTop = GroundLevels.Paving;
        public static readonly FurnitureParams PlanterSize = new(6.4f, 0.45f, 1.6f);
        public const float PlanterAlong = 7.0f;
        /// <summary>WP-13's street bench (FurnitureStyle.Bench), its back 0.7 m in front of the planter.</summary>
        public static readonly FurnitureParams BenchSize = new(1.8f, 0.8f, 0.62f);
        public const float BenchAlong = 5.2f;
        public static readonly float[] BenchAcross = [-1.9f, 1.9f];
    }

    /// <summary>Entrance axis between the gate and the main building (§2.5).</summary>
    public static class Axis
    {
        /// <summary>Flagpole proportions; placement comes from SouthIds.Flagpole in the overlay.</summary>
        public static readonly Vector2 FlagpoleFacing = new(0.802f, 0.597f);
        public const float PoleHeight = 22f, PoleFootRadius = 0.15f, PoleTopRadius = 0.08f;
    }

    /// <summary>
    /// Main building (§2). Frame: origin at the west end of B2's south-west facade (osm:way:687370085), X along the facade
    /// (≈ 126°), local +Z outward toward the gate (≈ 216°). B2 is about 72 m along X and 40 m deep (z −40…0). The frames, the
    /// front steps zone, the rear door's place and the roof level are read from the map data (MainBuildingSite.Layout).
    /// </summary>
    public static class Main
    {
        /// <summary>
        /// First-floor level of B2, the portico platform (WLD-INTERIORS). The interior index gives the doors' y in absolute metres,
        /// so the level above the local ground is kept here.
        /// </summary>
        public const float FloorY = 2.6f;
        public const int RisersLower = 9, RisersUpper = 8;
        public const float Tread = 0.36f, MidLanding = 2.0f;

        /// <summary>Portico (§2.4): four square columns, a pair at each end (初值, §14).</summary>
        public const float ColumnSize = 1.4f, ColumnZ = 8.0f, PorticoHeight = 12.4f, EntablatureDepth = 1.5f;
        public static readonly float[] SingleColumns = [-12f, -4f, 4f, 12f];
        public static readonly float[] PairColumns = [-20.6f, -18.6f, 18.6f, 20.6f];
        public const float PorticoHalfWidth = 21.7f, PorticoFront = 8.95f, BackWall = 2.0f;
        /// <summary>Round-headed openings in the portico's back wall: windows in four bays, the door in the middle.</summary>
        public static readonly float[] ArchWindows = [-15.3f, -8f, 8f, 15.3f];
        public const float ArchWidth = 3.0f, ArchSill = 1.1f, ArchSpring = 7.0f, DoorWidth = 3.6f, DoorSpring = 5.0f;

        /// <summary>
        /// Roof gallery (空廊, §2.3): a colonnaded walkway ring on B2's roof (its level is B2's height in the data), columns on the
        /// outer edge (初值). TopY, the gallery's crown at 46.1 m, is the building's own proportion; the data has no such value.
        /// </summary>
        public const float TopY = 46.1f;
        public const float GalleryX0 = 10f, GalleryX1 = 62f, GalleryZ0 = -34f, GalleryZ1 = -10f;
        public const float GalleryWalk = 4.0f, GalleryPitch = 4.0f, GalleryColumn = 0.8f, GalleryFascia = 1.4f;

        /// <summary>Rear door (b2.rear) on B2's north-east facade: the porch's sizes.</summary>
        public const float RearLandingWidth = 9f, RearLandingDepth = 3f, RearStepsWidth = 6f, RearTread = 0.33f;
        public const float RearCanopyY = 7.0f, RearColumnRadius = 0.26f;
    }

    /// <summary>Forest park (§3.2; MAP-QSH-TERRAIN).</summary>
    public static class Park
    {
        /// <summary>Peacock-house proportions; placement is the authored SouthIds.PeacockHouse point.</summary>
        public const float CageWidth = 9f, CageDepth = 7f, CageWall = 3.2f, CageRoofRise = 1.6f, CageMesh = 0.5f;

        /// <summary>
        /// Hill-top pavilion (design:south-hongfeng-pavilion; its place, side, seat wall radius, height and bearings are read from that
        /// feature); y follows the terrain at the top.
        /// </summary>
        public const float PavilionPost = 0.3f, PavilionEave = 2.9f, PavilionRoofRise = 1.5f;
    }

    /// <summary>
    /// Xuanbang (MAP-QSH-SOUTH, WLD-INTERIORS). The zone builds all three parts: main wing and south annex to 35.7 m, tower to
    /// 73.85 m, clock tower to 66.65 m. Footprints, the wing's and tower's storey counts, the clock tower's height and the
    /// skylight's centre come from the world model (XuanbangSite.Footprints).
    /// </summary>
    public static class Xuanbang
    {
        /// <summary>Main wing: 5.0 m ground storey + 7 × 4.2 m = 34.4 m wall top, cornice and parapet to 35.7 m.</summary>
        public const float MainGround = 5.0f, MainStorey = 4.2f, MainBay = 3.9f, MainPier = 1.3f;
        /// <summary>Tower: 5.0 m + 17 × 3.9 m = 71.3 m, cornice 0.9 m and parapet 1.65 m to 73.85 m.</summary>
        public const float TowerGround = 5.0f, TowerStorey = 3.9f, TowerBay = 3.2f, TowerPier = 0.9f, TowerParapet = 1.65f;
        /// <summary>Long axis of the slab (footprint edge running 41°).</summary>
        public static readonly Vector2 SlabAxis = new(0.6577f, -0.7532f);

        /// <summary>Pediment portico (§5.6), centred on the main front (初值): four columns, three arched doors behind.</summary>
        public static readonly float[] PorticoColumns = [-9f, -3f, 3f, 9f];
        public static readonly float[] Doors = [-6f, 0f, 6f];
        public const float PorticoHalfWidth = 11.5f, PorticoDepth = 7.6f, PodiumRise = 0.45f, ColumnRadius = 0.78f, ColumnHeight = 13.2f;
        public const float EntablatureHeight = 1.95f, PedimentRise = 4.2f;
        public const float DoorWidth = 3.2f, DoorSpring = 3.4f, UpperWindowSill = 7.0f, UpperWindowWidth = 2.6f, UpperWindowSpring = 3.0f;
        /// <summary>Small gables on the two projecting end blocks of the main wing (§5.7).</summary>
        public const float EndGableRise = 2.6f;
        /// <summary>Atrium skylight on the main wing (WLD-INTERIORS: the 2019 rendering's roof light), centred on the entity anchor.</summary>
        public const float SkylightSize = 14f, SkylightRise = 2.2f;

        /// <summary>
        /// Clock tower (§5.4, 初值): shaft to 46.0 m, cornice, setback dial stage to 54.6 m, cornice, lantern podium to the
        /// lantern floor at 56.4 m (P18 eye 58.0 m), slit columns to 60.6 m, entablature, bullet dome 61.1–66.2 m, finial to 66.65 m.
        /// </summary>
        public const float ShaftTop = 46.0f, StageTop = 54.6f, LanternFloor = 56.4f, LanternTop = 60.6f, DomeBase = 61.1f, DomeTop = 66.2f;
        public const float Quoin = 1.7f, StageInset = 1.6f, DialRadius = 2.5f, DialY = 50.7f, DialProud = 0.4f;
        public const int LanternColumns = 20;
        public const float LanternRadius = 4.3f, LanternColumn = 0.45f, CoreRadius = 1.7f, DomeRadius = 4.4f;
        /// <summary>Bullet dome profile (an ogive on a short upright skirt), (radius, height) as fractions of DomeRadius and DomeTop − DomeBase.</summary>
        public static readonly Vector2[] DomeProfile =
            [new(1f, 0), new(1f, 0.14f), new(0.98f, 0.28f), new(0.92f, 0.44f), new(0.8f, 0.6f), new(0.63f, 0.75f), new(0.42f, 0.87f), new(0.21f, 0.955f), new(0.06f, 0.995f), new(0, 1f)];
        public const float PortalWidth = 3.2f, PortalSpring = 4.4f;
        /// <summary>
        /// Floor and door threshold above the ground: the top of WP-15's paving (GroundSection.PavingY), so the side door's forecourt
        /// (design:south-clock-forecourt in world/south.json) runs level into the doorway.
        /// </summary>
        public const float Threshold = GroundLevels.Paving;
        /// <summary>A pair of narrow windows per storey on every face.</summary>
        public const float SlitWidth = 0.9f, SlitHeight = 2.6f, SlitGap = 1.3f, SlitFirst = 8.4f, SlitPitch = 4.0f;
        public const int SlitRows = 9;
    }

    /// <summary>
    /// Liren corner gateway (§6.3, 初值): two pier blocks and a fourth-storey bridge over a three-storey void on square columns,
    /// plus the east link. The pier and link storey counts come from the map data; the storeys follow WP-14's A and B at 15.6 m:
    /// wall top 15.1 m, coping 16.57 m (the data's height is the 15.6 m).
    /// </summary>
    public static class Liren
    {
        public const float GroundStorey = 3.85f, Storey = 3.75f, Parapet = 0.87f;
        public const float BridgeFloor = 11.7f, Soffit = 10.9f, ColumnSize = 1.1f, ColumnInset = 1.2f;
    }

    /// <summary>
    /// Sports venues (MAP-QSH-SOUTH; WLD-INTERIORS), 初值. Plans are fitted to the OSM footprints: the gym's circle
    /// by area about its centroid, the natatorium's ellipse by the footprint's second moments. Heights are above the ground at
    /// the plan's centre. The natatorium's dome profile reaches 15.6 m against the data's 15.0 m estimate, so it stays the profile's own.
    /// </summary>
    public static class Sports
    {
        /// <summary>
        /// Gym drum (§7.1): ochre base band to the platform storey, tea glass ring hall, silver rib ring, roof. The platform storey
        /// (大平台二层, both venues' public entrances) is the platform feature's height and the drum's top the gym's height, both
        /// in the map data.
        /// </summary>
        /// <summary>Insets from the fitted radius: base band, glass ring; the rib ring bulges out to about the footprint.</summary>
        public const float GymBaseInset = 2.2f, GymGlassInset = 3.0f;
        public const float GymGlassTop = 13.0f, GymTransom = 9.0f, GymBay = 3.6f;
        /// <summary>Slanted dark mullions (§7.1): each leans this far round the drum over the glass height.</summary>
        public const float GymMullionLean = 1.6f;
        /// <summary>Brown louvre blades under the rib ring, heights above the ground.</summary>
        public static readonly float[] GymLouvres = [11.75f, 12.1f, 12.45f];
        /// <summary>
        /// The bulging rib ring and its rolled edge turned down (§7.1): (radius offset from the fitted radius, height) from its
        /// foot behind the glass top over the bulge and round the roll to the roof's edge.
        /// </summary>
        public static readonly Vector2[] GymRibProfile =
            [new(-3.3f, 12.9f), new(-2.5f, 14.0f), new(-1.1f, 15.6f), new(0f, 17.4f), new(0.35f, 19.0f), new(0.25f, 20.5f), new(-0.35f, 21.6f), new(-1.4f, 22.35f), new(-2.6f, 22.6f)];
        public const int GymRibs = 72;
        /// <summary>Each rib is a slim fin standing proud of the smooth band, outlined so the ring reads as ribbed metal.</summary>
        public const float GymRibDepth = 0.14f, GymRibWidth = 0.24f;
        /// <summary>Roof dish (§7.1): broad and almost flat in the middle, (fraction of the fitted radius, height) from the edge in.</summary>
        public static readonly Vector2[] GymRoofProfile =
            [new(0.952f, 22.62f), new(0.90f, 23.5f), new(0.80f, 24.55f), new(0.66f, 25.35f), new(0.48f, 25.8f), new(0.28f, 25.97f), new(0f, 26.0f)];
        public const int GymSeams = 48;
        /// <summary>Two rings of vent caps: fraction of the fitted radius and count.</summary>
        public static readonly (float Ring, int Count)[] GymVents = [(0.36f, 10), (0.62f, 16)];
        /// <summary>The platform-storey public entrance (gym.deck, bearing 127°) and the field-level closed doors.</summary>
        public const float GymDoorBearing = 127f, GymDoorWidth = 3.0f, GymDoorHeight = 3.3f;
        public static readonly float[] GymServiceDoors = [37f, 217f, 307f];

        /// <summary>
        /// Natatorium (§7.2): a low white elliptical dome, its dark glass band near the top as a short clerestory drum under a cap.
        /// Profile (fraction of the fitted semi-axes, height): skirt, dome, then the drum foot.
        /// </summary>
        public static readonly Vector2[] PoolDomeProfile =
            [new(1f, -0.1f), new(1f, 1.6f), new(0.985f, 3.0f), new(0.955f, 4.8f), new(0.90f, 6.7f), new(0.82f, 8.5f), new(0.72f, 10.1f), new(0.60f, 11.4f), new(0.47f, 12.3f), new(0.455f, 12.4f)];
        public const float PoolDrum = 0.44f, PoolGlassFoot = 12.4f, PoolGlassTop = 14.2f, PoolBay = 3.0f;
        /// <summary>The white lid over the glass band: an overhanging rim, then a flat crown to 15.6 m.</summary>
        public static readonly Vector2[] PoolCapProfile =
            [new(0.47f, 14.2f), new(0.47f, 14.5f), new(0.42f, 14.85f), new(0.30f, 15.25f), new(0.15f, 15.5f), new(0f, 15.6f)];
        /// <summary>Meridian ribs on the dome from the skirt's cornice to the glass band (thin white lines that give the dome its structure).</summary>
        public const int PoolRibs = 32;
        /// <summary>The light concrete apron round the foot, as a fraction of the semi-axes.</summary>
        public const float PoolRing = 0.1f;
        /// <summary>Platform-storey entrance hall on the side facing the platform (closed until WP-31a registers it).</summary>
        public const float PoolDoorBearing = 172f, PoolEntryWidth = 9f, PoolEntryHeight = 4.2f, PoolEntryDepth = 2.6f;
    }

    /// <summary>
    /// Sports platform (大平台, MAP-QSH-SOUTH, 初值): the deck over qsh:way:601600526, its top at
    /// <see cref="Sports.PlatformTop"/>. Under the deck a round-column street arcade runs along every free edge; behind it the closed
    /// shops (§7.4, WLD-INTERIORS) fill a core block. Service roads keep running under the deck between the core and the gym
    /// and the stand. Heights are above the ground at the footprint's centroid. The deck's height and the north steps' place and
    /// direction are read from the map data.
    /// </summary>
    public static class Platform
    {
        /// <summary>Deck: slab under the top paving, the edge fascia band and its projection.</summary>
        public const float Slab = 0.75f, Fascia = 1.0f, FasciaProud = 0.12f;
        /// <summary>Parapet panels (栏板, §7.6): 1.1 m solid, collision to 1.5 m from the railing part (SYS-WALK).</summary>
        public const float Parapet = 1.1f, ParapetDepth = 0.22f, ParapetInset = 0.2f;
        /// <summary>Arcade columns along the free edges: round, at most this pitch, set in from the edge.</summary>
        public const float ColumnRadius = 0.36f, ColumnPitch = 7.2f, ColumnInset = 1.3f, ColumnEnd = 1.6f;
        /// <summary>Deck joints: a light grid on the top paving.</summary>
        public const float JointPitch = 7.2f, JointWidth = 0.12f;
        /// <summary>Probe distance outside an edge: an edge whose outside is a venue or another deck is not free.</summary>
        public const float AbutProbe = 0.8f;

        /// <summary>Shopfront bays on the core: target width, glazing sill and head, sign band; a closed door every second bay.</summary>
        public const float ShopBay = 4.6f, ShopSill = 0.45f, ShopHead = 3.2f, SignBottom = 3.35f, SignTop = 4.05f, Pilaster = 0.5f;
        public const float DoorWidth = 1.8f, DoorHeight = 2.5f;

        /// <summary>
        /// Grand steps (一组宽大台阶) in the south edge's notch: two flights and a landing, centred on the notch edge. The place is the
        /// zone's choice of edge (the data has no steps there).
        /// </summary>
        public const float GrandStairWidth = 13f, StairTread = 0.32f, StairLanding = 2.4f;
        public const int RisersPerFlight = 16;
        /// <summary>The OSM steps at the north tip (their place and direction are read from the footprint): drawn 3.0 m wide against its 2.5 m.</summary>
        public const float NorthStairWidth = 3.0f;
    }

    /// <summary>
    /// The stand (看台, qsh:way:217855276, MAP-QSH-SOUTH, 初值): a C of tiers round the west half of the track, sampled on rays
    /// from the track's centre (angles in degrees from +x toward +z, so 180 is west); each ray's nearest and farthest crossings of
    /// the footprint are the front and back of the tiers. The rows rise at most <see cref="Slope"/> and stop at <see cref="TopMax"/>,
    /// so the deep main stand on the west is the tallest and the curved ends fall away. The pillow canopy covers the main stand.
    /// </summary>
    public static class Stand
    {
        public const float StationStep = 1.5f, MinDepth = 6f;
        /// <summary>Front wall over the apron, the cross walk behind it, the back concourse and its solid parapet.</summary>
        public const float Front = 1.1f, FrontWalk = 1.4f, Concourse = 3.0f, Parapet = 1.1f;
        public const int Rows = 22;
        public const float Slope = 0.5f, TopMax = 12.0f;
        /// <summary>Pillow canopy (枕形罩棚): angle range and end taper, back overhang, reach over the rake, thickness, lift.</summary>
        public const float CanopyFrom = 140f, CanopyTo = 215f, CanopyEnd = 14f;
        public const float CanopyBack = 1.2f, CanopyReach = 0.8f, CanopyThickness = 4.4f, CanopyBackRise = 4.4f, CanopyFrontRise = 5.6f;
        /// <summary>The lens: the belly's depth against the top's, and the roundness of the edges (lower is fuller).</summary>
        public const float CanopyBelly = 0.75f, CanopyRound = 0.42f;
        public const int CanopySamples = 10;
        /// <summary>The pale gold middle section and its vertical division seams; the yellow seat block under it.</summary>
        public const float GoldFrom = 172f, GoldTo = 190f, YellowFrom = 160f, YellowTo = 202f;
        /// <summary>Dark grey steel: a column and a raking strut every this many stations, on the back concourse.</summary>
        public const int SupportEvery = 4;
        public const float Column = 0.5f;
        /// <summary>Concourse portals in the back wall at the platform level (where the deck meets the stand), closed.</summary>
        public const float PortalPitch = 12f, PortalWidth = 3.2f, PortalHeight = 3.0f;

        /// <summary>The low dark seats across the track (§7.3, design:south-stadium-east-seats): rows, depth, rise and walls.</summary>
        public const int LowRows = 5;
        public const float LowRow = 0.8f, LowRise = 0.4f, LowFront = 0.45f, LowWalk = 0.9f, LowBack = 1.2f;
    }

    /// <summary>
    /// The west lawn by the innovation centre (MAP-QSH-ADDITIONS R6, 初值): a transition green between the boundary and the
    /// north–south footpath, regrouped as a low grass mound (south.terrain.json) with a stone seat-wall arc on its crest facing
    /// north-east over the Zouma river to the core's research blocks. The crest, the arc's radius and bearings, its height and
    /// thickness are read from design:south-west-lawn-seat.
    /// </summary>
    public static class Lawn
    {
        public const int SeatChords = 6;
    }

    /// <summary>East lake shore (§4).</summary>
    public static class Lake
    {
        /// <summary>Retaining wall foot, coping top and balustrade top (the railing part's height) above the platform; balustrade line set back from the water.</summary>
        public static readonly float RailTop = new Campus.Contracts.Parts.RailingParams().Height;
        public const float WallBottom = -1.1f, CopingTop = 0.12f, RailSetback = 0.25f, BalusterPitch = 0.32f, NewelPitch = 3.0f;
        /// <summary>
        /// The wade step (design:south-eastlake-wade-step: its width and tread levels are read from it, and it lies on the nearest
        /// bank segment) fills the middle of that segment; its second tread is 0.15 m under the −0.5 m water. The treads' depths are the zone's own.
        /// </summary>
        public const float Tread1Depth = 0.6f, Tread2Depth = 1.2f;
        public const float BoatLength = 4.2f, BoatBeam = 1.35f, BoatDepth = 0.55f, BoatKeel = -0.80f;
        /// <summary>Wu Liren statue (qsh:node:7867995301, its place is read from the entity), facing the lake; the figure is 1.25 × a 2 m model, about 2.5 m.</summary>
        public const float StatueFacing = 190f, StatueScale = 1.25f;
        /// <summary>Top of the pedestal above the ground: the figure's base plate stands here.</summary>
        public const float PedestalTop = 1.95f;
        /// <summary>
        /// Hook points kept for the detail pass (§4.9), statue frame (x left, y up from PedestalTop, z towards the lake), metres:
        /// the seasonal leaf on the figure's left shoulder, and two in-ground uplights in front of the pedestal (lamp.statue).
        /// </summary>
        public static readonly Vector3 StatueLeaf = new(0.25f, 2.0f, 0f);
        public static readonly Vector3[] StatueUplights = [new(-0.9f, -PedestalTop, 1.7f), new(0.9f, -PedestalTop, 1.7f)];
        /// <summary>Hexagonal pavilion proportions; placement is the authored SouthIds.LakePavilion point.</summary>
        public const float PavilionFloor = 0.45f, PavilionPlatform = 3.4f, PavilionColumns = 2.5f, PavilionColumnHeight = 3.0f;
        public const float LowerEave = 3.7f, LowerRise = 1.0f, DrumRadius = 2.0f, UpperEave = 2.75f, UpperRise = 1.8f;
    }
}
