using System.Numerics;

namespace Campus.Systems.Weather.Precip;

/// <summary>
/// 降水池的可调参数集中在这里（WF-FLOW §4），由 Opus 评审定值。
/// 规格：SYS-WEATHER §3、§7；ENG-PERF §9。着色器自己的视觉常量在 precip.gdshader 开头，不与这里重复。
/// </summary>
public static class PrecipTuning
{
    /// <summary>降水池容量（ENG-PERF §9）。</summary>
    public const int Capacity = 8192;

    /// <summary>每 16 个实例里最后一个是溅落槽位，共 512 个；前缀抽稀时溅落与雨丝同比例减少。</summary>
    public const int SplashStride = 16;

    /// <summary>每实例 16 个 float：Transform3D 12 个（单位阵）+ CustomData 4 个（ENG-PERF §1.2 的统一格式）。</summary>
    public const int FloatsPerInstance = 16;

    /// <summary>相机循环盒的半边长（m）：雨 18、雪 14。</summary>
    public const float RainHalf = 18f;
    public const float SnowHalf = 14f;

    /// <summary>位移按 8h 取模：雪花的速度系数是 0.25 的整数倍，取模时不跳。</summary>
    public const float WrapPerHalf = 8f;

    /// <summary>雨丝下落 7 m/s；雪花 0.8 m/s，每片再乘 0.75、1、1.25（即 0.6–1.0 m/s）。</summary>
    public const float RainFall = 7f;
    public const float SnowFall = 0.8f;

    /// <summary>雨丝倾角（度）：小雨 5、大雨 15、雷雨 25，另加阵风。</summary>
    public const float TiltLight = 5f;
    public const float TiltHeavy = 15f;
    public const float TiltStorm = 25f;
    public const float TiltGust = 5f;

    /// <summary>雪的横漂（m/s）：基础量加阵风。</summary>
    public const float SnowDrift = 0.35f;
    public const float SnowGustDrift = 0.8f;

    /// <summary>
    /// 数量（SYS-WEATHER §7）：雨 = 6000 × 降水量 + 2192 × 风暴度，上限 8192（小雨 0.25 → 1500，大雨 1 → 6000，雷雨取满）；
    /// 雪 = 4000 × 降水量（大雪 4000）。再乘画面设置的"降水与粒子密度"。
    /// </summary>
    public const float RainPerPrecip = 6000f;
    public const float RainPerStorm = 2192f;
    public const float SnowPerPrecip = 4000f;

    /// <summary>降水量低于它时不画（避免零星几根）。</summary>
    public const float MinPrecip = 0.01f;

    /// <summary>雨的"大"从降水量 0.25 到 0.9 过渡（倾角由小雨插到大雨）。</summary>
    public const float HeavyFrom = 0.25f;
    public const float HeavyTo = 0.9f;

    /// <summary>
    /// 风向：西风，自西向东（SYS-MOTION §3.1；x 向东）。IWindField 的 CPU 读数只有阵风与相位、没有方向，
    /// 方向先取这条全校统一的西风。
    /// </summary>
    public static readonly Vector3 WindDir = new(1f, 0f, 0f);

    /// <summary>主线版 precip_occ 的常量顶面高度（m）：遮蔽高度图的烘焙在后置细节里（ENG-WP §2.1）。</summary>
    public const float OccConstantTop = 0f;

    /// <summary>主线版 precip_occ 常量纹理的边长（像素）。</summary>
    public const int OccConstantSize = 4;
}
