using System.Numerics;
using System.Text.Json;
using Campus.Camera;
using Campus.Contracts;
using GdUnit4;

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
            Check(move.X > 10 && Math.Abs(move.Y) < .00001f && Math.Abs(move.Z) < .00001f,
                "Forward flight must follow compass heading without pitch changing altitude.");
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
        Check(Vector3.Distance(Simulate(30), Simulate(144)) < .0001f, "Flight depends on the render frame rate.");
    }

    [TestCase]
    public void SpeedStepsAndGroundLimitKeepCloseInspectionSlow()
    {
        var tuning = new FlightTuning();
        Check(Math.Abs(tuning.SpeedAt(1) - .5f) < .0001f, "Lowest speed changed.");
        Check(Math.Abs(tuning.SpeedAt(16) - 200f) < .001f, "Highest speed changed.");
        var motion = new FlightMotion(tuning);
        motion.Step(new(new(0, 0, 0), 0, 0), new(0, 0, 1), true, 16, 0, 1);
        Check(motion.Velocity.Length() <= 12.001f, "Sprint near the ground exceeded the height-based limit.");
    }

    [TestCase]
    public void NestedLocksReleaseOnlyTheirOwnLease()
    {
        var locks = new InputLocks();
        var menu = locks.Acquire(InputLockReason.Menu);
        var secondMenu = locks.Acquire(InputLockReason.Menu);
        var atlas = locks.Acquire(InputLockReason.Atlas);
        menu.Dispose(); menu.Dispose();
        Check(locks.Locked && locks.Count(InputLockReason.Menu) == 1, "Double disposal released someone else's lock.");
        secondMenu.Dispose();
        Check(locks.Locked, "Closing a menu released the atlas lock.");
        atlas.Dispose();
        Check(!locks.Locked, "All input leases were released but input remains locked.");
    }

    [TestCase]
    public void DoubleForwardSprintEndsOnReleaseAndSpaceRequiresShortTap()
    {
        var input = new ObserverInputState(new());
        input.Press("move_forward"); input.Advance(.1f); input.Release("move_forward");
        input.Press("move_forward");
        Check(input.Sprint, "Double forward did not sprint.");
        input.Release("move_forward");
        Check(!input.Sprint, "Forward release did not end sprint.");
        input.Press("move_up"); input.Advance(.25f); input.Release("move_up");
        Check(!input.Press("move_up"), "Long ascent press accidentally switched mode.");
        input.Clear();
        input.Press("move_up"); input.Advance(.1f); input.Release("move_up");
        Check(input.Press("move_up"), "Short double-space did not switch mode.");
    }

    [TestCase]
    public void InputSettingsRejectEngineObjectSyntaxBeforeParsing()
    {
        foreach (string token in new[] { "Object(", "Resource(", "SubResource(", "ExtResource(" })
            Check(!InputBindings.IsPrimitiveText("[physical]\nmove_forward=" + token + "InputEventKey)"), "Unsafe object token accepted.");
        Check(InputBindings.IsPrimitiveText("[physical]\nmove_forward=87\n"), "Primitive physical key was rejected.");
    }

    [TestCase]
    public void PolygonDistanceDistinguishesCampusAndInteriorHoles()
    {
        var ring = new PolygonRings([new(-10, -10), new(10, -10), new(10, 10), new(-10, 10)],
            [[new(-2, -2), new(2, -2), new(2, 2), new(-2, 2)]]);
        Check(FlightBounds.RingDistance(new(5, 0), ring, out _) < 0, "Campus interior was classified outside.");
        Check(FlightBounds.RingDistance(new(15, 0), ring, out var normal) == 5 && normal.X > .99f,
            "Outside distance/normal is incorrect.");
        Check(FlightBounds.RingDistance(Vector2.Zero, ring, out _) > 0, "An excluded boundary hole was ignored.");
    }

    [TestCase]
    public void ViewSnapshotRoundTripsAndRejectsMissingOrUnknownFields()
    {
        var data = new ViewSaveData(ViewMode.Fly, new(new(1, 2, 3), 90, -30), null, 9, 9);
        string json = JsonSerializer.Serialize(data, ViewJsonContext.Contract.ViewSaveData);
        var restored = JsonSerializer.Deserialize(json, ViewJsonContext.Contract.ViewSaveData);
        Check(restored == data, "View snapshot changed during serialization.");
        foreach (string invalid in new[] { json.Replace("\"speedStep\":9,", "", StringComparison.Ordinal), json[..^1] + ",\"unknown\":1}" })
        {
            try { JsonSerializer.Deserialize(invalid, ViewJsonContext.Contract.ViewSaveData); }
            catch (JsonException) { continue; }
            throw new InvalidOperationException("View v1 accepted a missing or unknown property.");
        }
    }

    private static void Check(bool value, string error) { if (!value) throw new InvalidOperationException(error); }
}
