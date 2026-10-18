using System.Text.Json;
using Godot;

namespace Campus.Hud.Style;

/// <summary>
/// 生成界面唯一的 Theme（UI-VISUAL）：字体链、字号阶梯、类型变体、StyleBox 载体。
/// 颜色走 ui_panel 着色器按 ui_night 取日夜两值；载体与图标色一律取白，由着色器换色。
/// 不写零散的 theme_override_*，不另存共享 .tres。
/// 唯一的运行时改动：下拉展开框是 Window、挂不了着色器，它的颜色随日夜改写（<see cref="SetPopupDark"/>）。
/// </summary>
public sealed class UiThemeBuilder
{
    public const string FontDir = "res://assets/hud/fonts/";

    private readonly UiTokens _t;
    private readonly UiColors _c;
    private readonly Dictionary<string, FontFile> _files = new(StringComparer.Ordinal);
    private readonly HashSet<string> _types = new(StringComparer.Ordinal);

    public UiThemeBuilder(UiTokens tokens, UiColors colors)
    {
        _t = tokens;
        _c = colors;
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
        BuildLayout();
        BuildScrollBar();
    }

    public Theme Theme { get; }

    public static string TextType(string role) => "Text_" + role.Replace('.', '_').Replace('-', '_');

    public const string LayoutMetrics = "UiLayoutMetrics";
    public const string ScreenMargin = "UiScreenMargin", PageMargin = "UiPageMargin", PanelMargin = "UiPanelMargin", SmallMargin = "UiSmallMargin";
    public const string Groups = "UiGroups", Rows = "UiRows", Row = "UiRow", Corners = "UiCorners";
    public const string CardGrid = "UiCardGrid", Actions = "UiActions", Tabs = "UiTabs", Segments = "UiSegments";
    public const string PanelContainerType = "UiPanelContainer";
    public const string MinimapMargin = "UiMinimapMargin", MinimapBody = "UiMinimapBody";

    private void BuildLayout()
    {
        foreach (var value in _t.Get("metrics", "layout").EnumerateObject())
            if (value.Value.ValueKind == JsonValueKind.Number)
                Theme.SetConstant(value.Name, LayoutMetrics, value.Value.GetInt32());
        var aspect = _t.V2("metrics", "layout", "safeAspect");
        Theme.SetConstant("safeAspectWidth", LayoutMetrics, (int)aspect.X);
        Theme.SetConstant("safeAspectHeight", LayoutMetrics, (int)aspect.Y);
        foreach (var name in new[] { "primary", "secondary", "compact", "segment", "dropdown", "tab", "iconButton", "icon", "iconInButton",
                     "primaryMinWidth", "primaryPadX", "secondaryMinWidth", "dropdownMinWidth", "keycap", "tag" })
            Theme.SetConstant(name, LayoutMetrics, (int)_t.Size(name));
        MarginType(ScreenMargin, _t.Space("screenEdge"));
        MarginType(PageMargin, _t.Space("pagePad"));
        MarginType(PanelMargin, _t.Space("panelPad"));
        MarginType(SmallMargin, _t.F("metrics", "grid", "half"));
        MarginType(MinimapMargin, _t.F("metrics", "map", "minimapInset"));
        BoxType(MinimapBody, "VBoxContainer", 0);
        BoxType(Groups, "VBoxContainer", _t.Space("group"));
        BoxType(Rows, "VBoxContainer", _t.F("metrics", "layout", "rowGap"));
        BoxType(Row, "HBoxContainer", _t.Space("control"));
        BoxType(Segments, "HBoxContainer", 0);
        BoxType(Corners, "VBoxContainer", _t.F("metrics", "layout", "cornerGap"));
        BoxType(Actions, "HFlowContainer", _t.Space("control"));
        BoxType(Tabs, "HFlowContainer", _t.Size("tabGap"));
        Theme.SetTypeVariation(CardGrid, "GridContainer");
        Theme.SetConstant("h_separation", CardGrid, _t.I("metrics", "layout", "cardGap"));
        Theme.SetConstant("v_separation", CardGrid, _t.I("metrics", "layout", "cardGap"));
        Theme.SetTypeVariation(PanelContainerType, "PanelContainer");
        Theme.SetStylebox("panel", PanelContainerType, Carrier(_t.I("metrics", "shape", "expandMargin"), 0, 0, 0));
    }

    private void MarginType(string name, float margin)
    {
        Theme.SetTypeVariation(name, "MarginContainer");
        foreach (var side in new[] { "left", "top", "right", "bottom" })
            Theme.SetConstant("margin_" + side, name, (int)margin);
    }

    private void BoxType(string name, string baseType, float gap)
    {
        Theme.SetTypeVariation(name, baseType);
        Theme.SetConstant("separation", name, (int)gap);
        Theme.SetConstant("h_separation", name, (int)gap);
        Theme.SetConstant("v_separation", name, (int)gap);
    }

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
    private static StyleBox Carrier(int expand, float padX, float padY, float shiftY)
    {
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
            Theme.SetStylebox("panel", name, Carrier(expand, 0, 0, 0));
        }
        return name;
    }

    public string ButtonType(string fontRole, int padX, int padY, bool sunk, int expand, int iconMax, int separation = 8)
    {
        var name = $"Btn_{fontRole.Replace('.', '_').Replace('-', '_')}_{padX}_{padY}_{(sunk ? 1 : 0)}_{expand}_{iconMax}_{separation}";
        if (!_types.Add(name)) return name;
        Theme.SetTypeVariation(name, "Button");
        FillButtonItems(name, fontRole, Carrier(expand, padX, padY, sunk ? 1 : 0), iconMax, separation);
        return name;
    }

    private void FillButtonItems(string name, string fontRole, StyleBox box, int iconMax, int separation)
    {
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
            Theme.SetColor(state, name, Colors.White);
        Theme.SetConstant("icon_max_width", name, iconMax);
        Theme.SetConstant("h_separation", name, separation);
    }

    // ---------- 下拉框（UI-VISUAL）：闭合的按钮同按钮，走 ui_panel 载体；展开的 PopupMenu 是独立窗口，挂不了着色器 ----------
    /// <summary>OptionButton 的类型变体：载体与按钮一致，右侧留箭头；箭头取白色图标，由 ui_panel 按 role 换色。</summary>
    public string DropdownType(string fontRole, int padX, int padY, Texture2D arrow)
    {
        var name = $"Drop_{fontRole.Replace('.', '_').Replace('-', '_')}_{padX}_{padY}";
        if (!_types.Add(name)) return name;
        Theme.SetTypeVariation(name, "OptionButton");
        FillButtonItems(name, fontRole, Carrier(6, padX, padY, 0), 16, 8);
        Theme.SetIcon("arrow", name, arrow);
        Theme.SetConstant("arrow_margin", name, 2);
        Theme.SetConstant("modulate_arrow", name, 0);
        return name;
    }

    public const string PopupType = "DropdownPopup";
    private StyleBoxFlat? _popupPanel, _popupHover;
    private bool? _popupDark;

    /// <summary>
    /// 下拉展开框的 PopupMenu 类型变体：实底面板、外缘 line.panel、悬停与键盘焦点项 surface.selected 加左侧 2 px primary.line。
    /// 展开框是 Window，没有画布材质，颜色只能是真色值；<see cref="SetPopupDark"/> 在日夜切换后改写这几个对象（只改颜色，不增删项）。
    /// </summary>
    public string DropdownPopupType()
    {
        if (!_types.Add(PopupType)) return PopupType;
        Theme.SetTypeVariation(PopupType, "PopupMenu");
        _popupPanel = new StyleBoxFlat { DrawCenter = true, AntiAliasing = false };
        _popupPanel.SetBorderWidthAll(1);
        _popupPanel.SetContentMarginAll(4);
        _popupHover = new StyleBoxFlat { DrawCenter = true, AntiAliasing = false };
        _popupHover.SetBorderWidth(Side.Left, 2);
        Theme.SetStylebox("panel", PopupType, _popupPanel);
        Theme.SetStylebox("hover", PopupType, _popupHover);
        var fv = Font("body", out var size, out _);
        Theme.SetFont("font", PopupType, fv);
        Theme.SetFontSize("font_size", PopupType, size);
        Theme.SetConstant("v_separation", PopupType, 10);
        Theme.SetConstant("h_separation", PopupType, 8);
        Theme.SetConstant("item_start_padding", PopupType, 12);
        Theme.SetConstant("item_end_padding", PopupType, 12);
        SetPopupDark(false);
        return PopupType;
    }

    public void SetPopupDark(bool dark)
    {
        if (_popupPanel is null || _popupHover is null || _popupDark == dark) return;
        _popupDark = dark;
        var theme = dark ? "dark" : "light";
        _popupPanel.BgColor = _c.Theme(theme, "surface.solid");
        _popupPanel.BorderColor = _c.Theme(theme, "line.panel");
        _popupHover.BgColor = _c.Theme(theme, "surface.selected");
        _popupHover.BorderColor = _c.Theme(theme, "primary.line");
        var ink = _c.Theme(theme, "text.1");
        Theme.SetColor("font_color", PopupType, ink);
        Theme.SetColor("font_hover_color", PopupType, ink);
        Theme.SetColor("font_disabled_color", PopupType, _c.Theme(theme, "text.off"));
        Theme.SetColor("font_accelerator_color", PopupType, _c.Theme(theme, "text.2"));
        Theme.SetColor("font_separator_color", PopupType, _c.Theme(theme, "text.2"));
        // 当前项的单选标记：primary.line 实心点；其余项留同尺寸的透明位，文字对齐
        var on = Dot(_c.Theme(theme, "primary.line"));
        var off = Dot(new Color(0, 0, 0, 0));
        Theme.SetIcon("radio_checked", PopupType, on);
        Theme.SetIcon("radio_checked_disabled", PopupType, on);
        Theme.SetIcon("radio_unchecked", PopupType, off);
        Theme.SetIcon("radio_unchecked_disabled", PopupType, off);
    }

    private static ImageTexture Dot(Color color)
    {
        const int size = 12;
        var image = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        var center = (size - 1) / 2f;
        for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var d = MathF.Sqrt((x - center) * (x - center) + (y - center) * (y - center));
                var cover = Math.Clamp(3.5f - d, 0f, 1f);
                image.SetPixel(x, y, new Color(color.R, color.G, color.B, color.A * cover));
            }
        return ImageTexture.CreateFromImage(image);
    }

    // 滚动条：宽 4，轨道不画，拇指是白色载体，由 ui_panel kind 3 换成 line.scroll（UI-VISUAL）
    private void BuildScrollBar()
    {
        var halfThickness = _t.V2("metrics", "size", "scrollbar").X / 2;
        var grabber = new StyleBoxFlat { BgColor = Colors.White, AntiAliasing = false };
        grabber.ContentMarginLeft = grabber.ContentMarginRight = halfThickness;
        Theme.SetTypeVariation("ScrollThin", "VScrollBar");
        Theme.SetStylebox("scroll", "ScrollThin", new StyleBoxEmpty { ContentMarginLeft = halfThickness, ContentMarginRight = halfThickness });
        Theme.SetStylebox("scroll_focus", "ScrollThin", new StyleBoxEmpty());
        Theme.SetStylebox("grabber", "ScrollThin", grabber);
        Theme.SetStylebox("grabber_highlight", "ScrollThin", grabber);
        Theme.SetStylebox("grabber_pressed", "ScrollThin", grabber);
        var horizontalGrabber = new StyleBoxFlat
        {
            BgColor = Colors.White, AntiAliasing = false,
            ContentMarginTop = halfThickness, ContentMarginBottom = halfThickness,
        };
        Theme.SetTypeVariation("ScrollThinHorizontal", "HScrollBar");
        Theme.SetStylebox("scroll", "ScrollThinHorizontal", new StyleBoxEmpty { ContentMarginTop = halfThickness, ContentMarginBottom = halfThickness });
        Theme.SetStylebox("scroll_focus", "ScrollThinHorizontal", new StyleBoxEmpty());
        Theme.SetStylebox("grabber", "ScrollThinHorizontal", horizontalGrabber);
        Theme.SetStylebox("grabber_highlight", "ScrollThinHorizontal", horizontalGrabber);
        Theme.SetStylebox("grabber_pressed", "ScrollThinHorizontal", horizontalGrabber);
    }

    /// <summary>--ui-export-theme：把生成结果另存为 ui_theme.tres，只作预览与差异审查，运行时不读（UI-VISUAL）。</summary>
    public Error Export(string path) => ResourceSaver.Save(Theme, path);

    public static JsonElement Role(UiTokens t, string role) => t.Get("metrics", "type", "roles", role);
}
