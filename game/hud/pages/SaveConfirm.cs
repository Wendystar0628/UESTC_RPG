using Campus.Contracts.Save;
using Campus.Hud.Style;
using Campus.Hud.Widgets;
using Godot;

namespace Campus.Hud.Pages;

/// <summary>存档页的危险确认宿主：画在界面根的对话框层，同一时刻至多一个；何时开关由存档页决定。</summary>
internal sealed class SaveConfirm
{
    private readonly UiKit _kit;
    private readonly Control _dialogParent;
    private Control? _host;

    public SaveConfirm(UiKit kit, Control dialogParent)
    {
        _kit = kit;
        _dialogParent = dialogParent;
    }

    public bool IsOpen => _host is not null;

    /// <summary>打开覆盖确认，返回取消按钮供页面交还焦点。</summary>
    public TechButton ShowOverwrite(SlotId slot, Action confirm, Action cancel)
    {
        _host = _kit.Layout.Host(_dialogParent, "OverwriteConfirm");
        _host.MouseFilter = Control.MouseFilterEnum.Stop;
        var scrim = _kit.Box(_host, new UiFx(Fill: "scrim", Margin: 0));
        scrim.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var dialog = _kit.Layout.Dialog(_host, UiKit.T("saves.overwrite.title"));
        dialog.Tabs.Visible = false;
        _kit.Text(dialog.Body, UiKit.T("saves.overwrite.body", ("slot", SaveSlotCard.Number(slot))), "small", "text.2");
        var cancelButton = _kit.Button(dialog.Footer, ButtonStyle.Secondary, UiKit.T("saves.overwrite.cancel"), pressed: cancel);
        _kit.Button(dialog.Footer, ButtonStyle.Danger, UiKit.T("saves.overwrite"), "button.primary", pressed: confirm);
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
