using System.Text.Json;
using Campus.Gen.World;
using Campus.Pipeline.Map;
using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Maps;
using Campus.Kernel.Maps;
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
    public void PreparedSourcePreservesTheEffectiveWorldOverlaysAndClaims()
    {
        var fixture = Fixture.Load();
        var loaded = MapWorldLoading.LoadPrepared(new WorldModelLoaderAdapter(), fixture.Input, fixture.Claims);
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
        var overlayRoads = fixture.Input.SourceInputs.Entries
            .Where(pair => pair.Key.StartsWith("overlay/center-east.d/", StringComparison.Ordinal))
            .SelectMany(pair => AddedRoadIds(pair.Value))
            .ToArray();
        AssertThat(overlayRoads.Length > 0 && overlayRoads.All(id => context.World.Network.Roads.Any(road => road.Id.Value == id))).OverrideFailureMessage("The overlay's design footway is missing.").IsTrue();
        AssertThat(ReferenceEquals(context.SourceInputs, fixture.Input.SourceInputs.Entries) &&
            context.SourceInputs.Keys.Any(path => path.StartsWith("overlay/center-east.d/", StringComparison.Ordinal)) &&
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
        var fixture = Fixture.Load();
        var unsupported = new FailingSource(new NotSupportedException());
        AssertThrown(() => VegetationContext.Resolve(fixture.Map, Module, unsupported)).IsInstanceOf<NotSupportedException>();
        AssertThat(unsupported.Calls == 1).IsTrue();
        var stale = new FailingSource(new InvalidOperationException());
        AssertThrown(() => VegetationContext.Resolve(fixture.Map, Module, stale)).IsInstanceOf<InvalidOperationException>();
        AssertThat(stale.Calls == 1).IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void APreparedContextIsReusedWithItsCatalogAndSnapshots()
    {
        var fixture = Fixture.Load();
        var palette = Palette();
        var prepared = new BuildContext
        {
            Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog,
            World = fixture.Map.World,
            Map = fixture.Map.Descriptor,
            WorldSeed = 77,
            SourceInputs = fixture.Input.SourceInputs.Entries,
            ClaimedBy = (_, _) => new WpId("WP-99"),
            DataHash = "data",
            CodeHash = "code"
        };
        var source = new PreparedSource(prepared);
        var context = VegetationContext.Resolve(fixture.Map, Module, source)();
        AssertThat(source.Module == Module && ReferenceEquals(source.Context, fixture.Map)).OverrideFailureMessage("The prepared query used another map or module.").IsTrue();
        AssertThat(ReferenceEquals(context.World, prepared.World) && ReferenceEquals(context.SourceInputs, prepared.SourceInputs) &&
              ReferenceEquals(context.ClaimedBy, prepared.ClaimedBy) && context.WorldSeed == 77 && context.DataHash == "data" &&
              context.CodeHash == "code" && ReferenceEquals(context.Catalog, prepared.Catalog)).OverrideFailureMessage("The prepared context lost a field or replaced the palette snapshot.").IsTrue();
        var own = new BuildContext { World = prepared.World, Map = prepared.Map, WorldSeed = 1, Catalog = palette };
        AssertThat(ReferenceEquals(VegetationContext.Resolve(fixture.Map, Module, new PreparedSource(own))(), own)).OverrideFailureMessage("A context with a palette must be returned as is.").IsTrue();
    }

    private static Aspects Lowest(Aspects aspects) => Enum.GetValues<Aspects>().First(bit => (aspects & bit) != 0);
    private static string[] AddedRoadIds(ReadOnlyMemory<byte> bytes)
    {
        using var document = JsonDocument.Parse(bytes);
        return document.RootElement.GetProperty("ops").EnumerateArray()
            .Where(op => op.GetProperty("op").GetString() == "add" && op.GetProperty("kind").GetString() == "road")
            .Select(op => op.GetProperty("id").GetString()!).ToArray();
    }
    private static PaletteCatalog Palette() => Campus.Contracts.Tests.PaletteFixture.Catalog;

    private sealed class FailingSource(Exception failure) : IBakeContextSource
    {
        public int Calls { get; private set; }
        public BuildContext GetPreparedContext(IMapContext context, string moduleId)
        {
            Calls++;
            throw failure;
        }
    }

    private sealed class PreparedSource(BuildContext prepared) : IBakeContextSource
    {
        public IMapContext? Context { get; private set; }
        public string? Module { get; private set; }
        public BuildContext GetPreparedContext(IMapContext context, string moduleId)
        {
            (Context, Module) = (context, moduleId);
            return prepared;
        }
    }

    /// <summary>Real qingshuihe input and registered claims.</summary>
    private sealed record Fixture(IMapContext Map, MapLoadInput Input, MapClaimSnapshot Claims)
    {
        public static Fixture Load()
        {
            var entry = Campus.Tests.RealMapFixture.Entry();
            var id = entry.Descriptor.Id;
            var all = Campus.Tests.RealMapFixture.ReadFiles();
            var baseFiles = all.Where(pair => !pair.Key.StartsWith("overlay/", StringComparison.Ordinal))
                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
            var loader = new WorldModelLoaderAdapter();
            var baseInput = new MapLoadInput(entry, baseFiles);
            var map = MapWorldLoading.CreateContext(entry, MapWorldLoading.LoadPrepared(loader, baseInput), baseInput.SourceInputs);
            var assemblies = new[] { "Campus.Gen", "Campus.Gen.Maps.Qingshuihe" }
                .Select(System.Reflection.Assembly.Load).ToArray();
            var registry = new ServiceRegistry();
            registry.Discover(assemblies);
            var claims = MapClaimCapture.Capture(registry, id);
            return new(map, new MapLoadInput(entry, all), claims);
        }
    }
}
