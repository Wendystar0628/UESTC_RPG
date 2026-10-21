using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Audio;
using Campus.Hud.Pages;
using Campus.Hud.Widgets;
using Godot;

namespace Campus.Hud;

/// <summary>Owns live corner controls, refresh cadence, notices, and their idle fade.</summary>
internal sealed class HudResidents
{
    private const float IdleFade = 4f;
    private const double RefreshInterval = 0.1;
    private readonly HudRootDependencies _dependencies;
    private readonly HudView _view;
    private readonly HudPages _pages;
    private Resident? _resident;
    private PerfReadout? _perf;
    private float _idle, _cornerAlpha = 1;
    private double _refreshClock = RefreshInterval;
    private bool _primed;
    private ViewMode _lastMode;
    private Season _lastSeason;
    private int _entry;
    public bool Clean { get; set; }
    public bool TimeOpen { get; private set; } = true;

    public HudResidents(HudRootDependencies dependencies, HudView view, HudPages pages)
    {
        _dependencies = dependencies;
        _view = view;
        _pages = pages;
    }

    public void Enter()
    {
        _entry++;
        _primed = false;
        _lastMode = _dependencies.Model.Mode;
        _lastSeason = _dependencies.Model.Season;
        _dependencies.Model.World.InstanceChanged += OnWorldChanged;
        _dependencies.ViewControl.WalkEntryRefused += OnWalkEntryRefused;
    }

    public void Exit()
    {
        _entry++;
        _dependencies.Model.World.InstanceChanged -= OnWorldChanged;
        _dependencies.ViewControl.WalkEntryRefused -= OnWalkEntryRefused;
        Release();
        FreePerf();
    }

    public void Sync()
    {
        SyncPerf();
        if (!_dependencies.Flow.IsPlaying || _resident is not { } r) { _view.MinimapSlot.Visible = false; return; }
        var ui = _dependencies.InputRouter.InterfaceMode && !Clean;
        _view.MinimapSlot.Visible = !Clean && !_pages.BigPage;
        var show = ui && !_pages.Saves.Visible;
        r.TopRight.Visible = show;
        r.Speed.Visible = show && !_pages.Settings.Visible;
        r.Time.Visible = show && TimeOpen;
        r.Weather.Visible = show && TimeOpen && _dependencies.Model.Allows(WorldControlMask.Weather);
        r.Strip.Visible = show && !TimeOpen;
    }

    public void Tick(double delta, bool loading, bool galleryHud)
    {
        Sync();
        _refreshClock += delta;
        if (_refreshClock >= RefreshInterval) Refresh();
        if (_dependencies.Flow.IsPlaying && _dependencies.InputRouter.InterfaceMode && _view.Corners.IsVisibleInTree() && !loading
            && _dependencies.Flow.Activity == AppActivity.None && !galleryHud && !_pages.BigPage
            && _view.Root.GetViewport().GuiGetFocusOwner() is null && DisplayServer.AccessibilityScreenReaderActive() == 0)
            _idle += (float)delta;
        else _idle = 0;
        var target = _idle >= IdleFade ? 0f : 1f;
        var step = (float)delta / _dependencies.Motion.Fade(target < _cornerAlpha ? 1.2f : 0.18f);
        _cornerAlpha = Mathf.MoveToward(_cornerAlpha, target, _dependencies.Motion.Level == MotionLevel.Instant ? 1 : step);
        FadeResidents(_cornerAlpha, target > 0);
    }

    public void Touch() => _idle = 0;
    public void NextWeather() => _resident?.Weather.Next();
    public void ToggleTime(bool open)
    {
        TimeOpen = open;
        _dependencies.Audio.PlayUi(open ? UiCue.PageOpen : UiCue.PageClose);
        Sync();
    }

    private string ResetName() => _dependencies.Maps.Context?.Descriptor.World.DefaultName.Zh ?? "";
    private async Task SetMode(ViewMode mode)
    {
        var entry = _entry;
        await _dependencies.ViewControl.SetMode(mode, ModeSwitchVia.Ui);
        if (entry == _entry) Refresh();
    }

    private void OnWalkEntryRefused()
    {
        _lastMode = ViewMode.Fly;
        _pages.Toast.Show(UiKit.T("toast.walk.cannot_land"));
    }
    // ---------------- 四角的常驻件：进入漫游时建，回到开始页时释放（开始页上它们本来就不显示，不该占实例参数槽） ----------------
    private sealed record Resident(SpeedBar Speed, WeatherStrip Weather, TimeCard Time, TimeStrip Strip, Control TopRight)
    {
        public Control[] Controls { get; } = [Speed, Weather, Time, Strip, TopRight];
    }

    public void Build()
    {
        if (_resident is not null) return;
        var speed = SpeedBar.Create(_view.Kit, _view.BottomLeft, _dependencies.Model);
        speed.ModePicked += mode => _ = SetMode(mode);
        var weather = WeatherStrip.Create(_view.Kit, _view.BottomRight, _dependencies.Model, _dependencies.WorldControl);
        weather.Toast += text => _pages.Toast.Show(text, "weather_auto");
        var time = TimeCard.Create(_view.Kit, _view.BottomRight, _dependencies.Model, _dependencies.WorldControl, _dependencies.PaletteProbe, ResetName);
        time.Toast += text => _pages.Toast.Show(text);
        var strip = TimeStrip.Create(_view.Kit, _view.BottomRight, _dependencies.Model);
        strip.GuiInput += e => { if (e is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }) ToggleTime(true); };
        _resident = new Resident(speed, weather, time, strip, BuildTopRight());
        ResetIdle();
        _view.Layout();
    }

    public void Release()
    {
        if (_resident is not { } resident) return;
        foreach (var child in resident.Controls)
        {
            child.GetParent().RemoveChild(child);
            child.QueueFree();
        }
        _resident = null;
    }

    // ---------------- 性能信息：在右上角单独显隐，不跟随普通 HUD 淡出 ----------------
    private void SyncPerf()
    {
        var want = _dependencies.Settings.ShowPerf && _dependencies.Flow.IsPlaying && !Clean && _view.Root.Visible;
        if (want == (_perf is not null)) return;
        if (!want) { FreePerf(); return; }
        _perf = PerfReadout.Create(_view.Kit, _view.TopRight);
    }

    private void FreePerf()
    {
        if (_perf is not { } perf) return;
        _perf = null;
        perf.GetParent().RemoveChild(perf);   // 立即移出场景树，视口渲染耗时测量随之关闭
        perf.QueueFree();
    }

    private Control BuildTopRight()
    {
        var topRight = _view.Kit.Layout.Row(_view.TopRight);
        topRight.SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd;
        (string Icon, string Tip, Action Act)[] buttons =
        [
            ("save", "hud.button.saves", () => _pages.OpenSaves(fromTitle: false)),
            ("help", "hud.button.help", () => _pages.ToggleSettings(SettingsPage.Keys)),
            ("settings", "hud.button.settings", () => _pages.ToggleSettings(SettingsPage.Graphics)),
        ];
        for (var i = 0; i < buttons.Length; i++)
        {
            var (icon, tip, act) = buttons[i];
            var b = _view.Kit.Button(topRight, ButtonStyle.Icon, "", icon: icon, pin: true, pressed: act);
            b.TooltipText = UiKit.T(tip);
            b.BaseFill = "surface.panel";
            b.Refresh();
        }
        return topRight;
    }

    private void FadeResidents(float alpha, bool interactive)
    {
        if (_resident is not { } resident) return;
        foreach (var child in resident.Controls)
        {
            child.Modulate = new Color(1, 1, 1, alpha);
            UiLayout.SetInteractive(child, interactive && child.Visible);
        }
    }
    public void Refresh()
    {
        _refreshClock = 0;
        SyncNotices();
        if (_resident is not { } r) return;
        if (r.Speed.IsVisibleInTree()) r.Speed.Sync();
        if (r.Time.IsVisibleInTree()) r.Time.Sync();
        if (r.Weather.IsVisibleInTree()) r.Weather.Sync();
        if (r.Strip.IsVisibleInTree()) r.Strip.Sync();
    }

    private void OnWorldChanged(long instance)
    {
        _primed = false;
        _refreshClock = RefreshInterval;
    }

    private void SyncNotices()
    {
        // 开始页、加载与退场期间不出常驻提示（SYS-TITLE）；回到 Playing 后先记值再比较
        if (!_dependencies.Flow.IsPlaying) { _primed = false; return; }
        if (!_primed)
        {
            // 首次刷新或换世界后只记下当前值，不出提示
            _primed = true;
            _lastMode = _dependencies.Model.Mode;
            _lastSeason = _dependencies.Model.Season;
            return;
        }
        if (_dependencies.Model.Mode != _lastMode)
        {
            _lastMode = _dependencies.Model.Mode;
            _pages.Toast.Show(UiKit.T(_dependencies.Model.Mode == ViewMode.Walk ? "toast.mode.walk" : "toast.mode.fly"), _dependencies.Model.Mode == ViewMode.Walk ? "view_eye" : "view_free");
        }
        if (_dependencies.Model.Season != _lastSeason)
        {
            _lastSeason = _dependencies.Model.Season;
            _pages.Toast.Show(UiKit.T("hud.season." + TimeCard.SeasonCode(_dependencies.Model.Season).ToLowerInvariant()), "season_" + TimeCard.SeasonCode(_dependencies.Model.Season).ToLowerInvariant());
        }
    }

    public void ResetIdle()
    {
        _idle = 0;
        _cornerAlpha = 1;
        FadeResidents(1, interactive: true);
    }

}
