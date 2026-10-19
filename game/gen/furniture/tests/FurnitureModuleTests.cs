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
using Campus.Tests;

namespace Campus.Gen.Furniture.Tests;

using static Campus.Gen.Furniture.Tests.FurnitureTestSupport;

using static GdUnit4.Assertions;

[TestSuite]
public sealed class FurnitureModuleTests
{
    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void FurnitureLandsOnTheTerrain()
    {
        // 8% everywhere: poles sink 0.05–0.3 m below the lawn level at their centre; benches and bins share a concrete plinth.
        var terrain = Tilted(0.08f);
        var gentle = Build(new FakeWorld(World.Value) { Terrain = terrain });
        foreach (var lamp in gentle.Items.Where(i => i.Kind == FurnitureKind.StreetLamp))
        {
            var burial = terrain.HeightAt(lamp.Part.At.M41, lamp.Part.At.M43) + GroundLevels.Lawn - lamp.Part.At.M42;
            AssertThat(burial is >= 0.049f and <= 0.301f).OverrideFailureMessage($"{lamp.Key} buried {burial:F3} m").IsTrue();
        }
        var benches = gentle.Items.Where(i => i.Kind == FurnitureKind.Bench).ToList();
        AssertThat(benches.Count > 0).OverrideFailureMessage("no benches on 8%").IsTrue();
        foreach (var bench in benches)
        {
            AssertThat(bench.Pad is { Plinth: true } pad && MathF.Abs(pad.Top - bench.Part.At.M42) < TestTolerance.Position).OverrideFailureMessage($"{bench.Key} has no plinth on 8%").IsTrue();
            var plinth = bench.Pad!;
            var high = Corners(bench).Max(c => terrain.HeightAt(c.X, c.Y));
            var low = Corners(bench).Min(c => terrain.HeightAt(c.X, c.Y));
            AssertThat(plinth.Top >= high + GroundLevels.Paving - TestTolerance.Position && plinth.Bottom <= low - FurnitureStyle.PadSkirt + TestTolerance.Position).OverrideFailureMessage($"{bench.Key}: plinth does not cover the slope").IsTrue();
            AssertThat(bench.Colliders.OfType<BoxCollider>().Any(c => c.Foot == FootMat.Concrete)).OverrideFailureMessage($"{bench.Key}: plinth has no collider").IsTrue();
            AssertThat(plinth.Geometry().Meshes.Sum(m => m.Index.Length) > 0).OverrideFailureMessage($"{bench.Key}: empty plinth mesh").IsTrue();
            var bin = gentle.Items.SingleOrDefault(i => i.Key == bench.Key.Replace(":bench", ":bin", StringComparison.Ordinal));
            AssertThat(bin is null || bin.Pad is { Plinth: true } own && MathF.Abs(bin.Part.At.M42 - own.Top) < TestTolerance.Position).OverrideFailureMessage($"{bench.Key}: the bin is off its own plinth").IsTrue();
        }
        // Flat: rest stops stand on paved bays; authored deck kiosks stay at their declared elevation.
        var flatWorld = new FakeWorld(World.Value) { Terrain = FlatTerrain.Default };
        var flat = Build(flatWorld);
        var top = GroundLevels.Paving + FurnitureStyle.PadProud;
        AssertThat(flat.Items.Where(i => i.Pad is not null).All(i => !i.Pad!.Plinth)).OverrideFailureMessage("plinths on flat ground").IsTrue();
        AssertThat(flat.Items.Where(i => i.Kind is FurnitureKind.Bench or FurnitureKind.Bin).All(i => MathF.Abs(i.Part.At.M42 - (i.Pad is null ? GroundLevels.Paving : top)) < TestTolerance.Position)).OverrideFailureMessage("benches off their bays (or, on a plaza, off its paving)").IsTrue();
        var kiosks = flat.Items.Where(i => i.Kind == FurnitureKind.AtlasKiosk).ToList();
        var deckAnchors = flatWorld.Features.Values.Where(f => f.Kind.Value == "anchor" && f.TryText("form") == "kiosk"
            && f.TryNumber("groundY") is > FurnitureStyle.DeckAbove).ToArray();
        AssertThat(deckAnchors.Length > 0).OverrideFailureMessage("the map has no elevated kiosk landing to exercise").IsTrue();
        foreach (var anchor in deckAnchors)
        {
            var kiosk = kiosks.Single(k => k.Key == "anchor:" + anchor.Id.Value);
            var ring = ((PolygonGeometry)anchor.Geometry).Rings.Outer;
            var centre = ring.Aggregate(Vector2.Zero, (a, point) => a + point) / ring.Count;
            AssertThat(Vector2.Distance(kiosk.Anchor, centre) < TestTolerance.Position).OverrideFailureMessage($"{anchor.Id}: deck kiosk moved off its authored footprint").IsTrue();
        }
        foreach (var kiosk in kiosks)
        {
            var anchor = flatWorld.Features[new(kiosk.Key["anchor:".Length..])];
            var level = anchor.TryNumber("groundY") ?? flatWorld.Terrain.PadY(anchor.Id);
            AssertThat(MathF.Abs(kiosk.Part.At.M42 - level - top) < TestTolerance.Position).OverrideFailureMessage($"{anchor.Id}: kiosk is off its authored landing").IsTrue();
        }
        // 30%: no pole stands.
        var steep = Build(new FakeWorld(World.Value) { Terrain = Tilted(0.3f) });
        AssertThat(steep.Count(FurnitureKind.StreetLamp) == 0 && steep.Count(FurnitureKind.AtlasPost) == 0).OverrideFailureMessage("poles on a 30% grade").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void CollidersFollowThePartsWithTheirFootMaterial()
    {
        foreach (var item in RealLayout.Value.Items)
        {
            AssertThat(item.Colliders.All(c => c.Foot != FootMat.Auto)).OverrideFailureMessage($"{item.Key} leaves a collider on Auto (G21i)").IsTrue();
            foreach (var collider in item.Colliders)
            {
                var at = collider switch
                {
                    PostCollider post => post.C,
                    BoxCollider box => new Vector2(box.At.M41, box.At.M43),
                    RampCollider ramp => new Vector2(ramp.P0.X + ramp.P2.X, ramp.P0.Z + ramp.P2.Z) * 0.5f,
                    _ => throw new InvalidOperationException(),
                };
                AssertThat(Vector2.Distance(at, item.Anchor) < 2f).OverrideFailureMessage($"{item.Key}: a collider strayed {Vector2.Distance(at, item.Anchor):F2} m").IsTrue();
            }
            var expected = item.Kind switch
            {
                FurnitureKind.StreetLamp or FurnitureKind.AtlasPost => 1,
                FurnitureKind.BusStop => 2,
                // Seat and back, and its bay; a plaza bench stands on the paving itself.
                FurnitureKind.Bench => item.Pad is null ? 2 : 3,
                FurnitureKind.AtlasKiosk => 4,
                FurnitureKind.Bin => 2,
                FurnitureKind.OpenSign or FurnitureKind.ParkedCar or FurnitureKind.KerbRamp or FurnitureKind.Barrier => 1,
                // Two posts and the boards, and the bay; the fountain's box and its bay.
                FurnitureKind.Signpost => item.Pad is null ? 3 : 4,
                FurnitureKind.DrinkingFountain => item.Pad is null ? 1 : 2,
                // A vending group's bay rides on its first machine; a locker's box and a bay's sign, with their bays.
                FurnitureKind.VendingMachine or FurnitureKind.DeliveryLocker or FurnitureKind.BicycleBay => item.Pad is null ? 1 : 2,
                // Parked bikes have no collision (WLD-LIFE).
                FurnitureKind.EntranceMat or FurnitureKind.EntranceLamp or FurnitureKind.ParkedBicycle => 0,
                _ => throw new InvalidOperationException(),
            };
            AssertThat(item.Colliders.Count == expected).OverrideFailureMessage($"{item.Key}: {item.Colliders.Count} colliders").IsTrue();
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void ModuleTilesCarryEveryItemOnce()
    {
        var ctx = Context(Inputs());
        var module = new FurnitureModule();
        var layout = FurnitureModule.Layout(ctx);
        var tiles = module.Tiles(ctx).ToList();
        var parts = 0; var emitters = 0; var colliders = 0;
        foreach (var tile in tiles)
        {
            var output = module.Build(ctx, tile, LodLevel.L0);
            parts += output.Parts.Count;
            emitters += output.Emitters?.Count ?? 0;
            colliders += module.Collide(ctx, tile).Count;
            AssertThat(module.Build(ctx, tile, LodLevel.L1).Emitters is null).OverrideFailureMessage("emitters outside L0").IsTrue();
        }
        AssertThat(parts == layout.Items.Count).OverrideFailureMessage($"{parts} placed parts for {layout.Items.Count} items").IsTrue();
        AssertThat(emitters == layout.Items.Sum(i => i.Emitters.Count)).OverrideFailureMessage("emitters lost or repeated").IsTrue();
        AssertThat(module.DescribeLights(ctx).SequenceEqual(layout.Items.SelectMany(i => i.Emitters))).OverrideFailureMessage("light metadata differs from the item layout").IsTrue();
        AssertThat(colliders == layout.Items.Sum(i => i.Colliders.Count)).OverrideFailureMessage("colliders lost or repeated").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void LayoutDiagnosticsTravelWithTheFirstCellOnly()
    {
        // Structured diagnostics (ContentOutput.Diagnostics) are reported once per layout: with the first cell's L0 output.
        var module = new FurnitureModule();
        var quiet = Context(Inputs());
        AssertThat(module.Tiles(quiet).All(t => !module.Build(quiet, t, LodLevel.L0).Diagnostics.Any(d => d.Code == PlantingRows.MissingCode))).OverrideFailureMessage("warnings with the real planting input").IsTrue();
        var ctx = Context(new Dictionary<string, ReadOnlyMemory<byte>>());
        var tiles = module.Tiles(ctx).ToList();
        var warned = tiles.Where(t => module.Build(ctx, t, LodLevel.L0).Diagnostics.Count > 0).ToList();
        AssertThat(warned.Count == 1 && warned[0] == tiles[0]).OverrideFailureMessage($"{warned.Count} cells carry the warning").IsTrue();
        var diagnostics = module.Build(ctx, tiles[0], LodLevel.L0).Diagnostics;
        foreach (var diagnostic in diagnostics) diagnostic.Validate();
        AssertThat(diagnostics.SequenceEqual(FurnitureModule.Layout(ctx).Diagnostics)).OverrideFailureMessage("layout diagnostics changed in the module output").IsTrue();
        AssertThat(diagnostics.Count(d => d.Code == PlantingRows.MissingCode && d.Level == DiagnosticLevel.Warning) == 1).OverrideFailureMessage("not one missing-input warning").IsTrue();
        AssertThat(tiles.All(t => module.Build(ctx, t, LodLevel.L1).Diagnostics.Count == 0)).OverrideFailureMessage("warnings outside L0").IsTrue();
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
            new MapEnvironment("sky.json", "backdrop.json", []), []);
        return new BuildContext { Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog, World = World.Value, Map = map, WorldSeed = 1u, SourceInputs = inputs, GlobalInputs = FurnitureTestSupport.GlobalInputs };
    }

    /// <summary>A uniform tilt along x with a constant grade.</summary>
    private static FormulaTerrain Tilted(float grade) =>
        new((x, z) => grade * x, (x, z) => new TerrainFacts(grade, TerrainClass.Slope, 0f, 0f));
}
