using Campus.Contracts;
using N = System.Numerics;

namespace Campus.Systems.Vegetation;

/// <summary>
/// Pool choice per tree of one mother model (WLD-VEGETATION §11.4, ENG-PERF §9): L0+, L0 and L1 within the near pool
/// (150 m), the far pool to 400 m, nothing beyond (crown clouds come with the tile geometry). The L0+ / L0 / L1 switch
/// distances are for a 12 m tree and scale with the tree height (veg_card's VEG_LOD_L0 / VEG_LOD_L1); a tree moves to a
/// finer pool inside the distance and to a coarser one only 10% beyond it, so it never flickers on a boundary. veg_card
/// has already shrunk the finer level's extra cards at the distance itself, so the swap does not show.
/// </summary>
public sealed class VegetationBuckets
{
    public const float L0PlusDistance = 15f;
    public const float L0Distance = 40f;
    public const float NearDistance = 150f;
    public const float FarDistance = 400f;
    public const float Hysteresis = 1.1f;

    /// <summary>Level index of a tree drawn nowhere.</summary>
    public const byte Hidden = 4;

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
        Array.Fill(_level, Hidden);
        for (var i = 0; i < count; i++)
        {
            var r = records.Slice(i * InstanceStreamLayout.FloatsPerInstance, InstanceStreamLayout.FloatsPerInstance);
            var height = MathF.Max(r[15], 1f);
            // Distances are taken to the middle of the tree, not its foot.
            _centre[i] = new N.Vector3(r[3], r[7] + height * 0.5f, r[11]);
            _scale[i] = height / 12f;
        }
    }

    public int Count => _level.Length;

    /// <summary>0 L0+, 1 L0, 2 L1, 3 far, <see cref="Hidden"/> beyond the far pool (the <c>VegPool</c> order).</summary>
    public byte Level(int tree) => _level[tree];

    /// <summary>Re-buckets every tree for the eye; reset drops the hysteresis (a new world instance). True when any tree moved.</summary>
    public bool Update(N.Vector3 eye, bool reset)
    {
        var changed = false;
        for (var i = 0; i < _level.Length; i++)
        {
            var current = reset ? Hidden : _level[i];
            var next = Target(N.Vector3.Distance(eye, _centre[i]), _scale[i], current);
            if (next == _level[i]) continue;
            _level[i] = next;
            changed = true;
        }
        return changed;
    }

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
        d < NearDistance * widen ? (byte)2 :
        d < FarDistance * widen ? (byte)3 : Hidden;
}
