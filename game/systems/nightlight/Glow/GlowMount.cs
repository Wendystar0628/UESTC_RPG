using Campus.Contracts;
using Campus.Kernel.Instances;
using Godot;

namespace Campus.Systems.Nightlight.Glow;

/// <summary>
/// 一类 fx_glow 实例挂在内核的实例池上（IInstancePoolHost，ENG-PERF：每类一个 MultiMesh）。
/// 材质向材质库要 fx_glow 一份、只设 fx_kind；网格是中心在原点、朝 +Z 的 1 × 1 四边形（fx_glow 按 VERTEX.xy 取面片坐标）。
/// 清单变了就整块换缓冲；容量不够时按 256 取整重挂。亮度不经这里，全由着色器读通道。
/// </summary>
public sealed class GlowMount(GlowKind kind, string poolId)
{
    private const string Owner = "WP-39";
    private const int CapacityStep = 256;
    private const float BoundsPad = 8f;        // 灯头光晕随距离放大、夜雾里半径 ×2、朝镜头拉近，包围盒统一外扩
    public const uint Layer = 1u << 5;         // 第 6 层：草、粒子、光晕、光锥、光柱（ENG-PERF 图层登记）

    private static readonly float[] QuadPosition = [-0.5f, -0.5f, 0f, 0.5f, -0.5f, 0f, 0.5f, 0.5f, 0f, -0.5f, 0.5f, 0f];
    private static readonly float[] QuadUv = [0f, 1f, 1f, 1f, 1f, 0f, 0f, 0f];
    private static readonly int[] QuadIndex = [0, 1, 2, 0, 2, 3];

    private IInstancePool? _pool;
    private ShaderMaterial? _material;

    public GlowKind Kind { get; } = kind;
    public int Visible => _pool?.VisibleCount ?? 0;

    public void Upload(IInstancePoolHost host, Func<ShaderMaterial> createMaterial, GlowSet set)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(createMaterial);
        ArgumentNullException.ThrowIfNull(set);
        if (set.Kind != Kind) throw new ArgumentException($"Glow set {set.Kind} does not belong to the {Kind} pool.", nameof(set));
        if (set.Count == 0)
        {
            if (_pool is not null) _pool.VisibleCount = 0;
            return;
        }
        var box = Pad(set.Bounds);
        if (_pool is null || _pool.Capacity < set.Count)
        {
            _pool?.Free();
            if (_material is null)
            {
                _material = createMaterial();
                _material.SetShaderParameter("fx_kind", (int)Kind);
            }
            var capacity = (set.Count + CapacityStep - 1) / CapacityStep * CapacityStep;
            _pool = host.Mount(new InstancePoolSpec(poolId, Owner, QuadPosition, QuadUv, QuadIndex, _material, capacity, box, Layer));
        }
        var data = new float[_pool.Capacity * GlowSet.Stride];
        set.Buffer.AsSpan(0, set.Count * GlowSet.Stride).CopyTo(data);
        _pool.VisibleCount = 0;
        _pool.WriteBuffer(data);
        _pool.SetAabb(box);
        _pool.VisibleCount = set.Count;
    }

    /// <summary>撤掉实例（切图、离树）。材质留着，下次挂上时复用。</summary>
    public void Free()
    {
        _pool?.Free();
        _pool = null;
    }

    private static Aabb Pad(Box3 b)
    {
        var min = new Vector3(b.Min.X, b.Min.Y, b.Min.Z) - Vector3.One * BoundsPad;
        var max = new Vector3(b.Max.X, b.Max.Y, b.Max.Z) + Vector3.One * BoundsPad;
        return new Aabb(min, max - min);
    }
}
