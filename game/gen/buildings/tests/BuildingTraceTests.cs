using Campus.Tests;
using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Parts;
using Campus.Gen.Buildings.Build;
using Campus.Gen.Buildings.Model;
using Campus.Gen.Buildings.Plan;
using GdUnit4;

namespace Campus.Gen.Buildings.Tests;

using static Campus.Gen.Buildings.Tests.BuildingsTestSupport;

using static GdUnit4.Assertions;

[TestSuite]
public sealed class BuildingTraceTests
{
    [TestCase, TestCategory(TestReason.Contract)]
    public void TraceSourcesReproduceBuildAndCollidePerBuilding()
    {
        // Full-map sources are selected by the plans that actually contribute to each fixture cell.
        var door = Anchor("t:dorm", "dorm.south", new(21.3f, 0f, 0f), 0f, outsideY: -0.9f);
        WorldFeature[] features =
        [
            WorldFeatureFixture.Create("t:dorm", "building", new PolygonGeometry(new PolygonRings(Rect(0, 0, 60, 18))), props: "{\"category\":\"residence\"}", height: 19.8f, storeys: 6),
            WorldFeatureFixture.Create("t:lab", "building", new PolygonGeometry(new PolygonRings(Rect(10, 30, 40, 16))), props: "{\"category\":\"research\"}", height: 15.6f, storeys: 4),
            WorldFeatureFixture.Create("t:claimed", "building", new PolygonGeometry(new PolygonRings(Rect(5, 50, 20, 10))), props: "{\"category\":\"teaching\"}", height: 10.5f, storeys: 3),
            WorldFeatureFixture.Create("t:hall", "building", new PolygonGeometry(new PolygonRings(Rect(100, 0, 40, 30))), props: "{\"category\":\"sport\"}", height: 9f, storeys: 1),
            WorldFeatureFixture.Create("t:court", "building", new PolygonGeometry(Courtyard(0, 100)), props: "{\"category\":\"teaching\"}", height: 12f, storeys: 3),
            WorldFeatureFixture.Create("t:plant", "building", new PolygonGeometry(new PolygonRings(Rect(200, 0, 20, 12))), props: "{\"category\":\"utility\"}", height: 8f, storeys: 2),
        ];
        var world = World(Entrances(door), Falling(-1f, 0.9f), features);
        var ctx = Prepared(Ctx(world, 2u, claims: ["t:claimed"]));
        var module = new BuildingsModule();
        var plans = BuildingSet.For(world, 2u).Plans;
        var mine = plans.Where(p => BuildingsModule.Unclaimed(ctx, p)).ToArray();
        AssertThat(plans.Count == 6 && mine.Length == 5 && mine.All(p => p.Key != "t:claimed")).OverrideFailureMessage("Expected: " + ("the claimed building is a plan but not WP-14's")).IsTrue();

        // A cell with two buildings, and the coarser cells with more of them.
        var home = mine.Single(p => p.Key == "t:dorm").Tile(LodLevel.L0);
        var traced = module.TraceSources(ctx, LodLevel.L0).ToArray();
        var cell = AssertTraceMatches(module, ctx, home, LodLevel.L0, traced);
        AssertThat(cell.Select(t => t.SourceId).SequenceEqual(["building/t:dorm/t:dorm", "building/t:lab/t:lab"])).OverrideFailureMessage("Expected: " + ("two buildings share the dorm's L0 cell, the claimed neighbour in it is not traced: " + string.Join(", ", cell.Select(t => t.SourceId)))).IsTrue();
        AssertThat(!cell[0].Colliders.OfType<WallCollider>().Any(w => w.Tag == SurfaceTag.DoorClosed) && cell[0].Colliders.OfType<RampCollider>().Any(r => r.Stair is { Count: 7 })).OverrideFailureMessage("Expected: " + ("the dorm's colliders leave its open door clear and retain its flight of steps")).IsTrue();
        AssertThat(ColliderDumps(cell[0].Colliders).Any(d => d.Contains("\"M42\"", StringComparison.Ordinal)) && ColliderDumps(cell[0].Colliders).Any(d => d.Contains("\"Y1\"", StringComparison.Ordinal))).OverrideFailureMessage("Expected: " + ("the dumps reach into the box transforms and the wall heights")).IsTrue();
        AssertThat(AssertTraceMatches(module, ctx, plans.Single(p => p.Key == "t:dorm").Tile(LodLevel.L2), LodLevel.L2).Count == 5).OverrideFailureMessage("Expected: " + ("all five buildings share the L2 cell")).IsTrue();
        AssertThat(AssertTraceMatches(module, ctx, home, LodLevel.L0, traced).Count == 2 && AssertTraceMatches(module, ctx, new TileKey(7, 7), LodLevel.L0, traced).Count == 0).OverrideFailureMessage("Expected: " + ("an empty cell has no source")).IsTrue();

        var ids = new List<string>();
        foreach (var lod in Levels)
        {
            var levelSources = module.TraceSources(ctx, lod).ToArray();
            foreach (var tile in mine.Select(p => p.Tile(lod)).Distinct())
            {
                var traces = AssertTraceMatches(module, ctx, tile, lod, levelSources);
                AssertThat(traces.Count == mine.Count(p => p.Tile(lod) == tile)).OverrideFailureMessage("Expected: " + ($"{lod}: one source per building of the cell")).IsTrue();
                if (lod == LodLevel.L0) ids.AddRange(traces.Select(t => t.SourceId));
            }
        }
        AssertThat(ids.Count == 5 && ids.Distinct(StringComparer.Ordinal).Count() == 5).OverrideFailureMessage("Expected: " + ("every building has its own source ID over the map")).IsTrue();

        // The IDs and what they stand for do not depend on the order the features were declared in.
        var shuffled = Prepared(Ctx(World(Entrances(door), Falling(-1f, 0.9f), [.. Enumerable.Reverse(features)]), 2u, claims: ["t:claimed"]));
        {
            var a = traced.ToDictionary(t => t.SourceId);
            var b = module.TraceSources(shuffled, LodLevel.L0).ToDictionary(t => t.SourceId);
            AssertThat(a.Keys.Order(StringComparer.Ordinal).SequenceEqual(b.Keys.Order(StringComparer.Ordinal)) &&
                a.All(p => p.Value.Output.Meshes.Count == b[p.Key].Output.Meshes.Count && p.Value.Output.Meshes.Zip(b[p.Key].Output.Meshes).All(m => SameBundle(m.First, m.Second)) &&
                    ColliderDumps(p.Value.Colliders).SequenceEqual(ColliderDumps(b[p.Key].Colliders)))).OverrideFailureMessage("Expected: " + ("same sources, same geometry")).IsTrue();
        }

        // Enumeration reads the map's one window allocation; an empty world needs none (as Build).
        AssertThrown(() => module.TraceSources(Ctx(world, 2u), LodLevel.L0).ToArray()).IsInstanceOf<InvalidOperationException>();
        AssertThat(!module.TraceSources(Ctx(World(), 2u), LodLevel.L0).Any()).OverrideFailureMessage("Expected: " + ("nothing to number in an empty world")).IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void TraceSourcesReproduceBuildAndCollideOnTheRealCampus()
    {
        var real = RealMapFixture.Load().World;
        var ctx = Prepared(new BuildContext { Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog, World = real, Map = TestMap, WorldSeed = 1u, ClaimedBy = RealClaims(real) });
        var module = new BuildingsModule();
        var mine = BuildingSet.For(real, 1u).Plans.Where(p => BuildingsModule.Unclaimed(ctx, p)).ToArray();
        AssertThat(mine.Select(p => $"building/{p.Id.Value}/{p.Key}").Distinct(StringComparer.Ordinal).Count() == mine.Length).OverrideFailureMessage("Expected: " + ("a source ID per building part over the map")).IsTrue();
        foreach (var lod in new[] { LodLevel.L0, LodLevel.L2 })
        {
            // The busiest cell of the level (most buildings, then the lowest cell).
            var busiest = mine.GroupBy(p => p.Tile(lod)).OrderByDescending(g => g.Count()).ThenBy(g => g.Key.X).ThenBy(g => g.Key.Z).First();
            var traces = AssertTraceMatches(module, ctx, busiest.Key, lod, module.TraceSources(ctx, lod).ToArray());
            AssertThat(traces.Count == busiest.Count() && (lod != LodLevel.L0 || traces.Count > 1)).OverrideFailureMessage("Expected: " + ($"{lod}: every unclaimed building of the busiest cell, {traces.Count}")).IsTrue();
        }
    }

    /// <summary>
    /// Full-map sources selected by the actual plans of a cell, flattened, reproduce that cell's Build (meshes, parts and instances, in order) and its Collide
    /// (the same sequence at L0, the same multiset over the L0 cells of the traced buildings at a coarser level).
    /// </summary>
    private static IReadOnlyList<ContentSourceTrace> AssertTraceMatches(BuildingsModule module, BuildContext ctx, TileKey tile, LodLevel lod, IReadOnlyList<ContentSourceTrace>? allSources = null)
    {
        var cell = $"{lod} cell ({tile.X}, {tile.Z})";
        var built = module.Build(ctx, tile, lod);
        var set = BuildingSet.For(ctx.World, ctx.WorldSeed);
        var plans = set.Plans.Where(p => BuildingsModule.Unclaimed(ctx, p) && p.Tile(lod) == tile).ToArray();
        var ids = plans.Select(p => $"building/{p.Id.Value}/{p.Key}").ToHashSet(StringComparer.Ordinal);
        IReadOnlyList<ContentSourceTrace> traces = (allSources ?? module.TraceSources(ctx, lod).ToArray()).Where(t => ids.Contains(t.SourceId)).ToArray();
        AssertThat(traces.Count == plans.Length && traces.All(t => plans.Any(p => p.Id == t.Feature && t.SourceId == $"building/{p.Id.Value}/{p.Key}"))).OverrideFailureMessage("Expected: " + ($"{cell}: actual source feature and stable ID")).IsTrue();
        AssertThat(traces.Select(t => t.SourceId).Distinct(StringComparer.Ordinal).Count() == traces.Count).OverrideFailureMessage("Expected: " + ($"{cell}: source IDs are unique")).IsTrue();
        var meshes = traces.SelectMany(t => t.Output.Meshes).ToArray();
        AssertThat(meshes.Length == built.Meshes.Count && meshes.Zip(built.Meshes).All(m => SameBundle(m.First, m.Second))).OverrideFailureMessage("Expected: " + ($"{cell}: the traced meshes are the Build meshes, in order")).IsTrue();
        AssertThat(traces.SelectMany(t => t.Output.Parts).SequenceEqual(built.Parts) && traces.SelectMany(t => t.Output.Instances).SequenceEqual(built.Instances)).OverrideFailureMessage("Expected: " + ($"{cell}: the traced parts and instances are the Build ones")).IsTrue();
        foreach (var trace in traces)
        {
            var plan = plans.Single(p => trace.SourceId == $"building/{p.Id.Value}/{p.Key}");
            var footprint = trace.OccupiedContours.Single();
            AssertThat(footprint.Outer.SequenceEqual(plan.SkinOuter.Points) &&
                footprint.Holes.Count == plan.SkinHoles.Count &&
                footprint.Holes.Zip(plan.SkinHoles).All(h => h.First.SequenceEqual(h.Second.Points))).OverrideFailureMessage("Expected: " + ($"{cell}: source footprint follows the generated wall skin, including courtyard holes")).IsTrue();
            AssertThat(!ReferenceEquals(footprint.Outer, plan.SkinOuter.Points) &&
                footprint.Holes.Zip(plan.SkinHoles).All(h => !ReferenceEquals(h.First, h.Second.Points))).OverrideFailureMessage("Expected: " + ($"{cell}: source footprint is a snapshot of the plan")).IsTrue();
        }
        var cells = lod == LodLevel.L0 ? new[] { tile } : plans.Select(p => p.Tile(LodLevel.L0)).Distinct().ToArray();
        var expected = ColliderDumps(cells.SelectMany(c => module.Collide(ctx, c)));
        var actual = ColliderDumps(traces.SelectMany(t => t.Colliders));
        if (lod != LodLevel.L0)
        {
            expected.Sort(StringComparer.Ordinal);
            actual.Sort(StringComparer.Ordinal);
        }
        AssertThat(actual.SequenceEqual(expected)).OverrideFailureMessage("Expected: " + ($"{cell}: the traced colliders are the Collide colliders ({actual.Count} against {expected.Count})")).IsTrue();
        return traces;
    }

    /// <summary>Every field of a collider as one string (the records hold lists and arrays, so their own equality is by reference).</summary>
    private static List<string> ColliderDumps(IEnumerable<Collider> colliders)
    {
        var options = new JsonSerializerOptions { IncludeFields = true, NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals };
        return [.. colliders.Select(c => JsonSerializer.Serialize<Collider>(c, options))];
    }

    /// <summary>Same material and the same bits in every vertex array and flag.</summary>
    private static bool SameBundle(GeometryBundle a, GeometryBundle b)
    {
        static bool Bits<T>(T[]? x, T[]? y) where T : struct =>
            x is null || y is null ? x is null && y is null : System.Runtime.InteropServices.MemoryMarshal.AsBytes<T>(x).SequenceEqual(System.Runtime.InteropServices.MemoryMarshal.AsBytes<T>(y));
        static bool SameLods(IReadOnlyList<MeshLod>? x, IReadOnlyList<MeshLod>? y) =>
            x is null || y is null ? x is null && y is null
                : x.Count == y.Count && x.Zip(y).All(l => BitConverter.SingleToInt32Bits(l.First.Error) == BitConverter.SingleToInt32Bits(l.Second.Error) && l.First.Index.SequenceEqual(l.Second.Index));
        return a.Material == b.Material && Equals(a.Variant, b.Variant) && Equals(a.Shader, b.Shader) &&
            a.IgnoreOcclusionCulling == b.IgnoreOcclusionCulling && a.Reflect == b.Reflect &&
            Bits(a.Position, b.Position) && Bits(a.Normal, b.Normal) && Bits(a.Index, b.Index) && Bits(a.Ao, b.Ao) && Bits(a.Uv, b.Uv) && Bits(a.Uv2, b.Uv2) &&
            Bits(a.ColorIndices, b.ColorIndices) && Bits(a.ColorIndices, b.ColorIndices) && Bits(a.DecalBaseColorIndices, b.DecalBaseColorIndices) &&
            Bits(a.Flags, b.Flags) && Bits(a.Reserved, b.Reserved) && Bits(a.Custom0, b.Custom0) && Bits(a.Custom1, b.Custom1) && Bits(a.Custom2, b.Custom2) &&
            SameLods(a.Lods, b.Lods);
    }
}
