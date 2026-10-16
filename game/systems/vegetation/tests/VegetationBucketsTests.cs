using System.Numerics;
using Campus.Contracts;
using GdUnit4;

namespace Campus.Systems.Vegetation.Tests;

/// <summary>Pool choice of the vegetation runtime (WLD-VEGETATION §11.4): bands scaled by tree height, 10% hysteresis outwards.</summary>
[TestSuite]
public sealed class VegetationBucketsTests
{
    [TestCase]
    public void BandsFollowTheDistanceAndTheTreeHeight()
    {
        foreach (var (d, level) in new[] { (10f, 0), (20f, 1), (100f, 2), (300f, 3), (500f, 4) })
            Check(VegetationBuckets.Target(d, 1f, VegetationBuckets.Hidden) == level, $"{d} m on a 12 m tree");
        // A 24 m tree keeps L0+ to 30 m; the near / far / hidden bounds do not scale.
        Check(VegetationBuckets.Target(25f, 2f, VegetationBuckets.Hidden) == 0, "tall tree L0+");
        Check(VegetationBuckets.Target(160f, 2f, VegetationBuckets.Hidden) == 3, "near pool edge does not scale");
    }

    [TestCase]
    public void TreesMoveInAtOnceAndOutOnlyPastTenPercent()
    {
        Check(VegetationBuckets.Target(16f, 1f, 0) == 0 && VegetationBuckets.Target(17f, 1f, 0) == 1, "L0+ → L0 at 16.5 m");
        Check(VegetationBuckets.Target(160f, 1f, 2) == 2 && VegetationBuckets.Target(166f, 1f, 2) == 3, "L1 → far at 165 m");
        Check(VegetationBuckets.Target(14f, 1f, 1) == 0 && VegetationBuckets.Target(149f, 1f, 3) == 2, "inwards at the band itself");
    }

    [TestCase]
    public void UpdateReportsMovesAndResetDropsTheHistory()
    {
        var records = new float[2 * InstanceStreamLayout.FloatsPerInstance];
        InstanceStreamLayout.Write(records.AsSpan(0, 16), Matrix4x4.CreateTranslation(0f, 0f, 0f), new(0.1f, 16f, 0.5f, 12f));
        InstanceStreamLayout.Write(records.AsSpan(16, 16), Matrix4x4.CreateTranslation(200f, 0f, 0f), new(0.2f, 16f, 0.5f, 12f));
        var buckets = new VegetationBuckets(records);
        var eye = new Vector3(0f, 6f, 10f);   // 10 m from the first tree's middle
        Check(buckets.Update(eye, reset: false) && buckets.Level(0) == 0 && buckets.Level(1) == 3, "first bucketing");
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
