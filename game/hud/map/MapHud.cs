using Campus.Contracts;
using Campus.Contracts.Atlas;
using Campus.Contracts.Maps;
using Campus.Contracts.Startup;
using Campus.Hud.Motion;
using Godot;

namespace Campus.Hud.Map;

/// <summary>
/// WP-48b 的宿主：左上角常驻小地图与 M / G 打开的大地图（SYS-ATLAS）。
/// 宿主只管依赖、底图（map_base 材质与 SDF 绑定）的生命周期和两边共用的数据（当前图、标记条目、主题、显隐开关）；
/// 小地图与图集页各自的行为在 <see cref="MinimapController"/> 与 <see cref="AtlasController"/>。
/// 标记、区域与传送经 IAtlasService；玩家箭头与所在区域读 IObserverState；视锥读 ICameraRig.Pose；
/// 不直接调 ICameraRig.Transit（ENG-ARCH）。界面在 CanvasLayer 10（HudRoot 那一层）；
/// 小地图挂进 HudRoot 提供的左上角槽位。
/// </summary>
[KernelNode("hud-map")]
public partial class MapHud : Node, IMapAware, IBootParticipant
{
    private MapStyle _style = null!;
    private MapHudServices? _services;
    private MapHudContext _ctx = null!;
    private ShaderMaterial _material = null!;
    private MapBaseBinding? _base;
    private Minimap _minimap = null!;
    private MinimapController _minimapCtl = null!;
    private AtlasController _atlasCtl = null!;

    private MapDescriptor? _map;
    private MapGeo? _geo;
    private IReadOnlyList<MapEntry> _entries = [];
    private long _atlasRev = -1;

    private bool _uiEnabled = true;
    private bool _night;
    private bool _pure;

    // 两个控制共用的状态与依赖
    internal MapHudServices Services => _services!;
    internal HudRoot Hud => _services!.Hud;
    internal MapStyle Style => _style;
    internal MapHudContext Ctx => _ctx;
    internal MapDescriptor? Map => _map;
    internal MapGeo? Geo => _geo;
    internal IReadOnlyList<MapEntry> Entries => _entries;
    internal bool Instant { get; private set; }
    internal bool MinimapOff { get; set; }

    public override void _EnterTree()
    {
        if (_services is null) return;
        _minimapCtl.ResetWorld();
        _services.World.InstanceChanged += OnWorldChanged;
    }

    public override void _Ready()
    {
        ProcessPriority = FramePriority.Hud;
        _services = new MapHudServices();
        _style = MapStyle.Load(Hud.Ui);
        var atlas = _services.Atlas;

        _ctx = new MapHudContext(_style, Hud.Ui);
        _ctx.PinShapeLookup = id => atlas.Pins.FirstOrDefault(p => "pin:" + p.Id.ToString(System.Globalization.CultureInfo.InvariantCulture) == id.Value)?.Shape ?? PinShape.Diamond;
        _minimap = new Minimap(_ctx);
        AddChild(_minimap);
        CallDeferred(MethodName.MountMinimap);

        _material = MapMaterial.Acquire();
        MapMaterial.Configure(_material, _style);
        MapMaterial.SetThemeFront(_material, _ctx.ThemeFront);
        _base = new MapBaseBinding(_services.AtlasBase, _material);
        _minimap.Base.Bind(_material, _ctx.ResidentNight);
        _minimap.SyncTheme();

        _atlasCtl = new AtlasController(this);
        _minimapCtl = new MinimapController(this, _minimap, () => _atlasCtl.Open(AtlasTab.Layers), () => _atlasCtl.Open(AtlasTab.Index));
        _services.World.InstanceChanged += OnWorldChanged;
        atlas.Changed += RebuildEntries;
        RebuildEntries();
    }

    public override void _ExitTree()
    {
        _atlasCtl.Dispose();
        _services!.Surfaces.Report(UiSurface.Minimap, false);
        _base?.Dispose();
        _services!.Atlas.Changed -= RebuildEntries;
        _services.World.InstanceChanged -= OnWorldChanged;
    }

    private void OnWorldChanged(long instance) => _minimapCtl.ResetWorld();

    private void MountMinimap() => _minimapCtl.Mount(Hud.MinimapSlot);
    internal void BindPage(AtlasPage page) => page.Paper.Base.Bind(_material, false);

    // ------------------------------------------------------------------ 地图数据

    public void OnMapUnloading(MapId from)
    {
        _map = null;
        _geo = null;
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
        _atlasCtl.SetTitle(ctx.Descriptor.Name.Zh, ctx.Descriptor.Name.En.ToUpperInvariant());
        _base!.Clear();
        var page = _services!.Atlas.PageRect;   // IAtlasService 先于本节点收到 OnMapLoaded（按 ID 顺序）
        var pageMin = MapView.Flat(page.Min);
        _atlasCtl.SetBounds(new Rect2(pageMin, MapView.Flat(page.Max) - pageMin));
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
        if (_ctx.ResidentFollowsTheme != Hud.Ui.ResidentFollows)
        {
            _ctx.ResidentFollowsTheme = Hud.Ui.ResidentFollows;
            _minimapCtl.SyncTheme();
        }
        ApplyNight(_services!.Theme.Dark);
        _base!.Poll(_map?.Id);
        var playing = _services!.Flow.IsPlaying;
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

    public string Id => "hud-map";
    public int Order => 0;

    /// <summary>启动参数在 BeforeMap 才到：_Ready 时先按默认搭好，这里补上 --ui、--motion。昼夜主题读 UiThemeClock.Dark（含 --ui-theme）。</summary>
    public Task Initialize(BootPhase phase, BootContext context)
    {
        if (phase != BootPhase.BeforeMap) return Task.CompletedTask;
        var args = context.Run.Ui;
        _uiEnabled = args.Visible != false;
        Instant = args.Motion == MotionLevel.Instant;   // 界面截图用 instant（ENG-GATES）
        return Task.CompletedTask;
    }

    private void ApplyNight(bool night)
    {
        if (night == _night) return;
        _night = night;
        _ctx.ThemeFront = night ? 1 : 0;
        MapMaterial.SetThemeFront(_material, _ctx.ThemeFront);
        _minimapCtl.SyncTheme();
        _atlasCtl.RedrawAll();
    }

    // ------------------------------------------------------------------ 按键（UI-NAV、SYS-ATLAS）

    public override void _UnhandledInput(InputEvent e)
    {
        if (!_services!.Flow.IsPlaying) return;
        if (_atlasCtl.IsOpen) { _atlasCtl.HandleInput(e); return; }
        if (e.IsActionPressed("map")) { _atlasCtl.Open(AtlasTab.Layers); Handled(); }
        else if (e.IsActionPressed("atlas_index")) { _atlasCtl.Open(AtlasTab.Index); Handled(); }
        else if (e.IsActionPressed("minimap")) { MinimapOff = !MinimapOff; Handled(); }
        else if (e.IsActionPressed("clean_view")) _pure = !_pure;
    }

    internal void Handled() => GetViewport().SetInputAsHandled();
}
