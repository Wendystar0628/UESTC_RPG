using System.Collections.ObjectModel;
using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Gen.Tests.Support;
using Campus.Tests;
using GdUnit4;

namespace Campus.Gen.Collision.Tests;

using static GdUnit4.Assertions;

[TestSuite]
public sealed class CollisionInteriorIsolationTests
{
    private static readonly PaletteCatalog Catalog = new(new PaletteRegistry([]), []);
    private const string ProviderId = "interior.fixture";
    private static readonly FeatureId Feature = new("fixture:shared-building");

    [TestCase(Timeout = 10000)]
    public async Task InFlightOldMapUsesItsOwnProviderWhileNewMapBuilds()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var release = new ManualResetEventSlim();
        var oldMap = new MapId("fixture-old");
        var newMap = new MapId("fixture-new");
        var oldProvider = new FixtureProvider(oldMap, 3, [Feature], () =>
        {
            entered.TrySetResult();
            if (!release.Wait(TimeSpan.FromSeconds(5))) throw new InvalidOperationException("Old-map test worker was not released.");
        });
        var newProvider = new FixtureProvider(newMap, 7, [Feature]);
        var oldContext = Context(oldMap, Bind(oldProvider), open: true, catalog: true);
        var builder = new CollisionTileBuilder([], _ => []);
        var oldJob = Task.Run(() => builder.Build(oldContext, new(0, 0)));
        IReadOnlyList<Collider>? oldResult = null;
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            // This is the map transition boundary: a new context binds another instance under
            // the same provider/feature IDs while the old build is still inside CollisionStorey.
            var newContext = Context(newMap, Bind(newProvider), open: true, catalog: true);
            var newResult = builder.Build(newContext, new(0, 0));
            AssertThat(newResult.Count != 1 || newResult.Any(c => c is not BoxCollider box || MathF.Abs(box.At.M42 + box.Half.Y - 7) > .02f))
                .OverrideFailureMessage("Expected one box floor at 7 m from this map's provider.").IsFalse();
        }
        finally
        {
            release.Set();
            oldResult = await oldJob;
        }
        AssertThat(oldResult).OverrideFailureMessage("Old-map worker did not complete.").IsNotNull();
        AssertThat(oldResult!.Count != 1 || oldResult!.Any(c => c is not BoxCollider box || MathF.Abs(box.At.M42 + box.Half.Y - 3) > .02f))
            .OverrideFailureMessage("Expected one box floor at 3 m from this map's provider.").IsFalse();
    }

    [TestCase]
    public void OpenIndexedInteriorCannotUseAnEmptyProviderSnapshot()
    {
        var context = Context(new("fixture-missing"), EmptyBindings(), open: true, catalog: false);
        Exception? error = null;
        try { new CollisionTileBuilder([], _ => []).Build(context, new(0, 0)); }
        catch (Exception caught) { error = caught; }
        AssertThat(error).OverrideFailureMessage("Expected an explicit interior binding error.").IsNotNull();
        AssertThat(error!).IsInstanceOf<InvalidOperationException>();
    }

    [TestCase]
    public void CatalogBindingCannotDisappearBehindAClosedOrIncompleteIndex()
    {
        var context = Context(new("fixture-catalog"), EmptyBindings(), open: false, catalog: true);
        Exception? error = null;
        try { new CollisionTileBuilder([], _ => []).Build(context, new(0, 0)); }
        catch (Exception caught) { error = caught; }
        AssertThat(error).OverrideFailureMessage("Expected an explicit interior binding error.").IsNotNull();
        AssertThat(error!).IsInstanceOf<InvalidOperationException>();
    }

    [TestCase]
    public void UnconfiguredProviderDoesNotSilentlyProduceAnEmptyInterior()
    {
        var map = new MapId("fixture-unconfigured");
        var context = Context(map, Bind(new FixtureProvider(map, 3, [])), open: true, catalog: true);
        Exception? error = null;
        try { new CollisionTileBuilder([], _ => []).Build(context, new(0, 0)); }
        catch (Exception caught) { error = caught; }
        AssertThat(error).OverrideFailureMessage("Expected an explicit interior binding error.").IsNotNull();
        AssertThat(error!).IsInstanceOf<InvalidOperationException>();
    }

    [TestCase]
    public void MapWithoutDeclaredOrOpenInteriorsAllowsLegacyEmptySnapshot()
    {
        var map = new MapId("fixture-no-interiors");
        var context = new BuildContext { Map = Descriptor(map), World = FixtureWorld(open: false), WorldSeed = 1, Catalog = Catalog };
        var result = new CollisionTileBuilder([], _ => []).Build(context, new(0, 0));
        AssertThat(result.Count == 0).OverrideFailureMessage("A map without interior declarations produced extra collision shapes.").IsTrue();
    }

    private static BuildContext Context(MapId map, IReadOnlyDictionary<string, IInteriorProvider> providers, bool open, bool catalog)
    {
        var sources = new Dictionary<string, ReadOnlyMemory<byte>>(StringComparer.Ordinal);
        if (catalog) sources.Add("interiors.json", JsonSerializer.SerializeToUtf8Bytes(new
        {
            version = 1,
            interiors = new[] { new { providerId = ProviderId, featureIds = new[] { Feature.Value }, openFloors = new[] { 1 } } }
        }));
        return new BuildContext
        {
            Map = Descriptor(map),
            World = FixtureWorld(open),
            WorldSeed = 1,
            Catalog = Catalog,
            InteriorProviders = providers,
            SourceInputs = new ReadOnlyDictionary<string, ReadOnlyMemory<byte>>(sources)
        };
    }
    private static IReadOnlyDictionary<string, IInteriorProvider> Bind(IInteriorProvider provider) =>
        new ReadOnlyDictionary<string, IInteriorProvider>(new Dictionary<string, IInteriorProvider>(StringComparer.Ordinal) { [ProviderId] = provider });
    private static IReadOnlyDictionary<string, IInteriorProvider> EmptyBindings() =>
        new ReadOnlyDictionary<string, IInteriorProvider>(new Dictionary<string, IInteriorProvider>(StringComparer.Ordinal));

    private static MapDescriptor Descriptor(MapId map) => new(map, new("室内隔离测试", "Interior isolation fixture"), MapKind.Instance, 1, 1,
        new(0, 0, 0, Vector2.Zero, new(64, 64)), new(32, 32), new(new(0, -5, 0), new(64, 20, 64)),
        new(Vector2.Zero, new(64, 64)), new(1, 0, 64, -5, 20, 0, 0, 0), new(0, 0), new(0, 0),
        "fixture", [], new(MapTimeRule.Shared, new(12, Season.Autumn), new("测试", "Fixture"), false, "fixture-weather"),
        new("fixture-sky", "fixture-backdrop", []), new Dictionary<string, string>(), []);

    // The aggregate is a non-building marker so this fixture isolates the provider's floor, not automatic exterior walls.
    private static FakeWorld FixtureWorld(bool open) => new()
    {
        Boundary = new([new(0, 0), new(64, 0), new(64, 64), new(0, 64)]),
        Features = new ReadOnlyDictionary<FeatureId, WorldFeature>(
            new Dictionary<FeatureId, WorldFeature> { [Feature] = WorldFeatureFixture.Create(Feature.Value, "poi", new PointGeometry(new(2, 0, 2))) }),
        Spatial = new FlatSpatial(forbidden: "This isolation test must not fabricate ground support."),
        Interiors = new FakeInteriors(new InteriorIndexEntry(Feature, [], [], open, open ? ProviderId : null)),
    };
    private sealed class FixtureProvider(MapId map, float height, IReadOnlyList<FeatureId> features, Action? beforeCollision = null) : IInteriorProvider
    {
        private static readonly Vector2[] Ring = [new(1, 1), new(3, 1), new(3, 3), new(1, 3)];
        public IReadOnlyList<FeatureId> FeatureIds => features;
        public int Priority => 1;
        public InteriorLayout Layout(BuildContext context, FeatureId id)
        {
            CheckMap(context);
            return new(id, [new(1, height, 3, Ring)], [], [], [], [], [1], [], new(new(1, height - 0.2f, 1), new(3, height + 3, 3)), 1);
        }
        public IReadOnlyList<Collider> CollisionStorey(BuildContext context, FeatureId id, int storey)
        {
            CheckMap(context); beforeCollision?.Invoke();
            return [new SlabCollider(Ring, height, 0.2f, SurfaceTag.Ground)];
        }
        public ContentOutput BuildStorey(BuildContext context, FeatureId id, int storey, InteriorDetail detail)
        { CheckMap(context); return ContentOutput.Empty; }
        private void CheckMap(BuildContext context)
        {
            if (context.Map.Id != map) throw new InvalidOperationException("A provider from another map was reused.");
        }
    }
}
