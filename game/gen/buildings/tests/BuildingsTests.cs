using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Gen.Buildings.Build;
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
    // The WP-10 gasket class (dark equipment steel without outline), not the WP-12 roof fence of the same colour.
    private static bool Gasket(GeometryBundle b) => b.Material.Spec.Lit.Value == "equip.fence.lit" && b.Material.Spec.Line == LineClass.None;
    // WP-10 curtain fabric; the WP-12 heat-pump shrouds share its shading but not its colours.
    private static bool Fabric(GeometryBundle b) => b.Material.Spec.Normal == ToonNormal.Smooth && b.Material.Spec.Steps == ShadeSteps.TwoHalf &&
        !b.Material.Spec.Lit.Value.StartsWith("equip.", StringComparison.Ordinal);

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
        var dark = near.Where(Gasket).Sum(b => b.Index.Length / 3);
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
            Require(near.All(b => !Gasket(b) || b.Index.Length / 3 <= 24), $"{plan.Key}: no window gaskets");
        }
    }

    [TestCase]
    public void RoofsTakeTheWp12PartsByTheirZone()
    {
        WorldFeature[] features = [Feature("t:dorm", Rect(0, 0, 60, 18), 6, 19.8f, "residence"), Feature("t:lab", Rect(100, 0, 48, 20), 5, 21f, "research")];
        var plain = BuildingSet.For(World(features), 2u);
        var hero = BuildingSet.For(World(new ZonedFidelity(FidelityZone.K3, FidelityZone.K3, FidelityZone.K1), features), 2u);
        var module = new BuildingsModule();
        static bool Pump(GeometryBundle b) => b.Material.Spec.Lit.Value == "equip.grey.lit";
        static bool Fence(GeometryBundle b) => b.Material.Spec.Lit.Value == "equip.fence.lit" && b.Material.Spec.Line == LineClass.Building;
        static bool Strip(GeometryBundle b) => b.Material.Spec.Lit.Value == "roof.silverMetal.lit" && b.Material.Spec.Fx.HasFlag(ToonFx.Rim);
        static bool Tank(GeometryBundle b) => b.Material.Spec.Lit.Value == "roof.silverMetal.lit" && b.Material.Spec.Fx == ToonFx.BevelLift;
        static int Tris(IEnumerable<GeometryBundle> bs) => bs.Sum(b => b.Index.Length / 3);
        for (var i = 0; i < plain.Plans.Count; i++)
        {
            var (simple, full) = (plain.Plans[i], hero.Plans[i]);
            Require(simple.RoofZone == FidelityZone.K4 && full.RoofZone == FidelityZone.K1, "roof zone from the fidelity data, K4 without it");
            var near = module.BuildOne(simple, plain.Blocks[i], LodLevel.L0);
            var nearFull = module.BuildOne(full, hero.Blocks[i], LodLevel.L0);
            Require(near.Any(Fence) && near.Any(Pump) && near.Any(Strip), $"{simple.Key}: fenced plant and lightning strip");
            Require((simple.Style == Rules.BuildingStyle.Research) == near.Any(Tank), $"{simple.Key}: a water tank beside teaching plant only");
            Require(Tris(nearFull.Where(Pump)) > Tris(near.Where(Pump)), $"{simple.Key}: a K1 roof carries more and finer units");
            Require(Tris(nearFull.Where(Strip)) > Tris(near.Where(Strip)), $"{simple.Key}: K1 strip on supports, elsewhere one rod");
            var mid = module.BuildOne(simple, plain.Blocks[i], LodLevel.L1);
            Require(!mid.Any(Strip) && module.BuildOne(full, hero.Blocks[i], LodLevel.L1).Any(Strip), $"{simple.Key}: the rod stops at L0 on simple roofs");
            // Coping: the WP-12 cap reaches the parapet top, its drip hangs 60 mm under the slab outside the wall.
            var trim = near.Where(b => b.Material.Spec.Lit.Value == Rules.Materials.Lit(simple.Profile.Trim).Value && b.Material.Spec.Fx == ToonFx.BevelLift).ToArray();
            var ys = trim.SelectMany(b => Enumerable.Range(0, b.Position.Length / 3).Select(v => b.Position[3 * v + 1])).ToArray();
            Require(ys.Any(y => MathF.Abs(y - simple.EdgeTopY) < 1e-3f) && ys.Any(y => MathF.Abs(y - (simple.EdgeTopY - 0.21f)) < 1e-3f), $"{simple.Key}: coping and drip");
            foreach (var b in near.Where(b => Fence(b) || Pump(b)))
                for (var v = 0; v < b.Position.Length / 3; v++)
                {
                    var p = new Vector2(b.Position[3 * v], b.Position[3 * v + 2]);
                    Require(PlanRing.InsideFootprint(simple.Outer, simple.Holes, p) && PlanRing.DistanceToEdges(simple.Outer, simple.Holes, p) > 0.4f &&
                        b.Position[3 * v + 1] >= simple.RoofY - 1e-3f, $"{simple.Key}: plant stands on the roof, clear of the parapet");
                }
        }
    }

    [TestCase]
    public void WindowDeclarationsMatchTheGlass()
    {
        var world = World(Feature("t:a", LShape(0, 0), 4, 15.6f, "teaching"));
        var set = BuildingSet.For(world, 2u);
        var plan = set.Plans[0];
        var ctx = Prepared(Ctx(world, 2u));
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

    // ---------------------------------------------------------------- contract-29: the map-wide numbering provider

    [TestCase]
    public void TheProviderKeepsTheGenericNumbersWhenNothingIsClaimed()
    {
        var world = World(
            Feature("t:dorm", Rect(0, 0, 60, 18), 6, 19.8f, "residence"),
            Feature("t:lab", LShape(100, 0), 4, 15.6f, "research"),
            Feature("t:court", Courtyard(0, 100), 3, 12f, "teaching"),
            Feature("t:hall", Rect(200, 0, 40, 30), 1, 9f, "sport"));
        var set = BuildingSet.For(world, 4u);
        var module = new BuildingsModule();
        Require(module is IWindowNumberingProvider and IWindowMetadataSource, "the registered module is the provider");
        var snapshot = module.PrepareWindowNumbering(Ctx(world, 4u), []);
        // The numbers the generator gave before the provider: running sum of columns × storeys in (feature, key) order.
        var next = 0;
        Require(snapshot.Allocations.Count == set.Plans.Count, "one block per generic plan");
        for (var i = 0; i < set.Plans.Count; i++)
        {
            var (plan, a) = (set.Plans[i], snapshot.Allocations[i]);
            Require(a.Feature == plan.Id && a.Key == plan.Key && a.BaseId == next && a.Columns == plan.TotalColumns && a.Rows == plan.Storeys,
                $"{plan.Key}: base {a.BaseId}, expected {next}");
            next += plan.TotalColumns * plan.Storeys;
        }
        Require(snapshot.WindowCount == next && snapshot.Map.Value == "test", "the whole map, nothing else");
        Require(snapshot.Fingerprint.Length == 64 && snapshot.Fingerprint.All(c => char.IsAsciiHexDigitLower(c) || char.IsAsciiDigit(c)) &&
            module.PrepareWindowNumbering(Ctx(world, 4u), []).Fingerprint == snapshot.Fingerprint, "stable lowercase SHA-256");
        // Build and the metadata read that table: same glass and the same declarations as the generic blocks.
        var ctx = Ctx(world, 4u, snapshot);
        var declared = new List<WindowDecl>();
        for (var i = 0; i < set.Plans.Count; i++)
        {
            var output = module.Build(ctx, set.Plans[i].Tile(LodLevel.L0), LodLevel.L0);
            var glass = output.Meshes.Where(m => m.Material.Spec.Kind == ToonKind.Glass).ToArray();
            Require(glass.Length > 0 && glass.All(g => g.Uv2!.Where((_, k) => k % 2 == 0).All(x => x == set.Blocks[i].Base)), $"{set.Plans[i].Key}: glass keeps its base");
            var expected = FacadeOpenings.Declarations(set.Plans[i], set.Blocks[i]);
            Require(output.LooseWindows.Select(d => d.Id).SequenceEqual(expected.Select(d => d.Id)), $"{set.Plans[i].Key}: the same window numbers");
            declared.AddRange(output.LooseWindows);
        }
        Require(module.DescribeWindows(ctx).Windows.Select(d => d.Id).SequenceEqual(declared.Select(d => d.Id)), "metadata lists the L0 declarations");
        Expect<InvalidOperationException>(() => module.Build(Ctx(world, 4u), set.Plans[0].Tile(LodLevel.L0), LodLevel.L0), "no snapshot, no windows");
        var other = new WindowNumberingSnapshot(new MapId("elsewhere"), snapshot.Fingerprint, snapshot.Allocations);
        Expect<InvalidOperationException>(() => module.DescribeWindows(Ctx(world, 4u, other)), "another map's table");
    }

    [TestCase]
    public void AClaimedLayoutReplacesItsWholeFeature()
    {
        // t:a has two building parts; its zone pack claims the facade and numbers it as one 7 × 3 shell, sorted before t:b.
        var parts = new[]
        {
            new WorldFeaturePart("osm:way:1", new PolygonGeometry(new PolygonRings(Rect(0, 0, 30, 12))), 21f, 6),
            new WorldFeaturePart("osm:way:2", new PolygonGeometry(new PolygonRings(Rect(40, 0, 20, 12))), 7f, 2),
        };
        var world = World(Feature("t:a", Rect(0, 0, 60, 12), 6, 21f, "teaching") with { Parts = parts },
            Feature("t:b", Rect(200, 0, 30, 12), 3, 10.5f, "teaching"));
        var generic = WindowNumbering.For(world, 1u);
        var module = new BuildingsModule();
        var snapshot = module.PrepareWindowNumbering(Ctx(world, 1u, claims: ["t:a"]), [Layout("t:a", ("shell", 7, 3))]);
        Require(snapshot.Allocations.Count == 2 && snapshot.Get(new("t:a"), "shell") == new WindowAllocation(new("t:a"), "shell", 0, 7, 3),
            "both generic blocks of t:a gave way to the declared shell");
        Expect<KeyNotFoundException>(() => snapshot.Get(new("t:a"), "osm:way:1"), "no generic block left for a claimed part");
        var b = snapshot.Get(new("t:b"), "t:b");
        Require(b.BaseId == 21 && generic[2].Base != 21, $"t:b moves to just behind the shell (21, generic {generic[2].Base})");
        // The enable gate: glass and metadata both decode the moved base, never the generic one.
        var ctx = Ctx(world, 1u, snapshot, ["t:a"]);
        var tileB = BuildingSet.For(world, 1u).Plans.Single(p => p.Id.Value == "t:b").Tile(LodLevel.L0);
        Require(module.Tiles(ctx).SequenceEqual([tileB]), "the claimed feature gets no cell");
        foreach (var lod in new[] { LodLevel.L0, LodLevel.L1, LodLevel.L2, LodLevel.L3 })
            foreach (var g in module.Build(ctx, tileB, lod).Meshes.Where(m => m.Material.Spec.Kind == ToonKind.Glass))
                for (var i = 0; i < g.Position.Length / 3; i++)
                {
                    var id = (int)g.Uv2![2 * i] + (int)MathF.Floor(g.Uv![2 * i + 1]) * b.Columns + (int)MathF.Floor(g.Uv[2 * i]);
                    Require(g.Uv2[2 * i] == b.BaseId && id >= b.BaseId && id < b.BaseId + b.Count, $"{lod}: glass window {id} inside t:b's new block");
                }
        var metadata = module.DescribeWindows(ctx).Windows;
        Require(metadata.Count > 0 && metadata.All(d => d.BaseId == b.BaseId && d.Id >= b.BaseId && d.Id < b.BaseId + b.Count), "metadata on the new block");
        Require(module.Build(ctx, BuildingSet.For(world, 1u).Plans[0].Tile(LodLevel.L0), LodLevel.L0).Meshes.Count == 0, "WP-14 builds nothing of t:a");
    }

    [TestCase]
    public void AnEmptyLayoutDeclaresNoWindowsInAnyOrder()
    {
        var world = World(Feature("t:a", Rect(0, 0, 30, 12), 3, 10.5f, "teaching"), Feature("t:b", Rect(100, 0, 30, 12), 3, 10.5f, "teaching"),
            Feature("t:c", Rect(200, 0, 30, 12), 3, 10.5f, "teaching"));
        var module = new BuildingsModule();
        var ctx = Ctx(world, 1u, claims: ["t:a", "t:b"]);
        var snapshot = module.PrepareWindowNumbering(ctx, [Layout("t:b"), Layout("t:a", ("tower", 2, 5), ("main", 4, 3))]);
        Require(snapshot.Allocations.Select(a => (a.Feature.Value, a.Key, a.BaseId)).SequenceEqual([("t:a", "main", 0), ("t:a", "tower", 12), ("t:c", "t:c", 22)]),
            "ordinal (feature, key) order; t:b takes no IDs");
        Expect<KeyNotFoundException>(() => snapshot.Get(new("t:b"), "t:b"), "Grids = [] is no windows, not a fallback");
        var again = module.PrepareWindowNumbering(ctx, [Layout("t:a", ("main", 4, 3), ("tower", 2, 5)), Layout("t:b")]);
        Require(again.Fingerprint == snapshot.Fingerprint && again.Allocations.SequenceEqual(snapshot.Allocations), "declaration order does not matter");
        Require(module.PrepareWindowNumbering(ctx, [Layout("t:a", ("main", 4, 3), ("tower", 2, 5))]).Fingerprint != snapshot.Fingerprint,
            "an omitted empty declaration is a different input");
    }

    [TestCase]
    public void InvalidLayoutsPublishNoTable()
    {
        var parts = new[] { new WorldFeaturePart("osm:way:1", new PolygonGeometry(new PolygonRings(Rect(0, 0, 30, 12))), 10.5f, 3) };
        var shared = new[] { parts[0], new WorldFeaturePart("osm:way:2", new PolygonGeometry(new PolygonRings(Rect(40, 0, 20, 12))), 7f, 2) };
        var world = World(Feature("t:a", Rect(0, 0, 30, 12), 3, 10.5f, "teaching") with { Parts = parts },
            Feature("t:b", Rect(0, 0, 60, 12), 3, 10.5f, "teaching") with { Parts = shared });
        var module = new BuildingsModule();
        var ctx = Ctx(world, 1u, claims: ["t:a", "t:b"]);
        Expect<ArgumentException>(() => module.PrepareWindowNumbering(ctx, [Layout("t:a", ("x", 1, 1)), Layout("t:a")]), "two layouts for one feature");
        Expect<ArgumentException>(() => module.PrepareWindowNumbering(ctx, [Layout("t:a", ("x", 1, 1), ("x", 2, 2))]), "a key twice");
        Expect<ArgumentException>(() => module.PrepareWindowNumbering(ctx, [Layout("t:a", ("x", 0, 3))]), "an empty grid");
        Expect<ArgumentException>(() => module.PrepareWindowNumbering(ctx, [Layout("t:zz", ("x", 1, 1))]), "a feature not on the map");
        var roofOnly = new BuildContext { World = world, Map = TestMap, WorldSeed = 1u, ClaimedBy = (_, aspect) => aspect == Aspects.Roof ? new WpId("WP-18") : null };
        Expect<ArgumentException>(() => module.PrepareWindowNumbering(roofOnly, [Layout("t:a")]), "a roof claim grants no window layout");
        // osm:way:1 is numbered by t:a (first in order). t:b may leave it there and number its own grid (the Liren connector
        // beside the generic A/B blocks), but a grid renumbering that footprint would number one facade twice.
        var keep = module.PrepareWindowNumbering(ctx, [Layout("t:b", ("main", 3, 3))]);
        Require(keep.Get(new("t:a"), "osm:way:1").Count == WindowNumbering.For(world, 1u)[0].Count && keep.Get(new("t:b"), "main").Count == 9,
            "the shared footprint keeps its generic block beside the claimed grid");
        Expect<InvalidOperationException>(() => module.PrepareWindowNumbering(ctx, [Layout("t:b", ("osm:way:1", 3, 3))]), "a shared footprint numbered twice");
        Require(module.PrepareWindowNumbering(ctx, [Layout("t:a"), Layout("t:b", ("osm:way:1", 3, 3))]).WindowCount == 9, "once its holder is claimed too");
    }

    [TestCase]
    public void TheTableStopsAtTheOverrideCapacity()
    {
        var world = World(Feature("t:a", Rect(0, 0, 30, 12), 1, 3.5f, "service"), Feature("t:b", Rect(100, 0, 30, 12), 3, 10.5f, "teaching"));
        var generic = WindowNumbering.For(world, 1u)[1].Count;
        var module = new BuildingsModule();
        var ctx = Ctx(world, 1u, claims: ["t:a"]);
        var full = module.PrepareWindowNumbering(ctx, [Layout("t:a", ("strip", WindowNumberingSnapshot.Capacity - generic, 1))]);
        var last = full.Get(new("t:b"), "t:b");
        Require(full.WindowCount == WindowNumberingSnapshot.Capacity && last.IdAt(last.Columns - 1, last.Rows - 1) == 65535, "exactly 65536 IDs fit, the last is 65535");
        Expect<InvalidOperationException>(() => module.PrepareWindowNumbering(ctx, [Layout("t:a", ("strip", WindowNumberingSnapshot.Capacity - generic + 1, 1))]),
            "one more is refused, not wrapped or truncated");
        Expect<InvalidOperationException>(() => module.PrepareWindowNumbering(ctx, [Layout("t:a", ("strip", int.MaxValue, 2))]), "overflowing sizes are refused");
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

    private static void Expect<T>(Action action, string what) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}: {what}");
    }

    private static readonly MapDescriptor TestMap = new(new MapId("test"), new LocalizedName("测试", "Test"), MapKind.Outdoor, 1, 1u,
        new GeoFrame(0d, 0d, 32648, new(-1000f, -1000f), new(1000f, 1000f)), Vector2.Zero,
        new Box3(new(-1024f, -28f, -1024f), new(1024f, 2000f, 1024f)), new MapRect(new(-1024f, -1024f), new(1024f, 1024f)),
        new RoamLimits(0.9f, 30f, 3000f, -28f, 2000f, 30f, 40f, 150f), new TileKey(-16, -16), new TileKey(15, 15), "P00", [],
        new MapWorldRules(MapTimeRule.Shared, new WorldPreset(17.5, Season.Autumn), new LocalizedName("落金初灯", "Golden Hour"), true, "chengdu"),
        new MapEnvironment("sky.json", "backdrop.json", []), new Dictionary<string, string>(), []);

    /// <summary>A build context as the kernel hands it out: <paramref name="claims"/> are facade claims of a zone pack.</summary>
    private static BuildContext Ctx(IWorldModel world, uint seed, WindowNumberingSnapshot? numbering = null, string[]? claims = null) => new()
    {
        World = world,
        Map = TestMap,
        WorldSeed = seed,
        WindowNumbering = numbering,
        ClaimedBy = (feature, aspect) => aspect == Aspects.Facade && claims?.Contains(feature.Value) == true ? new WpId("WP-18") : null,
    };

    /// <summary>The kernel's prepass stand-in until WP-02 runs it: the provider's snapshot, published in the context.</summary>
    private static BuildContext Prepared(BuildContext ctx) => new()
    {
        World = ctx.World,
        Map = ctx.Map,
        WorldSeed = ctx.WorldSeed,
        ClaimedBy = ctx.ClaimedBy,
        WindowNumbering = new BuildingsModule().PrepareWindowNumbering(ctx, []),
    };

    private static ClaimedWindowLayout Layout(string feature, params (string Key, int Columns, int Rows)[] grids) =>
        new(new FeatureId(feature), [.. grids.Select(g => new WindowGridSpec(g.Key, g.Columns, g.Rows))]);

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

    /// <summary>Every footprint edge one segment: Zone above the ground band, GroundBand within it; roofs in <paramref name="roof"/>.</summary>
    private sealed class ZonedFidelity(FidelityZone zone, FidelityZone ground, FidelityZone roof = FidelityZone.K4) : IFidelityModel
    {
        public IReadOnlyDictionary<FeatureId, WorldFeature> Features { get; set; } = new Dictionary<FeatureId, WorldFeature>();
        public string InputHash => "test";
        public IReadOnlyList<FacadeSegment> Facades(FeatureId building)
        {
            var ring = ((PolygonGeometry)Features[building].Geometry).Rings.Outer;
            return [.. ring.Select((a, i) => new FacadeSegment(building, i, a, ring[(i + 1) % ring.Count], zone, ground))];
        }
        public RoofZone Roof(FeatureId building) => new(building, roof);
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
