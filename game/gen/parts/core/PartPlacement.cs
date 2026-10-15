using System.Collections.ObjectModel;
using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.Parts.Core;

/// <summary>Stable-order instance arrays and local socket frames, without merging copies into vertex buffers.</summary>
public static class PartPlacement
{
    public static IReadOnlyList<PlacedPart> Linear(PlacedPart source, int count, Vector3 step)
    {
        ArgumentNullException.ThrowIfNull(source); Count(count); GeometryMath.Finite(step, nameof(step));
        ValidateMatrix(source.At);
        var result = new PlacedPart[count];
        for (var i = 0; i < count; i++)
        {
            var at = source.At; at.M41 += step.X * i; at.M42 += step.Y * i; at.M43 += step.Z * i;
            ValidateMatrix(at); result[i] = source with { At = at };
        }
        return Array.AsReadOnly(result);
    }

    /// <summary>Rotates the source's entire local placement around a world-space centre using deterministic Y rotation.</summary>
    public static IReadOnlyList<PlacedPart> Radial(PlacedPart source, int count, Vector3 centre, float stepRadians)
    {
        ArgumentNullException.ThrowIfNull(source); Count(count); GeometryMath.Finite(centre, nameof(centre)); ValidateMatrix(source.At);
        if (!float.IsFinite(stepRadians)) throw new ArgumentOutOfRangeException(nameof(stepRadians));
        var result = new PlacedPart[count];
        for (var i = 0; i < count; i++)
        {
            var rotation = DetMath.RotationY(stepRadians * i);
            var at = source.At; at.M41 -= centre.X; at.M42 -= centre.Y; at.M43 -= centre.Z;
            at = Multiply(at, rotation); at.M41 += centre.X; at.M42 += centre.Y; at.M43 += centre.Z;
            ValidateMatrix(at); result[i] = source with { At = at };
        }
        return Array.AsReadOnly(result);
    }

    /// <summary>Row-vector transform: +Z faces forward and +Y follows the supplied up direction.</summary>
    public static Matrix4x4 Socket(Vector3 position, Vector3 forward, Vector3 up)
    {
        GeometryMath.Finite(position, nameof(position));
        var z = GeometryMath.Unit(forward); var x = GeometryMath.Unit(GeometryMath.Cross(up, z)); var y = GeometryMath.Unit(GeometryMath.Cross(z, x));
        return new(x.X, x.Y, x.Z, 0, y.X, y.Y, y.Z, 0, z.X, z.Y, z.Z, 0, position.X, position.Y, position.Z, 1);
    }

    public static IReadOnlyDictionary<string, Matrix4x4> BoxSockets(float width, float height, float depth)
    {
        GeometryMath.Positive(width, nameof(width)); GeometryMath.Positive(height, nameof(height)); GeometryMath.Positive(depth, nameof(depth));
        var sockets = new SortedDictionary<string, Matrix4x4>(StringComparer.Ordinal)
        {
            ["origin"] = Matrix4x4.Identity,
            ["top"] = Socket(new(0, height, 0), Vector3.UnitY, -Vector3.UnitZ),
            ["front"] = Socket(new(0, height * 0.5f, -depth * 0.5f), -Vector3.UnitZ, Vector3.UnitY),
            ["back"] = Socket(new(0, height * 0.5f, depth * 0.5f), Vector3.UnitZ, Vector3.UnitY),
            ["left"] = Socket(new(-width * 0.5f, height * 0.5f, 0), -Vector3.UnitX, Vector3.UnitY),
            ["right"] = Socket(new(width * 0.5f, height * 0.5f, 0), Vector3.UnitX, Vector3.UnitY),
        };
        return new ReadOnlyDictionary<string, Matrix4x4>(sockets);
    }
    private static void Count(int count)
    {
        if (count < 0 || count > 65536) throw new ArgumentOutOfRangeException(nameof(count));
    }
    private static void ValidateMatrix(Matrix4x4 m)
    {
        float[] values = [m.M11, m.M12, m.M13, m.M14, m.M21, m.M22, m.M23, m.M24, m.M31, m.M32, m.M33, m.M34, m.M41, m.M42, m.M43, m.M44];
        if (values.Any(v => !float.IsFinite(v)) || m.M14 != 0 || m.M24 != 0 || m.M34 != 0 || m.M44 != 1)
            throw new ArgumentException("Placement must be a finite affine matrix.", nameof(m));
    }
    private static Matrix4x4 Multiply(Matrix4x4 a, Matrix4x4 b) => new(
        a.M11 * b.M11 + a.M12 * b.M21 + a.M13 * b.M31, a.M11 * b.M12 + a.M12 * b.M22 + a.M13 * b.M32, a.M11 * b.M13 + a.M12 * b.M23 + a.M13 * b.M33, 0,
        a.M21 * b.M11 + a.M22 * b.M21 + a.M23 * b.M31, a.M21 * b.M12 + a.M22 * b.M22 + a.M23 * b.M32, a.M21 * b.M13 + a.M22 * b.M23 + a.M23 * b.M33, 0,
        a.M31 * b.M11 + a.M32 * b.M21 + a.M33 * b.M31, a.M31 * b.M12 + a.M32 * b.M22 + a.M33 * b.M32, a.M31 * b.M13 + a.M32 * b.M23 + a.M33 * b.M33, 0,
        a.M41 * b.M11 + a.M42 * b.M21 + a.M43 * b.M31 + b.M41, a.M41 * b.M12 + a.M42 * b.M22 + a.M43 * b.M32 + b.M42, a.M41 * b.M13 + a.M42 * b.M23 + a.M43 * b.M33 + b.M43, 1);
}
