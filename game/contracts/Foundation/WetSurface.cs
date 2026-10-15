namespace Campus.Contracts;

/// <summary>
/// 湿润与积水判定的 C# 版，与 precip_wet.gdshaderinc 逐项一致（SYS-WEATHER §7；G2 在抽样点上比对两边）。
/// 只用整数哈希与加减乘除，结果不依赖平台数学库。contract-5 第 4 条批准后整份移进契约（纯静态类），供 IGroundCover（WP-38）调用。
/// 主线版：积水潜势是程序噪声（地面遮罩 2 交付后改读它的 R 通道），遮蔽只按朝向。
/// </summary>
public static class WetSurface
{
    public const float ScaleA = 2.4f;
    public const float ScaleB = 0.9f;
    public const float WeightA = 0.62f;
    public const float PuddleTop = 0.80f;
    public const float PuddleSpan = 0.24f;
    public const float FlatFrom = 0.93f;
    public const float FlatTo = 0.985f;
    public const float ResponseFrom = 0.5f;
    public const float ResponseTo = 0.9f;

    /// <summary>积水潜势 0–1，与着色器 precip_puddle_potential 相同。</summary>
    public static float PuddlePotential(float x, float z) =>
        WeightA * ValueNoise(x / ScaleA, z / ScaleA) + (1f - WeightA) * ValueNoise(x / ScaleB + 17f, z / ScaleB + 5f);

    /// <summary>积水阈值：积水量越大阈值越低；积水量为 0 时不积水（由 IsPuddle 判断）。</summary>
    public static float PuddleThreshold(float level) => PuddleTop - PuddleSpan * Math.Clamp(level, 0f, 1f);

    /// <summary>暴露度 0–1：室内 0；朝天 1，立面约 0.6，朝下 0。</summary>
    public static float Exposure(float normalY, bool interior)
    {
        if (interior) return 0f;
        var ny = Math.Clamp(normalY, -1f, 1f);
        return SmoothStep(-0.6f, 0f, ny) * (0.6f + 0.4f * SmoothStep(0f, 0.7f, ny));
    }

    /// <summary>湿润量 = 天气湿润 × 暴露 × 材质响应（材质线表的湿润响应：沥青 1、默认 0.6、玻璃 0）。</summary>
    public static float Wet(float weatherWet, float exposure, float response) =>
        Math.Clamp(weatherWet, 0f, 1f) * Math.Clamp(exposure, 0f, 1f) * Math.Clamp(response, 0f, 1f);

    /// <summary>朝天地面上的积水级别：天气积水量 × 材质门限 × 平面门限。</summary>
    public static float PuddleLevel(float weatherPuddle, float response, float normalY) =>
        Math.Clamp(weatherPuddle, 0f, 1f) * SmoothStep(ResponseFrom, ResponseTo, response) * SmoothStep(FlatFrom, FlatTo, normalY);

    /// <summary>这一点有没有积水（硬边：着色器在阈值两侧只做一像素的抗锯齿）。</summary>
    public static bool IsPuddle(float x, float z, float level) =>
        level > 0f && PuddlePotential(x, z) > PuddleThreshold(level);

    internal static uint Hash(uint x)
    {
        unchecked
        {
            x ^= x >> 16;
            x *= 2146121005u;
            x ^= x >> 15;
            x *= 2221713035u;
            x ^= x >> 16;
            return x;
        }
    }

    internal static float Cell(int cx, int cz)
    {
        unchecked
        {
            var h = Hash(((uint)cx * 73856093u) ^ Hash((uint)cz * 19349663u + 1u));
            return (h >> 8) / 16777216f;
        }
    }

    internal static float ValueNoise(float x, float z)
    {
        var ix = MathF.Floor(x);
        var iz = MathF.Floor(z);
        var fx = x - ix;
        var fz = z - iz;
        var ux = fx * fx * (3f - 2f * fx);
        var uz = fz * fz * (3f - 2f * fz);
        int cx = (int)ix, cz = (int)iz;
        var a = Cell(cx, cz);
        var b = Cell(cx + 1, cz);
        var d = Cell(cx, cz + 1);
        var e = Cell(cx + 1, cz + 1);
        var top = a + (b - a) * ux;
        var bottom = d + (e - d) * ux;
        return top + (bottom - top) * uz;
    }

    private static float SmoothStep(float a, float b, float x)
    {
        var t = Math.Clamp((x - a) / (b - a), 0f, 1f);
        return t * t * (3f - 2f * t);
    }
}
