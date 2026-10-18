using Campus.Contracts.Atlas;
using Campus.Hud.Widgets;
using Godot;

namespace Campus.Hud.Map;

/// <summary>地图图层开关与图例；容器决定排版，按钮焦点依次沿阅读顺序移动。</summary>
public partial class LayerList : Control
{
    public const int OutskirtsBit = 8;

    private static readonly (int Bit, string Key, MarkerKind? Kind)[] Layers =
    [
        (0, "map.layers.anchor", MarkerKind.Anchor),
        (1, "map.layers.viewpoint", MarkerKind.Viewpoint),
        (2, "map.layers.interior", MarkerKind.Interior),
        (3, "map.layers.route", MarkerKind.Route),
        (4, "map.layers.landmark", MarkerKind.Landmark),
        (5, "map.layers.facility", MarkerKind.Facility),
        (6, "map.layers.custom", MarkerKind.Custom),
        (OutskirtsBit, "map.layers.outskirts", null),
    ];

    private static readonly string[] LegendKeys =
    [
        "map.legend.travelable", "map.legend.here", "map.legend.tracking", "map.legend.locked",
    ];

    private readonly MapHudContext _ctx;
    private readonly TechButton[] _buttons = new TechButton[Layers.Length];
    private readonly int[] _counts = new int[Layers.Length];
    private readonly List<LegendGlyph> _glyphs = [];

    public LayerList(MapHudContext ctx)
    {
        _ctx = ctx;
        FocusMode = FocusModeEnum.None;
        MouseFilter = MouseFilterEnum.Ignore;
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        SizeFlagsVertical = SizeFlags.ExpandFill;
        AccessibilityName = ctx.T("map.layers.title");

        var scroll = ctx.Ui.Layout.Scroll(this);
        scroll.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var stack = ctx.Ui.Layout.Stack(scroll);
        ctx.Ui.Text(stack, ctx.T("map.layers.show"), "mono-caps", "text.2");
        for (var i = 0; i < Layers.Length; i++)
        {
            var row = i;
            var button = ctx.Ui.Layout.ListRow(stack, "", pressed: () => Toggle(row));
            button.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            button.ClipText = true;
            button.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
            button.GuiInput += ev => OnRowInput(button, ev, row);
            _buttons[i] = button;
        }

        ctx.Ui.Text(stack, ctx.T("map.legend.title"), "mono-caps", "text.2");
        for (var i = 0; i < LegendKeys.Length; i++)
        {
            var row = ctx.Ui.Layout.Row(stack);
            var icon = ctx.Ui.Layout.Add(row, new LegendGlyph(ctx, i));
            _glyphs.Add(icon);
            var edge = ctx.Ui.Layout.Metric("iconButton");
            icon.CustomMinimumSize = new Vector2(edge, edge);
            var key = LegendKeys[i];
            var label = ctx.Ui.Text(row, ctx.T(key), "small", key == "map.legend.locked" ? "text.off" : "text.2");
            label.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        }
        RefreshRows();
    }

    /// <summary>默认设施与地标隐藏，其余显示。</summary>
    public int Mask { get; private set; } = ~((1 << 4) | (1 << 5));
    public event Action<int>? MaskChanged;
    public void SetMask(int mask) { Mask = mask; RefreshRows(); }
    public bool OwnsFocus => _buttons.Any(button => button.HasFocus());

    public void FocusFirst() => _buttons[0].GrabFocus();

    public override void _Draw()
    {
        foreach (var glyph in _glyphs) glyph.QueueRedraw();
    }

    public void SetCounts(IReadOnlyList<MapEntry> entries)
    {
        for (var i = 0; i < Layers.Length; i++)
            _counts[i] = Layers[i].Kind is { } kind ? entries.Count(entry => entry.Marker.Kind == kind) : -1;
        RefreshRows();
    }

    private void Toggle(int row)
    {
        Mask ^= 1 << Layers[row].Bit;
        RefreshRows();
        MaskChanged?.Invoke(Mask);
    }

    private void RefreshRows()
    {
        for (var i = 0; i < Layers.Length; i++)
        {
            var on = (Mask & (1 << Layers[i].Bit)) != 0;
            var count = _counts[i] >= 0 ? $"  ·  {_counts[i]}" : "";
            _buttons[i].Text = $"{_ctx.T(Layers[i].Key)}{count}";
            _buttons[i].Selected = on;
            _buttons[i].AccessibilityName = _buttons[i].Text;
        }
    }

    private void OnRowInput(TechButton button, InputEvent ev, int row)
    {
        if (ev is not InputEventKey { Pressed: true }) return;
        if (ev.IsActionPressed("ui_down", allowEcho: true) && row + 1 < _buttons.Length)
            _buttons[row + 1].GrabFocus();
        else if (ev.IsActionPressed("ui_up", allowEcho: true) && row > 0)
            _buttons[row - 1].GrabFocus();
        else return;
        _ctx.KeyboardFocus = true;
        button.AcceptEvent();
    }

    /// <summary>图例图形只在自身的小画布内使用坐标。</summary>
    private sealed partial class LegendGlyph(MapHudContext ctx, int state) : Control
    {
        public override void _Draw()
        {
            var tone = ctx.ToneFor(this);
            var center = Size / 2;
            var radius = MathF.Min(Size.X, Size.Y) * 0.3f;
            var off = state == 3;
            var ink = off ? tone.TextOff : tone.Graphic;
            DrawCircle(center, radius, state == 1 ? ink : tone.Solid, true, -1, true);
            if (off) DrawDashedRing(center, radius, tone.TextOff);
            else DrawArc(center, radius, 0, Mathf.Tau, 32, tone.PanelLine, 1f, true);
            MapDraw.AnchorIcon(this, center, radius * 1.33f, state == 1 ? tone.Solid : ink,
                ctx.Style.Metrics.IconStroke, false, false);
            if (state == 2) DrawArc(center, radius * 1.42f, 0, Mathf.Tau, 40, tone.PrimaryFill, 1.5f, true);
        }

        private void DrawDashedRing(Vector2 center, float radius, Color color)
        {
            const int dashes = 12;
            for (var i = 0; i < dashes; i++)
            {
                var start = i * Mathf.Tau / dashes;
                DrawArc(center, radius, start, start + Mathf.Tau / dashes * 0.55f, 4, color, 1f, true);
            }
        }
    }
}
