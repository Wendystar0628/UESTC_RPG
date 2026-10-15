using System.Numerics;

namespace Campus.Contracts;

public static partial class DetMath
{
    /// <summary>Right-handed X rotation, using System.Numerics row-vector layout.</summary>
    /// <remarks>Radians must be finite and within [-1048576, 1048576].</remarks>
    public static Matrix4x4 RotationX(float radians)
    {
        ValidateAngle(radians, nameof(radians));
        (double sin, double cos) = SinCosCore(radians);
        float s = (float)sin;
        float c = (float)cos;
        return new Matrix4x4(
            1f, 0f, 0f, 0f,
            0f, c, s, 0f,
            0f, -s, c, 0f,
            0f, 0f, 0f, 1f);
    }

    /// <summary>Right-handed Y rotation, using System.Numerics row-vector layout.</summary>
    /// <remarks>Radians must be finite and within [-1048576, 1048576].</remarks>
    public static Matrix4x4 RotationY(float radians)
    {
        ValidateAngle(radians, nameof(radians));
        (double sin, double cos) = SinCosCore(radians);
        float s = (float)sin;
        float c = (float)cos;
        return new Matrix4x4(
            c, 0f, -s, 0f,
            0f, 1f, 0f, 0f,
            s, 0f, c, 0f,
            0f, 0f, 0f, 1f);
    }

    /// <summary>Right-handed Z rotation, using System.Numerics row-vector layout.</summary>
    /// <remarks>Radians must be finite and within [-1048576, 1048576].</remarks>
    public static Matrix4x4 RotationZ(float radians)
    {
        ValidateAngle(radians, nameof(radians));
        (double sin, double cos) = SinCosCore(radians);
        float s = (float)sin;
        float c = (float)cos;
        return new Matrix4x4(
            c, s, 0f, 0f,
            -s, c, 0f, 0f,
            0f, 0f, 1f, 0f,
            0f, 0f, 0f, 1f);
    }

    /// <summary>Creates a right-handed rotation, normalizing a finite, nonzero axis.</summary>
    /// <remarks>Radians must be finite and within [-1048576, 1048576].</remarks>
    public static Matrix4x4 FromAxisAngle(Vector3 axis, float radians) => MatrixFromQuaternion(QuatFromAxisAngle(axis, radians));

    /// <summary>Creates Y yaw, X pitch, Z roll using System.Numerics conventions.</summary>
    /// <remarks>Each angle must be finite and within [-1048576, 1048576].</remarks>
    public static Matrix4x4 FromYawPitchRoll(float yaw, float pitch, float roll) => MatrixFromQuaternion(QuatFromYawPitchRoll(yaw, pitch, roll));

    /// <summary>Creates a unit quaternion, normalizing a finite, nonzero axis.</summary>
    /// <remarks>Radians must be finite and within [-1048576, 1048576].</remarks>
    public static Quaternion QuatFromAxisAngle(Vector3 axis, float radians)
    {
        ValidateAngle(radians, nameof(radians));
        (double x, double y, double z, _) = Normalize(axis.X, axis.Y, axis.Z, 0d, nameof(axis));
        (double sin, double cos) = SinCosCore((double)radians * 0.5d);
        return new Quaternion((float)(x * sin), (float)(y * sin), (float)(z * sin), (float)cos);
    }

    /// <summary>Creates a unit quaternion with Y yaw, X pitch, Z roll.</summary>
    /// <remarks>Each angle must be finite and within [-1048576, 1048576].</remarks>
    public static Quaternion QuatFromYawPitchRoll(float yaw, float pitch, float roll)
    {
        ValidateAngle(yaw, nameof(yaw));
        ValidateAngle(pitch, nameof(pitch));
        ValidateAngle(roll, nameof(roll));
        (double sy, double cy) = SinCosCore((double)yaw * 0.5d);
        (double sp, double cp) = SinCosCore((double)pitch * 0.5d);
        (double sr, double cr) = SinCosCore((double)roll * 0.5d);
        return new Quaternion(
            (float)(cy * sp * cr + sy * cp * sr),
            (float)(sy * cp * cr - cy * sp * sr),
            (float)(cy * cp * sr - sy * sp * cr),
            (float)(cy * cp * cr + sy * sp * sr));
    }

    /// <summary>Shortest-arc spherical interpolation, normalizing inputs and output.</summary>
    /// <remarks>
    /// Inputs must be finite, nonzero quaternions; t must be finite and in [0, 1].
    /// Opposite-sign representations of a rotation are aligned before interpolating.
    /// The returned sign at t=1 can therefore differ from b while describing the
    /// same rotation. Nearly coincident orientations use normalized linear blending.
    /// </remarks>
    public static Quaternion Slerp(Quaternion a, Quaternion b, float t)
    {
        if (!float.IsFinite(t) || t < 0f || t > 1f)
        {
            throw new ArgumentOutOfRangeException(nameof(t), "Interpolation weight must be finite and in [0, 1].");
        }

        (double ax, double ay, double az, double aw) = Normalize(a.X, a.Y, a.Z, a.W, nameof(a));
        (double bx, double by, double bz, double bw) = Normalize(b.X, b.Y, b.Z, b.W, nameof(b));
        double dot = ax * bx + ay * by + az * bz + aw * bw;
        if (dot < 0d)
        {
            bx = -bx;
            by = -by;
            bz = -bz;
            bw = -bw;
            dot = -dot;
        }

        double left;
        double right;
        if (dot > 0.9995d)
        {
            left = 1d - t;
            right = t;
        }
        else
        {
            double angle = Acos(dot);
            double denominator = Sin(angle);
            left = Sin((1d - t) * angle) / denominator;
            right = Sin(t * angle) / denominator;
        }

        (double x, double y, double z, double w) = Normalize(
            left * ax + right * bx, left * ay + right * by,
            left * az + right * bz, left * aw + right * bw, nameof(a));
        return new Quaternion((float)x, (float)y, (float)z, (float)w);
    }

    private static Matrix4x4 MatrixFromQuaternion(Quaternion q)
    {
        double x = q.X;
        double y = q.Y;
        double z = q.Z;
        double w = q.W;
        double xx = x * x;
        double yy = y * y;
        double zz = z * z;
        double xy = x * y;
        double xz = x * z;
        double yz = y * z;
        double wx = w * x;
        double wy = w * y;
        double wz = w * z;
        return new Matrix4x4(
            (float)(1d - 2d * (yy + zz)), (float)(2d * (xy + wz)), (float)(2d * (xz - wy)), 0f,
            (float)(2d * (xy - wz)), (float)(1d - 2d * (xx + zz)), (float)(2d * (yz + wx)), 0f,
            (float)(2d * (xz + wy)), (float)(2d * (yz - wx)), (float)(1d - 2d * (xx + yy)), 0f,
            0f, 0f, 0f, 1f);
    }

    private static (double X, double Y, double Z, double W) Normalize(double x, double y, double z, double w, string parameter)
    {
        if (!double.IsFinite(x) || !double.IsFinite(y) || !double.IsFinite(z) || !double.IsFinite(w))
        {
            throw new ArgumentException("Rotation components must be finite.", parameter);
        }

        // Scaling before squaring also supports subnormal and near-max float axes.
        double scale = Math.Max(Math.Max(Math.Abs(x), Math.Abs(y)), Math.Max(Math.Abs(z), Math.Abs(w)));
        if (scale == 0d)
        {
            throw new ArgumentException("A rotation axis or quaternion must be nonzero.", parameter);
        }

        x /= scale;
        y /= scale;
        z /= scale;
        w /= scale;
        double length = Math.Sqrt(x * x + y * y + z * z + w * w);
        return (x / length, y / length, z / length, w / length);
    }

    private static void ValidateAngle(float radians, string parameter)
    {
        if (!float.IsFinite(radians) || Math.Abs((double)radians) > MaxAngle)
        {
            throw new ArgumentOutOfRangeException(parameter, "Angle must be finite and within +/-1048576 radians.");
        }
    }
}
