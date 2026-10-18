using Campus.Hud.Style;
using Campus.Hud.Widgets;
using Godot;

namespace Campus.Hud.Pages;

public partial class SettingsCard
{
    private System.Collections.IEnumerable BuildLicenses(Control page)
    {
        var table = _kit.Layout.Add(page, new GridContainer
        {
            Columns = 2,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        }, UiThemeBuilder.CardGrid);
        foreach (var name in new[] { "self", "osm", "fonts", "godot" })
        {
            var title = _kit.Text(table, UiKit.T("licenses.name." + name), "small", "text.2");
            title.SizeFlagsStretchRatio = 1;
            title.SizeFlagsVertical = SizeFlags.ShrinkBegin;
            var detail = _kit.Text(table, UiKit.T("licenses." + name), "small", "text.1");
            detail.SizeFlagsStretchRatio = 3;
            yield return null;
        }
    }
}
