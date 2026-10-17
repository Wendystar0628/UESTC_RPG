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
        Require(plan.Entrances.Count == 1 && plan.Entrances[0].Anchor is null, "the generic entrance on the longest facade");
        Require(set.WindowCount == plan.TotalColumns * plan.Storeys, "window block covers the building grid");
        var bundles = new BuildingsModule().BuildOne(plan, set.Blocks[0], LodLevel.L0, Campus.Contracts.Tests.PaletteFixture.Catalog);
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
                Require((g.Reserved![i] & WindowCardEncoding.TypeMask) is >= 1 and <= 15, "window card type in the spare byte");
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
                var bundles = module.BuildOne(set.Plans[i], set.Blocks[i], lod, Campus.Contracts.Tests.PaletteFixture.Catalog);
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
        var b1 = new BuildingsModule().BuildOne(s1.Plans[0], s1.Blocks[0], LodLevel.L0, Campus.Contracts.Tests.PaletteFixture.Catalog);
        var b2 = new BuildingsModule().BuildOne(s2.Plans[0], s2.Blocks[0], LodLevel.L0, Campus.Contracts.Tests.PaletteFixture.Catalog);
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
                foreach (var g in module.BuildOne(plan, block, lod, Campus.Contracts.Tests.PaletteFixture.Catalog).Where(x => x.Material.Spec.Kind == ToonKind.Glass))
                    for (var i = 0; i < g.Position.Length / 3; i++)
                    {
                        Require(g.Uv2![2 * i] == block.Base && (int)MathF.Round(g.Uv2[2 * i + 1]) == plan.Profile.Channel + 32 * plan.TotalColumns,
                            $"{plan.Key} {lod}: UV2 is (BaseId, slot + 32 × columns)");
                        var id = block.Base + (int)MathF.Floor(g.Uv![2 * i + 1]) * plan.TotalColumns + (int)MathF.Floor(g.Uv[2 * i]);
                        Require(expected.TryGetValue(id, out var card) && (g.Reserved![i] & WindowCardEncoding.TypeMask) == card, $"{plan.Key} {lod}: window {id} keeps its cell and card");
                        seen.Add(id);
                    }
                Require(seen.SetEquals(expected.Keys), $"{plan.Key} {lod}: every opening has glass");
            }
        }
    }

    private static bool Sash(GeometryBundle b) => b.Material.Spec.Lit.Value == "frame.alu.lit" && b.Material.Spec.Fx == ToonFx.Rim;
    // The WP-10 gasket class (dark equipment steel without outline), not the WP-12 roof fence of the same colour.
    private static bool Gasket(GeometryBundle b) => HasColor(b, "equip.fence.lit") && b.Material.Spec.Line == LineClass.None;
    // WP-10 curtain fabric; the WP-12 heat-pump shrouds share its shading but not its colours.
    private static bool Fabric(GeometryBundle b) => b.Material.Spec.Normal == ToonNormal.Smooth && b.Material.Spec.Steps == ShadeSteps.TwoHalf &&
        !b.Material.Spec.Lit.Value.StartsWith("equip.", StringComparison.Ordinal);

    [TestCase]
    public void PhysicalCurtainFlagMatchesActualPerOpeningGeometryAtEachLodAndZone()
    {
        var world = World(Feature("t:dorm", Rect(0, 0, 60, 18), 6, 19.8f, "residence"));
        var set = BuildingSet.For(world, 1u);
        var plan = set.Plans[0];
        var present = 0;
        var absent = 0;
        foreach (var zone in new[] { FidelityZone.K1, FidelityZone.K2, FidelityZone.K3 })
            foreach (var lod in new[] { LodLevel.L0, LodLevel.L1, LodLevel.L2, LodLevel.L3 })
                for (var fi = 0; fi < plan.Facades.Count; fi++)
                {
                    var layout = plan.Facades[fi];
                    var frame = Mesh.EdgeFrame.Of(plan.SkinAt(layout.Ring), layout.Edge);
                    foreach (var opening in plan.Openings[fi].Where(o => o.Kind != BayKind.Door))
                    {
                        var sink = new Mesh.MeshSink(Campus.Contracts.Tests.PaletteFixture.Catalog);
                        var openings = new FacadeOpenings(plan, set.Blocks[0], lod, sink);
                        if (lod <= LodLevel.L1) openings.Fill(frame, layout, opening, zone);
                        else openings.FlatGlass(frame, layout, opening, -plan.Reveal);
                        var meshes = sink.Finish();
                        var actualCurtain = meshes.Any(Fabric);
                        if (actualCurtain) present++; else absent++;
                        Require(!actualCurtain || (zone <= FidelityZone.K2 && lod == LodLevel.L0), "Physical curtain remains confined to the existing near LOD/zone.");
                        var glass = meshes.Where(m => m.Material.Spec.Kind == ToonKind.Glass).ToArray();
                        Require(glass.Length > 0, "Each opening still has glass.");
                        var card = (byte)FacadeOpenings.Card(plan, opening, FacadeOpenings.Number(plan, set.Blocks[0], layout, opening));
                        foreach (var mesh in glass)
                            foreach (var encoded in mesh.Reserved!)
                            {
                                Require((encoded & WindowCardEncoding.TypeMask) == card, "Presence flag preserves the window card type.");
                                Require(((encoded & WindowCardEncoding.PhysicalCurtainFlag) != 0) == actualCurtain,
                                    $"{zone} {lod} bay {opening.Bay}: flag exactly matches actual curtain geometry.");
                            }
                    }
                }
        Require(present > 0 && absent > 0, "Exercise actual curtains and uncurtained openings, including distant LODs.");
    }

    [TestCase]
    public void K1AndK2OpeningsTakeTheWp10Parts()
    {
        // The walking corridor's ground band is K2, the facade above it K3 (ENG-ARCH FacadeSegment).
        var world = World(new ZonedFidelity(FidelityZone.K3, FidelityZone.K2), Feature("t:dorm", Rect(0, 0, 60, 18), 6, 19.8f, "residence"));
        var set = BuildingSet.For(world, 1u);
        var plan = set.Plans[0];
        for (var fi = 0; fi < plan.Facades.Count; fi++)
            for (var k = 0; k < plan.Openings[fi].Length; k++)
                Require(plan.ZoneOf(fi, k) == (plan.Openings[fi][k].Row == 0 ? FidelityZone.K2 : FidelityZone.K3), "ground floor in the K2 band, the rest K3");
        var module = new BuildingsModule();
        var near = module.BuildOne(plan, set.Blocks[0], LodLevel.L0, Campus.Contracts.Tests.PaletteFixture.Catalog);
        var mid = module.BuildOne(plan, set.Blocks[0], LodLevel.L1, Campus.Contracts.Tests.PaletteFixture.Catalog);
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
                        var sink = new Mesh.MeshSink(Campus.Contracts.Tests.PaletteFixture.Catalog);
                        new Build.FacadeOpenings(plan, set.Blocks[i], lod, sink).Fill(frame, layout, o, FidelityZone.K3);
                        levelMax = Math.Max(levelMax, sink.Triangles - 2);
                    }
                }
                Require(levelMax is >= 16 and <= 60, $"{plan.Key} {lod}: a simple window is 40–60 triangles at most ({levelMax})");
            }
            var near = module.BuildOne(plan, set.Blocks[i], LodLevel.L0, Campus.Contracts.Tests.PaletteFixture.Catalog);
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
        static bool Pump(GeometryBundle b) => HasColor(b, "equip.grey.lit");
        // The fence boards in the light metal grey of RoofRules.FenceBoard (the board class: building line, no effects).
        static bool Fence(GeometryBundle b) => HasColor(b, Rules.RoofRules.FenceBoard + ".lit") &&
            b.Material.Spec.Line == LineClass.Building && b.Material.Spec.Fx == ToonFx.None;
        static bool Strip(GeometryBundle b) => HasColor(b, "roof.silverMetal.lit") && b.Material.Spec.Fx.HasFlag(ToonFx.Rim);
        static bool Tank(GeometryBundle b) => HasColor(b, "roof.silverMetal.lit") && b.Material.Spec.Fx == ToonFx.BevelLift;
        static int Tris(IEnumerable<GeometryBundle> bs, string token) => bs.Sum(b => Enumerable.Range(0, b.Index.Length / 3).Count(t => TriangleColor(b, t * 3, token)));
        for (var i = 0; i < plain.Plans.Count; i++)
        {
            var (simple, full) = (plain.Plans[i], hero.Plans[i]);
            Require(simple.RoofZone == FidelityZone.K4 && full.RoofZone == FidelityZone.K1, "roof zone from the fidelity data, K4 without it");
            var near = module.BuildOne(simple, plain.Blocks[i], LodLevel.L0, Campus.Contracts.Tests.PaletteFixture.Catalog);
            var nearFull = module.BuildOne(full, hero.Blocks[i], LodLevel.L0, Campus.Contracts.Tests.PaletteFixture.Catalog);
            Require(near.Any(Fence) && near.Any(Pump) && near.Any(Strip), $"{simple.Key}: fenced plant and lightning strip");
            Require(!near.Concat(nearFull).Any(b => HasColor(b, "equip.fence.lit") && b.Material.Spec.Line == LineClass.Building),
                $"{simple.Key}: no dark fence steel left on the boards");
            Require((simple.Style == Rules.BuildingStyle.Research) == near.Any(Tank), $"{simple.Key}: a water tank beside teaching plant only");
            Require(Tris(nearFull.Where(Pump), "equip.grey.lit") > Tris(near.Where(Pump), "equip.grey.lit"), $"{simple.Key}: a K1 roof carries more and finer units");
            Require(Tris(nearFull.Where(Strip), "roof.silverMetal.lit") > Tris(near.Where(Strip), "roof.silverMetal.lit"), $"{simple.Key}: K1 strip on supports, elsewhere one rod");
            var mid = module.BuildOne(simple, plain.Blocks[i], LodLevel.L1, Campus.Contracts.Tests.PaletteFixture.Catalog);
            Require(!mid.Any(Strip) && module.BuildOne(full, hero.Blocks[i], LodLevel.L1, Campus.Contracts.Tests.PaletteFixture.Catalog).Any(Strip), $"{simple.Key}: the rod stops at L0 on simple roofs");
            // Coping: the WP-12 cap reaches the parapet top, its drip hangs 60 mm under the slab outside the wall.
            var trim = near.Where(b => HasColor(b, Rules.Materials.Lit(simple.Profile.Trim).Value) && b.Material.Spec.Fx == ToonFx.BevelLift).ToArray();
            var ys = trim.SelectMany(b => ColorVertices(b, Rules.Materials.Lit(simple.Profile.Trim).Value).Select(v => b.Position[3 * v + 1])).ToArray();
            Require(ys.Any(y => MathF.Abs(y - simple.EdgeTopY) < 1e-3f) && ys.Any(y => MathF.Abs(y - (simple.EdgeTopY - 0.21f)) < 1e-3f), $"{simple.Key}: coping and drip");
            foreach (var b in near.Where(b => Fence(b) || Pump(b)))
                foreach (var v in ColorVertices(b, Rules.RoofRules.FenceBoard + ".lit", "equip.grey.lit"))
                {
                    var p = new Vector2(b.Position[3 * v], b.Position[3 * v + 2]);
                    Require(PlanRing.InsideFootprint(simple.Outer, simple.Holes, p) && PlanRing.DistanceToEdges(simple.Outer, simple.Holes, p) > 0.4f &&
                        b.Position[3 * v + 1] >= simple.RoofY - 1e-3f, $"{simple.Key}: plant stands on the roof, clear of the parapet");
                }
        }
    }

    [TestCase]
    public void OrdinaryBuildingsKeepTheirOpeningsAndNumbers()
    {
        // Buildings without an entrance anchor keep 14-2b's layout cell for cell: openings, window numbers and cards (hash
        // of that layout taken on b66d4df). Anchored buildings only move their door; their window blocks never change.
        var features = new[]
        {
            Feature("t:dorm", Rect(0, 0, 60, 18), 6, 19.8f, "residence"), Feature("t:lab", new PolygonRings(LShape(100, 0)), 4, 15.6f, "research"),
            Feature("t:court", Courtyard(0, 100), 3, 12f, "teaching"), Feature("t:hall", Rect(200, 0, 40, 30), 1, 9f, "sport"),
            Feature("t:small", Rect(300, 0, 5, 6), 1, 3f, "service"),
        };
        var plain = BuildingSet.For(World(features), 4u);
        Require(LayoutHash(plain) == "f3e3eb7b1d0265df", $"14-2b layout of ordinary buildings ({LayoutHash(plain)})");
        var anchored = BuildingSet.For(World(new Entrances(Anchor("t:dorm", "a", new(21.3f, 0f, 0f), 0f)), features), 4u);
        Require(anchored.Blocks.SequenceEqual(plain.Blocks), "an entrance anchor leaves every window block as it was");
        Require(Layout(anchored).Replace(Layout(anchored, "t:dorm"), "") == Layout(plain).Replace(Layout(plain, "t:dorm"), ""),
            "the other buildings keep their layout");
    }

    [TestCase]
    public void FacadePresetsChangeOnlyTheirBuilding()
    {
        // MAP-QSH-SOUTH on one research block among ordinary buildings: larger glass in the same window cells; every
        // window block, and every other building cell for cell, as before.
        var preset = Rules.FacadePresets.ResearchStoneFrame;
        var features = new[]
        {
            Feature("t:dorm", Rect(0, 0, 60, 18), 6, 19.8f, "residence"), Feature("t:lab", new PolygonRings(LShape(100, 0)), 4, 15.6f, "research"),
            Feature("t:court", Courtyard(0, 100), 3, 12f, "teaching"), Feature("t:hall", Rect(200, 0, 40, 30), 1, 9f, "sport"),
        };
        var plain = BuildingSet.For(World(features), 4u);
        var styled = BuildingSet.For(World([.. features.Select(f => f.Id.Value == "t:lab" ? f with { Facade = new FacadePreset(preset.Key) } : f)]), 4u);
        Require(styled.Blocks.SequenceEqual(plain.Blocks), "every window block as before");
        Require(Layout(styled, where: p => p.Key != "t:lab") == Layout(plain, where: p => p.Key != "t:lab"), "the other buildings cell for cell");
        var (before, after) = (plain.Plans.Single(p => p.Key == "t:lab"), styled.Plans.Single(p => p.Key == "t:lab"));
        Require(after.Profile.Preset == preset.Key && before.Profile.Preset is null && after.Style == before.Style, "the preset over the research family");
        static string Cells(BuildingPlan p) => string.Join(" ", p.Openings.SelectMany((o, fi) => o.Select(x => $"{fi}/{x.Bay}/{x.Row}/{x.Kind}")));
        Require(Cells(after) == Cells(before), "the same window cells and kinds, so the same window numbers and cards");
        static float Glazed(BuildingPlan p) => p.Openings.Sum(o => o.Sum(x => (x.U1 - x.U0) * (x.Y1 - x.Y0)));
        Require(Glazed(after) > 1.6f * Glazed(before), $"more and larger glass: {Glazed(before):0} → {Glazed(after):0} m²");

        // 玻璃色, 框架材质 and 窗型 reach the meshes at every level: a fixture preset with colours of its own.
        var tinted = new Rules.FacadeLook("t.tinted") { Glass = "uestc.techblue", Frame = "wall.creamCoffee", Trim = "panel.brown", Window = PartCatalog.CurtainWall };
        var lab = features[1] with { Facade = new FacadePreset(tinted.Key) };
        foreach (var (name, world) in new[] { ("K3", World(lab)), ("K1", World(new ZonedFidelity(FidelityZone.K1, FidelityZone.K1), lab)) })
        {
            var plan = BuildingPlanner.Plan(BuildingPlanner.Sources(lab).Single(), world, 4u, [tinted])!;
            var block = new WindowBlock(plan.Id, plan.Key, 0, plan.TotalColumns, plan.Storeys);
            foreach (var lod in Levels)
            {
                var bundles = new BuildingsModule().BuildOne(plan, block, lod, Campus.Contracts.Tests.PaletteFixture.Catalog);
                foreach (var b in bundles) b.ValidateVertexData();
                var glass = bundles.Where(b => b.Material.Spec.Kind == ToonKind.Glass).ToArray();
                Require(lod == LodLevel.L4 || (glass.Length > 0 && glass.All(g => ColorVertices(g, "uestc.techblue.lit").Count() == g.Position.Length / 3)), $"{name} {lod}: glass in the preset's colour");
                var walls = bundles.Where(b => b.Material.Spec.Kind == ToonKind.Static && b.Material.Spec.Fx == (ToonFx.BevelLift | ToonFx.Mottle)).ToArray();
                Require(walls.Length > 0 && walls.All(b => ColorVertices(b, "wall.creamCoffee.lit").Count() == b.Position.Length / 3), $"{name} {lod}: the wall frame in the preset's colour");
                Require(!bundles.Any(Fabric), $"{name} {lod}: curtain walls hang no curtains");
            }
        }
        Require(BuildingsModule.UnknownPreset(BuildingSet.For(World(lab), 4u).Plans.Single()).Single().Level == DiagnosticLevel.Warning &&
            BuildingSet.For(World(lab), 4u).Plans.Single().Profile.Preset is null, "a key WP-14 does not know keeps the family, with a warning");

        // The real campus with the innovation centre's five buildings on the preset (the StyleOps of overlay/south.d/): no
        // window number of the map moves, the other buildings stay cell for cell, and every footprint of the five takes it.
        string[] innovation = ["qsh:relation:10807928", "qsh:relation:10807929", "qsh:relation:17732622", "qsh:way:779496164", "qsh:way:779496165"];
        var real = RealCampus();
        var withPreset = new StyledWorld(real, f => innovation.Contains(f.Id.Value) ? f with { Family = new BuildingFamily("research"), Facade = new FacadePreset(preset.Key) } : f);
        var (was, now) = (BuildingSet.For(real, 1u), BuildingSet.For(withPreset, 1u));
        Require(now.Blocks.SequenceEqual(was.Blocks), "real campus: every window block as before");
        Require(Layout(now, where: p => !innovation.Contains(p.Id.Value)) == Layout(was, where: p => !innovation.Contains(p.Id.Value)), "real campus: the other buildings cell for cell");
        var centre = now.Plans.Where(p => innovation.Contains(p.Id.Value)).ToArray();
        Require(centre.Length >= 10 && centre.All(p => p.Profile.Preset == preset.Key && p.Style == Rules.BuildingStyle.Research), $"the centre's {centre.Length} footprints take the preset");
    }

    /// <summary>The five dormitory presets of the north zone as WP-20's table gives them (MAP-QSH-NORTH).</summary>
    private static readonly (string Key, float Glazing, float WindowHeight, float Sill, float GroundSill, string Glass, string Frame, string Trim, string Accent, string Window)[] DormTable =
    [
        ("dorm.a.dormYellow", 0.50f, 1.60f, 1.00f, 1.00f, "glass", "wall.dormYellow", "wall", "band.terracotta", "opening.window.casement"),
        ("dorm.a.creamCoffee", 0.50f, 1.60f, 1.00f, 1.00f, "glass", "wall.creamCoffee", "wall", "band.terracotta", "opening.window.casement"),
        ("dorm.b", 0.62f, 1.75f, 0.85f, 1.00f, "glass", "wall", "wall.greyWhite", "panel.pinkSand", "opening.window.casement"),
        ("dorm.master.stoneBeige", 0.55f, 1.80f, 0.80f, 0.90f, "glass", "stone.beige", "wall.greyWhite", "stone.beige", "opening.window.casement"),
        ("dorm.master.greyWhite", 0.55f, 1.80f, 0.80f, 0.90f, "glass", "wall.greyWhite", "stone.beige", "stone.beige", "opening.window.casement"),
        ("dorm.master.creamCoffee", 0.55f, 1.80f, 0.80f, 0.90f, "glass", "wall.creamCoffee", "wall", "wall.creamCoffee", "opening.window.casement"),
        ("dorm.master.creamCoffeeWhite", 0.55f, 1.80f, 0.80f, 0.90f, "glass", "wall.creamCoffee", "wall", "wall", "opening.window.casement"),
    ];

    /// <summary>The three annex presets of the north zone (WP-20 DormPresets.json, WLD-INTERIORS) with the family each one lives in.</summary>
    private static readonly (string Key, string Family, Rules.BuildingStyle Style, float Glazing, float WindowHeight, float Sill, float GroundSill, string Frame, string Trim, string Window)[] AnnexTable =
    [
        ("annex.service.creamCoffee", "service", Rules.BuildingStyle.Service, 0.60f, 2.00f, 0.90f, 0.90f, "wall.creamCoffee", "wall", "opening.window.casement"),
        ("annex.utility.creamCoffee", "utility", Rules.BuildingStyle.Utility, 0.38f, 1.20f, 1.60f, 1.60f, "wall.creamCoffee", "wall", "opening.window.high"),
        ("annex.utility.greyWhite", "utility", Rules.BuildingStyle.Utility, 0.45f, 1.40f, 1.20f, 1.40f, "wall.greyWhite", "wall", "opening.window.high"),
    ];

    [TestCase]
    public void DormPresetsMatchTheTable()
    {
        var all = Rules.FacadePresets.All;
        Require(all.Select(p => p.Key).Distinct(StringComparer.Ordinal).Count() == all.Count, "one preset per key");
        var tokens = PaletteTokens();
        foreach (var (key, glazing, windowHeight, sill, groundSill, glass, frame, trim, accent, window) in DormTable)
        {
            var look = Rules.FacadePresets.Find(new FacadePreset(key)) ?? throw new InvalidOperationException($"Expected: {key} is a known preset");
            Require(look.Key == key, $"{key} is found under its own key");
            Require(look.Glazing == glazing && look.WindowHeight == windowHeight && look.Sill == sill && look.GroundSill == groundSill, $"{key}: the window's size");
            Require(look.Glass == glass && look.Frame == frame && look.Trim == trim && look.Accent == accent && look.Window == window, $"{key}: colours and window part");
            foreach (var family in new[] { glass, frame, trim, accent })
                Require(tokens.Contains(family + ".lit") && tokens.Contains(family + ".shade"), $"{key}: {family} is in the palette");
            // Only type A puts its 陶红 as aprons under the windows; the masters' accent takes the window column whole (on the stone frame it is no band).
            Require(look.AccentApron == (key.StartsWith("dorm.a.", StringComparison.Ordinal) ? true : (bool?)null), $"{key}: the accent as aprons");
        }
        Require(Rules.FacadePresets.Find(new FacadePreset("dorm.a.nothing")) is null, "an unknown key stays unknown");
    }

    [TestCase]
    public void DormPresetAccentDoesNotFollowTheClusterHash()
    {
        // DormA (pink-sand spandrels) and DormB (brown) by cluster: the same footprint on two world seeds, each way.
        var feature = Feature("t:dorm", Rect(0, 0, 60, 18), 6, 19.2f, "residence");
        uint? seedA = null, seedB = null;
        for (var seed = 1u; seed <= 64u && (seedA is null || seedB is null); seed++)
            switch (BuildingSet.For(World(feature), seed).Plans[0].Profile.Accent)
            {
                case "panel.pinkSand": seedA ??= seed; break;
                case "panel.brown": seedB ??= seed; break;
                default: break;
            }
        Require(seedA is not null && seedB is not null, "seeds for both dormitory variants");
        foreach (var (key, _, _, _, _, _, frame, trim, accent, _) in DormTable)
        {
            var styled = feature with { Facade = new FacadePreset(key) };
            var (a, b) = (BuildingSet.For(World(styled), seedA!.Value), BuildingSet.For(World(styled), seedB!.Value));
            Require(a.Plans[0].Profile == b.Plans[0].Profile && a.Plans[0].Profile.Accent == accent, $"{key}: one profile under either hash");
            foreach (var set in new[] { a, b })
            {
                var lit = new HashSet<string>(new BuildingsModule().BuildOne(set.Plans[0], set.Blocks[0], LodLevel.L0, Campus.Contracts.Tests.PaletteFixture.Catalog).SelectMany(m => m.ColorIndices!.Select(i => Campus.Contracts.Tests.PaletteFixture.Catalog.Get(new PaletteIndex(i)).Token.Value)));
                Require(lit.Contains(accent + ".lit") && lit.Contains(frame + ".lit") && lit.Contains(trim + ".lit"), $"{key}: frame, trim and accent reach the mesh");
                Require(!lit.Contains("panel.brown.lit") && (accent == "panel.pinkSand") == lit.Contains("panel.pinkSand.lit"), $"{key}: no spandrel colour of the family's own");
            }
        }
    }

    [TestCase]
    [TestCase]
    public void TypeAAccentIsTheApronUnderTheWindow()
    {
        // The 陶红 of type A sits only under the windows, as wide as the window, floor to sill: the area of the L0 mesh is the
        // sum of those aprons and no triangle leaves a floor-to-sill band. Against the wall (the facade from the plinth top to
        // the roof line, less its windows and doors) on 6 storeys of 19.2 m, the north zone's dormitory (3.175 m a storey), that
        // is about a sixth (0.16–0.18 over these four plan shapes) where lintels and aprons together took about a quarter.
        var full = Rules.FacadePresets.DormAYellow with { Key = "t.aFull", AccentApron = null };
        BuildingPlan Plan(WorldFeature f, Rules.FacadeLook look) => BuildingPlanner.Plan(BuildingPlanner.Sources(f).Single(), World(f), 4u, [look])!;
        foreach (var footprint in new PolygonRings[] { new(Rect(0, 0, 60, 18)), new(LShape(0, 0)), Courtyard(0, 0), new(Rect(0, 0, 40, 14)) })
        {
            var feature = Feature("t:dorm", footprint, 6, 19.2f, "residence");
            foreach (var look in new[] { Rules.FacadePresets.DormAYellow, Rules.FacadePresets.DormACreamCoffee })
            {
                var (apron, wide) = (Plan(feature with { Facade = new FacadePreset(look.Key) }, look), Plan(feature with { Facade = new FacadePreset(full.Key) }, full with { Frame = look.Frame }));
                var (share, spread) = (TerracottaShare(apron), TerracottaShare(wide));
                var what = $"{look.Key} on {footprint.Outer.Count} corners and {footprint.Holes.Count} courtyards";
                Require(spread.Wall is > 0.23 and < 0.29, $"{what}: about a quarter of the wall with the lintels ({spread.Wall:0.000})");
                Require(share.Wall is > 0.15 and < 0.19, $"{what}: about a sixth of the wall as aprons ({share.Wall:0.000})");
                var (area, expected, strays) = ApronCheck(apron);
                Require(strays == 0, $"{what}: {strays} triangles of 陶红 outside a floor-to-sill band");
                Require(MathF.Abs((float)(area / expected) - 1f) < 0.01f, $"{what}: the 陶红 is the aprons ({area:0.0} of {expected:0.0} m²)");
            }
        }
    }


    [TestCase]
    public void DormPresetsKeepEveryWindowNumber()
    {
        // The window cells and the storeys are the family's, so no window of a building changes its number (contract-29): on
        // fixtures of every plan shape, and on the real campus with its dormitories spread over the five presets.
        var features = new[]
        {
            Feature("t:slab", Rect(0, 0, 60, 18), 6, 19.2f, "residence"), Feature("t:ell", LShape(100, 0), 6, 19.2f, "residence"),
            Feature("t:court", Courtyard(0, 100), 5, 16f, "residence"), Feature("t:lab", Rect(300, 0, 40, 16), 4, 15.6f, "research"),
        };
        var plain = BuildingSet.For(World(features), 4u);
        foreach (var (key, _, _, _, _, _, _, _, _, _) in DormTable)
        {
            var styled = BuildingSet.For(World([.. features.Select(f => f.Id.Value != "t:lab" ? f with { Facade = new FacadePreset(key) } : f)]), 4u);
            Require(styled.Blocks.SequenceEqual(plain.Blocks), $"{key}: every window block as before");
            Require(Layout(styled, where: p => p.Key == "t:lab") == Layout(plain, where: p => p.Key == "t:lab"), $"{key}: the other buildings cell for cell");
            for (var i = 0; i < plain.Plans.Count; i++)
            {
                var (before, after) = (plain.Plans[i], styled.Plans[i]);
                Require(after.TotalColumns == before.TotalColumns && after.Storeys == before.Storeys, $"{key}: {before.Key} keeps its window grid");
                Require(Cells(after) == Cells(before), $"{key}: {before.Key} keeps its window cells and kinds");
                Require(after.Profile.Preset == (before.Key == "t:lab" ? null : key), $"{key}: {before.Key} takes the preset");
            }
        }

        var real = RealCampus();
        var was = BuildingSet.For(real, 1u);
        var dorms = was.Plans.Where(p => p.Style == Rules.BuildingStyle.Dormitory).Select(p => p.Id).Distinct().ToArray();
        Require(dorms.Length >= 20, $"the campus has dormitories ({dorms.Length})");
        var keyOf = dorms.Select((id, i) => (id, key: DormTable[i % DormTable.Length].Key)).ToDictionary(x => x.id, x => x.key);
        var now = BuildingSet.For(new StyledWorld(real, f => keyOf.TryGetValue(f.Id, out var k) ? f with { Facade = new FacadePreset(k) } : f), 1u);
        Require(now.Blocks.SequenceEqual(was.Blocks), "real campus: every window block as before");
        Require(Layout(now, where: p => !keyOf.ContainsKey(p.Id)) == Layout(was, where: p => !keyOf.ContainsKey(p.Id)), "real campus: the other buildings cell for cell");
        for (var i = 0; i < was.Plans.Count; i++)
        {
            var (before, after) = (was.Plans[i], now.Plans[i]);
            if (!keyOf.TryGetValue(before.Id, out var k)) continue;
            Require(after.Profile.Preset == k && after.TotalColumns == before.TotalColumns && after.Storeys == before.Storeys && Cells(after) == Cells(before),
                $"real campus: {before.Key} keeps its window grid under {k}");
        }
    }

    [TestCase]
    public void NorthAnnexPresetsMatchTheTable()
    {
        var tokens = PaletteTokens();
        foreach (var (key, _, _, glazing, windowHeight, sill, groundSill, frame, trim, window) in AnnexTable)
        {
            var look = Rules.FacadePresets.Find(new FacadePreset(key)) ?? throw new InvalidOperationException($"Expected: {key} is a known preset");
            Require(look.Glazing == glazing && look.WindowHeight == windowHeight && look.Sill == sill && look.GroundSill == groundSill, $"{key}: the window's size");
            Require(look.Glass == "glass" && look.Frame == frame && look.Trim == trim && look.Accent == frame && look.Window == window && look.AccentApron is null, $"{key}: colours and window part");
            foreach (var family in new[] { frame, trim })
                Require(tokens.Contains(family + ".lit") && tokens.Contains(family + ".shade"), $"{key}: {family} is in the palette");
        }
    }

    [TestCase]
    public void NorthPresetsReachTheRealBuildings()
    {
        // The zone pack keys these ways in overlay/north.d/: each one takes its preset, keeps its family, opens its windows at the
        // preset's height (the ground storey within 0.1 m) and shows the preset's wall; none of the five new keys is unknown any more.
        var real = RealCampus();
        var set = BuildingSet.For(real, 1u);
        (string Way, string Key)[] keyed =
        [
            ("qsh:way:949704499", "annex.service.creamCoffee"), ("qsh:way:217855247", "annex.utility.creamCoffee"), ("qsh:way:683646735", "annex.utility.greyWhite"),
            ("qsh:way:217855243", "dorm.master.creamCoffee"), ("qsh:way:544136505", "dorm.master.creamCoffeeWhite"),
        ];
        foreach (var (way, key) in keyed)
        {
            var plans = set.Plans.Where(p => p.Id.Value == way).ToArray();
            Require(plans.Length > 0, $"{way} is a building of the real campus");
            var look = Rules.FacadePresets.Find(new FacadePreset(key))!;
            foreach (var plan in plans)
            {
                Require(plan.Profile.Preset == key && plan.Profile.Wall == look.Frame && plan.Profile.Trim == look.Trim, $"{way}: takes {key}");
                Require(!BuildingsModule.UnknownPreset(plan).Any(), $"{way}: {key} is known");
                var windows = plan.Openings.SelectMany(o => o).Where(o => o.Kind == BayKind.Window).ToArray();
                // Upper storeys open at exactly the preset's height; the ground storey's sill is a little higher, which may take a few cm off the 6-storey blocks.
                Require(windows.Length > 0 && windows.All(w => MathF.Abs(w.Y1 - w.Y0 - look.WindowHeight!.Value) < (w.Row == 0 ? 0.1f : 0.01f)),
                    $"{way}: windows {look.WindowHeight} m high, got {string.Join(", ", windows.Select(w => (w.Y1 - w.Y0).ToString("0.000")).Distinct())}");
            }
        }
        var table = AnnexTable.ToDictionary(r => r.Key);
        foreach (var (way, key) in keyed.Where(k => table.ContainsKey(k.Key)))
            Require(set.Plans.Where(p => p.Id.Value == way).All(p => p.Style == table[key].Style), $"{way}: stays in the {table[key].Family} family");
    }

    [TestCase]
    public void NorthUnkeyedBlocksTakeTheBohanLanguage()
    {
        // 5232, 5561 and 9693 are not keyed in the overlay yet (WP-20): handed the keys as an overlay op would, each takes the
        // preset over whatever family the data gives it, and not one window changes its number (contract-29).
        var real = RealCampus();
        var was = BuildingSet.For(real, 1u);
        var keyOf = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["qsh:way:570145232"] = "dorm.master.creamCoffee",
            ["qsh:way:550365561"] = "dorm.master.creamCoffee",
            ["qsh:way:544269693"] = "dorm.master.creamCoffeeWhite",
        };
        var now = BuildingSet.For(new StyledWorld(real, f => keyOf.TryGetValue(f.Id.Value, out var k) ? f with { Facade = new FacadePreset(k) } : f), 1u);
        Require(now.Blocks.SequenceEqual(was.Blocks), "every window block as before");
        Require(Layout(now, where: p => !keyOf.ContainsKey(p.Id.Value)) == Layout(was, where: p => !keyOf.ContainsKey(p.Id.Value)), "the other buildings cell for cell");
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < was.Plans.Count; i++)
        {
            var (before, after) = (was.Plans[i], now.Plans[i]);
            if (!keyOf.TryGetValue(before.Id.Value, out var k)) continue;
            seen.Add(before.Id.Value);
            Require(after.Profile.Preset == k && after.Profile.Wall == Rules.Families.WallCreamCoffee, $"{before.Key} takes {k}");
            Require(after.TotalColumns == before.TotalColumns && after.Storeys == before.Storeys && Cells(after) == Cells(before), $"{before.Key} keeps its window grid");
        }
        Require(seen.Count == keyOf.Count, $"all three blocks planned ({seen.Count})");
    }

    private static IEnumerable<int> ColorVertices(GeometryBundle mesh, params string[] tokens)
    {
        var catalog = Campus.Contracts.Tests.PaletteFixture.Catalog;
        var indices = tokens.Select(token => catalog.Resolve(new PaletteToken(token)).Value).ToArray();
        return Enumerable.Range(0, mesh.Position.Length / 3)
            .Where(vertex => indices.Contains(mesh.ColorIndices?[vertex] ?? catalog.Resolve(mesh.Material.Spec.Lit).Value));
    }
    private static bool HasColor(GeometryBundle mesh, string token) => ColorVertices(mesh, token).Any();
    private static bool TriangleColor(GeometryBundle mesh, int offset, string token)
    {
        var index = Campus.Contracts.Tests.PaletteFixture.Catalog.Resolve(new PaletteToken(token)).Value;
        return Enumerable.Range(0, 3).All(corner => mesh.ColorIndices![mesh.Index[offset + corner]] == index);
    }

    private static string Cells(BuildingPlan p) => string.Join(" ", p.Openings.SelectMany((o, fi) => o.Select(x => $"{fi}/{x.Bay}/{x.Row}/{x.Kind}")));

    /// <summary>
    /// What its L0 mesh paints in 陶红, as a share of the building's facade: Gross over the plinth top to the roof line of every
    /// skin edge, windows and doors included; Wall over that without its openings, the wall as the eye reads it.
    /// </summary>
    private static (double Gross, double Wall) TerracottaShare(BuildingPlan plan)
    {
        var block = new WindowBlock(plan.Id, plan.Key, 0, plan.TotalColumns, plan.Storeys);
        var area = 0.0;
        foreach (var b in new BuildingsModule().BuildOne(plan, block, LodLevel.L0, Campus.Contracts.Tests.PaletteFixture.Catalog).Where(m => HasColor(m, "band.terracotta.lit")))
            for (var t = 0; t < b.Index.Length; t += 3)
            {
                if (!TriangleColor(b, t, "band.terracotta.lit")) continue;
                var a = P(b, b.Index[t]);
                area += 0.5 * Vector3.Cross(P(b, b.Index[t + 1]) - a, P(b, b.Index[t + 2]) - a).Length();
            }
        var facade = 0.0;
        for (var r = 0; r < plan.RingCount; r++)
            for (var e = 0; e < plan.SkinAt(r).Count; e++) facade += plan.SkinAt(r).Length[e] * (plan.RoofY - plan.PlinthTopY);
        var opened = plan.Openings.Sum(o => o.Sum(x => (double)(x.U1 - x.U0) * (x.Y1 - x.Y0)));
        return (area / facade, area / (facade - opened));
    }

    /// <summary>
    /// The 陶红 of a building's L0 mesh against what the plan says it should be as aprons: its area, the area of the aprons (every
    /// window's width × floor to sill, the ground floor from the plinth top), and the triangles that leave a floor-to-sill band.
    /// </summary>
    private static (double Area, double Expected, int Strays) ApronCheck(BuildingPlan plan)
    {
        var block = new WindowBlock(plan.Id, plan.Key, 0, plan.TotalColumns, plan.Storeys);
        var (area, strays) = (0.0, 0);
        foreach (var b in new BuildingsModule().BuildOne(plan, block, LodLevel.L0, Campus.Contracts.Tests.PaletteFixture.Catalog).Where(m => HasColor(m, "band.terracotta.lit")))
            for (var t = 0; t < b.Index.Length; t += 3)
            {
                if (!TriangleColor(b, t, "band.terracotta.lit")) continue;
                var (a, c, d) = (P(b, b.Index[t]), P(b, b.Index[t + 1]), P(b, b.Index[t + 2]));
                area += 0.5 * Vector3.Cross(c - a, d - a).Length();
                var (low, high) = (MathF.Min(a.Y, MathF.Min(c.Y, d.Y)), MathF.Max(a.Y, MathF.Max(c.Y, d.Y)));
                if (!Enumerable.Range(0, plan.Storeys).Any(k => low >= MathF.Max(plan.FloorY(k), plan.PlinthTopY) - 0.01f && high <= plan.SillY[k] + 0.01f)) strays++;
            }
        var expected = plan.Openings.Sum(o => o.Where(x => x.Kind == BayKind.Window)
            .Sum(x => (double)(x.U1 - x.U0) * (plan.SillY[x.Row] - MathF.Max(plan.FloorY(x.Row), plan.PlinthTopY))));
        return (area, expected, strays);
    }

    /// <summary>Every colour token of the palette registry (core and the packs' extensions).</summary>
    private static HashSet<string> PaletteTokens()
    {
        var root = Environment.GetEnvironmentVariable("UESTC_REPOSITORY") is { Length: > 0 } env ? env : AppContext.BaseDirectory;
        var directory = new DirectoryInfo(root);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "game", "style", "palette", "core.json"))) directory = directory.Parent;
        if (directory is null) throw new InvalidOperationException("Palette registry not found.");
        var palette = Path.Combine(directory.FullName, "game", "style", "palette");
        var tokens = new HashSet<string>(StringComparer.Ordinal);
        foreach (var file in new[] { Path.Combine(palette, "core.json") }.Concat(Directory.GetFiles(Path.Combine(palette, "ext"), "*.json")))
        {
            using var document = JsonDocument.Parse(File.ReadAllBytes(file));
            if (!document.RootElement.TryGetProperty("entries", out var entries)) continue;
            foreach (var entry in entries.EnumerateArray())
                if (entry.TryGetProperty("token", out var token)) tokens.Add(token.GetString()!);
        }
        return tokens;
    }

    [TestCase]
    public void DoorsOpenAtTheEntranceAnchors()
    {
        var index = new Entrances(
            Anchor("t:dorm", "dorm.south", new(21.3f, 0f, 0f), 0f),                          // off the bay centre, at the floor
            Anchor("t:lab", "lab.porch", new(112f, 1.2f, 0f), 0f, outsideY: 1.2f),             // raised onto a porch
            Anchor("t:low", "low.mid", new(220f, 2.0f, 0f), 0f),                                // no door height under the next floor
            Anchor("t:wing", "wing.east", new(320f, 0f, 8f), 90f));                             // opens into the taller block next door
        var features = new[]
        {
            Feature("t:dorm", Rect(0, 0, 60, 18), 6, 19.8f, "residence"), Feature("t:lab", Rect(100, 0, 40, 16), 4, 15.6f, "research"),
            Feature("t:low", Rect(200, 0, 40, 16), 4, 13.2f, "teaching"), Feature("t:wing", Rect(300, 0, 20, 16), 3, 10.5f, "teaching"),
            Feature("t:block", Rect(320, 0, 20, 16), 5, 18f, "teaching"),
        };
        var world = World(index, features);
        var set = BuildingSet.For(world, 2u);
        var generic = BuildingSet.For(World(features), 2u);
        var module = new BuildingsModule();
        foreach (var (plan, i) in set.Plans.Select((p, i) => (p, i)))
        {
            var decls = FacadeOpenings.Declarations(plan, set.Blocks[i]);
            Require(decls.Select(d => d.Id).Distinct().Count() == decls.Count && decls.All(d => d.Id >= set.Blocks[i].Base && d.Id < set.Blocks[i].Base + set.Blocks[i].Count),
                $"{plan.Key}: every opening keeps a unique number inside its block");
            foreach (var lod in Levels) foreach (var b in module.BuildOne(plan, set.Blocks[i], lod, Campus.Contracts.Tests.PaletteFixture.Catalog)) b.ValidateVertexData();
        }
        var dorm = set.Plans.Single(p => p.Key == "t:dorm");
        var entrance = dorm.Entrances.Single();
        Require(entrance.Anchor == "dorm.south" && entrance.Row == 0 && entrance.Landing, "the dorm's door moved to its anchor, with a landing");
        var door = dorm.DoorOf(entrance);
        Require(MathF.Abs(door.Y0 - dorm.FloorY(0)) < 1e-4f, "an anchor at the pad takes the building's floor (WLD-TERRAIN)");
        Require(Vector2.Distance(GlassCentre(dorm, entrance), new Vector2(21.3f, 0f)) <= Rules.EntranceRules.AnchorTolerance, "door within G18's 0.5 m of its anchor");
        Require(!dorm.Openings[entrance.Facade].Any(o => o.Row == 0 && o.Kind != BayKind.Door && o.U0 < door.U1 + Rules.EntranceRules.Pier && o.U1 > door.U0 - Rules.EntranceRules.Pier),
            "no window crowds the door");
        var old = generic.Plans.Single(p => p.Key == "t:dorm");
        Require(old.Entrances.Single().Facade != entrance.Facade || old.Entrances.Single().Bay != entrance.Bay, "the generic door is gone");
        var lab = set.Plans.Single(p => p.Key == "t:lab").Entrances.Single();
        var labDoor = set.Plans.Single(p => p.Key == "t:lab").DoorOf(lab);
        Require(lab.Anchor == "lab.porch" && !lab.Landing && MathF.Abs(labDoor.Y0 - 1.2f) < 1e-4f && labDoor.Y1 - labDoor.Y0 >= 2f,
            "a raised anchor opens a door at its floor, onto the porch, without a landing");
        Require(EntranceKit.Pieces(set.Plans.Single(p => p.Key == "t:lab"), lab).Count == 0, "no WP-14 landing under a porch");
        foreach (var key in new[] { "t:low", "t:wing" })
        {
            var plan = set.Plans.Single(p => p.Key == key);
            Require(plan.Entrances.Single().Anchor is null && Layout(set, key) == Layout(generic, key), $"{key}: the anchor is left alone and the generic door stays");
        }
        // The authored opening is clear, with the wall above and below the door retained.
        foreach (var plan in new[] { dorm, set.Plans.Single(p => p.Key == "t:lab") })
        {
            var e = plan.Entrances.Single();
            var opening = plan.DoorOf(e);
            var centre = GlassCentre(plan, e);
            var walls = module.Collide(Ctx(world, 2u), plan.Tile(LodLevel.L0)).OfType<WallCollider>().ToArray();
            Require(!walls.Any(w => Blocks(w, centre, (opening.Y0 + opening.Y1) * 0.5f)), $"{plan.Key}: open anchor must not be blocked");
            Require(walls.Any(w => Blocks(w, centre, opening.Y0 - 0.1f)) && walls.Any(w => Blocks(w, centre, opening.Y1 + 0.1f)),
                $"{plan.Key}: retain the wall below the threshold and above the lintel");
        }
        var closedPlan = generic.Plans.Single(p => p.Key == "t:dorm");
        var closedEntrance = closedPlan.Entrances.Single();
        var closedDoor = closedPlan.DoorOf(closedEntrance);
        Require(module.Collide(Ctx(World(features), 2u), closedPlan.Tile(LodLevel.L0)).OfType<WallCollider>()
            .Any(w => w.Tag == SurfaceTag.DoorClosed && Blocks(w, GlassCentre(closedPlan, closedEntrance), (closedDoor.Y0 + closedDoor.Y1) * 0.5f)),
            "the undeclared generic entrance stays closed");
    }

    [TestCase]
    public void ClaimedFacadesOpenNoDoors()
    {
        // WP-14 opens doors only on the facades it generates: a zone pack's Facade or Massing claim takes the door, its
        // landing, canopy and doorway colliders with it (the zone pack builds that door).
        var features = new[] { Feature("t:dorm", Rect(0, 0, 60, 18), 6, 19.8f, "residence"), Feature("t:lab", Rect(200, 0, 40, 16), 4, 15.6f, "research") };
        var world = World(new Entrances(Anchor("t:dorm", "dorm.south", new(21.3f, 0f, 0f), 0f)), features);
        var module = new BuildingsModule();
        var dorm = BuildingSet.For(world, 2u).Plans.Single(p => p.Key == "t:dorm");
        var tile = dorm.Tile(LodLevel.L0);
        Require(dorm.Entrances.Single().Anchor == "dorm.south" && module.Collide(Ctx(world, 2u), tile).OfType<WallCollider>().Any() &&
            !module.Collide(Ctx(world, 2u), tile).OfType<WallCollider>().Any(w => w.Tag == SurfaceTag.DoorClosed),
            "unclaimed, WP-14 opens the anchor door");
        foreach (var aspect in new[] { Aspects.Facade, Aspects.Massing })
        {
            var ctx = Prepared(new BuildContext { Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog, World = world, Map = TestMap, WorldSeed = 2u, ClaimedBy = (f, a) => a == aspect && f.Value == "t:dorm" ? new WpId("WP-18") : null });
            Require(!module.Tiles(ctx).Contains(tile) && module.Build(ctx, tile, LodLevel.L0) == ContentOutput.Empty && module.Collide(ctx, tile).Count == 0,
                $"{aspect} claimed: no door, landing, canopy or collider of WP-14");
            Require(module.Build(ctx, BuildingSet.For(world, 2u).Plans.Single(p => p.Key == "t:lab").Tile(LodLevel.L0), LodLevel.L0).Meshes.Count > 0, "the neighbour stays");
        }

        // The real campus under every zone pack's claims (the kernel's ledger rebuilt from the content modules): WP-14's
        // authored openings at open anchors remain clear. The library (16b), Xuanbang's clock tower
        // (18b) and, since 18c2, the gym deck are claimed: their doors are the zone packs'.
        var real = RealCampus();
        var rctx = new BuildContext { Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog, World = real, Map = TestMap, WorldSeed = 1u, ClaimedBy = RealClaims(real) };
        var anchored = BuildingSet.For(real, 1u).Plans.SelectMany(p => p.Entrances.Where(e => e.Anchor is not null).Select(e => (Plan: p, Door: e))).ToArray();
        foreach (var (plan, e) in anchored)
        {
            var mine = BuildingsModule.Unclaimed(rctx, plan);
            var centre = GlassCentre(plan, e);
            var opening = plan.DoorOf(e);
            var walls = module.Collide(rctx, plan.Tile(LodLevel.L0)).OfType<WallCollider>().ToArray();
            Require(!walls.Any(w => Blocks(w, centre, (opening.Y0 + opening.Y1) * 0.5f)), $"{e.Anchor} on {plan.Id.Value}: open doorway is clear");
            if (mine) Require(walls.Any(w => Blocks(w, centre, opening.Y1 + 0.1f)), $"{e.Anchor}: the lintel remains");
        }
        foreach (var id in new[] { "lib.main", "xb.clock", "gym.deck" })
            Require(anchored.Any(x => x.Door.Anchor == id && !BuildingsModule.Unclaimed(rctx, x.Plan)), $"{id}: claimed by its zone pack, WP-14 gives way");
    }

    [TestCase]
    public void AnchorsSetTheDoorHeadAndLogDoorsAlone()
    {
        // contract-34: the anchor's HeadY is the door top (under the next slab); without one the family's door rise stays.
        var dorm = Feature("t:dorm", Rect(0, 0, 60, 18), 6, 19.8f, "residence");
        PlanEntrance? Door(float head, out BuildingPlan plan)
        {
            var (feature, decl) = Anchor("t:dorm", "dorm.south", new(21.3f, 0f, 0f), 0f);
            plan = BuildingSet.For(World(new Entrances((feature, decl with { HeadY = head })), dorm), 2u).Plans.Single();
            return plan.Entrances.SingleOrDefault(e => e.Anchor == "dorm.south");
        }
        var known = Door(2.6f, out var p1)!;
        Require(MathF.Abs(p1.DoorOf(known).Y1 - 2.6f) < 1e-4f, "HeadY 2.6 is the door top");
        var unknown = Door(0f, out var p0)!;
        Require(MathF.Abs(p0.DoorOf(unknown).Y1 - (p0.FloorY(0) + Math.Clamp(p0.StoreyHeight - 1f, 2.1f, 2.4f))) < 1e-4f, "0 = unknown: the family's door rise");
        var tall = Door(9f, out var p2)!;
        Require(MathF.Abs(p2.DoorOf(tall).Y1 - (p2.FloorY(1) - Rules.EntranceRules.SlabDepth)) < 1e-4f, "never through the next slab");

        // Doors alone (outside landing on a porch or deck) are logged with the package expected to build what they open onto.
        var lab = Feature("t:lab", Rect(100, 0, 40, 16), 4, 15.6f, "research") with { Zone = new ZoneId("north") };
        var porch = new Entrances(Anchor("t:lab", "lab.porch", new(112f, 1.2f, 0f), 0f, outsideY: 1.2f));
        var deck = Feature("t:deck", Rect(100, -6, 40, 6), 1, 1.2f, "service");
        string Log(TestWorld w, Func<FeatureId, Aspects, WpId?> claims)
        {
            var ctx = new BuildContext { Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog, World = w, Map = TestMap, WorldSeed = 2u, ClaimedBy = claims };
            var plan = BuildingSet.For(w, 2u).Plans.Single(p => p.Key == "t:lab");
            return BuildingsModule.DoorsAlone(ctx, plan).Single(d => d.Level == DiagnosticLevel.Info && d.Code == "wp14.door.alone" && d.Feature == plan.Id).Message;
        }
        var byZone = Log(World(porch, lab), (_, _) => null);
        Require(byZone.Contains("lab.porch", StringComparison.Ordinal) && byZone.Contains("1.20 m", StringComparison.Ordinal) && byZone.Contains("north zone pack", StringComparison.Ordinal), byZone);
        var byDeck = Log(World(porch, lab, deck), (f, a) => f.Value == "t:deck" && a == Aspects.Massing ? new WpId("WP-21") : null);
        Require(byDeck.Contains("WP-21, which builds the deck block t:deck", StringComparison.Ordinal), byDeck);
        var nobody = Log(World(porch, lab with { Zone = null }), (_, _) => null);
        Require(nobody.Contains("undecided", StringComparison.Ordinal), nobody);
        var ctx0 = Prepared(Ctx(World(porch, lab), 2u));
        var plan0 = BuildingSet.For(ctx0.World, 2u).Plans.Single();
        Require(new BuildingsModule().Build(ctx0, plan0.Tile(LodLevel.L0), LodLevel.L0).Diagnostics.Count == 1 &&
            new BuildingsModule().Build(ctx0, plan0.Tile(LodLevel.L1), LodLevel.L1).Diagnostics.Count == 0, "logged once, with the L0 cell");
    }

    [TestCase]
    public void EntranceLandingsFollowTheOutsideGround()
    {
        // The ground south of the dorm (z < -1) lies 0.9 m below its pad; the lab stands on level ground (WLD-TERRAIN).
        var world = World(new Entrances(Anchor("t:dorm", "dorm.south", new(21.3f, 0f, 0f), 0f, outsideY: -0.9f)),
            new Falling(-1f, 0.9f), Feature("t:dorm", Rect(0, 0, 60, 18), 6, 19.8f, "residence"), Feature("t:lab", Rect(100, 20, 40, 16), 4, 15.6f, "research"));
        var set = BuildingSet.For(world, 2u);
        var dorm = set.Plans.Single(p => p.Key == "t:dorm");
        var steps = EntranceKit.Pieces(dorm, dorm.Entrances.Single());
        Require(steps.Select(p => p.Part).SequenceEqual([PartCatalog.Platform, PartCatalog.Railing, PartCatalog.Railing, PartCatalog.Steps]),
            "landing, guard railings on its sides (over 1.0 m) and one flight");
        var flight = (StairParams)steps[3].Params;
        Require(MathF.Abs(flight.Height - 1.05f) < 1e-3f && flight.Steps == 7 && flight.Variation == 0, $"1.05 m in 7 risers with cheek walls ({flight.Steps})");
        var sink = new Mesh.MeshSink(Campus.Contracts.Tests.PaletteFixture.Catalog);
        EntranceKit.Build(sink, steps, LodLevel.L0);
        Require(sink.Finish().Any(b => b.Material.Spec.Normal == ToonNormal.Smooth && b.Material.Spec.Lit.Value == "frame.alu.lit"), "handrails from 0.6 m (WP-11)");
        foreach (var lod in Levels) foreach (var b in new BuildingsModule().BuildOne(dorm, set.Blocks[0], lod, Campus.Contracts.Tests.PaletteFixture.Catalog)) b.ValidateVertexData();
        var colliders = new BuildingsModule().Collide(Ctx(world, 2u), dorm.Tile(LodLevel.L0));
        Require(colliders.OfType<RampCollider>().Any(r => r.Stair is { Count: 7 }), "the flight walks as one stair ramp");
        Require(colliders.OfType<BoxCollider>().Any(b => b.Tag == SurfaceTag.Ground), "the landing is walkable");
        Require(colliders.OfType<WallCollider>().Count(w => w.Tag == SurfaceTag.Barrier && w.Y1 - w.Y0 >= 1.5f) == 2, "its railings cannot be climbed over");
        var lab = set.Plans.Single(p => p.Key == "t:lab");
        var slope = EntranceKit.Pieces(lab, lab.Entrances.Single());
        Require(slope.Select(p => p.Part).SequenceEqual([PartCatalog.Platform, PartCatalog.Ramp]), "0.15 m: landing and a kerbed slope, no step");
        var ramp = (RampParams)slope[1].Params;
        Require(ramp.Height / ramp.Depth <= 1f / 12f + 1e-4f && ramp.Variation == 1, "the slope is 1:12 or gentler");
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
        var roofOnly = new BuildContext { Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog, World = world, Map = TestMap, WorldSeed = 1u, ClaimedBy = (_, aspect) => aspect == Aspects.Roof ? new WpId("WP-18") : null };
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

    /// <summary>
    /// Shared-boundary restoration (WP-08) gives a building part a detailed physical outline with extra collinear vertices and keeps
    /// the legacy outline as its facade outline. The generic building's footprint, bay grid and window block must follow the facade
    /// outline, vertex for vertex, so a part with a facade outline lays out exactly the windows the old data gave it.
    /// </summary>
    [TestCase]
    public void GenericBuildingsLayWindowsOnTheFacadeOutlineOfAPart()
    {
        var world = RealCampus();
        var checkedParts = 0;
        var differing = 0;
        foreach (var feature in world.Features.Values.OrderBy(f => f.Id.Value, StringComparer.Ordinal))
            foreach (var part in feature.Parts.Where(p => p.FacadeGeometry is not null).OrderBy(p => p.SourceId, StringComparer.Ordinal))
            {
                var sources = BuildingPlanner.Sources(feature).Where(s => s.Key == part.SourceId || s.Key.StartsWith(part.SourceId + "#", StringComparison.Ordinal)).ToArray();
                if (sources.Length == 0) continue; // the part is not of the building kind
                var facade = part.FacadeGeometry switch
                {
                    PolygonGeometry p => [p.Rings],
                    MultiPolygonGeometry m => m.Polygons,
                    _ => Array.Empty<PolygonRings>(),
                };
                Require(sources.Length == facade.Count, $"{part.SourceId}: one source per facade polygon");
                for (var i = 0; i < sources.Length; i++)
                {
                    Require(sources[i].Rings.Outer.SequenceEqual(facade[i].Outer), $"{part.SourceId}: the footprint's outer ring is the facade ring");
                    Require(sources[i].Rings.Holes.Count == facade[i].Holes.Count
                        && sources[i].Rings.Holes.Zip(facade[i].Holes).All(h => h.First.SequenceEqual(h.Second)), $"{part.SourceId}: the footprint's holes are the facade holes");
                    var reference = BuildingPlanner.Plan(sources[i] with { Rings = facade[i] }, world, 4u);
                    var actual = BuildingPlanner.Plan(sources[i], world, 4u);
                    Require((reference is null) == (actual is null), $"{part.SourceId}: the same footprint is built or skipped");
                    if (actual is null || reference is null) continue;
                    Require(actual.TotalColumns == reference.TotalColumns && actual.Grid.GridHash == reference.Grid.GridHash,
                        $"{part.SourceId}: the window grid is the one of the facade outline");
                    checkedParts++;
                }
                var detailed = part.Geometry switch
                {
                    PolygonGeometry p => [p.Rings],
                    MultiPolygonGeometry m => m.Polygons,
                    _ => Array.Empty<PolygonRings>(),
                };
                if (detailed.Count != facade.Count || detailed.Zip(facade).Any(t => !t.First.Outer.SequenceEqual(t.Second.Outer))) differing++;
            }
        Require(checkedParts >= 20, $"enough building parts with a facade outline are covered ({checkedParts})");
        Require(differing > 0, "the detailed and the facade outlines do differ, so the guard has teeth");
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

    // ---------------------------------------------------------------- fixtures
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

    /// <summary>Every opening of every plan (or of one key, or of those <paramref name="where"/> takes) with its number and card, in plan order.</summary>
    private static string Layout(BuildingSet set, string? key = null, Func<BuildingPlan, bool>? where = null)
    {
        var ci = System.Globalization.CultureInfo.InvariantCulture;
        var sb = new System.Text.StringBuilder();
        for (var i = 0; i < set.Plans.Count; i++)
        {
            var (plan, block) = (set.Plans[i], set.Blocks[i]);
            if ((key is not null && plan.Key != key) || where?.Invoke(plan) == false) continue;
            sb.Append(ci, $"P {plan.Key} base={block.Base} cols={block.Columns} rows={block.Rows}\n");
            for (var fi = 0; fi < plan.Facades.Count; fi++)
                foreach (var o in plan.Openings[fi])
                {
                    var n = FacadeOpenings.Number(plan, block, plan.Facades[fi], o);
                    sb.Append(ci, $"  {fi} b{o.Bay} r{o.Row} {o.Kind} {o.U0:R} {o.U1:R} {o.Y0:R} {o.Y1:R} #{n} {FacadeOpenings.Card(plan, o, n)}\n");
                }
        }
        return sb.ToString();
    }

    private static string LayoutHash(BuildingSet set) =>
        Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(Layout(set))))[..16];

    /// <summary>Plan position of an entrance door's centre on the glass plane (where the interior index puts it).</summary>
    private static Vector2 GlassCentre(BuildingPlan plan, PlanEntrance e)
    {
        var door = plan.DoorOf(e);
        var skin = plan.SkinAt(plan.Facades[e.Facade].Ring);
        var edge = plan.Facades[e.Facade].Edge;
        return skin.Points[edge] + skin.Dir[edge] * ((door.U0 + door.U1) * 0.5f) - skin.Normal[edge] * plan.Reveal;
    }

    private static bool Blocks(WallCollider wall, Vector2 point, float y)
    {
        var edge = wall.B - wall.A;
        var t = Math.Clamp(Vector2.Dot(point - wall.A, edge) / edge.LengthSquared(), 0f, 1f);
        return y > wall.Y0 && y < wall.Y1 && Vector2.Distance(point, wall.A + edge * t) <= wall.HalfT + 0.0001f;
    }

    /// <summary>An open entrance of a feature: door centre (floor height in Y), facing yaw (0 north, 90 east), outside landing 2.5 m out.</summary>
    private static (string Feature, EntranceDecl Door) Anchor(string feature, string id, Vector3 centre, float yawDeg, float? outsideY = null)
    {
        var yaw = yawDeg * (float.Pi / 180f);
        var facing = new Vector3(DetMath.Sin(yaw), 0f, -DetMath.Cos(yaw));
        var outside = centre + facing * 2.5f;
        outside.Y = outsideY ?? centre.Y;
        return (feature, new EntranceDecl(id, outside, centre - facing * 3f, 1.1f, true) { Center = centre, YawDeg = yawDeg });
    }

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
        Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog,
        World = world,
        Map = TestMap,
        WorldSeed = seed,
        WindowNumbering = numbering,
        ClaimedBy = (feature, aspect) => aspect == Aspects.Facade && claims?.Contains(feature.Value) == true ? new WpId("WP-18") : null,
    };

    /// <summary>The kernel's prepass stand-in until WP-02 runs it: the provider's snapshot, published in the context.</summary>
    private static BuildContext Prepared(BuildContext ctx) => new()
    {
        Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog,
        World = ctx.World,
        Map = ctx.Map,
        WorldSeed = ctx.WorldSeed,
        ClaimedBy = ctx.ClaimedBy,
        WindowNumbering = new BuildingsModule().PrepareWindowNumbering(ctx, []),
    };

    /// <summary>The repository's campus as the kernel loads it: base data, interior index and every zone overlay.</summary>
    private static IWorldModel RealCampus()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "game", "data", "maps", "qingshuihe", "campus.json"))) directory = directory.Parent;
        if (directory is null) throw new InvalidOperationException("Repository campus fixture not found.");
        var data = Path.Combine(directory.FullName, "game", "data", "maps", "qingshuihe");
        var files = new[] { "campus.json", "network.json", "interiors.json", "map.json" }
            .Concat(Directory.GetDirectories(Path.Combine(data, "overlay"), "*.d")
                    .Where(dir => Path.GetFileName(dir) is not "interior.d" and not "terrain.d" and not "outskirts.d" and not "walkable.d" and not "fidelity.d")
                    .SelectMany(dir => Directory.GetFiles(dir, "*.json")).Select(p => Path.GetRelativePath(data, p).Replace('\\', '/')))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToDictionary(name => name, name => File.ReadAllText(Path.Combine(data, name)), StringComparer.Ordinal);
        return Campus.Gen.World.WorldModelLoader.Load(new MapId("qingshuihe"), files).World;
    }

    /// <summary>
    /// The claims ledger of the real campus (the kernel's MapClaimLedger, rebuilt): every zone pack's claims under its owner.
    /// The other content modules (furniture, ground, vegetation, water) claim nothing.
    /// </summary>
    private static Func<FeatureId, Aspects, WpId?> RealClaims(IWorldModel world)
    {
        IContentModule[] zones = [new Campus.Gen.Zones.Core.CoreModule(), new Campus.Gen.Zones.South.SouthModule(),
            new Campus.Gen.Zones.CenterEast.CenterEastModule(), new Campus.Gen.Zones.North.NorthModule()];
        var owners = new Dictionary<(FeatureId, Aspects), WpId>();
        foreach (var zone in zones)
        {
            var owner = new WpId(zone.GetType().GetCustomAttributes(typeof(ContentModuleAttribute), false).OfType<ContentModuleAttribute>().Single().Owner);
            foreach (var claim in zone.Claims)
            {
                FeatureId[] claimed = claim switch
                {
                    FeatureClaim f => world.Features.ContainsKey(f.Feature) ? [f.Feature] : Array.Empty<FeatureId>(),
                    AreaClaim a => [.. world.Features.Values.Where(f => f.Zone == a.Zone).Select(f => f.Id)],
                    _ => throw new InvalidOperationException("Unknown claim kind."),
                };
                foreach (var feature in claimed)
                    foreach (var aspect in Enum.GetValues<Aspects>().Where(a => (claim.Aspects & a) != 0)) owners.Add((feature, aspect), owner);
            }
        }
        return (feature, aspect) => owners.TryGetValue((feature, aspect), out var owner) ? owner : null;
    }

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

    private static TestWorld World(Entrances entrances, params WorldFeature[] features) =>
        new(features.ToDictionary(f => f.Id), null) { Interiors = entrances };

    private static TestWorld World(Entrances entrances, ITerrain terrain, params WorldFeature[] features) =>
        new(features.ToDictionary(f => f.Id), null) { Interiors = entrances, Terrain = terrain };

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
        public ISpatialIndex Spatial { get; } = new Flat(features);
        public IInteriorIndex Interiors { get; init; } = new NoInteriors();
        public ITerrain Terrain { get; init; } = FlatTerrain.Default;
        public IOutskirtsModel? Outskirts => null;
        public IFidelityModel? Fidelity { get; } = fidelity;
    }

    /// <summary>A loaded world with its features rewritten (as an overlay op would), everything else as loaded.</summary>
    private sealed class StyledWorld(IWorldModel inner, Func<WorldFeature, WorldFeature> rewrite) : IWorldModel
    {
        public ITerrain Terrain => inner.Terrain;
        public PolygonRings Boundary => inner.Boundary;
        public IReadOnlyDictionary<FeatureId, WorldFeature> Features { get; } = inner.Features.ToDictionary(p => p.Key, p => rewrite(p.Value));
        public WorldNetwork Network => inner.Network;
        public ISpatialIndex Spatial => inner.Spatial;
        public IInteriorIndex Interiors => inner.Interiors;
        public IOutskirtsModel? Outskirts => inner.Outskirts;
        public IFidelityModel? Fidelity => inner.Fidelity;
    }

    /// <summary>The open entrances of the fixture's features (the interior index's EntranceDecl).</summary>
    private sealed class Entrances(params (string Feature, EntranceDecl Door)[] doors) : IInteriorIndex
    {
        public InteriorIndexEntry? Get(FeatureId id) => doors.Any(d => d.Feature == id.Value)
            ? new(id, [], [.. doors.Where(d => d.Feature == id.Value).Select(d => d.Door)], true, "test")
            : null;
        public IReadOnlyList<EntranceDecl> EntrancesNear(Vector3 p, float r) => [.. doors.Select(d => d.Door).Where(d => Vector3.Distance(d.Center, p) <= r)];
    }

    /// <summary>Level at 0 (pads too) north of <paramref name="edge"/> (z), <paramref name="drop"/> lower south of it.</summary>
    private sealed class Falling(float edge, float drop) : ITerrain
    {
        public bool IsFlat(TileKey tile) => false;
        public float HeightAt(float x, float z) => z < edge ? -drop : 0f;
        public void FillTile(TileKey tile, int level, Span<short> cm) => throw new NotSupportedException();
        public float PadY(FeatureId feature) => 0f;
        public float? WaterY(FeatureId water) => null;
        public TerrainFacts FactsAt(float x, float z) => new(0f, TerrainClass.Flat, float.PositiveInfinity, float.PositiveInfinity);
    }

    /// <summary>Flat ground; every fixture feature is a candidate of every query (the planner tests containment itself).</summary>
    private sealed class Flat(IReadOnlyDictionary<FeatureId, WorldFeature> features) : ISpatialIndex
    {
        public IReadOnlyList<FeatureId> Query(Box3 box) => [.. features.Keys.OrderBy(k => k.Value, StringComparer.Ordinal)];
        public float GroundY(float x, float z) => 0f;
        public CameraMedium MediumAt(Vector3 p) => new(MediumKind.Air);
        public WalkClass WalkClassAt(float x, float z) => WalkClass.Walk;
    }

    private sealed class NoInteriors : IInteriorIndex
    {
        public InteriorIndexEntry? Get(FeatureId id) => null;
        public IReadOnlyList<EntranceDecl> EntrancesNear(Vector3 p, float r) => [];
    }
}
