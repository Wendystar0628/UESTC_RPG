using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Vegetation;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Campus.Systems.Vegetation.Tests;

/// <summary>
/// Pool choice of the vegetation runtime (WLD-VEGETATION): near bands scaled by tree height, 10% hysteresis outwards,
/// and the near / far cross-fade band where a tree is in both pools (max-011 23c2c).
/// </summary>
[TestSuite]
public sealed class VegetationBucketsTests
{
    [TestCase]
    public void BandsFollowTheDistanceAndTheTreeHeight()
    {
        foreach (var (d, level) in new[] { (10f, 0), (20f, 1), (100f, 2), (160f, 2), (300f, 3), (1500f, 3) })
            AssertThat(VegetationBuckets.Target(d, 1f, VegetationBuckets.NotNear) == level).OverrideFailureMessage($"{d} m on a 12 m tree").IsTrue();
        // A 24 m tree keeps L0+ to 30 m; the near pool's outer bound does not scale.
        AssertThat(VegetationBuckets.Target(25f, 2f, VegetationBuckets.NotNear) == 0).OverrideFailureMessage("tall tree L0+").IsTrue();
        AssertThat(VegetationBuckets.Target(170f, 2f, VegetationBuckets.NotNear) == VegetationBuckets.NotNear).OverrideFailureMessage("near pool edge does not scale").IsTrue();
    }

    [TestCase]
    public void TreesMoveInAtOnceAndOutOnlyPastTenPercent()
    {
        AssertThat(VegetationBuckets.Target(16f, 1f, 0) == 0 && VegetationBuckets.Target(17f, 1f, 0) == 1).OverrideFailureMessage("L0+ → L0 at 16.5 m").IsTrue();
        AssertThat(VegetationBuckets.Target(180f, 1f, 2) == 2 && VegetationBuckets.Target(182f, 1f, 2) == VegetationBuckets.NotNear).OverrideFailureMessage("L1 out at 181.5 m").IsTrue();
        AssertThat(VegetationBuckets.Target(14f, 1f, 1) == 0 && VegetationBuckets.Target(164f, 1f, VegetationBuckets.NotNear) == 2).OverrideFailureMessage("inwards at the band itself").IsTrue();
    }

    [TestCase]
    public void TheCrossFadeBandIsInBothPools()
    {
        // The far pool covers the band at every detail factor (the shaders scale it by quality.x down to 0.7).
        var start = VegFade.NearFadeStart * VegetationBuckets.MinDetail;
        AssertThat(!VegetationBuckets.TargetFar(start - 1f, false) && VegetationBuckets.TargetFar(start + 1f, false)).OverrideFailureMessage("far pool joins at the band start").IsTrue();
        AssertThat(VegetationBuckets.TargetFar(start - 1f, true) && !VegetationBuckets.TargetFar(start / VegetationBuckets.Hysteresis - 1f, true)).OverrideFailureMessage("far pool leaves 10% inside").IsTrue();
        for (var d = start + 1f; d < VegFade.NearFadeEnd; d += 2f)
            AssertThat(VegetationBuckets.Target(d, 1f, VegetationBuckets.NotNear) == 2 && VegetationBuckets.TargetFar(d, false)).OverrideFailureMessage($"{d} m in both pools").IsTrue();
        // No crown clouds yet: the far stand-in has no outer bound.
        AssertThat(VegetationBuckets.TargetFar(5000f, true) == !VegFade.CloudReady).OverrideFailureMessage("far pool has no outer bound").IsTrue();
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
        AssertThat(buckets.Update(eye, reset: false) && buckets.Level(0) == 0 && !buckets.Far(0)).OverrideFailureMessage("first bucketing: near tree").IsTrue();
        AssertThat(buckets.Level(1) == VegetationBuckets.NotNear && buckets.Far(1)).OverrideFailureMessage("first bucketing: far tree").IsTrue();
        AssertThat(buckets.Level(2) == 2 && buckets.Far(2)).OverrideFailureMessage("first bucketing: tree in the band is in both pools").IsTrue();
        AssertThat(!buckets.Update(eye, reset: false)).OverrideFailureMessage("a still eye moves nothing").IsTrue();
        var back = new Vector3(0f, 6f, 16f);  // inside the 10% band: stays L0+ unless reset
        AssertThat(!buckets.Update(back, reset: false) && buckets.Level(0) == 0).OverrideFailureMessage("hysteresis holds").IsTrue();
        AssertThat(buckets.Update(back, reset: true) && buckets.Level(0) == 1).OverrideFailureMessage("a new world instance re-buckets without history").IsTrue();
    }
}
