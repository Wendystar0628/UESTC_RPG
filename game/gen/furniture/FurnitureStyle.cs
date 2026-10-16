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
public sealed record RoadRule(string Name, RoadTier Tier, IReadOnlyList<float> Rows);

/// <summary>
/// Every tunable value of the WP-13 placement in one place (ENG-TEAM §5): which roads, spacings, offsets from the kerb, the
/// rest-stop rhythm, the part sizes and the landing rules. Initial values follow WLD-LIFE §4, MAP-QSH-ADDITIONS §8, SYS-ATLAS §6,
/// WLD-INTERIORS §4 and WLD-TERRAIN §4.4; the Opus review sets the finals. Part looks stay in <see cref="StreetStyle"/>.
/// </summary>
public static class FurnitureStyle
{
    // ---- Roads (13b mainline: the ginkgo avenue and two representative secondary roads; the rest of the network follows) ----
    /// <remarks>
    /// Row offsets mirror WP-23's planting rule for the same roads (the map's <c>vegetation/planting.json</c>: the avenue's two ginkgo rows
    /// and its spring band, one row on each secondary road) until planted trees are readable from the world model.
    /// </remarks>
    public static IReadOnlyList<RoadRule> Roads { get; } = Array.AsReadOnly<RoadRule>(
    [
        new("银杏大道", RoadTier.Main, [7f, 14f, 17.5f]),
        new("水杉路", RoadTier.Secondary, [5f]),
        new("丹桂路", RoadTier.Secondary, [4.5f]),
    ]);

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
