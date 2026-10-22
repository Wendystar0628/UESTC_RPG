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
    public TitleMenu Title { get; }
    public SettingsCard Settings { get; }
    public SavePage Saves { get; }
    public Toast Toast { get; }
    public bool BigPage => Settings.Visible || Saves.Visible;
    public event Action? StateChanged;

    public HudPages(CanvasLayer owner, HudRootDependencies dependencies, HudView view)
    {
        _owner = owner;
        _dependencies = dependencies;
        Toast = Widgets.Toast.Create(view.Kit, view.HintHost);
        Title = TitleMenu.Create(view.Kit, view.PageHost, view.BackgroundHost);
        Title.SavesRequested += () => OpenSaves(fromTitle: true);
        Title.OverwriteRequested += () => OpenSaves(fromTitle: true, SaveIntent.NewGame);
        Title.SettingsRequested += () => OpenSettings(SettingsPage.General, fromTitle: true);
        Title.CreditsRequested += () => OpenSettings(SettingsPage.Licenses, fromTitle: true);
        Title.QuitRequested += RequestQuit;
        Title.Notice += text => Toast.Show(text);
        Settings = SettingsCard.Create(view.Kit, view.PageHost, dependencies.Settings, dependencies.InputRouter);
        Settings.CloseRequested += CloseSettings;
        Settings.QuitRequested += RequestQuit;
        Settings.ToTitleRequested += () => { CloseSettings(); _ = dependencies.Flow.ReturnToTitle(); };
        Saves = SavePage.Create(view.Kit, view.PageHost, view.DialogHost);
        Saves.CloseRequested += CloseSaves;
    }

    public void Exit()
    {
        Settings.Close();
        if (Saves.Visible) ReportSaves(false);
        Saves.Close();
    }

    private void Changed() => StateChanged?.Invoke();

    // ---------------- 大页 ----------------
    public void OpenSettings(SettingsPage page, bool fromTitle)
    {
        if (Saves.Visible) CloseSaves();
        Settings.Open(page, fromTitle);
        _dependencies.Audio.PlayUi(UiCue.PageOpen);
        if (_dependencies.Flow.IsPlaying) _dependencies.InputRouter.SetInterfaceMode(true);
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
        if (Settings.Visible) CloseSettings();
        Saves.Open(fromTitle, intent);
        ReportSaves(true);
        _dependencies.Audio.PlayUi(UiCue.PageOpen);
        if (_dependencies.Flow.IsPlaying) _dependencies.InputRouter.SetInterfaceMode(true);
        Changed();
    }

    public void CloseSaves()
    {
        if (!Saves.Visible) return;
        ReportSaves(false);
        Saves.Close();
        _dependencies.Audio.PlayUi(UiCue.PageClose);
        if (_dependencies.Flow.State == AppState.Title) Title.Open();
        Changed();
    }

    /// <summary>存档页的开关只在这里编排，开着即已建好、可交互，开关时如实上报（IUiSurfaceSink）。</summary>
    private static void ReportSaves(bool open) => Campus.Kernel.Registry.Kernel.Get<IUiSurfaceSink>().Report(UiSurface.SavePage, open);

    /// <summary>"退出"走关闭窗口的同一流程（UI-NAV）：交给场景树的关闭请求，由 IAppFlow 保存后退出。</summary>
    public void RequestQuit()
    {
        _dependencies.Settings.Flush();
        _owner.GetTree().Root.PropagateNotification((int)Node.NotificationWMCloseRequest);
    }

}
