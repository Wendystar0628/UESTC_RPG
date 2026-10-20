using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Audio;
using Campus.Contracts.Startup;
using Campus.Hud.Pages;
using Campus.Hud.Widgets;
using Godot;

namespace Campus.Hud;

/// <summary>The HUD composition boundary; each retained responsibility owns its state and subscriptions.</summary>
[KernelNode("hud")]
[KernelService(typeof(HudRoot))]
[KernelService(typeof(IAppLoadingPresentation))]
[KernelService(typeof(IAppScreenCoverage))]
[KernelService(typeof(IAppFrontispiecePresentation))]
public partial class HudRoot : CanvasLayer, IBootParticipant, IAppLoadingPresentation, IAppScreenCoverage, IAppFrontispiecePresentation
{
    private HudRootDependencies _dependencies = null!;
    private UiBootOptions _args = new();
    private HudView _view = null!;
    private HudPages _pages = null!;
    private HudResidents _residents = null!;
    private HudInput _input = null!;
    private HudLoading _loading = null!;
    private bool _initialized, _galleryTitle;

    public Control MinimapSlot => _view.MinimapSlot;
    public Control BackgroundHost => _view.BackgroundHost;
    public Control SafeFrame => _view.SafeFrame;
    public Control CornerHost => _view.Corners;
    public Control PageHost => _view.PageHost;
    public Control DialogHost => _view.DialogHost;
    public Control HintHost => _view.HintHost;
    public UiKit Ui => _view.Kit;
    internal Font CanvasFont => _view.Kit.Theme.Theme.DefaultFont;
    public bool Active => _loading.Active;
    public bool IsCovered => _loading.IsCovered;
    public float Progress => _loading.Progress;
    bool IAppScreenCoverage.IsCovered => ((IAppScreenCoverage)_loading).IsCovered;
    public IAppLoadingLease Show(EnterRequest? request = null) => _loading.Show(request);
    Task IAppFrontispiecePresentation.Show() => ((IAppFrontispiecePresentation)_loading).Show();
    Task IAppFrontispiecePresentation.Hide(float seconds) => ((IAppFrontispiecePresentation)_loading).Hide(seconds);

    public HudRoot()
    {
        Layer = 10;
        Name = "HudRoot";
        ProcessPriority = FramePriority.Hud;
    }

    public void Bind(HudRootDependencies dependencies)
    {
        ArgumentNullException.ThrowIfNull(dependencies);
        if (_initialized) throw new InvalidOperationException("Bind the HUD before its first tree entry.");
        _dependencies = dependencies;
    }

    public override void _Ready()
    {
        if (!_initialized) CreatePresentation();
        _view.Enter();
        _input.Enter();
        _residents.Enter();
        _loading.Enter();
        _pages.StateChanged += SyncCorners;
        _dependencies.Flow.Changed += OnFlowChanged;
        _dependencies.ThemeClock.Switched += OnThemeSwitched;
        SyncState();
    }

    private void CreatePresentation()
    {
        _view = new HudView(this, _dependencies);
        _pages = new HudPages(this, _dependencies, _view);
        _residents = new HudResidents(_dependencies, _view, _pages);
        _loading = new HudLoading(this, _dependencies, _view, () => _input.SyncHostInput());
        _input = new HudInput(this, _dependencies, _view, _pages, _residents, () => _loading.Active);
        _initialized = true;
    }

    public override void _Process(double delta)
    {
        _view.Tick(delta);
        _residents.Tick(delta, Active, _args.Gallery == "hud");
        _input.Tick(delta);
    }

    public override void _Input(InputEvent e) => _input.Input(e);
    public override void _UnhandledInput(InputEvent e) => _input.UnhandledInput(e);

    public override void _ExitTree()
    {
        if (!_initialized) return;
        _loading.Exit();
        // Stop observing host children before page/resident removal emits child signals.
        _input.Exit();
        _pages.StateChanged -= SyncCorners;
        _dependencies.Flow.Changed -= OnFlowChanged;
        _dependencies.ThemeClock.Switched -= OnThemeSwitched;
        _view.Exit();
        _residents.Exit();
        _pages.Exit();
        RequestReady();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationApplicationFocusIn) _dependencies.Motion.Refresh();
        else if (what == NotificationWMCloseRequest) _dependencies.Settings.Flush();
    }

    private void SyncCorners()
    {
        _residents.Sync();
        _input.SyncHostInput();
    }

    private void SyncState()
    {
        switch (_dependencies.Flow.State)
        {
            case AppState.Title:
                _residents.Release();
                _input.CancelWeatherHold();
                if (!_pages.Title.Visible) _pages.Title.Open();
                _view.MinimapSlot.Visible = false;
                break;
            case AppState.Playing:
                _residents.Build();
                if (!_galleryTitle) _pages.Title.Close();
                if (_args.Gallery == "hud") _dependencies.InputRouter.SetInterfaceMode(true);
                break;
            case AppState.Entering:
            case AppState.Leaving:
                _residents.Build();
                goto case AppState.Quitting;
            case AppState.Boot:
            case AppState.Quitting:
                _pages.Title.Close();
                if (_pages.Settings.Visible) _pages.CloseSettings();
                if (_pages.Saves.Visible) _pages.CloseSaves();
                break;
            default: throw new InvalidOperationException("Unknown application state.");
        }
        SyncCorners();
    }

    private void OnFlowChanged(AppState previous, AppState current)
    {
        if (current == AppState.Playing) _residents.ResetIdle();
        SyncState();
        if (previous == AppState.Boot && current == AppState.Title && _dependencies.Flow.LastError is { } errorKey)
            _pages.Toast.Show(UiKit.T(errorKey), bar: "warning.fill");
    }

    private void OnThemeSwitched(bool dark)
    {
        if (_pages.BigPage || _view.Kit.ResidentFollows) _dependencies.Audio.PlayUi(UiCue.ShiftDayNight);
    }
    public string Id => "hud";
    public int Order => 0;

    /// <summary>启动参数在 BeforeMap 才到：_Ready 时先按默认搭好，这里把 --ui、--ui-scale、--ui-gallery、--ui-export-theme 和交给动效、主题时钟的参数补上。</summary>
    public Task Initialize(BootPhase phase, BootContext context)
    {
        if (phase != BootPhase.BeforeMap) return Task.CompletedTask;
        _args = context.Run.Ui;
        _dependencies.Motion.Apply(_args);
        if (_args.ExportTheme is { } exportPath)
        {
            var error = _view.Kit.Theme.Export(exportPath);
            GD.Print($"UI theme exported: {exportPath} ({error})");
            GetTree().Quit(error == Error.Ok ? 0 : 2);
            return Task.CompletedTask;
        }
        _dependencies.ThemeClock.Apply(_args);
        _view.Configure(_args);
        if (_args.Gallery is { } gallery) CallDeferred(MethodName.OpenGallery, gallery);
        return Task.CompletedTask;
    }

    /// <summary>--ui-gallery=&lt;页名&gt;：启动时直接打开一页，供截图与评审（UI-VISUAL；完整的组件画廊归 35c）。</summary>
    private void OpenGallery(string page)
    {
        switch (page)
        {
            case "settings": _pages.OpenSettings(SettingsPage.General, fromTitle: false); break;
            case "graphics": _pages.OpenSettings(SettingsPage.Graphics, fromTitle: false); break;
            case "sound": _pages.OpenSettings(SettingsPage.Sound, fromTitle: false); break;
            case "keys": _pages.OpenSettings(SettingsPage.Keys, fromTitle: false); break;
            case "rebind": _pages.OpenSettings(SettingsPage.Rebind, fromTitle: false); break;
            case "licenses": _pages.OpenSettings(SettingsPage.Licenses, fromTitle: false); break;
            case "saves": _pages.OpenSaves(fromTitle: true); break;
            case "title": _residents.Clean = _galleryTitle = true; _pages.Title.Open(); break;
            case "hud": _dependencies.InputRouter.SetInterfaceMode(true); _residents.ResetIdle(); break;
            case "layout": Widgets.UiLayoutGallery.Create(_view.Kit, _view.PageHost); break;
            case "census": Census(3); break;
            default: throw new ArgumentException($"Unknown --ui-gallery page: {page}");
        }
    }

    /// <summary>
    /// --ui-gallery=census：数界面占着实例参数槽的件数（<see cref="UiKit.Holders"/>）。先在漫游中数一次，再经 IAppFlow 回到开始页，然后把设置卡、存档页各开关 <paramref name="cycles"/> 次，
    /// 每步记一行日志，最后走正常退出。
    /// </summary>
    private async void Census(int cycles)
    {
        try
        {
            for (var i = 0; i < 600 && !_dependencies.Flow.IsPlaying; i++) await NextFrame();
            for (var i = 0; i < 10; i++) await NextFrame();
            CensusLine("playing");
            await _dependencies.Flow.ReturnToTitle();
            for (var i = 0; i < 10; i++) await NextFrame();
            CensusLine($"title ({_dependencies.Flow.State})");
            for (var i = 1; i <= cycles; i++)
            {
                _pages.OpenSettings(SettingsPage.Graphics, fromTitle: true);
                await NextFrame();
                CensusLine($"settings open #{i}");
                _pages.CloseSettings();
                _pages.OpenSaves(fromTitle: true);
                await NextFrame();
                CensusLine($"saves open #{i}");
                _pages.CloseSaves();
                await NextFrame();
                CensusLine($"closed #{i}");
            }
        }
        finally { _pages.RequestQuit(); }
    }

    private SignalAwaiter NextFrame() => ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

    private void CensusLine(string step) => GD.Print($"UI census - {step}: {_view.Kit.Holders(_view.Root)} items hold shader instance slots");
}
