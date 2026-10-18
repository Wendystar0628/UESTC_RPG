using System.Globalization;
using System.Numerics;
using Campus.Contracts;
using Campus.Gen.World;

namespace Campus.Gen.Vegetation;

/// <summary>
/// What an understorey plant is (max-011 23c2b): ground cover in a tree pit, a loose shrub of a green-edge border, a module
/// of a clipped hedge run, or a grass tussock breaking up a lawn.
/// </summary>
public enum UnderKind : byte { PitCover = 0, Shrub = 1, Hedge = 2, Tuft = 3 }

/// <summary>
/// One understorey plant: a ball-form mother model (<see cref="TreeForms.IsBall"/>) instanced like a tree, but stretched by
/// Size on its own axes (x along Yaw, y up, z across), so one clipped ball serves as a cushion, a shrub, a hedge module or a
/// tussock. Position is the foot (on the soil of a pit, else on the terrain). Yaw turns about +Y the way the trees' Yaw does
/// (local x goes to (cos, 0, −sin)); hedge modules lay their x along the edge. Custom is the INSTANCE_CUSTOM block of
/// WLD-VEGETATION with the drawn height in w; its appearance variant carries <see cref="Understorey.UnderVariantBit"/>,
/// so veg_card and veg_trunk fade the plant out before its draw distance (23c2c).
/// </summary>
public sealed record GroundPlant(string Key, SpeciesInfo Species, int Variant, Vector3 Position, float Yaw, Vector3 Size, UnderKind Kind,
    float Seed, float WindPos)
{
    public float Height => TreeForms.HeightOf(Species, Variant) * Size.Y;

    public Vector4 Custom => new(Seed, Species.Row * 16 + Understorey.UnderVariantBit, WindPos, Height);
}

/// <summary>All understorey plants of one map, in a stable order (pit cover, borders, tussocks).</summary>
public sealed record UnderstoreyLayout(IReadOnlyList<GroundPlant> Plants)
{
    public int Count(UnderKind kind) => Plants.Count(p => p.Kind == kind);
}

/// <summary>
/// Understorey layout (WLD-VEGETATION; max-011 23c2b). Pure and deterministic from the world model, the species
/// table, the map's planting rules, the tree layout and the world seed:
/// <list type="bullet">
/// <item>tree pits get low clipped cushions round their trunks (the pit's frame, kerb and soil are WP-15's);</item>
/// <item>green edges that face a road or a walk get borders: runs of loose shrub clumps and runs of clipped hedge;</item>
/// <item>lawns get grass tussocks along their edges and round the trunks standing in them.</item>
/// </list>
/// Nothing stands on hard pavement (the planter's pavement index, tree pits aside), on a road, in a building, water, court
/// or car park, on a trunk, or on grades above <see cref="MaxGrade"/>. Shrubs, hedges and tussocks do not collide
/// (WLD-VEGETATION: only blocking hedges do, and the cluster hedges are WP-20's).
/// </summary>
public static class Understorey
{
    public const float MaxGrade = 0.3f;

    /// <summary>
    /// Grass tussocks are switched off for this release (max-011 21:01 review: flat tussocks read as moss patches on open lawn).
    /// The rule, the <c>tuft</c> species row and the <c>tufts</c> entry in planting.json stay; they come back with a dedicated
    /// grass shader (veg_grass). This is the only switch.
    /// </summary>
    public const bool TuftsEnabled = false;

    /// <summary>Draw distance (m) for shrubs, hedge modules and pit cover (max-011 21:01 review: 150 m to 100 m); the consumer's near pool reads it.</summary>
    public const float DrawDistance = 100f;

    /// <summary>
    /// Appearance-variant bit of INSTANCE_CUSTOM.y (row × 16 + variant) set on every understorey plant (23c2c): the veg
    /// shaders fade it out with Bayer dither from <see cref="FadeStart"/> to <see cref="DrawDistance"/> (VEG_UNDER_FADE),
    /// so it never pops at the draw distance. Bit 0 is the trees' whitewash.
    /// </summary>
    public const int UnderVariantBit = 2;

    /// <summary>Start of the understorey fade (m); it ends at <see cref="DrawDistance"/> (veg_* VEG_UNDER_FADE).</summary>
    public const float FadeStart = 85f;

    /// <summary>Draw distance (m) for tussocks, when <see cref="TuftsEnabled"/>.</summary>
    public const float TuftDrawDistance = 45f;

    /// <summary>Clearance of an understorey plant from pavement edges and road surfaces (m).</summary>
    public const float PaveClearance = 0.35f;

    /// <summary>Clearance from building, water, court and parking outlines (m).</summary>
    public const float BlockClearance = 0.6f;

    /// <summary>No shrub or tussock this close to a trunk (m); pit cushions keep <see cref="PitTrunkGap"/>.</summary>
    public const float TrunkGap = 0.9f;
    public const float PitTrunkGap = 0.35f;

    /// <summary>Pit cushions: radius (m) and set-in of their centres from the pit sides (m).</summary>
    public const float CushionRadius = 0.26f;
    public const float CushionInset = 0.42f;

    /// <summary>Strips (pits longer than this half length) get a row of cushions instead of four corners.</summary>
    public const float StripHalfLength = 1.5f;

    /// <summary>Border shrubs: drawn height range (m), and the share of a planted run's samples left open between clumps.</summary>
    public const float ShrubMin = 0.7f, ShrubMax = 1.1f, ClumpGap = 0.25f;

    /// <summary>Clipped hedge modules: height and half width (m); each module overlaps its neighbours by this share of the spacing.</summary>
    public const float HedgeHeight = 0.85f, HedgeHalfWidth = 0.42f, HedgeOverlap = 0.62f;

    /// <summary>Tussocks: drawn height range (m), flattened by <see cref="TuftFlatten"/> and widened by <see cref="TuftWiden"/>; round trunks at this ring (m).</summary>
    public const float TuftMin = 0.28f, TuftMax = 0.48f, TuftFlatten = 0.6f, TuftWiden = 1.4f, TuftRingMin = 0.9f, TuftRingMax = 1.9f;

    /// <summary>Green parcels smaller than this (m²) are verges and islands: no border, no tussocks along their edge.</summary>
    public const float MinParcel = 60f;

    public static UnderstoreyLayout Build(IWorldModel world, SpeciesTable species, PlantingRules rules, PlantingLayout trees, uint seed)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(species);
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(trees);
        var site = new Planting.Site(world, seed);
        var trunks = new TrunkGrid(trees.Trees);
        var plants = new List<GroundPlant>();
        var u = rules.Understorey;
        if (u.Pits is { } cover) PitCover(site, species.Get(cover.Species), cover, trees, plants);
        if (u.Borders is { } borders) Borders(site, species, borders, trunks, plants);
        if (TuftsEnabled && u.Tufts is { } tufts) Tufts(site, species.Get(tufts.Species), tufts, trees, trunks, plants);
        return new UnderstoreyLayout(plants.AsReadOnly());
    }

    // ---------------------------------------------------------------------------------------------------------
    // Tree pits: four corner cushions in a square pit, a row along a strip, a ring in a round bed.
    // ---------------------------------------------------------------------------------------------------------

    private static void PitCover(Planting.Site site, SpeciesInfo info, PitCover cover, PlantingLayout trees, List<GroundPlant> plants)
    {
        foreach (var pit in site.Pits)
        {
            var spots = new List<Vector2>();
            if (pit.Rings is null)
            {
                var r = MathF.Max(0.6f, pit.Radius * 0.42f);
                var n = Math.Max(4, (int)MathF.Round(6.2831853f * r / cover.Spacing));
                for (var i = 0; i < n; i++)
                {
                    var d = TreePlan.Azimuth(6.2831853f * (i + 0.5f) / n);
                    spots.Add(pit.Centre + new Vector2(d.X, d.Z) * r);
                }
            }
            else
            {
                var across = new Vector2(-pit.Axis.Y, pit.Axis.X);
                if (pit.HalfLength < StripHalfLength)
                {
                    var a = MathF.Max(0f, pit.HalfLength - CushionInset);
                    var b = MathF.Max(0f, pit.HalfWidth - CushionInset);
                    foreach (var (sa, sb) in new[] { (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f) }) spots.Add(pit.Centre + pit.Axis * (sa * a) + across * (sb * b));
                }
                else
                    for (var s = -pit.HalfLength + CushionInset; s <= pit.HalfLength - CushionInset + 1e-3f; s += cover.Spacing) spots.Add(pit.Centre + pit.Axis * s);
            }
            for (var i = 0; i < spots.Count; i++)
            {
                var p = spots[i];
                if (!pit.Holds(p) || trees.Trees.Any(t => Vector2.Distance(new(t.Position.X, t.Position.Z), p) < PitTrunkGap + CushionRadius)) continue;
                var key = "pit:" + pit.Key + ":" + i.ToString(CultureInfo.InvariantCulture);
                var (variant, h) = Pick(site.Seed, key);
                var model = Model(info, variant);
                var height = cover.Height * (0.9f + 0.2f * h);
                var radius = CushionRadius * (0.9f + 0.2f * Fraction(h, 7));
                var size = new Vector3(radius / model.Radius, height / model.Height, radius / model.Radius);
                Add(site, plants, key, info, variant, p, pit.SoilAbove, 6.2831853f * Fraction(h, 3), size, UnderKind.PitCover);
            }
        }
    }

    // ---------------------------------------------------------------------------------------------------------
    // Borders along the green edges that face a road or a walk: loose clumps and clipped hedge runs.
    // ---------------------------------------------------------------------------------------------------------

    private static void Borders(Planting.Site site, SpeciesTable species, BorderPlanting rule, TrunkGrid trunks, List<GroundPlant> plants)
    {
        for (var g = 0; g < site.Green.Count; g++)
        {
            var parcel = site.Green[g];
            if (parcel.Area < MinParcel) continue;
            var parcelKey = g.ToString(CultureInfo.InvariantCulture);
            var sample = 0;
            foreach (var (p, tangent, inward, s) in Walk(parcel, rule.Spacing))
            {
                var index = sample++;
                var run = (int)MathF.Floor(s / rule.Run);
                var runKey = parcelKey + ":" + run.ToString(CultureInfo.InvariantCulture);
                if (Hash01(site.Seed, "border-run", runKey) >= rule.Share) continue;
                var hedge = Hash01(site.Seed, "border-hedge", runKey) < rule.HedgeShare;
                var c = p + inward * rule.Inset;
                if (!PlanGeometry.Contains(parcel.Rings, c) || !Facing(site, c, rule.Reach) || !Clear(site, trunks, c, TrunkGap)) continue;
                var key = "border:" + parcelKey + ":" + index.ToString(CultureInfo.InvariantCulture);
                var (variant, h) = Pick(site.Seed, key);
                if (hedge)
                {
                    var info = species.Get(rule.Hedge[(int)(Hash01(site.Seed, "border-hedge-species", runKey) * rule.Hedge.Count) % rule.Hedge.Count]);
                    var model = Model(info, variant);
                    var half = rule.Spacing * HedgeOverlap;
                    var size = new Vector3(half / model.Radius, HedgeHeight / model.Height, HedgeHalfWidth / model.Radius);
                    Add(site, plants, key, info, variant, c, 0f, MathF.Atan2(-tangent.Y, tangent.X), size, UnderKind.Hedge);
                }
                else
                {
                    if (Fraction(h, 5) < ClumpGap) continue;
                    // One species per run, so a clump reads as one planting.
                    var info = species.Get(Pick(rule.Mix, Hash01(site.Seed, "border-mix", runKey)));
                    var model = Model(info, variant);
                    var height = ShrubMin + (ShrubMax - ShrubMin) * Fraction(h, 7);
                    var k = height / model.Height;
                    Add(site, plants, key, info, variant, c, 0f, 6.2831853f * Fraction(h, 3), new Vector3(k), UnderKind.Shrub);
                }
            }
        }
    }

    /// <summary>A road or a walk lies within <paramref name="reach"/>: the edge is a verge, not the far side of a lawn.</summary>
    private static bool Facing(Planting.Site site, Vector2 p, float reach) => site.OnRoad(p, reach) || site.IsPaved(p, reach);

    // ---------------------------------------------------------------------------------------------------------
    // Tussocks: along the lawn edges and round the trunks that stand in green.
    // ---------------------------------------------------------------------------------------------------------

    private static void Tufts(Planting.Site site, SpeciesInfo info, TuftPlanting rule, PlantingLayout trees, TrunkGrid trunks, List<GroundPlant> plants)
    {
        for (var g = 0; g < site.Green.Count; g++)
        {
            var parcel = site.Green[g];
            if (parcel.Area < MinParcel) continue;
            var sample = 0;
            foreach (var (p, _, inward, _) in Walk(parcel, rule.Cell))
            {
                var key = "tuft:" + g.ToString(CultureInfo.InvariantCulture) + ":" + (sample++).ToString(CultureInfo.InvariantCulture);
                var (variant, h) = Pick(site.Seed, key);
                if (Fraction(h, 11) >= rule.Share) continue;
                var c = p + inward * (0.3f + (rule.Edge - 0.3f) * Fraction(h, 5) * Fraction(h, 5));
                if (!PlanGeometry.Contains(parcel.Rings, c) || !Clear(site, trunks, c, TrunkGap)) continue;
                AddTuft(site, plants, key, info, variant, h, c);
            }
        }
        foreach (var tree in trees.Trees)
        {
            if (tree.Source == PlantSource.Pit || TreeForms.IsBall(tree.Species.Form)) continue;
            var at = new Vector2(tree.Position.X, tree.Position.Z);
            if (site.InPit(at) || site.IsPaved(at, 0f)) continue;
            for (var k = 0; k < rule.PerTree; k++)
            {
                var key = "tuft:" + tree.Key + ":" + k.ToString(CultureInfo.InvariantCulture);
                var (variant, h) = Pick(site.Seed, key);
                var d = TreePlan.Azimuth(6.2831853f * (k + Fraction(h, 3)) / rule.PerTree);
                var c = at + new Vector2(d.X, d.Z) * (TuftRingMin + (TuftRingMax - TuftRingMin) * Fraction(h, 5));
                if (!Clear(site, trunks, c, TuftRingMin * 0.8f)) continue;
                AddTuft(site, plants, key, info, variant, h, c);
            }
        }
    }

    private static void AddTuft(Planting.Site site, List<GroundPlant> plants, string key, SpeciesInfo info, int variant, float h, Vector2 c)
    {
        var model = Model(info, variant);
        var k = (TuftMin + (TuftMax - TuftMin) * Fraction(h, 7)) / model.Height;
        Add(site, plants, key, info, variant, c, 0f, 6.2831853f * Fraction(h, 3), new Vector3(k * TuftWiden, k * TuftFlatten, k * TuftWiden), UnderKind.Tuft);
    }

    // ---------------------------------------------------------------------------------------------------------
    // Common.
    // ---------------------------------------------------------------------------------------------------------

    /// <summary>Off pavement and roads, clear of buildings, water, courts and car parks, of trunks, on a gentle grade, inside the boundary.</summary>
    private static bool Clear(Planting.Site site, TrunkGrid trunks, Vector2 p, float trunkGap) =>
        PlanGeometry.Contains(site.World.Boundary, p) && !site.InPit(p) && !site.IsPaved(p, PaveClearance) && !site.OnRoad(p, PaveClearance) &&
        !site.Blocked(p, BlockClearance) && !trunks.Near(p, trunkGap) && site.World.Terrain.FactsAt(p.X, p.Y).Grade <= MaxGrade;

    private static void Add(Planting.Site site, List<GroundPlant> plants, string key, SpeciesInfo info, int variant, Vector2 p, float lift,
        float yaw, Vector3 size, UnderKind kind)
    {
        var y = site.World.Terrain.HeightAt(p.X, p.Y) + lift;
        var seed = (StableHash.Hash32(site.Seed, "veg", key) >> 8) * (1f / 16777216f);
        plants.Add(new GroundPlant(key, info, variant, new Vector3(p.X, y, p.Y), yaw, size, kind, seed, site.WindPos(p.X)));
    }

    /// <summary>The mother model's drawn height and crown radius (the yardsticks a plant's Size is measured against).</summary>
    private static (float Height, float Radius) Model(SpeciesInfo info, int variant)
    {
        var height = TreeForms.HeightOf(info, variant);
        return (height, TreeForms.Get(info.Form).CrownRadius * height * TreeForms.CrownScale(variant));
    }

    private static (int Variant, float Hash) Pick(uint seed, string key)
    {
        var h = StableHash.Hash32(seed, "under", key);
        return ((int)(h % TreeForms.Variants), (h >> 8) * (1f / 16777216f));
    }

    /// <summary>Another uniform value from one hash (the fractional part of a multiple), so one hash feeds several choices.</summary>
    private static float Fraction(float h, int k) => h * k - MathF.Floor(h * k);

    private static float Hash01(uint seed, string purpose, string key) => (StableHash.Hash32(seed, purpose, key) >> 8) * (1f / 16777216f);

    private static string Pick(IReadOnlyList<(string Species, float Weight)> mix, float u)
    {
        var total = mix.Sum(m => m.Weight);
        var acc = 0f;
        foreach (var (id, weight) in mix)
        {
            acc += weight / total;
            if (u < acc) return id;
        }
        return mix[^1].Species;
    }

    /// <summary>
    /// Points every <paramref name="step"/> m along a parcel's outer ring, with the unit tangent, the unit normal pointing into
    /// the parcel and the arc length.
    /// </summary>
    private static IEnumerable<(Vector2 P, Vector2 Tangent, Vector2 Inward, float S)> Walk(Planting.Parcel parcel, float step)
    {
        var ring = parcel.Rings.Outer;
        // Counter-clockwise rings (positive signed area) have the interior on the left of the direction of travel.
        var left = PlanGeometry.SignedArea(ring) > 0;
        var s = 0f;
        var next = step * 0.5f;
        for (var i = 0; i < ring.Count; i++)
        {
            var a = ring[i];
            var b = ring[(i + 1) % ring.Count];
            var length = Vector2.Distance(a, b);
            if (length < 1e-4f) continue;
            var t = (b - a) / length;
            var n = left ? new Vector2(-t.Y, t.X) : new Vector2(t.Y, -t.X);
            while (next <= s + length)
            {
                yield return (a + t * (next - s), t, n, next);
                next += step;
            }
            s += length;
        }
    }

    /// <summary>Trunk positions in 4 m cells, for the gap test.</summary>
    private sealed class TrunkGrid
    {
        private const float Cell = 4f;
        private readonly SpatialGrid<Vector2> _cells = new(Cell);

        public TrunkGrid(IEnumerable<PlantedTree> trees)
        {
            foreach (var t in trees)
            {
                if (TreeForms.IsBall(t.Species.Form)) continue;
                var p = new Vector2(t.Position.X, t.Position.Z);
                _cells.AddPoint(p, p);
            }
        }

        public bool Near(Vector2 p, float gap) => _cells.AnyAround(p, q => Vector2.DistanceSquared(p, q) < gap * gap);
    }
}
