using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.Parts.Core;

internal static class GeometryMath
{
    internal static void Positive(float value, string name)
    {
        if (!float.IsFinite(value) || value <= 0) throw new ArgumentOutOfRangeException(name, "A dimension must be finite and positive.");
    }
    internal static Vector3 Point(Vector2 p, Vector3 origin, Vector3 u, Vector3 v, Vector3 axis, float h) =>
        new(origin.X + u.X * p.X + v.X * p.Y + axis.X * h,
            origin.Y + u.Y * p.X + v.Y * p.Y + axis.Y * h,
            origin.Z + u.Z * p.X + v.Z * p.Y + axis.Z * h);
}
