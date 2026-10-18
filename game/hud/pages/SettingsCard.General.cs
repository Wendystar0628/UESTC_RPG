using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Save;
using Campus.Contracts.Settings;
using Campus.Hud.Widgets;
using Godot;

namespace Campus.Hud.Pages;

public partial class SettingsCard
{
    private System.Collections.IEnumerable BuildGeneral(Control page)
    {
        var ui = _kit.Layout.SettingGroup(page, UiKit.T("settings.group.interface"));
        Choice(ui, HudSettingDefinitions.Theme, (int)_settings.Theme, value => _settings.SetTheme((UiThemeMode)value));
        yield return null;
        Choice(ui, HudSettingDefinitions.Resident, (int)_settings.Resident, value => _settings.SetResident((ResidentTheme)value));
        yield return null;
        Toggle(ui, HudSettingDefinitions.Frost, _settings.Frost, _settings.SetFrost);
        yield return null;
        Toggle(ui, HudSettingDefinitions.ReduceTransparency, _settings.ReduceTransparency, _settings.SetReduceTransparency);
        yield return null;
        Choice(ui, HudSettingDefinitions.ReduceMotion, (int)_settings.ReduceMotion, value => _settings.SetReduceMotion((ReduceMotionMode)value));
        yield return null;
        Slider(ui, HudSettingDefinitions.UiScale, _settings.UiScale, _settings.SetUiScale, value => $"{value * 100:0}%");
        yield return null;
        var store = Svc<ISettingsStore>();
        var viewSettings = store.Section("ui");
        foreach (var definition in new[] { UiSettingDefinitions.FlyFov, UiSettingDefinitions.WalkFov })
        {
            Slider(ui, definition, viewSettings.Get(definition), value =>
            {
                viewSettings.Set(definition.Key, value);
                store.TrySave();
            }, value => $"{value:0}°");
            yield return null;
        }
        Choice(ui, UiSettingDefinitions.ViewBobbing, viewSettings.Get(UiSettingDefinitions.ViewBobbing), value =>
        {
            viewSettings.Set(UiSettingDefinitions.ViewBobbing.Key, value);
            store.TrySave();
        });
        yield return null;
        Toggle(ui, UiSettingDefinitions.AutoJump, viewSettings.Get(UiSettingDefinitions.AutoJump), value =>
        {
            viewSettings.Set(UiSettingDefinitions.AutoJump.Key, value);
            store.TrySave();
        });
        yield return null;
        Toggle(ui, HudSettingDefinitions.SingleKeyShortcuts, _settings.SingleKeyShortcuts, _settings.SetSingleKeyShortcuts);
        yield return null;
        Toggle(ui, HudSettingDefinitions.ShowPerf, _settings.ShowPerf, _settings.SetShowPerf);
        yield return null;

        var storageGroup = _kit.Layout.SettingGroup(page, UiKit.T("settings.group.storage"));
        var storageRow = _kit.Layout.SettingRow(storageGroup, UiKit.T("settings.storage"));
        var storage = _kit.Text(storageRow, "", "data", "text.2");
        var clear = _kit.Button(storageRow, ButtonStyle.Secondary, UiKit.T("settings.storage.clear"));
        void SyncStorage()
        {
            var bake = Svc<IBakeService>();
            var progress = bake.Progress;
            var state = progress.Total == 0 || progress.Done >= progress.Total ? UiKit.T("settings.storage.done")
                : UiKit.T("settings.storage.running", ("pct", $"{progress.Done * 100 / Math.Max(1, progress.Total)}"));
            storage.Text = $"{bake.CacheBytes / 1048576.0:0.0} MB · {state}";
        }
        clear.Pressed += () => { Svc<IBakeService>().Clear(); SyncStorage(); };
        SyncStorage();
        page.VisibilityChanged += SyncStorage;
        yield return null;
        var saveRow = _kit.Layout.SettingRow(storageGroup, UiKit.T("settings.save"));
        var save = _kit.Text(saveRow, "", "data", "text.2");
        void SyncSave()
        {
            var state = Svc<ISaveService>().State;
            save.Text = state.ReadOnly switch
            {
                ReadOnlyReason.None => state.Slot is { } slot ? $"{slot.Value.ToUpperInvariant()} · {(state.Last == SaveStatus.Failed ? UiKit.T("settings.save.failed") : UiKit.T("settings.save.ok"))}" : UiKit.T("settings.save.none"),
                ReadOnlyReason.SaveOff => UiKit.T("settings.save.off"),
                ReadOnlyReason.OtherWindow => UiKit.T("settings.save.other_window"),
                _ => throw new InvalidOperationException("Unknown read-only reason."),
            };
            _kit.Recolor(save, state.Last == SaveStatus.Failed ? "danger.text" : "text.2");
        }
        SyncSave();
        page.VisibilityChanged += SyncSave;
    }
}
