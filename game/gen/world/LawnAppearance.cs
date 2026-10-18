using Campus.Contracts;

namespace Campus.Gen.World;

/// <summary>Shared lawn palette columns, colour families and material for ground and near grass.</summary>
internal static class LawnAppearance
{
    public const ushort Grass = 150, GrassLight = 190, GrassDark = 192;
    public const string GrassFamily = "grass", LightFamily = "ground.grassLight", DarkFamily = "ground.grassDark";

    public static ToonSpec Spec() => new(new(GrassFamily + ".lit"), new(GrassFamily + ".shade")) { Wet = 0.4f };
}

/// <summary>
/// Smooth, deterministic value noise for lawn tones: two octaves of hashed lattice values with quintic fade.
/// Shared by the ground's contour field and near grass, using the existing integer noise implementation.
/// </summary>
internal static class LawnNoise
{
    public static uint Seed(uint worldSeed) => StableHash.Hash32(worldSeed, "ground.lawn");

    public static float At(float x, float z, uint seed, LawnTone tone)
    {
        var a = IntegerNoise.Value2D(x / tone.Scale, z / tone.Scale, seed);
        var b = IntegerNoise.Value2D(x / tone.DetailScale, z / tone.DetailScale, seed ^ 0x9E3779B9u);
        return a * (1f - tone.DetailWeight) + b * tone.DetailWeight;
    }

    /// <summary>The palette column under a grass foot; the caller supplies its surface's dark edge classification.</summary>
    public static ushort ColorAt(float x, float z, uint seed, LawnTone tone, bool darkEdge)
    {
        if (darkEdge) return LawnAppearance.GrassDark;
        var n = At(x, z, seed, tone);
        return n < tone.Low ? LawnAppearance.GrassDark : n < tone.High ? LawnAppearance.Grass : LawnAppearance.GrassLight;
    }
}

/// <summary>
/// Large-scale light and shade on lawns, so a big lawn reads as sunlit grass with depth rather than a flat carpet:
/// low-frequency noise splits it into broad lighter and darker patches with clean edges, and the lawn is one tone
/// darker along kerbs, paving and building feet. Tones follow the seasonal grass colour.
/// </summary>
public sealed record LawnTone
{
    public static LawnTone Default { get; } = new();

    /// <summary>Feature size of the two noise octaves, metres; patches come out about a third of this across.</summary>
    public float Scale { get; init; } = 64f;
    public float DetailScale { get; init; } = 17f;
    public float DetailWeight { get; init; }
    /// <summary>Noise below this is the darker tone, above <see cref="High"/> the lighter tone.</summary>
    public float Low { get; init; } = 0.36f;
    public float High { get; init; } = 0.66f;
    /// <summary>Width of the darker band along kerbs, paving and building feet; 0 turns it off.</summary>
    public float EdgeBand { get; init; } = 0.5f;
}
