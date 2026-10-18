using Godot;

namespace Campus.Hud.Map;

/// <summary>
/// 左上角常驻的小地图（SYS-ATLAS、UI-VISUAL）：外框 244×272，锚在 (32, 32)；上方 226² 的地图视口，
/// 下方 28 px 底栏（左区域名、右比例尺）。外框只切左上 12 px，切角旁一枚过孔，发丝线边，视口顶边中央等宽的"N"。
/// 常驻件，默认固定深屏。界面态在视口右侧贴 [大地图 M]、[目录 G] 两个 36 px 图标钮。
/// 共 8 个 Control：外框、视口、底图、标记层、玩家、N 签、两个按钮（SYS-ATLAS：≤ 12）。
/// </summary>
public partial class Minimap : Control
{
    public static readonly Vector2 Origin = new(32, 32);
    public static readonly Vector2 FrameSize = new(244, 272);
    private const float Inset = 9;
    private const float ViewportSide = 226;
    private const float BarHeight = 28;

    private readonly MapHudContext _ctx;
    private string _region = "";
    private (float Meters, float Pixels) _scale;
    private bool _night;

    public Minimap(MapHudContext ctx)
    {
        _ctx = ctx;
        Position = Origin;
        Size = FrameSize;
        MouseFilter = MouseFilterEnum.Stop;
        ctx.Resident.Add(this);
        Viewport = new Control { Position = new(Inset, Inset), Size = new(ViewportSide, ViewportSide), ClipContents = true, MouseFilter = MouseFilterEnum.Ignore };
        AddChild(Viewport);
        Base = new MapBase(crosses: false) { Size = Viewport.Size };
        Viewport.AddChild(Base);
        Markers = new MarkerLayer(ctx, atlas: false) { IconSize = 16 };
        Viewport.AddChild(Markers);
        Player = new PlayerMark(ctx) { ConeLength = ViewportSide * 0.5f * 0.3f };
        Viewport.AddChild(Player);
        Overlay = new DrawHost(h => DrawNorth(h, new Vector2(ViewportSide * 0.5f, 0))) { Size = Viewport.Size };
        Viewport.AddChild(Overlay);
        AtlasButton = new MapButton(MapButtonKind.Icon, ctx) { Position = new(FrameSize.X + 8, Inset), Size = new(36, 36), Glyph = MapDraw.AtlasGlyph, KeyHint = "M" };
        IndexButton = new MapButton(MapButtonKind.Icon, ctx) { Position = new(FrameSize.X + 8, Inset + 44), Size = new(36, 36), Glyph = MapDraw.IndexGlyph, KeyHint = "G" };
        AtlasButton.AccessibilityName = "大地图";
        IndexButton.AccessibilityName = "目录";
        AddChild(AtlasButton);
        AddChild(IndexButton);
        AccessibilityName = "小地图";
    }

    public Control Viewport { get; }
    public MapBase Base { get; }
    public MarkerLayer Markers { get; }
    public PlayerMark Player { get; }
    public DrawHost Overlay { get; }
    public MapButton AtlasButton { get; }
    public MapButton IndexButton { get; }
    public float ViewportHalf => ViewportSide * 0.5f;
    public event Action? Clicked;

    /// <summary>界面态时显示两个图标钮；漫游态不显示（SYS-ATLAS）。</summary>
    public void SetInterfaceState(bool on)
    {
        AtlasButton.Visible = on;
        IndexButton.Visible = on;
    }

    /// <summary>底栏：区域名变了才改字（≤ 10 Hz，不逐帧改文字）。</summary>
    public void SetRegion(string text)
    {
        if (text == _region) return;
        _region = text;
        AccessibilityName = text.Length > 0 ? "小地图，" + text : "小地图";
        QueueRedraw();
    }

    public void SetScale(float ppm)
    {
        var s = MapDraw.NiceScale(ppm, 64);
        if (s == _scale) return;
        _scale = s;
        QueueRedraw();
    }

    public void SyncTheme()
    {
        var night = _ctx.ResidentNight;
        if (night == _night) return;
        _night = night;
        Base.SetPinNight(night);
        QueueRedraw();
        Markers.Invalidate();
        AtlasButton.QueueRedraw();
        IndexButton.QueueRedraw();
        Overlay.QueueRedraw();
    }

    public override void _GuiInput(InputEvent e)
    {
        if (e is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true })
        {
            Clicked?.Invoke();
            AcceptEvent();
        }
    }

    public override void _Draw()
    {
        var t = _ctx.ResidentTone;
        var m = _ctx.Style.Metrics;
        var frame = new Rect2(Vector2.Zero, FrameSize);
        MapDraw.Panel(this, frame, m.ChamferPanel, t.Panel, t.PanelLine, t.Highlight);
        // 切角旁的过孔签名
        MapDraw.Via(this, new Vector2(m.ChamferPanel + 9, 4.5f), m.Via, t.Text2, false);
        // 视口外缘一圈发丝线，地图与面板分得开
        MapDraw.Outline(this, new Rect2(Inset - 0.5f, Inset - 0.5f, ViewportSide + 1, ViewportSide + 1), 0, t.Hair);
        // 底栏
        var barTop = Inset + ViewportSide;
        var baseline = barTop + BarHeight * 0.5f + 5;
        var small = _ctx.Style.Fonts["small"];
        var tick = _ctx.Style.Fonts["tick"];
        var scaleText = MapDraw.Meters(_scale.Meters);
        var right = FrameSize.X - Inset - 1;
        var barRight = right;
        var labelW = tick.Width(scaleText);
        MapDraw.ScaleBar(this, new Vector2(barRight, baseline - 1).Round() + new Vector2(0.5f, 0.5f), MathF.Round(_scale.Pixels), t.Text2);
        MapDraw.Text(this, tick, new(barRight - _scale.Pixels - 8 - labelW, baseline), scaleText, t.Text2);
        var regionWidth = barRight - _scale.Pixels - 16 - labelW - Inset - 3;
        MapDraw.Text(this, small, new(Inset + 3, baseline), Fit(small, _region, regionWidth), t.Text1);
    }

    /// <summary>"N"标签：视口顶边中央的一枚小签，压在地图之上（北朝上时固定在顶边）。</summary>
    public void DrawNorth(CanvasItem ci, Vector2 centerTop)
    {
        var t = _ctx.ResidentTone;
        var f = _ctx.Style.Fonts["tick"];
        var r = new Rect2(centerTop - new Vector2(9, 0), new Vector2(18, 15));
        ci.DrawRect(r, t.Panel);
        MapDraw.Outline(ci, r.Grow(-0.5f), 0, t.Hair);
        MapDraw.Text(ci, f, new(r.Position.X, r.Position.Y + 11.5f), "N", t.Text1, HorizontalAlignment.Center, r.Size.X);
    }

    private static string Fit(MapFont f, string text, float width)
    {
        if (f.Width(text) <= width) return text;
        while (text.Length > 1 && f.Width(text + "…") > width) text = text[..^1];
        return text + "…";
    }
}
