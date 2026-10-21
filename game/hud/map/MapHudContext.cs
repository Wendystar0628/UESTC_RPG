using Campus.Contracts;
using Campus.Contracts.Atlas;
using Campus.Hud.Widgets;
using Godot;

namespace Campus.Hud.Map;

/// <summary>地图界面的一条标记；展示元数据与可达模式均由 Atlas 发布。</summary>
public sealed record MapEntry(AtlasMarker Marker)
{
    public MarkerId Id => Marker.Id;
    public Vector2 At => MapView.Flat(Marker.At);
    public bool Travelable => Marker.Teleport is not null;
    public bool FlyOnly => Marker.Teleport is { Stand: null };
    public LocalizedName Name => Marker.Name;
    public WaypointGroup? Group => Marker.Group;
    public WaypointRecommend? Recommend => Marker.Recommend;
}

/// <summary>小地图与大地图共享的状态与令牌；不是 Godot 节点。</summary>
public sealed class MapHudContext(MapStyle style, UiKit ui)
{
    public MapStyle Style { get; } = style;
    public UiKit Ui { get; } = ui;
    public string T(string key) => UiKit.T(key);
    public string T(string key, params (string Name, string Value)[] args) => UiKit.T(key, args);
    public string Name(LocalizedName name) => TranslationServer.GetLocale().StartsWith("zh", StringComparison.OrdinalIgnoreCase) || name.En.Length == 0 ? name.Zh : name.En;

    /// <summary>地图的图标钮：SVG 图标经 UiKit 的图标材质随昼夜与常驻设置换色（UI-VISUAL-COMPONENTS）。</summary>
    public TechButton IconButton(Control parent, string icon, string key, bool pin = false)
    {
        var button = Ui.Button(parent, ButtonStyle.Icon, "", icon: icon, pin: pin);
        button.AccessibilityName = T(key);
        button.TooltipText = T(key);
        return button;
    }
    /// <summary>大页的主题：0 晶白 … 1 深屏；与 ui_night 同一语义（扫描前沿的位置）。</summary>
    public float ThemeFront { get; set; }
    public bool PageNight => ThemeFront >= 0.5f;
    /// <summary>常驻件"随主题"（设置项，UI-VISUAL）；默认 false，即常驻件固定深屏。</summary>
    public bool ResidentFollowsTheme { get; set; }
    public HashSet<Control> Resident { get; } = [];

    public bool ResidentNight => !ResidentFollowsTheme || PageNight;
    public MapTone Page => Style.Tone(PageNight);
    public MapTone ResidentTone => Style.Tone(ResidentNight);

    public MapTone ToneFor(Control c)
    {
        for (Node? n = c; n is not null; n = n.GetParent())
            if (n is Control k && Resident.Contains(k)) return ResidentTone;
        return Page;
    }

    public static string GroupName(WaypointGroup g) => g switch
    {
        WaypointGroup.Overview => UiKit.T("map.group.overview"),
        WaypointGroup.Core => UiKit.T("map.group.core"),
        WaypointGroup.South => UiKit.T("map.group.south"),
        WaypointGroup.West => UiKit.T("map.group.west"),
        WaypointGroup.North => UiKit.T("map.group.north"),
        WaypointGroup.East => UiKit.T("map.group.east"),
        WaypointGroup.Edge => UiKit.T("map.group.edge"),
        _ => throw new ArgumentOutOfRangeException(nameof(g)),
    };

    public static string SeasonName(Season s) => s switch
    {
        Season.Spring => UiKit.T("hud.season.spring"),
        Season.Summer => UiKit.T("hud.season.summer"),
        Season.Autumn => UiKit.T("hud.season.autumn"),
        Season.Winter => UiKit.T("hud.season.winter"),
        _ => throw new ArgumentOutOfRangeException(nameof(s)),
    };

    public static string WeatherName(WeatherKind? k) => k switch
    {
        null or WeatherKind.Clear => UiKit.T("weather.kind.clear"),
        WeatherKind.FewClouds => UiKit.T("weather.kind.few"),
        WeatherKind.Cloudy => UiKit.T("weather.kind.cloudy"),
        WeatherKind.Overcast => UiKit.T("weather.kind.overcast"),
        WeatherKind.Fog => UiKit.T("weather.kind.fog"),
        WeatherKind.LightRain => UiKit.T("weather.kind.drizzle"),
        WeatherKind.Rain => UiKit.T("weather.kind.rain"),
        WeatherKind.Thunderstorm => UiKit.T("weather.kind.storm"),
        WeatherKind.AfterRain => UiKit.T("weather.kind.clearing"),
        WeatherKind.Snow => UiKit.T("weather.kind.snow"),
        WeatherKind.AfterSnow => UiKit.T("weather.kind.after_snow"),
        _ => throw new ArgumentOutOfRangeException(nameof(k)),
    };

    /// <summary>标记的类型图标：可传送的标记用主色"通电"，其余用正文色（SYS-ATLAS）。</summary>
    public void Icon(CanvasItem ci, MapEntry e, Vector2 c, float size, Color ink, float stroke, bool solid)
    {
        var m = e.Marker;
        switch (m.Kind)
        {
            case MarkerKind.Anchor: MapDraw.AnchorIcon(ci, c, size, ink, stroke, m.Form == AnchorForm.Kiosk, solid); break;
            case MarkerKind.Viewpoint: MapDraw.ViewpointIcon(ci, c, size, ink, stroke, e.FlyOnly, solid); break;
            case MarkerKind.Interior: MapDraw.InteriorIcon(ci, c, size, ink, stroke); break;
            case MarkerKind.Route: MapDraw.RouteIcon(ci, c, size, ink, stroke); break;
            case MarkerKind.Landmark: MapDraw.LandmarkIcon(ci, c, size, ink, stroke); break;
            case MarkerKind.Facility: MapDraw.FacilityIcon(ci, c, size, ink, stroke); break;
            case MarkerKind.Custom: MapDraw.PinIcon(ci, c, size, ink, stroke, PinShapeOf(m)); break;
            case MarkerKind.Quest: break;
            default: throw new ArgumentOutOfRangeException(nameof(e));
        }
    }

    /// <summary>自定义标记的形状不在 AtlasMarker 里，按 "pin:&lt;整数&gt;" 回查 Pins。</summary>
    public Func<MarkerId, PinShape>? PinShapeLookup { get; set; }
    private PinShape PinShapeOf(AtlasMarker m) => PinShapeLookup?.Invoke(m.Id) ?? PinShape.Diamond;

    public static int LayerOf(MarkerKind k) => k switch
    {
        MarkerKind.Anchor => 0,
        MarkerKind.Viewpoint => 1,
        MarkerKind.Interior => 2,
        MarkerKind.Route => 3,
        MarkerKind.Landmark => 4,
        MarkerKind.Facility => 5,
        MarkerKind.Custom => 6,
        MarkerKind.Quest => 7,
        _ => throw new ArgumentOutOfRangeException(nameof(k)),
    };
}
