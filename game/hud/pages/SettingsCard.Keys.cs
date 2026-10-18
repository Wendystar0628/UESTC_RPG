using Campus.Hud.Widgets;
using Godot;

namespace Campus.Hud.Pages;

public partial class SettingsCard
{
    private System.Collections.IEnumerable BuildKeys(Control page)
    {
        var group = _kit.Layout.SettingGroup(page, UiKit.T("settings.group.keys"));
        var menu = _kit.Layout.SettingRow(group, UiKit.T("keys.menu"));
        Parts.Keycap(_kit, menu, KeyLabel("menu"));
        yield return null;
        foreach (var action in KeyboardActions())
        {
            var row = _kit.Layout.SettingRow(group, UiKit.T("rebind.action." + action));
            Parts.Keycap(_kit, row, KeyLabel(action));
            yield return null;
        }
        _kit.Text(page, UiKit.T("keys.note"), "small", "text.2");
    }
}
