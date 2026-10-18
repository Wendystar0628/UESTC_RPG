using Campus.Pipeline.Map;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Parts;
using Campus.Contracts.Bake;
using Campus.Kernel.Registry;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Campus.Kernel.Bake.Tests;

[TestSuite]
public sealed class BakeWindowTests
{
    private static readonly FeatureId Feature = new("fixture:facade");
    private static readonly WpId Owner = new("WP-16");

    [TestCase]
    public void MissingLayoutsReportEveryOwnerAndFeatureInOneStableDiagnostic()
    {
        var features = new[]
        {
            new WorldFeature(new("fixture:z"), FeatureSource.Design, new("poi"), new PointGeometry(Vector3.Zero)),
            new WorldFeature(new("fixture:a"), FeatureSource.Design, new("poi"), new PointGeometry(Vector3.Zero))
        };
        string Report(IEnumerable<WorldFeature> items)
        {
            var context = new BuildContext
            {
                Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog,
                World = new World(items.ToArray()),
                Map = Map(),
                WorldSeed = 9,
                ClaimedBy = (feature, aspect) => aspect == Aspects.Facade ? new WpId(feature.Value == "fixture:a" ? "WP-16" : "WP-21") : null
            };
            try { _ = BakeWindowPreparation.Prepare(context, new Dictionary<string, ContentModuleInput>()); }
            catch (InvalidDataException error) { return error.Message; }
            throw new InvalidOperationException("Missing layouts were silently accepted.");
        }
        var report = Report(features);
        if (report != Report(features.Reverse()) || !report.Contains("2 facade claims", StringComparison.Ordinal) ||
            !report.Contains("owner WP-16, feature fixture:a", StringComparison.Ordinal) ||
            !report.Contains("owner WP-21, feature fixture:z", StringComparison.Ordinal))
            throw new InvalidOperationException("The missing-layout diagnostic dropped an owner/feature or depended on input order.");
    }

    [TestCase]
    public void ClaimedFacadeMustDeclareACompleteLayoutButMayDeclareNoWindows()
    {
        var context = Context();
        var source = new Source();
        var provider = new Provider(Snapshot([]));
        var modules = Modules(source, provider);
        { string? __rejectionMessage = null; try { BakeWindowPreparation.Prepare(context, modules); } catch (InvalidDataException error) { __rejectionMessage = error.Message; } AssertThat(__rejectionMessage is not null).OverrideFailureMessage("Expected rejection: " + "Missing window layout").IsTrue(); AssertThat(__rejectionMessage!).OverrideFailureMessage("Expected rejection: " + "Missing window layout").Contains("Missing window layout"); };
        if (provider.Calls != 0) throw new InvalidOperationException("An incomplete declaration reached the allocator.");
        source.Layouts = [new(Feature, [])];
        var result = BakeWindowPreparation.Prepare(context, modules);
        if (result.Snapshot!.WindowCount != 0 || provider.Calls != 1 || provider.Layouts!.Single().Grids.Count != 0 ||
            !ReferenceEquals(result.Snapshot, source.SeenContext!.WindowNumbering))
            throw new InvalidOperationException("An explicit empty layout was lost or not shared with metadata.");
        // Source/Provider Tiles, Build and Collide throw; this entire two-phase path must remain geometry-free.
    }

    [TestCase]
    public void OwnerUnknownFeatureDuplicateLayoutAndDimensionsFailBeforeAllocation()
    {
        var source = new Source { Layouts = [new(Feature, [])] };
        var provider = new Provider(Snapshot([]));
        { string? __rejectionMessage = null; try { BakeWindowPreparation.Prepare(Context(new WpId("WP-18")), Modules(source, provider)); } catch (InvalidDataException error) { __rejectionMessage = error.Message; } AssertThat(__rejectionMessage is not null).OverrideFailureMessage("Expected rejection: " + "owner mismatch").IsTrue(); AssertThat(__rejectionMessage!).OverrideFailureMessage("Expected rejection: " + "owner mismatch").Contains("owner mismatch"); };
        { string? __rejectionMessage = null; try { BakeWindowPreparation.Prepare(Context(claimed: false), Modules(source, provider)); } catch (InvalidDataException error) { __rejectionMessage = error.Message; } AssertThat(__rejectionMessage is not null).OverrideFailureMessage("Expected rejection: " + "owner mismatch").IsTrue(); AssertThat(__rejectionMessage!).OverrideFailureMessage("Expected rejection: " + "owner mismatch").Contains("owner mismatch"); };
        source.Layouts = [new(new("fixture:absent"), [])];
        { string? __rejectionMessage = null; try { BakeWindowPreparation.Prepare(Context(), Modules(source, provider)); } catch (InvalidDataException error) { __rejectionMessage = error.Message; } AssertThat(__rejectionMessage is not null).OverrideFailureMessage("Expected rejection: " + "absent feature").IsTrue(); AssertThat(__rejectionMessage!).OverrideFailureMessage("Expected rejection: " + "absent feature").Contains("absent feature"); };
        source.Layouts = [new(Feature, []), new(Feature, [])];
        { string? __rejectionMessage = null; try { BakeWindowPreparation.Prepare(Context(), Modules(source, provider)); } catch (InvalidDataException error) { __rejectionMessage = error.Message; } AssertThat(__rejectionMessage is not null).OverrideFailureMessage("Expected rejection: " + "Duplicate complete").IsTrue(); AssertThat(__rejectionMessage!).OverrideFailureMessage("Expected rejection: " + "Duplicate complete").Contains("Duplicate complete"); };
        source.Layouts = [new(Feature, [])];
        var modules = Modules(source, provider);
        modules.Add("other-same-owner", new(new Source { Layouts = [new(Feature, [])] }, "other-code", Owner, false));
        { string? __rejectionMessage = null; try { BakeWindowPreparation.Prepare(Context(), modules); } catch (InvalidDataException error) { __rejectionMessage = error.Message; } AssertThat(__rejectionMessage is not null).OverrideFailureMessage("Expected rejection: " + "Duplicate complete").IsTrue(); AssertThat(__rejectionMessage!).OverrideFailureMessage("Expected rejection: " + "Duplicate complete").Contains("Duplicate complete"); };
        foreach (var grid in new WindowGridSpec[] { new("grid", 0, 1), new("grid", int.MaxValue, int.MaxValue), new(" ", 1, 1) })
        {
            source.Layouts = [new(Feature, [grid])];
            { string? __rejectionMessage = null; try { BakeWindowPreparation.Prepare(Context(), Modules(source, provider)); } catch (InvalidDataException error) { __rejectionMessage = error.Message; } AssertThat(__rejectionMessage is not null).OverrideFailureMessage("Expected rejection: " + "Invalid window dimensions").IsTrue(); AssertThat(__rejectionMessage!).OverrideFailureMessage("Expected rejection: " + "Invalid window dimensions").Contains("Invalid window dimensions"); };
        }
        source.Layouts = [new(Feature, [new("grid", 1, 1), new("grid", 1, 1)])];
        { string? __rejectionMessage = null; try { BakeWindowPreparation.Prepare(Context(), Modules(source, provider)); } catch (InvalidDataException error) { __rejectionMessage = error.Message; } AssertThat(__rejectionMessage is not null).OverrideFailureMessage("Expected rejection: " + "Duplicate window grid").IsTrue(); AssertThat(__rejectionMessage!).OverrideFailureMessage("Expected rejection: " + "Duplicate window grid").Contains("Duplicate window grid"); };
        if (provider.Calls != 0) throw new InvalidOperationException("Invalid claims reached the provider.");
    }

    [TestCase]
    public void ProviderMustBeUniqueAndMapCorrectExceptForWindowlessLegacyFixture()
    {
        var source = new Source { Layouts = [new(Feature, [])] };
        var modules = new Dictionary<string, ContentModuleInput>(StringComparer.Ordinal) { ["source"] = new(source, "source-code", Owner, false) };
        { string? __rejectionMessage = null; try { BakeWindowPreparation.Prepare(Context(), modules); } catch (InvalidDataException error) { __rejectionMessage = error.Message; } AssertThat(__rejectionMessage is not null).OverrideFailureMessage("Expected rejection: " + "found 0").IsTrue(); AssertThat(__rejectionMessage!).OverrideFailureMessage("Expected rejection: " + "found 0").Contains("found 0"); };
        modules.Add("provider-a", new(new Provider(Snapshot([])), "a-code", new("WP-14"), true));
        modules.Add("provider-b", new(new Provider(Snapshot([])), "b-code", new("WP-14"), true));
        { string? __rejectionMessage = null; try { BakeWindowPreparation.Prepare(Context(), modules); } catch (InvalidDataException error) { __rejectionMessage = error.Message; } AssertThat(__rejectionMessage is not null).OverrideFailureMessage("Expected rejection: " + "found 2").IsTrue(); AssertThat(__rejectionMessage!).OverrideFailureMessage("Expected rejection: " + "found 2").Contains("found 2"); };
        { string? __rejectionMessage = null; try { BakeWindowPreparation.Prepare(Context(), Modules(source,
            new Provider(new(new("another-map"), new string('a', 64), [])))); } catch (InvalidDataException error) { __rejectionMessage = error.Message; } AssertThat(__rejectionMessage is not null).OverrideFailureMessage("Expected rejection: " + "another map").IsTrue(); AssertThat(__rejectionMessage!).OverrideFailureMessage("Expected rejection: " + "another map").Contains("another map"); };
        var empty = new BuildContext { Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog, World = new World([]), Map = Map(), WorldSeed = 9 };
        if (BakeWindowPreparation.Prepare(empty, new Dictionary<string, ContentModuleInput>()).Snapshot is not null)
            throw new InvalidOperationException("A windowless legacy fixture unexpectedly acquired an allocation.");
        var building = new BuildContext { Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog, World = new World([new(Feature, FeatureSource.Design, new("building"), new PointGeometry(Vector3.Zero))]), Map = Map(), WorldSeed = 9 };
        { string? __rejectionMessage = null; try { BakeWindowPreparation.Prepare(building, new Dictionary<string, ContentModuleInput>()); } catch (InvalidDataException error) { __rejectionMessage = error.Message; } AssertThat(__rejectionMessage is not null).OverrideFailureMessage("Expected rejection: " + "found 0").IsTrue(); AssertThat(__rejectionMessage!).OverrideFailureMessage("Expected rejection: " + "found 0").Contains("found 0"); };
    }

    [TestCase]
    public void PreparedSnapshotIsDefensiveAndPublishedByIdentity()
    {
        var grids = new List<WindowGridSpec> { new("main", 2, 1) };
        var source = new Source { Layouts = [new(Feature, grids)], Metadata = new([], [Window(0), Window(1)]) };
        var snapshot = Snapshot([new(Feature, "main", 0, 2, 1)]);
        var provider = new Provider(snapshot);
        var result = BakeWindowPreparation.Prepare(Context(), Modules(source, provider));
        grids.Clear();
        if (provider.Layouts!.Single().Grids.Count != 1 || !ReferenceEquals(source.SeenContext!.WindowNumbering, snapshot))
            throw new InvalidOperationException("The declared input or shared snapshot was not retained.");
        var lights = new LightInventoryDocument(Map().Id, [], result.Metadata.Facades, result.Metadata.Windows)
        { WindowNumbering = snapshot };
        var inventory = new BakeLightInventory();
        inventory.Publish(lights);
        if (!ReferenceEquals(inventory.WindowNumbering, snapshot) || inventory.Windows.Count != 2 || inventory.Rev != 1)
            throw new InvalidOperationException("Inventory did not atomically publish the same prepared snapshot.");
        inventory.Clear();
        if (inventory.WindowNumbering is not null || inventory.Windows.Count != 0 || inventory.Rev != 2)
            throw new InvalidOperationException("Unloading retained an old map's window allocation.");
    }

    [TestCase]
    public void MetadataRejectsOverlappingLogicalCellsAndAllocationMismatch()
    {
        var snapshot = Snapshot([new(Feature, "main", 0, 2, 1)]);
        var facade = new WindowFacadeDecl(0, 2, new("window.general"), 0, 2, 1,
            new(-100, 5, 50), Vector3.UnitX, Vector3.UnitY, 2, 1, 1, [0])
        { Skip = [1] };
        BakeWindowPreparation.ValidateMetadata(new([facade], [Window(1)]), snapshot);
        { string? __rejectionMessage = null; try { BakeWindowPreparation.ValidateMetadata(new([facade], [Window(0)]), snapshot); } catch (InvalidDataException error) { __rejectionMessage = error.Message; } AssertThat(__rejectionMessage is not null).OverrideFailureMessage("Expected rejection: " + "Duplicate logical").IsTrue(); AssertThat(__rejectionMessage!).OverrideFailureMessage("Expected rejection: " + "Duplicate logical").Contains("Duplicate logical"); };
        { string? __rejectionMessage = null; try { BakeWindowPreparation.ValidateMetadata(new([], [Window(0), Window(0)]), snapshot); } catch (InvalidDataException error) { __rejectionMessage = error.Message; } AssertThat(__rejectionMessage is not null).OverrideFailureMessage("Expected rejection: " + "Duplicate logical").IsTrue(); AssertThat(__rejectionMessage!).OverrideFailureMessage("Expected rejection: " + "Duplicate logical").Contains("Duplicate logical"); };
        { string? __rejectionMessage = null; try { BakeWindowPreparation.ValidateMetadata(new([], [Window(2)]), snapshot); } catch (InvalidDataException error) { __rejectionMessage = error.Message; } AssertThat(__rejectionMessage is not null).OverrideFailureMessage("Expected rejection: " + "outside").IsTrue(); AssertThat(__rejectionMessage!).OverrideFailureMessage("Expected rejection: " + "outside").Contains("outside"); };
        { string? __rejectionMessage = null; try { BakeWindowPreparation.ValidateMetadata(new([], [Window(0) with { TotalColumns = 3 }]), snapshot); } catch (InvalidDataException error) { __rejectionMessage = error.Message; } AssertThat(__rejectionMessage is not null).OverrideFailureMessage("Expected rejection: " + "BaseId/TotalColumns").IsTrue(); AssertThat(__rejectionMessage!).OverrideFailureMessage("Expected rejection: " + "BaseId/TotalColumns").Contains("BaseId/TotalColumns"); };
        { string? __rejectionMessage = null; try { BakeWindowPreparation.ValidateMetadata(new([facade with { Skip = [1, 1] }], []), snapshot); } catch (InvalidDataException error) { __rejectionMessage = error.Message; } AssertThat(__rejectionMessage is not null).OverrideFailureMessage("Expected rejection: " + "Skip").IsTrue(); AssertThat(__rejectionMessage!).OverrideFailureMessage("Expected rejection: " + "Skip").Contains("Skip"); };
    }

    [TestCase]
    public void PreparedCatalogContextRejectsEarlyStaleUnknownAndUnloadedCallsWithoutLazyIO()
    {
        var source = new Source { Layouts = [new(Feature, [])] };
        var baseline = Context();
        var inputs = BuildInputSnapshot.Freeze(new Dictionary<string, ReadOnlyMemory<byte>> { ["campus.json"] = new byte[] { 1 } });
        var globals = BuildInputSnapshot.Freeze(new Dictionary<string, ReadOnlyMemory<byte>> { ["vegetation/species.json"] = new byte[] { 2 } });
        IReadOnlyList<IGroundMaskContributor> contributors = Array.AsReadOnly<IGroundMaskContributor>([new Contributor()]);
        var partLods = new PartLodCatalog([new(new PartId("fixture.part"), new BakeFixturePart())]);
        var context = new BuildContext
        {
            Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog,
            World = baseline.World,
            Map = baseline.Map,
            WorldSeed = baseline.WorldSeed,
            ClaimedBy = baseline.ClaimedBy,
            DataHash = baseline.DataHash,
            SourceInputs = inputs.Entries,
            GlobalInputs = globals,
            GroundMaskContributors = contributors,
            PartLods = partLods
        };
        var modules = Modules(source, new Provider(Snapshot([])));
        var mapContext = new Campus.Kernel.Maps.MapContext(context.Map, context.World, "fixture", inputs);
        var claims = new Lazy<MapClaimLedger>(() => new(context.World, [new("source", Owner, false, [new FeatureClaim(Feature, Aspects.Facade)])]));
        var reads = 0;
        var data = new Lazy<BakeService.LoadedMap>(() =>
        {
            reads++;
            var prepared = BakeWindowPreparation.Prepare(context, modules);
            return new(9, context.SourceInputs, [], context.InteriorProviders, prepared);
        });
        Func<FeatureId, Aspects, WpId?> claimLookup = (feature, aspect) => claims.Value.ClaimedBy(feature, aspect);
        var map = new BakeService.MapBuild(mapContext, modules, new Dictionary<string, BakeService.Interior>(), [], null!, claims, claimLookup, data,
            context.GlobalInputs, context.GroundMaskContributors, partLods, context.Catalog);
        AssertThrown(() => map.GetPreparedContext(mapContext, "source")).IsInstanceOf<InvalidOperationException>();
        if (reads != 0) throw new InvalidOperationException("Reading an unprepared context triggered lazy IO.");
        var loaded = data.Value;
        _ = claims.Value;
        var producer = map.CreateBuildContext("producer-code", "atlas-input");
        AssertThrown(() => map.GetPreparedContext(mapContext, "source")).IsInstanceOf<InvalidOperationException>();
        var lighting = new LightInventoryDocument(context.Map.Id, [], [], []) { WindowNumbering = loaded.Windows.Snapshot };
        map.CompletePreparation(new(lighting, new(context.Map.Id, [])));
        var a = map.GetPreparedContext(mapContext, "source");
        var b = map.GetPreparedContext(mapContext, "provider");
        if (!ReferenceEquals(a, map.GetPreparedContext(mapContext, "source")) || !ReferenceEquals(a.WindowNumbering, b.WindowNumbering) ||
            !ReferenceEquals(a.World, context.World) || !ReferenceEquals(a.SourceInputs, context.SourceInputs) ||
            !ReferenceEquals(a.ClaimedBy, b.ClaimedBy) || !ReferenceEquals(a.ClaimedBy, claimLookup) || a.DataHash != "" ||
            !ReferenceEquals(a.ClaimedBy, producer.ClaimedBy) || !ReferenceEquals(a.Catalog, producer.Catalog) ||
            !ReferenceEquals(a.SourceInputs, producer.SourceInputs) || !ReferenceEquals(a.WindowNumbering, producer.WindowNumbering) ||
            !ReferenceEquals(a.GlobalInputs, producer.GlobalInputs) || !ReferenceEquals(a.GroundMaskContributors, producer.GroundMaskContributors) ||
            !ReferenceEquals(a.PartLods, producer.PartLods) || producer.CodeHash != "producer-code" || producer.DataHash != "atlas-input" ||
            a.CodeHash != "source-code" || b.CodeHash != "provider-code" || a.ClaimedBy(Feature, Aspects.Facade) != Owner ||
            !ReferenceEquals(a.GlobalInputs, globals) || !ReferenceEquals(b.GlobalInputs, globals) ||
            !ReferenceEquals(a.GroundMaskContributors, contributors) || !ReferenceEquals(b.GroundMaskContributors, contributors) ||
            !ReferenceEquals(a.PartLods, partLods) || !ReferenceEquals(b.PartLods, partLods) ||
            !ReferenceEquals(source.SeenContext!.PartLods, partLods) ||
            !ReferenceEquals(((Provider)modules["provider"].Value).SeenPartLods, partLods))
            throw new InvalidOperationException("Catalog contexts mixed preparation generations or module identities.");
        AssertThrown(() => map.GetPreparedContext(mapContext with { }, "source")).IsInstanceOf<InvalidOperationException>();
        AssertThrown(() => map.GetPreparedContext(mapContext, "unknown")).IsInstanceOf<InvalidOperationException>();
        var warning = new BuildDiagnostic(DiagnosticLevel.Warning, "fixture.warning", "fixture warning");
        map.Collect("source", "metadata", [warning], Feature);
        if (!map.Diagnostics.TryDequeue(out var diagnostic) || diagnostic.Map != context.Map.Id || diagnostic.Owner != Owner ||
            diagnostic.Module != "source" || diagnostic.Stage != "metadata" || diagnostic.Diagnostic.Feature != Feature ||
            diagnostic.Diagnostic.Code != warning.Code || diagnostic.Diagnostic.Level != warning.Level)
            throw new InvalidOperationException("Build diagnostics lost their map, owner, module, stage or feature context.");
        try { map.Collect("source", "geometry", [warning, default]); }
        catch (ArgumentException) { }
        if (!map.Diagnostics.IsEmpty) throw new InvalidOperationException("Invalid diagnostics partially entered the collection.");
        map.Active = false;
        map.Collect("source", "geometry", [warning]);
        if (!map.Diagnostics.IsEmpty) throw new InvalidOperationException("Unloaded map accepted late diagnostics.");
        AssertThrown(() => map.GetPreparedContext(mapContext, "source")).IsInstanceOf<InvalidOperationException>();
        if (reads != 1) throw new InvalidOperationException("Context lookup re-ran preparation.");
    }

    [TestCase]
    public void UnloadedBuildStopsBeforeEncodingAndNeverPublishesItsOldHandle()
    {
        var context = Context();
        var snapshot = BuildInputSnapshot.Freeze(context.SourceInputs);
        var mapContext = new Campus.Kernel.Maps.MapContext(context.Map, context.World, "fixture", snapshot);
        using var started = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var producer = new PausedProducer(started, release);
        var map = new BakeService.MapBuild(mapContext, new Dictionary<string, ContentModuleInput>(),
            new Dictionary<string, BakeService.Interior>(), [new("cancelled", producer, "code")], null!,
            new(() => new(context.World, [])), (_, _) => null, new(() => new(9, snapshot.Entries, [],
                new Dictionary<string, IInteriorProvider>(), new(null, new([], [])))),
            context.GlobalInputs, context.GroundMaskContributors, new PartLodCatalog([]), context.Catalog);
        var root = Environment.GetEnvironmentVariable("UESTC_REPOSITORY")!;
        var cache = Path.Combine(root, "output", "bake-cancel", Guid.NewGuid().ToString("N"));
        using var service = new BakeService(new(cache, true, false, false, false, 1, null));
        var queue = new BakeQueue(1);
        Exception? stopped = null;
        try
        {
            var key = new BakeKey(context.Map.Id, "cancelled", "fixture");
            var handle = queue.Ensure(key, BakePriority.Background, () =>
            {
                try { return service.Build(map, key); }
                catch (Exception error) { stopped = error; throw; }
            });
            if (!started.Wait(TimeSpan.FromSeconds(5))) throw new TimeoutException("Build did not start.");
            map.Active = false;
            queue.ForgetMap(key.Map);
            map.Lifetime.Cancel();
            release.Set();
            queue.Dispose();
            if (stopped is not OperationCanceledException || handle.State != BakeState.Failed || handle.Product is not null ||
                !queue.Failures.IsEmpty || Directory.Exists(cache))
                throw new InvalidOperationException("An unloaded build entered encoding, published Ready, or wrote cache data.");
        }
        finally { release.Set(); queue.Dispose(); map.Lifetime.Dispose(); }
    }

    private sealed class PausedProducer(ManualResetEventSlim started, ManualResetEventSlim release) : IBakeProducer
    {
        public bool CanBuild(BakeKey key) => key.Module == "cancelled";
        public object Build(BakeKey key, BuildContext context)
        {
            started.Set();
            if (!release.Wait(TimeSpan.FromSeconds(5))) throw new TimeoutException("Build was not released.");
            // Encoding this unsupported result would fail; cancellation must stop before the codec is called.
            return new object();
        }
    }

    private static Dictionary<string, ContentModuleInput> Modules(Source source, Provider provider) => new(StringComparer.Ordinal)
    {
        ["source"] = new(source, "source-code", Owner, false),
        ["provider"] = new(provider, "provider-code", new("WP-14"), true)
    };
    private static WindowNumberingSnapshot Snapshot(IReadOnlyList<WindowAllocation> allocations) => new(Map().Id, new string('a', 64), allocations);
    private static WindowDecl Window(int id) => new(id, 0, 2, new("window.general"), new(-100 + id, 5, 50), Vector3.UnitX, Vector3.UnitY, 1, 1);
    private static BuildContext Context(WpId? owner = null, bool claimed = true) => new()
    {
        Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog,
        World = new World([new(Feature, FeatureSource.Design, new("poi"), new PointGeometry(Vector3.Zero))]),
        Map = Map(),
        WorldSeed = 9,
        DataHash = "base-input",
        ClaimedBy = (feature, aspect) =>
            claimed && feature == Feature && aspect == Aspects.Facade ? owner ?? Owner : null
    };
    private static MapDescriptor Map() => new(new("fixture"), new("Fixture", "Fixture"), MapKind.Outdoor, 1, 1,
        new(0, 0, 32648, Vector2.Zero, new(64, 64)), new(32, 32), new(Vector3.Zero, new(64, 64, 64)),
        new(Vector2.Zero, new(64, 64)), new(1, 0, 64, 0, 64, 1, 1, 1), new(0, 0), new(0, 0), "start", [],
        new(MapTimeRule.Shared, new(17.5, Season.Autumn), new("Default", "Default"), false, "fixture"),
        new("sky.json", "backdrop.json", []), new Dictionary<string, string>(), []);
    private class Module : IContentModule
    {
        public IReadOnlyList<Claim> Claims => [];
        public ModuleBudget Budget => new(0, 0, 0);
        public IEnumerable<TileKey> Tiles(BuildContext ctx) => throw new InvalidOperationException("Metadata must not call Tiles.");
        public ContentOutput Build(BuildContext ctx, TileKey tile, LodLevel lod) => throw new InvalidOperationException("Metadata must not call Build.");
        public IReadOnlyList<Collider> Collide(BuildContext ctx, TileKey tile) => throw new InvalidOperationException("Metadata must not call Collide.");
    }
    private sealed class Source : Module, IWindowMetadataSource
    {
        public IReadOnlyList<ClaimedWindowLayout> Layouts { get; set; } = [];
        public WindowMetadata Metadata { get; set; } = new([], []);
        public BuildContext? SeenContext { get; private set; }
        public IReadOnlyList<ClaimedWindowLayout> DescribeWindowLayouts(IWorldModel world, uint seed) => Layouts;
        public WindowMetadata DescribeWindows(BuildContext context) { SeenContext = context; return Metadata; }
    }
    private sealed class Provider(WindowNumberingSnapshot snapshot) : Module, IWindowNumberingProvider
    {
        public int Calls { get; private set; }
        public IReadOnlyList<ClaimedWindowLayout>? Layouts { get; private set; }
        public PartLodCatalog? SeenPartLods { get; private set; }
        public WindowNumberingSnapshot PrepareWindowNumbering(BuildContext prepass, IReadOnlyList<ClaimedWindowLayout> layouts)
        {
            Calls++; Layouts = layouts; SeenPartLods = prepass.PartLods;
            if (prepass.WindowNumbering is not null || prepass.CodeHash != "provider-code")
                throw new InvalidOperationException("Provider prepass has a final snapshot or the wrong code identity.");
            return snapshot; // A fixed protocol fixture, never a second allocation implementation.
        }
    }
    private sealed class World(IReadOnlyList<WorldFeature> features) : IWorldModel
    {
        public IReadOnlyDictionary<FeatureId, WorldFeature> Features { get; } = features.ToDictionary(feature => feature.Id);
        public PolygonRings Boundary => new([Vector2.Zero, Vector2.UnitX, Vector2.One, Vector2.UnitY]);
        public WorldNetwork Network => WorldNetwork.Empty;
        public ISpatialIndex Spatial => throw new NotSupportedException();
        public IInteriorIndex Interiors { get; } = new Campus.Tests.TestInteriorIndex();
        public IOutskirtsModel? Outskirts => null;
        public IFidelityModel? Fidelity => null;
    }
    private sealed class Contributor : IGroundMaskContributor
    {
        public string Id => "fixture.leaves";
        public GroundMaskChannels Channels => GroundMaskChannels.GinkgoLeaves;
        public void Paint(BuildContext context, in GroundMaskGrid grid, Span<byte> mask1, Span<byte> mask2) =>
            throw new InvalidOperationException("Catalog lookup must not paint masks.");
    }
}
