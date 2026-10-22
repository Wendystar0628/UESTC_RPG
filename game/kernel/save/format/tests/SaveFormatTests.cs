using Campus.Tests;
using System.Buffers.Binary;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
    private static SaveMeta Meta(ulong gen = 1, ulong line = 1) => new(SaveContainer.CurrentFormat, new("s1"), SlotKind.Auto,
        gen, line, 20, new("test-map"), 17.5, 17.5, Season.Autumn, WeatherKind.Clear, ViewMode.Fly,
        new(new Vector3(1, 2, 3), 4, 5), null, null, null, 1792082400, SaveReason.Test);
    private static SaveDocument Document(ulong gen = 1, ulong line = 1) => new(Meta(gen, line),
        new SortedDictionary<string, RawSaveSection>(StringComparer.Ordinal)
        { ["world"] = new(1, "{\"v\":1,\"data\":{\"hours\":17.5}}"u8.ToArray()) }, null);
    private static SaveSections Sections(TestSection? section = null, int version = 1, RestorePhase phase = RestorePhase.World) =>
        new([new(new("world") { Version = version, Phase = phase, Order = 10 }, section ?? new())]);
    private sealed class TempSaveDirectory : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
            "uestc-rpg-save-case-" + Environment.ProcessId + "-" + Interlocked.Increment(ref _testId));

        public void Dispose()
        {
            if (Directory.Exists(Path)) Directory.Delete(Path, recursive: true);
        }
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
    [TestCategory(TestReason.Regression)]
    public void FormatTwoMetadataShapeRequiresANewFormatForSchemaChanges()
    {
        AssertThat(SaveContainer.CurrentFormat).IsEqual(2);
        var encoded = SaveContainer.Encode(Document());
        var metaLength = (int)BinaryPrimitives.ReadUInt32LittleEndian(encoded.AsSpan(12));
        using var json = JsonDocument.Parse(encoded.AsMemory(SaveContainer.HeaderSize, metaLength));
        using var formatTwo = JsonDocument.Parse("""
            {
                "format": 2,
                "slot": "s1",
                "kind": "auto",
                "gen": 1,
                "line": 1,
                "playSeconds": 20,
                "map": "test-map",
                "worldHours": 17.5,
                "clock": 17.5,
                "season": "autumn",
                "weather": "clear",
                "mode": "fly",
                "pose": { "position": [1, 2, 3], "yawDeg": 4, "pitchDeg": 5, "fovDeg": null },
                "space": null,
                "region": null,
                "placeKey": null,
                "savedAtUnix": 1792082400,
                "reason": "test",
                "ruleset": "sightseeing",
                "hasThumb": false
            }
            """);
        AssertThat(JsonElement.DeepEquals(json.RootElement, formatTwo.RootElement))
            .OverrideFailureMessage("Metadata changed without updating the complete format 2 schema snapshot.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestTag.Determinism)]
    [TestCategory(TestReason.Contract)]
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
    [TestCategory(TestReason.Contract)]
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
    [TestCategory(TestReason.Contract)]
    public void LengthGenerationFlagsAndOtherVersionsAreRejected()
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
        var older = original.ToArray(); BinaryPrimitives.WriteUInt16LittleEndian(older.AsSpan(8), 1); Rehash(older);
        var newer = original.ToArray(); BinaryPrimitives.WriteUInt16LittleEndian(newer.AsSpan(8), (ushort)(SaveContainer.CurrentFormat + 1)); Rehash(newer);
        var length = original.ToArray(); BinaryPrimitives.WriteUInt32LittleEndian(length.AsSpan(12), 17000); Rehash(length);
        foreach (var invalid in new[] { flags, older, newer, length })
        {
            var thrown = false;
            try { SaveContainer.Decode(invalid); }
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
    [TestCategory(TestReason.Contract)]
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
    [TestCategory(TestReason.Contract)]
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
    [TestCategory(TestReason.Contract)]
    public void NoncoreErrorsAndVersionMismatchBecomeDefaults()
    {
        var older = Sections(null, 3, RestorePhase.Content).Prepare(Document());
        AssertThat(older.Sections[0].Dto is null && older.Sections[0].Repair is not null && older.Sections[0].Error is SaveFormatException).OverrideFailureMessage("A section with another version should be replaced by its default.").IsTrue();
        var newer = Document() with { Sections = new Dictionary<string, RawSaveSection>
        { ["world"] = new(2, "{\"v\":2,\"data\":{\"hours\":17.5}}"u8.ToArray()) } };
        AssertThat(Sections(phase: RestorePhase.Content).Prepare(newer).Sections[0].Error is SaveFormatException)
            .OverrideFailureMessage("A noncore version mismatch must use the same default-repair path in both directions.").IsTrue();
        var noncore = Sections(phase: RestorePhase.Content).Prepare(Document() with
        { Sections = new Dictionary<string, RawSaveSection> { ["world"] = new(1, "{\"v\":1,\"data\":{\"hours\":\"bad\"}}"u8.ToArray()) } });
        AssertThat(noncore.Sections[0].Dto is null && noncore.Sections[0].Repair is not null && noncore.Sections[0].Error is JsonException).OverrideFailureMessage("Invalid noncore section should be repaired independently.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void IncompleteWritePreservesOldGenerationAndReadbackMustPass()
    {
        using var temp = new TempSaveDirectory();
        using var repository = new SaveRepository(temp.Path, true, Sections());
        repository.Write(Document());
        var originalFailure = new IOException("disk full");
        Exception? received = null;
        string? incomplete = null;
        try
        {
            repository.Write(Document(2), (stream, bytes) =>
            {
                incomplete = stream.Name;
                stream.Write(bytes.AsSpan(0, 40));
                File.SetAttributes(stream.Name, FileAttributes.ReadOnly);
                throw originalFailure;
            });
        }
        catch (AggregateException error) { received = error; }
        finally { if (incomplete is not null && File.Exists(incomplete)) File.SetAttributes(incomplete, FileAttributes.Normal); }
        AssertThat(received is AggregateException aggregate && aggregate.InnerExceptions.Count > 1 && aggregate.InnerExceptions.Any(error => ReferenceEquals(error, originalFailure)))
            .OverrideFailureMessage("The aggregate must contain the original write failure and the actual incomplete-file cleanup failure.").IsTrue();
        if (incomplete is not null) File.Delete(incomplete);
        AssertThat(repository.Scan(new("s1")).Info.Meta?.Gen == 1).OverrideFailureMessage("Failed write destroyed the previous save.").IsTrue();
        try { repository.Write(Document(2), (stream, bytes) => stream.Write(bytes.AsSpan(0, 40))); }
        catch (SaveFormatException) { }
        AssertThat(repository.Scan(new("s1")).Info.Meta?.Gen == 1).OverrideFailureMessage("A truncated file was committed.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void RecoveryLockLineBoundariesAndRetentionAreEnforced()
    {
        using var temp = new TempSaveDirectory();
        var root = temp.Path;
        using var repository = new SaveRepository(root, true, Sections());
        using var second = new SaveRepository(root, true, Sections());
        AssertThat(!second.Enabled && second.ReadOnly == ReadOnlyReason.OtherWindow).OverrideFailureMessage("Second writer acquired lock.").IsTrue();
        for (ulong gen = 1; gen <= 3; gen++) repository.Write(Document(gen));
        using (var held = new FileStream(Path.Combine(root, "s1", "g000001.sav"), FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            var committed = repository.Write(Document(4));
            AssertThat(committed.Info.Meta?.Gen == 4 && repository.PendingMaintenanceErrors.Count != 0)
                .OverrideFailureMessage("A deletion conflict must leave the new generation committed and maintenance pending.").IsTrue();
        }
        repository.Write(Document(5));
        AssertThat(repository.PendingMaintenanceErrors.Count == 0).OverrideFailureMessage("The next commit did not retry pending maintenance.").IsTrue();
        AssertThat(Directory.GetFiles(Path.Combine(root, "s1")).Length == 3).OverrideFailureMessage("Retention is not three generations.").IsTrue();
        var broken = Path.Combine(root, "s1", "g000005.sav");
        var invalidCore = Document(5) with
        { Sections = new Dictionary<string, RawSaveSection> { ["world"] = new(1, "{\"v\":1,\"data\":{\"hours\":\"bad\"}}"u8.ToArray()) } };
        File.WriteAllBytes(broken, SaveContainer.Encode(invalidCore));
        AssertThat(second.Scan(new("s1")).Info.Meta?.Gen == 4 && File.Exists(broken)).OverrideFailureMessage("Readonly scanner moved a file.").IsTrue();
        var recovered = repository.Scan(new("s1"));
        AssertThat(recovered.Info.Health == SlotHealth.Ok && recovered.Info.Meta?.Gen == 4).OverrideFailureMessage("Did not recover one generation.").IsTrue();
        AssertThat(File.Exists(broken) && recovered.MaxGeneration == 5).OverrideFailureMessage("Recovery moved the unreadable file or lost the generation high-water mark.").IsTrue();
        repository.Write(Document(6, 6));
        AssertThat(Directory.GetFiles(Path.Combine(root, "s1")).Length == 1).OverrideFailureMessage("Old world survived successful new-world commit.").IsTrue();
        File.WriteAllBytes(Path.Combine(root, "s1", "g000006.sav"), [0]);
        AssertThat(repository.Scan(new("s1")).Info.Health == SlotHealth.Corrupt).OverrideFailureMessage("Crossed a world-line boundary.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void HashFailureCannotIdentifyAWorldLineOrSelectAnOlderGeneration()
    {
        using var temp = new TempSaveDirectory();
        using var repository = new SaveRepository(temp.Path, true, Sections());
        repository.Write(Document());
        var damagedBody = SaveContainer.Encode(Document(2));
        damagedBody[^1] ^= 1;
        var damagedMeta = SaveContainer.Encode(Document(2, 2));
        var lineOffset = damagedMeta.AsSpan(SaveContainer.HeaderSize).IndexOf("\"line\":2"u8);
        AssertThat(lineOffset >= 0).OverrideFailureMessage("Encoded test metadata has no world-line field.").IsTrue();
        damagedMeta[SaveContainer.HeaderSize + lineOffset + "\"line\":"u8.Length] = (byte)'1';
        var latest = Path.Combine(temp.Path, "s1", "g000002.sav");
        var previous = Path.Combine(temp.Path, "s1", "g000001.sav");
        foreach (var damaged in new[] { damagedBody, damagedMeta })
        {
            var rejected = false;
            try { SaveContainer.Decode(damaged); }
            catch (SaveFormatException error)
            {
                rejected = true;
                AssertThat(error.Metadata is null).OverrideFailureMessage("Unverified bytes supplied a world-line identity.").IsTrue();
            }
            AssertThat(rejected).OverrideFailureMessage("A hash-damaged container was accepted.").IsTrue();
            File.WriteAllBytes(latest, damaged);
            var snapshot = repository.Scan(new("s1"));
            AssertThat(snapshot.Info.Health == SlotHealth.Corrupt && snapshot.Info.Meta is null && snapshot.Save is null && snapshot.MaxGeneration == 2)
                .OverrideFailureMessage("An unidentified generation selected an older save or exposed unverified metadata.").IsTrue();
            AssertThat(File.Exists(previous) && File.ReadAllBytes(latest).SequenceEqual(damaged))
                .OverrideFailureMessage("Scanning an unidentified generation changed the stored files.").IsTrue();
        }
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void SaveOffDoesNotCreateDirectoryAndAllCorruptFilesStayPut()
    {
        using var offTemp = new TempSaveDirectory();
        var offRoot = offTemp.Path;
        using (var off = new SaveRepository(offRoot, false, Sections()))
        { AssertThat(off.Scan(new("s1")).Info.Health == SlotHealth.Empty).OverrideFailureMessage("Empty readonly scan failed.").IsTrue(); }
        AssertThat(!Directory.Exists(offRoot)).OverrideFailureMessage("Save-off created its root.").IsTrue();
        using var temp = new TempSaveDirectory();
        var root = temp.Path;
        using var repository = new SaveRepository(root, true, Sections());
        Directory.CreateDirectory(Path.Combine(root, "s1"));
        var file = Path.Combine(root, "s1", "g000001.sav"); File.WriteAllBytes(file, [0]);
        AssertThat(repository.Scan(new("s1")).Info.Health == SlotHealth.Corrupt && File.Exists(file)).OverrideFailureMessage("All-corrupt slot was moved or deleted.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Regression)]
    public void UnreadableSlotMetadataStaysCorruptDuringSaveOffScan()
    {
        using var temp = new TempSaveDirectory();
        using (var writer = new SaveRepository(temp.Path, true, Sections())) writer.Write(Document());
        var invalid = SaveContainer.Encode(Document(2));
        var metaLength = (int)BinaryPrimitives.ReadUInt32LittleEndian(invalid.AsSpan(12));
        using var meta = JsonDocument.Parse(invalid.AsMemory(SaveContainer.HeaderSize, metaLength));
        var slot = Encoding.UTF8.GetBytes(meta.RootElement.GetProperty("slot").GetRawText());
        var key = "\"slot\":"u8;
        var metadata = invalid.AsSpan(SaveContainer.HeaderSize, metaLength);
        var offset = metadata.IndexOf(key) + key.Length;
        metadata.Slice(offset, slot.Length).Fill((byte)' ');
        metadata[offset] = (byte)'0';
        Rehash(invalid);
        var latest = Path.Combine(temp.Path, "s1", "g000002.sav");
        var previous = Path.Combine(temp.Path, "s1", "g000001.sav");
        var previousBytes = File.ReadAllBytes(previous);
        File.WriteAllBytes(latest, invalid);
        using var reader = new SaveRepository(temp.Path, false, Sections());
        var snapshot = reader.Scan(new("s1"));
        AssertThat(!reader.Enabled && snapshot.Info.Health == SlotHealth.Corrupt && snapshot.Info.Meta is null &&
            snapshot.Save is null && snapshot.MaxGeneration == 2).IsTrue();
        AssertThat(File.ReadAllBytes(latest).SequenceEqual(invalid) && File.ReadAllBytes(previous).SequenceEqual(previousBytes))
            .OverrideFailureMessage("Unreadable slot metadata changed files or crossed an unknown world line.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void NewerGenerationUsesPreviousGenerationAndUnreadableNewLineCannotBeWrittenOver()
    {
        using var temp = new TempSaveDirectory();
        var root = temp.Path;
        using var repository = new SaveRepository(root, true, Sections());
        repository.Write(Document());
        var newer = SaveContainer.Encode(Document(2)); BinaryPrimitives.WriteUInt16LittleEndian(newer.AsSpan(8), (ushort)(SaveContainer.CurrentFormat + 1)); Rehash(newer);
        var path = Path.Combine(root, "s1", "g000002.sav"); File.WriteAllBytes(path, newer);
        AssertThat(repository.Scan(new("s1")).Info.Meta?.Gen == 1).OverrideFailureMessage("A newer-format generation did not select the older one.").IsTrue();
        File.WriteAllBytes(path, [0]);
        AssertThat(repository.Scan(new("s1")).Info.Health == SlotHealth.Corrupt).OverrideFailureMessage("An unknown generation identity must not infer an older world line.").IsTrue();
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
