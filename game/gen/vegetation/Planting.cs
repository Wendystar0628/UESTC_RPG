using System.Globalization;
using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Gen.World;

namespace Campus.Gen.Vegetation;

/// <summary>Where a planted tree came from (WLD-VEGETATION §3, §4): road rows (the ginkgo avenue among them), the avenue band, OSM points, groves.</summary>
public enum PlantSource : byte { Road = 0, Band = 1, OsmTree = 2, Grove = 3 }

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

/// <summary>All trees of one map, in a stable order (sources in enum order, then placement order).</summary>
public sealed record PlantingLayout(IReadOnlyList<PlantedTree> Trees)
{
    public int Count(PlantSource source) => Trees.Count(t => t.Source == source);
}

/// <summary>
/// Whole-campus tree layout (WLD-VEGETATION §3–§4; ENG-ARCH §1 ports the old landscape.ts placement). Pure and
/// deterministic from the world model, the species table, the map's planting rules and the world seed. Order: linear
/// planting along the named roads (the ginkgo avenue's four rows and spring band first), then the OSM tree points, then
/// groves on the green parcels. Every tree lands on the terrain: y = HeightAt(centre), and none stands where the grade
/// exceeds 25% (WLD-TERRAIN §4.4). Trunks keep clear of buildings, water, courts, parking and road surfaces; crowns of
/// groves keep clear too, and groves leave the interior of large lawns open where the cluster noise is low (§5.3).
/// </summary>
public static class Planting
{
    /// <summary>Steepest grade a tree may stand on (WLD-TERRAIN §4.4: no trees above 25%).</summary>
    public const float MaxGrade = 0.25f;

    /// <summary>Trunk clearance from building, water, court and parking outlines, and beyond a road's half width.</summary>
    public const float TrunkClearance = 0.8f;

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
        return new PlantingLayout(trees.AsReadOnly());
    }

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
            if (trees.Any(t => Vector2.Distance(new(t.Position.X, t.Position.Z), p) < SameTree)) continue;
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
        trees.Add(tree);
        site.Occupy(p, crown * (crownClear ? StandOverlap : 1f));
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

    private sealed record Parcel(PolygonRings Rings, Vector2 Min, Vector2 Max, float Area);

    /// <summary>The world as the planter sees it: blocking outlines and roads in a coarse grid, green parcels, occupied crowns.</summary>
    private sealed class Site
    {
        private const float Cell = 32f;
        private readonly Dictionary<(int, int), List<Parcel>> _blocking = [];
        private readonly Dictionary<(int, int), List<NetworkLine>> _roads = [];
        private readonly Dictionary<(int, int), List<(Vector2 P, float R)>> _occupied = [];
        private readonly float _windMin;
        private readonly float _windSpan;

        public Site(IWorldModel world, uint seed)
        {
            World = world;
            Seed = seed;
            var green = new List<Parcel>();
            foreach (var feature in world.Features.Values.OrderBy(f => f.Id.Value, StringComparer.Ordinal))
            {
                foreach (var kind in Blocking)
                    foreach (var rings in Polygons(feature, kind))
                        Index(_blocking, rings.Min, rings.Max, rings);
                foreach (var rings in Polygons(feature, "green")) green.Add(rings);
            }
            Green = green;
            foreach (var road in world.Network.Roads.OrderBy(r => r.Id.Value, StringComparer.Ordinal))
            {
                if (road.Line.Points.Count < 2) continue;
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

        private static IEnumerable<Parcel> Polygons(WorldFeature feature, string kind)
        {
            foreach (var part in Parts(feature, kind))
            {
                IEnumerable<PolygonRings> polygons = part.Geometry switch
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
