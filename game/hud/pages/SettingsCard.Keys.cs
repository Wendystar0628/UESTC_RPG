using Campus.Hud.Widgets;
using Godot;

namespace Campus.Hud.Pages;

// 键位页（UI-NAV）
public partial class SettingsCard
{
    private float BuildKeys(Control page, float width)
    {
        float y = 8;
        (string Key, string[] Actions)[] rows =
        [
            ("keys.menu", ["menu"]), ("keys.move", ["move_forward", "move_left", "move_backward", "move_right"]), ("keys.updown", ["move_up", "move_down"]),
            ("keys.sprint", ["sprint"]), ("keys.time", ["time_panel"]), ("keys.forward", ["time_forward"]), ("keys.flow", ["toggle_flow"]),
            ("keys.season", ["next_season"]), ("keys.weather", ["weather"]), ("keys.mode", ["toggle_mode"]), ("keys.map", ["map"]),
            ("keys.index", ["atlas_index"]), ("keys.minimap", ["minimap"]), ("keys.help", ["help"]), ("keys.clean", ["clean_view"]),
        ];
        Parts.GroupHeader(_kit, page, new Vector2(8, y), width, UiKit.T("settings.group.keys"));
        y += 24;
        foreach (var (key, actions) in rows)
        {
            Row(page, y, UiKit.T(key));
            float kx = ControlX;
            foreach (var action in actions)
            {
                if (!InputMap.HasAction(action)) continue;
                kx += Parts.Keycap(_kit, page, new Vector2(kx, y + 8), KeyLabel(action)) + 6;
            }
            y += 40;
        }
        _kit.Text(page, UiKit.T("keys.note"), "small", "text.2", new Vector2(8, y + 8), width: width - 8, wrap: true);
        return y + 64;
    }
}
