using System.Numerics;
using System.Text;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Gen.Parts.Stubs;
using Campus.Kernel.Stubs;
using Campus.Kernel.Stubs.Foundation;
using GdUnit4;

namespace Campus.Tests;

[TestSuite]
public sealed class D0EntranceHeightReferenceTests
{
    private static readonly FeatureId Feature = new("fixture:building");
    private const string Provider = "interior.stub.library";
    private const string Catalog = """
        {"version":1,"interiors":[{"providerId":"interior.stub.library",
         "featureIds":["fixture:building"],"openFloors":[1],"entrances":[
         {"id":"entry.main","featureId":"fixture:building","outsideLanding":[0,0.25,-2.5],
          "insideLanding":[0,0.5,3],"doorCenter":[0,0.75,0],"facingDeg":0,"clearWidth":1.25,"open":true@HEIGHT@}]}]}
        """;

    [TestCase]
    public void LegacyConfigurePreservesAbsoluteButRejectsPadBeforeMutatingProviders()
    {
        var stub = new LibraryInteriorStub();
        var providers = new Dictionary<string, IInteriorProvider> { [Provider] = stub };
        Throws<JsonException>(() => D0InteriorSetup.Configure(Bytes("pad"), providers));
        Require(stub.FeatureIds.Count == 0, "Missing terrain partially configured the provider.");
        foreach (var reference in new string?[] { null, "absolute" })
        {
            var bindings = D0InteriorSetup.Configure(Bytes(reference), providers);
            var layout = stub.Layout(Context(providers), Feature);
            Require(bindings.Single().AuthoredEntranceCount == 1 && layout.Entrances.Single().Center.Y == .75f &&
                layout.Entrances.Single().Outside.Y == .25f && layout.Entrances.Single().Inside.Y == .5f,
                "The existing two-argument API moved legacy absolute coordinates.");
        }
        Throws<ArgumentNullException>(() => D0InteriorSetup.Configure(Bytes("pad"), providers, null!));
    }

    [TestCase]
    public void ExplicitTerrainResolvesOnceAndBuildConfiguredKeepsAbsoluteCoordinates()
    {
        var stub = new LibraryInteriorStub();
        var providers = new Dictionary<string, IInteriorProvider> { [Provider] = stub };
        var terrain = new ProbeTerrain();
        var bindings = D0InteriorSetup.Configure(Bytes("pad"), providers, terrain);
        Require(terrain.Calls == 1, "One entrance should query its feature pad once during configuration.");
        var context = Context(providers);
        var first = D0InteriorSetup.BuildConfigured(context, bindings).Entries.Single().Entrances.Single();
        var second = D0InteriorSetup.BuildConfigured(context, bindings).Entries.Single().Entrances.Single();
        Require(first == second && first.Center.Y == 2.75f && first.Outside.Y == 2.25f && first.Inside.Y == 2.5f &&
            first.Id == "entry.main" && first.YawDeg == 0 && first.ClearWidth == 1.25f && terrain.Calls == 1,
            "BuildConfigured reparsed or re-offset a resolved entrance.");
    }

    private static byte[] Bytes(string? heightRef) => Encoding.UTF8.GetBytes(Catalog.Replace("@HEIGHT@",
        heightRef is null ? "" : ",\"heightRef\":\"" + heightRef + "\"", StringComparison.Ordinal));

    private static BuildContext Context(IReadOnlyDictionary<string, IInteriorProvider> providers)
    {
        var bounds = new Box3(new(-16, 0, -16), new(16, 8, 16));
        var ring = new PolygonRings([new(-16, -16), new(16, -16), new(16, 16), new(-16, 16)]);
        var world = new StubWorldModel(new StubSpatialIndex(), new InteriorIndexStub(), new StubFidelityModel());
        world.Configure([new WorldFeature(Feature, FeatureSource.Osm, new("building"), new PolygonGeometry(ring))
        { Bounds = bounds, Height = 8, Storeys = 2 }], boundary: ring);
        var map = new MapDescriptor(new("height-test"), new("Test", "Test"), MapKind.Outdoor, 1, 1,
            new(0, 0, 32648, new(-64, -64), new(64, 64)), Vector2.Zero, bounds,
            new(new(-64, -64), new(64, 64)), new(.9f, 30, 3000, -28, 2000, 30, 40, 150),
            new(-1, -1), new(0, 0), "spawn", [],
            new(MapTimeRule.Shared, new(17.5, Season.Autumn), new("Test", "Test"), false, "test"),
            new("sky.json", "backdrop.json", []), new Dictionary<string, string>(), []);
        return new BuildContext { Map = map, World = world, WorldSeed = 1, InteriorProviders = providers };
    }

    private sealed class ProbeTerrain : ITerrain
    {
        public int Calls { get; private set; }
        public float PadY(FeatureId feature)
        {
            Calls++;
            if (feature != Feature) throw new KeyNotFoundException(feature.Value);
            return 2;
        }
        public bool IsFlat(TileKey tile) => throw new NotSupportedException();
        public float HeightAt(float x, float z) => throw new NotSupportedException();
        public void FillTile(TileKey tile, int level, Span<short> cm) => throw new NotSupportedException();
        public float? WaterY(FeatureId water) => throw new NotSupportedException();
        public TerrainFacts FactsAt(float x, float z) => throw new NotSupportedException();
    }

    private static void Throws<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
