using Campus.Gen.Tests.Support;
using Campus.Tests;
using Campus.Gen.BakeInput;
using System.Numerics;
using System.Runtime.ExceptionServices;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Parts;
using Campus.Contracts.Bake;
using Campus.Contracts.AppFlow;
using Campus.Kernel.Registry;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Campus.Kernel.Bake.Tests;

[TestSuite]
public sealed class BakeWindowTests
{
    private static readonly FeatureId Feature = new("fixture:facade");
    private const string Owner = "fixture.facade-owner";

    private static GroundCoverData GroundCover() => new(new(new(Vector2.Zero, Vector2.One), 1, 1, 1f), new byte[4], new byte[4]);

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
                CodeHash = "fixture", WindowNumbering = new((Map()).Id, new string('0', 64), []),
                Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog,
                World = World(items),
                Map = Map(),
                WorldSeed = 9,
                ClaimedBy = (feature, aspect) => aspect == Aspects.Facade ? (feature.Value == "fixture:a" ? "source:a" : "source:z") : null
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
        if (result.Snapshot.WindowCount != 0 || provider.Calls != 1 || provider.Layouts!.Single().Grids.Count != 0 ||
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
        AssertThrown(() => BakeWindowPreparation.Prepare(Context("different-module"), Modules(source, provider))).IsInstanceOf<InvalidDataException>();
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
    public void ProviderMustBeUniqueAndMapCorrectWithExplicitWindowlessAllocation()
    {
        var source = new Source { Layouts = [new(Feature, [])] };
        var modules = new Dictionary<string, ContentModuleInput>(StringComparer.Ordinal) { ["source"] = new(source, "source-code", Owner, false) };
        AssertThrown(() => BakeWindowPreparation.Prepare(Context(), modules)).IsInstanceOf<InvalidDataException>();
        modules.Add("provider-a", new(new Provider(Snapshot([])), "a-code", "fixture.window-provider", true));
        modules.Add("provider-b", new(new Provider(Snapshot([])), "b-code", "fixture.window-provider", true));
        AssertThrown(() => BakeWindowPreparation.Prepare(Context(), modules)).IsInstanceOf<InvalidDataException>();
        AssertThrown(() => BakeWindowPreparation.Prepare(Context(), Modules(source,
            new Provider(new(new("another-map"), new string('a', 64), []))))).IsInstanceOf<InvalidDataException>();
        var empty = new BuildContext { CodeHash = "fixture", WindowNumbering = new((Map()).Id, new string('0', 64), []), Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog, World = World([]), Map = Map(), WorldSeed = 9 };
        if (!ReferenceEquals(BakeWindowPreparation.Prepare(empty, new Dictionary<string, ContentModuleInput>()).Snapshot, empty.WindowNumbering))
            throw new InvalidOperationException("A windowless map lost its explicit empty allocation.");
        var lighting = new LightInventoryDocument(empty.Map.Id, [], [], [], EmptyNightlight()) { WindowNumbering = empty.WindowNumbering };
        var resident = new BakeResidentData(new(lighting, new(empty.Map.Id, []), GroundCover()), new([], []), [], [],
            new(new Dictionary<string, IReadOnlyList<TileKey>>(), []), []);
        BakeMapMetadataBuilder.ValidateResident(resident, empty.Map.Id, new Dictionary<string, ContentModuleInput>(), []);
        AssertThrown(() => BakeMapMetadataBuilder.ValidateResident(resident with { Metadata = resident.Metadata with { Lighting = lighting with { WindowNumbering = null } } },
            empty.Map.Id, new Dictionary<string, ContentModuleInput>(), [])).IsInstanceOf<InvalidDataException>();
        var building = new BuildContext { CodeHash = "fixture", WindowNumbering = new((Map()).Id, new string('0', 64), []), Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog, World = World([new(Feature, new("building"), new PointGeometry(Vector3.Zero))]), Map = Map(), WorldSeed = 9 };
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
            CodeHash = "fixture", WindowNumbering = new((baseline.Map).Id, new string('0', 64), []),
            Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog,
            World = baseline.World,
            Map = baseline.Map,
            WorldSeed = baseline.WorldSeed,
            ClaimedBy = baseline.ClaimedBy,
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
            return new(9, context.SourceInputs, [], context.InteriorProviders, prepared, []);
        });
        Func<FeatureId, Aspects, string?> claimLookup = (feature, aspect) => claims.Value.ClaimedBy(feature, aspect);
        var map = new BakeService.MapBuild(mapContext, modules, new Dictionary<string, BakeService.Interior>(), [], null!, claimLookup, data,
            context.GlobalInputs, context.GroundMaskContributors, partLods, context.Catalog, null, null);
        AssertThrown(() => map.Content.GetPreparedContext(mapContext, "source")).IsInstanceOf<InvalidOperationException>();
        AssertThrown(() => map.Content.GetPreparedBuildContext(source)).IsInstanceOf<InvalidOperationException>();
        AssertThrown(() => map.Directory.GetPreparedModuleTilesAsync(mapContext)).IsInstanceOf<InvalidOperationException>();
        if (reads != 0) throw new InvalidOperationException("Reading an unprepared context triggered lazy IO.");
        var loaded = data.Value;
        _ = claims.Value;
        var producer = map.Content.CreateBuildContext("producer-code");
        AssertThrown(() => map.Content.GetPreparedContext(mapContext, "source")).IsInstanceOf<InvalidOperationException>();
        var lighting = new LightInventoryDocument(context.Map.Id, [], [], [], EmptyNightlight()) { WindowNumbering = loaded.Windows.Snapshot };
        // Metadata still cannot enumerate cells; the explicit cold-resident directory phase can.
        source.PreparingDirectory = provider.PreparingDirectory = true;
        var groundCover = GroundCover();
        map.Directory.Prepare(new(lighting, new(context.Map.Id, []), groundCover));
        source.PreparingDirectory = provider.PreparingDirectory = false;
        map.Content.CompletePreparation(new(lighting, new(context.Map.Id, []), groundCover));
        map.Directory.PublishDirectory();
        var a = map.Content.GetPreparedContext(mapContext, "source");
        var b = map.Content.GetPreparedContext(mapContext, "provider");
        if (!ReferenceEquals(a, map.Content.GetPreparedContext(mapContext, "source")) || !ReferenceEquals(a.WindowNumbering, b.WindowNumbering) ||
            !ReferenceEquals(a.World, context.World) || !ReferenceEquals(a.SourceInputs, context.SourceInputs) ||
            !ReferenceEquals(a.ClaimedBy, b.ClaimedBy) || !ReferenceEquals(a.ClaimedBy, claimLookup) ||
            !ReferenceEquals(a.ClaimedBy, producer.ClaimedBy) || !ReferenceEquals(a.Catalog, producer.Catalog) ||
            !ReferenceEquals(a.SourceInputs, producer.SourceInputs) || !ReferenceEquals(a.WindowNumbering, producer.WindowNumbering) ||
            !ReferenceEquals(a.GlobalInputs, producer.GlobalInputs) || !ReferenceEquals(a.GroundMaskContributors, producer.GroundMaskContributors) ||
            !ReferenceEquals(a.PartLods, producer.PartLods) || producer.CodeHash != "producer-code" ||
            a.CodeHash != "source-code" || b.CodeHash != "provider-code" || a.ClaimedBy(Feature, Aspects.Facade) != "source" ||
            !ReferenceEquals(a.GlobalInputs, globals) || !ReferenceEquals(b.GlobalInputs, globals) ||
            !ReferenceEquals(a.GroundMaskContributors, contributors) || !ReferenceEquals(b.GroundMaskContributors, contributors) ||
            !ReferenceEquals(a.PartLods, partLods) || !ReferenceEquals(b.PartLods, partLods) ||
            !ReferenceEquals(source.SeenContext!.PartLods, partLods) ||
            !ReferenceEquals(((Provider)modules["provider"].Value).SeenPartLods, partLods))
            throw new InvalidOperationException("Catalog contexts mixed preparation generations or module identities.");
        AssertThrown(() => map.Content.GetPreparedContext(mapContext with { }, "source")).IsInstanceOf<InvalidOperationException>();
        AssertThrown(() => map.Content.GetPreparedContext(mapContext, "unknown")).IsInstanceOf<InvalidOperationException>();
        var counted = new TilesOnlyModule();
        var auxiliary = new DirectoryProducer();
        var indexed = new BakeService.MapBuild(mapContext,
            new Dictionary<string, ContentModuleInput> { ["counted"] = new(counted, "counted-code", Owner, false) },
            new Dictionary<string, BakeService.Interior>(), [new("fixture.auxiliary", auxiliary, "auxiliary-code")], null!, claimLookup, data,
            context.GlobalInputs, context.GroundMaskContributors, partLods, context.Catalog, null, null);
        indexed.Directory.Prepare(map.Content.Metadata!);
        indexed.Content.CompletePreparation(map.Content.Metadata!);
        indexed.Directory.PublishDirectory();
        var firstTiles = indexed.Directory.GetPreparedModuleTilesAsync(mapContext);
        var secondTiles = indexed.Directory.GetPreparedModuleTilesAsync(mapContext);
        var tiles = firstTiles.GetAwaiter().GetResult();
        if (!ReferenceEquals(firstTiles, secondTiles) || !ReferenceEquals(tiles, secondTiles.GetAwaiter().GetResult()) ||
            counted.Calls != 1 || !ReferenceEquals(counted.Context!.World, indexed.Content.GetPreparedContext(mapContext, "counted").World) ||
            !tiles.Modules["counted"].SequenceEqual(counted.Cells.Distinct().OrderBy(tile => tile.X).ThenBy(tile => tile.Z)))
            throw new InvalidOperationException("Prepared map consumers re-enumerated Tiles or lost the frozen module context.");
        AssertThrown(() => indexed.Directory.GetPreparedModuleTilesAsync(mapContext with { })).IsInstanceOf<InvalidOperationException>();
        if (tiles.Auxiliary.Count != 1 || auxiliary.DirectoryCalls != 1 || auxiliary.PrepareCalls != 1)
            throw new InvalidOperationException("Auxiliary directory did not use the already prepared content source.");
        var restored = new BakeService.MapBuild(mapContext, indexed.Content.Modules, indexed.Content.Interiors,
            [new("fixture.auxiliary", auxiliary, "auxiliary-code")], null!, claimLookup, data,
            context.GlobalInputs, context.GroundMaskContributors, partLods, context.Catalog, null, null);
        restored.Directory.Prepare(map.Content.Metadata!, tiles);
        restored.Content.CompletePreparation(map.Content.Metadata!);
        restored.Directory.PublishDirectory();
        if (auxiliary.DirectoryCalls != 1 || auxiliary.PrepareCalls != 2 || counted.Calls != 1 ||
            restored.Directory.GetPreparedModuleTilesAsync(mapContext).Result.Auxiliary.Single() != tiles.Auxiliary.Single())
            throw new InvalidOperationException("Restoring resident directories reran source or producer layout enumeration.");
        restored.Lifetime.Dispose();
        var catalogue = BakeCatalog.Keys(indexed, mapOnly: false).ToArray();
        var tree = new Campus.Kernel.Lod.LodTree(context.Map.Bounds, new Campus.Kernel.Lod.StreamSettings());
        if (catalogue.Distinct().Count() != catalogue.Length || counted.Calls != 1 ||
            !catalogue.Any(key => BakeRequestKeys.Parse(key).Level == LodLevel.L4) ||
            catalogue.Any(key => { var request = BakeRequestKeys.Parse(key); return !tree.Contains(new(request.Tile, request.Level)); }) ||
            BakeCatalog.Keys(indexed, mapOnly: true).Any())
            throw new InvalidOperationException("Full baking duplicated or re-enumerated modules, escaped the stream tree, or ignored map-only mode.");
        indexed.Active = false;
        if (!ReferenceEquals(indexed.Content.GetPreparedBuildContext(counted), counted.Context) ||
            !ReferenceEquals(indexed.Directory.PreparedTiles, tiles) || !BakeCatalog.Keys(indexed, mapOnly: false).SequenceEqual(catalogue))
            throw new InvalidOperationException("Retiring a map revoked its already frozen content or catalog directory.");
        AssertThrown(() => indexed.Content.GetPreparedBuildContext(new object())).IsInstanceOf<InvalidOperationException>();
        AssertThrown(() => indexed.Content.GetPreparedContext(mapContext, "counted")).IsInstanceOf<InvalidOperationException>();
        AssertThrown(() => indexed.Directory.GetPreparedModuleTilesAsync(mapContext)).IsInstanceOf<InvalidOperationException>();
        indexed.Lifetime.Cancel();
        AssertThrown(() => BakeCatalog.Keys(indexed, mapOnly: false).ToArray()).IsInstanceOf<OperationCanceledException>();
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
        AssertThrown(() => map.Content.GetPreparedContext(mapContext, "source")).IsInstanceOf<InvalidOperationException>();
        AssertThrown(() => map.Content.GetModule<Source>(mapContext, "source")).IsInstanceOf<InvalidOperationException>();
        AssertThrown(() => map.Content.FreezeInputs(map.Content.Metadata!)).IsInstanceOf<InvalidOperationException>();
        AssertThrown(() => map.Content.CompletePreparation(map.Content.Metadata!)).IsInstanceOf<InvalidOperationException>();
        if (reads != 1) throw new InvalidOperationException("Context lookup re-ran preparation.");
        if (source.TileCalls != 1 || provider.TileCalls != 1)
            throw new InvalidOperationException("Prepared lookups re-enumerated the cold-resident directory.");
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void UnloadedBuildStopsBeforeEncodingAndNeverPublishesItsOldHandle()
        => AssertUnloadedBuild(beforeContext: false, collision: false, fault: false);

    [TestCase]
    [TestCategory(TestReason.Regression)]
    public void AcceptedBuildRetainsItsFrozenContextAndCollisionDirectoryAfterRetirement()
    {
        AssertUnloadedBuild(beforeContext: true, collision: false, fault: false);
        AssertUnloadedBuild(beforeContext: false, collision: true, fault: false);
    }

    [TestCase]
    [TestCategory(TestReason.Regression)]
    public void RealProducerFaultAfterRetirementReachesEveryDrain()
        => AssertUnloadedBuild(beforeContext: false, collision: false, fault: true);

    private static void AssertUnloadedBuild(bool beforeContext, bool collision, bool fault)
    {
        var context = Context();
        var snapshot = BuildInputSnapshot.Freeze(context.SourceInputs, "fixture");
        var mapContext = new Campus.Kernel.Maps.MapContext(context.Map, context.World, "fixture", snapshot);
        using var started = new ManualResetEventSlim();
        using var failed = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var original = fault ? new InvalidDataException("Producer failed after map cancellation.") : null;
        var producer = new PausedProducer(started, release, collision) { Failure = original };
        var map = new BakeService.MapBuild(mapContext, new Dictionary<string, ContentModuleInput>(),
            new Dictionary<string, BakeService.Interior>(), [new("cancelled", producer, "code")], null!,
            (_, _) => null, new(() => new(9, snapshot.Entries, [],
                new Dictionary<string, IInteriorProvider>(), new(context.WindowNumbering, new([], [])), [])),
            context.GlobalInputs, context.GroundMaskContributors, new PartLodCatalog([]), context.Catalog, null, null);
        var lighting = new LightInventoryDocument(context.Map.Id, [], [], [], EmptyNightlight())
            { WindowNumbering = map.Data.Value.Windows.Snapshot };
        var metadata = new BakeMapMetadata(lighting, new(context.Map.Id, []), GroundCover());
        map.Directory.Prepare(metadata);
        map.Content.CompletePreparation(metadata);
        map.Directory.PublishDirectory();
        // Arm after preparation; CanBuild precedes the frozen context read, Inputs precedes the collision directory read.
        producer.PauseBeforeContext = beforeContext;
        producer.PauseBeforeDirectory = collision;
        var cache = Path.Combine(Path.GetTempPath(), "uestc-rpg-bake-cancel-" + Guid.NewGuid().ToString("N"));
        var options = new BakeOptions(cache, true, false, false, false, 1, null);
        using var terrain = BakeInputFixture.CreateTerrainCache(options);
        var queue = new BakeQueue(1, static () => { });
        var products = new BakeProductBuilder(options, terrain, queue);
        Exception? stopped = null;
        try
        {
            var key = collision ? BakeRequestKeys.Collision(context.Map.Id, "cancelled", default) :
                BakeRequestKeys.Tile(context.Map.Id, "cancelled", default, LodLevel.L0);
            var handle = queue.Ensure(map.Work, key, BakePriority.Background, () =>
            {
                try { return products.Build(map, key); }
                catch (Exception error) { stopped = error; failed.Set(); throw; }
            });
            var signal = WaitHandle.WaitAny([started.WaitHandle, failed.WaitHandle], TimeSpan.FromSeconds(5));
            if (signal == 1) ExceptionDispatchInfo.Capture(stopped!).Throw();
            if (signal == WaitHandle.WaitTimeout) throw new TimeoutException("Build did not start.");
            map.Active = false;
            var firstDrain = queue.ForgetMap(map.Work);
            var secondDrain = queue.ForgetMap(map.Work);
            map.Lifetime.Cancel();
            AssertThrown(() => queue.Ensure(map.Work, key, BakePriority.Background, () => new object()))
                .IsInstanceOf<OperationCanceledException>();
            if (firstDrain.IsCompleted || secondDrain.IsCompleted || handle.Done.IsCompleted)
                throw new InvalidOperationException("A drain completed before the accepted producer stopped.");
            release.Set();
            queue.Dispose();
            if (original is null)
                Task.WhenAll(firstDrain, secondDrain).GetAwaiter().GetResult();
            else
                foreach (var task in new[] { firstDrain, secondDrain, handle.Done })
                {
                    try { task.GetAwaiter().GetResult(); throw new InvalidOperationException("A producer failure was lost."); }
                    catch (AggregateException aggregate) when (aggregate.Flatten().InnerExceptions.Any(error => ReferenceEquals(error, original))) { }
                    catch (InvalidDataException error) when (ReferenceEquals(error, original)) { }
                }
            var failureRetained = original is null ? queue.Failure is null :
                queue.Failure is AppStartupException failure && ReferenceEquals(failure.InnerException, original);
            if ((original is null ? stopped is not OperationCanceledException || !handle.Done.IsCanceled : !ReferenceEquals(stopped, original)) ||
                handle.State != BakeState.Failed || handle.Product is not null || !failureRetained || Directory.Exists(cache) ||
                original is not null && !ReferenceEquals(handle.Failure, original))
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
        public bool UsesModule(IContentModule module) => false;
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
        public object Build(BakeKey key, BuildContext context, string inputHash) => throw new InvalidOperationException("Directory preparation generated a payload.");
    }

    private sealed class PausedProducer(ManualResetEventSlim started, ManualResetEventSlim release, bool collision) : IBakeProducer
    {
        public BakePayloadClass PayloadClass => collision ? BakePayloadClass.Tile : BakePayloadClass.Product;
        public BakeProductKind ProductKind => collision ? BakeProductKind.Collision : BakeProductKind.Fauna;
        public bool PauseBeforeContext;
        public bool PauseBeforeDirectory;
        public Exception? Failure;
        public BakeInputDependencies Inputs(BakeKey key)
        {
            if (PauseBeforeDirectory) Pause();
            return BakeInputDependencies.World;
        }
        public bool UsesModule(IContentModule module) => false;
        public IReadOnlyList<BakeTileEntry> Tiles(BuildContext context) =>
            collision ? [] : [new(BakeRequestKeys.Tile(context.Map.Id, "cancelled", default, LodLevel.L0), context.Map.Bounds)];
        public bool CanBuild(BakeKey key)
        {
            if (PauseBeforeContext) Pause();
            return key.Module == "cancelled";
        }
        public object Build(BakeKey key, BuildContext context, string inputHash)
        {
            if (!PauseBeforeContext && !PauseBeforeDirectory) Pause();
            if (Failure is not null) throw Failure;
            // Encoding this unsupported result would fail; cancellation must stop before the codec is called.
            return new object();
        }
        private void Pause()
        {
            started.Set();
            if (!release.Wait(TimeSpan.FromSeconds(5))) throw new TimeoutException("Build was not released.");
        }
    }

    private static NightlightCatalog EmptyNightlight() => new([], ReadOnlyMemory<float>.Empty, ReadOnlyMemory<float>.Empty, 0, 0);

    private static Dictionary<string, ContentModuleInput> Modules(Source source, Provider provider) => new(StringComparer.Ordinal)
    {
        ["source"] = new(source, "source-code", Owner, false),
        ["provider"] = new(provider, "provider-code", "fixture.window-provider", true)
    };
    private static WindowNumberingSnapshot Snapshot(IReadOnlyList<WindowAllocation> allocations) => new(Map().Id, new string('a', 64), allocations);
    private static WindowDecl Window(int id) => new(id, 0, 2, new("window.general"), new(-100 + id, 5, 50), Vector3.UnitX, Vector3.UnitY, 1, 1);
    private static BuildContext Context(string? module = null, bool claimed = true) => new()
    {
        CodeHash = "fixture", WindowNumbering = new((Map()).Id, new string('0', 64), []),
        Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog,
        World = World([new(Feature, new("poi"), new PointGeometry(Vector3.Zero))]),
        Map = Map(),
        WorldSeed = 9,
        ClaimedBy = (feature, aspect) =>
            claimed && feature == Feature && aspect == Aspects.Facade ? module ?? "source" : null
    };
    private static MapDescriptor Map() => new(new("fixture"), new("Fixture", "Fixture"), MapKind.Outdoor, 1, 1,
        new(0, 0, 32648, Vector2.Zero, new(64, 64)), new(32, 32), new(Vector3.Zero, new(64, 64, 64)),
        new(Vector2.Zero, new(64, 64)), new(1, 0, 64, 0, 64, 1, 1, 1), new(0, 0), new(0, 0), "start", [],
        new(MapTimeRule.Shared, new(17.5, Season.Autumn), new("Default", "Default"), false, "fixture"),
        new("sky.json", "backdrop.json", []), []);
    private class Module : IContentModule
    {
        public BakeInputDependencies BakeInputs => BakeInputDependencies.World;
        public BakeInputDependencies CollisionInputs => BakeInputDependencies.World;
        public bool PreparingDirectory { get; set; }
        public int TileCalls { get; private set; }
        public IReadOnlyList<Claim> Claims => [];
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
            if (prepass.WindowNumbering.Map != prepass.Map.Id || prepass.WindowNumbering.Allocations.Count != 0 ||
                prepass.WindowNumbering.NextId != 0 || prepass.CodeHash != "provider-code")
                throw new InvalidOperationException("Provider prepass lacks this map's explicit empty snapshot or has the wrong code identity.");
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
