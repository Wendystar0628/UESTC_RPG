using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Vegetation;
using GdUnit4;

namespace Campus.Systems.Vegetation.Tests;

/// <summary>
/// Pool choice of the vegetation runtime (WLD-VEGETATION §11.4): near bands scaled by tree height, 10% hysteresis outwards,
/// and the near / far cross-fade band where a tree is in both pools (max-011 23c2c).
/// </summary>
[TestSuite]
public sealed class VegetationBucketsTests
{
    [TestCase]
    public void BandsFollowTheDistanceAndTheTreeHeight()
    {
        foreach (var (d, level) in new[] { (10f, 0), (20f, 1), (100f, 2), (160f, 2), (300f, 3), (1500f, 3) })
            Check(VegetationBuckets.Target(d, 1f, VegetationBuckets.NotNear) == level, $"{d} m on a 12 m tree");
        // A 24 m tree keeps L0+ to 30 m; the near pool's outer bound does not scale.
        Check(VegetationBuckets.Target(25f, 2f, VegetationBuckets.NotNear) == 0, "tall tree L0+");
        Check(VegetationBuckets.Target(170f, 2f, VegetationBuckets.NotNear) == VegetationBuckets.NotNear, "near pool edge does not scale");
    }

    [TestCase]
    public void TreesMoveInAtOnceAndOutOnlyPastTenPercent()
    {
        Check(VegetationBuckets.Target(16f, 1f, 0) == 0 && VegetationBuckets.Target(17f, 1f, 0) == 1, "L0+ → L0 at 16.5 m");
        Check(VegetationBuckets.Target(180f, 1f, 2) == 2 && VegetationBuckets.Target(182f, 1f, 2) == VegetationBuckets.NotNear, "L1 out at 181.5 m");
        Check(VegetationBuckets.Target(14f, 1f, 1) == 0 && VegetationBuckets.Target(164f, 1f, VegetationBuckets.NotNear) == 2, "inwards at the band itself");
    }

    [TestCase]
    public void TheCrossFadeBandIsInBothPools()
    {
        // The far pool covers the band at every detail factor (the shaders scale it by quality.x down to 0.7).
        var start = VegFade.NearFadeStart * VegetationBuckets.MinDetail;
        Check(!VegetationBuckets.TargetFar(start - 1f, false) && VegetationBuckets.TargetFar(start + 1f, false), "far pool joins at the band start");
        Check(VegetationBuckets.TargetFar(start - 1f, true) && !VegetationBuckets.TargetFar(start / VegetationBuckets.Hysteresis - 1f, true), "far pool leaves 10% inside");
        for (var d = start + 1f; d < VegFade.NearFadeEnd; d += 2f)
            Check(VegetationBuckets.Target(d, 1f, VegetationBuckets.NotNear) == 2 && VegetationBuckets.TargetFar(d, false), $"{d} m in both pools");
        // No crown clouds yet: the far stand-in has no outer bound.
        Check(VegetationBuckets.TargetFar(5000f, true) == !VegFade.CloudReady, "far pool has no outer bound");
    }

    [TestCase]
    public void UpdateReportsMovesAndResetDropsTheHistory()
    {
        var records = new float[3 * InstanceStreamLayout.FloatsPerInstance];
        InstanceStreamLayout.Write(records.AsSpan(0, 16), Matrix4x4.CreateTranslation(0f, 0f, 0f), new(0.1f, 16f, 0.5f, 12f));
        InstanceStreamLayout.Write(records.AsSpan(16, 16), Matrix4x4.CreateTranslation(300f, 0f, 0f), new(0.2f, 16f, 0.5f, 12f));
        InstanceStreamLayout.Write(records.AsSpan(32, 16), Matrix4x4.CreateTranslation(150f, 0f, 10f), new(0.3f, 16f, 0.5f, 12f));
        var buckets = new VegetationBuckets(records);
        var eye = new Vector3(0f, 6f, 10f);   // 10 m from the first tree's middle
        Check(buckets.Update(eye, reset: false) && buckets.Level(0) == 0 && !buckets.Far(0), "first bucketing: near tree");
        Check(buckets.Level(1) == VegetationBuckets.NotNear && buckets.Far(1), "first bucketing: far tree");
        Check(buckets.Level(2) == 2 && buckets.Far(2), "first bucketing: tree in the band is in both pools");
        Check(!buckets.Update(eye, reset: false), "a still eye moves nothing");
        var back = new Vector3(0f, 6f, 16f);  // inside the 10% band: stays L0+ unless reset
        Check(!buckets.Update(back, reset: false) && buckets.Level(0) == 0, "hysteresis holds");
        Check(buckets.Update(back, reset: true) && buckets.Level(0) == 1, "a new world instance re-buckets without history");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
