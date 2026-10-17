using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Maps;
using Campus.Kernel.Maps;
using Campus.Kernel.Registry;
using Campus.Kernel.Stubs.Foundation;
using GdUnit4;

namespace Campus.Systems.Vegetation.Tests;

/// <summary>
/// Where the vegetation worker's BuildContext comes from: the D0 stub source takes the effective world (base files, every overlay,
/// the frozen source snapshot and the registered claims); a formal source is used as is and fails loudly.
/// </summary>
[TestSuite]
public sealed class VegetationContextTests
{
    private const string Module = "vegetation";
    private const string DesignRoad = "design:center-east-riverside-walk";

    [TestCase]
    public void D0StubSourceReadsTheEffectiveWorldNotTheFourBaseFiles()
    {
        var fixture = Fixture.Load();
        var source = new FailingSource(new NotSupportedException("stub"));
        var reads = 0;
        var resolve = VegetationContext.Resolve(fixture.Map, Module, source, sourceIsD0Stub: true, Palette(), BuildInputSnapshot.Empty,
            new WorldModelLoaderAdapter(), () => { reads++; return (fixture.Input, fixture.Claims); });
        Check(source.Calls == 0 && reads == 1, "The D0 branch asked the stub source or did not read the map inputs once.");
        Check(!fixture.Map.World.Network.Roads.Any(road => road.Id.Value == DesignRoad),
            "Fixture error: the base-only world already holds the design footway.");
        var context = resolve();
        Check(context.World.Network.Roads.Any(road => road.Id.Value == DesignRoad), "The overlay's design footway is missing from the world.");
        Check(ReferenceEquals(context.SourceInputs, fixture.Input.SourceInputs.Entries) &&
              context.SourceInputs.Keys.Any(path => path.StartsWith("overlay/center-east.d/", StringComparison.Ordinal)) &&
              context.SourceInputs.ContainsKey("vegetation/planting.json"), "The context lost the frozen source snapshot.");
        var (feature, aspect, owner) = fixture.Claims.Sources.Where(s => !s.Global)
            .SelectMany(s => s.Claims.OfType<FeatureClaim>().Select(c => (c.Feature, Aspect: Lowest(c.Aspects), s.Owner)))
            .First(c => context.World.Features.ContainsKey(c.Feature));
        Check(context.ClaimedBy(feature, aspect) == owner, "The claims ledger was not carried into the context.");
        Check(context.Map.Id == fixture.Map.Descriptor.Id && context.WorldSeed == fixture.Map.Descriptor.Seed, "Map identity or seed differs.");
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
                VegetationContext.Resolve(fixture.Map, Module, source, sourceIsD0Stub: false, Palette(), BuildInputSnapshot.Empty,
                    new ThrowingLoader(), () => throw new InvalidOperationException("The map inputs were read on the formal branch."));
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
        var context = VegetationContext.Resolve(fixture.Map, Module, source, sourceIsD0Stub: false, palette, BuildInputSnapshot.Empty,
            new ThrowingLoader(), () => throw new InvalidOperationException("The map inputs were read on the formal branch."))();
        Check(source.Module == Module && ReferenceEquals(source.Context, fixture.Map), "The prepared query used another map or module.");
        Check(ReferenceEquals(context.World, prepared.World) && ReferenceEquals(context.SourceInputs, prepared.SourceInputs) &&
              ReferenceEquals(context.ClaimedBy, prepared.ClaimedBy) && context.WorldSeed == 77 && context.DataHash == "data" &&
              context.CodeHash == "code" && ReferenceEquals(context.Catalog, prepared.Catalog), "The prepared context lost a field or replaced the palette snapshot.");
        var own = new BuildContext { World = prepared.World, Map = prepared.Map, WorldSeed = 1, Catalog = palette };
        Check(ReferenceEquals(VegetationContext.Resolve(fixture.Map, Module, new PreparedSource(own), false, Palette(), BuildInputSnapshot.Empty,
            new ThrowingLoader(), () => throw new InvalidOperationException())(), own), "A context with a palette must be returned as is.");
    }

    [TestCase]
    public void TheD0BranchIsChosenByTheRegisteredSourceBeingAStubNotByCatchingErrors()
    {
        var stub = new ServiceRegistry();
        stub.Discover([typeof(BakeContextSourceStub).Assembly]);
        Check(VegetationContext.SourceIsD0Stub(stub), "The registered D0 stub was not recognised.");
        var formal = new ServiceRegistry();
        formal.Discover([typeof(FormalSource).Assembly]);
        Check(!VegetationContext.SourceIsD0Stub(formal), "A formal source was taken for the D0 stub.");
        Check(!VegetationContext.SourceIsD0Stub(new ServiceRegistry()), "A registry without the service counts as the D0 stub.");
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

    [KernelService(typeof(IBakeContextSource), Owner = "WP-02a")]
    private sealed class FormalSource : IBakeContextSource
    {
        public BuildContext GetPreparedContext(IMapContext context, string moduleId) => throw new NotSupportedException();
    }

    private sealed class ThrowingLoader : IWorldModelLoader
    {
        public WorldModelLoadResult Load(MapId map, IReadOnlyDictionary<string, ReadOnlyMemory<byte>> files) =>
            throw new InvalidOperationException("A world was loaded on the formal branch.");
    }

    /// <summary>The real qingshuihe data: a base-only map context (what D0 holds), the full load input and the registered claims.</summary>
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
