using Campus.Contracts;
using Campus.Contracts.Atlas;
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
public sealed class MapHudContext(MapStyle style)
{
    public MapStyle Style { get; } = style;
    /// <summary>大页的主题：0 晶白 … 1 深屏；与 ui_night 同一语义（扫描前沿的位置）。</summary>
    public float ThemeFront { get; set; }
    public bool PageNight => ThemeFront >= 0.5f;
    /// <summary>常驻件"随主题"（设置项，UI-VISUAL §3.6）；默认 false，即常驻件固定深屏。</summary>
    public bool ResidentFollowsTheme { get; set; }
    /// <summary>最近一次输入来自键盘时才画焦点环（gui/common/show_focus_state_on_pointer_event = 0 的同义）。</summary>
    public bool KeyboardFocus { get; set; }
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

    /// <summary>键盘焦点：控件外 3 px 一圈 1 物理像素的核，外衬 1 px 面板底，两侧各一条冷色色散（UI-VISUAL §5.6）。</summary>
    public static void FocusRing(CanvasItem ci, Rect2 r, MapTone t)
    {
        var core = r.Grow(3);
        MapDraw.Outline(ci, core.Grow(1), 0, t.FocusBacking);
        var a = t.IrisA; a.A *= 0.35f;
        var b = t.IrisB; b.A *= 0.35f;
        MapDraw.Outline(ci, core.Grow(0.7f), 0, a);
        MapDraw.Outline(ci, core.Grow(-0.7f), 0, b);
        MapDraw.Outline(ci, core, 0, t.PrimaryLine);
    }

    public static string GroupName(WaypointGroup g) => g switch
    {
        WaypointGroup.Overview => "总览",
        WaypointGroup.Core => "核心",
        WaypointGroup.South => "南",
        WaypointGroup.West => "西",
        WaypointGroup.North => "北",
        WaypointGroup.East => "东",
        WaypointGroup.Edge => "边界",
        _ => throw new ArgumentOutOfRangeException(nameof(g)),
    };

    public static string GroupEn(WaypointGroup g) => g switch
    {
        WaypointGroup.Overview => "OVERVIEW",
        WaypointGroup.Core => "CORE",
        WaypointGroup.South => "SOUTH",
        WaypointGroup.West => "WEST",
        WaypointGroup.North => "NORTH",
        WaypointGroup.East => "EAST",
        WaypointGroup.Edge => "EDGE",
        _ => throw new ArgumentOutOfRangeException(nameof(g)),
    };

    public static string SeasonName(Season s) => s switch
    {
        Season.Spring => "春",
        Season.Summer => "夏",
        Season.Autumn => "秋",
        Season.Winter => "冬",
        _ => throw new ArgumentOutOfRangeException(nameof(s)),
    };

    public static string WeatherName(WeatherKind? k) => k switch
    {
        null or WeatherKind.Clear => "晴",
        WeatherKind.FewClouds => "少云",
        WeatherKind.Cloudy => "多云",
        WeatherKind.Overcast => "阴",
        WeatherKind.Fog => "雾",
        WeatherKind.LightRain => "小雨",
        WeatherKind.Rain => "雨",
        WeatherKind.Thunderstorm => "雷雨",
        WeatherKind.AfterRain => "初晴",
        WeatherKind.Snow => "雪",
        WeatherKind.AfterSnow => "雪后",
        _ => throw new ArgumentOutOfRangeException(nameof(k)),
    };

    public static string Clock(double hours)
    {
        var minutes = (int)Math.Round(((hours % 24) + 24) % 24 * 60) % (24 * 60);
        return $"{minutes / 60:00}:{minutes % 60:00}";
    }

    /// <summary>标记的类型图标：可传送的标记用主色"通电"，其余用正文色（SYS-ATLAS §2.2、§5）。</summary>
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
