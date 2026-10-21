using Campus.Gen.Tests.Support;
using Campus.Gen.World;
using Campus.Gen.BakeInput;
using static Campus.Kernel.Bake.Tests.BakeInputFixture;
using System.Numerics;
using System.Text;
using System.Text.Json;
using Campus.Contracts;
using Campus.Kernel.Registry;
using Campus.Contracts.Bake;
using Campus.Contracts.Maps;
using Campus.Contracts.Parts;
using GdUnit4;
using Campus.Tests;

namespace Campus.Kernel.Bake.Tests;

[TestSuite]
public sealed class BakeInputTests
{
    [TestCase]
    [TestCategory(TestReason.Regression)]
    public void AuxiliaryInputsUseWholeSiteBoundsAndOriginalSourceCode()
    {
        var feature = Feature("fixture:outside-owner", 250, "fixture", "one", "one");
        BuildContext With(WorldFeature value) => Context(new FakeWorld
        {
            Boundary = new([]), Features = new Dictionary<FeatureId, WorldFeature> { [value.Id] = value }
        });
        var before = With(feature);
        var after = With(feature with { Name = "changed" });
        var key = BakeRequestKeys.Tile(before.Map.Id, BakeModuleIds.FaunaRuntime, default, LodLevel.L0);
        var footprint = new MapRect(new(0, 0), new(300, 100));
        string Stamp(BuildContext context, string sourceCode, MapRect? area) =>
            BakeInputFingerprint.Compute(context, key, "runtime-code", new(context), BakeInputDependencies.World,
                new Dictionary<string, string> { ["fauna"] = sourceCode }, area);
        if (Stamp(before, "source-one", footprint) == Stamp(after, "source-one", footprint) ||
            Stamp(before, "source-one", footprint) == Stamp(before, "source-two", footprint))
            throw new InvalidOperationException("Auxiliary freshness ignored remote site bounds or source module code.");
        if (Stamp(before, "source-one", null) != Stamp(after, "source-one", null))
            throw new InvalidOperationException("The fixture did not place its dependency outside the owner-cell margin.");
    }

    [TestCase]
    [TestCategory(TestReason.Regression)]
    public void SceneAtlasChangesInvalidateItsRenderConsumersWithoutChangingCollision()
    {
        var world = new FakeWorld { Boundary = new([]) };
        var before = Context(world, global: Atlas(512));
        var after = Context(world, global: Atlas(1024));
        foreach (var module in new[] { "furniture", "zone.north", "zone.center-east", "zone.west", "ground", "interior.research-six" })
        {
            if (Hash(before, module) == Hash(after, module))
                throw new InvalidOperationException("Scene atlas UV changes did not invalidate a rendered consumer.");
            var key = new BakeKey(before.Map.Id, module, "C/0/0");
            if (BakeInputFingerprint.Compute(before, key, "runtime-code", new(before), BakeInputDependencies.World) !=
                BakeInputFingerprint.Compute(after, key, "runtime-code", new(after), BakeInputDependencies.World))
                throw new InvalidOperationException("Scene atlas changes invalidated collision.");
        }
        if (Hash(before, "unrelated") != Hash(after, "unrelated"))
            throw new InvalidOperationException("Scene atlas changes invalidated unrelated content.");

        static BuildInputSnapshot Atlas(int size) => BuildInputSnapshot.Freeze(
            new Dictionary<string, ReadOnlyMemory<byte>>
            { [SceneTextInputs.AtlasKey] = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { size })) }, "fixture");
    }

    [TestCase]
    [TestCategory(TestReason.Regression)]
    public void MapBuildSourceIndexIsSharedWithoutTerrainQueriesOrCrossingWorlds()
    {
        var field = new CountingFingerprintField(1);
        var context = Context(new FakeWorld { Boundary = new([]), Terrain = field });
        var map = Map(context);
        using var lifetime = map.Lifetime;
        var keys = new[]
        {
            new BakeKey(context.Map.Id, "collision.world", "C/0/0"),
            new BakeKey(context.Map.Id, "collision.world", "C/1/0"),
            BakeRequestKeys.Tile(context.Map.Id, "ground", default, LodLevel.L0)
        };
        var referenceField = new CountingFingerprintField(1);
        var reference = Context(new FakeWorld { Boundary = new([]), Terrain = referenceField });
        var expected = keys.Select(key => BakeInputFingerprint.Compute(reference, key, "runtime-code", new(reference), BakeInputDependencies.World)).ToArray();
        var actual = new string[keys.Length];
        Parallel.For(0, keys.Length, index => actual[index] =
            BakeInputFingerprint.Compute(context, keys[index], "runtime-code", sources: map.Inputs.SourceIndex(context), dependencies: BakeInputDependencies.World));
        if (!actual.SequenceEqual(expected) || !field.PointReads.IsEmpty || !field.TileReads.IsEmpty ||
            !ReferenceEquals(map.Inputs.SourceIndex(context), map.Inputs.SourceIndex(context)))
            throw new InvalidOperationException("Concurrent keys changed source stamps, queried terrain, or rebuilt the map index.");

        var changedField = new CountingFingerprintField(2);
        var changed = Context(new FakeWorld { Boundary = new([]), Terrain = changedField });
        var nextMap = Map(changed);
        using var nextLifetime = nextMap.Lifetime;
        var next = BakeInputFingerprint.Compute(changed, keys[0], "runtime-code", sources: nextMap.Inputs.SourceIndex(changed), dependencies: BakeInputDependencies.World);
        if (next == actual[0] || !changedField.PointReads.IsEmpty || !changedField.TileReads.IsEmpty ||
            next != BakeInputFingerprint.Compute(changed, keys[0], "runtime-code", new(changed), BakeInputDependencies.World))
            throw new InvalidOperationException("The next MapBuild reused the preceding world's source index or queried terrain.");

        static BakeService.MapBuild Map(BuildContext context) => new(
            new TestMapContext(context.Map, context.World), new Dictionary<string, ContentModuleInput>(),
            new Dictionary<string, BakeService.Interior>(), [], null!, context.ClaimedBy, null!,
            context.GlobalInputs, context.GroundMaskContributors, context.PartLods!, context.Catalog, null, null);
    }

    [TestCase]
    [TestCategory(TestReason.Regression)]
    public void PadSourcePresenceAndValuesInvalidateWithoutReadingResolvedPads()
    {
        var prop = Feature("fixture:prop", 10, "牌", "说明", "入口");
        var green = prop with { Id = new("fixture:green"), Kind = new("green") };
        var building = prop with { Id = new("fixture:building"), Kind = new("building") };
        var world = new FakeWorld { Boundary = new([]), Features = ((WorldFeature[])[prop, green, building]).ToDictionary(feature => feature.Id) };
        const string planting = """{"roads":[],"groves":{"mix":{},"areas":[],"washNear":[]}}""";
        foreach (var module in new[] { "furniture", "vegetation" })
        {
            var context = Context(world, planting: planting);
            world.Terrain = Terrain(new Dictionary<FeatureId, float>());
            var absent = Hash(context, module);
            world.Terrain = Terrain(new Dictionary<FeatureId, float> { [building.Id] = 0f });
            var atZero = Hash(context, module);
            world.Terrain = Terrain(new Dictionary<FeatureId, float> { [building.Id] = 2f });
            var raised = Hash(context, module);
            if (absent == atZero || atZero == raised)
                throw new InvalidOperationException("Pad presence or elevation did not invalidate its consumers.");
        }

        static TerrainHeightField Terrain(IReadOnlyDictionary<FeatureId, float> pads) => new(
            new(JsonSerializer.Serialize(pads.OrderBy(pair => pair.Key.Value).ToArray(), ContractJson.Options), []),
            new(Vector2.Zero, new(64, 64)), (_, _) => new(TerrainDifferential.Constant(0), TerrainClass.Flat), [], pads);
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void CellInputsIgnoreExplanationsAndDistantObjectsButKeepDisplayedText()
    {
        var world = new FakeWorld { Boundary = new([]) };
        var local = Feature("fixture:local", 10, "校门", "旧说明", "入口");
        var distant = Feature("fixture:distant", 1000, "远楼", "说明", "远处");
        world.Features = ((WorldFeature[])[local, distant]).ToDictionary(feature => feature.Id);
        var baseline = Hash(Context(world), "furniture");
        world.Features = ((WorldFeature[])[Feature("fixture:local", 10, "校门", "新说明", "入口"), distant with { Name = "远处改名" }]).ToDictionary(feature => feature.Id);
        if (Hash(Context(world), "furniture") != baseline) throw new InvalidOperationException("Explanations or a distant object invalidated this cell.");
        world.Features = ((WorldFeature[])[local with { Name = "北校门" }, distant]).ToDictionary(feature => feature.Id);
        if (Hash(Context(world), "furniture") == baseline) throw new InvalidOperationException("Displayed name did not invalidate its cell.");
        world.Features = ((WorldFeature[])[Feature("fixture:local", 10, "校门", "旧说明", "出口"), distant]).ToDictionary(feature => feature.Id);
        if (Hash(Context(world), "furniture") == baseline) throw new InvalidOperationException("Displayed text did not invalidate its cell.");

        string Interiors(int localFloor, int distantFloor) => JsonSerializer.Serialize(new { version = 3, interiors = new[]
        {
            new { providerId = "fixture.local", featureIds = new[] { local.Id.Value }, openParts = new[] { new { featureId = local.Id.Value, sourceId = "", kind = "indoorFloor", floors = new[] { localFloor } } }, entrances = Array.Empty<object>() },
            new { providerId = "fixture.distant", featureIds = new[] { distant.Id.Value }, openParts = new[] { new { featureId = distant.Id.Value, sourceId = "", kind = "indoorFloor", floors = new[] { distantFloor } } }, entrances = Array.Empty<object>() }
        } });
        baseline = Hash(Context(world, interiors: Interiors(1, 1)), "furniture");
        if (Hash(Context(world, interiors: Interiors(1, 2)), "furniture") != baseline ||
            Hash(Context(world, interiors: Interiors(2, 1)), "furniture") == baseline)
            throw new InvalidOperationException("Interior catalog changes did not stay with their building's source cells.");

        var parcel = Green("fixture:green", 10, 300);
        var remoteParcel = Green("fixture:remote-green", 1000, 60);
        const string planting = """{"roads":[],"groves":{"mix":{},"areas":[],"washNear":[]}}""";
        world.Features = ((WorldFeature[])[parcel, remoteParcel]).ToDictionary(feature => feature.Id);
        baseline = Hash(Context(world, planting: planting), "vegetation");
        world.Features = ((WorldFeature[])[parcel, Green("fixture:remote-green", 1000, 80)]).ToDictionary(feature => feature.Id);
        if (Hash(Context(world, planting: planting), "vegetation") != baseline)
            throw new InvalidOperationException("An independent distant green parcel invalidated this vegetation cell.");
        world.Features = ((WorldFeature[])[Green("fixture:green", 10, 320), remoteParcel]).ToDictionary(feature => feature.Id);
        if (Hash(Context(world, planting: planting), "vegetation") == baseline)
            throw new InvalidOperationException("The far end of this cell's green parcel did not invalidate its vegetation.");

        var group = Green("fixture:group", 180, 180) with
        {
            Kind = new("sportGroup"),
            Props = JsonSerializer.SerializeToElement(new { members = new[] { "fixture:court" }, gate = new[] { 180f, 30f },
                gateGap = 3f, treePitch = 3f, treeOffset = 1.5f, sharedGapBelow = 8f })
        };
        var globals = BuildInputSnapshot.Freeze(new Dictionary<string, ReadOnlyMemory<byte>>
        { ["vegetation/species.json"] = File.ReadAllBytes(Path.Combine(RepositoryFixture.Root, "game", "data", "vegetation", "species.json")) }, "fixture");
        world.Features = ((WorldFeature[])[parcel, group, remoteParcel]).ToDictionary(feature => feature.Id);
        baseline = Hash(Context(world, planting: planting, global: globals), "vegetation");
        world.Features = ((WorldFeature[])[parcel, Green("fixture:group", 180, 190) with
        { Kind = group.Kind, Props = group.Props }, remoteParcel]).ToDictionary(feature => feature.Id);
        if (Hash(Context(world, planting: planting, global: globals), "vegetation") == baseline)
            throw new InvalidOperationException("A sport wall in this parcel's connected neighbourhood did not invalidate vegetation.");

        static WorldFeature Green(string id, float x, float length) => WorldFeatureFixture.Create(id, "green",
            new PolygonGeometry(new([new(x, 10), new(x + length, 10), new(x + length, 50), new(x, 50)])));
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void SpeciesChangesOnlyItsConsumersAndWindowFingerprintIsNotACellDependency()
    {
        var world = new FakeWorld { Boundary = new([]) };
        var local = Feature("fixture:local", 10, "楼", "说明", "入口") with { Kind = new("building") };
        world.Features = ((WorldFeature[])[local]).ToDictionary(feature => feature.Id);
        var a = Context(world, "one"); var b = Context(world, "two");
        if (Hash(a, "vegetation") == Hash(b, "vegetation") || Hash(a, "buildings") != Hash(b, "buildings"))
            throw new InvalidOperationException("Species changes reached the wrong content modules.");
        var allocation = new WindowAllocation(local.Id, "main", 0, 2, 1);
        var first = new WindowNumberingSnapshot(a.Map.Id, new string('a', 64), [allocation]);
        var second = new WindowNumberingSnapshot(a.Map.Id, new string('b', 64), [allocation]);
        if (Hash(Context(world, numbering: first), "buildings") != Hash(Context(world, numbering: second), "buildings") ||
            Hash(Context(world, numbering: first), "buildings") == Hash(Context(world, numbering:
                new(a.Map.Id, new string('c', 64), [allocation with { Columns = 3 }])), "buildings"))
            throw new InvalidOperationException("The cell did not use its actual window allocation.");
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void InputReaderBorrowsTheFrozenLoadingVersion()
    {
        var raw = Encoding.UTF8.GetBytes("old-geo");
        var inputs = BuildInputSnapshot.Freeze(new Dictionary<string, ReadOnlyMemory<byte>> { ["world/geo.json"] = raw }, "fixture");
        var world = new FakeWorld { Boundary = new([]) };
        var map = new TestMapContext(Context(world).Map, world, inputs, "must-not-reread-source-files");
        var captured = BakeInputFingerprint.ReadInputs(map);
        raw[0] = (byte)'X';
        if (!ReferenceEquals(captured, inputs.Entries) || Encoding.UTF8.GetString(captured["world/geo.json"].Span) != "old-geo")
            throw new InvalidOperationException("The baker changed the captured input version.");
        try { BakeInputFingerprint.ReadInputs(map with { Inputs = BuildInputSnapshot.Empty }); }
        catch (InvalidDataException) { return; }
        throw new InvalidOperationException("A missing frozen snapshot was accepted.");
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void WindowContextForwardsTheFrozenResourcesAndCapabilities()
    {
        var original = Context(new FakeWorld { Boundary = new([]) });
        var forwarded = BakeWindowPreparation.Context(original, "module-code", original.WindowNumbering);
        if (!ReferenceEquals(original.SourceInputs, forwarded.SourceInputs) || !ReferenceEquals(original.GlobalInputs, forwarded.GlobalInputs) ||
            !ReferenceEquals(original.GroundMaskContributors, forwarded.GroundMaskContributors) || !ReferenceEquals(original.PartLods, forwarded.PartLods) ||
            !ReferenceEquals(original.Catalog, forwarded.Catalog))
            throw new InvalidOperationException("Preparation dropped a frozen resource or capability.");
    }

    [TestCase]
    [TestCategory(TestReason.Regression)]
    [RequireGodotRuntime]
    [TestCategory("NativeRender")]
    public void PartialLocalSourceIndexKeepsPackagedStampsWithoutRecomputingSources()
    {
        var context = Context(new FakeWorld { Boundary = new([]) });
        var directory = Path.Combine(Path.GetTempPath(), "uestc-rpg-source-index-" + Guid.NewGuid().ToString("N"));
        var options = new BakeOptions(Path.Combine(directory, "packaged"), false, false, false, false, 1, null);
        var key = new BakeKey(context.Map.Id, "resident", "source-index");
        const string codeIdentity = "current-code";
        var identity = BakeSourceIndexCache.Identity(codeIdentity);
        var stamp = new string('a', 32);
        var source = new BakeSourceIndex(context);
        var product = BakeRequestKeys.Tile(context.Map.Id, "ground", default, LodLevel.L0);
        source.RememberProduct(product, new string('b', 32));
        source.RememberProduct(product with { Detail = "L4/0/0" }, new string('c', 32));
        var saved = source.Snapshot("current", stamp) with
        { Cells = Enumerable.Range(0, 16384).Select(i => new BakeSourceCell(new(i, -i), stamp)).ToArray() };
        try
        {
            using var packaged = CreateTerrainCache(options);
            packaged.WriteProduct(key, identity, saved);
            using var local = CreateTerrainCache(options with { CacheRoot = Path.Combine(directory, "local"), PackagedCacheRoot = options.CacheRoot });
            var own = saved.Products[0] with { Stamp = new string('d', 32) };
            local.WriteProduct(key, identity, saved with { SourceVersion = "older", Products = [own] });
            var cache = new BakeSourceIndexCache(local, context.Map.Id, "current", codeIdentity);
            if (cache.ResolveResidentStamp(() => throw new InvalidOperationException("Warm source inputs were expanded.")) != stamp)
                throw new InvalidOperationException("The matching packaged source version was ignored.");
            var restored = new BakeSourceIndex(context, cache);
            if (!restored.TryProduct(own.Key, out var value) || value != own.Stamp ||
                saved.Products.Any(item => !restored.TryProduct(item.Key, out _)))
                throw new InvalidOperationException("A partial local index hid packaged keys or lost local precedence.");
            restored.Persist();
            var persisted = local.ReadProduct<BakeSourceIndexData>(key, identity)!;
            if (persisted.SourceVersion != saved.SourceVersion || persisted.Products.Length != saved.Products.Length || !persisted.Cells.SequenceEqual(saved.Cells))
                throw new InvalidOperationException("Merged product stamps were not persisted for the next startup.");
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true); }
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void BuildCodeManifestRejectsMissingOrDuplicateRoots()
    {
        var key = BakeCodeManifest.Chain(BakeCodeChains.Shared);
        var table = new BuildCodeIndex(new BakeCodeManifest(BakeCodeManifest.CurrentSchema, new Dictionary<string, string> { [key] = "shared-code" }, new Dictionary<string, string>()));
        if (table.Chain(BakeCodeChains.Shared) != "shared-code") throw new InvalidOperationException("Build root lookup changed its stored hash.");
        ExpectInvalid(() => table.Chain(BakeCodeChains.Collision));
        var duplicate = Encoding.UTF8.GetBytes("{\"schema\":2,\"registrations\":{},\"roots\":{\"chain:shared\":\"a\",\"chain:shared\":\"b\"}}");
        ExpectInvalid(() => BuildCodeIndex.Parse(duplicate));
        ExpectInvalid(() => new BuildCodeIndex(new BakeCodeManifest(BakeCodeManifest.CurrentSchema + 1,
            new Dictionary<string, string> { [key] = "shared-code" }, new Dictionary<string, string>())));

        static void ExpectInvalid(Action action)
        {
            try { action(); }
            catch (InvalidDataException) { return; }
            throw new InvalidOperationException("An invalid build code manifest was accepted.");
        }
    }

    [TestCase]
    [TestCategory(TestReason.Regression)]
    [RequireGodotRuntime]
    [TestCategory("NativeRender")]
    public void ChangedBuildCodeCannotRestoreSavedProductStamps()
    {
        var context = Context(new FakeWorld { Boundary = new([]) });
        var directory = Path.Combine(Path.GetTempPath(), "uestc-rpg-code-index-" + Guid.NewGuid().ToString("N"));
        var options = new BakeOptions(directory, false, false, false, false, 1, null);
        var table = new BuildCodeIndex(new BakeCodeManifest(BakeCodeManifest.CurrentSchema,
            new Dictionary<string, string> { [BakeCodeManifest.Chain(BakeCodeChains.Shared)] = "index-code" }, new Dictionary<string, string>()));
        var resident = new string('a', 32);
        var key = BakeRequestKeys.Tile(context.Map.Id, "ground", default, LodLevel.L0);
        var index = new BakeSourceIndex(context);
        index.RememberProduct(key, new string('b', 32));
        try
        {
            using var terrain = new TerrainCacheService(options, table);
            terrain.WriteProduct(new(context.Map.Id, "resident", "source-index"), BakeSourceIndexCache.Identity("before"), index.Snapshot("inputs", resident));
            var cache = new BakeSourceIndexCache(terrain, context.Map.Id, "inputs", "after");
            var recomputed = false;
            if (cache.ResolveResidentStamp(() => { recomputed = true; return resident; }) != resident || !recomputed || cache.Read() is not null)
                throw new InvalidOperationException("Changed build code reused saved product input stamps.");
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true); }
    }

    [TestCase, TestCategory(TestReason.Regression)]
    public void TerrainPointRequestsShareCanonicalBlocksAcrossOrderAndFractionalEdges()
    {
        Vector2[] points = [new(-.25f, -.25f), new(255.75f, 255.75f), new(3000.2f, -2000.8f)];
        var blocks = TerrainBlockCoverage.Points(points);
        var reordered = TerrainBlockCoverage.Points(points.Reverse().Concat(points).ToArray());
        if (!blocks.SequenceEqual(reordered)) throw new InvalidOperationException("Terrain block identity depends on point order or duplicates.");
        foreach (var point in points)
        {
            var grid = TerrainSampling.QueryGrid(point);
            for (var z = 0; z < grid.Height; z++)
                for (var x = 0; x < grid.Width; x++)
                    if (!blocks.Contains(TileGrid.At(grid.Origin + new Vector2(x, z), LodLevel.L2)))
                        throw new InvalidOperationException("A fractional query lost an authoritative interpolation node.");
        }
        if (blocks.Contains(new TileKey(5, 0))) throw new InvalidOperationException("Sparse terrain requests loaded unrelated intermediate blocks.");
        var region = TerrainBlockCoverage.Region(new(new(255.75f), new(255.75f))).ToHashSet();
        if (!region.SetEquals(TerrainBlockCoverage.Points([new(255.75f)])))
            throw new InvalidOperationException("Point and zero-area region queries disagree at a block edge.");
    }

    [TestCase, TestCategory(TestReason.Regression)]
    public void OfflineTerrainCoversRoamingFixedPosesAndExistingSightReach()
    {
        var source = Context(new FakeWorld());
        var title = new Pose(new(3000.2f, 495, -2928.1f), 13, -2.5f);
        var descriptor = source.Map with { Title = new(title, "fixture") };
        var context = new TestMapContext(descriptor, source.World, BuildInputSnapshot.Empty);
        var domain = TerrainBlockCoverage.OfflineDomain(context);
        var blocks = TerrainBlockCoverage.Region(domain).ToHashSet();
        var reach = descriptor.Roam.MaxRadius + WalkRules.LandingSightDistance;
        Vector2[] queries = [descriptor.Center - new Vector2(reach), descriptor.Center + new Vector2(reach),
            new(title.Position.X, title.Position.Z), new(title.Position.X + WalkRules.LandingSightDistance, title.Position.Z)];
        foreach (var point in queries)
            if (TerrainBlockCoverage.Points([point]).Any(block => !blocks.Contains(block)))
                throw new InvalidOperationException("Offline terrain omitted a legal/fixed query or its interpolation edge.");
    }

    private static string Hash(BuildContext context, string module) => BakeInputFingerprint.Compute(context,
        BakeRequestKeys.Tile(context.Map.Id, module, new(0, 0), LodLevel.L0), "runtime-code", new(context), Inputs(module));
    private static BakeInputDependencies Inputs(string module) => module switch
    {
        "vegetation" => new Campus.Gen.Vegetation.VegetationModule().BakeInputs,
        "furniture" => new Campus.Gen.Furniture.FurnitureModule().BakeInputs,
        "ground" => new Campus.Gen.Ground.GroundModule().BakeInputs,
        "buildings" => new Campus.Gen.Buildings.BuildingsModule().BakeInputs,
        "zone.north" => new Campus.Gen.Maps.Qingshuihe.North.NorthModule().BakeInputs,
        "zone.center-east" => new Campus.Gen.Maps.Qingshuihe.CenterEast.CenterEastModule().BakeInputs,
        "zone.west" => new Campus.Gen.Maps.Qingshuihe.West.WestModule().BakeInputs,
        "interior.research-six" => new Campus.Gen.Maps.Qingshuihe.West.SixInterior.SixCInteriorProvider().BakeInputs,
        _ => BakeInputDependencies.World
    };
}
