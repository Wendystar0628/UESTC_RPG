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
[KernelService(typeof(IAppStartupFailurePresentation))]
public partial class HudRoot : CanvasLayer, IBootParticipant, IAppLoadingPresentation, IAppScreenCoverage, IAppFrontispiecePresentation,
    IAppStartupFailurePresentation
{
    private HudRootDependencies _dependencies = null!;
    private UiBootOptions _args = new();
    private HudView _view = null!;
    private HudPages _pages = null!;
    private HudResidents _residents = null!;
    private HudInput _input = null!;
    private HudLoading _loading = null!;
    private HudFrontispiece _frontispiece = null!;
    private HudFailure _failure = null!;
    private bool _initialized, _galleryTitle;
    private SettingsPage? _gallerySettings;
    // 节点寿命：_Ready 建、_ExitTree 取消；画廊任务挂在它上面，由 _Process 读结果。
    private CancellationTokenSource _lifetime = null!;
    private Task? _gallery;

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
    Task<bool> IAppStartupFailurePresentation.Show(IAppStartupFailureSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        // 尚未搭好界面（首次进树前）就没有可以画失败页的地方。
        return _initialized ? _failure.Show(session) : Task.FromResult(false);
    }

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
        _lifetime = new CancellationTokenSource();
        _view.Enter();
        _input.Enter();
        _residents.Enter();
        _frontispiece.Enter();
        _loading.Enter();
        _failure.Enter();
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
        _failure = new HudFailure(this, _dependencies, _view, _frontispiece, () => _input.SyncHostInput());
        _input = new HudInput(this, _dependencies, _view, _pages, _residents, () => _loading.Active, () => _failure.Active);
        _initialized = true;
    }

    public override void _Process(double delta)
    {
        _view.Tick(delta);
        _residents.Tick(delta, Active, _args.Gallery == "hud");
        _input.Tick(delta);
        _failure.Tick();
        ObserveGallery();
    }

    public override void _Input(InputEvent e) => _input.Input(e);
    public override void _UnhandledInput(InputEvent e) => _input.UnhandledInput(e);

    public override void _ExitTree()
    {
        if (!_initialized) return;
        _lifetime.Cancel();
        _lifetime.Dispose();
        _failure.Exit();
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
                OpenGallerySettings();
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
            return _dependencies.Exit.FinishQuit(error == Error.Ok ? 0 : 2);
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
            case "settings": OpenGallerySettings(SettingsPage.General); break;
            case "graphics": OpenGallerySettings(SettingsPage.Graphics); break;
            case "sound": OpenGallerySettings(SettingsPage.Sound); break;
            case "keys": OpenGallerySettings(SettingsPage.Keys); break;
            case "rebind": OpenGallerySettings(SettingsPage.Rebind); break;
            case "licenses": OpenGallerySettings(SettingsPage.Licenses); break;
            case "saves": _pages.OpenSaves(fromTitle: true); break;
            case "title": _residents.Clean = _galleryTitle = true; _pages.Title.Open(); break;
            case "hud": _dependencies.InputRouter.SetInterfaceMode(true); _residents.ResetIdle(); break;
            case "atlas": break; // The map HUD opens its own gallery after entering Playing.
            case "layout": Widgets.UiLayoutGallery.Create(_view.Kit, _view.PageHost); break;
            case "census": StartGallery(lifetime => Census(3, lifetime)); break;
            case "failure": StartGallery(_ => FailureGallery()); break;
            default: throw new ArgumentException($"Unknown --ui-gallery page: {page}");
        }
    }

    // 设置页画廊拍的是游戏内设置：进地图途中 Entering 会关掉设置页，所以记下要开的页，到 Playing 再开（同 atlas）。
    private void OpenGallerySettings(SettingsPage? page = null)
    {
        _gallerySettings = page ?? _gallerySettings;
        if (_gallerySettings is not { } open || _dependencies.Flow.State != AppState.Playing) return;
        _gallerySettings = null;
        _pages.OpenSettings(open, fromTitle: false);
    }

    private void StartGallery(Func<CancellationToken, Task> run)
    {
        if (_gallery is not null) throw new InvalidOperationException("A UI gallery is already running.");
        _gallery = RunGallery(run, _lifetime.Token);
    }

    // 节点离树取消画廊不算失败，按寿命令牌认取消；其余故障留在任务里，由 _Process 在主线程重抛。
    private static async Task RunGallery(Func<CancellationToken, Task> run, CancellationToken lifetime)
    {
        try { await run(lifetime); }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
    }

    private void ObserveGallery()
    {
        if (_gallery is not { IsCompleted: true } completed) return;
        _gallery = null;
        completed.GetAwaiter().GetResult();
    }

    /// <summary>--ui-gallery=failure：示例失败走真实的启动失败页；没能展示就报错，不静默留空画面。失败页的寿命随本节点 _ExitTree 一起取消。</summary>
    private async Task FailureGallery()
    {
        if (!await _failure.ShowGallery()) throw new InvalidOperationException("The startup failure gallery could not be shown.");
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
