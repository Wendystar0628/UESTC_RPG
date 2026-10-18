using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Maps;
using Campus.Contracts.Parts;
using Campus.Gen.Tests.Support;
using Campus.Gen.World;
using Campus.Gen.Zones.West.Bridgehead;
using Campus.Gen.Zones.West.Framework;
using Campus.Gen.Zones.Common;
using Campus.Gen.Zones.West.Wetland;
using GdUnit4;
using C = Campus.Gen.Zones.West.WestTune.Carving;
using H = Campus.Gen.Zones.West.WestTune.Shelter;
using T = Campus.Gen.Zones.West.WestTune.LeveeSteps;
using W = Campus.Gen.Zones.West.WestTune.Wetland;

namespace Campus.Gen.Zones.West.Tests;
using static GdUnit4.Assertions;

/// <summary>19c: wetland boardwalk, wading beach, levee steps, bridgehead shelter and the 国防科技园 carving.</summary>
[TestSuite]
public sealed class West19cTests
{
    /// <summary>The wetland low ground's outer ring (west.terrain.json design:terrain:west-wetland-low).</summary>
    private static readonly Vector2[] LowGround =
        [new(-226, -366), new(-195, -372), new(-178, -300), new(-186, -205), new(-230, -145), new(-300, -142), new(-272, -225), new(-248, -300)];

    [TestCase]
    public void BoardwalkRunsThroughTheReedsClearOfTheBridgesAndTheStream()
    {
        var world = World();
        var path = BoardwalkSite.Path;
        AssertThat(path.Length is > 200f and < 260f).OverrideFailureMessage($"the boardwalk is about 230 m long, got {path.Length}").IsTrue();
        // Both mouths meet paving: the A10 plaza in the south, the footway at the 西门车行桥's east head in the north.
        var plaza = ((PolygonGeometry)world.Features[new("design:west-a10-plaza")].Geometry).Rings.Outer;
        AssertThat(PlanGeometry.InsideByCrossings(plaza, path.Points[0]) || Distance([.. plaza, plaza[0]], path.Points[0]) < 1.5f).OverrideFailureMessage("the south mouth lands on the A10 plaza").IsTrue();
        var footway = Line(world, "qsh:source:way:552816780");
        AssertThat(footway.Any(p => Vector2.Distance(p, path.Points[^1]) < 1f)).OverrideFailureMessage("the north mouth lands on the bridgehead footway's end").IsTrue();
        // Inside the low ground (except the last few metres climbing out to the north footway).
        for (var s = 4f; s < path.Length - 8f; s += 2f)
            AssertThat(PlanGeometry.InsideByCrossings(LowGround, path.At(s).P)).OverrideFailureMessage($"boardwalk station {s:0} lies in the wetland low ground").IsTrue();
        // Clear of WP-28's bridges (r005: all bridges are WP-28's) and of the stream except at the beach turn.
        foreach (var id in new[] { "qsh:source:way:692240860", "qsh:source:way:552816781" })
        {
            var bridge = world.Network.Bridges.Single(b => b.Id.Value == id).Line.Points;
            var near = path.Points.Min(p => Distance(bridge, p));
            AssertThat(near > 5f).OverrideFailureMessage($"the boardwalk keeps clear of bridge {id}, nearest {near:0.0} m").IsTrue();
        }
        var stream = Line(world, "qsh:source:way:217855313", waterway: true);
        var beach = BoardwalkSite.Beach;
        for (var s = 0f; s < path.Length; s += 1f)
        {
            var d = Distance(stream, path.At(s).P);
            var limit = MathF.Abs(s - beach.S) < 15f ? 3.2f : 6f;
            AssertThat(d > limit).OverrideFailureMessage($"station {s:0}: deck centre {d:0.0} m from the stream (limit {limit})").IsTrue();
        }
        // The beach lies between the deck and the stream, on the side the rail opens.
        var (bs, lateral) = path.Project(W.BeachCentre);
        AssertThat(MathF.Abs(bs - beach.S) < 0.01f && MathF.Abs(lateral) > W.Width * 0.5f).OverrideFailureMessage("the beach lies beside the deck").IsTrue();
        AssertThat(Distance(stream, W.BeachCentre) < Distance(stream, path.At(bs).P)).OverrideFailureMessage("the beach is on the stream side").IsTrue();
        AssertThat(W.WadeRing.Any(p => Distance(stream, p) < 2f)).OverrideFailureMessage("the wading polygon reaches into the stream").IsTrue();
        // P11's spot is on the deck.
        AssertThat(MathF.Abs(path.Project(W.P11).Lateral) < W.Width * 0.5f - 0.3f).OverrideFailureMessage("P11 stands on the deck").IsTrue();
    }

    [TestCase]
    public void RailingsMeetTheWatersideRuleAndOpenOnlyAtTheBeach()
    {
        var ctx = Context(World());
        var colliders = Colliders(ctx, new BoardwalkSite());
        var y = BoardwalkSite.DeckTop((_, _) => 0f);
        AssertThat(MathF.Abs(y - (W.PavingTop + W.Lip)) < 0.01f).OverrideFailureMessage("on flat ground the deck sits a lip above the paving").IsTrue();
        var rails = colliders.OfType<WallCollider>().Where(c => c.Tag == SurfaceTag.Barrier).ToArray();
        AssertThat(rails.Length > 40 && rails.All(r => r.Y1 - y >= WalkRules.BarrierHeight - 0.01f)).OverrideFailureMessage("every rail barrier tops out at 1.5 m over the deck").IsTrue();
        AssertThat(W.RailHeight >= 1.05f).OverrideFailureMessage("the visible rail is at least 1.05 m").IsTrue();
        // One gap, on the water side, opposite the beach.
        var path = BoardwalkSite.Path;
        var (s, side) = BoardwalkSite.Beach;
        var runs = BoardwalkSite.RailRuns(side);
        AssertThat(runs.Length == 2 && MathF.Abs(runs[1].S0 - runs[0].S1 - W.BeachGap) < 0.01f && BoardwalkSite.RailRuns(-side).Length == 1).OverrideFailureMessage("only the water side opens, by the beach gap").IsTrue();
        var gap = path.At(s).P + Route.Right(path.At(s).T) * side * BoardwalkSite.RailLateral;
        AssertThat(rails.All(r => Distance([r.A, r.B], gap) > W.BeachGap * 0.5f - 0.05f)).OverrideFailureMessage("no rail barrier crosses the beach gap").IsTrue();
        // The deck is 2 m slabs of timber; everything carries a foot material.
        var slabs = colliders.OfType<BoxCollider>().Where(b => b.Tag == SurfaceTag.Ground).ToArray();
        AssertThat(slabs.Length == (int)MathF.Ceiling(path.Length / W.SlabLength) && slabs.All(b => b.Foot == FootMat.WoodDeck)).OverrideFailureMessage("the deck is timber slabs every 2 m").IsTrue();
        AssertThat(colliders.All(c => c.Foot != FootMat.Auto)).OverrideFailureMessage("every boardwalk collider carries a foot material").IsTrue();
        AssertThat(colliders.OfType<RampCollider>().Any() || colliders.OfType<BoxCollider>().Any(b => b.Tag == SurfaceTag.Stair)).OverrideFailureMessage("steps lead down to the beach").IsTrue();
    }

    [TestCase]
    public void LampsLightThePlanksFromTheDrySideOnly()
    {
        var ctx = Context(World());
        var module = new WestModule();
        var lights = module.ItemsFor(ctx, LodLevel.L0).Where(i => i.Light is not null).Select(i => i.Light!).ToArray();
        AssertThat(module.DescribeLights(ctx).SequenceEqual(lights)).OverrideFailureMessage("light metadata matches every site's L0 output").IsTrue();
        var lamps = lights.Where(l => l.Channel.Value == "lamp.landscape").ToArray();
        AssertThat(lamps.Length == BoardwalkSite.LampStations().Length && lamps.Length >= 12).OverrideFailureMessage($"one landscape marker per boardwalk lamp, got {lamps.Length}").IsTrue();
        var path = BoardwalkSite.Path;
        var (_, waterSide) = BoardwalkSite.Beach;
        foreach (var l in lamps)
        {
            var (_, lateral) = path.Project(new(l.Position.X, l.Position.Z));
            AssertThat(MathF.Sign(lateral) == -waterSide && MathF.Abs(lateral) < W.Width * 0.5f).OverrideFailureMessage("a lamp stands on the deck's dry edge").IsTrue();
            AssertThat(l.Position.Y < BoardwalkSite.DeckTop((_, _) => 0f) + 0.7f && l.Range <= 4f).OverrideFailureMessage("the lamp is low with a short pool").IsTrue();
        }
        // The emitters reach the content output of their cells.
        var emitted = module.Tiles(ctx).Sum(t => module.Build(ctx, t, LodLevel.L0).Emitters?.Count ?? 0);
        AssertThat(emitted == lights.Length).OverrideFailureMessage($"every marker is emitted once, {emitted} of {lights.Length}").IsTrue();
        AssertThat(lights.Count(l => l.Channel.Value == "lamp.street") == H.Posts - 1).OverrideFailureMessage("the shelter has one downlight marker per bay").IsTrue();
    }

    [TestCase]
    public void LeveeStepsFollowTheTerrainAndVanishOnFlatGround()
    {
        foreach (var j in T.Junctions) AssertThat(LeveeStepsSite.Flight(j, (_, _) => 0f) is null).OverrideFailureMessage($"{j.Key}: no flight on flat ground").IsTrue();
        foreach (var j in T.Junctions)
        {
            var f = LeveeStepsSite.Flight(j, LeveeHeight) ?? throw new InvalidOperationException($"{j.Key}: a flight on the levee");
            var riser = f.Flight.Height / f.Flight.Steps;
            AssertThat(MathF.Abs(f.Flight.Height - T.LeveeRise) < 0.1f && riser is >= 0.12f and <= 0.18f).OverrideFailureMessage($"{j.Key}: rise {f.Flight.Height:0.00} in {f.Flight.Steps} risers").IsTrue();
            var tread = f.Flight.Depth / f.Flight.Steps;
            AssertThat(tread is >= 0.28f and <= 0.55f).OverrideFailureMessage($"{j.Key}: tread {tread:0.00}").IsTrue();
            // Its top tread lands on the crest, its first riser at the slope foot, along the path.
            var topEdge = f.Frame.P(0, f.Flight.Depth);
            AssertThat(MathF.Abs(LeveeHeight(topEdge.X, topEdge.Y) - (f.Foot + f.Flight.Height)) < 0.12f).OverrideFailureMessage($"{j.Key}: the top tread meets the levee top").IsTrue();
            AssertThat(Vector2.Dot(f.Frame.W, Vector2.Normalize(j.Top - j.Toward)) > 0.99f).OverrideFailureMessage($"{j.Key}: the flight climbs toward the levee").IsTrue();
        }
        var world = new FakeWorld(World()) { Terrain = new FormulaTerrain(LeveeHeight) };
        var colliders = Colliders(Context(world), new LeveeStepsSite());
        AssertThat(colliders.OfType<RampCollider>().Count(r => r.Tag == SurfaceTag.Stair && r.Stair is not null) == 2).OverrideFailureMessage("each flight walks as one stair ramp").IsTrue();
        AssertThat(colliders.All(c => c.Foot == FootMat.StonePaving)).OverrideFailureMessage("stone underfoot").IsTrue();
    }

    [TestCase]
    public void ShelterStandsOnItsFootprintAndOpensToTheRoad()
    {
        var world = World();
        AssertThat(world.Features.ContainsKey(new(H.Entity))).OverrideFailureMessage("the shelter footprint is in the world model").IsTrue();
        var f = ShelterSite.Frame(world);
        var length = ShelterSite.Length(world);
        var service = world.Network.Roads.Single(r => r.Id.Value == "qsh:source:way:217855160").Line.Points;
        AssertThat(Distance(service, f.P(length * 0.5f, H.Depth + H.RoofFront)) < Distance(service, f.P(length * 0.5f, 0))).OverrideFailureMessage("the open side faces the service road").IsTrue();
        var front = f.P(length * 0.5f, H.Depth + H.RoofFront);
        AssertThat(Distance(service, front) > 2.5f).OverrideFailureMessage($"the front eave clears the carriageway, {Distance(service, front):0.0} m to its centreline").IsTrue();
        var colliders = Colliders(Context(world), new ShelterSite());
        AssertThat(colliders.OfType<PostCollider>().Count() == H.Posts && colliders.All(c => c.Foot != FootMat.Auto)).OverrideFailureMessage("posts as cylinders, every collider with a foot").IsTrue();
        AssertThat(H.Clear >= 2.6f).OverrideFailureMessage("the roof clears a standing visitor").IsTrue();
    }

    [TestCase]
    public void CarvingLiesInTheGreenWedgeAwayFromTheBridgeStone()
    {
        var world = World();
        var at = CarvingSite.At(world);
        foreach (var id in new[] { "qsh:source:way:544161580", "qsh:source:way:544161581" })
        {
            var road = world.Network.Roads.Single(r => r.Id.Value == id);
            var d = Distance(road.Line.Points, at) - road.Width * 0.5f;
            AssertThat(d > C.Length * 0.5f + 0.5f).OverrideFailureMessage($"the carving stays off {id}, {d:0.0} m from its edge").IsTrue();
        }
        AssertThat(Vector2.Distance(at, new(-238.37f, -411.91f)) > 10f).OverrideFailureMessage("it keeps clear of WP-28's bridge-head stone").IsTrue();
        var box = Colliders(Context(world), new CarvingSite()).OfType<BoxCollider>().Single();
        AssertThat(box.Half.Y * 2f >= C.Height - 0.01f && box.Foot == FootMat.StonePaving).OverrideFailureMessage("one box with a top").IsTrue();
    }

    // ---- Fixtures ----

    private static float LeveeHeight(float x, float z)
    {
        var d = Distance(LeveeLine, new(x, z));
        if (d <= T.CrestHalf) return T.LeveeRise;
        var run = T.LeveeRise * T.SideRun;
        return d >= T.CrestHalf + run ? 0f : T.LeveeRise * (1f - (d - T.CrestHalf) / run);
    }

    private static Vector2[] LeveeLine => LeveeStepsSite.Levee;

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

    private static float Distance(IReadOnlyList<Vector2> line, Vector2 p)
    {
        var best = float.MaxValue;
        for (var i = 0; i + 1 < line.Count; i++)
        {
            Vector2 a = line[i], b = line[i + 1];
            var t = Math.Clamp(Vector2.Dot(p - a, b - a) / Vector2.Dot(b - a, b - a), 0f, 1f);
            best = MathF.Min(best, Vector2.Distance(a + (b - a) * t, p));
        }
        return best;
    }

    private static BuildContext Context(IWorldModel world) => new() { Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog, World = world, Map = null!, WorldSeed = 1 };

    private static string DataDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "game", "data", "maps", "qingshuihe", "campus.json"))) directory = directory.Parent;
        if (directory is null) throw new InvalidOperationException("Repository campus fixture not found.");
        return Path.Combine(directory.FullName, "game", "data", "maps", "qingshuihe");
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

}
