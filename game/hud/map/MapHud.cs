using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Atlas;
using Campus.Contracts.Maps;
using Godot;

namespace Campus.Hud.Map;

/// <summary>
/// WP-48b 的宿主：左上角常驻小地图与 M / G 打开的大地图（SYS-ATLAS）。
/// 宿主只管依赖、底图（map_base 材质与 SDF 绑定）的生命周期和两边共用的数据（当前图、标记条目、主题、显隐开关）；
/// 小地图与图集页各自的行为在 <see cref="MinimapController"/> 与 <see cref="AtlasController"/>。
/// 标记、区域与传送经 IAtlasService；玩家箭头与所在区域读 IObserverState；视锥读 ICameraRig.Pose；
/// 不直接调 ICameraRig.Transit（ENG-ARCH）。界面在 CanvasLayer 10（HudRoot 那一层）；
/// WP-35 的左上角槽位与全屏页槽位合入后改挂到槽位里。
/// </summary>
[KernelNode("hud-map", Owner = "WP-48b")]
public partial class MapHud : Node, IMapAware
{
    private MapStyle? _style;
    private MapHudServices? _services;
    private MapHudContext _ctx = null!;
    private ShaderMaterial? _material;
    private MapBaseBinding? _base;
    private CanvasLayer? _layer;
    private Minimap _minimap = null!;
    private AtlasPage _page = null!;
    private MinimapController _minimapCtl = null!;
    private AtlasController _atlasCtl = null!;

    private MapDescriptor? _map;
    private MapGeo? _geo;
    private IReadOnlyList<MapEntry> _entries = [];
    private long _atlasRev = -1;

    private bool _uiEnabled = true;
    private string _themeArg = "auto";
    private bool _night;
    private bool _pure;

    // 两个控制共用的状态与依赖
    internal MapHudServices Services => _services!;
    internal MapStyle Style => _style!;
    internal MapHudContext Ctx => _ctx;
    internal CanvasLayer? Layer => _layer;
    internal MapDescriptor? Map => _map;
    internal MapGeo? Geo => _geo;
    internal IReadOnlyList<MapEntry> Entries => _entries;
    internal bool Instant { get; private set; }
    internal bool MinimapOff { get; set; }

    public override void _Ready()
    {
        ProcessPriority = FramePriority.Hud;
        _style = MapStyle.TryLoad();
        if (_style is null) { SetProcess(false); return; }
        _services = new MapHudServices();
        var atlas = _services.Atlas;
        ReadArguments();

        _ctx = new MapHudContext(_style);
        _ctx.PinShapeLookup = id => atlas.Pins.FirstOrDefault(p => "pin:" + p.Id.ToString(System.Globalization.CultureInfo.InvariantCulture) == id.Value)?.Shape ?? PinShape.Diamond;
        _night = InitialNight();
        _ctx.ThemeFront = _night ? 1 : 0;

        var layer = new CanvasLayer { Layer = 10 };
        _layer = layer;
        AddChild(layer);
        var root = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        layer.AddChild(root);
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _minimap = new Minimap(_ctx);
        root.AddChild(_minimap);
        CallDeferred(MethodName.MountMinimap);
        // 大地图在扉页之后预建、隐藏：第一次按 M 不建控件（SYS-ATLAS）
        _page = new AtlasPage(_ctx) { Visible = false };
        root.AddChild(_page);

        _material = MapMaterial.Acquire();
        if (_material is not null)
        {
            MapMaterial.Configure(_material, _style);
            MapMaterial.SetThemeFront(_material, _ctx.ThemeFront);
        }
        _base = new MapBaseBinding(_services.AtlasBase, _material);
        _minimap.Base.Bind(_material, _ctx.ResidentNight);
        _page.Paper.Base.Bind(_material, false);
        _minimap.SyncTheme();

        _atlasCtl = new AtlasController(this, _page);
        _minimapCtl = new MinimapController(this, _minimap, () => _atlasCtl.Open(AtlasTab.Layers), () => _atlasCtl.Open(AtlasTab.Index));
        atlas.Changed += RebuildEntries;
        RebuildEntries();
    }

    public override void _ExitTree()
    {
        if (_style is null) return;
        _atlasCtl.Dispose();
        _base?.Dispose();
        _services!.Atlas.Changed -= RebuildEntries;
    }

    private void MountMinimap() => _minimapCtl.Mount();

    // ------------------------------------------------------------------ 地图数据

    public void OnMapUnloading(MapId from)
    {
        _map = null;
        _geo = null;
        if (_style is null) return;
        _base!.Clear();
        _atlasCtl.Unload();
        _entries = [];
        _minimapCtl.SetEntries(_entries);
        _atlasCtl.SetEntries(_entries);
    }

    public void OnMapLoaded(IMapContext ctx)
    {
        _map = ctx.Descriptor;
        _geo = new MapGeo(ctx.Descriptor.Geo);
        if (_style is null) return;
        _atlasCtl.SetTitle(ctx.Descriptor.Name.Zh, ctx.Descriptor.Name.En.ToUpperInvariant());
        _base!.Clear();
        _atlasRev = -1;
        RebuildEntries();
    }

    private void RebuildEntries()
    {
        var atlas = _services!.Atlas;
        if (atlas.Rev == _atlasRev) return;
        _atlasRev = atlas.Rev;
        _entries = [.. atlas.Markers.Where(m => atlas.StateOf(m.Id) != UnlockState.Hidden)
            .Select(m => new MapEntry(m))];
        _minimapCtl.SetEntries(_entries);
        _atlasCtl.SetEntries(_entries);
    }

    // ------------------------------------------------------------------ 每帧

    public override void _Process(double delta)
    {
        var dt = (float)Math.Min(delta, 0.1);
        UpdateTheme();
        if (_base!.Poll(_map?.Id) && _base.PageRect is { } pageRect) _atlasCtl.SetBounds(pageRect);
        var playing = _services!.Flow.State switch
        {
            AppState.Playing => true,
            AppState.Boot or AppState.Title or AppState.Entering or AppState.Leaving or AppState.Quitting => false,
            _ => throw new InvalidOperationException(_services.Flow.State.ToString()),
        };
        _atlasCtl.Frame(dt, playing);
        _minimapCtl.Frame(dt, playing, hidden: !_uiEnabled || _pure || MinimapOff || _atlasCtl.IsOpen || _map is null);
    }

    internal float HorizontalFov(Pose pose)
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
            else if (arg.StartsWith("--motion=", StringComparison.Ordinal)) Instant = arg[9..] == "instant";   // 界面截图用 instant（ENG-GATES）
        }
    }

    private bool InitialNight() => _themeArg switch
    {
        "light" => false,
        "dark" => true,
        _ => _services!.World.Sun.AltDeg < 0,
    };

    /// <summary>
    /// 大页随昼夜主题：按真实太阳高度带回差锁存（下穿 −0.5° 切深屏，上穿 +0.5° 切回，快进中不判断）。
    /// 这是 WP-35 UiThemeClock 的同一规则；它合入后改读它的锁存结果，这里只保留读数。
    /// </summary>
    private void UpdateTheme()
    {
        var world = _services!.World;
        var night = _night;
        if (_themeArg is "auto" && (world.Transition & WorldTransition.Time) == 0)
        {
            var alt = world.Sun.AltDeg;
            if (!night && alt < _style!.Metrics.ToDarkBelowDeg) night = true;
            else if (night && alt > _style!.Metrics.ToLightAboveDeg) night = false;
        }
        if (night == _night) return;
        _night = night;
        _ctx.ThemeFront = night ? 1 : 0;
        if (_material is not null) MapMaterial.SetThemeFront(_material, _ctx.ThemeFront);
        _minimapCtl.SyncTheme();
        _atlasCtl.RedrawAll();
    }

    // ------------------------------------------------------------------ 按键（UI-NAV、SYS-ATLAS）

    public override void _UnhandledInput(InputEvent e)
    {
        if (_style is null || _services!.Flow.State != AppState.Playing) return;
        if (_atlasCtl.IsOpen) { _atlasCtl.HandleInput(e); return; }
        if (e.IsActionPressed("map")) { _atlasCtl.Open(AtlasTab.Layers); Handled(); }
        else if (e.IsActionPressed("atlas_index")) { _atlasCtl.Open(AtlasTab.Index); Handled(); }
        else if (e.IsActionPressed("minimap")) { MinimapOff = !MinimapOff; Handled(); }
        else if (e.IsActionPressed("clean_view")) _pure = !_pure;
    }

    internal void Handled() => GetViewport().SetInputAsHandled();
}
