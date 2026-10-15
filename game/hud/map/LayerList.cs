using Campus.Contracts.Atlas;
using Godot;

namespace Campus.Hud.Map;

/// <summary>
/// 左栏"图层"页（SYS-ATLAS §4.3）：锚点、观景点、可进入的室内、路线、地标、设施、我的标记，另加"外围街区"；
/// 每行写当前图里的数量。下面是图例：可前往、此处、追踪中、未解锁（RPG 预留，观光模式下灰显）。
/// 整页 1 个 Control。
/// </summary>
public partial class LayerList : Control
{
    private const float Pad = 16;
    private const float RowHeight = 40;
    private const float Top = 12;
    public const int OutskirtsBit = 8;

    private static readonly (int Bit, string Zh, MarkerKind? Kind)[] Layers =
    [
        (0, "锚点", MarkerKind.Anchor), (1, "观景点", MarkerKind.Viewpoint), (2, "可进入的室内", MarkerKind.Interior),
        (3, "路线", MarkerKind.Route), (4, "地标", MarkerKind.Landmark), (5, "设施", MarkerKind.Facility),
        (6, "我的标记", MarkerKind.Custom), (OutskirtsBit, "外围街区", null),
    ];

    private static readonly MapEntry LegendAnchor = new(new AtlasMarker(new MarkerId("legend"), MarkerKind.Anchor, new("", ""), default,
        "anchor", 0, 0, null, new TeleportSpec(new Campus.Contracts.Pose(default, 0, 0), null)), null);
    private static readonly (string Zh, int State)[] LegendStates = [("可前往", 0), ("此处", 1), ("追踪中", 2), ("未解锁（RPG 预留）", 3)];

    private readonly MapHudContext _ctx;
    private readonly int[] _counts = new int[Layers.Length];
    private int _hover = -1;
    private int _focus;

    public LayerList(MapHudContext ctx)
    {
        _ctx = ctx;
        FocusMode = FocusModeEnum.All;
        MouseFilter = MouseFilterEnum.Stop;
        AccessibilityName = "图层";
        MouseExited += () => { _hover = -1; QueueRedraw(); };
        FocusEntered += QueueRedraw;
        FocusExited += QueueRedraw;
    }

    /// <summary>默认：设施、地标关，其余开（SYS-ATLAS §12）。</summary>
    public int Mask { get; private set; } = ~((1 << 4) | (1 << 5));
    public event Action<int>? MaskChanged;

    public void SetCounts(IReadOnlyList<MapEntry> entries)
    {
        for (var i = 0; i < Layers.Length; i++)
            _counts[i] = Layers[i].Kind is { } k ? entries.Count(e => e.Marker.Kind == k) : -1;
        QueueRedraw();
    }

    private void Toggle(int row)
    {
        Mask ^= 1 << Layers[row].Bit;
        QueueRedraw();
        MaskChanged?.Invoke(Mask);
    }

    private int RowAt(float y)
    {
        var i = (int)MathF.Floor((y - Top - 32) / RowHeight);
        return i >= 0 && i < Layers.Length ? i : -1;
    }

    public override void _GuiInput(InputEvent e)
    {
        switch (e)
        {
            case InputEventMouseMotion mm:
                var h = RowAt(mm.Position.Y);
                if (h != _hover) { _hover = h; QueueRedraw(); }
                break;
            case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } mb:
                var r = RowAt(mb.Position.Y);
                if (r >= 0) { _ctx.KeyboardFocus = false; _focus = r; Toggle(r); }
                AcceptEvent();
                break;
            case InputEventKey { Pressed: true } k:
                if (e.IsActionPressed("ui_down", allowEcho: true)) { _ctx.KeyboardFocus = true; _focus = Math.Min(Layers.Length - 1, _focus + 1); QueueRedraw(); AcceptEvent(); }
                else if (e.IsActionPressed("ui_up", allowEcho: true)) { _ctx.KeyboardFocus = true; _focus = Math.Max(0, _focus - 1); QueueRedraw(); AcceptEvent(); }
                else if (e.IsActionPressed("ui_accept") || k.PhysicalKeycode == Key.Space) { _ctx.KeyboardFocus = true; Toggle(_focus); AcceptEvent(); }
                break;
        }
    }

    public override void _Draw()
    {
        var t = _ctx.ToneFor(this);
        var w = Size.X;
        var body = _ctx.Style.Fonts["body"];
        var data = _ctx.Style.Fonts["data"];
        var caps = _ctx.Style.Fonts["mono-caps"];
        var small = _ctx.Style.Fonts["small"];
        var stroke = _ctx.Style.Metrics.IconStroke;
        GroupTitle(Top + 20, "显示", "LAYERS", t, small, caps, w);
        for (var i = 0; i < Layers.Length; i++)
        {
            var top = Top + 32 + i * RowHeight;
            var on = (Mask & (1 << Layers[i].Bit)) != 0;
            if (i == _hover) DrawRect(new Rect2(0, top, w, RowHeight), t.RowHover);
            // 复选框：18 px，1 px 控件边；开时主色实底加墨色勾（UI-VISUAL §7.3）
            var box = new Rect2(Pad, top + (RowHeight - 18) * 0.5f, 18, 18);
            if (on)
            {
                DrawRect(box, t.PrimaryFill);
                MapDraw.Outline(this, box.Grow(-0.5f), 0, t.PrimaryLine);
                MapDraw.CheckGlyph(this, box.GetCenter(), t.PrimaryInk, 1.6f);
            }
            else MapDraw.Outline(this, box.Grow(-0.5f), 0, t.Control);
            MapDraw.Text(this, body, new(Pad + 30, top + 26), Layers[i].Zh, on ? t.Text1 : t.Text2);
            if (_counts[i] >= 0)
            {
                var c = _counts[i].ToString(System.Globalization.CultureInfo.InvariantCulture);
                MapDraw.Text(this, data, new(w - Pad - data.Width(c), top + 25), c, t.Text2);
            }
            if (HasFocus() && _ctx.KeyboardFocus && i == _focus) MapHudContext.FocusRing(this, new Rect2(4, top + 4, w - 8, RowHeight - 8), t);
        }
        // 图例：写明各类标记的图标和状态
        var y = Top + 32 + Layers.Length * RowHeight + 28;
        GroupTitle(y, "图例", "LEGEND", t, small, caps, w);
        y += 18;
        foreach (var (zh, state) in LegendStates)
        {
            var c = new Vector2(Pad + 12, y + 18);
            var off = state == 3;
            var ink = off ? t.TextOff : t.Graphic;
            DrawCircle(c, 12, state == 1 ? ink : t.Solid, true, -1, true);
            if (off) DashedRing(c, 12, t.TextOff);
            else DrawArc(c, 12, 0, Mathf.Tau, 32, t.PanelLine, 1f, true);
            _ctx.Icon(this, LegendAnchor, c, 16, state == 1 ? t.Solid : ink, stroke, false);
            if (state == 2) DrawArc(c, 17, 0, Mathf.Tau, 40, t.PrimaryFill, 1.5f, true);
            MapDraw.Text(this, small, new(Pad + 40, y + 23), zh, off ? t.TextOff : t.Text2);
            y += 40;
        }
    }

    private void DashedRing(Vector2 c, float r, Color col)
    {
        const int Dashes = 12;
        for (var i = 0; i < Dashes; i++)
        {
            var a = i * Mathf.Tau / Dashes;
            DrawArc(c, r, a, a + Mathf.Tau / Dashes * 0.55f, 4, col, 1f, true);
        }
    }

    private void GroupTitle(float baseline, string zh, string en, MapTone t, MapFont small, MapFont caps, float w)
    {
        MapDraw.Text(this, small, new(Pad, baseline), zh, t.Text2);
        var x = Pad + small.Width(zh) + 8;
        MapDraw.Text(this, caps, new(x, baseline - 1), en, t.TextOff);
        x += caps.Width(en) + 10;
        var ly = MathF.Round(baseline - 5) + 0.5f;
        DrawLine(new(MathF.Round(x), ly), new(w - Pad, ly), t.Hair, MapDraw.Hair);
    }
}
