using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.Zones.South;

/// <summary>
/// Every tunable value of the south zone in one place, for review (ENG-TEAM §0.1): materials as colour names, and the
/// dimensions of each site. Coordinates are world (x, z) in metres taken from the OSM footprints in campus.json;
/// local frames are described per block. Values marked "初值" are starting values awaiting the main session's call.
/// </summary>
public static class SouthTune
{
    public const int BudgetTrianglesL0 = 90_000;
    public const int BudgetTrianglesL1 = 45_000;
    public const int BudgetDrawCalls = 18;

    /// <summary>Materials (ART-STYLE §8 colour names only).</summary>
    public static class Look
    {
        private static ToonSpec S(string family, ToonFx fx = ToonFx.None, LineClass line = LineClass.Building, float wet = 0.6f) =>
            new(new(family + ".lit"), new(family + ".shade")) { Fx = fx, Line = line, Wet = wet };

        /// <summary>Main building and gate: light grey-white stone (MAP-QSH-SOUTH §1.2, §2.2).</summary>
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
        public static readonly ToonSpec Glass = new(new("glass.lit"), new("glass.shade"))
        { Kind = ToonKind.Glass, Fx = ToonFx.GlassStreak | ToonFx.Glare, Wet = 0 };
    }

    /// <summary>
    /// South gate (MAP-QSH-SOUTH §1). Frame: origin midway between the two guard houses, X from the west house to the east
    /// house (bearing ≈ 126°), local +Z outward to the street (≈ 216°), so the campus side is −Z.
    /// </summary>
    public static class Gate
    {
        public static readonly Vector2 HouseWest = new(-532.535f, 719.865f);
        public static readonly Vector2 HouseEast = new(-510.945f, 735.3575f);
        /// <summary>Traffic islands (OSM parts of relation 8407525), local x of their centres.</summary>
        public const float IslandWestX = -5.71f, IslandEastX = 5.96f, IslandZ = -0.26f;
        public const float IslandWidth = 1.9f, IslandLength = 7.3f, IslandRise = 0.15f;

        public const float HouseSize = 5.6f, HouseHeight = 3.4f, HousePlinth = 0.45f;
        public const float HouseRoof = 0.36f, HouseEave = 0.45f;
        public const float HouseWindowSill = 1.0f, HouseWindowHead = 2.6f;

        /// <summary>Stone pillars beside the carriageway, on the islands' street ends (初值).</summary>
        public const float PillarSize = 1.3f, PillarHeight = 6.4f, PillarZ = 1.6f;

        public const float BarrierZ = -1.4f, BarrierHeight = 0.95f, BarrierGap = 0.25f;
        /// <summary>Turnstile row: cabinet width and depth, flap height; gaps stay below 0.5 m (SYS-WALK §7).</summary>
        public const float TurnstileZ = -0.6f, CabinetWidth = 0.26f, CabinetDepth = 1.5f, CabinetHeight = 1.02f;
        public const int TurnstilePassages = 2;

        /// <summary>Boundary collision: 1.5 m minimum, the gate's invisible top-up to 2.2 m (WLD-OUTSKIRTS §7.1).</summary>
        public const float BoundaryTop = 2.2f;

        /// <summary>Name wall (§1.3): low granite wall left of the gate seen from the street, one slab per character (初值).</summary>
        public const float NameWallX0 = -33.5f, NameWallX1 = -19.5f, NameWallZ = 5.2f;
        public const float NameWallHeight = 1.35f, NameWallThickness = 0.7f, NameSlab = 1.0f, NameSlabPitch = 1.25f;
        public const int NameCharacters = 6;

        /// <summary>Forecourt (§1.4): stone spheres, yellow-black bollards, yellow no-parking grid.</summary>
        public const float SphereRowZ = 4.4f, SpherePitch = 1.7f, SphereSize = 0.6f;
        public const float GridX0 = -4.5f, GridX1 = 4.75f, GridZ0 = 1.2f, GridZ1 = 9.0f, GridPitch = 1.6f, GridLine = 0.15f;
    }

    /// <summary>Entrance axis between the gate and the main building (§2.5).</summary>
    public static class Axis
    {
        /// <summary>National flagpole at the centre of the front lawn (osm:way:568303118, its paved inner square).</summary>
        public static readonly Vector2 Flagpole = new(-394.17f, 557.41f);
        public static readonly Vector2 FlagpoleFacing = new(0.802f, 0.597f);
        public const float PoleHeight = 22f, PoleFootRadius = 0.15f, PoleTopRadius = 0.08f;
    }

    /// <summary>
    /// Main building (§2). Frame: origin at the west end of B2's south-west facade (osm:way:687370085), X along the facade
    /// (≈ 126°), local +Z outward toward the gate (≈ 216°). B2 is about 72 m along X and 40 m deep (z −40…0).
    /// </summary>
    public static class Main
    {
        public static readonly Vector2 FrontA = new(-374.74f, 476.82f);
        public static readonly Vector2 FrontB = new(-316.62f, 519.34f);

        /// <summary>First-floor level of B2, the portico platform (WLD-INTERIORS §2.3, §2.17).</summary>
        public const float FloorY = 2.6f;
        /// <summary>Front steps zone (osm:way:618752446): x from StepsX0 to StepsX1, out to StepsZ.</summary>
        public const float StepsX0 = 12.15f, StepsX1 = 60.65f, StepsZ = 17.4f;
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

        /// <summary>Roof gallery (空廊, §2.3): a colonnaded walkway ring on B2's roof, columns on the outer edge (初值).</summary>
        public const float RoofY = 34.4f, TopY = 46.1f;
        public const float GalleryX0 = 10f, GalleryX1 = 62f, GalleryZ0 = -34f, GalleryZ1 = -10f;
        public const float GalleryWalk = 4.0f, GalleryPitch = 4.0f, GalleryColumn = 0.8f, GalleryFascia = 1.4f;

        /// <summary>Rear door (b2.rear) on B2's north-east facade, local x and z in the front frame.</summary>
        public const float RearX = 35.6f, RearZ = -40.3f;
        public const float RearLandingWidth = 9f, RearLandingDepth = 3f, RearStepsWidth = 6f, RearTread = 0.33f;
        public const float RearCanopyY = 7.0f, RearColumnRadius = 0.26f;

        /// <summary>Wing heights through south.overlay.json HeightOps: one storey height (≈ 4.28 m) across all wings.</summary>
        public const float StoreyHeight = 4.28f;
    }

    /// <summary>Forest park (§3.2; MAP-QSH-TERRAIN §1.2, §4).</summary>
    public static class Park
    {
        /// <summary>Peacock house on the hill's south-east foot (MAP-QSH-TERRAIN §4.5), between the two woodland paths.</summary>
        public static readonly Vector2 PeacockHouse = new(-302f, 305f);
        public const float CageWidth = 9f, CageDepth = 7f, CageWall = 3.2f, CageRoofRise = 1.6f, CageMesh = 0.5f;

        /// <summary>Hill-top pavilion and seat wall (design:south-hongfeng-pavilion); y follows the terrain at the top.</summary>
        public static readonly Vector2 HillTop = new(-352f, 238f);
        public const float PavilionSize = 4.2f, PavilionPost = 0.3f, PavilionEave = 2.9f, PavilionRoofRise = 1.5f;
        public const float SeatWallRadius = 5.4f, SeatWallHeight = 0.45f;
    }
}
