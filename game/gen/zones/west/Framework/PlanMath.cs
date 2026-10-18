using System.Numerics;
using Campus.Contracts;
using Campus.Gen.World;

namespace Campus.Gen.Zones.West.Framework;

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

    /// <summary>Area centroid of a simple ring (vertex mean for a degenerate one).</summary>
    public static Vector2 Centroid(IReadOnlyList<Vector2> ring)
    {
        ArgumentNullException.ThrowIfNull(ring);
        float a = 0, cx = 0, cz = 0;
        for (var i = 0; i < ring.Count; i++)
        {
            Vector2 p = ring[i], q = ring[(i + 1) % ring.Count];
            var cross = p.X * q.Y - q.X * p.Y;
            a += cross;
            cx += (p.X + q.X) * cross;
            cz += (p.Y + q.Y) * cross;
        }
        if (MathF.Abs(a) < 1e-6f)
        {
            float sx = 0, sz = 0;
            foreach (var p in ring) { sx += p.X; sz += p.Y; }
            return new(sx / ring.Count, sz / ring.Count);
        }
        return new(cx / (3f * a), cz / (3f * a));
    }

    /// <summary>Plan unit vector of a bearing in degrees (0 north = −z, 90 east = +x).</summary>
    public static Vector2 Heading(float bearing)
    {
        var a = bearing * (MathF.PI / 180f);
        return new(MathF.Sin(a), -MathF.Cos(a));
    }

    /// <summary>A frame at <paramref name="origin"/> whose local +Z faces <paramref name="bearing"/> (local +X is then its left).</summary>
    public static PlanFrame Facing(Vector2 origin, float bearing)
    {
        var w = Heading(bearing);
        return new(origin, new(w.Y, -w.X));
    }
}
