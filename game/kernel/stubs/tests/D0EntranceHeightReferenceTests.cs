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

    [TestCase]
    public void ClosedOnlyD0BuildPublishesIndexWithoutProvidersOrWalkableContent()
    {
        var context = Context(new Dictionary<string, IInteriorProvider>());
        var catalog = Encoding.UTF8.GetBytes(ClosedCatalog());
        var bindings = D0InteriorSetup.Configure(catalog, context.InteriorProviders, context.World.Terrain);
        Require(bindings.Count == 0, "Closed entrance configured a provider.");
        var result = D0InteriorSetup.BuildConfigured(context, bindings, catalog);
        var entry = result.Entries.Single();
        Require(entry.Feature == Feature && !entry.Open && entry.ProviderId is null && entry.Storeys.Count == 2 &&
            entry.Storeys[1].FloorY == 4 && entry.Entrances.Single().Center.Y == .15f,
            "D0 lost closed entrance or original building storeys.");
        Require(result.Content.Meshes.Count == 0 && result.Colliders.Count == 0 && result.NavNodes.Count == 0 &&
            result.NavEdges.Count == 0 && result.NavPortals.Count == 0, "Closed register generated walkable content.");
        var index = (InteriorIndexStub)context.World.Interiors;
        index.Configure(result.Entries);
        Require(index.Get(Feature) == entry && index.EntrancesNear(new(0, .15f, 0), 1).Single().Id == "entry.closed",
            "D0 final configured index omitted closed registration.");
        var again = D0InteriorSetup.BuildConfigured(context, bindings, catalog);
        Require(ReferenceEquals(again.Entries.Single().Storeys, entry.Storeys) &&
            again.Entries.Single().Entrances.Count == 1, "Existing closed index duplicated doors or replaced storeys.");
        Require(D0InteriorSetup.Build(catalog, context, context.InteriorProviders).Entries.Single().Entrances.Count == 1,
            "Legacy D0 Build omitted the closed register.");
        var terrain = new ProbeTerrain();
        using var pad = JsonDocument.Parse(ClosedCatalog(extra: ",\"heightRef\":\"pad\",\"headY\":2"));
        var resolved = InteriorEntranceJson.ParseClosedEntrances(pad.RootElement, terrain)[Feature].Single();
        Require(terrain.Calls == 1 && resolved.Center.Y == 2.15f && resolved.HeadY == 4,
            "Closed parsing did not reuse explicit pad/headY semantics.");
    }

    [TestCase]
    public void ClosedRegisterDoesNotChangeOpenProviderCallsOrNavigation()
    {
        var fallback = new LibraryInteriorStub();
        var sourceProviders = new Dictionary<string, IInteriorProvider> { [Provider] = fallback };
        var bindings = D0InteriorSetup.Configure(Bytes(null), sourceProviders);
        var probe = new CountingProvider(fallback);
        var context = Context(new Dictionary<string, IInteriorProvider> { [Provider] = probe });
        var closedFeature = new FeatureId("fixture:closed");
        var source = context.World.Features[Feature];
        ((StubWorldModel)context.World).Configure([source, source with { Id = closedFeature }]);
        var baseline = D0InteriorSetup.BuildConfigured(context, bindings, Bytes(null));
        int baselineCalls = probe.Calls;
        probe.Calls = 0;
        using var closed = JsonDocument.Parse(ClosedCatalog(feature: closedFeature.Value));
        var raw = closed.RootElement.GetProperty("closedEntrances").GetRawText();
        var mixed = Encoding.UTF8.GetString(Bytes(null)).TrimEnd();
        mixed = mixed[..^1] + ",\"closedEntrances\":" + raw + "}";
        var result = D0InteriorSetup.BuildConfigured(context, bindings, Encoding.UTF8.GetBytes(mixed));
        Require(probe.Calls == baselineCalls && result.Content.Meshes.Count == baseline.Content.Meshes.Count &&
            result.Colliders.Count == baseline.Colliders.Count && result.NavNodes.SequenceEqual(baseline.NavNodes) &&
            result.NavEdges.SequenceEqual(baseline.NavEdges) && result.NavPortals.SequenceEqual(baseline.NavPortals) &&
            result.Entries.Single(entry => entry.Feature == Feature).Entrances.SequenceEqual(baseline.Entries.Single().Entrances) &&
            result.Entries.Single(entry => entry.Feature == Feature).ProviderId == Provider &&
            !result.Entries.Single(entry => entry.Feature == closedFeature).Open,
            "Closed registration changed open provider generation or navigation.");
    }

    private sealed class CountingProvider(IInteriorProvider inner) : IInteriorProvider
    {
        public int Calls { get; set; }
        public IReadOnlyList<FeatureId> FeatureIds => inner.FeatureIds;
        public int Priority => inner.Priority;
        public InteriorLayout Layout(BuildContext ctx, FeatureId id) { Calls++; return inner.Layout(ctx, id); }
        public ContentOutput BuildStorey(BuildContext ctx, FeatureId id, int storey, InteriorDetail detail)
        { Calls++; return inner.BuildStorey(ctx, id, storey, detail); }
        public IReadOnlyList<Collider> CollisionStorey(BuildContext ctx, FeatureId id, int storey)
        { Calls++; return inner.CollisionStorey(ctx, id, storey); }
        public NavFloorGraph Nav(BuildContext ctx, FeatureId id, int storey) { Calls++; return inner.Nav(ctx, id, storey); }
    }

    [TestCase]
    public void InvalidD0ClosedClaimsFailBeforeProviderGeneration()
    {
        var context = Context(new Dictionary<string, IInteriorProvider>());
        foreach (var catalog in new[]
        {
            ClosedCatalog().Replace("\"open\":false", "\"open\":true", StringComparison.Ordinal),
            ClosedCatalog(feature: "missing"),
            ClosedCatalog(extra: ",\"heightRef\":\"unknown\""),
            ClosedCatalog().Replace("}]}", "}," + ClosedCatalog().Split("\"closedEntrances\":[", StringSplitOptions.None)[1], StringComparison.Ordinal)
        })
            Throws<JsonException>(() => D0InteriorSetup.BuildConfigured(context,
                [new("must-not-be-called", [Feature], [1], 0)], Encoding.UTF8.GetBytes(catalog)));
        var source = context.World.Features[Feature];
        ((StubWorldModel)context.World).Configure([source with { Kind = new("water") }]);
        Throws<JsonException>(() => D0InteriorSetup.BuildConfigured(context, [], Encoding.UTF8.GetBytes(ClosedCatalog())));
    }

    private static string ClosedCatalog(string feature = "fixture:building", string extra = "") =>
        "{\"version\":1,\"interiors\":[],\"closedEntrances\":[{\"id\":\"entry.closed\",\"featureId\":\"" + feature +
        "\",\"doorCenter\":[0,0.15,0],\"outsideLanding\":[0,0.15,-1],\"insideLanding\":[0,0.15,1],\"facingDeg\":0,\"clearWidth\":2.4,\"open\":false" + extra + "}]}";

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
        return new BuildContext { Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog, Map = map, World = world, WorldSeed = 1, InteriorProviders = providers };
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
