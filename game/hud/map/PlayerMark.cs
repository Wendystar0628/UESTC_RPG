using Godot;

namespace Campus.Hud.Map;

/// <summary>
/// 玩家：主色箭头加视锥（map.player）。箭头朝向是观者的偏航（IObserverState），视锥是镜头的水平视场角
/// （ICameraRig.Pose），二者分开读（ENG-ROADMAP 第 2 条）。每帧只改 Position 与 Rotation；
/// 视锥相对箭头的夹角或视场角变化超过 0.5° 才重画。
/// </summary>
public partial class PlayerMark : Control
{
    private readonly MapHudContext _ctx;
    private float _coneOffsetDeg;
    private float _hfovDeg = 90;
    private bool _night;

    public PlayerMark(MapHudContext ctx)
    {
        _ctx = ctx;
        MouseFilter = MouseFilterEnum.Ignore;
    }

    /// <summary>视锥长度（逻辑像素）：小地图取视口半宽的 0.3（SYS-ATLAS）。</summary>
    public float ConeLength { get; set; } = 34;

    public void Follow(Vector2 position, float observerYawDeg, float cameraYawDeg, float hfovDeg)
    {
        Position = position;
        Rotation = Mathf.DegToRad(observerYawDeg);
        var offset = Mathf.Wrap(cameraYawDeg - observerYawDeg, -180f, 180f);
        var night = _ctx.ToneFor(this).IsNight;
        if (MathF.Abs(offset - _coneOffsetDeg) > 0.5f || MathF.Abs(hfovDeg - _hfovDeg) > 0.5f || night != _night)
        {
            _coneOffsetDeg = offset;
            _hfovDeg = hfovDeg;
            _night = night;
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        var t = _ctx.ToneFor(this);
        // 视锥：主色 45% → 0 的扇形渐变（扇形用一圈三角形，顶点处不透明度 45%，远端为 0）
        var half = Mathf.DegToRad(_hfovDeg * 0.5f);
        var mid = Mathf.DegToRad(_coneOffsetDeg) - Mathf.Pi / 2;
        const int Steps = 16;
        var apex = t.PlayerCone;
        var rim = apex; rim.A = 0;
        for (var i = 0; i < Steps; i++)
        {
            var a0 = mid - half + 2 * half * i / Steps;
            var a1 = mid - half + 2 * half * (i + 1) / Steps;
            DrawPolygon([Vector2.Zero, new Vector2(MathF.Cos(a0), MathF.Sin(a0)) * ConeLength, new Vector2(MathF.Cos(a1), MathF.Sin(a1)) * ConeLength],
                [apex, rim, rim]);
        }
        // 箭头：尖朝上（偏航 0 = 北），1.5 px 描边；深屏可带 ≤ 4 px 的辉光，只此一处
        Vector2[] arrow = [new(0, -8.5f), new(6.2f, 6.5f), new(0, 3.2f), new(-6.2f, 6.5f)];
        var glow = _ctx.Style.Metrics.PlayerGlowDark;
        if (t.IsNight && glow > 0 && t.Glow.A > 0)
        {
            for (var g = glow; g >= 1; g -= 1)
            {
                var c = t.Glow; c.A = _ctx.Style.Metrics.GlowMax * MathF.Exp(-g / (glow * 0.5f)) * 0.5f;
                DrawPolyline([.. arrow.Select(p => p * (1 + g / 7f)), arrow[0] * (1 + g / 7f)], c, 2f, true);
            }
        }
        DrawColoredPolygon(arrow, t.PlayerFill);
        DrawPolyline([.. arrow, arrow[0]], t.PlayerLine, _ctx.Style.Metrics.PlayerLine, true);
    }
}
