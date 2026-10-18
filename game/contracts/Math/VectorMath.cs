using System.Numerics;

namespace Campus.Contracts;

/// <summary>Pure vector predicates and directions shared by generation and runtime.</summary>
public static class VectorMath
{
    public static bool Finite(Vector2 v) => float.IsFinite(v.X) && float.IsFinite(v.Y);
    public static bool Finite(Vector3 v) => float.IsFinite(v.X) && float.IsFinite(v.Y) && float.IsFinite(v.Z);
    public static bool Finite(Vector4 v) => float.IsFinite(v.X) && float.IsFinite(v.Y) && float.IsFinite(v.Z) && float.IsFinite(v.W);

    public static double Dot(Vector2 a, Vector2 b) => (double)a.X * b.X + (double)a.Y * b.Y;
    public static double Dot(Vector3 a, Vector3 b) => (double)a.X * b.X + (double)a.Y * b.Y + (double)a.Z * b.Z;
    public static double Cross(Vector2 a, Vector2 b) => (double)a.X * b.Y - (double)a.Y * b.X;
    public static double Cross(Vector2 origin, Vector2 a, Vector2 b) =>
        ((double)a.X - origin.X) * ((double)b.Y - origin.Y) - ((double)a.Y - origin.Y) * ((double)b.X - origin.X);
    public static Vector3 Cross(Vector3 a, Vector3 b) => Vector3.Cross(a, b);

    /// <summary>Normalize a finite direction; reject lengths at or below the caller's geometric tolerance.</summary>
    public static Vector2 Unit(Vector2 v, double minimumLength = 1e-12)
    {
        double length = Math.Sqrt(Dot(v, v));
        if (!Finite(v) || !(length > minimumLength)) throw new ArgumentException("A direction must be finite and nonzero.", nameof(v));
        return new((float)(v.X / length), (float)(v.Y / length));
    }

    public static Vector3 Unit(Vector3 v, double minimumLength = 1e-12)
    {
        double length = Math.Sqrt(Dot(v, v));
        if (!Finite(v) || !(length > minimumLength)) throw new ArgumentException("A direction must be finite and nonzero.", nameof(v));
        return new((float)(v.X / length), (float)(v.Y / length), (float)(v.Z / length));
    }

    /// <summary>Explicit fallback for existing mesh/sampling algorithms whose degenerate directions are allowed.</summary>
    public static Vector2 Unit(Vector2 v, Vector2 fallback, double minimumLength = 1e-12)
    {
        double length = Math.Sqrt(Dot(v, v));
        return Finite(v) && length > minimumLength ? new((float)(v.X / length), (float)(v.Y / length)) : fallback;
    }

    public static Vector3 Unit(Vector3 v, Vector3 fallback, double minimumLength = 1e-12)
    {
        double length = Math.Sqrt(Dot(v, v));
        return Finite(v) && length > minimumLength ? new((float)(v.X / length), (float)(v.Y / length), (float)(v.Z / length)) : fallback;
    }
}
