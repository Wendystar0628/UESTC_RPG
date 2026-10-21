using Campus.Contracts.Atlas;
using Campus.Hud.Motion;
using Campus.Hud.Widgets;
using Godot;

namespace Campus.Hud.Map;

/// <summary>
/// 大地图的地图纸（SYS-ATLAS）：由父容器分配可用空间，左上切角。
/// 缩放范围取标记配置；滚轮以光标为中心连续缩放，左键拖动，WASD 或方向键平移。
/// 拖动只改两个层的 Position；缩放档变化时重画（≤ 10 Hz），缩放动画中层按比例 Scale（SYS-ATLAS）。
/// </summary>
public partial class AtlasPaper : Control
{
    public Vector2 CanvasSize => Size;
    public float MinPpm { get; private set; }
    public float MaxPpm { get; private set; }
    private const float WheelStep = 1.25f;
    private const float ZoomSeconds = 0.22f;
    private const float PanSpeed = 600f;

    private readonly MapHudContext _ctx;
    private MapView _view = new(Vector2.Zero, 0.8f, 0);
    private Rect2 _bounds;
    private float _zoomFrom, _zoomTo;
    private Tween? _zoomTween;
    private Vector2 _zoomAnchorWorld, _zoomAnchorLocal;
    private bool _dragging;
    private Vector2 _dragLast;
    private float _dragTravel;
    private Vector2? _cursor;
    private Rect2? _fitPending;
    private readonly Label _readout;
    private readonly DrawHost _scale;

    public AtlasPaper(MapHudContext ctx)
    {
        _ctx = ctx;
        ClipContents = true;
        MouseFilter = MouseFilterEnum.Stop;
        AccessibilityName = ctx.T("map.paper.title");
        Base = new MapBase(crosses: true, chamfer: ctx.Style.Metrics.ChamferPage);
        AddChild(Base);
        Base.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        Markers = new MarkerLayer(ctx, atlas: true) { IconSize = 20 };
        AddChild(Markers);
        Player = new PlayerMark(ctx) { ConeLength = 46 };
        AddChild(Player);
        Overlay = new DrawHost(DrawOverlay);
        AddChild(Overlay);
        Overlay.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var ui = ctx.Ui;
        var margin = ui.Layout.Margin(this);
        margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var stack = ui.Layout.Stack(margin, groups: false);
        _readout = ui.Text(stack, "", "data", "text.1");
        var tools = ui.Layout.Flow(stack);
        ZoomIn = ctx.IconButton(tools, (c, p, k, w) => MapDraw.PlusGlyph(c, p, k, w, true), "map.paper.zoom_in");
        ZoomOut = ctx.IconButton(tools, (c, p, k, w) => MapDraw.PlusGlyph(c, p, k, w, false), "map.paper.zoom_out");
        Locate = ctx.IconButton(tools, MapDraw.LocateGlyph, "map.paper.locate");
        Fit = ctx.IconButton(tools, MapDraw.FitGlyph, "map.paper.fit");
        ui.Layout.Add(stack, new Control { SizeFlagsVertical = SizeFlags.ExpandFill });
        var footer = ui.Layout.Flow(stack);
        ui.Text(footer, ctx.T("map.paper.hints"), "small", "text.2");
        _scale = new DrawHost(DrawScale) { CustomMinimumSize = new Vector2(ui.Layout.Metric("mapScaleWidth"), ui.Layout.Metric("tag")) };
        footer.AddChild(_scale);
        ui.Text(footer, "© OpenStreetMap contributors", "small", "text.2");
        Card = new TravelCard(ctx) { Visible = false };
        AddChild(Card);
        ZoomIn.Pressed += () => ZoomBy(WheelStep, Size * 0.5f);
        ZoomOut.Pressed += () => ZoomBy(1 / WheelStep, Size * 0.5f);
        Resized += ResizeCanvas;
    }

    public MapBase Base { get; }
    public MarkerLayer Markers { get; }
    public PlayerMark Player { get; }
    public DrawHost Overlay { get; }
    public TravelCard Card { get; }
    public TechButton ZoomIn { get; }
    public TechButton ZoomOut { get; }
    public TechButton Locate { get; }
    public TechButton Fit { get; }
    public MapView View => _view;
    /// <summary>光标读数：(经纬度, 距此多少米)；由 AtlasPage 每帧给出。</summary>
    public string Readout { get => _readout.Text; set { _readout.Text = value; _scale.QueueRedraw(); } }
    public Vector2? Cursor => _cursor;
    public event Action<MapEntry?>? Clicked;
    public event Action<MapEntry?>? HoverChanged;
    public Func<Vector2?>? ConnectorFrom { get; set; }

    private void ResizeCanvas()
    {
        if (_fitPending is { } fit) { _fitPending = null; FitTo(fit); }
        else _view = Clamp(_view);
        Markers.Invalidate();
        Overlay.QueueRedraw();
    }

    /// <summary>地图页范围：拖动和缩放都不让地图纸移出这个矩形（atlasRect）。</summary>
    public void SetBounds(Rect2 world, float zoomMin, float zoomMax)
    {
        _bounds = world;
        MinPpm = zoomMin;
        MaxPpm = zoomMax;
    }

    public void SetView(Vector2 center, float ppm)
    {
        StopZoom();
        _view = Clamp(new MapView(center, Math.Clamp(ppm, MinPpm, MaxPpm), 0));
    }

    /// <summary>让一块地面范围适配地图页（"全图"），留白由页内边距令牌决定。</summary>
    public void FitTo(Rect2 world)
    {
        if (Size.X <= 0 || Size.Y <= 0) { _fitPending = world; return; }
        var margin = 4 * _ctx.Ui.Tokens.Space("pagePad");
        var ppm = MathF.Min(Math.Max(1, Size.X - margin) / world.Size.X, Math.Max(1, Size.Y - margin) / world.Size.Y);
        SetView(world.GetCenter(), ppm);
    }

    public void PanTo(Vector2 world) => _view = Clamp(_view with { Center = world });

    public void ZoomBy(float factor, Vector2 local)
    {
        var target = Math.Clamp((_zoomTween is { } running && running.IsRunning() ? _zoomTo : _view.Ppm) * factor, MinPpm, MaxPpm);
        StopZoom();
        _zoomFrom = _view.Ppm;
        _zoomTo = target;
        _zoomAnchorLocal = local;
        _zoomAnchorWorld = _view.ToWorld(local, Size);
        // 位移类动效只在完整档出现；其余档直接到位（UI-VISUAL）。曲线取 Enter：不回弹的缓出。
        if (!_ctx.Ui.Motion.Spatial) { ApplyZoom(1); return; }
        var c = _ctx.Ui.Tokens.Curve("Enter");
        _zoomTween = CreateTween();
        _zoomTween.TweenMethod(Callable.From<float>(t => ApplyZoom(UiEase.Bezier(t, c.X, c.Y, c.Z, c.W))), 0f, 1f, ZoomSeconds);
    }

    private void StopZoom()
    {
        _zoomTween?.Kill();
        _zoomTween = null;
    }

    /// <summary>缩放缓动的一帧：以锚点为不动点，按缓动进度 e 在对数缩放比上插值。</summary>
    private void ApplyZoom(float e)
    {
        var ppm = MathF.Exp(Mathf.Lerp(MathF.Log(_zoomFrom), MathF.Log(_zoomTo), e));
        var center = _zoomAnchorWorld - (_zoomAnchorLocal - Size * 0.5f) / ppm;
        _view = Clamp(new MapView(center, ppm, 0));
    }

    private MapView Clamp(MapView v)
    {
        var half = Size * 0.5f / v.Ppm;
        var min = _bounds.Position + half;
        var max = _bounds.End - half;
        var c = new Vector2(min.X > max.X ? _bounds.GetCenter().X : Math.Clamp(v.Center.X, min.X, max.X),
            min.Y > max.Y ? _bounds.GetCenter().Y : Math.Clamp(v.Center.Y, min.Y, max.Y));
        return v with { Center = c };
    }

    /// <summary>每帧：键盘平移，然后把视图交给底图、标记层与玩家。</summary>
    public void Tick(float dt, Vector2 keyPan)
    {
        if (keyPan != Vector2.Zero) _view = Clamp(_view with { Center = _view.Center + keyPan * PanSpeed * dt / _view.Ppm });
        Base.SetView(_view);
        Markers.Follow(_view, Size);
    }

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
                AcceptEvent();
                break;
            case InputEventMouseMotion mm:
                _cursor = mm.Position;
                if (_dragging)
                {
                    var d = mm.Position - _dragLast;
                    _dragLast = mm.Position;
                    _dragTravel += d.Length();
                    StopZoom();
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
        MapDraw.Outline(h, new Rect2(Vector2.Zero, Size).Grow(-0.5f), _ctx.Style.Metrics.ChamferPage - 0.2f, t.PanelLine);
        if (!Card.Visible || ConnectorFrom?.Invoke() is not { } from) return;
        var card = Card.CardRect;
        var to = new Vector2(card.Position.X > from.X ? card.Position.X : card.End.X,
            Math.Clamp(from.Y, card.Position.Y, card.End.Y));
        var start = from + (to - from).Normalized() * 14;
        MapDraw.Trace(h, start, to, t.Trace, 1.2f);
    }

    private void DrawScale(DrawHost h)
    {
        var t = _ctx.ToneFor(this);
        var data = _ctx.Style.Fonts["data"];
        var (meters, px) = MapDraw.NiceScale(_view.Ppm, h.Size.X * 0.6f);
        var label = MapDraw.Meters(meters);
        var baseline = h.Size.Y * 0.75f;
        MapDraw.ScaleBar(h, new Vector2(MathF.Round(h.Size.X), MathF.Round(baseline)) + new Vector2(0.5f, 0.5f), MathF.Round(px), t.Text1);
        MapDraw.Text(h, data, new(h.Size.X - px - data.Width(label) - 8, baseline), label, t.Text2);
    }
    public void SelectMarker(MarkerId? id)
    {
        Markers.Selected = id;
        Markers.Invalidate();
    }
}
