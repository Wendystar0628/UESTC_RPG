using System.Text.Json;
using Campus.Contracts;
using Godot;
using Services = Campus.Kernel.Registry.Kernel;

namespace Campus.Hud.Style;

/// <summary>
/// 界面唯一的取色入口（UI-VISUAL §0.1、§3.8）：读 res://style/palette/ext/ui.json，经契约 <see cref="UiPalette"/> 解析，
/// 启动时用 <see cref="UiPalette.Validate(UiPaletteDocument, PaletteRegistry)"/> 整表校验。代码里不写色值。
/// </summary>
public sealed class UiColors
{
    public const string Path = "res://style/palette/ext/ui.json";
    public static readonly string[] Themes = ["light", "dark"];

    private readonly UiPalette _palette;
    private readonly string? _nowFallback;

    private UiColors(UiPalette palette, string? nowFallback)
    {
        _palette = palette;
        _nowFallback = nowFallback;
    }

    public static UiColors Load()
    {
        var bytes = Godot.FileAccess.GetFileAsBytes(Path);
        if (bytes.Length == 0) throw new FileNotFoundException("Missing UI palette.", Path);
        var document = JsonSerializer.Deserialize<UiPaletteDocument>(bytes, ContractJson.Options)
            ?? throw new InvalidDataException("ui.json is empty.");
        var registry = SceneRegistry();
        string? fallback = null;
        if (!registry.TryGet(new PaletteToken("window.warm"), out _))
        {
            // contract-4 第 1 条：场景色名登记表的运行时入口合入之前，"此刻"暖色点临时退到暖琥珀。
            fallback = "warning.dark";
            GD.PushWarning("UI palette: scene colour window.warm is not registered yet; ui.now falls back to warning.dark (contract-4).");
        }
        var palette = new UiPalette(document, registry);
        if (fallback is null) UiPalette.Validate(document, registry);
        return new UiColors(palette, fallback);
    }

    private static PaletteRegistry SceneRegistry()
    {
        try { return Services.Get<PaletteRegistry>(); }
        catch (InvalidOperationException) { return new PaletteRegistry([]); }
    }

    public Color Theme(string theme, string token)
    {
        if (_nowFallback is not null && token == "now") return ToColor(_palette.Resolve(_nowFallback));
        return ToColor(_palette.ResolveTheme(theme, token));
    }

    public Color Resolve(string reference)
    {
        if (_nowFallback is not null && (reference == "now" || reference == "window.warm")) return ToColor(_palette.Resolve(_nowFallback));
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
