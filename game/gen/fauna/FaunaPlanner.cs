using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using S = Campus.Gen.Fauna.FaunaStyle;

namespace Campus.Gen.Fauna;

/// <summary>Which model an animal uses.</summary>
public readonly record struct FaunaModelKey(FaunaKind Kind, CatCoat Coat, byte Pose, bool White)
{
    public static FaunaModelKey CatOf(CatCoat coat, CatPose pose) => new(FaunaKind.Cat, coat, (byte)pose, false);
    public static FaunaModelKey PigeonOf(BirdPose pose, bool white) => new(FaunaKind.Pigeon, CatCoat.GingerWhite, (byte)pose, white);
    public static FaunaModelKey SparrowOf(BirdPose pose) => new(FaunaKind.Sparrow, CatCoat.GingerWhite, (byte)pose, false);
    public static FaunaModelKey SwanOf(SwanPose pose) => new(FaunaKind.Swan, CatCoat.GingerWhite, (byte)pose, false);
    public static FaunaModelKey CrowOf() => new(FaunaKind.Crow, CatCoat.GingerWhite, (byte)BirdPose.Perch, false);

    public AnimalMesh Build() => Kind switch
    {
        FaunaKind.Cat => FaunaModels.Cat(Coat, (CatPose)Pose),
        FaunaKind.Pigeon => FaunaModels.Pigeon((BirdPose)Pose, White),
        FaunaKind.Sparrow => FaunaModels.Sparrow((BirdPose)Pose),
        FaunaKind.Swan => FaunaModels.Swan((SwanPose)Pose),
        FaunaKind.Crow => FaunaModels.Crow(),
        _ => throw new ArgumentOutOfRangeException(nameof(Kind), Kind, "Unknown fauna kind."),
    };
}

/// <summary>One animal of a set: model, world position of its feet, heading (radians, model +Z turned about +Y) and scale.</summary>
public sealed record FaunaInstance(FaunaModelKey Model, Vector3 Position, float Yaw, float Scale);

/// <summary>
/// One place with its own latch (WLD-LIFE): the animals it shows in each set. A set may be empty (the pigeons' night
/// roost is under the eaves, out of sight). <see cref="Bounds"/> covers every set, so a switch waits until all of them are off-screen.
/// </summary>
public sealed record FaunaSite(string Key, FaunaKind Kind, IReadOnlyList<FaunaInstance> Day, IReadOnlyList<FaunaInstance> Night,
    IReadOnlyList<FaunaInstance> Rain, Box3 Bounds)
{
    public IReadOnlyList<FaunaInstance> Set(FaunaSet set) => set switch
    {
        FaunaSet.Day => Day,
        FaunaSet.Night => Night,
        FaunaSet.Rain => Rain,
        _ => throw new ArgumentOutOfRangeException(nameof(set), set, "Unknown fauna set."),
    };
}

public sealed record FaunaLayout(IReadOnlyList<FaunaSite> Sites, IReadOnlyList<string> Log);

/// <summary>
/// Chooses the mainline animal sites from the world model alone (WLD-LIFE): no feature IDs, no map IDs. Buildings are found by
/// the entity category (residence, research, dining), the pigeon plaza is the largest stepped plaza (overlay kind "steps"), perches
/// are seat walls (kind "seat-wall") and sparrows keep to trees of the planting network. Ground heights come from the world's
/// terrain (WLD-TERRAIN). Deterministic: the same world and seed give the same layout.
/// </summary>
public static class FaunaPlanner
{
    private sealed record Footprint(WorldFeature Feature, IReadOnlyList<Vector2> Ring, Vector2 Min, Vector2 Max, float Area, string Category);

    /// <param name="lakes">The map's lakes in order (MapDescriptor.Env.ReflectLakes): the first holds four black swans, the second two.
    /// Without it the two largest water bodies stand in.</param>
    public static FaunaLayout Build(IWorldModel world, uint seed, IReadOnlyList<FeatureId>? lakes = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        var log = new List<string>();
        var buildings = Footprints(world, f => f.Kind == S.Building).ToArray();
        var water = Footprints(world, f => f.Kind == S.Water).ToArray();
        var roads = world.Network.Roads.Where(r => r.Line.Points.Count >= 2).ToArray();
        bool Free(Vector2 p) => !buildings.Any(b => Inside(b, p)) && !water.Any(w => Inside(w, p));
        float Ground(Vector2 p) => world.Spatial.GroundY(p.X, p.Y);

        var sites = new List<FaunaSite>();
        var catSpots = new List<Vector2>();
        Cats(world, seed, buildings, roads, Free, Ground, sites, catSpots, log);
        Pigeons(world, seed, buildings, Free, Ground, sites, log);
        Sparrows(world, seed, buildings, Free, Ground, sites, log);
        Swans(world, seed, lakes, water, buildings, sites, log);
        Crows(world, seed, buildings, sites, log);
        return new(sites.AsReadOnly(), log.AsReadOnly());
    }

    // ------------------------------------------------------------ cats

    private static void Cats(IWorldModel world, uint seed, Footprint[] buildings, NetworkLine[] roads, Func<Vector2, bool> free,
        Func<Vector2, float> ground, List<FaunaSite> sites, List<Vector2> spots, List<string> log)
    {
        var residence = Ranked(buildings.Where(b => b.Category == S.Residence), seed, "fauna.cat").ToArray();
        var research = Ranked(buildings.Where(b => b.Category == S.Research), seed, "fauna.cat").ToArray();
        // 1. The dorm pair (ginger-and-white lying, calico sitting); 2. the black cat by another dorm; 3. the white cat at a research building.
        var plans = new (string Key, Footprint[] From, CatCoat[] Coats)[]
        {
            ("cat.dorm-pair", residence, [CatCoat.GingerWhite, CatCoat.Calico]),
            ("cat.dorm-black", residence, [CatCoat.Black]),
            ("cat.research-white", research, [CatCoat.White]),
        };
        foreach (var (key, from, coats) in plans)
        {
            var placed = false;
            foreach (var b in from)
            {
                if (DoorSide(b, roads, free) is not { } door) continue;
                if (spots.Any(s => Vector2.Distance(s, door.Spot) < S.CatSiteSpacing)) continue;
                spots.Add(door.Spot);
                sites.Add(CatSite(key, seed, coats, door, ground));
                log.Add($"{key}: by '{b.Feature.Name ?? b.Feature.Id.Value}' at ({door.Spot.X:F1}, {door.Spot.Y:F1})");
                placed = true;
                break;
            }
            if (!placed) log.Add($"{key}: no building with a free door side near a road; left out");
        }
    }

    private readonly record struct Door(Vector2 Wall, Vector2 Spot, Vector2 Out, Vector2 Along);

    /// <summary>The building's long-enough wall nearest to a road, with a free spot in front of it (and free room for the night pair).</summary>
    private static Door? DoorSide(Footprint b, NetworkLine[] roads, Func<Vector2, bool> free)
    {
        Door? best = null;
        var bestD = float.MaxValue;
        var ring = b.Ring;
        for (var i = 0; i < ring.Count; i++)
        {
            var a = ring[i];
            var c = ring[(i + 1) % ring.Count];
            var edge = c - a;
            var length = edge.Length();
            if (length < 4f) continue;
            var along = edge / length;
            var mid = (a + c) * 0.5f;
            var outward = new Vector2(along.Y, -along.X);
            if (Inside(b, mid + outward * 0.3f)) outward = -outward;
            var spot = mid + outward * S.CatDoorOffset;
            // Without a road network (a stub world) the longest free wall stands in for the door side.
            var d = roads.Length == 0 ? -length : roads.Min(r => DistanceToLine(r.Line.Points, spot) - r.Width * 0.5f);
            if ((roads.Length > 0 && (d > S.CatRoadReach || d < 0.8f)) || d >= bestD) continue;
            if (!free(spot) || !free(spot + along * 0.6f) || !free(spot - along * 0.6f) || !free(spot + outward * S.CatNightOut)) continue;
            bestD = d;
            best = new(mid + outward * S.CatEaveOffset, spot, outward, along);
        }
        return best;
    }

    private static FaunaSite CatSite(string key, uint seed, CatCoat[] coats, Door door, Func<Vector2, float> ground)
    {
        var rng = new Rng(StableHash.Hash32(seed, key, "fauna.cat.pose"));
        FaunaInstance Cat(CatCoat coat, CatPose pose, Vector2 at, float turnDeg)
        {
            var heading = Rotate(door.Out, (turnDeg + (rng.NextFloat() - 0.5f) * 20f) * float.Pi / 180f);
            return new(FaunaModelKey.CatOf(coat, pose), new(at.X, ground(at), at.Y), Yaw(heading), S.Scale);
        }
        var o = door.Out;
        var t = door.Along;
        List<FaunaInstance> day = [], night = [], rain = [];
        if (coats.Length == 2)
        {
            day.Add(Cat(coats[0], CatPose.Loaf, door.Spot - t * 0.4f, 25f));
            day.Add(Cat(coats[1], CatPose.Sit, door.Spot + t * 0.45f, -20f));
            night.Add(Cat(coats[0], CatPose.Sit, door.Spot + o * S.CatNightOut - t * 0.4f, 15f));
            night.Add(Cat(coats[1], CatPose.Loaf, door.Spot + o * S.CatNightOut + t * 0.5f, -35f));
            rain.Add(Cat(coats[0], CatPose.Curl, door.Wall - t * 0.45f, 80f));
            rain.Add(Cat(coats[1], CatPose.Sit, door.Wall + t * 0.4f + o * 0.05f, -10f));
        }
        else if (coats[0] == CatCoat.Black)
        {
            day.Add(Cat(coats[0], CatPose.Loaf, door.Spot, 30f));
            night.Add(Cat(coats[0], CatPose.Sit, door.Spot + t * 0.3f, -15f));
            rain.Add(Cat(coats[0], CatPose.Curl, door.Wall, 90f));
        }
        else
        {
            // The white cat on the research building's steps: by day only ("more cats by day, fewer at night").
            day.Add(Cat(coats[0], CatPose.Sit, door.Spot, 10f));
            rain.Add(Cat(coats[0], CatPose.Curl, door.Wall, -80f));
        }
        return Site(key, FaunaKind.Cat, day, night, rain);
    }

    // ------------------------------------------------------------ pigeons

    private static void Pigeons(IWorldModel world, uint seed, Footprint[] buildings, Func<Vector2, bool> free, Func<Vector2, float> ground,
        List<FaunaSite> sites, List<string> log)
    {
        var plazas = Footprints(world, f => f.Kind == S.Steps).OrderByDescending(p => p.Area).ThenBy(p => p.Feature.Id.Value, StringComparer.Ordinal).ToArray();
        if (plazas.Length == 0) { log.Add("pigeons: no stepped plaza; left out"); return; }
        var plaza = plazas[0];
        var centre = Centroid(plaza.Ring);
        var foot = Prop(plaza.Feature, "footRadius") ?? MathF.Sqrt(plaza.Area / float.Pi) * 0.9f;
        // Toward the library when there is one (the flock keeps to the plaza and the library's great steps, WLD-LIFE).
        var library = buildings.Where(b => b.Category == "library").OrderByDescending(b => b.Area).FirstOrDefault();
        var toward = library is null ? Vector2.UnitY : OrganicMeshExt.Unit2(Centroid(library.Ring) - centre, Vector2.UnitY);
        var flock = centre + toward * (foot * S.PigeonRingFraction);
        var rng = new Rng(StableHash.Hash32(seed, "fauna.pigeons", "flock"));
        var day = new List<FaunaInstance>();
        var taken = new List<Vector2>();
        for (var attempt = 0; attempt < 200 && day.Count < S.PigeonCount; attempt++)
        {
            var angle = rng.NextFloat() * 2f * float.Pi;
            var radius = MathF.Sqrt(rng.NextFloat()) * S.PigeonScatter;
            var p = flock + new Vector2(MathF.Cos(angle), MathF.Sin(angle) * 0.7f) * radius;
            var yaw = rng.NextFloat() * 2f * float.Pi;
            var peck = rng.NextFloat() < 0.6f;
            if (!free(p) || taken.Any(q => Vector2.Distance(p, q) < 0.5f)) continue;
            taken.Add(p);
            var white = day.Count == 4;   // one white bird among the grey (WLD-LIFE)
            day.Add(new(FaunaModelKey.PigeonOf(peck ? BirdPose.Peck : BirdPose.Stand, white), new(p.X, ground(p) + S.PavingLift, p.Y), yaw, S.Scale));
        }
        sites.Add(Site("pigeons.plaza", FaunaKind.Pigeon, day, [], []));
        log.Add($"pigeons.plaza: {day.Count} birds at ({flock.X:F1}, {flock.Y:F1}) on '{plaza.Feature.Id.Value}'");
    }

    // ------------------------------------------------------------ sparrows

    private static void Sparrows(IWorldModel world, uint seed, Footprint[] buildings, Func<Vector2, bool> free, Func<Vector2, float> ground,
        List<FaunaSite> sites, List<string> log)
    {
        var hosts = buildings.Where(b => b.Category is S.Dining or S.Residence).ToArray();
        var trees = world.Network.Trees
            .Select(t => (Tree: t, P: new Vector2(t.Position.X, t.Position.Z)))
            .Where(t => hosts.Any(b => DistanceToRing(b.Ring, t.P) <= S.SparrowTreeReach) && free(t.P))
            .OrderBy(t => StableHash.Hash32(seed, t.Tree.Id.Value, "fauna.sparrow")).ThenBy(t => t.Tree.Id.Value, StringComparer.Ordinal)
            .ToArray();
        var groups = new List<Vector2>();
        foreach (var (tree, at) in trees)
        {
            if (groups.Count >= S.SparrowGroups) break;
            if (groups.Any(g => Vector2.Distance(g, at) < S.SparrowGroupSpacing)) continue;
            var rng = new Rng(StableHash.Hash32(seed, tree.Id.Value, "fauna.sparrow.group"));
            var birds = new List<FaunaInstance>();
            for (var attempt = 0; attempt < 60 && birds.Count < S.SparrowsPerGroup; attempt++)
            {
                var angle = rng.NextFloat() * 2f * float.Pi;
                var p = at + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * (1.5f + rng.NextFloat() * 1.8f);
                if (!free(p) || birds.Any(b => Vector2.Distance(p, new(b.Position.X, b.Position.Z)) < 0.3f)) continue;
                var pose = rng.NextFloat() < 0.55f ? BirdPose.Peck : BirdPose.Stand;
                birds.Add(new(FaunaModelKey.SparrowOf(pose), new(p.X, ground(p), p.Y), rng.NextFloat() * 2f * float.Pi, S.Scale));
            }
            if (birds.Count == 0) continue;
            groups.Add(at);
            var key = $"sparrows.tree-{groups.Count}";
            sites.Add(Site(key, FaunaKind.Sparrow, birds, [], []));
            log.Add($"{key}: {birds.Count} birds under tree '{tree.Id.Value}' at ({at.X:F1}, {at.Y:F1})");
        }
        // Perched pairs on seat-wall tops ("屋檐站立" stand-in where a perch height is known exactly).
        var walls = world.Features.Values.Where(f => f.Kind == S.SeatWall && f.Geometry is PolylineGeometry && f.Height is > 0.2f)
            .OrderBy(f => StableHash.Hash32(seed, f.Id.Value, "fauna.sparrow.wall")).ThenBy(f => f.Id.Value, StringComparer.Ordinal)
            .Take(S.SparrowWalls).ToArray();
        foreach (var wall in walls)
        {
            var points = ((PolylineGeometry)wall.Geometry).Line.Points;
            var birds = new List<FaunaInstance>();
            foreach (var (t, turn) in new[] { (0.36f, 0.5f), (0.41f, -0.6f) })
            {
                var (p, dir) = AlongLine(points, t);
                var normal = new Vector2(-dir.Y, dir.X);
                var heading = Rotate(normal, turn);
                birds.Add(new(FaunaModelKey.SparrowOf(BirdPose.Perch), new(p.X, ground(p) + wall.Height!.Value, p.Y), Yaw(heading), S.Scale));
            }
            var key = $"sparrows.wall-{sites.Count(s => s.Key.StartsWith("sparrows.wall", StringComparison.Ordinal)) + 1}";
            sites.Add(Site(key, FaunaKind.Sparrow, birds, [], []));
            log.Add($"{key}: 2 birds on seat wall '{wall.Id.Value}'");
        }
    }

    // ------------------------------------------------------------ black swans

    private static void Swans(IWorldModel world, uint seed, IReadOnlyList<FeatureId>? lakes, Footprint[] water, Footprint[] buildings,
        List<FaunaSite> sites, List<string> log)
    {
        var chosen = (lakes is { Count: > 0 }
                ? lakes.Select(id => water.Where(w => w.Feature.Id == id).OrderByDescending(w => w.Area).FirstOrDefault())
                : water.GroupBy(w => w.Feature.Id).Select(g => g.OrderByDescending(w => w.Area).First()).OrderByDescending(w => w.Area).Take(2))
            .Where(w => w is not null).Select(w => w!).Take(2).ToArray();
        for (var li = 0; li < chosen.Length; li++)
        {
            var lake = chosen[li];
            var count = li == 0 ? S.SwansFirstLake : S.SwansSecondLake;
            // One normal water level per body: ITerrain.WaterY, else the water module's rule (bank - 0.5 m + 0.004 m).
            var centre = Centroid(lake.Ring);
            var y = world.Terrain.WaterY(lake.Feature.Id) ?? world.Spatial.GroundY(centre.X, centre.Y) - 0.5f + 0.004f;
            var rng = new Rng(StableHash.Hash32(seed, lake.Feature.Id.Value, "fauna.swans"));
            bool Open(Vector2 p, float margin) => Inside(lake, p) && DistanceToRing(lake.Ring, p) >= margin && !buildings.Any(b => Inside(b, p));
            var day = new List<FaunaInstance>();
            for (var attempt = 0; attempt < 400 && day.Count < count; attempt++)
            {
                var p = new Vector2(lake.Min.X + rng.NextFloat() * (lake.Max.X - lake.Min.X), lake.Min.Y + rng.NextFloat() * (lake.Max.Y - lake.Min.Y));
                var heading = rng.NextFloat() * 2f * float.Pi;
                if (!Open(p, S.SwanShoreMargin) || day.Any(d => Vector2.Distance(p, new(d.Position.X, d.Position.Z)) < S.SwanPairGap * 4f)) continue;
                // A pair: the second bird a little behind and beside the first, on nearly the same heading.
                var dir = new Vector2(MathF.Sin(heading), MathF.Cos(heading));
                var mate = p - dir * S.SwanPairGap + new Vector2(dir.Y, -dir.X) * 0.9f;
                day.Add(new(FaunaModelKey.SwanOf(SwanPose.Float), new(p.X, y, p.Y), heading, 1f));
                if (day.Count < count && Open(mate, S.SwanShoreMargin * 0.6f))
                    day.Add(new(FaunaModelKey.SwanOf(SwanPose.Float), new(mate.X, y, mate.Y), heading + 0.25f, 1f));
            }
            // Night and heavy rain: tucked in together near the shore point closest to a building (the swan house on the
            // first lake, a bank on the second), asleep.
            var anchor = lake.Ring.OrderBy(v => buildings.Length == 0 ? 0 : buildings.Min(b => DistanceToRing(b.Ring, v))).ThenBy(v => v.X).First();
            var inward = OrganicMeshExt.Unit2(centre - anchor, Vector2.UnitY);
            var huddle = anchor + inward * S.SwanHuddleOut;
            for (var step = 0; step < 20 && !Open(huddle, 2.5f); step++) huddle += inward * 1.5f;
            var night = new List<FaunaInstance>();
            for (var i = 0; i < day.Count; i++)
            {
                var angle = i * 2.4f;
                var p = huddle + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * (i == 0 ? 0f : 1.3f + 0.25f * i);
                night.Add(new(FaunaModelKey.SwanOf(SwanPose.Sleep), new(p.X, y, p.Y), angle * 1.7f, 1f));
            }
            var key = $"swans.lake-{li + 1}";
            sites.Add(Site(key, FaunaKind.Swan, day, night, []));
            log.Add($"{key}: {day.Count} black swans on '{lake.Feature.Name ?? lake.Feature.Id.Value}' at water y {y:F2}");
        }
        if (chosen.Length == 0) log.Add("swans: no lake; left out");
    }

    // ------------------------------------------------------------ dusk crows

    private static void Crows(IWorldModel world, uint seed, Footprint[] buildings, List<FaunaSite> sites, List<string> log)
    {
        // Dorm roofs with a known height (WP-14 draws their parapet coping at pad + height + about 0.95 m).
        var roofs = Ranked(buildings.Where(b => b.Category == S.Residence && b.Feature.Height is >= 9f), seed, "fauna.crow").ToArray();
        var groups = new List<Vector2>();
        foreach (var b in roofs)
        {
            if (groups.Count >= S.CrowGroups) break;
            var ring = b.Ring;
            var (a, c) = Enumerable.Range(0, ring.Count).Select(i => (ring[i], ring[(i + 1) % ring.Count]))
                .OrderByDescending(e => Vector2.Distance(e.Item1, e.Item2)).First();
            var length = Vector2.Distance(a, c);
            if (length < 6f) continue;
            var mid = (a + c) * 0.5f;
            if (groups.Any(g => Vector2.Distance(g, mid) < S.CrowGroupSpacing)) continue;
            var along = (c - a) / length;
            var outward = new Vector2(along.Y, -along.X);
            if (Inside(b, mid + outward * 0.3f)) outward = -outward;
            var y = world.Terrain.PadY(b.Feature.Id) + b.Feature.Height!.Value + S.CrowCopingRise;
            var rng = new Rng(StableHash.Hash32(seed, b.Feature.Id.Value, "fauna.crow.group"));
            var n = 2 + (rng.NextFloat() < 0.5f ? 1 : 0);
            var crows = new List<FaunaInstance>();
            var at = (rng.NextFloat() - 0.5f) * length * 0.4f;
            for (var i = 0; i < n; i++)
            {
                var p = mid + along * at;
                var heading = Rotate(outward, (rng.NextFloat() - 0.5f) * 1.6f);
                crows.Add(new(FaunaModelKey.CrowOf(), new(p.X, y, p.Y), Yaw(heading), S.Scale));
                at += 0.8f + rng.NextFloat() * 0.9f;
            }
            groups.Add(mid);
            var key = $"crows.roof-{groups.Count}";
            sites.Add(Site(key, FaunaKind.Crow, crows, [], []));
            log.Add($"{key}: {n} crows on '{b.Feature.Name ?? b.Feature.Id.Value}' parapet at y {y:F1}");
        }
    }

    // ------------------------------------------------------------ geometry helpers

    private static FaunaSite Site(string key, FaunaKind kind, IReadOnlyList<FaunaInstance> day, IReadOnlyList<FaunaInstance> night, IReadOnlyList<FaunaInstance> rain)
    {
        var all = day.Concat(night).Concat(rain).ToArray();
        var min = new Vector3(float.MaxValue);
        var max = new Vector3(float.MinValue);
        foreach (var a in all)
        {
            min = Vector3.Min(min, a.Position - new Vector3(1.2f, 0.2f, 1.2f));
            max = Vector3.Max(max, a.Position + new Vector3(1.2f, 1.0f, 1.2f));
        }
        if (all.Length == 0) { min = Vector3.Zero; max = Vector3.Zero; }
        return new(key, kind, day.ToArray(), night.ToArray(), rain.ToArray(), new(min, max));
    }

    private static IEnumerable<Footprint> Ranked(IEnumerable<Footprint> items, uint seed, string purpose) =>
        items.OrderBy(b => StableHash.Hash32(seed, b.Feature.Id.Value, purpose)).ThenBy(b => b.Feature.Id.Value, StringComparer.Ordinal);

    private static IEnumerable<Footprint> Footprints(IWorldModel world, Func<WorldFeature, bool> filter)
    {
        foreach (var f in world.Features.Values.OrderBy(f => f.Id.Value, StringComparer.Ordinal))
        {
            if (!filter(f)) continue;
            var rings = (f.Parts.Count > 0 ? f.Parts.Select(p => p.Geometry) : [f.Geometry]).SelectMany(Polygons).ToArray();
            if (rings.Length == 0) continue;
            // The largest polygon stands for the feature (doors face its walls); the rest still count as occupied.
            foreach (var ring in rings.OrderByDescending(r => MathF.Abs(SignedArea(r.Outer))))
            {
                if (ring.Outer.Count < 3) continue;
                var min = new Vector2(ring.Outer.Min(p => p.X), ring.Outer.Min(p => p.Y));
                var max = new Vector2(ring.Outer.Max(p => p.X), ring.Outer.Max(p => p.Y));
                yield return new(f, ring.Outer, min, max, MathF.Abs(SignedArea(ring.Outer)), Category(f));
            }
        }
    }

    private static string Category(WorldFeature f) =>
        f.Props is { ValueKind: JsonValueKind.Object } props && props.TryGetProperty("category", out var c) && c.ValueKind == JsonValueKind.String
            ? c.GetString() ?? "" : "";

    private static float? Prop(WorldFeature f, string name) =>
        f.Props is { ValueKind: JsonValueKind.Object } props && props.TryGetProperty("props", out var inner) && inner.ValueKind == JsonValueKind.Object
            && inner.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetSingle()
        : f.Props is { ValueKind: JsonValueKind.Object } direct && direct.TryGetProperty(name, out var d) && d.ValueKind == JsonValueKind.Number ? d.GetSingle()
        : null;

    private static IEnumerable<PolygonRings> Polygons(OverlayGeometry g) => g switch
    {
        PolygonGeometry p => [p.Rings],
        MultiPolygonGeometry m => m.Polygons,
        _ => [],
    };

    private static bool Inside(Footprint f, Vector2 p) =>
        p.X >= f.Min.X && p.X <= f.Max.X && p.Y >= f.Min.Y && p.Y <= f.Max.Y && Crossings(f.Ring, p);

    private static bool Crossings(IReadOnlyList<Vector2> ring, Vector2 p)
    {
        var inside = false;
        for (int i = 0, j = ring.Count - 1; i < ring.Count; j = i++)
        {
            var a = ring[i];
            var b = ring[j];
            if ((a.Y > p.Y) != (b.Y > p.Y) && p.X < (b.X - a.X) * (p.Y - a.Y) / (b.Y - a.Y) + a.X) inside = !inside;
        }
        return inside;
    }

    private static float SignedArea(IReadOnlyList<Vector2> ring)
    {
        double sum = 0;
        for (int i = 0, j = ring.Count - 1; i < ring.Count; j = i++) sum += (double)ring[j].X * ring[i].Y - (double)ring[i].X * ring[j].Y;
        return (float)(sum * 0.5);
    }

    private static Vector2 Centroid(IReadOnlyList<Vector2> ring)
    {
        var sum = Vector2.Zero;
        foreach (var p in ring) sum += p;
        return sum / ring.Count;
    }

    private static float DistanceToLine(IReadOnlyList<Vector2> line, Vector2 p)
    {
        var best = float.MaxValue;
        for (var i = 0; i + 1 < line.Count; i++) best = MathF.Min(best, Segment(line[i], line[i + 1], p));
        return best;
    }

    private static float DistanceToRing(IReadOnlyList<Vector2> ring, Vector2 p)
    {
        var best = float.MaxValue;
        for (var i = 0; i < ring.Count; i++) best = MathF.Min(best, Segment(ring[i], ring[(i + 1) % ring.Count], p));
        return best;
    }

    private static float Segment(Vector2 a, Vector2 b, Vector2 p)
    {
        var ab = b - a;
        var len = ab.LengthSquared();
        var t = len <= 1e-12f ? 0 : Math.Clamp(Vector2.Dot(p - a, ab) / len, 0, 1);
        return Vector2.Distance(p, a + ab * t);
    }

    private static (Vector2 Point, Vector2 Dir) AlongLine(IReadOnlyList<Vector2> line, float fraction)
    {
        var total = 0f;
        for (var i = 0; i + 1 < line.Count; i++) total += Vector2.Distance(line[i], line[i + 1]);
        var target = total * fraction;
        for (var i = 0; i + 1 < line.Count; i++)
        {
            var seg = Vector2.Distance(line[i], line[i + 1]);
            if (target <= seg || i + 2 == line.Count)
            {
                var dir = OrganicMeshExt.Unit2(line[i + 1] - line[i], Vector2.UnitX);
                return (line[i] + dir * MathF.Min(target, seg), dir);
            }
            target -= seg;
        }
        return (line[0], Vector2.UnitX);
    }

    /// <summary>Heading in the plan (x, z) to the model yaw: model +Z turned by yaw about +Y points along (sin yaw, cos yaw).</summary>
    private static float Yaw(Vector2 heading) => MathF.Atan2(heading.X, heading.Y);

    private static Vector2 Rotate(Vector2 v, float radians)
    {
        var c = MathF.Cos(radians);
        var s = MathF.Sin(radians);
        return new(v.X * c - v.Y * s, v.X * s + v.Y * c);
    }
}

internal static class OrganicMeshExt
{
    internal static Vector2 Unit2(Vector2 v, Vector2 fallback)
    {
        var l = v.Length();
        return l > 1e-6f ? v / l : fallback;
    }
}
