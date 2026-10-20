namespace Campus.Contracts;

/// <summary>
/// 湿润与积水判定及共用规则值（SYS-WEATHER）；着色器规则值由构建步骤生成。
/// 只用整数哈希与加减乘除，结果不依赖平台数学库。contract-5 第 4 条批准后整份移进契约（纯静态类），供 IGroundCover（WP-38）调用。
/// 主线版：积水潜势是程序噪声（地面遮罩 2 交付后改读它的 R 通道），遮蔽只按朝向。
/// </summary>
public static class WetSurface
{
    public const float OffsetX = 17f;
    public const float OffsetZ = 5f;
    public const float ExposureFrom = -0.6f;
    public const float ExposureSide = 0.6f;
    public const float ExposureGain = 0.4f;
    public const float ExposureTo = 0.7f;

    public const float ScaleA = 2.4f;
    public const float ScaleB = 0.9f;
    public const float WeightA = 0.62f;
    public const float PuddleTop = 0.80f;
    public const float PuddleSpan = 0.22f;
    public const float FlatFrom = 0.990f;
    public const float FlatTo = 0.997f;
    public const float ResponseFrom = 0.45f;
    public const float ResponseTo = 0.55f;
    /// <summary>草地、绿篱与裸土的材质响应；由材质表上传，低于积水资格门限。</summary>
    public const float PorousResponse = 0.4f;

    /// <summary>积水潜势 0–1，与着色器 precip_puddle_potential 相同。</summary>
    public static float PuddlePotential(float x, float z) =>
        WeightA * IntegerNoise.PuddleValue2D(x / ScaleA, z / ScaleA) + (1f - WeightA) * IntegerNoise.PuddleValue2D(x / ScaleB + OffsetX, z / ScaleB + OffsetZ);

    /// <summary>积水阈值：积水量越大阈值越低；积水量为 0 时不积水（由 IsPuddle 判断）。</summary>
    public static float PuddleThreshold(float level) => PuddleTop - PuddleSpan * Math.Clamp(level, 0f, 1f);

    /// <summary>暴露度 0–1：室内 0；朝天 1，立面约 0.6，朝下 0。</summary>
    public static float Exposure(float normalY, bool interior)
    {
        if (interior) return 0f;
        var ny = Math.Clamp(normalY, -1f, 1f);
        return ScalarMath.Smoothstep(ExposureFrom, 0f, ny) * (ExposureSide + ExposureGain * ScalarMath.Smoothstep(0f, ExposureTo, ny));
    }

    /// <summary>湿润量 = 天气湿润 × 暴露 × 材质响应（材质线表的湿润响应：沥青 1、默认 0.6、玻璃 0）。</summary>
    public static float Wet(float weatherWet, float exposure, float response) =>
        Math.Clamp(weatherWet, 0f, 1f) * Math.Clamp(exposure, 0f, 1f) * Math.Clamp(response, 0f, 1f);

    /// <summary>朝天地面上的积水级别：天气积水量 × 材质门限 × 平面门限。</summary>
    public static float PuddleLevel(float weatherPuddle, float response, float normalY) =>
        Math.Clamp(weatherPuddle, 0f, 1f) * ScalarMath.Smoothstep(ResponseFrom, ResponseTo, response) * ScalarMath.Smoothstep(FlatFrom, FlatTo, normalY);

    /// <summary>这一点有没有积水（硬边：着色器在阈值两侧只做一像素的抗锯齿）。</summary>
    public static bool IsPuddle(float x, float z, float level) =>
        level > 0f && PuddlePotential(x, z) > PuddleThreshold(level);

}
