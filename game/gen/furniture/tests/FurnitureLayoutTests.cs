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
        Check(a.Items.Select(i => (i.Key, i.Part.At, i.Part.Params)).SequenceEqual(b.Items.Select(i => (i.Key, i.Part.At, i.Part.Params))), "two builds differ");
        Check(a.Items.Select(i => i.Key).Distinct().Count() == a.Items.Count, "duplicate keys");
        var lamps = a.Items.Where(i => i.Kind == FurnitureKind.StreetLamp).ToList();
        var disc = lamps.Count(i => ((LampParams)i.Part.Params).Variation == 0);
        var arm = lamps.Count(i => ((LampParams)i.Part.Params).Variation == 1);
        // The avenue is about 1.43 km: 52 slots a side at 27.5 m, less junctions and crossings; the two secondary roads 0.69 and 0.62 km at 35 m.
        Check(disc is >= 70 and <= 104, $"{disc} avenue lamps");
        Check(arm is >= 20 and <= 36, $"{arm} secondary lamps");
        Check(a.Count(FurnitureKind.Bench) >= 20, $"{a.Count(FurnitureKind.Bench)} benches");
        Check(a.Count(FurnitureKind.Bin) * 2 >= a.Count(FurnitureKind.Bench) - 2 && a.Count(FurnitureKind.Bin) <= a.Count(FurnitureKind.Bench), "a bin at every other rest stop");
        Check(a.Count(FurnitureKind.AtlasKiosk) == 6 && a.Count(FurnitureKind.AtlasPost) == 7, "six kiosks and seven posts (SYS-ATLAS §6)");
        Check(a.Count(FurnitureKind.BusStop) == 2, $"{a.Count(FurnitureKind.BusStop)} shuttle stops");
        var open = World.Value.Interiors.EntrancesNear(Vector3.Zero, 1e5f).Where(e => e.Open).Select(e => e.Id).Distinct().Count();
        Check(new[] { FurnitureKind.EntranceMat, FurnitureKind.OpenSign, FurnitureKind.EntranceLamp }.All(k => a.Count(k) == open), $"one kit at each of {open} open entrances");
        Console.WriteLine("furniture: " + string.Join(", ", Enum.GetValues<FurnitureKind>().Select(k => $"{k} {a.Count(k)}")) + $"; disc {disc}, arm {arm}");
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
            Check(bin is null || MathF.Abs(bin.Part.At.M42 - plinth.Top) < 1e-4f, $"{bench.Key}: the bin is off the plinth");
        }
        // Flat: rest stops stand on paved bays just proud of the paving; the kiosk on the A06 deck stays on its deck.
        var flat = FurniturePlanner.Build(World.Value);
        var top = FurnitureStyle.PavingLift + FurnitureStyle.PadProud;
        Check(flat.Items.Where(i => i.Pad is not null).All(i => !i.Pad!.Plinth), "plinths on flat ground");
        Check(flat.Items.Where(i => i.Kind is FurnitureKind.Bench or FurnitureKind.Bin).All(i => MathF.Abs(i.Part.At.M42 - top) < 1e-5f), "benches off their bays");
        var kiosks = flat.Items.Where(i => i.Kind == FurnitureKind.AtlasKiosk).ToList();
        Check(kiosks.Count(k => MathF.Abs(k.Part.At.M42 - 5f - top) < 1e-3f) == 1 && kiosks.Count(k => MathF.Abs(k.Part.At.M42 - top) < 1e-5f) == 5, "kiosks: one on the A06 deck, five on their pads");
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
                FurnitureKind.Bin or FurnitureKind.OpenSign => 1,
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
            var item = layout.Items.Single(i => i.Key == "anchor:" + feature.Id.Value);
            var ring = ((PolygonGeometry)feature.Geometry).Rings.Outer;
            var centre = ring.Aggregate(Vector2.Zero, (a, p) => a + p) / ring.Count;
            Check(Vector2.Distance(centre, item.Anchor) < 0.05f, $"{item.Key} off its footprint");
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
