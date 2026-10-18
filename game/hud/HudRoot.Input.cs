using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Hud.Pages;
using Campus.Hud.Widgets;
using Godot;
using Services = Campus.Kernel.Registry.Kernel;

namespace Campus.Hud;

public partial class HudRoot
{
    // ---------------- 输入（UI-NAV）：三层，经 IInputRouter 分发 ----------------
    // 层的先后：数值小的先跑（IInputRouter.RegisterLayer）。第 4 层"漫游"归 WP-06。
    public const int LayerDialog = 100, LayerPage = 200, LayerHud = 300;
    private static readonly string[] Actions = ["menu", "help", "time_panel", "time_forward", "toggle_flow", "next_season", "weather", "clean_view"];
    private readonly List<IDisposable> _layers = [];
    private IInputRouter _router = null!;

    private void ConnectInput()
    {
        if (_layers.Count > 0) return;
        _router = Services.Get<IInputRouter>();
        _layers.Add(_router.RegisterLayer("WP-35.dialog", LayerDialog, DialogLayer));
        _layers.Add(_router.RegisterLayer("WP-35.page", LayerPage, PageLayer));
        _layers.Add(_router.RegisterLayer("WP-35.hud", LayerHud, HudLayer));
        _router.InterfaceModeChanged += OnInterfaceModeChanged;
    }

    private void Touch() => _idle = 0;

    /// <summary>漫游态 ⇄ 界面态：经 IInputRouter.SetInterfaceMode，由 WP-06 统一改鼠标捕获。</summary>
    private void SetInterface(bool on) => _router.SetInterfaceMode(on);

    // 第 1 层：改键捕获期间吞掉所有动作；具体按下的物理键由 _Input 交给设置卡
    private bool DialogLayer(InputActionEvent e) => _settingsCard.Visible && _settingsCard.Capturing;

    // 第 2 层：大页（设置卡、存档页，含从开始页打开的）：Esc 关页，打开这一页的 H 再按一次关页，其余动作一律吞掉
    private bool PageLayer(InputActionEvent e)
    {
        if (!BigPage) return false;
        if (e.Pressed && !e.Echo)
        {
            switch (e.Action)
            {
                case "menu":
                    if (_settingsCard.Visible) CloseSettings();
                    else if (!_savePage.DismissConfirm()) CloseSaves();
                    break;
                case "help":
                    if (_settingsCard.Visible) CloseSettings();
                    break;
            }
        }
        return true;
    }

    // 第 3 层：普通 HUD：Esc 在漫游态与界面态之间切换（有面板展开时先收面板），单键快捷键受总开关约束
    private bool HudLayer(InputActionEvent e)
    {
        if (_flow.State == AppState.Title) return e.Action == "menu";   // 开始页主菜单上按 Esc 什么也不做
        if (!Playing) return false;
        if (e.Action == "weather" && !e.Pressed)
        {
            // Y 单击：松开时还没满 0.6 s，就切到下一种天气（UI-NAV）
            if (!_weatherHeld) return false;
            _weatherHeld = false;
            if (_weatherHold < 0.6f) _resident?.Weather.Next();
            return true;
        }
        if (!e.Pressed) return false;
        if (e.Action == "menu")
        {
            if (e.Echo) return true;
            if (_timeOpen && InterfaceState) ToggleTime(false);
            else SetInterface(!InterfaceState);
            return true;
        }
        if (!_settings.SingleKeyShortcuts) return false;
        // 只有 . 可以按住连续前进；其余键的自动重复直接吞掉
        if (e.Echo && e.Action != "time_forward") return Array.IndexOf(Actions, e.Action) >= 0;
        switch (e.Action)
        {
            case "time_panel":
                var wasInterface = InterfaceState;
                if (!wasInterface) SetInterface(true);
                ToggleTime(!_timeOpen || !wasInterface);
                return true;
            case "help":
                OpenSettings(SettingsPage.Keys, fromTitle: false);
                return true;
            case "time_forward":
                if (!_model.Allows(WorldControlMask.Clock)) return false;
                _control.Advance(1);
                RefreshHud();
                var clock = _model.World.Clock % 24;
                _toast.Show(UiKit.T("toast.time_forward", ("clock", WorldTime.FormatClock(clock))));
                return true;
            case "toggle_flow":
                if (!_model.Allows(WorldControlMask.Flow)) return false;
                _control.SetFlow(_model.World.BaseFlow.Rate == 0 ? _model.World.LastFlowRate : 0);
                RefreshHud();
                _toast.Show(UiKit.T(_model.World.BaseFlow.Rate == 0 ? "toast.flow.paused" : "toast.flow.resumed"), _model.World.BaseFlow.Rate == 0 ? "pause" : null);
                return true;
            case "next_season":
                if (!_model.Allows(WorldControlMask.Season)) return false;
                _control.SetSeason((Season)(((int)_model.World.SeasonTarget + 1) % 4));
                RefreshHud();
                return true;
            case "weather":
                if (!_model.Allows(WorldControlMask.Weather)) return false;
                _weatherHeld = true;
                _weatherHold = 0;
                return true;
            case "clean_view":
                _clean = !_clean;
                return true;
            default:
                return false;
        }
    }

    public override void _Input(InputEvent e)
    {
        if (e is InputEventMouseMotion or InputEventMouseButton or InputEventKey { Pressed: true }) Touch();
        // 改键捕获：下一个物理键交给设置卡（输入优先级第 1 层）
        if (_settingsCard.Visible && _settingsCard.Capturing && e is InputEventKey { Pressed: true, Echo: false } key)
        {
            if (_settingsCard.Capture(key) is { } conflict) _toast.Show(conflict, bar: "warning.fill");
            Handled();
        }
    }

    public override void _UnhandledInput(InputEvent e)
    {
        // 有面板展开时，第一次单击画面只收起面板（UI-NAV）；单击是鼠标事件，不走动作路由
        if (Playing && e is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } && _timeOpen && InterfaceState && !BigPage)
        {
            ToggleTime(false);
            Handled();
            return;
        }
    }

    private void Handled() => GetViewport().SetInputAsHandled();
}
