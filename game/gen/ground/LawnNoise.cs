using Campus.Contracts;

namespace Campus.Gen.Ground;

/// <summary>
/// Smooth, deterministic value noise for lawn tones: two octaves of hashed lattice values with quintic fade.
/// Integer hashing and polynomials only, so every machine produces the same bits (D-165).
/// </summary>
internal static class LawnNoise
{
    public static uint Seed(BuildContext ctx) => StableHash.Hash32(ctx.WorldSeed, "ground.lawn");

    public static float At(float x, float z, uint seed, LawnTone tone)
    {
        var a = Octave(x / tone.Scale, z / tone.Scale, seed);
        var b = Octave(x / tone.DetailScale, z / tone.DetailScale, seed ^ 0x9E3779B9u);
        return a * (1f - tone.DetailWeight) + b * tone.DetailWeight;
    }

    private static float Octave(float x, float z, uint seed)
    {
        var ix = (int)MathF.Floor(x);
        var iz = (int)MathF.Floor(z);
        var fx = Fade(x - ix);
        var fz = Fade(z - iz);
        var v00 = Lattice(ix, iz, seed);
        var v10 = Lattice(ix + 1, iz, seed);
        var v01 = Lattice(ix, iz + 1, seed);
        var v11 = Lattice(ix + 1, iz + 1, seed);
        var top = v00 + fx * (v10 - v00);
        var bottom = v01 + fx * (v11 - v01);
        return top + fz * (bottom - top);
    }

    private static float Fade(float t) => t * t * t * (t * (t * 6f - 15f) + 10f);

    private static float Lattice(int x, int z, uint seed)
    {
        unchecked
        {
            var h = seed ^ ((uint)x * 0x8DA6B343u) ^ ((uint)z * 0xD8163841u);
            h ^= h >> 15;
            h *= 0x2C1B3C6Du;
            h ^= h >> 12;
            h *= 0x297A2D39u;
            h ^= h >> 15;
            return (h >> 8) * (1f / 16777216f);
        }
    }
}
