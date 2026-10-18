using Campus.Gen.Tests.Support;
using Campus.Pipeline.Map;
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
        var raw = Encoding.UTF8.GetBytes("old-campus");
        var inputs = BuildInputSnapshot.Freeze(new Dictionary<string, ReadOnlyMemory<byte>> { ["campus.json"] = raw });
        var world = new FakeWorld { Boundary = new([]) };
        var map = new TestMapContext(Context(world).Map, world, inputs, "must-not-reread-source-files");
        var captured = BakeInputFingerprint.ReadInputs(map);
        raw[0] = (byte)'X';
        if (!ReferenceEquals(captured, inputs.Entries) || Encoding.UTF8.GetString(captured["campus.json"].Span) != "old-campus")
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
        BakeRequestKeys.Tile(context.Map.Id, module, new(0, 0), LodLevel.L0), "runtime-code");
    private static WorldFeature Feature(string id, float x, string name, string note, string text) =>
        WorldFeatureFixture.Create(id, "prop", new PointGeometry(new(x, 0, 10))) with
        {
            Bounds = new(new(x, 0, 10), new(x + 1, 2, 11)), Name = name,
            Props = JsonSerializer.SerializeToElement(new { note, text }, ContractJson.Options)
        };
    private static BuildContext Context(IWorldModel world, string species = "one", WindowNumberingSnapshot? numbering = null, string? planting = null) => new()
    {
        Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog, World = world, WorldSeed = 1, CodeHash = "module-code", WindowNumbering = numbering,
        Map = new(new("fixture"), new("Fixture", "Fixture"), MapKind.Outdoor, 1, 1,
            new(0, 0, 32648, Vector2.Zero, new(64, 64)), new(32, 32), new(Vector3.Zero, new(64, 64, 64)),
            new(Vector2.Zero, new(64, 64)), new(1, 0, 64, 0, 64, 1, 1, 1), new(0, 0), new(0, 0), "start", [],
            new(MapTimeRule.Shared, new(17.5, Season.Autumn), new("Default", "Default"), false, "fixture"),
            new("sky.json", "backdrop.json", []), new Dictionary<string, string>(), []),
        GlobalInputs = BuildInputSnapshot.Freeze(new Dictionary<string, ReadOnlyMemory<byte>>
            { ["vegetation/species.json"] = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { species })) }),
        SourceInputs = planting is null ? new Dictionary<string, ReadOnlyMemory<byte>>() :
            new Dictionary<string, ReadOnlyMemory<byte>> { ["vegetation/planting.json"] = Encoding.UTF8.GetBytes(planting) },
        PartLods = new PartLodCatalog([new(new PartId("fixture.part"), new BakeFixturePart())])
    };
}

internal sealed class BakeFixturePart : IPartDef
{
    public IReadOnlyList<LodLevel> SupportedLods => [LodLevel.L0, LodLevel.L2];
}
