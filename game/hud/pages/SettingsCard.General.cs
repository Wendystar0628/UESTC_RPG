using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Save;
using Campus.Hud.Widgets;
using Godot;

namespace Campus.Hud.Pages;

// 设置页（界面、存储）
public partial class SettingsCard
{
    private float BuildGeneral(Control page, float width)
    {
        float y = 8;
        Parts.GroupHeader(_kit, page, new Vector2(8, y), width, UiKit.T("settings.group.interface"));
        y += 24;
        Row(page, y, UiKit.T("settings.theme"));
        var theme = Segmented.Create(_kit, page, new Vector2(ControlX, y + 2), [72, 72, 72],
            [UiKit.T("settings.theme.auto"), UiKit.T("settings.theme.light"), UiKit.T("settings.theme.dark")], false, selected: (int)_settings.Theme);
        theme.Changed += i => _settings.SetTheme((UiThemeMode)i);
        y += 40;
        Row(page, y, "　" + UiKit.T("settings.resident"));
        var resident = Segmented.Create(_kit, page, new Vector2(ControlX, y + 2), [72, 72],
            [UiKit.T("settings.resident.dark"), UiKit.T("settings.resident.follow")], false, selected: (int)_settings.Resident);
        resident.Changed += i => _settings.SetResident((ResidentTheme)i);
        y += 40;
        Row(page, y, UiKit.T("settings.frost"));
        var frost = DipSwitch.Create(_kit, page, new Vector2(ControlX, y + 10), _settings.Frost, false);
        frost.Toggled += _settings.SetFrost;
        y += 40;
        Row(page, y, UiKit.T("settings.reduce_transparency"));
        var opaque = DipSwitch.Create(_kit, page, new Vector2(ControlX, y + 10), _settings.ReduceTransparency, false);
        opaque.Toggled += _settings.SetReduceTransparency;
        y += 40;
        Row(page, y, UiKit.T("settings.reduce_motion"));
        var motion = Segmented.Create(_kit, page, new Vector2(ControlX, y + 2), [88, 64, 64],
            [UiKit.T("settings.follow_system"), UiKit.T("ui.common.on"), UiKit.T("ui.common.off")], false, selected: (int)_settings.ReduceMotion);
        motion.Changed += i => _settings.SetReduceMotion((ReduceMotionMode)i);
        y += 40;
        Row(page, y, UiKit.T("settings.ui_scale"));
        var scale = Fader.Create(_kit, page, new Vector2(ControlX + 6, y + 2), 200, 0.75f, 1.5f, 0.05f, _settings.UiScale, false, v => $"{v * 100:0}%");
        scale.Changed += _settings.SetUiScale;
        y += 40;
        Row(page, y, UiKit.T("settings.single_keys"));
        var keys = DipSwitch.Create(_kit, page, new Vector2(ControlX, y + 10), _settings.SingleKeyShortcuts, false);
        keys.Toggled += _settings.SetSingleKeyShortcuts;
        y += 40;
        Row(page, y, UiKit.T("settings.show_perf"));
        var perf = DipSwitch.Create(_kit, page, new Vector2(ControlX, y + 10), _settings.ShowPerf, false);
        perf.Toggled += _settings.SetShowPerf;
        y += 52;

        Parts.GroupHeader(_kit, page, new Vector2(8, y), width, UiKit.T("settings.group.storage"));
        y += 24;
        Row(page, y, UiKit.T("settings.storage"));
        var storage = _kit.Text(page, "", "data", "text.2", new Vector2(ControlX, y + 11));
        var clear = _kit.Button(page, ButtonStyle.Secondary, UiKit.T("settings.storage.clear"), new Rect2(width - 132 + 8, y, 132, 40));
        void SyncStorage()
        {
            var bake = Svc<IBakeService>();
            var p = bake.Progress;
            var state = p.Total == 0 || p.Done >= p.Total ? UiKit.T("settings.storage.done")
                : UiKit.T("settings.storage.running", ("pct", $"{p.Done * 100 / Math.Max(1, p.Total)}"));
            storage.Text = $"{bake.CacheBytes / 1048576.0:0.0} MB · {state}";
        }
        clear.Pressed += () => { Svc<IBakeService>().Clear(); SyncStorage(); };
        SyncStorage();
        page.VisibilityChanged += SyncStorage;
        y += 40;
        Row(page, y, UiKit.T("settings.save"));
        var save = _kit.Text(page, "", "data", "text.2", new Vector2(ControlX, y + 11));
        void SyncSave()
        {
            var s = Svc<ISaveService>().State;
            save.Text = s.ReadOnly switch
            {
                ReadOnlyReason.None => s.Slot is { } slot ? $"{slot.Value.ToUpperInvariant()} · {(s.Last == SaveStatus.Failed ? UiKit.T("settings.save.failed") : UiKit.T("settings.save.ok"))}" : UiKit.T("settings.save.none"),
                ReadOnlyReason.SaveOff => UiKit.T("settings.save.off"),
                ReadOnlyReason.OtherWindow => UiKit.T("settings.save.other_window"),
                _ => throw new InvalidOperationException("Unknown read-only reason."),
            };
            _kit.Recolor(save, s.Last == SaveStatus.Failed ? "danger.text" : "text.2");
        }
        SyncSave();
        page.VisibilityChanged += SyncSave;
        return y + 56;
    }
}
