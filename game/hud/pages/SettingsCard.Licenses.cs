using Campus.Hud.Widgets;
using Godot;

namespace Campus.Hud.Pages;

// 许可页
public partial class SettingsCard
{
    private float BuildLicenses(Control page, float width)
    {
        float y = 8;
        foreach (var key in new[] { "licenses.self", "licenses.osm", "licenses.fonts", "licenses.godot" })
        {
            var label = _kit.Text(page, UiKit.T(key), "small", "text.1", new Vector2(8, y), width: width - 8, wrap: true);
            y += label.GetCombinedMinimumSize().Y + 16;
        }
        return y + 16;
    }
}
