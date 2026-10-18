using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Tests;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Campus.Gen.World.Tests;

[TestSuite]
public sealed class WorldWalkRegionsTests
{
    [TestCase]
    public void RegisteredRestrictionsOverrideBridgesAndBuildingsDoNotBlockTheirProjection()
    {
        var regions = new WorldWalkRegions([
            WorldFeatureFixture.Create("water", "water", new PolygonGeometry(Box(-20, -20, 20, 20))),
            WorldFeatureFixture.Create("building", "building", new PolygonGeometry(Box(-8, -8, -4, -4))),
            WorldFeatureFixture.Create("wade", "wade", new PolygonGeometry(Box(-2, -2, 2, 2))),
            WorldFeatureFixture.Create("view", "viewOnly", new PolygonGeometry(Box(4, -2, 8, 2))),
            WorldFeatureFixture.Create("seal", "seal", new PolygonGeometry(Box(12, -2, 16, 2)))
        ], Box(-32, -32, 32, 32), WorldNetwork.Empty with
        { Bridges = [new(new("bridge"), new([new(-24, 0), new(24, 0)]), 6)] });
        AssertThat(regions.At(0, 0) == WalkClass.Wade).OverrideFailureMessage("Registered shallow water was lost.").IsTrue();
        AssertThat(regions.At(6, 0) == WalkClass.Forbidden && regions.At(14, 0) == WalkClass.Forbidden).OverrideFailureMessage("A bridge bypassed a registered restriction.").IsTrue();
        AssertThat(regions.At(-12, 0) == WalkClass.Walk && regions.At(-12, 4) == WalkClass.Water).OverrideFailureMessage("Bridge permission ignored its network width.").IsTrue();
        AssertThat(regions.At(28, 0) == WalkClass.Walk && regions.At(33, 0) == WalkClass.Outside).OverrideFailureMessage("Unrestricted ground or the campus boundary changed.").IsTrue();
        var building = new WorldWalkRegions([WorldFeatureFixture.Create("building", "building", new PolygonGeometry(Box(-8, -8, -4, -4)))],
            Box(-32, -32, 32, 32), WorldNetwork.Empty);
        AssertThat(building.At(-6, -6) == WalkClass.Walk).OverrideFailureMessage("A building projection blocked physical openings and floors.").IsTrue();
    }

    [TestCase]
    public void PolygonBridgeKeepsItsHolesAndReplacesItsNamedNetworkLines()
    {
        var deck = new PolygonRings(Box(-20, -8, 20, 8).Outer, [Box(-2, -2, 2, 2).Outer]);
        var bridge = WorldFeatureFixture.Create("deck", "bridge-deck", new PolygonGeometry(deck)) with
        { Props = JsonSerializer.SerializeToElement(new { axis = "bridge-axis", walks = new[] { "bridge-walk" } }) };
        var regions = new WorldWalkRegions([WorldFeatureFixture.Create("water", "water", new PolygonGeometry(Box(-40, -40, 40, 40))), bridge],
            Box(-32, -32, 32, 32), WorldNetwork.Empty with
            {
                Bridges = [new(new("bridge-axis"), new([new(-40, 0), new(40, 0)]), 6),
                    new(new("bridge-walk"), new([new(-40, 6), new(40, 6)]), 3)]
            });
        AssertThat(regions.At(0, 0) == WalkClass.Water && regions.At(10, 0) == WalkClass.Walk).OverrideFailureMessage("A network line filled a declared deck hole.").IsTrue();
        AssertThat(regions.At(28, 6) == WalkClass.Water && regions.At(34, 0) == WalkClass.Outside).OverrideFailureMessage("A replaced line extended the bridge or opened the campus exterior.").IsTrue();
        var channel = new WorldWalkRegions([], Box(-32, -32, 32, 32), WorldNetwork.Empty with
        { Waterways = [new(new("channel"), new([new(-20, 12), new(20, 12)]), 4)] });
        AssertThat(channel.At(0, 12) == WalkClass.Water && channel.At(0, 16) == WalkClass.Walk).OverrideFailureMessage("Waterway classification ignored its source width.").IsTrue();
    }

    private static PolygonRings Box(float x0, float z0, float x1, float z1) =>
        new([new(x0, z0), new(x1, z0), new(x1, z1), new(x0, z1)]);
}
