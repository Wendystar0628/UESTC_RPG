namespace Campus.Gen.Diorama;

/// <summary>Deterministic integer-hash value noise for the backdrop shapes (no platform transcendental functions).</summary>
internal static class DioramaNoise
{
    public static uint Hash(uint x, uint seed)
    {
        var h = x * 0x9E3779B1u ^ seed * 0x85EBCA77u;
        h ^= h >> 15;
        h *= 0x2C1B3C6Du;
        h ^= h >> 12;
        h *= 0x297A2D39u;
        h ^= h >> 15;
        return h;
    }

    /// <summary>Uniform 0–1 from a hash.</summary>
    public static float Unit(uint x, uint seed) => (Hash(x, seed) >> 8) * (1f / 16777216f);

    /// <summary>Smooth 1-D value noise in −1…1 with unit period, periodic over <paramref name="wrap"/> cells (0 = no wrap).</summary>
    public static float Value(float t, uint seed, int wrap = 0)
    {
        var i = (int)MathF.Floor(t);
        var f = t - i;
        var a = Cell(i, wrap);
        var b = Cell(i + 1, wrap);
        var s = f * f * (3f - 2f * f);
        return (Unit(a, seed) + (Unit(b, seed) - Unit(a, seed)) * s) * 2f - 1f;
    }

    private static uint Cell(int i, int wrap)
    {
        if (wrap > 0) i = ((i % wrap) + wrap) % wrap;
        return unchecked((uint)i);
    }
}
