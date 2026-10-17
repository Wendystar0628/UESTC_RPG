using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using GdUnit4;

namespace Campus.Gen.World.Tests;

[TestSuite]
public sealed class WorldWalkRegionsTests
{
    [TestCase]
    public void RegisteredRestrictionsOverrideBridgesAndBuildingsDoNotBlockTheirProjection()
    {
        var regions = new WorldWalkRegions([
            Feature("water", "water", Box(-20, -20, 20, 20)),
            Feature("building", "building", Box(-8, -8, -4, -4)),
            Feature("wade", "wade", Box(-2, -2, 2, 2)),
            Feature("view", "viewOnly", Box(4, -2, 8, 2)),
            Feature("seal", "seal", Box(12, -2, 16, 2))
        ], Box(-32, -32, 32, 32), WorldNetwork.Empty with
        { Bridges = [new(new("bridge"), new([new(-24, 0), new(24, 0)]), 6)] });
        Require(regions.At(0, 0) == WalkClass.Wade, "Registered shallow water was lost.");
        Require(regions.At(6, 0) == WalkClass.Forbidden && regions.At(14, 0) == WalkClass.Forbidden,
            "A bridge bypassed a registered restriction.");
        Require(regions.At(-12, 0) == WalkClass.Walk && regions.At(-12, 4) == WalkClass.Water,
            "Bridge permission ignored its network width.");
        Require(regions.At(28, 0) == WalkClass.Walk && regions.At(33, 0) == WalkClass.Outside,
            "Unrestricted ground or the campus boundary changed.");
        var building = new WorldWalkRegions([Feature("building", "building", Box(-8, -8, -4, -4))],
            Box(-32, -32, 32, 32), WorldNetwork.Empty);
        Require(building.At(-6, -6) == WalkClass.Walk, "A building projection blocked physical openings and floors.");
    }

    [TestCase]
    public void PolygonBridgeKeepsItsHolesAndReplacesItsNamedNetworkLines()
    {
        var deck = new PolygonRings(Box(-20, -8, 20, 8).Outer, [Box(-2, -2, 2, 2).Outer]);
        var bridge = Feature("deck", "bridge-deck", deck) with
        { Props = JsonSerializer.SerializeToElement(new { axis = "bridge-axis", walks = new[] { "bridge-walk" } }) };
        var regions = new WorldWalkRegions([Feature("water", "water", Box(-40, -40, 40, 40)), bridge],
            Box(-32, -32, 32, 32), WorldNetwork.Empty with
            {
                Bridges = [new(new("bridge-axis"), new([new(-40, 0), new(40, 0)]), 6),
                    new(new("bridge-walk"), new([new(-40, 6), new(40, 6)]), 3)]
            });
        Require(regions.At(0, 0) == WalkClass.Water && regions.At(10, 0) == WalkClass.Walk,
            "A network line filled a declared deck hole.");
        Require(regions.At(28, 6) == WalkClass.Water && regions.At(34, 0) == WalkClass.Outside,
            "A replaced line extended the bridge or opened the campus exterior.");
        var channel = new WorldWalkRegions([], Box(-32, -32, 32, 32), WorldNetwork.Empty with
        { Waterways = [new(new("channel"), new([new(-20, 12), new(20, 12)]), 4)] });
        Require(channel.At(0, 12) == WalkClass.Water && channel.At(0, 16) == WalkClass.Walk,
            "Waterway classification ignored its source width.");
    }

    private static WorldFeature Feature(string id, string kind, PolygonRings area) =>
        new(new(id), FeatureSource.Design, new(kind), new PolygonGeometry(area));
    private static PolygonRings Box(float x0, float z0, float x1, float z1) =>
        new([new(x0, z0), new(x1, z0), new(x1, z1), new(x0, z1)]);
    private static void Require(bool value, string message)
    { if (!value) throw new InvalidOperationException(message); }
}
