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
    public void ConstructionFreezesBothCallerDictionaryAndRawArrays()
    {
        var raw = new byte[] { 1, 2, 3 };
        var files = new Dictionary<string, ReadOnlyMemory<byte>>(StringComparer.Ordinal) { ["campus.json"] = raw };
        var input = new MapLoadInput(Entry: Entry(), Files: files, WalkBytes: ReadOnlyMemory<byte>.Empty);
        raw[0] = 9;
        files["campus.json"] = new byte[] { 7 };
        files["network.json"] = new byte[] { 8 };
        Require(input.Files.Count == 1 && input.Files["campus.json"].Span.SequenceEqual(new byte[] { 1, 2, 3 }),
            "Published map bytes still alias the caller's dictionary or array.");
        Require(ReferenceEquals(input.Files, input.SourceInputs.Entries), "Files is not backed by the frozen snapshot.");
    }

    [TestCase]
    public void PositionalDeconstructionAndWalkOnlyWithKeepTheSameMapSnapshot()
    {
        var entry = Entry();
        var original = new MapLoadInput(entry, Files(1), new byte[] { 3 });
        var regeneratedWalk = original with { WalkBytes = new byte[] { 4, 5 } };
        var (actualEntry, actualFiles, actualWalk) = regeneratedWalk;
        Require(ReferenceEquals(actualEntry, entry) && ReferenceEquals(original.SourceInputs, regeneratedWalk.SourceInputs) &&
            ReferenceEquals(actualFiles, original.SourceInputs.Entries), "Walk-only with copied or lost the map snapshot.");
        Require(actualWalk.Span.SequenceEqual(new byte[] { 4, 5 }) && original.WalkBytes.Span.SequenceEqual(new byte[] { 3 }),
            "Positional deconstruction or WalkBytes with semantics changed.");

        var replacementBytes = new byte[] { 6 };
        var replaced = original with
        {
            Files = new Dictionary<string, ReadOnlyMemory<byte>>(StringComparer.Ordinal) { ["campus.json"] = replacementBytes }
        };
        replacementBytes[0] = 9;
        Require(!ReferenceEquals(replaced.SourceInputs, original.SourceInputs) &&
            replaced.Files["campus.json"].Span[0] == 6 && original.Files["campus.json"].Span[0] == 1,
            "Files with did not create a separate frozen version.");
    }

    [TestCase]
    public void OldAndNewContextsRetainTheirOwnParsedInputVersionWithoutRefreezing()
    {
        var entry = Entry();
        var oldInput = new MapLoadInput(entry, Files(1), ReadOnlyMemory<byte>.Empty);
        var newInput = new MapLoadInput(entry, Files(2), ReadOnlyMemory<byte>.Empty);
        var oldWorld = Loaded(entry);
        var newWorld = Loaded(entry);
        var oldContext = entry.CreateContext(oldWorld, oldInput.SourceInputs);
        var newContext = entry.CreateContext(newWorld, newInput.SourceInputs);
        Require(ReferenceEquals(oldContext.World, oldWorld.World) && ReferenceEquals(newContext.World, newWorld.World),
            "A context changed the world to which its source version belongs.");
        Require(ReferenceEquals(oldContext.SourceInputs, oldInput.SourceInputs) &&
            ReferenceEquals(newContext.SourceInputs, newInput.SourceInputs) &&
            !ReferenceEquals(oldContext.SourceInputs, newContext.SourceInputs), "Context construction refroze or mixed map versions.");
        Require(oldContext.SourceInputs.Require("campus.json").Span[0] == 1 &&
            newContext.SourceInputs.Require("campus.json").Span[0] == 2, "An old context observed newer source bytes.");
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
        return new(descriptor, "res://data/fixture/", "res://bake/fixture/boot/", []);
    }
    private static WorldModelLoadResult Loaded(MapCatalogEntry entry)
    {
        var world = new MutableWorldFixture();
        world.Configure([], boundary: new PolygonRings([new(-64, -64), new(64, -64), new(64, 64), new(-64, 64)]));
        return new(world, entry.Descriptor.Geo, entry.Descriptor.Center, entry.Descriptor.Bounds,
            entry.Descriptor.TileMin, entry.Descriptor.TileMax);
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
