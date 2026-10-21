using Campus.Contracts;
using Campus.Contracts.Quality;

namespace Campus.Kernel.Lighting;

/// <summary>
/// Engine-side shadow configuration for the two shadow knobs game/kernel/lighting/ owns (SYS-GRAPHICS, ENG-PERF).
/// None of these change specialization constants: atlas size, split count, distance, splits and the
/// caster mask are all switched without pipeline compilation. Small casters always cast (style default).
/// </summary>
public readonly record struct ShadowSettings(int AtlasSize, int Splits, float MaxDistance,
    float Split1, float Split2, float Split3, uint CasterMask)
{
    // ENG-PERF: PSSM 4 at 1600 m splits at 48 / 192 / 640 m; PSSM 2 uses Split1 = 0.15.
    private const float Near1 = 48f, Near2 = 192f, Near3 = 640f;

    public static ShadowSettings From(QualityView view)
    {
        ArgumentNullException.ThrowIfNull(view);
        return From((int)MathF.Round(view[QualityKnob.ShadowQuality]), view[QualityKnob.ShadowDistance]);
    }

    /// <param name="quality">0 = 2048 · 2 splits, 1 = 4096 · 4 splits, 2 = 8192 · 4 splits.</param>
    public static ShadowSettings From(int quality, float distance)
    {
        var (size, splits) = quality switch
        {
            0 => (2048, 2),
            1 => (4096, 4),
            2 => (8192, 4),
            _ => throw new ArgumentOutOfRangeException(nameof(quality), quality, "Shadow quality is 0, 1 or 2."),
        };
        if (!float.IsFinite(distance) || distance < 600f || distance > 1600f)
            throw new ArgumentOutOfRangeException(nameof(distance), distance, "Shadow distance is 600–1600 m.");
        var mask = ~RenderLayers.NonCasters;
        if (splits == 2) return new(size, 2, distance, 0.15f, 0f, 0f, mask);
        // Keep the near cascades at their absolute size; the third boundary never exceeds 60 % of the range.
        var s1 = Near1 / distance;
        var s2 = Near2 / distance;
        var s3 = MathF.Min(Near3 / distance, 0.6f);
        return new(size, 4, distance, s1, s2, MathF.Max(s3, s2 + 0.05f), mask);
    }
}
