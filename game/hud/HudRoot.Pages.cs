using Campus.Contracts.AppFlow;
using Campus.Contracts.Audio;
using Campus.Contracts;
using Campus.Hud.Pages;
using Godot;
using Services = Campus.Kernel.Registry.Kernel;

namespace Campus.Hud;

public partial class HudRoot
{
    // ---------------- 大页 ----------------
    private void OpenSettings(SettingsPage page, bool fromTitle)
    {
        if (_savePage.Visible) CloseSaves();
        _settingsCard.Open(page, fromTitle);
        _audio?.PlayUi(UiCue.PageOpen);
        if (Playing) SetInterface(true);
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
        if (Playing) SetInterface(true);
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

    /// <summary>"退出"走关闭窗口的同一流程（UI-NAV）：交给场景树的关闭请求，由 IAppFlow 保存后退出。</summary>
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
}
