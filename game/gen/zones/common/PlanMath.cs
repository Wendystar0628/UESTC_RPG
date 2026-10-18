using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.Zones.Common;

/// <summary>Small plan-geometry helpers on world (x, z) rings, in scalar arithmetic.</summary>
public static class PlanMath
{
    /// <summary>The ring walked so that its outside lies on the right of the path (lateral positive in a sweep); positive-area rings already are.</summary>
    public static Vector2[] OutsideRight(IReadOnlyList<Vector2> ring)
    {
        var points = ring.ToArray();
        if (PlanGeometry.SignedArea(points) < 0) Array.Reverse(points);
        return points;
    }

    /// <summary>A frame at <paramref name="origin"/> whose local +Z faces <paramref name="bearing"/> (local +X is then its left).</summary>
    public static PlanFrame Facing(Vector2 origin, float bearing)
    {
        var w = Angles.Heading(bearing);
        return new(origin, new(w.Y, -w.X));
    }

    /// <summary>Rigid matrix that stands a lathe's axis (local Y) along the frame's +Z: X→U, Y→W, Z→down.</summary>
    public static Matrix4x4 AxisOut(PlanFrame f, float x, float y, float z)
    {
        var p = f.P(x, z);
        return new(f.U.X, 0, f.U.Y, 0,
            f.W.X, 0, f.W.Y, 0,
            0, -1, 0, 0,
            p.X, y, p.Y, 1);
    }
}
