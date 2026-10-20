using System.Numerics;
using Campus.Tests;
using Campus.Gen.Geometry;
using Campus.Gen.Sites;
using Campus.Gen.Sites.Tests;
using Campus.Contracts;
using Campus.Gen.World;
using GdUnit4;

namespace Campus.Gen.Maps.Qingshuihe.Public.Tests;

using static GdUnit4.Assertions;

/// <summary>WP-29 29a: the registered corner pocket greens of world/public.json, loaded with every other world file of the map.</summary>
[TestSuite]
public sealed class PublicModuleTests
{
    private static readonly string[] Carriageway = ["service", "residential", "tertiary", "secondary", "primary", "unclassified", "living_street", "road", "trunk"];
    private static readonly Lazy<IWorldModel> Loaded = new(() => RealMapFixture.Load().World);

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void AuthoredPocketGreensLoadWithTheWorld()
    {
        var world = Loaded.Value;
        var benches = PocketGreens.Benches(world);
        var pits = PocketGreens.Pits(world);
        AssertThat(benches.Count > 0 && benches.Count == world.Features.Values.Count(f => f.Kind.Value == PublicTune.ArcBenchKind && f.Id.Value.StartsWith(PublicIds.Prefix, StringComparison.Ordinal))).IsTrue();
        AssertThat(pits.Count > 0 && pits.Count == world.Features.Values.Count(f => f.Kind.Value == PublicTune.TreePitKind && f.TryText("stone") == "site" && f.Id.Value.StartsWith(PublicIds.Prefix, StringComparison.Ordinal))).IsTrue();
        foreach (var bench in benches)
        {
            var pad = Pad(world, bench);
            AssertThat(pad.Zone?.Value == "public").OverrideFailureMessage($"{pad.Id.Value} is filed under the public zone").IsTrue();
            AssertThat(pad.TryText("highway") == "pedestrian").OverrideFailureMessage($"{pad.Id.Value} is a pedestrian area WP-15 paves").IsTrue();
        }
    }

    /// <summary>Each pad keeps off carriageways, footways, buildings, water, parking, courts and every other package's additions.</summary>
    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void PadsKeepClearOfRoadsBuildingsAndOtherAdditions()
    {
        var world = Loaded.Value;
        foreach (var bench in PocketGreens.Benches(world))
        {
            var (c, r) = Disc(Pad(world, bench));
            AssertThat(Vector2.Distance(c, bench.Centre) < TestTolerance.Position).OverrideFailureMessage($"{bench.Id.Value} is centred on its pad").IsTrue();
            foreach (var road in world.Network.Roads)
            {
                var type = road.Props.TryText("highway") ?? "";
                var edge = road.Width * 0.5f + (Carriageway.Contains(type) ? 0.15f : 0f);
                var d = MathF.Sqrt(PlanLines.PolylineDistanceSquared(road.Line.Points, c)) - edge - r;
                AssertThat(d > 0.2f).OverrideFailureMessage($"{bench.Id.Value} pad comes within {d:F2} m of road {road.Id.Value} ({type})").IsTrue();
            }
            foreach (var f in world.Features.Values)
            {
                if (f.Id.Value.StartsWith(PublicIds.Prefix, StringComparison.Ordinal)) continue;
                var kind = f.Kind.Value;
                if ((kind is "building" or "water" or "parking" or "sport" or "road") && (f.Geometry is PolygonGeometry or MultiPolygonGeometry))
                    foreach (var ring in f.Polygons().Select(p => p.Outer))
                        AssertThat(PlanRings.DistanceToRing(c, ring) - r > 0.2f && !PlanRings.PointInRing(c, ring)).OverrideFailureMessage($"{bench.Id.Value} pad overlaps {f.Id.Value} ({kind})").IsTrue();
                if (f.Id.Value.StartsWith("design:", StringComparison.Ordinal) && f.Geometry is PointGeometry p)
                    AssertThat(Vector2.Distance(c, new(p.Position.X, p.Position.Z)) > r + 1f).OverrideFailureMessage($"{bench.Id.Value} pad sits on {f.Id.Value}").IsTrue();
            }
            foreach (var tree in world.Network.Trees)
                AssertThat(Vector2.Distance(c, new(tree.Position.X, tree.Position.Z)) > r + 1f).OverrideFailureMessage($"{bench.Id.Value} pad sits on OSM tree {tree.Id.Value}").IsTrue();
        }
    }

    /// <summary>The bench and tree fit on their authored pad with room for a sitter.</summary>
    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void BenchAndTreeFitOnThePad()
    {
        var world = Loaded.Value;
        var pits = PocketGreens.Pits(world);
        foreach (var bench in PocketGreens.Benches(world))
        {
            var (c, r) = Disc(Pad(world, bench));
            AssertThat(bench.Radius + PublicTune.Bench.PostOffset + PublicTune.Bench.PostSize < r - 0.2f).OverrideFailureMessage($"{bench.Id.Value} back stays on the paving").IsTrue();
            var pit = pits.Single(p => Vector2.Distance(p.Centre, c) < r);
            AssertThat(Vector2.Distance(pit.Centre, c) + pit.Radius < r - 0.3f).OverrideFailureMessage($"{pit.Id.Value} lies inside the pad with a paved margin").IsTrue();
            AssertThat(bench.Radius - PublicTune.Bench.HalfDepth - (Vector2.Distance(pit.Centre, c) + pit.Radius) > 0.6f).OverrideFailureMessage($"{pit.Id.Value} leaves knee room in front of the bench").IsTrue();
            var toPit = Vector2.Normalize(pit.Centre - c);
            AssertThat(Vector2.Dot(toPit, -Angles.Heading((bench.From + bench.To) * 0.5f)) > 0.95f).OverrideFailureMessage($"{pit.Id.Value} stands north-east of the pad centre, so its evening shadow misses the bench").IsTrue();
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void SeatsAndCollidersShareTheSeatBoxes()
    {
        var ctx = Context();
        var module = new PublicModule();
        var world = ctx.World;
        float Ground(Vector2 p) => world.Terrain.HeightAt(p.X, p.Y);
        var colliders = module.Tiles(ctx).SelectMany(t => module.Collide(ctx, t)).ToArray();
        var benches = PocketGreens.Benches(world);
        AssertThat(colliders.OfType<BoxCollider>().Count() == benches.Count * PublicTune.Bench.ColliderBoxes).OverrideFailureMessage("four seat boxes per bench").IsTrue();
        AssertThat(colliders.OfType<PostCollider>().Count() == PocketGreens.Pits(world).Count).OverrideFailureMessage("one kerb post per pit").IsTrue();
        foreach (var bench in benches)
        {
            var deck = PocketGreens.Paving(bench, Ground);
            foreach (var box in PocketGreens.SeatBoxes(bench, Ground))
            {
                var top = box.At.M42 + box.Half.Y - deck;
                AssertThat(top is >= 0.35f and <= 1.25f).OverrideFailureMessage($"{bench.Id.Value} seat box top {top:F2} m is jumpable (SYS-WALK)").IsTrue();
            }
            var seats = PocketGreens.Seats(bench, Ground);
            AssertThat(seats.Count == PublicTune.Bench.ColliderBoxes && seats.All(s => s.Back)).OverrideFailureMessage($"{bench.Id.Value} declares a backed seat edge on every box").IsTrue();
            foreach (var seat in seats)
            {
                var mid = (seat.From + seat.To) * 0.5f;
                AssertThat(Vector2.Dot(new(seat.Facing.X, seat.Facing.Z), bench.Centre - new Vector2(mid.X, mid.Z)) > 0f).OverrideFailureMessage($"{bench.Id.Value} sitters face the pad centre").IsTrue();
                AssertThat(MathF.Abs(mid.Y - deck - PublicTune.Bench.SeatTop) < TestTolerance.Position).OverrideFailureMessage($"{bench.Id.Value} seat edge at the seat top").IsTrue();
            }
        }
    }

    [TestCase, TestCategory(TestTag.Determinism)]
    [TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void BuildIsDeterministicWithRegisteredColours()
    {
        var ctx = Context();
        var module = new PublicModule();
        var tiles = module.Tiles(ctx).ToArray();
        AssertThat(tiles.Length > 0).OverrideFailureMessage("pocket greens occupy cells").IsTrue();
        foreach (var lod in new[] { LodLevel.L0, LodLevel.L1, LodLevel.L2 })
        {
            var cells = ZoneTestCells.Heaviest(module, ctx, lod);
            var total = 0;
            foreach (var cell in cells)
            {
                var a = module.Build(ctx, cell, lod);
                var b = module.Build(ctx, cell, lod);
                AssertThat(a.Meshes.Count > 0).OverrideFailureMessage($"{lod} {cell}: {a.Meshes.Count} draws").IsTrue();
                AssertThat(a.Meshes.Count == b.Meshes.Count && a.Meshes.Zip(b.Meshes).All(p => p.First.Position.SequenceEqual(p.Second.Position) && p.First.Index.SequenceEqual(p.Second.Index))).OverrideFailureMessage($"{lod} {cell} builds the same bytes twice").IsTrue();
                var triangles = a.Meshes.Sum(m => m.Index.Length / 3);
                total += triangles;
                foreach (var mesh in a.Meshes)
                    AssertThat(mesh.Position.All(float.IsFinite) && mesh.Normal.All(float.IsFinite)).OverrideFailureMessage($"{lod} {cell}: finite vertices").IsTrue();
            }
            AssertThat(total > 0).OverrideFailureMessage($"{lod} builds something").IsTrue();
        }
        foreach (var material in new[] { PublicTune.Look.Wood, PublicTune.Look.Granite, PublicTune.Look.Steel, PublicTune.Look.Soil })
            AssertThat(ctx.Catalog.TryGet(material.Lit, out _) && ctx.Catalog.TryGet(material.Shade, out _)).IsTrue();
        AssertThat(module.Build(ctx, new(0, 0), LodLevel.L4).Meshes.Count == 0).OverrideFailureMessage("no whole-map product").IsTrue();
        foreach (var t in tiles) AssertThat(module.Build(ctx, TileGrid.At(TileGrid.Origin(t), LodLevel.L3), LodLevel.L3).Meshes.Count == 0).OverrideFailureMessage("nothing at L3").IsTrue();
    }

    // ---- Helpers ----

    private static WorldFeature Pad(IWorldModel world, ArcBench bench) =>
        world.Features[new(bench.Id.Value[..^"-bench".Length])];

    private static (Vector2 Centre, float Radius) Disc(WorldFeature pad)
    {
        var ring = ((PolygonGeometry)pad.Geometry).Rings.Outer;
        var c = PlanPolygons.Centroid(ring);
        return (c, ring.Min(p => Vector2.Distance(p, c)));
    }

    private static BuildContext Context() => new() { CodeHash = "fixture", WindowNumbering = new(new MapId("fixture"), new string('0', 64), []), Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog, World = Loaded.Value, Map = null!, WorldSeed = 1 };

}
