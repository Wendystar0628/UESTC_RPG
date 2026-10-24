using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Audio;
using Campus.Hud.Pages;
using Campus.Hud.Widgets;
using Godot;

namespace Campus.Hud;

/// <summary>Owns live corner controls, refresh cadence and notices; the corners stay resident (UI-NAV).</summary>
internal sealed class HudResidents
{
    private const double RefreshInterval = 0.1;
    private readonly HudRootDependencies _dependencies;
    private readonly HudView _view;
    private readonly HudPages _pages;
    private Resident? _resident;
    private PerfReadout? _perf;
    private double _refreshClock = RefreshInterval;
    private bool _primed;
    private ViewMode _lastMode;
    private Season _lastSeason;
    private readonly HudTask _modeSwitch;
    public bool Clean { get; set; }
    public bool TimeOpen { get; private set; }
    /// <summary>时间卡此刻真的展开在画面上（四角件可见且卡已展开），Esc 与光标按它判断。</summary>
    public bool TimeShown => _resident is { } r && r.Time.IsVisibleInTree();

    public HudResidents(HudRootDependencies dependencies, HudView view, HudPages pages)
    {
        _dependencies = dependencies;
        _view = view;
        _pages = pages;
        _modeSwitch = new HudTask(dependencies.Failure);
    }

    public void Enter()
    {
        _primed = false;
        _lastMode = _dependencies.Model.Mode;
        _lastSeason = _dependencies.Model.Season;
        _dependencies.Model.World.InstanceChanged += OnWorldChanged;
        _dependencies.ViewControl.WalkEntryRefused += OnWalkEntryRefused;
    }

    public void Exit()
    {
        _modeSwitch.Cancel();
        _dependencies.Model.World.InstanceChanged -= OnWorldChanged;
        _dependencies.ViewControl.WalkEntryRefused -= OnWalkEntryRefused;
        Release();
        FreePerf();
    }

    // 四角件常驻，整体显隐只由宿主仲裁的角区决定（HudInput）；这里只切时间卡的展开与收起。
    public void Sync()
    {
        SyncPerf();
        if (_resident is not { } r) return;
        r.Time.Visible = TimeOpen;
        r.Weather.Visible = TimeOpen && _dependencies.Model.Allows(WorldControlMask.Weather);
        r.Strip.Visible = !TimeOpen;
    }

    public void Tick(double delta)
    {
        Sync();
        _refreshClock += delta;
        if (_refreshClock >= RefreshInterval) Refresh();
    }

    public void NextWeather() => _resident?.Weather.Next();
    public void ToggleTime(bool open)
    {
        TimeOpen = open;
        _dependencies.Audio.PlayUi(open ? UiCue.PageOpen : UiCue.PageClose);
        Sync();
    }

    private string ResetName() => _dependencies.Maps.Context?.Descriptor.World.DefaultName.Zh ?? "";
    // 公开 SetMode 的任务归调用方（相机只接自己发起的切换的失败）：等到切换真正结束，失败交 IAppFailure；HUD 已离树就不再刷新。
    private async Task SetMode(ViewMode mode, CancellationToken token)
    {
        await _dependencies.ViewControl.SetMode(mode, ModeSwitchVia.Ui);
        if (!token.IsCancellationRequested) Refresh();
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

    /// <summary>每次进入游玩新建；进游戏时时间卡收成一条，不展开任何面板（UI-NAV）。</summary>
    public void Build()
    {
        if (_resident is not null) return;
        TimeOpen = false;
        var speed = SpeedBar.Create(_view.Kit, _view.BottomLeft, _dependencies.Model);
        speed.ModePicked += mode => _modeSwitch.Start(token => SetMode(mode, token));
        var weather = WeatherStrip.Create(_view.Kit, _view.BottomRight, _dependencies.Model, _dependencies.WorldControl);
        weather.Toast += text => _pages.Toast.Show(text, "weather_auto");
        var time = TimeCard.Create(_view.Kit, _view.BottomRight, _dependencies.Model, _dependencies.WorldControl, _dependencies.PaletteProbe, ResetName);
        time.Toast += text => _pages.Toast.Show(text);
        var strip = TimeStrip.Create(_view.Kit, _view.BottomRight, _dependencies.Model);
        strip.GuiInput += e => { if (e is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }) ToggleTime(true); };
        _resident = new Resident(speed, weather, time, strip, BuildTopRight());
        Sync();
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

    // ---------------- 性能信息：在右上角单独显隐 ----------------
    private void SyncPerf()
    {
        var want = _dependencies.Settings.Get(HudSettingDefinitions.ShowPerf) && _dependencies.Flow.IsPlaying && !Clean && _view.Root.Visible;
        if (want == (_perf is not null)) return;
        if (!want) { FreePerf(); return; }
        _perf = PerfReadout.Create(_view.Kit, _view.TopRight);
    }

    private void FreePerf()
    {
        if (_perf is not { } perf) return;
        _perf = null;
        perf.GetParent().RemoveChild(perf);   // 立即移出场景树，停止刷新
        perf.QueueFree();
    }

    private Control BuildTopRight()
    {
        var topRight = _view.Kit.Layout.Row(_view.TopRight);
        topRight.SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd;
        // 存档钮没有快捷键；说明与设置钮标出当前绑定（Esc 打开设置卡，UI-NAV）。
        (string Icon, string Tip, string? Action, Action Act)[] buttons =
        [
            ("save", "hud.button.saves", null, () => _pages.OpenSaves(fromTitle: false)),
            ("help", "hud.button.help", "help", () => _pages.ToggleSettings(SettingsPage.Keys)),
            ("settings", "hud.button.settings", "menu", () => _pages.ToggleSettings(SettingsPage.Graphics)),
        ];
        for (var i = 0; i < buttons.Length; i++)
        {
            var (icon, tip, action, act) = buttons[i];
            var b = _view.Kit.Button(topRight, ButtonStyle.Icon, "", icon: icon, pin: true, pressed: act);
            b.BaseFill = "surface.panel";
            if (action is { } bound)
                Parts.KeyHint(_view.Kit, b, UiKit.T(tip), () => Parts.KeyLabel(_dependencies.InputRouter, bound), pin: true);
            else
            {
                b.TooltipText = UiKit.T(tip);
                b.AccessibilityName = UiKit.T(tip);
            }
            b.Refresh();
        }
        return topRight;
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
}
