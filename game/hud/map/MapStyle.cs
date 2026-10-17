using System.Text.Json;
using Campus.Contracts;
using Godot;

namespace Campus.Hud.Map;

/// <summary>
/// 地图界面的令牌入口：颜色只来自 ui.json（map 段与 themes 段，UI-VISUAL），几何与字号来自 ui_metrics.json
/// （WP-35 的非色令牌，UI-VISUAL）。两个文件都只读一次；代码里不写色值。
/// </summary>
public sealed class MapStyle
{
    public const string PalettePath = "res://style/palette/ext/ui.json";
    public const string MetricsPath = "res://hud/style/ui_metrics.json";

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

    /// <summary>读不到令牌时返回 null：界面整体不显示，只记一条警告，不挡主线（ENG-WP）。</summary>
    public static MapStyle? TryLoad()
    {
        if (!Godot.FileAccess.FileExists(PalettePath) || !Godot.FileAccess.FileExists(MetricsPath))
        {
            GD.PushWarning($"WP-48b: UI tokens missing ({PalettePath}, {MetricsPath}); map HUD stays hidden.");
            return null;
        }
        var document = JsonSerializer.Deserialize<UiPaletteDocument>(Godot.FileAccess.GetFileAsBytes(PalettePath), ContractJson.Options)
            ?? throw new InvalidDataException("ui.json is empty.");
        // 地图界面与其他消费者使用同一份完整场景目录。
        var palette = new UiPalette(document, Campus.Kernel.Registry.Kernel.Get<IPaletteCatalogSource>().Catalog);
        using var metrics = JsonDocument.Parse(Godot.FileAccess.GetFileAsBytes(MetricsPath));
        var root = metrics.RootElement;
        return new MapStyle(MapTone.From(palette, "light", false), MapTone.From(palette, "dark", true),
            MapTable(palette, "light"), MapTable(palette, "dark"), MapMetrics.From(root), MapFonts.From(root));
    }

    private static Color[] MapTable(UiPalette palette, string theme)
    {
        var table = new Color[ShaderPaletteSize];
        for (var i = 0; i < Roles.Length; i++)
        {
            var (token, part) = Roles[i];
            table[i] = ToColor(part is null ? palette.ResolveMap(theme, token) : palette.ResolveMap(theme, token, part));
        }
        return table;
    }

    internal static Color ToColor(System.Numerics.Vector4 v) => new(v.X, v.Y, v.Z, v.W);
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

    internal static MapTone From(UiPalette p, string theme, bool night)
    {
        Color T(string token) => MapStyle.ToColor(p.ResolveTheme(theme, token));
        Color M(string token, params string[] parts) => MapStyle.ToColor(p.ResolveMap(theme, token, parts));
        // now 借自场景色 window.warm，要经场景色名登记表解析；登记表还没入库时不画暖色点（只少一枚点，不影响读数）
        static Color Scene(Func<Color> resolve)
        {
            try { return resolve(); }
            catch (FormatException) { return new Color(0, 0, 0, 0); }
        }
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
            Now = Scene(() => T("now")),
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

/// <summary>ui_metrics.json 里地图界面用到的几何、主题切换与动效时长（逻辑像素，1920×1080 基准）。</summary>
public sealed record MapMetrics(
    float BoundDash, float BoundGap, float HaloWidth, float PlayerLine, float PlayerGlowDark,
    float ToDarkBelowDeg, float ToLightAboveDeg, float FrontWidth, float SweepMs,
    float ChamferPage, float ChamferPanel, float ChamferCard, float Via, float IconStroke, float Indicator,
    float ScrimMs, float PageInMs, float PageOutMs, float ReducedFadeMs, float GlowMax)
{
    internal static MapMetrics From(JsonElement root)
    {
        var m = root.GetProperty("metrics");
        var map = m.GetProperty("map");
        var dash = map.GetProperty("map.bound").GetProperty("dash");
        var player = map.GetProperty("map.player");
        var theme = root.GetProperty("themeSwitch");
        var chamfer = m.GetProperty("shape").GetProperty("chamfer");
        var durations = root.GetProperty("motion").GetProperty("durations");
        var page = durations.GetProperty("page");
        return new(dash[0].GetSingle(), dash[1].GetSingle(), map.GetProperty("map.label.halo").GetProperty("width").GetSingle(),
            player.GetProperty("lineWidth").GetSingle(), player.GetProperty("glow").GetProperty("dark").GetSingle(),
            theme.GetProperty("toDarkBelowDeg").GetSingle(), theme.GetProperty("toLightAboveDeg").GetSingle(),
            theme.GetProperty("frontWidth").GetSingle(), theme.GetProperty("sweepMs").GetSingle(),
            chamfer.GetProperty("page").GetSingle(), chamfer.GetProperty("panel").GetSingle(), chamfer.GetProperty("card").GetSingle(),
            m.GetProperty("shape").GetProperty("signatureVia").GetSingle(), m.GetProperty("line").GetProperty("iconStroke").GetSingle(),
            m.GetProperty("line").GetProperty("indicator").GetSingle(), durations.GetProperty("scrim").GetSingle(),
            page.GetProperty("in")[1].GetSingle(), page.GetProperty("out")[0].GetSingle(),
            root.GetProperty("motion").GetProperty("reduced").GetProperty("fadeMax").GetSingle(),
            m.GetProperty("glow").GetProperty("max").GetSingle());
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
/// 字体按 ui_metrics.json 的 type 段组装：项目字体文件（WP-35 登记在 game/assets/hud/fonts/）在时直接用；
/// 还没入库时按家族名取系统字体，缺字由系统回退补上。
/// </summary>
public sealed class MapFonts
{
    private const string FontDir = "res://assets/hud/fonts/";
    private readonly Dictionary<string, MapFont> _roles = new(StringComparer.Ordinal);
    public MapFont this[string role] => _roles[role];

    internal static MapFonts From(JsonElement root)
    {
        var type = root.GetProperty("metrics").GetProperty("type");
        var families = type.GetProperty("families");
        var chains = type.GetProperty("chains");
        var cache = new Dictionary<string, Font>(StringComparer.Ordinal);
        Font Face(string family, string weight)
        {
            var key = family + "/" + weight;
            if (cache.TryGetValue(key, out var hit)) return hit;
            var f = families.GetProperty(family);
            Font font;
            var files = f.GetProperty("files");
            var file = files.TryGetProperty(weight, out var w) ? w.GetString() : files.GetProperty("Regular").GetString();
            var path = FontDir + file;
            if (file is not null && ResourceLoader.Exists(path)) font = GD.Load<Font>(path);
            else
            {
                font = new SystemFont
                {
                    FontNames = [f.GetProperty("name").GetString() ?? "", family == "mono" ? "monospace" : "sans-serif"],
                    FontWeight = weight switch { "Light" => 300, "Medium" => 500, _ => 400 },
                    SubpixelPositioning = TextServer.SubpixelPositioning.Auto,
                };
            }
            cache[key] = font;
            return font;
        }
        var fonts = new MapFonts();
        foreach (var role in type.GetProperty("roles").EnumerateObject())
        {
            var r = role.Value;
            var weight = r.GetProperty("weight").GetString() ?? "Regular";
            var chain = chains.GetProperty(r.GetProperty("chain").GetString() ?? "body").EnumerateArray().Select(e => e.GetString() ?? "").ToArray();
            var size = r.GetProperty("size").GetInt32();
            var primary = Face(chain[0], weight);
            var variation = new FontVariation { BaseFont = primary };
            variation.SpacingGlyph = (int)MathF.Round(r.GetProperty("tracking").GetSingle() * size);
            var fallbacks = new Godot.Collections.Array<Font>();
            foreach (var family in chain.Skip(1)) fallbacks.Add(Face(family, weight));
            variation.Fallbacks = fallbacks;
            var upper = r.TryGetProperty("upper", out var u) && u.GetBoolean();
            fonts._roles[role.Name] = new MapFont(variation, size, upper);
        }
        return fonts;
    }
}
