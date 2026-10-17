using Godot;

namespace Campus.Hud.Map;

public enum AtlasTab { Layers, Index }

/// <summary>
/// 大地图（SYS-ATLAS、UI-VISUAL）：背后 scrim，标题行（图名中英、MAP 01/01、季节·钟点·天气、[收起 M]），
/// 左栏 320（图层｜目录两页）锚在 (32, 104)，地图纸 1512×900 锚在 (376, 104)。大页，随昼夜主题。
/// 控件在扉页之后预建、隐藏（SYS-ATLAS）；全部约 20 个 Control（≤ 120）。
/// </summary>
public partial class AtlasPage : Control
{
    private static readonly Vector2 LeftAnchor = new(32, 104);
    private static readonly Vector2 LeftSize = new(320, 900);
    private const float TabHeight = 38;

    private readonly MapHudContext _ctx;
    private string _title = "";
    private string _titleEn = "";
    private string _status = "";

    public AtlasPage(MapHudContext ctx)
    {
        _ctx = ctx;
        MouseFilter = MouseFilterEnum.Stop;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AccessibilityName = "大地图";
        LayersTab = new MapButton(MapButtonKind.Tab, ctx) { Position = LeftAnchor + new Vector2(16, 12), Size = new((LeftSize.X - 32) * 0.5f, TabHeight), Text = "图层" };
        IndexTab = new MapButton(MapButtonKind.Tab, ctx) { Position = LeftAnchor + new Vector2(16 + (LeftSize.X - 32) * 0.5f, 12), Size = new((LeftSize.X - 32) * 0.5f, TabHeight), Text = "目录", KeyHint = "G" };
        AddChild(LayersTab);
        AddChild(IndexTab);
        var body = new Rect2(LeftAnchor + new Vector2(0, 12 + TabHeight + 8), new Vector2(LeftSize.X, LeftSize.Y - 12 - TabHeight - 8 - 12));
        Layers = new LayerList(ctx) { Position = body.Position, Size = body.Size };
        Index = new IndexList(ctx) { Position = body.Position, Size = body.Size, Visible = false };
        AddChild(Layers);
        AddChild(Index);
        Paper = new AtlasPaper(ctx);
        AddChild(Paper);
        Close = new MapButton(MapButtonKind.Secondary, ctx)
        {
            Text = "收起",
            KeyHint = "M",
            Glyph = MapDraw.CloseGlyph,
            Size = new(112, 36),
            Position = new(AtlasPaper.Origin.X + AtlasPaper.PaperSize.X - 112, 30),
            AccessibilityName = "收起大地图",
        };
        AddChild(Close);
        LayersTab.Pressed += () => ShowTab(AtlasTab.Layers);
        IndexTab.Pressed += () => ShowTab(AtlasTab.Index);
        ShowTab(AtlasTab.Layers);
    }

    public MapButton LayersTab { get; }
    public MapButton IndexTab { get; }
    public LayerList Layers { get; }
    public IndexList Index { get; }
    public AtlasPaper Paper { get; }
    public MapButton Close { get; }
    public AtlasTab Tab { get; private set; }

    public void ShowTab(AtlasTab tab)
    {
        Tab = tab;
        LayersTab.Selected = tab == AtlasTab.Layers;
        IndexTab.Selected = tab == AtlasTab.Index;
        Layers.Visible = tab == AtlasTab.Layers;
        Index.Visible = tab == AtlasTab.Index;
        switch (tab)
        {
            case AtlasTab.Layers: if (IsVisibleInTree()) Layers.GrabFocus(); break;
            case AtlasTab.Index:
                Index.SelectFirst();
                if (IsVisibleInTree()) Index.GrabFocus();
                break;
            default: throw new ArgumentOutOfRangeException(nameof(tab));
        }
    }

    public void SetTitle(string zh, string en)
    {
        _title = zh;
        _titleEn = en;
        QueueRedraw();
    }

    /// <summary>标题行的季节·钟点·天气小签；变了才重画（不逐帧改字）。</summary>
    public void SetStatus(string status)
    {
        if (status == _status) return;
        _status = status;
        QueueRedraw();
    }

    public void RedrawAll()
    {
        QueueRedraw();
        foreach (var c in new CanvasItem[] { LayersTab, IndexTab, Layers, Index, Close, Paper.Overlay, Paper.Card, Paper.Card.Primary, Paper.Card.Fly, Paper.Card.Track, Paper.ZoomIn, Paper.ZoomOut, Paper.Locate, Paper.Fit, Paper.Base, Paper.Player })
            c.QueueRedraw();
        Paper.Markers.Invalidate();
    }

    public override void _Draw()
    {
        var t = _ctx.Page;
        var m = _ctx.Style.Metrics;
        DrawRect(new Rect2(Vector2.Zero, Size), t.Scrim);
        // 标题行：思源黑体 Light 30（+0.2 em）加一行 mono-caps（UI-VISUAL）
        var title = _ctx.Style.Fonts["map.title"];
        var caps = _ctx.Style.Fonts["mono-caps"];
        var small = _ctx.Style.Fonts["small"];
        var x = AtlasPaper.Origin.X;
        var baseline = 30 + title.Ascent;
        MapDraw.Text(this, title, new(x, baseline), _title, t.Text1);
        x += title.Width(_title) + 20;
        var meta = $"{_titleEn} · MAP 01/01";
        MapDraw.Text(this, caps, new(x, baseline - 2), meta, t.Text2);
        x += caps.Width(meta) + 20;
        if (_status.Length > 0)
        {
            var w = small.Width(_status) + 20;
            var tag = new Rect2(x, baseline - 19, w, 24);
            DrawRect(tag, t.Panel);
            MapDraw.Outline(this, tag.Grow(-0.5f), 0, t.Control);
            MapDraw.Text(this, small, new(x + 10, baseline - 2), _status, t.Text1);
        }
        // 左栏：面板，左上切角 12，顶边内高光；页签下一条发丝线
        var left = new Rect2(LeftAnchor, LeftSize);
        MapDraw.Panel(this, left, m.ChamferPanel, t.Panel, t.PanelLine, t.Highlight);
        MapDraw.Via(this, LeftAnchor + new Vector2(m.ChamferPanel + 9, 5), m.Via, t.Text2, false);
        var ly = MathF.Round(LeftAnchor.Y + 12 + TabHeight) + 0.5f;
        DrawLine(new(LeftAnchor.X + 16, ly), new(LeftAnchor.X + LeftSize.X - 16, ly), t.Hair, MapDraw.Hair);
    }
}
