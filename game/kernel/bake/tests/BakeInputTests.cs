using Campus.Gen.Tests.Support;
using Campus.Gen.World;
using Campus.Pipeline.Map;
using System.Collections.Concurrent;
using System.Numerics;
using System.Text;
using System.Text.Json;
using Campus.Contracts;
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
    public void SceneAtlasChangesInvalidateItsRenderConsumersWithoutChangingCollision()
    {
        var world = new FakeWorld { Boundary = new([]) };
        var before = Context(world, global: Atlas(512));
        var after = Context(world, global: Atlas(1024));
        foreach (var module in new[] { "furniture", "zone.north", "zone.center-east", "zone.west" })
        {
            if (Hash(before, module) == Hash(after, module))
                throw new InvalidOperationException("Scene atlas UV changes did not invalidate a rendered consumer.");
            var key = new BakeKey(before.Map.Id, module, "C/0/0");
            if (BakeInputFingerprint.Compute(before, key, "runtime-code", new(world)) !=
                BakeInputFingerprint.Compute(after, key, "runtime-code", new(world)))
                throw new InvalidOperationException("Scene atlas changes invalidated collision.");
        }
        if (Hash(before, "ground") != Hash(after, "ground"))
            throw new InvalidOperationException("Scene atlas changes invalidated unrelated content.");

        static BuildInputSnapshot Atlas(int size) => BuildInputSnapshot.Freeze(
            new Dictionary<string, ReadOnlyMemory<byte>>
            { [SceneTextInputs.AtlasKey] = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { size })) });
    }

    [TestCase]
    [TestCategory(TestReason.Regression)]
    public void MapBuildSamplesAreSharedAcrossKeysWithoutChangingFingerprintsOrCrossingWorlds()
    {
        var field = new CountingFingerprintField(1);
        var context = Context(new FakeWorld { Boundary = new([]), Terrain = field, Fidelity = field });
        var map = Map(context);
        using var lifetime = map.Lifetime;
        var keys = new[]
        {
            new BakeKey(context.Map.Id, "collision.world", "C/0/0"),
            new BakeKey(context.Map.Id, "collision.world", "C/1/0"),
            BakeRequestKeys.Tile(context.Map.Id, "ground", default, LodLevel.L0)
        };
        var referenceField = new CountingFingerprintField(1);
        var reference = Context(new FakeWorld { Boundary = new([]), Terrain = referenceField, Fidelity = referenceField });
        var expected = keys.Select(key => BakeInputFingerprint.Compute(reference, key, "runtime-code", new(reference.World))).ToArray();
        var actual = new string[keys.Length];
        Parallel.For(0, keys.Length, index => actual[index] =
            BakeInputFingerprint.Compute(context, keys[index], "runtime-code", samples: map.FingerprintSamples));
        if (!actual.SequenceEqual(expected) || field.PointReads.IsEmpty ||
            field.PointReads.Keys.Any(key => key.Kind is "height" or "facts") ||
            !field.PointReads.Keys.Any(key => key.Kind == "sample") || field.PointReads.Values.Any(count => count != 1) || field.TileReads.Values.Any(count => count != 1))
            throw new InvalidOperationException("Concurrent keys changed their fingerprints or repeated a shared terrain/fidelity read.");

        var changedField = new CountingFingerprintField(2);
        var changed = Context(new FakeWorld { Boundary = new([]), Terrain = changedField, Fidelity = changedField });
        var nextMap = Map(changed);
        using var nextLifetime = nextMap.Lifetime;
        var next = BakeInputFingerprint.Compute(changed, keys[0], "runtime-code", samples: nextMap.FingerprintSamples);
        if (next == actual[0] || changedField.PointReads.IsEmpty || changedField.TileReads.IsEmpty ||
            !changedField.PointReads.Keys.Any(key => key.Kind == "sample") ||
            changedField.PointReads.Keys.Any(key => key.Kind is "height" or "facts") ||
            changedField.PointReads.Values.Any(count => count != 1) || changedField.TileReads.Values.Any(count => count != 1) ||
            next != BakeInputFingerprint.Compute(changed, keys[0], "runtime-code", new(changed.World)))
            throw new InvalidOperationException("The next MapBuild reused the preceding world's samples.");

        static BakeService.MapBuild Map(BuildContext context) => new(
            new TestMapContext(context.Map, context.World), new Dictionary<string, ContentModuleInput>(),
            new Dictionary<string, BakeService.Interior>(), [], null!, null!, context.ClaimedBy, null!,
            context.GlobalInputs, context.GroundMaskContributors, context.PartLods!, context.Catalog);
    }

    [TestCase]
    [TestCategory(TestReason.Regression)]
    public void MissingPadsDoNotBlockFingerprintsAndPadPresenceAndHeightChangeThem()
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
        { ["vegetation/species.json"] = File.ReadAllBytes(Path.Combine(RepositoryFixture.Root, "game", "data", "vegetation", "species.json")) });
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
        var inputs = BuildInputSnapshot.Freeze(new Dictionary<string, ReadOnlyMemory<byte>> { ["world/geo.json"] = raw });
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
        var forwarded = BakeWindowPreparation.Context(original, "cell-input", "module-code", null);
        if (!ReferenceEquals(original.SourceInputs, forwarded.SourceInputs) || !ReferenceEquals(original.GlobalInputs, forwarded.GlobalInputs) ||
            !ReferenceEquals(original.GroundMaskContributors, forwarded.GroundMaskContributors) || !ReferenceEquals(original.PartLods, forwarded.PartLods) ||
            !ReferenceEquals(original.Catalog, forwarded.Catalog))
            throw new InvalidOperationException("Preparation dropped a frozen resource or capability.");
    }

    private static string Hash(BuildContext context, string module) => BakeInputFingerprint.Compute(context,
        BakeRequestKeys.Tile(context.Map.Id, module, new(0, 0), LodLevel.L0), "runtime-code", new(context.World));
    private static WorldFeature Feature(string id, float x, string name, string note, string text) =>
        WorldFeatureFixture.Create(id, "prop", new PointGeometry(new(x, 0, 10))) with
        {
            Bounds = new(new(x, 0, 10), new(x + 1, 2, 11)), Name = name,
            Props = JsonSerializer.SerializeToElement(new { note, text }, ContractJson.Options)
        };
    private static BuildContext Context(IWorldModel world, string species = "one", WindowNumberingSnapshot? numbering = null, string? planting = null,
        BuildInputSnapshot? global = null) => new()
    {
        Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog, World = world, WorldSeed = 1, CodeHash = "module-code", WindowNumbering = numbering,
        Map = new(new("fixture"), new("Fixture", "Fixture"), MapKind.Outdoor, 1, 1,
            new(0, 0, 32648, Vector2.Zero, new(64, 64)), new(32, 32), new(Vector3.Zero, new(64, 64, 64)),
            new(Vector2.Zero, new(64, 64)), new(1, 0, 64, 0, 64, 1, 1, 1), new(0, 0), new(0, 0), "start", [],
            new(MapTimeRule.Shared, new(17.5, Season.Autumn), new("Default", "Default"), false, "fixture"),
            new("sky.json", "backdrop.json", []), new Dictionary<string, string>(), []),
        GlobalInputs = global ?? BuildInputSnapshot.Freeze(new Dictionary<string, ReadOnlyMemory<byte>>
            { ["vegetation/species.json"] = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { species })) }),
        SourceInputs = planting is null ? new Dictionary<string, ReadOnlyMemory<byte>>() :
            new Dictionary<string, ReadOnlyMemory<byte>> { ["vegetation/planting.json"] = Encoding.UTF8.GetBytes(planting) },
        PartLods = new PartLodCatalog([new(new PartId("fixture.part"), new BakeFixturePart())])
    };

    private sealed class CountingFingerprintField(float height) : ITerrain, IFidelityModel
    {
        public ConcurrentDictionary<(string Kind, int X, int Z), int> PointReads { get; } = new();
        public ConcurrentDictionary<(TileKey Tile, int Level), int> TileReads { get; } = new();
        private void Read(string kind, float x, float z) => PointReads.AddOrUpdate(
            (kind, BitConverter.SingleToInt32Bits(x), BitConverter.SingleToInt32Bits(z)), 1, (_, count) => count + 1);
        public bool IsFlat(TileKey tile) => height == 0;
        public float HeightAt(float x, float z) { Read("height", x, z); return height; }
        public (float Height, TerrainFacts Facts) SampleAt(float x, float z)
        {
            Read("sample", x, z);
            return (height, new(0, TerrainClass.Flat, float.PositiveInfinity, float.PositiveInfinity));
        }
        public TerrainFacts FactsAt(float x, float z) { Read("facts", x, z); return new(0, TerrainClass.Flat, float.PositiveInfinity, float.PositiveInfinity); }
        public void FillTile(TileKey tile, int level, Span<short> cm)
        {
            TileReads.AddOrUpdate((tile, level), 1, (_, count) => count + 1);
            cm.Fill(checked((short)(height / TerrainSampling.Centimetre)));
        }
        public float PadY(FeatureId feature) => throw new KeyNotFoundException();
        public bool TryGetPadY(FeatureId feature, out float y) { y = 0; return false; }
        public float? WaterY(FeatureId water) => null;
        public IReadOnlyList<FacadeSegment> Facades(FeatureId building) => [];
        public RoofZone Roof(FeatureId building) => new(building, FidelityZone.K3);
        public FidelityZone GroundAt(float x, float z) { Read("ground", x, z); return FidelityZone.K3; }
        public float WalkDistanceAt(float x, float z) { Read("walk", x, z); return 0; }
        public string InputHash => "fixture";
    }
}

internal sealed class BakeFixturePart : IPartDef
{
    public IReadOnlyList<LodLevel> SupportedLods => [LodLevel.L0, LodLevel.L2];
}
