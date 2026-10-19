using Campus.Tests;
using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Parts;
using Campus.Gen.Buildings.Build;
using Campus.Gen.Buildings.Model;
using Campus.Gen.Buildings.Plan;
using Campus.Gen.Tests.Support;
using GdUnit4;

namespace Campus.Gen.Buildings.Tests;

using static Campus.Gen.Buildings.Tests.BuildingsTestSupport;

using static GdUnit4.Assertions;

[TestSuite]
public sealed class EntranceTests
{
    [TestCase, TestCategory(TestReason.Contract)]
    public void DoorsOpenAtTheEntranceAnchors()
    {
        var index = Entrances(
            Anchor("t:dorm", "dorm.south", new(21.3f, 0f, 0f), 0f),                          // off the bay centre, at the floor
            Anchor("t:lab", "lab.porch", new(112f, 1.2f, 0f), 0f, outsideY: 1.2f),             // raised onto a porch
            Anchor("t:low", "low.mid", new(220f, 2.0f, 0f), 0f),                                // no door height under the next floor
            Anchor("t:wing", "wing.east", new(320f, 0f, 8f), 90f));                             // opens into the taller block next door
        var features = new[]
        {
            WorldFeatureFixture.Create("t:dorm", "building", new PolygonGeometry(new PolygonRings(Rect(0, 0, 60, 18))), props: "{\"category\":\"residence\"}", height: 19.8f, storeys: 6), WorldFeatureFixture.Create("t:lab", "building", new PolygonGeometry(new PolygonRings(Rect(100, 0, 40, 16))), props: "{\"category\":\"research\"}", height: 15.6f, storeys: 4),
            WorldFeatureFixture.Create("t:low", "building", new PolygonGeometry(new PolygonRings(Rect(200, 0, 40, 16))), props: "{\"category\":\"teaching\"}", height: 13.2f, storeys: 4), WorldFeatureFixture.Create("t:wing", "building", new PolygonGeometry(new PolygonRings(Rect(300, 0, 20, 16))), props: "{\"category\":\"teaching\"}", height: 10.5f, storeys: 3),
            WorldFeatureFixture.Create("t:block", "building", new PolygonGeometry(new PolygonRings(Rect(320, 0, 20, 16))), props: "{\"category\":\"teaching\"}", height: 18f, storeys: 5),
        };
        var world = World(index, features);
        var set = BuildingSet.For(world, 2u);
        var generic = BuildingSet.For(World(features), 2u);
        var module = new BuildingsModule();
        foreach (var (plan, i) in set.Plans.Select((p, i) => (p, i)))
        {
            var decls = FacadeOpenings.Declarations(plan, set.Blocks[i]);
            AssertThat(decls.Select(d => d.Id).Distinct().Count() == decls.Count && decls.All(d => d.Id >= set.Blocks[i].Base && d.Id < set.Blocks[i].Base + set.Blocks[i].Count)).OverrideFailureMessage("Expected: " + ($"{plan.Key}: every opening keeps a unique number inside its block")).IsTrue();
            foreach (var lod in Levels) foreach (var b in module.BuildOne(plan, set.Blocks[i], lod, Campus.Contracts.Tests.PaletteFixture.Catalog)) b.ValidateVertexData();
        }
        var dorm = set.Plans.Single(p => p.Key == "t:dorm");
        var entrance = dorm.Entrances.Single();
        AssertThat(entrance.Anchor == "dorm.south" && entrance.Row == 0 && entrance.Landing).OverrideFailureMessage("Expected: " + ("the dorm's door moved to its anchor, with a landing")).IsTrue();
        var door = dorm.DoorOf(entrance);
        AssertThat(MathF.Abs(door.Y0 - dorm.FloorY(0)) < TestTolerance.Position).OverrideFailureMessage("Expected: " + ("an anchor at the pad takes the building's floor (WLD-TERRAIN)")).IsTrue();
        AssertThat(Vector2.Distance(GlassCentre(dorm, entrance), new Vector2(21.3f, 0f)) <= Rules.EntranceRules.AnchorTolerance).OverrideFailureMessage("Expected: " + ("door within G18's 0.5 m of its anchor")).IsTrue();
        AssertThat(!dorm.Openings[entrance.Facade].Any(o => o.Row == 0 && o.Kind != BayKind.Door && o.U0 < door.U1 + Rules.EntranceRules.Pier && o.U1 > door.U0 - Rules.EntranceRules.Pier)).OverrideFailureMessage("Expected: " + ("no window crowds the door")).IsTrue();
        var old = generic.Plans.Single(p => p.Key == "t:dorm");
        AssertThat(old.Entrances.Single().Facade != entrance.Facade || old.Entrances.Single().Bay != entrance.Bay).OverrideFailureMessage("Expected: " + ("the generic door is gone")).IsTrue();
        var lab = set.Plans.Single(p => p.Key == "t:lab").Entrances.Single();
        var labDoor = set.Plans.Single(p => p.Key == "t:lab").DoorOf(lab);
        AssertThat(lab.Anchor == "lab.porch" && !lab.Landing && MathF.Abs(labDoor.Y0 - 1.2f) < TestTolerance.Position && labDoor.Y1 - labDoor.Y0 >= 2f).OverrideFailureMessage("Expected: " + ("a raised anchor opens a door at its floor, onto the porch, without a landing")).IsTrue();
        AssertThat(EntranceKit.Pieces(set.Plans.Single(p => p.Key == "t:lab"), lab).Count == 0).OverrideFailureMessage("Expected: " + ("no WP-14 landing under a porch")).IsTrue();
        foreach (var key in new[] { "t:low", "t:wing" })
        {
            var plan = set.Plans.Single(p => p.Key == key);
            AssertThat(plan.Entrances.Single().Anchor is null && Layout(set, key) == Layout(generic, key)).OverrideFailureMessage("Expected: " + ($"{key}: the anchor is left alone and the generic door stays")).IsTrue();
        }
        // The authored opening is clear, with the wall above and below the door retained.
        foreach (var plan in new[] { dorm, set.Plans.Single(p => p.Key == "t:lab") })
        {
            var e = plan.Entrances.Single();
            var opening = plan.DoorOf(e);
            var centre = GlassCentre(plan, e);
            var walls = module.Collide(Ctx(world, 2u), plan.Tile(LodLevel.L0)).OfType<WallCollider>().ToArray();
            AssertThat(!walls.Any(w => Blocks(w, centre, (opening.Y0 + opening.Y1) * 0.5f))).OverrideFailureMessage("Expected: " + ($"{plan.Key}: open anchor must not be blocked")).IsTrue();
            AssertThat(walls.Any(w => Blocks(w, centre, opening.Y0 - 0.1f)) && walls.Any(w => Blocks(w, centre, opening.Y1 + 0.1f))).OverrideFailureMessage("Expected: " + ($"{plan.Key}: retain the wall below the threshold and above the lintel")).IsTrue();
        }
        var closedPlan = generic.Plans.Single(p => p.Key == "t:dorm");
        var closedEntrance = closedPlan.Entrances.Single();
        var closedDoor = closedPlan.DoorOf(closedEntrance);
        AssertThat(module.Collide(Ctx(World(features), 2u), closedPlan.Tile(LodLevel.L0)).OfType<WallCollider>()
            .Any(w => w.Tag == SurfaceTag.DoorClosed && Blocks(w, GlassCentre(closedPlan, closedEntrance), (closedDoor.Y0 + closedDoor.Y1) * 0.5f))).OverrideFailureMessage("Expected: " + ("the undeclared generic entrance stays closed")).IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void ClaimedFacadesOpenNoDoors()
    {
        // WP-14 opens doors only on the facades it generates: a zone pack's Facade or Massing claim takes the door, its
        // landing, canopy and doorway colliders with it (the zone pack builds that door).
        var features = new[] { WorldFeatureFixture.Create("t:dorm", "building", new PolygonGeometry(new PolygonRings(Rect(0, 0, 60, 18))), props: "{\"category\":\"residence\"}", height: 19.8f, storeys: 6), WorldFeatureFixture.Create("t:lab", "building", new PolygonGeometry(new PolygonRings(Rect(200, 0, 40, 16))), props: "{\"category\":\"research\"}", height: 15.6f, storeys: 4) };
        var world = World(Entrances(Anchor("t:dorm", "dorm.south", new(21.3f, 0f, 0f), 0f)), features);
        var module = new BuildingsModule();
        var dorm = BuildingSet.For(world, 2u).Plans.Single(p => p.Key == "t:dorm");
        var tile = dorm.Tile(LodLevel.L0);
        AssertThat(dorm.Entrances.Single().Anchor == "dorm.south" && module.Collide(Ctx(world, 2u), tile).OfType<WallCollider>().Any() &&
            !module.Collide(Ctx(world, 2u), tile).OfType<WallCollider>().Any(w => w.Tag == SurfaceTag.DoorClosed)).OverrideFailureMessage("Expected: " + ("unclaimed, WP-14 opens the anchor door")).IsTrue();
        foreach (var aspect in new[] { Aspects.Facade, Aspects.Massing })
        {
            var ctx = Prepared(new BuildContext { Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog, World = world, Map = TestMap, WorldSeed = 2u, ClaimedBy = (f, a) => a == aspect && f.Value == "t:dorm" ? new WpId("WP-18") : null });
            AssertThat(!module.Tiles(ctx).Contains(tile) && module.Build(ctx, tile, LodLevel.L0) == ContentOutput.Empty && module.Collide(ctx, tile).Count == 0).OverrideFailureMessage("Expected: " + ($"{aspect} claimed: no door, landing, canopy or collider of WP-14")).IsTrue();
            AssertThat(module.Build(ctx, BuildingSet.For(world, 2u).Plans.Single(p => p.Key == "t:lab").Tile(LodLevel.L0), LodLevel.L0).Meshes.Count > 0).OverrideFailureMessage("Expected: " + ("the neighbour stays")).IsTrue();
        }

        // The real campus under every zone pack's claims (the kernel's ledger rebuilt from the content modules): WP-14's
        // authored openings at open anchors remain clear. The library (16b), Xuanbang's clock tower
        // (18b) and, since 18c2, the gym deck are claimed: their doors are the zone packs'.
        var real = RealMapFixture.Load().World;
        var rctx = new BuildContext { Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog, World = real, Map = TestMap, WorldSeed = 1u, ClaimedBy = RealClaims(real) };
        var anchored = BuildingSet.For(real, 1u).Plans.SelectMany(p => p.Entrances.Where(e => e.Anchor is not null).Select(e => (Plan: p, Door: e))).ToArray();
        foreach (var (plan, e) in anchored)
        {
            var mine = BuildingsModule.Unclaimed(rctx, plan);
            var centre = GlassCentre(plan, e);
            var opening = plan.DoorOf(e);
            var walls = module.Collide(rctx, plan.Tile(LodLevel.L0)).OfType<WallCollider>().ToArray();
            AssertThat(!walls.Any(w => Blocks(w, centre, (opening.Y0 + opening.Y1) * 0.5f))).OverrideFailureMessage("Expected: " + ($"{e.Anchor} on {plan.Id.Value}: open doorway is clear")).IsTrue();
            if (mine) AssertThat(walls.Any(w => Blocks(w, centre, opening.Y1 + 0.1f))).OverrideFailureMessage("Expected: " + ($"{e.Anchor}: the lintel remains")).IsTrue();
        }
        AssertThat(anchored.Any(x => !BuildingsModule.Unclaimed(rctx, x.Plan)))
            .OverrideFailureMessage("Authored zone entrances give way to their owning pack.").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void AnchorsSetTheDoorHeadAndLogDoorsAlone()
    {
        // contract-34: the anchor's HeadY is the door top (under the next slab); without one the family's door rise stays.
        var dorm = WorldFeatureFixture.Create("t:dorm", "building", new PolygonGeometry(new PolygonRings(Rect(0, 0, 60, 18))), props: "{\"category\":\"residence\"}", height: 19.8f, storeys: 6);
        PlanEntrance? Door(float head, out BuildingPlan plan)
        {
            var (feature, decl) = Anchor("t:dorm", "dorm.south", new(21.3f, 0f, 0f), 0f);
            plan = BuildingSet.For(World(Entrances((feature, decl with { HeadY = head })), dorm), 2u).Plans.Single();
            return plan.Entrances.SingleOrDefault(e => e.Anchor == "dorm.south");
        }
        var known = Door(2.6f, out var p1)!;
        AssertThat(MathF.Abs(p1.DoorOf(known).Y1 - 2.6f) < TestTolerance.Position).OverrideFailureMessage("Expected: " + ("HeadY 2.6 is the door top")).IsTrue();
        var unknown = Door(0f, out var p0)!;
        AssertThat(MathF.Abs(p0.DoorOf(unknown).Y1 - (p0.FloorY(0) + Math.Clamp(p0.StoreyHeight - 1f, 2.1f, 2.4f))) < TestTolerance.Position).OverrideFailureMessage("Expected: " + ("0 = unknown: the family's door rise")).IsTrue();
        var tall = Door(9f, out var p2)!;
        AssertThat(MathF.Abs(p2.DoorOf(tall).Y1 - (p2.FloorY(1) - Rules.EntranceRules.SlabDepth)) < TestTolerance.Position).OverrideFailureMessage("Expected: " + ("never through the next slab")).IsTrue();

        // Doors alone (outside landing on a porch or deck) are logged with the package expected to build what they open onto.
        var lab = WorldFeatureFixture.Create("t:lab", "building", new PolygonGeometry(new PolygonRings(Rect(100, 0, 40, 16))), props: "{\"category\":\"research\"}", height: 15.6f, storeys: 4) with { Zone = new ZoneId("north") };
        var porch = Entrances(Anchor("t:lab", "lab.porch", new(112f, 1.2f, 0f), 0f, outsideY: 1.2f));
        var deck = WorldFeatureFixture.Create("t:deck", "building", new PolygonGeometry(new PolygonRings(Rect(100, -6, 40, 6))), props: "{\"category\":\"service\"}", height: 1.2f, storeys: 1);
        string Log(FakeWorld w, Func<FeatureId, Aspects, WpId?> claims)
        {
            var ctx = new BuildContext { Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog, World = w, Map = TestMap, WorldSeed = 2u, ClaimedBy = claims };
            var plan = BuildingSet.For(w, 2u).Plans.Single(p => p.Key == "t:lab");
            return BuildingsModule.DoorsAlone(ctx, plan).Single(d => d.Level == DiagnosticLevel.Info && d.Code == "wp14.door.alone" && d.Feature == plan.Id).Message;
        }
        var byZone = Log(World(porch, lab), (_, _) => null);
        AssertThat(byZone.Contains("lab.porch", StringComparison.Ordinal) && byZone.Contains("1.20 m", StringComparison.Ordinal) && byZone.Contains("north zone pack", StringComparison.Ordinal)).OverrideFailureMessage("Expected: " + (byZone)).IsTrue();
        var byDeck = Log(World(porch, lab, deck), (f, a) => f.Value == "t:deck" && a == Aspects.Massing ? new WpId("WP-21") : null);
        AssertThat(byDeck.Contains("WP-21, which builds the deck block t:deck", StringComparison.Ordinal)).OverrideFailureMessage("Expected: " + (byDeck)).IsTrue();
        var nobody = Log(World(porch, lab with { Zone = null }), (_, _) => null);
        AssertThat(nobody.Contains("undecided", StringComparison.Ordinal)).OverrideFailureMessage("Expected: " + (nobody)).IsTrue();
        var ctx0 = Prepared(Ctx(World(porch, lab), 2u));
        var plan0 = BuildingSet.For(ctx0.World, 2u).Plans.Single();
        AssertThat(new BuildingsModule().Build(ctx0, plan0.Tile(LodLevel.L0), LodLevel.L0).Diagnostics.Count == 1 &&
            new BuildingsModule().Build(ctx0, plan0.Tile(LodLevel.L1), LodLevel.L1).Diagnostics.Count == 0).OverrideFailureMessage("Expected: " + ("logged once, with the L0 cell")).IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void EntranceLandingsFollowTheOutsideGround()
    {
        // The ground south of the dorm (z < -1) lies 0.9 m below its pad; the lab stands on level ground (WLD-TERRAIN).
        var world = World(Entrances(Anchor("t:dorm", "dorm.south", new(21.3f, 0f, 0f), 0f, outsideY: -0.9f)),
            Falling(-1f, 0.9f), WorldFeatureFixture.Create("t:dorm", "building", new PolygonGeometry(new PolygonRings(Rect(0, 0, 60, 18))), props: "{\"category\":\"residence\"}", height: 19.8f, storeys: 6), WorldFeatureFixture.Create("t:lab", "building", new PolygonGeometry(new PolygonRings(Rect(100, 20, 40, 16))), props: "{\"category\":\"research\"}", height: 15.6f, storeys: 4));
        var set = BuildingSet.For(world, 2u);
        var dorm = set.Plans.Single(p => p.Key == "t:dorm");
        var steps = EntranceKit.Pieces(dorm, dorm.Entrances.Single());
        AssertThat(steps.Select(p => p.Part).SequenceEqual([PartCatalog.Platform, PartCatalog.Railing, PartCatalog.Railing, PartCatalog.Steps])).OverrideFailureMessage("Expected: " + ("landing, guard railings on its sides (over 1.0 m) and one flight")).IsTrue();
        var flight = (StairParams)steps[3].Params;
        AssertThat(MathF.Abs(flight.Height - 1.05f) < TestTolerance.Position && flight.Steps == 7 && flight.Variation == 0).OverrideFailureMessage("Expected: " + ($"1.05 m in 7 risers with cheek walls ({flight.Steps})")).IsTrue();
        var sink = new Mesh.MeshSink(Campus.Contracts.Tests.PaletteFixture.Catalog);
        EntranceKit.Build(sink, steps, LodLevel.L0);
        AssertThat(sink.Finish().Any(b => b.Material.Spec.Normal == ToonNormal.Smooth && b.Material.Spec.Lit.Value == "frame.alu.lit")).OverrideFailureMessage("Expected: " + ("handrails from 0.6 m (WP-11)")).IsTrue();
        foreach (var lod in Levels) foreach (var b in new BuildingsModule().BuildOne(dorm, set.Blocks[0], lod, Campus.Contracts.Tests.PaletteFixture.Catalog)) b.ValidateVertexData();
        var colliders = new BuildingsModule().Collide(Ctx(world, 2u), dorm.Tile(LodLevel.L0));
        AssertThat(colliders.OfType<RampCollider>().Any(r => r.Stair is { Count: 7 })).OverrideFailureMessage("Expected: " + ("the flight walks as one stair ramp")).IsTrue();
        AssertThat(colliders.OfType<BoxCollider>().Any(b => b.Tag == SurfaceTag.Ground)).OverrideFailureMessage("Expected: " + ("the landing is walkable")).IsTrue();
        AssertThat(colliders.OfType<WallCollider>().Count(w => w.Tag == SurfaceTag.Barrier && w.Y1 - w.Y0 >= 1.5f) == 2).OverrideFailureMessage("Expected: " + ("its railings cannot be climbed over")).IsTrue();
        var lab = set.Plans.Single(p => p.Key == "t:lab");
        var slope = EntranceKit.Pieces(lab, lab.Entrances.Single());
        AssertThat(slope.Select(p => p.Part).SequenceEqual([PartCatalog.Platform, PartCatalog.Ramp])).OverrideFailureMessage("Expected: " + ("0.15 m: landing and a kerbed slope, no step")).IsTrue();
        var ramp = (RampParams)slope[1].Params;
        AssertThat(ramp.Height / ramp.Depth <= 1f / 12f + TestTolerance.Position && ramp.Variation == 1).OverrideFailureMessage("Expected: " + ("the slope is 1:12 or gentler")).IsTrue();
    }
}
