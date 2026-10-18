using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Audio;
using Campus.Contracts.Maps;
using Campus.Contracts.Startup;
using Campus.Hud.Motion;
using Campus.Hud.Pages;
using Campus.Hud.Style;
using Campus.Hud.Widgets;
using Godot;
using Services = Campus.Kernel.Registry.Kernel;

namespace Campus.Hud;

/// <summary>
/// 界面根（UI-VISUAL、UI-NAV）：CanvasLayer 10，搭控件树、挂唯一的 Theme。
/// 漫游态（鼠标被捕获）只留小地图槽位和临时提示；界面态（鼠标可见）四角出现，4 秒没有操作后淡出；
/// 大页（设置卡、存档页）打开时占用观者输入；开始页只在 IAppFlow 的 Title 状态出现。
/// 不显示的页不留件（引擎只在释放节点时归还实例参数槽，<see cref="UiKit.Holders"/>）：大页与开始页打开时建、关上就释放，
/// 四角的常驻件在进入漫游时建、回到开始页时释放。
/// </summary>
[KernelNode("hud", Owner = "WP-35")]
[KernelService(typeof(HudRoot), Owner = "WP-35")]
public partial class HudRoot : CanvasLayer, IBootParticipant
{
    private const float IdleFade = 4f;

    private HudArguments _args = HudArguments.None;
    private UiThemeBuilder _theme = null!;

    private UiKit _kit = null!;
    private HudSettings _settings = null!;
    private HudModel _model = null!;
    private MotionPolicy _motion = null!;
    private IWorldControl _control = null!;
    private IAppFlow _flow = null!;
    private IAudioService _audio = null!;
    private UiThemeClock _clock = null!;
    private Control _root = null!, _corners = null!, _minimapSlot = null!;
    private Resident? _resident;
    private PerfReadout? _perf;
    private Toast _toast = null!;
    private SettingsCard _settingsCard = null!;
    private SavePage _savePage = null!;
    private TitleMenu _title = null!;
    private bool _timeOpen = true, _clean, _hidden, _weatherHeld, _primed, _galleryTitle;   // 时间卡在界面态默认展开，T 收起（UI-VISUAL）
    private float _idle, _weatherHold, _scaleDebounce = -1;
    private ViewMode _lastMode;
    private Season _lastSeason;
    private bool _initialized, _bound;

    public Control MinimapSlot => _minimapSlot;

    public HudRoot()
    {
        Layer = 10;
        Name = "HudRoot";
        ProcessPriority = FramePriority.Hud;
    }

    public override void _Ready()
    {
        if (_initialized)
        {
            ConnectInput();
            BindEvents();
            _kit.SetResidentFollows(_settings.Resident == ResidentTheme.Follow);
            _primed = false;
            _lastMode = _model.Mode;
            _lastSeason = _model.Season;
            ApplyScale();
            Layout();
            SyncState();
            return;
        }
        _settings = Services.Get<HudSettings>();
        _model = Services.Get<HudModel>();
        _motion = Services.Get<MotionPolicy>();
        _control = Services.Get<IWorldControl>();
        _flow = Services.Get<IAppFlow>();
        _audio = Services.Get<IAudioService>();

        var tokens = UiTokens.Load();
        var colors = UiColors.Load();
        var materials = new UiMaterials(tokens, colors);
        var theme = new UiThemeBuilder(tokens, colors);
        _clock = Services.Get<UiThemeClock>();
        _kit = new UiKit(tokens, colors, materials, theme, _motion, _clock);
        _theme = theme;

        AddChild(_clock);

        _root = new Control
        {
            Name = "Root",
            Theme = theme.Theme,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            TextureFilter = CanvasItem.TextureFilterEnum.LinearWithMipmaps
        };
        AddChild(_root);
        _root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        _minimapSlot = new Control { Name = "MinimapSlot", MouseFilter = Control.MouseFilterEnum.Ignore, Size = tokens.V2("metrics", "size", "minimap") };
        _root.AddChild(_minimapSlot);
        _corners = new Control { Name = "Corners", MouseFilter = Control.MouseFilterEnum.Ignore };
        _root.AddChild(_corners);
        _corners.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        _title = TitleMenu.Create(_kit, _root);
        _title.SavesRequested += () => OpenSaves(fromTitle: true);
        _title.OverwriteRequested += () => OpenSaves(fromTitle: true, SaveIntent.NewGame);
        _title.SettingsRequested += () => OpenSettings(SettingsPage.General, fromTitle: true);
        _title.CreditsRequested += () => OpenSettings(SettingsPage.Licenses, fromTitle: true);
        _title.QuitRequested += RequestQuit;
        _title.Notice += text => _toast.Show(text);
        ConnectInput();
        _settingsCard = SettingsCard.Create(_kit, _root, _settings, _router);
        _settingsCard.CloseRequested += CloseSettings;
        _settingsCard.QuitRequested += RequestQuit;
        _settingsCard.ToTitleRequested += () => { CloseSettings(); _ = _flow.ReturnToTitle(); };
        _savePage = SavePage.Create(_kit, _root);
        _savePage.CloseRequested += CloseSaves;
        _toast = Toast.Create(_kit, _root);

        _initialized = true;
        BindEvents();
        _kit.SetResidentFollows(_settings.Resident == ResidentTheme.Follow);
        _lastMode = _model.Mode;
        _lastSeason = _model.Season;
        ApplyScale();
        Layout();
        SyncState();
    }

    public override void _Process(double delta)
    {
        _settings.Tick(delta);
        _model.Tick(delta);
        if (_scaleDebounce >= 0 && (_scaleDebounce += (float)delta) >= 0.2f) { _scaleDebounce = -1; ApplyScale(); }
        SyncCorners();
        // 自动隐藏只作用于没人在用的普通 HUD（UI-NAV）：焦点、大页、改键、读屏都让它保持显示
        if (Playing && InterfaceState && !BigPage && _root.GetViewport().GuiGetFocusOwner() is null && DisplayServer.AccessibilityScreenReaderActive() == 0)
            _idle += (float)delta;
        else _idle = 0;
        var target = _idle >= IdleFade ? 0f : 1f;
        var step = (float)delta / (target < _corners.Modulate.A ? 1.2f : 0.18f);
        var a = Mathf.MoveToward(_corners.Modulate.A, target, _motion.Level == MotionLevel.Instant ? 1 : step);
        _corners.Modulate = new Color(1, 1, 1, a);
        if (_weatherHeld)
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

    public override void _ExitTree()
    {
        if (_bound)
        {
            _bound = false;
            _clock.Switched -= OnThemeSwitched;
            _root.Resized -= Layout;
            _settings.Changed -= OnSettingsChanged;
            _flow.Changed -= OnFlowChanged;
            _model.Changed -= OnModelChanged;
        }
        _router.InterfaceModeChanged -= OnInterfaceModeChanged;
        foreach (var layer in _layers) layer.Dispose();
        _layers.Clear();
        if (_initialized)
        {
            _settingsCard.Close();
            _savePage.Close();
            ReleaseResident();
            FreePerf();
            _weatherHeld = false;
            // 同实例重新入树时复用根控件，恢复订阅并重建已释放的四角。
            RequestReady();
        }
    }

    public override void _Notification(int what)
    {
        // 失焦切到界面态归 WP-06 的 IInputRouter
        if (what == NotificationApplicationFocusIn) _motion.Refresh();
        else if (what == NotificationWMCloseRequest) _settings.Flush();
    }
}
