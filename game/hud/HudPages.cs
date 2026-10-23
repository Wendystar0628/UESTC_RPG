using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Audio;
using Campus.Contracts.Save;
using Campus.Hud.I18n;
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
    // 界面发起的流程操作（进入、回标题）、画质应用与存档操作（立即保存、删除）各一个持有者，随 HUD 离树取消（Exit）。
    private readonly HudTask _flow, _quality, _storage;
    private bool _watchingLeaveSave;
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
        _storage = new HudTask(dependencies.Failure);
        Toast = Widgets.Toast.Create(view.Kit, view.HintHost);
        Title = TitleMenu.Create(view.Kit, view.PageHost, view.BackgroundHost, dependencies.Saves, dependencies.Maps);
        Title.EnterRequested += request => StartEnter(request);
        Title.SavesRequested += () => OpenSaves(fromTitle: true);
        Title.OverwriteRequested += () => OpenSaves(fromTitle: true, SaveIntent.NewGame);
        Title.SettingsRequested += () => OpenSettings(SettingsPage.General, fromTitle: true);
        Title.QuitRequested += RequestQuit;
        Title.Notice += text => Toast.Show(text);
        Settings = SettingsCard.Create(view.Kit, view.PageHost, dependencies.Settings, dependencies.InputRouter, _quality);
        Settings.CloseRequested += CloseSettings;
        Settings.QuitRequested += RequestQuit;
        Settings.ToTitleRequested += ReturnToTitle;
        Saves = SavePage.Create(view.Kit, view.PageHost, view.DialogHost, dependencies.Saves, dependencies.Maps);
        Saves.CloseRequested += CloseSaves;
        Saves.EnterRequested += EnterFromSaves;
        Saves.SaveNowRequested += SaveNow;
        Saves.DeleteRequested += DeleteSave;
    }

    public void Exit()
    {
        StopLeaveSaveWatch();
        _flow.Cancel();
        _quality.Cancel();
        _storage.Cancel();
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
        // 进入已经发出时不重开开始页：选中存档后直接由加载页盖住，不闪回菜单。
        if (_dependencies.Flow.State == AppState.Title && !_flow.Running) Title.Open();
        Changed();
    }

    // ---------------- 立即保存与删除 ----------------
    // 存档页只发请求；写盘与删除由存档服务持有，界面一侧只等结果给回执（UI-NAV 临时提示），进行中不再开第二次。
    private void SaveNow() => _storage.Start(SaveNowCore);

    private async Task SaveNowCore(CancellationToken token)
    {
        var result = await _dependencies.Saves.SaveNow(SaveReason.Manual).WaitAsync(token);
        if (result.Status == SaveStatus.Saved) Toast.Show(UiKit.T("saves.saved"));
        // 没存上：存档服务给了原因键就写原因，否则只写"没存上"（SYS-SAVE-FLOW）。
        else Warn(result.Error is { } key && Tr.Has(key) ? UiKit.T(key) : UiKit.T("saves.save_failed"));
    }

    private void DeleteSave(SlotId slot, ulong? gen) => _storage.Start(token => DeleteCore(slot, gen, token));

    private async Task DeleteCore(SlotId slot, ulong? gen, CancellationToken token)
    {
        var saves = _dependencies.Saves;
        var result = await (gen is { } entry ? saves.DeleteEntry(slot, entry) : saves.DeleteSlot(slot)).WaitAsync(token);
        // 删成功由存档页随 SlotsChanged 重建体现；保存失败的原因文案说的是"没有保存"，删除失败不借用。
        switch (result.Status)
        {
            case DeleteStatus.Deleted: break;
            case DeleteStatus.Rejected: Warn(UiKit.T("save.error.delete_unavailable")); break;
            case DeleteStatus.Failed: Warn(UiKit.T("saves.delete_failed")); break;
            default: throw new InvalidOperationException("Unknown delete status.");
        }
    }

    // ---------------- 进入与回标题 ----------------
    // 进入由 IAppFlow 持有并处置失败（SYS-TITLE）；开始页与存档页只发请求，界面一侧的等待与失败提示只在这里，进行中不再开第二次。
    private bool StartEnter(EnterRequest request) => _flow.Start(token => EnterCore(request, token));

    // 先发请求再关页：关页时看到进入已在进行，就不重开开始页。
    private void EnterFromSaves(EnterRequest request)
    {
        if (StartEnter(request)) CloseSaves();
    }

    private async Task EnterCore(EnterRequest request, CancellationToken token)
    {
        var result = await _dependencies.Flow.Enter(request).WaitAsync(token);
        switch (result)
        {
            case EnterResult.Entered: break;
            case EnterResult.Rejected: break;
            case EnterResult.Failed: Warn(FlowErrorText() ?? UiKit.T("title.enter_failed")); break;
            default: throw new InvalidOperationException("Unknown enter result.");
        }
    }

    /// <summary>
    /// 回到开始页时的一次提示（进入失败由 <see cref="EnterCore"/> 按结果提示，这里不重复）：
    /// 流程留下文案键就提示它（冷启动读档失败等）；回标题时保存失败照常离开，提示继续会回到哪一代。
    /// 保存转后台写盘（Deferred）、开始页出现时还没落定的，由 <see cref="WatchLeaveSave"/> 等它的结果。
    /// </summary>
    public void ReportTitleReturn(AppState previous)
    {
        StopLeaveSaveWatch();
        if (previous == AppState.Entering) return;
        if (FlowErrorText() is { } error) { Warn(error); return; }
        if (previous != AppState.Leaving) return;
        // 还在写就是这次离开的保存，结果没落定，State.Last 可能还是上一次的；写完了 Last 才是这次的结果。
        var saves = _dependencies.Saves;
        if (saves.Busy) WatchLeaveSave();
        else if (saves.State.Last == SaveStatus.Failed) WarnLeaveUnsaved(saves.State);
    }

    // 本次离开的那一次后台写盘：开始页作用域内只订阅到它落定，失败提示一次；
    // 离开开始页（新游戏、继续、退出）或界面离树时随作用域退订（ENG-ARCH）。
    private void WatchLeaveSave()
    {
        _watchingLeaveSave = true;
        _dependencies.Saves.SlotsChanged += OnLeaveSavePublished;
        _dependencies.Flow.Changed += OnLeaveScopeChanged;
    }

    // 开始页上没有世界，存档服务此时只会发布这次写盘的结果（SYS-SAVE-FLOW）：第一次 Saved 或 Failed 即落定。
    private void OnLeaveSavePublished()
    {
        var state = _dependencies.Saves.State;
        if (state.Last is not (SaveStatus.Saved or SaveStatus.Failed)) return;
        StopLeaveSaveWatch();
        if (state.Last == SaveStatus.Failed) WarnLeaveUnsaved(state);
    }

    private void OnLeaveScopeChanged(AppState previous, AppState current)
    {
        if (current != AppState.Title) StopLeaveSaveWatch();
    }

    private void StopLeaveSaveWatch()
    {
        if (!_watchingLeaveSave) return;
        _watchingLeaveSave = false;
        _dependencies.Saves.SlotsChanged -= OnLeaveSavePublished;
        _dependencies.Flow.Changed -= OnLeaveScopeChanged;
    }

    private void WarnLeaveUnsaved(SaveState state) => Warn(state.LastSavedAtUnix is { } unix
        ? UiKit.T("title.leave_unsaved", ("saved", SaveMetaText.SavedAt(unix)))
        : UiKit.T("title.leave_unsaved.no_time"));

    // IAppFlow.LastError 有文案键时用键，否则是原异常的消息，不直接给观者看。
    private string? FlowErrorText() => _dependencies.Flow.LastError is { } key && Tr.Has(key) ? UiKit.T(key) : null;

    private void Warn(string text) => Toast.Show(text, bar: "warning.fill");

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
