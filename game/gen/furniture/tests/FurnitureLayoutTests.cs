using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Maps;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Street;
using Campus.Gen.World;
using GdUnit4;

namespace Campus.Gen.Furniture.Tests;

[TestSuite]
public sealed class FurnitureLayoutTests
{
    private static readonly Lazy<IWorldModel> World = new(Load);

    private static IWorldModel Load()
    {
        var data = Path.Combine(FindRepository(), "game", "data", "maps", "qingshuihe");
        var files = new[] { "campus.json", "network.json", "interiors.json", "map.json" }
            .ToDictionary(name => name, name => File.ReadAllText(Path.Combine(data, name)), StringComparer.Ordinal);
        foreach (var overlay in Directory.GetFiles(Path.Combine(data, "overlay"), "*.overlay.json").Order(StringComparer.Ordinal))
            files["overlay/" + Path.GetFileName(overlay)] = File.ReadAllText(overlay);
        return WorldModelLoader.Load(new("qingshuihe"), files).World;
    }

    [TestCase]
    public void TheMainRoadAndTwoSecondaryRoadsAreFurnishedDeterministically()
    {
        var a = FurniturePlanner.Build(World.Value);
        var b = FurniturePlanner.Build(World.Value);
        var lamps = a.Items.Where(i => i.Kind == FurnitureKind.StreetLamp).ToList();
        var disc = lamps.Count(i => ((LampParams)i.Part.Params).Variation == 0);
        var arm = lamps.Count(i => ((LampParams)i.Part.Params).Variation == 1);
        Console.WriteLine("furniture: " + string.Join(", ", Enum.GetValues<FurnitureKind>().Select(k => $"{k} {a.Count(k)}")) + $"; disc {disc}, arm {arm}");
        Check(a.Items.Select(i => (i.Key, i.Part.At, i.Part.Params)).SequenceEqual(b.Items.Select(i => (i.Key, i.Part.At, i.Part.Params))), "two builds differ");
        Check(a.Items.Select(i => i.Key).Distinct().Count() == a.Items.Count, "duplicate keys");
        // The avenue is about 1.43 km: 52 slots a side at 27.5 m, less junctions and crossings; the two secondary roads 0.69 and 0.62 km at 35 m.
        Check(disc is >= 70 and <= 104, $"{disc} avenue lamps");
        Check(arm is >= 20 and <= 36, $"{arm} secondary lamps");
        // Rest stops only where a walk takes a bench (Opus review 12:15): the avenue's north half and Shuisha Road's walk.
        Check(a.Count(FurnitureKind.Bench) >= 8, $"{a.Count(FurnitureKind.Bench)} benches");
        Check(a.Count(FurnitureKind.Bin) * 2 >= a.Count(FurnitureKind.Bench) - 2 && a.Count(FurnitureKind.Bin) <= a.Count(FurnitureKind.Bench), "a bin at every other rest stop");
        // SYS-ATLAS §6: six kiosks and seven posts, less A01 (on a carriageway) and A05 (in water) until their gate plazas land.
        Check(a.Count(FurnitureKind.AtlasKiosk) == 4 && a.Count(FurnitureKind.AtlasPost) == 7, "four kiosks and seven posts");
        Check(a.Count(FurnitureKind.BusStop) == 2, $"{a.Count(FurnitureKind.BusStop)} shuttle stops");
        var open = World.Value.Interiors.EntrancesNear(Vector3.Zero, 1e5f).Where(e => e.Open).Select(e => e.Id).Distinct().Count();
        Check(new[] { FurnitureKind.EntranceMat, FurnitureKind.OpenSign, FurnitureKind.EntranceLamp }.All(k => a.Count(k) == open), $"one kit at each of {open} open entrances");
    }

    [TestCase]
    public void BenchesLineAWalkAndBinsStandApart()
    {
        // Opus review 12:15: a bench lines a walk and faces it, its bay joined to the walk, a passage of 1.2 m left in front; it
        // never faces a carriageway or sits in the strip between the kerb and the tree rows. Its bin stands 2–4 m away on its own bay.
        var world = World.Value;
        var layout = FurniturePlanner.Build(world);
        var walks = world.Network.Roads.Where(r => Highway(r) is "footway" or "path" or "pedestrian").ToList();
        var lanes = world.Network.Roads.Where(Carriageway).ToList();
        var benches = layout.Items.Where(i => i.Kind == FurnitureKind.Bench).ToList();
        foreach (var item in benches.Concat(layout.Items.Where(i => i.Kind == FurnitureKind.Bin)))
        {
            var p = (FurnitureParams)item.Part.Params;
            var facing = Front(item.Part.At);
            var front = item.Anchor + facing * (p.Depth * 0.5f);
            var walk = walks.MinBy(w => Distance(front, w));
            var edge = Distance(front, walk!) - Half(walk!);
            Check(edge is >= -0.02f and <= FurnitureStyle.WalkGap + 0.1f, $"{item.Key}: front {edge:F2} m off its walk's edge");
            var toWalk = Vector2.Normalize(Nearest(front, walk!) - item.Anchor);
            Check(Vector2.Dot(toWalk, facing) > 0.97f, $"{item.Key} does not face its walk");
            Check(2f * Half(walk!) - FurnitureStyle.FeetReach >= FurnitureStyle.PassClear, $"{item.Key}: walk too narrow to pass");
            var pad = item.Pad ?? throw new InvalidOperationException($"{item.Key} has no bay");
            var padFront = new Vector2(pad.Frame.M41, pad.Frame.M43) + facing * pad.HalfDepth;
            Check(Distance(padFront, walk!) < Half(walk!), $"{item.Key}: its bay does not reach the walk");
            Check(MathF.Abs(pad.Top - item.Part.At.M42) < 1e-4f, $"{item.Key} is off its bay");
            var radius = p.Depth * 0.5f;
            foreach (var lane in lanes)
                Check(Distance(item.Anchor, lane) - Half(lane) - radius >= FurnitureStyle.RoadSetback - 0.6f, $"{item.Key} stands in the kerbside strip of {lane.Id.Value}");
            if (item.Kind != FurnitureKind.Bench) continue;
            for (var d = 0f; d <= FurnitureStyle.FaceClear; d += 0.5f)
                Check(lanes.All(l => Distance(front + facing * d, l) >= Half(l)), $"{item.Key} faces a carriageway {d:F1} m ahead");
        }
        foreach (var bin in layout.Items.Where(i => i.Kind == FurnitureKind.Bin))
        {
            var bench = benches.Single(i => i.Key == bin.Key.Replace(":bin", ":bench", StringComparison.Ordinal));
            var gap = Vector2.Distance(bin.Anchor, bench.Anchor) - (FurnitureStyle.Bench.Width + FurnitureStyle.Bin.Width) * 0.5f;
            Check(gap is >= FurnitureStyle.BinGapMin - 0.15f and <= FurnitureStyle.BinGapMax + 0.15f, $"{bin.Key} stands {gap:F2} m from its bench");
            Check(!ReferenceEquals(bin.Pad, bench.Pad), $"{bin.Key} shares the bench's bay");
        }
    }

    [TestCase]
    public void AnchorsOnACarriagewayInWaterOrInABuildingAreLeftOutAndLogged()
    {
        // Opus 12:38: until the gate plazas land (18c3, WP-30) A01 lands on a carriageway and A05 in water; neither gets a kiosk.
        var world = World.Value;
        var layout = FurniturePlanner.Build(world);
        Console.WriteLine("furniture log: " + string.Join(" | ", layout.Log));
        foreach (var (id, where) in new[] { ("A01", "on a carriageway"), ("A05", "in water") })
        {
            Check(!layout.Items.Any(i => i.Key == "anchor:design:anchor:" + id), $"{id} placed");
            Check(layout.Log.Count(l => l == $"anchor design:anchor:{id} (kiosk) not placed: its point lands {where}") == 1, $"{id} not logged as {where}");
        }
        // A building: the same anchor moved onto a building's centre is left out too, and nothing else changes.
        var building = world.Features.Values.Where(f => f.Kind.Value == "building" && f.Geometry is PolygonGeometry).OrderBy(f => f.Id.Value, StringComparer.Ordinal)
            .Select(f => (Feature: f, Centre: PlanGeometry.Centroid(((PolygonGeometry)f.Geometry).Rings.Outer)))
            .First(b => PlanGeometry.Contains(((PolygonGeometry)b.Feature.Geometry).Rings, b.Centre) && PlanGeometry.DistanceToRing(b.Centre, ((PolygonGeometry)b.Feature.Geometry).Rings.Outer) > 3f);
        var a02 = world.Features[new("design:anchor:A02")];
        var ring = ((PolygonGeometry)a02.Geometry).Rings.Outer;
        var shift = building.Centre - ring.Aggregate(Vector2.Zero, (a, p) => a + p) / ring.Count;
        var moved = a02 with { Geometry = new PolygonGeometry(new PolygonRings([.. ring.Select(p => p + shift)], [])) };
        var indoors = FurniturePlanner.Build(new FeatureWorld(world, moved));
        Check(!indoors.Items.Any(i => i.Key == "anchor:design:anchor:A02"), "a kiosk stands in a building");
        Check(indoors.Log.Contains("anchor design:anchor:A02 (kiosk) not placed: its point lands in a building"), "the building fallback is not logged");
        Check(indoors.Items.Count == layout.Items.Count - 1, "the building fallback changed other furniture");
    }

    [TestCase]
    public void AnchorsStandOnOrBesidePaving()
    {
        // Opus review 12:15: a kiosk's apron or a post's foot touches pedestrian paving (≤ 0.5 m off it); the facing never changes.
        var world = World.Value;
        var layout = FurniturePlanner.Build(world);
        var lines = world.Network.Roads.Where(r => Highway(r) is "footway" or "path" or "pedestrian" or "steps").ToList();
        var areas = new List<PolygonRings>();
        foreach (var feature in world.Features.Values.Where(f => f.Kind.Value == "road"))
        {
            var sources = feature.Parts.Count == 0 ? [(feature.Props, feature.Geometry)] : feature.Parts.Select(p => (p.Props ?? feature.Props, p.Geometry)).ToList();
            foreach (var (props, geometry) in sources)
                if (RoadType(props) is "footway" or "path" or "pedestrian" or "steps")
                    areas.AddRange(geometry switch { PolygonGeometry p => [p.Rings], MultiPolygonGeometry m => m.Polygons, _ => [] });
        }
        float Gap(Vector2 q) => MathF.Max(0f, MathF.Min(
            lines.Select(l => Distance(q, l) - Half(l)).DefaultIfEmpty(float.MaxValue).Min(),
            areas.Select(a => PlanGeometry.Contains(a, q) ? 0f : a.Holes.Append(a.Outer).Min(r => PlanGeometry.DistanceToRing(q, r))).DefaultIfEmpty(float.MaxValue).Min()));
        foreach (var item in layout.Items.Where(i => i.Kind is FurnitureKind.AtlasKiosk or FurnitureKind.AtlasPost))
        {
            var feature = world.Features[new(item.Key["anchor:".Length..])];
            Check(Vector2.Dot(Front(item.Part.At), FurniturePlanner.Facing(feature.Props!.Value.GetProperty("yawDeg").GetSingle())) > 0.999f, $"{item.Key} turned");
            if (feature.Props!.Value.TryGetProperty("groundY", out var deck) && deck.GetSingle() > 0.5f) continue;
            var ring = ((PolygonGeometry)feature.Geometry).Rings.Outer;
            var centre = ring.Aggregate(Vector2.Zero, (a, p) => a + p) / ring.Count;
            if (Vector2.Distance(centre, item.Anchor) < 1e-3f && Gap(centre) > FurnitureStyle.AnchorPavingReach)
            {
                Console.WriteLine($"{item.Key}: no pedestrian paving within {FurnitureStyle.AnchorPavingReach} m, left for the anchor data");
                continue;
            }
            var hw = item.Pad is { } apron ? apron.HalfWidth : ((SignParams)item.Part.Params).Width * 0.5f;
            var hd = item.Pad is { } pad ? pad.HalfDepth : ((SignParams)item.Part.Params).Depth * 0.5f;
            var gap = float.MaxValue;
            for (var i = -1; i <= 1; i++)
                for (var k = -1; k <= 1; k++)
                    if (i != 0 || k != 0)
                    {
                        var q = Xf.Point(item.Part.At, new(i * hw, 0, k * hd));
                        gap = MathF.Min(gap, Gap(new(q.X, q.Z)));
                    }
            Console.WriteLine($"{item.Key}: footprint {gap:F2} m off paving");
            Check(gap <= FurnitureStyle.AnchorPavingGap + 0.1f, $"{item.Key}: footprint {gap:F2} m off pedestrian paving");
        }
    }

    [TestCase]
    public void AvenueLampsKeepTheirRhythm()
    {
        // MAP-QSH-ADDITIONS §8.5: the avenue's lamps keep a regular 25–30 m; only junctions may nudge one by a step or two.
        foreach (var side in new[] { ":l:", ":r:" })
        {
            var row = FurniturePlanner.Build(World.Value).Items.Where(i => i.Kind == FurnitureKind.StreetLamp && i.Key.StartsWith("lamp:0:0" + side, StringComparison.Ordinal)).ToList();
            var gaps = row.Zip(row.Skip(1), (p, q) => Vector2.Distance(p.Anchor, q.Anchor)).ToList();
            Check(gaps.Count > 30, $"{gaps.Count} gaps on side {side}");
            var regular = gaps.Count(g => g is >= 24f and <= 31f);
            Check(regular >= gaps.Count * 0.75f, $"side {side}: only {regular} of {gaps.Count} gaps are 25–30 m");
        }
    }

    [TestCase]
    public void RoadFurnitureKeepsOffRoadsBuildingsAndWater()
    {
        var world = World.Value;
        var outlines = world.Features.Values.Where(f => f.Kind.Value is "building" or "water").SelectMany(Polygons).ToList();
        foreach (var item in FurniturePlanner.Build(world).Items.Where(i => i.Kind is FurnitureKind.StreetLamp or FurnitureKind.Bench or FurnitureKind.Bin or FurnitureKind.BusStop))
        {
            var p = item.Anchor;
            Check(PlanGeometry.Contains(world.Boundary, p), $"{item.Key} outside the campus");
            Check(!outlines.Any(o => PlanGeometry.Contains(o, p)), $"{item.Key} inside a building or water");
            foreach (var road in world.Network.Roads)
            {
                var d = MathF.Sqrt(road.Line.Points.Zip(road.Line.Points.Skip(1), (u, v) => PlanGeometry.SegmentDistanceSquared(p, u, v)).DefaultIfEmpty(float.MaxValue).Min());
                Check(d > MathF.Max(road.Width, 2f) * 0.5f + 0.2f, $"{item.Key} stands on {road.Id.Value} ({d:F2} m from its centre line)");
            }
        }
    }

    [TestCase]
    public void FurnitureLandsOnTheTerrain()
    {
        // 8% everywhere: poles sink 0.05–0.3 m below the lawn level at their centre; benches and bins share a concrete plinth.
        var terrain = new Tilted(0.08f);
        var gentle = FurniturePlanner.Build(new TerrainWorld(World.Value, terrain));
        foreach (var lamp in gentle.Items.Where(i => i.Kind == FurnitureKind.StreetLamp))
        {
            var burial = terrain.HeightAt(lamp.Part.At.M41, lamp.Part.At.M43) + FurnitureStyle.LawnLift - lamp.Part.At.M42;
            Check(burial is >= 0.049f and <= 0.301f, $"{lamp.Key} buried {burial:F3} m");
        }
        var benches = gentle.Items.Where(i => i.Kind == FurnitureKind.Bench).ToList();
        Check(benches.Count > 10, "no benches on 8%");
        foreach (var bench in benches)
        {
            Check(bench.Pad is { Plinth: true } pad && MathF.Abs(pad.Top - bench.Part.At.M42) < 1e-4f, $"{bench.Key} has no plinth on 8%");
            var plinth = bench.Pad!;
            var high = Corners(bench).Max(c => terrain.HeightAt(c.X, c.Y));
            var low = Corners(bench).Min(c => terrain.HeightAt(c.X, c.Y));
            Check(plinth.Top >= high + FurnitureStyle.PavingLift - 1e-3f && plinth.Bottom <= low - FurnitureStyle.PadSkirt + 1e-3f, $"{bench.Key}: plinth does not cover the slope");
            Check(bench.Colliders.OfType<BoxCollider>().Any(c => c.Foot == FootMat.Concrete), $"{bench.Key}: plinth has no collider");
            Check(plinth.Geometry().Meshes.Sum(m => m.Index.Length) > 0, $"{bench.Key}: empty plinth mesh");
            var bin = gentle.Items.SingleOrDefault(i => i.Key == bench.Key.Replace(":bench", ":bin", StringComparison.Ordinal));
            Check(bin is null || bin.Pad is { Plinth: true } own && MathF.Abs(bin.Part.At.M42 - own.Top) < 1e-4f, $"{bench.Key}: the bin is off its own plinth");
        }
        // Flat: rest stops stand on paved bays just proud of the paving; the kiosk on the A06 deck stays on its deck.
        var flat = FurniturePlanner.Build(World.Value);
        var top = FurnitureStyle.PavingLift + FurnitureStyle.PadProud;
        Check(flat.Items.Where(i => i.Pad is not null).All(i => !i.Pad!.Plinth), "plinths on flat ground");
        Check(flat.Items.Where(i => i.Kind is FurnitureKind.Bench or FurnitureKind.Bin).All(i => MathF.Abs(i.Part.At.M42 - top) < 1e-5f), "benches off their bays");
        var kiosks = flat.Items.Where(i => i.Kind == FurnitureKind.AtlasKiosk).ToList();
        Check(kiosks.Count(k => MathF.Abs(k.Part.At.M42 - 5f - top) < 1e-3f) == 1 && kiosks.Count(k => MathF.Abs(k.Part.At.M42 - top) < 1e-5f) == kiosks.Count - 1,
            "kiosks: one on the A06 deck, the others on their pads");
        // 30%: no pole stands.
        var steep = FurniturePlanner.Build(new TerrainWorld(World.Value, new Tilted(0.3f)));
        Check(steep.Count(FurnitureKind.StreetLamp) == 0 && steep.Count(FurnitureKind.AtlasPost) == 0, "poles on a 30% grade");
    }

    [TestCase]
    public void CollidersFollowThePartsWithTheirFootMaterial()
    {
        foreach (var item in FurniturePlanner.Build(World.Value).Items)
        {
            Check(item.Colliders.All(c => c.Foot != FootMat.Auto), $"{item.Key} leaves a collider on Auto (G21i)");
            foreach (var collider in item.Colliders)
            {
                var at = collider switch { PostCollider post => post.C, BoxCollider box => new Vector2(box.At.M41, box.At.M43), _ => throw new InvalidOperationException() };
                Check(Vector2.Distance(at, item.Anchor) < 2f, $"{item.Key}: a collider strayed {Vector2.Distance(at, item.Anchor):F2} m");
            }
            var expected = item.Kind switch
            {
                FurnitureKind.StreetLamp or FurnitureKind.AtlasPost => 1,
                FurnitureKind.BusStop => 2,
                FurnitureKind.Bench => 3,
                FurnitureKind.AtlasKiosk => 4,
                FurnitureKind.Bin => 2,
                FurnitureKind.OpenSign => 1,
                FurnitureKind.EntranceMat or FurnitureKind.EntranceLamp => 0,
                _ => throw new InvalidOperationException(),
            };
            Check(item.Colliders.Count == expected, $"{item.Key}: {item.Colliders.Count} colliders");
        }
    }

    [TestCase]
    public void AnchorsAndEntrancesFaceTheirData()
    {
        var world = World.Value;
        var layout = FurniturePlanner.Build(world);
        foreach (var feature in world.Features.Values.Where(f => f.Kind.Value == "anchor"))
        {
            if (layout.Items.SingleOrDefault(i => i.Key == "anchor:" + feature.Id.Value) is not { } item)
            {
                Check(layout.Log.Any(l => l.StartsWith($"anchor {feature.Id.Value} ", StringComparison.Ordinal) && l.Contains("not placed", StringComparison.Ordinal)), $"{feature.Id.Value} missing without a log line");
                continue;
            }
            var ring = ((PolygonGeometry)feature.Geometry).Rings.Outer;
            var centre = ring.Aggregate(Vector2.Zero, (a, p) => a + p) / ring.Count;
            // On its footprint, or moved to the paving edge nearby (AnchorsStandOnOrBesidePaving).
            Check(Vector2.Distance(centre, item.Anchor) < FurnitureStyle.AnchorPavingReach + 0.1f, $"{item.Key} strayed {Vector2.Distance(centre, item.Anchor):F2} m from its footprint");
            var yaw = feature.Props!.Value.GetProperty("yawDeg").GetSingle();
            Check(Vector2.Dot(Front(item.Part.At), FurniturePlanner.Facing(yaw)) > 0.999f, $"{item.Key} does not face {yaw}°");
        }
        foreach (var door in world.Interiors.EntrancesNear(Vector3.Zero, 1e5f).Where(e => e.Open))
        {
            var outward = FurniturePlanner.Facing(door.YawDeg);
            var mat = layout.Items.Single(i => i.Key == $"door:{door.Id}:mat");
            var lamp = layout.Items.Single(i => i.Key == $"door:{door.Id}:lamp");
            Check(Vector2.Dot(mat.Anchor - new Vector2(door.Center.X, door.Center.Z), outward) > 0.4f, $"{door.Id}: mat not in front of the door");
            // On the entrance floor, never below the paving (flat terrain: 0.15 m).
            var floor = MathF.Max(door.Center.Y, FurnitureStyle.PavingLift);
            Check(MathF.Abs(mat.Part.At.M42 - floor) < 1e-4f && lamp.Part.At.M42 > floor + 2f, $"{door.Id}: kit off the entrance floor");
            Check(Vector2.Dot(Front(lamp.Part.At), outward) > 0.999f && lamp.Emitters.Single().Channel == StreetStyle.FirstGroup, $"{door.Id}: lamp");
        }
        // A building whose props another package claims keeps its own kit.
        var claimed = world.Interiors.EntrancesNear(Vector3.Zero, 1e5f).First(e => e.Open);
        var owner = world.Features.Keys.First(f => world.Interiors.Get(f)?.Entrances.Any(e => e.Id == claimed.Id) == true);
        var building = world.Interiors.Get(owner)!.Feature;
        var skipped = FurniturePlanner.Build(world, (f, a) => f == building && a == Aspects.Props ? new WpId("WP-18") : null);
        Check(!skipped.Items.Any(i => i.Key.StartsWith($"door:{claimed.Id}:", StringComparison.Ordinal)), "kit placed on a claimed building");
        Check(skipped.Count(FurnitureKind.EntranceMat) == layout.Count(FurnitureKind.EntranceMat) - world.Interiors.Get(owner)!.Entrances.Count(e => e.Open), "claim skipped other kits");
    }

    [TestCase]
    public void ModuleTilesCarryEveryItemOnceWithinBudget()
    {
        var rect = new MapRect(new(-1280f, -1280f), new(1280f, 1280f));
        var map = new MapDescriptor(new MapId("test"), new LocalizedName("测试", "Test"), MapKind.Outdoor, 1, 1u,
            new GeoFrame(0d, 0d, 32648, new(-200f, -200f), new(200f, 200f)), Vector2.Zero,
            new Box3(new(-1280f, -28f, -1280f), new(1280f, 2000f, 1280f)), rect,
            new RoamLimits(0.9f, 30f, 3000f, -28f, 2000f, 30f, 40f, 150f),
            new TileKey(-20, -20), new TileKey(19, 19), "P00", [],
            new MapWorldRules(MapTimeRule.Shared, new WorldPreset(17.5, Season.Autumn), new LocalizedName("落金初灯", "Golden Hour"), true, "chengdu"),
            new MapEnvironment("sky.json", "backdrop.json", []), new Dictionary<string, string>(), []);
        var ctx = new BuildContext { World = World.Value, Map = map, WorldSeed = 1u };
        var module = new FurnitureModule();
        var layout = FurnitureModule.Layout(ctx);
        var tiles = module.Tiles(ctx).ToList();
        var parts = 0; var emitters = 0; var colliders = 0;
        var heaviest = 0; var heaviestL1 = 0; var draws = 0;
        var triangles = new Dictionary<(string, object), (int L0, int L1, int Meshes)>();
        foreach (var tile in tiles)
        {
            var output = module.Build(ctx, tile, LodLevel.L0);
            parts += output.Parts.Count;
            emitters += output.Emitters?.Count ?? 0;
            colliders += module.Collide(ctx, tile).Count;
            var l0 = 0; var l1 = 0;
            foreach (var part in output.Parts)
            {
                var key = (part.Part.Value, part.Params);
                if (!triangles.TryGetValue(key, out var t)) triangles[key] = t = Measure(part);
                l0 += t.L0; l1 += t.L1;
            }
            l0 += output.Meshes.Sum(m => m.Index.Length / 3);
            heaviest = Math.Max(heaviest, l0);
            heaviestL1 = Math.Max(heaviestL1, l1);
            draws = Math.Max(draws, output.Parts.Select(p => (p.Part.Value, p.Params)).Distinct().Sum(k => triangles[k].Meshes) + output.Meshes.Count);
            Check(module.Build(ctx, tile, LodLevel.L1).Emitters is null, "emitters outside L0");
        }
        Check(parts == layout.Items.Count, $"{parts} placed parts for {layout.Items.Count} items");
        Check(emitters == layout.Items.Sum(i => i.Emitters.Count), "emitters lost or repeated");
        Check(colliders == layout.Items.Sum(i => i.Colliders.Count), "colliders lost or repeated");
        Check(heaviest <= module.Budget.TrianglesL0 && heaviestL1 <= module.Budget.TrianglesL1 && draws <= module.Budget.DrawCalls,
            $"heaviest cell L0 {heaviest}, L1 {heaviestL1}, draws {draws} over {module.Budget}");
        Console.WriteLine($"furniture module: {tiles.Count} cells, {parts} parts, {emitters} lamp markers, {colliders} colliders; heaviest cell L0 {heaviest} / L1 {heaviestL1} triangles, {draws} draws");
    }

    private static (int L0, int L1, int Meshes) Measure(PlacedPart part)
    {
        var type = typeof(StreetGallery).Assembly.GetTypes().Single(t => t.GetCustomAttributes(typeof(PartAttribute), false).OfType<PartAttribute>().Any(a => a.Id == part.Part.Value));
        var def = Activator.CreateInstance(type)!;
        var lods = (System.Collections.IDictionary)type.GetProperty("Lods")!.GetValue(def)!;
        PartGeometry? Level(LodLevel l) => lods.Contains(l) ? (PartGeometry)((Delegate)lods[l]!).DynamicInvoke(part.Params)! : null;
        var l0 = Level(LodLevel.L0)!;
        return (l0.Meshes.Sum(m => m.Index.Length / 3), Level(LodLevel.L1)?.Meshes.Sum(m => m.Index.Length / 3) ?? 0, l0.Meshes.Count);
    }

    private static Vector2 Front(Matrix4x4 at) => Vector2.Normalize(new(-at.M31, -at.M33));

    private static string? Highway(NetworkLine line) => RoadType(line.Props);

    private static string? RoadType(JsonElement? props) =>
        props is { ValueKind: JsonValueKind.Object } p && (p.TryGetProperty("roadType", out var t) || p.TryGetProperty("highway", out t)) && t.ValueKind == JsonValueKind.String ? t.GetString() : null;

    private static bool Carriageway(NetworkLine line) => Highway(line) is "service" or "residential" or "tertiary" or "secondary" or "primary" or "unclassified" or "living_street";

    private static float Half(NetworkLine line) => MathF.Max(line.Width, 2f) * 0.5f;

    private static Vector2 Nearest(Vector2 p, NetworkLine line) =>
        line.Line.Points.Zip(line.Line.Points.Skip(1), (u, v) =>
        {
            var d = v - u;
            var t = d.LengthSquared() < 1e-8f ? 0f : Math.Clamp(Vector2.Dot(p - u, d) / d.LengthSquared(), 0f, 1f);
            return u + d * t;
        }).MinBy(q => Vector2.DistanceSquared(p, q));

    private static float Distance(Vector2 p, NetworkLine line) => Vector2.Distance(p, Nearest(p, line));

    private static IEnumerable<Vector2> Corners(FurnitureItem item)
    {
        var p = (FurnitureParams)item.Part.Params;
        for (var i = -1; i <= 1; i += 2)
            for (var k = -1; k <= 1; k += 2)
            {
                var q = Xf.Point(item.Part.At, new(i * p.Width * 0.5f, 0, k * p.Depth * 0.5f));
                yield return new(q.X, q.Z);
            }
    }

    private static IEnumerable<PolygonRings> Polygons(WorldFeature feature) =>
        (feature.Parts.Count == 0 ? [feature.Geometry] : feature.Parts.Where(p => (p.Props is { ValueKind: JsonValueKind.Object } props && props.TryGetProperty("kind", out var k) ? k.GetString() : feature.Kind.Value) == feature.Kind.Value).Select(p => p.Geometry))
        .SelectMany(g => g switch { PolygonGeometry p => [p.Rings], MultiPolygonGeometry m => m.Polygons, _ => Array.Empty<PolygonRings>() });

    /// <summary>A uniform tilt along x with a constant grade.</summary>
    private sealed class Tilted(float grade) : ITerrain
    {
        public bool IsFlat(TileKey tile) => false;
        public float HeightAt(float x, float z) => grade * x;
        public void FillTile(TileKey tile, int level, Span<short> cm) => throw new NotSupportedException();
        public float PadY(FeatureId feature) => 0f;
        public float? WaterY(FeatureId water) => null;
        public TerrainFacts FactsAt(float x, float z) => new(grade, TerrainClass.Slope, 0f, 0f);
    }

    /// <summary>The world with one feature replaced.</summary>
    private sealed class FeatureWorld(IWorldModel inner, WorldFeature feature) : IWorldModel
    {
        private readonly Dictionary<FeatureId, WorldFeature> _features = new(inner.Features) { [feature.Id] = feature };
        public ITerrain Terrain => inner.Terrain;
        public PolygonRings Boundary => inner.Boundary;
        public IReadOnlyDictionary<FeatureId, WorldFeature> Features => _features;
        public WorldNetwork Network => inner.Network;
        public ISpatialIndex Spatial => inner.Spatial;
        public IInteriorIndex Interiors => inner.Interiors;
        public IOutskirtsModel? Outskirts => inner.Outskirts;
        public IFidelityModel? Fidelity => inner.Fidelity;
    }

    private sealed class TerrainWorld(IWorldModel inner, ITerrain terrain) : IWorldModel
    {
        public ITerrain Terrain => terrain;
        public PolygonRings Boundary => inner.Boundary;
        public IReadOnlyDictionary<FeatureId, WorldFeature> Features => inner.Features;
        public WorldNetwork Network => inner.Network;
        public ISpatialIndex Spatial => inner.Spatial;
        public IInteriorIndex Interiors => inner.Interiors;
        public IOutskirtsModel? Outskirts => inner.Outskirts;
        public IFidelityModel? Fidelity => inner.Fidelity;
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static string FindRepository()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "ownership.json"))) return current.FullName;
            current = current.Parent;
        }
        throw new DirectoryNotFoundException("Repository root not found.");
    }
}
