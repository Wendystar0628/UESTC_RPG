using Campus.Contracts.AppFlow;
using Campus.Contracts.Audio;
using Campus.Contracts;
using Campus.Hud.Widgets;
using Godot;
using Services = Campus.Kernel.Registry.Kernel;

namespace Campus.Hud;

public partial class HudRoot
{
    // ---------------- 状态 ----------------
    private bool Playing => _flow.IsPlaying;
    private bool InterfaceState => _router.InterfaceMode;
    private bool BigPage => _settingsCard.Visible || _savePage.Visible;

    private void SyncState()
    {
        var state = _flow.State;
        _root.Visible = !_hidden;
        switch (state)
        {
            case AppState.Title:
                ReleaseResident();
                if (!_title.Visible) _title.Open();
                _minimapSlot.Visible = false;
                break;
            case AppState.Playing:
                BuildResident();
                if (!_galleryTitle) _title.Close();
                if (_args.Gallery == "hud") SetInterface(true);
                break;
            case AppState.Entering:
            case AppState.Leaving:
                BuildResident();   // 进出都在雾幕下，建在这里不露出建页那一帧
                goto case AppState.Quitting;
            case AppState.Boot:
            case AppState.Quitting:
                _title.Close();
                if (_settingsCard.Visible) CloseSettings();
                if (_savePage.Visible) CloseSaves();
                break;
            default:
                throw new InvalidOperationException("Unknown application state.");
        }
        SyncCorners();
    }

    private void SyncCorners()
    {
        SyncPerf();
        if (!Playing || _resident is not { } r) { _minimapSlot.Visible = false; SyncHostInput(); return; }
        var ui = InterfaceState && !_clean;
        _minimapSlot.Visible = !_clean && !BigPage;
        var show = ui && !_savePage.Visible;
        r.TopRight.Visible = show;
        r.Speed.Visible = show && !_settingsCard.Visible;
        r.Time.Visible = show && _timeOpen;
        r.Weather.Visible = show && _timeOpen && _model.Allows(WorldControlMask.Weather);
        r.Strip.Visible = show && !_timeOpen;
        SyncHostInput();
    }

    private void RefreshHud()
    {
        _refreshClock = 0;
        SyncNotices();
        if (_resident is not { } r) return;
        if (r.Speed.IsVisibleInTree()) r.Speed.Sync();
        if (r.Time.IsVisibleInTree()) r.Time.Sync();
        if (r.Weather.IsVisibleInTree()) r.Weather.Sync();
        if (r.Strip.IsVisibleInTree()) r.Strip.Sync();
    }

    private void OnWorldChanged(long instance)
    {
        _primed = false;
        _refreshClock = RefreshInterval;
    }

    private void SyncNotices()
    {
        if (!_primed)
        {
            // 首次刷新或换世界后只记下当前值，不出提示
            _primed = true;
            _lastMode = _model.Mode;
            _lastSeason = _model.Season;
            return;
        }
        if (_model.Mode != _lastMode)
        {
            _lastMode = _model.Mode;
            _toast.Show(UiKit.T(_model.Mode == ViewMode.Walk ? "toast.mode.walk" : "toast.mode.fly"), _model.Mode == ViewMode.Walk ? "view_eye" : "view_free");
        }
        if (_model.Season != _lastSeason)
        {
            _lastSeason = _model.Season;
            _toast.Show(UiKit.T("hud.season." + TimeCard.SeasonCode(_model.Season).ToLowerInvariant()), "season_" + TimeCard.SeasonCode(_model.Season).ToLowerInvariant());
        }
    }

    private void OnSettingsChanged()
    {
        _kit.SetResidentFollows(_settings.Resident == ResidentTheme.Follow);
        _scaleDebounce = 0;
    }

    private void BindEvents()
    {
        if (_bound) return;
        _bound = true;
        _clock.Switched += OnThemeSwitched;
        _root.Resized += Layout;
        _safeFrame.Resized += Layout;
        _settings.Changed += OnSettingsChanged;
        _flow.Changed += OnFlowChanged;
        _model.World.InstanceChanged += OnWorldChanged;
        Services.Get<IViewControl>().WalkEntryRefused += OnWalkEntryRefused;
    }

    private void OnWalkEntryRefused()
    {
        _lastMode = ViewMode.Fly;
        _toast.Show(UiKit.T("toast.walk.cannot_land"));
    }

    private void OnThemeSwitched(bool dark)
    {
        if (_settingsCard.Visible || _savePage.Visible || _kit.ResidentFollows) _audio.PlayUi(UiCue.ShiftDayNight);
    }

    private void OnFlowChanged(AppState previous, AppState current)
    {
        if (current == AppState.Playing) ResetIdleFade();
        SyncState();
    }

    private void OnInterfaceModeChanged(bool enabled)
    {
        if (enabled) ResetIdleFade();
        SyncCorners();
    }

    private void ResetIdleFade()
    {
        _idle = 0;
        _cornerAlpha = 1;
        FadeResidents(1, interactive: true);
    }

    // ---------------- 界面缩放（UI-VISUAL）：不乘 DPI ----------------
    private void ApplyScale()
    {
        var window = DisplayServer.WindowGetSize();
        if (window.X <= 0 || window.Y <= 0) return;
        var s = Math.Min(window.X / 1920f, window.Y / 1080f);
        var lo = 0.85f / s;
        var hi = Math.Min(window.X / 1280f, window.Y / 720f) / s;
        var user = _args.Scale ?? _settings.UiScale;
        GetTree().Root.ContentScaleFactor = Math.Clamp(user, lo, Math.Max(lo, hi));
    }
}
