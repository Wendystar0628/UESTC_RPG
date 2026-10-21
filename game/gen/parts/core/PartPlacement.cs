using System.Collections.ObjectModel;
using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.Parts.Core;

/// <summary>Stable-order instance arrays and local socket frames, without merging copies into vertex buffers.</summary>
public static class PartPlacement
{
    public static IReadOnlyList<PlacedPart> Linear(PlacedPart source, int count, Vector3 step)
    {
        ArgumentNullException.ThrowIfNull(source); Count(count);
        if (!VectorMath.Finite(step)) throw new ArgumentException("Coordinates must be finite.", nameof(step));
        ValidateMatrix(source.At);
        var result = new PlacedPart[count];
        for (var i = 0; i < count; i++)
        {
            var at = source.At; at.M41 += step.X * i; at.M42 += step.Y * i; at.M43 += step.Z * i;
            ValidateMatrix(at); result[i] = source with { At = at };
        }
        return Array.AsReadOnly(result);
    }

    /// <summary>Row-vector transform: +Z faces forward and +Y follows the supplied up direction.</summary>
    public static Matrix4x4 Socket(Vector3 position, Vector3 forward, Vector3 up)
    {
        if (!VectorMath.Finite(position)) throw new ArgumentException("Coordinates must be finite.", nameof(position));
        var z = VectorMath.Unit(forward); var x = VectorMath.Unit(VectorMath.Cross(up, z)); var y = VectorMath.Unit(VectorMath.Cross(z, x));
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
        GeometryVertexLimits.Require(count);
    }
    private static void ValidateMatrix(Matrix4x4 m)
    {
        float[] values = [m.M11, m.M12, m.M13, m.M14, m.M21, m.M22, m.M23, m.M24, m.M31, m.M32, m.M33, m.M34, m.M41, m.M42, m.M43, m.M44];
        if (values.Any(v => !float.IsFinite(v)) || m.M14 != 0 || m.M24 != 0 || m.M34 != 0 || m.M44 != 1)
            throw new ArgumentException("Placement must be a finite affine matrix.", nameof(m));
    }
}
