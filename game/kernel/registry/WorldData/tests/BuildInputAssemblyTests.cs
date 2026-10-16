using System.Security.Cryptography;
using System.Text;
using Campus.Contracts;
using Campus.Kernel.Maps;
using GdUnit4;

namespace Campus.Tests;

[TestSuite]
public sealed class BuildInputAssemblyTests
{
    [TestCase]
    public void AssetBoundaryReadsAndFreezesOnceAcrossMapConsumers()
    {
        var raw = Encoding.UTF8.GetBytes("species-version-one");
        var reads = 0;
        var assets = new Lazy<BuildInputSnapshot>(() => BuildInputAssembly.ReadRequiredGlobalInputs(
            ["vegetation/species.json"], key => { reads++; return raw; }));
        var firstMap = assets.Value;
        raw[0] = 0;
        var secondMap = assets.Value;
        Require(reads == 1 && ReferenceEquals(firstMap, secondMap), "Asset bytes were read or frozen again per map.");
        Require(firstMap.Require("vegetation/species.json").Span[0] == (byte)'s', "Published bytes alias a caller-owned mutable array.");
        var empty = BuildInputAssembly.ReadRequiredGlobalInputs([], _ => throw new InvalidOperationException("Unexpected fixture IO."));
        Require(ReferenceEquals(empty, BuildInputSnapshot.Empty), "A fixture with no requested global assets lost its empty default.");
    }

    [TestCase]
    public void RequiredInputsFailWithScopeAndKeyBeforeAnyConsumerRuns()
    {
        var missing = Throws<InvalidDataException>(() => BuildInputAssembly.ReadRequiredGlobalInputs(
            ["vegetation/species.json"], _ => ReadOnlyMemory<byte>.Empty));
        Require(missing.Message.Contains("scope=global", StringComparison.Ordinal) &&
            missing.Message.Contains("vegetation/species.json", StringComparison.Ordinal), "Missing-input error lost scope or key.");
        var read = false;
        _ = Throws<ArgumentException>(() => BuildInputAssembly.ReadRequiredGlobalInputs(
            ["res://data/vegetation/species.json"], _ => { read = true; return new byte[] { 1 }; }));
        Require(!read, "A non-canonical key reached the IO boundary.");
    }

    [TestCase]
    public void ScopedRawInputChangesInvalidateOnlyTheAffectedInputSets()
    {
        var speciesOne = Snapshot(("vegetation/species.json", "species-1"));
        var speciesTwo = Snapshot(("vegetation/species.json", "species-2"));
        var mapA = Snapshot(("campus.json", "campus-a"), ("vegetation/planting.json", "planting-a"));
        var mapB = Snapshot(("campus.json", "campus-b"), ("vegetation/planting.json", "planting-b"));
        var changedA = Snapshot(("campus.json", "campus-a"), ("vegetation/planting.json", "planting-a-edited"));
        var aBefore = Hash(speciesOne, mapA.Entries);
        var bBefore = Hash(speciesOne, mapB.Entries);
        Require(aBefore != Hash(speciesTwo, mapA.Entries) && bBefore != Hash(speciesTwo, mapB.Entries),
            "A species edit did not invalidate every consuming map's input hash.");
        Require(aBefore != Hash(speciesOne, changedA.Entries) && bBefore == Hash(speciesOne, mapB.Entries),
            "A planting edit affected the wrong map or failed to invalidate its own map.");
        Require(Hash(speciesOne, Snapshot(("value.json", "{\"a\":1}")).Entries) !=
            Hash(speciesOne, Snapshot(("value.json", "{\"a\": 1}")).Entries), "Hashing lost changes to actual raw input bytes.");
    }

    [TestCase]
    public void ScopeAndLengthFramingPreventAmbiguousRecordsAndIgnoreDictionaryInsertionOrder()
    {
        var one = Snapshot(("same.json", "payload"));
        Require(Hash(one, BuildInputSnapshot.Empty.Entries) != Hash(BuildInputSnapshot.Empty, one.Entries),
            "Global and map keys collided.");
        Require(Hash(BuildInputSnapshot.Empty, Snapshot(("ab", "c")).Entries) !=
            Hash(BuildInputSnapshot.Empty, Snapshot(("a", "bc")).Entries), "Key/raw-byte boundaries collided.");
        var forward = new Dictionary<string, ReadOnlyMemory<byte>>(StringComparer.Ordinal)
        { ["a.json"] = new byte[] { 1 }, ["z.json"] = new byte[] { 2 } };
        var reverse = new Dictionary<string, ReadOnlyMemory<byte>>(StringComparer.Ordinal)
        { ["z.json"] = new byte[] { 2 }, ["a.json"] = new byte[] { 1 } };
        Require(Hash(one, forward) == Hash(one, reverse), "Input dictionary iteration order changed the hash.");
    }

    [TestCase]
    public void ContributorsAreReadOnlyAndChannelConflictsFailBeforePainting()
    {
        var first = new Contributor("z", GroundMaskChannels.CanopyShelter);
        var second = new Contributor("a", GroundMaskChannels.GinkgoLeaves);
        var source = new List<IGroundMaskContributor> { first, second };
        var snapshot = BuildInputAssembly.FreezeGroundMaskContributors(source);
        source.Clear();
        Require(snapshot.Select(item => item.Id).SequenceEqual(["a", "z"]), "Contributor snapshot was mutable or unsorted.");
        _ = Throws<InvalidOperationException>(() => BuildInputAssembly.FreezeGroundMaskContributors(
            [second, new Contributor("conflict", GroundMaskChannels.GinkgoLeaves)]));
        _ = Throws<InvalidOperationException>(() => BuildInputAssembly.FreezeGroundMaskContributors(
            [second, new Contributor("a", GroundMaskChannels.OtherLeaves)]));
        _ = Throws<InvalidOperationException>(() => BuildInputAssembly.FreezeGroundMaskContributors(
            [new Contributor("unknown", (GroundMaskChannels)8)]));
        Require(first.PaintCalls == 0 && second.PaintCalls == 0, "Composition painted buffers before ownership validation completed.");
    }

    private static BuildInputSnapshot Snapshot(params (string Key, string Value)[] entries) =>
        BuildInputSnapshot.Freeze(entries.ToDictionary(entry => entry.Key,
            entry => (ReadOnlyMemory<byte>)Encoding.UTF8.GetBytes(entry.Value), StringComparer.Ordinal));
    private static string Hash(BuildInputSnapshot globals, IReadOnlyDictionary<string, ReadOnlyMemory<byte>> map)
    {
        // This tests the framed input stream, not a second production hash algorithm. WP-02a keeps its existing XxHash128.
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        BuildInputAssembly.AppendScopedInputs(bytes => hash.AppendData(bytes.Span), globals, map);
        return Convert.ToHexStringLower(hash.GetHashAndReset());
    }
    private static T Throws<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T error) { return error; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }

    // No registration attributes or test-assembly scanning: this exercises a detached capability list only.
    private sealed class Contributor(string id, GroundMaskChannels channels) : IContentModule, IGroundMaskContributor
    {
        public string Id => id;
        GroundMaskChannels IGroundMaskContributor.Channels => channels;
        public int PaintCalls { get; private set; }
        public IReadOnlyList<Claim> Claims => [];
        public ModuleBudget Budget { get; } = new(0, 0, 0);
        public IEnumerable<TileKey> Tiles(BuildContext context) => [];
        public ContentOutput Build(BuildContext context, TileKey tile, LodLevel lod) => ContentOutput.Empty;
        public void Paint(BuildContext context, in GroundMaskGrid grid, Span<byte> mask1, Span<byte> mask2) => PaintCalls++;
    }
}
