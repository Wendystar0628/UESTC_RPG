using System.Text.Json;
using Campus.Gen.World;
using Campus.Gen.Tests.Support;
using Campus.Pipeline.Map;
using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Maps;
using Campus.Kernel.Maps;
using Campus.Kernel.Bake;
using Campus.Kernel.Registry;
using Campus.Tests;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Campus.Systems.Vegetation.Tests;

/// <summary>
/// The vegetation worker consumes the prepared production world and its frozen inputs as is.
/// </summary>
[TestSuite]
public sealed class VegetationContextTests
{
    private const string Module = "vegetation";

    [TestCase]
    [TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void PreparedSourcePreservesTheAuthoredWorldAndClaims()
    {
        var fixture = Fixture.Load();
        using var terrain = new TerrainCacheService(new(Path.GetTempPath(), Live: true, Strict: false, Background: false,
            MapOnly: false, LogicalProcessors: 1, SeedOverride: null));
        var loaded = MapWorldLoading.LoadPrepared(new WorldModelLoaderAdapter(terrain), fixture.Input, fixture.Claims);
        var map = MapWorldLoading.CreateContext(fixture.Input.Entry, loaded, fixture.Input.SourceInputs);
        var prepared = new BuildContext
        {
            World = map.World,
            Map = map.Descriptor,
            WorldSeed = map.Descriptor.Seed,
            Catalog = Palette(),
            SourceInputs = fixture.Input.SourceInputs.Entries,
            ClaimedBy = fixture.Claims.Build(map.World).ClaimedBy
        };
        var source = new PreparedSource(prepared);
        var context = VegetationContext.Resolve(map, Module, source)();
        var authoredRoads = fixture.Input.SourceInputs.Entries
            .Where(pair => pair.Key.Equals("world/center-east.json", StringComparison.Ordinal))
            .SelectMany(pair => AuthoredRoadIds(pair.Value))
            .ToArray();
        AssertThat(authoredRoads.Length > 0 && authoredRoads.All(id => context.World.Network.Roads.Any(road => road.Id.Value == id))).OverrideFailureMessage("The authored design footway is missing.").IsTrue();
        AssertThat(ReferenceEquals(context.SourceInputs, fixture.Input.SourceInputs.Entries) &&
            context.SourceInputs.Keys.Any(path => path.Equals("world/center-east.json", StringComparison.Ordinal)) &&
            context.SourceInputs.ContainsKey("vegetation/planting.json")).OverrideFailureMessage("The prepared source lost the frozen input version.").IsTrue();
        var (feature, aspect, owner) = fixture.Claims.Sources.Where(s => !s.Global)
            .SelectMany(s => s.Claims.OfType<FeatureClaim>().Select(c => (c.Feature, Aspect: Lowest(c.Aspects), s.Owner)))
            .First(c => context.World.Features.ContainsKey(c.Feature));
        AssertThat(context.ClaimedBy(feature, aspect) == owner).OverrideFailureMessage("The prepared source lost registered claims.").IsTrue();
        AssertThat(ReferenceEquals(context, prepared) && source.Module == Module && ReferenceEquals(source.Context, map)).OverrideFailureMessage("Prepared context identity or source query changed.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void AFailingFormalSourceThrowsAndNeverFallsBackToAnotherWorld()
    {
        var map = new TestMapContext(RealMapFixture.Entry().Descriptor, new FakeWorld());
        var unsupported = new FailingSource(new NotSupportedException());
        AssertThrown(() => VegetationContext.Resolve(map, Module, unsupported)).IsInstanceOf<NotSupportedException>();
        AssertThat(unsupported.Calls == 1).IsTrue();
        var stale = new FailingSource(new InvalidOperationException());
        AssertThrown(() => VegetationContext.Resolve(map, Module, stale)).IsInstanceOf<InvalidOperationException>();
        AssertThat(stale.Calls == 1).IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void APreparedContextIsReusedWithItsCatalogAndSnapshots()
    {
        var inputs = BuildInputSnapshot.Freeze(new Dictionary<string, ReadOnlyMemory<byte>>(StringComparer.Ordinal)
        {
            ["snapshot.json"] = "{}"u8.ToArray()
        });
        var map = new TestMapContext(RealMapFixture.Entry().Descriptor, new FakeWorld(), inputs);
        var palette = Palette();
        var prepared = new BuildContext
        {
            Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog,
            World = map.World,
            Map = map.Descriptor,
            WorldSeed = 77,
            SourceInputs = map.SourceInputs.Entries,
            ClaimedBy = (_, _) => new WpId("WP-99"),
            DataHash = "data",
            CodeHash = "code"
        };
        var source = new PreparedSource(prepared);
        var context = VegetationContext.Resolve(map, Module, source)();
        AssertThat(source.Module == Module && ReferenceEquals(source.Context, map)).OverrideFailureMessage("The prepared query used another map or module.").IsTrue();
        AssertThat(ReferenceEquals(context.World, prepared.World) && ReferenceEquals(context.SourceInputs, prepared.SourceInputs) &&
              ReferenceEquals(context.ClaimedBy, prepared.ClaimedBy) && context.WorldSeed == 77 && context.DataHash == "data" &&
              context.CodeHash == "code" && ReferenceEquals(context.Catalog, prepared.Catalog)).OverrideFailureMessage("The prepared context lost a field or replaced the palette snapshot.").IsTrue();
        var own = new BuildContext { World = prepared.World, Map = prepared.Map, WorldSeed = 1, Catalog = palette };
        AssertThat(ReferenceEquals(VegetationContext.Resolve(map, Module, new PreparedSource(own))(), own)).OverrideFailureMessage("A context with a palette must be returned as is.").IsTrue();
    }

    private static Aspects Lowest(Aspects aspects) => Enum.GetValues<Aspects>().First(bit => (aspects & bit) != 0);
    private static string[] AuthoredRoadIds(ReadOnlyMemory<byte> bytes)
    {
        using var document = JsonDocument.Parse(bytes);
        return document.RootElement.GetProperty("features").EnumerateArray()
            .Where(op => op.GetProperty("id").GetString()!.StartsWith("design:", StringComparison.Ordinal) && op.GetProperty("kind").GetString() == "road" && op.GetProperty("geometry").GetProperty("type").GetString() == "polyline")
            .Select(op => op.GetProperty("id").GetString()!).ToArray();
    }
    private static PaletteCatalog Palette() => Campus.Contracts.Tests.PaletteFixture.Catalog;

    private sealed class FailingSource(Exception failure) : IBakeContextSource
    {
        public Task<IReadOnlyDictionary<string, IReadOnlyList<TileKey>>> GetPreparedModuleTilesAsync(IMapContext context) => throw failure;
        public int Calls { get; private set; }
        public BuildContext GetPreparedContext(IMapContext context, string moduleId)
        {
            Calls++;
            throw failure;
        }
    }

    private sealed class PreparedSource(BuildContext prepared) : IBakeContextSource
    {
        public Task<IReadOnlyDictionary<string, IReadOnlyList<TileKey>>> GetPreparedModuleTilesAsync(IMapContext context) =>
            throw new InvalidOperationException("Vegetation context resolution must not enumerate module tiles.");
        public IMapContext? Context { get; private set; }
        public string? Module { get; private set; }
        public BuildContext GetPreparedContext(IMapContext context, string moduleId)
        {
            (Context, Module) = (context, moduleId);
            return prepared;
        }
    }

    /// <summary>Real qingshuihe input and registered claims.</summary>
    private sealed record Fixture(MapLoadInput Input, MapClaimSnapshot Claims)
    {
        public static Fixture Load()
        {
            var entry = Campus.Tests.RealMapFixture.Entry();
            var id = entry.Descriptor.Id;
            var all = Campus.Tests.RealMapFixture.ReadFiles();
            var assemblies = new[] { "Campus.Gen", "Campus.Gen.Maps.Qingshuihe" }
                .Select(System.Reflection.Assembly.Load).ToArray();
            var registry = new ServiceRegistry();
            registry.Discover(assemblies);
            var claims = MapClaimCapture.Capture(registry, id);
            return new(new MapLoadInput(entry, all), claims);
        }
    }
}
