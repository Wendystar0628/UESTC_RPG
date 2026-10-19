using System.Text;
using System.Text.Json.Serialization.Metadata;
using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Save;
using Campus.Kernel.Registry;
using GdUnit4;

namespace Campus.Arch.Tests
{
    [TestSuite]
    public sealed class ServiceRegistryTests
    {
        [TestCase]
        [TestCategory("Contract")]
        public void SaveScopesKeepTheRawIdAndReplaceOnlyTheirOwnFallback()
        {
            var registry = DiscoverFixtures();
            var sections = registry.Entries.Where(entry => entry.Kind == "save" && entry.Id == "fixture-weather").ToArray();
            if (sections.Length != 2 || sections.Any(entry => entry.Map is not null))
                throw new InvalidOperationException("Global and per-map sections must coexist without rewriting their IDs or maps.");
            if (sections.Single(entry => entry.Scope == SaveScope.Global).Type != typeof(GlobalWeatherSection) ||
                sections.Single(entry => entry.Scope == SaveScope.PerMap).Type != typeof(PerMapWeatherSection))
                throw new InvalidOperationException("Fallback replacement crossed a save scope.");
            // Exercise the runner's effective-map grouping and attribute selection too.
            RegistryGateRunner.Run(registry, "fixture/index.json", "*", ReadFixture, [], FixtureFiles);
        }

        [TestCase]
        [TestCategory("Contract")]
        public void ReleaseClearsSameInstanceAliasesWithoutDisposingOrDroppingOtherInstances()
        {
            var registry = DiscoverFixtures();
            var first = (LifetimeService)registry.Get<ILifetimeFirst>();
            if (!ReferenceEquals(first, registry.Get<ILifetimeSecond>())) throw new InvalidOperationException("Service aliases are not shared.");
            registry.Add<ILifetimeExtra>(first);
            var unrelated = new LifetimeService();
            registry.Add<ILifetimeUnrelated>(unrelated);
            registry.Release(typeof(LifetimeService));
            if (first.Disposed || unrelated.Disposed || !ReferenceEquals(unrelated, registry.Get<ILifetimeUnrelated>()))
                throw new InvalidOperationException("Release disposed an owned object or removed an unrelated instance.");
            var replacement = registry.Get<ILifetimeSecond>();
            if (ReferenceEquals(first, replacement) || !ReferenceEquals(replacement, registry.Get<ILifetimeFirst>()) ||
                !ReferenceEquals(replacement, registry.Create(typeof(LifetimeService))))
                throw new InvalidOperationException("A released service could not be rebuilt consistently.");
            var extraGone = false;
            try { _ = registry.Get<ILifetimeExtra>(); }
            catch (InvalidOperationException) { extraGone = true; }
            if (!extraGone) throw new InvalidOperationException("An explicitly added same-instance alias survived Release.");
        }

        [TestCase]
        [TestCategory("Contract")]
        public void BakeProducerIsDiscoveredAndConstructibleFromItsEntry()
        {
            var registry = DiscoverFixtures();
            var entry = registry.Entries.Single(item => item.Kind == "bake" && item.Id == "fixture.bake");
            if (entry.Scope is not null || registry.Create(entry.Type) is not IBakeProducer producer ||
                !producer.CanBuild(new(new("fixture"), "fixture.bake", "L0")))
                throw new InvalidOperationException("Bake producer entry cannot be used by the bake service.");
        }

        private static ServiceRegistry DiscoverFixtures()
        {
            var registry = new ServiceRegistry();
            registry.Discover([typeof(ServiceRegistryTests).Assembly]);
            return registry;
        }

        private static byte[] ReadFixture(string path) => Encoding.UTF8.GetBytes(path switch
        {
            "fixture/index.json" => """{"version":1,"defaultMap":"fixture","maps":[{"id":"fixture","dir":"res://fixture/"}]}""",
            "res://fixture/map.json" => """{"id":"fixture","version":1,"dataRect":{"min":[0,0],"max":[64,64]},"defaultSpawn":"start","spawns":[{"id":"start","fly":{"position":[0,2,0],"yawDeg":0,"pitchDeg":0}}],"env":{"sky":"sky.json","backdrop":"backdrop.json","reflectLakes":[]}}""",
            "res://fixture/waypoints.json" => """{"version":1,"waypoints":[]}""",
            "res://fixture/world/geo.json" => """{"origin":[0,0],"crs":"EPSG:3857","bounds":[0,0,64,64],"entityPrefix":"fixture","license":"fixture","source":"fixture","date":"2026-10-19","boundary":{"outer":[[0,0],[64,0],[64,64],[0,64]],"holes":[]}}""",
            "res://fixture/world/core.json" => """{"features":[],"trees":[]}""",
            "res://fixture/interiors.json" => """{"version":1,"interiors":[]}""",
            _ => throw new InvalidOperationException($"Unexpected fixture input: {path}")
        });

        private static IEnumerable<string> FixtureFiles(string directory) =>
            directory.TrimEnd('/') == "res://fixture/world" ? ["core.json"] : [];
    }

    public interface ILifetimeFirst { }
    public interface ILifetimeSecond { }
    public interface ILifetimeExtra { }
    public interface ILifetimeUnrelated { }

    [KernelService(typeof(ILifetimeFirst), Owner = "WP-00")]
    [KernelService(typeof(ILifetimeSecond), Owner = "WP-00")]
    public sealed class LifetimeService : ILifetimeFirst, ILifetimeSecond, ILifetimeExtra, ILifetimeUnrelated, IDisposable
    {
        public bool Disposed { get; private set; }
        public void Dispose() => Disposed = true;
    }

    [BakeProducer("fixture.bake", Owner = "WP-02a")]
    public sealed class FixtureBakeProducer : IBakeProducer
    {
        public bool CanBuild(BakeKey key) => key.Module == "fixture.bake";
        public object Build(BakeKey key, BuildContext context) => throw new NotSupportedException("This test checks registration, not baking.");
    }

    public abstract class RegistrySaveFixture : ISaveSection
    {
        public JsonTypeInfo TypeInfo => throw new NotSupportedException();
        public object Capture(in CaptureContext context) => throw new NotSupportedException();
        public void Restore(object dto, RestoreContext context) => throw new NotSupportedException();
        public object CreateDefault(NewGameContext context) => throw new NotSupportedException();
    }

    [SaveSection("fixture-weather", Owner = "WP-45a", Scope = SaveScope.Global, Phase = RestorePhase.World, Order = 991)]
    public sealed class GlobalWeatherSection : RegistrySaveFixture { }

    [SaveSection("fixture-weather", Owner = "WP-45a", Scope = SaveScope.PerMap, Phase = RestorePhase.World, Order = 992)]
    public sealed class PerMapWeatherSection : RegistrySaveFixture { }
}

namespace Campus.Kernel.Stubs.Tests
{
    [SaveSection("fixture-weather", Owner = "WP-00", Scope = SaveScope.Global, Phase = RestorePhase.World, Order = 991)]
    public sealed class GlobalWeatherFallback : Campus.Arch.Tests.RegistrySaveFixture { }
}
