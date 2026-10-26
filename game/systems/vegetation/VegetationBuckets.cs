using Campus.Contracts;
using Campus.Contracts.Vegetation;
using Campus.Contracts.Quality;
using N = System.Numerics;

namespace Campus.Systems.Vegetation;

/// <summary>
/// Per-tree near-pool choice and independent shadow membership. Godot visibility ranges and mesh LOD select an entire
/// MultiMesh together, so they cannot make these per-tree choices. Far stand-ins belong to static tile geometry.
/// </summary>
public sealed class VegetationBuckets
{
    public const float L0PlusDistance = 15f;
    public const float L0Distance = 40f;
    public const float Hysteresis = 1.1f;
    public const float ReferenceTreeHeight = 12f;

    public static float NearReach => VegFade.NearFadeEnd * QualityRules.DetailDistanceScale;

    /// <summary>Level index outside the near pool.</summary>
    public const byte NotNear = 3;

    private readonly N.Vector3[] _centre;
    private readonly float[] _scale;
    private readonly byte[] _level;
    private readonly byte[] _target;
    private readonly float[] _started;
    private readonly bool[] _shadow;
    private int _transitions;
    private int _membershipChanges;

    /// <summary>Records are the 16-float instance records: the trunk foot is the translation, the height is INSTANCE_CUSTOM.w.</summary>
    public VegetationBuckets(ReadOnlySpan<float> records)
    {
        if (records.Length % InstanceStreamLayout.FloatsPerInstance != 0)
            throw new ArgumentException("Instance records must be complete.", nameof(records));
        var count = records.Length / InstanceStreamLayout.FloatsPerInstance;
        _centre = new N.Vector3[count];
        _scale = new float[count];
        _level = new byte[count];
        _target = new byte[count];
        _started = new float[count];
        _shadow = new bool[count];
        Array.Fill(_level, NotNear);
        Array.Fill(_target, NotNear);
        Array.Fill(_started, -1f);
        for (var i = 0; i < count; i++)
        {
            var r = records.Slice(i * InstanceStreamLayout.FloatsPerInstance, InstanceStreamLayout.FloatsPerInstance);
            var height = MathF.Max(r[15], 1f);
            // Match the card-shrink/winter detail metric: the middle of the tree, not its foot.
            _centre[i] = new N.Vector3(r[3], r[7] + height * 0.5f, r[11]);
            _scale[i] = height / ReferenceTreeHeight;
        }
    }

    public int Count => _level.Length;

    /// <summary>0 L0+, 1 L0, 2 L1 (the <c>VegPool</c> order), <see cref="NotNear"/> outside the near pool.</summary>
    public byte Level(int tree) => _level[tree];
    public byte TargetLevel(int tree) => _target[tree];
    public float Started(int tree) => _started[tree];
    public bool IsTransitioning => _transitions != 0;
    public bool Contains(int tree, VegPool pool) => _level[tree] == (byte)pool || _target[tree] == (byte)pool;

    public bool InShadow(int tree) => _shadow[tree];

    internal bool MembershipChanged(VegPool pool) => (_membershipChanges & (1 << (byte)pool)) != 0;

    /// <summary>Original record order is the stable tree identity; prefix compaction never changes its clock.</summary>
    public bool Update(N.Vector3 eye, bool reset, float shadowReach, float clock, bool nearVisible)
    {
        var changed = false;
        _membershipChanges = 0;
        for (var i = 0; i < _level.Length; i++)
        {
            var d = N.Vector3.Distance(eye, _centre[i]);
            var shadow = d <= shadowReach;
            if (_shadow[i] != shadow)
            {
                _shadow[i] = shadow;
                _membershipChanges |= 1 << (byte)VegPool.Shadow;
                changed = true;
            }
            var before = (1 << _level[i]) | (1 << _target[i]);
            changed |= UpdateLevel(i, d, reset, clock, nearVisible);
            _membershipChanges |= before ^ ((1 << _level[i]) | (1 << _target[i]));
        }
        return changed;
    }

    private bool UpdateLevel(int i, float d, bool reset, float clock, bool nearVisible)
    {
        var changed = false;
        if (_started[i] >= 0f && Elapsed(clock, _started[i]) >= RenderFade.Seconds)
        {
            _level[i] = _target[i];
            _started[i] = -1f;
            _transitions--;
            changed = true;
        }
        if (!nearVisible || reset || _level[i] == NotNear)
        {
            var next = nearVisible ? Target(d, _scale[i], NotNear) : NotNear;
            if (_started[i] >= 0f) _transitions--;
            changed |= _level[i] != next || _target[i] != next || _started[i] >= 0f;
            _level[i] = _target[i] = next;
            _started[i] = -1f;
            return changed;
        }
        // Finish the current segment before applying a reversal; there is no half-fade restart.
        if (_started[i] >= 0f) return changed;
        var near = Target(d, _scale[i], _level[i]);
        if (near == _level[i]) return changed;
        _target[i] = near;
        _started[i] = clock;
        _transitions++;
        return true;
    }

    public static float Elapsed(float clock, float started) =>
        (clock - started + WorldTime.AnimLoopSeconds) % WorldTime.AnimLoopSeconds;

    /// <summary>The distance band triggers a bounded fade; a ready owner cell always retains at least L1.</summary>
    public static byte Target(float distance, float scale, byte current)
    {
        var raw = Raw(distance, scale);
        return raw < current && current < NotNear ? Raw(distance * Hysteresis, scale) : raw;
    }

    private static byte Raw(float d, float scale) =>
        d < L0PlusDistance * scale * QualityRules.DetailDistanceScale * (1f - VegFade.LevelBandFraction) ? (byte)0 :
        d < L0Distance * scale * QualityRules.DetailDistanceScale * (1f - VegFade.LevelBandFraction) ? (byte)1 : (byte)2;
}
