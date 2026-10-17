using System.Text.Json;
using Campus.Contracts;
using Godot;
using Services = Campus.Kernel.Registry.Kernel;

namespace Campus.Hud.Style;

/// <summary>
/// 界面唯一的取色入口（UI-VISUAL）：读 res://style/palette/ext/ui.json，经契约 <see cref="UiPalette"/> 解析，
/// 启动时用 <see cref="UiPalette.Validate(UiPaletteDocument, PaletteCatalog)"/> 整表校验。代码里不写色值。
/// </summary>
public sealed class UiColors
{
    public const string Path = "res://style/palette/ext/ui.json";
    public static readonly string[] Themes = ["light", "dark"];

    private readonly UiPalette _palette;

    private UiColors(UiPalette palette)
    {
        _palette = palette;
    }

    public static UiColors Load()
    {
        var bytes = Godot.FileAccess.GetFileAsBytes(Path);
        if (bytes.Length == 0) throw new FileNotFoundException("Missing UI palette.", Path);
        var document = JsonSerializer.Deserialize<UiPaletteDocument>(bytes, ContractJson.Options)
            ?? throw new InvalidDataException("ui.json is empty.");
        var catalog = Services.Get<IPaletteCatalogSource>().Catalog;
        UiPalette.Validate(document, catalog);
        return new UiColors(new UiPalette(document, catalog));
    }

    public Color Theme(string theme, string token)
    {
        return ToColor(_palette.ResolveTheme(theme, token));
    }

    public Color Resolve(string reference)
    {
        return ToColor(_palette.Resolve(reference));
    }

    /// <summary>ui_panel 的色表：按 <see cref="UiTokens.PanelRoles"/> 的顺序排，长度补到 64。</summary>
    public Color[] RoleTable(UiTokens tokens, string theme)
    {
        var table = new Color[64];
        for (var i = 0; i < tokens.PanelRoles.Count; i++) table[i] = Theme(theme, tokens.PanelRoles[i]);
        return table;
    }

    private static Color ToColor(System.Numerics.Vector4 v) => new(v.X, v.Y, v.Z, v.W);
}
