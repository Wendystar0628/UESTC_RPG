using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Atlas;
using Campus.Contracts.Maps;
using Godot;

namespace Campus.Hud.Map;

/// <summary>图集页（大地图）部分：打开关闭、地图纸每帧、标记卡、传送与追踪、页内按键（SYS-ATLAS）。</summary>
public partial class MapHud
{
    private const float HereCardMeters = 20;
    private const float HereIndexMeters = 40;
    private const float HereIndexDeg = 60;
    private const float HoverPrefetchSeconds = 0.3f;
    private const float IndexPrefetchSeconds = 0.15f;
    private const string HudLayerName = "HudRoot";
    private const int AttributionLayer = 11;

    private readonly List<CanvasLayer> _covered = [];
    private bool _open;
    private bool _fromRoam;
    private IDisposable? _lease;
    private ITransitHandle? _pending;
    private bool _pendingFromRoam;
    private float _pageFade = 1;

    private MarkerId? _hover;
    private float _hoverAge;
    private bool _hoverPrefetched;
    private string _overlayKey = "";
    private int _statusKey = -1;

    private void WireAtlasPage()
    {
        _page.Close.Pressed += () => Close(restoreRoam: true);
        _page.Layers.MaskChanged += mask => { _page.Paper.Markers.LayerMask = mask; _page.Paper.Markers.Invalidate(); };
        _page.Paper.Markers.LayerMask = _page.Layers.Mask;
        _page.Index.SelectionChanged += e => { _page.Paper.PanTo(e.At); _page.Paper.Card.Visible = false; _page.Paper.Markers.NameSelected = true; _page.Paper.SelectMarker(e.Id); };
        _page.Index.Activated += OpenCard;
        _page.Index.Hovered += e => BeginHover(e.Id, IndexPrefetchSeconds);
        _page.Paper.Clicked += e => { if (e is null) CloseCard(); else OpenCard(e); };
        _page.Paper.HoverChanged += e => { _page.Paper.Markers.Hover = e?.Id; _page.Paper.Markers.Invalidate(); BeginHover(e?.Id, HoverPrefetchSeconds); };
        _page.Paper.Locate.Pressed += () => _page.Paper.PanTo(PlayerAt());
        _page.Paper.Fit.Pressed += FitAll;
        _page.Paper.ConnectorFrom = () => _page.Paper.Card.Entry is { } e ? _page.Paper.View.ToLocal(e.At, AtlasPaper.PaperSize) : null;
        _page.Paper.Card.Primary.Pressed += () => TravelSelected(_page.Paper.Card.PrimaryStyle);
        _page.Paper.Card.Fly.Pressed += () => TravelSelected(_page.Paper.Card.PrimaryStyle == TravelStyle.Teleport ? TravelStyle.Fly : TravelStyle.Teleport);
        _page.Paper.Card.Track.Pressed += TrackSelected;
    }

    /// <summary>
    /// 大地图打开时小地图和其余界面隐藏（SYS-ATLAS），署名任何时刻只出现一份：地图纸右下角那条（ODbL）。
    /// 所以打开期间收起 WP-35 的 HudRoot 层（四角件、小地图槽位）和 WP-01 的署名层（CanvasLayer 11，UI-NAV），
    /// 关上时恢复原来的显隐。contract-9 的界面打开状态（IUiSurfaceSink）落地后，改为报告状态、由各层自己隐藏。
    /// </summary>
    private void CoverOthers(bool cover)
    {
        if (!cover)
        {
            foreach (var layer in _covered)
                if (IsInstanceValid(layer)) layer.Visible = true;
            _covered.Clear();
            return;
        }
        foreach (var node in GetParent()?.GetChildren() ?? [])
        {
            if (node is not CanvasLayer { Visible: true } layer || layer == _layer) continue;
            if (layer.Name != HudLayerName && layer.Layer != AttributionLayer) continue;
            layer.Visible = false;
            _covered.Add(layer);
        }
    }

    // ------------------------------------------------------------------ 大地图

    private void Open(AtlasTab tab)
    {
        if (_open || _map is null || _style is null || _pending is not null) return;
        var playing = _flow.State == AppState.Playing;
        if (!playing) return;
        _open = true;
        _fromRoam = Input.MouseMode == Input.MouseModeEnum.Captured;
        Input.MouseMode = Input.MouseModeEnum.Visible;
        _lease ??= _rig.LockInput(InputLockReason.Atlas);
        CloseCard();
        _page.Paper.SelectMarker(null);
        switch (tab)
        {
            case AtlasTab.Layers: _page.Paper.SetView(PlayerAt(), 0.8f); break;
            case AtlasTab.Index: FitAll(); break;
            default: throw new ArgumentOutOfRangeException(nameof(tab));
        }
        _page.Visible = true;
        CoverOthers(true);
        _page.ShowTab(tab);
        _pageFade = 0;
        _page.Modulate = new Color(1, 1, 1, 0);
        _page.RedrawAll();
        UpdatePage(0);
    }

    private void Close(bool restoreRoam)
    {
        if (!_open) return;
        _open = false;
        _page.Visible = false;
        CoverOthers(false);
        CloseCard();
        _hover = null;
        if (_pending is null) { _lease?.Dispose(); _lease = null; }
        if (restoreRoam && _fromRoam && _pending is null) Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    private void FitAll()
    {
        if (_map is null) return;
        var b = _map.Bounds;
        _page.Paper.FitTo(new Rect2(b.Min.X, b.Min.Z, b.Max.X - b.Min.X, b.Max.Z - b.Min.Z));
    }

    private Vector2 PlayerAt() => new(_observer.Feet.X, _observer.Feet.Z);

    private void UpdatePage(float dt)
    {
        var fadeSeconds = _style!.Metrics.ReducedFadeMs / 1000f;
        if (_pageFade < 1)
        {
            _pageFade = _instant ? 1 : MathF.Min(1, _pageFade + dt / fadeSeconds);
            _page.Modulate = new Color(1, 1, 1, _pageFade);
        }
        var pan = Vector2.Zero;
        if (Input.IsActionPressed("move_left")) pan.X -= 1;
        if (Input.IsActionPressed("move_right")) pan.X += 1;
        if (Input.IsActionPressed("move_forward")) pan.Y -= 1;
        if (Input.IsActionPressed("move_backward")) pan.Y += 1;
        if (!_page.Index.HasFocus() && !_page.Layers.HasFocus())
        {
            if (Input.IsActionPressed("ui_left")) pan.X -= 1;
            if (Input.IsActionPressed("ui_right")) pan.X += 1;
            if (Input.IsActionPressed("ui_up")) pan.Y -= 1;
            if (Input.IsActionPressed("ui_down")) pan.Y += 1;
        }
        var paper = _page.Paper;
        paper.Tick(dt, pan.LimitLength(1), _motion.ReduceMotion || _instant);
        var view = paper.View;
        var pose = _rig.Pose;
        paper.Player.Follow(view.ToLocal(PlayerAt(), AtlasPaper.PaperSize), _observer.YawDeg, pose.YawDeg, HorizontalFov(pose));
        UpdateHere(pose);
        if (_hover is { } hover && !_hoverPrefetched)
        {
            _hoverAge += dt;
            if (_hoverAge >= HoverPrefetchSeconds) { _atlas.Prefetch(hover); _hoverPrefetched = true; }
        }
        // 标题行小签：季节、钟点（按世界分钟）、天气，变了才重排字
        var statusKey = ((int)_world.SeasonTarget * 1440 + (int)(_world.Clock * 60)) * 16 + (int)_world.Weather.Kind;
        if (statusKey != _statusKey)
        {
            _statusKey = statusKey;
            _page.SetStatus($"{MapHudContext.SeasonName(_world.SeasonTarget)} · {MapHudContext.Clock(_world.Clock)} · {MapHudContext.WeatherName(_world.Weather.Kind)}");
        }
        var readout = Readout(paper);
        var (scaleM, _) = MapDraw.NiceScale(view.Ppm, 160);
        var key = readout + "|" + scaleM.ToString(System.Globalization.CultureInfo.InvariantCulture) + "|" + paper.Card.Visible + "|" +
            (paper.Card.Visible && paper.ConnectorFrom?.Invoke() is { } c ? c.Round().ToString() : "");
        if (key != _overlayKey)
        {
            _overlayKey = key;
            paper.Readout = readout;
            paper.Overlay.QueueRedraw();
        }
    }

    /// <summary>光标读数：经纬度与"距此多少米"；光标不在地图纸上时写玩家所在处。</summary>
    private string Readout(AtlasPaper paper)
    {
        if (_geo is null) return "";
        if (paper.Cursor is { } cursor)
        {
            var w = paper.View.ToWorld(cursor, AtlasPaper.PaperSize);
            var d = w.DistanceTo(PlayerAt());
            return $"{_geo.Format(w)} · 距此 {MathF.Round(d):0} m";
        }
        return $"{_geo.Format(PlayerAt())} · 此处";
    }

    /// <summary>"此处"：镜头在 40 m 以内、朝向偏差在 60° 以内（UI-NAV）；标记层与目录各用一份。</summary>
    private void UpdateHere(Pose pose)
    {
        var cam = new Vector2(pose.Position.X, pose.Position.Z);
        var changed = false;
        foreach (var e in _entries)
        {
            if (e.Marker.Teleport is not { } spec) continue;
            var fly = spec.Fly;
            var near = cam.DistanceTo(new Vector2(fly.Position.X, fly.Position.Z)) <= HereIndexMeters
                && MathF.Abs(Mathf.Wrap(pose.YawDeg - fly.YawDeg, -180f, 180f)) <= HereIndexDeg;
            changed |= near ? _page.Index.Here.Add(e.Id) : _page.Index.Here.Remove(e.Id);
            if (near) _page.Paper.Markers.Here.Add(e.Id); else _page.Paper.Markers.Here.Remove(e.Id);
        }
        if (changed) { _page.Index.QueueRedraw(); _page.Paper.Markers.Invalidate(); }
    }

    private void BeginHover(MarkerId? id, float seconds)
    {
        if (id == _hover) return;
        _hover = id;
        _hoverPrefetched = false;
        _hoverAge = HoverPrefetchSeconds - seconds;
    }

    private void OpenCard(MapEntry e) => OpenCard(e, keepFocus: false);

    private void OpenCard(MapEntry e, bool keepFocus)
    {
        var paper = _page.Paper;
        var p = paper.View.ToLocal(e.At, AtlasPaper.PaperSize);
        if (p.X < 80 || p.X > AtlasPaper.PaperSize.X - 80 || p.Y < 80 || p.Y > AtlasPaper.PaperSize.Y - 80)
        {
            paper.PanTo(e.At);
            p = AtlasPaper.PaperSize * 0.5f;
        }
        const float Gap = 88;
        var right = p.X + Gap + TravelCard.Width < AtlasPaper.PaperSize.X - 72;
        var x = right ? p.X + Gap : p.X - Gap - TravelCard.Width;
        var y = Math.Clamp(p.Y - 150, 16, AtlasPaper.PaperSize.Y - TravelCard.Height - 64);
        paper.Card.Position = new Vector2(MathF.Round(x), MathF.Round(y));
        var here = PlayerAt().DistanceTo(e.At) <= HereCardMeters
            || (!_atlas.CanTravel(e.Id, TravelStyle.Teleport, out var why) && why == TravelRefusal.Here);
        paper.Card.Show(e, Meta(e), here, _atlas.Tracked == e.Id, TravelStyle.Teleport);
        paper.Markers.NameSelected = false;
        paper.SelectMarker(e.Id);
        _page.Index.Select(e.Id);
        _atlas.Prefetch(e.Id);
        if (!keepFocus && !paper.Card.Primary.Disabled) paper.Card.Primary.GrabFocus();
        else if (!keepFocus) paper.Card.Track.GrabFocus();
    }

    private void CloseCard()
    {
        if (!_page.Paper.Card.Visible) return;
        _page.Paper.Card.Visible = false;
        _page.Paper.Markers.NameSelected = true;
        _page.Paper.SelectMarker(null);
        if (!_page.IsVisibleInTree()) return;
        if (_page.Tab == AtlasTab.Index) _page.Index.GrabFocus();
        else _page.Layers.GrabFocus();
    }

    /// <summary>等宽元信息："P05 · 西区 · 30.7516°N 103.9229°E"（SYS-ATLAS）。</summary>
    private string Meta(MapEntry e)
    {
        var area = e.Marker.Region is { } r && _atlas.Regions.FirstOrDefault(region => region.Id == r) is { } reg ? reg.Name.Zh
            : e.Group is not { } g ? ""
            : g switch
            {
                WaypointGroup.Overview or WaypointGroup.Edge => MapHudContext.GroupName(g),
                WaypointGroup.Core or WaypointGroup.South or WaypointGroup.West or WaypointGroup.North or WaypointGroup.East
                    => MapHudContext.GroupName(g) + "区",
                _ => throw new InvalidOperationException(g.ToString()),
            };
        var parts = new List<string> { e.Id.Value };
        if (area.Length > 0) parts.Add(area);
        if (_geo is not null) parts.Add(_geo.Format(e.At));
        return string.Join(" · ", parts);
    }

    // ------------------------------------------------------------------ 传送与追踪

    private void TravelSelected(TravelStyle style)
    {
        if (_page.Paper.Card.Entry is not { } e || _pending is not null) return;
        if (!_atlas.CanTravel(e.Id, style, out _)) return;
        var handle = _atlas.Travel(e.Id, style);
        if (handle is null) return;
        // 输入占用不随大地图收拢释放，保持到还给控制（SYS-ATLAS 第 1、6 步）
        _pending = handle;
        _pendingFromRoam = _fromRoam;
        Close(restoreRoam: false);
        if (handle.Done.IsCompleted) FinishTravel();
    }

    private void FinishTravel()
    {
        _pending = null;
        _lease?.Dispose();
        _lease = null;
        if (_pendingFromRoam && !_open) Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    private void TrackSelected()
    {
        if (_page.Paper.Card.Entry is not { } e) return;
        _atlas.Track(_atlas.Tracked == e.Id ? null : e.Id);
    }

    /// <summary>大地图打开期间只认 M、G、N、Esc 和本页内的键，其余单键快捷键一律不响应。</summary>
    private void AtlasInput(InputEvent e)
    {
        if (e is not (InputEventKey or InputEventAction)) return;
        if (!e.IsPressed() || e.IsEcho()) { Handled(); return; }
        _ctx.KeyboardFocus = true;
        var key = e is InputEventKey k ? k.PhysicalKeycode : Key.None;
        var card = _page.Paper.Card;
        if (e.IsActionPressed("map")) Close(restoreRoam: true);
        else if (e.IsActionPressed("atlas_index")) _page.ShowTab(_page.Tab == AtlasTab.Layers ? AtlasTab.Index : AtlasTab.Layers);
        else if (e.IsActionPressed("minimap")) _minimapOff = !_minimapOff;
        else if (e.IsActionPressed("menu"))
        {
            if (card.Visible) CloseCard();
            else Close(restoreRoam: false);
        }
        else if (key is Key.Home) _page.Paper.PanTo(PlayerAt());
        else if (e.IsActionPressed("waypoint_0")) FitAll();
        else if (key is Key.Equal or Key.KpAdd) _page.Paper.ZoomBy(1.25f, AtlasPaper.PaperSize * 0.5f);
        else if (key is Key.Minus or Key.KpSubtract) _page.Paper.ZoomBy(0.8f, AtlasPaper.PaperSize * 0.5f);
        else if (card.Visible && e.IsActionPressed("toggle_mode")) card.Fly.Press();
        else if (card.Visible && e.IsActionPressed("time_panel")) card.Track.Press();
        else if (card.Visible && (key is Key.Space || e.IsActionPressed("ui_accept"))) card.Primary.Press();
        Handled();
    }
}
