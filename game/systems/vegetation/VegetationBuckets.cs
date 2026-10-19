using Campus.Contracts;
using Campus.Contracts.Vegetation;
using N = System.Numerics;

namespace Campus.Systems.Vegetation;

/// <summary>
/// Pool choice per tree of one mother model (WLD-VEGETATION, ENG-PERF): L0+, L0 and L1 within the near pool, and
/// the far pool (<see cref="VegFade"/>). The L0+ / L0 / L1 switch distances are for a 12 m tree and scale with the tree height
/// (veg_common's VEG_LOD_L0 / VEG_LOD_L1); veg_card has already shrunk the finer level's extra cards at the distance itself,
/// so those swaps do not show. Between the near pool's L1 and the far stand-in a tree is in both pools over the
/// cross-fade band (<see cref="VegFade.NearFadeStart"/>–<see cref="VegFade.NearFadeEnd"/>) and the shaders split its pixels
/// with complementary Bayer dither (max-011 23c2c). The far pool starts at the band for the lowest detail factor
/// (<see cref="MinDetail"/>), since the shaders scale the band by quality.x. A tree moves inwards at once and outwards only
/// 10% beyond a bound, so it never flickers on a boundary. Without crown clouds the far pool has no outer bound.
/// </summary>
public sealed class VegetationBuckets
{
    public const float L0PlusDistance = 15f;
    public const float L0Distance = 40f;
    public const float Hysteresis = 1.1f;

    /// <summary>Lowest detail factor of the graphics presets (SYS-GRAPHICS: 0.7–1.0).</summary>
    public const float MinDetail = 0.7f;

    /// <summary>Level index of a tree outside the near pool (it may still be in the far pool, <see cref="Far"/>).</summary>
    public const byte NotNear = 3;

    /// <summary>Level index of a tree drawn nowhere.</summary>
    public const byte Hidden = 4;

    private readonly N.Vector3[] _centre;
    private readonly float[] _scale;
    private readonly byte[] _level;
    private readonly bool[] _far;

    /// <summary>Records are the 16-float instance records: the trunk foot is the translation, the height is INSTANCE_CUSTOM.w.</summary>
    public VegetationBuckets(ReadOnlySpan<float> records)
    {
        if (records.Length % InstanceStreamLayout.FloatsPerInstance != 0)
            throw new ArgumentException("Instance records must be complete.", nameof(records));
        var count = records.Length / InstanceStreamLayout.FloatsPerInstance;
        _centre = new N.Vector3[count];
        _scale = new float[count];
        _level = new byte[count];
        _far = new bool[count];
        Array.Fill(_level, Hidden);
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

    /// <summary>0 L0+, 1 L0, 2 L1 (the <c>VegPool</c> order), <see cref="NotNear"/> outside the near pool, <see cref="Hidden"/> in no pool.</summary>
    public byte Level(int tree) => _level[tree];

    /// <summary>The tree is (also) in the far pool.</summary>
    public bool Far(int tree) => _far[tree];

    /// <summary>Re-buckets every tree for the eye; reset drops the hysteresis (a new world instance). True when any tree moved.</summary>
    public bool Update(N.Vector3 eye, bool reset)
    {
        var changed = false;
        for (var i = 0; i < _level.Length; i++)
        {
            var d = N.Vector3.Distance(eye, _centre[i]);
            var far = TargetFar(d, !reset && _far[i]);
            var near = Target(d, _scale[i], reset ? NotNear : Math.Min(_level[i], NotNear));
            var next = near == NotNear && !far ? Hidden : near;
            if (next == _level[i] && far == _far[i]) continue;
            _level[i] = next;
            _far[i] = far;
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

    /// <summary>Far-pool membership: from the band's start at the lowest detail factor, left only 10% inside it.</summary>
    public static bool TargetFar(float distance, bool current)
    {
        var start = VegFade.NearFadeStart * MinDetail / (current ? Hysteresis : 1f);
        if (distance <= start) return false;
        return !VegFade.CloudReady || distance < VegFade.CloudFadeEnd * (current ? Hysteresis : 1f);
    }

    private static byte Raw(float d, float scale, float widen) =>
        d < L0PlusDistance * scale * widen ? (byte)0 :
        d < L0Distance * scale * widen ? (byte)1 :
        d < VegFade.NearFadeEnd * widen ? (byte)2 : NotNear;
}
