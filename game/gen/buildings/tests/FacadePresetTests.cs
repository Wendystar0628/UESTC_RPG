using Campus.Tests;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Parts;
using Campus.Gen.Buildings.Build;
using Campus.Gen.Buildings.Model;
using Campus.Gen.Buildings.Plan;
using GdUnit4;

namespace Campus.Gen.Buildings.Tests;

using static Campus.Gen.Buildings.Tests.BuildingsTestSupport;

using static GdUnit4.Assertions;

[TestSuite]
public sealed class FacadePresetTests
{
    [TestCase, TestCategory(TestReason.Contract)]
    public void OrdinaryBuildingsKeepTheirOpeningsAndNumbers()
    {
        // Buildings without an entrance anchor lay out the same way every build: openings, window numbers and cards cell for
        // cell. Anchored buildings only move their door; their window blocks never change.
        var features = new[]
        {
            WorldFeatureFixture.Create("t:dorm", "building", new PolygonGeometry(new PolygonRings(Rect(0, 0, 60, 18))), props: "{\"category\":\"residence\"}", height: 19.8f, storeys: 6), WorldFeatureFixture.Create("t:lab", "building", new PolygonGeometry(new PolygonRings(LShape(100, 0))), props: "{\"category\":\"research\"}", height: 15.6f, storeys: 4),
            WorldFeatureFixture.Create("t:court", "building", new PolygonGeometry(Courtyard(0, 100)), props: "{\"category\":\"teaching\"}", height: 12f, storeys: 3), WorldFeatureFixture.Create("t:hall", "building", new PolygonGeometry(new PolygonRings(Rect(200, 0, 40, 30))), props: "{\"category\":\"sport\"}", height: 9f, storeys: 1),
            WorldFeatureFixture.Create("t:small", "building", new PolygonGeometry(new PolygonRings(Rect(300, 0, 5, 6))), props: "{\"category\":\"service\"}", height: 3f, storeys: 1),
        };
        var plain = BuildingSet.For(World(features), 4u);
        AssertThat(Layout(BuildingSet.For(World(features), 4u)) == Layout(plain)).OverrideFailureMessage("Expected: " + ("the same input lays out ordinary buildings cell for cell")).IsTrue();
        var anchored = BuildingSet.For(World(Entrances(Anchor("t:dorm", "a", new(21.3f, 0f, 0f), 0f)), features), 4u);
        AssertThat(anchored.Blocks.SequenceEqual(plain.Blocks)).OverrideFailureMessage("Expected: " + ("an entrance anchor leaves every window block as it was")).IsTrue();
        AssertThat(Layout(anchored).Replace(Layout(anchored, "t:dorm"), "") == Layout(plain).Replace(Layout(plain, "t:dorm"), "")).OverrideFailureMessage("Expected: " + ("the other buildings keep their layout")).IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void FacadePresetsChangeOnlyTheirBuilding()
    {
        // MAP-QSH-SOUTH on one research block among ordinary buildings: larger glass in the same window cells; every
        // window block, and every other building cell for cell, as before.
        var preset = Rules.FacadePresets.ResearchStoneFrame;
        var features = new[]
        {
            WorldFeatureFixture.Create("t:dorm", "building", new PolygonGeometry(new PolygonRings(Rect(0, 0, 60, 18))), props: "{\"category\":\"residence\"}", height: 19.8f, storeys: 6), WorldFeatureFixture.Create("t:lab", "building", new PolygonGeometry(new PolygonRings(LShape(100, 0))), props: "{\"category\":\"research\"}", height: 15.6f, storeys: 4),
            WorldFeatureFixture.Create("t:court", "building", new PolygonGeometry(Courtyard(0, 100)), props: "{\"category\":\"teaching\"}", height: 12f, storeys: 3), WorldFeatureFixture.Create("t:hall", "building", new PolygonGeometry(new PolygonRings(Rect(200, 0, 40, 30))), props: "{\"category\":\"sport\"}", height: 9f, storeys: 1),
        };
        var plain = BuildingSet.For(World(features), 4u);
        var styled = BuildingSet.For(World([.. features.Select(f => f.Id.Value == "t:lab" ? f with { Facade = new FacadePreset(preset.Key) } : f)]), 4u);
        AssertThat(styled.Blocks.SequenceEqual(plain.Blocks)).OverrideFailureMessage("Expected: " + ("every window block as before")).IsTrue();
        AssertThat(Layout(styled, where: p => p.Key != "t:lab") == Layout(plain, where: p => p.Key != "t:lab")).OverrideFailureMessage("Expected: " + ("the other buildings cell for cell")).IsTrue();
        var (before, after) = (plain.Plans.Single(p => p.Key == "t:lab"), styled.Plans.Single(p => p.Key == "t:lab"));
        AssertThat(after.Profile.Preset == preset.Key && before.Profile.Preset is null && after.Style == before.Style).OverrideFailureMessage("Expected: " + ("the preset over the research family")).IsTrue();
        static string Cells(BuildingPlan p) => string.Join(" ", p.Openings.SelectMany((o, fi) => o.Select(x => $"{fi}/{x.Bay}/{x.Row}/{x.Kind}")));
        AssertThat(Cells(after) == Cells(before)).OverrideFailureMessage("Expected: " + ("the same window cells and kinds, so the same window numbers and cards")).IsTrue();
        static float Glazed(BuildingPlan p) => p.Openings.Sum(o => o.Sum(x => (x.U1 - x.U0) * (x.Y1 - x.Y0)));
        AssertThat(Glazed(after) > Glazed(before)).OverrideFailureMessage("Expected: " + ($"more and larger glass: {Glazed(before):0} → {Glazed(after):0} m²")).IsTrue();

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
                AssertThat(lod == LodLevel.L4 || (glass.Length > 0 && glass.All(g => ColorVertices(g, "uestc.techblue.lit").Count() == g.Position.Length / 3))).OverrideFailureMessage("Expected: " + ($"{name} {lod}: glass in the preset's colour")).IsTrue();
                var walls = bundles.Where(b => b.Material.Spec.Kind == ToonKind.Static && b.Material.Spec.Fx == (ToonFx.BevelLift | ToonFx.Mottle)).ToArray();
                AssertThat(walls.Length > 0 && walls.All(b => ColorVertices(b, "wall.creamCoffee.lit").Count() == b.Position.Length / 3)).OverrideFailureMessage("Expected: " + ($"{name} {lod}: the wall frame in the preset's colour")).IsTrue();
                AssertThat(!bundles.Any(Fabric)).OverrideFailureMessage("Expected: " + ($"{name} {lod}: curtain walls hang no curtains")).IsTrue();
            }
        }
        AssertThat(BuildingsModule.UnknownPreset(BuildingSet.For(World(lab), 4u).Plans.Single()).Single().Level == DiagnosticLevel.Warning &&
            BuildingSet.For(World(lab), 4u).Plans.Single().Profile.Preset is null).OverrideFailureMessage("Expected: " + ("a key WP-14 does not know keeps the family, with a warning")).IsTrue();

        // The real campus with the innovation centre's five buildings on the preset (the authored world/south.json): no
        // window number of the map moves, the other buildings stay cell for cell, and every footprint of the five takes it.
        var real = RealMapFixture.Load().World;
        var innovation = real.Features.Values.Where(f => f.Facade?.Value == preset.Key).Select(f => f.Id.Value).ToHashSet(StringComparer.Ordinal);
        var withPreset = Styled(real, f => innovation.Contains(f.Id.Value) ? f with { Family = new BuildingFamily("research"), Facade = new FacadePreset(preset.Key) } : f);
        var (was, now) = (BuildingSet.For(real, 1u), BuildingSet.For(withPreset, 1u));
        AssertThat(now.Blocks.SequenceEqual(was.Blocks)).OverrideFailureMessage("Expected: " + ("real campus: every window block as before")).IsTrue();
        AssertThat(Layout(now, where: p => !innovation.Contains(p.Id.Value)) == Layout(was, where: p => !innovation.Contains(p.Id.Value))).OverrideFailureMessage("Expected: " + ("real campus: the other buildings cell for cell")).IsTrue();
        var centre = now.Plans.Where(p => innovation.Contains(p.Id.Value)).ToArray();
        AssertThat(centre.Length > 0 && centre.All(p => p.Profile.Preset == preset.Key && p.Style == Rules.BuildingStyle.Research)).OverrideFailureMessage("Expected: " + ($"the centre's {centre.Length} footprints take the preset")).IsTrue();
    }

    private static Rules.FacadeLook[] DormLooks => Rules.FacadePresets.All
        .Where(look => look.Key.StartsWith("dorm.", StringComparison.Ordinal)).ToArray();

    [TestCase, TestCategory(TestReason.Contract)]
    public void PresetsResolveTheirPaletteFamiliesAndWindowParts()
    {
        var all = Rules.FacadePresets.All;
        AssertThat(all.Select(p => p.Key).Distinct(StringComparer.Ordinal).Count() == all.Count).IsTrue();
        var tokens = Campus.Contracts.Tests.PaletteFixture.Catalog.Entries.Select(entry => entry.Token.Value).ToHashSet(StringComparer.Ordinal);
        foreach (var look in all)
        {
            AssertThat(Rules.FacadePresets.Find(new FacadePreset(look.Key)) == look).IsTrue();
            foreach (var family in new[] { look.Glass, look.Frame, look.Trim, look.Accent }.OfType<string>())
                AssertThat(tokens.Contains(family + ".lit") && tokens.Contains(family + ".shade")).IsTrue();
            if (look.Window is { } window) AssertThat(PartCatalog.Find(new PartId(window)) is not null).IsTrue();
        }
        AssertThat(Rules.FacadePresets.Find(new FacadePreset("t:unknown")) is null).IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void DormPresetAccentDoesNotFollowTheClusterHash()
    {
        // DormA (pink-sand spandrels) and DormB (brown) by cluster: the same footprint on two world seeds, each way.
        var feature = WorldFeatureFixture.Create("t:dorm", "building", new PolygonGeometry(new PolygonRings(Rect(0, 0, 60, 18))), props: "{\"category\":\"residence\"}", height: 19.2f, storeys: 6);
        uint? seedA = null, seedB = null;
        for (var seed = 1u; seed <= 64u && (seedA is null || seedB is null); seed++)
            switch (BuildingSet.For(World(feature), seed).Plans[0].Profile.Accent)
            {
                case "panel.pinkSand": seedA ??= seed; break;
                case "panel.brown": seedB ??= seed; break;
                default: break;
            }
        AssertThat(seedA is not null && seedB is not null).OverrideFailureMessage("Expected: " + ("seeds for both dormitory variants")).IsTrue();
        foreach (var look in DormLooks)
        {
            var (key, frame, trim, accent) = (look.Key, look.Frame!, look.Trim!, look.Accent!);
            var styled = feature with { Facade = new FacadePreset(key) };
            var (a, b) = (BuildingSet.For(World(styled), seedA!.Value), BuildingSet.For(World(styled), seedB!.Value));
            AssertThat(a.Plans[0].Profile == b.Plans[0].Profile && a.Plans[0].Profile.Accent == accent).OverrideFailureMessage("Expected: " + ($"{key}: one profile under either hash")).IsTrue();
            foreach (var set in new[] { a, b })
            {
                var lit = new HashSet<string>(new BuildingsModule().BuildOne(set.Plans[0], set.Blocks[0], LodLevel.L0, Campus.Contracts.Tests.PaletteFixture.Catalog).SelectMany(m => m.ColorIndices!.Select(i => Campus.Contracts.Tests.PaletteFixture.Catalog.Get(new PaletteIndex(i)).Token.Value)));
                AssertThat(lit.Contains(accent + ".lit") && lit.Contains(frame + ".lit") && lit.Contains(trim + ".lit")).OverrideFailureMessage("Expected: " + ($"{key}: frame, trim and accent reach the mesh")).IsTrue();
                AssertThat(!lit.Contains("panel.brown.lit") && (accent == "panel.pinkSand") == lit.Contains("panel.pinkSand.lit")).OverrideFailureMessage("Expected: " + ($"{key}: no spandrel colour of the family's own")).IsTrue();
            }
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
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
            var feature = WorldFeatureFixture.Create("t:dorm", "building", new PolygonGeometry(footprint), props: "{\"category\":\"residence\"}", height: 19.2f, storeys: 6);
            foreach (var look in new[] { Rules.FacadePresets.DormAYellow, Rules.FacadePresets.DormACreamCoffee })
            {
                var (apron, wide) = (Plan(feature with { Facade = new FacadePreset(look.Key) }, look), Plan(feature with { Facade = new FacadePreset(full.Key) }, full with { Frame = look.Frame }));
                var (share, spread) = (TerracottaShare(apron), TerracottaShare(wide));
                var what = $"{look.Key} on {footprint.Outer.Count} corners and {footprint.Holes.Count} courtyards";
                AssertThat(share.Wall > 0 && share.Wall < spread.Wall).OverrideFailureMessage("Expected: " + ($"{what}: aprons alone ({share.Wall:0.000}) take less of the wall than aprons with lintels ({spread.Wall:0.000})")).IsTrue();
                var (area, expected, strays) = ApronCheck(apron);
                AssertThat(strays == 0).OverrideFailureMessage("Expected: " + ($"{what}: {strays} triangles of 陶红 outside a floor-to-sill band")).IsTrue();
                AssertThat(MathF.Abs((float)(area / expected) - 1f) < TestTolerance.Position).OverrideFailureMessage("Expected: " + ($"{what}: the 陶红 is the aprons ({area:0.0} of {expected:0.0} m²)")).IsTrue();
            }
        }
    }


    [TestCase, TestCategory(TestReason.Contract)]
    public void DormPresetsKeepEveryWindowNumber()
    {
        // The window cells and the storeys are the family's, so no window of a building changes its number (contract-29): on
        // fixtures of every plan shape, and on the real campus with its dormitories spread over the five presets.
        var features = new[]
        {
            WorldFeatureFixture.Create("t:slab", "building", new PolygonGeometry(new PolygonRings(Rect(0, 0, 60, 18))), props: "{\"category\":\"residence\"}", height: 19.2f, storeys: 6), WorldFeatureFixture.Create("t:ell", "building", new PolygonGeometry(new PolygonRings(LShape(100, 0))), props: "{\"category\":\"residence\"}", height: 19.2f, storeys: 6),
            WorldFeatureFixture.Create("t:court", "building", new PolygonGeometry(Courtyard(0, 100)), props: "{\"category\":\"residence\"}", height: 16f, storeys: 5), WorldFeatureFixture.Create("t:lab", "building", new PolygonGeometry(new PolygonRings(Rect(300, 0, 40, 16))), props: "{\"category\":\"research\"}", height: 15.6f, storeys: 4),
        };
        var plain = BuildingSet.For(World(features), 4u);
        foreach (var look in DormLooks)
        {
            var key = look.Key;
            var styled = BuildingSet.For(World([.. features.Select(f => f.Id.Value != "t:lab" ? f with { Facade = new FacadePreset(key) } : f)]), 4u);
            AssertThat(styled.Blocks.SequenceEqual(plain.Blocks)).OverrideFailureMessage("Expected: " + ($"{key}: every window block as before")).IsTrue();
            AssertThat(Layout(styled, where: p => p.Key == "t:lab") == Layout(plain, where: p => p.Key == "t:lab")).OverrideFailureMessage("Expected: " + ($"{key}: the other buildings cell for cell")).IsTrue();
            for (var i = 0; i < plain.Plans.Count; i++)
            {
                var (before, after) = (plain.Plans[i], styled.Plans[i]);
                AssertThat(after.TotalColumns == before.TotalColumns && after.Storeys == before.Storeys).OverrideFailureMessage("Expected: " + ($"{key}: {before.Key} keeps its window grid")).IsTrue();
                AssertThat(Cells(after) == Cells(before)).OverrideFailureMessage("Expected: " + ($"{key}: {before.Key} keeps its window cells and kinds")).IsTrue();
                AssertThat(after.Profile.Preset == (before.Key == "t:lab" ? null : key)).OverrideFailureMessage("Expected: " + ($"{key}: {before.Key} takes the preset")).IsTrue();
            }
        }

        var real = RealMapFixture.Load().World;
        var was = BuildingSet.For(real, 1u);
        var dorms = was.Plans.Where(p => p.Style == Rules.BuildingStyle.Dormitory).Select(p => p.Id).Distinct().ToArray();
        AssertThat(dorms.Length > 0).OverrideFailureMessage("Expected: " + ($"the campus has dormitories ({dorms.Length})")).IsTrue();
        var keyOf = dorms.Select((id, i) => (id, key: DormLooks[i % DormLooks.Length].Key)).ToDictionary(x => x.id, x => x.key);
        var now = BuildingSet.For(Styled(real, f => keyOf.TryGetValue(f.Id, out var k) ? f with { Facade = new FacadePreset(k) } : f), 1u);
        AssertThat(now.Blocks.SequenceEqual(was.Blocks)).OverrideFailureMessage("Expected: " + ("real campus: every window block as before")).IsTrue();
        AssertThat(Layout(now, where: p => !keyOf.ContainsKey(p.Id)) == Layout(was, where: p => !keyOf.ContainsKey(p.Id))).OverrideFailureMessage("Expected: " + ("real campus: the other buildings cell for cell")).IsTrue();
        for (var i = 0; i < was.Plans.Count; i++)
        {
            var (before, after) = (was.Plans[i], now.Plans[i]);
            if (!keyOf.TryGetValue(before.Id, out var k)) continue;
            AssertThat(after.Profile.Preset == k && after.TotalColumns == before.TotalColumns && after.Storeys == before.Storeys && Cells(after) == Cells(before)).OverrideFailureMessage("Expected: " + ($"real campus: {before.Key} keeps its window grid under {k}")).IsTrue();
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void NorthPresetsReachTheRealBuildings()
    {
        // Presets supply nominal window dimensions; the authored storey height can cap a row's clear opening.
        var real = RealMapFixture.Load().World;
        var set = BuildingSet.For(real, 1u);
        var keyed = real.Features.Values.Where(f => f.Facade is { } facade &&
            (facade.Value.StartsWith("annex.", StringComparison.Ordinal) || facade.Value.StartsWith("dorm.", StringComparison.Ordinal)))
            .Select(f => (Way: f.Id.Value, Key: f.Facade!.Value.Value)).ToArray();
        AssertThat(keyed.Length > 0).IsTrue();
        foreach (var (way, key) in keyed)
        {
            var plans = set.Plans.Where(p => p.Id.Value == way).ToArray();
            AssertThat(plans.Length > 0).OverrideFailureMessage("Expected: " + ($"{way} is a building of the real campus")).IsTrue();
            var look = Rules.FacadePresets.Find(new FacadePreset(key))!;
            foreach (var plan in plans)
            {
                AssertThat(plan.Profile.Preset == key && plan.Profile.Wall == look.Frame && plan.Profile.Trim == look.Trim).OverrideFailureMessage("Expected: " + ($"{way}: takes {key}")).IsTrue();
                AssertThat(!BuildingsModule.UnknownPreset(plan).Any()).OverrideFailureMessage("Expected: " + ($"{way}: {key} is known")).IsTrue();
                var windows = plan.Openings.SelectMany(o => o).Where(o => o.Kind == BayKind.Window).ToArray();
                AssertThat(plan.Profile.WindowHeight == look.WindowHeight && plan.Profile.Sill == look.Sill && plan.Profile.GroundSill == look.GroundSill)
                    .OverrideFailureMessage($"{way}: nominal window dimensions come from {key}.").IsTrue();
                // The storey rows already account for the plinth, slab clearance and roof; openings must use those exact rows.
                AssertThat(windows.Length > 0 && windows.All(w => MathF.Abs(w.Y0 - plan.SillY[w.Row]) < TestTolerance.Position &&
                    MathF.Abs(w.Y1 - plan.HeadY[w.Row]) < TestTolerance.Position && w.Y1 > w.Y0 && w.Y1 - w.Y0 <= look.WindowHeight!.Value + TestTolerance.Position))
                    .OverrideFailureMessage($"{way}: windows follow their constrained sill/head rows and stay within the preset's nominal height.").IsTrue();
            }
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void NorthUnkeyedBlocksTakeTheBohanLanguage()
    {
        // Sample dormitories take their authored keys; each takes the
        // preset over whatever family the data gives it, and not one window changes its number (contract-29).
        var real = RealMapFixture.Load().World;
        var was = BuildingSet.For(real, 1u);
        var candidates = was.Plans.Where(p => p.Style == Rules.BuildingStyle.Dormitory).Select(p => p.Id.Value).Distinct().Take(3).ToArray();
        AssertThat(candidates.Length > 0).IsTrue();
        var keyOf = candidates.Select((id, i) => (id, key: i == candidates.Length - 1 ? Rules.FacadePresets.DormMasterCreamCoffeeWhite.Key : Rules.FacadePresets.DormMasterCreamCoffee.Key))
            .ToDictionary(p => p.id, p => p.key, StringComparer.Ordinal);
        var now = BuildingSet.For(Styled(real, f => keyOf.TryGetValue(f.Id.Value, out var k) ? f with { Facade = new FacadePreset(k) } : f), 1u);
        AssertThat(now.Blocks.SequenceEqual(was.Blocks)).OverrideFailureMessage("Expected: " + ("every window block as before")).IsTrue();
        AssertThat(Layout(now, where: p => !keyOf.ContainsKey(p.Id.Value)) == Layout(was, where: p => !keyOf.ContainsKey(p.Id.Value))).OverrideFailureMessage("Expected: " + ("the other buildings cell for cell")).IsTrue();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < was.Plans.Count; i++)
        {
            var (before, after) = (was.Plans[i], now.Plans[i]);
            if (!keyOf.TryGetValue(before.Id.Value, out var k)) continue;
            seen.Add(before.Id.Value);
            AssertThat(after.Profile.Preset == k && after.Profile.Wall == Rules.Families.WallCreamCoffee).OverrideFailureMessage("Expected: " + ($"{before.Key} takes {k}")).IsTrue();
            AssertThat(after.TotalColumns == before.TotalColumns && after.Storeys == before.Storeys && Cells(after) == Cells(before)).OverrideFailureMessage("Expected: " + ($"{before.Key} keeps its window grid")).IsTrue();
        }
        AssertThat(seen.Count == keyOf.Count).OverrideFailureMessage("Expected: " + ($"all three blocks planned ({seen.Count})")).IsTrue();
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
                if (!Enumerable.Range(0, plan.Storeys).Any(k => low >= MathF.Max(plan.FloorY(k), plan.PlinthTopY) - TestTolerance.Position && high <= plan.SillY[k] + TestTolerance.Position)) strays++;
            }
        var expected = plan.Openings.Sum(o => o.Where(x => x.Kind == BayKind.Window)
            .Sum(x => (double)(x.U1 - x.U0) * (plan.SillY[x.Row] - MathF.Max(plan.FloorY(x.Row), plan.PlinthTopY))));
        return (area, expected, strays);
    }

}
