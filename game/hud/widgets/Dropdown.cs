using Campus.Contracts;
using Campus.Hud.Style;
using Godot;

namespace Campus.Hud.Widgets;

/// <summary>
/// 下拉框（UI-VISUAL）：引擎的 OptionButton，闭合时的外观同次按钮（1 px line.control，悬停 surface.selected，键盘焦点衍射边，
/// 右侧箭头），由 ui_panel 载体着色；展开的 PopupMenu 用 <see cref="UiThemeBuilder.DropdownPopupType"/> 的实底面板。
/// 用法与 <see cref="Segmented"/> 一致：Create、Selected、Changed（只在用户选择时发出）、Select(index, notify)。
/// 键盘：确认键展开，上下选项，确认键选定，取消键收起；Tab 与方向键焦点照常走。
/// </summary>
public partial class Dropdown : OptionButton
{
    private UiKit _kit = null!;
    private bool _hover, _focus, _open, _pin;

    public event Action<int>? Changed;

    public static Dropdown Create(UiKit kit, Control parent, string[] labels, int selected = -1, bool pin = false, string font = "body")
    {
        var arrow = kit.Icon("chevron_down");
        var drop = new Dropdown
        {
            _kit = kit,
            _pin = pin,
            FitToLongestItem = true,
            CustomMinimumSize = new Vector2(kit.Layout.Metric("dropdownMinWidth"), kit.Layout.Metric("dropdown")),
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
            FocusMode = FocusModeEnum.All,
            MouseFilter = MouseFilterEnum.Stop,
            ThemeTypeVariation = kit.Theme.DropdownType(font, kit.Layout.Metric("dropdownPadX"), kit.Layout.Metric("dropdownPadY"), arrow),
            Alignment = HorizontalAlignment.Left,
        };
        drop.Material = kit.Materials.Panel;
        for (var i = 0; i < labels.Length; i++) drop.AddItem(labels[i], i);
        drop.Select(selected);
        var popup = drop.GetPopup();
        popup.ThemeTypeVariation = kit.Theme.DropdownPopupType();
        popup.AboutToPopup += () => { drop.SyncPopupTheme(); drop._open = true; drop.Refresh(); };
        popup.PopupHide += () => { drop._open = false; drop.Refresh(); };
        drop.ItemSelected += index => drop.Changed?.Invoke((int)index);
        // 展开框的颜色要在展开前就位：鼠标进入、取得焦点、按下时都先对一次
        drop.MouseEntered += () => { drop._hover = true; drop.SyncPopupTheme(); drop.Refresh(); };
        drop.MouseExited += () => { drop._hover = false; drop.Refresh(); };
        drop.FocusEntered += () => { drop._focus = drop.HasFocus(true); drop.SyncPopupTheme(); drop.Refresh(); };
        drop.FocusExited += () => { drop._focus = false; drop.Refresh(); };
        drop.ButtonDown += drop.SyncPopupTheme;
        parent.AddChild(drop);
        if (pin) kit.Apply(drop, new UiFx(Pin: true));
        drop.Refresh();
        return drop;
    }

    /// <summary>程序选中某项；notify 时同用户选择一样发 Changed。</summary>
    public void Select(int index, bool notify)
    {
        var changed = index != Selected;
        Select(index);
        if (changed && notify) Changed?.Invoke(index);
    }

    // 展开框是独立窗口挂不了 ui_panel，打开前按界面主题钟的当前主题改写它的样式颜色（全局参数不读回，G4）
    private void SyncPopupTheme() => _kit.Theme.SetPopupDark(_pin && !_kit.ResidentFollows || _kit.ThemeState.Dark);

    private void Refresh()
    {
        var fx = new UiFx(
            Fill: _open ? "surface.selected" : null,
            Line: _open ? "primary.line" : "line.control",
            Role: Disabled ? "text.off" : "text.1",
            Flags: (Disabled ? UiFlags.Dashed : 0) | (_open ? 0 : UiFlags.HoverSelected),
            Hover: _hover && !Disabled ? 1 : 0,
            Focus: _focus ? 1 : 0,
            Pin: _pin);
        _kit.Apply(this, fx, remember: false);
    }
}
