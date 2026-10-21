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
