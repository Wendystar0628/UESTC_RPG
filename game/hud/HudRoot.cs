using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Audio;
using Campus.Contracts.Maps;
using Campus.Hud.Motion;
using Campus.Hud.Pages;
using Campus.Hud.Style;
using Campus.Hud.Widgets;
using Godot;
using Services = Campus.Kernel.Registry.Kernel;

namespace Campus.Hud;

/// <summary>
/// 界面根（UI-VISUAL §11.2、UI-NAV §3、§5）：CanvasLayer 10，搭控件树、挂唯一的 Theme。
/// 漫游态（鼠标被捕获）只留小地图槽位和临时提示；界面态（鼠标可见）四角出现，4 秒没有操作后淡出；
/// 大页（设置卡、存档页）打开时占用观者输入；开始页只在 IAppFlow 的 Title 状态出现。
/// </summary>
[KernelNode("hud", Owner = "WP-35")]
public partial class HudRoot : CanvasLayer
{
    private const float IdleFade = 4f;

    private UiKit _kit = null!;
    private HudSettings _settings = null!;
    private HudModel _model = null!;
    private MotionPolicy _motion = null!;
    private IWorldControl _control = null!;
    private IAppFlow? _flow;
    private IAudioService? _audio;
    private UiThemeClock _clock = null!;
    private Control _root = null!, _corners = null!, _topRight = null!, _minimapSlot = null!;
    private TimeCard _timeCard = null!;
    private TimeStrip _timeStrip = null!;
    private WeatherStrip _weather = null!;
    private SpeedBar _speed = null!;
    private Toast _toast = null!;
    private SettingsCard _settingsCard = null!;
    private SavePage _savePage = null!;
    private TitleMenu _title = null!;
    private bool _timeOpen = true, _clean, _hidden, _weatherHeld, _primed, _galleryTitle;   // 时间卡在界面态默认展开，T 收起（UI-VISUAL §7.5）
    private float _idle, _weatherHold, _scaleDebounce = -1;
    private ViewMode _lastMode;
    private Season _lastSeason;

    public HudRoot()
    {
        Layer = 10;
        Name = "HudRoot";
        ProcessPriority = FramePriority.Hud;
    }

    public override void _Ready()
    {
        _settings = Services.Get<HudSettings>();
        _model = Services.Get<HudModel>();
        _motion = (MotionPolicy)Services.Get<IMotionSettings>();
        _control = Services.Get<IWorldControl>();
        try { _flow = Services.Get<IAppFlow>(); } catch (InvalidOperationException) { _flow = null; }
        try { _audio = Services.Get<IAudioService>(); } catch (InvalidOperationException) { _audio = null; }

        var tokens = UiTokens.Load();
        var colors = UiColors.Load();
        var materials = new UiMaterials(tokens, colors);
        var theme = new UiThemeBuilder(tokens, colors, materials.Available);
        _kit = new UiKit(tokens, colors, materials, theme, _motion);
        if (ExportThemeArgument() is { } exportPath)
        {
            var error = theme.Export(exportPath);
            GD.Print($"UI theme exported: {exportPath} ({error})");
            GetTree().Quit(error == Error.Ok ? 0 : 2);
            return;
        }

        _clock = new UiThemeClock(Services.Get<IWorldState>(), _settings, _motion, tokens);
        AddChild(_clock);
        _clock.Switched += _ => { if (_settingsCard.Visible || _savePage.Visible || _kit.ResidentFollows) _audio?.PlayUi(UiCue.ShiftDayNight); };

        _root = new Control
        {
            Name = "Root",
            Theme = theme.Theme,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            TextureFilter = CanvasItem.TextureFilterEnum.LinearWithMipmaps
        };
        AddChild(_root);
        _root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _root.Resized += Layout;

        _minimapSlot = new Control { Name = "MinimapSlot", MouseFilter = Control.MouseFilterEnum.Ignore, Size = tokens.V2("metrics", "size", "minimap") };
        _root.AddChild(_minimapSlot);
        _corners = new Control { Name = "Corners", MouseFilter = Control.MouseFilterEnum.Ignore };
        _root.AddChild(_corners);
        _corners.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        _speed = SpeedBar.Create(_kit, _corners, _model);
        _speed.ModePicked += mode => _ = SetMode(mode);
        _weather = WeatherStrip.Create(_kit, _corners, _model, _control);
        _weather.Toast += text => _toast.Show(text, "weather_auto");
        _timeCard = TimeCard.Create(_kit, _corners, _model, _control, Services.Get<IPaletteProbe>(), ResetName);
        _timeCard.Toast += text => _toast.Show(text);
        _timeStrip = TimeStrip.Create(_kit, _corners, _model);
        _timeStrip.GuiInput += e => { if (e is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }) ToggleTime(true); };
        BuildTopRight();

        _title = TitleMenu.Create(_kit, _root);
        _title.SavesRequested += () => OpenSaves(fromTitle: true);
        _title.SettingsRequested += () => OpenSettings(SettingsPage.General, fromTitle: true);
        _title.CreditsRequested += () => OpenSettings(SettingsPage.Licenses, fromTitle: true);
        _title.QuitRequested += RequestQuit;
        _title.Notice += text => _toast.Show(text);
        _settingsCard = SettingsCard.Create(_kit, _root, _settings);
        _settingsCard.CloseRequested += CloseSettings;
        _settingsCard.QuitRequested += RequestQuit;
        _settingsCard.ToTitleRequested += () => { CloseSettings(); _ = _flow?.ReturnToTitle(); };
        _savePage = SavePage.Create(_kit, _root);
        _savePage.CloseRequested += CloseSaves;
        _toast = Toast.Create(_kit, _root);

        _settings.Changed += OnSettingsChanged;
        _kit.SetResidentFollows(_settings.Resident == ResidentTheme.Follow);
        if (_flow is not null) _flow.Changed += (_, _) => SyncState();
        _model.Changed += OnModelChanged;
        _lastMode = _model.Mode;
        _lastSeason = _model.Season;
        _hidden = Argument("ui") == "0";
        ApplyScale();
        Layout();
        SyncState();
        if (Argument("ui-gallery") is { } gallery) CallDeferred(MethodName.OpenGallery, gallery);
    }

    /// <summary>--ui-gallery=&lt;页名&gt;：启动时直接打开一页，供截图与评审（UI-VISUAL §11.12；完整的组件画廊归 35c）。</summary>
    private void OpenGallery(string page)
    {
        switch (page)
        {
            case "settings": OpenSettings(SettingsPage.General, fromTitle: false); break;
            case "graphics": OpenSettings(SettingsPage.Graphics, fromTitle: false); break;
            case "sound": OpenSettings(SettingsPage.Sound, fromTitle: false); break;
            case "keys": OpenSettings(SettingsPage.Keys, fromTitle: false); break;
            case "rebind": OpenSettings(SettingsPage.Rebind, fromTitle: false); break;
            case "licenses": OpenSettings(SettingsPage.Licenses, fromTitle: false); break;
            case "saves": OpenSaves(fromTitle: true); break;
            case "title": _clean = _galleryTitle = true; _title.Open(); break;
            case "hud": break;
            default: throw new ArgumentException($"Unknown --ui-gallery page: {page}");
        }
    }

    private static string? Argument(string name)
    {
        foreach (var arg in OS.GetCmdlineUserArgs())
        {
            if (arg == "--" + name) return "1";
            if (arg.StartsWith("--" + name + "=", StringComparison.Ordinal)) return arg[(name.Length + 3)..];
        }
        return null;
    }

    private static string? ExportThemeArgument() => Argument("ui-export-theme") switch
    {
        null => null,
        "1" => "res://hud/ui_theme.tres",
        var path => path,
    };

    private string ResetName()
    {
        try { return Services.Get<IMapService>().Context?.Descriptor.World.DefaultName.Zh ?? ""; }
        catch (InvalidOperationException) { return ""; }
    }

    // ---------------- 布局：四角贴角，中间留空（UI-VISUAL §8.2、§8.3） ----------------
    private void Layout()
    {
        var view = _root.Size;
        if (view.X <= 0) return;
        var edge = _kit.Tokens.Space("screenEdge");
        _minimapSlot.Position = new Vector2(edge, edge);
        _speed.Position = new Vector2(edge, view.Y - 36 - _speed.Size.Y);
        _timeCard.Position = new Vector2(view.X - edge - _timeCard.Size.X, view.Y - 36 - _timeCard.Size.Y);
        _timeStrip.Position = new Vector2(view.X - edge - _timeStrip.Size.X, view.Y - 36 - _timeStrip.Size.Y);
        _weather.Position = new Vector2(_timeCard.Position.X - 12 - _weather.Size.X, view.Y - 36 - _weather.Size.Y);
        _topRight.Position = new Vector2(view.X - edge - _topRight.Size.X, edge);
    }

    private void BuildTopRight()
    {
        var size = _kit.Tokens.Size("hudIconButton");
        _topRight = new Control { Size = new Vector2(size * 3 + 16, size), MouseFilter = Control.MouseFilterEnum.Ignore };
        _corners.AddChild(_topRight);
        (string Icon, string Tip, Action Act)[] buttons =
        [
            ("save", "hud.button.saves", () => OpenSaves(fromTitle: false)),
            ("help", "hud.button.help", () => ToggleSettings(SettingsPage.Keys)),
            ("settings", "hud.button.settings", () => ToggleSettings(SettingsPage.Graphics)),
        ];
        for (var i = 0; i < buttons.Length; i++)
        {
            var (icon, tip, act) = buttons[i];
            var b = _kit.Button(_topRight, ButtonStyle.Icon, "", new Rect2(i * (size + 8), 0, size, size), padX: 8, icon: icon, iconMax: 24, pin: true, padY: 8,
                pressed: act);
            b.TooltipText = UiKit.T(tip);
            b.BaseFill = "surface.panel";
            b.Refresh();
        }
    }

    // ---------------- 状态 ----------------
    private bool Playing => _flow is null || _flow.State == AppState.Playing;
    private bool InterfaceState => Input.MouseMode != Input.MouseModeEnum.Captured;
    private bool BigPage => _settingsCard.Visible || _savePage.Visible;

    private void SyncState()
    {
        var state = _flow?.State ?? AppState.Playing;
        _root.Visible = !_hidden;
        switch (state)
        {
            case AppState.Title:
                if (!_title.Visible) _title.Open();
                _corners.Visible = _minimapSlot.Visible = false;
                break;
            case AppState.Playing:
                _title.Visible = _galleryTitle;
                break;
            case AppState.Boot:
            case AppState.Entering:
            case AppState.Leaving:
            case AppState.Quitting:
                _title.Visible = false;
                if (_settingsCard.Visible) CloseSettings();
                if (_savePage.Visible) CloseSaves();
                break;
            default:
                throw new InvalidOperationException("Unknown application state.");
        }
        SyncCorners();
    }

    private void SyncCorners()
    {
        if (!Playing) { _corners.Visible = false; _minimapSlot.Visible = false; return; }
        var ui = InterfaceState && !_clean;
        _minimapSlot.Visible = !_clean && !BigPage;
        _corners.Visible = ui && !_savePage.Visible;
        _speed.Visible = !_settingsCard.Visible;
        _timeCard.Visible = _timeOpen;
        _weather.Visible = _timeOpen && _model.Allows(WorldControlMask.Weather);
        _timeStrip.Visible = !_timeOpen;
    }

    public override void _Process(double delta)
    {
        _settings.Tick(delta);
        _model.Tick(delta);
        if (_scaleDebounce >= 0 && (_scaleDebounce += (float)delta) >= 0.2f) { _scaleDebounce = -1; ApplyScale(); }
        SyncCorners();
        // 自动隐藏只作用于没人在用的普通 HUD（UI-NAV §3）：焦点、大页、改键、读屏都让它保持显示
        if (Playing && InterfaceState && !BigPage && _root.GetViewport().GuiGetFocusOwner() is null && DisplayServer.AccessibilityScreenReaderActive() == 0)
            _idle += (float)delta;
        else _idle = 0;
        var target = _idle >= IdleFade ? 0f : 1f;
        var step = (float)delta / (target < _corners.Modulate.A ? 1.2f : 0.18f);
        var a = Mathf.MoveToward(_corners.Modulate.A, target, _motion.Level == MotionLevel.Instant ? 1 : step);
        _corners.Modulate = new Color(1, 1, 1, a);
        if (_weatherHeld && Input.IsActionPressed("weather"))
        {
            _weatherHold += (float)delta;
            if (_weatherHold >= 0.6f)
            {
                _weatherHeld = false;
                _control.SetWeatherAuto();
                _model.Poke();
                _toast.Show(UiKit.T("weather.auto_toast"), "weather_auto");
            }
        }
    }

    private void Touch() => _idle = 0;

    private void OnModelChanged()
    {
        if (!_primed)
        {
            // 第一次采样只记下当前值，不出提示
            _primed = true;
            _lastMode = _model.Mode;
            _lastSeason = _model.Season;
            return;
        }
        if (_model.Mode != _lastMode)
        {
            _lastMode = _model.Mode;
            _toast.Show(UiKit.T(_model.Mode == ViewMode.Walk ? "toast.mode.walk" : "toast.mode.fly"), _model.Mode == ViewMode.Walk ? "view_eye" : "view_free");
        }
        if (_model.Season != _lastSeason)
        {
            _lastSeason = _model.Season;
            _toast.Show(UiKit.T("hud.season." + TimeCard.SeasonCode(_model.Season).ToLowerInvariant()), "season_" + TimeCard.SeasonCode(_model.Season).ToLowerInvariant());
        }
    }

    private void OnSettingsChanged()
    {
        _kit.SetResidentFollows(_settings.Resident == ResidentTheme.Follow);
        _scaleDebounce = 0;
    }

    // ---------------- 界面缩放（UI-VISUAL §11.10）：不乘 DPI ----------------
    private void ApplyScale()
    {
        var window = DisplayServer.WindowGetSize();
        if (window.X <= 0 || window.Y <= 0) return;
        var s = Math.Min(window.X / 1920f, window.Y / 1080f);
        var lo = 0.85f / s;
        var hi = Math.Min(window.X / 1280f, window.Y / 720f) / s;
        var user = float.TryParse(Argument("ui-scale"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var forced)
            ? forced : _settings.UiScale;
        GetTree().Root.ContentScaleFactor = Math.Clamp(user, lo, Math.Max(lo, hi));
    }

    // ---------------- 大页 ----------------
    private void OpenSettings(SettingsPage page, bool fromTitle)
    {
        if (_savePage.Visible) CloseSaves();
        _settingsCard.Open(page, fromTitle);
        _audio?.PlayUi(UiCue.PageOpen);
        if (Playing) Input.MouseMode = Input.MouseModeEnum.Visible;
        SyncCorners();
    }

    private void ToggleSettings(SettingsPage page)
    {
        if (_settingsCard.Visible && _settingsCard.Page == page) CloseSettings();
        else OpenSettings(page, fromTitle: !Playing);
    }

    private void CloseSettings()
    {
        if (!_settingsCard.Visible) return;
        _settingsCard.Close();
        _settings.Flush();
        _audio?.PlayUi(UiCue.PageClose);
        if (_flow?.State == AppState.Title) _title.Open();
        SyncCorners();
    }

    private void OpenSaves(bool fromTitle)
    {
        if (_settingsCard.Visible) CloseSettings();
        _savePage.Open(fromTitle);
        _audio?.PlayUi(UiCue.PageOpen);
        if (Playing) Input.MouseMode = Input.MouseModeEnum.Visible;
        SyncCorners();
    }

    private void CloseSaves()
    {
        if (!_savePage.Visible) return;
        _savePage.Close();
        _audio?.PlayUi(UiCue.PageClose);
        if (_flow?.State == AppState.Title) _title.Open();
        SyncCorners();
    }

    private void ToggleTime(bool open)
    {
        _timeOpen = open;
        _audio?.PlayUi(open ? UiCue.PageOpen : UiCue.PageClose);
        SyncCorners();
    }

    /// <summary>"退出"走关闭窗口的同一流程（UI-NAV §5）：交给场景树的关闭请求，由 IAppFlow 保存后退出。</summary>
    private void RequestQuit()
    {
        _settings.Flush();
        GetTree().Root.PropagateNotification((int)NotificationWMCloseRequest);
    }

    private async Task SetMode(ViewMode mode)
    {
        try { await Services.Get<IViewControl>().SetMode(mode, ModeSwitchVia.Ui); }
        catch (InvalidOperationException) { }
        _model.Poke();
    }

    // ---------------- 输入（UI-NAV §5） ----------------
    public override void _Input(InputEvent e)
    {
        if (e is InputEventMouseMotion or InputEventMouseButton or InputEventKey { Pressed: true }) Touch();
        // 改键捕获：下一个按键交给设置卡（输入优先级第 1 层）
        if (_settingsCard.Visible && _settingsCard.Capturing && e is InputEventKey { Pressed: true, Echo: false } key)
        {
            if (_settingsCard.Capture(key) is { } conflict) _toast.Show(conflict, bar: "warning.fill");
            GetViewport().SetInputAsHandled();
        }
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (_flow?.State == AppState.Title)
        {
            if (e.IsActionPressed("menu"))
            {
                if (_settingsCard.Visible) CloseSettings();
                else if (_savePage.Visible) CloseSaves();
                GetViewport().SetInputAsHandled();
            }
            return;
        }
        if (!Playing) return;
        if (e.IsActionPressed("menu"))
        {
            if (_settingsCard.Visible) { CloseSettings(); Handled(); return; }
            if (_savePage.Visible) { CloseSaves(); Handled(); return; }
            if (_timeOpen && InterfaceState) { ToggleTime(false); Handled(); return; }
            if (InterfaceState) { Input.MouseMode = Input.MouseModeEnum.Captured; Handled(); }
            return;   // 漫游态 → 界面态由相机一侧释放鼠标
        }
        if (e is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } && _timeOpen && InterfaceState && !BigPage)
        {
            ToggleTime(false);   // 有面板展开时，第一次单击画面只收起面板
            Handled();
            return;
        }
        if (e is not InputEventKey { Pressed: true }) return;
        if (BigPage)
        {
            // 大页打开时单键快捷键不响应，打开这一页的键除外（输入优先级第 2 层）
            if (e.IsActionPressed("help") && _settingsCard.Visible) { CloseSettings(); Handled(); }
            return;
        }
        if (!_settings.SingleKeyShortcuts && !e.IsActionPressed("menu")) return;
        if (e.IsActionPressed("time_panel"))
        {
            if (!InterfaceState) Input.MouseMode = Input.MouseModeEnum.Visible;
            ToggleTime(!_timeOpen || !InterfaceState);
            Handled();
        }
        else if (e.IsActionPressed("help"))
        {
            OpenSettings(SettingsPage.Keys, fromTitle: false);
            Handled();
        }
        else if (e.IsActionPressed("time_forward", true) && _model.Allows(WorldControlMask.Clock))
        {
            _control.Advance(1);
            _model.Poke();
            var clock = (_model.World.Clock) % 24;
            _toast.Show(UiKit.T("toast.time_forward", ("clock", $"{(int)clock:00}:{(int)(clock % 1 * 60):00}")));
            Handled();
        }
        else if (e.IsActionPressed("toggle_flow") && _model.Allows(WorldControlMask.Flow))
        {
            _control.SetFlow(_model.FlowRate == 0 ? _timeCard.LastFlow : 0);
            _model.Poke();
            _toast.Show(UiKit.T(_model.World.Flow.Rate == 0 ? "toast.flow.paused" : "toast.flow.resumed"), _model.World.Flow.Rate == 0 ? "pause" : null);
            Handled();
        }
        else if (e.IsActionPressed("next_season") && _model.Allows(WorldControlMask.Season))
        {
            _control.SetSeason((Season)(((int)_model.World.SeasonTarget + 1) % 4));
            _model.Poke();
            Handled();
        }
        else if (e.IsActionPressed("weather") && _model.Allows(WorldControlMask.Weather))
        {
            _weatherHeld = true;
            _weatherHold = 0;
            Handled();
        }
        else if (e.IsActionPressed("clean_view"))
        {
            _clean = !_clean;
            Handled();
        }
    }

    public override void _Notification(int what)
    {
        if (what == NotificationApplicationFocusOut && Playing && Input.MouseMode == Input.MouseModeEnum.Captured)
            Input.MouseMode = Input.MouseModeEnum.Visible;   // 失焦时主动切到界面态（UI-NAV §5）
        else if (what == NotificationApplicationFocusIn) _motion.Refresh();
        else if (what == NotificationWMCloseRequest) _settings.Flush();
    }

    public override void _UnhandledKeyInput(InputEvent e)
    {
        // Y 单击：松开时还没满 0.6 s，就切到下一种天气（UI-NAV §4.10）
        if (e is InputEventKey { Pressed: false } && e.IsActionReleased("weather") && _weatherHeld)
        {
            _weatherHeld = false;
            if (_weatherHold < 0.6f) _weather.Next();
            Handled();
        }
    }

    private void Handled() => GetViewport().SetInputAsHandled();
}
