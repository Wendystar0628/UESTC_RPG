using System.Numerics;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Street;

namespace Campus.Gen.Furniture;

/// <summary>Illuminance tier of a road (MAP-QSH-ADDITIONS §8.1): it picks the lamp, the spacing and the sides.</summary>
public enum RoadTier
{
    /// <summary>Main axis (grade one): white disc lamps on both sides, opposite each other, 25–30 m apart.</summary>
    Main,
    /// <summary>Secondary road (grade two): grey single-arm LED lamps on one side, 30–40 m apart.</summary>
    Secondary,
}

/// <summary>
/// A road that gets street furniture, by its name in the network (names are map data, never entity IDs). <paramref name="Rows"/>
/// are the planted tree rows' offsets from its centre line (both sides), kept clear by benches and bins.
/// </summary>
public sealed record RoadRule(string Name, RoadTier Tier, IReadOnlyList<float> Rows, IReadOnlyList<RestNode>? Nodes = null)
{
    /// <summary>Kerb ramps where the road's walks reach its kerb (13c2b, Opus review 16:23: the osmanthus road's junctions).</summary>
    public bool KerbRamps { get; init; }
}

/// <summary>
/// A rest node of a road (Opus review 16:23): a place on the map (a plaza edge, a junction) where the road's rest stops stand, instead
/// of along its line. <paramref name="At"/> is its plan position in map metres, <paramref name="Label"/> the name its log lines use.
/// </summary>
public sealed record RestNode(string Label, Vector2 At);

/// <summary>
/// Every tunable value of the WP-13 placement in one place (ENG-TEAM §5): which roads, spacings, offsets from the kerb, the
/// rest-stop rhythm, the part sizes and the landing rules. Initial values follow WLD-LIFE §4, MAP-QSH-ADDITIONS §8, SYS-ATLAS §6,
/// WLD-INTERIORS §4 and WLD-TERRAIN §4.4; the Opus review sets the finals. Part looks stay in <see cref="StreetStyle"/>.
/// </summary>
public static class FurnitureStyle
{
    // ---- Roads (13b mainline: the ginkgo avenue and two representative secondary roads; the rest of the network follows) ----
    /// <remarks>
    /// The roads and their tiers are decided here; the row offsets are only the fallback. The placement reads the real offsets from the
    /// map's <see cref="PlantingKey"/> build input (WP-23's planting rule: the avenue's two ginkgo rows and its spring band, one row on
    /// each secondary road, <see cref="PlantingRows"/>) and uses these when the input cannot say, with a warning diagnostic.
    /// </remarks>
    public static IReadOnlyList<RoadRule> Roads { get; } = Array.AsReadOnly<RoadRule>(
    [
        new("银杏大道", RoadTier.Main, [7f, 14f, 17.5f]),
        new("水杉路", RoadTier.Secondary, [5f]),
        // The osmanthus road keeps no benches along its walks (Opus review 16:23): its stops are at its two nodes, where the walks end:
        // the pedestrian plaza at the west end (where the west-south walk runs into it) and the south mouth junction.
        new("丹桂路", RoadTier.Secondary, [4.5f], [new("west plaza", new(-99.8f, -442.7f)), new("south mouth", new(69.9f, -833.1f))]) { KerbRamps = true },
    ]);

    // ---- Kerb ramps (13c2b): where a walk reaches a ramped road's kerb, at its end or where it crosses ----
    public static RampParams KerbRamp { get; } = new(1.5f, 0.15f, 1.05f);
    /// <summary>A walk's end this far beyond the kerb (or short of it) counts as reaching it.</summary>
    public const float RampKerbReach = 2.5f;
    /// <summary>Two ramps on one kerb keep at least this far apart; the first one stands.</summary>
    public const float RampGap = 4f;
    /// <summary>
    /// A ramp leads onto pedestrian paving: this far behind the kerb must be paving (a walk or a plaza), else it slides along the kerb by up
    /// to <see cref="RampSlide"/> to find it, else it is left out.
    /// </summary>
    public const float RampWalkBehind = 0.6f;
    public const float RampSlide = 4.5f;

    // ---- Signposts and drinking fountains (Opus review 13c2b 21:05). Interim rules, all of them here: when WP-44's walk: points ----
    // ---- land, the points replace them. Both keep to "no piling up": rather too few than too many.                              ----
    /// <summary>
    /// A signpost stands at a junction of the effective network's pedestrian lines where at least <see cref="SignpostBranches"/>
    /// branches meet (line ends within <see cref="JunctionSnap"/> of one another, a line running through counting two) and a landmark lies
    /// within <see cref="SignpostLandmarkReach"/>: beside the paving nearest the junction (within <see cref="SignpostReach"/>), facing it.
    /// One to a line of sight: none within <see cref="SignpostSpacing"/> of another, the junctions with more branches and then the nearer
    /// landmark going first. Landmarks are the features whose subcategory or category is listed here.
    /// </summary>
    public static SignParams Signpost { get; } = new(1.2f, 2.6f, 0.12f) { Variation = 1 };
    public const int SignpostBranches = 3;
    public const float JunctionSnap = 1.5f;
    public const float SignpostLandmarkReach = 50f;
    public const float SignpostSpacing = 80f;
    public const float SignpostReach = 6f;
    public static IReadOnlyList<string> LandmarkSubcategories { get; } = Array.AsReadOnly(new[] { "landmark" });
    public static IReadOnlyList<string> LandmarkCategories { get; } = Array.AsReadOnly(new[] { "library" });
    /// <summary>
    /// One drinking fountain for each sports venue and teaching building with a main entrance (an open entrance of the interior index; of
    /// several, the first by ID): beside the paving <see cref="FountainNear"/>–<see cref="FountainFar"/> in front of the door, nearest
    /// first, facing the paving.
    /// </summary>
    public static EquipmentParams Fountain { get; } = new(0.6f, 1.1f, 0.45f);
    public const float FountainNear = 10f;
    public const float FountainFar = 20f;
    public static IReadOnlyList<string> FountainCategories { get; } = Array.AsReadOnly(new[] { "sport", "teaching" });

    // ---- Vending groups, delivery lockers and shared-bicycle bays (13c2c): where people use them, a few clusters, blank between ----
    // ---- (WLD-LIFE §3.5, §4.5; ART-AMBIENCE §1.3 rule 4: one theme a place, things in a cluster; "no piling up": rather too few). ----
    /// <summary>
    /// A vending group is <see cref="VendingMin"/>–<see cref="VendingMax"/> machines side by side, <see cref="VendingSideGap"/> apart, on one
    /// paved bay, every other group holding one low-saturation odd machine (WLD-LIFE §4.5). Groups stand at the dormitory cluster gates
    /// (the open gate first, then by ID), then at the canteens and the sports venues (<see cref="VendingCategories"/>: by the first open
    /// entrance, else the side of the building nearest the network); none within <see cref="VendingSpacing"/> of another (one to a line of
    /// sight) and none within <see cref="MainAxisClear"/> of a main road (ART-AMBIENCE §1.2: no vending groups on the main axis).
    /// </summary>
    public static EquipmentParams Vending { get; } = new(1f, 1.85f, 0.78f);
    public const int VendingMin = 2;
    public const int VendingMax = 3;
    public const float VendingSideGap = 0.06f;
    public const float VendingSpacing = 150f;
    public const float MainAxisClear = 40f;
    public static IReadOnlyList<string> VendingCategories { get; } = Array.AsReadOnly(new[] { "dining", "sport" });
    /// <summary>
    /// Service things (vending groups, lockers, bays) stand off the edge of the paving or the carriageway nearest their site, within
    /// <see cref="ServiceReach"/> of it, facing the edge, on a bay that leaves <see cref="ServiceFront"/> of standing room in front; a bay's
    /// painted frame reaches to <see cref="FurnitureStyle.WalkGap"/> off the edge instead.
    /// </summary>
    public const float ServiceReach = 14f;
    public const float ServiceFront = 1.2f;
    /// <summary>
    /// At a cluster gate they stay outside the cluster's outline by this much (the fence, hedge, turnstiles and guard house stand inside
    /// it, MAP-QSH-NORTH §4.7) and at least <see cref="GateKeep"/> off the gate point, so the way through stays clear.
    /// </summary>
    public const float GateOutside = 0.8f;
    public const float GateKeep = 4f;
    /// <summary>
    /// Delivery lockers: WLD-LIFE §4.5 keeps them to two places. Their two places (the west gate's clinic wicket, inside West Gate 2) are
    /// not in the map data yet (WP-30's gates), so on the mainline they stand at the first <see cref="LockerSites"/> gates that hold a
    /// vending group, beside it: the dormitory gate is where deliveries are picked up. WP-30's gate points replace them.
    /// </summary>
    public static EquipmentParams Locker { get; } = new(2.4f, 2f, 0.6f);
    public const int LockerSites = 2;
    /// <summary>
    /// Shared-bicycle bays (WLD-LIFE §3.5: the zones register them later; P0 starts with a few): at the open cluster gates, then at the
    /// first open entrance of the teaching buildings, libraries and canteens (<see cref="BayCategories"/>), then the map's bicycle parking points
    /// (<see cref="BaySubcategories"/>); at a door <see cref="BayNear"/>–
    /// <see cref="BayFar"/> out from the door and in front of it; none within <see cref="BaySpacing"/> of another. Each holds
    /// <see cref="BayBikesMin"/>–<see cref="BayBikesMax"/> bikes on every other slot, front wheel to the back line: stand-ins for WP-55's
    /// rentable stock, which the kernel mounts in their place once WP-55 lands (set both to zero then).
    /// </summary>
    public static RoofParams BikeBay { get; } = new(3.6f, 1.25f, 2f);
    public static VehicleParams Bike { get; } = new(0.6f, 1.1f, 1.75f);
    public static IReadOnlyList<string> BayCategories { get; } = Array.AsReadOnly(new[] { "teaching", "library", "dining" });
    /// <summary>Then the points the map marks for parking bicycles (their subcategory), each a site like a gate's.</summary>
    public static IReadOnlyList<string> BaySubcategories { get; } = Array.AsReadOnly(new[] { "bicycle" });
    public const float BayNear = 6f;
    public const float BayFar = 25f;
    public const float BaySpacing = 100f;
    public const int BayBikesMin = 2;
    public const int BayBikesMax = 3;
    public const float BayBikePitch = 1.2f;

    // ---- Parked cars (WLD-LIFE §3.3: a few low-saturation cars in each car park, autumn evening) ----
    /// <summary>Slots run along a lot's longest edge at this pitch, the cars' noses this far in from it, at most this many a lot.</summary>
    public const float CarSlot = 2.6f;
    public const float CarNoseIn = 0.6f;
    public const int CarsPerLot = 3;
    /// <summary>A slot holds a car when its hash falls under this share (sparse, WLD-LIFE §3.3).</summary>
    public const float CarShare = 0.4f;
    /// <summary>A car keeps this far inside its lot's outline; it is not parked where the ground drops more than this under it.</summary>
    public const float CarLotMargin = 0.3f;
    public const float CarMaxDrop = 0.2f;
    /// <summary>A 64 m cell shows at most this many car looks (shape and colour): each is its own set of draws.</summary>
    public const int CarLooksPerCell = 2;
    public static VehicleParams Saloon { get; } = new(1.8f, 1.47f, 4.5f);
    public static VehicleParams Hatch { get; } = new(1.72f, 1.52f, 4f) { Variation = 1 };

    /// <summary>The map input (relative to the map's DataRoot) the tree-row offsets come from: WP-23's planting rules.</summary>
    public const string PlantingKey = "vegetation/planting.json";

    /// <summary>Lamp spacing along the road: grade one 25–30 m (ART-FIDELITY §6, kept regular on the avenue, §8.5), grade two 30–40 m.</summary>
    public const float MainLampSpacing = 27.5f;
    public const float SecondaryLampSpacing = 35f;
    /// <summary>Pole centre from the carriageway edge (the kerb face).</summary>
    public const float LampKerbGap = 0.8f;
    /// <summary>A lamp blocked at its slot may slide along the road by up to this much, in these steps, before it is dropped.</summary>
    public const float LampMaxShift = 4.5f;
    public const float ShiftStep = 1.5f;

    /// <summary>
    /// Rest stops (a bench, and a bin at every other one) belong to the lamp rhythm: midway between two lamps, every this many lamp
    /// gaps on a side. Each lines the nearest walk on that side within <see cref="RestReach"/> of the road's centre line, sliding
    /// along it by up to <see cref="RestSlide"/>; a slot with no walk that takes a bench stays empty.
    /// </summary>
    public const int RestEvery = 4;
    public const float RestReach = 20f;
    public const float RestSlide = 6f;
    /// <summary>A bench's front edge stands this far off the walk's edge, facing the walk (Opus review 12:15).</summary>
    public const float WalkGap = 0.1f;
    /// <summary>Sitters' feet reach this far onto the walk; the walk keeps at least <see cref="PassClear"/> clear beyond them.</summary>
    public const float FeetReach = 0.5f;
    public const float PassClear = 1.2f;
    /// <summary>No carriageway within this distance ahead of a bench's front edge: it looks across the walk, never at traffic.</summary>
    public const float FaceClear = 12f;
    /// <summary>Benches and bins keep this far off any carriageway's edge, out of the green strip between the kerb and the trees.</summary>
    public const float RoadSetback = 3f;
    /// <summary>
    /// A road's rest node (<see cref="RestNode"/>) takes up to this many benches, on the pedestrian paving within this reach of it, the
    /// spots sampled every <see cref="NodeStep"/> and the benches at least <see cref="NodeBenchGap"/> apart. The rules are the walk benches'.
    /// </summary>
    public const int NodeBenches = 2;
    public const float NodeReach = 20f;
    public const float NodeStep = 1f;
    public const float NodeBenchGap = 4f;
    /// <summary>
    /// A node bench may stand on a plaza's paving (Opus review 17:28), the kiosk's way: its back this far inside the paving edge and
    /// parallel to it, facing into the plaza; tried before the bench beside the paving.
    /// </summary>
    public const float PlazaBenchClear = 0.35f;
    /// <summary>
    /// A node bench may stand on a walk at least <see cref="NodeWalkMin"/> wide (Opus review 13c2b 21:05): at its edge, its back this far
    /// inside the paving edge and parallel to it, facing across the walk, and leaving at least <see cref="NodeWalkPass"/> clear in front of
    /// its sitters' feet. Narrower walks take no bench on them (and WP-20 cuts no bays into them); tried after the plaza, before the
    /// bench beside the paving.
    /// </summary>
    public const float NodeWalkMin = 4f;
    public const float NodeWalkPass = 2.5f;
    public const float WalkBenchClear = 0.15f;
    /// <summary>
    /// A plaza node bench (Opus review 13c2b rework): on the plaza's paving, back to its outer edge, facing in, with at least
    /// <see cref="PlazaPass"/> of free paving in front of its sitters' feet; a walk line the plaza carries does not count against it
    /// (the bench only keeps <see cref="PlazaWalkLineClear"/> off the line itself, so it never sits across a way). Its back may stand
    /// <see cref="PlazaWallClear"/> off a building wall that is the plaza's edge. The plaza's edges are searched within
    /// <see cref="NodePlazaReach"/> of the node, nearest first; narrower walks (under <see cref="NodeWalkMin"/>) still take none.
    /// </summary>
    public const float PlazaPass = 2.5f;
    public const float PlazaWalkLineClear = 0.3f;
    public const float PlazaWallClear = 0.3f;
    public const float NodePlazaReach = 40f;
    /// <summary>A plaza bench is not set against a bank: the ground this far round it rises at most this much above its paving.</summary>
    public const float PlazaBankReach = 0.6f;
    public const float PlazaBankRise = 0.15f;
    /// <summary>The bin stands on its own bay along the same walk edge, this clear gap from the bench: tried in this order, 2–4 m.</summary>
    public static IReadOnlyList<float> BinGaps { get; } = Array.AsReadOnly([2.5f, 3f, 3.5f, 2f, 4f]);
    public const float BinGapMin = 2f;
    public const float BinGapMax = 4f;
    /// <summary>A bench or bin keeps this far beyond its footprint off a planted tree row's line (a trunk and its root flare).</summary>
    public const float RowClear = 0.6f;
    /// <summary>Shuttle-stop pole from the kerb face.</summary>
    public const float StopKerbGap = 0.7f;
    /// <summary>
    /// A kiosk's apron or a post's footprint must touch pedestrian paving, never more than this off it (Opus review 12:15): one that
    /// does not touch moves the shortest way to the paving's edge, keeping its facing; by at most <see cref="AnchorPavingReach"/>,
    /// beyond which the anchor stays and its data needs review (WP-48c).
    /// </summary>
    public const float AnchorPavingGap = 0.5f;
    public const float AnchorPavingReach = 10f;
    /// <summary>
    /// A kiosk whose apron does not touch paving stands whole on it (Opus review 14:20): its back edge this far inside the paving edge,
    /// parallel to it, its face towards the paving. It may slide along the edge by up to <see cref="ApronSlide"/> to fit, and the
    /// outline is checked every <see cref="ApronSample"/>.
    /// </summary>
    public const float ApronPavingClear = 0.5f;
    public const float ApronSlide = 4.5f;
    public const float ApronSample = 0.25f;

    /// <summary>Clearances from building, water, court and parking outlines; from other roads beyond their half width; from trees.</summary>
    public const float OutlineClear = 1.0f;
    public const float RoadClear = 0.5f;
    public const float TreeClear = 1.2f;
    /// <summary>Two placed things keep this far apart (beyond their footprint radii).</summary>
    public const float ItemGap = 0.6f;

    // ---- Landing (WLD-TERRAIN §4.4) ----
    /// <summary>
    /// WP-15 lays its surfaces above the height field: lawn 0.10 m, paving and kerb tops 0.15 m (GroundSection). Furniture takes
    /// its heights from those levels until the world model can say which surface lies at a point.
    /// </summary>
    public const float LawnLift = 0.10f;
    public const float PavingLift = 0.15f;
    /// <summary>Point things (poles, posts): y = H(centre) + lawn − burial, burial 0.05–0.3 m growing with the drop under the footprint.</summary>
    public const float BurialMin = 0.05f;
    public const float BurialMax = 0.3f;
    /// <summary>No pole stands where the grade exceeds 25%.</summary>
    public const float PointMaxGrade = 0.25f;
    /// <summary>Pads under small entities: a paved bay up to 5% across the footprint, a concrete plinth (台基) steeper.</summary>
    public const float SmallMaxGrade = 0.05f;
    /// <summary>Pads stand this far proud of the paving over the highest ground, so their edge reads; their skirt reaches this far below the lowest.</summary>
    public const float PadProud = 0.01f;
    public const float PadSkirt = 0.05f;
    public const float PadBevel = 0.015f;
    /// <summary>Beyond this drop across the footprint a plinth would read as a wall: the thing is not placed.</summary>
    public const float PlinthMaxDrop = 0.6f;
    public const string BayStone = "ground.paver";
    public const string PlinthStone = "ground.concrete";
    public const FootMat BayFoot = FootMat.BrickPaver;
    /// <summary>
    /// A bench's or bin's bay runs from <see cref="PadJoin"/> inside the walk's edge (so bay and walk read as one paving) to this
    /// far behind the furniture, and this far past its ends.
    /// </summary>
    public const float BayBehind = 0.4f;
    public const float BayEnds = 0.35f;
    public const float PadJoin = 0.05f;
    /// <summary>The kiosk's apron reaches this far beyond its canopy on every side.</summary>
    public const float ApronMargin = 0.35f;
    /// <summary>An anchor's groundY this far above the terrain is a deck that is not in the height field (SYS-ATLAS §6: A06).</summary>
    public const float DeckAbove = 0.5f;

    // ---- Parts at typical sizes (13a gallery values) ----
    public static LampParams MainLamp { get; } = new(0.85f, 6f, 0.85f);
    public static LampParams SecondaryLamp { get; } = new(1.7f, 7f, 0.5f) { Variation = 1 };
    public static FurnitureParams Bench { get; } = new(1.8f, 0.8f, 0.62f);
    public static FurnitureParams Bin { get; } = new(2f, 1f, 0.55f);
    public static SignParams Stop { get; } = new(1.2f, 2.5f, 0.18f);

    // ---- Atlas anchors (SYS-ATLAS §6): sizes come from the overlay footprint and height; these are the fallbacks ----
    public static SignParams Kiosk { get; } = new(2f, 2.6f, 0.8f);
    public static SignParams Post { get; } = new(0.3f, 1.05f, 0.3f);

    // ---- Open entrance (WLD-INTERIORS §4): mat in front of the door, standing sign beside it, lamp over the door head ----
    public const float MatDepth = 1.0f;
    public const float MatThickness = 0.025f;
    /// <summary>Mat width = clear width + this, kept within [MatMinWidth, MatMaxWidth].</summary>
    public const float MatExtra = 0.7f;
    public const float MatMinWidth = 1.4f;
    public const float MatMaxWidth = 2.4f;
    /// <summary>Gap between the door plane and the mat's back edge (keeps it off the threshold).</summary>
    public const float MatGap = 0.08f;
    public static SignParams OpenSign { get; } = new(0.45f, 1.3f, 0.32f) { Variation = 1 };
    /// <summary>
    /// The standing sign: this far beyond the mat's side edge, its centre this far out from the door plane (clear of door leaves
    /// held open flat against the reveals, which reach about 1.1 m out).
    /// </summary>
    public const float SignBeside = 0.45f;
    public const float SignOut = 1.3f;
    public static LampParams DoorLamp { get; } = new(0.28f, 0.3f, 0.18f);
    /// <summary>
    /// The entrance anchor carries no door height, so the lamp's bottom sits this far above the floor (a 2.4 m door head plus a
    /// gap). Doorways a zone builds taller place their own kit and claim the building's Props (see the layout).
    /// </summary>
    public const float DoorLampY = 2.55f;

    // ---- Module budget (ENG-BUDGET §8, WP-13 with WP-25: 8% of the main pass), heaviest 64 m cell ----
    public const int BudgetTrianglesL0 = 9000;
    public const int BudgetTrianglesL1 = 2400;
    public const int BudgetDrawCalls = 24;
}
