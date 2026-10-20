using Campus.Gen.Tests.Support;
using Campus.Gen.World;
using static GdUnit4.Assertions;
using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Atlas;
using Campus.Kernel.Boot;
using Campus.Kernel.Maps;
using GdUnit4;

namespace Campus.Tests;

[TestSuite]
public sealed class MapSpawnResolutionTests
{

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void ExplicitSpawnWinsOverSameNamedWaypoint()
    {
        var authored = new MapSpawn("P00", new(new Vector3(12, 50, 18), 35, -20),
            new(new Vector3(11, 0, 17), 32));
        var entry = Entry([authored]);
        AssertThat(entry.ResolveSpawn("P00") == authored).OverrideFailureMessage("Explicit spawn lost priority over a same-named waypoint.").IsTrue();
        foreach (var unknown in new[] { "missing", "", "p00" })
            AssertThrown(() => entry.ResolveSpawn(unknown)).IsInstanceOf<ArgumentException>();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void ReviewCaptureSharesDefaultOverviewAndNamedWaypointResolution()
    {
        var entry = Entry([]);
        var inputs = BuildInputSnapshot.Freeze(new Dictionary<string, ReadOnlyMemory<byte>>
        {
            ["waypoints.json"] = JsonSerializer.SerializeToUtf8Bytes(new WaypointFile(1, entry.Waypoints), ContractJson.Options),
            ["review.json"] = System.Text.Encoding.UTF8.GetBytes("{\"version\":1,\"poses\":[{\"id\":\"V1\",\"waypoint\":\"P00\"}]}")
        }, "fixture");
        var context = new MapContext(entry.Descriptor, new FakeWorld { Boundary = new([]) }, entry.DataRoot, inputs);
        AssertThat(ReviewPoseResolver.Resolve("V1", context) == entry.Waypoints[0].Fly).OverrideFailureMessage("The V1 overview did not follow the authored P00 relation.").IsTrue();
        AssertThat(ReviewPoseResolver.Resolve("P01", context) == entry.Waypoints[1].Fly).OverrideFailureMessage("Capture did not resolve the named waypoint in the frozen input version.").IsTrue();
        AssertThrown(() => ReviewPoseResolver.Resolve("missing", context)).IsInstanceOf<ArgumentException>();
    }

    [TestCase, TestCategory(TestReason.Regression)]
    public void EveryAuthoredDestinationIsRejectedBeforeInteriorPublicationWhenItsPartIsClosed()
    {
        var entry = Entry([]);
        var feature = new FeatureId("fixture:split-building");
        var ring = new PolygonRings([new(100, 100), new(120, 100), new(120, 120), new(100, 120)]);
        var allowed = new StoreyInfo(1, 0, 20, [new(100, 100), new(110, 100), new(110, 120), new(100, 120)]);
        var interior = new InteriorIndexEntry(feature, [new(1, 0, 20, ring.Outer)], [], true, "fixture.provider", [1], [allowed]);
        var world = new FakeWorld { Boundary = ring, Features = new Dictionary<FeatureId, WorldFeature>
            { [feature] = new(feature, new("building"), new PolygonGeometry(ring)) }, Interiors = new FakeInteriors(interior) };
        var closedPose = new Pose(new(115, 2, 110), 0, 0);
        var goodPose = closedPose with { Position = new(105, 2, 110) };
        var cases = new List<(MapDescriptor Map, Dictionary<string, ReadOnlyMemory<byte>> Files)>
        {
            (entry.Descriptor with { Spawns = [new("bad-spawn", closedPose, null)] }, []),
            (entry.Descriptor, new() { ["waypoints.json"] = JsonSerializer.SerializeToUtf8Bytes(
                new WaypointFile(1, [entry.Waypoints[0] with { Fly = closedPose }]), ContractJson.Options) }),
            (entry.Descriptor, new() { ["waypoints.json"] = JsonSerializer.SerializeToUtf8Bytes(
                new WaypointFile(1, [entry.Waypoints[0] with { Stand = new(closedPose.Position, 0) }]), ContractJson.Options) }),
            (entry.Descriptor, new() { ["review.json"] = JsonSerializer.SerializeToUtf8Bytes(
                new { version = 1, poses = new[] { new { id = "unused-review", pose = closedPose } } }, ContractJson.Options) }),
            (entry.Descriptor, new() { ["atlas/markers.json"] = JsonSerializer.SerializeToUtf8Bytes(
                new { markers = new[] { new { id = "bad-anchor", teleport = new TeleportSpec(closedPose, null) } } }, ContractJson.Options) })
        };
        foreach (var item in cases)
        {
            var context = new MapContext(item.Map, world, entry.DataRoot, BuildInputSnapshot.Freeze(item.Files, "fixture"));
            AssertThrown(() => context.PublishInteriors([interior])).IsInstanceOf<WorldDataException>();
            AssertThat(ReferenceEquals(context.World, world)).OverrideFailureMessage("A bad destination published a partly prepared map.").IsTrue();
        }
        var valid = entry.Descriptor with { Spawns = [new("valid-spawn", goodPose, null)] };
        var completed = new MapContext(valid, world, entry.DataRoot, BuildInputSnapshot.Empty);
        completed.PublishInteriors([interior]);
        AssertThat(completed.World.Interiors.IsOpen(feature, 1, goodPose.Position)).IsTrue();
    }

    private static MapCatalogEntry Entry(IReadOnlyList<MapSpawn> spawns)
    {
        var first = new Waypoint("P00", new("Default", "Default"), WaypointGroup.Overview,
            new(new Vector3(0, 30, 40), 10, -15), new(new Vector3(2, 0, 3), 25), null,
            new(17.5, Season.Autumn));
        var other = first with { Id = "P01", Fly = new(new Vector3(-20, 25, 10), 80, -10), Stand = null };
        var file = new MapFile(new("spawn-test"), new("Test", "Test"), MapKind.Outdoor, 1, 1,
            new(new(-64, -64), new(64, 64)), new(.9f, 30, 3000, -28, 2000, 30, 40, 150), "P00", spawns,
            new(MapTimeRule.Shared, new(17.5, Season.Autumn), new("Default", "Default"), true, "test"),
            new("sky.json", "backdrop.json", []), []);
        return new(file.Complete(new(103, 30, 32648, new(-32, -32), new(32, 32)), Vector2.Zero,
            new(new(-64, -28, -64), new(64, 2000, 64)), new(-1, -1), new(0, 0)),
            "res://data/maps/spawn-test/", [first, other]);
    }

}
