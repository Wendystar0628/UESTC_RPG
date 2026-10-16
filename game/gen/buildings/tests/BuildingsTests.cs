using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Gen.Buildings.Model;
using Campus.Gen.Buildings.Plan;
using GdUnit4;

namespace Campus.Gen.Buildings.Tests;

[TestSuite]
public sealed class BuildingsTests
{
    private static readonly LodLevel[] Levels = [LodLevel.L0, LodLevel.L1, LodLevel.L2, LodLevel.L3, LodLevel.L4];

    [TestCase]
    public void DormitoryHasStoreysBaysAndNumberedWindows()
    {
        var world = World(Feature("t:dorm", Rect(0, 0, 60, 18), 6, 19.8f, "residence"));
        var set = BuildingSet.For(world, 1u);
        Require(set.Plans.Count == 1, "one plan");
        var plan = set.Plans[0];
        Require(plan.Storeys == 6, "storeys from data");
        Require(plan.TotalColumns >= 20, "bays on all four facades");
        Require(plan.EntranceFacade >= 0, "an entrance on the longest facade");
        Require(set.WindowCount == plan.TotalColumns * plan.Storeys, "window block covers the building grid");
        var bundles = new BuildingsModule().BuildOne(plan, set.Blocks[0], LodLevel.L0);
        var glass = bundles.Where(b => b.Material.Spec.Kind == ToonKind.Glass).ToArray();
        Require(glass.Length > 0, "glass bundle");
        var block = set.Blocks[0];
        foreach (var g in glass)
            for (var i = 0; i < g.Position.Length / 3; i++)
            {
                Require(g.Uv2![2 * i] == block.Base, "UV2.x is the building window base");
                var packed = (int)MathF.Round(g.Uv2[2 * i + 1]);
                Require(packed / 32 == plan.TotalColumns, "UV2.y carries the grid column count");
                var col = (int)MathF.Floor(g.Uv![2 * i]);
                var row = (int)MathF.Floor(g.Uv[2 * i + 1]);
                var id = block.Base + row * plan.TotalColumns + col;
                Require(id >= block.Base && id < block.Base + block.Count, "window number inside the block");
                Require(g.Reserved![i] is >= 1 and <= 15, "window card type in the spare byte");
            }
    }

    [TestCase]
    public void EveryLevelIsValidWoundAndFinite()
    {
        var world = World(
            Feature("t:a", Rect(0, 0, 48, 16), 5, 19.5f, "teaching"),
            Feature("t:b", LShape(100, 0), 4, 15.6f, "research"),
            Feature("t:c", Courtyard(0, 100), 3, 12f, "residence"),
            Feature("t:d", Rect(200, 0, 5, 6), 1, 3f, "service"));
        var set = BuildingSet.For(world, 7u);
        Require(set.Plans.Count == 4, "four plans");
        var module = new BuildingsModule();
        for (var i = 0; i < set.Plans.Count; i++)
            foreach (var lod in Levels)
            {
                var bundles = module.BuildOne(set.Plans[i], set.Blocks[i], lod);
                Require(bundles.Count > 0, $"{set.Plans[i].Key} {lod} has geometry");
                foreach (var b in bundles)
                {
                    b.ValidateVertexData();
                    Require(b.Position.Length / 3 <= 65536, "surface vertex limit");
                    Require(b.Ao is not null && b.Flags is not null && b.Reserved is not null, "COLOR bytes always supplied");
                    for (var v = 0; v < b.Normal.Length / 3; v++)
                    {
                        var n = new Vector3(b.Normal[3 * v], b.Normal[3 * v + 1], b.Normal[3 * v + 2]);
                        Require(MathF.Abs(n.Length() - 1f) < 1e-3f, "unit normals");
                    }
                    for (var t = 0; t < b.Index.Length; t += 3)
                    {
                        var a = P(b, b.Index[t]);
                        var g = Vector3.Cross(P(b, b.Index[t + 1]) - a, P(b, b.Index[t + 2]) - a);
                        var n = N(b, b.Index[t]) + N(b, b.Index[t + 1]) + N(b, b.Index[t + 2]);
                        // Godot front faces are clockwise: the geometric normal opposes the shading normal.
                        Require(Vector3.Dot(g, n) <= 1e-6f, "front-face winding");
                    }
                }
            }
    }

    [TestCase]
    public void GenerationIsDeterministic()
    {
        var world1 = World(Feature("t:a", LShape(0, 0), 6, 21f, "residence"));
        var world2 = World(Feature("t:a", LShape(0, 0), 6, 21f, "residence"));
        var s1 = BuildingSet.For(world1, 3u);
        var s2 = BuildingSet.For(world2, 3u);
        var b1 = new BuildingsModule().BuildOne(s1.Plans[0], s1.Blocks[0], LodLevel.L0);
        var b2 = new BuildingsModule().BuildOne(s2.Plans[0], s2.Blocks[0], LodLevel.L0);
        Require(b1.Count == b2.Count, "same bundle count");
        for (var i = 0; i < b1.Count; i++)
        {
            Require(b1[i].Position.SequenceEqual(b2[i].Position), "same positions");
            Require(b1[i].Normal.SequenceEqual(b2[i].Normal), "same normals");
            Require(b1[i].Index.SequenceEqual(b2[i].Index), "same indices");
            Require(b1[i].Material == b2[i].Material, "same materials");
        }
    }

    [TestCase]
    public void PartGlassKeepsTheWindowNumbersAndCards()
    {
        var world = World(
            Feature("t:dorm", Rect(0, 0, 60, 18), 6, 19.8f, "residence"),
            Feature("t:hall", Rect(100, 0, 40, 30), 1, 9f, "sport"),
            Feature("t:plant", Rect(200, 0, 20, 12), 2, 8f, "utility"));
        var set = BuildingSet.For(world, 5u);
        var module = new BuildingsModule();
        for (var b = 0; b < set.Plans.Count; b++)
        {
            var plan = set.Plans[b];
            var block = set.Blocks[b];
            // Expected cells straight from the plan, with the encoding the mainline glass used.
            var expected = new Dictionary<int, byte>();
            for (var fi = 0; fi < plan.Facades.Count; fi++)
                foreach (var o in plan.Openings[fi])
                {
                    var id = block.Base + o.Row * plan.TotalColumns + plan.Facades[fi].ColumnStart + o.Bay;
                    var hash = StableHash.Hash32(plan.Seed, "card", id.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    expected.Add(id, (byte)plan.CardOf(o.Kind, hash));
                }
            foreach (var lod in new[] { LodLevel.L0, LodLevel.L1, LodLevel.L2, LodLevel.L3 })
            {
                var seen = new HashSet<int>();
                foreach (var g in module.BuildOne(plan, block, lod).Where(x => x.Material.Spec.Kind == ToonKind.Glass))
                    for (var i = 0; i < g.Position.Length / 3; i++)
                    {
                        Require(g.Uv2![2 * i] == block.Base && (int)MathF.Round(g.Uv2[2 * i + 1]) == plan.Profile.Channel + 32 * plan.TotalColumns,
                            $"{plan.Key} {lod}: UV2 is (BaseId, slot + 32 × columns)");
                        var id = block.Base + (int)MathF.Floor(g.Uv![2 * i + 1]) * plan.TotalColumns + (int)MathF.Floor(g.Uv[2 * i]);
                        Require(expected.TryGetValue(id, out var card) && g.Reserved![i] == card, $"{plan.Key} {lod}: window {id} keeps its cell and card");
                        seen.Add(id);
                    }
                Require(seen.SetEquals(expected.Keys), $"{plan.Key} {lod}: every opening has glass");
            }
        }
    }

    private static bool Sash(GeometryBundle b) => b.Material.Spec.Lit.Value == "frame.alu.lit" && b.Material.Spec.Fx == ToonFx.Rim;
    private static bool Fabric(GeometryBundle b) => b.Material.Spec.Normal == ToonNormal.Smooth && b.Material.Spec.Steps == ShadeSteps.TwoHalf;

    [TestCase]
    public void K1AndK2OpeningsTakeTheWp10Parts()
    {
        // The walking corridor's ground band is K2, the facade above it K3 (ENG-ARCH §3 FacadeSegment).
        var world = World(new ZonedFidelity(FidelityZone.K3, FidelityZone.K2), Feature("t:dorm", Rect(0, 0, 60, 18), 6, 19.8f, "residence"));
        var set = BuildingSet.For(world, 1u);
        var plan = set.Plans[0];
        for (var fi = 0; fi < plan.Facades.Count; fi++)
            for (var k = 0; k < plan.Openings[fi].Length; k++)
                Require(plan.ZoneOf(fi, k) == (plan.Openings[fi][k].Row == 0 ? FidelityZone.K2 : FidelityZone.K3), "ground floor in the K2 band, the rest K3");
        var module = new BuildingsModule();
        var near = module.BuildOne(plan, set.Blocks[0], LodLevel.L0);
        var mid = module.BuildOne(plan, set.Blocks[0], LodLevel.L1);
        Require(near.Any(Sash), "K2 windows carry the WP-10 two-level sashes at L0");
        Require(near.Any(Fabric), "K2 room windows hang WP-10 curtains at L0");
        Require(!mid.Any(Sash) && !mid.Any(Fabric), "L1 keeps the simplified frame ring only");
        // Without window gaskets the dark steel is only the astragal between the entrance leaves (two boxes).
        var dark = near.Where(b => b.Material.Spec.Lit.Value == "equip.fence.lit").Sum(b => b.Index.Length / 3);
        Require(Rules.OpeningRules.Gaskets || dark <= 24, "window gaskets follow OpeningRules.Gaskets");
        Require(near.Concat(mid).All(b => b.Uv2 is null || b.Uv2.Where((_, i) => i % 2 == 0).All(x => x == set.Blocks[0].Base)),
            "no part glass keeps WP-10's placeholder numbering");
    }

    [TestCase]
    public void K3OpeningsTakeTheSimpleWindow()
    {
        var world = World(
            Feature("t:dorm", Rect(0, 0, 60, 18), 6, 19.8f, "residence"),
            Feature("t:hall", Rect(100, 0, 40, 30), 1, 9f, "sport"),
            Feature("t:lab", Rect(200, 0, 48, 16), 5, 21f, "research"),
            Feature("t:canteen", Rect(300, 0, 40, 24), 2, 9.5f, "dining"));
        var set = BuildingSet.For(world, 3u);
        var module = new BuildingsModule();
        for (var i = 0; i < set.Plans.Count; i++)
        {
            var plan = set.Plans[i];
            foreach (var lod in new[] { LodLevel.L0, LodLevel.L1 })
            {
                var levelMax = 0;
                for (var fi = 0; fi < plan.Facades.Count; fi++)
                {
                    var layout = plan.Facades[fi];
                    var frame = Mesh.EdgeFrame.Of(plan.SkinAt(layout.Ring), layout.Edge);
                    foreach (var o in plan.Openings[fi].Where(o => o.Kind != BayKind.Door))
                    {
                        var sink = new Mesh.MeshSink(null);
                        new Build.FacadeOpenings(plan, set.Blocks[i], lod, sink).Fill(frame, layout, o, FidelityZone.K3);
                        levelMax = Math.Max(levelMax, sink.Triangles - 2);
                    }
                }
                Require(levelMax is >= 16 and <= 60, $"{plan.Key} {lod}: a simple window is 40–60 triangles at most ({levelMax})");
            }
            var near = module.BuildOne(plan, set.Blocks[i], LodLevel.L0);
            Require(!near.Any(Sash) && !near.Any(Fabric), $"{plan.Key}: K3 has no sashes, handles or curtains");
            Require(near.All(b => b.Material.Spec.Lit.Value != "equip.fence.lit" || b.Index.Length / 3 <= 24), $"{plan.Key}: no window gaskets");
        }
    }

    [TestCase]
    public void WindowDeclarationsMatchTheGlass()
    {
        var world = World(Feature("t:a", LShape(0, 0), 4, 15.6f, "teaching"));
        var set = BuildingSet.For(world, 2u);
        var plan = set.Plans[0];
        var ctx = new BuildContext { World = world, Map = null!, WorldSeed = 2u };
        var output = new BuildingsModule().Build(ctx, plan.Tile(LodLevel.L0), LodLevel.L0);
        var decls = output.LooseWindows;
        Require(decls.Count == plan.Openings.Sum(o => o.Length), "one declaration per opening");
        Require(decls.Select(d => d.Id).Distinct().Count() == decls.Count, "window numbers are unique");
        var fi = 0;
        for (var r = 0; r < plan.RingCount; r++)
        {
            var skin = plan.SkinAt(r);
            for (var e = 0; e < skin.Count; e++, fi++)
                foreach (var o in plan.Openings[fi])
                {
                    var id = set.Blocks[0].Base + o.Row * plan.TotalColumns + plan.Facades[fi].ColumnStart + o.Bay;
                    var d = decls.Single(x => x.Id == id);
                    var outward = new Vector3(skin.Normal[e].X, 0, skin.Normal[e].Y);
                    Require(Vector3.Distance(Vector3.Cross(d.Right, d.Up), outward) < 1e-4f, "Right × Up points out of the facade");
                    var mid = skin.Points[e] + skin.Dir[e] * ((o.U0 + o.U1) * 0.5f) - skin.Normal[e] * plan.Reveal;
                    Require(Vector3.Distance(d.Center, new Vector3(mid.X, (o.Y0 + o.Y1) * 0.5f, mid.Y)) < 1e-3f, "centred on the opening's glass");
                    Require(MathF.Abs(d.Width - (o.U1 - o.U0)) < 1e-4f && d.BaseId == set.Blocks[0].Base && d.TotalColumns == plan.TotalColumns,
                        "size and numbering as the glass");
                }
        }
        Require(new BuildingsModule().Build(ctx, plan.Tile(LodLevel.L1), LodLevel.L1).LooseWindows.Count == 0, "declared with the L0 cell only");
    }

    [TestCase]
    public void WindowNumbersFollowFeatureOrder()
    {
        var world = World(
            Feature("t:b", Rect(100, 0, 30, 12), 3, 10.5f, "teaching"),
            Feature("t:a", Rect(0, 0, 30, 12), 3, 10.5f, "teaching"));
        var blocks = WindowNumbering.For(world, 1u);
        Require(blocks.Count == 2 && blocks[0].Feature.Value == "t:a" && blocks[0].Base == 0, "ordinal FeatureId order from zero");
        Require(blocks[1].Base == blocks[0].Count, "contiguous allocation");
    }

    [TestCase]
    public void PartsAreBuiltSeparatelyWithTheirOwnHeights()
    {
        var parts = new[]
        {
            new WorldFeaturePart("osm:way:2", new PolygonGeometry(new PolygonRings(Rect(40, 0, 20, 12))), 7f, 2),
            new WorldFeaturePart("osm:way:1", new PolygonGeometry(new PolygonRings(Rect(0, 0, 30, 12))), 21f, 6),
        };
        var entity = Feature("qsh:way:1", Rect(0, 0, 60, 12), 6, 21f, "teaching") with { Parts = parts };
        var set = BuildingSet.For(World(entity), 1u);
        Require(set.Plans.Count == 2, "one plan per part, none for the aggregate");
        Require(set.Plans[0].Key == "osm:way:1" && set.Plans[0].Storeys == 6, "first part by source id, six storeys");
        Require(set.Plans[1].Key == "osm:way:2" && set.Plans[1].Storeys == 2, "second part keeps its own storeys");
        Require(set.Blocks[1].Base == set.Blocks[0].Count, "parts take consecutive window blocks");
    }

    [TestCase]
    public void BuildingPartsCountUnderAnyParentKind()
    {
        // WP-08 layout: the entity keeps its own kind (here poi), each part carries its source record with "kind".
        static JsonElement Raw(string kind, string category) =>
            JsonDocument.Parse("{\"kind\":\"" + kind + "\",\"category\":\"" + category + "\"}").RootElement.Clone();
        var parts = new[]
        {
            new WorldFeaturePart("osm:way:10", new PolygonGeometry(new PolygonRings(Rect(0, 0, 30, 14))), 12f, 3) { Props = Raw("building", "teaching") },
            new WorldFeaturePart("osm:way:11", new PolygonGeometry(new PolygonRings(Rect(40, 0, 20, 14))), 8f, 2) { Props = Raw("building", "teaching") },
            new WorldFeaturePart("osm:way:12", new PolygonGeometry(new PolygonRings(Rect(0, 30, 60, 30))), null, null) { Props = Raw("green", "landscape") },
        };
        var park = new WorldFeature(new FeatureId("qsh:way:9"), FeatureSource.Osm, new DesignKind("poi"),
            new PolygonGeometry(new PolygonRings(Rect(0, 0, 60, 60))))
        { Parts = parts, Props = Raw("poi", "teaching") };
        var lawn = new WorldFeature(new FeatureId("qsh:way:8"), FeatureSource.Osm, new DesignKind("green"),
            new PolygonGeometry(new PolygonRings(Rect(100, 0, 20, 20))));
        var set = BuildingSet.For(World(park, lawn), 1u);
        Require(set.Plans.Count == 2, "the two building parts of a poi entity, not its green part nor the lawn");
        Require(set.Plans.All(p => p.Style == Rules.BuildingStyle.Academic), "style from the part's own category");
    }

    [TestCase]
    public void BaysAndWindowColumnsComeFromThePlanKernel()
    {
        var world = World(
            Feature("t:dorm", Rect(0, 0, 36, 14), 6, 19.8f, "residence"),
            Feature("t:l", LShape(100, 0), 4, 15.6f, "research"),
            Feature("t:court", Courtyard(0, 100), 3, 12f, "teaching"));
        var set = BuildingSet.For(world, 1u);
        foreach (var plan in set.Plans)
        {
            var bays = plan.Grid.Bays;
            Require(plan.TotalColumns == bays.Count, "one window column per kernel bay");
            var seen = 0;
            foreach (var f in plan.Facades)
                for (var k = 0; k < f.Bays; k++, seen++)
                {
                    var bay = bays[f.ColumnStart + k];
                    Require(bay.Index == f.ColumnStart + k && bay.Ring == f.Ring && bay.Edge == f.Edge && bay.Bay == k,
                        $"{plan.Key}: column {f.ColumnStart + k} is kernel bay r{bay.Ring}/e{bay.Edge}/b{bay.Bay}");
                    Require((bay.WindowKind == Campus.Gen.Interior.FacadeWindowKind.Blank) == (f.Kinds[k] == BayKind.Blank), "blank bays agree");
                }
            Require(seen == bays.Count, "every kernel bay is laid out once");
            var again = Campus.Gen.Interior.FacadeGridGenerator.Generate(plan.Grid.Footprint, BuildingPlanner.GridOptions(plan.Profile));
            Require(again.GridHash == plan.Grid.GridHash, "same grid for the interiors from the same footprint and options");
        }
        var dorm = set.Plans.Single(p => p.Key == "t:dorm");
        Require(dorm.Facades.Where(f => f.Ring == 0).Max(f => f.Bays) == 10, "dormitory 36 m facade: ten 3.6 m bays");
    }

    [TestCase]
    public void TriangulatorCoversPolygonWithHole()
    {
        var outer = Rect(0, 0, 10, 10);
        var hole = new List<Vector2> { new(3, 3), new(3, 7), new(7, 7), new(7, 3) };
        var (pts, tris) = Triangulator.Triangulate(outer, [hole]);
        var area = 0f;
        for (var t = 0; t < tris.Length; t += 3)
            area += MathF.Abs(PlanRing.Cross(pts[tris[t + 1]] - pts[tris[t]], pts[tris[t + 2]] - pts[tris[t]])) * 0.5f;
        Require(MathF.Abs(area - 84f) < 0.01f, $"area {area} equals 100 - 16");
    }

    // ---------------------------------------------------------------- fixtures
    private static Vector3 P(GeometryBundle b, int i) => new(b.Position[3 * i], b.Position[3 * i + 1], b.Position[3 * i + 2]);
    private static Vector3 N(GeometryBundle b, int i) => new(b.Normal[3 * i], b.Normal[3 * i + 1], b.Normal[3 * i + 2]);

    private static void Require(bool condition, string what)
    {
        if (!condition) throw new InvalidOperationException("Expected: " + what);
    }

    private static List<Vector2> Rect(float x, float z, float w, float d) => [new(x, z), new(x + w, z), new(x + w, z + d), new(x, z + d)];

    private static List<Vector2> LShape(float x, float z) =>
        [new(x, z), new(x + 50, z), new(x + 50, z + 15), new(x + 16, z + 15), new(x + 16, z + 45), new(x, z + 45)];

    private static PolygonRings Courtyard(float x, float z) =>
        new(Rect(x, z, 50, 40), [new List<Vector2> { new(x + 12, z + 12), new(x + 12, z + 28), new(x + 38, z + 28), new(x + 38, z + 12) }]);

    private static WorldFeature Feature(string id, List<Vector2> outer, int storeys, float height, string category) =>
        Feature(id, new PolygonRings(outer), storeys, height, category);

    private static WorldFeature Feature(string id, PolygonRings rings, int storeys, float height, string category) =>
        new(new FeatureId(id), FeatureSource.Osm, new DesignKind("building"), new PolygonGeometry(rings))
        {
            Height = height,
            Storeys = storeys,
            Props = JsonDocument.Parse("{\"category\":\"" + category + "\"}").RootElement.Clone(),
        };

    private static TestWorld World(params WorldFeature[] features) => new(features.ToDictionary(f => f.Id), null);

    private static TestWorld World(ZonedFidelity fidelity, params WorldFeature[] features)
    {
        var map = features.ToDictionary(f => f.Id);
        fidelity.Features = map;
        return new(map, fidelity);
    }

    /// <summary>Every footprint edge one segment: Zone above the ground band, GroundBand within it.</summary>
    private sealed class ZonedFidelity(FidelityZone zone, FidelityZone ground) : IFidelityModel
    {
        public IReadOnlyDictionary<FeatureId, WorldFeature> Features { get; set; } = new Dictionary<FeatureId, WorldFeature>();
        public string InputHash => "test";
        public IReadOnlyList<FacadeSegment> Facades(FeatureId building)
        {
            var ring = ((PolygonGeometry)Features[building].Geometry).Rings.Outer;
            return [.. ring.Select((a, i) => new FacadeSegment(building, i, a, ring[(i + 1) % ring.Count], zone, ground))];
        }
        public RoofZone Roof(FeatureId building) => new(building, FidelityZone.K4);
        public FidelityZone GroundAt(float x, float z) => FidelityZone.K3;
        public float WalkDistanceAt(float x, float z) => 0f;
    }

    private sealed class TestWorld(IReadOnlyDictionary<FeatureId, WorldFeature> features, IFidelityModel? fidelity) : IWorldModel
    {
        public PolygonRings Boundary { get; } = new([new(-1000, -1000), new(1000, -1000), new(1000, 1000), new(-1000, 1000)]);
        public IReadOnlyDictionary<FeatureId, WorldFeature> Features { get; } = features;
        public WorldNetwork Network => WorldNetwork.Empty;
        public ISpatialIndex Spatial { get; } = new Flat();
        public IInteriorIndex Interiors { get; } = new NoInteriors();
        public IOutskirtsModel? Outskirts => null;
        public IFidelityModel? Fidelity { get; } = fidelity;
    }

    private sealed class Flat : ISpatialIndex
    {
        public IReadOnlyList<FeatureId> Query(Box3 box) => [];
        public float GroundY(float x, float z) => 0f;
        public CameraMedium MediumAt(Vector3 p) => new(MediumKind.Air);
        public float? SupportY(float x, float z, float yHint) => 0f;
        public WalkClass WalkClassAt(float x, float z) => WalkClass.Walk;
        public Vector3? NearestSafe(Vector3 p, float maxDist) => p;
    }

    private sealed class NoInteriors : IInteriorIndex
    {
        public InteriorIndexEntry? Get(FeatureId id) => null;
        public IReadOnlyList<EntranceDecl> EntrancesNear(Vector3 p, float r) => [];
    }
}
