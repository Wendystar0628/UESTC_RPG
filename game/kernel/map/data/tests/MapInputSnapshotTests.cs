using Campus.Gen.World;
using static GdUnit4.Assertions;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Kernel.Maps;
using GdUnit4;

namespace Campus.Tests;

[TestSuite]
public sealed class MapInputSnapshotTests
{
    [TestCase]
    [RequireGodotRuntime]
    public void ProductionAndDiskCaptureAgreeWhenOptionalMapResourcesAreAbsent()
    {
        var entry = RealMapFixture.Entry();
        var expected = RealMapFixture.ReadFiles();
        var actual = new MapDataSource().ReadLoadInput(entry).Files;
        AssertThat(actual.Keys.Order(StringComparer.Ordinal).SequenceEqual(expected.Keys.Order(StringComparer.Ordinal))).IsTrue();
        foreach (var (name, bytes) in expected)
            AssertThat(actual[name].Span.SequenceEqual(bytes.Span)).IsTrue();
    }

    [TestCase]
    public void ConstructionFreezesBothCallerDictionaryAndRawArrays()
    {
        var raw = new byte[] { 1, 2, 3 };
        var files = new Dictionary<string, ReadOnlyMemory<byte>>(StringComparer.Ordinal) { ["campus.json"] = raw };
        var input = new MapLoadInput(Entry: Entry(), Files: files);
        raw[0] = 9;
        files["campus.json"] = new byte[] { 7 };
        files["network.json"] = new byte[] { 8 };
        AssertThat(input.Files.Count == 1 && input.Files["campus.json"].Span.SequenceEqual(new byte[] { 1, 2, 3 })).OverrideFailureMessage("Published map bytes still alias the caller's dictionary or array.").IsTrue();
        AssertThat(ReferenceEquals(input.Files, input.SourceInputs.Entries)).OverrideFailureMessage("Files is not backed by the frozen snapshot.").IsTrue();
    }

    [TestCase]
    public void ReplacementFilesFreezeANewInputVersion()
    {
        var original = new MapLoadInput(Entry(), Files(1));
        var replacementBytes = new byte[] { 6 };
        var replaced = original with
        {
            Files = new Dictionary<string, ReadOnlyMemory<byte>>(StringComparer.Ordinal) { ["campus.json"] = replacementBytes }
        };
        replacementBytes[0] = 9;
        AssertThat(!ReferenceEquals(replaced.SourceInputs, original.SourceInputs) &&
            replaced.Files["campus.json"].Span[0] == 6 && original.Files["campus.json"].Span[0] == 1).OverrideFailureMessage("Files with did not create a separate frozen version.").IsTrue();
    }

    [TestCase]
    public void OldAndNewContextsRetainTheirOwnParsedInputVersionWithoutRefreezing()
    {
        var entry = Entry();
        var oldInput = new MapLoadInput(entry, Files(1));
        var newInput = new MapLoadInput(entry, Files(2));
        var oldWorld = Loaded(entry);
        var newWorld = Loaded(entry);
        var oldContext = MapWorldLoading.CreateContext(entry, oldWorld, oldInput.SourceInputs);
        var newContext = MapWorldLoading.CreateContext(entry, newWorld, newInput.SourceInputs);
        AssertThat(ReferenceEquals(oldContext.World, oldWorld.World) && ReferenceEquals(newContext.World, newWorld.World)).OverrideFailureMessage("A context changed the world to which its source version belongs.").IsTrue();
        AssertThat(ReferenceEquals(oldContext.SourceInputs, oldInput.SourceInputs) &&
            ReferenceEquals(newContext.SourceInputs, newInput.SourceInputs) &&
            !ReferenceEquals(oldContext.SourceInputs, newContext.SourceInputs)).OverrideFailureMessage("Context construction refroze or mixed map versions.").IsTrue();
        AssertThat(oldContext.SourceInputs.Require("campus.json").Span[0] == 1 &&
            newContext.SourceInputs.Require("campus.json").Span[0] == 2).OverrideFailureMessage("An old context observed newer source bytes.").IsTrue();
    }

    private static IReadOnlyDictionary<string, ReadOnlyMemory<byte>> Files(byte version) =>
        new Dictionary<string, ReadOnlyMemory<byte>>(StringComparer.Ordinal) { ["campus.json"] = new byte[] { version } };
    private static MapCatalogEntry Entry()
    {
        var descriptor = new MapDescriptor(new("fixture-map"), new("测试地图", "Fixture"), MapKind.Outdoor, 1, 1,
            new(0, 0, 32648, new(-64), new(64)), Vector2.Zero,
            new(new(-64, -28, -64), new(64, 2000, 64)), new(new(-64), new(64)),
            new(0.9f, 30, 3000, -28, 2000, 30, 40, 150), new(-1, -1), new(0, 0),
            "spawn", [new("spawn", new(new(0, 20, 30), 0, -15), null)],
            new(MapTimeRule.Shared, new(17.5, Season.Autumn), new("默认", "Default"), false, "fixture"),
            new("sky.json", "backdrop.json", []), new Dictionary<string, string>(), []);
        return new(descriptor, "res://data/fixture/", []);
    }
    private static WorldModelLoadResult Loaded(MapCatalogEntry entry)
    {
        var world = new MutableWorldFixture();
        world.Configure([], boundary: new PolygonRings([new(-64, -64), new(64, -64), new(64, 64), new(-64, 64)]));
        return new(world, entry.Descriptor.Geo, entry.Descriptor.Center, entry.Descriptor.Bounds,
            entry.Descriptor.TileMin, entry.Descriptor.TileMax);
    }
}
