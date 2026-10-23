using Campus.Contracts;
using Campus.Contracts.Save;
using Campus.Tests;
using GdUnit4;
using static GdUnit4.Assertions;
using TempEntries = Campus.Kernel.Save.Format.Tests.SaveFormatTests.TempSaveDirectory;

namespace Campus.Kernel.Save.Format.Tests;

[TestSuite]
public sealed class SaveEntryTests
{
    private static SaveSections Sections() => SaveFormatTests.Sections();

    private static SaveDocument Document(ulong gen, SaveReason reason = SaveReason.Test, ulong line = 1)
    {
        var document = SaveFormatTests.Document(gen, line);
        return document with { Meta = document.Meta with { Reason = reason } };
    }

    [TestCase]
    [TestCategory(TestReason.Regression)]
    public void ManualAndAutomaticPoolsRotateIndependentlyAndCommitReportsDiskEntries()
    {
        using var temp = new TempEntries();
        using var repository = new SaveRepository(temp.Path, true, Sections());
        ulong generation = 1;
        repository.Write(Document(generation, SaveReason.NewGame));
        SlotSnapshot? committed = null;
        for (var index = 0; index <= SaveSlots.ManualLimit; index++)
        {
            repository.Write(Document(++generation, SaveReason.Manual));
            committed = repository.Write(Document(++generation, SaveReason.Quit));
        }
        var entries = committed!.Info.Entries;
        AssertThat(entries.Count(e => e.Meta?.Reason == SaveReason.Manual) == SaveSlots.ManualLimit &&
            entries.Count(e => e.Meta?.Reason != SaveReason.Manual) == SaveSlots.AutoLimit &&
            entries.Count == committed.Info.Generations && committed.Info.Meta?.Gen == generation).IsTrue();
        AssertThat(!File.Exists(temp.Entry(2)) && File.Exists(temp.Entry(4)) &&
            entries.Select(e => e.Gen).SequenceEqual(entries.Select(e => e.Gen).OrderByDescending(g => g)))
            .OverrideFailureMessage("Manual overflow must publish a new entry and remove only the oldest manual entry.").IsTrue();
        var rescanned = repository.Scan(new("s1"));
        AssertThat(rescanned.Info.Entries.SequenceEqual(entries) && rescanned.Info.Generations == entries.Count).IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void ExactEntryFailureNeverFallsBackOrCrossesAnUnknownWorldLine()
    {
        using var temp = new TempEntries();
        using var repository = new SaveRepository(temp.Path, true, Sections());
        repository.Write(Document(1));
        var broken = Document(2, SaveReason.Manual) with { Sections = new Dictionary<string, RawSaveSection>
            { ["world"] = new(1, "{\"v\":1,\"data\":{\"hours\":\"bad\"}}"u8.ToArray()) } };
        File.WriteAllBytes(temp.Entry(2), SaveContainer.Encode(broken));
        var scan = repository.Scan(new("s1"));
        AssertThat(scan.Info.Meta?.Gen == 1 && scan.Info.Entries.Count == 2 &&
            scan.Info.Entries[0].Health == SlotHealth.Corrupt && scan.Info.Entries[0].Meta?.Reason == SaveReason.Manual).IsTrue();
        var rejected = false;
        try { repository.ReadEntry(new("s1"), 2); }
        catch (SaveFormatException) { rejected = true; }
        AssertThat(rejected && repository.ReadEntry(new("s1"), 1).Document.Meta.Gen == 1).IsTrue();
        File.WriteAllBytes(temp.Entry(2), [0]);
        AssertThat(repository.Scan(new("s1")).Info.Health == SlotHealth.Corrupt).IsTrue();
        rejected = false;
        try { repository.ReadEntry(new("s1"), 1); }
        catch (SaveFormatException) { rejected = true; }
        AssertThat(rejected).OverrideFailureMessage("Exact selection crossed an unknown world-line identity.").IsTrue();
        File.WriteAllBytes(temp.Entry(2), SaveContainer.Encode(Document(2, SaveReason.NewGame, 2)));
        rejected = false;
        try { repository.ReadEntry(new("s1"), 1); }
        catch (SaveFormatException) { rejected = true; }
        AssertThat(rejected && repository.Scan(new("s1")).Info.Entries.Count == 1)
            .OverrideFailureMessage("Exact selection crossed a known world-line boundary.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void ExactThumbnailCacheDoesNotChangeTheLatestSummary()
    {
        using var temp = new TempEntries();
        using var repository = new SaveRepository(temp.Path, true, Sections());
        var first = "RIFF\u0004\0\0\0WEBP"u8.ToArray();
        var second = "RIFF\u0005\0\0\0WEBPa"u8.ToArray();
        repository.Write(Document(1, SaveReason.Manual) with { Thumbnail = first });
        var scan = repository.Write(Document(2) with { Thumbnail = second });
        AssertThat(scan.Thumbnails.Count == 2 && scan.Thumbnails[1].SequenceEqual(first) &&
            scan.Thumbnails[2].SequenceEqual(second)).IsTrue();
        AssertThat(repository.ReadEntry(new("s1"), 1).Document.Meta.Gen == 1 &&
            repository.Scan(new("s1")).Info.Meta?.Gen == 2).IsTrue();
        File.Delete(temp.Entry(1));
        AssertThat(scan.Thumbnails[1].SequenceEqual(first) && !repository.Scan(new("s1")).Thumbnails.ContainsKey(1))
            .OverrideFailureMessage("The published cache must stay usable without rereading an entry file.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void IndividualDeletionOnlyAcceptsManualEntriesAndWholeDeletionKeepsTheProcessLock()
    {
        using var temp = new TempEntries();
        using var repository = new SaveRepository(temp.Path, true, Sections());
        repository.Write(Document(1, SaveReason.Manual));
        repository.Write(Document(2));
        var rejected = false;
        try { repository.DeleteEntry(new("s1"), 2); }
        catch (InvalidOperationException) { rejected = true; }
        AssertThat(rejected && File.Exists(temp.Entry(2))).IsTrue();
        var deleted = repository.DeleteEntry(new("s1"), 1);
        AssertThat(!File.Exists(temp.Entry(1)) && deleted.Info.Entries.Count == 1 && deleted.Info.Meta?.Gen == 2).IsTrue();
        using var readOnly = new SaveRepository(temp.Path, false, Sections());
        rejected = false;
        try { readOnly.DeleteSlot(new("s1")); }
        catch (InvalidOperationException) { rejected = true; }
        AssertThat(rejected && File.Exists(temp.Entry(2))).IsTrue();
        var empty = repository.DeleteSlot(new("s1"));
        using var secondWriter = new SaveRepository(temp.Path, true, Sections());
        AssertThat(empty.Info.Health == SlotHealth.Empty && !Directory.Exists(Path.Combine(temp.Path, "s1")) &&
            repository.Enabled && !secondWriter.Enabled).IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Regression)]
    public void ANewerOlderEntryProtectsTheWholeSlotAndCannotBeLoadedAround()
    {
        using var temp = new TempEntries();
        using var repository = new SaveRepository(temp.Path, true, Sections());
        repository.Write(Document(1, SaveReason.Manual));
        var future = Document(1, SaveReason.Manual) with { Sections = new Dictionary<string, RawSaveSection>
            { ["world"] = new(2, "{\"v\":2,\"data\":{\"hours\":17.5}}"u8.ToArray()) } };
        var futureBytes = SaveContainer.Encode(future);
        File.WriteAllBytes(temp.Entry(1), futureBytes);
        File.WriteAllBytes(temp.Entry(2), SaveContainer.Encode(Document(2)));
        var scan = repository.Scan(new("s1"));
        AssertThat(scan.Info.Entries[0].Health == SlotHealth.Ok && scan.Info.Health == SlotHealth.Newer && scan.Save is null).IsTrue();
        var rejected = false;
        try { repository.ReadEntry(new("s1"), 2); }
        catch (SaveFormatException) { rejected = true; }
        AssertThat(rejected).OverrideFailureMessage("Selecting a readable entry bypassed the whole-slot future-version guard.").IsTrue();
        rejected = false;
        try { repository.DeleteEntry(new("s1"), 1); }
        catch (InvalidOperationException) { rejected = true; }
        AssertThat(rejected && File.ReadAllBytes(temp.Entry(1)).SequenceEqual(futureBytes)).IsTrue();
        repository.Write(Document(1) with { Meta = Document(1).Meta with { Slot = new("s2") } });
        AssertThat(File.ReadAllBytes(temp.Entry(1)).SequenceEqual(futureBytes) && File.Exists(temp.Entry(2)))
            .OverrideFailureMessage("Another slot's maintenance changed the future slot.").IsTrue();
        AssertThat(repository.DeleteSlot(new("s1")).Info.Health == SlotHealth.Empty &&
            !File.Exists(temp.Entry(1)) && !File.Exists(temp.Entry(2)))
            .OverrideFailureMessage("Explicit whole-world deletion did not remove the future slot.").IsTrue();
    }
}
