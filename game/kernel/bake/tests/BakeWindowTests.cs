using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Parts;
using Campus.Kernel.Registry;
using GdUnit4;

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
            try { _ = BakeWindowPreparation.Prepare(context, new Dictionary<string, BakeService.Module>()); }
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
        Reject(() => BakeWindowPreparation.Prepare(context, modules), "Missing window layout");
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
        Reject(() => BakeWindowPreparation.Prepare(Context(new WpId("WP-18")), Modules(source, provider)), "owner mismatch");
        Reject(() => BakeWindowPreparation.Prepare(Context(claimed: false), Modules(source, provider)), "owner mismatch");
        source.Layouts = [new(new("fixture:absent"), [])];
        Reject(() => BakeWindowPreparation.Prepare(Context(), Modules(source, provider)), "absent feature");
        source.Layouts = [new(Feature, []), new(Feature, [])];
        Reject(() => BakeWindowPreparation.Prepare(Context(), Modules(source, provider)), "Duplicate complete");
        source.Layouts = [new(Feature, [])];
        var modules = Modules(source, provider);
        modules.Add("other-same-owner", new(new Source { Layouts = [new(Feature, [])] }, "other-code", Owner, false));
        Reject(() => BakeWindowPreparation.Prepare(Context(), modules), "Duplicate complete");
        foreach (var grid in new WindowGridSpec[] { new("grid", 0, 1), new("grid", int.MaxValue, int.MaxValue), new(" ", 1, 1) })
        {
            source.Layouts = [new(Feature, [grid])];
            Reject(() => BakeWindowPreparation.Prepare(Context(), Modules(source, provider)), "Invalid window dimensions");
        }
        source.Layouts = [new(Feature, [new("grid", 1, 1), new("grid", 1, 1)])];
        Reject(() => BakeWindowPreparation.Prepare(Context(), Modules(source, provider)), "Duplicate window grid");
        if (provider.Calls != 0) throw new InvalidOperationException("Invalid claims reached the provider.");
    }

    [TestCase]
    public void ProviderMustBeUniqueAndMapCorrectExceptForWindowlessLegacyFixture()
    {
        var source = new Source { Layouts = [new(Feature, [])] };
        var modules = new Dictionary<string, BakeService.Module>(StringComparer.Ordinal) { ["source"] = new(source, "source-code", Owner, false) };
        Reject(() => BakeWindowPreparation.Prepare(Context(), modules), "found 0");
        modules.Add("provider-a", new(new Provider(Snapshot([])), "a-code", new("WP-14"), true));
        modules.Add("provider-b", new(new Provider(Snapshot([])), "b-code", new("WP-14"), true));
        Reject(() => BakeWindowPreparation.Prepare(Context(), modules), "found 2");
        Reject(() => BakeWindowPreparation.Prepare(Context(), Modules(source,
            new Provider(new(new("another-map"), new string('a', 64), [])))), "another map");
        var empty = new BuildContext { Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog, World = new World([]), Map = Map(), WorldSeed = 9 };
        if (BakeWindowPreparation.Prepare(empty, new Dictionary<string, BakeService.Module>()).Snapshot is not null)
            throw new InvalidOperationException("A windowless legacy fixture unexpectedly acquired an allocation.");
        var building = new BuildContext { Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog, World = new World([new(Feature, FeatureSource.Design, new("building"), new PointGeometry(Vector3.Zero))]), Map = Map(), WorldSeed = 9 };
        Reject(() => BakeWindowPreparation.Prepare(building, new Dictionary<string, BakeService.Module>()), "found 0");
    }

    [TestCase]
    public void PreparedSnapshotIsDefensiveAndPublishedByIdentityAfterCacheRoundTrip()
    {
        var grids = new List<WindowGridSpec> { new("main", 2, 1) };
        var source = new Source { Layouts = [new(Feature, grids)], Metadata = new([], [Window(0), Window(1)]) };
        var snapshot = Snapshot([new(Feature, "main", 0, 2, 1)]);
        var provider = new Provider(snapshot);
        var result = BakeWindowPreparation.Prepare(Context(), Modules(source, provider));
        grids.Clear();
        if (provider.Layouts!.Single().Grids.Count != 1 || !ReferenceEquals(source.SeenContext!.WindowNumbering, snapshot))
            throw new InvalidOperationException("The declared input or shared snapshot was not retained.");
        var hash = BakeWindowPreparation.IncludeNumbering("base-input", snapshot);
        var changed = new WindowNumberingSnapshot(snapshot.Map, new string('b', 64), snapshot.Allocations);
        if (hash == BakeWindowPreparation.IncludeNumbering("base-input", changed) || hash != source.SeenContext.DataHash)
        {
            // Context's base input is 'base-input'; provider fingerprint must affect all final producers equally.
            throw new InvalidOperationException("Window fingerprint did not enter the final input identity.");
        }
        var lights = new LightInventoryDocument(1, BakeOptions.Abi, Map().Id, hash, [], result.Metadata.Facades, result.Metadata.Windows)
        { WindowNumbering = snapshot };
        var doors = new EntranceInventoryDocument(1, BakeOptions.Abi, Map().Id, hash, []);
        var lightBytes = BakeMapMetadataCodec.Encode(lights);
        var doorBytes = BakeMapMetadataCodec.Encode(doors);
        var loaded = BakeMapMetadataCodec.Read(lightBytes, doorBytes, Map().Id, hash, snapshot);
        var inventory = new BakeLightInventory();
        inventory.Publish(loaded.Lighting);
        if (!ReferenceEquals(inventory.WindowNumbering, snapshot) || inventory.Windows.Count != 2 || inventory.Rev != 1)
            throw new InvalidOperationException("Inventory did not atomically publish the same prepared snapshot.");
        Reject(() => BakeMapMetadataCodec.Read(lightBytes, doorBytes, Map().Id, hash, changed), "Cached window numbering");
        var old = BakeMapMetadataCodec.Encode(lights with { WindowNumbering = null });
        Reject(() => BakeMapMetadataCodec.Read(old, doorBytes, Map().Id, hash, snapshot), "Cached window numbering");
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
        Reject(() => BakeWindowPreparation.ValidateMetadata(new([facade], [Window(0)]), snapshot), "Duplicate logical");
        Reject(() => BakeWindowPreparation.ValidateMetadata(new([], [Window(0), Window(0)]), snapshot), "Duplicate logical");
        Reject(() => BakeWindowPreparation.ValidateMetadata(new([], [Window(2)]), snapshot), "outside");
        Reject(() => BakeWindowPreparation.ValidateMetadata(new([], [Window(0) with { TotalColumns = 3 }]), snapshot), "BaseId/TotalColumns");
        Reject(() => BakeWindowPreparation.ValidateMetadata(new([facade with { Skip = [1, 1] }], []), snapshot), "Skip");
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
        var mapContext = new Campus.Kernel.Maps.MapContext(context.Map, context.World, "fixture", "fixture", inputs);
        var claims = new Lazy<MapClaimLedger>(() => new(context.World, [new("source", Owner, false, [new FeatureClaim(Feature, Aspects.Facade)])]));
        var reads = 0;
        var data = new Lazy<BakeService.LoadedMap>(() =>
        {
            reads++;
            var prepared = BakeWindowPreparation.Prepare(context, modules);
            return new(9, context.SourceInputs, "final-input", "atlas-input", new(), [], context.InteriorProviders, prepared);
        });
        var map = new BakeService.MapBuild(mapContext, modules, new Dictionary<string, BakeService.Interior>(), [], null!, claims, data,
            context.GlobalInputs, context.GroundMaskContributors, partLods, context.Catalog);
        ExpectNotReady(() => map.GetPreparedContext(mapContext, "source"));
        if (reads != 0) throw new InvalidOperationException("Reading an unprepared context triggered lazy IO.");
        var loaded = data.Value;
        _ = claims.Value;
        var lighting = new LightInventoryDocument(1, BakeOptions.Abi, context.Map.Id, "hash", [], [], []) { WindowNumbering = loaded.Windows.Snapshot };
        map.CompletePreparation(new(lighting, new(1, BakeOptions.Abi, context.Map.Id, "hash", [])));
        var a = map.GetPreparedContext(mapContext, "source");
        var b = map.GetPreparedContext(mapContext, "provider");
        if (!ReferenceEquals(a, map.GetPreparedContext(mapContext, "source")) || !ReferenceEquals(a.WindowNumbering, b.WindowNumbering) ||
            !ReferenceEquals(a.World, context.World) || !ReferenceEquals(a.SourceInputs, context.SourceInputs) || a.DataHash != "final-input" ||
            a.CodeHash != "source-code" || b.CodeHash != "provider-code" || a.ClaimedBy(Feature, Aspects.Facade) != Owner ||
            !ReferenceEquals(a.GlobalInputs, globals) || !ReferenceEquals(b.GlobalInputs, globals) ||
            !ReferenceEquals(a.GroundMaskContributors, contributors) || !ReferenceEquals(b.GroundMaskContributors, contributors) ||
            !ReferenceEquals(a.PartLods, partLods) || !ReferenceEquals(b.PartLods, partLods) ||
            !ReferenceEquals(source.SeenContext!.PartLods, partLods) ||
            !ReferenceEquals(((Provider)modules["provider"].Value).SeenPartLods, partLods))
            throw new InvalidOperationException("Catalog contexts mixed preparation generations or module identities.");
        ExpectNotReady(() => map.GetPreparedContext(mapContext with { }, "source"));
        ExpectNotReady(() => map.GetPreparedContext(mapContext, "unknown"));
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
        ExpectNotReady(() => map.GetPreparedContext(mapContext, "source"));
        if (reads != 1) throw new InvalidOperationException("Context lookup re-ran preparation.");
    }

    private static Dictionary<string, BakeService.Module> Modules(Source source, Provider provider) => new(StringComparer.Ordinal)
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
    private static void Reject(Action action, string message)
    {
        try { action(); }
        catch (InvalidDataException error) when (error.Message.Contains(message, StringComparison.Ordinal)) { return; }
        throw new InvalidOperationException("Expected rejection: " + message);
    }
    private static void ExpectNotReady(Action action)
    {
        try { action(); }
        catch (InvalidOperationException) { return; }
        throw new InvalidOperationException("A stale/unprepared/unknown context was accepted.");
    }
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
        public IInteriorIndex Interiors => throw new NotSupportedException();
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
