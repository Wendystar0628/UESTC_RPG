using Campus.Contracts;
using Campus.Contracts.Atlas;
using Campus.Hud.Widgets;
using Godot;

namespace Campus.Hud.Map;

/// <summary>图集页控制（大地图）：打开关闭、地图纸每帧、标记卡、传送与追踪、页内按键。</summary>
internal sealed class AtlasController : IDisposable
{
    private const float HereCardMeters = 20;
    private const float HereIndexMeters = 40;
    private const float HereIndexDeg = 60;
    private const int AttributionLayer = 11;
    private readonly MapHud _host;
    private readonly MapHudServices _s;
    private AtlasPage? _page;
    private AtlasPage Page => _page ?? throw new InvalidOperationException("Atlas page is closed.");
    private readonly List<CanvasLayer> _covered = [];
    private string _title = "", _titleEn = "";
    private Rect2 _bounds;
    private int? _layerMask;
    private bool _open;
    private IDisposable? _lease;
    private ITransitHandle? _pending;
    private readonly DestinationPrefetch _prefetch;
    private string _overlayKey = "";
    private int _statusKey = -1;

    public AtlasController(MapHud host)
    {
        _host = host;
        _s = host.Services;
        _prefetch = new DestinationPrefetch(_s.Atlas.Prefetch);
    }

    private void CreatePage()
    {
        var page = new AtlasPage(_host.Ctx);
        _page = page;
        page.TabChanged += ReportPage;
        _host.Hud.PageHost.AddChild(page);
        _host.BindPage(page);
        page.SetTitle(_title, _titleEn);
        page.Paper.SetBounds(_bounds, _s.Atlas.ZoomMin, _s.Atlas.ZoomMax);
        page.Close.Pressed += Close;
        page.Layers.MaskChanged += mask =>
        {
            _layerMask = mask;
            page.Paper.Markers.LayerMask = mask;
            page.Paper.Markers.Invalidate();
        };
        if (_layerMask is { } mask) page.Layers.SetMask(mask);
        page.Paper.Markers.LayerMask = page.Layers.Mask;
        page.Index.SelectionChanged += e =>
        {
            page.Paper.PanTo(e.At);
            page.Paper.Card.Visible = false;
            _prefetch.HideCard();
            page.Paper.Markers.NameSelected = true;
            page.Paper.SelectMarker(e.Id);
        };
        page.Index.Activated += OpenCard;
        page.Index.Hovered += e =>
        {
            page.Paper.Markers.Hover = e?.Id;
            page.Paper.Markers.Invalidate();
            HoverPrefetch(e, DestinationPrefetch.IndexHoverSeconds);
        };
        page.Paper.Clicked += e => { if (e is null) CloseCard(); else OpenCard(e); };
        page.Paper.HoverChanged += e =>
        {
            page.Paper.Markers.Hover = e?.Id;
            page.Paper.Markers.Invalidate();
            HoverPrefetch(e, DestinationPrefetch.MarkerHoverSeconds);
        };
        page.Paper.Locate.Pressed += () => page.Paper.PanTo(PlayerAt());
        page.Paper.Fit.Pressed += FitAll;
        page.Paper.ConnectorFrom = () => page.Paper.Card.Entry is { } e ? page.Paper.View.ToLocal(e.At, page.Paper.CanvasSize) : null;
        page.Paper.Card.Primary.Pressed += () => TravelSelected(page.Paper.Card.PrimaryStyle);
        page.Paper.Card.Fly.Pressed += () => TravelSelected(page.Paper.Card.PrimaryStyle == TravelStyle.Teleport ? TravelStyle.Fly : TravelStyle.Teleport);
        page.Paper.Card.Track.Pressed += TrackSelected;
        SetEntries(_host.Entries);
        _statusKey = -1;
        _overlayKey = "";
    }
    public bool IsOpen => _open;

    public void Dispose()
    {
        if (_open) Close();
        _prefetch.Dispose();
        CoverOthers(false);
        _lease?.Dispose();
        _lease = null;
    }

    public void SetTitle(string zh, string en) { _title = zh; _titleEn = en; _page?.SetTitle(zh, en); }
    public void SetBounds(Rect2 world) { _bounds = world; _page?.Paper.SetBounds(world, _s.Atlas.ZoomMin, _s.Atlas.ZoomMax); }
    public void RedrawAll() => _page?.RedrawAll();

    public void SetEntries(IReadOnlyList<MapEntry> entries)
    {
        if (_page is null) return;
        Page.Paper.Markers.Tracked = _s.Atlas.Tracked;
        Page.Paper.Markers.SetEntries(entries);
        Page.Index.SetEntries(entries);
        Page.Layers.SetCounts(entries);
        if (Page.Paper.Card.Entry is { } shown && entries.FirstOrDefault(e => e.Id == shown.Id) is { } fresh) OpenCard(fresh, keepFocus: true);
    }

    /// <summary>换图卸载：收起大页，不带出旧图的状态。</summary>
    public void Unload()
    {
        if (_open) Close();
    }

    /// <summary>每帧。离开 Playing 就收起；传送交接完成时还输入占用；打开期间推进地图纸。</summary>
    public void Frame(float dt, bool playing)
    {
        if (!playing && _open) Close();
        if (_pending is { } pending && pending.Done.IsCompleted) FinishTravel();
        if (_open) UpdatePage(dt);
    }

    /// <summary>
    /// 大地图打开时小地图和其余界面隐藏（SYS-ATLAS），署名任何时刻只出现一份：地图纸右下角那条（ODbL）。
    /// 角区由 HudRoot 的四层宿主仲裁；这里只收起渲染内核 AttributionLayer 的重复署名（CanvasLayer 11），关上时恢复。
    /// </summary>
    private void CoverOthers(bool cover)
    {
        if (!cover)
        {
            foreach (var layer in _covered)
                if (GodotObject.IsInstanceValid(layer)) layer.Visible = true;
            _covered.Clear();
            return;
        }
        foreach (var node in _host.GetParent()?.GetChildren() ?? [])
        {
            if (node is not CanvasLayer { Visible: true } layer || layer.Layer != AttributionLayer) continue;
            layer.Visible = false;
            _covered.Add(layer);
        }
    }

    // ------------------------------------------------------------------ 大地图

    /// <summary>光标由界面根按页宿主里真实可见的大页持租约（HudInput），这里只占观者输入。</summary>
    public void Open(AtlasTab tab)
    {
        if (_open || _host.Map is null || _pending is not null) return;
        // 页宿主看不见（--ui=0）时不开：看不见的大页不能占输入、上报界面面（UI-NAV）。
        if (!_s.Flow.IsPlaying || !_host.Hud.PageHost.IsVisibleInTree()) return;
        _open = true;
        _lease ??= _s.Rig.LockInput(InputLockReason.Atlas);
        CreatePage();
        CloseCard();
        Page.Paper.SelectMarker(null);
        switch (tab)
        {
            case AtlasTab.Layers: Page.Paper.SetView(PlayerAt(), 0.8f); break;
            case AtlasTab.Index: FitAll(); break;
            default: throw new ArgumentOutOfRangeException(nameof(tab));
        }
        Page.Visible = true;
        CoverOthers(true);
        Page.ShowTab(tab);
        Page.RedrawAll();
        FadeInPage();
        UpdatePage(0);
    }

    /// <summary>淡入期间页内不接输入也不取焦点，淡完再开并聚焦当前标签页；减少动态效果与 instant 档直接到位。</summary>
    private void FadeInPage()
    {
        var page = Page;
        var seconds = _host.Hud.Ui.Motion.Fade(_host.Style.Metrics.PageInMs / 1000f);
        void Ready()
        {
            page.Modulate = Colors.White;
            UiLayout.SetInteractive(page, true);
            page.ShowTab(page.Tab);
        }
        if (seconds <= 0) { Ready(); return; }
        page.Modulate = new Color(1, 1, 1, 0);
        UiLayout.SetInteractive(page, false);
        var tween = page.CreateTween();
        tween.TweenProperty(page, "modulate:a", 1f, seconds);
        tween.Finished += Ready;
    }

    private void Close()
    {
        if (!_open) return;
        _open = false;
        _s.Surfaces.Report(UiSurface.AtlasIndex, false);
        _s.Surfaces.Report(UiSurface.Atlas, false);
        Page.Visible = false;
        CoverOthers(false);
        CloseCard();
        _prefetch.Clear();
        Page.QueueFree();
        _page = null;
        if (_pending is null) { _lease?.Dispose(); _lease = null; }
    }

    private void ReportPage(AtlasTab tab)
    {
        var open = _page is { } page && page.IsVisibleInTree();
        _s.Surfaces.Report(UiSurface.Atlas, open);
        _s.Surfaces.Report(UiSurface.AtlasIndex, open && (tab switch
        {
            AtlasTab.Layers => false,
            AtlasTab.Index => true,
            _ => throw new ArgumentOutOfRangeException(nameof(tab))
        }));
    }

    private void FitAll()
    {
        if (_host.Map is not { } map) return;
        var b = map.Bounds;
        Page.Paper.FitTo(new Rect2(b.Min.X, b.Min.Z, b.Max.X - b.Min.X, b.Max.Z - b.Min.Z));
    }

    private Vector2 PlayerAt() => new(_s.Observer.Feet.X, _s.Observer.Feet.Z);

    private void UpdatePage(float dt)
    {
        var pan = Vector2.Zero;
        if (Input.IsActionPressed("move_left")) pan.X -= 1;
        if (Input.IsActionPressed("move_right")) pan.X += 1;
        if (Input.IsActionPressed("move_forward")) pan.Y -= 1;
        if (Input.IsActionPressed("move_backward")) pan.Y += 1;
        if (!Page.Index.OwnsFocus && !Page.Layers.OwnsFocus)
        {
            if (Input.IsActionPressed("ui_left")) pan.X -= 1;
            if (Input.IsActionPressed("ui_right")) pan.X += 1;
            if (Input.IsActionPressed("ui_up")) pan.Y -= 1;
            if (Input.IsActionPressed("ui_down")) pan.Y += 1;
        }
        var paper = Page.Paper;
        paper.Tick(dt, pan.LimitLength(1));
        _prefetch.Tick(dt);
        var view = paper.View;
        var pose = _s.Rig.Pose;
        paper.Player.Follow(view.ToLocal(PlayerAt(), Page.Paper.CanvasSize), _s.Observer.YawDeg, pose.YawDeg, _host.HorizontalFov(pose));
        UpdateHere(pose);
        // 标题行小签：季节、钟点（按世界分钟）、天气，变了才重排字
        var world = _s.World;
        var statusKey = ((int)world.SeasonTarget * 1440 + (int)(world.Clock * 60)) * 16 + (int)world.Weather.Kind;
        if (statusKey != _statusKey)
        {
            _statusKey = statusKey;
            Page.SetStatus($"{MapHudContext.SeasonName(world.SeasonTarget)} · {WorldTime.FormatClock(world.Clock, roundToMinute: true)} · {MapHudContext.WeatherName(world.Weather.Kind)}");
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
        if (_host.Geo is not { } geo) return "";
        if (paper.Cursor is { } cursor)
        {
            var w = paper.View.ToWorld(cursor, Page.Paper.CanvasSize);
            var d = w.DistanceTo(PlayerAt());
            return _host.Ctx.T("map.readout.distance", ("coordinates", geo.Format(w)),
                ("distance", MathF.Round(d).ToString("0", System.Globalization.CultureInfo.InvariantCulture)));
        }
        return _host.Ctx.T("map.readout.here", ("coordinates", geo.Format(PlayerAt())));
    }

    /// <summary>"此处"：镜头在 40 m 以内、朝向偏差在 60° 以内（UI-NAV）；标记层与目录各用一份。</summary>
    private void UpdateHere(Pose pose)
    {
        var cam = new Vector2(pose.Position.X, pose.Position.Z);
        var changed = false;
        foreach (var e in _host.Entries)
        {
            if (e.Marker.Teleport is not { } spec) continue;
            var fly = spec.Fly;
            var near = cam.DistanceTo(new Vector2(fly.Position.X, fly.Position.Z)) <= HereIndexMeters
                && MathF.Abs(Mathf.Wrap(pose.YawDeg - fly.YawDeg, -180f, 180f)) <= HereIndexDeg;
            changed |= near ? Page.Index.Here.Add(e.Id) : Page.Index.Here.Remove(e.Id);
            if (near) Page.Paper.Markers.Here.Add(e.Id); else Page.Paper.Markers.Here.Remove(e.Id);
        }
        if (changed) { Page.Index.Refresh(); Page.Paper.Markers.Invalidate(); }
    }

    private void OpenCard(MapEntry e) => OpenCard(e, keepFocus: false);

    private void OpenCard(MapEntry e, bool keepFocus)
    {
        var paper = Page.Paper;
        var p = paper.View.ToLocal(e.At, paper.CanvasSize);
        if (!new Rect2(Vector2.Zero, paper.CanvasSize).HasPoint(p))
        {
            paper.PanTo(e.At);
        }
        var here = PlayerAt().DistanceTo(e.At) <= HereCardMeters
            || (!_s.Atlas.CanTravel(e.Id, TravelStyle.Teleport, out var why) && why == TravelRefusal.Here);
        paper.Card.Show(e, Meta(e), here, _s.Atlas.Tracked == e.Id, TravelStyle.Teleport);
        _prefetch.ShowCard(e.Id, paper.Card.PrimaryStyle);
        paper.Markers.NameSelected = false;
        paper.SelectMarker(e.Id);
        Page.Index.Select(e.Id);
        if (!keepFocus && paper.Card.Primary.Visible && !paper.Card.Primary.Disabled) paper.Card.Primary.GrabFocus();
        else if (!keepFocus) paper.Card.Track.GrabFocus();
    }

    /// <summary>只有可传送的标记才计悬停；移到别处（含不可传送的标记）算移开。</summary>
    private void HoverPrefetch(MapEntry? e, float dwellSeconds) => _prefetch.Hover(e is { Travelable: true } ? e.Id : null, dwellSeconds);

    private void CloseCard()
    {
        if (!Page.Paper.Card.Visible) return;
        Page.Paper.Card.Visible = false;
        _prefetch.HideCard();
        Page.Paper.Markers.NameSelected = true;
        Page.Paper.SelectMarker(null);
        if (!Page.IsVisibleInTree()) return;
        if (Page.Tab == AtlasTab.Index) Page.Index.FocusFirst();
        else Page.Layers.FocusFirst();
    }

    /// <summary>等宽元信息："P05 · 西区 · 30.7516°N 103.9229°E"（SYS-ATLAS）。</summary>
    private string Meta(MapEntry e)
    {
        var area = e.Marker.Region is { } r && _s.Atlas.Regions.FirstOrDefault(region => region.Id == r) is { } reg ? _host.Ctx.Name(reg.Name)
            : e.Group is not { } g ? ""
            : g switch
            {
                WaypointGroup.Overview or WaypointGroup.Edge => MapHudContext.GroupName(g),
                WaypointGroup.Core or WaypointGroup.South or WaypointGroup.West or WaypointGroup.North or WaypointGroup.East
                    => _host.Ctx.T("map.group.region", ("name", MapHudContext.GroupName(g))),
                _ => throw new InvalidOperationException(g.ToString()),
            };
        var parts = new List<string> { e.Id.Value };
        if (area.Length > 0) parts.Add(area);
        if (_host.Geo is { } geo) parts.Add(geo.Format(e.At));
        return string.Join(" · ", parts);
    }

    // ------------------------------------------------------------------ 传送与追踪

    private void TravelSelected(TravelStyle style)
    {
        if (Page.Paper.Card.Entry is not { } e || _pending is not null) return;
        if (!_s.Atlas.CanTravel(e.Id, style, out _)) return;
        var handle = _s.Atlas.Travel(e.Id, style);
        if (handle is null) return;
        // 输入占用不随大地图收拢释放，保持到还给控制（SYS-ATLAS 第 1、6 步）
        _pending = handle;
        Close();
        if (handle.Done.IsCompleted) FinishTravel();
    }

    private void FinishTravel()
    {
        _pending = null;
        _lease?.Dispose();
        _lease = null;
    }

    private void TrackSelected()
    {
        if (Page.Paper.Card.Entry is not { } e) return;
        _s.Atlas.Track(_s.Atlas.Tracked == e.Id ? null : e.Id);
    }

    /// <summary>大地图打开期间只认 M、G、N、Esc 和本页内的键，其余单键快捷键一律不响应。</summary>
    public void HandleInput(InputEvent e)
    {
        if (e is not (InputEventKey or InputEventAction)) return;
        if (!e.IsPressed() || e.IsEcho()) { _host.Handled(); return; }
        var key = e is InputEventKey k ? k.PhysicalKeycode : Key.None;
        var card = Page.Paper.Card;
        if (e.IsActionPressed("map")) Close();
        else if (e.IsActionPressed("atlas_index")) Page.ShowTab(Page.Tab == AtlasTab.Layers ? AtlasTab.Index : AtlasTab.Layers);
        else if (e.IsActionPressed("minimap")) _host.MinimapOff = !_host.MinimapOff;
        else if (e.IsActionPressed("menu"))
        {
            if (card.Visible) CloseCard();
            else Close();
        }
        else if (key is Key.Home) Page.Paper.PanTo(PlayerAt());
        else if (e.IsActionPressed("waypoint_0")) FitAll();
        else if (key is Key.Equal or Key.KpAdd) Page.Paper.ZoomBy(1.25f, Page.Paper.CanvasSize * 0.5f);
        else if (key is Key.Minus or Key.KpSubtract) Page.Paper.ZoomBy(0.8f, Page.Paper.CanvasSize * 0.5f);
        else if (card.Visible && card.Fly.Visible && !card.Fly.Disabled && e.IsActionPressed("toggle_mode"))
            TravelSelected(card.PrimaryStyle == TravelStyle.Teleport ? TravelStyle.Fly : TravelStyle.Teleport);
        else if (card.Visible && e.IsActionPressed("time_panel")) TrackSelected();
        else if (card.Visible && card.Primary.Visible && !card.Primary.Disabled && (key is Key.Space || e.IsActionPressed("ui_accept")))
            TravelSelected(card.PrimaryStyle);
        _host.Handled();
    }
}
