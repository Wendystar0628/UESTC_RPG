using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Atlas;
using Campus.Contracts.Maps;
using KernelRegistry = Campus.Kernel.Registry.Kernel;
using Godot;

namespace Campus.Hud.Map;

/// <summary>
/// WP-48b 的入口：左上角常驻小地图与 M / G 打开的大地图（SYS-ATLAS §3、§4）。
/// 只依赖契约与 Kernel.Get：标记、区域与传送经 IAtlasService；玩家箭头与所在区域读 IObserverState；
/// 视锥读 ICameraRig.Pose；不直接调 ICameraRig.Transit（ENG-ARCH §2）。界面在 CanvasLayer 10（HudRoot 那一层）；
/// WP-35 的左上角槽位与全屏页槽位合入后改挂到槽位里。
/// </summary>
[KernelNode("hud-map", Owner = "WP-48b")]
public partial class MapHud : Node, IMapAware
{
    private const float RegionHz = 10;
    private const float RegionHold = 1f;
    private const float WalkHalfWidth = 120;
    private const float HereCardMeters = 20;
    private const float HereIndexMeters = 40;
    private const float HereIndexDeg = 60;
    private const float HoverPrefetchSeconds = 0.3f;
    private const float IndexPrefetchSeconds = 0.15f;

    private IAtlasService _atlas = null!;
    private IObserverState _observer = null!;
    private ICameraRig _rig = null!;
    private IWorldState _world = null!;
    private IAppFlow _flow = null!;
    private IVeilState _veil = null!;
    private IMotionSettings _motion = null!;
    private IMediumState _medium = null!;

    private MapStyle? _style;
    private MapHudContext _ctx = null!;
    private ShaderMaterial? _material;
    private const string HudLayerName = "HudRoot";
    private const int AttributionLayer = 11;
    private CanvasLayer? _layer;
    private readonly List<CanvasLayer> _covered = [];
    private Control _root = null!;
    private Minimap _minimap = null!;
    private AtlasPage _page = null!;

    private MapDescriptor? _map;
    private MapGeo? _geo;
    private readonly Dictionary<string, Waypoint> _waypoints = new(StringComparer.Ordinal);
    private IReadOnlyList<MapEntry> _entries = [];
    private Dictionary<string, AtlasRegion> _regions = new(StringComparer.Ordinal);
    private long _atlasRev = -1;

    private bool _uiEnabled = true;
    private string _themeArg = "auto";
    private bool _instant;
    private bool _night;
    private bool _pure;
    private bool _minimapOff;
    private bool _open;
    private bool _fromRoam;
    private IDisposable? _lease;
    private ITransitHandle? _pending;
    private bool _pendingFromRoam;
    private float _pageFade = 1;

    private Spring _halfWidth = new(WalkHalfWidth, 0.6f);
    private Vector2 _lastFeet;
    private long _instance = -1;
    private float _speed;
    private float _regionClock;
    private string? _regionShown;
    private string? _regionCandidate;
    private float _regionCandidateAge;

    private MarkerId? _hover;
    private float _hoverAge;
    private bool _hoverPrefetched;
    private string _overlayKey = "";
    private int _statusKey = -1;

    public override void _Ready()
    {
        ProcessPriority = FramePriority.Hud;
        _style = MapStyle.TryLoad();
        if (_style is null) { SetProcess(false); return; }
        _atlas = KernelRegistry.Get<IAtlasService>();
        _observer = KernelRegistry.Get<IObserverState>();
        _rig = KernelRegistry.Get<ICameraRig>();
        _world = KernelRegistry.Get<IWorldState>();
        _flow = KernelRegistry.Get<IAppFlow>();
        _veil = KernelRegistry.Get<IVeilState>();
        _motion = KernelRegistry.Get<IMotionSettings>();
        _medium = KernelRegistry.Get<IMediumState>();
        ReadArguments();

        _ctx = new MapHudContext(_style);
        _ctx.PinShapeLookup = id => _atlas.Pins.FirstOrDefault(p => "pin:" + p.Id.ToString(System.Globalization.CultureInfo.InvariantCulture) == id.Value)?.Shape ?? PinShape.Diamond;
        _night = InitialNight();
        _ctx.ThemeFront = _night ? 1 : 0;

        var layer = new CanvasLayer { Layer = 10 };
        _layer = layer;
        AddChild(layer);
        _root = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        layer.AddChild(_root);
        _root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _minimap = new Minimap(_ctx);
        _root.AddChild(_minimap);
        CallDeferred(MethodName.MountMinimap);
        // 大地图在扉页之后预建、隐藏：第一次按 M 不建控件（SYS-ATLAS §4.1）
        _page = new AtlasPage(_ctx) { Visible = false };
        _root.AddChild(_page);

        _material = MapMaterial.Acquire();
        if (_material is not null)
        {
            MapMaterial.Configure(_material, _style);
            MapMaterial.SetThemeFront(_material, _ctx.ThemeFront);
        }
        _minimap.Base.Bind(_material, _ctx.ResidentNight);
        _page.Paper.Base.Bind(_material, false);
        _minimap.SyncTheme();

        _minimap.Clicked += () => Open(AtlasTab.Layers);
        _minimap.AtlasButton.Pressed += () => Open(AtlasTab.Layers);
        _minimap.IndexButton.Pressed += () => Open(AtlasTab.Index);
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
        _atlas.Changed += RebuildEntries;
        RebuildEntries();
    }

    /// <summary>
    /// 小地图挂进 WP-35 的左上角槽位 "MinimapSlot"（槽位名由 WP-35 定，SYS-ATLAS §10）：U、--ui=0、过场、设置卡与存档页
    /// 打开时随槽位隐藏。没有 HudRoot 时（只有 D0 舞台）留在本节点自己的 CanvasLayer 10 上。
    /// 大地图页留在本节点自己的 CanvasLayer 10 上，打开期间由 <see cref="CoverOthers"/> 收起其余界面层。
    /// </summary>
    private void MountMinimap()
    {
        if (GetParent()?.FindChild("MinimapSlot", true, false) is not Control slot) return;
        _minimap.Reparent(slot, keepGlobalTransform: false);
        _minimap.Position = Vector2.Zero;
    }

    /// <summary>
    /// 大地图打开时小地图和其余界面隐藏（SYS-ATLAS §4.1），署名任何时刻只出现一份：地图纸右下角那条（ODbL）。
    /// 所以打开期间收起 WP-35 的 HudRoot 层（四角件、小地图槽位）和 WP-01 的署名层（CanvasLayer 11，UI-NAV §9），
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

    public override void _ExitTree()
    {
        if (_style is null) return;
        CoverOthers(false);
        _atlas.Changed -= RebuildEntries;
        _lease?.Dispose();
        _lease = null;
    }

    // ------------------------------------------------------------------ 地图数据

    public void OnMapUnloading(MapId from)
    {
        _map = null;
        _geo = null;
        _waypoints.Clear();
        if (_style is null) return;
        if (_open) Close(restoreRoam: false);
        _entries = [];
        _minimap.Markers.SetEntries(_entries);
    }

    public void OnMapLoaded(IMapContext ctx)
    {
        _map = ctx.Descriptor;
        _geo = new MapGeo(ctx.Descriptor.Geo);
        _waypoints.Clear();
        var path = ctx.DataRoot + "waypoints.json";
        if (Godot.FileAccess.FileExists(path))
        {
            var file = JsonSerializer.Deserialize<WaypointFile>(Godot.FileAccess.GetFileAsBytes(path), ContractJson.Options);
            foreach (var w in file?.Waypoints ?? []) _waypoints[w.Id] = w;
        }
        if (_style is null) return;
        _page.SetTitle(ctx.Descriptor.Name.Zh, ctx.Descriptor.Name.En.ToUpperInvariant());
        _page.Paper.SetBounds(PageRect(ctx.Descriptor));
        _atlasRev = -1;
        RebuildEntries();
    }

    /// <summary>
    /// 地图页范围 atlasRect：在本图 atlas/markers.json 里（WP-48c），契约里还没有读它的入口；
    /// 先按 dataRect 四周各外扩 20%（清水河即 ±1536 m）。
    /// </summary>
    private static Rect2 PageRect(MapDescriptor d)
    {
        var min = MapView.Flat(d.DataRect.Min);
        var size = MapView.Flat(d.DataRect.Max) - min;
        return new Rect2(min - size * 0.1f, size * 1.2f);
    }

    private void RebuildEntries()
    {
        if (_style is null || _atlas.Rev == _atlasRev) return;
        _atlasRev = _atlas.Rev;
        _regions = _atlas.Regions.GroupBy(r => r.Id, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
        _entries = [.. _atlas.Markers.Where(m => _atlas.StateOf(m.Id) != UnlockState.Hidden)
            .Select(m => new MapEntry(m, m.WaypointId is { } w && _waypoints.TryGetValue(w, out var wp) ? wp : null))];
        _minimap.Markers.Tracked = _atlas.Tracked;
        _minimap.Markers.SetEntries(_entries);
        _page.Paper.Markers.Tracked = _atlas.Tracked;
        _page.Paper.Markers.SetEntries(_entries);
        _page.Index.SetEntries(_entries);
        _page.Layers.SetCounts(_entries);
        if (_page.Paper.Card.Entry is { } shown && _entries.FirstOrDefault(e => e.Id == shown.Id) is { } fresh) OpenCard(fresh, keepFocus: true);
    }

    // ------------------------------------------------------------------ 每帧

    public override void _Process(double delta)
    {
        var dt = (float)Math.Min(delta, 0.1);
        UpdateTheme();
        var playing = _flow.State switch
        {
            AppState.Playing => true,
            AppState.Boot or AppState.Title or AppState.Entering or AppState.Leaving or AppState.Quitting => false,
            _ => throw new InvalidOperationException(_flow.State.ToString()),
        };
        if (!playing && _open) Close(restoreRoam: false);
        if (_pending is { } pending && pending.Done.IsCompleted) FinishTravel();

        var minimapVisible = _uiEnabled && playing && !_pure && !_minimapOff && !_open && _map is not null;
        _minimap.Visible = minimapVisible;
        if (minimapVisible) UpdateMinimap(dt);
        _page.Visible = _open;
        if (_open) UpdatePage(dt);
    }

    private void UpdateMinimap(float dt)
    {
        var feet = _observer.Feet;
        var at = new Vector2(feet.X, feet.Z);
        if (_world.Instance != _instance)
        {
            // 新的世界实例（读档、新游戏）：平滑量直接落到新值（ENG-ARCH §2）
            _instance = _world.Instance;
            _lastFeet = at;
            _speed = 0;
            _halfWidth.Snap(TargetHalfWidth(0));
        }
        var v = dt > 0 ? (at - _lastFeet).Length() / dt : 0;
        _lastFeet = at;
        _speed += (MathF.Min(v, 400) - _speed) * MathF.Min(1, dt * 4);
        var w = _halfWidth.Step(TargetHalfWidth(_speed), dt);
        var ppm = _minimap.ViewportHalf / w;
        var view = new MapView(at, ppm, 0);
        var size = _minimap.Viewport.Size;
        _minimap.Base.SetView(view);
        _minimap.Markers.Follow(view, size);
        var pose = _rig.Pose;
        _minimap.Player.Follow(size * 0.5f, _observer.YawDeg, pose.YawDeg, HorizontalFov(pose));
        _minimap.SetScale(ppm);
        _minimap.SetInterfaceState(Input.MouseMode == Input.MouseModeEnum.Visible);
        // 镜头在室内、地下、水下时整体不透明度 0.6；雾幕段跟着雾幕淡出、淡回（SYS-ATLAS §3）
        var dim = _medium.Current.Kind switch
        {
            MediumKind.Interior or MediumKind.Underground or MediumKind.Underwater => 0.6f,
            MediumKind.Air or MediumKind.FogSea => 1f,
            _ => throw new InvalidOperationException(_medium.Current.Kind.ToString()),
        };
        _minimap.Modulate = new Color(1, 1, 1, dim * (1 - Math.Clamp(_veil.Opacity, 0, 1)));
        UpdateRegion(dt, at);
    }

    /// <summary>行走固定 120 m；旁观 W = clamp(120 + 1.1·h + 2.5·v, 120, 1300)（SYS-ATLAS §3）。</summary>
    private float TargetHalfWidth(float speed)
    {
        switch (_observer.Mode)
        {
            case ViewMode.Walk: return WalkHalfWidth;
            case ViewMode.Fly:
                // 离地高度 h 应取常驻的低精度地形（SYS-VIEW §2.1）；界面拿不到地形，观光期地面是相对平面
                // （campus.json 的 heightNote），先按眼高计
                var h = MathF.Max(0, _observer.Eye.Y);
                return Math.Clamp(WalkHalfWidth + 1.1f * h + 2.5f * speed, WalkHalfWidth, 1300);
            default: throw new InvalidOperationException(_observer.Mode.ToString());
        }
    }

    private float HorizontalFov(Pose pose)
    {
        var vfov = pose.FovDeg ?? 60;
        var vp = GetViewport().GetVisibleRect().Size;
        var aspect = vp.Y > 0 ? vp.X / vp.Y : 16f / 9f;
        return Mathf.RadToDeg(2 * MathF.Atan(MathF.Tan(Mathf.DegToRad(vfov) * 0.5f) * aspect));
    }

    /// <summary>每秒 10 次查 8 m 区域栅格；新区域连续保持 1 s 才换字（SYS-ATLAS §3）。</summary>
    private void UpdateRegion(float dt, Vector2 at)
    {
        _regionClock += dt;
        if (_regionClock < 1 / RegionHz && _regionShown is not null) return;
        var step = _regionClock;
        _regionClock = 0;
        var id = _atlas.RegionAt(at.X, at.Y) ?? "";
        if (_regionShown is null) { _regionShown = id; _regionCandidate = id; ShowRegion(); return; }
        if (id != _regionCandidate) { _regionCandidate = id; _regionCandidateAge = 0; }
        else _regionCandidateAge += step;
        if (_regionCandidate != _regionShown && _regionCandidateAge >= RegionHold) { _regionShown = _regionCandidate; ShowRegion(); }
        else if (_observer.Space is { Building: not null }) ShowRegion();
    }

    private void ShowRegion()
    {
        var text = RegionText(_regionShown);
        if (_observer.Space is { Building: not null } space) text += $" · {space.Floor + 1}F";
        _minimap.SetRegion(text);
    }

    /// <summary>"核心区 · 图书馆"：大区名 · 地点名；区域表还空着时写图名。</summary>
    private string RegionText(string? id)
    {
        if (id is { Length: > 0 } && _regions.TryGetValue(id, out var place))
        {
            if (place.Parent is { } p && _regions.TryGetValue(p, out var parent)) return parent.Name.Zh + " · " + place.Name.Zh;
            return place.Name.Zh;
        }
        return _map?.Name.Zh ?? "";
    }

    // ------------------------------------------------------------------ 主题

    private void ReadArguments()
    {
        foreach (var arg in OS.GetCmdlineUserArgs())
        {
            if (arg.StartsWith("--ui=", StringComparison.Ordinal)) _uiEnabled = arg[5..] != "0";
            else if (arg.StartsWith("--ui-theme=", StringComparison.Ordinal)) _themeArg = arg[11..];
            else if (arg.StartsWith("--motion=", StringComparison.Ordinal)) _instant = arg[9..] == "instant";   // 界面截图用 instant（ENG-GATES §1）
        }
    }

    private bool InitialNight() => _themeArg switch
    {
        "light" => false,
        "dark" => true,
        _ => _world.Sun.AltDeg < 0,
    };

    /// <summary>
    /// 大页随昼夜主题：按真实太阳高度带回差锁存（下穿 −0.5° 切深屏，上穿 +0.5° 切回，快进中不判断）。
    /// 这是 WP-35 UiThemeClock 的同一规则；它合入后改读它的锁存结果，这里只保留读数。
    /// </summary>
    private void UpdateTheme()
    {
        var night = _night;
        if (_themeArg is "auto" && (_world.Transition & WorldTransition.Time) == 0)
        {
            var alt = _world.Sun.AltDeg;
            if (!night && alt < _style!.Metrics.ToDarkBelowDeg) night = true;
            else if (night && alt > _style!.Metrics.ToLightAboveDeg) night = false;
        }
        if (night == _night) return;
        _night = night;
        _ctx.ThemeFront = night ? 1 : 0;
        if (_material is not null) MapMaterial.SetThemeFront(_material, _ctx.ThemeFront);
        _minimap.SyncTheme();
        _page.RedrawAll();
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

    /// <summary>"此处"：镜头在 40 m 以内、朝向偏差在 60° 以内（UI-NAV §4.5）；标记层与目录各用一份。</summary>
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
        paper.Card.Show(e, Meta(e), Reach(e), Recommend(e), here, _atlas.Tracked == e.Id, TravelStyle.Teleport);
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

    /// <summary>等宽元信息："P05 · 西区 · 30.7516°N 103.9229°E"（SYS-ATLAS §4.5）。</summary>
    private string Meta(MapEntry e)
    {
        var area = e.Marker.Region is { } r && _regions.TryGetValue(r, out var reg) ? reg.Name.Zh
            : e.Waypoint?.Group is not { } g ? ""
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

    private static string Reach(MapEntry e) => e.Marker.Teleport switch
    {
        null => "只能追踪",
        { Stand: null } => "仅旁观",
        _ => "旁观 · 行走",
    };

    private static string Recommend(MapEntry e) => e.Waypoint?.Recommend is { } r
        ? $"推荐 {MapHudContext.SeasonName(r.Season)} {MapHudContext.Clock(r.Clock)} · {MapHudContext.WeatherName(r.Weather)}"
        : "";

    // ------------------------------------------------------------------ 传送与追踪

    private void TravelSelected(TravelStyle style)
    {
        if (_page.Paper.Card.Entry is not { } e || _pending is not null) return;
        if (!_atlas.CanTravel(e.Id, style, out _)) return;
        var handle = _atlas.Travel(e.Id, style);
        if (handle is null) return;
        // 输入占用不随大地图收拢释放，保持到还给控制（SYS-ATLAS §7.2 第 1、6 步）
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

    // ------------------------------------------------------------------ 按键（UI-NAV §5、SYS-ATLAS §4.7）

    public override void _UnhandledInput(InputEvent e)
    {
        if (_style is null || _flow.State != AppState.Playing) return;
        if (_open) { AtlasInput(e); return; }
        if (e.IsActionPressed("map")) { Open(AtlasTab.Layers); Handled(); }
        else if (e.IsActionPressed("atlas_index")) { Open(AtlasTab.Index); Handled(); }
        else if (e.IsActionPressed("minimap")) { _minimapOff = !_minimapOff; Handled(); }
        else if (e.IsActionPressed("clean_view")) _pure = !_pure;
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

    private void Handled() => GetViewport().SetInputAsHandled();
}
