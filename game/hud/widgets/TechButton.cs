using Campus.Hud.Style;
using Godot;

namespace Campus.Hud.Widgets;

public enum ButtonStyle : byte { Primary, Secondary, Text, Danger, Icon, Segment, Cell, Ghost, Tab, Item }

/// <summary>
/// 界面按钮（UI-VISUAL）：形状与颜色全部由 ui_panel kind 0 画，按钮自己的字和图标按 role 换色。
/// 六种状态：常态、悬停（效果弹簧 0.15 s 进 / 0.2 s 出）、按下（0 ms 进 / 120 ms 出，内容下沉 1 px）、
/// 焦点（衍射边 160 ms 收紧，只给键盘）、禁用（断电，仍可聚焦，读出原因）、选中（点亮）。
/// </summary>
public partial class TechButton : Button
{
    private UiKit _kit = null!;
    private Tween? _hoverTween, _focusTween;
    private float _hover, _focus;
    private bool _down, _off, _selected;

    public ButtonStyle Style { get; private set; }
    public bool Pin { get; set; }
    public float Chamfer { get; set; }
    public string? OffReason { get; set; }
    /// <summary>这个按钮代表的值（分段、天气格等用）。</summary>
    public int Value { get; set; }

    public bool Selected
    {
        get => _selected;
        set { if (_selected == value) return; _selected = value; Refresh(); }
    }

    /// <summary>断电态：字 n-500、边改虚线、不发光；仍可聚焦，悬浮说明写原因（UI-VISUAL）。</summary>
    public bool Off
    {
        get => _off;
        set { if (_off == value) return; _off = value; TooltipText = value ? OffReason ?? "" : ""; Refresh(); }
    }

    /// <summary>常态的底色（压在实景上的常驻图标钮要有面板底，UI-VISUAL）；null 为透明。</summary>
    public string? BaseFill { get; set; }

    /// <summary>覆盖默认的内容色，例如强调色的文字按钮。</summary>
    public string? RoleOverride { get; set; }

    public static TechButton Create(UiKit kit, ButtonStyle style, string text, string? icon, string fontRole, bool pin)
    {
        var padX = style switch
        {
            ButtonStyle.Segment or ButtonStyle.Cell => kit.Layout.Metric("segmentPadX"),
            ButtonStyle.Primary or ButtonStyle.Danger => kit.Layout.Metric("primaryPadX"),
            ButtonStyle.Icon => (kit.Layout.Metric("iconButton") - kit.Layout.Metric("iconInButton")) / 2,
            _ => kit.Layout.Metric("buttonPadX"),
        };
        var button = Create(kit, style, text, icon, fontRole, padX, kit.Layout.Metric("buttonPadY"),
            kit.Layout.Metric("iconInButton"), pin);
        var metric = style switch
        {
            ButtonStyle.Primary or ButtonStyle.Danger => "primary",
            ButtonStyle.Icon => "iconButton",
            ButtonStyle.Segment or ButtonStyle.Cell => "segment",
            ButtonStyle.Tab => "tab",
            _ => "secondary",
        };
        var width = style switch
        {
            ButtonStyle.Primary or ButtonStyle.Danger => kit.Layout.Metric("primaryMinWidth"),
            ButtonStyle.Secondary => kit.Layout.Metric("secondaryMinWidth"),
            ButtonStyle.Icon => kit.Layout.Metric("iconButton"),
            _ => 0,
        };
        button.CustomMinimumSize = new Vector2(width, kit.Layout.Metric(metric));
        button.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        return button;
    }

    public static TechButton Create(UiKit kit, ButtonStyle style, string text, string? icon, string fontRole, int padX, int padY,
        int iconMax, bool pin)
    {
        var button = new TechButton
        {
            _kit = kit,
            Style = style,
            Text = text,
            Pin = pin,
            FocusMode = FocusModeEnum.All,
            MouseFilter = MouseFilterEnum.Stop,
            ThemeTypeVariation = kit.Theme.ButtonType(fontRole, padX, padY, false, (int)kit.Tokens.F("metrics", "shape", "expandMargin"), iconMax,
                kit.Layout.Metric("iconGap")),
            ClipText = false,
        };
        if (icon is not null) { button.Icon = kit.Icon(icon); button.ExpandIcon = true; }
        button.Material = kit.Materials.Panel;
        button.Chamfer = style switch
        {
            ButtonStyle.Primary or ButtonStyle.Danger => kit.Tokens.Chamfer("primary"),
            _ => 0,
        };
        button.MouseEntered += () => button.AnimateHover(true);
        button.MouseExited += () => button.AnimateHover(false);
        button.FocusEntered += () => button.AnimateFocus(!Input.IsMouseButtonPressed(MouseButton.Left));
        button.FocusExited += () => button.AnimateFocus(false);
        button.ButtonDown += () => { button._down = true; button.Refresh(); };
        button.ButtonUp += () => { button._down = false; button.Refresh(); };
        button.Ready += button.Refresh;
        return button;
    }

    private void AnimateHover(bool on)
    {
        _hoverTween?.Kill();
        var response = _kit.Tokens.F("motion", "springs", "effect", on ? "in" : "out");
        var seconds = _kit.Motion.Fade(Motion.UiEase.SpringSettle(response));
        var from = _hover;
        var to = on ? 1f : 0f;
        if (seconds <= 0) { _hover = to; Refresh(); return; }
        _hoverTween = CreateTween();
        _hoverTween.TweenMethod(Callable.From<float>(t => { _hover = from + (to - from) * Motion.UiEase.Spring(t * seconds, response); Refresh(); }),
            0f, 1f, seconds);
    }

    private void AnimateFocus(bool keyboard)
    {
        _focusTween?.Kill();
        if (!keyboard || !HasFocus()) { _focus = 0; Refresh(); return; }
        var seconds = _kit.Motion.Spatial ? _kit.Tokens.Ms("focus", "tighten") : 0f;
        if (seconds <= 0) { _focus = 1; Refresh(); return; }
        var c = _kit.Tokens.Curve("Enter");
        _focusTween = CreateTween();
        _focusTween.TweenMethod(Callable.From<float>(t => { _focus = Math.Max(0.001f, Motion.UiEase.Bezier(t, c.X, c.Y, c.Z, c.W)); Refresh(); }),
            0f, 1f, seconds);
    }

    public void Refresh()
    {
        var fx = Compute();
        _kit.Apply(this, fx, remember: false);
        if (Style == ButtonStyle.Text) RefreshTextStyle();
    }

    private UiFx Compute()
    {
        var hover = _off ? 0 : _hover;
        var pressed = _down && !_off;
        var fx = new UiFx(Chamfer: Chamfer, Pin: Pin, Hover: hover, Focus: _focus);
        switch (Style)
        {
            case ButtonStyle.Primary:
                fx = _off
                    ? fx with { Fill = "surface.sunk", Role = "text.off" }
                    : fx with { Fill = pressed ? "primary.press" : "primary.fill", Line = "primary.line", Role = "primary.ink", Flags = UiFlags.Primary };
                break;
            case ButtonStyle.Danger:
                fx = _off
                    ? fx with { Fill = "surface.sunk", Role = "text.off" }
                    : fx with { Fill = "danger.fill", Role = "danger.ink", Flags = UiFlags.Solid };
                break;
            case ButtonStyle.Secondary:
            case ButtonStyle.Icon:
                fx = fx with
                {
                    Fill = pressed ? "surface.sunk" : _selected ? "surface.selected" : BaseFill,
                    Line = "line.control",
                    Role = _off ? "text.off" : _selected || hover > 0.5f ? "text.accent" : "text.1",
                    Flags = (_off ? UiFlags.Dashed : 0) | (_selected || pressed ? 0 : UiFlags.HoverSelected),
                };
                break;
            case ButtonStyle.Segment:
                fx = fx with
                {
                    Fill = pressed ? "surface.sunk" : _selected ? "surface.selected" : "surface.hover",
                    Role = _off ? "text.off" : _selected ? "text.1" : hover > 0.5f ? "text.accent" : "text.2",
                    Flags = _off ? UiFlags.Dashed : 0,
                    Line = _off ? "line.control" : null,
                    Hover = _selected ? 0 : hover,
                };
                if (!_selected && !pressed) fx = fx with { Fill = hover > 0.01f ? "surface.hover" : null };
                break;
            case ButtonStyle.Cell:
                fx = fx with
                {
                    Fill = pressed ? "surface.sunk" : _selected ? "surface.selected" : null,
                    Line = _off ? "line.control" : null,
                    Role = _off ? "text.off" : _selected || hover > 0.5f ? "text.accent" : "text.1",
                    Flags = (_off ? UiFlags.Dashed : 0) | UiFlags.HoverSelected,
                };
                break;
            case ButtonStyle.Text:
                fx = fx with { Role = _off ? "text.off" : "text.accent", Hover = 0 };
                break;
            case ButtonStyle.Ghost:
                fx = fx with { Role = "text.1" };
                break;
            case ButtonStyle.Tab:
                fx = fx with { Role = _off ? "text.off" : _selected ? "text.1" : hover > 0.5f ? "text.accent" : "text.2", Hover = 0 };
                break;
            case ButtonStyle.Item:
                // 列表项与开始页菜单：悬停 b-300 12% 底 + 左侧 2 px 指示条（UI-VISUAL）
                fx = fx with
                {
                    Fill = _selected ? "surface.selected" : null,
                    Flags = UiFlags.RowBar,
                    Role = _off ? "text.off" : _selected || hover > 0.5f ? "text.accent" : "text.1",
                    Hover = Math.Max(hover, _selected ? 1 : 0),
                };
                break;
            default:
                throw new InvalidOperationException("Unknown button style.");
        }
        if (RoleOverride is { } role && !_off) fx = fx with { Role = role };
        return fx;
    }

    // 文字按钮悬停：1 px 下划线（偏移 5），按下透明度 75%（UI-VISUAL）。下划线是一块 1 px 的面板，颜色同字色。
    private Panel? _underline;

    private void RefreshTextStyle()
    {
        SelfModulate = new Color(1, 1, 1, _down ? 0.75f : 1f);
        var show = _hover >= 0.5f && !_off;
        if (show && _underline is null)
        {
            var font = GetThemeFont("font");
            var size = GetThemeFontSize("font_size");
            var width = font.GetStringSize(Text, HorizontalAlignment.Left, -1, size).X;
            var y = Mathf.Round(Size.Y / 2 + font.GetAscent(size) / 2 + 5);
            _underline = _kit.Box(this, new UiFx(Fill: RoleOverride ?? "text.accent", Pin: Pin));
            _underline.Position = new Vector2(GetThemeStylebox("normal").GetMargin(Side.Left), y);
            _underline.Size = new Vector2(width, 1);
        }
        if (_underline is not null) _underline.Visible = show;
    }

    public override void _Notification(int what)
    {
        if (what == NotificationThemeChanged && _kit is not null) Refresh();
    }
}
