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

[TestSuite]
public sealed class BuildingTraceTests
{
    [TestCase]
    public void TraceSourcesReproduceBuildAndCollidePerBuilding()
    {
        // Full-map sources are selected by the plans that actually contribute to each fixture cell.
        var door = Anchor("t:dorm", "dorm.south", new(21.3f, 0f, 0f), 0f, outsideY: -0.9f);
        WorldFeature[] features =
        [
            Feature("t:dorm", Rect(0, 0, 60, 18), 6, 19.8f, "residence"),
            Feature("t:lab", Rect(10, 30, 40, 16), 4, 15.6f, "research"),
            Feature("t:claimed", Rect(5, 50, 20, 10), 3, 10.5f, "teaching"),
            Feature("t:hall", Rect(100, 0, 40, 30), 1, 9f, "sport"),
            Feature("t:court", Courtyard(0, 100), 3, 12f, "teaching"),
            Feature("t:plant", Rect(200, 0, 20, 12), 2, 8f, "utility"),
        ];
        var world = World(new Entrances(door), new Falling(-1f, 0.9f), features);
        var ctx = Prepared(Ctx(world, 2u, claims: ["t:claimed"]));
        var module = new BuildingsModule();
        var plans = BuildingSet.For(world, 2u).Plans;
        var mine = plans.Where(p => BuildingsModule.Unclaimed(ctx, p)).ToArray();
        Require(plans.Count == 6 && mine.Length == 5 && mine.All(p => p.Key != "t:claimed"), "the claimed building is a plan but not WP-14's");

        // A cell with two buildings, and the coarser cells with more of them.
        var home = mine.Single(p => p.Key == "t:dorm").Tile(LodLevel.L0);
        var traced = module.TraceSources(ctx, LodLevel.L0).ToArray();
        var cell = AssertTraceMatches(module, ctx, home, LodLevel.L0, traced);
        Require(cell.Select(t => t.SourceId).SequenceEqual(["building/t:dorm/t:dorm", "building/t:lab/t:lab"]),
            "two buildings share the dorm's L0 cell, the claimed neighbour in it is not traced: " + string.Join(", ", cell.Select(t => t.SourceId)));
        Require(!cell[0].Colliders.OfType<WallCollider>().Any(w => w.Tag == SurfaceTag.DoorClosed) && cell[0].Colliders.OfType<RampCollider>().Any(r => r.Stair is { Count: 7 }),
            "the dorm's colliders leave its open door clear and retain its flight of steps");
        Require(ColliderDumps(cell[0].Colliders).Any(d => d.Contains("\"M42\"", StringComparison.Ordinal)) && ColliderDumps(cell[0].Colliders).Any(d => d.Contains("\"Y1\"", StringComparison.Ordinal)),
            "the dumps reach into the box transforms and the wall heights");
        Require(AssertTraceMatches(module, ctx, plans.Single(p => p.Key == "t:dorm").Tile(LodLevel.L2), LodLevel.L2).Count == 5, "all five buildings share the L2 cell");
        Require(AssertTraceMatches(module, ctx, home, LodLevel.L0, traced).Count == 2 && AssertTraceMatches(module, ctx, new TileKey(7, 7), LodLevel.L0, traced).Count == 0, "an empty cell has no source");

        var ids = new List<string>();
        foreach (var lod in Levels)
        {
            var levelSources = module.TraceSources(ctx, lod).ToArray();
            foreach (var tile in mine.Select(p => p.Tile(lod)).Distinct())
            {
                var traces = AssertTraceMatches(module, ctx, tile, lod, levelSources);
                Require(traces.Count == mine.Count(p => p.Tile(lod) == tile), $"{lod}: one source per building of the cell");
                if (lod == LodLevel.L0) ids.AddRange(traces.Select(t => t.SourceId));
            }
        }
        Require(ids.Count == 5 && ids.Distinct(StringComparer.Ordinal).Count() == 5, "every building has its own source ID over the map");

        // The IDs and what they stand for do not depend on the order the features were declared in.
        var shuffled = Prepared(Ctx(World(new Entrances(door), new Falling(-1f, 0.9f), [.. Enumerable.Reverse(features)]), 2u, claims: ["t:claimed"]));
        {
            var a = traced.ToDictionary(t => t.SourceId);
            var b = module.TraceSources(shuffled, LodLevel.L0).ToDictionary(t => t.SourceId);
            Require(a.Keys.Order(StringComparer.Ordinal).SequenceEqual(b.Keys.Order(StringComparer.Ordinal)) &&
                a.All(p => p.Value.Output.Meshes.Count == b[p.Key].Output.Meshes.Count && p.Value.Output.Meshes.Zip(b[p.Key].Output.Meshes).All(m => SameBundle(m.First, m.Second)) &&
                    ColliderDumps(p.Value.Colliders).SequenceEqual(ColliderDumps(b[p.Key].Colliders))), "same sources, same geometry");
        }

        // Enumeration reads the map's one window allocation; an empty world needs none (as Build).
        Expect<InvalidOperationException>(() => module.TraceSources(Ctx(world, 2u), LodLevel.L0).ToArray(), "no snapshot, no windows");
        Require(!module.TraceSources(Ctx(World(), 2u), LodLevel.L0).Any(), "nothing to number in an empty world");
    }

    [TestCase]
    public void TraceSourcesReproduceBuildAndCollideOnTheRealCampus()
    {
        var real = RealCampus();
        var ctx = Prepared(new BuildContext { Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog, World = real, Map = TestMap, WorldSeed = 1u, ClaimedBy = RealClaims(real) });
        var module = new BuildingsModule();
        var mine = BuildingSet.For(real, 1u).Plans.Where(p => BuildingsModule.Unclaimed(ctx, p)).ToArray();
        Require(mine.Select(p => $"building/{p.Id.Value}/{p.Key}").Distinct(StringComparer.Ordinal).Count() == mine.Length, "a source ID per building part over the map");
        foreach (var lod in new[] { LodLevel.L0, LodLevel.L2 })
        {
            // The busiest cell of the level (most buildings, then the lowest cell).
            var busiest = mine.GroupBy(p => p.Tile(lod)).OrderByDescending(g => g.Count()).ThenBy(g => g.Key.X).ThenBy(g => g.Key.Z).First();
            var traces = AssertTraceMatches(module, ctx, busiest.Key, lod, module.TraceSources(ctx, lod).ToArray());
            Require(traces.Count == busiest.Count() && (lod != LodLevel.L0 || traces.Count > 1), $"{lod}: every unclaimed building of the busiest cell, {traces.Count}");
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
        Require(traces.Count == plans.Length && traces.All(t => plans.Any(p => p.Id == t.Feature && t.SourceId == $"building/{p.Id.Value}/{p.Key}")), $"{cell}: actual source feature and stable ID");
        Require(traces.Select(t => t.SourceId).Distinct(StringComparer.Ordinal).Count() == traces.Count, $"{cell}: source IDs are unique");
        var meshes = traces.SelectMany(t => t.Output.Meshes).ToArray();
        Require(meshes.Length == built.Meshes.Count && meshes.Zip(built.Meshes).All(m => SameBundle(m.First, m.Second)), $"{cell}: the traced meshes are the Build meshes, in order");
        Require(traces.SelectMany(t => t.Output.Parts).SequenceEqual(built.Parts) && traces.SelectMany(t => t.Output.Instances).SequenceEqual(built.Instances),
            $"{cell}: the traced parts and instances are the Build ones");
        foreach (var trace in traces)
        {
            var plan = plans.Single(p => trace.SourceId == $"building/{p.Id.Value}/{p.Key}");
            var footprint = trace.OccupiedContours.Single();
            Require(footprint.Outer.SequenceEqual(plan.SkinOuter.Points) &&
                footprint.Holes.Count == plan.SkinHoles.Count &&
                footprint.Holes.Zip(plan.SkinHoles).All(h => h.First.SequenceEqual(h.Second.Points)),
                $"{cell}: source footprint follows the generated wall skin, including courtyard holes");
            Require(!ReferenceEquals(footprint.Outer, plan.SkinOuter.Points) &&
                footprint.Holes.Zip(plan.SkinHoles).All(h => !ReferenceEquals(h.First, h.Second.Points)),
                $"{cell}: source footprint is a snapshot of the plan");
        }
        var cells = lod == LodLevel.L0 ? new[] { tile } : plans.Select(p => p.Tile(LodLevel.L0)).Distinct().ToArray();
        var expected = ColliderDumps(cells.SelectMany(c => module.Collide(ctx, c)));
        var actual = ColliderDumps(traces.SelectMany(t => t.Colliders));
        if (lod != LodLevel.L0)
        {
            expected.Sort(StringComparer.Ordinal);
            actual.Sort(StringComparer.Ordinal);
        }
        Require(actual.SequenceEqual(expected), $"{cell}: the traced colliders are the Collide colliders ({actual.Count} against {expected.Count})");
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
