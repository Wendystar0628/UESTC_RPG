using System.Text;
using System.Text.Json.Serialization.Metadata;
using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Save;
using Campus.Contracts.Maps;
using Campus.Kernel.Registry;
using Campus.Gates;
using GdUnit4;

namespace Campus.Arch.Tests
{
    [TestSuite]
    public sealed class ServiceRegistryTests
    {
        [TestCase]
        [TestCategory("Contract")]
        public void SaveScopesKeepTheRawIdAndReplaceOnlyTheirOwnStub()
        {
            var registry = DiscoverFixtures();
            var sections = registry.Entries.Where(entry => entry.Kind == RegistrationKind.Save && entry.Id == "fixture-weather").ToArray();
            if (sections.Length != 2 || sections.Any(entry => entry.Map is not null))
                throw new InvalidOperationException("Global and per-map sections must coexist without rewriting their IDs or maps.");
            if (sections.Single(entry => entry.Scope == SaveScope.Global.ToString()).Type != typeof(GlobalWeatherSection) ||
                sections.Single(entry => entry.Scope == SaveScope.PerMap.ToString()).Type != typeof(PerMapWeatherSection))
                throw new InvalidOperationException("Stub replacement crossed a save scope.");
            // Exercise the runner's effective-map grouping and attribute selection too.
            RegistryGateRunner.Run(registry, "fixture/index.json", null, true, ReadFixture, [], FixtureFiles);
        }

        [TestCase]
        [TestCategory("Contract")]
        public async Task MapReleaseDisposesOwnedInstancesAndKeepsBorrowedBindings()
        {
            var registry = DiscoverFixtures();
            var borrowed = new LifetimeService();
            registry.Add<ILifetimeExtra>(borrowed);
            var map = new MapId("fixture-map");
            var first = registry.Create(typeof(MapLifetimeService), map);
            if (!ReferenceEquals(first, registry.Create(typeof(MapLifetimeService), map))) throw new InvalidOperationException("Map scope was not reused.");
            await registry.ReleaseMapAsync(map);
            if (!((MapLifetimeService)first).Disposed || borrowed.Disposed || !ReferenceEquals(borrowed, registry.Get<ILifetimeExtra>()))
                throw new InvalidOperationException("Map release did not preserve borrowed instances.");
            var second = registry.Create(typeof(MapLifetimeService), map);
            if (ReferenceEquals(first, second)) throw new InvalidOperationException("Reload reused the disposed map instance.");
            var global = registry.Get<ILifetimeFirst>();
            if (!ReferenceEquals(global, registry.Get<ILifetimeSecond>())) throw new InvalidOperationException("Global aliases differ.");
            await registry.ShutdownAsync();
            if (!((LifetimeService)global).Disposed || !((MapLifetimeService)second).Disposed || borrowed.Disposed)
                throw new InvalidOperationException("Registry disposal did not respect ownership.");
        }

        [TestCase]
        [TestCategory("Contract")]
        public async Task TransientsAreDistinctAndDisposeBeforeTheirSharedDependency()
        {
            DisposalLog.Clear();
            var registry = DiscoverFixtures();
            var map = new MapId("fixture");
            var first = registry.Create(typeof(TransientConsumer), map);
            var second = registry.Create(typeof(TransientConsumer), map);
            if (ReferenceEquals(first, second)) throw new InvalidOperationException("Transient requests reused an instance.");
            await registry.ReleaseMapAsync(map);
            if (!DisposalLog.SequenceEqual(["consumer", "consumer"])) throw new InvalidOperationException("Scope did not release its transient consumers.");
            await registry.ShutdownAsync();
            if (!DisposalLog.SequenceEqual(["consumer", "consumer", "dependency"])) throw new InvalidOperationException("Dependency release did not follow consumers.");
        }

        public static readonly List<string> DisposalLog = [];

        [TestCase, TestCategory("Regression")]
        public async Task ShutdownStopsOwnedProducersAndAwaitsBothDrainsBeforeRelease()
        {
            var probe = new ShutdownProbe(); var registry = DiscoverMapOwners(probe);
            var maps = registry.Get<IMapService>(); registry.Get<IMapLifetime>();
            var map = new MapId("fixture-map");
            var owned = (MapLifetimeService)registry.Create(typeof(MapLifetimeService), map);
            probe.ReleaseMap = async () => { await registry.ReleaseMapAsync(map); probe.Calls.Add("map-owned-release"); };
            var stopping = registry.SealMapPreparationAsync();
            Assertions.AssertThrown(() => registry.Add(probe)).IsInstanceOf<InvalidOperationException>();
            Assertions.AssertThrown(() => registry.Get<SharedDependency>()).IsInstanceOf<InvalidOperationException>();
            if (!ReferenceEquals(maps, registry.Get<IMapService>()) || !ReferenceEquals(probe, registry.Get<ShutdownProbe>()))
                throw new InvalidOperationException("Stopping preparation removed an existing owned or supplied binding.");
            var shutdown = registry.ShutdownAsync();
            if (!ReferenceEquals(stopping, registry.SealMapPreparationAsync()) || stopping.IsCompleted || shutdown.IsCompleted ||
                !probe.Calls.SequenceEqual(["map-stop", "runtime-stop", "map-drain"]))
                throw new InvalidOperationException("Owned producers were not stopped before awaiting their shared drain.");
            probe.MapDrain.SetResult(); await probe.RuntimeEntered.Task;
            if (shutdown.IsCompleted || owned.Disposed || probe.Calls.Any(call => call.EndsWith("release", StringComparison.Ordinal)))
                throw new InvalidOperationException("An owned object was released before runtime preparation drained.");
            Assertions.AssertThrown(() => registry.Get<SharedDependency>()).IsInstanceOf<InvalidOperationException>();
            probe.RuntimeDrain.SetResult(); await shutdown;
            if (!probe.Calls.SequenceEqual(["map-stop", "runtime-stop", "map-drain", "runtime-drain", "map-owned-release", "runtime-release", "map-release"]) ||
                !owned.Disposed || !ReferenceEquals(shutdown, registry.ShutdownAsync()))
                throw new InvalidOperationException("Shutdown did not retain one task and reverse owned release order.");
        }

        [TestCase, TestCategory("Regression")]
        public async Task RuntimeOnlyShutdownNeverConstructsAnAbsentMapService()
        {
            var probe = new ShutdownProbe(); probe.RuntimeDrain.SetResult();
            var registry = DiscoverMapOwners(probe); registry.Get<IMapLifetime>();
            await registry.ShutdownAsync();
            if (probe.MapsConstructed != 0 || !probe.Calls.SequenceEqual(["runtime-stop", "runtime-drain", "runtime-release"]))
                throw new InvalidOperationException("Runtime-only shutdown constructed a map service or lost its owned runtime.");
        }

        [TestCase, TestCategory("Contract")]
        public async Task ShutdownLeavesSuppliedMapAndRuntimeOwnersUntouched()
        {
            var probe = new ShutdownProbe(); var registry = new ServiceRegistry();
            registry.Add<IMapService>(new ShutdownMapService(probe));
            registry.Add<IMapLifetime>(new ShutdownMapLifetime(probe));
            var stopping = registry.SealMapPreparationAsync();
            Assertions.AssertThrown(() => registry.Discover([typeof(SharedDependency)], _ => "fixture"))
                .IsInstanceOf<InvalidOperationException>();
            await stopping;
            await registry.ShutdownAsync();
            if (probe.Calls.Count != 0) throw new InvalidOperationException("Shutdown stopped or disposed a supplied map owner.");
        }

        [TestCase, TestCategory("Regression")]
        public async Task StopDrainAndReleaseFailuresAllSurviveShutdown()
        {
            var stop = new IOException("stop"); var data = new IOException("data");
            var runtime = new IOException("runtime"); var release = new IOException("release");
            var probe = new ShutdownProbe { StopFailure = stop, ReleaseFailure = release };
            probe.MapDrain.SetException(data); probe.RuntimeDrain.SetException(runtime);
            var registry = DiscoverMapOwners(probe); registry.Get<IMapService>(); registry.Get<IMapLifetime>();
            try { await registry.ShutdownAsync(); throw new InvalidOperationException("Shutdown lost its original failures."); }
            catch (AggregateException error)
            {
                var causes = error.Flatten().InnerExceptions;
                Exception[] expected = [stop, data, runtime, release];
                if (causes.Count != expected.Length || expected.Any(cause => !causes.Contains(cause)) ||
                    !probe.Calls.SequenceEqual(["map-stop", "runtime-stop", "map-drain", "runtime-drain", "runtime-release", "map-release"]))
                    throw new InvalidOperationException("A failed phase prevented another phase or lost its original cause.", error);
            }
        }

        private static ServiceRegistry DiscoverMapOwners(ShutdownProbe probe)
        {
            var registry = new ServiceRegistry(); registry.Add(probe);
            registry.Discover([typeof(ShutdownMapService), typeof(ShutdownMapLifetime), typeof(SharedDependency), typeof(MapLifetimeService)], _ => "fixture");
            return registry;
        }

        [TestCase]
        [TestCategory("Contract")]
        public void BakeProducerIsDiscoveredAndConstructibleFromItsEntry()
        {
            var registry = DiscoverFixtures();
            var entry = registry.Entries.Single(item => item.Kind == RegistrationKind.Bake && item.Id == "fixture.bake");
            if (entry.Scope is not null || registry.Create(entry.Type, new MapId("fixture")) is not IBakeProducer producer ||
                !producer.CanBuild(new(new("fixture"), "fixture.bake", "L0")))
                throw new InvalidOperationException("Bake producer entry cannot be used by the bake service.");
        }

        private static ServiceRegistry DiscoverFixtures()
        {
            var registry = new ServiceRegistry();
            registry.Discover([typeof(LifetimeService), typeof(MapLifetimeService), typeof(SharedDependency),
                typeof(TransientConsumer), typeof(FixtureBakeProducer), typeof(GlobalWeatherSection),
                typeof(PerMapWeatherSection), typeof(Campus.Kernel.Stubs.Tests.GlobalWeatherStub)], _ => "fixture");
            return registry;
        }

        private static byte[] ReadFixture(string path) => Encoding.UTF8.GetBytes(path switch
        {
            "fixture/index.json" => """{"version":1,"defaultMap":"fixture","maps":[{"id":"fixture","dir":"res://fixture/"}]}""",
            "res://fixture/map.json" => """{"id":"fixture","version":1,"dataRect":{"min":[0,0],"max":[64,64]},"defaultSpawn":"start","spawns":[{"id":"start","fly":{"position":[0,2,0],"yawDeg":0,"pitchDeg":0}}],"env":{"sky":"sky.json","backdrop":"backdrop.json","reflectLakes":[]}}""",
            "res://fixture/waypoints.json" => """{"version":1,"waypoints":[]}""",
            "res://fixture/world/geo.json" => """{"origin":[0,0],"crs":"EPSG:3857","bounds":[0,0,64,64],"entityPrefix":"fixture","license":"fixture","source":"fixture","date":"2026-10-19","boundary":{"outer":[[0,0],[64,0],[64,64],[0,64]],"holes":[]}}""",
            "res://fixture/world/core.json" => """{"features":[],"trees":[]}""",
            "res://fixture/interiors.json" => """{"version":2,"interiors":[]}""",
            _ => throw new InvalidOperationException($"Unexpected fixture input: {path}")
        });

        private static IEnumerable<string> FixtureFiles(string directory) =>
            directory.TrimEnd('/') == "res://fixture/world" ? ["core.json"] : [];
    }

    public interface ILifetimeFirst { }
    public interface ILifetimeSecond { }
    public interface ILifetimeExtra { }
    public interface ILifetimeUnrelated { }

    public sealed class ShutdownProbe
    {
        public List<string> Calls { get; } = [];
        public TaskCompletionSource MapDrain { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource RuntimeDrain { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource RuntimeEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int MapsConstructed { get; set; }
        public Func<Task>? ReleaseMap { get; set; }
        public Exception? StopFailure { get; init; }
        public Exception? ReleaseFailure { get; init; }
    }

    [KernelService(typeof(IMapService))]
    public sealed class ShutdownMapService : IMapService, IDisposable
    {
        public event Action<MapPrepareEvent>? PrepareCommand { add { } remove { } }
        private readonly ShutdownProbe _probe;
        public ShutdownMapService(ShutdownProbe probe) { _probe = probe; probe.MapsConstructed++; }
        public long Rev => throw new NotSupportedException();
        public MapId Default => throw new NotSupportedException();
        public IReadOnlyList<MapDescriptor> Maps => throw new NotSupportedException();
        public IReadOnlyList<MapVisit> Visits => throw new NotSupportedException();
        public MapId Current => throw new NotSupportedException();
        public IMapContext? Context => throw new NotSupportedException();
        public IMapTransition? Transition => throw new NotSupportedException();
        public MapSpawn ResolveSpawn(MapId map, string? spawn = null) => throw new NotSupportedException();
        public Task Prepare(MapId map) => throw new NotSupportedException();
        public void StopNewWork() { _probe.Calls.Add("map-stop"); if (_probe.StopFailure is { } error) throw error; }
        public Task DrainAsync() { _probe.Calls.Add("map-drain"); return _probe.MapDrain.Task; }
        public void Dispose() => _probe.Calls.Add("map-release");
    }

    [KernelService(typeof(IMapLifetime))]
    public sealed class ShutdownMapLifetime(ShutdownProbe probe) : IMapLifetime, IDisposable
    {
        public Task Mount(IMapContext context) => throw new NotSupportedException();
        public string DescribeReadiness() => throw new NotSupportedException();
        public void StopNewWork() => probe.Calls.Add("runtime-stop");
        public async Task ShutdownAsync()
        {
            probe.Calls.Add("runtime-drain"); probe.RuntimeEntered.TrySetResult(); await probe.RuntimeDrain.Task;
            if (probe.ReleaseMap is { } release) await release();
        }
        public void Dispose() { probe.Calls.Add("runtime-release"); if (probe.ReleaseFailure is { } error) throw error; }
    }

    [KernelService(typeof(ILifetimeFirst))]
    [KernelService(typeof(ILifetimeSecond))]
    public sealed class LifetimeService : ILifetimeFirst, ILifetimeSecond, ILifetimeExtra, ILifetimeUnrelated, IDisposable
    {
        public bool Disposed { get; private set; }
        public void Dispose() => Disposed = true;
    }

    [KernelService(typeof(MapLifetimeService), Lifetime = RegistrationLifetime.Map)]
    public sealed class MapLifetimeService : IDisposable
    {
        public bool Disposed { get; private set; }
        public void Dispose() => Disposed = true;
    }

    [KernelService(typeof(SharedDependency))]
    public sealed class SharedDependency : IDisposable
    {
        public void Dispose() => ServiceRegistryTests.DisposalLog.Add("dependency");
    }

    [KernelService(typeof(TransientConsumer), Lifetime = RegistrationLifetime.Transient)]
    public sealed class TransientConsumer(SharedDependency dependency) : IDisposable
    {
        private readonly SharedDependency _dependency = dependency;
        public void Dispose() { GC.KeepAlive(_dependency); ServiceRegistryTests.DisposalLog.Add("consumer"); }
    }

    [BakeProducer("fixture.bake")]
    public sealed class FixtureBakeProducer : IBakeProducer
    {
        public BakeInputDependencies Inputs(BakeKey key) => BakeInputDependencies.World;
        public bool UsesModule(IContentModule module) => false;
        public BakePayloadClass PayloadClass => BakePayloadClass.Product;
        public BakeProductKind ProductKind => BakeProductKind.Collision;
        public IReadOnlyList<BakeTileEntry> Tiles(BuildContext context) => [];
        public bool CanBuild(BakeKey key) => key.Module == "fixture.bake";
        public object Build(BakeKey key, BuildContext context, string inputHash) => throw new NotSupportedException("This test checks registration, not baking.");
    }

    public abstract class RegistrySaveFixture : ISaveSection
    {
        public JsonTypeInfo TypeInfo => throw new NotSupportedException();
        public object Capture(in CaptureContext context) => throw new NotSupportedException();
        public void Restore(object dto, RestoreContext context) => throw new NotSupportedException();
        public object CreateDefault(NewGameContext context) => throw new NotSupportedException();
    }

    [SaveSection("fixture-weather", Scope = SaveScope.Global, Phase = RestorePhase.World, Order = 991)]
    public sealed class GlobalWeatherSection : RegistrySaveFixture { }

    [SaveSection("fixture-weather", Scope = SaveScope.PerMap, Phase = RestorePhase.World, Order = 992)]
    public sealed class PerMapWeatherSection : RegistrySaveFixture { }
}

namespace Campus.Kernel.Stubs.Tests
{
    [SaveSection("fixture-weather", IsStub = true, Scope = SaveScope.Global, Phase = RestorePhase.World, Order = 991)]
    public sealed class GlobalWeatherStub : Campus.Arch.Tests.RegistrySaveFixture { }
}
