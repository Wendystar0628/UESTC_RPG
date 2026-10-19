using Campus.Contracts;
using Campus.Contracts.Vegetation;
using N = System.Numerics;

namespace Campus.Systems.Vegetation;

/// <summary>
/// Near pool choice for each tree. L0+/L0 distances scale with height, the 165 m exit retains the original 10% hysteresis.
/// Far stand-ins belong to static tile geometry and never enter these buckets.
/// </summary>
public sealed class VegetationBuckets
{
    public const float L0PlusDistance = 15f;
    public const float L0Distance = 40f;
    public const float Hysteresis = 1.1f;

    /// <summary>Level index outside the near pool.</summary>
    public const byte NotNear = 3;

    private readonly N.Vector3[] _centre;
    private readonly float[] _scale;
    private readonly byte[] _level;

    /// <summary>Records are the 16-float instance records: the trunk foot is the translation, the height is INSTANCE_CUSTOM.w.</summary>
    public VegetationBuckets(ReadOnlySpan<float> records)
    {
        if (records.Length % InstanceStreamLayout.FloatsPerInstance != 0)
            throw new ArgumentException("Instance records must be complete.", nameof(records));
        var count = records.Length / InstanceStreamLayout.FloatsPerInstance;
        _centre = new N.Vector3[count];
        _scale = new float[count];
        _level = new byte[count];
        Array.Fill(_level, NotNear);
        for (var i = 0; i < count; i++)
        {
            var r = records.Slice(i * InstanceStreamLayout.FloatsPerInstance, InstanceStreamLayout.FloatsPerInstance);
            var height = MathF.Max(r[15], 1f);
            // Distances are taken to the middle of the tree, not its foot (the shaders' veg_pool_code takes the same point).
            _centre[i] = new N.Vector3(r[3], r[7] + height * 0.5f, r[11]);
            _scale[i] = height / 12f;
        }
    }

    public int Count => _level.Length;

    /// <summary>0 L0+, 1 L0, 2 L1 (the <c>VegPool</c> order), <see cref="NotNear"/> outside the near pool.</summary>
    public byte Level(int tree) => _level[tree];

    /// <summary>Re-buckets every tree for the eye; reset drops the hysteresis (a new world instance). True when any tree moved.</summary>
    public bool Update(N.Vector3 eye, bool reset)
    {
        var changed = false;
        for (var i = 0; i < _level.Length; i++)
        {
            var d = N.Vector3.Distance(eye, _centre[i]);
            var near = Target(d, _scale[i], reset ? NotNear : Math.Min(_level[i], NotNear));
            if (near == _level[i]) continue;
            _level[i] = near;
            changed = true;
        }
        return changed;
    }

    /// <summary>Near level (0–2) or <see cref="NotNear"/> for a tree now at <paramref name="current"/> (0–3).</summary>
    public static byte Target(float distance, float scale, byte current)
    {
        var raw = Raw(distance, scale, 1f);
        if (raw <= current) return raw;
        // Coarser only past the widened bounds.
        return Math.Max(current, Raw(distance, scale, Hysteresis));
    }

    private static byte Raw(float d, float scale, float widen) =>
        d < L0PlusDistance * scale * widen ? (byte)0 :
        d < L0Distance * scale * widen ? (byte)1 :
        d < VegFade.NearFadeEnd * widen ? (byte)2 : NotNear;
}
