using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Water;
using Campus.Systems.Water;
using GdUnit4;

namespace Campus.Tests;

[TestSuite]
public sealed class ReflectionLakesTests
{
    private static WaterReflectionSurface Lake(string id, int slot, float x0, float z0, float x1, float z1)
    {
        var rings = new PolygonRings([new(x0, z0), new(x1, z0), new(x1, z1), new(x0, z1)]);
        return new(new FeatureId(id), rings, -0.3f, slot, new(x0, z0), new(x1, z1), (x1 - x0) * (z1 - z0));
    }

    private static ReflectionLakes TwoLakes() =>
        ReflectionLakes.FromBodies([Lake("west", 0, -300f, -100f, -100f, 100f), Lake("east", 1, 100f, -100f, 300f, 100f)], 48);

    [TestCase]
    [TestCategory(TestReason.Contract)]
    [RequireGodotRuntime]
    public void LargestLakeOnScreenWinsAndSwitchesOnlyPastHysteresis()
    {
        var lakes = TwoLakes();
        var applied = lakes.Select(new(-200f, 10f, 0f), [], -1);
        if (applied != 0) throw new InvalidOperationException("camera over the west lake must pick it");
        // Halfway between the lakes both are about the same size: keep the current one.
        if (lakes.Select(new(5f, 10f, 0f), [], applied) != 0) throw new InvalidOperationException("switched without hysteresis");
        if (lakes.Select(new(200f, 10f, 0f), [], applied) != 1) throw new InvalidOperationException("must switch to the east lake");
        // A destination query must not change the real camera's previous lake.
        if (lakes.Select(new(5f, 10f, 0f), [], applied) != 0) throw new InvalidOperationException("destination query changed applied selection");
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    [RequireGodotRuntime]
    public void HiddenLakesDoNotCount()
    {
        var lakes = TwoLakes();
        Plane[] eastOnly = [new(-Vector3.UnitX, 0f)];
        if (lakes.Select(new(-200f, 10f, 0f), eastOnly, -1) != 1) throw new InvalidOperationException("a lake out of view must not win");
        if (Math.Abs(lakes.WaterY(1) + 0.3f) > TestTolerance.Position) throw new InvalidOperationException("water level lost");
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void ReflectedPlanesPreserveTheSourceVisibleHalfSpace()
    {
        const float waterY = -0.3f;
        var plane = new Plane(Vector3.Normalize(new(1f, 2f, -1f)), -8f);
        var reflected = ReflectionLakes.ReflectPlane(plane, waterY);
        var point = new Vector3(4f, 12f, -3f);
        var mirror = ReflectionLakes.ReflectPoint(point, waterY);
        if (MathF.Abs(Plane.DotCoordinate(plane, point) - Plane.DotCoordinate(reflected, mirror)) > TestTolerance.Position)
            throw new InvalidOperationException("reflection changed the signed frustum distance");
        if (Vector3.Distance(ReflectionLakes.ReflectPoint(mirror, waterY), point) > TestTolerance.Position)
            throw new InvalidOperationException("reflecting twice must recover the original eye");
    }
}
