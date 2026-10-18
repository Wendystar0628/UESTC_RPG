using System.Numerics;
using Campus.Contracts;

namespace Campus.Systems.Weather.Precip;

/// <summary>降水池一帧的输出：可见实例数与三个 instance uniform（ENG-PERF 的 precip_d、precip_v、precip_k）。</summary>
public readonly record struct PrecipFrame(int VisibleCount, Vector4 D, Vector4 V, Vector4 K);

/// <summary>
/// 降水池的纯逻辑（不碰引擎）：实例缓冲、数量、循环盒位移的累加与 instance uniform 的取值。
/// 规格：SYS-WEATHER；ENG-PERF；SYS-MOTION。只读天气向量，不自己判断天气，不计时。
/// </summary>
public sealed class PrecipModel
{
    private Vector3 _d;
    private double _lastStep;
    private long _instance;
    private bool _started;

    /// <summary>当前的循环盒位移（m），已对 8h 取模。</summary>
    public Vector3 Displacement => _d;

    /// <summary>
    /// 静态实例缓冲：变换为单位阵，CustomData = (p0.x, p0.y, p0.z, r)，p0 在单位立方体里、r 在 [0, 1)。
    /// 实例按带种子的生成器顺序排列，任何前缀都是均匀抽稀（VisibleInstanceCount）。
    /// </summary>
    public static float[] BuildBuffer()
    {
        var rng = new Rng(StableHash.Hash32(0u, "weather", "precip", "pool"));
        var data = new float[PrecipTuning.Capacity * PrecipTuning.FloatsPerInstance];
        for (var i = 0; i < PrecipTuning.Capacity; i++)
        {
            var o = i * PrecipTuning.FloatsPerInstance;
            data[o + 0] = 1f;
            data[o + 5] = 1f;
            data[o + 10] = 1f;
            data[o + 12] = rng.NextFloat();
            data[o + 13] = rng.NextFloat();
            data[o + 14] = rng.NextFloat();
            data[o + 15] = rng.NextFloat();
        }
        return data;
    }

    /// <summary>降水池的小网格：一个四边形，UV 的 x 跨线、y 沿线；位置由着色器按 UV 算出，这里只给占位坐标。</summary>
    public static (float[] Position, float[] Uv, int[] Index) BuildQuad() => (
        [-0.5f, 0f, 0f, 0.5f, 0f, 0f, -0.5f, 1f, 0f, 0.5f, 1f, 0f],
        [0f, 0f, 1f, 0f, 0f, 1f, 1f, 1f],
        [0, 2, 1, 1, 2, 3]);

    /// <summary>雨与雪的数量（未乘画面设置的密度）。</summary>
    public static int BaseCount(in WeatherVec w)
    {
        var p = Math.Clamp(w.Precip, 0f, 1f);
        if (p < PrecipTuning.MinPrecip) return 0;
        var storm = Math.Clamp(w.Storm, 0f, 1f);
        var count = IsSnow(w)
            ? PrecipTuning.SnowPerPrecip * p
            : PrecipTuning.RainPerPrecip * p + PrecipTuning.RainPerStorm * storm;
        return (int)Math.Min(PrecipTuning.Capacity, MathF.Round(count));
    }

    /// <summary>乘"降水与粒子密度"后的可见实例数。</summary>
    public static int VisibleCount(in WeatherVec w, float density)
    {
        if (!float.IsFinite(density)) throw new ArgumentOutOfRangeException(nameof(density));
        return (int)Math.Min(PrecipTuning.Capacity, MathF.Round(BaseCount(w) * Math.Clamp(density, 0f, 1f)));
    }

    public static bool IsSnow(in WeatherVec w) => w.PrecipKind >= 0.5f;

    public static float HalfSize(in WeatherVec w) => IsSnow(w) ? PrecipTuning.SnowHalf : PrecipTuning.RainHalf;

    /// <summary>雨丝倾角（度）：小雨 5° → 大雨 15°，风暴再加到 25°，阵风另加。</summary>
    public static float TiltDeg(in WeatherVec w, float gust)
    {
        var heavy = Godot.Mathf.SmoothStep(PrecipTuning.HeavyFrom, PrecipTuning.HeavyTo, w.Precip);
        var tilt = PrecipTuning.TiltLight + (PrecipTuning.TiltHeavy - PrecipTuning.TiltLight) * heavy;
        tilt += (PrecipTuning.TiltStorm - PrecipTuning.TiltHeavy) * Math.Clamp(w.Storm, 0f, 1f);
        return tilt + PrecipTuning.TiltGust * Math.Clamp(gust, 0f, 1f);
    }

    /// <summary>降水的速度（m/s）：雨按倾角顺风斜落；雪慢落、横漂。</summary>
    public static Vector3 Velocity(in WeatherVec w, float gust)
    {
        if (IsSnow(w))
        {
            var drift = PrecipTuning.SnowDrift + PrecipTuning.SnowGustDrift * Math.Clamp(gust, 0f, 1f);
            return PrecipTuning.WindDir * drift - Vector3.UnitY * PrecipTuning.SnowFall;
        }
        var tan = MathF.Tan(TiltDeg(w, gust) * (MathF.PI / 180f));
        return PrecipTuning.WindDir * (PrecipTuning.RainFall * tan) - Vector3.UnitY * PrecipTuning.RainFall;
    }

    /// <summary>
    /// 每帧调用一次。位移只在一拍二跨步时按跨过的时长累加（ENG-PERF），所以改风、换强度时雨丝不跳，
    /// anim_time 回绕时也不跳；世界实例变了（读档、新游戏）就从当前拍重新起算。
    /// </summary>
    public PrecipFrame Step(in WeatherVec w, long instance, double step12, float gust, float density, int stepRate)
    {
        if (!double.IsFinite(step12)) throw new ArgumentOutOfRangeException(nameof(step12));
        if (!WorldTime.IsStepRate(stepRate)) throw new ArgumentOutOfRangeException(nameof(stepRate));
        var h = HalfSize(w);
        var v = Velocity(w, gust);
        if (!_started || instance != _instance)
        {
            _started = true;
            _instance = instance;
            _lastStep = step12;
        }
        var dt = step12 - _lastStep;
        if (dt < 0) dt += WorldTime.AnimLoopSeconds;
        if (dt > 1.0) dt = 0.0;           // 跳拨动画时钟（--anim、读档）不当作经过了时间
        _lastStep = step12;
        if (dt > 0) _d = Wrap(_d + v * (float)dt, PrecipTuning.WrapPerHalf * h);
        var speed = v.Length();
        var dir = speed > 1e-4f ? v / speed : -Vector3.UnitY;
        var len = IsSnow(w) ? 0f : speed / stepRate;   // 丝长取一拍的步长
        return new PrecipFrame(VisibleCount(w, density),
            new Vector4(_d, h),
            new Vector4(dir, len),
            new Vector4(IsSnow(w) ? 1f : 0f, Math.Clamp(w.Precip, 0f, 1f), 0f, 0f));
    }

    /// <summary>
    /// 降水池的 CustomAabb：可达范围"中心 ± 3 km"（ENG-PERF），y 取 floorY…ceilY，再各边放出一个循环盒。
    /// </summary>
    public static Box3 PoolBounds(Vector2 center, float maxRadius, float floorY, float ceilY)
    {
        var m = PrecipTuning.RainHalf;
        return new Box3(new Vector3(center.X - maxRadius - m, floorY - m, center.Y - maxRadius - m),
            new Vector3(center.X + maxRadius + m, ceilY + m, center.Y + maxRadius + m));
    }

    /// <summary>逐分量对 period 取模到 [0, period)。</summary>
    public static Vector3 Wrap(Vector3 d, float period) => new(Mod(d.X, period), Mod(d.Y, period), Mod(d.Z, period));

    private static float Mod(float x, float m)
    {
        var r = x % m;
        return r < 0 ? r + m : r;
    }
}
