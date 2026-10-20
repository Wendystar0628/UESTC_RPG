using Campus.Gen.Tests.Support;
using static GdUnit4.Assertions;
using Campus.Gen.BakeInput;
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
    [TestCategory(TestReason.Contract)]
    public async Task NewMapConfigurationDoesNotChangeAnOlderMapScope()
    {
        var registry = ProviderRegistry();
        var type = typeof(FixtureBoxInterior);
        var providerId = type.GetCustomAttribute<InteriorProviderAttribute>()!.Id;
        var referenceFeature = new FeatureId("fixture:building:global");
        var featureA = new FeatureId("fixture:building:a");
        var featureB = new FeatureId("fixture:building:b");
        var reference = (BoxInteriorProvider)registry.Create(type, new MapId("fixture-global"));
        reference.Configure(new([referenceFeature], [new(referenceFeature, "", [1])], new Dictionary<FeatureId, IReadOnlyList<EntranceDecl>>(), null, []));
        var referenceContext = Context(new("fixture-global"), referenceFeature, -200f,
            new Dictionary<string, IInteriorProvider> { [providerId] = reference });
        var referenceLayout = reference.Layout(referenceContext, referenceFeature);

        var mapA = new MapId("fixture-a");
        var mapB = new MapId("fixture-b");
        var a = (IInteriorProvider)registry.Create(type, mapA);
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
            var b = (IInteriorProvider)registry.Create(type, mapB);
            AssertThat(!ReferenceEquals(a, b) && !ReferenceEquals(a, reference) && !ReferenceEquals(b, reference)).OverrideFailureMessage("Fresh map providers reused each other or another map scope.").IsTrue();
            var providersB = new Dictionary<string, IInteriorProvider> { [providerId] = b };
            Configure(providerId, featureB, [3, 4, 5], providersB);
            var contextB = Context(mapB, featureB, 200f, providersB);
            var layoutB = b.Layout(contextB, featureB);
            AssertThat(b.FeatureIds.SequenceEqual([featureB]) && layoutB.OpenStoreys.SequenceEqual([3, 4, 5])).OverrideFailureMessage("The second map was not independently configured.").IsTrue();
            AssertThat(ReferenceEquals(registry.Create(type, new MapId("fixture-global")), reference)).OverrideFailureMessage("Map creation replaced another scope.").IsTrue();
            AssertThat(reference.FeatureIds.SequenceEqual([referenceFeature])).OverrideFailureMessage("Fresh-map configuration changed another map scope's feature IDs.").IsTrue();
            var currentReference = reference.Layout(referenceContext, referenceFeature);
            AssertThat(currentReference.Bounds == referenceLayout.Bounds && currentReference.OpenStoreys.SequenceEqual(referenceLayout.OpenStoreys)).OverrideFailureMessage("Fresh-map configuration changed another map scope's layout.").IsTrue();
        }
        finally { resume.TrySetResult(true); }

        var afterA = await previousJob;
        AssertThat(afterA.Bounds == beforeA.Bounds && afterA.OpenStoreys.SequenceEqual([1, 2]) &&
            afterA.Storeys.Select(storey => storey.Index).SequenceEqual(beforeA.Storeys.Select(storey => storey.Index)) &&
            afterA.Entrances.Select(entrance => entrance.Id).SequenceEqual(beforeA.Entrances.Select(entrance => entrance.Id))).OverrideFailureMessage("The older map's layout changed after the newer map was configured.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void ScopedCreationRejectsUnregisteredEngineAndServiceTypes()
    {
        var registry = ProviderRegistry();
        var map = new MapId("fixture-map");
        foreach (var type in new[] { typeof(UnregisteredInterior), typeof(Godot.Node), typeof(Campus.Kernel.Maps.MapService) })
        {
            AssertThrown(() => registry.Create(type, map)).IsInstanceOf<InvalidOperationException>();
        }
        // Rejections must not leave the cycle guard or instance cache poisoned.
        var first = (IInteriorProvider)registry.Create(typeof(FixtureBoxInterior), map);
        var second = (IInteriorProvider)registry.Create(typeof(FixtureBoxInterior), new MapId(map.Value + "-second"));
        AssertThat(!ReferenceEquals(first, second)).OverrideFailureMessage("Different map scopes reused one provider.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void BuildContextCopiesAndOrdinallySortsTheProviderMapping()
    {
        var registry = ProviderRegistry();
        var map = new MapId("fixture-snapshot");
        var first = (IInteriorProvider)registry.Create(typeof(FixtureBoxInterior), map);
        var second = (IInteriorProvider)registry.Create(typeof(FixtureBoxInterior), new MapId(map.Value + "-second"));
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

    [TestCase, TestCategory(TestReason.Contract)]
    public async Task FactoryProductsHaveMapIdentityAndReleaseBeforeTheirFactory()
    {
        var registry=ProviderRegistry();var map=new MapId("factory-scope");
        try
        {
            var factory=(FixtureFactory)registry.Create(typeof(FixtureFactory),map);
            var first=registry.CreateInterior(typeof(FixtureFactory),"fixture.family.first",map);
            var second=registry.CreateInterior(typeof(FixtureFactory),"fixture.family.second",map);
            var other=registry.CreateInterior(typeof(FixtureFactory),"fixture.family.first",new("other-scope"));
            AssertThat(ReferenceEquals(first,registry.CreateInterior(typeof(FixtureFactory),"fixture.family.first",map))&&
                !ReferenceEquals(first,second)&&!ReferenceEquals(first,other)).IsTrue();
            await registry.ReleaseMapAsync(map);
            AssertThat(factory.Released.SequenceEqual(["fixture.family.second","fixture.family.first","factory"])).IsTrue();
        }
        finally { await registry.ShutdownAsync(); }
    }

    private static ServiceRegistry ProviderRegistry()
    {
        var registry = new ServiceRegistry();
        // The map-independent fixture declaration exercises the production box implementation across distinct test maps.
        registry.Discover([typeof(FixtureBoxInterior).Assembly], _ => "fixture");
        return registry;
    }

    private static void Configure(string providerId, FeatureId feature, int[] floors,
        IReadOnlyDictionary<string, IInteriorProvider> providers)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(new
        {
            version = 2,
            interiors = new[] { new { providerId, featureIds = new[] { feature.Value }, openParts = new[] { new { featureId = feature.Value, sourceId = "", floors } }, entrances = Array.Empty<object>() } }
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
        world.Features = ((WorldFeature[])[new WorldFeature(feature, new("building"), new PolygonGeometry(ring))
        { Bounds = bounds, Height = 24f, Storeys = 6 }]).ToDictionary(feature => feature.Id);
        world.Boundary = ring;
        var descriptor = new MapDescriptor(map, new("测试地图", "Fixture"), MapKind.Outdoor, 1, 7,
            new(0, 0, 32648, new(-512, -512), new(512, 512)), Vector2.Zero,
            new(new(-512, -28, -512), new(512, 2000, 512)), new(new(-512, -512), new(512, 512)),
            new(0.9f, 30, 3000, -28, 2000, 30, 40, 150), new(-8, -8), new(7, 7),
            "spawn", [new("spawn", new(new(0, 20, 30), 0, -15), null)],
            new(MapTimeRule.Shared, new(17.5, Season.Autumn), new("测试时刻", "Fixture"), false, "fixture"),
            new("sky.json", "backdrop.json", []), []);
        return new BuildContext { CodeHash="fixture",WindowNumbering=new(map,new string('0',64),[]),Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog, Map = descriptor, World = world, WorldSeed = 7, InteriorProviders = providers };
    }


    [InteriorProvider("fixture.box")]
    public sealed class FixtureBoxInterior : BoxInteriorProvider
    {
    }

    // Deliberately lacks a registration attribute even though its assembly is discovered.
    private sealed class UnregisteredInterior : BoxInteriorProvider
    {
    }
    [InteriorProviderFactory("fixture.family.")]
    public sealed class FixtureFactory : IInteriorProviderFactory,IDisposable
    {
        public List<string> Released { get; }=[];
        public IInteriorProvider Create(string providerId)=>new OwnedBox(providerId,Released);
        public void Dispose()=>Released.Add("factory");
    }
    private sealed class OwnedBox(string id,List<string> released) : BoxInteriorProvider,IDisposable
    { public void Dispose()=>released.Add(id); }

}
