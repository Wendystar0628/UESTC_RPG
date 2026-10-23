using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Audio;
using Campus.Hud.Pages;
using Campus.Hud.Widgets;
using Godot;

namespace Campus.Hud;

/// <summary>Owns title/settings/save page state and page transitions.</summary>
internal sealed class HudPages
{
    private readonly CanvasLayer _owner;
    private readonly HudRootDependencies _dependencies;
    private readonly Control _host;
    // 界面发起的流程操作（进入、回标题）与画质应用各一个持有者，随 HUD 离树取消（Exit）。
    private readonly HudTask _flow, _quality;
    public TitleMenu Title { get; }
    public SettingsCard Settings { get; }
    public SavePage Saves { get; }
    public Toast Toast { get; }
    public bool BigPage => Settings.IsVisibleInTree() || Saves.IsVisibleInTree();
    public event Action? StateChanged;

    public HudPages(CanvasLayer owner, HudRootDependencies dependencies, HudView view)
    {
        _owner = owner;
        _dependencies = dependencies;
        _host = view.PageHost;
        _flow = new HudTask(dependencies.Failure);
        _quality = new HudTask(dependencies.Failure);
        Toast = Widgets.Toast.Create(view.Kit, view.HintHost);
        Title = TitleMenu.Create(view.Kit, view.PageHost, view.BackgroundHost, _flow);
        Title.SavesRequested += () => OpenSaves(fromTitle: true);
        Title.OverwriteRequested += () => OpenSaves(fromTitle: true, SaveIntent.NewGame);
        Title.SettingsRequested += () => OpenSettings(SettingsPage.General, fromTitle: true);
        Title.QuitRequested += RequestQuit;
        Title.Notice += text => Toast.Show(text);
        Settings = SettingsCard.Create(view.Kit, view.PageHost, dependencies.Settings, dependencies.InputRouter, _quality);
        Settings.CloseRequested += CloseSettings;
        Settings.QuitRequested += RequestQuit;
        Settings.ToTitleRequested += ReturnToTitle;
        Saves = SavePage.Create(view.Kit, view.PageHost, view.DialogHost, _flow);
        Saves.CloseRequested += CloseSaves;
    }

    public void Exit()
    {
        _flow.Cancel();
        _quality.Cancel();
        Settings.Close();
        Saves.Close();
    }

    private void Changed() => StateChanged?.Invoke();

    // ---------------- 大页 ----------------
    // 页宿主看不见（--ui=0）时不开大页：看不见的页不能占输入、上报界面面或申请光标（UI-NAV）。
    public void OpenSettings(SettingsPage page, bool fromTitle)
    {
        if (!_host.IsVisibleInTree()) return;
        if (Saves.Visible) CloseSaves();
        Settings.Open(page, fromTitle);
        _dependencies.Audio.PlayUi(UiCue.PageOpen);
        Changed();
    }

    public void ToggleSettings(SettingsPage page)
    {
        if (Settings.Visible && Settings.Page == page) CloseSettings();
        else OpenSettings(page, fromTitle: !_dependencies.Flow.IsPlaying);
    }

    public void CloseSettings()
    {
        if (!Settings.Visible) return;
        Settings.Close();
        _dependencies.Settings.Flush();
        _dependencies.Audio.PlayUi(UiCue.PageClose);
        if (_dependencies.Flow.State == AppState.Title) Title.Open();
        Changed();
    }

    public void OpenSaves(bool fromTitle, SaveIntent intent = SaveIntent.Browse)
    {
        if (!_host.IsVisibleInTree()) return;
        if (Settings.Visible) CloseSettings();
        Saves.Open(fromTitle, intent);
        _dependencies.Audio.PlayUi(UiCue.PageOpen);
        Changed();
    }

    public void CloseSaves()
    {
        if (!Saves.Visible) return;
        Saves.Close();
        _dependencies.Audio.PlayUi(UiCue.PageClose);
        if (_dependencies.Flow.State == AppState.Title) Title.Open();
        Changed();
    }

    // 回标题由 IAppFlow 持有（SYS-TITLE），界面只等它结束；进入或回标题正在进行时不再开第二次。
    private void ReturnToTitle()
    {
        CloseSettings();
        _flow.Start(token => _dependencies.Flow.ReturnToTitle().WaitAsync(token));
    }

    /// <summary>"退出"走关闭窗口的同一流程（UI-NAV）：交给场景树的关闭请求，由 IAppFlow 保存后退出。</summary>
    public void RequestQuit()
    {
        _dependencies.Settings.Flush();
        _owner.GetTree().Root.PropagateNotification((int)Node.NotificationWMCloseRequest);
    }

}
