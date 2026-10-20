using Campus.Hud.Style;
using Campus.Hud.Widgets;
using Godot;

namespace Campus.Hud.Map;

/// <summary>
/// 地图专有颜色来自 ui.json；几何与字体复用界面已加载的 UiTokens 和唯一 Theme。
/// </summary>
public sealed class MapStyle
{
    /// <summary>map_base 色表的顺序：与 map_base.gdshader 的 C_* 常量一一对应，只在末尾追加。</summary>
    public static readonly (string Token, string? Part)[] Roles =
    [
        ("map.base", "fill"), ("map.base", "cross"), ("map.outskirts", "line"), ("map.outskirts", "fill"), ("map.edge", "line"),
        ("map.land", null), ("map.green", "spring"), ("map.green", "summer"), ("map.green", "autumn"), ("map.green", "winter"),
        ("map.green", "dots"), ("map.water", "fill"), ("map.water", "hatch"), ("map.water", "shore"), ("map.plaza", null),
        ("map.road", "fill"), ("map.road", "line"), ("map.sport", "line"), ("map.sport", "inner"), ("map.building", "fill"),
        ("map.building", "line"), ("map.landmark", "fill"), ("map.landmark", "line"), ("map.bound", "line"),
        ("map.interior", "fill"), ("map.interior", "line"),
    ];
    public const int ShaderPaletteSize = 32;

    private MapStyle(MapTone day, MapTone night, Color[] mapDay, Color[] mapNight, MapMetrics metrics, MapFonts fonts)
    {
        Day = day; Night = night; MapDay = mapDay; MapNight = mapNight; Metrics = metrics; Fonts = fonts;
    }

    public MapTone Day { get; }
    public MapTone Night { get; }
    public Color[] MapDay { get; }
    public Color[] MapNight { get; }
    public MapMetrics Metrics { get; }
    public MapFonts Fonts { get; }
    public MapTone Tone(bool night) => night ? Night : Day;

    public static MapStyle Load(UiKit ui)
    {
        var palette = ui.Colors;
        return new MapStyle(MapTone.From(palette, "light", false), MapTone.From(palette, "dark", true),
            MapTable(palette, "light"), MapTable(palette, "dark"), MapMetrics.From(ui.Tokens), new MapFonts(ui.Theme));
    }

    private static Color[] MapTable(UiColors palette, string theme)
    {
        var table = new Color[ShaderPaletteSize];
        for (var i = 0; i < Roles.Length; i++)
        {
            var (token, part) = Roles[i];
            table[i] = part is null ? palette.Map(theme, token) : palette.Map(theme, token, part);
        }
        return table;
    }
}

/// <summary>一套主题（晶白或深屏）下地图界面要用的语义色。</summary>
public sealed class MapTone
{
    public bool IsNight { get; private init; }
    public Color Panel { get; private init; }
    public Color Solid { get; private init; }
    public Color Raised { get; private init; }
    public Color Sunk { get; private init; }
    public Color Selected { get; private init; }
    public Color Hover { get; private init; }
    public Color RowHover { get; private init; }
    public Color Text1 { get; private init; }
    public Color Text2 { get; private init; }
    public Color TextOff { get; private init; }
    public Color Accent { get; private init; }
    public Color Hair { get; private init; }
    public Color PanelLine { get; private init; }
    public Color Control { get; private init; }
    public Color Highlight { get; private init; }
    public Color CardHover { get; private init; }
    public Color PrimaryFill { get; private init; }
    public Color PrimaryHover { get; private init; }
    public Color PrimaryPress { get; private init; }
    public Color PrimaryInk { get; private init; }
    public Color PrimaryLine { get; private init; }
    public Color Graphic { get; private init; }
    public Color LedOn { get; private init; }
    public Color LedHalo { get; private init; }
    public Color Glow { get; private init; }
    public Color Scrim { get; private init; }
    public Color ShadowFloat { get; private init; }
    public Color IrisA { get; private init; }
    public Color IrisB { get; private init; }
    public Color FocusBacking { get; private init; }
    public Color Trace { get; private init; }
    public Color Now { get; private init; }
    public Color NowOutline { get; private init; }
    public Color Land { get; private init; }
    public Color Base { get; private init; }
    public Color Interior { get; private init; }
    public Color InteriorLine { get; private init; }
    public Color LabelZh { get; private init; }
    public Color LabelEn { get; private init; }
    public Color LabelHalo { get; private init; }
    public Color PlayerFill { get; private init; }
    public Color PlayerLine { get; private init; }
    public Color PlayerCone { get; private init; }

    internal static MapTone From(UiColors p, string theme, bool night)
    {
        Color T(string token) => p.Theme(theme, token);
        Color M(string token, params string[] parts) => p.Map(theme, token, parts);
        return new MapTone
        {
            IsNight = night,
            Panel = T("surface.panel"),
            Solid = T("surface.solid"),
            Raised = T("surface.raised"),
            Sunk = T("surface.sunk"),
            Selected = T("surface.selected"),
            Hover = T("surface.hover"),
            RowHover = T("surface.row.hover"),
            Text1 = T("text.1"),
            Text2 = T("text.2"),
            TextOff = T("text.off"),
            Accent = T("text.accent"),
            Hair = T("line.hair"),
            PanelLine = T("line.panel"),
            Control = T("line.control"),
            Highlight = T("line.highlight"),
            CardHover = T("line.card.hover"),
            PrimaryFill = T("primary.fill"),
            PrimaryHover = T("primary.hover"),
            PrimaryPress = T("primary.press"),
            PrimaryInk = T("primary.ink"),
            PrimaryLine = T("primary.line"),
            Graphic = T("graphic"),
            LedOn = T("led.on"),
            LedHalo = T("led.halo"),
            Glow = T("glow"),
            Scrim = T("scrim"),
            ShadowFloat = T("shadow.float"),
            IrisA = T("iris.a"),
            IrisB = T("iris.b"),
            FocusBacking = T("focus.backing"),
            Trace = T("trace"),
            Now = T("now"),
            NowOutline = T("now.outline"),
            Land = M("map.land"),
            Base = M("map.base", "fill"),
            Interior = M("map.interior", "fill"),
            InteriorLine = M("map.interior", "line"),
            LabelZh = M("map.label", "zh"),
            LabelEn = M("map.label", "en"),
            LabelHalo = M("map.label.halo", "color"),
            PlayerFill = M("map.player", "fill"),
            PlayerLine = M("map.player", "line"),
            PlayerCone = M("map.player", "cone"),
        };
    }
}

/// <summary>地图画布用到的几何、主题扫描前沿宽与进场时长（逻辑像素，1920×1080 基准）。</summary>
public sealed record MapMetrics(
    float BoundDash, float BoundGap, float HaloWidth, float PlayerLine, float PlayerGlowDark,
    float FrontWidth,
    float ChamferPage, float IconStroke, float PageInMs, float GlowMax)
{
    internal static MapMetrics From(UiTokens t)
    {
        var dash = t.Get("metrics", "map", "map.bound", "dash");
        var pageIn = t.Get("motion", "durations", "page", "in");
        return new(dash[0].GetSingle(), dash[1].GetSingle(), t.F("metrics", "map", "map.label.halo", "width"),
            t.F("metrics", "map", "map.player", "lineWidth"), t.F("metrics", "map", "map.player", "glow", "dark"),
            t.F("themeSwitch", "frontWidth"),
            t.Chamfer("page"), t.F("metrics", "line", "iconStroke"), pageIn[1].GetSingle(),
            t.F("metrics", "glow", "max"));
    }
}

/// <summary>一个字号角色：字体链、字号与字距（UI-VISUAL）。</summary>
public sealed record MapFont(Font Font, int Size, bool Upper)
{
    public float Ascent => Font.GetAscent(Size);
    public float Height => Font.GetHeight(Size);
    public float Width(string text) => Font.GetStringSize(Upper ? text.ToUpperInvariant() : text, HorizontalAlignment.Left, -1, Size).X;
    public string Shape(string text) => Upper ? text.ToUpperInvariant() : text;
}

/// <summary>
/// 地图画布的字体角色薄包装：引用唯一 Theme 已生成的字体与字号。
/// </summary>
public sealed class MapFonts
{
    private readonly UiThemeBuilder _theme;
    private readonly Dictionary<string, MapFont> _roles = new(StringComparer.Ordinal);
    internal MapFonts(UiThemeBuilder theme) => _theme = theme;
    public MapFont this[string role]
    {
        get
        {
            if (_roles.TryGetValue(role, out var font)) return font;
            var type = UiThemeBuilder.TextType(role);
            font = new MapFont(_theme.Theme.GetFont("font", type), _theme.Theme.GetFontSize("font_size", type), _theme.Upper(role));
            _roles.Add(role, font);
            return font;
        }
    }
}
