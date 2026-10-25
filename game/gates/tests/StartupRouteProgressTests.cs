using System.Numerics;
using Campus.Contracts;
using Campus.Gates;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Campus.Tests;

[TestSuite]
public sealed class StartupRouteProgressTests
{
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
