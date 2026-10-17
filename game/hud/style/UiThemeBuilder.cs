using System.Text.Json;
using Godot;

namespace Campus.Hud.Style;

/// <summary>
/// 生成界面唯一的 Theme（UI-VISUAL）：字体链、字号阶梯、类型变体、StyleBox 载体。
/// 颜色走 ui_panel 着色器按 ui_night 取日夜两值；Theme 里的颜色只作无着色器时的兜底，取晶白值。
/// 不写零散的 theme_override_*，不另存共享 .tres。
/// </summary>
public sealed class UiThemeBuilder
{
    public const string FontDir = "res://assets/hud/fonts/";

    private readonly UiTokens _t;
    private readonly UiColors _c;
    private readonly Dictionary<string, FontFile> _files = new(StringComparer.Ordinal);
    private readonly HashSet<string> _types = new(StringComparer.Ordinal);
    private readonly bool _shaded;

    public UiThemeBuilder(UiTokens tokens, UiColors colors, bool shaded)
    {
        _t = tokens;
        _c = colors;
        _shaded = shaded;
        Theme = new Theme();
        foreach (var role in _t.Get("metrics", "type", "roles").EnumerateObject())
        {
            var fv = Font(role.Name, out var size, out var lineHeight);
            var name = TextType(role.Name);
            Theme.SetTypeVariation(name, "Label");
            Theme.SetFont("font", name, fv);
            Theme.SetFontSize("font_size", name, size);
            Theme.SetColor("font_color", name, _c.Theme("light", "text.1"));
            Theme.SetConstant("line_spacing", name, Math.Max(0, (int)Math.Round(size * (lineHeight - 1.25f))));
        }
        Theme.DefaultFont = Font("body", out var bodySize, out _);
        Theme.DefaultFontSize = bodySize;
        ThemeDB.FallbackFont = Theme.DefaultFont;
        BuildScrollBar();
    }

    public Theme Theme { get; }

    public static string TextType(string role) => "Text_" + role.Replace('.', '_').Replace('-', '_');

    // ---------- 字体（UI-VISUAL）：正文 Inter → 思源黑体 SC；数据 JetBrains Mono → 思源黑体 SC；标题 思源黑体 SC → Inter ----------
    private FontFile File(string family, string weight)
    {
        var files = _t.Get("metrics", "type", "families", family, "files");
        if (!files.TryGetProperty(weight, out var file)) file = files.GetProperty("Regular");
        var name = file.GetString()!;
        if (_files.TryGetValue(name, out var font)) return font;
        font = GD.Load<FontFile>(FontDir + name) ?? throw new FileNotFoundException("Missing UI font.", FontDir + name);
        font.AllowSystemFallback = false;
        font.Hinting = TextServer.Hinting.Light;
        font.SubpixelPositioning = TextServer.SubpixelPositioning.Auto;
        font.Antialiasing = TextServer.FontAntialiasing.Gray;
        _files[name] = font;
        return font;
    }

    public FontVariation Font(string role, out int size, out float lineHeight)
    {
        var r = _t.Get("metrics", "type", "roles", role);
        size = r.GetProperty("size").GetInt32();
        lineHeight = r.GetProperty("lineHeight").GetSingle();
        var weight = r.GetProperty("weight").GetString()!;
        var chain = _t.Get("metrics", "type", "chains", r.GetProperty("chain").GetString()!);
        var families = chain.EnumerateArray().Select(e => e.GetString()!).ToArray();
        var fv = new FontVariation { BaseFont = File(families[0], weight) };
        var fallbacks = new Godot.Collections.Array<Font>();
        foreach (var family in families.Skip(1)) fallbacks.Add(File(family, weight));
        fv.Fallbacks = fallbacks;
        fv.SpacingGlyph = (int)Math.Round(r.GetProperty("tracking").GetSingle() * size);
        if (r.TryGetProperty("tnum", out var tnum) && tnum.GetBoolean())
        {
            var tag = TextServerManager.GetPrimaryInterface().NameToTag(_t.S("metrics", "type", "tabularFigures"));
            fv.OpentypeFeatures = new Godot.Collections.Dictionary { { tag, 1 } };
        }
        return fv;
    }

    public bool Upper(string role) =>
        _t.Get("metrics", "type", "roles", role).TryGetProperty("upper", out var up) && up.GetBoolean();

    // ---------- StyleBox 载体（UI-VISUAL）：着色器在载体上画切角 SDF 与发丝线 ----------
    private StyleBox Carrier(int expand, float padX, float padY, float shiftY, string fallbackFill)
    {
        if (!_shaded)
        {
            // 兜底：没有界面材质时用晶白值画一个平涂块，保证能读。
            var plain = new StyleBoxFlat
            {
                BgColor = _c.Theme("light", fallbackFill),
                BorderColor = _c.Theme("light", "line.control"),
                CornerDetail = 1,
                AntiAliasing = false
            };
            plain.SetBorderWidthAll(fallbackFill == "surface.panel" ? 0 : 1);
            plain.ContentMarginLeft = plain.ContentMarginRight = padX;
            plain.ContentMarginTop = padY + shiftY;
            plain.ContentMarginBottom = padY - shiftY;
            return plain;
        }
        var sb = new StyleBoxFlat { BgColor = Colors.White, DrawCenter = true, AntiAliasing = false };
        sb.ExpandMarginLeft = sb.ExpandMarginRight = sb.ExpandMarginTop = sb.ExpandMarginBottom = expand;
        sb.ContentMarginLeft = sb.ContentMarginRight = padX;
        sb.ContentMarginTop = padY + shiftY;
        sb.ContentMarginBottom = padY - shiftY;
        return sb;
    }

    public string PanelType(int expand)
    {
        var name = "PanelCarrier" + expand;
        if (_types.Add(name))
        {
            Theme.SetTypeVariation(name, "Panel");
            Theme.SetStylebox("panel", name, Carrier(expand, 0, 0, 0, "surface.panel"));
        }
        return name;
    }

    public string ButtonType(string fontRole, int padX, int padY, bool sunk, int expand, int iconMax, int separation = 8)
    {
        var name = $"Btn_{fontRole.Replace('.', '_').Replace('-', '_')}_{padX}_{padY}_{(sunk ? 1 : 0)}_{expand}_{iconMax}_{separation}";
        if (!_types.Add(name)) return name;
        Theme.SetTypeVariation(name, "Button");
        var box = Carrier(expand, padX, padY, sunk ? 1 : 0, "surface.solid");
        foreach (var state in new[] { "normal", "hover", "pressed", "disabled", "hover_pressed", "normal_mirrored", "hover_mirrored",
                     "pressed_mirrored", "disabled_mirrored", "hover_pressed_mirrored" })
            Theme.SetStylebox(state, name, box);
        Theme.SetStylebox("focus", name, new StyleBoxEmpty());
        var fv = Font(fontRole, out var size, out _);
        Theme.SetFont("font", name, fv);
        Theme.SetFontSize("font_size", name, size);
        var ink = _c.Theme("light", "text.1");
        foreach (var state in new[] { "font_color", "font_hover_color", "font_pressed_color", "font_disabled_color", "font_focus_color", "font_hover_pressed_color" })
            Theme.SetColor(state, name, ink);
        foreach (var state in new[] { "icon_normal_color", "icon_hover_color", "icon_pressed_color", "icon_disabled_color", "icon_focus_color", "icon_hover_pressed_color" })
            Theme.SetColor(state, name, _shaded ? Colors.White : ink);
        Theme.SetConstant("icon_max_width", name, iconMax);
        Theme.SetConstant("h_separation", name, separation);
        return name;
    }

    // 滚动条：宽 4，轨道不画，拇指是白色载体，由 ui_panel kind 3 换成 line.scroll（UI-VISUAL）
    private void BuildScrollBar()
    {
        var grabber = new StyleBoxFlat { BgColor = _shaded ? Colors.White : _c.Theme("light", "line.scroll"), AntiAliasing = false };
        grabber.ContentMarginLeft = grabber.ContentMarginRight = _t.V2("metrics", "size", "scrollbar").X / 2;
        Theme.SetTypeVariation("ScrollThin", "VScrollBar");
        Theme.SetStylebox("scroll", "ScrollThin", new StyleBoxEmpty { ContentMarginLeft = 2, ContentMarginRight = 2 });
        Theme.SetStylebox("scroll_focus", "ScrollThin", new StyleBoxEmpty());
        Theme.SetStylebox("grabber", "ScrollThin", grabber);
        Theme.SetStylebox("grabber_highlight", "ScrollThin", grabber);
        Theme.SetStylebox("grabber_pressed", "ScrollThin", grabber);
    }

    /// <summary>--ui-export-theme：把生成结果另存为 ui_theme.tres，只作预览与差异审查，运行时不读（UI-VISUAL）。</summary>
    public Error Export(string path) => ResourceSaver.Save(Theme, path);

    public static JsonElement Role(UiTokens t, string role) => t.Get("metrics", "type", "roles", role);
}
