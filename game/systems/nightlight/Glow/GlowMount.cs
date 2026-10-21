using Campus.Contracts;
using Campus.Kernel.Instances;
using Godot;

namespace Campus.Systems.Nightlight.Glow;

/// <summary>
/// One owner tile and fx_glow kind mounted through the kernel instance-pool host.
/// 材质向材质库要 fx_glow 一份、只设 fx_kind；网格是中心在原点、朝 +Z 的 1 × 1 四边形（fx_glow 按 VERTEX.xy 取面片坐标）。
/// One frozen tile buffer is uploaded at mount; brightness remains driven by shader channels.
/// </summary>
public sealed class GlowMount(GlowKind kind, string poolId)
{
    private const int CapacityStep = 256;
    public const float BoundsPad = 8f;        // 灯头光晕随距离放大、夜雾里半径 ×2、朝镜头拉近，包围盒统一外扩
    public const uint Layer = RenderLayers.GrassAndEffects;         // 第 6 层：草、粒子与光晕（ENG-PERF 图层登记）

    private static readonly float[] QuadPosition = [-0.5f, -0.5f, 0f, 0.5f, -0.5f, 0f, 0.5f, 0.5f, 0f, -0.5f, 0.5f, 0f];
    private static readonly float[] QuadUv = [0f, 1f, 1f, 1f, 1f, 0f, 0f, 0f];
    private static readonly int[] QuadIndex = [0, 1, 2, 0, 2, 3];

    private IInstancePool? _pool;
    private ShaderMaterial? _material;
    private bool _uploaded;
    private int _count;

    public bool IsReady => _uploaded && (_count == 0 || (_pool is not null && _pool.IsReady &&
        _material is not null && GodotObject.IsInstanceValid(_material) && _material.Shader is not null));

    public bool VisibleReady => _uploaded && (_count == 0 || _pool is not null && _pool.VisibleCount == _count);

    public void SetVisible(bool visible)
    {
        var count = visible ? _count : 0;
        if (_pool is not null && _pool.VisibleCount != count) _pool.VisibleCount = count;
    }

    public GlowKind Kind { get; } = kind;

    public void Upload(IInstancePoolHost host, Func<ShaderMaterial> createMaterial, GlowSet set)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(createMaterial);
        ArgumentNullException.ThrowIfNull(set);
        if (set.Kind != Kind) throw new ArgumentException($"Glow set {set.Kind} does not belong to the {Kind} pool.", nameof(set));
        _uploaded = false;
        _count = set.Count;
        if (set.Count == 0)
        {
            if (_pool is not null) _pool.VisibleCount = 0;
            _uploaded = true;
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
            _pool = host.Mount(new InstancePoolSpec(poolId, QuadPosition, QuadUv, QuadIndex, _material, ShaderId.FxGlow, capacity, box, Layer));
        }
        var data = new float[_pool.Capacity * GlowSet.Stride];
        set.Buffer.AsSpan(0, set.Count * GlowSet.Stride).CopyTo(data);
        _pool.VisibleCount = 0;
        _pool.WriteBuffer(data);
        _pool.SetAabb(box);
        _uploaded = true;
    }

    /// <summary>Releases this tile pool; the node retains its shared per-kind material.</summary>
    public void Free()
    {
        _pool?.Free();
        _pool = null;
        _material = null;
        _uploaded = false;
    }

    private static Aabb Pad(Box3 b)
    {
        var min = new Vector3(b.Min.X, b.Min.Y, b.Min.Z) - Vector3.One * BoundsPad;
        var max = new Vector3(b.Max.X, b.Max.Y, b.Max.Z) + Vector3.One * BoundsPad;
        return new Aabb(min, max - min);
    }
}
