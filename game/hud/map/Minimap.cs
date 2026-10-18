using Campus.Hud.Widgets;
using Godot;

namespace Campus.Hud.Map;

/// <summary>左上角常驻小地图；外框尺寸来自角区令牌，地图视口与底栏由容器分配。</summary>
public partial class Minimap : Control
{
    private readonly MapHudContext _ctx;
    private readonly Label _regionLabel;
    private readonly DrawHost _scaleHost;
    private string _region = "";
    private (float Meters, float Pixels) _scale;
    private bool _night;

    public Minimap(MapHudContext ctx)
    {
        _ctx = ctx;
        Theme = ctx.Ui.Theme.Theme;
        CustomMinimumSize = ctx.Ui.Tokens.V2("metrics", "size", "minimap");
        MouseFilter = MouseFilterEnum.Stop;
        ctx.Resident.Add(this);
        var ui = ctx.Ui;
        var panel = ui.Panel(this, new Campus.Hud.Style.UiFx(Fill: "surface.panel", Line: "line.panel", Chamfer: ui.Tokens.Chamfer("panel"), Pin: true));
        panel.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var margin = ui.Layout.Add(panel, new MarginContainer(), "UiMinimapMargin");
        var stack = ui.Layout.Add(margin, new VBoxContainer(), "UiMinimapBody");
        Viewport = new Control { ClipContents = true, MouseFilter = MouseFilterEnum.Ignore, SizeFlagsVertical = SizeFlags.ExpandFill, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        stack.AddChild(Viewport);
        Base = new MapBase(crosses: false);
        Viewport.AddChild(Base);
        Base.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        Markers = new MarkerLayer(ctx, atlas: false) { IconSize = 16 };
        Viewport.AddChild(Markers);
        Player = new PlayerMark(ctx);
        Viewport.AddChild(Player);
        Overlay = new DrawHost(h => DrawNorth(h, new Vector2(h.Size.X * 0.5f, 0)));
        Viewport.AddChild(Overlay);
        Overlay.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var buttons = ui.Layout.Corner(Viewport, LayoutPreset.TopRight);
        AtlasButton = ctx.IconButton(buttons, MapDraw.AtlasGlyph, "map.atlas.title", pin: true);
        IndexButton = ctx.IconButton(buttons, MapDraw.IndexGlyph, "map.index.title", pin: true);
        var bar = ui.Layout.Row(stack);
        bar.CustomMinimumSize = new Vector2(0, ui.Tokens.F("metrics", "map", "minimapBarHeight"));
        _regionLabel = ui.Text(bar, "", "small", pin: true, wrap: false);
        _regionLabel.ClipText = true;
        _regionLabel.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        _scaleHost = new DrawHost(DrawScale) { SizeFlagsVertical = SizeFlags.ExpandFill };
        bar.AddChild(_scaleHost);
        Viewport.Resized += () =>
        {
            Player.ConeLength = Viewport.Size.X * 0.15f;
            Markers.Invalidate();
            Overlay.QueueRedraw();
        };
        AccessibilityName = ctx.T("map.minimap.title");
    }

    public Control Viewport { get; }
    public MapBase Base { get; }
    public MarkerLayer Markers { get; }
    public PlayerMark Player { get; }
    public DrawHost Overlay { get; }
    public TechButton AtlasButton { get; }
    public TechButton IndexButton { get; }
    public float ViewportHalf => Viewport.Size.X * 0.5f;
    public event Action? Clicked;

    public void SetInterfaceState(bool on)
    {
        AtlasButton.Visible = on;
        IndexButton.Visible = on;
    }

    public void SetRegion(string text)
    {
        if (text == _region) return;
        _region = text;
        _regionLabel.Text = text;
        AccessibilityName = text.Length > 0 ? _ctx.T("map.minimap.region_accessible", ("name", text)) : _ctx.T("map.minimap.title");
    }

    public void SetScale(float ppm)
    {
        var s = MapDraw.NiceScale(ppm, Viewport.Size.X * 0.28f);
        if (s == _scale) return;
        _scale = s;
        var tick = _ctx.Style.Fonts["tick"];
        _scaleHost.CustomMinimumSize = new Vector2(_scale.Pixels + tick.Width(MapDraw.Meters(_scale.Meters)) + _ctx.Ui.Tokens.Space("control"), 0);
        _scaleHost.QueueRedraw();
    }

    public void SyncTheme()
    {
        var night = _ctx.ResidentNight;
        if (night == _night) return;
        _night = night;
        Base.SetPinNight(night);
        Markers.Invalidate();
        _scaleHost.QueueRedraw();
        Overlay.QueueRedraw();
        foreach (var button in new[] { AtlasButton, IndexButton })
            foreach (var child in button.GetChildren()) if (child is CanvasItem item) item.QueueRedraw();
    }

    public override void _GuiInput(InputEvent e)
    {
        if (e is not InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true }) return;
        Clicked?.Invoke();
        AcceptEvent();
    }

    private void DrawScale(DrawHost h)
    {
        var t = _ctx.ResidentTone;
        var f = _ctx.Style.Fonts["tick"];
        var text = MapDraw.Meters(_scale.Meters);
        var baseline = (h.Size.Y + f.Ascent) * 0.5f;
        MapDraw.ScaleBar(h, new Vector2(h.Size.X, baseline - 1).Round() + new Vector2(0.5f, 0.5f), MathF.Round(_scale.Pixels), t.Text2);
        MapDraw.Text(h, f, new(0, baseline), text, t.Text2);
    }

    /// <summary>地图画布顶边的北向签。</summary>
    public void DrawNorth(CanvasItem ci, Vector2 centerTop)
    {
        var t = _ctx.ResidentTone;
        var f = _ctx.Style.Fonts["tick"];
        var r = new Rect2(centerTop - new Vector2(9, 0), new Vector2(18, 15));
        ci.DrawRect(r, t.Panel);
        MapDraw.Outline(ci, r.Grow(-0.5f), 0, t.Hair);
        MapDraw.Text(ci, f, new(r.Position.X, r.Position.Y + 11.5f), "N", t.Text1, HorizontalAlignment.Center, r.Size.X);
    }
}
