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
public sealed class EntranceTests
{
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
}
