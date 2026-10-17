using System.Numerics;
using System.Text;
using System.Text.Json;
using Campus.Contracts;
using Campus.Kernel.Renderer;
using Campus.Kernel.Stubs;
using Campus.Kernel.Stubs.Foundation;
using GdUnit4;

namespace Campus.Tests;

[TestSuite]
public sealed class D0IntegrationTests
{
    [TestCase]
    public void NestedInputLocksDoNotUnlockEachOther()
    {
        var world = new WorldStateStub(new WeatherProcessStub(), new TestRuleset());
        var camera = new CameraRigStub(world);
        var first = camera.LockInput(InputLockReason.Menu);
        var second = camera.LockInput(InputLockReason.Menu);
        var map = camera.LockInput(InputLockReason.Atlas);
        first.Dispose(); first.Dispose();
        if (camera.LockCount(InputLockReason.Menu) != 1 || !camera.InputLocked)
            throw new InvalidOperationException("Nested lock was released prematurely.");
        second.Dispose();
        if (!camera.InputLocked) throw new InvalidOperationException("Independent map lock was lost.");
        map.Dispose();
        if (camera.InputLocked) throw new InvalidOperationException("Completed input leases stayed locked.");
    }

    [TestCase]
    public void ZeroInputCaptureStepUsesPlayerBoundsWithoutChangingView()
    {
        var camera = new CameraRigStub(new WorldStateStub(new WeatherProcessStub(), new TestRuleset()));
        camera.ConfigureBounds(new(new(-10, -5, -20), new(10, 30, 20)));
        var requested = new Pose(new(50, 90, -40), 37, -68, 75);
        camera.SetPose(requested);
        if (camera.Pose != requested) throw new InvalidOperationException("SetPose no longer preserves direct placement.");
        camera.IntegrateInput(Vector3.Zero, Vector2.Zero, 0);
        if (camera.Pose != requested with { Position = new(10, 30, -20) })
            throw new InvalidOperationException("Zero-input capture skipped player bounds or changed the requested view.");
    }

    [TestCase]
    public void GodotAndNumericsTransformsKeepOrientationAndTranslation()
    {
        var matrix = Matrix4x4.CreateScale(2, 3, 4) * DetMath.RotationY(.6f) * Matrix4x4.CreateTranslation(14, 6, -2);
        if (matrix.ToGodot().ToNumerics() != matrix)
            throw new InvalidOperationException("Row/column conversion lost matrix values.");
    }

    [TestCase]
    public void CampusIntegerLevelsAcceptDecimalAndExponentEncoding()
    {
        using var stream = typeof(D0IntegrationTests).Assembly.GetManifestResourceStream("Campus.Tests.campus-levels-integer.json")!;
        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        var data = D0CampusLoader.FromBytes(Encoding.UTF8.GetBytes(json));
        if (data.Features.Single().Value.Storeys != 2) throw new InvalidOperationException("Integral level encoding was lost.");
        foreach (var invalid in new[] { "2.5", "2147483648.0", "1e999" })
        {
            try { D0CampusLoader.FromBytes(Encoding.UTF8.GetBytes(json.Replace("2e0", invalid, StringComparison.Ordinal))); }
            catch (JsonException) { continue; }
            throw new InvalidOperationException("Invalid level was accepted: " + invalid);
        }
    }

    private sealed class TestRuleset : IRuleset
    {
        public RulesetDescriptor Current { get; } = new(new("test"), [ViewMode.Fly, ViewMode.Walk],
            WorldControlMask.All, "all-unlocked", [], []);
        public long Rev => 1;
    }
}
