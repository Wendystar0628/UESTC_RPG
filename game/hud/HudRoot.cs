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
[KernelService(typeof(IAppLoadingPresentation), Owner = "WP-35")]
[KernelService(typeof(IAppScreenCoverage), Owner = "WP-35")]
[KernelService(typeof(IAppFrontispiecePresentation), Owner = "WP-35")]
public partial class HudRoot : CanvasLayer, IBootParticipant, IAppLoadingPresentation, IAppScreenCoverage, IAppFrontispiecePresentation
{
    private const float IdleFade = 4f;
    private const double RefreshInterval = 0.1;

    private UiBootOptions _args = new();
    private UiThemeBuilder _theme = null!;

    private UiKit _kit = null!;
    private HudSettings _settings = null!;
    private HudModel _model = null!;
    private MotionPolicy _motion = null!;
    private IWorldControl _control = null!;
    private IAppFlow _flow = null!;
    private IAudioService _audio = null!;
    private UiThemeClock _clock = null!;
    private Control _root = null!, _safeFrame = null!, _corners = null!, _minimapSlot = null!;
    private Control _loadingHost = null!;
    private Control _backgroundHost = null!, _pageHost = null!, _dialogHost = null!, _hintHost = null!;
    private VBoxContainer _topLeft = null!, _topRight = null!, _bottomLeft = null!, _bottomRight = null!;
    private float _cornerAlpha = 1;
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
    private double _refreshClock = RefreshInterval;

    public Control MinimapSlot => _minimapSlot;
    public Control BackgroundHost => _backgroundHost;
    public Control SafeFrame => _safeFrame;
    public Control CornerHost => _corners;
    public Control PageHost => _pageHost;
    public Control DialogHost => _dialogHost;
    public Control HintHost => _hintHost;
    public UiKit Ui => _kit;

    public HudRoot()
    {
        Layer = 10;
        Name = "HudRoot";
        ProcessPriority = FramePriority.Hud;
    }

    public override void _Ready()
    {
        RenderingServer.Singleton.FramePostDraw += OnCoverageDrawn;
        if (_initialized)
        {
            ConnectInput();
            BindHostInput();
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

        // 必需应用边界与普通 HUD 分开显隐；同一 CanvasLayer 内后绘制，--ui=0 也真实覆盖。
        _loadingHost = new Control
        {
            Name = "LoadingHost",
            Theme = theme.Theme,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            TextureFilter = CanvasItem.TextureFilterEnum.LinearWithMipmaps
        };
        AddChild(_loadingHost);
        _loadingHost.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        _frontispiece = new ColorRect { Name = "Frontispiece", Color = colors.Resolve("loading.background"),
            Visible = false, MouseFilter = Control.MouseFilterEnum.Ignore };
        AddChild(_frontispiece);
        MoveChild(_frontispiece, 0);
        _frontispiece.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _backgroundHost = _kit.Layout.Host(_root, "BackgroundHost");
        _safeFrame = _kit.Layout.SafeFrame(_root);
        _corners = _kit.Layout.Host(_safeFrame, "CornerHost");
        // 树顺序就是 Tab 阅读序：左上、右上、左下、右下。
        _topLeft = _kit.Layout.Corner(_corners, Control.LayoutPreset.TopLeft);
        _topRight = _kit.Layout.Corner(_corners, Control.LayoutPreset.TopRight);
        _bottomLeft = _kit.Layout.Corner(_corners, Control.LayoutPreset.BottomLeft);
        _bottomRight = _kit.Layout.Corner(_corners, Control.LayoutPreset.BottomRight);
        _minimapSlot = _kit.Layout.Add(_topLeft, new Control
        {
            Name = "MinimapSlot", CustomMinimumSize = tokens.V2("metrics", "size", "minimap")
        });
        _pageHost = _kit.Layout.Host(_safeFrame, "PageHost");
        _dialogHost = _kit.Layout.Host(_safeFrame, "DialogHost");
        _hintHost = _kit.Layout.Host(_safeFrame, "HintHost");
        UiLayout.SetInteractive(_backgroundHost, false);
        UiLayout.SetInteractive(_hintHost, false);

        _title = TitleMenu.Create(_kit, _pageHost, _backgroundHost);
        _title.SavesRequested += () => OpenSaves(fromTitle: true);
        _title.OverwriteRequested += () => OpenSaves(fromTitle: true, SaveIntent.NewGame);
        _title.SettingsRequested += () => OpenSettings(SettingsPage.General, fromTitle: true);
        _title.CreditsRequested += () => OpenSettings(SettingsPage.Licenses, fromTitle: true);
        _title.QuitRequested += RequestQuit;
        _title.Notice += text => _toast.Show(text);
        ConnectInput();
        _settingsCard = SettingsCard.Create(_kit, _pageHost, _settings, _router);
        _settingsCard.CloseRequested += CloseSettings;
        _settingsCard.QuitRequested += RequestQuit;
        _settingsCard.ToTitleRequested += () => { CloseSettings(); _ = _flow.ReturnToTitle(); };
        _savePage = SavePage.Create(_kit, _pageHost, _dialogHost);
        _savePage.CloseRequested += CloseSaves;
        _toast = Toast.Create(_kit, _hintHost);
        _initialized = true;
        BindHostInput();
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
        if (_scaleDebounce >= 0 && (_scaleDebounce += (float)delta) >= 0.2f) { _scaleDebounce = -1; ApplyScale(); }
        SyncCorners();
        _refreshClock += delta;
        if (_refreshClock >= RefreshInterval) RefreshHud();
        // 只把真正交给玩家、未被遮住的界面时间算作空闲；图库保留待评审的 HUD。
        if (Playing && InterfaceState && _corners.IsVisibleInTree() && !Active
            && _flow.Activity == AppActivity.None && _args.Gallery != "hud" && !BigPage
            && _root.GetViewport().GuiGetFocusOwner() is null && DisplayServer.AccessibilityScreenReaderActive() == 0)
            _idle += (float)delta;
        else _idle = 0;
        var target = _idle >= IdleFade ? 0f : 1f;
        var step = (float)delta / _motion.Fade(target < _cornerAlpha ? 1.2f : 0.18f);
        _cornerAlpha = Mathf.MoveToward(_cornerAlpha, target, _motion.Level == MotionLevel.Instant ? 1 : step);
        FadeResidents(_cornerAlpha, target > 0);
        SyncHostInput();
        if (_weatherHeld)
        {
            _weatherHold += (float)delta;
            if (_weatherHold >= 0.6f)
            {
                _weatherHeld = false;
                _control.SetWeatherAuto();
                RefreshHud();
                _toast.Show(UiKit.T("weather.auto_toast"), "weather_auto");
            }
        }
    }

    public override void _ExitTree()
    {
        RenderingServer.Singleton.FramePostDraw -= OnCoverageDrawn;
        _loadingCovered = _frontispieceCovered = false;
        UnbindHostInput();
        if (_bound)
        {
            _bound = false;
            _clock.Switched -= OnThemeSwitched;
            _root.Resized -= Layout;
            _safeFrame.Resized -= Layout;
            _settings.Changed -= OnSettingsChanged;
            _flow.Changed -= OnFlowChanged;
            _model.World.InstanceChanged -= OnWorldChanged;
            Services.Get<IViewControl>().WalkEntryRefused -= OnWalkEntryRefused;
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
