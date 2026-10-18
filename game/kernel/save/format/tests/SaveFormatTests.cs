using Campus.Tests;
using System.Buffers.Binary;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Campus.Contracts;
using Campus.Contracts.Save;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Campus.Kernel.Save.Format.Tests;

[TestSuite]
public sealed class SaveFormatTests
{
    private static long _testId;
    private static SaveMeta Meta(ulong gen = 1, ulong line = 1) => new(1, "test", new("s1"), SlotKind.Auto,
        gen, line, 20, new("test-map"), 17.5, 17.5, Season.Autumn, WeatherKind.Clear, ViewMode.Fly,
        new(new Vector3(1, 2, 3), 4, 5), null, null, null, 1792082400, SaveReason.Test);
    private static SaveDocument Document(ulong gen = 1, ulong line = 1) => new(Meta(gen, line),
        new SortedDictionary<string, RawSaveSection>(StringComparer.Ordinal)
        { ["world"] = new(1, "{\"v\":1,\"data\":{\"hours\":17.5}}"u8.ToArray()) }, null);
    private static SaveSections Sections(TestSection? section = null, int version = 1, RestorePhase phase = RestorePhase.World) =>
        new([new(new("world") { Owner = "WP-50", Version = version, Phase = phase, Order = 10 }, section ?? new())]);
    private static string TestDirectory()
    {
        var root = RepositoryFixture.Root;
        return Path.GetFullPath(Path.Combine(root, "output", "save-tests", "case-" + Environment.ProcessId + "-" + Interlocked.Increment(ref _testId)));
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
    public void RoundTripsPreserveNumericBitsAndUnknownEnvelopes()
    {
        var rng = new Rng(50);
        var unknown = Encoding.UTF8.GetBytes("{ \"v\" : 47, \"data\" : {\"unseen\": [1, 2, 3], \"text\":\"未来\"} }");
        for (var i = 0; i < 20; i++)
        {
            var hours = rng.NextFloat() * 1000000d + rng.NextFloat() / 1000000d;
            var section = new TestSection { Current = new(hours) };
            var registry = Sections(section);
            var raw = registry.Serialize([new("world", registry.Registered[0], section.Current)],
                new Dictionary<string, RawSaveSection> { ["unseen"] = new(47, unknown) });
            var decoded = SaveContainer.Decode(SaveContainer.Encode(new(Meta(), raw, null)), 1, new("s1"));
            var prepared = registry.Prepare(decoded);
            AssertThat(BitConverter.DoubleToInt64Bits(((TestState)prepared.Sections[0].Dto!).Hours) == BitConverter.DoubleToInt64Bits(hours)).OverrideFailureMessage("Double lost bits.").IsTrue();
            AssertThat(decoded.Sections["unseen"].Envelope.SequenceEqual(unknown)).OverrideFailureMessage("Unknown section bytes changed.").IsTrue();
        }
    }

    [TestCase]
    public void TruncationAndBitFlipAreRejected()
    {
        var original = SaveContainer.Encode(Document());
        {
            var thrown = false;
            try { SaveContainer.Decode(original[..(original.Length / 2)]); }
            catch (SaveFormatException) { thrown = true; }
            AssertThat(thrown).OverrideFailureMessage("Invalid save was accepted.").IsTrue();
        }
        var flipped = original.ToArray(); flipped[^1] ^= 1;
        {
            var thrown = false;
            try { SaveContainer.Decode(flipped); }
            catch (SaveFormatException) { thrown = true; }
            AssertThat(thrown).OverrideFailureMessage("Invalid save was accepted.").IsTrue();
        }
    }

    [TestCase]
    public void LengthGenerationFlagsAndNewerVersionsAreRejected()
    {
        var original = SaveContainer.Encode(Document());
        {
            var thrown = false;
            try { SaveContainer.Decode(original, 2); }
            catch (SaveFormatException) { thrown = true; }
            AssertThat(thrown).OverrideFailureMessage("Invalid save was accepted.").IsTrue();
        }
        {
            var thrown = false;
            try { SaveContainer.Decode(original, expectedSlot: new("s2")); }
            catch (SaveFormatException) { thrown = true; }
            AssertThat(thrown).OverrideFailureMessage("Invalid save was accepted.").IsTrue();
        }
        var flags = original.ToArray(); flags[10] = 5; Rehash(flags);
        {
            var thrown = false;
            try { SaveContainer.Decode(flags); }
            catch (SaveFormatException) { thrown = true; }
            AssertThat(thrown).OverrideFailureMessage("Invalid save was accepted.").IsTrue();
        }
        var newer = original.ToArray(); newer[8] = 2; Rehash(newer);
        {
            var thrown = false;
            try { SaveContainer.Decode(newer); }
            catch (SaveFormatException) { thrown = true; }
            AssertThat(thrown).OverrideFailureMessage("Invalid save was accepted.").IsTrue();
        }
        var length = original.ToArray(); BinaryPrimitives.WriteUInt32LittleEndian(length.AsSpan(12), 17000); Rehash(length);
        {
            var thrown = false;
            try { SaveContainer.Decode(length); }
            catch (SaveFormatException) { thrown = true; }
            AssertThat(thrown).OverrideFailureMessage("Invalid save was accepted.").IsTrue();
        }
        var newSection = Document() with { Sections = new Dictionary<string, RawSaveSection> { ["world"] = new(2, "{\"v\":2,\"data\":{\"hours\":17.5}}"u8.ToArray()) } };
        {
            var thrown = false;
            try { Sections().Prepare(SaveContainer.Decode(SaveContainer.Encode(newSection))); }
            catch (SaveFormatException) { thrown = true; }
            AssertThat(thrown).OverrideFailureMessage("Invalid save was accepted.").IsTrue();
        }
    }

    [TestCase]
    public void InvalidBrotliAndBadThumbnailAreContained()
    {
        {
            var thrown = false;
            try { SaveContainer.Decode(ReplaceCompressedBody("not brotli"u8.ToArray())); }
            catch (SaveFormatException) { thrown = true; }
            AssertThat(thrown).OverrideFailureMessage("Invalid save was accepted.").IsTrue();
        }
        var thumbnail = "RIFF\u0004\0\0\0WEBP"u8.ToArray();
        var thumbSave = SaveContainer.Encode(Document() with { Thumbnail = thumbnail });
        thumbSave[^1] = 0; Rehash(thumbSave);
        AssertThat(SaveContainer.Decode(thumbSave).Thumbnail is null).OverrideFailureMessage("Invalid thumbnail was exposed to the image decoder.").IsTrue();
    }

    [TestCase]
    public void DuplicateUnknownMissingAndDeepJsonAreRejectedBeforeRestore()
    {
        foreach (var data in new[] { "{\"hours\":1,\"hours\":2}", "{\"hours\":1,\"surprise\":2}", "{}", "{\"hours\":\"wrong\"}" })
        {
            var document = Document() with { Sections = new Dictionary<string, RawSaveSection> { ["world"] = new(1, Encoding.UTF8.GetBytes("{\"v\":1,\"data\":" + data + "}")) } };
            {
                var thrown = false;
                try { Sections().Prepare(SaveContainer.Decode(SaveContainer.Encode(document))); }
                catch (SaveFormatException) { thrown = true; }
                AssertThat(thrown).OverrideFailureMessage("Invalid save was accepted.").IsTrue();
            }
        }
        var deep = Encoding.UTF8.GetBytes(new string('[', 65) + "0" + new string(']', 65));
        try { using var parsed = SaveContainer.ParseJson(deep); }
        catch (JsonException) { return; }
        throw new InvalidOperationException("Depth 65 accepted.");
    }

    [TestCase]
    public void NoncoreErrorsAndVersionMismatchBecomeDefaults()
    {
        var older = Sections(null, 3, RestorePhase.Content).Prepare(Document());
        AssertThat(older.Sections[0].Dto is null && older.Sections[0].Repair is not null).OverrideFailureMessage("A section with another version should be replaced by its default.").IsTrue();
        {
            var thrown = false;
            try { Sections(null, 3).Prepare(Document()); }
            catch (SaveFormatException) { thrown = true; }
            AssertThat(thrown).OverrideFailureMessage("Invalid save was accepted.").IsTrue();
        }
        var noncore = Sections(phase: RestorePhase.Content).Prepare(Document() with
        { Sections = new Dictionary<string, RawSaveSection> { ["world"] = new(1, "{\"v\":1,\"data\":{\"hours\":\"bad\"}}"u8.ToArray()) } });
        AssertThat(noncore.Sections[0].Dto is null && noncore.Sections[0].Repair is not null).OverrideFailureMessage("Invalid noncore section should be repaired independently.").IsTrue();
    }

    [TestCase]
    public void IncompleteWritePreservesOldGenerationAndReadbackMustPass()
    {
        using var repository = new SaveRepository(TestDirectory(), true, Sections());
        repository.Write(Document());
        try { repository.Write(Document(2), (stream, bytes) => { stream.Write(bytes.AsSpan(0, 40)); throw new IOException("disk full"); }); }
        catch (IOException) { }
        AssertThat(repository.Scan(new("s1")).Info.Meta?.Gen == 1).OverrideFailureMessage("Failed write destroyed the previous save.").IsTrue();
        try { repository.Write(Document(2), (stream, bytes) => stream.Write(bytes.AsSpan(0, 40))); }
        catch (SaveFormatException) { }
        AssertThat(repository.Scan(new("s1")).Info.Meta?.Gen == 1).OverrideFailureMessage("A truncated file was committed.").IsTrue();
    }

    [TestCase]
    public void RecoveryLockLineBoundariesAndRetentionAreEnforced()
    {
        var root = TestDirectory();
        using var repository = new SaveRepository(root, true, Sections());
        using var second = new SaveRepository(root, true, Sections());
        AssertThat(!second.Enabled && second.ReadOnly == ReadOnlyReason.OtherWindow).OverrideFailureMessage("Second writer acquired lock.").IsTrue();
        for (ulong gen = 1; gen <= 4; gen++) repository.Write(Document(gen));
        AssertThat(Directory.GetFiles(Path.Combine(root, "s1")).Length == 3).OverrideFailureMessage("Retention is not three generations.").IsTrue();
        var broken = Path.Combine(root, "s1", "g000004.sav"); File.WriteAllBytes(broken, [1, 2, 3]);
        AssertThat(second.Scan(new("s1")).Info.Meta?.Gen == 3 && File.Exists(broken)).OverrideFailureMessage("Readonly scanner moved a file.").IsTrue();
        var recovered = repository.Scan(new("s1"));
        AssertThat(recovered.Info.Health == SlotHealth.Ok && recovered.Info.Meta?.Gen == 3).OverrideFailureMessage("Did not recover one generation.").IsTrue();
        AssertThat(File.Exists(broken) && recovered.MaxGeneration == 4).OverrideFailureMessage("Recovery moved the unreadable file or lost the generation high-water mark.").IsTrue();
        repository.Write(Document(5, 5));
        AssertThat(Directory.GetFiles(Path.Combine(root, "s1")).Length == 1).OverrideFailureMessage("Old world survived successful new-world commit.").IsTrue();
        File.WriteAllBytes(Path.Combine(root, "s1", "g000005.sav"), [0]);
        AssertThat(repository.Scan(new("s1")).Info.Health == SlotHealth.Corrupt).OverrideFailureMessage("Crossed a world-line boundary.").IsTrue();
    }

    [TestCase]
    public void SaveOffDoesNotCreateDirectoryAndAllCorruptFilesStayPut()
    {
        var offRoot = TestDirectory();
        using (var off = new SaveRepository(offRoot, false, Sections()))
        { AssertThat(off.Scan(new("s1")).Info.Health == SlotHealth.Empty).OverrideFailureMessage("Empty readonly scan failed.").IsTrue(); }
        AssertThat(!Directory.Exists(offRoot)).OverrideFailureMessage("Save-off created its root.").IsTrue();
        var root = TestDirectory();
        using var repository = new SaveRepository(root, true, Sections());
        Directory.CreateDirectory(Path.Combine(root, "s1"));
        var file = Path.Combine(root, "s1", "g000001.sav"); File.WriteAllBytes(file, [0]);
        AssertThat(repository.Scan(new("s1")).Info.Health == SlotHealth.Corrupt && File.Exists(file)).OverrideFailureMessage("All-corrupt slot was moved or deleted.").IsTrue();
    }

    [TestCase]
    public void NewerGenerationFallsBackAndUnreadableNewLineCannotBeWrittenOver()
    {
        var root = TestDirectory();
        using var repository = new SaveRepository(root, true, Sections());
        repository.Write(Document());
        var newer = SaveContainer.Encode(Document(2)); newer[8] = 2; Rehash(newer);
        var path = Path.Combine(root, "s1", "g000002.sav"); File.WriteAllBytes(path, newer);
        AssertThat(repository.Scan(new("s1")).Info.Meta?.Gen == 1).OverrideFailureMessage("A newer-format generation did not fall back to the older one.").IsTrue();
        var invalidNewLine = Document(2, 2) with
        { Sections = new Dictionary<string, RawSaveSection> { ["world"] = new(1, "{\"v\":1,\"data\":{\"hours\":\"bad\"}}"u8.ToArray()) } };
        File.WriteAllBytes(path, SaveContainer.Encode(invalidNewLine));
        AssertThat(repository.Scan(new("s1")).Info.Health == SlotHealth.Corrupt).OverrideFailureMessage("Recovered a different world line.").IsTrue();
        var rejected = false;
        try { repository.Write(Document(3, 2)); } catch (InvalidOperationException) { rejected = true; }
        AssertThat(rejected && File.Exists(path)).OverrideFailureMessage("Automatic write replaced an unreadable slot.").IsTrue();
    }
}

public sealed record TestState(double Hours);
public sealed class TestSection : ISaveSection<TestState>
{
    public TestState Current { get; set; } = new(17.5);
    public System.Text.Json.Serialization.Metadata.JsonTypeInfo<TestState> TypeInfo => TestSaveJsonContext.Strict.TestState;
    public TestState Capture(in CaptureContext context) => Current;
    public void Restore(TestState state, RestoreContext context) => Current = state;
    public TestState CreateDefault(NewGameContext context) => new(context.Start.Clock);
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(TestState))]
internal partial class TestSaveJsonContext : JsonSerializerContext
{
    internal static TestSaveJsonContext Strict { get; } = new(new JsonSerializerOptions(ContractJson.Options)
    { UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow, RespectRequiredConstructorParameters = true });
}
