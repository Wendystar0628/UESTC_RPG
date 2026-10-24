using Campus.Gen.World;
using Campus.Gen.Tests.Support;
using Campus.Gen.BakeInput;
using System.Numerics;
using System.Text;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.AppFlow;
using Campus.Kernel.Maps;

namespace Campus.Tests;

internal static class MapServiceTestSupport
{
    internal sealed class Fixture
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
        public Fixture(bool initialize = true, bool waypointDefault = false, ICameraRig? camera = null)
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
            Service = new(camera ?? Camera, Runtime, Loader, Data);
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

    internal sealed class TestData(MapCatalog catalog) : MapDataSource(new Campus.Kernel.Boot.RequiredResources())
    {
        private TaskCompletionSource<MapLoadInput>? _pendingRead;
        private MapLoadInput? _delayedInput;
        private TaskCompletionSource<MapCatalog>? _pendingCatalog;
        public List<MapLoadInput> Inputs { get; } = [];
        public void DelayRead() => _pendingRead = new();
        public void ReleaseRead(Exception? failure = null)
        {
            if (failure is null) _pendingRead!.SetResult(_delayedInput!);
            else _pendingRead!.SetException(failure);
        }
        public void DelayCatalog() => _pendingCatalog = new();
        public void ReleaseCatalog() => _pendingCatalog!.SetResult(catalog);
        public override Task<MapCatalog> ReadCatalog() => _pendingCatalog?.Task ?? Task.FromResult(catalog);
        public override Task<MapLoadInput> ReadLoadInputAsync(MapCatalogEntry entry)
        {
            var files = new Dictionary<string, ReadOnlyMemory<byte>>(StringComparer.Ordinal)
            {
                ["map.json"] = JsonSerializer.SerializeToUtf8Bytes(new { id = entry.Descriptor.Id.Value })
            };
            var input = new MapLoadInput(entry, BuildInputSnapshot.Freeze(files, "fixture/" + Inputs.Count));
            Inputs.Add(input);
            if (_pendingRead is { } pending) { _delayedInput = input; return pending.Task; }
            return Task.FromResult(input);
        }
        public override MapClaimSnapshot? ReadClaims(MapId map) => null;
        public override void WriteMapRect(MapRect rect) { }
        public override void ReportError(string context, Exception error) { }
    }
    internal sealed class TestLoader(MapCatalog catalog) : IWorldModelLoader
    {
        private MapId? _delayMap;
        private TaskCompletionSource? _delayedEntered;
        private TaskCompletionSource? _releaseDelayed;
        private int _loads;
        private int _completedLoads;
        public HashSet<MapId> Failing { get; } = [];
        public int Loads => Volatile.Read(ref _loads);
        public int CompletedLoads => Volatile.Read(ref _completedLoads);
        public bool DelayedLoadStarted => _delayedEntered?.Task.IsCompleted == true;
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
            var result = new WorldModelLoadResult(world, d.Geo, d.Center, d.Bounds, d.TileMin, d.TileMax);
            Interlocked.Increment(ref _completedLoads);
            return result;
        }
    }
    internal sealed class TestRuntime : IMapRuntime, IMapLifetime
    {
        public Exception? Failure { get; private set; }
        public AppStartupException ReportFailure([FailureHandler("SYS-MAPS")] Exception error)
        {
            var failure = error as AppStartupException ?? new("Map preparation failed.", error);
            Failure = failure;
            return failure;
        }
        private TaskCompletionSource? _mount;
        private bool _delayMount;
        public void ClearDestination()
        {
            Arrival?.Cancel(); Arrival = null; ArrivalChanged?.Invoke(null);
        }
        public IMapArrival? Arrival { get; private set; }
        public event Action<IMapArrival?>? ArrivalChanged;
        public bool MountReady => _mount?.Task.IsCompletedSuccessfully == true;
        public bool MountPending => _mount is { Task.IsCompleted: false };
        public int Arrivals { get; private set; }
        public List<MapId> Mounted { get; } = [];
        public void DelayMount() => _delayMount = true;
        public void ReleaseMount() => _mount!.TrySetResult();
        public string DescribeReadiness() => $"mountReady={MountReady},arrival={Arrival?.Done.Status}";
        public void StopNewWork() { _mount?.TrySetCanceled(); Arrival?.Cancel(); }
        public Task ShutdownAsync()
        {
            StopNewWork();
            return Task.CompletedTask;
        }
        public Task Mount(IMapContext context)
        {
            Mounted.Add(context.Descriptor.Id);
            _mount = new();
            if (!_delayMount) _mount.TrySetResult();
            return _mount.Task;
        }
        public IMapArrival Arrive(Pose pose, ViewMode mode, NavSpace? space = null, System.Numerics.Vector3? collisionFeet = null)
        {
            if (!MountReady) throw new InvalidOperationException("A map must be mounted before arrival.");
            Arrival?.Cancel();
            Arrivals++;
            var arrival = new TestArrival(new(pose, mode, space, collisionFeet));
            Arrival = arrival;
            ArrivalChanged?.Invoke(arrival);
            arrival.Complete();
            return arrival;
        }
    }
    internal sealed class TestArrival(MapStreamDestination destination) : IMapArrival
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
