using Campus.Gen.Tests.Support;
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
    [TestCategory(TestReason.Contract)]
    [RequireGodotRuntime]
    public async Task ProductionAndDiskCaptureAgreeWhenOptionalMapResourcesAreAbsent()
    {
        var entry = RealMapFixture.Entry();
        var expected = RealMapFixture.ReadFiles();
        var input = await new MapDataSource(new Campus.Kernel.Boot.RequiredResources()).ReadLoadInputAsync(entry);
        var actual = input.Files;
        AssertThat(actual.Keys.Order(StringComparer.Ordinal).SequenceEqual(expected.Keys.Order(StringComparer.Ordinal))).IsTrue();
        foreach (var (name, bytes) in expected)
            AssertThat(actual[name].Span.SequenceEqual(bytes.Span)).IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void ConstructionFreezesBothCallerDictionaryAndRawArrays()
    {
        var raw = new byte[] { 1, 2, 3 };
        var files = new Dictionary<string, ReadOnlyMemory<byte>>(StringComparer.Ordinal) { ["world/geo.json"] = raw };
        var input = new MapLoadInput(Entry: Entry(), SourceInputs: BuildInputSnapshot.Freeze(files, "fixture-map-1"));
        raw[0] = 9;
        files["world/geo.json"] = new byte[] { 7 };
        files["world/core.json"] = new byte[] { 8 };
        AssertThat(input.Files.Count == 1 && input.Files["world/geo.json"].Span.SequenceEqual(new byte[] { 1, 2, 3 })).OverrideFailureMessage("Published map bytes still alias the caller's dictionary or array.").IsTrue();
        AssertThat(ReferenceEquals(input.Files, input.SourceInputs.Entries)).OverrideFailureMessage("Files is not backed by the frozen snapshot.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void ReplacementSnapshotPublishesANewInputVersion()
    {
        var original = new MapLoadInput(Entry(), BuildInputSnapshot.Freeze(Files(1), "fixture-map-1"));
        var replacementBytes = new byte[] { 6 };
        var replaced = original with
        {
            SourceInputs = BuildInputSnapshot.Freeze(new Dictionary<string, ReadOnlyMemory<byte>>(StringComparer.Ordinal)
                { ["world/geo.json"] = replacementBytes }, "fixture-map-2")
        };
        replacementBytes[0] = 9;
        AssertThat(!ReferenceEquals(replaced.SourceInputs, original.SourceInputs) &&
            replaced.SourceInputs.Version == "fixture-map-2" && original.SourceInputs.Version == "fixture-map-1" &&
            replaced.Files["world/geo.json"].Span[0] == 6 && original.Files["world/geo.json"].Span[0] == 1).OverrideFailureMessage("Snapshot replacement did not retain separate frozen versions.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void OldAndNewContextsRetainTheirOwnParsedInputVersionWithoutRefreezing()
    {
        var entry = Entry();
        var oldInput = new MapLoadInput(entry, BuildInputSnapshot.Freeze(Files(1), "fixture-map-1"));
        var newInput = new MapLoadInput(entry, BuildInputSnapshot.Freeze(Files(2), "fixture-map-2"));
        var oldWorld = Loaded(entry);
        var newWorld = Loaded(entry);
        var oldContext = MapWorldLoading.CreateContext(entry, oldWorld, oldInput.SourceInputs);
        var newContext = MapWorldLoading.CreateContext(entry, newWorld, newInput.SourceInputs);
        AssertThat(ReferenceEquals(oldContext.World, oldWorld.World) && ReferenceEquals(newContext.World, newWorld.World)).OverrideFailureMessage("A context changed the world to which its source version belongs.").IsTrue();
        AssertThat(ReferenceEquals(oldContext.SourceInputs, oldInput.SourceInputs) &&
            ReferenceEquals(newContext.SourceInputs, newInput.SourceInputs) &&
            !ReferenceEquals(oldContext.SourceInputs, newContext.SourceInputs)).OverrideFailureMessage("Context construction refroze or mixed map versions.").IsTrue();
        AssertThat(oldContext.SourceInputs.Require("world/geo.json").Span[0] == 1 &&
            newContext.SourceInputs.Require("world/geo.json").Span[0] == 2).OverrideFailureMessage("An old context observed newer source bytes.").IsTrue();
    }

    private static IReadOnlyDictionary<string, ReadOnlyMemory<byte>> Files(byte version) =>
        new Dictionary<string, ReadOnlyMemory<byte>>(StringComparer.Ordinal) { ["world/geo.json"] = new byte[] { version } };
    private static MapCatalogEntry Entry()
    {
        var descriptor = new MapDescriptor(new("fixture-map"), new("测试地图", "Fixture"), MapKind.Outdoor, 1, 1,
            new(0, 0, 32648, new(-64), new(64)), Vector2.Zero,
            new(new(-64, -28, -64), new(64, 2000, 64)), new(new(-64), new(64)),
            new(0.9f, 30, 3000, -28, 2000, 30, 40, 150), new(-1, -1), new(0, 0),
            "spawn", [new("spawn", new(new(0, 20, 30), 0, -15), null)],
            new(MapTimeRule.Shared, new(17.5, Season.Autumn), new("默认", "Default"), false, "fixture"),
            new("sky.json", "backdrop.json", []), []);
        return new(descriptor, "res://data/fixture/", []);
    }
    private static WorldModelLoadResult Loaded(MapCatalogEntry entry)
    {
        var world = new FakeWorld { Boundary = new([new(-64, -64), new(64, -64), new(64, 64), new(-64, 64)]) };
        return new(world, entry.Descriptor.Geo, entry.Descriptor.Center, entry.Descriptor.Bounds,
            entry.Descriptor.TileMin, entry.Descriptor.TileMax);
    }
}
