using Campus.Contracts;
using Campus.Contracts.Atlas;
using Godot;

namespace Campus.Hud.Map;

/// <summary>
/// 目录页（SYS-ATLAS §4.4、UI-NAV §4.5）：按区分组（总览、核心、南、西、北、东、边界），组内依次是锚点、观景点、
/// 可进入的室内、路线。每项是 96×54 缩略图、中英双行名称、编号；"此处"用过孔灯。整张列表只算 1 个 Control，
/// 行在 _Draw 里画；没有搜索框，也没有文字输入。↑↓ 选择（地图页随之平移、高亮），Enter 打开传送卡。
/// </summary>
public partial class IndexList : Control
{
    private const float HeaderHeight = 40;
    private const float RowHeight = 66;
    private const float Pad = 16;
    private static readonly Vector2 Thumb = new(96, 54);

    private readonly MapHudContext _ctx;
    private readonly List<Row> _rows = [];
    private int _selected = -1;
    private int _hover = -1;
    private float _scroll;
    private float _contentHeight;

    private sealed record Row(float Top, float Height, string? Header, string? HeaderEn, MapEntry? Entry);

    public IndexList(MapHudContext ctx)
    {
        _ctx = ctx;
        FocusMode = FocusModeEnum.All;
        MouseFilter = MouseFilterEnum.Stop;
        ClipContents = true;
        AccessibilityName = "目录";
        MouseExited += () => { _hover = -1; QueueRedraw(); };
        FocusEntered += QueueRedraw;
        FocusExited += QueueRedraw;
    }

    public event Action<MapEntry>? SelectionChanged;
    public event Action<MapEntry>? Activated;
    public event Action<MapEntry>? Hovered;
    public ISet<MarkerId> Here { get; } = new HashSet<MarkerId>();
    public MapEntry? SelectedEntry => _selected >= 0 ? _rows[_selected].Entry : null;

    public void SetEntries(IReadOnlyList<MapEntry> entries)
    {
        var keep = SelectedEntry?.Id;
        _rows.Clear();
        float y = 4;
        var groups = entries.Where(e => e.Travelable)
            .GroupBy(e => e.Group).OrderBy(g => g.Key is { } gk ? (int)gk : int.MaxValue);
        foreach (var g in groups)
        {
            var label = g.Key is { } wg ? MapHudContext.GroupName(wg) : "其他";
            var en = g.Key is { } wg2 ? MapHudContext.GroupEn(wg2) : "OTHER";
            _rows.Add(new Row(y, HeaderHeight, label, en, null));
            y += HeaderHeight;
            foreach (var e in g.OrderBy(KindOrder).ThenBy(e => e.Id.Value, StringComparer.Ordinal))
            {
                _rows.Add(new Row(y, RowHeight, null, null, e));
                y += RowHeight;
            }
        }
        _contentHeight = y + 8;
        _selected = keep is { } k ? _rows.FindIndex(r => r.Entry?.Id == k) : -1;
        ClampScroll();
        QueueRedraw();
    }

    private static int KindOrder(MapEntry e) => e.Marker.Kind switch
    {
        MarkerKind.Anchor => 0,
        MarkerKind.Viewpoint => 1,
        MarkerKind.Interior => 2,
        MarkerKind.Route => 3,
        MarkerKind.Landmark or MarkerKind.Facility or MarkerKind.Custom or MarkerKind.Quest => 4,
        _ => throw new ArgumentOutOfRangeException(nameof(e)),
    };

    public void Select(MarkerId id)
    {
        var i = _rows.FindIndex(r => r.Entry?.Id == id);
        if (i >= 0) SetSelected(i, notify: false);
    }

    public void SelectFirst()
    {
        if (_selected < 0) Step(1);
    }

    private void SetSelected(int i, bool notify)
    {
        if (i == _selected) return;
        _selected = i;
        var row = _rows[i];
        if (row.Top - _scroll < 0) _scroll = row.Top - 4;
        else if (row.Top + row.Height - _scroll > Size.Y) _scroll = row.Top + row.Height - Size.Y + 4;
        ClampScroll();
        QueueRedraw();
        if (notify && row.Entry is { } e) SelectionChanged?.Invoke(e);
    }

    private void Step(int dir)
    {
        var i = _selected;
        for (var n = 0; n < _rows.Count; n++)
        {
            i += dir;
            if (i < 0 || i >= _rows.Count) return;
            if (_rows[i].Entry is not null) { SetSelected(i, notify: true); return; }
        }
    }

    private void ClampScroll() => _scroll = Math.Clamp(_scroll, 0, Math.Max(0, _contentHeight - Size.Y));

    private int RowAt(float y)
    {
        var yy = y + _scroll;
        return _rows.FindIndex(r => yy >= r.Top && yy < r.Top + r.Height && r.Entry is not null);
    }

    public override void _GuiInput(InputEvent e)
    {
        switch (e)
        {
            case InputEventMouseMotion mm:
                {
                    var i = RowAt(mm.Position.Y);
                    if (i != _hover)
                    {
                        _hover = i;
                        QueueRedraw();
                        if (i >= 0 && _rows[i].Entry is { } h) Hovered?.Invoke(h);
                    }
                    break;
                }
            case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.WheelUp or MouseButton.WheelDown } wheel:
                _scroll += (wheel.ButtonIndex == MouseButton.WheelDown ? 1 : -1) * RowHeight * 0.75f * MathF.Max(1, wheel.Factor);
                ClampScroll();
                QueueRedraw();
                AcceptEvent();
                break;
            case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } mb:
                {
                    var i = RowAt(mb.Position.Y);
                    if (i >= 0)
                    {
                        _ctx.KeyboardFocus = false;
                        GrabFocus();
                        SetSelected(i, notify: true);
                        if (_rows[i].Entry is { } a) Activated?.Invoke(a);
                    }
                    AcceptEvent();
                    break;
                }
            case InputEventKey { Pressed: true }:
                if (e.IsActionPressed("ui_down", allowEcho: true)) { _ctx.KeyboardFocus = true; Step(1); AcceptEvent(); }
                else if (e.IsActionPressed("ui_up", allowEcho: true)) { _ctx.KeyboardFocus = true; Step(-1); AcceptEvent(); }
                else if (e.IsActionPressed("ui_accept") && SelectedEntry is { } sel) { _ctx.KeyboardFocus = true; Activated?.Invoke(sel); AcceptEvent(); }
                break;
        }
    }

    public override void _Notification(int what)
    {
        if (what == NotificationResized) ClampScroll();
    }

    public override void _Draw()
    {
        var t = _ctx.ToneFor(this);
        var w = Size.X;
        var caps = _ctx.Style.Fonts["mono-caps"];
        var body = _ctx.Style.Fonts["body"];
        var small = _ctx.Style.Fonts["small"];
        var data = _ctx.Style.Fonts["data"];
        var stroke = _ctx.Style.Metrics.IconStroke;
        for (var i = 0; i < _rows.Count; i++)
        {
            var row = _rows[i];
            var top = row.Top - _scroll;
            if (top + row.Height < 0 || top > Size.Y) continue;
            if (row.Header is { } header)
            {
                // 组标题："mono-caps 小标签 + 发丝线"（UI-VISUAL §5.1）
                var baseline = top + 26;
                var zhF = _ctx.Style.Fonts["small"];
                MapDraw.Text(this, zhF, new(Pad, baseline), header, t.Text2);
                var x = Pad + zhF.Width(header) + 8;
                MapDraw.Text(this, caps, new(x, baseline - 1), row.HeaderEn ?? "", t.TextOff);
                x += caps.Width(row.HeaderEn ?? "") + 10;
                DrawLine(new(MathF.Round(x), MathF.Round(baseline - 5) + 0.5f), new(w - Pad, MathF.Round(baseline - 5) + 0.5f), t.Hair, MapDraw.Hair);
                continue;
            }
            var e = row.Entry!;
            var rect = new Rect2(0, top, w, row.Height);
            if (i == _selected)
            {
                DrawRect(rect, t.Selected);
                DrawRect(new Rect2(0, top, _ctx.Style.Metrics.Indicator, row.Height), t.PrimaryLine);
            }
            else if (i == _hover) DrawRect(rect, t.RowHover);
            // 缩略图位：WP-27 的截图机交图前是一块凹面，左下角是类型图标
            var th = new Rect2(Pad, top + (row.Height - Thumb.Y) * 0.5f, Thumb);
            DrawRect(th, t.Sunk);
            MapDraw.Outline(this, th.Grow(-0.5f), 0, t.Hair);
            var chip = new Vector2(th.Position.X + 13, th.End.Y - 13);
            DrawCircle(chip, 10, t.Solid, true, -1, true);
            DrawArc(chip, 10, 0, Mathf.Tau, 28, t.PanelLine, 1f, true);
            _ctx.Icon(this, e, chip, 14, e.Travelable ? t.Graphic : t.Text1, stroke, false);
            // 名称：中文主名、英文副名；右上角等宽编号
            var tx = th.End.X + 12;
            var idText = e.Id.Value;
            var idW = data.Width(idText);
            var nameWidth = w - tx - Pad - idW - 12;
            MapDraw.Text(this, body, new(tx, top + 29), Clip(body, e.Name.Zh, nameWidth), t.Text1);
            MapDraw.Text(this, data, new(w - Pad - idW, top + 28), idText, t.Text2);
            // 第二行右端的小签："此处"（过孔灯）或"仅旁观"；英文副名让出它的位置
            var tagX = w - Pad;
            var tagW = 0f;
            if (Here.Contains(e.Id))
            {
                var hereW = small.Width("此处");
                MapDraw.Text(this, small, new(tagX - hereW, top + 50), "此处", t.Accent);
                MapDraw.Led(this, new Vector2(tagX - hereW - 8, top + 45), t, true);
                tagW = hereW + 16;
            }
            else if (e.FlyOnly)
            {
                var label = "仅旁观";
                tagW = small.Width(label);
                MapDraw.Text(this, small, new(tagX - tagW, top + 50), label, t.TextOff);
            }
            MapDraw.Text(this, small, new(tx, top + 50), Clip(small, e.Name.En, w - tx - Pad - (tagW > 0 ? tagW + 10 : 0)), t.Text2);
        }
        if (_contentHeight > Size.Y)
        {
            // 滚动位置：右缘 2 px 的细条
            var h = MathF.Max(24, Size.Y * Size.Y / _contentHeight);
            var y = (Size.Y - h) * (_scroll / (_contentHeight - Size.Y));
            DrawRect(new Rect2(w - 4, y, 2, h), t.Control with { A = 0.6f });
        }
        if (HasFocus() && _ctx.KeyboardFocus && _selected >= 0)
        {
            var row = _rows[_selected];
            MapHudContext.FocusRing(this, new Rect2(4, row.Top - _scroll + 4, w - 8, row.Height - 8), t);
        }
    }

    private static string Clip(MapFont f, string text, float width)
    {
        if (f.Width(text) <= width) return text;
        while (text.Length > 1 && f.Width(text + "…") > width) text = text[..^1];
        return text + "…";
    }
}
