using Campus.Contracts.Atlas;
using Godot;

namespace Campus.Hud.Map;

/// <summary>
/// 传送卡（SYS-ATLAS §4.5）：宽 372 的实底卡片，左上切角；缩略图 336×189、名称 h3 与英文名、等宽元信息、
/// 标签行（可达模式、推荐状态）、按钮。主按钮默认【传送】，另有【飞过去】【追踪】；就在这里（20 m 内）时
/// 两个前往按钮灰显、写"此处"。卡片不提供"应用推荐"。不是网页地图那种地点卡片（INV-1）。
/// </summary>
public partial class TravelCard : Control
{
    public const float Width = 372;
    public const float Height = 400;
    private const float Pad = 18;
    private static readonly Vector2 ThumbSize = new(336, 189);

    private readonly MapHudContext _ctx;
    private MapEntry? _entry;
    private string _meta = "";
    private string _recommend = "";
    private string _reach = "";

    public TravelCard(MapHudContext ctx)
    {
        _ctx = ctx;
        Size = new(Width, Height);
        MouseFilter = MouseFilterEnum.Stop;
        var y = Height - Pad - 44;
        Primary = new MapButton(MapButtonKind.Primary, ctx) { Position = new(Pad, y), Size = new(152, 44), Text = "传送" };
        Fly = new MapButton(MapButtonKind.Secondary, ctx) { Position = new(Pad + 160, y + 4), Size = new(84, 40), Text = "飞过去" };
        Track = new MapButton(MapButtonKind.Secondary, ctx) { Position = new(Pad + 252, y + 4), Size = new(84, 40), Text = "追踪" };
        AddChild(Primary);
        AddChild(Fly);
        AddChild(Track);
    }

    public MapButton Primary { get; }
    public MapButton Fly { get; }
    public MapButton Track { get; }
    public MapEntry? Entry => _entry;
    /// <summary>主按钮是【传送】还是【飞过去】（设置项"地图上的前往方式"，SYS-ATLAS §12）。</summary>
    public TravelStyle PrimaryStyle { get; private set; } = TravelStyle.Teleport;

    public void Show(MapEntry entry, string meta, string reach, string recommend, bool here, bool tracked, TravelStyle primary)
    {
        _entry = entry;
        _meta = meta;
        _reach = reach;
        _recommend = recommend;
        PrimaryStyle = primary;
        var travel = entry.Travelable;
        Primary.Visible = travel;
        Fly.Visible = travel;
        Primary.Text = here ? "此处" : primary == TravelStyle.Teleport ? "传送" : "飞过去";
        Fly.Text = primary == TravelStyle.Teleport ? "飞过去" : "传送";
        Primary.Disabled = here;
        Fly.Disabled = here;
        Track.Text = tracked ? "取消追踪" : "追踪";
        Track.Position = travel ? new(Pad + 252, Track.Position.Y) : new(Pad, Track.Position.Y);
        Primary.AccessibilityName = Primary.Text + "：" + entry.Name.Zh;
        Fly.AccessibilityName = Fly.Text + "：" + entry.Name.Zh;
        Track.AccessibilityName = Track.Text + "：" + entry.Name.Zh;
        AccessibilityName = entry.Name.Zh;
        Visible = true;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_entry is not { } e) return;
        var t = _ctx.ToneFor(this);
        var m = _ctx.Style.Metrics;
        var r = new Rect2(Vector2.Zero, Size);
        MapDraw.FloatShadow(this, r, m.ChamferCard, t.ShadowFloat, 8, 24);
        MapDraw.Panel(this, r, m.ChamferCard, t.Solid, t.PanelLine);
        // 选中：顶边一条 1.5 px 的冷色色散线（UI-VISUAL §5.6）
        var a = t.IrisA; a.A *= 0.35f;
        var b = t.IrisB; b.A *= 0.35f;
        DrawLine(new(m.ChamferCard, 0.75f), new(Width, 0.75f), a, 0.75f);
        DrawLine(new(m.ChamferCard, 1.5f), new(Width, 1.5f), b, 0.75f);
        // 缩略图位（WP-27 交图前是一块凹面，中央是类型图标）
        var thumb = new Rect2(Pad, Pad, ThumbSize);
        DrawRect(thumb, t.Sunk);
        MapDraw.Outline(this, thumb.Grow(-0.5f), 0, t.Hair);
        _ctx.Icon(this, e, thumb.GetCenter(), 28, t.TextOff, m.IconStroke, false);
        var h3 = _ctx.Style.Fonts["h3"];
        var small = _ctx.Style.Fonts["small"];
        var data = _ctx.Style.Fonts["data"];
        var y = thumb.End.Y + 30;
        MapDraw.Text(this, h3, new(Pad, y), e.Name.Zh, t.Text1);
        MapDraw.Text(this, small, new(Pad, y + 22), e.Name.En, t.Text2);
        MapDraw.Text(this, data, new(Pad, y + 46), _meta, t.Text2);
        // 标签行
        var x = Pad;
        var tagTop = y + 58;
        x = Tag(x, tagTop, _reach, t, small, null);
        if (_recommend.Length > 0) Tag(x + 8, tagTop, _recommend, t, small, t.Now);
    }

    private float Tag(float x, float top, string text, MapTone t, MapFont f, Color? dot)
    {
        var dotW = dot is { A: > 0 } ? 14f : 0f;
        var w = MathF.Ceiling(f.Width(text)) + 20 + dotW;
        var r = new Rect2(x, top, w, 24);
        MapDraw.Outline(this, r.Grow(-0.5f), 0, t.Control);
        if (dot is { A: > 0 } now)
        {
            // 推荐时刻的小点：界面里唯一的暖色点（"太阳 / 此刻"），晶白上加 1.5 px 墨描边
            var c = new Vector2(x + 13, top + 12);
            DrawCircle(c, 3.5f, now, true, -1, true);
            if (t.NowOutline.A > 0) DrawArc(c, 3.5f, 0, Mathf.Tau, 16, t.NowOutline, 1.5f, true);
        }
        MapDraw.Text(this, f, new(x + 10 + dotW, top + 17), text, t.Text2);
        return x + w;
    }
}
