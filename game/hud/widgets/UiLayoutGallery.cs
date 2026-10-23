using Campus.Contracts;
using Campus.Hud.Style;
using Godot;

namespace Campus.Hud.Widgets;

/// <summary>--ui-gallery=layout 的同一页布局样例；关闭时释放整棵树及其材质持有者。</summary>
public static class UiLayoutGallery
{
    public static Control Create(UiKit kit, Control parent)
    {
        var layout = kit.Layout;
        var root = layout.Host(parent, "LayoutGallery");
        var lease = Campus.Kernel.Registry.Kernel.Get<ICameraRig>().LockInput(InputLockReason.Menu);
        root.TreeExiting += lease.Dispose;
        root.Theme = kit.Theme.Theme;
        var scrim = kit.Box(root, new UiFx(Fill: "scrim", Margin: 0));
        scrim.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var safe = layout.SafeFrame(root);
        var frame = layout.Page(safe, UiKit.T("hud.button.settings"), "savesWidth", centered: true);
        foreach (var key in new[] { "settings.theme", "settings.group.storage", "settings.tab.sound" })
            kit.Button(frame.Tabs, ButtonStyle.Tab, UiKit.T(key));

        var group = layout.SettingGroup(frame.Body, UiKit.T("settings.group.interface"));
        var theme = layout.SettingRow(group, UiKit.T("settings.theme"));
        Segmented.Create(kit, theme, [UiKit.T("settings.theme.auto"), UiKit.T("settings.theme.light"), UiKit.T("settings.theme.dark")], selected: 0);
        var resident = layout.SettingRow(group, UiKit.T("settings.resident"));
        Dropdown.Create(kit, resident, [UiKit.T("settings.resident.dark"), UiKit.T("settings.resident.follow")], selected: 0);
        var motion = layout.SettingRow(group, UiKit.T("settings.reduce_motion"));
        DipSwitch.Create(kit, motion, true);
        var scale = layout.SettingRow(group, UiKit.T("settings.ui_scale"));
        Fader.Create(kit, scale, 0.75f, 1.5f, 0.05f, 1, format: v => $"{v * 100:0}%");

        Parts.GroupHeader(kit, frame.Body, UiKit.T("settings.group.storage"));
        var cards = layout.Cards(frame.Body);
        for (var i = 0; i < 3; i++)
        {
            var card = layout.Card(cards);
            kit.Text(card, UiKit.T("saves.title") + $" {i + 1:00}", "h3");
            kit.Text(card, UiKit.T("saves.detail.empty"), colorRole: "text.2");
            kit.Button(card, ButtonStyle.Secondary, UiKit.T("saves.new_here"), pressed: () => OpenDialog(kit, safe, frame));
        }
        kit.Button(frame.Footer, ButtonStyle.Secondary, UiKit.T("ui.common.close"), pressed: root.QueueFree);
        return root;
    }

    private static void OpenDialog(UiKit kit, Control safe, UiPageFrame page)
    {
        UiLayout.SetInteractive(page.Shell, false);
        var host = kit.Layout.Host(safe, "DialogGallery");
        var scrim = kit.Box(host, new UiFx(Fill: "scrim", Margin: 0));
        scrim.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var dialog = kit.Layout.Dialog(host, UiKit.T("saves.overwrite.title"));
        kit.Text(dialog.Body, UiKit.T("saves.detail.overwrite"));
        void Close() { UiLayout.SetInteractive(page.Shell, true); host.QueueFree(); }
        kit.Button(dialog.Footer, ButtonStyle.Secondary, UiKit.T("saves.overwrite.cancel"), pressed: Close);
        kit.Button(dialog.Footer, ButtonStyle.Primary, UiKit.T("ui.common.close"), pressed: Close);
    }
}
