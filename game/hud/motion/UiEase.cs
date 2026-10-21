namespace Campus.Hud.Motion;

/// <summary>
/// 动效曲线（UI-VISUAL）：贝塞尔 Enter (.2,.7,.2,1)、Exit (.3,0,.8,.15)、Standard (.2,0,0,1)，临界阻尼弹簧的精确解，一拍二步进。
/// 不用 Godot 的 Back、Elastic、Bounce、Spring 过渡。曲线参数由 ui_metrics.json 给出，调用方传进来。
/// </summary>
public static class UiEase
{
    /// <summary>三次贝塞尔 (0,0)-(x1,y1)-(x2,y2)-(1,1) 在 x = t 处的 y；先牛顿迭代，再用区间二分求根。</summary>
    public static float Bezier(float t, float x1, float y1, float x2, float y2)
    {
        if (t <= 0) return 0;
        if (t >= 1) return 1;
        var u = t;
        for (var i = 0; i < 8; i++)
        {
            var x = Sample(u, x1, x2) - t;
            if (MathF.Abs(x) < 1e-5f) return Sample(u, y1, y2);
            var d = Slope(u, x1, x2);
            if (MathF.Abs(d) < 1e-6f) break;
            u -= x / d;
        }
        float lo = 0, hi = 1;
        u = t;
        for (var i = 0; i < 24; i++)
        {
            var x = Sample(u, x1, x2);
            if (MathF.Abs(x - t) < 1e-5f) break;
            if (x < t) lo = u; else hi = u;
            u = (lo + hi) / 2;
        }
        return Sample(u, y1, y2);
    }

    private static float Sample(float u, float a, float b) => ((1 - 3 * b + 3 * a) * u + (3 * b - 6 * a)) * u * u + 3 * a * u;
    private static float Slope(float u, float a, float b) => 3 * (1 - 3 * b + 3 * a) * u * u + 2 * (3 * b - 6 * a) * u + 3 * a;

    /// <summary>临界阻尼（ζ = 1）弹簧从 0 走向 1 的位置；response 为固有周期（秒）。</summary>
    public static float Spring(float seconds, float response) =>
        response <= 0 ? 1 : SpringStep(0, 0, 1, SpringOmega(response), seconds).Value;

    /// <summary>临界阻尼弹簧的精确解：从 (value, velocity) 朝 target 走 dt 秒后的位置与速度；全部弹簧只用这一份。</summary>
    internal static (float Value, float Velocity) SpringStep(float value, float velocity, float target, float omega, float dt)
    {
        var x = value - target;
        var e = MathF.Exp(-omega * dt);
        return (target + (x + (velocity + omega * x) * dt) * e, (velocity - omega * (velocity + omega * x) * dt) * e);
    }

    internal static float SpringOmega(float response) => 2 * MathF.PI / MathF.Max(response, 1e-3f);

    /// <summary>临界阻尼弹簧进入 1% 的时间（UI-VISUAL：0.15 s → 158 ms）。</summary>
    public static float SpringSettle(float response) => response * 1.0536f;

    /// <summary>一拍二步进：T = floor(t·rate)/rate。</summary>
    public static float Stepped(float t, float rate) => MathF.Floor(t * rate) / rate;
}

/// <summary>临界阻尼弹簧（阻尼比 1）：可被打断的跟随，不过冲（UI-VISUAL）；数学见 <see cref="UiEase.SpringStep"/>。</summary>
public struct Spring(float value, float response)
{
    public float Value = value;
    private float _velocity;
    private readonly float _omega = UiEase.SpringOmega(response);

    public float Step(float target, float dt)
    {
        (Value, _velocity) = UiEase.SpringStep(Value, _velocity, target, _omega, dt);
        return Value;
    }

    public void Snap(float v) { Value = v; _velocity = 0; }
}
