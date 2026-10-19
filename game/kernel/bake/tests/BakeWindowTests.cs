using Campus.Gen.Tests.Support;
using Campus.Tests;
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
    [TestCategory(TestReason.Contract)]
    public void MissingLayoutsRejectEveryInputOrder()
    {
        var features = new[]
        {
            WorldFeatureFixture.Create("fixture:z", "poi", new PointGeometry(Vector3.Zero)),
            WorldFeatureFixture.Create("fixture:a", "poi", new PointGeometry(Vector3.Zero))
        };
        foreach (var items in new[] { features, features.Reverse().ToArray() })
        {
            var context = new BuildContext
            {
                Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog,
                World = World(items),
                Map = Map(),
                WorldSeed = 9,
                ClaimedBy = (feature, aspect) => aspect == Aspects.Facade ? new WpId(feature.Value == "fixture:a" ? "WP-16" : "WP-21") : null
            };
            AssertThrown(() => BakeWindowPreparation.Prepare(context, new Dictionary<string, ContentModuleInput>()))
                .IsInstanceOf<InvalidDataException>();
        }
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void ClaimedFacadeMustDeclareACompleteLayoutButMayDeclareNoWindows()
    {
        var context = Context();
        var source = new Source();
        var provider = new Provider(Snapshot([]));
        var modules = Modules(source, provider);
        AssertThrown(() => BakeWindowPreparation.Prepare(context, modules)).IsInstanceOf<InvalidDataException>();
        if (provider.Calls != 0) throw new InvalidOperationException("An incomplete declaration reached the allocator.");
        source.Layouts = [new(Feature, [])];
        var result = BakeWindowPreparation.Prepare(context, modules);
        if (result.Snapshot!.WindowCount != 0 || provider.Calls != 1 || provider.Layouts!.Single().Grids.Count != 0 ||
            !ReferenceEquals(result.Snapshot, source.SeenContext!.WindowNumbering))
            throw new InvalidOperationException("An explicit empty layout was lost or not shared with metadata.");
        // Source/Provider Tiles, Build and Collide throw; this entire two-phase path must remain geometry-free.
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void OwnerUnknownFeatureDuplicateLayoutAndDimensionsFailBeforeAllocation()
    {
        var source = new Source { Layouts = [new(Feature, [])] };
        var provider = new Provider(Snapshot([]));
        AssertThrown(() => BakeWindowPreparation.Prepare(Context(new WpId("WP-18")), Modules(source, provider))).IsInstanceOf<InvalidDataException>();
        AssertThrown(() => BakeWindowPreparation.Prepare(Context(claimed: false), Modules(source, provider))).IsInstanceOf<InvalidDataException>();
        source.Layouts = [new(new("fixture:absent"), [])];
        AssertThrown(() => BakeWindowPreparation.Prepare(Context(), Modules(source, provider))).IsInstanceOf<InvalidDataException>();
        source.Layouts = [new(Feature, []), new(Feature, [])];
        AssertThrown(() => BakeWindowPreparation.Prepare(Context(), Modules(source, provider))).IsInstanceOf<InvalidDataException>();
        source.Layouts = [new(Feature, [])];
        var modules = Modules(source, provider);
        modules.Add("other-same-owner", new(new Source { Layouts = [new(Feature, [])] }, "other-code", Owner, false));
        AssertThrown(() => BakeWindowPreparation.Prepare(Context(), modules)).IsInstanceOf<InvalidDataException>();
        foreach (var grid in new WindowGridSpec[] { new("grid", 0, 1), new("grid", int.MaxValue, int.MaxValue), new(" ", 1, 1) })
        {
            source.Layouts = [new(Feature, [grid])];
            AssertThrown(() => BakeWindowPreparation.Prepare(Context(), Modules(source, provider))).IsInstanceOf<InvalidDataException>();
        }
        source.Layouts = [new(Feature, [new("grid", 1, 1), new("grid", 1, 1)])];
        AssertThrown(() => BakeWindowPreparation.Prepare(Context(), Modules(source, provider))).IsInstanceOf<InvalidDataException>();
        if (provider.Calls != 0) throw new InvalidOperationException("Invalid claims reached the provider.");
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void ProviderMustBeUniqueAndMapCorrectExceptForWindowlessLegacyFixture()
    {
        var source = new Source { Layouts = [new(Feature, [])] };
        var modules = new Dictionary<string, ContentModuleInput>(StringComparer.Ordinal) { ["source"] = new(source, "source-code", Owner, false) };
        AssertThrown(() => BakeWindowPreparation.Prepare(Context(), modules)).IsInstanceOf<InvalidDataException>();
        modules.Add("provider-a", new(new Provider(Snapshot([])), "a-code", new("WP-14"), true));
        modules.Add("provider-b", new(new Provider(Snapshot([])), "b-code", new("WP-14"), true));
        AssertThrown(() => BakeWindowPreparation.Prepare(Context(), modules)).IsInstanceOf<InvalidDataException>();
        AssertThrown(() => BakeWindowPreparation.Prepare(Context(), Modules(source,
            new Provider(new(new("another-map"), new string('a', 64), []))))).IsInstanceOf<InvalidDataException>();
        var empty = new BuildContext { Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog, World = World([]), Map = Map(), WorldSeed = 9 };
        if (BakeWindowPreparation.Prepare(empty, new Dictionary<string, ContentModuleInput>()).Snapshot is not null)
            throw new InvalidOperationException("A windowless legacy fixture unexpectedly acquired an allocation.");
        var building = new BuildContext { Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog, World = World([new(Feature, new("building"), new PointGeometry(Vector3.Zero))]), Map = Map(), WorldSeed = 9 };
        AssertThrown(() => BakeWindowPreparation.Prepare(building, new Dictionary<string, ContentModuleInput>())).IsInstanceOf<InvalidDataException>();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
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
        var lights = new LightInventoryDocument(Map().Id, [], result.Metadata.Facades, result.Metadata.Windows, EmptyNightlight())
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
    [TestCategory(TestReason.Contract)]
    public void MetadataRejectsOverlappingLogicalCellsAndAllocationMismatch()
    {
        var snapshot = Snapshot([new(Feature, "main", 0, 2, 1)]);
        var facade = new WindowFacadeDecl(0, 2, new("window.general"), 0, 2, 1,
            new(-100, 5, 50), Vector3.UnitX, Vector3.UnitY, 2, 1, 1, [0])
        { Skip = [1] };
        BakeWindowPreparation.ValidateMetadata(new([facade], [Window(1)]), snapshot);
        AssertThrown(() => BakeWindowPreparation.ValidateMetadata(new([facade], [Window(0)]), snapshot)).IsInstanceOf<InvalidDataException>();
        AssertThrown(() => BakeWindowPreparation.ValidateMetadata(new([], [Window(0), Window(0)]), snapshot)).IsInstanceOf<InvalidDataException>();
        AssertThrown(() => BakeWindowPreparation.ValidateMetadata(new([], [Window(2)]), snapshot)).IsInstanceOf<InvalidDataException>();
        AssertThrown(() => BakeWindowPreparation.ValidateMetadata(new([], [Window(0) with { TotalColumns = 3 }]), snapshot)).IsInstanceOf<InvalidDataException>();
        AssertThrown(() => BakeWindowPreparation.ValidateMetadata(new([facade with { Skip = [1, 1] }], []), snapshot)).IsInstanceOf<InvalidDataException>();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void PreparedCatalogContextRejectsEarlyStaleUnknownAndUnloadedCallsWithoutLazyIO()
    {
        var source = new Source { Layouts = [new(Feature, [])] };
        var baseline = Context();
        var inputs = BuildInputSnapshot.Freeze(new Dictionary<string, ReadOnlyMemory<byte>> { ["world/geo.json"] = new byte[] { 1 } }, "fixture");
        var globals = BuildInputSnapshot.Freeze(new Dictionary<string, ReadOnlyMemory<byte>> { ["vegetation/species.json"] = new byte[] { 2 } }, "fixture");
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
        var provider = new Provider(Snapshot([]));
        var modules = Modules(source, provider);
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
        AssertThrown(() => map.GetPreparedModuleTilesAsync(mapContext)).IsInstanceOf<InvalidOperationException>();
        if (reads != 0) throw new InvalidOperationException("Reading an unprepared context triggered lazy IO.");
        var loaded = data.Value;
        _ = claims.Value;
        var producer = map.CreateBuildContext("producer-code", "atlas-input");
        AssertThrown(() => map.GetPreparedContext(mapContext, "source")).IsInstanceOf<InvalidOperationException>();
        var lighting = new LightInventoryDocument(context.Map.Id, [], [], [], EmptyNightlight()) { WindowNumbering = loaded.Windows.Snapshot };
        // Metadata still cannot enumerate cells; the explicit cold-resident directory phase can.
        source.PreparingDirectory = provider.PreparingDirectory = true;
        map.PrepareDirectory(new(lighting, new(context.Map.Id, [])));
        source.PreparingDirectory = provider.PreparingDirectory = false;
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
        var counted = new TilesOnlyModule();
        var auxiliary = new DirectoryProducer();
        var indexed = new BakeService.MapBuild(mapContext,
            new Dictionary<string, ContentModuleInput> { ["counted"] = new(counted, "counted-code", Owner, false) },
            new Dictionary<string, BakeService.Interior>(), [new("fixture.auxiliary", auxiliary, "auxiliary-code")], null!, claims, claimLookup, data,
            context.GlobalInputs, context.GroundMaskContributors, partLods, context.Catalog);
        indexed.PrepareDirectory(map.Metadata!);
        indexed.CompletePreparation(map.Metadata!);
        var firstTiles = indexed.GetPreparedModuleTilesAsync(mapContext);
        var secondTiles = indexed.GetPreparedModuleTilesAsync(mapContext);
        var tiles = firstTiles.GetAwaiter().GetResult();
        if (!ReferenceEquals(firstTiles, secondTiles) || !ReferenceEquals(tiles, secondTiles.GetAwaiter().GetResult()) ||
            counted.Calls != 1 || !ReferenceEquals(counted.Context!.World, indexed.GetPreparedContext(mapContext, "counted").World) ||
            !tiles.Modules["counted"].SequenceEqual(counted.Cells.Distinct().OrderBy(tile => tile.X).ThenBy(tile => tile.Z)))
            throw new InvalidOperationException("Prepared map consumers re-enumerated Tiles or lost the frozen module context.");
        AssertThrown(() => indexed.GetPreparedModuleTilesAsync(mapContext with { })).IsInstanceOf<InvalidOperationException>();
        if (tiles.Auxiliary.Count != 1 || auxiliary.DirectoryCalls != 1 || auxiliary.PrepareCalls != 1)
            throw new InvalidOperationException("Auxiliary directory did not use the already prepared content source.");
        var restored = new BakeService.MapBuild(mapContext, indexed.Modules, indexed.Interiors,
            [new("fixture.auxiliary", auxiliary, "auxiliary-code")], null!, claims, claimLookup, data,
            context.GlobalInputs, context.GroundMaskContributors, partLods, context.Catalog);
        restored.PrepareDirectory(map.Metadata!, tiles);
        restored.CompletePreparation(map.Metadata!);
        if (auxiliary.DirectoryCalls != 1 || auxiliary.PrepareCalls != 2 || counted.Calls != 1 ||
            restored.GetPreparedModuleTilesAsync(mapContext).Result.Auxiliary.Single() != tiles.Auxiliary.Single())
            throw new InvalidOperationException("Restoring resident directories reran source or producer layout enumeration.");
        restored.Lifetime.Dispose();
        var catalogue = BakeService.CatalogKeys(indexed, mapOnly: false).ToArray();
        var tree = new Campus.Kernel.Lod.LodTree(context.Map.Bounds, new Campus.Kernel.Lod.StreamSettings());
        if (catalogue.Distinct().Count() != catalogue.Length || counted.Calls != 1 ||
            !catalogue.Any(key => BakeRequestKeys.Parse(key).Level == LodLevel.L4) ||
            catalogue.Any(key => { var request = BakeRequestKeys.Parse(key); return !tree.Contains(new(request.Tile, request.Level)); }) ||
            BakeService.CatalogKeys(indexed, mapOnly: true).Any())
            throw new InvalidOperationException("Full baking duplicated or re-enumerated modules, escaped the stream tree, or ignored map-only mode.");
        indexed.Lifetime.Cancel();
        AssertThrown(() => BakeService.CatalogKeys(indexed, mapOnly: false).ToArray()).IsInstanceOf<OperationCanceledException>();
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
        indexed.Active = false;
        AssertThrown(() => indexed.GetPreparedModuleTilesAsync(mapContext)).IsInstanceOf<InvalidOperationException>();
        map.Collect("source", "geometry", [warning]);
        if (!map.Diagnostics.IsEmpty) throw new InvalidOperationException("Unloaded map accepted late diagnostics.");
        AssertThrown(() => map.GetPreparedContext(mapContext, "source")).IsInstanceOf<InvalidOperationException>();
        if (reads != 1) throw new InvalidOperationException("Context lookup re-ran preparation.");
        if (source.TileCalls != 1 || provider.TileCalls != 1)
            throw new InvalidOperationException("Prepared lookups re-enumerated the cold-resident directory.");
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void UnloadedBuildStopsBeforeEncodingAndNeverPublishesItsOldHandle()
    {
        var context = Context();
        var snapshot = BuildInputSnapshot.Freeze(context.SourceInputs, "fixture");
        var mapContext = new Campus.Kernel.Maps.MapContext(context.Map, context.World, "fixture", snapshot);
        using var started = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var producer = new PausedProducer(started, release);
        var map = new BakeService.MapBuild(mapContext, new Dictionary<string, ContentModuleInput>(),
            new Dictionary<string, BakeService.Interior>(), [new("cancelled", producer, "code")], null!,
            new(() => new(context.World, [])), (_, _) => null, new(() => new(9, snapshot.Entries, [],
                new Dictionary<string, IInteriorProvider>(), new(null, new([], [])))),
            context.GlobalInputs, context.GroundMaskContributors, new PartLodCatalog([]), context.Catalog);
        var cache = Path.Combine(Path.GetTempPath(), "uestc-rpg-bake-cancel-" + Guid.NewGuid().ToString("N"));
        using var service = new BakeService(new(cache, true, false, false, false, 1, null));
        var queue = new BakeQueue(1, static () => { });
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
        finally
        {
            release.Set(); queue.Dispose(); map.Lifetime.Dispose();
            if (Directory.Exists(cache)) Directory.Delete(cache, recursive: true);
        }
    }

    private sealed class DirectoryProducer : IBakeProducer, IBakeContentConsumer
    {
        public BakePayloadClass PayloadClass => BakePayloadClass.Product;
        public BakeProductKind ProductKind => BakeProductKind.Vegetation;
        public BakeInputDependencies Inputs(BakeKey key) => BakeInputDependencies.World;
        public int PrepareCalls;
        public int DirectoryCalls;
        private bool _prepared;
        public bool CanBuild(BakeKey key) => key.Module == BakeModuleIds.VegetationGrass;
        public void Prepare(Lazy<IReadOnlyList<PreparedContentModule>> modules, LightInventoryDocument lighting)
        {
            // Eager consumers are legal; preparation must not re-enter a directory task awaiting publication.
            if (modules.Value.Single().Context.CodeHash != "counted-code" || lighting.Nightlight is null)
                throw new InvalidOperationException("Consumer saw incomplete frozen inputs.");
            _prepared = true; PrepareCalls++;
        }
        public IReadOnlyList<BakeTileEntry> Tiles(BuildContext context)
        {
            if (!_prepared) throw new InvalidOperationException("Directory enumeration preceded consumer preparation.");
            DirectoryCalls++;
            return [new(BakeRequestKeys.Tile(context.Map.Id, BakeModuleIds.VegetationGrass, default, LodLevel.L0), context.Map.Bounds)];
        }
        public object Build(BakeKey key, BuildContext context) => throw new InvalidOperationException("Directory preparation generated a payload.");
    }

    private sealed class PausedProducer(ManualResetEventSlim started, ManualResetEventSlim release) : IBakeProducer
    {
        public BakePayloadClass PayloadClass => BakePayloadClass.Product;
        public BakeProductKind ProductKind => BakeProductKind.Fauna;
        public BakeInputDependencies Inputs(BakeKey key) => BakeInputDependencies.World;
        public IReadOnlyList<BakeTileEntry> Tiles(BuildContext context) => [];
        public bool CanBuild(BakeKey key) => key.Module == "cancelled";
        public object Build(BakeKey key, BuildContext context)
        {
            started.Set();
            if (!release.Wait(TimeSpan.FromSeconds(5))) throw new TimeoutException("Build was not released.");
            // Encoding this unsupported result would fail; cancellation must stop before the codec is called.
            return new object();
        }
    }

    private static NightlightCatalog EmptyNightlight() => new([], ReadOnlyMemory<float>.Empty, ReadOnlyMemory<float>.Empty, 0, 0);

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
        World = World([new(Feature, new("poi"), new PointGeometry(Vector3.Zero))]),
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
        new("sky.json", "backdrop.json", []), []);
    private class Module : IContentModule
    {
        public bool PreparingDirectory { get; set; }
        public int TileCalls { get; private set; }
        public IReadOnlyList<Claim> Claims => [];
        public ModuleBudget Budget => new(0, 0, 0);
        public virtual IEnumerable<TileKey> Tiles(BuildContext ctx)
        {
            if (!PreparingDirectory) throw new InvalidOperationException("Only explicit directory preparation may call Tiles.");
            TileCalls++;
            return [];
        }
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
    private sealed class TilesOnlyModule : Module
    {
        public readonly TileKey[] Cells = [new(2, 3), new(-1, 2), new(2, 3)];
        public int Calls;
        public BuildContext? Context;
        public override IEnumerable<TileKey> Tiles(BuildContext context)
        { Interlocked.Increment(ref Calls); Context = context; return Cells; }
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

    private static FakeWorld World(IReadOnlyList<WorldFeature> features) => new()
    {
        Features = features.ToDictionary(feature => feature.Id),
        Boundary = new([Vector2.Zero, Vector2.UnitX, Vector2.One, Vector2.UnitY]),
        Spatial = new FlatSpatial(forbidden: "Window metadata must not query spatial data."),
    };

    private sealed class Contributor : IGroundMaskContributor
    {
        public string Id => "fixture.leaves";
        public GroundMaskChannels Channels => GroundMaskChannels.GinkgoLeaves;
        public void Paint(BuildContext context, in GroundMaskGrid grid, Span<byte> mask1, Span<byte> mask2) =>
            throw new InvalidOperationException("Catalog lookup must not paint masks.");
    }
}
