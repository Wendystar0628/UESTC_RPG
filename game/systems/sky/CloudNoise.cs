using Campus.Contracts;

namespace Campus.Systems.Sky;

/// <summary>Resource layout and coverage lookup for the build-generated cloud_noise texture (ENG-PERF, SYS-WEATHER).</summary>
public static class CloudNoise
{
    public const int Size = 512;
    public const int CoverageSteps = 101;
    public const string ResourcePath = "res://data/cloud-noise.res";
    public const string ThresholdMetadata = "coverage_thresholds";

    /// <summary>Linear lookup into the coverage → threshold table.</summary>
    public static float ThresholdFor(float[] table, float coverage)
    {
        ArgumentNullException.ThrowIfNull(table);
        if (!float.IsFinite(coverage) || coverage <= 0f) return 1f;
        var x = Math.Clamp(coverage, 0f, 1f) * (table.Length - 1);
        var i = Math.Min((int)x, table.Length - 2);
        return ScalarMath.Lerp(table[i], table[i + 1], x - i);
    }
}
