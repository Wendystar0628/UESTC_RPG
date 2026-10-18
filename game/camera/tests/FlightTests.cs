using System.Numerics;
using System.Text.Json;
using Campus.Camera;
using Campus.Contracts;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Campus.Tests;

[TestSuite]
public sealed class FlightTests
{
    [TestCase]
    public void FlyForwardIsHorizontalAtEveryPitch()
    {
        foreach (float pitch in new[] { -89f, 0f, 89f })
        {
            var motion = new FlightMotion(new());
            var move = motion.Step(new(new(0, 100, 0), 90, pitch), new(0, 0, 1), false, 9, 0, 1);
            AssertThat(move.X > 10 && Math.Abs(move.Y) < .00001f && Math.Abs(move.Z) < .00001f).OverrideFailureMessage("Forward flight must follow compass heading without pitch changing altitude.")
                .IsTrue();
        }
    }

    [TestCase]
    public void AccelerationTravelMatchesAcrossFrameRates()
    {
        Vector3 Simulate(int fps)
        {
            var motion = new FlightMotion(new());
            var pose = new Pose(new(0, 100, 0), 37, -20);
            Vector3 total = default;
            for (int i = 0; i < fps; i++) total += motion.Step(pose, new(0, 0, 1), false, 9, 0, 1f / fps);
            return total;
        }
        AssertThat(Vector3.Distance(Simulate(30), Simulate(144)) < .0001f).OverrideFailureMessage("Flight depends on the render frame rate.")
            .IsTrue();
    }

    [TestCase]
    public void SpeedStepsAndGroundLimitKeepCloseInspectionSlow()
    {
        var tuning = new FlightTuning();
        AssertThat(Math.Abs(tuning.SpeedAt(1) - .5f) < .0001f).OverrideFailureMessage("Lowest speed changed.").IsTrue();
        AssertThat(Math.Abs(tuning.SpeedAt(16) - 200f) < .001f).OverrideFailureMessage("Highest speed changed.").IsTrue();
        var motion = new FlightMotion(tuning);
        motion.Step(new(new(0, 0, 0), 0, 0), new(0, 0, 1), true, 16, 0, 1);
        AssertThat(motion.Velocity.Length() <= 12.001f).OverrideFailureMessage("Sprint near the ground exceeded the height-based limit.")
            .IsTrue();
    }

    [TestCase]
    public void NestedLocksReleaseOnlyTheirOwnLease()
    {
        var locks = new InputLocks();
        var menu = locks.Acquire(InputLockReason.Menu);
        var secondMenu = locks.Acquire(InputLockReason.Menu);
        var atlas = locks.Acquire(InputLockReason.Atlas);
        menu.Dispose(); menu.Dispose();
        AssertThat(locks.Locked && locks.Count(InputLockReason.Menu) == 1).OverrideFailureMessage("Double disposal released someone else's lock.")
            .IsTrue();
        secondMenu.Dispose();
        AssertThat(locks.Locked).OverrideFailureMessage("Closing a menu released the atlas lock.").IsTrue();
        atlas.Dispose();
        AssertThat(!locks.Locked).OverrideFailureMessage("All input leases were released but input remains locked.").IsTrue();
    }

    [TestCase]
    public void DoubleForwardSprintEndsOnReleaseAndSpaceRequiresShortTap()
    {
        var input = new ObserverInputState(new());
        input.Press("move_forward"); input.Advance(.1f); input.Release("move_forward");
        input.Press("move_forward");
        AssertThat(input.Sprint).OverrideFailureMessage("Double forward did not sprint.").IsTrue();
        input.Release("move_forward");
        AssertThat(!input.Sprint).OverrideFailureMessage("Forward release did not end sprint.").IsTrue();
        input.Press("move_up"); input.Advance(.25f); input.Release("move_up");
        AssertThat(!input.Press("move_up")).OverrideFailureMessage("Long ascent press accidentally switched mode.").IsTrue();
        input.Clear();
        input.Press("move_up"); input.Advance(.1f); input.Release("move_up");
        AssertThat(input.Press("move_up")).OverrideFailureMessage("Short double-space did not switch mode.").IsTrue();
    }

    [TestCase]
    public void PolygonDistanceDistinguishesCampusAndInteriorHoles()
    {
        var ring = new PolygonRings([new(-10, -10), new(10, -10), new(10, 10), new(-10, 10)],
            [[new(-2, -2), new(2, -2), new(2, 2), new(-2, 2)]]);
        AssertThat(FlightBounds.RingDistance(new(5, 0), ring, out _) < 0).OverrideFailureMessage("Campus interior was classified outside.")
            .IsTrue();
        AssertThat(FlightBounds.RingDistance(new(15, 0), ring, out var normal) == 5 && normal.X > .99f).OverrideFailureMessage("Outside distance/normal is incorrect.")
            .IsTrue();
        AssertThat(FlightBounds.RingDistance(Vector2.Zero, ring, out _) > 0).OverrideFailureMessage("An excluded boundary hole was ignored.")
            .IsTrue();
    }

    [TestCase]
    public void ViewSnapshotRoundTripsAndRejectsMissingOrUnknownFields()
    {
        var data = new ViewSaveData(ViewMode.Fly, new(new(1, 2, 3), 90, -30), null, 9, 9);
        string json = JsonSerializer.Serialize(data, ViewJsonContext.Contract.ViewSaveData);
        var restored = JsonSerializer.Deserialize(json, ViewJsonContext.Contract.ViewSaveData);
        AssertThat(restored == data).OverrideFailureMessage("View snapshot changed during serialization.").IsTrue();
        foreach (string invalid in new[] { json.Replace("\"speedStep\":9,", "", StringComparison.Ordinal), json[..^1] + ",\"unknown\":1}" })
        {
            try { JsonSerializer.Deserialize(invalid, ViewJsonContext.Contract.ViewSaveData); }
            catch (JsonException) { continue; }
            throw new InvalidOperationException("View v1 accepted a missing or unknown property.");
        }
    }

}
