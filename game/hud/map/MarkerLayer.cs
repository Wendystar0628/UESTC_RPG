using Campus.Contracts.Atlas;
using Godot;

namespace Campus.Hud.Map;

/// <summary>
/// 标记层：一个 _Draw 控件画全部图标。每帧只改 Position（与 Scale），缩放变化超过 3%、旋转变了、
/// 标记集合或状态变了才重画（SYS-ATLAS）。图标先垫一枚底，再画线框，主色只给可传送的标记、选中和追踪。
/// </summary>
public partial class MarkerLayer : Control
{
    private readonly MapHudContext _ctx;
    private readonly bool _atlas;
    private IReadOnlyList<MapEntry> _entries = [];
    private MapView _ref = new(Vector2.Zero, 1, 0);
    private Vector2 _viewport;
    private bool _dirty = true;

    public MarkerLayer(MapHudContext ctx, bool atlas)
    {
        _ctx = ctx;
        _atlas = atlas;
        MouseFilter = MouseFilterEnum.Ignore;
    }

    /// <summary>按 MapHudContext.LayerOf 编号的图层开关（SYS-ATLAS）。</summary>
    public int LayerMask { get; set; } = -1;
    public MarkerId? Hover { get; set; }
    public MarkerId? Selected { get; set; }
    public MarkerId? Tracked { get; set; }
    /// <summary>传送卡开着时卡上已写名字，选中的标记旁不再写。</summary>
    public bool NameSelected { get; set; } = true;
    public ISet<MarkerId> Here { get; } = new HashSet<MarkerId>();
    public float IconSize { get; set; } = 20;

    public void SetEntries(IReadOnlyList<MapEntry> entries) { _entries = entries; Invalidate(); }
    public void Invalidate() { _dirty = true; QueueRedraw(); }

    public bool Shows(MapEntry e) => _atlas
        ? (LayerMask & (1 << MapHudContext.LayerOf(e.Marker.Kind))) != 0 && e.Marker.Kind != MarkerKind.Quest
        : e.Marker.Kind is MarkerKind.Anchor or MarkerKind.Interior or MarkerKind.Custom || e.Id == Tracked;

    /// <summary>每帧调用：视图只平移时改 Position；缩放偏离参考值 3% 以上或旋转变化时重画。</summary>
    public void Follow(MapView view, Vector2 viewport)
    {
        if (_dirty || viewport != _viewport || MathF.Abs(view.Ppm / _ref.Ppm - 1) > 0.03f || view.Rotation != _ref.Rotation)
        {
            _ref = view;
            _viewport = viewport;
            _dirty = false;
            QueueRedraw();
        }
        var k = view.Ppm / _ref.Ppm;
        var d = (_ref.Center - view.Center) * view.Ppm;
        var c = MathF.Cos(view.Rotation); var s = MathF.Sin(view.Rotation);
        Position = viewport * 0.5f * (1 - k) + new Vector2(c * d.X + s * d.Y, -s * d.X + c * d.Y);
        Scale = new Vector2(k, k);
    }

    /// <summary>屏幕点下的标记（大地图的悬停与单击），按画面上的距离取最近的一枚。</summary>
    public MapEntry? Pick(Vector2 viewportLocal, MapView view)
    {
        MapEntry? best = null;
        var bestD = IconSize * 0.7f;
        foreach (var e in _entries)
        {
            if (!Shows(e)) continue;
            var d = view.ToLocal(e.At, _viewport).DistanceTo(viewportLocal);
            if (d < bestD) { bestD = d; best = e; }
        }
        return best;
    }

    public override void _Draw()
    {
        var t = _ctx.ToneFor(this);
        var stroke = _ctx.Style.Metrics.IconStroke;
        var size = IconSize;
        MapEntry? named = null;
        foreach (var e in _entries)
        {
            if (!Shows(e)) continue;
            var p = _ref.ToLocal(e.At, _viewport).Round();
            var here = Here.Contains(e.Id);
            var hot = e.Id == Hover || e.Id == Selected;
            var ink = e.Travelable ? t.Graphic : t.Text1;
            var pad = size * 0.62f;
            // 底：先有一层面板底，清蓝才不直接贴在底图上（UI-VISUAL 第 1 条）
            DrawCircle(p, pad, here ? ink : t.Solid, true, -1, true);
            DrawArc(p, pad, 0, Mathf.Tau, 32, hot ? t.CardHover : t.PanelLine, 1f, true);
            _ctx.Icon(this, e, p, size * 0.82f, here ? t.Solid : ink, stroke, false);
            if (e.Id == Selected)
            {
                // 选中：衍射光边——1 物理像素的核，两侧冷色色散（UI-VISUAL）
                var r = pad + 3;
                var a = t.IrisA; a.A *= 0.35f;
                var b = t.IrisB; b.A *= 0.35f;
                DrawArc(p, r + 0.8f, 0, Mathf.Tau, 40, a, 0.8f, true);
                DrawArc(p, r - 0.8f, 0, Mathf.Tau, 40, b, 0.8f, true);
                DrawArc(p, r, 0, Mathf.Tau, 40, t.PrimaryLine, 1f, true);
            }
            if (e.Id == Tracked) DrawArc(p, pad + 6, 0, Mathf.Tau, 40, t.PrimaryFill, 1.5f, true);
            if (_atlas && (e.Id == Hover || (e.Id == Selected && NameSelected))) named = e;
        }
        if (named is not null) DrawName(named, t);
    }

    /// <summary>悬停或选中的标记旁写名字：中文主名 + 英文副名，带 2 px 晕（SYS-ATLAS）。</summary>
    private void DrawName(MapEntry e, MapTone t)
    {
        var p = _ref.ToLocal(e.At, _viewport).Round();
        var zh = _ctx.Style.Fonts["small"];
        var en = _ctx.Style.Fonts["tick"];
        var x = p.X + IconSize * 0.62f + 8;
        var halo = _ctx.Style.Metrics.HaloWidth;
        MapDraw.HaloText(this, zh, new(x, p.Y + 1), e.Name.Zh, t.LabelZh, t.LabelHalo, halo);
        if (e.Name.En.Length > 0) MapDraw.HaloText(this, en, new(x, p.Y + 15), e.Name.En, t.LabelEn, t.LabelHalo, halo);
    }
}
