using Campus.Tests;
using System.Buffers.Binary;
using System.IO.Compression;
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
    internal static SaveDocument Document(ulong gen = 1, ulong line = 1) => new(Meta(gen, line),
        new SortedDictionary<string, RawSaveSection>(StringComparer.Ordinal)
        { ["world"] = new(1, "{\"v\":1,\"data\":{\"hours\":17.5}}"u8.ToArray()) }, null);
    internal static SaveSections Sections(TestSection? section = null, int version = 1, RestorePhase phase = RestorePhase.World) =>
        new([new(new("world") { Version = version, Phase = phase, Order = 10 }, section ?? new())]);
    internal sealed class TempSaveDirectory : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
            "uestc-rpg-save-case-" + Environment.ProcessId + "-" + Interlocked.Increment(ref _testId));
        public string Entry(ulong gen) => System.IO.Path.Combine(Path, "s1",
            "g" + gen.ToString("D6", System.Globalization.CultureInfo.InvariantCulture) + ".sav");

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
    private static byte[] ReplaceCompressedBody(byte[] compressed, SaveDocument? document = null)
    {
        var source = SaveContainer.Encode(document ?? Document());
        var metaLength = (int)BinaryPrimitives.ReadUInt32LittleEndian(source.AsSpan(12));
        var bytes = new byte[64 + metaLength + compressed.Length];
        source.AsSpan(0, 64 + metaLength).CopyTo(bytes);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(16), (uint)compressed.Length);
        compressed.CopyTo(bytes, 64 + metaLength); Rehash(bytes); return bytes;
    }

    private static byte[] CompressBody(ReadOnlySpan<byte> body)
    {
        using var compressed = new MemoryStream();
        using (var brotli = new BrotliStream(compressed, CompressionLevel.Fastest, leaveOpen: true)) brotli.Write(body);
        return compressed.ToArray();
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
    public void NoncoreErrorsAndOlderVersionsBecomeDefaults()
    {
        var older = Sections(null, 3, RestorePhase.Content).Prepare(Document());
        AssertThat(older.Sections[0].Dto is null && older.Sections[0].Repair is not null && older.Sections[0].Error is SaveFormatException).OverrideFailureMessage("A section with another version should be replaced by its default.").IsTrue();
        var noncore = Sections(phase: RestorePhase.Content).Prepare(Document() with
        { Sections = new Dictionary<string, RawSaveSection> { ["world"] = new(1, "{\"v\":1,\"data\":{\"hours\":\"bad\"}}"u8.ToArray()) } });
        AssertThat(noncore.Sections[0].Dto is null && noncore.Sections[0].Repair is not null && noncore.Sections[0].Error is JsonException).OverrideFailureMessage("Invalid noncore section should be repaired independently.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Regression)]
    public void ExpandedBrotliAndEncodingShareTheContainerLimit()
    {
        var body = new byte[SaveContainer.MaxFile + 1];
        body.AsSpan().Fill((byte)' ');
        var json = Encoding.UTF8.GetBytes("{\"format\":" + SaveContainer.CurrentFormat + ",\"sections\":{}}");
        json.CopyTo(body, 0);
        var compressed = CompressBody(body);
        AssertThat(compressed.Length < SaveContainer.MaxCompressed && body.Length > SaveContainer.MaxFile).IsTrue();
        var rejected = false;
        try { SaveContainer.Decode(ReplaceCompressedBody(compressed)); }
        catch (SaveFormatException) { rejected = true; }
        AssertThat(rejected).OverrideFailureMessage("A small Brotli input expanded beyond the container limit.").IsTrue();
        var boundary = SaveContainer.Decode(ReplaceCompressedBody(CompressBody(body.AsSpan(0, SaveContainer.MaxFile))));
        AssertThat(boundary.Meta.Gen == Document().Meta.Gen && boundary.Sections.Count == 0).IsTrue();
        var envelope = Encoding.UTF8.GetBytes("{\"v\":1,\"data\":{\"text\":\"" + new string('x', SaveContainer.MaxFile) + "\"}}");
        rejected = false;
        try { SaveContainer.Encode(Document() with { Sections = new Dictionary<string, RawSaveSection> { ["unseen"] = new(1, envelope) } }); }
        catch (SaveFormatException) { rejected = true; }
        AssertThat(rejected).OverrideFailureMessage("Encoding accepted JSON that its bounded decoder cannot read.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Regression)]
    public void InterruptedStagingPreservesOldGenerationAndVerificationPrecedesPublication()
    {
        using var temp = new TempSaveDirectory();
        using var repository = new SaveRepository(temp.Path, true, Sections());
        repository.Write(Document());
        var previous = Path.Combine(temp.Path, "s1", "g000001.sav");
        var previousBytes = File.ReadAllBytes(previous);
        var originalFailure = new IOException("disk full");
        Exception? received = null;
        string? incomplete = null;
        try
        {
            repository.Write(Document(2), (stream, bytes) =>
            {
                incomplete = stream.Name;
                stream.Write(bytes.AsSpan(0, SaveContainer.HeaderSize / 2));
                stream.Flush(flushToDisk: true);
                File.SetAttributes(stream.Name, FileAttributes.ReadOnly);
                throw originalFailure;
            });
        }
        catch (AggregateException error) { received = error; }
        finally { if (incomplete is not null && File.Exists(incomplete)) File.SetAttributes(incomplete, FileAttributes.Normal); }
        AssertThat(received is AggregateException aggregate && aggregate.InnerExceptions.Count > 1 && aggregate.InnerExceptions.Any(error => ReferenceEquals(error, originalFailure)))
            .OverrideFailureMessage("The aggregate must contain the original write failure and the actual incomplete-file cleanup failure.").IsTrue();
        AssertThat(incomplete is not null && File.Exists(incomplete) && new FileInfo(incomplete).Length < SaveContainer.HeaderSize)
            .OverrideFailureMessage("The interrupted write must leave an incomplete file behind before recovery is checked.").IsTrue();
        var interrupted = repository.Scan(new("s1"));
        AssertThat(interrupted.Info.Health == SlotHealth.Ok && interrupted.Info.Meta?.Gen == 1 && interrupted.MaxGeneration == 1)
            .OverrideFailureMessage("An interrupted unpublished write blocked the old generation or consumed its successor's number.").IsTrue();
        AssertThat(File.ReadAllBytes(previous).SequenceEqual(previousBytes)).OverrideFailureMessage("Failed write changed the previous save.").IsTrue();
        var rejected = false;
        try { repository.Write(Document(2), (stream, bytes) => stream.Write(bytes.AsSpan(0, SaveContainer.HeaderSize / 2))); }
        catch (SaveFormatException) { rejected = true; }
        AssertThat(rejected && repository.Scan(new("s1")).Info.Meta?.Gen == 1).OverrideFailureMessage("A truncated file was published.").IsTrue();
        var invalidCore = Document(2) with
        { Sections = new Dictionary<string, RawSaveSection> { ["world"] = new(1, "{\"v\":1,\"data\":{\"hours\":\"bad\"}}"u8.ToArray()) } };
        rejected = false;
        try { repository.Write(invalidCore); }
        catch (SaveFormatException) { rejected = true; }
        AssertThat(rejected && repository.Scan(new("s1")).Info.Meta?.Gen == 1).OverrideFailureMessage("A generation failing Prepare was published.").IsTrue();
        var committed = repository.Write(Document(2));
        AssertThat(committed.Info.Meta?.Gen == 2 && repository.Scan(new("s1")).Info.Meta?.Gen == 2 && !File.Exists(incomplete))
            .OverrideFailureMessage("A retry did not publish the same available generation and clean abandoned staging.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void OnlyTheRepositoryLockOwnerCleansAbandonedStagingOnInitialization()
    {
        using var temp = new TempSaveDirectory();
        string abandoned;
        byte[] previousBytes;
        var previous = Path.Combine(temp.Path, "s1", "g000001.sav");
        using (var owner = new SaveRepository(temp.Path, true, Sections()))
        {
            owner.Write(Document());
            previousBytes = File.ReadAllBytes(previous);
            // Keep a real truncated staging file as if the process stopped before publishing it.
            using (var staging = new FileStream(owner.StagingPath(new("s1")), FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                abandoned = staging.Name;
                staging.Write(SaveContainer.Encode(Document(2)).AsSpan(0, SaveContainer.HeaderSize / 2));
                staging.Flush(flushToDisk: true);
            }
            AssertThat(new FileInfo(abandoned).Length < SaveContainer.HeaderSize).IsTrue();
            using var otherWindow = new SaveRepository(temp.Path, true, Sections());
            using var saveOff = new SaveRepository(temp.Path, false, Sections());
            AssertThat(!otherWindow.Enabled && File.Exists(abandoned) && otherWindow.Scan(new("s1")).Info.Meta?.Gen == 1 &&
                saveOff.Scan(new("s1")).Info.Meta?.Gen == 1).OverrideFailureMessage("A read-only repository cleaned staging or exposed it as a generation.").IsTrue();
        }
        using var nextOwner = new SaveRepository(temp.Path, true, Sections());
        AssertThat(nextOwner.Enabled && !File.Exists(abandoned) && nextOwner.Scan(new("s1")).Info.Meta?.Gen == 1 &&
            File.ReadAllBytes(previous).SequenceEqual(previousBytes)).OverrideFailureMessage("The next lock owner did not clean staging while preserving the committed save.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void PublicationNeverOverwritesAnExistingGeneration()
    {
        using var temp = new TempSaveDirectory();
        using var repository = new SaveRepository(temp.Path, true, Sections());
        repository.Write(Document());
        var previous = Path.Combine(temp.Path, "s1", "g000001.sav");
        var previousBytes = File.ReadAllBytes(previous);
        var occupied = Path.Combine(temp.Path, "s1", "g000002.sav");
        var occupiedBytes = SaveContainer.Encode(Document(2, 2));
        var rejected = false;
        try
        {
            repository.Write(Document(2), (stream, bytes) =>
            {
                stream.Write(bytes);
                File.WriteAllBytes(occupied, occupiedBytes);
            });
        }
        catch (IOException) { rejected = true; }
        AssertThat(rejected && File.ReadAllBytes(occupied).SequenceEqual(occupiedBytes) &&
            File.ReadAllBytes(previous).SequenceEqual(previousBytes)).OverrideFailureMessage("A failed publication overwrote or deleted a committed generation.").IsTrue();
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
            AssertThat(committed.Info.Meta?.Gen == 4 && committed.Info.Generations == SaveSlots.AutoLimit + 1 &&
                committed.Info.Entries.Count == committed.Info.Generations && repository.PendingMaintenanceErrors.Count != 0)
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
    public void NewerFormatsAndKnownSectionsAreReadOnlyWithoutRecoveryOrCleanup()
    {
        var newerHeader = SaveContainer.Encode(Document(2));
        BinaryPrimitives.WriteUInt16LittleEndian(newerHeader.AsSpan(8), (ushort)(SaveContainer.CurrentFormat + 1));
        Rehash(newerHeader);
        var source = SaveContainer.Encode(Document(2));
        var originalMetaLength = (int)BinaryPrimitives.ReadUInt32LittleEndian(source.AsSpan(12));
        var metadata = JsonSerializer.SerializeToUtf8Bytes(Meta(2) with { Format = SaveContainer.CurrentFormat + 1 }, SaveFormatJsonContext.Strict.SaveMeta);
        var newerMetadata = new byte[source.Length - originalMetaLength + metadata.Length];
        source.AsSpan(0, SaveContainer.HeaderSize).CopyTo(newerMetadata);
        metadata.CopyTo(newerMetadata, SaveContainer.HeaderSize);
        source.AsSpan(SaveContainer.HeaderSize + originalMetaLength).CopyTo(newerMetadata.AsSpan(SaveContainer.HeaderSize + metadata.Length));
        BinaryPrimitives.WriteUInt32LittleEndian(newerMetadata.AsSpan(12), (uint)metadata.Length);
        Rehash(newerMetadata);
        var newerBody = ReplaceCompressedBody(CompressBody(Encoding.UTF8.GetBytes(
            "{\"format\":" + (SaveContainer.CurrentFormat + 1) + ",\"sections\":{}}")), Document(2));
        var newerSection = SaveContainer.Encode(Document(2) with { Sections = new Dictionary<string, RawSaveSection>
        { ["world"] = new(2, "{\"v\":2,\"data\":{\"hours\":17.5}}"u8.ToArray()) } });
        var perMap = new SaveSections([
            new(new("world") { Version = 1, Phase = RestorePhase.World, Order = 10 }, new TestSection()),
            new(new("visit") { Version = 1, Phase = RestorePhase.Content, Order = 20, Scope = SaveScope.PerMap }, new TestSection())]);
        var newerPerMap = SaveContainer.Encode(Document(2) with { Sections = new Dictionary<string, RawSaveSection>
        {
            ["world"] = new(1, "{\"v\":1,\"data\":{\"hours\":\"bad\"}}"u8.ToArray()),
            ["map:other-map/visit"] = new(2, "{\"v\":2,\"data\":{\"hours\":17.5}}"u8.ToArray())
        } });
        foreach (var (registry, future) in new[]
        {
            (Sections(), newerHeader), (Sections(), newerMetadata), (Sections(), newerBody),
            (Sections(), newerSection), (Sections(phase: RestorePhase.Content), newerSection), (perMap, newerPerMap)
        })
        {
            using var temp = new TempSaveDirectory();
            using (var writer = new SaveRepository(temp.Path, true, registry)) writer.Write(Document());
            var previous = Path.Combine(temp.Path, "s1", "g000001.sav");
            var previousBytes = File.ReadAllBytes(previous);
            var latest = Path.Combine(temp.Path, "s1", "g000002.sav");
            File.WriteAllBytes(latest, future);
            using var reader = new SaveRepository(temp.Path, false, registry);
            var staging = Path.Combine(temp.Path, "s1", ".save-future.tmp");
            File.WriteAllBytes(staging, [0]);
            AssertThat(reader.Scan(new("s1")).Info.Health == SlotHealth.Newer).IsTrue();
            using var repository = new SaveRepository(temp.Path, true, registry);
            var scanned = repository.Scan(new("s1"));
            AssertThat(scanned.Info.Health == SlotHealth.Newer && scanned.Save is null && scanned.MaxGeneration == 2 && File.Exists(staging))
                .OverrideFailureMessage("A future generation recovered an older save or was cleaned during initialization.").IsTrue();
            foreach (var (replace, reason) in new[] { (false, SaveReason.NewGame), (true, SaveReason.Test) })
            {
                var rejected = false;
                try { repository.Write(Document(3, 3) with { Meta = Meta(3, 3) with { Reason = reason } }, replaceUnusable: replace); }
                catch (InvalidOperationException) { rejected = true; }
                AssertThat(rejected).OverrideFailureMessage("An unconfirmed new game or ordinary write replaced a future slot.").IsTrue();
            }
            var externalRejected = false;
            try { repository.ReadExternal(latest); }
            catch (SaveFormatException) { externalRejected = true; }
            AssertThat(externalRejected).OverrideFailureMessage("A future external save was prepared for restore.").IsTrue();
            repository.Write(Document() with { Meta = Meta() with { Slot = new("s2") } });
            AssertThat(File.ReadAllBytes(latest).SequenceEqual(future) && File.ReadAllBytes(previous).SequenceEqual(previousBytes) && File.Exists(staging) &&
                !File.Exists(Path.Combine(temp.Path, "s1", "g000003.sav")))
                .OverrideFailureMessage("Another slot's maintenance modified the read-only future slot.").IsTrue();
            var replacement = repository.Write(Document(3, 3) with { Meta = Meta(3, 3) with { Reason = SaveReason.NewGame } },
                replaceUnusable: true);
            AssertThat(replacement.Info.Health == SlotHealth.Ok && replacement.Info.Meta?.Gen == 3 &&
                replacement.Info.Entries.Count == 1 && !File.Exists(latest) && !File.Exists(previous))
                .OverrideFailureMessage("An explicitly confirmed new game did not replace the future world after publication.").IsTrue();
        }
    }

    [TestCase]
    [TestCategory(TestReason.Regression)]
    public void FutureSlotStopsPendingMaintenanceWithoutDiscardingItsOriginalFailure()
    {
        using var temp = new TempSaveDirectory();
        using var repository = new SaveRepository(temp.Path, true, Sections());
        for (ulong gen = 1; gen <= SaveSlots.AutoLimit; gen++) repository.Write(Document(gen));
        var previous = Path.Combine(temp.Path, "s1", "g000001.sav");
        using (var held = new FileStream(previous, FileMode.Open, FileAccess.Read, FileShare.Read)) repository.Write(Document(4));
        AssertThat(repository.PendingMaintenanceErrors.Count != 0 && File.Exists(previous)).IsTrue();
        var originalFailure = repository.PendingMaintenanceErrors[0];
        var latest = Path.Combine(temp.Path, "s1", "g000005.sav");
        var future = SaveContainer.Encode(Document(5));
        BinaryPrimitives.WriteUInt16LittleEndian(future.AsSpan(8), (ushort)(SaveContainer.CurrentFormat + 1));
        Rehash(future);
        File.WriteAllBytes(latest, future);
        repository.Write(Document() with { Meta = Meta() with { Slot = new("s2") } });
        AssertThat(repository.Scan(new("s1")).Info.Health == SlotHealth.Newer && File.Exists(previous) &&
            File.ReadAllBytes(latest).SequenceEqual(future) && repository.PendingMaintenanceErrors.Any(error => ReferenceEquals(error, originalFailure)))
            .OverrideFailureMessage("A delayed sweep modified the future slot or discarded its earlier cleanup failure.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void UnreadableNewLineCannotRecoverOrBeWrittenOver()
    {
        using var temp = new TempSaveDirectory();
        var root = temp.Path;
        using var repository = new SaveRepository(root, true, Sections());
        repository.Write(Document());
        var path = Path.Combine(root, "s1", "g000002.sav");
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
