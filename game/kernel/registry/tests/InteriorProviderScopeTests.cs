using System.Numerics;
using System.Reflection;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Kernel.Maps;
using Campus.Gen.Parts.Stubs;
using Campus.Kernel.Registry;
using GdUnit4;

namespace Campus.Tests;

[TestSuite]
public sealed class InteriorProviderScopeTests
{
    [TestCase]
    public async Task NewMapConfigurationDoesNotChangeAnOlderRunningMapOrTheGlobalCache()
    {
        var registry = RealProviderRegistry();
        var type = typeof(LibraryInteriorStub);
        var providerId = type.GetCustomAttribute<InteriorProviderAttribute>()!.Id;
        var globalFeature = new FeatureId("fixture:building:global");
        var featureA = new FeatureId("fixture:building:a");
        var featureB = new FeatureId("fixture:building:b");
        var global = (BoxInteriorProviderStub)registry.Create(type);
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
            Require(a.FeatureIds.SequenceEqual([featureA]), "The older job saw the new map's feature IDs.");
            return a.Layout(contextA, featureA);
        });
        try
        {
            await entered.Task;
            var b = registry.CreateInteriorProvider(type, mapB);
            Require(!ReferenceEquals(a, b) && !ReferenceEquals(a, global) && !ReferenceEquals(b, global),
                "Fresh map providers reused each other or the global cached instance.");
            var providersB = new Dictionary<string, IInteriorProvider> { [providerId] = b };
            Configure(providerId, featureB, [3, 4, 5], providersB);
            var contextB = Context(mapB, featureB, 200f, providersB);
            var layoutB = b.Layout(contextB, featureB);
            Require(b.FeatureIds.SequenceEqual([featureB]) && layoutB.OpenStoreys.SequenceEqual([3, 4, 5]),
                "The second map was not independently configured.");
            Require(ReferenceEquals(registry.Create(type), global), "Fresh creation replaced the global cache.");
            Require(global.FeatureIds.SequenceEqual([globalFeature]), "Fresh-map configuration changed global feature IDs.");
            var currentGlobal = global.Layout(globalContext, globalFeature);
            Require(currentGlobal.Bounds == globalLayout.Bounds && currentGlobal.OpenStoreys.SequenceEqual(globalLayout.OpenStoreys),
                "Fresh-map configuration changed the global layout.");
        }
        finally { resume.TrySetResult(true); }

        var afterA = await previousJob;
        Require(afterA.Bounds == beforeA.Bounds && afterA.OpenStoreys.SequenceEqual([1, 2]) &&
            afterA.Storeys.Select(storey => storey.Index).SequenceEqual(beforeA.Storeys.Select(storey => storey.Index)) &&
            afterA.Entrances.Select(entrance => entrance.Id).SequenceEqual(beforeA.Entrances.Select(entrance => entrance.Id)),
            "The older map's layout changed after the newer map was configured.");
    }

    [TestCase]
    public void NarrowFactoryRejectsUnregisteredEngineAndServiceTypes()
    {
        var registry = RealProviderRegistry();
        var map = new MapId("fixture-map");
        foreach (var type in new[] { typeof(UnregisteredInterior), typeof(Godot.Node), typeof(Campus.Kernel.Maps.MapService) })
        {
            var error = Throws<InvalidOperationException>(() => registry.CreateInteriorProvider(type, map));
            Require(error.Message.Contains(type.FullName!, StringComparison.Ordinal), "Rejected type was not identified in the error.");
        }
        // Rejections must not leave the cycle guard or instance cache poisoned.
        var first = registry.CreateInteriorProvider(typeof(LibraryInteriorStub), map);
        var second = registry.CreateInteriorProvider(typeof(LibraryInteriorStub), map);
        Require(!ReferenceEquals(first, second), "Factory did not recover after rejecting invalid requests.");
    }

    [TestCase]
    public void BuildContextCopiesAndOrdinallySortsTheProviderMapping()
    {
        var registry = RealProviderRegistry();
        var map = new MapId("fixture-snapshot");
        var first = registry.CreateInteriorProvider(typeof(LibraryInteriorStub), map);
        var second = registry.CreateInteriorProvider(typeof(LibraryInteriorStub), map);
        var input = new Dictionary<string, IInteriorProvider>(StringComparer.OrdinalIgnoreCase)
        { ["z.provider"] = first, ["a.provider"] = second };
        var context = Context(map, new("fixture:building:snapshot"), 0f, input);
        input.Clear();
        input["new.provider"] = first;

        Require(context.InteriorProviders.Keys.SequenceEqual(["a.provider", "z.provider"], StringComparer.Ordinal),
            "Context did not retain an ordinally sorted independent mapping.");
        Require(ReferenceEquals(context.InteriorProviders["a.provider"], second) &&
            ReferenceEquals(context.InteriorProviders["z.provider"], first) &&
            !context.InteriorProviders.ContainsKey("A.PROVIDER"), "Context changed provider identity or inherited a non-ordinal comparer.");
        var writableView = (IDictionary<string, IInteriorProvider>)context.InteriorProviders;
        _ = Throws<NotSupportedException>(() => writableView.Add("another.provider", first));
    }

    private static ServiceRegistry RealProviderRegistry()
    {
        var registry = new ServiceRegistry();
        // Only the production pure-.NET fallback assembly is discovered. Never scan Campus.Tests or use Kernel.Services.
        registry.Discover([typeof(LibraryInteriorStub).Assembly]);
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
        Require(bindings.Count == 1 && bindings[0].FeatureIds.SequenceEqual([feature]) &&
            bindings[0].OpenFloors.SequenceEqual(floors), "Configure lost catalog feature/floor coverage.");
    }

    private static BuildContext Context(MapId map, FeatureId feature, float offset,
        IReadOnlyDictionary<string, IInteriorProvider> providers)
    {
        var bounds = new Box3(new(offset, 0f, offset), new(offset + 32f, 24f, offset + 40f));
        var ring = new PolygonRings([new(offset, offset), new(offset + 32f, offset),
            new(offset + 32f, offset + 40f), new(offset, offset + 40f)]);
        var world = new MutableWorldFixture();
        world.Configure([new WorldFeature(feature, FeatureSource.Osm, new("building"), new PolygonGeometry(ring))
        { Bounds = bounds, Height = 24f, Storeys = 6 }], boundary: ring);
        var descriptor = new MapDescriptor(map, new("测试地图", "Fixture"), MapKind.Outdoor, 1, 7,
            new(0, 0, 32648, new(-512, -512), new(512, 512)), Vector2.Zero,
            new(new(-512, -28, -512), new(512, 2000, 512)), new(new(-512, -512), new(512, 512)),
            new(0.9f, 30, 3000, -28, 2000, 30, 40, 150), new(-8, -8), new(7, 7),
            "spawn", [new("spawn", new(new(0, 20, 30), 0, -15), null)],
            new(MapTimeRule.Shared, new(17.5, Season.Autumn), new("测试时刻", "Fixture"), false, "fixture"),
            new("sky.json", "backdrop.json", []), new Dictionary<string, string>(), []);
        return new BuildContext { Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog, Map = descriptor, World = world, WorldSeed = 7, InteriorProviders = providers };
    }

    private static T Throws<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T error) { return error; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    // Deliberately unregistered, and its assembly is never passed to Discover.
    private sealed class UnregisteredInterior : BoxInteriorProviderStub { }
}
