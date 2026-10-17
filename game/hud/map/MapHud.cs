using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Atlas;
using Campus.Contracts.Maps;
using KernelRegistry = Campus.Kernel.Registry.Kernel;
using Godot;

namespace Campus.Hud.Map;

/// <summary>
/// WP-48b 的入口：左上角常驻小地图与 M / G 打开的大地图（SYS-ATLAS）。
/// 只依赖契约与 Kernel.Get：标记、区域与传送经 IAtlasService；玩家箭头与所在区域读 IObserverState；
/// 视锥读 ICameraRig.Pose；不直接调 ICameraRig.Transit（ENG-ARCH）。界面在 CanvasLayer 10（HudRoot 那一层）；
/// WP-35 的左上角槽位与全屏页槽位合入后改挂到槽位里。
/// </summary>
[KernelNode("hud-map", Owner = "WP-48b")]
public partial class MapHud : Node, IMapAware
{
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
    private CanvasLayer? _layer;
    private Control _root = null!;
    private Minimap _minimap = null!;
    private AtlasPage _page = null!;

    private MapDescriptor? _map;
    private MapGeo? _geo;
    private IReadOnlyList<MapEntry> _entries = [];
    private long _atlasRev = -1;

    private bool _uiEnabled = true;
    private string _themeArg = "auto";
    private bool _instant;
    private bool _night;
    private bool _pure;
    private bool _minimapOff;

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
        // 大地图在扉页之后预建、隐藏：第一次按 M 不建控件（SYS-ATLAS）
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

        WireMinimap();
        WireAtlasPage();
        _atlas.Changed += RebuildEntries;
        RebuildEntries();
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
        if (_style is null) return;
        if (_open) Close(restoreRoam: false);
        _entries = [];
        _minimap.Markers.SetEntries(_entries);
    }

    public void OnMapLoaded(IMapContext ctx)
    {
        _map = ctx.Descriptor;
        _geo = new MapGeo(ctx.Descriptor.Geo);
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
        _entries = [.. _atlas.Markers.Where(m => _atlas.StateOf(m.Id) != UnlockState.Hidden)
            .Select(m => new MapEntry(m))];
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
        // 离开 Playing 期间（标题页、进出过场）机位会动而 Instance 不变；回到 Playing 的第一帧要重置一次
        if (!playing) _resetMinimapView = true;
        if (!playing && _open) Close(restoreRoam: false);
        if (_pending is { } pending && pending.Done.IsCompleted) FinishTravel();

        var minimapVisible = _uiEnabled && playing && !_pure && !_minimapOff && !_open && _map is not null;
        _minimap.Visible = minimapVisible;
        if (minimapVisible) UpdateMinimap(dt);
        _page.Visible = _open;
        if (_open) UpdatePage(dt);
    }

    private float HorizontalFov(Pose pose)
    {
        var vfov = pose.FovDeg ?? 60;
        var vp = GetViewport().GetVisibleRect().Size;
        var aspect = vp.Y > 0 ? vp.X / vp.Y : 16f / 9f;
        return Mathf.RadToDeg(2 * MathF.Atan(MathF.Tan(Mathf.DegToRad(vfov) * 0.5f) * aspect));
    }

    // ------------------------------------------------------------------ 主题

    private void ReadArguments()
    {
        foreach (var arg in OS.GetCmdlineUserArgs())
        {
            if (arg.StartsWith("--ui=", StringComparison.Ordinal)) _uiEnabled = arg[5..] != "0";
            else if (arg.StartsWith("--ui-theme=", StringComparison.Ordinal)) _themeArg = arg[11..];
            else if (arg.StartsWith("--motion=", StringComparison.Ordinal)) _instant = arg[9..] == "instant";   // 界面截图用 instant（ENG-GATES）
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

    // ------------------------------------------------------------------ 按键（UI-NAV、SYS-ATLAS）

    public override void _UnhandledInput(InputEvent e)
    {
        if (_style is null || _flow.State != AppState.Playing) return;
        if (_open) { AtlasInput(e); return; }
        if (e.IsActionPressed("map")) { Open(AtlasTab.Layers); Handled(); }
        else if (e.IsActionPressed("atlas_index")) { Open(AtlasTab.Index); Handled(); }
        else if (e.IsActionPressed("minimap")) { _minimapOff = !_minimapOff; Handled(); }
        else if (e.IsActionPressed("clean_view")) _pure = !_pure;
    }

    private void Handled() => GetViewport().SetInputAsHandled();
}
