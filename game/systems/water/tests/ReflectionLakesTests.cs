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
    [RequireGodotRuntime]
    public void LargestLakeOnScreenWinsAndSwitchesOnlyPastHysteresis()
    {
        var lakes = TwoLakes();
        static bool All(Vector3 p) => true;
        if (lakes.Select(new(-200f, 10f, 0f), All) != 0) throw new InvalidOperationException("camera over the west lake must pick it");
        // Halfway between the lakes both are about the same size: keep the current one.
        if (lakes.Select(new(5f, 10f, 0f), All) != 0) throw new InvalidOperationException("switched without hysteresis");
        if (lakes.Select(new(200f, 10f, 0f), All) != 1) throw new InvalidOperationException("must switch to the east lake");
    }

    [TestCase]
    [RequireGodotRuntime]
    public void HiddenLakesDoNotCount()
    {
        var lakes = TwoLakes();
        bool EastOnly(Vector3 p) => p.X > 0f;
        if (lakes.Select(new(-200f, 10f, 0f), EastOnly) != 1) throw new InvalidOperationException("a lake out of view must not win");
        if (Math.Abs(lakes.WaterY(1) + 0.3f) > 1e-6f) throw new InvalidOperationException("water level lost");
    }
}
