using Campus.Contracts.Maps;
using Campus.Contracts.Save;
using Campus.Hud.Style;
using Campus.Hud.Widgets;
using Godot;

namespace Campus.Hud.Pages;

/// <summary>
/// 存档页的危险确认宿主：画在界面根的对话框层，同一时刻至多一个；何时开关由存档页决定。
/// 框内附被影响存档的缩略卡（截图、地点、时刻、时长、保存时间）；覆盖、删整位、删一份手动档与替换最旧手动档共用（UI-VISUAL-COMPONENTS、SYS-SAVE）。
/// </summary>
internal sealed class SaveConfirm
{
    private readonly UiKit _kit;
    private readonly Control _dialogParent;
    private readonly ISaveService _saves;
    private readonly IMapService _maps;
    private Control? _host;

    public SaveConfirm(UiKit kit, Control dialogParent, ISaveService saves, IMapService maps)
    {
        _kit = kit;
        _dialogParent = dialogParent;
        _saves = saves;
        _maps = maps;
    }

    public bool IsOpen => _host is not null;

    /// <summary>打开覆盖确认，返回取消按钮供页面交还焦点；坏档换一句说明，有手动档时写明连同几份一起替换。</summary>
    public TechButton ShowOverwrite(SlotInfo slot, Action confirm, Action cancel)
    {
        var manual = SaveEntryPools.Manual(slot).Count();
        var body = slot.Health == SlotHealth.Corrupt ? "saves.overwrite.body_corrupt"
            : manual > 0 ? "saves.overwrite.body_manual" : "saves.overwrite.body";
        return Show(slot.Slot, SaveCardBody.Of(slot, _saves), null, UiKit.T("saves.overwrite.title"),
            UiKit.T(body, ("slot", SaveSlotCard.Number(slot.Slot)), ("manual", Count(manual))), "saves.overwrite", confirm, cancel);
    }

    /// <summary>删整条世界线（DeleteSlot）：写明连同几份手动存档。</summary>
    public TechButton ShowDeleteSlot(SlotInfo slot, Action confirm, Action cancel)
    {
        var manual = SaveEntryPools.Manual(slot).Count();
        return Show(slot.Slot, SaveCardBody.Of(slot, _saves), null, UiKit.T("saves.delete_slot.title"),
            UiKit.T(manual > 0 ? "saves.delete_slot.body_manual" : "saves.delete_slot.body",
                ("slot", SaveSlotCard.Number(slot.Slot)), ("manual", Count(manual))), "saves.delete", confirm, cancel);
    }

    /// <summary>删一份手动档（DeleteEntry），缩略卡画的就是这一份。</summary>
    public TechButton ShowDeleteEntry(SlotId slot, SaveEntry entry, Action confirm, Action cancel) =>
        Show(slot, SaveCardBody.Of(slot, entry, _saves), entry.Meta?.Reason, UiKit.T("saves.delete_entry.title"),
            UiKit.T("saves.delete_entry.body", ("slot", SaveSlotCard.Number(slot))), "saves.delete", confirm, cancel);

    /// <summary>手动池已满时的立即保存：附将被替换的最旧一份（SYS-SAVE、DECISION-317）。</summary>
    public TechButton ShowReplaceManual(SlotId slot, SaveEntry oldest, Action confirm, Action cancel) =>
        Show(slot, SaveCardBody.Of(slot, oldest, _saves), oldest.Meta?.Reason, UiKit.T("saves.replace.title"),
            UiKit.T("saves.replace.body", ("limit", Count(SaveSlots.ManualLimit))), "saves.replace", confirm, cancel);

    private static string Count(int value) => value.ToString(System.Globalization.CultureInfo.InvariantCulture);

    private TechButton Show(SlotId slot, SaveCardBody save, SaveReason? kind, string title, string body, string confirmText, Action confirm, Action cancel)
    {
        _host = _kit.Layout.Host(_dialogParent, "SaveConfirm");
        _host.MouseFilter = Control.MouseFilterEnum.Stop;
        var scrim = _kit.Box(_host, new UiFx(Fill: "scrim", Margin: 0));
        scrim.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var dialog = _kit.Layout.Dialog(_host, title);
        dialog.Tabs.Visible = false;
        _kit.Text(dialog.Body, body, "small", "text.2");
        // 缩略卡只展示，不接焦点和鼠标；外观与存档卡同一卡身。
        var panel = _kit.Panel(dialog.Body, new UiFx(Fill: "surface.solid", Line: "line.panel", Chamfer: _kit.Tokens.Chamfer("card")));
        panel.MouseFilter = Control.MouseFilterEnum.Ignore;
        var margin = _kit.Layout.Margin(panel);
        var content = _kit.Layout.Stack(margin, groups: false);
        var header = _kit.Layout.Row(content);
        _kit.Text(header, $"SLOT {SaveSlotCard.Number(slot)}", "mono-caps", "text.2");
        if (kind is { } reason) SaveSlotCard.KindTag(_kit, header, reason);
        SaveSlotCard.Summary(_kit, content, margin, save, _maps);
        var cancelButton = _kit.Button(dialog.Footer, ButtonStyle.Secondary, UiKit.T("saves.overwrite.cancel"), pressed: cancel);
        _kit.Button(dialog.Footer, ButtonStyle.Danger, UiKit.T(confirmText), "button.primary", pressed: confirm);
        return cancelButton;
    }

    /// <summary>立即移出树并排队释放；未打开时什么也不做。</summary>
    public void Release()
    {
        if (_host is null) return;
        _host.Visible = false;
        _host.GetParent()?.RemoveChild(_host);
        _host.QueueFree();
        _host = null;
    }
}
