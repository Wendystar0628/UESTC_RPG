using Campus.Gen.Geometry;
using System.Globalization;
using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Gen.World;

namespace Campus.Gen.Vegetation;

/// <summary>
/// Where a planted tree came from (WLD-VEGETATION): road rows (the ginkgo avenue among them), the avenue band, OSM
/// points, groves; then the empty tree pits of the overlays (perPit trees each) and the design trees the overlays hand to
/// WP-23 (kind tree, props.planter WP-23). The last two are planted after every other source, so the earlier order is kept.
/// </summary>
public enum PlantSource : byte { Road = 0, Band = 1, OsmTree = 2, Grove = 3, Pit = 4, Design = 5 }

/// <summary>
/// One planted tree. Position is the trunk foot on the terrain (the mother model's trunk already runs Burial below it,
/// WLD-TERRAIN). Variant picks the mother model (0–3); Scale stretches it uniformly; Height is the scaled model
/// height. Custom is the INSTANCE_CUSTOM block of WLD-VEGETATION: (seed, row × 16 + look bits, wind position,
/// height), the look bit 1 being the lime-washed lower trunk (§1.7).
/// </summary>
public sealed record PlantedTree(string Key, SpeciesInfo Species, int Variant, Vector3 Position, float Yaw, float Scale,
    float Height, bool Wash, PlantSource Source, float Seed, float WindPos)
{
    public Vector4 Custom => new(Seed, Species.Row * 16 + (Wash ? 1 : 0), WindPos, Height);

    /// <summary>Crown radius on the ground (for spacing, canopy projection and leaf potential).</summary>
    public float CrownRadius => TreeForms.Get(Species.Form).CrownRadius * Height * TreeForms.CrownScale(Variant);
}

/// <summary>A tree the rules would have planted on hard pavement: it gives way. Index is its place in the planting order before the giving way.</summary>
public sealed record AvoidedTree(int Index, PlantedTree Tree);

/// <summary>
/// All trees of one map, in a stable order (sources in enum order, then placement order). Avoided lists the trees that gave
/// way to hard pavement (see <see cref="Planting"/>): they are not in Trees, and Trees is otherwise exactly the planting order.
/// </summary>
public sealed record PlantingLayout(IReadOnlyList<PlantedTree> Trees)
{
    public IReadOnlyList<AvoidedTree> Avoided { get; init; } = [];

    /// <summary>Tree pits that already held a tree (an OSM point or a rule tree), so the pit planting left them alone.</summary>
    public int PitsHeld { get; init; }

    public int Count(PlantSource source) => Trees.Count(t => t.Source == source);

    /// <summary>The planting order before pavement gave way: Trees with the Avoided ones put back in their places.</summary>
    public IReadOnlyList<PlantedTree> BeforeGivingWay()
    {
        var all = new List<PlantedTree>(Trees.Count + Avoided.Count);
        int kept = 0, given = 0;
        while (kept < Trees.Count || given < Avoided.Count)
            all.Add(given < Avoided.Count && Avoided[given].Index == all.Count ? Avoided[given++].Tree : Trees[kept++]);
        return all.AsReadOnly();
    }
}

/// <summary>
/// Whole-campus tree layout (WLD-VEGETATION; ENG-ARCH ports the old landscape.ts placement). Pure and
/// deterministic from the world model, the species table, the map's planting rules and the world seed. Order: linear
/// planting along the named roads (the ginkgo avenue's four rows and spring band first), then the OSM tree points, then
/// groves on the green parcels. Every tree lands on the terrain: y = HeightAt(centre), and none stands where the grade
/// exceeds 25% (WLD-TERRAIN). Trunks keep clear of buildings, water, courts, parking and road surfaces; crowns of
/// groves keep clear too, and groves leave the interior of large lawns open where the cluster noise is low (§5.3).
/// Hard pavement of the effective world (pedestrian areas, footway, path, steps and cycleway areas, and the design walks
/// the overlays add) takes no tree except in a tree pit; a tree that would stand on it gives way last, so every other tree
/// keeps its key, look and place (<see cref="PlantingLayout.Avoided"/>).
/// </summary>
public static partial class Planting
{
    /// <summary>Steepest grade a tree may stand on (WLD-TERRAIN: no trees above 25%).</summary>
    public const float MaxGrade = 0.25f;

    /// <summary>Trunk clearance from building, water, court and parking outlines, and beyond a road's half width.</summary>
    public const float TrunkClearance = 0.8f;

    /// <summary>Road types WP-15 paves as walks and plazas (GroundClassifier): hard pavement, no trunk on it or within <see cref="TrunkClearance"/> of it.</summary>
    private static readonly string[] Paved = ["pedestrian", "footway", "path", "steps", "cycleway"];

    /// <summary>The overlay kind of a tree pit (a design feature): a tree may stand in one although it lies on pavement.</summary>
    public const string TreePitKind = "tree-pit";

    /// <summary>Gap kept between two crowns of grove trees (landscape.ts: 1.2 m).</summary>
    public const float CrownGap = 1.2f;

    /// <summary>OSM tree points closer than this to an already planted tree are the same tree.</summary>
    public const float SameTree = 2.0f;

    /// <summary>landscape.ts: big lawns (m²) whose interior beyond EdgeKeep stays open where the cluster noise is below OpenNoise.</summary>
    public const float BigLawn = 8000f;
    public const float EdgeKeep = 24f;
    public const float OpenNoise = 0.51f;

    /// <summary>Grove species stay coherent over cells of this size (m), so neighbours form small stands.</summary>
    public const float StandCell = 36f;

    /// <summary>Share of a grove tree's species pick that follows its stand; the rest is its own draw.</summary>
    public const float StandWeight = 0.7f;

    /// <summary>Grove crowns are spaced at this share of their radius, so neighbouring crowns interlock into stands.</summary>
    public const float StandOverlap = 0.7f;

    /// <summary>Fixed density for each green parcel's area^0.84 quota (area in m²).</summary>
    public const float GroveDensityCoefficient = 0.006077f;
    public const float GroveAreaExponent = 0.84f;

    private static readonly string[] Blocking = ["building", "water", "sport", "parking"];

    public static PlantingLayout Build(IWorldModel world, SpeciesTable species, PlantingRules rules, uint seed)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(species);
        ArgumentNullException.ThrowIfNull(rules);
        var site = new Site(world, seed);
        var trees = new List<PlantedTree>();
        PlantRoads(site, species, rules, trees);
        PlantOsmTrees(site, species, rules, trees);
        PlantGroves(site, species, rules, trees);
        var held = PlantPits(site, species, rules, trees);
        PlantDesignTrees(site, species, trees);
        return new PlantingLayout(trees.AsReadOnly()) { Avoided = site.Avoided.AsReadOnly(), PitsHeld = held };
    }
}
