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
    /// <summary>Plan vector length with double accumulation and a single float result rounding.</summary>
    public static float Length(Vector2 value) => (float)Math.Sqrt(Dot(value, value));
    public static double Cross(Vector2 a, Vector2 b) => (double)a.X * b.Y - (double)a.Y * b.X;
    public static double Cross(Vector2 origin, Vector2 a, Vector2 b) =>
        ((double)a.X - origin.X) * ((double)b.Y - origin.Y) - ((double)a.Y - origin.Y) * ((double)b.X - origin.X);
    /// <summary>Scalar float multiply/subtract order used by mesh winding and degeneracy predicates.</summary>
    public static Vector3 Cross(Vector3 a, Vector3 b) =>
        new(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X);

    /// <summary>Signed triangle orientation along outward. Promote coordinates before subtraction and retain double precision through the scalar triple product.</summary>
    public static double TriangleOrientation(Vector3 a, Vector3 b, Vector3 c, Vector3 outward)
    {
        double abX = (double)b.X - a.X, abY = (double)b.Y - a.Y, abZ = (double)b.Z - a.Z;
        double acX = (double)c.X - a.X, acY = (double)c.Y - a.Y, acZ = (double)c.Z - a.Z;
        return (abY * acZ - abZ * acY) * outward.X
            + (abZ * acX - abX * acZ) * outward.Y
            + (abX * acY - abY * acX) * outward.Z;
    }

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

}
