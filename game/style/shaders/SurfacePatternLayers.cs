using Campus.Contracts;

namespace Campus.Style.Materials;

/// <summary>
/// 表面图样的层号登记（ART-STYLE，D-195）：层号定义图样的行组织（直缝、错缝……），不按材质 ID 或加载顺序分配；
/// 登记后不改号、不复用。模数、缝宽、铺向与相位由做这块表面的包给（SurfacePatternSpec 与米 UV），这里只认层。
/// toon_static.gdshader 的 pattern_layer 按同一编号分支。
/// </summary>
public static class SurfacePatternLayers
{
    /// <summary>直缝方砖：缝在 U、V 两向都对齐。首个用户是图书馆前广场的 600×600 mm 花岗岩（GroundPaving）。</summary>
    public const ushort StackBond = 1;

    /// <summary>
    /// 错缝（顺砖）：V 向一行一行，行缝通长；U 向的竖缝隔行错半格。首个用户是人行步道的 300×150 mm 透水砖
    /// （GroundPaving，U 沿步道）；墙面砖也用这一层（U 沿墙水平、V 向上）。
    /// </summary>
    public const ushort RunningBond = 2;

    /// <summary>新层只在末尾追加；这里穷举，没有登记的层号直接拒绝（WF-BUILD）。</summary>
    public static bool IsRegistered(ushort layer) => layer switch
    {
        StackBond or RunningBond => true,
        _ => false
    };

    /// <summary>形状校验（契约）之外再查层号；没登记的层在取材质与缓存之前失败。</summary>
    public static void Validate(ToonSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        spec.ValidateSurfacePattern();
        if (spec.SurfacePattern is { } pattern && !IsRegistered(pattern.Layer))
            throw new NotSupportedException($"Surface-pattern layer {pattern.Layer} is not registered in SurfacePatternLayers.");
    }
}
