using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Maps;
using Campus.Contracts.Parts;
using Campus.Gen.Parts.Core;
using Campus.Gen.Parts.Street;
using Campus.Pipeline.Map;
using Campus.Gen.World;
using Campus.Gen.Tests.Support;
using GdUnit4;

namespace Campus.Gen.Furniture.Tests;

using static Campus.Gen.Furniture.Tests.FurnitureTestSupport;

[TestSuite]
public sealed class FurnitureModuleTests
{
    [TestCase]
    public void FurnitureLandsOnTheTerrain()
    {
        // 8% everywhere: poles sink 0.05–0.3 m below the lawn level at their centre; benches and bins share a concrete plinth.
        var terrain = Tilted(0.08f);
        var gentle = Build(new FakeWorld(World.Value) { Terrain = terrain });
        foreach (var lamp in gentle.Items.Where(i => i.Kind == FurnitureKind.StreetLamp))
        {
            var burial = terrain.HeightAt(lamp.Part.At.M41, lamp.Part.At.M43) + FurnitureStyle.LawnLift - lamp.Part.At.M42;
            Check(burial is >= 0.049f and <= 0.301f, $"{lamp.Key} buried {burial:F3} m");
        }
        var benches = gentle.Items.Where(i => i.Kind == FurnitureKind.Bench).ToList();
        Check(benches.Count > 10, "no benches on 8%");
        foreach (var bench in benches)
        {
            Check(bench.Pad is { Plinth: true } pad && MathF.Abs(pad.Top - bench.Part.At.M42) < 0.01f, $"{bench.Key} has no plinth on 8%");
            var plinth = bench.Pad!;
            var high = Corners(bench).Max(c => terrain.HeightAt(c.X, c.Y));
            var low = Corners(bench).Min(c => terrain.HeightAt(c.X, c.Y));
            Check(plinth.Top >= high + FurnitureStyle.PavingLift - 0.01f && plinth.Bottom <= low - FurnitureStyle.PadSkirt + 0.01f, $"{bench.Key}: plinth does not cover the slope");
            Check(bench.Colliders.OfType<BoxCollider>().Any(c => c.Foot == FootMat.Concrete), $"{bench.Key}: plinth has no collider");
            Check(plinth.Geometry().Meshes.Sum(m => m.Index.Length) > 0, $"{bench.Key}: empty plinth mesh");
            var bin = gentle.Items.SingleOrDefault(i => i.Key == bench.Key.Replace(":bench", ":bin", StringComparison.Ordinal));
            Check(bin is null || bin.Pad is { Plinth: true } own && MathF.Abs(bin.Part.At.M42 - own.Top) < 0.01f, $"{bench.Key}: the bin is off its own plinth");
        }
        // Flat: rest stops stand on paved bays just proud of the paving; the kiosk on the A06 deck stays on its deck.
        var flat = Build(World.Value);
        var top = FurnitureStyle.PavingLift + FurnitureStyle.PadProud;
        Check(flat.Items.Where(i => i.Pad is not null).All(i => !i.Pad!.Plinth), "plinths on flat ground");
        Check(flat.Items.Where(i => i.Kind is FurnitureKind.Bench or FurnitureKind.Bin).All(i => MathF.Abs(i.Part.At.M42 - (i.Pad is null ? FurnitureStyle.PavingLift : top)) < 0.01f),
            "benches off their bays (or, on a plaza, off its paving)");
        var kiosks = flat.Items.Where(i => i.Kind == FurnitureKind.AtlasKiosk).ToList();
        Check(kiosks.Count(k => MathF.Abs(k.Part.At.M42 - 5f - top) < 0.01f) == 1 && kiosks.Count(k => MathF.Abs(k.Part.At.M42 - top) < 0.01f) == kiosks.Count - 1,
            "kiosks: one on the A06 deck, the others on their pads");
        // 30%: no pole stands.
        var steep = Build(new FakeWorld(World.Value) { Terrain = Tilted(0.3f) });
        Check(steep.Count(FurnitureKind.StreetLamp) == 0 && steep.Count(FurnitureKind.AtlasPost) == 0, "poles on a 30% grade");
    }

    [TestCase]
    public void CollidersFollowThePartsWithTheirFootMaterial()
    {
        foreach (var item in Build(World.Value).Items)
        {
            Check(item.Colliders.All(c => c.Foot != FootMat.Auto), $"{item.Key} leaves a collider on Auto (G21i)");
            foreach (var collider in item.Colliders)
            {
                var at = collider switch
                {
                    PostCollider post => post.C,
                    BoxCollider box => new Vector2(box.At.M41, box.At.M43),
                    RampCollider ramp => new Vector2(ramp.P0.X + ramp.P2.X, ramp.P0.Z + ramp.P2.Z) * 0.5f,
                    _ => throw new InvalidOperationException(),
                };
                Check(Vector2.Distance(at, item.Anchor) < 2f, $"{item.Key}: a collider strayed {Vector2.Distance(at, item.Anchor):F2} m");
            }
            var expected = item.Kind switch
            {
                FurnitureKind.StreetLamp or FurnitureKind.AtlasPost => 1,
                FurnitureKind.BusStop => 2,
                // Seat and back, and its bay; a plaza bench stands on the paving itself.
                FurnitureKind.Bench => item.Pad is null ? 2 : 3,
                FurnitureKind.AtlasKiosk => 4,
                FurnitureKind.Bin => 2,
                FurnitureKind.OpenSign or FurnitureKind.ParkedCar or FurnitureKind.KerbRamp => 1,
                // Two posts and the boards, and the bay; the fountain's box and its bay.
                FurnitureKind.Signpost => item.Pad is null ? 3 : 4,
                FurnitureKind.DrinkingFountain => item.Pad is null ? 1 : 2,
                // A vending group's bay rides on its first machine; a locker's box and a bay's sign, with their bays.
                FurnitureKind.VendingMachine or FurnitureKind.DeliveryLocker or FurnitureKind.BicycleBay => item.Pad is null ? 1 : 2,
                // Parked bikes have no collision (WLD-LIFE).
                FurnitureKind.EntranceMat or FurnitureKind.EntranceLamp or FurnitureKind.ParkedBicycle => 0,
                _ => throw new InvalidOperationException(),
            };
            Check(item.Colliders.Count == expected, $"{item.Key}: {item.Colliders.Count} colliders");
        }
    }

    [TestCase]
    public void ModuleTilesCarryEveryItemOnceWithinBudget()
    {
        var ctx = Context(Inputs());
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
        Check(module.DescribeLights(ctx).SequenceEqual(layout.Items.SelectMany(i => i.Emitters)), "light metadata differs from the item layout");
        Check(colliders == layout.Items.Sum(i => i.Colliders.Count), "colliders lost or repeated");
        Check(heaviest <= module.Budget.TrianglesL0 && heaviestL1 <= module.Budget.TrianglesL1 && draws <= module.Budget.DrawCalls,
            $"heaviest cell L0 {heaviest}, L1 {heaviestL1}, draws {draws} over {module.Budget}");
        Console.WriteLine($"furniture module: {tiles.Count} cells, {parts} parts, {emitters} lamp markers, {colliders} colliders; heaviest cell L0 {heaviest} / L1 {heaviestL1} triangles, {draws} draws");
    }

    [TestCase]
    public void LayoutDiagnosticsTravelWithTheFirstCellOnly()
    {
        // Structured diagnostics (ContentOutput.Diagnostics) are reported once per layout: with the first cell's L0 output.
        var module = new FurnitureModule();
        var quiet = Context(Inputs());
        Check(module.Tiles(quiet).All(t => !module.Build(quiet, t, LodLevel.L0).Diagnostics.Any(d => d.Code == PlantingRows.MissingCode)), "warnings with the real planting input");
        var ctx = Context(new Dictionary<string, ReadOnlyMemory<byte>>());
        var tiles = module.Tiles(ctx).ToList();
        var warned = tiles.Where(t => module.Build(ctx, t, LodLevel.L0).Diagnostics.Count > 0).ToList();
        Check(warned.Count == 1 && warned[0] == tiles[0], $"{warned.Count} cells carry the warning");
        var diagnostics = module.Build(ctx, tiles[0], LodLevel.L0).Diagnostics;
        foreach (var diagnostic in diagnostics) diagnostic.Validate();
        Check(diagnostics.SequenceEqual(FurnitureModule.Layout(ctx).Diagnostics), "layout diagnostics changed in the module output");
        Check(diagnostics.Count(d => d.Code == PlantingRows.MissingCode && d.Level == DiagnosticLevel.Warning) == 1, "not one missing-input warning");
        Check(tiles.All(t => module.Build(ctx, t, LodLevel.L1).Diagnostics.Count == 0), "warnings outside L0");
    }

    private static BuildContext Context(IReadOnlyDictionary<string, ReadOnlyMemory<byte>> inputs)
    {
        var rect = new MapRect(new(-1280f, -1280f), new(1280f, 1280f));
        var map = new MapDescriptor(new MapId("test"), new LocalizedName("测试", "Test"), MapKind.Outdoor, 1, 1u,
            new GeoFrame(0d, 0d, 32648, new(-200f, -200f), new(200f, 200f)), Vector2.Zero,
            new Box3(new(-1280f, -28f, -1280f), new(1280f, 2000f, 1280f)), rect,
            new RoamLimits(0.9f, 30f, 3000f, -28f, 2000f, 30f, 40f, 150f),
            new TileKey(-20, -20), new TileKey(19, 19), "P00", [],
            new MapWorldRules(MapTimeRule.Shared, new WorldPreset(17.5, Season.Autumn), new LocalizedName("落金初灯", "Golden Hour"), true, "chengdu"),
            new MapEnvironment("sky.json", "backdrop.json", []), new Dictionary<string, string>(), []);
        return new BuildContext { Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog, World = World.Value, Map = map, WorldSeed = 1u, SourceInputs = inputs };
    }

    private static (int L0, int L1, int Meshes) Measure(PlacedPart part)
    {
        var type = RegistrationDiscovery.Discover([typeof(StreetGallery).Assembly])
            .Single(entry => entry.Kind == "part" && entry.Id == part.Part.Value).Type;
        var def = Activator.CreateInstance(type)!;
        var lods = (System.Collections.IDictionary)type.GetProperty("Lods")!.GetValue(def)!;
        PartGeometry? Level(LodLevel l) => lods.Contains(l) ? (PartGeometry)((Delegate)lods[l]!).DynamicInvoke(part.Params)! : null;
        var l0 = Level(LodLevel.L0)!;
        return (l0.Meshes.Sum(m => m.Index.Length / 3), Level(LodLevel.L1)?.Meshes.Sum(m => m.Index.Length / 3) ?? 0, l0.Meshes.Count);
    }

    /// <summary>A uniform tilt along x with a constant grade.</summary>
    private static FormulaTerrain Tilted(float grade) =>
        new((x, z) => grade * x, (x, z) => new TerrainFacts(grade, TerrainClass.Slope, 0f, 0f));
}
