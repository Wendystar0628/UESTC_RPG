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
public partial class HudRoot : CanvasLayer, IBootParticipant, IAppLoadingPresentation, IAppScreenCoverage, IAppFrontispiecePresentation, IRegistryExitParticipant
{
    private HudRootDependencies _dependencies = null!;
    private UiBootOptions _args = new();
    private HudView _view = null!;
    private HudPages _pages = null!;
    private HudResidents _residents = null!;
    private HudInput _input = null!;
    private HudLoading _loading = null!;
    private HudFrontispiece _frontispiece = null!;
    private bool _initialized, _galleryTitle, _galleryChecks;
    // 游戏内大页的画廊：进地图途中 Entering 会关掉大页，所以记下要开的页，到 Playing 再开（同 atlas）。
    private Action? _galleryInGame;
    // 画廊任务的持有者：节点离树取消（不算失败），其余失败交 IAppFailure。
    private HudTask _gallery = null!;

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
    bool IAppScreenCoverage.IsCovered => _loading.IsCovered || _frontispiece.IsCovered;
    public IAppLoadingLease Show(EnterRequest? request = null) => _loading.Show(request);
    async Task IAppFrontispiecePresentation.Show()
    {
        await ((IAppFrontispiecePresentation)_frontispiece).Show();
        SyncTitleTheme();
    }
    Task IAppFrontispiecePresentation.Hide(float seconds) => ((IAppFrontispiecePresentation)_frontispiece).Hide(seconds);

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
        _frontispiece.Enter();
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
        _frontispiece = new HudFrontispiece(this, _dependencies, _view);
        _loading = new HudLoading(this, _dependencies, _view, () => _input.SyncHostInput());
        _input = new HudInput(this, _dependencies, _view, _pages, _residents, () => _loading.Active);
        _gallery = new HudTask(_dependencies.Failure);
        _initialized = true;
    }

    public override void _Process(double delta)
    {
        _view.Tick(delta);
        _residents.Tick(delta);
        _input.Tick(delta);
    }

    public override void _Input(InputEvent e) => _input.Input(e);
    public override void _UnhandledInput(InputEvent e) => _input.UnhandledInput(e);

    // 界面发起的全部等待：应用退出先停接、取消，等它们交付完失败或取消，节点才离树释放界面（ENG-ARCH「关闭顺序」）。
    private HudTask[] Tasks => [_gallery, .. _input.Tasks, .. _pages.Tasks, .. _residents.Tasks];

    public void PrepareForExit()
    {
        if (!_initialized) return;
        ReportUnfinishedGallery();
        foreach (var task in Tasks) task.Stop();
    }

    public Task DrainForExitAsync() => _initialized ? Task.WhenAll(Tasks.Select(task => task.DrainAsync())) : Task.CompletedTask;

    public override void _ExitTree()
    {
        if (!_initialized) return;
        ReportUnfinishedGallery();
        _gallery.Cancel();
        _loading.Exit();
        _frontispiece.Exit();
        // Stop host arbitration before page/resident removal.
        _input.Exit();
        _pages.StateChanged -= SyncCorners;
        _dependencies.Flow.Changed -= OnFlowChanged;
        _dependencies.ThemeClock.Switched -= OnThemeSwitched;
        _view.Exit();
        _residents.Exit();
        _pages.Exit();
        RequestReady();
    }

    // 排版检查没跑完就退出等于没测：截图先于检查收尾时照失败报，不静默取消（GAME-176）；退出排空后已不在跑，离树不重报。
    private void ReportUnfinishedGallery()
    {
        if (_galleryChecks && _gallery.Running)
            _dependencies.Failure.Report(new TimeoutException("The UI gallery layout check did not finish before exit."));
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
                break;
            case AppState.Playing:
                _residents.Build();
                if (!_galleryTitle) _pages.Title.Close();
                OpenGalleryInGame();
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
        SyncTitleTheme();
        SyncCorners();
    }

    /// <summary>开始页期间没有世界，自动主题取浅色（SYS-TITLE）；其余状态按世界太阳高度。</summary>
    private void SyncTitleTheme() => _dependencies.ThemeClock.SetTitle(_dependencies.Flow.State == AppState.Title);

    private void OnFlowChanged(AppState previous, AppState current)
    {
        SyncState();
        if (current == AppState.Title) _pages.ReportTitleReturn(previous);
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
            // 启动工作只请求关闭、不等应用关闭，否则关闭排空启动任务时两边互等（ENG-ARCH）。
            _dependencies.Exit.RequestQuit(error == Error.Ok ? 0 : 2);
            return Task.CompletedTask;
        }
        _dependencies.ThemeClock.Apply(_args);
        _view.Configure(_args);
        if (_args.Gallery is { } gallery) CallDeferred(MethodName.OpenGallery, gallery);
        return Task.CompletedTask;
    }

    /// <summary>
    /// --ui-gallery=&lt;页名&gt;：启动时直接打开一页，供截图与评审（UI-VISUAL；完整的组件画廊归 35c）。
    /// 设置卡每个页签、存档页、大地图两个页签都有一页，界面轮逐页拍（scripts/shots/ui-poses.txt）；这些大页开着时同时跑排版收敛检查。
    /// </summary>
    private void OpenGallery(string page)
    {
        switch (page)
        {
            case "settings": OpenGalleryInGame(() => _pages.OpenSettings(SettingsPage.General, fromTitle: false)); break;
            case "graphics": OpenGalleryInGame(() => _pages.OpenSettings(SettingsPage.Graphics, fromTitle: false)); break;
            case "sound": OpenGalleryInGame(() => _pages.OpenSettings(SettingsPage.Sound, fromTitle: false)); break;
            case "keys": OpenGalleryInGame(() => _pages.OpenSettings(SettingsPage.Keys, fromTitle: false)); break;
            case "rebind": OpenGalleryInGame(() => _pages.OpenSettings(SettingsPage.Rebind, fromTitle: false)); break;
            case "saves": OpenGalleryInGame(() => _pages.OpenSaves(fromTitle: false)); break;
            case "title": _residents.Clean = _galleryTitle = true; _pages.Title.Open(); break;
            case "hud": break; // The resident corners are always shown in Playing.
            case "atlas" or "atlas-index": CheckGalleryLayout(); break; // The map HUD opens its own gallery after entering Playing.
            case "layout": Widgets.UiLayoutGallery.Create(_view.Kit, _view.PageHost); CheckGalleryLayout(); break;
            case "census": StartGallery(lifetime => Census(3, lifetime)); break;
            default: throw new ArgumentException($"Unknown --ui-gallery page: {page}");
        }
    }

    private void OpenGalleryInGame(Action? open = null)
    {
        if (open is not null)
        {
            _galleryInGame = open;
            CheckGalleryLayout();
        }
        if (_galleryInGame is not { } pending || _dependencies.Flow.State != AppState.Playing) return;
        _galleryInGame = null;
        pending();
    }

    // 大页在 Playing 才开；进了 Playing 再等页面出现、结构落定，然后判尺寸是否反复变化（UiLayoutGallery.AssertSettledAsync）。
    private void CheckGalleryLayout()
    {
        _galleryChecks = true;
        StartGallery(async lifetime =>
        {
            while (!_dependencies.Flow.IsPlaying) await NextFrame(lifetime);
            await Widgets.UiLayoutGallery.AssertSettledAsync(_view.PageHost, lifetime);
        });
    }

    private void StartGallery(Func<CancellationToken, Task> run)
    {
        if (!_gallery.Start(run)) throw new InvalidOperationException("A UI gallery is already running.");
    }

    /// <summary>
    /// --ui-gallery=census：数界面占着实例参数槽的件数（<see cref="UiKit.Holders"/>）。先在漫游中数一次，再经 IAppFlow 回到开始页，然后把设置卡、存档页各开关 <paramref name="cycles"/> 次，
    /// 每步记一行日志，最后走正常退出。
    /// </summary>
    private async Task Census(int cycles, CancellationToken lifetime)
    {
        try
        {
            for (var i = 0; i < 600 && !_dependencies.Flow.IsPlaying; i++) await NextFrame(lifetime);
            for (var i = 0; i < 10; i++) await NextFrame(lifetime);
            CensusLine("playing");
            await _dependencies.Flow.ReturnToTitle();
            for (var i = 0; i < 10; i++) await NextFrame(lifetime);
            CensusLine($"title ({_dependencies.Flow.State})");
            for (var i = 1; i <= cycles; i++)
            {
                _pages.OpenSettings(SettingsPage.Graphics, fromTitle: true);
                await NextFrame(lifetime);
                CensusLine($"settings open #{i}");
                _pages.CloseSettings();
                _pages.OpenSaves(fromTitle: true);
                await NextFrame(lifetime);
                CensusLine($"saves open #{i}");
                _pages.CloseSaves();
                await NextFrame(lifetime);
                CensusLine($"closed #{i}");
            }
        }
        // 走完或出错都正常退出；节点已离树（被取消）时页面已拆，不再请求。
        finally { if (!lifetime.IsCancellationRequested) _pages.RequestQuit(); }
    }

    private Task NextFrame(CancellationToken lifetime) => FrameAwaiter.ProcessAsync(this, lifetime);

    private void CensusLine(string step) => GD.Print($"UI census - {step}: {_view.Kit.Holders(_view.Root)} items hold shader instance slots");
}
