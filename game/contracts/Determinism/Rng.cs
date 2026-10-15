namespace Campus.Contracts;

/// <summary>A reproducible SplitMix32 stream, including seed zero.</summary>
/// <remarks>
/// The state advances by 0x9E3779B9, then uses the fixed 0x21F0AAAD / 0x735A2D97
/// integer mixer. Every arithmetic overflow is modulo 2^32. Copying this struct
/// copies its stream position; default(Rng) is equivalent to new Rng(0).
/// This is a procedural-content generator, not a cryptographic generator.
/// Changing the algorithm changes generated content and requires a bake ABI change.
/// </remarks>
public struct Rng
{
    private uint _state;

    public Rng(uint seed) => _state = seed;

    /// <summary>Returns one of 2^24 equally spaced values in [0, 1).</summary>
    public float NextFloat() => (NextUInt32() >> 8) * (1f / 16777216f);

    /// <summary>Returns an unbiased integer in [0, max); max must be positive.</summary>
    /// <remarks>Rejection sampling can consume more than one stream value.</remarks>
    public int NextInt(int max)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(max);
        uint bound = (uint)max;
        uint threshold = unchecked(0u - bound) % bound;
        uint value;
        do
        {
            value = NextUInt32();
        }
        while (value < threshold);

        return (int)(value % bound);
    }

    private uint NextUInt32()
    {
        unchecked
        {
            _state += 0x9E3779B9u;
            uint value = _state;
            value = (value ^ (value >> 16)) * 0x21F0AAADu;
            value = (value ^ (value >> 15)) * 0x735A2D97u;
            return value ^ (value >> 15);
        }
    }
}
