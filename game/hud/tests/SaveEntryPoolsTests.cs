using Campus.Contracts.Save;
using Campus.Hud.Pages;
using Campus.Kernel.Save.Format.Tests;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Campus.Tests;

[TestSuite]
public sealed class SaveEntryPoolsTests
{
    // 存档页按两个池看条目：自动档与身份读不出的条目不占手动名额；手动池满了，立即保存要确认替换的是最旧的那份手动档（DECISION-317）。
    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void ManualSaveConfirmsReplacingOnlyTheOldestManualEntryWhenFull()
    {
        var almost = Slot(SlotHealth.Ok, SaveSlots.ManualLimit - 1);
        AssertThat(SaveEntryPools.Manual(almost).Count() == SaveSlots.ManualLimit - 1 &&
            almost.Entries.Count(entry => !SaveEntryPools.IsManual(entry)) > SaveSlots.AutoLimit)
            .OverrideFailureMessage("The fixture must hold one manual entry fewer than the limit beside automatic and unidentified entries.").IsTrue();
        AssertThat(SaveEntryPools.ReplacedByManualSave(almost) is null)
            .OverrideFailureMessage("Automatic or unidentified entries were counted against the manual pool.").IsTrue();

        var full = Slot(SlotHealth.Ok, SaveSlots.ManualLimit);
        var oldest = SaveEntryPools.Manual(full).MinBy(entry => entry.Gen);
        AssertThat(SaveEntryPools.ReplacedByManualSave(full) == oldest)
            .OverrideFailureMessage("A full manual pool must name its oldest manual entry for the replace confirmation.").IsTrue();

        var newer = Slot(SlotHealth.Newer, 1);
        var manual = SaveEntryPools.Manual(newer).Single();
        AssertThat(SaveEntryPools.Deletable(full, oldest!) && !full.Entries.Where(e => !SaveEntryPools.IsManual(e)).Any(e => SaveEntryPools.Deletable(full, e)) &&
            !SaveEntryPools.Deletable(newer, manual) && !SaveEntryPools.Readable(newer, manual))
            .OverrideFailureMessage("Only identified manual entries outside a newer-version slot may be deleted, and a newer-version slot is never read.").IsTrue();
    }

    // 新到旧：最老的一份身份读不出，自动档多于轮换份数，手动档按要求的份数穿插其间。
    private static SlotInfo Slot(SlotHealth health, int manual)
    {
        var reasons = new List<SaveReason?> { null };
        for (var i = 0; i < Math.Max(manual, SaveSlots.AutoLimit + 1); i++)
        {
            reasons.Add(i % 2 == 0 ? SaveReason.Interval : SaveReason.Quit);
            if (i < manual) reasons.Add(SaveReason.Manual);
        }
        var entries = reasons.Select((reason, index) => new SaveEntry((ulong)(index + 1), reason is null ? SlotHealth.Corrupt : SlotHealth.Ok,
            reason is { } r ? SaveFormatTests.Document((ulong)(index + 1)).Meta with { Reason = r } : null)).Reverse().ToArray();
        return new SlotInfo(new("s1"), SlotKind.Auto, health, entries[0].Meta, entries.Length) { Entries = entries };
    }
}
