using Campus.Contracts;
using System.Numerics;

namespace Campus.Gen.Parts.Core;

/// <summary>Rigid row-vector transforms for part placement.</summary>
public static class Xf
{
    public static readonly Matrix4x4 Identity = Matrix4x4.Identity;
    /// <summary>Local +X becomes world +Y (pieces swept along X stand up).</summary>
    public static readonly Matrix4x4 XUp = new(0, 1, 0, 0, -1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1);
    /// <summary>Local +X becomes world −Y, local +Z stays +Z (a path drawn from the top down).</summary>
    public static readonly Matrix4x4 XDown = new(0, -1, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1);
    /// <summary>Quarter turn about Y: local +X becomes world −Z, local +Z becomes world +X.</summary>
    public static readonly Matrix4x4 YQuarter = new(0, 0, -1, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 1);

    public static Matrix4x4 Move(float x, float y, float z) => new(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, x, y, z, 1);
    public static Matrix4x4 Move(Vector3 v) => Move(v.X, v.Y, v.Z);
    public static Matrix4x4 RotX(float degrees) => Matrix4x4.CreateRotationX(degrees * Angles.DegreesToRadians);
    public static Matrix4x4 RotY(float degrees) => Matrix4x4.CreateRotationY(degrees * Angles.DegreesToRadians);

    /// <summary>Apply a first, then b.</summary>
    public static Matrix4x4 Then(this Matrix4x4 a, Matrix4x4 b) => new(
        a.M11 * b.M11 + a.M12 * b.M21 + a.M13 * b.M31, a.M11 * b.M12 + a.M12 * b.M22 + a.M13 * b.M32, a.M11 * b.M13 + a.M12 * b.M23 + a.M13 * b.M33, 0,
        a.M21 * b.M11 + a.M22 * b.M21 + a.M23 * b.M31, a.M21 * b.M12 + a.M22 * b.M22 + a.M23 * b.M32, a.M21 * b.M13 + a.M22 * b.M23 + a.M23 * b.M33, 0,
        a.M31 * b.M11 + a.M32 * b.M21 + a.M33 * b.M31, a.M31 * b.M12 + a.M32 * b.M22 + a.M33 * b.M32, a.M31 * b.M13 + a.M32 * b.M23 + a.M33 * b.M33, 0,
        a.M41 * b.M11 + a.M42 * b.M21 + a.M43 * b.M31 + b.M41, a.M41 * b.M12 + a.M42 * b.M22 + a.M43 * b.M32 + b.M42, a.M41 * b.M13 + a.M42 * b.M23 + a.M43 * b.M33 + b.M43, 1);

    public static Vector3 Point(Matrix4x4 m, Vector3 p) => new(
        p.X * m.M11 + p.Y * m.M21 + p.Z * m.M31 + m.M41,
        p.X * m.M12 + p.Y * m.M22 + p.Z * m.M32 + m.M42,
        p.X * m.M13 + p.Y * m.M23 + p.Z * m.M33 + m.M43);

    public static Vector3 Direction(Matrix4x4 m, Vector3 n)
    {
        var x = n.X * m.M11 + n.Y * m.M21 + n.Z * m.M31;
        var y = n.X * m.M12 + n.Y * m.M22 + n.Z * m.M32;
        var z = n.X * m.M13 + n.Y * m.M23 + n.Z * m.M33;
        return VectorMath.Unit(new Vector3(x, y, z), n);
    }

    /// <summary>Right-handed frame whose local +X follows <paramref name="axis"/>; lays swept pipes along any direction.</summary>
    public static Matrix4x4 AlongX(Vector3 axis, Vector3 origin)
    {
        var x = VectorMath.Unit(axis);
        var hint = Math.Abs(x.Y) < 0.9f ? Vector3.UnitY : Vector3.UnitX;
        var dot = x.X * hint.X + x.Y * hint.Y + x.Z * hint.Z;
        var y = VectorMath.Unit(new Vector3(hint.X - x.X * dot, hint.Y - x.Y * dot, hint.Z - x.Z * dot));
        var z = VectorMath.Cross(x, y);
        return new(x.X, x.Y, x.Z, 0, y.X, y.Y, y.Z, 0, z.X, z.Y, z.Z, 0, origin.X, origin.Y, origin.Z, 1);
    }
}
