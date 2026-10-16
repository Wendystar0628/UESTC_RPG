using System.Globalization;
using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Gen.World;

namespace Campus.Gen.Vegetation;

/// <summary>
/// Where a planted tree came from (WLD-VEGETATION §3, §4): road rows (the ginkgo avenue among them), the avenue band, OSM
/// points, groves; then the empty tree pits of the overlays (perPit trees each) and the design trees the overlays hand to
/// WP-23 (kind tree, props.planter WP-23). The last two are planted after every other source, so the earlier order is kept.
/// </summary>
public enum PlantSource : byte { Road = 0, Band = 1, OsmTree = 2, Grove = 3, Pit = 4, Design = 5 }

/// <summary>
/// One planted tree. Position is the trunk foot on the terrain (the mother model's trunk already runs Burial below it,
/// WLD-TERRAIN §4.4). Variant picks the mother model (0–3); Scale stretches it uniformly; Height is the scaled model
/// height. Custom is the INSTANCE_CUSTOM block of WLD-VEGETATION §11.2: (seed, row × 16 + look bits, wind position,
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
/// Whole-campus tree layout (WLD-VEGETATION §3–§4; ENG-ARCH §1 ports the old landscape.ts placement). Pure and
/// deterministic from the world model, the species table, the map's planting rules and the world seed. Order: linear
/// planting along the named roads (the ginkgo avenue's four rows and spring band first), then the OSM tree points, then
/// groves on the green parcels. Every tree lands on the terrain: y = HeightAt(centre), and none stands where the grade
/// exceeds 25% (WLD-TERRAIN §4.4). Trunks keep clear of buildings, water, courts, parking and road surfaces; crowns of
/// groves keep clear too, and groves leave the interior of large lawns open where the cluster noise is low (§5.3).
/// Hard pavement of the effective world (pedestrian areas, footway, path, steps and cycleway areas, and the design walks
/// the overlays add) takes no tree except in a tree pit; a tree that would stand on it gives way last, so every other tree
/// keeps its key, look and place (<see cref="PlantingLayout.Avoided"/>).
/// </summary>
public static class Planting
{
    /// <summary>Steepest grade a tree may stand on (WLD-TERRAIN §4.4: no trees above 25%).</summary>
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

    // ---------------------------------------------------------------------------------------------------------
    // Tree pits and design trees (max-011 23c2b): planted last, so every earlier tree keeps its key, look and place.
    // ---------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Each tree pit of the overlays that no planted tree stands in gets its perPit trees (props.species, else the rules' pit
    /// species: square pits and strips one, round beds the other), evenly along the pit's long side. A pit that already holds a
    /// tree (an OSM point or a rule tree kept by the pit) is left as it is. Returns the number of such pits.
    /// </summary>
    private static int PlantPits(Site site, SpeciesTable species, PlantingRules rules, List<PlantedTree> trees)
    {
        var held = 0;
        foreach (var pit in site.Pits)
        {
            if (pit.PerPit <= 0) continue;
            if (trees.Any(t => pit.Holds(new(t.Position.X, t.Position.Z))))
            {
                held++;
                continue;
            }
            var id = pit.Species is { } given && species.TryGet(given, out _) ? given : pit.Rings is null ? rules.Pits.Round : rules.Pits.Square;
            var info = species.Get(id);
            for (var k = 0; k < pit.PerPit; k++)
            {
                var p = pit.Centre + pit.Axis * (pit.HalfLength * 2f * ((k + 0.5f) / pit.PerPit - 0.5f));
                var key = pit.PerPit == 1 ? pit.Key : pit.Key + ":" + k.ToString(CultureInfo.InvariantCulture);
                Place(site, info, p, key, PlantSource.Pit, trees, scale: null);
            }
        }
        return held;
    }

    /// <summary>
    /// Trees the overlays place and hand to WP-23 (kind tree, props.planter WP-23, props.species): planted where they stand,
    /// with the mother model whose height is nearest to the feature's height, scaled to it. A design tree on top of an already
    /// planted one (within <see cref="SameTree"/>) is that tree.
    /// </summary>
    private static void PlantDesignTrees(Site site, SpeciesTable species, List<PlantedTree> trees)
    {
        foreach (var feature in site.World.Features.Values.Where(f => f.Kind.Value == DesignTreeKind).OrderBy(f => f.Id.Value, StringComparer.Ordinal))
        {
            if (Text(feature.Props, "planter") != Planter || feature.Geometry is not PointGeometry point) continue;
            if (Text(feature.Props, "species") is not { } id || !species.TryGet(id, out var info)) continue;
            var p = new Vector2(point.Position.X, point.Position.Z);
            if (trees.Any(t => Vector2.Distance(new(t.Position.X, t.Position.Z), p) < SameTree)) continue;
            Place(site, info!, p, feature.Id.Value, PlantSource.Design, trees, feature.Height is > 0f and var h ? h : null);
        }
    }

    /// <summary>The overlay kind of a design tree, and the planter value that hands it to this package.</summary>
    public const string DesignTreeKind = "tree";
    public const string Planter = "WP-23";

    /// <summary>A deliberately placed tree (pit or design): no clearance or pavement test, only the boundary and the terrain.</summary>
    private static void Place(Site site, SpeciesInfo info, Vector2 p, string key, PlantSource source, List<PlantedTree> trees, float? scale)
    {
        if (!PlanGeometry.Contains(site.World.Boundary, p)) return;
        var probe = Probe(site, info, key);
        if (scale is { } height)
        {
            // The mother model nearest the given height, scaled to it exactly.
            var variant = Enumerable.Range(0, TreeForms.Variants).MinBy(v => MathF.Abs(TreeForms.HeightOf(info, v) - height));
            probe = probe with { Variant = variant, Scale = height / TreeForms.HeightOf(info, variant), Height = height };
        }
        var terrain = site.World.Terrain;
        var tree = probe with { Position = new Vector3(p.X, terrain.HeightAt(p.X, p.Y), p.Y), Source = source, WindPos = site.WindPos(p.X) };
        site.Occupy(p, tree.CrownRadius);
        trees.Add(tree);
    }

    private static string? Text(JsonElement? props, string name) =>
        props is { ValueKind: JsonValueKind.Object } p && p.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    // ---------------------------------------------------------------------------------------------------------
    // Linear planting (§3): rows parallel to every road of a rule's name, chained end to end.
    // ---------------------------------------------------------------------------------------------------------

    private static void PlantRoads(Site site, SpeciesTable species, PlantingRules rules, List<PlantedTree> trees)
    {
        for (var r = 0; r < rules.Roads.Count; r++)
        {
            var rule = rules.Roads[r];
            var chains = Chains(site.World.Network.Roads.Where(l => RoadName(l) == rule.Name).ToList());
            for (var c = 0; c < chains.Count; c++)
            {
                var chain = chains[c];
                for (var side = -1; side <= 1; side += 2)
                {
                    for (var w = 0; w < rule.Rows.Count; w++)
                    {
                        var row = rule.Rows[w];
                        var start = rule.Spacing * (row.Stagger ? 1f : 0.5f);
                        var i = 0;
                        foreach (var (p, n) in Samples(chain, start, rule.Spacing))
                        {
                            var index = i++;
                            var key = Key("road", r, c, side, w, index);
                            var id = rule.Species[index % rule.Species.Count];
                            Try(site, species.Get(id), p + n * (side * row.Offset), key, rule.Wash, PlantSource.Road, trees, crownClear: false);
                        }
                    }
                    if (rule.Band is not { } band) continue;
                    var j = 0;
                    foreach (var (p, n) in Samples(chain, band.Spacing * 0.5f, band.Spacing))
                    {
                        var index = j++;
                        var key = Key("band", r, c, side, 0, index);
                        var id = band.Species[index % band.Species.Count];
                        Try(site, species.Get(id), p + n * (side * band.Offset), key, false, PlantSource.Band, trees, crownClear: false);
                    }
                }
            }
        }
    }

    private static string? RoadName(NetworkLine line)
    {
        if (line.Props is not { ValueKind: JsonValueKind.Object } props) return null;
        foreach (var name in new[] { "name:zh", "name" })
            if (props.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String) return value.GetString();
        return null;
    }

    /// <summary>Joins lines that share end points (within 1 m) into chains, in ID order, so a road split by a bridge stays one row.</summary>
    private static List<List<Vector2>> Chains(List<NetworkLine> lines)
    {
        var left = lines.OrderBy(l => l.Id.Value, StringComparer.Ordinal).Select(l => l.Line.Points.ToList()).Where(p => p.Count >= 2).ToList();
        var chains = new List<List<Vector2>>();
        while (left.Count > 0)
        {
            var chain = left[0];
            left.RemoveAt(0);
            for (var grown = true; grown;)
            {
                grown = false;
                for (var i = 0; i < left.Count; i++)
                {
                    var l = left[i];
                    if (Vector2.Distance(chain[^1], l[0]) < 1f) chain.AddRange(l.Skip(1));
                    else if (Vector2.Distance(chain[^1], l[^1]) < 1f) chain.AddRange(Enumerable.Reverse(l).Skip(1));
                    else if (Vector2.Distance(chain[0], l[^1]) < 1f) chain.InsertRange(0, l.Take(l.Count - 1));
                    else if (Vector2.Distance(chain[0], l[0]) < 1f) chain.InsertRange(0, Enumerable.Reverse(l).Take(l.Count - 1));
                    else continue;
                    left.RemoveAt(i);
                    grown = true;
                    break;
                }
            }
            chains.Add(chain);
        }
        return chains;
    }

    /// <summary>Points every <paramref name="spacing"/> along a polyline from <paramref name="start"/>, with the unit left normal there.</summary>
    private static IEnumerable<(Vector2 P, Vector2 N)> Samples(List<Vector2> line, float start, float spacing)
    {
        var lengths = new float[line.Count];
        for (var i = 1; i < line.Count; i++) lengths[i] = lengths[i - 1] + Vector2.Distance(line[i - 1], line[i]);
        var total = lengths[^1];
        for (var s = start; s < total; s += spacing)
        {
            // The normal comes from a 4 m chord, so rows round the bends instead of kinking at the vertices.
            var a = At(line, lengths, MathF.Max(0f, s - 2f));
            var b = At(line, lengths, MathF.Min(total, s + 2f));
            var t = b - a;
            if (t.LengthSquared() < 1e-6f) continue;
            t = Vector2.Normalize(t);
            yield return (At(line, lengths, s), new Vector2(-t.Y, t.X));
        }
    }

    private static Vector2 At(List<Vector2> line, float[] lengths, float s)
    {
        var i = 1;
        while (i < line.Count - 1 && lengths[i] < s) i++;
        var span = lengths[i] - lengths[i - 1];
        return Vector2.Lerp(line[i - 1], line[i], span > 1e-6f ? Math.Clamp((s - lengths[i - 1]) / span, 0f, 1f) : 0f);
    }

    // ---------------------------------------------------------------------------------------------------------
    // OSM tree points (§3): the species of the nearest linear-planting road, else the default (camphor).
    // ---------------------------------------------------------------------------------------------------------

    private static void PlantOsmTrees(Site site, SpeciesTable species, PlantingRules rules, List<PlantedTree> trees)
    {
        var named = rules.Roads.SelectMany(rule => site.World.Network.Roads.Where(l => RoadName(l) == rule.Name)
            .Select(l => (Rule: rule, Line: l))).ToList();
        foreach (var point in site.World.Network.Trees.OrderBy(t => t.Id.Value, StringComparer.Ordinal))
        {
            var p = new Vector2(point.Position.X, point.Position.Z);
            if (trees.Any(t => Vector2.Distance(new(t.Position.X, t.Position.Z), p) < SameTree) ||
                site.Avoided.Any(a => Vector2.Distance(new(a.Tree.Position.X, a.Tree.Position.Z), p) < SameTree)) continue;
            var id = rules.OsmDefault;
            var wash = false;
            var best = rules.OsmNear;
            foreach (var (rule, line) in named)
            {
                var d = MathF.Sqrt(PolylineDistanceSquared(p, line.Line.Points));
                if (d < best) (best, id, wash) = (d, rule.Species[0], rule.Wash);
            }
            if (point.Species is { Length: > 0 } given && species.TryGet(given, out var known)) id = known!.Id;
            Try(site, species.Get(id), p, point.Id.Value, wash, PlantSource.OsmTree, trees, crownClear: false);
        }
    }

    // ---------------------------------------------------------------------------------------------------------
    // Groves (§4, landscape.ts): green parcels weighted by area^0.84; open lawns kept; species by place and stand.
    // ---------------------------------------------------------------------------------------------------------

    private static void PlantGroves(Site site, SpeciesTable species, PlantingRules rules, List<PlantedTree> trees)
    {
        var parcels = site.Green.Where(p => p.Area > 35f).ToList();
        if (parcels.Count == 0) return;
        var weights = new float[parcels.Count];
        var sum = 0f;
        for (var i = 0; i < parcels.Count; i++) weights[i] = sum += DetMath.Pow(parcels[i].Area, 0.84f);
        var target = Math.Min(rules.Groves.Target, (int)MathF.Round(parcels.Sum(p => p.Area) / rules.Groves.AreaPerTree));
        var areas = rules.Groves.Areas.Select(a => (Area: a, Box: site.Envelope(a.Name))).Where(a => a.Box is not null).ToList();
        var wash = rules.Groves.WashNear.Select(a => (Area: a, Box: site.Envelope(a.Name))).Where(a => a.Box is not null).ToList();
        var rng = new Rng(StableHash.Hash32(site.Seed, "veg", "groves"));
        var planted = 0;
        for (var attempt = 0; attempt < target * 90 && planted < target; attempt++)
        {
            var choice = rng.NextFloat() * sum;
            var parcel = parcels[Math.Max(0, Array.FindIndex(weights, w => w >= choice))];
            var x = parcel.Min.X + rng.NextFloat() * (parcel.Max.X - parcel.Min.X);
            var z = parcel.Min.Y + rng.NextFloat() * (parcel.Max.Y - parcel.Min.Y);
            var draw = rng.NextFloat();
            var p = new Vector2(x, z);
            if (!PlanGeometry.Contains(parcel.Rings, p) || !PlanGeometry.Contains(site.World.Boundary, p)) continue;
            var edge = Edge(parcel.Rings, p);
            var cluster = (DetMath.Sin(x / 53f + DetMath.Cos(z / 47f)) + DetMath.Cos(z / 59f - x / 139f) + 2f) / 4f;
            if (parcel.Area > BigLawn && edge > EdgeKeep && cluster < OpenNoise) continue;
            var mix = areas.FirstOrDefault(a => Near(a.Box!.Value, p, a.Area.Radius)).Area?.Mix ?? rules.Groves.Mix;
            var stand = Hash01(site.Seed, "stand", (int)MathF.Floor(x / StandCell), (int)MathF.Floor(z / StandCell));
            var info = species.Get(Pick(mix, StandWeight * stand + (1f - StandWeight) * draw));
            var key = "grove:" + planted.ToString(CultureInfo.InvariantCulture) + ":" + attempt.ToString(CultureInfo.InvariantCulture);
            var washed = wash.Any(a => Near(a.Box!.Value, p, a.Area.Radius));
            // Crowns include their envelope: keep the crown inside the parcel's edge band (landscape.ts: 0.7 r).
            var probe = Probe(site, info, key);
            if (edge < probe.CrownRadius * 0.7f) continue;
            if (Try(site, info, p, key, washed, PlantSource.Grove, trees, crownClear: true)) planted++;
        }
    }

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

    private static bool Near((Vector2 Min, Vector2 Max) box, Vector2 p, float radius)
    {
        var dx = MathF.Max(0f, MathF.Max(box.Min.X - p.X, p.X - box.Max.X));
        var dz = MathF.Max(0f, MathF.Max(box.Min.Y - p.Y, p.Y - box.Max.Y));
        return dx * dx + dz * dz <= radius * radius;
    }

    private static float Edge(PolygonRings rings, Vector2 p) =>
        rings.Holes.Aggregate(PlanGeometry.DistanceToRing(p, rings.Outer), (d, h) => MathF.Min(d, PlanGeometry.DistanceToRing(p, h)));

    // ---------------------------------------------------------------------------------------------------------
    // Common: variant, scale, terrain, clearance.
    // ---------------------------------------------------------------------------------------------------------

    /// <summary>The variant, scale and seed a key gets, before the position is tested.</summary>
    private static PlantedTree Probe(Site site, SpeciesInfo info, string key)
    {
        var h = StableHash.Hash32(site.Seed, "veg", key);
        var seed = (h >> 8) * (1f / 16777216f);
        var h2 = StableHash.Hash32(h, "look");
        var variant = (int)(h2 % TreeForms.Variants);
        var scale = 0.92f + 0.16f * ((h2 >> 8) & 0xffff) / 65535f;
        var yaw = 6.2831853f * (StableHash.Hash32(h, "yaw") >> 8) * (1f / 16777216f);
        var height = TreeForms.HeightOf(info, variant) * scale;
        return new PlantedTree(key, info, variant, Vector3.Zero, yaw, scale, height, false, PlantSource.Grove, seed, 0f);
    }

    private static bool Try(Site site, SpeciesInfo info, Vector2 p, string key, bool wash, PlantSource source, List<PlantedTree> trees, bool crownClear)
    {
        if (!PlanGeometry.Contains(site.World.Boundary, p)) return false;
        var probe = Probe(site, info, key);
        var crown = probe.CrownRadius;
        // Trunks keep off building, water, court and parking outlines and road surfaces; grove crowns keep clear entirely.
        var keep = crownClear ? crown + 0.7f : TrunkClearance;
        if (site.Blocked(p, keep) || site.OnRoad(p, crownClear ? crown * 0.5f + TrunkClearance : TrunkClearance)) return false;
        if (crownClear && !site.Spaced(p, crown * StandOverlap)) return false;
        var terrain = site.World.Terrain;
        if (terrain.FactsAt(p.X, p.Y).Grade > MaxGrade) return false;
        var position = new Vector3(p.X, terrain.HeightAt(p.X, p.Y), p.Y);
        var tree = probe with { Position = position, Wash = wash, Source = source, WindPos = site.WindPos(p.X) };
        site.Occupy(p, crown * (crownClear ? StandOverlap : 1f));
        // Pavement is tested last and the tree still counts as planted (its place stays taken, a grove's count and keys go on),
        // so only the trees on pavement leave the layout and every other tree stays exactly as it was.
        if (site.OnPavement(p)) site.Avoided.Add(new(trees.Count + site.Avoided.Count, tree));
        else trees.Add(tree);
        return true;
    }

    private static float Hash01(uint seed, string purpose, int x, int z) =>
        (StableHash.Hash32(seed, purpose, x.ToString(CultureInfo.InvariantCulture), z.ToString(CultureInfo.InvariantCulture)) >> 8) * (1f / 16777216f);

    private static string Key(string kind, int rule, int chain, int side, int row, int index) => string.Join(':', kind,
        rule.ToString(CultureInfo.InvariantCulture), chain.ToString(CultureInfo.InvariantCulture),
        side < 0 ? "l" : "r", row.ToString(CultureInfo.InvariantCulture), index.ToString(CultureInfo.InvariantCulture));

    private static float PolylineDistanceSquared(Vector2 p, IReadOnlyList<Vector2> line)
    {
        var best = float.MaxValue;
        for (var i = 1; i < line.Count; i++) best = MathF.Min(best, PlanGeometry.SegmentDistanceSquared(p, line[i - 1], line[i]));
        return best;
    }

    internal sealed record Parcel(PolygonRings Rings, Vector2 Min, Vector2 Max, float Area);

    /// <summary>One piece of hard pavement: an area, or a centre line with its half width. Min and Max box it (without the clearance).</summary>
    private sealed record Pave(PolygonRings? Rings, IReadOnlyList<Vector2>? Line, float Half, Vector2 Min, Vector2 Max);

    /// <summary>
    /// A tree pit: an area (a square pit or a planting strip; Axis along its long side, HalfLength and HalfWidth its half
    /// extents), or a disc round Centre (a round bed). Key is the feature ID, with the polygon's index for a multi-polygon.
    /// PerPit and Species come from the feature's props; SoilAbove is the soil surface above the ground (m).
    /// </summary>
    internal sealed record Pit(string Key, PolygonRings? Rings, Vector2 Centre, float Radius, Vector2 Axis, float HalfLength, float HalfWidth,
        int PerPit, string? Species, float SoilAbove)
    {
        public bool Holds(Vector2 p) => Rings is { } rings ? PlanGeometry.Contains(rings, p) : Vector2.Distance(p, Centre) <= Radius;
    }

    /// <summary>The world as the planter sees it: blocking outlines and roads in a coarse grid, green parcels, occupied crowns.</summary>
    internal sealed class Site
    {
        private const float Cell = 32f;
        private readonly Dictionary<(int, int), List<Parcel>> _blocking = [];
        private readonly Dictionary<(int, int), List<NetworkLine>> _roads = [];
        private readonly Dictionary<(int, int), List<Pave>> _pavement = [];
        private readonly List<Pit> _pits = [];
        private readonly Dictionary<(int, int), List<(Vector2 P, float R)>> _occupied = [];
        private readonly float _windMin;
        private readonly float _windSpan;

        public Site(IWorldModel world, uint seed)
        {
            World = world;
            Seed = seed;
            var green = new List<Parcel>();
            // The effective network (WP-08) publishes a design walk as a network line under its feature's ID and keeps the feature's
            // polyline: line and polyline are one walk. The network is the effective source, so the walk is filed once, as the line.
            var published = world.Network.Roads.Select(r => r.Id.Value).ToHashSet(StringComparer.Ordinal);
            foreach (var feature in world.Features.Values.OrderBy(f => f.Id.Value, StringComparer.Ordinal))
            {
                foreach (var kind in Blocking)
                    foreach (var rings in Polygons(feature, kind))
                        Index(_blocking, rings.Min, rings.Max, rings);
                foreach (var rings in Polygons(feature, "green")) green.Add(rings);
                // The effective world's hard pavement: the road areas and design walks of the features (the network's own
                // centre lines are roads above), and the tree pits that may stand on it.
                if (!(IsWalk(feature) && published.Contains(feature.Id.Value)))
                    foreach (var (props, geometry) in RoadSources(feature)) IndexPavement(props, geometry);
                if (feature.Kind.Value == TreePitKind) AddPits(feature);
            }
            Green = green;
            foreach (var road in world.Network.Roads.OrderBy(r => r.Id.Value, StringComparer.Ordinal))
            {
                if (road.Line.Points.Count < 2) continue;
                // A design walk is pavement, not a road: it takes the trunks that stand on it (tree pits excepted) and not the ones
                // beside it, and a tree on it still counts as planted. Only the walk's own line is read, at the network's width.
                if (world.Features.TryGetValue(road.Id, out var walk) && IsWalk(walk) && RoadType(road.Props) is { } type && Paved.Contains(type))
                {
                    IndexPavement(road.Props, new PolylineGeometry(road.Line), road.Width);
                    continue;
                }
                var min = road.Line.Points.Aggregate(new Vector2(float.MaxValue), Vector2.Min) - new Vector2(road.Width * 0.5f);
                var max = road.Line.Points.Aggregate(new Vector2(float.MinValue), Vector2.Max) + new Vector2(road.Width * 0.5f);
                Index(_roads, min, max, road);
            }
            var outer = world.Boundary.Outer;
            _windMin = outer.Count > 0 ? outer.Min(p => p.X) : -1000f;
            _windSpan = MathF.Max(1f, (outer.Count > 0 ? outer.Max(p => p.X) : 1000f) - _windMin);
        }

        public IWorldModel World { get; }
        public uint Seed { get; }
        public List<Parcel> Green { get; }

        /// <summary>The trees that gave way to pavement so far, in planting order.</summary>
        public List<AvoidedTree> Avoided { get; } = [];

        /// <summary>The tree pits of the overlays, in feature-ID order.</summary>
        public IReadOnlyList<Pit> Pits => _pits;

        /// <summary>WLD-VEGETATION §11.2 z: 0 at the west edge of the campus, 1 at the east edge.</summary>
        public float WindPos(float x) => Math.Clamp((x - _windMin) / _windSpan, 0f, 1f);

        public bool Blocked(Vector2 p, float radius)
        {
            foreach (var parcel in Near(_blocking, p))
            {
                if (p.X < parcel.Min.X - radius || p.X > parcel.Max.X + radius || p.Y < parcel.Min.Y - radius || p.Y > parcel.Max.Y + radius) continue;
                if (PlanGeometry.Contains(parcel.Rings, p) || Edge(parcel.Rings, p) < radius) return true;
            }
            return false;
        }

        public bool OnRoad(Vector2 p, float clearance)
        {
            foreach (var road in Near(_roads, p))
            {
                var r = road.Width * 0.5f + clearance;
                if (PolylineDistanceSquared(p, road.Line.Points) < r * r) return true;
            }
            return false;
        }

        /// <summary>True when a trunk at <paramref name="p"/> stands on hard pavement or within <see cref="TrunkClearance"/> of it, and not in a tree pit.</summary>
        public bool OnPavement(Vector2 p) => !InPit(p) && IsPaved(p, TrunkClearance);

        public bool InPit(Vector2 p) => _pits.Any(pit => pit.Holds(p));

        /// <summary>True when <paramref name="p"/> lies on hard pavement or within <paramref name="clearance"/> of it (tree pits included).</summary>
        public bool IsPaved(Vector2 p, float clearance)
        {
            foreach (var pave in Near(_pavement, p))
            {
                if (pave.Rings is { } rings)
                {
                    if (p.X < pave.Min.X - clearance || p.X > pave.Max.X + clearance ||
                        p.Y < pave.Min.Y - clearance || p.Y > pave.Max.Y + clearance) continue;
                    if (PlanGeometry.Contains(rings, p) || Edge(rings, p) < clearance) return true;
                }
                else
                {
                    var r = pave.Half + clearance;
                    if (PolylineDistanceSquared(p, pave.Line!) < r * r) return true;
                }
            }
            return false;
        }

        public bool Spaced(Vector2 p, float radius)
        {
            var (cx, cz) = Key(p);
            for (var dx = -1; dx <= 1; dx++)
                for (var dz = -1; dz <= 1; dz++)
                    if (_occupied.TryGetValue((cx + dx, cz + dz), out var list) &&
                        list.Any(o => Vector2.Distance(p, o.P) < radius + o.R + CrownGap))
                        return false;
            return true;
        }

        public void Occupy(Vector2 p, float radius)
        {
            var key = Key(p);
            if (!_occupied.TryGetValue(key, out var list)) _occupied.Add(key, list = []);
            list.Add((p, radius));
        }

        /// <summary>Plan envelope of the features named <paramref name="name"/> (all parts), or null when the map has none.</summary>
        public (Vector2 Min, Vector2 Max)? Envelope(string name)
        {
            var boxes = World.Features.Values.Where(f => f.Name == name)
                .Select(f => (new Vector2(f.Bounds.Min.X, f.Bounds.Min.Z), new Vector2(f.Bounds.Max.X, f.Bounds.Max.Z))).ToList();
            if (boxes.Count == 0) return null;
            return (boxes.Aggregate(new Vector2(float.MaxValue), (a, b) => Vector2.Min(a, b.Item1)),
                boxes.Aggregate(new Vector2(float.MinValue), (a, b) => Vector2.Max(a, b.Item2)));
        }

        /// <summary>The road sources of a feature: its parts of kind road, else its own geometry; each with the props that carry the road type.</summary>
        private static IEnumerable<(JsonElement? Props, OverlayGeometry Geometry)> RoadSources(WorldFeature feature)
        {
            if (feature.Parts.Count == 0)
            {
                if (feature.Kind.Value == "road") yield return (feature.Props, feature.Geometry);
                yield break;
            }
            foreach (var part in Parts(feature, "road")) yield return (part.Props ?? feature.Props, part.Geometry);
        }

        /// <summary>A feature that is just a polyline of its own (a design walk): the form in which the network line of its ID is also filed.</summary>
        private static bool IsWalk(WorldFeature feature) => feature.Parts.Count == 0 && feature.Geometry is PolylineGeometry;

        private void IndexPavement(JsonElement? props, OverlayGeometry geometry, float width = 0f)
        {
            var type = RoadType(props);
            if (type is null || !Paved.Contains(type)) return;
            switch (geometry)
            {
                case PolygonGeometry polygon:
                    IndexPavement(polygon.Rings);
                    break;
                case MultiPolygonGeometry multi:
                    foreach (var rings in multi.Polygons) IndexPavement(rings);
                    break;
                case PolylineGeometry { Line.Points: { Count: >= 2 } points }:
                    var half = (width > 0f ? width : PavedWidth(props, type)) * 0.5f;
                    var min = points.Aggregate(new Vector2(float.MaxValue), Vector2.Min) - new Vector2(half);
                    var max = points.Aggregate(new Vector2(float.MinValue), Vector2.Max) + new Vector2(half);
                    Index(_pavement, min, max, new Pave(null, points, half, min, max));
                    break;
            }
        }

        private void IndexPavement(PolygonRings rings)
        {
            if (rings.Outer.Count < 3) return;
            var min = rings.Outer.Aggregate(new Vector2(float.MaxValue), Vector2.Min);
            var max = rings.Outer.Aggregate(new Vector2(float.MinValue), Vector2.Max);
            Index(_pavement, min, max, new Pave(rings, null, 0f, min, max));
        }

        /// <summary>Width of a paved line as WP-15 paves it (GroundClassifier): its own width, else the type's default.</summary>
        private static float PavedWidth(JsonElement? props, string type) =>
            Number(props, "width") is { } width and > 0f ? width : type switch
            {
                "path" => 2f,
                "cycleway" => 3f,
                "pedestrian" => 5f,
                _ => 2.5f
            };

        /// <summary>A tree pit feature (design addition): square pits are areas; a round bed is a point with a radius, else half its size.</summary>
        private void AddPits(WorldFeature feature)
        {
            var perPit = Number(feature.Props, "perPit") is { } n and >= 0f ? (int)MathF.Round(n) : 1;
            if (Text(feature.Props, "plant") is { } plant && plant != "tree") perPit = 0;
            var species = Text(feature.Props, "species");
            var soil = Number(feature.Props, "soilAbove") is { } s and >= 0f ? s : 0.3f;
            var id = feature.Id.Value;
            switch (feature.Geometry)
            {
                case PolygonGeometry polygon:
                    AddPit(id, polygon.Rings);
                    break;
                case MultiPolygonGeometry multi:
                    for (var i = 0; i < multi.Polygons.Count; i++) AddPit(id + ":" + i.ToString(CultureInfo.InvariantCulture), multi.Polygons[i]);
                    break;
                case PointGeometry point:
                    var radius = Number(feature.Props, "radius") is { } r and > 0f ? r : Number(feature.Props, "size") is { } size and > 0f ? size * 0.5f : 0.9f;
                    _pits.Add(new(id, null, new Vector2(point.Position.X, point.Position.Z), radius, Vector2.UnitX, 0f, 0f, perPit, species, soil));
                    break;
            }

            void AddPit(string key, PolygonRings rings)
            {
                var outer = rings.Outer;
                if (outer.Count < 3) return;
                // The long side gives the axis (pits are squares or strips); the extents are measured across it.
                var e0 = outer[1] - outer[0];
                var e1 = outer[2] - outer[1];
                var axis = Vector2.Normalize(e0.LengthSquared() >= e1.LengthSquared() ? e0 : e1);
                var across = new Vector2(-axis.Y, axis.X);
                var centre = PlanGeometry.Centroid(outer);
                float a0 = float.MaxValue, a1 = float.MinValue, b0 = float.MaxValue, b1 = float.MinValue;
                foreach (var q in outer)
                {
                    var d = q - centre;
                    (a0, a1) = (MathF.Min(a0, Vector2.Dot(d, axis)), MathF.Max(a1, Vector2.Dot(d, axis)));
                    (b0, b1) = (MathF.Min(b0, Vector2.Dot(d, across)), MathF.Max(b1, Vector2.Dot(d, across)));
                }
                _pits.Add(new(key, rings, centre, 0f, axis, (a1 - a0) * 0.5f, (b1 - b0) * 0.5f, perPit, species, soil));
            }
        }

        private static string? Text(JsonElement? props, string name) =>
            props is { ValueKind: JsonValueKind.Object } p && p.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

        private static string? RoadType(JsonElement? props)
        {
            if (props is not { ValueKind: JsonValueKind.Object } p) return null;
            foreach (var name in new[] { "roadType", "highway" })
                if (p.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String) return value.GetString();
            return null;
        }

        private static float? Number(JsonElement? props, string name) =>
            props is { ValueKind: JsonValueKind.Object } p && p.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number &&
            value.TryGetSingle(out var number) && float.IsFinite(number) ? number : null;

        private static IEnumerable<Parcel> Polygons(WorldFeature feature, string kind)
        {
            foreach (var part in Parts(feature, kind))
            {
                IEnumerable<PolygonRings> polygons = PlanGeometry.FacadeGeometryOf(part) switch
                {
                    PolygonGeometry polygon => [polygon.Rings],
                    MultiPolygonGeometry multi => multi.Polygons,
                    _ => []
                };
                foreach (var rings in polygons)
                {
                    if (rings.Outer.Count < 3) continue;
                    var min = rings.Outer.Aggregate(new Vector2(float.MaxValue), Vector2.Min);
                    var max = rings.Outer.Aggregate(new Vector2(float.MinValue), Vector2.Max);
                    var area = MathF.Abs((float)PlanGeometry.SignedArea(rings.Outer)) - rings.Holes.Sum(h => MathF.Abs((float)PlanGeometry.SignedArea(h)));
                    yield return new Parcel(rings, min, max, area);
                }
            }
        }

        /// <summary>The parts of a feature of one kind (a part's own kind wins over its feature's), as the world model groups them.</summary>
        public static IEnumerable<WorldFeaturePart> Parts(WorldFeature feature, string kind)
        {
            if (feature.Parts.Count == 0)
            {
                if (feature.Kind.Value == kind && feature.Geometry is not (PointGeometry or PolylineGeometry))
                    yield return new("", feature.Geometry, feature.Height, feature.Storeys);
                yield break;
            }
            foreach (var part in feature.Parts)
            {
                var partKind = part.Props is { ValueKind: JsonValueKind.Object } props && props.TryGetProperty("kind", out var declared)
                    ? declared.GetString() : feature.Kind.Value;
                if (partKind == kind) yield return part;
            }
        }

        private static (int, int) Key(Vector2 p) => ((int)MathF.Floor(p.X / Cell), (int)MathF.Floor(p.Y / Cell));

        private static void Index<T>(Dictionary<(int, int), List<T>> grid, Vector2 min, Vector2 max, T item)
        {
            var (x0, z0) = Key(min - new Vector2(Cell * 0.5f));
            var (x1, z1) = Key(max + new Vector2(Cell * 0.5f));
            for (var x = x0; x <= x1; x++)
                for (var z = z0; z <= z1; z++)
                {
                    if (!grid.TryGetValue((x, z), out var list)) grid.Add((x, z), list = []);
                    list.Add(item);
                }
        }

        private static IEnumerable<T> Near<T>(Dictionary<(int, int), List<T>> grid, Vector2 p) =>
            grid.TryGetValue(Key(p), out var list) ? list : [];
    }
}
