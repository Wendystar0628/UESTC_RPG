namespace Campus.Contracts;

/// <summary>Shared scalar arithmetic. Explicit polynomial order preserves existing procedural output.</summary>
public static class ScalarMath
{
    /// <summary>Unclamped interpolation; extrapolation is intentional.</summary>
    public static float Lerp(float a, float b, float t) => a + (b - a) * t;
    public static double Lerp(double a, double b, double t) => a + (b - a) * t;

    /// <summary>Cubic smoothstep on [0,1], clamped outside it.</summary>
    public static float Smoothstep(float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return t * t * (3f - 2f * t);
    }

    public static double Smoothstep(double t)
    {
        t = Math.Clamp(t, 0d, 1d);
        return t * t * (3d - 2d * t);
    }

    /// <summary>Edges may descend. Equal edges produce a hard step, including the upper side at the edge.</summary>
    public static float Smoothstep(float edge0, float edge1, float value) =>
        edge0 == edge1 ? (value < edge0 ? 0f : 1f) : Smoothstep((value - edge0) / (edge1 - edge0));
    public static double Smoothstep(double edge0, double edge1, double value) =>
        edge0 == edge1 ? (value < edge0 ? 0d : 1d) : Smoothstep((value - edge0) / (edge1 - edge0));

    /// <summary>Quintic smootherstep, used by lattice noise and movement easing; distinct from cubic smoothstep.</summary>
    public static float Smootherstep(float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return t * t * t * (t * (t * 6f - 15f) + 10f);
    }

    public static double Smootherstep(double t)
    {
        t = Math.Clamp(t, 0d, 1d);
        return t * t * t * (t * (t * 6d - 15d) + 10d);
    }
}
