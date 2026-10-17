using System.Numerics;
using Campus.Contracts;
using GdUnit4;

namespace Campus.Kernel.Collision.Tests;

public sealed partial class CollisionCoreTests
{
    [TestCase]
    public void SourceExpansionDoesNotCountInwardFacadeThickness()
    {
        var solid = new PolygonRings([new(0, 0), new(4, 0), new(4, 4), new(0, 4)]);
        var wall = new WallCollider(new(0, .3f), new(4, .3f), .3f, 0, 3, SurfaceTag.Wall);
        Require(MaximumOutwardExpansion([], wall, [solid]) == 0,
            "The complete .6m wall thickness lies on the directed facade's material side.");
    }

    [TestCase]
    public void SourceExpansionRejectsRealOutwardDistanceAbovePointThree()
    {
        var solid = new PolygonRings([new(0, 0), new(4, 0), new(4, 4), new(0, 4)]);
        var wall = new WallCollider(new(0, -.151f), new(4, -.151f), .15f, 0, 3, SurfaceTag.Wall);
        Require(MaximumOutwardExpansion([], wall, [solid]) > .3f,
            "A real .301m outward face must still fail the .3m limit.");
    }

    [TestCase]
    public void SourceExpansionRetainsCourtyardAndProjectedHoles()
    {
        Vector2[] outer = [new(0, 0), new(6, 0), new(6, 6), new(0, 6)];
        Vector2[] hole = [new(2, 2), new(2, 4), new(4, 4), new(4, 2)];
        var solid = new PolygonRings(outer, [hole]);
        var courtyardWall = new WallCollider(new(2, 2.3f), new(4, 2.3f), .3f, 0, 3, SurfaceTag.Wall);
        Require(MaximumOutwardExpansion([], courtyardWall, [solid]) > .3f,
            "A courtyard remains empty; inward-facing courtyard expansion cannot be filled by the outer ring.");
        Require(OutwardDistanceSquared(new(3, 1.7f), [], [solid]) == 0,
            "The material side of the courtyard facade has no outward expansion.");

        var island = new PolygonRings([new(2.5f, 2.5f), new(3.5f, 2.5f), new(3.5f, 3.5f), new(2.5f, 3.5f)]);
        Require(OutwardDistanceSquared(new(3, 3), [], [solid, island]) == 0
            && OutwardDistanceSquared(new(2.25f, 3), [], [solid, island]) > 0,
            "Each contour subtracts its own holes before union: another actual component fills only its own island.");

        // Four actual cap strips leave a 2x2 hole; their projection is a union, never a convex hull.
        var cap = Strip(0, 0, 6, 2).Concat(Strip(0, 4, 6, 6))
            .Concat(Strip(0, 2, 2, 4)).Concat(Strip(4, 2, 6, 4)).ToArray();
        Require(OutwardDistanceSquared(new(3, 3), cap, null) == 1,
            "Closed projected material must preserve the cap's horizontal hole.");
    }

    [TestCase]
    public void SourceExpansionUsesOnlyItsOwnGeometry()
    {
        var own = Strip(0, 0, 1, 1);
        var foreign = Strip(0, 0, 8, 8);
        var wall = new WallCollider(new(3, 3), new(4, 3), .1f, 0, 3, SurfaceTag.Wall);
        var check = new CollisionCoreTests();
        check.CheckExpansion("own", own, [wall]);
        Require(check._failureCount == 1 && check._failures.Single().Contains("source=own", StringComparison.Ordinal),
            "The source must fail without importing another source's geometry or tile ground.");
        Require(MaximumOutwardExpansion(foreign, wall) == 0,
            "The unrelated surface would hide this failure if the source pairing were lost.");
    }

    private static Triangle[] Strip(float x0, float z0, float x1, float z1) =>
        [new(new(x0, 0, z0), new(x1, 0, z0), new(x1, 0, z1)),
            new(new(x0, 0, z0), new(x1, 0, z1), new(x0, 0, z1))];
}
