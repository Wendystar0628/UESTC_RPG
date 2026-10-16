using System.Numerics;

namespace Campus.Gen.Zones.CenterEast.Framework;

/// <summary>Row-vector affine matrix products in scalar arithmetic (no fused or SIMD paths, D-165).</summary>
public static class Affine
{
    /// <summary>a then b: a point transformed by the result equals the point transformed by a, then by b.</summary>
    public static Matrix4x4 Mul(Matrix4x4 a, Matrix4x4 b) => new(
        a.M11 * b.M11 + a.M12 * b.M21 + a.M13 * b.M31, a.M11 * b.M12 + a.M12 * b.M22 + a.M13 * b.M32, a.M11 * b.M13 + a.M12 * b.M23 + a.M13 * b.M33, 0,
        a.M21 * b.M11 + a.M22 * b.M21 + a.M23 * b.M31, a.M21 * b.M12 + a.M22 * b.M22 + a.M23 * b.M32, a.M21 * b.M13 + a.M22 * b.M23 + a.M23 * b.M33, 0,
        a.M31 * b.M11 + a.M32 * b.M21 + a.M33 * b.M31, a.M31 * b.M12 + a.M32 * b.M22 + a.M33 * b.M32, a.M31 * b.M13 + a.M32 * b.M23 + a.M33 * b.M33, 0,
        a.M41 * b.M11 + a.M42 * b.M21 + a.M43 * b.M31 + b.M41, a.M41 * b.M12 + a.M42 * b.M22 + a.M43 * b.M32 + b.M42,
        a.M41 * b.M13 + a.M42 * b.M23 + a.M43 * b.M33 + b.M43, 1);
}
