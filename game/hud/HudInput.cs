using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Hud.Pages;
using Campus.Hud.Widgets;
using Campus.Contracts.Diagnostics;
using Godot;

namespace Campus.Hud;

/// <summary>Owns router registrations, host arbitration, weather hold, and pending photo export.</summary>
internal sealed class HudInput
{
    private readonly IDiagnosticLog _diagnostics;
    private readonly CanvasLayer _owner;
    private readonly HudRootDependencies _dependencies;
    private readonly HudView _view;
    private readonly HudPages _pages;
    private readonly HudResidents _residents;
    private readonly Func<bool> _loading;
    private bool _weatherHeld;
    private float _weatherHold;
    private int _entry;

    public HudInput(CanvasLayer owner, HudRootDependencies dependencies, HudView view, HudPages pages,
        HudResidents residents, Func<bool> loading)
    {
        _diagnostics = dependencies.Diagnostics;
        _owner = owner;
        _dependencies = dependencies;
        _view = view;
        _pages = pages;
        _residents = residents;
        _loading = loading;
    }

    public void Enter()
    {
        _entry++;
        ConnectInput();
        BindHostInput();
    }

    public void Exit()
    {
        _entry++;
        _photoCancellation?.Cancel();
        UnbindHostInput();
        _dependencies.InputRouter.InterfaceModeChanged -= OnInterfaceModeChanged;
        foreach (var layer in _layers) layer.Dispose();
        _layers.Clear();
        CancelWeatherHold();
        ObservePhotoExport();
    }

    public void CancelWeatherHold() => _weatherHeld = false;

    public void Tick(double delta)
    {
        ObservePhotoExport();
        SyncHostInput();
        if (!_weatherHeld) return;
        _weatherHold += (float)delta;
        if (_weatherHold < 0.6f) return;
        _weatherHeld = false;
        _dependencies.WorldControl.SetWeatherAuto();
        _residents.Refresh();
        _pages.Toast.Show(UiKit.T("weather.auto_toast"), "weather_auto");
    }

    private void OnInterfaceModeChanged(bool enabled)
    {
        if (enabled) _residents.ResetIdle();
        _residents.Sync();
        SyncHostInput();
    }
    // ---------------- 输入（UI-NAV）：三层，经 IInputRouter 分发 ----------------
    // 层的先后：数值小的先跑（IInputRouter.RegisterLayer）。第 4 层"漫游"归 WP-06。
    public const int LayerDialog = 100, LayerPage = 200, LayerHud = 300;
    private static readonly string[] Actions = ["menu", "help", "time_panel", "time_forward", "toggle_flow", "next_season", "weather", "clean_view"];
    private readonly List<IDisposable> _layers = [];
    private readonly HashSet<Control> _hostChildren = [];
    private bool _hostInputBound;

    private void ConnectInput()
    {
        if (_layers.Count > 0) return;
        _layers.Add(_dependencies.InputRouter.RegisterLayer("WP-35.dialog", LayerDialog, DialogLayer));
        _layers.Add(_dependencies.InputRouter.RegisterLayer("WP-35.page", LayerPage, PageLayer));
        _layers.Add(_dependencies.InputRouter.RegisterLayer("WP-35.hud", LayerHud, HudLayer));
        _dependencies.InputRouter.InterfaceModeChanged += OnInterfaceModeChanged;
    }

    // 引擎递归禁用保留每个控件自身的 FocusMode/MouseFilter，重新启用时无需恢复镜像。
    public void SyncHostInput()
    {
        if (_photoCancellation is { IsCancellationRequested: false } photo && (!_dependencies.Flow.IsPlaying || _loading()))
            photo.Cancel();
        if (!_hostInputBound) return;
        var dialog = HasVisibleContent(_view.DialogHost);
        var page = HasVisibleContent(_view.PageHost);
        _view.Corners.Visible = _dependencies.Flow.IsPlaying && !_loading() && !_residents.Clean && !dialog && !page;
        UiLayout.SetInteractive(_view.DialogHost, dialog && _view.Root.Visible && !_loading());
        UiLayout.SetInteractive(_view.PageHost, !dialog && _view.Root.Visible && !_loading());
        UiLayout.SetInteractive(_view.Corners, !dialog && !page && _dependencies.InputRouter.InterfaceMode && _view.Root.Visible && !_loading());
        // 开始菜单与设置/存档共用页面层，大页打开时菜单退出阅读顺序。
        UiLayout.SetInteractive(_pages.Title, !_pages.BigPage && !dialog && !_loading());
    }

    private void BindHostChild(Node child)
    {
        if (!_hostInputBound || child is not Control control || !_hostChildren.Add(control)) return;
        control.VisibilityChanged += SyncHostInput;
    }

    private void UnbindHostChild(Node child)
    {
        if (child is Control control && _hostChildren.Remove(control))
            control.VisibilityChanged -= SyncHostInput;
    }

    private void BindHostInput()
    {
        if (_hostInputBound) return;
        _hostInputBound = true;
        foreach (var host in new[] { _view.PageHost, _view.DialogHost })
        {
            host.ChildOrderChanged += SyncHostInput;
            host.ChildEnteredTree += BindHostChild;
            host.ChildExitingTree += UnbindHostChild;
            foreach (var child in host.GetChildren()) BindHostChild(child);
        }
    }

    private void UnbindHostInput()
    {
        if (!_hostInputBound) return;
        // 先停止仲裁，再解除所有观察，最后才由 _ExitTree 清理页面和居民件。
        _hostInputBound = false;
        foreach (var host in new[] { _view.PageHost, _view.DialogHost })
        {
            host.ChildOrderChanged -= SyncHostInput;
            host.ChildEnteredTree -= BindHostChild;
            host.ChildExitingTree -= UnbindHostChild;
        }
        foreach (var child in _hostChildren) child.VisibilityChanged -= SyncHostInput;
        _hostChildren.Clear();
    }

    private static bool HasVisibleContent(Control host) => host.GetChildren()
        .OfType<Control>().Any(child => !child.IsQueuedForDeletion() && child.IsVisibleInTree());

    /// <summary>漫游态 ⇄ 界面态：经 IInputRouter.SetInterfaceMode，由 WP-06 统一改鼠标捕获。</summary>
    private void SetInterface(bool on) => _dependencies.InputRouter.SetInterfaceMode(on);

    // 第 1 层：改键捕获期间吞掉所有动作；具体按下的物理键由 _Input 交给设置卡
    private bool DialogLayer(InputActionEvent e) => _loading() || _pages.Settings.Visible && _pages.Settings.Capturing;

    // 第 2 层：大页（设置卡、存档页，含从开始页打开的）：Esc 关页，打开这一页的 H 再按一次关页，其余动作一律吞掉
    private bool PageLayer(InputActionEvent e)
    {
        if (!_pages.BigPage) return false;
        if (e.Pressed && !e.Echo)
        {
            switch (e.Action)
            {
                case "menu":
                    if (_pages.Settings.Visible) _pages.CloseSettings();
                    else if (!_pages.Saves.DismissConfirm()) _pages.CloseSaves();
                    break;
                case "help":
                    if (_pages.Settings.Visible) _pages.CloseSettings();
                    break;
            }
        }
        return true;
    }

    // 第 3 层：普通 HUD：Esc 在漫游态与界面态之间切换（有面板展开时先收面板），单键快捷键受总开关约束
    private bool HudLayer(InputActionEvent e)
    {
        if (_dependencies.Flow.State == AppState.Title) return e.Action == "menu";   // 开始页主菜单上按 Esc 什么也不做
        if (!_dependencies.Flow.IsPlaying) return false;
        if (HasVisibleContent(_view.PageHost) || HasVisibleContent(_view.DialogHost)) return false;
        if (e.Action == "weather" && !e.Pressed)
        {
            // Y 单击：松开时还没满 0.6 s，就切到下一种天气（UI-NAV）
            if (!_weatherHeld) return false;
            _weatherHeld = false;
            if (_weatherHold < 0.6f) _residents.NextWeather();
            return true;
        }
        if (!e.Pressed) return false;
        if (e.Action == "menu")
        {
            if (e.Echo) return true;
            if (_residents.TimeOpen && _dependencies.InputRouter.InterfaceMode) _residents.ToggleTime(false);
            else SetInterface(!_dependencies.InputRouter.InterfaceMode);
            return true;
        }
        // F12 不是单字符键，不受单键快捷键总开关约束（UI-NAV）；纯净模式下同样可用
        if (e.Action == "photo_export")
        {
            if (!e.Echo && !_photoPending)
            {
                ObservePhotoExport();
                _photoExport = ExportPhoto();
            }
            return true;
        }
        if (!_dependencies.Settings.SingleKeyShortcuts) return false;
        // 只有 . 可以按住连续前进；其余键的自动重复直接吞掉
        if (e.Echo && e.Action != "time_forward") return Array.IndexOf(Actions, e.Action) >= 0;
        return Shortcut(e.Action);
    }

    private bool Shortcut(string action)
    {
        switch (action)
        {
            case "time_panel":
                var wasInterface = _dependencies.InputRouter.InterfaceMode;
                if (!wasInterface) SetInterface(true);
                _residents.ToggleTime(!_residents.TimeOpen || !wasInterface);
                return true;
            case "help":
                _pages.OpenSettings(SettingsPage.Keys, fromTitle: false);
                return true;
            case "time_forward":
                if (!_dependencies.Model.Allows(WorldControlMask.Clock)) return false;
                _dependencies.WorldControl.Advance(1);
                _residents.Refresh();
                var clock = _dependencies.Model.World.Clock % 24;
                _pages.Toast.Show(UiKit.T("toast.time_forward", ("clock", WorldTime.FormatClock(clock))));
                return true;
            case "toggle_flow":
                if (!_dependencies.Model.Allows(WorldControlMask.Flow)) return false;
                _dependencies.WorldControl.SetFlow(_dependencies.Model.World.BaseFlow.Rate == 0 ? _dependencies.Model.World.LastFlowRate : 0);
                _residents.Refresh();
                _pages.Toast.Show(UiKit.T(_dependencies.Model.World.BaseFlow.Rate == 0 ? "toast.flow.paused" : "toast.flow.resumed"), _dependencies.Model.World.BaseFlow.Rate == 0 ? "pause" : null);
                return true;
            case "next_season":
                if (!_dependencies.Model.Allows(WorldControlMask.Season)) return false;
                _dependencies.WorldControl.SetSeason((Season)(((int)_dependencies.Model.World.SeasonTarget + 1) % 4));
                _residents.Refresh();
                return true;
            case "weather":
                if (!_dependencies.Model.Allows(WorldControlMask.Weather)) return false;
                _weatherHeld = true;
                _weatherHold = 0;
                return true;
            case "clean_view":
                _residents.Clean = !_residents.Clean;
                return true;
            default:
                return false;
        }
    }

    public void Input(InputEvent e)
    {
        if (_loading()) { Handled(); return; }
        if (e is InputEventMouseMotion or InputEventMouseButton or InputEventKey { Pressed: true }) _residents.Touch();
        // 改键捕获：下一个物理键交给设置卡（输入优先级第 1 层）
        if (_pages.Settings.Visible && _pages.Settings.Capturing && e is InputEventKey { Pressed: true, Echo: false } key)
        {
            if (_pages.Settings.Capture(key) is { } conflict) _pages.Toast.Show(conflict, bar: "warning.fill");
            Handled();
        }
    }

    public void UnhandledInput(InputEvent e)
    {
        // 有面板展开时，第一次单击画面只收起面板（UI-NAV）；单击是鼠标事件，不走动作路由
        if (_dependencies.Flow.IsPlaying && e is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } && _residents.TimeOpen && _dependencies.InputRouter.InterfaceMode
            && !HasVisibleContent(_view.PageHost) && !HasVisibleContent(_view.DialogHost))
        {
            _residents.ToggleTime(false);
            Handled();
            return;
        }
    }

    private bool _photoPending;
    private Task? _photoExport;
    private CancellationTokenSource? _photoCancellation;

    private void ObservePhotoExport()
    {
        if (_photoExport is not { IsCompleted: true } completed) return;
        _photoExport = null;
        completed.GetAwaiter().GetResult();
    }

    // 照片导出（UI-NAV）：后端忙、不可用时不提示；成功或失败各出一条临时提示
    private async Task ExportPhoto()
    {
        if (_photoPending || !_dependencies.Flow.IsPlaying || _loading()) return;
        _photoPending = true;
        var entry = _entry;
        using var cancellation = new CancellationTokenSource();
        _photoCancellation = cancellation;
        void OnFlowChanged(AppState previous, AppState next)
        {
            if (next != AppState.Playing) cancellation.Cancel();
        }
        _dependencies.Flow.Changed += OnFlowChanged;
        try
        {
            CaptureResult result;
            try { result = await _dependencies.Capture.CaptureAsync(new(HideUi: true), cancellation.Token); }
            catch (OperationCanceledException) { return; }
            catch (Exception error)
            {
                PhotoFailed(error, entry);
                return;
            }
            if (entry != _entry || !GodotObject.IsInstanceValid(_owner) || !_owner.IsInsideTree()) return;
            switch (result.Status)
            {
                case CaptureStatus.Saved:
                    var folder = result.Path is { Length: > 0 } path ? System.IO.Path.GetDirectoryName(path) ?? path : "";
                    _pages.Toast.Show(UiKit.T("toast.photo.saved", ("folder", folder)));
                    break;
                case CaptureStatus.Failed:
                    _pages.Toast.Show(UiKit.T("toast.photo.failed"), bar: "warning.fill");
                    break;
                case CaptureStatus.Busy:
                case CaptureStatus.Unavailable:
                    break;
                default: throw new ArgumentOutOfRangeException(nameof(result.Status));
            }
        }
        finally
        {
            _dependencies.Flow.Changed -= OnFlowChanged;
            _photoCancellation = null;
            _photoPending = false;
        }
    }

    private void PhotoFailed([FailureHandler("UI-NAV")] Exception error, int entry)
    {
        _diagnostics.Warning("[HUD] Photo export failed.", error);
        if (entry != _entry || !GodotObject.IsInstanceValid(_owner) || !_owner.IsInsideTree()) return;
        _pages.Toast.Show(UiKit.T("toast.photo.failed"), bar: "warning.fill");
    }

    private void Handled() => _owner.GetViewport().SetInputAsHandled();
}
