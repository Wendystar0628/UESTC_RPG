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
public sealed class FacadePresetTests
{
    [TestCase]
    public void OrdinaryBuildingsKeepTheirOpeningsAndNumbers()
    {
        // Buildings without an entrance anchor lay out the same way every build: openings, window numbers and cards cell for
        // cell. Anchored buildings only move their door; their window blocks never change.
        var features = new[]
        {
            Feature("t:dorm", Rect(0, 0, 60, 18), 6, 19.8f, "residence"), Feature("t:lab", new PolygonRings(LShape(100, 0)), 4, 15.6f, "research"),
            Feature("t:court", Courtyard(0, 100), 3, 12f, "teaching"), Feature("t:hall", Rect(200, 0, 40, 30), 1, 9f, "sport"),
            Feature("t:small", Rect(300, 0, 5, 6), 1, 3f, "service"),
        };
        var plain = BuildingSet.For(World(features), 4u);
        AssertThat(Layout(BuildingSet.For(World(features), 4u)) == Layout(plain)).OverrideFailureMessage("Expected: " + ("the same input lays out ordinary buildings cell for cell")).IsTrue();
        var anchored = BuildingSet.For(World(Entrances(Anchor("t:dorm", "a", new(21.3f, 0f, 0f), 0f)), features), 4u);
        AssertThat(anchored.Blocks.SequenceEqual(plain.Blocks)).OverrideFailureMessage("Expected: " + ("an entrance anchor leaves every window block as it was")).IsTrue();
        AssertThat(Layout(anchored).Replace(Layout(anchored, "t:dorm"), "") == Layout(plain).Replace(Layout(plain, "t:dorm"), "")).OverrideFailureMessage("Expected: " + ("the other buildings keep their layout")).IsTrue();
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
        AssertThat(styled.Blocks.SequenceEqual(plain.Blocks)).OverrideFailureMessage("Expected: " + ("every window block as before")).IsTrue();
        AssertThat(Layout(styled, where: p => p.Key != "t:lab") == Layout(plain, where: p => p.Key != "t:lab")).OverrideFailureMessage("Expected: " + ("the other buildings cell for cell")).IsTrue();
        var (before, after) = (plain.Plans.Single(p => p.Key == "t:lab"), styled.Plans.Single(p => p.Key == "t:lab"));
        AssertThat(after.Profile.Preset == preset.Key && before.Profile.Preset is null && after.Style == before.Style).OverrideFailureMessage("Expected: " + ("the preset over the research family")).IsTrue();
        static string Cells(BuildingPlan p) => string.Join(" ", p.Openings.SelectMany((o, fi) => o.Select(x => $"{fi}/{x.Bay}/{x.Row}/{x.Kind}")));
        AssertThat(Cells(after) == Cells(before)).OverrideFailureMessage("Expected: " + ("the same window cells and kinds, so the same window numbers and cards")).IsTrue();
        static float Glazed(BuildingPlan p) => p.Openings.Sum(o => o.Sum(x => (x.U1 - x.U0) * (x.Y1 - x.Y0)));
        AssertThat(Glazed(after) > 1.6f * Glazed(before)).OverrideFailureMessage("Expected: " + ($"more and larger glass: {Glazed(before):0} → {Glazed(after):0} m²")).IsTrue();

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

        // The real campus with the innovation centre's five buildings on the preset (the StyleOps of overlay/south.d/): no
        // window number of the map moves, the other buildings stay cell for cell, and every footprint of the five takes it.
        string[] innovation = ["qsh:relation:10807928", "qsh:relation:10807929", "qsh:relation:17732622", "qsh:way:779496164", "qsh:way:779496165"];
        var real = RealCampus();
        var withPreset = Styled(real, f => innovation.Contains(f.Id.Value) ? f with { Family = new BuildingFamily("research"), Facade = new FacadePreset(preset.Key) } : f);
        var (was, now) = (BuildingSet.For(real, 1u), BuildingSet.For(withPreset, 1u));
        AssertThat(now.Blocks.SequenceEqual(was.Blocks)).OverrideFailureMessage("Expected: " + ("real campus: every window block as before")).IsTrue();
        AssertThat(Layout(now, where: p => !innovation.Contains(p.Id.Value)) == Layout(was, where: p => !innovation.Contains(p.Id.Value))).OverrideFailureMessage("Expected: " + ("real campus: the other buildings cell for cell")).IsTrue();
        var centre = now.Plans.Where(p => innovation.Contains(p.Id.Value)).ToArray();
        AssertThat(centre.Length >= 10 && centre.All(p => p.Profile.Preset == preset.Key && p.Style == Rules.BuildingStyle.Research)).OverrideFailureMessage("Expected: " + ($"the centre's {centre.Length} footprints take the preset")).IsTrue();
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

    /// <summary>The three annex presets of the north zone (WP-20, WLD-INTERIORS) with the family each one lives in.</summary>
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
        AssertThat(all.Select(p => p.Key).Distinct(StringComparer.Ordinal).Count() == all.Count).OverrideFailureMessage("Expected: " + ("one preset per key")).IsTrue();
        var tokens = PaletteTokens();
        foreach (var (key, glazing, windowHeight, sill, groundSill, glass, frame, trim, accent, window) in DormTable)
        {
            var look = Rules.FacadePresets.Find(new FacadePreset(key)) ?? throw new InvalidOperationException($"Expected: {key} is a known preset");
            AssertThat(look.Key == key).OverrideFailureMessage("Expected: " + ($"{key} is found under its own key")).IsTrue();
            AssertThat(look.Glazing == glazing && look.WindowHeight == windowHeight && look.Sill == sill && look.GroundSill == groundSill).OverrideFailureMessage("Expected: " + ($"{key}: the window's size")).IsTrue();
            AssertThat(look.Glass == glass && look.Frame == frame && look.Trim == trim && look.Accent == accent && look.Window == window).OverrideFailureMessage("Expected: " + ($"{key}: colours and window part")).IsTrue();
            foreach (var family in new[] { glass, frame, trim, accent })
                AssertThat(tokens.Contains(family + ".lit") && tokens.Contains(family + ".shade")).OverrideFailureMessage("Expected: " + ($"{key}: {family} is in the palette")).IsTrue();
            // Only type A puts its 陶红 as aprons under the windows; the masters' accent takes the window column whole (on the stone frame it is no band).
            AssertThat(look.AccentApron == (key.StartsWith("dorm.a.", StringComparison.Ordinal) ? true : (bool?)null)).OverrideFailureMessage("Expected: " + ($"{key}: the accent as aprons")).IsTrue();
        }
        AssertThat(Rules.FacadePresets.Find(new FacadePreset("dorm.a.nothing")) is null).OverrideFailureMessage("Expected: " + ("an unknown key stays unknown")).IsTrue();
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
        AssertThat(seedA is not null && seedB is not null).OverrideFailureMessage("Expected: " + ("seeds for both dormitory variants")).IsTrue();
        foreach (var (key, _, _, _, _, _, frame, trim, accent, _) in DormTable)
        {
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
                AssertThat(spread.Wall is > 0.23 and < 0.29).OverrideFailureMessage("Expected: " + ($"{what}: about a quarter of the wall with the lintels ({spread.Wall:0.000})")).IsTrue();
                AssertThat(share.Wall is > 0.15 and < 0.19).OverrideFailureMessage("Expected: " + ($"{what}: about a sixth of the wall as aprons ({share.Wall:0.000})")).IsTrue();
                var (area, expected, strays) = ApronCheck(apron);
                AssertThat(strays == 0).OverrideFailureMessage("Expected: " + ($"{what}: {strays} triangles of 陶红 outside a floor-to-sill band")).IsTrue();
                AssertThat(MathF.Abs((float)(area / expected) - 1f) < 0.01f).OverrideFailureMessage("Expected: " + ($"{what}: the 陶红 is the aprons ({area:0.0} of {expected:0.0} m²)")).IsTrue();
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

        var real = RealCampus();
        var was = BuildingSet.For(real, 1u);
        var dorms = was.Plans.Where(p => p.Style == Rules.BuildingStyle.Dormitory).Select(p => p.Id).Distinct().ToArray();
        AssertThat(dorms.Length >= 20).OverrideFailureMessage("Expected: " + ($"the campus has dormitories ({dorms.Length})")).IsTrue();
        var keyOf = dorms.Select((id, i) => (id, key: DormTable[i % DormTable.Length].Key)).ToDictionary(x => x.id, x => x.key);
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

    [TestCase]
    public void NorthAnnexPresetsMatchTheTable()
    {
        var tokens = PaletteTokens();
        foreach (var (key, _, _, glazing, windowHeight, sill, groundSill, frame, trim, window) in AnnexTable)
        {
            var look = Rules.FacadePresets.Find(new FacadePreset(key)) ?? throw new InvalidOperationException($"Expected: {key} is a known preset");
            AssertThat(look.Glazing == glazing && look.WindowHeight == windowHeight && look.Sill == sill && look.GroundSill == groundSill).OverrideFailureMessage("Expected: " + ($"{key}: the window's size")).IsTrue();
            AssertThat(look.Glass == "glass" && look.Frame == frame && look.Trim == trim && look.Accent == frame && look.Window == window && look.AccentApron is null).OverrideFailureMessage("Expected: " + ($"{key}: colours and window part")).IsTrue();
            foreach (var family in new[] { frame, trim })
                AssertThat(tokens.Contains(family + ".lit") && tokens.Contains(family + ".shade")).OverrideFailureMessage("Expected: " + ($"{key}: {family} is in the palette")).IsTrue();
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
            AssertThat(plans.Length > 0).OverrideFailureMessage("Expected: " + ($"{way} is a building of the real campus")).IsTrue();
            var look = Rules.FacadePresets.Find(new FacadePreset(key))!;
            foreach (var plan in plans)
            {
                AssertThat(plan.Profile.Preset == key && plan.Profile.Wall == look.Frame && plan.Profile.Trim == look.Trim).OverrideFailureMessage("Expected: " + ($"{way}: takes {key}")).IsTrue();
                AssertThat(!BuildingsModule.UnknownPreset(plan).Any()).OverrideFailureMessage("Expected: " + ($"{way}: {key} is known")).IsTrue();
                var windows = plan.Openings.SelectMany(o => o).Where(o => o.Kind == BayKind.Window).ToArray();
                // Upper storeys open at exactly the preset's height; the ground storey's sill is a little higher, which may take a few cm off the 6-storey blocks.
                AssertThat(windows.Length > 0 && windows.All(w => MathF.Abs(w.Y1 - w.Y0 - look.WindowHeight!.Value) < (w.Row == 0 ? 0.1f : 0.01f))).OverrideFailureMessage("Expected: " + ($"{way}: windows {look.WindowHeight} m high, got {string.Join(", ", windows.Select(w => (w.Y1 - w.Y0).ToString("0.000")).Distinct())}")).IsTrue();
            }
        }
        var table = AnnexTable.ToDictionary(r => r.Key);
        foreach (var (way, key) in keyed.Where(k => table.ContainsKey(k.Key)))
            AssertThat(set.Plans.Where(p => p.Id.Value == way).All(p => p.Style == table[key].Style)).OverrideFailureMessage("Expected: " + ($"{way}: stays in the {table[key].Family} family")).IsTrue();
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
}
