using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Street;

namespace Campus.Gen.Furniture;

/// <summary>
/// The class of a road for street furniture (WLD-LIFE-STREET, MAP-QSH-ATMOSPHERE), read from the network line's own
/// <c>highway</c> by <see cref="FurnitureStyle.TierOf"/>: how often it has lamps and rest stops is <see cref="FurnitureStyle.Rule"/>.
/// </summary>
public enum RoadTier
{
    /// <summary>The tree-lined main axis (a carriageway with a tree row and a spring band in the planting rules): white disc lamps on both sides.</summary>
    Avenue,
    /// <summary>The campus's other trunk carriageways (<c>residential</c> and above): grey single-arm LED lamps on one side.</summary>
    Trunk,
    /// <summary>Service lanes (<c>service</c>, <c>unclassified</c>): the same lamps, further apart, few rest stops.</summary>
    Branch,
    /// <summary>Pedestrian streets (<c>pedestrian</c>): lamps only; footways, paths and steps get none (the third, point-like lighting grade has no part yet).</summary>
    Walk,
}

/// <summary>
/// What a <see cref="RoadTier"/> gets: its lamps (<paramref name="LampSpacing"/> apart, on <paramref name="BothSides"/> opposite each other
/// or on the side with more free slots; <paramref name="DiscLamp"/> the white disc head, else the grey single arm) and its rest stops (a
/// bench, and a bin at every <see cref="FurnitureStyle.RestBinEvery"/>-th one) midway between two lamps, every <paramref name="RestEvery"/> lamp
/// gaps on a side; 0: none along the line.
/// </summary>
public sealed record TierRule(float LampSpacing, bool BothSides, bool DiscLamp, int RestEvery);

/// <summary>
/// Every tunable value of the WP-13 placement in one place (WF-FLOW): which roads, spacings, offsets from the kerb, the
/// rest-stop rhythm, the part sizes and the landing rules. Initial values follow WLD-LIFE, MAP-QSH-ADDITIONS, SYS-ATLAS,
/// WLD-INTERIORS and WLD-TERRAIN; the Opus review sets the finals. Part looks stay in <see cref="StreetStyle"/>.
/// </summary>
public static class FurnitureStyle
{
    // ---- Roads: every lit carriageway and pedestrian street of the campus, by its class (WLD-LIFE-STREET) ----
    /// <summary>
    /// Lamp and rest-stop rules by class. The avenue and the trunk roads follow MAP-QSH-ATMOSPHERE (grade one 25–30 m, kept
    /// regular on the avenue, grade two 30–40 m); the service lanes sit at the far end of grade two with a stop every eighth lamp gap;
    /// pedestrian streets are lit like grade two, with no stops along their line (stops belong at the plaza ends and junction nodes).
    /// Together they put some tens of bins on the campus, most of them on the trunk roads (WLD-LIFE-STREET: 92 in all).
    /// </summary>
    public static TierRule Rule(RoadTier tier) => tier switch
    {
        RoadTier.Avenue => new(27.5f, true, true, 4),
        RoadTier.Trunk => new(35f, false, false, 4),
        RoadTier.Branch => new(40f, false, false, 8),
        RoadTier.Walk => new(40f, false, false, 0),
        _ => throw new ArgumentOutOfRangeException(nameof(tier), tier, "Unknown road tier."),
    };

    /// <summary>
    /// The class of a network line by its <c>highway</c> (<paramref name="type"/>); null for what gets no lamps (footways,
    /// paths, steps, cycleways, tracks, roads under construction, motorways). A carriageway the planting rules give a tree row and a band
    /// (<see cref="AvenueRows"/> offsets) is the avenue.
    /// </summary>
    public static RoadTier? TierOf(string? type, bool treeLined) => type switch
    {
        "residential" or "living_street" or "tertiary" or "secondary" or "primary" => treeLined ? RoadTier.Avenue : RoadTier.Trunk,
        "service" or "unclassified" => RoadTier.Branch,
        "pedestrian" => RoadTier.Walk,
        _ => null,
    };

    /// <summary>
    /// The tree rows' offsets come from the map's <see cref="PlantingKey"/> build input (WP-23's planting rule, <see cref="PlantingRows"/>):
    /// the avenue's ginkgo row and its spring band each side, one row on each other tree-lined road. A road with this many offsets is the avenue;
    /// without the input nothing is a tree row and nothing is the avenue, with a warning diagnostic.
    /// </summary>
    public const int AvenueRows = 2;

    /// <summary>A bin at every this-many-th rest stop of a road (the first has one).</summary>
    public const int RestBinEvery = 2;

    /// <summary>
    /// A road with a feature of its own gets it from the map's overlay (the road named by the feature's <c>road</c> prop): a
    /// <see cref="RestNodeKind"/> point is a place where the road's rest stops stand, instead of along its line (the road then has
    /// none along it); a <see cref="KerbRampsKind"/> feature gives it kerb ramps where its walks reach its kerb.
    /// </summary>
    public const string RestNodeKind = "restNode";
    public const string KerbRampsKind = "kerbRamps";

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

    // ---- Barriers (WLD-LIFE-STREET 路障): white stone spheres across the mouth of a wide walk where it meets a carriageway ----
    /// <summary>
    /// A main junction is where a carriageway meets a walk (footway, path, pedestrian street) at least <see cref="BarrierWalkMin"/> wide: the
    /// walk's end within <see cref="RampKerbReach"/> of the kerb, or the walk crossing the carriageway (a row on each side). The row stands
    /// <see cref="BarrierSetback"/> inside the walk from the kerb, across it, <see cref="BarrierPitch"/> apart, centred on the walk and
    /// <see cref="BarrierEdge"/> clear of its edges: cars stop, people pass between the spheres. The kerb is looked for within
    /// <see cref="BarrierSearch"/> along the walk, in <see cref="BarrierStep"/> steps; two rows within <see cref="BarrierRowGap"/> are one junction.
    /// Size is the part's default (white stone, variation 0).
    /// </summary>
    public static FurnitureParams BarrierSphere { get; } = (FurnitureParams)PartCatalog.Find(new(PartCatalog.StoneSphere))!.Defaults;
    public const float BarrierWalkMin = 3f;
    public const float BarrierSetback = 1f;
    public const float BarrierPitch = 1.5f;
    public const float BarrierEdge = 0.15f;
    public const float BarrierSearch = 8f;
    public const float BarrierStep = 0.25f;
    public const float BarrierRowGap = 2f;

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
    // ---- (WLD-LIFE; WLD-AMBIENCE rule 4: one theme a place, things in a cluster; "no piling up": rather too few). ----
    /// <summary>
    /// A vending group is <see cref="VendingMin"/>–<see cref="VendingMax"/> machines side by side, <see cref="VendingSideGap"/> apart, on one
    /// paved bay, every other group holding one low-saturation odd machine (WLD-LIFE). Groups stand at the dormitory cluster gates
    /// (the open gate first, then by ID), then at the canteens and the sports venues (<see cref="VendingCategories"/>: by the first open
    /// entrance, else the side of the building nearest the network); none within <see cref="VendingSpacing"/> of another (one to a line of
    /// sight) and none within <see cref="MainAxisClear"/> of a main road (WLD-AMBIENCE: no vending groups on the main axis).
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
    /// it, MAP-QSH-NORTH) and at least <see cref="GateKeep"/> off the gate point, so the way through stays clear.
    /// </summary>
    public const float GateOutside = 0.8f;
    public const float GateKeep = 4f;
    /// <summary>
    /// Delivery lockers: WLD-LIFE keeps them to two places. Their two places (the west gate's clinic wicket, inside West Gate 2) are
    /// not in the map data yet (WP-30's gates), so on the mainline they stand at the first <see cref="LockerSites"/> gates that hold a
    /// vending group, beside it: the dormitory gate is where deliveries are picked up. WP-30's gate points replace them.
    /// </summary>
    public static EquipmentParams Locker { get; } = new(2.4f, 2f, 0.6f);
    public const int LockerSites = 2;
    /// <summary>
    /// Shared-bicycle bays (WLD-LIFE: the zones register them later; P0 starts with a few): at the open cluster gates, then at the
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

    // ---- Parked cars (WLD-LIFE: a few low-saturation cars in each car park, autumn evening) ----
    /// <summary>Slots run along a lot's longest edge at this pitch, the cars' noses this far in from it, at most this many a lot.</summary>
    public const float CarSlot = 2.6f;
    public const float CarNoseIn = 0.6f;
    public const int CarsPerLot = 3;
    /// <summary>A slot holds a car when its hash falls under this share (sparse, WLD-LIFE).</summary>
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

    /// <summary>Pole centre from the carriageway edge (the kerb face).</summary>
    public const float LampKerbGap = 0.8f;
    /// <summary>A lamp blocked at its slot may slide along the road by up to this much, in these steps, before it is dropped.</summary>
    public const float LampMaxShift = 4.5f;
    public const float ShiftStep = 1.5f;

    /// <summary>
    /// Rest stops (a bench, and a bin at every <see cref="RestBinEvery"/>-th one) belong to the lamp rhythm: midway between two lamps, every
    /// <see cref="TierRule.RestEvery"/> lamp gaps on a side. Each lines the nearest walk on that side within <see cref="RestReach"/> of the
    /// road's centre line, sliding along it by up to <see cref="RestSlide"/>; a slot with no walk that takes a bench stays empty.
    /// </summary>
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
    /// A road's rest node (<see cref="RestNodeKind"/>) takes up to this many benches, on the pedestrian paving within this reach of it, the
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
    /// <see cref="PlazaWallClear"/> (at least 1.5 m) off any building wall. The plaza's edges are searched within
    /// <see cref="NodePlazaReach"/> of the node, nearest first; narrower walks (under <see cref="NodeWalkMin"/>) still take none.
    /// </summary>
    public const float PlazaPass = 2.5f;
    public const float PlazaWalkLineClear = 0.3f;
    public const float PlazaWallClear = 1.5f;
    /// <summary>
    /// A plaza bench's back stands against planting (Opus review 13c2c): a tree or a registered tree row within this far behind its back
    /// at one of its two ends or its middle. A plaza edge with none takes no bench, so a bench never backs onto bare paving or a wall.
    /// </summary>
    public const float PlazaBackPlanting = 4f;
    /// <summary>A plaza with no edge backed by planting takes one bench beside a tree: a tree or row within this far of the bench's middle.</summary>
    public const float PlazaBesideTree = 7f;
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

    // ---- Landing (WLD-TERRAIN) ----
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
    /// <summary>An anchor's groundY this far above the terrain is a deck that is not in the height field (SYS-ATLAS: A06).</summary>
    public const float DeckAbove = 0.5f;

    // ---- Parts at typical sizes (13a gallery values) ----
    public static LampParams MainLamp { get; } = new(0.85f, 6f, 0.85f);
    public static LampParams SecondaryLamp { get; } = new(1.7f, 7f, 0.5f) { Variation = 1 };
    public static FurnitureParams Bench { get; } = new(1.8f, 0.8f, 0.62f);
    public static FurnitureParams Bin { get; } = new(2f, 1f, 0.55f);
    public static SignParams Stop { get; } = new(1.2f, 2.5f, 0.18f);

    // ---- Atlas anchors (SYS-ATLAS): sizes come from the overlay footprint and height; these are the standard proportions ----
    public static SignParams Kiosk { get; } = new(2f, 2.6f, 0.8f);
    public static SignParams Post { get; } = new(0.3f, 1.05f, 0.3f);

    // ---- Open entrance (WLD-INTERIORS): mat in front of the door, standing sign beside it, lamp over the door head ----
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

    // ---- Module budget (ENG-BUDGET, WP-13 with WP-25: 8% of the main pass), heaviest 64 m cell ----
    public const int ServiceCellMaxTriangles = 9000;

    public const int ServiceCellMaxDraws = 24;
}
