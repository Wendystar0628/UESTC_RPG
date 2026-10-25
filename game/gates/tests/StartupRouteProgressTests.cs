using System.Numerics;
using Campus.Contracts;
using Campus.Gates;
using Campus.Contracts.Perf;
using Campus.Contracts.Startup;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Campus.Tests;

[TestSuite]
public sealed class StartupRouteProgressTests
{
    [TestCase]
    [TestCategory(TestReason.Smoke)]
    public void CollectionWaitsForRealTimeAndResetsWhenStreamingReturns()
    {
        var options = CollectionRequest();
        var conditions = new PerfCollectionConditions(options);
        var pipelines = new FramePipelineCounts(0, 1, 2, 3, 4);
        AssertThat(conditions.Stable(true, true, pipelines, 0)).IsFalse();
        AssertThat(conditions.Stable(true, true, pipelines, 100)).IsFalse();
        AssertThat(conditions.Stable(true, true, pipelines, 200)).IsFalse();
        AssertThat(conditions.Stable(true, true, pipelines, options.RealtimeSeconds * 1000)).IsTrue();
        AssertThat(conditions.Stable(true, false, pipelines, 3000)).IsFalse();
        AssertThat(conditions.Stable(true, true, pipelines, 4000)).IsFalse();
        AssertThat(conditions.Stable(true, true, pipelines, 4100)).IsFalse();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void MissingIdentityStaleFactsAndExternalLoadCannotBecomeCleanSamples()
    {
        var options = CollectionRequest();
        var conditions = new PerfCollectionConditions(options);
        var p0 = new PerfSamplingFact { Capability = PerfFactCapability.Supported, Value = 0, QueryEndMs = 1000 };
        var facts = new PerfSamplingFacts("session", 1, "render-device", p0, default, p0);
        AssertThat(conditions.Missing(facts, 1000) is null && conditions.PerformanceReady(facts)).IsTrue();
        AssertThat(conditions.Missing(facts with { RenderGpuUuid = null }, 1000) is not null).IsTrue();
        AssertThat(conditions.Missing(facts, 1000 + options.MaximumFactAgeSeconds * 1000 + 1) is not null).IsTrue();
        AssertThat(conditions.RejectPollution(facts with { OtherProcessesGpuUtilFraction = p0 with { Value = .7 } }, out _)).IsTrue();
        var unavailable = p0 with { Capability = PerfFactCapability.Unavailable, Value = null, Reason = "counter unavailable" };
        AssertThat(conditions.Missing(facts with { OtherProcessesGpuUtilFraction = unavailable }, 1000) is not null).IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Regression)]
    public void FunctionalPollutionRetainsItsReasonWithoutRejectingOrHidingMissingFacts()
    {
        var options = CollectionRequest();
        var formal = new PerfCollectionConditions(options);
        var functional = new PerfCollectionConditions(options with { FunctionalSmoke = true });
        var p0 = new PerfSamplingFact { Capability = PerfFactCapability.Supported, Value = 0, QueryEndMs = 1000 };
        var clean = new PerfSamplingFacts("session", 1, "render-device", p0, default, p0);
        foreach (var facts in new[]
        {
            clean with { OtherProcessesGpuUtilFraction = p0 with { Value = options.OtherGpuFraction + .1 } }
        })
        {
            AssertThat(formal.RejectPollution(facts, out var formalReason)).IsTrue();
            AssertThat(formalReason is not null).IsTrue();
            AssertThat(functional.RejectPollution(facts, out var functionalReason)).IsFalse();
            AssertThat(functionalReason == formalReason).IsTrue();
        }
        AssertThat(functional.RejectPollution(clean, out var cleanReason)).IsFalse();
        AssertThat(cleanReason is null).IsTrue();
        var missing = clean with { OtherProcessesGpuUtilFraction = p0 with { Capability = PerfFactCapability.Unavailable, Value = null } };
        AssertThat(functional.Missing(missing, 1000) is not null).IsTrue();
    }

    private static PerfCollectionOptions CollectionRequest() => new()
    {
        SceneId = "condition-fixture", SampleSegmentId = "sample-fixture", FunctionalSmoke = false, StableFrames = 2,
        RealtimeSeconds = 2, SampleSeconds = 1, OtherGpuFraction = .2, MaximumFactAgeSeconds = 3
    };

    [TestCase]
    [TestCategory(TestReason.Smoke)]
    public void BothModesCompleteOnlyAfterObservedForwardTravel()
    {
        var entry = RealMapFixture.Entry();
        var stand = entry.Waypoints.Single(point => point.Id == entry.Descriptor.DefaultSpawn).Stand!;
        foreach (var mode in new[] { ViewMode.Walk, ViewMode.Fly })
        {
            var start = new Pose(mode == ViewMode.Walk ? stand.Feet : stand.Feet + Vector3.UnitY * WalkRules.StandingEye,
                stand.YawDeg, 0);
            var forward = Angles.Forward(start.YawDeg, 0);
            var sample = new StartupRouteProgress(start, mode, 2000, start.Position, start.YawDeg, mode);
            sample.Observe(start.Position + forward * WalkRules.WalkSpeed, mode, 1000);
            sample.Observe(start.Position + forward * WalkRules.WalkSpeed * 2, mode, 2000);
            sample.Complete();
            AssertThat(MathF.Abs(sample.Distance - WalkRules.WalkSpeed * 2) < TestTolerance.Position &&
                MathF.Abs(sample.ForwardDistance - sample.Distance) < TestTolerance.Position).IsTrue();
        }
    }

    [TestCase]
    [TestCategory(TestReason.Regression)]
    public void RequestedPointCannotStandInForTheActualSpawn()
    {
        var entry = RealMapFixture.Entry();
        var stands = entry.Waypoints.Where(point => point.Stand is not null).Select(point => point.Stand!).ToArray();
        var requested = stands[0];
        var actual = stands.First(stand => Vector3.Distance(stand.Feet, requested.Feet) > StartupRouteProgress.PositionTolerance);
        AssertThat(Vector3.Distance(actual.Feet, requested.Feet) > StartupRouteProgress.PositionTolerance).IsTrue();
        RequireInvalid(() => new StartupRouteProgress(new(requested.Feet, requested.YawDeg, 0),
            ViewMode.Walk, 1000, actual.Feet, actual.YawDeg, ViewMode.Walk));
    }

    [TestCase]
    [TestCategory(TestReason.Smoke)]
    public void PartialStoppedAndWrongModeRoutesFail()
    {
        var stand = RealMapFixture.Entry().Waypoints.First(point => point.Stand is not null).Stand!;
        var pose = new Pose(stand.Feet, stand.YawDeg, 0);
        var sample = new StartupRouteProgress(pose, ViewMode.Walk, 2000, pose.Position, pose.YawDeg, ViewMode.Walk);
        var next = pose.Position + Angles.Forward(pose.YawDeg, 0) * WalkRules.WalkSpeed;
        sample.Observe(next, ViewMode.Walk, 1000);
        RequireInvalid(sample.Complete);
        RequireInvalid(() => sample.Observe(next, ViewMode.Walk, 2000));
        RequireInvalid(() => sample.RequireMode(ViewMode.Fly));
    }

    private static void RequireInvalid(Action operation)
    {
        var rejected = false;
        try { operation(); }
        catch (InvalidOperationException) { rejected = true; }
        AssertThat(rejected).IsTrue();
    }
}
