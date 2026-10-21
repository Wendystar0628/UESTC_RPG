using Campus.Gen.World;
using Campus.Gen.Tests.Support;
using static GdUnit4.Assertions;
using Campus.Gen.BakeInput;
using System.Numerics;
using System.Text;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Save;
using Campus.Kernel.Maps;
using Campus.Kernel.Registry;
using GdUnit4;

namespace Campus.Tests;

[TestSuite]
public sealed class MapServiceTests
{
    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void ResolveDefaultWaypointWithoutAnActiveMap()
    {
        var f = new Fixture(waypointDefault: true);
        var spawn = f.Service.ResolveSpawn(f.B);
        AssertThat(spawn.Id == "P00" && spawn.Fly == new Pose(new(37, 19, -4), 15, -12, 60) &&
            spawn.Stand == new WaypointStand(new(37, 0, -4), 15)).OverrideFailureMessage("DefaultSpawn backed by a waypoint was not resolved with its fly and stand poses.").IsTrue();
        AssertThat(f.Service.Context is null && f.Service.Current == default && f.Service.Rev == 0 &&
            f.Loader.Loads == 0 && f.Runtime.Arrivals == 0).OverrideFailureMessage("Spawn lookup unexpectedly loaded or travelled to a map.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void ResolveExtraSpawnUsesTheRequestedMap()
    {
        var f = new Fixture(); f.Boot();
        var context = f.Service.Context;
        var extra = f.Service.ResolveSpawn(f.C, "extra");
        AssertThat(extra.Fly == new Pose(new(15, 25, 6), 40, -5) && extra.Stand is null).OverrideFailureMessage("Extra spawn did not come from the explicitly requested map.").IsTrue();
        AssertThat(ReferenceEquals(context, f.Service.Context) && f.Service.Current == f.A && f.Service.Rev == 1 && f.Loader.Loads == 1).OverrideFailureMessage("Resolving another map's extra spawn changed the active map.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void ResolveSpawnRejectsUnknownMapAndUnknownSpawn()
    {
        var f = new Fixture(waypointDefault: true);
        foreach (var query in new (MapId Map, string? Spawn)[]
        { (new("missing-map"), null), (f.A, "missing-spawn"), (f.A, "extra"), (f.B, "") })
        {
            try { f.Service.ResolveSpawn(query.Map, query.Spawn); }
            catch (ArgumentException) { continue; }
            throw new InvalidOperationException("Unknown map/spawn was silently replaced by a default.");
        }
        AssertThat(f.Loader.Loads == 0 && f.Service.Transition is null).OverrideFailureMessage("Rejected spawn query started work.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void ResolveSpawnRequiresCatalogueInitialization()
    {
        var f = new Fixture(initialize: false);
        try { f.Service.ResolveSpawn(f.A); }
        catch (InvalidOperationException)
        {
            var initialized = f.Service.Initialize();
            f.Until(() => initialized.IsCompleted);
            initialized.GetAwaiter().GetResult();
            AssertThat(f.Service.ResolveSpawn(f.A).Id == "start").OverrideFailureMessage("Initialized catalogue did not resolve its default.").IsTrue();
            return;
        }
        throw new InvalidOperationException("Spawn lookup before catalogue initialization was accepted.");
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void MapsSaveUsesStringIdsAndVectorArraysAndRejectsUnknownFields()
    {
        var f = new Fixture(); f.Boot();
        var context = new CaptureContext(SaveReason.Test, f.A, false) { Moment = new WorldSaveMoment(0, 0, Season.Autumn, 0) };
        var dto = f.Service.Capture(in context);
        var json = JsonSerializer.Serialize(dto, f.Service.TypeInfo);
        AssertThat(json.Contains("\"current\":\"map-a\"", StringComparison.Ordinal) && json.Contains("\"position\":[", StringComparison.Ordinal)).OverrideFailureMessage("Map save lost contract ID/vector encoding.").IsTrue();
        var restored = JsonSerializer.Deserialize(json, f.Service.TypeInfo)!;
        AssertThat(restored.Current == dto.Current && restored.Visits.Single() == dto.Visits.Single()).OverrideFailureMessage("Map save round trip changed values.").IsTrue();
        try { JsonSerializer.Deserialize(json[..^1] + ",\"unknown\":1}", f.Service.TypeInfo); }
        catch (JsonException) { return; }
        throw new InvalidOperationException("Unknown map save field was accepted.");
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void RestoreDropsDeletedMapVisitsAndPreservesKnownVisitsInMapOrder()
    {
        var f = new Fixture();
        var current = new MapVisit(f.A, new(new(12, 20, -3), 45, -10), ViewMode.Fly);
        var known = new MapVisit(f.C, new(new(-9, 30, 4), 80, -15), ViewMode.Walk);
        var removed = new MapVisit(new("deleted-map"), new(new(2, 20, 8), 0, 0), ViewMode.Fly);
        var restore = new RestoreContext { Map = f.A, FromVersion = 1, Missing = false };
        f.Service.Restore(new(f.A, [known, removed, current]), restore);
        AssertThat(f.Service.Visits.SequenceEqual(new[] { current, known })).OverrideFailureMessage("Deleted map visits were retained or known visits changed their values or ordering.").IsTrue();
        AssertThat(restore.Repairs.Count == 1 && restore.Repairs[0].Contains(removed.Map.Value, StringComparison.Ordinal)).OverrideFailureMessage("Dropping a deleted map visit did not report its repair.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void CatalogRejectsTraversalAndDerivesCentroidFromBoundary()
    {
        var f = new Fixture();
        try { MapCatalog.ReadIndex(Encoding.UTF8.GetBytes("{\"version\":1,\"defaultMap\":\"map-a\",\"maps\":[{\"id\":\"map-a\",\"dir\":\"res://data/../map-a/\"}]}")); }
        catch (InvalidDataException)
        {
            var map = f.Catalog.Maps[0];
            var file = new MapFile(map.Id, map.Name, map.Kind, map.Version, map.Seed, map.DataRect, map.Roam,
                map.DefaultSpawn, map.Spawns, map.World, map.Env, map.Portals);
            var entry = MapCatalog.ReadEntry(new(map.Id, "res://data/" + map.Id.Value + '/'),
                JsonSerializer.SerializeToUtf8Bytes(file, ContractJson.Options),
                Encoding.UTF8.GetBytes("""
                    {"origin":[104,31],"crs":"EPSG:32648","bounds":[0,0,10,10],
                     "boundary":{"outer":[[0,0],[10,0],[0,10]],"holes":[]}}
                    """), []);
            AssertThat(Vector2.Distance(entry.Descriptor.Center, new(10f / 3, 10f / 3)) < TestTolerance.Position).OverrideFailureMessage("Map center used bounding-box center instead of polygon centroid.").IsTrue();
            return;
        }
        throw new InvalidOperationException("Traversal map root was accepted.");
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void FoundationWaitsForMountWithoutSelectingAnEndpoint()
    {
        var f = new Fixture();
        f.Runtime.DelayMount();
        var pose = f.Camera.Pose;
        var preparing = f.Service.Prepare(f.A);
        f.Until(() => f.Runtime.MountPending);
        AssertThat(!preparing.IsCompleted && f.Runtime.Arrivals == 0 && f.Camera.Pose == pose && f.Service.Current == default).IsTrue();
        f.Runtime.ReleaseMount();
        f.Until(() => preparing.IsCompleted);
        preparing.GetAwaiter().GetResult();
        AssertThat(f.Service.Current == f.A && f.Runtime.Arrivals == 0 && f.Service.Rev == 1 && !f.Camera.InputLocked).IsTrue();
        var context = f.Service.Context;
        f.Service.Prepare(f.A).GetAwaiter().GetResult();
        AssertThat(ReferenceEquals(context, f.Service.Context) && f.Loader.Loads == 1 && f.Camera.Pose == pose).IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void FailedFoundationRetainsItsOriginalFailureWithoutArrival()
    {
        var f = new Fixture(); f.Loader.Failing.Add(f.A);
        var preparing = f.Service.Prepare(f.A);
        f.Until(() => preparing.IsCompleted);
        AssertThat(preparing.IsFaulted && preparing.Exception!.InnerException is InvalidDataException &&
            f.Runtime.Arrivals == 0 && f.Service.Rev == 0 && !f.Camera.InputLocked).IsTrue();
    }

    private sealed class Fixture
    {
        public MapId A { get; } = new("map-a");
        public MapId B { get; } = new("map-b");
        public MapId C { get; } = new("map-c");
        public MapCatalog Catalog { get; }
        public MapService Service { get; }
        public TestCameraRig Camera { get; }
        public TestRuntime Runtime { get; } = new();
        public TestLoader Loader { get; }
        public TestData Data { get; }
        public Fixture(bool initialize = true, bool waypointDefault = false)
        {
            var entries = new[] { A, B, C }.Select(MakeMap).ToArray();
            if (waypointDefault)
                entries[1] = entries[1] with
                {
                    Descriptor = entries[1].Descriptor with { DefaultSpawn = "P00", Spawns = [] },
                    Waypoints = [new("P00", new("总览", "Overview"), WaypointGroup.Overview,
                        new(new(37, 19, -4), 15, -12, 60), new(new(37, 0, -4), 15), null,
                        new(17.5, Season.Autumn, WeatherKind.Clear))]
                };
            entries[2] = entries[2] with
            {
                Descriptor = entries[2].Descriptor with
                { Spawns = [.. entries[2].Descriptor.Spawns, new("extra", new(new(15, 25, 6), 40, -5), null)] }
            };
            Catalog = new(new(1, A, entries.Select(e => new MapIndexEntry(e.Descriptor.Id, e.DataRoot)).ToArray()), entries);
            Data = new(Catalog); Loader = new(Catalog);
            var world = new TestWorldState(new TestWeatherProcess(), new TestRuleset());
            Camera = new(world);
            Service = new(Camera, Runtime, Loader, Data);
            if (initialize)
            {
                var initialized = Service.Initialize();
                Until(() => initialized.IsCompleted);
                initialized.GetAwaiter().GetResult();
            }
        }
        public void Boot() { var task = Service.Prepare(A); Until(() => task.IsCompleted); task.GetAwaiter().GetResult(); }
        public void Until(Func<bool> ready)
        {
            if (!SpinWait.SpinUntil(() => { Service.Step(); return ready(); }, 5000))
                throw new InvalidOperationException("Map transition did not finish; stage=" + Service.Transition?.Stage);
        }
        private static MapCatalogEntry MakeMap(MapId id)
        {
            var map = new MapDescriptor(id, new(id.Value, id.Value), MapKind.Outdoor, 1, 1,
                new(104, 31, 32648, new(-32), new(32)), Vector2.Zero, new(new(-32, -10, -32), new(32, 100, 32)),
                new(new(-64), new(64)), new(.9f, 30, 3000, -28, 2000, 30, 40, 150), new(-1, -1), new(0, 0),
                "start", [new("start", new(new(0, 20, 10), 0, -10), null)],
                new(MapTimeRule.Shared, new(17.5, Season.Autumn, WeatherKind.Clear), new("默认", "Default"), false, "test"),
                new("sky.json", "backdrop.json", []), []);
            return new(map, "res://data/" + id.Value + '/', []);
        }
    }

    private sealed class TestData(MapCatalog catalog) : MapDataSource(new Campus.Kernel.Boot.RequiredResources())
    {
        private readonly Dictionary<MapId, int> _versions = [];
        private readonly Dictionary<MapId, int> _reads = [];
        public List<MapLoadInput> Inputs { get; } = [];
        public int RectWrites { get; private set; }
        public void SetVersion(MapId map, int version) => _versions[map] = version;
        public int ReadCount(MapId map) => _reads.GetValueOrDefault(map);
        public override Task<MapCatalog> ReadCatalog() => Task.FromResult(catalog);
        public override Task<MapLoadInput> ReadLoadInputAsync(MapCatalogEntry entry)
        {
            _reads[entry.Descriptor.Id] = ReadCount(entry.Descriptor.Id) + 1;
            var files = new Dictionary<string, ReadOnlyMemory<byte>>(StringComparer.Ordinal)
            {
                ["map.json"] = JsonSerializer.SerializeToUtf8Bytes(new { id = entry.Descriptor.Id.Value }),
                ["world/geo.json"] = JsonSerializer.SerializeToUtf8Bytes(new { revision = _versions.GetValueOrDefault(entry.Descriptor.Id, 1) })
            };
            var input = new MapLoadInput(entry, BuildInputSnapshot.Freeze(files, "fixture/" + Inputs.Count));
            Inputs.Add(input);
            return Task.FromResult(input);
        }
        public override MapClaimSnapshot? ReadClaims(MapId map) => null;
        public override void WriteMapRect(MapRect rect) => RectWrites++;
        public override void ReportError(string context, Exception error) { }
    }
    private sealed class TestLoader(MapCatalog catalog) : IWorldModelLoader
    {
        private readonly System.Collections.Concurrent.ConcurrentDictionary<IWorldModel, int> _worldVersions = new();
        private MapId? _delayMap;
        private TaskCompletionSource? _delayedEntered;
        private TaskCompletionSource? _releaseDelayed;
        private int _loads;
        public HashSet<MapId> Failing { get; } = [];
        public int Loads => Volatile.Read(ref _loads);
        public int CompletedLoads => _worldVersions.Count;
        public bool DelayedLoadStarted => _delayedEntered?.Task.IsCompleted == true;
        public int WorldVersion(IWorldModel world) => _worldVersions[world];
        public void DelayNext(MapId map)
        { _delayMap = map; _delayedEntered = new(); _releaseDelayed = new(); }
        public void ReleaseDelayed() => _releaseDelayed?.TrySetResult();
        public WorldModelLoadResult Load(MapId map, IReadOnlyDictionary<string, ReadOnlyMemory<byte>> files,
            WorldModelLoadInputs inputs)
        {
            Interlocked.Increment(ref _loads);
            if (_delayMap == map)
            {
                _delayMap = null;
                var release = _releaseDelayed!;
                _delayedEntered!.TrySetResult();
                release.Task.GetAwaiter().GetResult();
            }
            if (Failing.Contains(map)) throw new InvalidDataException("Fixture load failure.");
            catalog.TryGet(map, out var entry);
            var d = entry.Descriptor;
            var world = new FakeWorld { Boundary = new([]) };
            using var document = JsonDocument.Parse(files["world/geo.json"]);
            _worldVersions[world] = document.RootElement.GetProperty("revision").GetInt32();
            return new(world, d.Geo, d.Center, d.Bounds, d.TileMin, d.TileMax);
        }
    }
    private sealed class TestRuntime : IMapRuntime, IMapLifetime
    {
        public Exception? Failure { get; private set; }
        public AppStartupException ReportFailure([FailureHandler("SYS-MAPS")] Exception error)
        {
            var failure = error as AppStartupException ?? new(AppStartupFailureCodes.MapPreparation, "Map preparation failed.", error);
            Failure = failure;
            return failure;
        }
        private TaskCompletionSource? _mount;
        private bool _delayMount;
        private bool _arrivalReady = true;
        public void ClearDestination()
        {
            Arrival?.Cancel(); Arrival = null; ArrivalChanged?.Invoke(null);
        }
        public IMapArrival? Arrival { get; private set; }
        public event Action<IMapArrival?>? ArrivalChanged;
        public bool MountReady => _mount?.Task.IsCompletedSuccessfully == true;
        public bool ArrivalReady
        {
            get => _arrivalReady;
            set { _arrivalReady = value; if (value) (Arrival as TestArrival)?.Complete(); }
        }
        public bool MountPending => _mount is { Task.IsCompleted: false };
        public int Arrivals { get; private set; }
        public Pose? LastArrival { get; private set; }
        public List<MapId> Mounted { get; } = [];
        public List<IMapContext> Contexts { get; } = [];
        public void DelayMount() => _delayMount = true;
        public void ReleaseMount() => _mount!.TrySetResult();
        public void FailMount(string message)
        {
            Failure = new InvalidDataException(message);
            _mount!.TrySetException(Failure);
        }
        public string DescribeReadiness() => $"mountReady={MountReady},arrival={Arrival?.Done.Status}";
        public void StopNewWork() => Arrival?.Cancel();
        public Task ShutdownAsync(MapId map)
        {
            StopNewWork();
            return Task.CompletedTask;
        }
        public Task Mount(IMapContext context)
        {
            Mounted.Add(context.Descriptor.Id);
            Contexts.Add(context);
            _mount = new();
            if (!_delayMount) _mount.TrySetResult();
            return _mount.Task;
        }
        public IMapArrival Arrive(Pose pose, ViewMode mode, NavSpace? space = null, System.Numerics.Vector3? collisionFeet = null)
        {
            if (!MountReady) throw new InvalidOperationException("A map must be mounted before arrival.");
            Arrival?.Cancel();
            Arrivals++;
            LastArrival = pose;
            var arrival = new TestArrival(new(pose, mode, space, collisionFeet));
            Arrival = arrival;
            ArrivalChanged?.Invoke(arrival);
            if (ArrivalReady) arrival.Complete();
            return arrival;
        }
    }
    private sealed class TestArrival(MapStreamDestination destination) : IMapArrival
    {
        private readonly TaskCompletionSource _done = new();
        public MapStreamDestination Destination => destination;
        public Task Done => _done.Task;
        public int Completed => Done.IsCompletedSuccessfully ? Total : 0;
        public int Total => 1;
        public event Action? ProgressChanged;
        public void Cancel() => _done.TrySetCanceled();
        public void Complete() { if (_done.TrySetResult()) ProgressChanged?.Invoke(); }
    }
}
