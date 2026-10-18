using Campus.Contracts;
using Campus.Hud.Style;
using Godot;

namespace Campus.Hud.Widgets;

/// <summary>
/// 下拉框（UI-VISUAL）：引擎的 OptionButton，闭合时的外观同次按钮（1 px line.control，悬停 surface.selected，键盘焦点衍射边，
/// 右侧箭头），由 ui_panel 载体着色；展开的 PopupMenu 用 <see cref="UiThemeBuilder.DropdownPopupType"/> 的实底面板。
/// 用法与 <see cref="Segmented"/> 一致：Create、Selected、Changed（只在用户选择时发出）、Select(index, notify)。
/// 键盘与手柄：确认键展开，上下选项，确认键选定，取消键收起；Tab 与方向键焦点照常走。
/// </summary>
public partial class Dropdown : OptionButton
{
    private const int PadX = 12, PadY = 2;

    private UiKit _kit = null!;
    private bool _hover, _focus, _open;

    public event Action<int>? Changed;

    public static Dropdown Create(UiKit kit, Control parent, Vector2 position, string[] labels, int selected = -1,
        float minWidth = 0, float height = 36, string font = "body")
    {
        var arrow = kit.Icon("chevron_down");
        var fv = kit.Theme.Font(font, out var fontSize, out _);
        var widest = labels.Length == 0 ? 0 : labels.Max(l => fv.GetStringSize(l, HorizontalAlignment.Left, -1, fontSize).X);
        var width = MathF.Ceiling(Math.Max(minWidth, widest + PadX * 2 + arrow.GetWidth() + 4));
        var drop = new Dropdown
        {
            _kit = kit,
            Position = position,
            Size = new Vector2(width, height),
            CustomMinimumSize = new Vector2(width, height),
            FocusMode = FocusModeEnum.All,
            MouseFilter = MouseFilterEnum.Stop,
            ThemeTypeVariation = kit.Theme.DropdownType(font, PadX, PadY, arrow),
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
        drop.FocusEntered += () => { drop._focus = !Input.IsMouseButtonPressed(MouseButton.Left); drop.SyncPopupTheme(); drop.Refresh(); };
        drop.FocusExited += () => { drop._focus = false; drop.Refresh(); };
        drop.ButtonDown += drop.SyncPopupTheme;
        parent.AddChild(drop);
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
    private void SyncPopupTheme()
    {
        if (GetTree().Root.FindChild("UiThemeClock", true, false) is UiThemeClock clock) _kit.Theme.SetPopupDark(clock.Dark);
    }

    private void Refresh()
    {
        var fx = new UiFx(
            Fill: _open ? "surface.selected" : null,
            Line: _open ? "primary.line" : "line.control",
            Role: Disabled ? "text.off" : "text.1",
            Flags: (Disabled ? UiFlags.Dashed : 0) | (_open ? 0 : UiFlags.HoverSelected),
            Hover: _hover && !Disabled ? 1 : 0,
            Focus: _focus ? 1 : 0);
        _kit.Apply(this, fx, remember: false);
    }
}
