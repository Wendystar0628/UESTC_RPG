using Campus.Contracts.Atlas;
using Godot;

namespace Campus.Hud.Map;

/// <summary>
/// 大地图的地图纸（SYS-ATLAS）：1512×900，锚在 (376, 104)，左上切角 14。
/// 0.4–3.0 px/m 连续缩放，滚轮一格 ×1.25、以光标为中心、220 ms 缓动；左键拖动、WASD 或方向键每秒 600 px 平移。
/// 拖动只改两个层的 Position；缩放档变化时重画（≤ 10 Hz），缩放动画中层按比例 Scale（SYS-ATLAS）。
/// </summary>
public partial class AtlasPaper : Control
{
    public static readonly Vector2 Origin = new(376, 104);
    public static readonly Vector2 PaperSize = new(1512, 900);
    public const float MinPpm = 0.4f;
    public const float MaxPpm = 3.0f;
    private const float WheelStep = 1.25f;
    private const float ZoomSeconds = 0.22f;
    private const float PanSpeed = 600f;

    private readonly MapHudContext _ctx;
    private MapView _view = new(Vector2.Zero, 0.8f, 0);
    private Rect2 _bounds;
    private float _zoomFrom, _zoomTo, _zoomT = 1;
    private Vector2 _zoomAnchorWorld, _zoomAnchorLocal;
    private bool _dragging;
    private Vector2 _dragLast;
    private float _dragTravel;
    private Vector2? _cursor;

    public AtlasPaper(MapHudContext ctx)
    {
        _ctx = ctx;
        Position = Origin;
        Size = PaperSize;
        ClipContents = true;
        MouseFilter = MouseFilterEnum.Stop;
        AccessibilityName = "地图页";
        Base = new MapBase(crosses: true, chamfer: ctx.Style.Metrics.ChamferPage) { Size = PaperSize };
        AddChild(Base);
        Markers = new MarkerLayer(ctx, atlas: true) { IconSize = 20 };
        AddChild(Markers);
        Player = new PlayerMark(ctx) { ConeLength = 46 };
        AddChild(Player);
        Overlay = new DrawHost(DrawOverlay) { Size = PaperSize };
        AddChild(Overlay);
        Card = new TravelCard(ctx) { Visible = false };
        AddChild(Card);
        float bx = PaperSize.X - 16 - 40;
        ZoomIn = Side(new(bx, 16), (c, p, k, w) => MapDraw.PlusGlyph(c, p, k, w, true), "放大");
        ZoomOut = Side(new(bx, 56), (c, p, k, w) => MapDraw.PlusGlyph(c, p, k, w, false), "缩小");
        Locate = Side(new(bx, 112), MapDraw.LocateGlyph, "回到玩家");
        Fit = Side(new(bx, 160), MapDraw.FitGlyph, "全图");
        ZoomIn.Pressed += () => ZoomBy(WheelStep, PaperSize * 0.5f);
        ZoomOut.Pressed += () => ZoomBy(1 / WheelStep, PaperSize * 0.5f);
    }

    public MapBase Base { get; }
    public MarkerLayer Markers { get; }
    public PlayerMark Player { get; }
    public DrawHost Overlay { get; }
    public TravelCard Card { get; }
    public MapButton ZoomIn { get; }
    public MapButton ZoomOut { get; }
    public MapButton Locate { get; }
    public MapButton Fit { get; }
    public MapView View => _view;
    /// <summary>光标读数：(经纬度, 距此多少米)；由 AtlasPage 每帧给出。</summary>
    public string Readout { get; set; } = "";
    public Vector2? Cursor => _cursor;
    public event Action<MapEntry?>? Clicked;
    public event Action<MapEntry?>? HoverChanged;
    public Func<Vector2?>? ConnectorFrom { get; set; }

    private MapButton Side(Vector2 at, Action<CanvasItem, Vector2, Color, float> glyph, string name)
    {
        var b = new MapButton(MapButtonKind.Icon, _ctx) { Position = at, Size = new(40, 40), Glyph = glyph, AccessibilityName = name };
        AddChild(b);
        return b;
    }

    /// <summary>地图页范围：拖动和缩放都不让地图纸移出这个矩形（atlasRect）。</summary>
    public void SetBounds(Rect2 world) => _bounds = world;

    public void SetView(Vector2 center, float ppm)
    {
        _zoomT = 1;
        _view = Clamp(new MapView(center, Math.Clamp(ppm, MinPpm, MaxPpm), 0));
    }

    /// <summary>让一块地面范围适配地图页（"全图"），四周留 48 px。</summary>
    public void FitTo(Rect2 world)
    {
        var ppm = MathF.Min((PaperSize.X - 96) / world.Size.X, (PaperSize.Y - 96) / world.Size.Y);
        SetView(world.GetCenter(), ppm);
    }

    public void PanTo(Vector2 world) => _view = Clamp(_view with { Center = world });

    public void ZoomBy(float factor, Vector2 local)
    {
        var target = Math.Clamp((_zoomT < 1 ? _zoomTo : _view.Ppm) * factor, MinPpm, MaxPpm);
        _zoomFrom = _view.Ppm;
        _zoomTo = target;
        _zoomT = 0;
        _zoomAnchorLocal = local;
        _zoomAnchorWorld = _view.ToWorld(local, PaperSize);
    }

    private MapView Clamp(MapView v)
    {
        var half = PaperSize * 0.5f / v.Ppm;
        var min = _bounds.Position + half;
        var max = _bounds.End - half;
        var c = new Vector2(min.X > max.X ? _bounds.GetCenter().X : Math.Clamp(v.Center.X, min.X, max.X),
            min.Y > max.Y ? _bounds.GetCenter().Y : Math.Clamp(v.Center.Y, min.Y, max.Y));
        return v with { Center = c };
    }

    /// <summary>每帧：缩放缓动、键盘平移，然后把视图交给底图、标记层与玩家。</summary>
    public void Tick(float dt, Vector2 keyPan, bool reduceMotion)
    {
        if (_zoomT < 1)
        {
            _zoomT = reduceMotion ? 1 : MathF.Min(1, _zoomT + dt / ZoomSeconds);
            var e = Ease(_zoomT);
            var ppm = MathF.Exp(Mathf.Lerp(MathF.Log(_zoomFrom), MathF.Log(_zoomTo), e));
            var center = _zoomAnchorWorld - (_zoomAnchorLocal - PaperSize * 0.5f) / ppm;
            _view = Clamp(new MapView(center, ppm, 0));
        }
        if (keyPan != Vector2.Zero) _view = Clamp(_view with { Center = _view.Center + keyPan * PanSpeed * dt / _view.Ppm });
        Base.SetView(_view);
        Markers.Follow(_view, PaperSize);
    }

    /// <summary>贝塞尔 (.2, .7, .2, 1)：不回弹的缓出（UI-VISUAL）。</summary>
    private static float Ease(float x)
    {
        float lo = 0, hi = 1;
        for (var i = 0; i < 16; i++)
        {
            var mid = (lo + hi) * 0.5f;
            if (Bezier(mid, 0.2f, 0.2f) < x) lo = mid; else hi = mid;
        }
        return Bezier((lo + hi) * 0.5f, 0.7f, 1f);
    }

    private static float Bezier(float t, float p1, float p2) => 3 * (1 - t) * (1 - t) * t * p1 + 3 * (1 - t) * t * t * p2 + t * t * t;

    public override void _GuiInput(InputEvent e)
    {
        switch (e)
        {
            case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.WheelUp or MouseButton.WheelDown } wheel:
                ZoomBy(wheel.ButtonIndex == MouseButton.WheelUp ? WheelStep : 1 / WheelStep, wheel.Position);
                AcceptEvent();
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left } mb:
                if (mb.Pressed) { _dragging = true; _dragLast = mb.Position; _dragTravel = 0; }
                else if (_dragging)
                {
                    _dragging = false;
                    if (_dragTravel < 4) Clicked?.Invoke(Markers.Pick(mb.Position, _view));
                }
                _ctx.KeyboardFocus = false;
                AcceptEvent();
                break;
            case InputEventMouseMotion mm:
                _cursor = mm.Position;
                if (_dragging)
                {
                    var d = mm.Position - _dragLast;
                    _dragLast = mm.Position;
                    _dragTravel += d.Length();
                    _zoomT = 1;
                    _view = Clamp(_view with { Center = _view.Center - d / _view.Ppm });
                }
                else HoverChanged?.Invoke(Markers.Pick(mm.Position, _view));
                AcceptEvent();
                break;
        }
    }

    public override void _Notification(int what)
    {
        if (what == NotificationMouseExit) { _cursor = null; HoverChanged?.Invoke(null); }
    }

    private void DrawOverlay(DrawHost h)
    {
        var t = _ctx.ToneFor(this);
        var m = _ctx.Style.Metrics;
        var data = _ctx.Style.Fonts["data"];
        var small = _ctx.Style.Fonts["small"];
        var keycap = _ctx.Style.Fonts["keycap"];
        // 地图纸外缘：1 物理像素发丝线，左上切角
        MapDraw.Outline(h, new Rect2(Vector2.Zero, PaperSize).Grow(-0.5f), m.ChamferPage - 0.2f, t.PanelLine);
        // 走线：从选中的标记连到传送卡（只表示连接）
        if (Card.Visible && ConnectorFrom?.Invoke() is { } from)
        {
            var cardLeft = Card.Position.X > from.X;
            var to = new Vector2(cardLeft ? Card.Position.X : Card.Position.X + TravelCard.Width, Math.Clamp(from.Y, Card.Position.Y + 28, Card.Position.Y + TravelCard.Height - 28));
            var start = from + (to - from).Normalized() * 14;
            MapDraw.Trace(h, start, to, t.Trace, 1.2f);
        }
        // 光标读数（左上，等宽）
        if (Readout.Length > 0)
        {
            var w = data.Width(Readout) + 24;
            var r = new Rect2(24, 20, w, 28);
            h.DrawRect(r, t.Solid with { A = 0.92f });
            MapDraw.Outline(h, r.Grow(-0.5f), 0, t.PanelLine);
            MapDraw.Text(h, data, new(r.Position.X + 12, r.Position.Y + 19), Readout, t.Text1);
        }
        // 底部：键帽提示条（左）、比例尺与 OSM 署名（右；署名必须在地图页上，ODbL）
        var y = PaperSize.Y - 20 - 24;
        var x = 24f;
        foreach (var (key, label) in (ReadOnlySpan<(string, string)>)[("滚轮", "缩放"), ("拖动", "平移"), ("G", "目录"), ("Esc", "返回")])
        {
            x += MapDraw.Keycap(h, keycap, new(x, y), key, t) + 8;
            MapDraw.Text(h, small, new(x, y + 17), label, t.Text2);
            x += small.Width(label) + 20;
        }
        var attribution = "© OpenStreetMap contributors";
        var aw = small.Width(attribution);
        var right = PaperSize.X - 24;
        MapDraw.Text(h, small, new(right - aw, y + 17), attribution, t.Text2);
        var (meters, px) = MapDraw.NiceScale(_view.Ppm, 160);
        var label2 = MapDraw.Meters(meters);
        var scaleRight = right - aw - 28;
        MapDraw.ScaleBar(h, new Vector2(MathF.Round(scaleRight), MathF.Round(y + 14)) + new Vector2(0.5f, 0.5f), MathF.Round(px), t.Text1);
        MapDraw.Text(h, data, new(scaleRight - px - 10 - data.Width(label2), y + 17), label2, t.Text2);
    }

    public void SelectMarker(MarkerId? id)
    {
        Markers.Selected = id;
        Markers.Invalidate();
    }
}
