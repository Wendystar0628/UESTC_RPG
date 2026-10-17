using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Gen.World;
using Campus.Gen.Zones.West.Four;
using Campus.Gen.Zones.West.Framework;
using Campus.Gen.Zones.West.Motto;
using Campus.Gen.Zones.West.Six;
using GdUnit4;
using A = Campus.Gen.Zones.West.WestTune.A10;
using F4 = Campus.Gen.Zones.West.WestTune.Four;
using I = Campus.Gen.Zones.West.WestTune.Innovation;
using M = Campus.Gen.Zones.West.WestTune.Motto;
using S = Campus.Gen.Zones.West.WestTune.Six;

namespace Campus.Gen.Zones.West.Tests;

[TestSuite]
public sealed class WestModuleTests
{
    private static readonly LodLevel[] Levels = [LodLevel.L0, LodLevel.L1, LodLevel.L2, LodLevel.L3, LodLevel.L4];

    [TestCase]
    public void OverlayCorrectsStoreysAndRegistersTheA10Plaza()
    {
        var world = World();
        foreach (var (id, height, storeys) in new[] { ("qsh:relation:11334253", 19.5f, 5), ("qsh:relation:8426842", 19.5f, 5), ("qsh:way:682336066", 11.7f, 3) })
        {
            var f = world.Features[new(id)];
            Require(f.Height == height && f.Storeys == storeys, $"{id} raised to {storeys} storeys");
            Require(f.Parts.All(p => p.Height == height && p.Storeys == storeys), $"every part of {id} follows its HeightOp");
        }
        // 研究院大楼 keeps its per-part 4–7 storeys: an entity-wide HeightOp would flatten them (contract-31).
        var institute = world.Features[new("qsh:relation:11031411")].Parts.Where(p => p.Storeys is not null).Select(p => p.Storeys!.Value).ToArray();
        Require(institute.Min() == 4 && institute.Max() == 7, "研究院大楼 parts stay 4–7 storeys");
        Require(!world.Features.ContainsKey(new("qsh:way:779496166")), "六号's duplicate C shell stays hidden");
        var plaza = world.Features[new("design:west-a10-plaza")];
        Require(plaza.Kind.Value == "road" && plaza.Geometry is PolygonGeometry g && g.Rings.Outer.Zip(A.Plaza).All(p => Vector2.Distance(p.First, p.Second) < 0.01f),
            "the A10 plaza is registered as the tune's polygon");
        Require(PlanMath.Inside(A.Plaza, A.Post), "the A10 post stands on its plaza");
    }

    [TestCase]
    public void ClaimsCoverEveryPartOfSix()
    {
        var world = World();
        var claims = new WestModule().Claims.OfType<FeatureClaim>().Select(c => c.Feature.Value).OrderBy(s => s, StringComparer.Ordinal).ToArray();
        Require(claims.SequenceEqual(new[] { S.EntityC, S.EntityA, S.EntityB, WestTune.Shelter.Entity }.OrderBy(s => s, StringComparer.Ordinal)),
            $"六号's three entities and the 19c bus shelter are claimed: {string.Join(",", claims)}");
        string[] Parts(string id) => world.Features[new(id)].Parts.Select(p => p.SourceId).OrderBy(s => s, StringComparer.Ordinal).ToArray();
        Require(Parts(S.EntityC).SequenceEqual(new[] { S.PartC, S.LinkBC, S.LinkAB, S.Tower }.OrderBy(s => s, StringComparer.Ordinal)), "the C entity holds the block, both links and the tower");
        Require(Parts(S.EntityA).SequenceEqual([S.PartA]) && Parts(S.EntityB).SequenceEqual([S.PartB]), "A and B are single blocks");
        Require(SixSite.Plan(world, S.EntityC, S.PartC).Holes.Count == 1, "C keeps its courtyard");
    }

    [TestCase]
    public void BlocksStandAtTheirHeightsWithATallestA()
    {
        var a = S.BaseHeight + SixSite.Style(S.UpperA).Top;
        var b = S.BaseHeight + SixSite.Style(S.UpperB).Top;
        var c = S.BaseHeight + SixSite.Style(S.UpperC).Top;
        Require(MathF.Abs(a - 41.5f) < 0.6f, $"A reaches about 41.5 m, got {a}");
        Require(MathF.Abs(b - 32.5f) < 0.6f && MathF.Abs(c - 32.5f) < 0.6f, $"B and C reach about 32.5 m, got {b}, {c}");
        Require(a > b && a > c + S.SignRise, "A stays the tallest, above C's crown sign");
        // The glass base sits back from the stone line by the recess on every face.
        var world = World();
        foreach (var k in SixSite.Blocks)
        {
            var plan = SixSite.Plan(world, k.Entity, k.Part);
            var inner = SixSite.BasePlan(plan);
            Require(inner.Outer.Count == plan.Outer.Count && inner.Holes.Count == plan.Holes.Count, $"{k.Key}: the base keeps the plan's rings");
            Require(MathF.Abs(PlanMath.SignedArea2(inner.Outer)) < MathF.Abs(PlanMath.SignedArea2(plan.Outer)), $"{k.Key}: the base glass is set back inside the stone");
            Require(inner.Outer.All(p => PlanMath.Inside(plan.Outer, p)), $"{k.Key}: every base corner lies under the stone");
        }
    }

    [TestCase]
    public void EveryLevelIsValidAndEveryItemLandsInOneCell()
    {
        var ctx = Context();
        var module = new WestModule();
        Require(module.Tiles(ctx).Any(), "the module occupies cells");
        foreach (var lod in Levels)
        {
            var items = module.ItemsFor(ctx, lod);
            Require(items.Count > 0, $"{lod} has items");
            var size = 64 << (int)lod;
            var cells = items.Select(i => ((int)MathF.Floor(i.Anchor.X / size), (int)MathF.Floor(i.Anchor.Y / size))).Distinct().ToArray();
            var meshes = 0;
            var parts = 0;
            foreach (var (x, z) in lod == LodLevel.L4 ? [(0, 0)] : cells)
            {
                var output = module.Build(ctx, new(x, z), lod);
                foreach (var mesh in output.Meshes)
                {
                    mesh.ValidateVertexData();
                    Require(mesh.Position.Length / 3 <= 65536, "bundle vertex limit");
                    meshes++;
                }
                parts += output.Parts.Count;
            }
            Require(meshes > 0, $"{lod} emits meshes");
            Require(items.Count(i => i.Part is not null) == parts, $"{lod}: every placed part is emitted exactly once");
            string[] kit = [Contracts.Parts.PartCatalog.EntranceMat, Contracts.Parts.PartCatalog.OpenSign, Contracts.Parts.PartCatalog.EntranceLamp];
            Require(!items.Any(i => i.Part is { } p && kit.Contains(p.Part.Value)), $"{lod}: no entrance kit placed by the zone (WP-13's)");
        }
    }

    [TestCase]
    public void HeaviestCellsStayInsideTheDeclaredBudget()
    {
        var ctx = Context();
        var module = new WestModule();
        foreach (var (lod, limit) in new[] { (LodLevel.L0, module.Budget.TrianglesL0), (LodLevel.L1, module.Budget.TrianglesL1) })
        {
            var size = 64 << (int)lod;
            var cells = module.ItemsFor(ctx, lod).Select(i => ((int)MathF.Floor(i.Anchor.X / size), (int)MathF.Floor(i.Anchor.Y / size))).Distinct();
            foreach (var (x, z) in cells)
            {
                var output = module.Build(ctx, new(x, z), lod);
                var triangles = output.Meshes.Sum(m => m.Index.Length / 3);
                Require(triangles <= limit, $"{lod} cell ({x}, {z}) has {triangles} triangles, budget {limit}");
                Require(output.Meshes.Select(m => m.Material).Distinct().Count() <= module.Budget.DrawCalls, $"{lod} cell ({x}, {z}) material count");
            }
        }
    }

    [TestCase]
    public void BuildIsDeterministic()
    {
        var a = Bytes(Context());
        var b = Bytes(Context());
        Require(a.SequenceEqual(b), "two worlds give the same bytes");
    }

    [TestCase]
    public void EastDoorIsOpenAndTheOtherEntrancesAreShut()
    {
        var ctx = Context();
        var module = new WestModule();
        var colliders = module.Tiles(ctx).SelectMany(t => module.Collide(ctx, t)).ToArray();
        Require(colliders.All(c => c.Foot != Contracts.Audio.FootMat.Auto), "every collider fills its foot material");
        // Nothing at walking height crosses the doorway between the vestibule cheeks.
        var outward = PlanMath.Heading(77f);
        for (var d = -S.Recess - 1f; d <= 0.5f; d += 0.25f)
        {
            var p = S.EastDoor + outward * d;
            Require(!colliders.OfType<WallCollider>().Any(w => w.Y0 < 1.0f && w.Y1 > 1.0f && Near(w.A, w.B, p, w.HalfT + 0.05f)),
                $"the east doorway is open {d:0.00} m along its axis");
        }
        // 六号's shut doors (19b's entrance boxes and portal add their own DoorClosed fronts elsewhere in the zone).
        var shut = colliders.OfType<WallCollider>().Where(w => w.Tag == SurfaceTag.DoorClosed && (w.A.X + w.B.X) * 0.5f < -583f).ToArray();
        Require(shut.Length == S.ShutDoors.Length, $"one DoorClosed segment per shut entrance, got {shut.Length}");
        foreach (var door in S.ShutDoors)
            Require(shut.Any(w => Vector2.Distance((w.A + w.B) * 0.5f, door) < S.Recess + 0.6f), $"shut door {door} is DoorClosed");
        // The motto stone is one unclimbable box.
        Require(colliders.OfType<BoxCollider>().Any(b => b.Tag == SurfaceTag.Prop && MathF.Abs(b.Half.Y * 2 - M.Height) < 0.01f), "the motto stone collides as a 2 m box");
    }

    [TestCase]
    public void EveryFacadeClaimDeclaresItsWindowGrids()
    {
        var world = World();
        var layouts = new WestModule().DescribeWindowLayouts(world, 1).ToDictionary(l => l.Feature.Value, l => l.Grids, StringComparer.Ordinal);
        Require(layouts.Count == 4, $"four Facade-claimed features declare layouts, got {layouts.Count}");
        Require(layouts[WestTune.Shelter.Entity].Count == 0, "the bus shelter declares an empty window layout");
        Require(layouts[S.EntityA].Single().Key == "a" && layouts[S.EntityA].Single().Rows == S.UpperA, "A: one grid over its upper floors");
        Require(layouts[S.EntityB].Single().Rows == S.UpperB && layouts[S.EntityC].Single().Rows == S.UpperC, "B and C: one grid each");
        Require(layouts.Values.SelectMany(g => g).All(g => g.Columns > 0), "every grid has cells");
    }

    [TestCase]
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
        var ctx = new BuildContext { World = world, Map = Descriptor(), WorldSeed = 1, WindowNumbering = new(new("qingshuihe"), new string('a', 64), allocations) };
        var metadata = module.DescribeWindows(ctx);
        var declared = new List<int>();
        foreach (var f in metadata.Facades)
            for (var r = 0; r < f.Rows; r++)
                for (var c = 0; c < f.Columns; c++)
                    if (!f.Skip.Contains(r * f.Columns + c)) declared.Add(f.BaseId + r * f.TotalColumns + f.ColumnStart + c);
        Require(declared.Count == declared.Distinct().Count() && declared.Count > 0, "every window is declared once");
        Require(declared.All(id => allocations.Any(a => id >= a.BaseId && id < a.BaseId + a.Count && a.Feature.Value != "a-earlier")), "declared IDs lie in the zone's grids");
        var ids = new HashSet<int>();
        var size = 64;
        foreach (var (x, z) in module.ItemsFor(ctx, LodLevel.L0).Select(i => ((int)MathF.Floor(i.Anchor.X / size), (int)MathF.Floor(i.Anchor.Y / size))).Distinct())
            foreach (var mesh in module.Build(ctx, new(x, z), LodLevel.L0).Meshes.Where(m => m.Uv2 is not null))
                for (var v = 0; v < mesh.Uv2!.Length / 2; v++)
                {
                    var columns = (int)MathF.Floor(mesh.Uv2[2 * v + 1] / 32f);
                    ids.Add((int)mesh.Uv2[2 * v] + (int)MathF.Floor(mesh.Uv![2 * v + 1]) * columns + (int)MathF.Floor(mesh.Uv[2 * v]));
                }
        Require(ids.SetEquals(declared), $"L0 glass carries exactly the declared windows ({ids.Count} vs {declared.Count})");
    }

    [TestCase]
    public void WindowTablePublishesWithTheWestDeclarations()
    {
        var data = DataDirectory();
        var files = new[] { "campus.json", "network.json", "interiors.json", "map.json" }
            .Concat(Directory.GetDirectories(Path.Combine(data, "overlay"), "*.d")
                    .Where(dir => Path.GetFileName(dir) is not "interior.d" and not "terrain.d" and not "outskirts.d" and not "walkable.d" and not "fidelity.d")
                    .SelectMany(dir => Directory.GetFiles(dir, "*.json")).Select(p => Path.GetRelativePath(data, p).Replace('\\', '/')))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToDictionary(name => name, name => File.ReadAllText(Path.Combine(data, name)), StringComparer.Ordinal);
        var world = WorldModelLoader.Load(new("qingshuihe"), files).World;
        var module = new WestModule();
        var claimed = module.Claims.OfType<FeatureClaim>().Where(c => (c.Aspects & Aspects.Facade) != 0).Select(c => c.Feature).ToHashSet();
        WpId? Owner(FeatureId f, Aspects a) => claimed.Contains(f) ? new WpId("WP-19") : null;
        var prepass = new BuildContext { World = world, Map = Descriptor(), WorldSeed = 1, ClaimedBy = Owner };
        var layouts = module.DescribeWindowLayouts(world, 1);
        var snapshot = new global::Campus.Gen.Buildings.BuildingsModule().PrepareWindowNumbering(prepass, layouts);
        var ours = snapshot.Allocations.Where(a => claimed.Contains(a.Feature)).ToArray();
        Require(ours.Length == layouts.Sum(l => l.Grids.Count) && ours.All(a => a.Columns > 0 && a.Rows > 0), "every west grid is allocated with cells");
        Require(snapshot.WindowCount <= WindowNumberingSnapshot.Capacity, $"the table fits ({snapshot.WindowCount})");
        var metadata = module.DescribeWindows(new BuildContext { World = world, Map = Descriptor(), WorldSeed = 1, ClaimedBy = Owner, WindowNumbering = snapshot });
        Require(metadata.Facades.Count > 0, "the west zone declares facade runs");
    }

    [TestCase]
    public void TerrainOverlayParsesAndKeepsTheA10PlazaAndTheGatesLevel()
    {
        var file = TerrainOverlayJson.Parse(File.ReadAllBytes(Path.Combine(DataDirectory(), "overlay", "terrain", "west.terrain.json")));
        Require(file.Zone.Value == "west" && file.Owner.Value == "WP-19", "the file belongs to WP-19's west zone");
        Require(file.Ops.All(o => o.Id.StartsWith("design:terrain:west-", StringComparison.Ordinal) && o.Zone.Value == "west"), "ids and zones");
        var low = file.Ops.OfType<TerrainPlatformOp>().Single(o => o.Id == "design:terrain:west-wetland-low");
        Require(low.Rings.Holes.Count == 1 && low.Rings.Holes[0].Zip(A.Dry).All(p => Vector2.Distance(p.First, p.Second) < 0.01f), "the low ground leaves the plaza's dry hole");
        Require(A.Plaza.All(p => PlanMath.Inside(A.Dry, p)) && PlanMath.Inside(A.Dry, A.Post), "the plaza and its post lie inside the dry hole");
        var stream = file.Ops.OfType<TerrainChannelOp>().Single(o => o.Id == "design:terrain:west-wetland-stream");
        var canal = file.Ops.OfType<TerrainChannelOp>().Single(o => o.Id == "design:terrain:west-hongguang-north");
        Require(stream.WaterY is -0.5 && canal.WaterY is -1.2, "the wetland stream keeps the lake level, the canal runs 0.7 m lower");
        // The west gates' 20 m zero bands (MAP-QSH-TERRAIN §11): no authored point comes near them.
        Vector2[] gates = [new(-372.7f, -505.0f), new(-197.6f, -731.3f), new(-620.0f, -190.0f)];
        var points = file.Ops.SelectMany(Points).ToArray();
        Require(points.All(p => gates.All(g => Vector2.Distance(p, g) > 60f)), "every terrain operation stays out of the gates' zero bands");

        static IEnumerable<Vector2> Points(TerrainOp op) => op switch
        {
            TerrainPlatformOp p => p.Rings.Outer.Concat(p.Rings.Holes.SelectMany(h => h)),
            TerrainSlopeOp s when s.Geometry is PolylineGeometry l => l.Line.Points,
            TerrainChannelOp c => c.Line?.Points ?? [],
            _ => [],
        };
    }

    [TestCase]
    public void OtherWestBuildingsGoToWp14WithTheirFamilies()
    {
        var world = World();
        foreach (var (id, family) in new[]
        {
            ("qsh:relation:11334255", "research"), ("qsh:relation:11665641", "research"), ("qsh:relation:11334253", "research"),
            ("qsh:relation:8426842", "research"), ("qsh:relation:11031411", "research"), ("qsh:way:603368216", "research"),
            ("qsh:relation:11032065", "research"), ("qsh:way:795677483", "research"), ("qsh:way:795677482", "research"),
            ("qsh:way:570145228", "office"), ("qsh:way:779496151", "research"), ("qsh:way:779496154", "research"),
        })
            Require(world.Features[new(id)].Family?.Value == family, $"{id} is styled {family} for WP-14");
        foreach (var id in new[] { F4.EntityA, F4.EntityB, F4.EntityC, "qsh:relation:8426842" })
            Require(world.Features[new(id)].Facade?.Value == "research.stoneFrame", $"{id} takes the stone-and-glass preset");
        // 19b claims nothing: the bodies stay WP-14's, the zone only adds entrance boxes, shafts and the portal in front of them.
        Require(new WestModule().Claims.OfType<FeatureClaim>().Count() == 4, "only 六号's three entities and the 19c bus shelter are claimed");
    }

    [TestCase]
    public void FourGlassBoxesSitOnTheirStoneLinesAndAreShut()
    {
        var world = World();
        var parts = new Dictionary<string, string> { [F4.EntityA] = "osm:relation:11334255", [F4.EntityB] = "osm:relation:11665641", [F4.EntityC] = "osm:relation:11334252" };
        foreach (var box in F4.Boxes)
        {
            var ring = Six.SixSite.Plan(world, box.Entity, parts[box.Entity]).Outer;
            var onLine = Enumerable.Range(0, ring.Count).Any(i => Near(ring[i], ring[(i + 1) % ring.Count], box.Door, 0.3f));
            Require(onLine, $"四号 {box.Key}: the door lies on the block's stone line");
            var out1 = PlanMath.Heading(box.Facing);
            Require(!PlanMath.Inside(ring, box.Door + out1 * 2f) && PlanMath.Inside(ring, box.Door - out1 * 2f), $"四号 {box.Key}: the box faces out of the block");
        }
        var ctx = Context();
        var module = new WestModule();
        var colliders = module.Tiles(ctx).SelectMany(t => module.Collide(ctx, t)).ToArray();
        foreach (var box in F4.Boxes)
            Require(colliders.OfType<WallCollider>().Any(w => w.Tag == SurfaceTag.DoorClosed && Vector2.Distance((w.A + w.B) * 0.5f, box.Door) < F4.GlassDepth + 1f),
                $"四号 {box.Key}: the glass box front is DoorClosed");
        Require(colliders.OfType<WallCollider>().Any(w => w.Tag == SurfaceTag.DoorClosed && Vector2.Distance((w.A + w.B) * 0.5f, I.MainDoor) < I.PortalDepth + 0.5f),
            "创新中心: the main portal is DoorClosed");
        // The seat wall runs inside the lawn, clear of the radial paths' gaps.
        foreach (var arc in F4.SeatArcs)
            Require(FourSite.SeatLine(arc).All(p => Vector2.Distance(p, F4.LawnCentre) < arc.Max(q => Vector2.Distance(q, F4.LawnCentre))), "the seat wall is inset into the lawn");
    }

    [TestCase]
    public void InnovationShaftsBridgeWingAndBar()
    {
        var world = World();
        var feature = world.Features[new(I.Entity)];
        var plans = feature.Parts.Select(p => p.Geometry).OfType<PolygonGeometry>().Select(g => g.Rings.Outer)
            .Concat(feature.Parts.Select(p => p.Geometry).OfType<MultiPolygonGeometry>().SelectMany(m => m.Polygons.Select(r => r.Outer))).ToArray();
        foreach (var (centre, length) in I.Shafts)
        {
            Require(plans.Any(r => PlanMath.Inside(r, centre)), $"shaft {centre} stands on a link of 创新中心");
            var ends = new[] { centre + I.WingAxis * (length * 0.5f + 1.5f), centre - I.WingAxis * (length * 0.5f + 1.5f) };
            Require(ends.All(e => plans.Any(r => PlanMath.Inside(r, e))), $"shaft {centre} reaches a building at both ends");
        }
    }

    [TestCase]
    public void FourFanLawnIsAShallowDish()
    {
        var file = TerrainOverlayJson.Parse(File.ReadAllBytes(Path.Combine(DataDirectory(), "overlay", "terrain", "west.terrain.json")));
        var dish = file.Ops.OfType<TerrainBasinOp>().Single(o => o.Id == "design:terrain:west-four-fan-lawn");
        Require(dish.Height is 0.45 && dish.Shape is TerrainCircle { Radius: 32 } c && Vector2.Distance(c.Center, F4.LawnCentre) < 0.01f,
            "the fan lawn is a 0.45 m dish of radius 32 m (MAP-QSH-TERRAIN §10.2)");
    }

    // ---- helpers ----

    private static bool Near(Vector2 a, Vector2 b, Vector2 p, float r)
    {
        var t = Math.Clamp(Vector2.Dot(p - a, b - a) / Vector2.Dot(b - a, b - a), 0f, 1f);
        return Vector2.Distance(a + (b - a) * t, p) < r;
    }

    private static byte[] Bytes(BuildContext ctx)
    {
        var module = new WestModule(WestSites.All.ToArray());
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        foreach (var tile in module.Tiles(ctx))
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

    private static BuildContext Context() => new() { World = World(), Map = null!, WorldSeed = 1 };

    private static string DataDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "game", "data", "maps", "qingshuihe", "campus.json"))) directory = directory.Parent;
        if (directory is null) throw new InvalidOperationException("Repository campus fixture not found.");
        return Path.Combine(directory.FullName, "game", "data", "maps", "qingshuihe");
    }

    private static MapDescriptor Descriptor()
    {
        var rect = new MapRect(new(-1280f, -1280f), new(1280f, 1280f));
        return new MapDescriptor(new MapId("qingshuihe"), new LocalizedName("清水河", "Qingshuihe"), MapKind.Outdoor, 1, 1u,
            new GeoFrame(0d, 0d, 32648, new(-200f, -200f), new(200f, 200f)), Vector2.Zero, new Box3(new(-1280f, -28f, -1280f), new(1280f, 2000f, 1280f)), rect,
            new RoamLimits(0.9f, 30f, 3000f, -28f, 2000f, 30f, 40f, 150f), new TileKey(-20, -20), new TileKey(19, 19), "P00", [],
            new MapWorldRules(MapTimeRule.Shared, new WorldPreset(17.5, Season.Autumn), new LocalizedName("落金初灯", "Golden Hour"), true, "chengdu"),
            new MapEnvironment("sky.json", "backdrop.json", []), new Dictionary<string, string>(), []);
    }

    private static IWorldModel World()
    {
        var data = DataDirectory();
        var files = new[] { "campus.json", "network.json", "interiors.json", "map.json" }
            .Concat(Directory.GetFiles(Path.Combine(data, "overlay", "west.d"), "*.json").Order(StringComparer.Ordinal)
                .Select(path => Path.GetRelativePath(data, path).Replace('\\', '/')))
            .ToDictionary(name => name, name => File.ReadAllText(Path.Combine(data, name)), StringComparer.Ordinal);
        return WorldModelLoader.Load(new("qingshuihe"), files).World;
    }

    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
