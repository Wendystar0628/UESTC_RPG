using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Kernel.Stubs;
using Campus.Kernel.Stubs.Foundation;
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
    public void LookupRequiresInitializationAndRejectsUnknownIdsWithoutSideEffects()
    {
        var unloaded = new MapServiceStub(CreateCamera());
        Throws<InvalidOperationException>(() => unloaded.ResolveSpawn(new("spawn-test")));

        var fixture = CreateFixture();
        var before = (fixture.Camera.Pose, fixture.Service.Current, fixture.Service.Context,
            fixture.Service.Rev, fixture.Service.Visits, fixture.Service.Transition);
        Throws<ArgumentException>(() => fixture.Service.ResolveSpawn(new("other-map")));
        Throws<ArgumentException>(() => fixture.Service.ResolveSpawn(new("other-map"), "P00"));
        Throws<ArgumentException>(() => fixture.Service.ResolveSpawn(fixture.Map, "missing"));
        Throws<ArgumentException>(() => fixture.Service.ResolveSpawn(fixture.Map, ""));
        Throws<ArgumentException>(() => fixture.Service.ResolveSpawn(fixture.Map, "p00"));
        Check((fixture.Camera.Pose, fixture.Service.Current, fixture.Service.Context,
            fixture.Service.Rev, fixture.Service.Visits, fixture.Service.Transition) == before,
            "Rejected lookups changed camera, map, visits or transition state.");
    }

    [TestCase]
    public void EmptySpawnsResolveDefaultAndNamedWaypointsIncludingStand()
    {
        var fixture = CreateFixture();
        Check(fixture.Service.Maps.Single().Spawns.Count == 0, "Fixture must have no explicit spawns.");
        var before = fixture.Camera.Pose;
        IMapService service = fixture.Service;
        var resolved = service.ResolveSpawn(fixture.Map);
        Check(resolved == new MapSpawn("P00", fixture.First.Fly, fixture.First.Stand),
            "DefaultSpawn must resolve a waypoint and preserve its standing destination.");
        var alternate = service.ResolveSpawn(fixture.Map, "P01");
        Check(alternate == new MapSpawn("P01", fixture.Other.Fly, null),
            "An explicit waypoint must override DefaultSpawn and preserve fly-only status.");
        Check(fixture.Camera.Pose == before && fixture.Service.Rev == 0 && fixture.Service.Transition is null,
            "Resolving a different destination must not move the camera or begin Travel.");
    }

    [TestCase]
    public void ExplicitMapSpawnWinsOverSameNamedWaypoint()
    {
        var authored = new MapSpawn("P00", new(new Vector3(12, 50, 18), 35, -20),
            new(new Vector3(11, 0, 17), 32));
        var fixture = CreateFixture([authored]);
        Check(fixture.First.Fly != authored.Fly && fixture.First.Stand != authored.Stand,
            "Fixture must distinguish the explicit spawn from its same-named waypoint.");
        Check(fixture.Service.ResolveSpawn(fixture.Map) == authored &&
            fixture.Service.ResolveSpawn(fixture.Map, "P00") == authored,
            "Named spawns must take priority over waypoints for both default and explicit lookup.");
        Check(fixture.Service.ResolvePose(fixture.Service.Maps.Single(), "P00") == authored.Fly,
            "The existing D0 pose helper must share catalogue resolution.");
    }

    [TestCase]
    public void LookupRemainsAvailableWhileMountedContextIsAbsent()
    {
        Fixture? fixture = null;
        var observed = false;
        var runtime = new ObservingRuntime(() =>
        {
            var current = fixture ?? throw new InvalidOperationException("Fixture was not initialized.");
            Check(current.Service.Context is null, "Probe must run between Unload and Mount.");
            Check(current.Service.ResolveSpawn(current.Map, "P01").Fly == current.Other.Fly,
                "Spawn lookup incorrectly depends on the currently mounted context.");
            observed = true;
        });
        fixture = CreateFixture(runtime: runtime);
        var transition = fixture.Service.Travel(new(fixture.Map, Cause: MapTravelCause.Test));
        Check(observed && transition.Done.IsCompletedSuccessfully && transition.Done.Result == MapTravelResult.Arrived,
            "The synchronous D0 runtime did not complete the unload lookup probe.");
    }

    private static Fixture CreateFixture(IReadOnlyList<MapSpawn>? spawns = null, IMapRuntime? runtime = null)
    {
        var map = new MapId("spawn-test");
        var first = new Waypoint("P00", new("Default", "Default"), WaypointGroup.Overview,
            new(new Vector3(0, 30, 40), 10, -15), new(new Vector3(2, 0, 3), 25), null,
            new(17.5, Season.Autumn));
        var other = first with { Id = "P01", Fly = new(new Vector3(-20, 25, 10), 80, -10), Stand = null };
        var file = new MapFile(map, new("Test", "Test"), MapKind.Outdoor, 1, 1,
            new(new(-64, -64), new(64, 64)), new(.9f, 30, 3000, -28, 2000, 30, 40, 150),
            "P00", spawns ?? [],
            new(MapTimeRule.Shared, new(17.5, Season.Autumn), new("Default", "Default"), true, "test"),
            new("sky.json", "backdrop.json", []), new Dictionary<string, string>(), []);
        var index = new MapIndex(1, map, [new(map, "res://data/maps/spawn-test/")]);
        var camera = CreateCamera();
        var service = new MapServiceStub(camera, runtime);
        var world = new StubWorldModel(new StubSpatialIndex(), new InteriorIndexStub(), new StubFidelityModel());
        var inputs = BuildInputSnapshot.Freeze(new Dictionary<string, ReadOnlyMemory<byte>>(StringComparer.Ordinal)
        { ["map.json"] = JsonSerializer.SerializeToUtf8Bytes(file, ContractJson.Options) });
        service.LoadFromBytes(JsonSerializer.SerializeToUtf8Bytes(index, ContractJson.Options), inputs, world,
            new(103, 30, 32648, new(-32, -32), new(32, 32)), Vector2.Zero,
            new(new(-64, -28, -64), new(64, 2000, 64)), new(-1, -1), new(0, 0),
            "res://bake/spawn-test/boot/", [first, other]);
        Check(ReferenceEquals(service.Context?.SourceInputs, inputs), "D0 published another input version.");
        return new(map, service, camera, first, other);
    }

    private static CameraRigStub CreateCamera() => new(new WorldStateStub(new WeatherProcessStub(), new TestRuleset()));
    private sealed record Fixture(MapId Map, MapServiceStub Service, CameraRigStub Camera, Waypoint First, Waypoint Other);

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

    private sealed class ObservingRuntime(Action onUnload) : IMapRuntime
    {
        private readonly MapRuntimeStub _inner = new();
        public bool IsUnloaded => _inner.IsUnloaded;
        public bool ResidentReady => _inner.ResidentReady;
        public bool DestinationReady => _inner.DestinationReady;
        public bool WarmReady => _inner.WarmReady;
        public string? Error => _inner.Error;
        public void Unload(MapId from) { onUnload(); _inner.Unload(from); }
        public void Mount(IMapContext context) => _inner.Mount(context);
        public void Prefetch(Pose destination, ViewMode mode, NavSpace? space = null) => _inner.Prefetch(destination, mode, space);
        public void BeginWarm() => _inner.BeginWarm();
    }

    private sealed class TestRuleset : IRuleset
    {
        public RulesetDescriptor Current { get; } = new(new("spawn-test"), [ViewMode.Fly, ViewMode.Walk],
            WorldControlMask.All, "all-unlocked", [], []);
        public long Rev => 1;
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Throws<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }
}
