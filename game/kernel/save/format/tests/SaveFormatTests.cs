using System.Buffers.Binary;
using System.IO.Compression;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Campus.Contracts;
using Campus.Contracts.Save;
using GdUnit4;

namespace Campus.Kernel.Save.Format.Tests;

[TestSuite]
public sealed class SaveFormatTests
{
    private static long _testId;
    private static SaveMeta Meta(ulong gen = 1, ulong line = 1) => new(1, "p0-golden", new("s1"), SlotKind.Auto,
        gen, line, 20, new("test-map"), 17.5, 17.5, Season.Autumn, WeatherKind.Clear, ViewMode.Fly,
        new(new Vector3(1, 2, 3), 4, 5), null, null, null, 1792082400, SaveReason.Test);
    private static SaveDocument Document(ulong gen = 1, ulong line = 1) => new(Meta(gen, line),
        new SortedDictionary<string, RawSaveSection>(StringComparer.Ordinal)
        { ["world"] = new(1, "{\"v\":1,\"data\":{\"hours\":17.5}}"u8.ToArray()) }, null);
    private static SaveSections Sections(TestSection? section = null, int version = 1, RestorePhase phase = RestorePhase.World) =>
        new([new(new("world") { Owner = "WP-50", Version = version, Phase = phase, Order = 10 }, section ?? new())]);
    private static string TestDirectory()
    {
        var root = Environment.GetEnvironmentVariable("UESTC_REPOSITORY") ?? FindRoot();
        return Path.GetFullPath(Path.Combine(root, "output", "save-tests", "case-" + Environment.ProcessId + "-" + Interlocked.Increment(ref _testId)));
    }
    private static string FindRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "ownership.json"))) return directory.FullName;
        throw new InvalidOperationException("Set UESTC_REPOSITORY to the tested worktree.");
    }
    private static void Check(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject(Action action, bool newer = false)
    {
        try { action(); }
        catch (SaveFormatException error) { Check(error.NewerBuild == newer, "Incorrect newer/corrupt classification."); return; }
        throw new InvalidOperationException("Invalid save was accepted.");
    }
    private static void Rehash(byte[] bytes)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        hash.AppendData(bytes, 0, 32); hash.AppendData(bytes, 64, bytes.Length - 64);
        hash.GetHashAndReset().CopyTo(bytes, 32);
    }
    private static byte[] ReplaceCompressedBody(byte[] compressed)
    {
        var source = SaveContainer.Encode(Document());
        var metaLength = (int)BinaryPrimitives.ReadUInt32LittleEndian(source.AsSpan(12));
        var bytes = new byte[64 + metaLength + compressed.Length];
        source.AsSpan(0, 64 + metaLength).CopyTo(bytes);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(16), (uint)compressed.Length);
        compressed.CopyTo(bytes, 64 + metaLength); Rehash(bytes); return bytes;
    }

    [TestCase]
    public void ThousandDeterministicRoundTripsPreserveNumericBitsAndUnknownEnvelopes()
    {
        var rng = new Rng(50);
        var unknown = Encoding.UTF8.GetBytes("{ \"v\" : 47, \"data\" : {\"unseen\": [1, 2, 3], \"text\":\"未来\"} }");
        for (var i = 0; i < 1000; i++)
        {
            var hours = rng.NextFloat() * 1000000d + rng.NextFloat() / 1000000d;
            var section = new TestSection { Current = new(hours) };
            var registry = Sections(section);
            var raw = registry.Serialize([new("world", registry.Registered[0], section.Current)],
                new Dictionary<string, RawSaveSection> { ["unseen"] = new(47, unknown) });
            var decoded = SaveContainer.Decode(SaveContainer.Encode(new(Meta(), raw, null)), 1, new("s1"));
            var prepared = registry.Prepare(decoded);
            Check(BitConverter.DoubleToInt64Bits(((TestState)prepared.Sections[0].Dto!).Hours) == BitConverter.DoubleToInt64Bits(hours), "Double lost bits.");
            Check(decoded.Sections["unseen"].Envelope.SequenceEqual(unknown), "Unknown section bytes changed.");
        }
    }

    [TestCase]
    public void ThousandTruncationsAndBitFlipsAreRejected()
    {
        var original = SaveContainer.Encode(Document());
        var rng = new Rng(1000);
        for (var i = 0; i < 1000; i++)
        {
            var corrupted = i % 2 == 0 ? original[..rng.NextInt(original.Length)] : original.ToArray();
            if (i % 2 != 0) corrupted[rng.NextInt(corrupted.Length)] ^= (byte)(1 << rng.NextInt(8));
            Reject(() => SaveContainer.Decode(corrupted));
        }
    }

    [TestCase]
    public void LengthGenerationFlagsAndNewerVersionsAreChecked()
    {
        var original = SaveContainer.Encode(Document());
        Reject(() => SaveContainer.Decode(original, 2));
        Reject(() => SaveContainer.Decode(original, expectedSlot: new("s2")));
        var flags = original.ToArray(); flags[10] = 5; Rehash(flags); Reject(() => SaveContainer.Decode(flags));
        var newer = original.ToArray(); newer[8] = 2; Rehash(newer); Reject(() => SaveContainer.Decode(newer), newer: true);
        var length = original.ToArray(); BinaryPrimitives.WriteUInt32LittleEndian(length.AsSpan(12), 17000); Rehash(length);
        Reject(() => SaveContainer.Decode(length));
        var newSection = Document() with { Sections = new Dictionary<string, RawSaveSection> { ["world"] = new(2, "{\"v\":2,\"data\":{\"hours\":17.5}}"u8.ToArray()) } };
        Reject(() => Sections().Prepare(SaveContainer.Decode(SaveContainer.Encode(newSection))), newer: true);
    }

    [TestCase]
    public void DecompressionBombTruncatedBrotliAndBadThumbnailAreContained()
    {
        using var compressed = new MemoryStream();
        using (var writer = new BrotliStream(compressed, CompressionLevel.Fastest, leaveOpen: true))
        {
            var chunk = new byte[8192];
            for (var i = 0; i <= SaveContainer.MaxBody / chunk.Length; i++) writer.Write(chunk);
        }
        Reject(() => SaveContainer.Decode(ReplaceCompressedBody(compressed.ToArray())));
        var source = SaveContainer.Encode(Document());
        var metaLength = (int)BinaryPrimitives.ReadUInt32LittleEndian(source.AsSpan(12));
        var body = source[(64 + metaLength)..];
        Reject(() => SaveContainer.Decode(ReplaceCompressedBody(body[..^1])));
        Reject(() => SaveContainer.Decode(ReplaceCompressedBody([.. body, 0])));
        var thumbnail = "RIFF\u0004\0\0\0WEBP"u8.ToArray();
        var thumbSave = SaveContainer.Encode(Document() with { Thumbnail = thumbnail });
        thumbSave[^1] = 0; Rehash(thumbSave);
        Check(SaveContainer.Decode(thumbSave).Thumbnail is null, "Invalid thumbnail was exposed to the image decoder.");
    }

    [TestCase]
    public void DuplicateUnknownMissingAndDeepJsonAreRejectedBeforeRestore()
    {
        foreach (var data in new[] { "{\"hours\":1,\"hours\":2}", "{\"hours\":1,\"surprise\":2}", "{}", "{\"hours\":\"wrong\"}" })
        {
            var document = Document() with { Sections = new Dictionary<string, RawSaveSection> { ["world"] = new(1, Encoding.UTF8.GetBytes("{\"v\":1,\"data\":" + data + "}")) } };
            Reject(() => Sections().Prepare(SaveContainer.Decode(SaveContainer.Encode(document))));
        }
        var deep = Encoding.UTF8.GetBytes(new string('[', 65) + "0" + new string(']', 65));
        try { using var parsed = SaveContainer.ParseJson(deep); }
        catch (JsonException) { return; }
        throw new InvalidOperationException("Depth 65 accepted.");
    }

    [TestCase]
    public void MigrationRunsEveryStepAndNoncoreErrorsBecomeDefaults()
    {
        var section = new TestSection();
        var registry = Sections(section, 3);
        var prepared = registry.Prepare(Document());
        Check(section.Migrations.SequenceEqual(new[] { 1, 2 }), "Migration skipped a version.");
        Check(((TestState)prepared.Sections[0].Dto!).Hours == 19.5, "Migration output differs.");
        var noncore = Sections(phase: RestorePhase.Content).Prepare(Document() with
        { Sections = new Dictionary<string, RawSaveSection> { ["world"] = new(1, "{\"v\":1,\"data\":{\"hours\":\"bad\"}}"u8.ToArray()) } });
        Check(noncore.Sections[0].Dto is null && noncore.Sections[0].Repair is not null, "Invalid noncore section should be repaired independently.");
        var mapped = SaveMigrations.RemapIds(JsonNode.Parse("{\"items\":[\"old\"],\"id\":\"old\"}")!, new Dictionary<string, string> { ["old"] = "new" });
        Check(mapped["id"]!.GetValue<string>() == "new" && mapped["items"]![0]!.GetValue<string>() == "new", "ID mapping failed.");
    }

    [TestCase]
    public void IncompleteWritePreservesOldGenerationAndReadbackMustPass()
    {
        using var repository = new SaveRepository(TestDirectory(), true, Sections());
        repository.Write(Document());
        try { repository.Write(Document(2), (stream, bytes) => { stream.Write(bytes.AsSpan(0, 40)); throw new IOException("disk full"); }); }
        catch (IOException) { }
        Check(repository.Scan(new("s1")).Info.Meta?.Gen == 1, "Failed write destroyed the previous save.");
        try { repository.Write(Document(2), (stream, bytes) => stream.Write(bytes.AsSpan(0, 40))); }
        catch (SaveFormatException) { }
        Check(repository.Scan(new("s1")).Info.Meta?.Gen == 1, "A truncated file was committed.");
    }

    [TestCase]
    public void RecoveryLockLineBoundariesAndRetentionAreEnforced()
    {
        var root = TestDirectory();
        using var repository = new SaveRepository(root, true, Sections());
        using var second = new SaveRepository(root, true, Sections());
        Check(!second.Enabled && second.ReadOnly == ReadOnlyReason.OtherWindow, "Second writer acquired lock.");
        for (ulong gen = 1; gen <= 4; gen++) repository.Write(Document(gen));
        Check(Directory.GetFiles(Path.Combine(root, "s1")).Length == 3, "Retention is not three generations.");
        var broken = Path.Combine(root, "s1", "g000004.sav"); File.WriteAllBytes(broken, [1, 2, 3]);
        Check(second.Scan(new("s1")).Info.Health == SlotHealth.RecoveredOlderGen && File.Exists(broken), "Readonly scanner moved a file.");
        var recovered = repository.Scan(new("s1"));
        Check(recovered.Info.Health == SlotHealth.RecoveredOlderGen && recovered.Info.Meta?.Gen == 3, "Did not recover one generation.");
        Check(!File.Exists(broken) && recovered.MaxGeneration == 4, "Recovery lost the generation high-water mark.");
        repository.Write(Document(5, 5));
        Check(Directory.GetFiles(Path.Combine(root, "s1")).Length == 1, "Old world survived successful new-world commit.");
        File.WriteAllBytes(Path.Combine(root, "s1", "g000005.sav"), [0]);
        Check(repository.Scan(new("s1")).Info.Health == SlotHealth.Corrupt, "Crossed a world-line boundary.");
    }

    [TestCase]
    public void SaveOffDoesNotCreateDirectoryAndAllCorruptFilesStayPut()
    {
        var offRoot = TestDirectory();
        using (var off = new SaveRepository(offRoot, false, Sections()))
        { Check(off.Scan(new("s1")).Info.Health == SlotHealth.Empty, "Empty readonly scan failed."); }
        Check(!Directory.Exists(offRoot), "Save-off created its root.");
        var root = TestDirectory();
        using var repository = new SaveRepository(root, true, Sections());
        Directory.CreateDirectory(Path.Combine(root, "s1"));
        var file = Path.Combine(root, "s1", "g000001.sav"); File.WriteAllBytes(file, [0]);
        Check(repository.Scan(new("s1")).Info.Health == SlotHealth.Corrupt && File.Exists(file), "All-corrupt slot was moved or deleted.");
    }

    [TestCase]
    public void NewerBuildCannotBeAutomaticallyReplacedAndCoreFailureCannotCrossLines()
    {
        var root = TestDirectory();
        using var repository = new SaveRepository(root, true, Sections());
        repository.Write(Document());
        var newer = SaveContainer.Encode(Document(2)); newer[8] = 2; Rehash(newer);
        var path = Path.Combine(root, "s1", "g000002.sav"); File.WriteAllBytes(path, newer);
        Check(repository.Scan(new("s1")).Info.Health == SlotHealth.NewerBuild, "Newer build fell back to old data.");
        var rejected = false;
        try { repository.Write(Document(3)); } catch (InvalidOperationException) { rejected = true; }
        Check(rejected && File.ReadAllBytes(path).SequenceEqual(newer), "Automatic write replaced a newer build.");
        var invalidNewLine = Document(2, 2) with
        { Sections = new Dictionary<string, RawSaveSection> { ["world"] = new(1, "{\"v\":1,\"data\":{\"hours\":\"bad\"}}"u8.ToArray()) } };
        File.WriteAllBytes(path, SaveContainer.Encode(invalidNewLine));
        Check(repository.Scan(new("s1")).Info.Health == SlotHealth.Corrupt, "Recovered a different world line.");
        Check(File.Exists(path), "All-corrupt new line was automatically moved.");
    }

    [TestCase]
    public void FrozenVersionOneGoldenCanBeDecodedAndRewritten()
    {
        var root = Environment.GetEnvironmentVariable("UESTC_REPOSITORY") ?? FindRoot();
        var bytes = File.ReadAllBytes(Path.Combine(root, "game", "kernel", "save", "tests", "golden", "container-v1.sav"));
        var document = SaveContainer.Decode(bytes);
        var second = SaveContainer.Decode(SaveContainer.Encode(document));
        Check(second.Meta == document.Meta, "Golden metadata changed on rewrite.");
        foreach (var (key, raw) in document.Sections) Check(second.Sections[key].Envelope.SequenceEqual(raw.Envelope), "Golden envelope changed.");
    }
}

public sealed record TestState(double Hours);
public sealed class TestSection : ISaveSection<TestState>
{
    public TestState Current { get; set; } = new(17.5);
    public List<int> Migrations { get; } = [];
    public System.Text.Json.Serialization.Metadata.JsonTypeInfo<TestState> TypeInfo => TestSaveJsonContext.Strict.TestState;
    public TestState Capture(in CaptureContext context) => Current;
    public void Restore(TestState state, RestoreContext context) => Current = state;
    public TestState CreateDefault(NewGameContext context) => new(context.Start.Clock);
    public JsonNode Migrate(int version, JsonNode data)
    {
        Migrations.Add(version);
        var copy = data.DeepClone(); copy["hours"] = copy["hours"]!.GetValue<double>() + 1; return copy;
    }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(TestState))]
internal partial class TestSaveJsonContext : JsonSerializerContext
{
    internal static TestSaveJsonContext Strict { get; } = new(new JsonSerializerOptions(ContractJson.Options)
    { UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow, RespectRequiredConstructorParameters = true });
}
