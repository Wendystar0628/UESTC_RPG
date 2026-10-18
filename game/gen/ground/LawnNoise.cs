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
        var a = IntegerNoise.Value2D(x / tone.Scale, z / tone.Scale, seed);
        var b = IntegerNoise.Value2D(x / tone.DetailScale, z / tone.DetailScale, seed ^ 0x9E3779B9u);
        return a * (1f - tone.DetailWeight) + b * tone.DetailWeight;
    }
}
