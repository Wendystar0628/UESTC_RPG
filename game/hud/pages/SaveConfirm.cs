using Campus.Contracts.Maps;
using Campus.Contracts.Save;
using Campus.Hud.Style;
using Campus.Hud.Widgets;
using Godot;

namespace Campus.Hud.Pages;

/// <summary>
/// 存档页的危险确认宿主：画在界面根的对话框层，同一时刻至多一个；何时开关由存档页决定。
/// 框内附被影响存档的缩略卡（截图、地点、时刻、时长、保存时间），覆盖与删除共用（UI-VISUAL-COMPONENTS）。
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

    /// <summary>打开覆盖确认，返回取消按钮供页面交还焦点；坏档换一句说明。</summary>
    public TechButton ShowOverwrite(SlotInfo slot, Action confirm, Action cancel) =>
        Show(slot, "saves.overwrite.title", slot.Health == SlotHealth.Corrupt ? "saves.overwrite.body_corrupt" : "saves.overwrite.body",
            "saves.overwrite", confirm, cancel);

    private TechButton Show(SlotInfo slot, string title, string body, string confirmText, Action confirm, Action cancel)
    {
        _host = _kit.Layout.Host(_dialogParent, "SaveConfirm");
        _host.MouseFilter = Control.MouseFilterEnum.Stop;
        var scrim = _kit.Box(_host, new UiFx(Fill: "scrim", Margin: 0));
        scrim.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var dialog = _kit.Layout.Dialog(_host, UiKit.T(title));
        dialog.Tabs.Visible = false;
        _kit.Text(dialog.Body, UiKit.T(body, ("slot", SaveSlotCard.Number(slot.Slot))), "small", "text.2");
        // 缩略卡只展示，不接焦点和鼠标；外观与存档卡同一卡身。
        var panel = _kit.Panel(dialog.Body, new UiFx(Fill: "surface.solid", Line: "line.panel", Chamfer: _kit.Tokens.Chamfer("card")));
        panel.MouseFilter = Control.MouseFilterEnum.Ignore;
        var margin = _kit.Layout.Margin(panel);
        var content = _kit.Layout.Stack(margin, groups: false);
        _kit.Text(content, $"SLOT {SaveSlotCard.Number(slot.Slot)}", "mono-caps", "text.2");
        SaveSlotCard.Summary(_kit, content, margin, slot, _saves, _maps);
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
