using Campus.Contracts.Atlas;
using Godot;

namespace Campus.Hud.Map;

/// <summary>
/// 版图式的绘制原语（UI-VISUAL §5）：只切左上角的面板、1 物理像素发丝线（宽度传 −1，任何缩放下都是 1 物理像素）、
/// 过孔、走线、键帽，以及地图标记与界面图标（UI-VISUAL §6：网格 24、活动区 20、线宽 1.5、方头、只用 0/45/90°）。
/// </summary>
public static class MapDraw
{
    public const float Hair = -1f;

    public static Vector2[] ChipPoints(Rect2 r, float chamfer)
    {
        var p = r.Position;
        var e = r.End;
        return chamfer <= 0
            ? [p, new(e.X, p.Y), e, new(p.X, e.Y)]
            : [new(p.X + chamfer, p.Y), new(e.X, p.Y), e, new(p.X, e.Y), new(p.X, p.Y + chamfer)];
    }

    /// <summary>面板：平涂、外缘发丝线、顶边 1 px 内高光（UI-VISUAL §5.7）。</summary>
    public static void Panel(CanvasItem ci, Rect2 r, float chamfer, Color fill, Color line, Color? highlight = null)
    {
        var pts = ChipPoints(r, chamfer);
        ci.DrawColoredPolygon(pts, fill);
        if (highlight is { } h && h.A > 0)
            ci.DrawLine(new(r.Position.X + chamfer, r.Position.Y + 1.5f), new(r.End.X - 1, r.Position.Y + 1.5f), h, Hair);
        Outline(ci, r.Grow(-0.5f), Math.Max(0, chamfer - 0.2f), line);
    }

    public static void Outline(CanvasItem ci, Rect2 r, float chamfer, Color line)
    {
        if (line.A <= 0) return;
        var pts = ChipPoints(r, chamfer);
        var closed = new Vector2[pts.Length + 1];
        pts.CopyTo(closed, 0);
        closed[^1] = pts[0];
        ci.DrawPolyline(closed, line, Hair);
    }

    /// <summary>浮层影（shadow.float）：只给卡片这类浮层，带蓝的墨，几层衰减的平移矩形近似模糊。</summary>
    public static void FloatShadow(CanvasItem ci, Rect2 r, float chamfer, Color shadow, float dy, float blur)
    {
        if (shadow.A <= 0) return;
        const int Steps = 6;
        for (var i = Steps; i >= 1; i--)
        {
            var t = i / (float)Steps;
            var grow = blur * 0.5f * t;
            var c = shadow; c.A *= (1 - t) * (1 - t) * 0.5f + 0.06f;
            ci.DrawColoredPolygon(ChipPoints(new Rect2(r.Position + new Vector2(0, dy * (1 - t * 0.5f)), r.Size).Grow(grow), chamfer + grow * 0.4f), c);
        }
    }

    public static void Via(CanvasItem ci, Vector2 c, float diameter, Color color, bool filled)
    {
        var r = diameter * 0.5f;
        if (filled) ci.DrawCircle(c, r, color, true, -1, true);
        else ci.DrawArc(c, r - 0.5f, 0, Mathf.Tau, 20, color, 1f, true);
    }

    /// <summary>过孔灯：Ø6 的核，深屏加 2 px 晕（UI-VISUAL §5.4）。</summary>
    public static void Led(CanvasItem ci, Vector2 c, MapTone t, bool on)
    {
        if (on && t.LedHalo.A > 0)
        {
            var halo = t.LedHalo; halo.A = 0.22f;
            ci.DrawCircle(c, 5f, halo, true, -1, true);
        }
        Via(ci, c, 6, on ? t.LedOn : t.Control, on);
    }

    public static void Text(CanvasItem ci, MapFont f, Vector2 baseline, string text, Color color, HorizontalAlignment align = HorizontalAlignment.Left, float width = -1)
        => ci.DrawString(f.Font, baseline, f.Shape(text), align, width, f.Size, color);

    /// <summary>压在地图上的字：先画 2 px 晕，再画字（SYS-ATLAS §2.5）。</summary>
    public static void HaloText(CanvasItem ci, MapFont f, Vector2 baseline, string text, Color color, Color halo, float haloWidth)
    {
        ci.DrawStringOutline(f.Font, baseline, f.Shape(text), HorizontalAlignment.Left, -1, f.Size, (int)MathF.Round(haloWidth * 2), halo);
        ci.DrawString(f.Font, baseline, f.Shape(text), HorizontalAlignment.Left, -1, f.Size, color);
    }

    /// <summary>键帽：凸起面、3 px 圆角近似为小斜角、等宽字（UI-VISUAL §7.2）。</summary>
    public static float Keycap(CanvasItem ci, MapFont f, Vector2 topLeft, string key, MapTone t)
    {
        var w = MathF.Max(24, MathF.Ceiling(f.Width(key)) + 14);
        var r = new Rect2(topLeft, new Vector2(w, 24));
        ci.DrawColoredPolygon(Bevel(r, 3), t.Raised);
        var closed = Bevel(r.Grow(-0.5f), 2.6f);
        ci.DrawPolyline([.. closed, closed[0]], t.Control, Hair);
        ci.DrawLine(new(r.Position.X + 3, r.End.Y - 1.5f), new(r.End.X - 3, r.End.Y - 1.5f), t.Control, Hair);
        Text(ci, f, new(r.Position.X, r.Position.Y + 12 + f.Ascent * 0.36f), key, t.Text1, HorizontalAlignment.Center, w);
        return w;
    }

    private static Vector2[] Bevel(Rect2 r, float k)
    {
        var p = r.Position; var e = r.End;
        return [new(p.X + k, p.Y), new(e.X - k, p.Y), new(e.X, p.Y + k), new(e.X, e.Y - k), new(e.X - k, e.Y), new(p.X + k, e.Y), new(p.X, e.Y - k), new(p.X, p.Y + k)];
    }

    /// <summary>走线：45°/90° 折线，两端各一个过孔，只表示"连接"（UI-VISUAL §5.5）。</summary>
    public static void Trace(CanvasItem ci, Vector2 from, Vector2 to, Color color, float width)
    {
        var d = to - from;
        var diag = MathF.Min(MathF.Abs(d.X), MathF.Abs(d.Y));
        var knee = from + new Vector2(MathF.Sign(d.X) * diag, MathF.Sign(d.Y) * diag);
        ci.DrawPolyline([from, knee, to], color, width, true);
        Via(ci, from, 5, color, true);
        Via(ci, to, 5, color, false);
    }

    /// <summary>刻度式比例尺：横线 + 两端 1×5 px 刻（UI-VISUAL §5.3）。</summary>
    public static void ScaleBar(CanvasItem ci, Vector2 rightEnd, float length, Color color)
    {
        var l = new Vector2(rightEnd.X - length, rightEnd.Y);
        ci.DrawLine(l, rightEnd, color, Hair);
        ci.DrawLine(l, l - new Vector2(0, 5), color, Hair);
        ci.DrawLine(rightEnd, rightEnd - new Vector2(0, 5), color, Hair);
        ci.DrawLine(new(l.X + length * 0.5f, rightEnd.Y), new(l.X + length * 0.5f, rightEnd.Y - 3), color, Hair);
    }

    /// <summary>"好看的整数"比例尺：给定每米像素数与想要的大致像素长度，返回 (米, 像素)。</summary>
    public static (float Meters, float Pixels) NiceScale(float ppm, float targetPx)
    {
        ReadOnlySpan<float> steps = [10, 20, 25, 50, 100, 200, 250, 500, 1000, 2000];
        var best = steps[0];
        foreach (var s in steps) if (s * ppm <= targetPx) best = s;
        return (best, best * ppm);
    }

    public static string Meters(float m) => m >= 1000 ? $"{m / 1000f:0.#} km" : $"{m:0} m";

    // ------------------------------------------------------------------ 图标（以 c 为中心，s 为活动区边长）

    private static void L(CanvasItem ci, Vector2 a, Vector2 b, Color c, float w) => ci.DrawLine(a, b, c, w, true);

    /// <summary>锚点：光学基准点——环、中心点、四根刻线；导览亭外加一圈细方框（SYS-ATLAS §5）。</summary>
    public static void AnchorIcon(CanvasItem ci, Vector2 c, float s, Color col, float w, bool kiosk, bool solid)
    {
        var u = s / 20f;
        if (solid) ci.DrawCircle(c, 4.6f * u, col, true, -1, true);
        else ci.DrawArc(c, 4.6f * u, 0, Mathf.Tau, 24, col, w, true);
        ci.DrawCircle(c, 1.3f * u, col, true, -1, true);
        foreach (var d in (ReadOnlySpan<Vector2>)[Vector2.Up, Vector2.Right, Vector2.Down, Vector2.Left])
            L(ci, c + d * 6.6f * u, c + d * 9.4f * u, col, w);
        if (kiosk) ci.DrawRect(new Rect2(c - new Vector2(7.4f, 7.4f) * u, new Vector2(14.8f, 14.8f) * u), col, false, 1f, true);
    }

    /// <summary>观景点：取景框的四个角，中心一点；仅旁观的在右上角加一枚上箭头（旁观）。</summary>
    public static void ViewpointIcon(CanvasItem ci, Vector2 c, float s, Color col, float w, bool flyOnly, bool solid)
    {
        var u = s / 20f;
        var hx = 7.5f * u; var hy = 5.5f * u; var k = 3.2f * u;
        if (solid) ci.DrawRect(new Rect2(c - new Vector2(hx, hy), new Vector2(hx, hy) * 2), col, true);
        foreach (var (sx, sy) in (ReadOnlySpan<(float, float)>)[(-1, -1), (1, -1), (1, 1), (-1, 1)])
        {
            var p = c + new Vector2(sx * hx, sy * hy);
            L(ci, p, p - new Vector2(sx * k, 0), col, w);
            L(ci, p, p - new Vector2(0, sy * k), col, w);
        }
        ci.DrawCircle(c, 1.3f * u, col, true, -1, true);
        if (flyOnly)
        {
            var t = c + new Vector2(hx + 1.5f * u, -hy - 2f * u);
            L(ci, t + new Vector2(-2.2f, 2.2f) * u, t, col, w);
            L(ci, t, t + new Vector2(2.2f, 2.2f) * u, col, w);
        }
    }

    /// <summary>可进入的室内：门框加一枚进门的箭头。</summary>
    public static void InteriorIcon(CanvasItem ci, Vector2 c, float s, Color col, float w)
    {
        var u = s / 20f;
        var door = new Rect2(c + new Vector2(-1f, -7f) * u, new Vector2(8f, 14f) * u);
        ci.DrawRect(door, col, false, w, true);
        L(ci, c + new Vector2(-8f, 0) * u, c + new Vector2(2f, 0) * u, col, w);
        L(ci, c + new Vector2(-1.5f, -3.5f) * u, c + new Vector2(2f, 0) * u, col, w);
        L(ci, c + new Vector2(-1.5f, 3.5f) * u, c + new Vector2(2f, 0) * u, col, w);
    }

    /// <summary>路线起点：一枚过孔接一段 45° 折线（走线的起点），不用足迹。</summary>
    public static void RouteIcon(CanvasItem ci, Vector2 c, float s, Color col, float w)
    {
        var u = s / 20f;
        var a = c + new Vector2(-5.5f, 3.5f) * u;
        ci.DrawArc(a, 2.4f * u, 0, Mathf.Tau, 16, col, w, true);
        ci.DrawPolyline([a + new Vector2(2.4f, 0) * u, c + new Vector2(1f, 3.5f) * u, c + new Vector2(7f, -2.5f) * u], col, w, true);
    }

    /// <summary>地标：八角（图书馆穹顶的平面八瓣，简化为八边形）。</summary>
    public static void LandmarkIcon(CanvasItem ci, Vector2 c, float s, Color col, float w)
    {
        var u = s / 20f;
        var pts = new Vector2[9];
        for (var i = 0; i < 8; i++)
        {
            var a = Mathf.Pi / 8 + i * Mathf.Pi / 4;
            pts[i] = c + new Vector2(MathF.Cos(a), MathF.Sin(a)) * 7f * u;
        }
        pts[8] = pts[0];
        ci.DrawPolyline(pts, col, w, true);
        ci.DrawCircle(c, 1.3f * u, col, true, -1, true);
    }

    public static void FacilityIcon(CanvasItem ci, Vector2 c, float s, Color col, float w)
    {
        var u = s / 20f;
        ci.DrawRect(new Rect2(c - new Vector2(5.5f, 5.5f) * u, new Vector2(11f, 11f) * u), col, false, w, true);
        ci.DrawCircle(c, 1.5f * u, col, true, -1, true);
    }

    /// <summary>自定义标记的 6 种单色线框形状（靠形状区分，不靠颜色）。</summary>
    public static void PinIcon(CanvasItem ci, Vector2 c, float s, Color col, float w, PinShape shape)
    {
        var u = s / 20f;
        Vector2[] P(params (float X, float Y)[] q) => [.. q.Select(p => c + new Vector2(p.X, p.Y) * u), c + new Vector2(q[0].X, q[0].Y) * u];
        switch (shape)
        {
            case PinShape.Diamond: ci.DrawPolyline(P((0, -7), (7, 0), (0, 7), (-7, 0)), col, w, true); break;
            case PinShape.Circle: ci.DrawArc(c, 6.5f * u, 0, Mathf.Tau, 24, col, w, true); break;
            case PinShape.Square: ci.DrawPolyline(P((-6, -6), (6, -6), (6, 6), (-6, 6)), col, w, true); break;
            case PinShape.Triangle: ci.DrawPolyline(P((0, -7), (7, 6), (-7, 6)), col, w, true); break;
            case PinShape.Star:
                {
                    var q = new (float, float)[10];
                    for (var i = 0; i < 10; i++)
                    {
                        var a = -Mathf.Pi / 2 + i * Mathf.Pi / 5;
                        var r = i % 2 == 0 ? 7.5f : 3.2f;
                        q[i] = (MathF.Cos(a) * r, MathF.Sin(a) * r);
                    }
                    ci.DrawPolyline(P(q), col, w, true);
                    break;
                }
            case PinShape.Flag:
                L(ci, c + new Vector2(-4.5f, 7.5f) * u, c + new Vector2(-4.5f, -7.5f) * u, col, w);
                ci.DrawPolyline([c + new Vector2(-4.5f, -7f) * u, c + new Vector2(6.5f, -4f) * u, c + new Vector2(-4.5f, -1f) * u], col, w, true);
                break;
            default: throw new ArgumentOutOfRangeException(nameof(shape));
        }
    }

    /// <summary>大地图按钮的意象：版图——方框里的走线与过孔（UI-VISUAL §6.2）。</summary>
    public static void AtlasGlyph(CanvasItem ci, Vector2 c, Color col, float w)
    {
        ci.DrawRect(new Rect2(c - new Vector2(8, 8), new Vector2(16, 16)), col, false, w, true);
        ci.DrawPolyline([c + new Vector2(-4.5f, 4.5f), c + new Vector2(-0.5f, 0.5f), c + new Vector2(4.5f, 0.5f)], col, w, true);
        ci.DrawArc(c + new Vector2(-4.5f, 4.5f), 1.6f, 0, Mathf.Tau, 12, col, w, true);
        ci.DrawCircle(c + new Vector2(4.5f, -4f), 1.6f, col, true, -1, true);
    }

    /// <summary>目录：三行条目，每行前一枚过孔。</summary>
    public static void IndexGlyph(CanvasItem ci, Vector2 c, Color col, float w)
    {
        for (var i = -1; i <= 1; i++)
        {
            var y = c.Y + i * 5.5f;
            ci.DrawCircle(new(c.X - 6.5f, y), 1.5f, col, true, -1, true);
            L(ci, new(c.X - 2.5f, y), new(c.X + 8f, y), col, w);
        }
    }

    public static void PlusGlyph(CanvasItem ci, Vector2 c, Color col, float w, bool plus)
    {
        L(ci, c - new Vector2(6.5f, 0), c + new Vector2(6.5f, 0), col, w);
        if (plus) L(ci, c - new Vector2(0, 6.5f), c + new Vector2(0, 6.5f), col, w);
    }

    /// <summary>回到玩家：准星环加四根刻线（与"小地图：方框加十字准线"同族）。</summary>
    public static void LocateGlyph(CanvasItem ci, Vector2 c, Color col, float w)
    {
        ci.DrawArc(c, 5.5f, 0, Mathf.Tau, 24, col, w, true);
        ci.DrawCircle(c, 1.5f, col, true, -1, true);
        foreach (var d in (ReadOnlySpan<Vector2>)[Vector2.Up, Vector2.Right, Vector2.Down, Vector2.Left])
            L(ci, c + d * 5.5f, c + d * 9f, col, w);
    }

    /// <summary>全图：四个向外的角。</summary>
    public static void FitGlyph(CanvasItem ci, Vector2 c, Color col, float w)
    {
        foreach (var (sx, sy) in (ReadOnlySpan<(float, float)>)[(-1, -1), (1, -1), (1, 1), (-1, 1)])
        {
            var p = c + new Vector2(sx * 8, sy * 8);
            L(ci, p, p - new Vector2(sx * 4.5f, 0), col, w);
            L(ci, p, p - new Vector2(0, sy * 4.5f), col, w);
        }
        ci.DrawRect(new Rect2(c - new Vector2(3, 3), new Vector2(6, 6)), col, false, w, true);
    }

    public static void CloseGlyph(CanvasItem ci, Vector2 c, Color col, float w)
    {
        L(ci, c + new Vector2(-5, -5), c + new Vector2(5, 5), col, w);
        L(ci, c + new Vector2(5, -5), c + new Vector2(-5, 5), col, w);
    }

    public static void CheckGlyph(CanvasItem ci, Vector2 c, Color col, float w)
        => ci.DrawPolyline([c + new Vector2(-4.5f, 0.2f), c + new Vector2(-1.4f, 3.3f), c + new Vector2(4.6f, -3.2f)], col, w, true);
}
