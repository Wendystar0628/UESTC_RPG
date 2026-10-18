using Campus.Hud.Widgets;
using Godot;

namespace Campus.Hud.Map;

public enum AtlasTab { Layers, Index }

/// <summary>大地图：标题与侧栏由容器排版，地图纸占满剩余区域；只在打开时持有界面件。</summary>
public partial class AtlasPage : Control
{
    private readonly MapHudContext _ctx;
    private readonly Label _title, _subtitle, _status;

    public AtlasPage(MapHudContext ctx)
    {
        _ctx = ctx;
        Theme = ctx.Ui.Theme.Theme;
        MouseFilter = MouseFilterEnum.Stop;
        AccessibilityName = ctx.T("map.atlas.title");
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var ui = ctx.Ui;
        var stack = ui.Layout.Stack(this);
        stack.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var header = ui.Layout.Row(stack);
        var names = ui.Layout.Stack(header, groups: false);
        _title = ui.Text(names, "", "map.title");
        _subtitle = ui.Text(names, "", "mono-caps", "text.2");
        _status = ui.Text(header, "", "small", "text.2");
        _status.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        Close = ui.Button(header, ButtonStyle.Secondary, ctx.T("map.atlas.close"), icon: "close");
        Close.AccessibilityName = ctx.T("map.atlas.close_accessible");
        var body = ui.Layout.Row(stack);
        body.SizeFlagsVertical = SizeFlags.ExpandFill;
        var sidebar = ui.Layout.Add(body, new Control { CustomMinimumSize = new Vector2(ui.Layout.Metric("sidebarWidth"), 0), SizeFlagsVertical = SizeFlags.ExpandFill });
        var frame = ui.Layout.Sidebar(sidebar, ctx.T("map.atlas.title"));
        frame.Header.Visible = false;
        frame.Footer.Visible = false;
        frame.Scroll.VerticalScrollMode = ScrollContainer.ScrollMode.Disabled;
        frame.Body.SizeFlagsVertical = SizeFlags.ExpandFill;
        LayersTab = ui.Button(frame.Tabs, ButtonStyle.Tab, ctx.T("map.tab.layers"));
        IndexTab = ui.Button(frame.Tabs, ButtonStyle.Tab, ctx.T("map.tab.index"));
        Layers = new LayerList(ctx);
        Index = new IndexList(ctx) { Visible = false };
        frame.Body.AddChild(Layers);
        frame.Body.AddChild(Index);
        Paper = new AtlasPaper(ctx) { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill };
        body.AddChild(Paper);
        LayersTab.Pressed += () => ShowTab(AtlasTab.Layers);
        IndexTab.Pressed += () => ShowTab(AtlasTab.Index);
        ShowTab(AtlasTab.Layers);
    }

    public TechButton LayersTab { get; }
    public TechButton IndexTab { get; }
    public LayerList Layers { get; }
    public IndexList Index { get; }
    public AtlasPaper Paper { get; }
    public TechButton Close { get; }
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
            case AtlasTab.Layers: if (IsVisibleInTree()) Layers.FocusFirst(); break;
            case AtlasTab.Index:
                Index.SelectFirst();
                if (IsVisibleInTree()) Index.FocusFirst();
                break;
            default: throw new ArgumentOutOfRangeException(nameof(tab));
        }
    }

    public void SetTitle(string zh, string en)
    {
        _title.Text = TranslationServer.GetLocale().StartsWith("zh", StringComparison.OrdinalIgnoreCase) || en.Length == 0 ? zh : en;
        _subtitle.Text = _ctx.T("map.atlas.meta", ("name", en));
    }

    public void SetStatus(string status) => _status.Text = status;

    public void RedrawAll()
    {
        QueueRedraw();
        foreach (var c in new CanvasItem[] { Layers, Index, Paper.Overlay, Paper.Card, Paper.Base, Paper.Player }) c.QueueRedraw();
        Paper.Markers.Invalidate();
    }

    public override void _Draw() => DrawRect(new Rect2(Vector2.Zero, Size), _ctx.Page.Scrim);
}
