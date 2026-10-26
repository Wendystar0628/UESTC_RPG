using Campus.Tests;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Vegetation;
using Campus.Contracts.Quality;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Campus.Systems.Vegetation.Tests;

/// <summary>Distance triggers, bounded transitions and separately selected shadow casters.</summary>
[TestSuite]
public sealed class VegetationBucketsTests
{
    private static float First => VegetationBuckets.L0PlusDistance * QualityRules.DetailDistanceScale * (1f - VegFade.LevelBandFraction);
    private static float Second => VegetationBuckets.L0Distance * QualityRules.DetailDistanceScale * (1f - VegFade.LevelBandFraction);
    private static Vector3 Eye(float distance) => new(0, VegetationBuckets.ReferenceTreeHeight * .5f, distance);
    private static VegetationBuckets Tree(float x = 0f)
    {
        var records = new float[InstanceStreamLayout.FloatsPerInstance];
        InstanceStreamLayout.Write(records, Matrix4x4.CreateTranslation(x, 0, 0), new(.1f, 16, .5f, VegetationBuckets.ReferenceTreeHeight));
        return new(records);
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void BandsFollowTheSharedQualityFactorAndTreeHeight()
    {
        AssertThat(VegetationBuckets.Target(First * .8f, 1f, VegetationBuckets.NotNear) == (byte)VegPool.L0Plus).IsTrue();
        AssertThat(VegetationBuckets.Target((First + Second) * .5f, 1f, VegetationBuckets.NotNear) == (byte)VegPool.L0).IsTrue();
        AssertThat(VegetationBuckets.Target(Second * 1.5f, 1f, VegetationBuckets.NotNear) == (byte)VegPool.L1).IsTrue();
        AssertThat(VegetationBuckets.Target(First * 1.5f, 2f, VegetationBuckets.NotNear) == (byte)VegPool.L0Plus).IsTrue();
        AssertThat(VegetationBuckets.Target(First * .99f, 1f, (byte)VegPool.L0) == (byte)VegPool.L0 &&
            VegetationBuckets.Target(First / VegetationBuckets.Hysteresis * .99f, 1f, (byte)VegPool.L0) == (byte)VegPool.L0Plus).OverrideFailureMessage("reverse triggers retain their distance hysteresis").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Regression)]
    public void ATransitionFinishesAndReleasesTheOldLevelWhileTheEyeIsStill()
    {
        var buckets = Tree();
        buckets.Update(Eye(0), false, VegetationBuckets.NearReach, 0, true);
        var distance = (First + VegetationBuckets.L0PlusDistance * QualityRules.DetailDistanceScale) * .5f;
        AssertThat(distance > First).OverrideFailureMessage("fixture must stop inside the trigger band").IsTrue();
        var start = RenderFade.Seconds * 4f;
        AssertThat(buckets.Update(Eye(distance), false, VegetationBuckets.NearReach, start, true) &&
            buckets.Contains(0, VegPool.L0Plus) && buckets.Contains(0, VegPool.L0) && buckets.IsTransitioning).IsTrue();
        AssertThat(!buckets.MembershipChanged(VegPool.L0Plus) && buckets.MembershipChanged(VegPool.L0) &&
            !buckets.MembershipChanged(VegPool.Shadow)).OverrideFailureMessage("starting a fade changes the target prefix, while the source only needs its transition texel updated").IsTrue();
        buckets.Update(Eye(distance), false, VegetationBuckets.NearReach, start + RenderFade.Seconds * 1.5f, true);
        AssertThat(!buckets.Contains(0, VegPool.L0Plus) && buckets.Contains(0, VegPool.L0) && !buckets.IsTransitioning &&
            buckets.Level(0) == buckets.TargetLevel(0)).OverrideFailureMessage("a stationary camera must not keep two half-visible levels").IsTrue();
        AssertThat(buckets.MembershipChanged(VegPool.L0Plus) && !buckets.MembershipChanged(VegPool.L0)).IsTrue();
        AssertThat(!buckets.Update(Eye(distance), false, VegetationBuckets.NearReach, start + RenderFade.Seconds * 2f, true)).OverrideFailureMessage("settled state requires no further prefix or texture upload").IsTrue();
        AssertThat(!buckets.MembershipChanged(VegPool.L0Plus) && !buckets.MembershipChanged(VegPool.L0) &&
            !buckets.MembershipChanged(VegPool.Shadow)).IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Regression)]
    public void AReversalCompletesItsCurrentSegmentAcrossTheClockLoop()
    {
        var buckets = Tree();
        buckets.Update(Eye(0), false, VegetationBuckets.NearReach, 0, true);
        var started = WorldTime.AnimLoopSeconds - RenderFade.Seconds * .5f;
        buckets.Update(Eye(First * 1.05f), false, VegetationBuckets.NearReach, started, true);
        var originalStart = buckets.Started(0);
        buckets.Update(Eye(0), false, VegetationBuckets.NearReach, 0, true);
        AssertThat(buckets.Started(0) == originalStart && buckets.Level(0) == (byte)VegPool.L0Plus &&
            buckets.TargetLevel(0) == (byte)VegPool.L0).OverrideFailureMessage("reversing before completion must not restart a half-fade").IsTrue();
        var reversedAt = RenderFade.Seconds * .75f;
        buckets.Update(Eye(0), false, VegetationBuckets.NearReach, reversedAt, true);
        AssertThat(buckets.Level(0) == (byte)VegPool.L0 && buckets.TargetLevel(0) == (byte)VegPool.L0Plus &&
            buckets.Started(0) == reversedAt).OverrideFailureMessage("the completed segment becomes the reverse segment's full starting level").IsTrue();
        AssertThat(!buckets.MembershipChanged(VegPool.L0Plus) && !buckets.MembershipChanged(VegPool.L0)).OverrideFailureMessage("a completed segment that immediately reverses keeps both prefixes while their transition changes").IsTrue();
        buckets.Update(Eye(0), false, VegetationBuckets.NearReach, reversedAt + RenderFade.Seconds * 1.5f, true);
        AssertThat(!buckets.IsTransitioning && buckets.Level(0) == (byte)VegPool.L0Plus).IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Regression)]
    public void EqualCountsWithDifferentTreeIdentitiesStillChangeBothPrefixes()
    {
        var stride = InstanceStreamLayout.FloatsPerInstance;
        var records = new float[stride * 2];
        var separation = VegetationBuckets.NearReach * 4f;
        InstanceStreamLayout.Write(records.AsSpan(0, stride), Matrix4x4.Identity, new(0, 0, 0, VegetationBuckets.ReferenceTreeHeight));
        InstanceStreamLayout.Write(records.AsSpan(stride, stride), Matrix4x4.CreateTranslation(separation, 0, 0), new(0, 0, 0, VegetationBuckets.ReferenceTreeHeight));
        var buckets = new VegetationBuckets(records);
        buckets.Update(Eye(0), true, VegetationBuckets.NearReach, 0, true);
        AssertThat(buckets.Contains(0, VegPool.L0Plus) && !buckets.Contains(1, VegPool.L0Plus) &&
            buckets.InShadow(0) && !buckets.InShadow(1)).IsTrue();
        buckets.Update(Eye(0) + new Vector3(separation, 0, 0), true, VegetationBuckets.NearReach, 0, true);
        AssertThat(!buckets.Contains(0, VegPool.L0Plus) && buckets.Contains(1, VegPool.L0Plus) &&
            !buckets.InShadow(0) && buckets.InShadow(1) && buckets.MembershipChanged(VegPool.L0Plus) &&
            buckets.MembershipChanged(VegPool.L1) && buckets.MembershipChanged(VegPool.Shadow)).IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Regression)]
    public void ShadowMembershipIsIndependentOfVisibleCellReadinessAndTracksQuality()
    {
        var fine = ShadowSettings.From(1, ShadowSettings.MaximumDistance);
        var shadowReach = fine.MaxDistance * fine.Split2;
        AssertThat(shadowReach > VegetationBuckets.NearReach).OverrideFailureMessage("fixture must cover a tree beyond the near envelope").IsTrue();
        var buckets = Tree((shadowReach + VegetationBuckets.NearReach) * .5f);
        AssertThat(buckets.Update(Eye(0), false, shadowReach, 0, false) && buckets.Level(0) == VegetationBuckets.NotNear &&
            buckets.InShadow(0)).OverrideFailureMessage("a shadow-only cell must not acquire visible near membership").IsTrue();
        var balanced = ShadowSettings.From(0, ShadowSettings.MinimumDistance);
        AssertThat(buckets.Update(Eye(0), false, balanced.MaxDistance * balanced.Split1, 0, false) && !buckets.InShadow(0)).IsTrue();
        AssertThat(buckets.MembershipChanged(VegPool.Shadow) && !buckets.MembershipChanged(VegPool.L1)).IsTrue();
    }
}
