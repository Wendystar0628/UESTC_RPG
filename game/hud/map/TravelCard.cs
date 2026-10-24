using Campus.Contracts;
using Campus.Contracts.Atlas;
using Campus.Hud.Style;
using Campus.Hud.Widgets;
using Godot;

namespace Campus.Hud.Map;

/// <summary>传送卡锚在地图内角落；标题与操作常驻，缩略图、元信息和推荐标签可滚动。</summary>
public partial class TravelCard : Control
{
    private readonly MapHudContext _ctx;
    private readonly Label _title, _subtitle, _meta, _reach, _recommend;
    private readonly VBoxContainer _content, _header;
    private readonly HFlowContainer _actions;
    private readonly ScrollContainer _scroll;
    private readonly Control _panel;
    private readonly DrawHost _thumbnail;
    private MapEntry? _entry;

    public TravelCard(MapHudContext ctx)
    {
        _ctx = ctx;
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var ui = ctx.Ui;
        _content = ui.Layout.FloatingCard(this);
        _panel = (Control)_content.GetParent().GetParent();
        _panel.MouseFilter = MouseFilterEnum.Stop;
        ui.Apply(_panel, new UiFx(Fill: "surface.solid", Line: "line.panel", Chamfer: ui.Tokens.Chamfer("card"), Flags: UiFlags.Float));
        _header = ui.Layout.Stack(_content, groups: false);
        _title = ui.Text(_header, "", "h3");
        _subtitle = ui.Text(_header, "", "small", "text.2");
        _scroll = ui.Layout.Scroll(_content);
        _scroll.VerticalScrollMode = ScrollContainer.ScrollMode.MaximizeFirst;
        var body = ui.Layout.Stack(_scroll, groups: false);
        var aspect = ui.Tokens.V2("metrics", "map", "travelThumbnailAspect");
        var thumbFrame = ui.Layout.Add(body, new AspectRatioContainer { Ratio = aspect.X / aspect.Y, SizeFlagsHorizontal = SizeFlags.ExpandFill });
        thumbFrame.Resized += () => thumbFrame.CustomMinimumSize = new Vector2(0, thumbFrame.Size.X / thumbFrame.Ratio);
        _thumbnail = new DrawHost(DrawThumbnail);
        thumbFrame.AddChild(_thumbnail);
        _meta = ui.Text(body, "", "data", "text.2");
        _reach = ui.Text(body, "", "small", "text.2");
        _recommend = ui.Text(body, "", "small", "text.2");
        _actions = ui.Layout.Flow(_content);
        Primary = ui.Button(_actions, ButtonStyle.Primary, ctx.T("map.travel.teleport"), "button.primary");
        Fly = ui.Button(_actions, ButtonStyle.Secondary, ctx.T("map.travel.fly"));
        Track = ui.Button(_actions, ButtonStyle.Secondary, ctx.T("map.travel.track"));
        Resized += FitBody;
        _header.MinimumSizeChanged += FitBody;
        _actions.MinimumSizeChanged += FitBody;
        Ready += FitBody;
    }

    public TechButton Primary { get; }
    public TechButton Fly { get; }
    public TechButton Track { get; }
    public MapEntry? Entry => _entry;
    public TravelStyle PrimaryStyle { get; private set; } = TravelStyle.Teleport;
    public Rect2 CardRect => new(_panel.GlobalPosition - GlobalPosition, _panel.Size);

    private void FitBody()
    {
        var reserved = _header.GetCombinedMinimumSize().Y + _actions.GetCombinedMinimumSize().Y
            + 2 * _ctx.Ui.Tokens.Space("panelPad") + 2 * _content.GetThemeConstant("separation");
        _scroll.CustomMaximumSize = new Vector2(-1, Math.Max(0, Size.Y - reserved));
    }

    public void Show(MapEntry entry, string meta, bool here, bool tracked, TravelStyle primary)
    {
        _entry = entry;
        _title.Text = _ctx.Name(entry.Name);
        _subtitle.Text = entry.Name.En;
        _meta.Text = meta;
        _reach.Text = _ctx.T(entry.Marker.Teleport switch
        {
            null => "map.travel.track_only",
            { Stand: null } => "map.travel.fly_only",
            _ => "map.travel.both_modes",
        });
        _recommend.Text = entry.Recommend is { } r
            ? _ctx.T("map.travel.recommend", ("season", MapHudContext.SeasonName(r.Season)),
                ("clock", WorldTime.FormatClock(r.Clock, roundToMinute: true)), ("weather", MapHudContext.WeatherName(r.Weather)))
            : "";
        _recommend.Visible = _recommend.Text.Length > 0;
        PrimaryStyle = primary;
        Primary.Visible = Fly.Visible = entry.Travelable;
        Primary.Text = _ctx.T(here ? "map.travel.here" : primary == TravelStyle.Teleport ? "map.travel.teleport" : "map.travel.fly");
        Fly.Text = _ctx.T(primary == TravelStyle.Teleport ? "map.travel.fly" : "map.travel.teleport");
        Primary.Off = Fly.Off = here;
        Track.Text = _ctx.T(tracked ? "map.travel.untrack" : "map.travel.track");
        foreach (var button in new[] { Primary, Fly, Track })
            button.AccessibilityName = _ctx.T("map.travel.action_accessible", ("action", button.Text), ("name", _ctx.Name(entry.Name)));
        AccessibilityName = _ctx.Name(entry.Name);
        Visible = true;
        FitBody();
        _thumbnail.QueueRedraw();
    }

    private void DrawThumbnail(DrawHost h)
    {
        var t = _ctx.ToneFor(this);
        var rect = new Rect2(Vector2.Zero, h.Size);
        h.DrawRect(rect, t.Sunk);
        MapDraw.Outline(h, rect.Grow(-0.5f), 0, t.Hair);
        if (_entry is { } entry) _ctx.Icon(h, entry, rect.GetCenter(), 28, t.TextOff, _ctx.Style.Metrics.IconStroke, false);
    }
}
