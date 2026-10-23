using Campus.Contracts.Maps;
using Campus.Contracts.Save;
using Campus.Hud.Style;
using Campus.Hud.Widgets;
using Godot;

namespace Campus.Hud.Pages;

/// <summary>
/// 存档页选中存档位后列出的条目，新到旧（SYS-SAVE、DECISION-317）：每行类型签条、缩略图、地点与保存时间，命中层同存档卡。
/// 选中态只由存档页切换；读取、删除与立即保存的取舍也归存档页。
/// </summary>
internal sealed class SaveEntryList
{
    private readonly UiKit _kit;
    private readonly List<(ulong Gen, PanelContainer Panel, TechButton Hit)> _rows;

    private SaveEntryList(UiKit kit, List<(ulong, PanelContainer, TechButton)> rows)
    {
        _kit = kit;
        _rows = rows;
    }

    /// <summary>条目行被选中（按下或得到焦点），参数是代号。</summary>
    public event Action<ulong>? Selected;

    public static SaveEntryList Build(UiKit kit, Control parent, SlotInfo slot, ISaveService saves, IMapService maps)
    {
        var rows = new List<(ulong, PanelContainer, TechButton)>();
        var list = new SaveEntryList(kit, rows);
        if (slot.Entries.Count == 0) return list;
        var section = kit.Layout.Stack(parent, groups: false);
        kit.Text(section, UiKit.T("saves.entries", ("slot", SaveSlotCard.Number(slot.Slot)),
            ("count", slot.Entries.Count.ToString(System.Globalization.CultureInfo.InvariantCulture))), "mono-caps", "text.2");
        foreach (var entry in slot.Entries)
        {
            var gen = entry.Gen;
            var (panel, hit) = Row(kit, section, SaveCardBody.Of(slot.Slot, entry, saves), maps);
            hit.TooltipText = $"SLOT {SaveSlotCard.Number(slot.Slot)} · {SaveMetaText.SavedAt(entry.Meta?.SavedAtUnix ?? 0)}";
            hit.Pressed += () => list.Selected?.Invoke(gen);
            hit.FocusEntered += () => list.Selected?.Invoke(gen);
            rows.Add((gen, panel, hit));
        }
        return list;
    }

    /// <summary>某一代的命中层，存档页重建后按它交回焦点；不在列表里为 null。</summary>
    public TechButton? Hit(ulong gen) => _rows.FirstOrDefault(row => row.Gen == gen).Hit;

    public bool Contains(Control? control) => control is not null && _rows.Any(row => row.Hit == control);

    /// <summary>null 表示选中的是整个存档位（卡片），条目行都不亮。</summary>
    public void SetSelected(ulong? gen)
    {
        foreach (var (rowGen, panel, hit) in _rows)
        {
            var selected = rowGen == gen;
            _kit.Apply(panel, RowFx(_kit, selected));
            hit.Selected = selected;
        }
    }

    // 一行：左缩略图（宽随行高按 16:9 取，行高由右侧两行字决定），右侧上行类型签条与保存时间、下行地点。
    private static (PanelContainer, TechButton) Row(UiKit kit, Control parent, SaveCardBody save, IMapService maps)
    {
        var panel = kit.Panel(parent, RowFx(kit, selected: false));
        var margin = kit.Layout.Margin(panel);
        var body = kit.Layout.Row(margin);
        var thumbnail = kit.Panel(body, new UiFx(Fill: save.Health == SlotHealth.Corrupt ? "danger.fill" : "surface.sunk", Line: "line.hair"));
        thumbnail.Resized += () =>
        {
            var width = thumbnail.Size.Y * SaveSlotCard.ThumbnailRatio;
            if (!Mathf.IsEqualApprox(thumbnail.CustomMinimumSize.X, width)) thumbnail.CustomMinimumSize = new Vector2(width, 0);
        };
        SaveSlotCard.TryThumbnail(thumbnail, save.Thumbnail);
        var data = kit.Layout.Stack(body, groups: false);
        var header = kit.Layout.Row(data);
        if (save.Meta is { } meta) SaveSlotCard.KindTag(kit, header, meta.Reason);
        var savedAt = kit.Text(header, SaveMetaText.SavedAt(save.Meta?.SavedAtUnix ?? 0), "data", "text.2", wrap: false);
        savedAt.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        savedAt.HorizontalAlignment = HorizontalAlignment.Right;
        kit.Text(data, SaveSlotCard.Place(save, maps), "body", SaveSlotCard.PlaceRole(save.Health));
        var hit = kit.Button(panel, ButtonStyle.Ghost, "");
        hit.SizeFlagsHorizontal = hit.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        return (panel, hit);
    }

    private static UiFx RowFx(UiKit kit, bool selected) => new(Fill: "surface.solid", Line: selected ? "primary.line" : "line.panel",
        Chamfer: kit.Tokens.Chamfer("card"), Flags: selected ? UiFlags.SelectedIris : 0);
}
