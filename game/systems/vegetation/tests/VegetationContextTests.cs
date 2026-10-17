using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Maps;
using Campus.Kernel.Maps;
using Campus.Kernel.Registry;
using GdUnit4;

namespace Campus.Systems.Vegetation.Tests;

/// <summary>
/// The vegetation worker consumes the prepared production world and its frozen inputs as is.
/// </summary>
[TestSuite]
public sealed class VegetationContextTests
{
    private const string Module = "vegetation";
    private const string DesignRoad = "design:center-east-riverside-walk";

    [TestCase]
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
        Check(context.World.Network.Roads.Any(road => road.Id.Value == DesignRoad), "The overlay's design footway is missing.");
        Check(ReferenceEquals(context.SourceInputs, fixture.Input.SourceInputs.Entries) &&
            context.SourceInputs.Keys.Any(path => path.StartsWith("overlay/center-east.d/", StringComparison.Ordinal)) &&
            context.SourceInputs.ContainsKey("vegetation/planting.json"), "The prepared source lost the frozen input version.");
        var (feature, aspect, owner) = fixture.Claims.Sources.Where(s => !s.Global)
            .SelectMany(s => s.Claims.OfType<FeatureClaim>().Select(c => (c.Feature, Aspect: Lowest(c.Aspects), s.Owner)))
            .First(c => context.World.Features.ContainsKey(c.Feature));
        Check(context.ClaimedBy(feature, aspect) == owner, "The prepared source lost registered claims.");
        Check(ReferenceEquals(context, prepared) && source.Module == Module && ReferenceEquals(source.Context, map),
            "Prepared context identity or source query changed.");
    }

    [TestCase]
    public void AFailingFormalSourceThrowsAndNeverFallsBackToAnotherWorld()
    {
        var fixture = Fixture.Load();
        foreach (var failure in new Exception[] { new NotSupportedException("not prepared"), new InvalidOperationException("stale context") })
        {
            var source = new FailingSource(failure);
            var thrown = false;
            try
            {
                VegetationContext.Resolve(fixture.Map, Module, source);
            }
            catch (Exception error) when (ReferenceEquals(error, failure)) { thrown = true; }
            Check(thrown && source.Calls == 1, $"{failure.GetType().Name} from the formal source did not surface unchanged.");
        }
    }

    [TestCase]
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
        Check(source.Module == Module && ReferenceEquals(source.Context, fixture.Map), "The prepared query used another map or module.");
        Check(ReferenceEquals(context.World, prepared.World) && ReferenceEquals(context.SourceInputs, prepared.SourceInputs) &&
              ReferenceEquals(context.ClaimedBy, prepared.ClaimedBy) && context.WorldSeed == 77 && context.DataHash == "data" &&
              context.CodeHash == "code" && ReferenceEquals(context.Catalog, prepared.Catalog), "The prepared context lost a field or replaced the palette snapshot.");
        var own = new BuildContext { World = prepared.World, Map = prepared.Map, WorldSeed = 1, Catalog = palette };
        Check(ReferenceEquals(VegetationContext.Resolve(fixture.Map, Module, new PreparedSource(own))(), own), "A context with a palette must be returned as is.");
    }

    private static Aspects Lowest(Aspects aspects) => Enum.GetValues<Aspects>().First(bit => (aspects & bit) != 0);
    private static PaletteCatalog Palette() => Campus.Contracts.Tests.PaletteFixture.Catalog;

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

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
            var data = DataDirectory();
            var id = new MapId("qingshuihe");
            var dir = "res://data/maps/" + id.Value + "/";
            var all = new SortedDictionary<string, ReadOnlyMemory<byte>>(StringComparer.Ordinal);
            foreach (var path in Directory.EnumerateFiles(data, "*.json", SearchOption.AllDirectories))
                all[Path.GetRelativePath(data, path).Replace('\\', '/')] = File.ReadAllBytes(path);
            var entry = MapCatalog.ReadEntry(new MapIndexEntry(id, dir), all["map.json"].Span, all["campus.json"], all["waypoints.json"].Span);
            var baseFiles = new[] { "campus.json", "network.json", "interiors.json", "map.json" }
                .ToDictionary(name => name, name => all[name], StringComparer.Ordinal);
            var loader = new WorldModelLoaderAdapter();
            var baseInput = new MapLoadInput(entry, baseFiles, ReadOnlyMemory<byte>.Empty);
            var map = entry.CreateContext(MapWorldLoading.LoadPrepared(loader, baseInput), baseInput.SourceInputs);
            var assemblies = Directory.EnumerateFiles(AppContext.BaseDirectory, "Campus.Gen.*.dll")
                .Where(path => !Path.GetFileName(path).StartsWith("Campus.Gen.Tests", StringComparison.Ordinal))
                .Order(StringComparer.Ordinal).Select(path => System.Reflection.Assembly.LoadFrom(path)).ToArray();
            var registry = new ServiceRegistry();
            registry.Discover(assemblies);
            var claims = MapClaimLedger.Capture(registry, id);
            return new(map, new MapLoadInput(entry, all, ReadOnlyMemory<byte>.Empty), claims);
        }

        private static string DataDirectory()
        {
            for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
            {
                var data = Path.Combine(directory.FullName, "game", "data", "maps", "qingshuihe");
                if (File.Exists(Path.Combine(data, "campus.json"))) return data;
            }
            throw new DirectoryNotFoundException("Real campus data is missing.");
        }
    }
}
