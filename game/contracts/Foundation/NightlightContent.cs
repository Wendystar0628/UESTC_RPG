using System.Numerics;
using Campus.Contracts.Bake;

namespace Campus.Contracts;

/// <summary>一盏灯在灯表里的全部数据（灯脚、灯头、光斑、通道、两个阈值）。</summary>
public readonly record struct LampRecord(
    Vector3 Base,          // 灯脚处的地面点（x, 地面高度, z）
    float HeadHeight,      // 灯头离地高度（m），光晕与灯锥从这里出
    float Radius,          // 地面光斑半径（m），冬夜再乘 sys.poolGain
    int ColorColumn,       // 调色板色号 0–511（灯色，bank 1 为 256 起）
    float Energy,          // 级别：一级 1.0、二级 0.75、三级 0.5（MAP-QSH-ADDITIONS）
    int Slot,              // 灯光通道槽位
    float ThresholdDeg,    // 开灯阈值（太阳高度，度）：h_eff 低于它就亮；≥ 89° 为常亮
    float Keep,            // 保留阈值 κ ∈ [0, 1)：通道保留比例低于它时熄（熄一半、整组关）
    int Sequence,          // 偶发事件序号（0 = 不参与）
    float GlowRadius)      // 灯头光晕半径（m）
{
    public Vector3 Head => Base + new Vector3(0f, HeadHeight, 0f);
}

/// <summary>fx_glow 的分支，与着色器里的 fx_kind 一致（材质 uniform，不是 #define）。只在末尾追加。</summary>
public enum GlowKind
{
    Window = 0,        // 窗灯光晕：贴立面的面片
    Lamp = 1,          // 路灯与景观灯的灯头光晕：朝镜头的面片
    LampCone = 2,      // 光锥（雾夜、雨夜可见；主线留空）
    ShaftOutdoor = 3,  // 室外软边光柱（主线留空）
    ShaftIndoor = 4,   // 室内棱柱光柱（主线留空）
    Dust = 5,          // 细尘（主线留空）
    Firefly = 6,       // 萤火虫（主线留空）
}

/// <summary>
/// 一组 fx_glow 实例：MultiMesh 的整块缓冲，每个实例 16 个 float = Transform3D 的 12 个（行主序）+ INSTANCE_CUSTOM 的 4 个。
/// 亮度全由着色器读通道算，CPU 不逐盏更新；没亮的实例在 vertex() 里塌成 0 面积。
/// </summary>
public sealed record GlowSet(GlowKind Kind, int Count, float[] Buffer, Box3 Bounds)
{
    public const int Stride = 16;
}

/// <summary>One lamp or window owner tile. Lamp IDs index the resident catalog; reflection indices index this Glow buffer.</summary>
public sealed record NightlightTileContent(BakeKey Key, TileKey Tile,
    IReadOnlyList<int> LampIds, GlowSet Glow,
    IReadOnlyList<int> ReflectionWindowIndices);

/// <summary>Resident full-map lamp numbering, real ground heights and prepacked textures; arrays are frozen by the producer.</summary>
public sealed record NightlightCatalog(IReadOnlyList<LampRecord> Lamps,
    ReadOnlyMemory<float> LampTable, ReadOnlyMemory<float> LampGrid,
    int GridWidth, int GridHeight)
{
    public ReadOnlyMemory<float> WindowThresholds { get; init; }
}

/// <summary>Builds the resident lamp catalog synchronously from frozen map inputs before auxiliary directories are prepared.</summary>
public interface INightlightCatalogBuilder
{
    NightlightCatalog BuildCatalog(BuildContext context, IReadOnlyList<LightEmitter> emitters);
}
