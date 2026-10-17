using Godot;

namespace Campus.Hud.Map;

public enum MapButtonKind { Icon, Primary, Secondary, Tab }

/// <summary>
/// 地图界面的按钮（UI-VISUAL 的六种状态里用到的五种：闲置、悬停、按下、焦点、禁用）。
/// 一个按钮只算 1 个 Control；字和图标都在 _Draw 里画，状态变了才重画。
/// </summary>
public partial class MapButton : Control
{
    private bool _hover;
    private bool _down;
    private bool _disabled;
    private bool _selected;
    private string _text = "";

    public MapButton(MapButtonKind kind, MapHudContext ctx)
    {
        Kind = kind;
        Ctx = ctx;
        FocusMode = FocusModeEnum.All;
        MouseFilter = MouseFilterEnum.Stop;
        MouseDefaultCursorShape = CursorShape.PointingHand;
        MouseEntered += () => { _hover = true; QueueRedraw(); };
        MouseExited += () => { _hover = false; _down = false; QueueRedraw(); };
        FocusEntered += QueueRedraw;
        FocusExited += QueueRedraw;
    }

    public MapButtonKind Kind { get; }
    public MapHudContext Ctx { get; }
    public event Action? Pressed;
    /// <summary>图标按钮的绘制：(画布, 中心, 颜色, 线宽)。</summary>
    public Action<CanvasItem, Vector2, Color, float>? Glyph { get; set; }
    public string KeyHint { get; set; } = "";

    public string Text
    {
        get => _text;
        set { if (_text == value) return; _text = value; QueueRedraw(); }
    }

    public bool Disabled
    {
        get => _disabled;
        set { if (_disabled == value) return; _disabled = value; FocusMode = value ? FocusModeEnum.None : FocusModeEnum.All; QueueRedraw(); }
    }

    public bool Selected
    {
        get => _selected;
        set { if (_selected == value) return; _selected = value; QueueRedraw(); }
    }

    public void Press()
    {
        if (!_disabled) Pressed?.Invoke();
    }

    public override void _GuiInput(InputEvent e)
    {
        if (_disabled) return;
        switch (e)
        {
            case InputEventMouseButton { ButtonIndex: MouseButton.Left } mb:
                if (mb.Pressed) { _down = true; QueueRedraw(); }
                else if (_down) { _down = false; QueueRedraw(); if (_hover) Press(); }
                AcceptEvent();
                break;
            case InputEventKey { Pressed: true, Echo: false } k when e.IsActionPressed("ui_accept") || k.PhysicalKeycode == Key.Space:
                Press();
                AcceptEvent();
                break;
        }
    }

    public override void _Draw()
    {
        var t = Ctx.ToneFor(this);
        var r = new Rect2(Vector2.Zero, Size);
        var stroke = Ctx.Style.Metrics.IconStroke;
        switch (Kind)
        {
            case MapButtonKind.Icon:
                {
                    var fill = _down ? t.Sunk : t.Panel;
                    MapDraw.Panel(this, r, 0, fill, _hover ? t.CardHover : t.PanelLine, t.Highlight);
                    if (_hover && !_down) DrawRect(r.Grow(-1), t.Hover);
                    var ink = _disabled ? t.TextOff : _selected ? t.Accent : t.Text1;
                    Glyph?.Invoke(this, r.GetCenter(), ink, stroke);
                    if (KeyHint.Length > 0)
                    {
                        var f = Ctx.Style.Fonts["tick"];
                        MapDraw.Text(this, f, new(r.End.X - 4 - f.Width(KeyHint), r.End.Y - 4), KeyHint, t.Text2);
                    }
                    break;
                }
            case MapButtonKind.Primary:
                {
                    var fill = _disabled ? t.Sunk : _down ? t.PrimaryPress : _hover ? t.PrimaryHover : t.PrimaryFill;
                    var chamfer = Size.X >= 48 ? Ctx.Style.Metrics.ChamferCard - 2 : 0;
                    DrawColoredPolygon(MapDraw.ChipPoints(r, chamfer), fill);
                    if (!_disabled) MapDraw.Outline(this, r.Grow(-1.5f), Math.Max(0, chamfer - 0.6f), t.PrimaryLine);
                    var f = Ctx.Style.Fonts["button.primary"];
                    MapDraw.Text(this, f, new(0, r.Size.Y * 0.5f + f.Ascent * 0.36f), _text, _disabled ? t.TextOff : t.PrimaryInk, HorizontalAlignment.Center, r.Size.X);
                    break;
                }
            case MapButtonKind.Secondary:
                {
                    var fill = _down ? t.Sunk : _hover ? t.Selected : t.Raised;
                    DrawRect(r, fill);
                    MapDraw.Outline(this, r.Grow(-0.5f), 0, _disabled ? t.Hair : t.Control);
                    var f = Ctx.Style.Fonts["segment"];
                    var ink = _disabled ? t.TextOff : t.Text1;
                    var baseline = r.Size.Y * 0.5f + f.Ascent * 0.36f;
                    if (Glyph is null && KeyHint.Length == 0)
                    {
                        MapDraw.Text(this, f, new(0, baseline), _text, ink, HorizontalAlignment.Center, r.Size.X);
                        break;
                    }
                    // 图标 + 字 + 键名：[× 收起 M]
                    var hintF = Ctx.Style.Fonts["tick"];
                    var glyphW = Glyph is null ? 0 : 22f;
                    var hintW = KeyHint.Length > 0 ? hintF.Width(KeyHint) + 10 : 0;
                    var x = (r.Size.X - glyphW - f.Width(_text) - hintW) * 0.5f;
                    Glyph?.Invoke(this, new Vector2(x + 8, r.Size.Y * 0.5f), ink, stroke);
                    MapDraw.Text(this, f, new(x + glyphW, baseline), _text, ink);
                    if (hintW > 0) MapDraw.Text(this, hintF, new(x + glyphW + f.Width(_text) + 10, baseline), KeyHint, t.TextOff);
                    break;
                }
            case MapButtonKind.Tab:
                {
                    if (_hover && !_selected) DrawRect(r, t.Hover);
                    var f = Ctx.Style.Fonts[_selected ? "segment.on" : "segment"];
                    var label = _text;
                    var textW = f.Width(label);
                    var hintF = Ctx.Style.Fonts["tick"];
                    var hintW = KeyHint.Length > 0 ? hintF.Width(KeyHint) + 8 : 0;
                    var x = (r.Size.X - textW - hintW) * 0.5f;
                    var baseline = r.Size.Y * 0.5f + f.Ascent * 0.36f;
                    MapDraw.Text(this, f, new(x, baseline), label, _selected ? t.Text1 : t.Text2);
                    if (hintW > 0) MapDraw.Text(this, hintF, new(x + textW + 8, baseline), KeyHint, t.TextOff);
                    if (_selected) DrawRect(new Rect2(0, r.Size.Y - Ctx.Style.Metrics.Indicator, r.Size.X, Ctx.Style.Metrics.Indicator), t.PrimaryLine);
                    break;
                }
            default: throw new InvalidOperationException(Kind.ToString());
        }
        if (HasFocus() && Ctx.KeyboardFocus) MapHudContext.FocusRing(this, r, t);
    }
}
