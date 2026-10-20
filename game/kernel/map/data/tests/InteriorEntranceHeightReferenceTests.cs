using Campus.Gen.Tests.Support;
using static GdUnit4.Assertions;
using Campus.Gen.BakeInput;
using System.Numerics;
using System.Text;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Maps;
using GdUnit4;
using Campus.Kernel.Maps;

namespace Campus.Tests;

[TestSuite]
public sealed class InteriorEntranceHeightReferenceTests
{
    private static readonly FeatureId Feature = new("fixture:building");
    private const string Provider = "fixture.box";
    private const string Catalog = """
        {"version":2,"interiors":[{"providerId":"fixture.box",
         "featureIds":["fixture:building"],"openParts":[{"featureId":"fixture:building","sourceId":"","floors":[1]}],"entrances":[
         {"id":"entry.main","featureId":"fixture:building","outsideLanding":[0,0.25,-2.5],
          "insideLanding":[0,0.5,3],"doorCenter":[0,0.75,0],"facingDeg":0,"clearWidth":1.25,"open":true@HEIGHT@}]}]}
        """;

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void ConfigurePreservesAbsoluteButRejectsPadBeforeMutatingProviders()
    {
        foreach (var reference in new string?[] { null, "absolute" })
        {
            var box = new InteriorProviderScopeTests.FixtureBoxInterior();
            var providers = new Dictionary<string, IInteriorProvider> { [Provider] = box };
            AssertThrown(() => InteriorAssembly.Configure(Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(Bytes(reference))
                .Replace("\"version\":2", "\"version\":1", StringComparison.Ordinal)), providers)).IsInstanceOf<JsonException>();
            AssertThrown(() => InteriorAssembly.Configure(Bytes("pad"), providers)).IsInstanceOf<JsonException>();
            AssertThat(box.FeatureIds.Count == 0).OverrideFailureMessage("Missing terrain partially configured the provider.").IsTrue();
            var bindings = InteriorAssembly.Configure(Bytes(reference), providers);
            var layout = box.Layout(Context(providers), Feature);
            AssertThat(bindings.Single().AuthoredEntranceCount == 1 && layout.Entrances.Single().Center.Y == .75f &&
                layout.Entrances.Single().Outside.Y == .25f && layout.Entrances.Single().Inside.Y == .5f).OverrideFailureMessage("The terrain-free configuration moved absolute coordinates.").IsTrue();
            AssertThrown(() => InteriorAssembly.Configure(Bytes("pad"), providers, null!)).IsInstanceOf<ArgumentNullException>();
        }
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void ExplicitTerrainResolvesOnceAndBuildConfiguredKeepsAbsoluteCoordinates()
    {
        var box = new InteriorProviderScopeTests.FixtureBoxInterior();
        var providers = new Dictionary<string, IInteriorProvider> { [Provider] = box };
        var terrain = new ProbeTerrain();
        var bindings = InteriorAssembly.Configure(Bytes("pad"), providers, terrain);
        AssertThat(terrain.Calls == 1).OverrideFailureMessage("One entrance should query its feature pad once during configuration.").IsTrue();
        var context = Context(providers);
        var firstEntry = InteriorAssembly.BuildConfigured(context, bindings).Entries.Single();
        var secondEntry = InteriorAssembly.BuildConfigured(context, bindings).Entries.Single();
        AssertThat(firstEntry.OpenStoreys.SequenceEqual(bindings.Single().OpenFloors) &&
            secondEntry.OpenStoreys.SequenceEqual(firstEntry.OpenStoreys)).OverrideFailureMessage("Interior assembly lost the provider's authoritative open storeys.").IsTrue();
        var first = firstEntry.Entrances.Single();
        var second = secondEntry.Entrances.Single();
        AssertThat(first == second && first.Center.Y == 2.75f && first.Outside.Y == 2.25f && first.Inside.Y == 2.5f &&
            first.Id == "entry.main" && first.YawDeg == 0 && first.ClearWidth == 1.25f && terrain.Calls == 1).OverrideFailureMessage("BuildConfigured reparsed or re-offset a resolved entrance.").IsTrue();
    }

    private static byte[] Bytes(string? heightRef) => Encoding.UTF8.GetBytes(Catalog.Replace("@HEIGHT@",
        heightRef is null ? "" : ",\"heightRef\":\"" + heightRef + "\"", StringComparison.Ordinal));

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void ClosedOnlyBuildPublishesIndexWithoutProvidersOrWalkableContent()
    {
        var context = Context(new Dictionary<string, IInteriorProvider>());
        var catalog = Encoding.UTF8.GetBytes(ClosedCatalog());
        var bindings = InteriorAssembly.Configure(catalog, context.InteriorProviders, context.World.Terrain);
        AssertThat(bindings.Count == 0).OverrideFailureMessage("Closed entrance configured a provider.").IsTrue();
        var result = InteriorAssembly.BuildConfigured(context, bindings, catalog);
        var entry = result.Entries.Single();
        AssertThat(entry.Feature == Feature && !entry.Open && entry.ProviderId is null && entry.OpenStoreys.Count == 0 && entry.Storeys.Count == 2 &&
            entry.Storeys[1].FloorY == 4 && entry.Entrances.Single().Center.Y == .15f).OverrideFailureMessage("Interior assembly lost closed entrance or original building storeys.").IsTrue();
        AssertThat(result.Content.Meshes.Count == 0 && result.Colliders.Count == 0 && result.NavNodes.Count == 0 &&
            result.NavEdges.Count == 0 && result.NavPortals.Count == 0).OverrideFailureMessage("Closed register generated walkable content.").IsTrue();
        var index = new FakeInteriors(result.Entries.ToArray());
        ((FakeWorld)context.World).Interiors = index;
        AssertThat(index.Get(Feature) == entry && index.EntrancesNear(new(0, .15f, 0), 1).Single().Id == "entry.closed").OverrideFailureMessage("Interior assembly final configured index omitted closed registration.").IsTrue();
        var again = InteriorAssembly.BuildConfigured(context, bindings, catalog);
        AssertThat(ReferenceEquals(again.Entries.Single().Storeys, entry.Storeys) &&
            again.Entries.Single().Entrances.Count == 1).OverrideFailureMessage("Existing closed index duplicated doors or replaced storeys.").IsTrue();
        AssertThat(InteriorAssembly.Build(catalog, context, context.InteriorProviders).Entries.Single().Entrances.Count == 1).OverrideFailureMessage("Legacy Interior assembly Build omitted the closed register.").IsTrue();
        var terrain = new ProbeTerrain();
        using var pad = JsonDocument.Parse(ClosedCatalog(extra: ",\"heightRef\":\"pad\",\"headY\":2"));
        var resolved = InteriorEntranceJson.ParseClosedEntrances(pad.RootElement, terrain)[Feature].Single();
        AssertThat(terrain.Calls == 1 && resolved.Center.Y == 2.15f && resolved.HeadY == 4).OverrideFailureMessage("Closed parsing did not reuse explicit pad/headY semantics.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void ClosedRegisterDoesNotChangeOpenProviderCallsOrNavigation()
    {
        var fallback = new InteriorProviderScopeTests.FixtureBoxInterior();
        var sourceProviders = new Dictionary<string, IInteriorProvider> { [Provider] = fallback };
        var bindings = InteriorAssembly.Configure(Bytes(null), sourceProviders);
        var probe = new CountingProvider(fallback);
        var context = Context(new Dictionary<string, IInteriorProvider> { [Provider] = probe });
        var closedFeature = new FeatureId("fixture:closed");
        var source = context.World.Features[Feature];
        ((FakeWorld)context.World).Features = ((WorldFeature[])[source, source with { Id = closedFeature }]).ToDictionary(feature => feature.Id);
        ((FakeWorld)context.World).Interiors = new FakeInteriors(context.World.Interiors.Get(Feature)!,
            new(closedFeature,Campus.Gen.World.ClosedBuildingStoreys.ClosedBuilding(source with {Id=closedFeature},FlatTerrain.Default),[],false,null,[],[]));
        var baseline = InteriorAssembly.BuildConfigured(context, bindings, Bytes(null));
        int baselineCalls = probe.Calls;
        probe.Calls = 0;
        using var closed = JsonDocument.Parse(ClosedCatalog(feature: closedFeature.Value));
        var raw = closed.RootElement.GetProperty("closedEntrances").GetRawText();
        var mixed = Encoding.UTF8.GetString(Bytes(null)).TrimEnd();
        mixed = mixed[..^1] + ",\"closedEntrances\":" + raw + "}";
        var result = InteriorAssembly.BuildConfigured(context, bindings, Encoding.UTF8.GetBytes(mixed));
        AssertThat(probe.Calls == baselineCalls && result.Content.Meshes.Count == baseline.Content.Meshes.Count &&
            result.Colliders.Count == baseline.Colliders.Count && result.NavNodes.SequenceEqual(baseline.NavNodes) &&
            result.NavEdges.SequenceEqual(baseline.NavEdges) && result.NavPortals.SequenceEqual(baseline.NavPortals) &&
            result.Entries.Single(entry => entry.Feature == Feature).Entrances.SequenceEqual(baseline.Entries.Single().Entrances) &&
            result.Entries.Single(entry => entry.Feature == Feature).ProviderId == Provider &&
            !result.Entries.Single(entry => entry.Feature == closedFeature).Open).OverrideFailureMessage("Closed registration changed open provider generation or navigation.").IsTrue();
    }

    private sealed class CountingProvider(IInteriorProvider inner) : IInteriorProvider
    {
        public int Calls { get; set; }
        public BakeInputDependencies BakeInputs => inner.BakeInputs;
        public BakeInputDependencies CollisionInputs => inner.CollisionInputs;
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
    [TestCategory(TestReason.Contract)]
    public void InvalidClosedClaimsFailBeforeProviderGeneration()
    {
        var context = Context(new Dictionary<string, IInteriorProvider>());
        foreach (var catalog in new[]
        {
            ClosedCatalog().Replace("\"open\":false", "\"open\":true", StringComparison.Ordinal),
            ClosedCatalog(feature: "missing"),
            ClosedCatalog(extra: ",\"heightRef\":\"unknown\""),
            ClosedCatalog().Replace("}]}", "}," + ClosedCatalog().Split("\"closedEntrances\":[", StringSplitOptions.None)[1], StringComparison.Ordinal)
        })
            AssertThrown(() => InteriorAssembly.BuildConfigured(context,
                [new("must-not-be-called", [Feature], [new(Feature, "", [1])], 0)], Encoding.UTF8.GetBytes(catalog))).IsInstanceOf<JsonException>();
        var source = context.World.Features[Feature];
        ((FakeWorld)context.World).Features = ((WorldFeature[])[source with { Kind = new("water") }]).ToDictionary(feature => feature.Id);
        // The loaded index contains only features with building footprints.
        ((FakeWorld)context.World).Interiors = new FakeInteriors();
        AssertThrown(() => InteriorAssembly.BuildConfigured(context, [], Encoding.UTF8.GetBytes(ClosedCatalog()))).IsInstanceOf<JsonException>();
    }

    [TestCase, TestCategory(TestReason.Regression)]
    public void PartRangeKeepsAnotherWingsSameNumberedFloorClosedAndRejectsAnInvalidElevatorStop()
    {
        var provider = new InteriorProviderScopeTests.FixtureBoxInterior();
        var providers = new Dictionary<string, IInteriorProvider> { [Provider] = provider };
        var context = Context(providers);
        var world = (FakeWorld)context.World;
        var left = new PolygonRings([new(-16, -16), new(0, -16), new(0, 16), new(-16, 16)]);
        var right = new PolygonRings([new(0, -16), new(16, -16), new(16, 16), new(0, 16)]);
        var feature = world.Features[Feature] with { Height = 12, Storeys = 3,
            Parts = [new("open-wing", new PolygonGeometry(left), 12, 3), new("closed-wing", new PolygonGeometry(right), 12, 3)] };
        world.Features = new Dictionary<FeatureId, WorldFeature> { [Feature] = feature };
        var catalog = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(Bytes(null))
            .Replace("\"sourceId\":\"\",\"floors\":[1]", "\"sourceId\":\"open-wing\",\"floors\":[1,3]", StringComparison.Ordinal));
        var bindings = InteriorAssembly.ConfigureForWorld(catalog, providers, world);
        var layout = InteriorAssembly.ValidatedLayout(provider, bindings.Single(), context, Feature);
        var entries = InteriorAssembly.IndexEntries([layout], bindings, providers);
        var prepared = Campus.Gen.World.WorldModelSnapshot.WithInteriors(world, entries);
        AssertThat(prepared.Interiors.IsOpen(Feature, 3, new(-8, 9, 0)) &&
            !prepared.Interiors.IsOpen(Feature, 3, new(8, 9, 0)) &&
            !prepared.Interiors.IsOpen(Feature, 2, new(-8, 5, 0))).OverrideFailureMessage("Part-scoped access expanded to a closed wing or middle floor.").IsTrue();
        var invalid = layout with { Elevators = [new("lift", 1, 3,
            new(new(-9, 1, -1), new(-7, 3, 1)), new(new(7, 9, -1), new(9, 11, 1)))] };
        AssertThrown(() => InteriorAssembly.IndexEntries([invalid], bindings, providers)).IsInstanceOf<InvalidDataException>();
        var missing = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(catalog).Replace("open-wing", "missing", StringComparison.Ordinal));
        AssertThrown(() => InteriorAssembly.ConfigureForWorld(missing,
            new Dictionary<string, IInteriorProvider> { [Provider] = new InteriorProviderScopeTests.FixtureBoxInterior() }, world))
            .IsInstanceOf<InvalidDataException>();
    }

    private static string ClosedCatalog(string feature = "fixture:building", string extra = "") =>
        "{\"version\":2,\"interiors\":[],\"closedEntrances\":[{\"id\":\"entry.closed\",\"featureId\":\"" + feature +
        "\",\"doorCenter\":[0,0.15,0],\"outsideLanding\":[0,0.15,-1],\"insideLanding\":[0,0.15,1],\"facingDeg\":0,\"clearWidth\":2.4,\"open\":false" + extra + "}]}";

    private static BuildContext Context(IReadOnlyDictionary<string, IInteriorProvider> providers)
    {
        var bounds = new Box3(new(-16, 0, -16), new(16, 8, 16));
        var ring = new PolygonRings([new(-16, -16), new(16, -16), new(16, 16), new(-16, 16)]);
        var world = new FakeWorld { Boundary = new([]) };
        world.Features = ((WorldFeature[])[new WorldFeature(Feature, new("building"), new PolygonGeometry(ring))
        { Bounds = bounds, Height = 8, Storeys = 2 }]).ToDictionary(feature => feature.Id);
        world.Boundary = ring;
        world.Interiors = new FakeInteriors([new(Feature, providers.Count == 0 ? [new(1,0,4,ring.Outer),new(2,4,4,ring.Outer)] : [], [],false,null,[],[])]);
        var map = new MapDescriptor(new("height-test"), new("Test", "Test"), MapKind.Outdoor, 1, 1,
            new(0, 0, 32648, new(-64, -64), new(64, 64)), Vector2.Zero, bounds,
            new(new(-64, -64), new(64, 64)), new(.9f, 30, 3000, -28, 2000, 30, 40, 150),
            new(-1, -1), new(0, 0), "spawn", [],
            new(MapTimeRule.Shared, new(17.5, Season.Autumn), new("Test", "Test"), false, "test"),
            new("sky.json", "backdrop.json", []), []);
        return new BuildContext { CodeHash = "fixture", WindowNumbering = new((map).Id, new string('0', 64), []), Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog, Map = map, World = world, WorldSeed = 1, InteriorProviders = providers };
    }

    private sealed class ProbeTerrain : ITerrain
    {
        public TerrainInputSummary SourceInputs => throw new NotSupportedException();
        public int Calls { get; private set; }
        public float PadY(FeatureId feature)
        {
            Calls++;
            if (feature != Feature) throw new KeyNotFoundException(feature.Value);
            return 2;
        }
        public bool TryGetPadY(FeatureId feature, out float y)
        {
            Calls++;
            y = feature == Feature ? 2 : default;
            return feature == Feature;
        }
        public bool IsFlat(TileKey tile) => throw new NotSupportedException();
        public float HeightAt(float x, float z) => throw new NotSupportedException();
        public (float Height, TerrainFacts Facts) SampleAt(float x, float z) => throw new NotSupportedException();
        public void FillTile(TileKey tile, int level, Span<short> cm) => throw new NotSupportedException();
        public float? WaterY(FeatureId water) => throw new NotSupportedException();
        public TerrainFacts FactsAt(float x, float z) => throw new NotSupportedException();
    }

}
