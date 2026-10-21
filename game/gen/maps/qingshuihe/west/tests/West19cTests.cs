using Campus.Gen.Sites.Tests;
using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Tests;
using Campus.Contracts.Audio;
using Campus.Contracts.Maps;
using Campus.Contracts.Parts;
using Campus.Gen.Tests.Support;
using Campus.Gen.World;
using Campus.Gen.Maps.Qingshuihe.West.Bridgehead;
using Campus.Gen.Maps.Qingshuihe.West.Framework;
using Campus.Gen.Sites;
using Campus.Gen.Maps.Qingshuihe.West.Wetland;
using GdUnit4;
using C = Campus.Gen.Maps.Qingshuihe.West.WestTune.Carving;
using H = Campus.Gen.Maps.Qingshuihe.West.WestTune.Shelter;
using T = Campus.Gen.Maps.Qingshuihe.West.WestTune.LeveeSteps;
using W = Campus.Gen.Maps.Qingshuihe.West.WestTune.Wetland;

namespace Campus.Gen.Maps.Qingshuihe.West.Tests;
using static GdUnit4.Assertions;

/// <summary>19c: wetland boardwalk, wading beach, levee steps, bridgehead shelter and the 国防科技园 carving.</summary>
[TestSuite]
public sealed class West19cTests
{
    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void BoardwalkRunsThroughTheReedsClearOfTheBridgesAndTheStream()
    {
        var world = World();
        var path = BoardwalkSite.Path(world);
        // Both mouths meet paving: the A10 plaza in the south, the footway at the 西门车行桥's east head in the north.
        var plaza = ((PolygonGeometry)world.Features[new(WestIds.A10Plaza)].Geometry).Rings.Outer;
        AssertThat(PlanRings.InsideByCrossings(plaza, path.Points[0]) || Distance([.. plaza, plaza[0]], path.Points[0]) < 1.5f).OverrideFailureMessage("the south mouth lands on the A10 plaza").IsTrue();
        var footway = Line(world, WestIds.Way552816780.Source);
        AssertThat(footway.Any(p => Vector2.Distance(p, path.Points[^1]) < 1f)).OverrideFailureMessage("the north mouth lands on the bridgehead footway's end").IsTrue();
        // Inside the low ground (except the last few metres climbing out to the north footway).
        for (var s = 4f; s < path.Length - 8f; s += 2f)
            AssertThat(PlanRings.InsideByCrossings(((TerrainPlatformOp)world.TerrainOperations[WestIds.WetlandLow]).Rings.Outer, path.At(s).P)).OverrideFailureMessage($"boardwalk station {s:0} lies in the wetland low ground").IsTrue();
        // Clear of game/gen/maps/qingshuihe/waterfront/'s bridges (r005: all bridges are game/gen/maps/qingshuihe/waterfront/'s) and of the stream except at the beach turn.
        foreach (var id in new[] { WestIds.Way692240860.Source, WestIds.Way552816781.Source })
        {
            var bridge = world.Network.Bridges.Single(b => b.Id.Value == id).Line.Points;
            var near = path.Points.Min(p => Distance(bridge, p));
            AssertThat(near > 5f).OverrideFailureMessage($"the boardwalk keeps clear of bridge {id}, nearest {near:0.0} m").IsTrue();
        }
        var stream = Line(world, WestIds.Way217855313.Source, waterway: true);
        var beach = BoardwalkSite.Beach(world);
        for (var s = 0f; s < path.Length; s += 1f)
        {
            var d = Distance(stream, path.At(s).P);
            AssertThat(d > W.Width * 0.5f).OverrideFailureMessage($"station {s:0}: deck centre {d:0.0} m from the stream").IsTrue();
        }
        // The beach lies between the deck and the stream, on the side the rail opens.
        var (bs, lateral) = path.Project(W.BeachCentre(world));
        AssertThat(MathF.Abs(bs - beach.S) < TestTolerance.Position && MathF.Abs(lateral) > W.Width * 0.5f).OverrideFailureMessage("the beach lies beside the deck").IsTrue();
        AssertThat(Distance(stream, W.BeachCentre(world)) < Distance(stream, path.At(bs).P)).OverrideFailureMessage("the beach is on the stream side").IsTrue();
        AssertThat(W.WadeRing(world).Any(p => Distance(stream, p) < 2f)).OverrideFailureMessage("the wading polygon reaches into the stream").IsTrue();
        // P11's spot is on the deck.
        AssertThat(MathF.Abs(path.Project(W.P11).Lateral) < W.Width * 0.5f - 0.3f).OverrideFailureMessage("P11 stands on the deck").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void RailingsMeetTheWatersideRuleAndOpenOnlyAtTheBeach()
    {
        var world = World();
        var ctx = Context(world);
        var colliders = Colliders(ctx, new BoardwalkSite());
        var y = BoardwalkSite.DeckTop(world, world.Terrain.HeightAt);
        AssertThat(MathF.Abs(BoardwalkSite.DeckTop(world, (_, _) => 0f) - (W.PavingTop + W.Lip)) < TestTolerance.Position).OverrideFailureMessage("on flat ground the deck sits a lip above the paving").IsTrue();
        var rails = colliders.OfType<WallCollider>().Where(c => c.Tag == SurfaceTag.Barrier).ToArray();
        AssertThat(rails.Length > 0 && rails.All(r => r.Y1 - y >= WalkRules.BarrierHeight - TestTolerance.Position)).OverrideFailureMessage("every rail barrier tops out at 1.5 m over the deck").IsTrue();
        AssertThat(W.RailHeight >= new Campus.Contracts.Parts.RailingParams().Height).OverrideFailureMessage("the visible rail is at least the railing part's height").IsTrue();
        // One gap, on the water side, opposite the beach.
        var path = BoardwalkSite.Path(world);
        var (s, side) = BoardwalkSite.Beach(world);
        var runs = BoardwalkSite.RailRuns(world, side);
        AssertThat(runs.Length == 2 && MathF.Abs(runs[1].S0 - runs[0].S1 - W.BeachGap) < TestTolerance.Position && BoardwalkSite.RailRuns(world, -side).Length == 1).OverrideFailureMessage("only the water side opens, by the beach gap").IsTrue();
        var gap = path.At(s).P + Route.Right(path.At(s).T) * side * BoardwalkSite.RailLateral;
        AssertThat(rails.All(r => Distance([r.A, r.B], gap) > W.BeachGap * 0.5f - TestTolerance.Layout)).OverrideFailureMessage("no rail barrier crosses the beach gap").IsTrue();
        // The deck is 2 m slabs of timber; everything carries a foot material.
        var slabs = colliders.OfType<BoxCollider>().Where(b => b.Tag == SurfaceTag.Ground).ToArray();
        AssertThat(slabs.Length == (int)MathF.Ceiling(path.Length / W.SlabLength) && slabs.All(b => b.Foot == FootMat.WoodDeck)).OverrideFailureMessage("the deck is timber slabs every 2 m").IsTrue();
        AssertThat(colliders.All(c => c.Foot != FootMat.Auto)).OverrideFailureMessage("every boardwalk collider carries a foot material").IsTrue();
        AssertThat(colliders.OfType<RampCollider>().Any() || colliders.OfType<BoxCollider>().Any(b => b.Tag == SurfaceTag.Stair)).OverrideFailureMessage("steps lead down to the beach").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void LampsLightThePlanksFromTheDrySideOnly()
    {
        var world = World();
        var ctx = Context(world);
        var module = new WestModule();
        var lights = ZoneTestCells.Items(module, ctx, LodLevel.L0).Where(i => i.Light is not null).Select(i => i.Light!).ToArray();
        AssertThat(module.DescribeLights(ctx).OrderBy(light => light.ToString(), StringComparer.Ordinal)
            .SequenceEqual(lights.OrderBy(light => light.ToString(), StringComparer.Ordinal))).OverrideFailureMessage("light metadata matches every site's L0 output").IsTrue();
        var lamps = lights.Where(l => l.Channel.Value == "lamp.landscape").ToArray();
        AssertThat(lamps.Length == BoardwalkSite.LampStations(world).Length).OverrideFailureMessage($"one landscape marker per boardwalk lamp, got {lamps.Length}").IsTrue();
        var path = BoardwalkSite.Path(world);
        var (_, waterSide) = BoardwalkSite.Beach(world);
        foreach (var l in lamps)
        {
            var (_, lateral) = path.Project(new(l.Position.X, l.Position.Z));
            AssertThat(MathF.Sign(lateral) == -waterSide && MathF.Abs(lateral) < W.Width * 0.5f).OverrideFailureMessage("a lamp stands on the deck's dry edge").IsTrue();
            AssertThat(l.Position.Y < BoardwalkSite.DeckTop(world, world.Terrain.HeightAt) + W.LampHeight + 0.08f && l.Range <= 4f).OverrideFailureMessage("the lamp is low with a short pool").IsTrue();
        }
        // The emitters reach the content output of their cells.
        var emitted = module.Tiles(ctx).Sum(t => module.Build(ctx, t, LodLevel.L0).Emitters?.Count ?? 0);
        AssertThat(emitted == lights.Length).OverrideFailureMessage($"every marker is emitted once, {emitted} of {lights.Length}").IsTrue();
        AssertThat(lights.Count(l => l.Channel.Value == "lamp.street") == H.Posts - 1).OverrideFailureMessage("the shelter has one downlight marker per bay").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void LeveeStepsFollowTheTerrainAndVanishOnFlatGround()
    {
        var world = World();
        var line = world.TerrainLine(WestIds.Levee);
        float LeveeHeight(float x, float z)
        {
            var distance = MathF.Sqrt(PlanLines.PolylineDistanceSquared(line, new(x, z)));
            var crest = T.CrestHalf(world);
            var rise = T.LeveeRise(world);
            var run = rise * T.SideRun(world);
            return distance <= crest ? rise : distance >= crest + run ? 0f : rise * (1f - (distance - crest) / run);
        }
        foreach (var j in T.Junctions(world)) AssertThat(LeveeStepsSite.Flight(world, j, (_, _) => 0f) is null).OverrideFailureMessage($"{j.Key}: no flight on flat ground").IsTrue();
        foreach (var j in T.Junctions(world))
        {
            var f = LeveeStepsSite.Flight(world, j, LeveeHeight) ?? throw new InvalidOperationException($"{j.Key}: a flight on the levee");
            var riser = f.Flight.Height / f.Flight.Steps;
            AssertThat(MathF.Abs(f.Flight.Height - T.LeveeRise(world)) < 2 * TestTolerance.Layout && MathF.Abs(riser - T.Riser) <= TestTolerance.Layout).OverrideFailureMessage($"{j.Key}: rise {f.Flight.Height:0.00} in {f.Flight.Steps} risers").IsTrue();
            var tread = f.Flight.Depth / f.Flight.Steps;
            AssertThat(tread is >= 0.28f and <= 0.55f).OverrideFailureMessage($"{j.Key}: tread {tread:0.00}").IsTrue();
            // Its top tread lands on the crest, its first riser at the slope foot, along the path.
            var topEdge = f.Frame.P(0, f.Flight.Depth);
            AssertThat(MathF.Abs(LeveeHeight(topEdge.X, topEdge.Y) - (f.Foot + f.Flight.Height)) < 6 * TestTolerance.Position).OverrideFailureMessage($"{j.Key}: the top tread meets the levee top").IsTrue();
            AssertThat(Vector2.Dot(f.Frame.W, Vector2.Normalize(j.Top - j.Toward)) > 0.99f).OverrideFailureMessage($"{j.Key}: the flight climbs toward the levee").IsTrue();
        }
        var fixture = new FakeWorld(world) { Terrain = new FormulaTerrain(LeveeHeight) };
        var colliders = Colliders(Context(fixture), new LeveeStepsSite());
        AssertThat(colliders.OfType<RampCollider>().Count(r => r.Tag == SurfaceTag.Stair && r.Stair is not null) == T.Junctions(world).Length).OverrideFailureMessage("each flight walks as one stair ramp").IsTrue();
        AssertThat(colliders.All(c => c.Foot == FootMat.StonePaving)).OverrideFailureMessage("stone underfoot").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void ShelterStandsOnItsFootprintAndOpensToTheRoad()
    {
        var world = World();
        AssertThat(world.Features.ContainsKey(new(H.Entity))).OverrideFailureMessage("the shelter footprint is in the world model").IsTrue();
        var f = ShelterSite.Frame(world);
        var length = ShelterSite.Length(world);
        var service = world.Network.Roads.Single(r => r.Id.Value == WestIds.Way217855160.Source).Line.Points;
        AssertThat(Distance(service, f.P(length * 0.5f, H.Depth + H.RoofFront)) < Distance(service, f.P(length * 0.5f, 0))).OverrideFailureMessage("the open side faces the service road").IsTrue();
        var front = f.P(length * 0.5f, H.Depth + H.RoofFront);
        AssertThat(Distance(service, front) > 2.5f).OverrideFailureMessage($"the front eave clears the carriageway, {Distance(service, front):0.0} m to its centreline").IsTrue();
        var colliders = Colliders(Context(world), new ShelterSite());
        AssertThat(colliders.OfType<PostCollider>().Count() == H.Posts && colliders.All(c => c.Foot != FootMat.Auto)).OverrideFailureMessage("posts as cylinders, every collider with a foot").IsTrue();
        AssertThat(H.Clear >= 2.6f).OverrideFailureMessage("the roof clears a standing visitor").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void CarvingLiesInTheGreenWedgeAwayFromTheBridgeStone()
    {
        var world = World();
        var at = CarvingSite.At(world);
        foreach (var id in new[] { WestIds.Way544161580.Source, Campus.Gen.Maps.Qingshuihe.Boundary.BoundaryFeatureIds.WestGateRoad.Source })
        {
            var road = world.Network.Roads.Single(r => r.Id.Value == id);
            var d = Distance(road.Line.Points, at) - road.Width * 0.5f;
            AssertThat(d > C.Length * 0.5f + 0.5f).OverrideFailureMessage($"the carving stays off {id}, {d:0.0} m from its edge").IsTrue();
        }
        AssertThat(world.Features.Values.Where(feature => feature.Kind.Value == Campus.Gen.Maps.Qingshuihe.Waterfront.WaterfrontTune.StoneKind)
            .All(feature => Vector2.Distance(at, feature.Point()) > 10f)).OverrideFailureMessage("it keeps clear of game/gen/maps/qingshuihe/waterfront/'s bridge-head stone").IsTrue();
        var box = Colliders(Context(world), new CarvingSite()).OfType<BoxCollider>().Single();
        AssertThat(box.Half.Y * 2f >= C.Height - TestTolerance.Position && box.Foot == FootMat.StonePaving).OverrideFailureMessage("one box with a top").IsTrue();
    }

    // ---- Fixtures ----

    private static List<Collider> Colliders(BuildContext ctx, ZoneSite site)
    {
        var module = new WestModule([site]);
        return [.. module.Tiles(ctx).SelectMany(t => module.Collide(ctx, t))];
    }

    private static Vector2[] Line(IWorldModel world, string id, bool waterway = false, bool optional = false)
    {
        var lines = waterway ? world.Network.Waterways : world.Network.Roads;
        var found = lines.FirstOrDefault(r => r.Id.Value == id);
        if (found is null) return optional ? [] : throw new InvalidOperationException($"{id} is missing from the network");
        return [.. found.Line.Points];
    }

    private static float Distance(IReadOnlyList<Vector2> line, Vector2 point) =>
        MathF.Sqrt(PlanLines.PolylineDistanceSquared(line, point));

    private static BuildContext Context(IWorldModel world) => new() { CodeHash = "fixture", WindowNumbering = new(new MapId("fixture"), new string('0', 64), []), GlobalInputs = TextInputs(), Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog, World = world, Map = null!, WorldSeed = 1 };

    private static BuildInputSnapshot TextInputs() => BuildInputSnapshot.Freeze(new Dictionary<string, ReadOnlyMemory<byte>>
    {
        [SceneTextInputs.AtlasKey] = File.ReadAllBytes(RepositoryFixture.FilePath("game", "data", "scene-text", "atlas.json"))
    }, "fixture");

    private static IWorldModel World() => RealMapFixture.Load().World;

}
