using System.Numerics;

namespace Campus.Gen.Zones.South.Framework;

/// <summary>Small plan-geometry helpers on world (x, z) rings, in scalar arithmetic.</summary>
public static class PlanMath
{
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

    /// <summary>Point in ring by crossings (boundary points may go either way).</summary>
    public static bool Inside(IReadOnlyList<Vector2> ring, Vector2 p)
    {
        var inside = false;
        for (int i = 0, j = ring.Count - 1; i < ring.Count; j = i++)
        {
            Vector2 a = ring[i], b = ring[j];
            if ((a.Y > p.Y) != (b.Y > p.Y) && p.X < (b.X - a.X) * (p.Y - a.Y) / (b.Y - a.Y) + a.X) inside = !inside;
        }
        return inside;
    }

    /// <summary>An angle in degrees wrapped to (−180, 180].</summary>
    public static float Turn(float degrees)
    {
        var d = degrees % 360f;
        if (d > 180f) d -= 360f;
        if (d <= -180f) d += 360f;
        return d;
    }

    /// <summary>Plan unit vector of a bearing in degrees (0 north = −z, 90 east = +x).</summary>
    public static Vector2 Heading(float bearing)
    {
        var a = bearing * (MathF.PI / 180f);
        return new(MathF.Sin(a), -MathF.Cos(a));
    }

    /// <summary>A frame at <paramref name="origin"/> whose local +Z faces <paramref name="bearing"/>.</summary>
    public static PlanFrame Facing(Vector2 origin, float bearing)
    {
        var w = Heading(bearing);
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
