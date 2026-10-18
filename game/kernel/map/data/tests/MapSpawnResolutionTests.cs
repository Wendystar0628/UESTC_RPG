using Campus.Gen.World;
using static GdUnit4.Assertions;
using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Kernel.Boot;
using Campus.Kernel.Maps;
using GdUnit4;

namespace Campus.Tests;

[TestSuite]
public sealed class MapSpawnResolutionTests
{
    [TestCase]
    public void LegacyImplementationExplicitlyRejectsCatalogueLookup()
    {
        IMapService legacy = new TestMapService(); // Implements no ResolveSpawn: the default method must fail explicitly.
        AssertThrown(() => legacy.ResolveSpawn(new("spawn-test"))).IsInstanceOf<NotSupportedException>();
        AssertThrown(() => legacy.ResolveSpawn(new("spawn-test"), "P00")).IsInstanceOf<NotSupportedException>();
    }

    [TestCase]
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
    public void ReviewCaptureSharesDefaultOverviewAndNamedWaypointResolution()
    {
        var entry = Entry([]);
        var inputs = BuildInputSnapshot.Freeze(new Dictionary<string, ReadOnlyMemory<byte>>
        {
            ["waypoints.json"] = JsonSerializer.SerializeToUtf8Bytes(new WaypointFile(1, entry.Waypoints), ContractJson.Options),
            ["review.json"] = System.Text.Encoding.UTF8.GetBytes("{\"version\":1,\"poses\":[{\"id\":\"V1\",\"waypoint\":\"P00\"}]}")
        });
        var context = new MapContext(entry.Descriptor, new TestWorldModel(), entry.DataRoot, inputs);
        AssertThat(ReviewPoseResolver.Resolve("V1", context) == entry.Waypoints[0].Fly).OverrideFailureMessage("The V1 overview did not follow the authored P00 relation.").IsTrue();
        AssertThat(ReviewPoseResolver.Resolve("P01", context) == entry.Waypoints[1].Fly).OverrideFailureMessage("Capture did not resolve the named waypoint in the frozen input version.").IsTrue();
        AssertThrown(() => ReviewPoseResolver.Resolve("missing", context)).IsInstanceOf<ArgumentException>();
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
            new("sky.json", "backdrop.json", []), new Dictionary<string, string>(), []);
        return new(file.Complete(new(103, 30, 32648, new(-32, -32), new(32, 32)), Vector2.Zero,
            new(new(-64, -28, -64), new(64, 2000, 64)), new(-1, -1), new(0, 0)),
            "res://data/maps/spawn-test/", [first, other]);
    }

}
