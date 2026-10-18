using Campus.Gen.Tests.Support;
using static GdUnit4.Assertions;
using Campus.Pipeline.Map;
using System.Numerics;
using System.Reflection;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Kernel.Maps;
using Campus.Gen.Interior.Shell.Box;
using Campus.Kernel.Registry;
using GdUnit4;

namespace Campus.Tests;

[TestSuite]
public sealed class InteriorProviderScopeTests
{
    [TestCase]
    public async Task NewMapConfigurationDoesNotChangeAnOlderRunningMapOrTheGlobalCache()
    {
        var registry = ProviderRegistry();
        var type = typeof(FixtureBoxInterior);
        var providerId = type.GetCustomAttribute<InteriorProviderAttribute>()!.Id;
        var globalFeature = new FeatureId("fixture:building:global");
        var featureA = new FeatureId("fixture:building:a");
        var featureB = new FeatureId("fixture:building:b");
        var global = (BoxInteriorProvider)registry.Create(type);
        global.Configure([globalFeature], [1], new Dictionary<FeatureId, IReadOnlyList<EntranceDecl>>());
        var globalContext = Context(new("fixture-global"), globalFeature, -200f,
            new Dictionary<string, IInteriorProvider> { [providerId] = global });
        var globalLayout = global.Layout(globalContext, globalFeature);

        var mapA = new MapId("fixture-a");
        var mapB = new MapId("fixture-b");
        var a = registry.CreateInteriorProvider(type, mapA);
        var providersA = new Dictionary<string, IInteriorProvider> { [providerId] = a };
        Configure(providerId, featureA, [1, 2], providersA);
        var contextA = Context(mapA, featureA, 0f, providersA);
        var beforeA = a.Layout(contextA, featureA);
        var entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var resume = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var previousJob = Task.Run(async () =>
        {
            entered.SetResult(true);
            await resume.Task;
            AssertThat(a.FeatureIds.SequenceEqual([featureA])).OverrideFailureMessage("The older job saw the new map's feature IDs.").IsTrue();
            return a.Layout(contextA, featureA);
        });
        try
        {
            await entered.Task;
            var b = registry.CreateInteriorProvider(type, mapB);
            AssertThat(!ReferenceEquals(a, b) && !ReferenceEquals(a, global) && !ReferenceEquals(b, global)).OverrideFailureMessage("Fresh map providers reused each other or the global cached instance.").IsTrue();
            var providersB = new Dictionary<string, IInteriorProvider> { [providerId] = b };
            Configure(providerId, featureB, [3, 4, 5], providersB);
            var contextB = Context(mapB, featureB, 200f, providersB);
            var layoutB = b.Layout(contextB, featureB);
            AssertThat(b.FeatureIds.SequenceEqual([featureB]) && layoutB.OpenStoreys.SequenceEqual([3, 4, 5])).OverrideFailureMessage("The second map was not independently configured.").IsTrue();
            AssertThat(ReferenceEquals(registry.Create(type), global)).OverrideFailureMessage("Fresh creation replaced the global cache.").IsTrue();
            AssertThat(global.FeatureIds.SequenceEqual([globalFeature])).OverrideFailureMessage("Fresh-map configuration changed global feature IDs.").IsTrue();
            var currentGlobal = global.Layout(globalContext, globalFeature);
            AssertThat(currentGlobal.Bounds == globalLayout.Bounds && currentGlobal.OpenStoreys.SequenceEqual(globalLayout.OpenStoreys)).OverrideFailureMessage("Fresh-map configuration changed the global layout.").IsTrue();
        }
        finally { resume.TrySetResult(true); }

        var afterA = await previousJob;
        AssertThat(afterA.Bounds == beforeA.Bounds && afterA.OpenStoreys.SequenceEqual([1, 2]) &&
            afterA.Storeys.Select(storey => storey.Index).SequenceEqual(beforeA.Storeys.Select(storey => storey.Index)) &&
            afterA.Entrances.Select(entrance => entrance.Id).SequenceEqual(beforeA.Entrances.Select(entrance => entrance.Id))).OverrideFailureMessage("The older map's layout changed after the newer map was configured.").IsTrue();
    }

    [TestCase]
    public void NarrowFactoryRejectsUnregisteredEngineAndServiceTypes()
    {
        var registry = ProviderRegistry();
        var map = new MapId("fixture-map");
        foreach (var type in new[] { typeof(UnregisteredInterior), typeof(Godot.Node), typeof(Campus.Kernel.Maps.MapService) })
        {
            AssertThrown(() => registry.CreateInteriorProvider(type, map)).IsInstanceOf<InvalidOperationException>();
        }
        // Rejections must not leave the cycle guard or instance cache poisoned.
        var first = registry.CreateInteriorProvider(typeof(FixtureBoxInterior), map);
        var second = registry.CreateInteriorProvider(typeof(FixtureBoxInterior), map);
        AssertThat(!ReferenceEquals(first, second)).OverrideFailureMessage("Factory did not recover after rejecting invalid requests.").IsTrue();
    }

    [TestCase]
    public void BuildContextCopiesAndOrdinallySortsTheProviderMapping()
    {
        var registry = ProviderRegistry();
        var map = new MapId("fixture-snapshot");
        var first = registry.CreateInteriorProvider(typeof(FixtureBoxInterior), map);
        var second = registry.CreateInteriorProvider(typeof(FixtureBoxInterior), map);
        var input = new Dictionary<string, IInteriorProvider>(StringComparer.OrdinalIgnoreCase)
        { ["z.provider"] = first, ["a.provider"] = second };
        var context = Context(map, new("fixture:building:snapshot"), 0f, input);
        input.Clear();
        input["new.provider"] = first;

        AssertThat(context.InteriorProviders.Keys.SequenceEqual(["a.provider", "z.provider"], StringComparer.Ordinal)).OverrideFailureMessage("Context did not retain an ordinally sorted independent mapping.").IsTrue();
        AssertThat(ReferenceEquals(context.InteriorProviders["a.provider"], second) &&
            ReferenceEquals(context.InteriorProviders["z.provider"], first) &&
            !context.InteriorProviders.ContainsKey("A.PROVIDER")).OverrideFailureMessage("Context changed provider identity or inherited a non-ordinal comparer.").IsTrue();
        var writableView = (IDictionary<string, IInteriorProvider>)context.InteriorProviders;
        AssertThrown(() => writableView.Add("another.provider", first)).IsInstanceOf<NotSupportedException>();
    }

    private static ServiceRegistry ProviderRegistry()
    {
        var registry = new ServiceRegistry();
        // The global fixture registration exercises the production box implementation across distinct test maps.
        registry.Discover([typeof(FixtureBoxInterior).Assembly]);
        return registry;
    }

    private static void Configure(string providerId, FeatureId feature, int[] floors,
        IReadOnlyDictionary<string, IInteriorProvider> providers)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(new
        {
            version = 1,
            interiors = new[] { new { providerId, featureIds = new[] { feature.Value }, openFloors = floors, entrances = Array.Empty<object>() } }
        });
        var bindings = InteriorAssembly.Configure(bytes, providers);
        AssertThat(bindings.Count == 1 && bindings[0].FeatureIds.SequenceEqual([feature]) &&
            bindings[0].OpenFloors.SequenceEqual(floors)).OverrideFailureMessage("Configure lost catalog feature/floor coverage.").IsTrue();
    }

    private static BuildContext Context(MapId map, FeatureId feature, float offset,
        IReadOnlyDictionary<string, IInteriorProvider> providers)
    {
        var bounds = new Box3(new(offset, 0f, offset), new(offset + 32f, 24f, offset + 40f));
        var ring = new PolygonRings([new(offset, offset), new(offset + 32f, offset),
            new(offset + 32f, offset + 40f), new(offset, offset + 40f)]);
        var world = new FakeWorld { Boundary = new([]) };
        world.Features = ((WorldFeature[])[new WorldFeature(feature, FeatureSource.Osm, new("building"), new PolygonGeometry(ring))
        { Bounds = bounds, Height = 24f, Storeys = 6 }]).ToDictionary(feature => feature.Id);
        world.Boundary = ring;
        var descriptor = new MapDescriptor(map, new("测试地图", "Fixture"), MapKind.Outdoor, 1, 7,
            new(0, 0, 32648, new(-512, -512), new(512, 512)), Vector2.Zero,
            new(new(-512, -28, -512), new(512, 2000, 512)), new(new(-512, -512), new(512, 512)),
            new(0.9f, 30, 3000, -28, 2000, 30, 40, 150), new(-8, -8), new(7, 7),
            "spawn", [new("spawn", new(new(0, 20, 30), 0, -15), null)],
            new(MapTimeRule.Shared, new(17.5, Season.Autumn), new("测试时刻", "Fixture"), false, "fixture"),
            new("sky.json", "backdrop.json", []), new Dictionary<string, string>(), []);
        return new BuildContext { Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog, Map = descriptor, World = world, WorldSeed = 7, InteriorProviders = providers };
    }


    [InteriorProvider("fixture.box", Owner = "WP-00")]
    public sealed class FixtureBoxInterior : BoxInteriorProvider { }

    // Deliberately lacks a registration attribute even though its assembly is discovered.
    private sealed class UnregisteredInterior : BoxInteriorProvider { }
}
