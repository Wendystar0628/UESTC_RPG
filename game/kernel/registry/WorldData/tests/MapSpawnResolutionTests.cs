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
        IMapService legacy = new LegacyMapService();
        Throws<NotSupportedException>(() => legacy.ResolveSpawn(new("spawn-test")));
        Throws<NotSupportedException>(() => legacy.ResolveSpawn(new("spawn-test"), "P00"));
    }

    [TestCase]
    public void ExplicitSpawnWinsOverSameNamedWaypoint()
    {
        var authored = new MapSpawn("P00", new(new Vector3(12, 50, 18), 35, -20),
            new(new Vector3(11, 0, 17), 32));
        var entry = Entry([authored]);
        Check(entry.ResolveSpawn("P00") == authored, "Explicit spawn lost priority over a same-named waypoint.");
        foreach (var unknown in new[] { "missing", "", "p00" })
            Throws<ArgumentException>(() => entry.ResolveSpawn(unknown));
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
        var context = new MapContext(entry.Descriptor, new TestWorldModel(), entry.DataRoot, entry.BakeRoot, inputs);
        Check(ReviewPoseResolver.Resolve("V1", context) == entry.Waypoints[0].Fly,
            "The V1 overview did not follow the authored P00 relation.");
        Check(ReviewPoseResolver.Resolve("P01", context) == entry.Waypoints[1].Fly,
            "Capture did not resolve the named waypoint in the frozen input version.");
        Throws<ArgumentException>(() => ReviewPoseResolver.Resolve("missing", context));
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
            "res://data/maps/spawn-test/", "res://bake/spawn-test/boot/", [first, other]);
    }

    // Deliberately implements only the pre-extension interface: the default method must fail explicitly.
    private sealed class LegacyMapService : IMapService
    {
        public long Rev => 0;
        public MapId Default => default;
        public IReadOnlyList<MapDescriptor> Maps => [];
        public IReadOnlyList<MapVisit> Visits => [];
        public MapId Current => default;
        public IMapContext? Context => null;
        public IMapTransition? Transition => null;
        public event Action<MapId, MapTravelCause>? Arrived { add { } remove { } }
        public IMapTransition Travel(MapTravelRequest request) => throw new NotSupportedException();
    }

    private static void Check(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
    private static void Throws<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }
}
