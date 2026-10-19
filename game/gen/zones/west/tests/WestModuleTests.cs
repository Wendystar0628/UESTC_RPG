using Campus.Gen.Zones.Common;
using Campus.Gen.Zones.Common.Tests;
using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Tests;
using Campus.Contracts.Maps;
using Campus.Gen.World;
using Campus.Gen.Zones.West.Four;
using Campus.Gen.Zones.West.Framework;
using Campus.Gen.Zones.West.Motto;
using Campus.Gen.Zones.West.Six;
using GdUnit4;
using F4 = Campus.Gen.Zones.West.WestTune.Four;
using I = Campus.Gen.Zones.West.WestTune.Innovation;
using M = Campus.Gen.Zones.West.WestTune.Motto;
using S = Campus.Gen.Zones.West.WestTune.Six;

namespace Campus.Gen.Zones.West.Tests;
using static GdUnit4.Assertions;

[TestSuite]
public sealed class WestModuleTests
{
    private static readonly LodLevel[] Levels = [LodLevel.L0, LodLevel.L1, LodLevel.L2, LodLevel.L3, LodLevel.L4];

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void WholeSourcesReproduceProductionAndKeepClaimedPlans()
    {
        var ctx = Context();
        var module = new WestModule();
        foreach (var lod in new[] { LodLevel.L0, LodLevel.L2 })
        {
            var traces = module.TraceSources(ctx, lod).ToArray();
            AssertThat(traces.Select(t => t.SourceId).Distinct(StringComparer.Ordinal).Count() == traces.Length).OverrideFailureMessage("source IDs are unique").IsTrue();
            foreach (var claim in module.Claims.OfType<FeatureClaim>())
                AssertThat(traces.Any(t => t.Feature == claim.Feature && t.Output.Meshes.Count > 0 && t.Colliders.Count > 0 && t.OccupiedContours.Count > 0)).OverrideFailureMessage($"{lod}: claimed source {claim.Feature} carries production output, collision and solid contours").IsTrue();
            var size = TileGrid.Edge(lod);
            var cells = ZoneTestCells.Items(module, ctx, lod).Select(i => new TileKey((int)MathF.Floor(i.Anchor.X / size), (int)MathF.Floor(i.Anchor.Y / size))).Distinct();
            var built = cells.Select(t => module.Build(ctx, t, lod)).ToArray();
            AssertThat(Triangles(traces.SelectMany(t => t.Output.Meshes)).SequenceEqual(Triangles(built.SelectMany(o => o.Meshes)))).OverrideFailureMessage($"{lod}: source triangles reproduce tiled production").IsTrue();
            AssertThat(traces.SelectMany(t => t.Output.Parts).OrderBy(p => p.ToString(), StringComparer.Ordinal)
                .SequenceEqual(built.SelectMany(o => o.Parts).OrderBy(p => p.ToString(), StringComparer.Ordinal))).OverrideFailureMessage("source parts reproduce production").IsTrue();
            AssertThat(traces.SelectMany(t => t.Output.Instances).SequenceEqual(built.SelectMany(o => o.Instances))).OverrideFailureMessage("source instances reproduce production").IsTrue();
            string Dump(Collider collider) => System.Text.Json.JsonSerializer.Serialize<Collider>(collider, ContractJson.Options);
            AssertThat(traces.SelectMany(t => t.Colliders).Select(Dump).Order(StringComparer.Ordinal)
                .SequenceEqual(module.Tiles(ctx).SelectMany(t => module.Collide(ctx, t)).Select(Dump).Order(StringComparer.Ordinal))).OverrideFailureMessage("source colliders reproduce tiled production").IsTrue();
            var c = traces.Single(t => t.Feature == new FeatureId(S.EntityC));
            AssertThat(c.OccupiedContours.First().Holes.Single().SequenceEqual(SixSite.Plan(ctx.World, S.EntityC, S.PartC).Holes.Single())).OverrideFailureMessage("C keeps its own courtyard hole").IsTrue();
            foreach (var block in SixSite.Blocks.Where(b => b.Entity != S.EntityC))
                AssertThat(traces.Single(t => t.Feature == new FeatureId(block.Entity)).OccupiedContours.Single().Outer
                    .SequenceEqual(SixSite.Plan(ctx.World, block.Entity, block.Part).Outer)).OverrideFailureMessage("A and B carry only their own generated plan").IsTrue();
        }

        static IEnumerable<string> Triangles(IEnumerable<GeometryBundle> meshes)
        {
            var triangles = new List<string>();
            foreach (var mesh in meshes)
                for (var i = 0; i < mesh.Index.Length; i += 3)
                {
                    string Vertex(int v) => string.Join(",", mesh.Position.Skip(v * 3).Take(3).Concat(mesh.Normal.Skip(v * 3).Take(3)).Select(BitConverter.SingleToInt32Bits));
                    triangles.Add($"{mesh.Material}/{Vertex(mesh.Index[i])}/{Vertex(mesh.Index[i + 1])}/{Vertex(mesh.Index[i + 2])}");
                }
            return triangles.Order(StringComparer.Ordinal);
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void AuthoredStoreysAndTheA10PlazaLoad()
    {
        var world = World();
        using var authored = System.Text.Json.JsonDocument.Parse(RealMapFixture.ReadFiles()["world/west.json"]);
        foreach (var record in authored.RootElement.GetProperty("features").EnumerateArray())
        {
            var feature = world.Features[new(record.GetProperty("id").GetString()!)];
            float? height = record.TryGetProperty("height", out var authoredHeight) ? authoredHeight.GetSingle() : null;
            int? storeys = record.TryGetProperty("storeys", out var authoredStoreys) ? authoredStoreys.GetInt32() : null;
            AssertThat(feature.Height == height && feature.Storeys == storeys).OverrideFailureMessage("registered height and storeys apply to the record").IsTrue();
            if (!record.TryGetProperty("parts", out var parts)) continue;
            AssertThat(parts.EnumerateArray().All(raw =>
            {
                var part = feature.Parts.Single(part => part.SourceId == raw.GetProperty("id").GetString());
                float? partHeight = raw.TryGetProperty("height", out var h) ? h.GetSingle() : null;
                int? partStoreys = raw.TryGetProperty("storeys", out var n) ? n.GetInt32() : null;
                return part.Height == partHeight && part.Storeys == partStoreys;
            })).OverrideFailureMessage("registered height and storeys apply to every part").IsTrue();
        }
        // 研究院大楼 keeps its per-part 4–7 storeys: one aggregate floor count would flatten them (contract-31).
        var institute = world.Features[new(WestIds.Relation11031411.Entity)].Parts.Where(p => p.Storeys is not null).Select(p => p.Storeys!.Value).ToArray();
        AssertThat(institute.Distinct().Count() > 1).OverrideFailureMessage("研究院大楼 keeps distinct per-part floor counts").IsTrue();
        AssertThat(!world.Features.ContainsKey(new(WestIds.Way779496166.Entity))).OverrideFailureMessage("六号's duplicate C shell stays hidden").IsTrue();
        var plaza = world.Features[new(WestIds.A10Plaza)];
        AssertThat(plaza.Kind.Value == "road" && plaza.Geometry is PolygonGeometry).OverrideFailureMessage("the A10 plaza is registered as paving").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void ClaimsCoverEveryPartOfSix()
    {
        var world = World();
        var claims = new WestModule().Claims.OfType<FeatureClaim>().Select(c => c.Feature.Value).OrderBy(s => s, StringComparer.Ordinal).ToArray();
        AssertThat(claims.SequenceEqual(new[] { S.EntityC, S.EntityA, S.EntityB, WestTune.Shelter.Entity }.OrderBy(s => s, StringComparer.Ordinal))).OverrideFailureMessage($"六号's three entities and the 19c bus shelter are claimed: {string.Join(",", claims)}").IsTrue();
        string[] Parts(string id) => world.Features[new(id)].Parts.Select(p => p.SourceId).OrderBy(s => s, StringComparer.Ordinal).ToArray();
        AssertThat(Parts(S.EntityC).SequenceEqual(new[] { S.PartC, S.LinkBC, S.LinkAB, S.Tower }.OrderBy(s => s, StringComparer.Ordinal))).OverrideFailureMessage("the C entity holds the block, both links and the tower").IsTrue();
        AssertThat(Parts(S.EntityA).SequenceEqual([S.PartA]) && Parts(S.EntityB).SequenceEqual([S.PartB])).OverrideFailureMessage("A and B are single blocks").IsTrue();
        AssertThat(SixSite.Plan(world, S.EntityC, S.PartC).Holes.Count == 1).OverrideFailureMessage("C keeps its courtyard").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void BlocksStandAtTheirHeightsWithATallestA()
    {
        var world = World();
        float Reach(string key) => S.BaseHeight + SixSite.Style(SixSite.Blocks.Single(k => k.Key == key).Upper(world)).Top;
        float a = Reach("a"), b = Reach("b"), c = Reach("c");
        AssertThat(MathF.Abs(a - world.Features[new(S.EntityA)].Height!.Value) < 12 * TestTolerance.Layout).OverrideFailureMessage($"A reaches its data height, got {a}").IsTrue();
        AssertThat(MathF.Abs(b - world.Features[new(S.EntityB)].Height!.Value) < 12 * TestTolerance.Layout).OverrideFailureMessage($"B reaches its data height, got {b}").IsTrue();
        AssertThat(a > b && a > c + S.SignRise).OverrideFailureMessage("A stays the tallest, above C's crown sign").IsTrue();
        // The glass base sits back from the stone line by the recess on every face.
        foreach (var k in SixSite.Blocks)
        {
            var plan = SixSite.Plan(world, k.Entity, k.Part);
            var inner = SixSite.BasePlan(plan);
            AssertThat(inner.Outer.Count == plan.Outer.Count && inner.Holes.Count == plan.Holes.Count).OverrideFailureMessage($"{k.Key}: the base keeps the plan's rings").IsTrue();
            AssertThat(Math.Abs(PlanGeometry.SignedArea(inner.Outer)) < Math.Abs(PlanGeometry.SignedArea(plan.Outer))).OverrideFailureMessage($"{k.Key}: the base glass is set back inside the stone").IsTrue();
            AssertThat(inner.Outer.All(p => PlanGeometry.InsideByCrossings(plan.Outer, p))).OverrideFailureMessage($"{k.Key}: every base corner lies under the stone").IsTrue();
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void EveryLevelIsValidAndEveryItemLandsInOneCell()
    {
        var ctx = Context();
        var module = new WestModule();
        AssertThat(module.Tiles(ctx).Any()).OverrideFailureMessage("the module occupies cells").IsTrue();
        foreach (var lod in Levels)
        {
            var items = ZoneTestCells.Items(module, ctx, lod);
            AssertThat(items.Count > 0).OverrideFailureMessage($"{lod} has items").IsTrue();
            var size = TileGrid.Edge(lod);
            var cells = items.Select(i => ((int)MathF.Floor(i.Anchor.X / size), (int)MathF.Floor(i.Anchor.Y / size))).Distinct().ToArray();
            var meshes = 0;
            var parts = 0;
            foreach (var (x, z) in lod == LodLevel.L4 ? [(0, 0)] : cells)
            {
                var output = module.Build(ctx, new(x, z), lod);
                foreach (var mesh in output.Meshes)
                {
                    mesh.ValidateVertexData();
                    AssertThat(mesh.Position.Length / 3 <= 65536).OverrideFailureMessage("bundle vertex limit").IsTrue();
                    meshes++;
                }
                parts += output.Parts.Count;
            }
            AssertThat(meshes > 0).OverrideFailureMessage($"{lod} emits meshes").IsTrue();
            AssertThat(items.Count(i => i.Part is not null) == parts).OverrideFailureMessage($"{lod}: every placed part is emitted exactly once").IsTrue();
            string[] kit = [Contracts.Parts.PartCatalog.EntranceMat, Contracts.Parts.PartCatalog.OpenSign, Contracts.Parts.PartCatalog.EntranceLamp];
            AssertThat(!items.Any(i => i.Part is { } p && kit.Contains(p.Part.Value))).OverrideFailureMessage($"{lod}: no entrance kit placed by the zone (WP-13's)").IsTrue();
        }
    }

    [TestCase, TestCategory(TestTag.Determinism)]
    [TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void BuildIsDeterministic()
    {
        var ctx = Context();
        var cells = ZoneTestCells.Heaviest(new WestModule(WestSites.All.ToArray()), ctx);
        var a = Bytes(ctx, cells);
        var b = Bytes(Context(), cells);
        AssertThat(a.SequenceEqual(b)).OverrideFailureMessage("two worlds give the same bytes").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void EastDoorIsOpenAndTheOtherEntrancesAreShut()
    {
        var ctx = Context();
        var module = new WestModule();
        var colliders = module.Tiles(ctx).SelectMany(t => module.Collide(ctx, t)).ToArray();
        AssertThat(colliders.All(c => c.Foot != Contracts.Audio.FootMat.Auto)).OverrideFailureMessage("every collider fills its foot material").IsTrue();
        // Nothing at walking height crosses the doorway between the vestibule cheeks.
        var outward = Angles.Heading(77f);
        for (var d = -S.Recess - 1f; d <= 0.5f; d += 0.25f)
        {
            var p = SixSite.EastDoor(ctx.World) + outward * d;
            AssertThat(!colliders.OfType<WallCollider>().Any(w => w.Y0 < 1.0f && w.Y1 > 1.0f && Near(w.A, w.B, p, w.HalfT + TestTolerance.Layout))).OverrideFailureMessage($"the east doorway is open {d:0.00} m along its axis").IsTrue();
        }
        // 六号's shut doors (19b's entrance boxes and portal add their own DoorClosed fronts elsewhere in the zone).
        var shut = colliders.OfType<WallCollider>().Where(w => w.Tag == SurfaceTag.DoorClosed && (w.A.X + w.B.X) * 0.5f < -583f).ToArray();
        AssertThat(shut.Length == S.ShutDoors(ctx.World).Length).OverrideFailureMessage($"one DoorClosed segment per shut entrance, got {shut.Length}").IsTrue();
        foreach (var door in S.ShutDoors(ctx.World))
            AssertThat(shut.Any(w => Vector2.Distance((w.A + w.B) * 0.5f, door) < S.Recess + 0.6f)).OverrideFailureMessage($"shut door {door} is DoorClosed").IsTrue();
        // The motto stone is one unclimbable box.
        AssertThat(colliders.OfType<BoxCollider>().Any(b => b.Tag == SurfaceTag.Prop && MathF.Abs(b.Half.Y * 2 - M.Height) < TestTolerance.Position)).OverrideFailureMessage("the motto stone collides as a 2 m box").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void EveryFacadeClaimDeclaresItsWindowGrids()
    {
        var world = World();
        var layouts = new WestModule().DescribeWindowLayouts(world, 1).ToDictionary(l => l.Feature.Value, l => l.Grids, StringComparer.Ordinal);
        AssertThat(layouts.Keys.ToHashSet(StringComparer.Ordinal).SetEquals(new WestModule().Claims.OfType<FeatureClaim>()
            .Where(claim => (claim.Aspects & Aspects.Facade) != 0).Select(claim => claim.Feature.Value))).OverrideFailureMessage("every claimed facade declares a layout").IsTrue();
        AssertThat(layouts[WestTune.Shelter.Entity].Count == 0).OverrideFailureMessage("the bus shelter declares an empty window layout").IsTrue();
        int Upper(string key) => SixSite.Blocks.Single(k => k.Key == key).Upper(world);
        AssertThat(layouts[S.EntityA].Single().Key == "a" && layouts[S.EntityA].Single().Rows == Upper("a")).OverrideFailureMessage("A: one grid over its upper floors").IsTrue();
        AssertThat(layouts[S.EntityB].Single().Rows == Upper("b") && layouts[S.EntityC].Single().Rows == S.UpperC).OverrideFailureMessage("B and C: one grid each").IsTrue();
        AssertThat(layouts.Values.SelectMany(g => g).All(g => g.Columns > 0)).OverrideFailureMessage("every grid has cells").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void NumberedGlassMatchesTheDeclaredWindows()
    {
        var world = World();
        var module = new WestModule();
        var specs = module.DescribeWindowLayouts(world, 1).SelectMany(l => l.Grids.Select(g => (l.Feature, g)))
            .Prepend((new FeatureId("a-earlier"), new WindowGridSpec("block", 37, 3)))
            .OrderBy(p => p.Item1.Value, StringComparer.Ordinal).ThenBy(p => p.Item2.Key, StringComparer.Ordinal);
        var allocations = new List<WindowAllocation>();
        var next = 0;
        foreach (var (feature, grid) in specs)
        {
            allocations.Add(new(feature, grid.Key, next, grid.Columns, grid.Rows));
            next += grid.Columns * grid.Rows;
        }
        var ctx = new BuildContext { GlobalInputs = TextInputs(), Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog, World = world, Map = RealMapFixture.Entry().Descriptor, WorldSeed = 1, WindowNumbering = new(RealMapFixture.Entry().Descriptor.Id, new string('a', 64), allocations) };
        var metadata = module.DescribeWindows(ctx);
        var declared = new List<int>();
        foreach (var f in metadata.Facades)
            for (var r = 0; r < f.Rows; r++)
                for (var c = 0; c < f.Columns; c++)
                    if (!f.Skip.Contains(r * f.Columns + c)) declared.Add(f.BaseId + r * f.TotalColumns + f.ColumnStart + c);
        AssertThat(declared.Count == declared.Distinct().Count() && declared.Count > 0).OverrideFailureMessage("every window is declared once").IsTrue();
        AssertThat(declared.All(id => allocations.Any(a => id >= a.BaseId && id < a.BaseId + a.Count && a.Feature.Value != "a-earlier"))).OverrideFailureMessage("declared IDs lie in the zone's grids").IsTrue();
        var ids = new HashSet<int>();
        var size = 64;
        foreach (var (x, z) in ZoneTestCells.Items(module, ctx, LodLevel.L0).Select(i => ((int)MathF.Floor(i.Anchor.X / size), (int)MathF.Floor(i.Anchor.Y / size))).Distinct())
            foreach (var mesh in module.Build(ctx, new(x, z), LodLevel.L0).Meshes.Where(m => m.Uv2 is not null && m.Material.Spec.Kind != ToonKind.SceneText))
                for (var v = 0; v < mesh.Uv2!.Length / 2; v++)
                {
                    var columns = (int)MathF.Floor(mesh.Uv2[2 * v + 1] / 32f);
                    ids.Add((int)mesh.Uv2[2 * v] + (int)MathF.Floor(mesh.Uv![2 * v + 1]) * columns + (int)MathF.Floor(mesh.Uv[2 * v]));
                }
        AssertThat(ids.SetEquals(declared)).OverrideFailureMessage($"L0 glass carries exactly the declared windows ({ids.Count} vs {declared.Count})").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void WindowTablePublishesWithTheWestDeclarations()
    {
        var world = World();
        var module = new WestModule();
        var claimed = module.Claims.OfType<FeatureClaim>().Where(c => (c.Aspects & Aspects.Facade) != 0).Select(c => c.Feature).ToHashSet();
        WpId? Owner(FeatureId f, Aspects a) => claimed.Contains(f) ? new WpId("WP-19") : null;
        var prepass = new BuildContext { GlobalInputs = TextInputs(), Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog, World = world, Map = RealMapFixture.Entry().Descriptor, WorldSeed = 1, ClaimedBy = Owner };
        var layouts = module.DescribeWindowLayouts(world, 1);
        var snapshot = new global::Campus.Gen.Buildings.BuildingsModule().PrepareWindowNumbering(prepass, layouts);
        var ours = snapshot.Allocations.Where(a => claimed.Contains(a.Feature)).ToArray();
        AssertThat(ours.Length == layouts.Sum(l => l.Grids.Count) && ours.All(a => a.Columns > 0 && a.Rows > 0)).OverrideFailureMessage("every west grid is allocated with cells").IsTrue();
        AssertThat(snapshot.WindowCount <= WindowNumberingSnapshot.Capacity).OverrideFailureMessage($"the table fits ({snapshot.WindowCount})").IsTrue();
        var metadata = module.DescribeWindows(new BuildContext { GlobalInputs = TextInputs(), Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog, World = world, Map = RealMapFixture.Entry().Descriptor, WorldSeed = 1, ClaimedBy = Owner, WindowNumbering = snapshot });
        AssertThat(metadata.Facades.Count > 0).OverrideFailureMessage("the west zone declares facade runs").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void TerrainOverlayParsesAndKeepsTheA10PlazaAndTheGatesLevel()
    {
        var file = TerrainOverlayJson.Parse(File.ReadAllBytes(Path.Combine(RealMapFixture.DataDirectory(), "world", "west.terrain.json")));
        AssertThat(file.Zone.Value == "west" && file.Owner.Value == "WP-19").OverrideFailureMessage("the file belongs to WP-19's west zone").IsTrue();
        AssertThat(file.Ops.All(o => o.Id.StartsWith(WestIds.TerrainPrefix, StringComparison.Ordinal) && o.Zone.Value == "west")).OverrideFailureMessage("ids and zones").IsTrue();
        var low = file.Ops.OfType<TerrainPlatformOp>().Single(o => o.Id == WestIds.WetlandLow);
        var plaza = ((PolygonGeometry)World().Features[new(WestIds.A10Plaza)].Geometry).Rings.Outer;
        AssertThat(low.Rings.Holes.Count == 1 && plaza.All(p => PlanGeometry.InsideByCrossings(low.Rings.Holes[0], p))).OverrideFailureMessage("the low ground leaves a dry hole round the plaza").IsTrue();
        var stream = file.Ops.OfType<TerrainChannelOp>().Single(o => o.Id == WestIds.WetlandStream);
        var canal = file.Ops.OfType<TerrainChannelOp>().Single(o => o.Id == WestIds.HongguangNorth);
        AssertThat(stream.WaterY > canal.WaterY).OverrideFailureMessage("the wetland stream stays above the canal").IsTrue();
        // The west gates' 20 m zero bands (MAP-QSH-TERRAIN): no authored point comes near them.
        var terrainWorld = World();
        var boundary = terrainWorld.Boundary.Outer;
        var gates = new[]
        {
            Campus.Gen.Zones.DesignBoundary.BoundaryFeatureIds.WestGateRoad.Source,
            Campus.Gen.Zones.DesignBoundary.BoundaryFeatureIds.West2GateRoad.Source,
            Campus.Gen.Zones.DesignBoundary.BoundaryFeatureIds.NorthwestGateRoad.Source,
        }.Select(id =>
        {
            var road = terrainWorld.Network.Roads.Single(item => item.Id.Value == id).Line.Points;
            return Enumerable.Range(0, road.Count - 1)
                .SelectMany(i => Enumerable.Range(0, boundary.Count).Select(j =>
                    PlanLines.SegmentIntersection(road[i], road[i + 1], boundary[j], boundary[(j + 1) % boundary.Count], 1e-6f)))
                .First(point => point.HasValue)!.Value;
        }).ToArray();
        var points = file.Ops.SelectMany(Points).ToArray();
        AssertThat(points.All(p => gates.All(g => Vector2.Distance(p, g) > 60f))).OverrideFailureMessage("every terrain operation stays out of the gates' zero bands").IsTrue();

        static IEnumerable<Vector2> Points(TerrainOp op) => op switch
        {
            TerrainPlatformOp p => p.Rings.Outer.Concat(p.Rings.Holes.SelectMany(h => h)),
            TerrainSlopeOp s when s.Geometry is PolylineGeometry l => l.Line.Points,
            TerrainChannelOp c => c.Line?.Points ?? [],
            _ => [],
        };
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void OtherWestBuildingsGoToWp14WithTheirFamilies()
    {
        var world = World();
        foreach (var (id, family) in new[]
        {
            (WestIds.Relation11334255.Entity, "research"), (WestIds.Relation11665641.Entity, "research"), (WestIds.Relation11334253.Entity, "research"),
            (WestIds.Relation8426842.Entity, "research"), (WestIds.Relation11031411.Entity, "research"), (WestIds.Way603368216.Entity, "research"),
            (WestIds.Relation11032065.Entity, "research"), (WestIds.Way795677483.Entity, "research"), (WestIds.Way795677482.Entity, "research"),
            (WestIds.Way570145228.Entity, "office"), (WestIds.Way779496151.Entity, "research"), (WestIds.Way779496154.Entity, "research"),
        })
            AssertThat(world.Features[new(id)].Family?.Value == family).OverrideFailureMessage($"{id} is styled {family} for WP-14").IsTrue();
        foreach (var id in new[] { F4.EntityA, F4.EntityB, F4.EntityC, WestIds.Relation8426842.Entity })
            AssertThat(world.Features[new(id)].Facade?.Value == "research.stoneFrame").OverrideFailureMessage($"{id} takes the stone-and-glass preset").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void FourGlassBoxesSitOnTheirStoneLinesAndAreShut()
    {
        var world = World();
        var parts = new Dictionary<string, string> { [F4.EntityA] = WestIds.Relation11334255.Part, [F4.EntityB] = WestIds.Relation11665641.Part, [F4.EntityC] = WestIds.Relation11334252.Part };
        foreach (var box in F4.Boxes(world))
        {
            var ring = Six.SixSite.Plan(world, box.Entity, parts[box.Entity]).Outer;
            var onLine = Enumerable.Range(0, ring.Count).Any(i => Near(ring[i], ring[(i + 1) % ring.Count], box.Door, 0.3f));
            AssertThat(onLine).OverrideFailureMessage($"四号 {box.Key}: the door lies on the block's stone line").IsTrue();
            var out1 = Angles.Heading(box.Facing);
            AssertThat(!PlanGeometry.InsideByCrossings(ring, box.Door + out1 * 2f) && PlanGeometry.InsideByCrossings(ring, box.Door - out1 * 2f)).OverrideFailureMessage($"四号 {box.Key}: the box faces out of the block").IsTrue();
        }
        var ctx = Context();
        var module = new WestModule();
        var colliders = module.Tiles(ctx).SelectMany(t => module.Collide(ctx, t)).ToArray();
        foreach (var box in F4.Boxes(world))
            AssertThat(colliders.OfType<WallCollider>().Any(w => w.Tag == SurfaceTag.DoorClosed && Vector2.Distance((w.A + w.B) * 0.5f, box.Door) < F4.GlassDepth + 1f)).OverrideFailureMessage($"四号 {box.Key}: the glass box front is DoorClosed").IsTrue();
        AssertThat(colliders.OfType<WallCollider>().Any(w => w.Tag == SurfaceTag.DoorClosed && Vector2.Distance((w.A + w.B) * 0.5f, I.MainDoor(world)) < I.PortalDepth + 0.5f)).OverrideFailureMessage("创新中心: the main portal is DoorClosed").IsTrue();
        // The seat wall runs inside the lawn, clear of the radial paths' gaps.
        foreach (var arc in F4.SeatArcs(world))
            AssertThat(FourSite.SeatLine(world, arc).All(p => Vector2.Distance(p, F4.LawnCentre(world)) < arc.Max(q => Vector2.Distance(q, F4.LawnCentre(world))))).OverrideFailureMessage("the seat wall is inset into the lawn").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void InnovationShaftsBridgeWingAndBar()
    {
        var world = World();
        var feature = world.Features[new(I.Entity)];
        var plans = feature.Parts.Select(p => p.Geometry).OfType<PolygonGeometry>().Select(g => g.Rings.Outer)
            .Concat(feature.Parts.Select(p => p.Geometry).OfType<MultiPolygonGeometry>().SelectMany(m => m.Polygons.Select(r => r.Outer))).ToArray();
        foreach (var (centre, length) in I.Shafts(world))
        {
            AssertThat(plans.Any(r => PlanGeometry.InsideByCrossings(r, centre))).OverrideFailureMessage($"shaft {centre} stands on a link of 创新中心").IsTrue();
            var ends = new[] { centre + I.WingAxis * (length * 0.5f + 1.5f), centre - I.WingAxis * (length * 0.5f + 1.5f) };
            AssertThat(ends.All(e => plans.Any(r => PlanGeometry.InsideByCrossings(r, e)))).OverrideFailureMessage($"shaft {centre} reaches a building at both ends").IsTrue();
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void FourFanLawnIsAShallowDish()
    {
        var world = World();
        var file = TerrainOverlayJson.Parse(File.ReadAllBytes(Path.Combine(RealMapFixture.DataDirectory(), "world", "west.terrain.json")));
        var dish = file.Ops.OfType<TerrainBasinOp>().Single(o => o.Id == WestIds.FourFanLawn);
        AssertThat(dish.Height > 0 && dish.Shape is TerrainCircle { Radius: > 0 } c && Vector2.Distance(c.Center, F4.LawnCentre(world)) < TestTolerance.Position).OverrideFailureMessage("the fan lawn is a shallow circular dish centred on the authored layout").IsTrue();
    }

    // ---- helpers ----

    private static bool Near(Vector2 a, Vector2 b, Vector2 p, float radius) =>
        PlanLines.SegmentDistanceSquared(p, a, b) < radius * radius;

    private static byte[] Bytes(BuildContext ctx, IEnumerable<TileKey> cells)
    {
        var module = new WestModule(WestSites.All.ToArray());
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        foreach (var tile in cells)
        {
            var output = module.Build(ctx, tile, LodLevel.L0);
            foreach (var mesh in output.Meshes)
            {
                foreach (var v in mesh.Position) writer.Write(v);
                foreach (var v in mesh.Normal) writer.Write(v);
                foreach (var v in mesh.Index) writer.Write(v);
            }
            foreach (var part in output.Parts) writer.Write(part.Part.Value + part.At.ToString());
        }
        return stream.ToArray();
    }

    private static BuildContext Context() => new() { GlobalInputs = TextInputs(), Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog, World = World(), Map = null!, WorldSeed = 1 };

    private static BuildInputSnapshot TextInputs() => BuildInputSnapshot.Freeze(new Dictionary<string, ReadOnlyMemory<byte>>
    {
        [SceneTextInputs.AtlasKey] = File.ReadAllBytes(RepositoryFixture.FilePath("game", "data", "scene-text", "atlas.json"))
    }, "fixture");

    private static IWorldModel World() => RealMapFixture.Load().World;

}
