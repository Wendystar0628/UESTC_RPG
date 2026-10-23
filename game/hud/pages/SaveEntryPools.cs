using Campus.Contracts.Save;

namespace Campus.Hud.Pages;

/// <summary>
/// 存档页按两个池看一条世界线的条目（SYS-SAVE、DECISION-317）：类型只取 meta 的保存原因，身份读不出的条目不属于任何一池。
/// 条目列表新到旧（SlotInfo.Entries），所以最旧的手动档是最后一份。这里只读，不重判存档服务的轮换与删除规则。
/// </summary>
internal static class SaveEntryPools
{
    public static bool IsManual(SaveEntry entry) => entry.Meta?.Reason == SaveReason.Manual;

    public static IEnumerable<SaveEntry> Manual(SlotInfo slot) => slot.Entries.Where(IsManual);

    public static IEnumerable<SaveEntry> Auto(SlotInfo slot) => slot.Entries.Where(entry => entry.Meta is { Reason: not SaveReason.Manual });

    /// <summary>手动池已满时立即保存要替换的那一份；没满为 null。</summary>
    public static SaveEntry? ReplacedByManualSave(SlotInfo slot)
    {
        var manual = Manual(slot).ToList();
        return manual.Count >= SaveSlots.ManualLimit ? manual[^1] : null;
    }

    /// <summary>能单删的只有身份可读的手动档，且存档位里没有来自更新版本的条目（SYS-SAVE-FORMAT）。</summary>
    public static bool Deletable(SlotInfo slot, SaveEntry entry) => slot.Health != SlotHealth.Newer && IsManual(entry);

    /// <summary>能读取的条目：本身可读，且存档位不是来自更新版本的只读位。</summary>
    public static bool Readable(SlotInfo slot, SaveEntry entry) => slot.Health != SlotHealth.Newer && entry.Health == SlotHealth.Ok;
}
