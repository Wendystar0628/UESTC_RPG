using Campus.Contracts;
using System.Numerics;

namespace Campus.Gen.Parts.Circulation.Geometry;

/// <summary>
/// Rigid placement: local (x, y, z) maps to Origin + X·x + Y·y + Z·z. The axes are orthonormal and right-handed,
/// so normals transform with the same axes and triangle winding is preserved.
/// </summary>
internal readonly record struct Frame(Vector3 Origin, Vector3 X, Vector3 Y, Vector3 Z)
{
    internal static Frame Identity { get; } = new(Vector3.Zero, Vector3.UnitX, Vector3.UnitY, Vector3.UnitZ);

    internal static Frame At(float x, float y, float z) => Identity with { Origin = new(x, y, z) };

    /// <summary>
    /// Lays a WP-09 sweep (path in kernel X/Z, section up along kernel Y) into the vertical plane through x:
    /// kernel X → z, kernel Y → x, kernel Z → y. Path points are therefore (z, y) pairs; positive lateral is below an
    /// ascending path and the section's up axis runs across +x.
    /// </summary>
    internal static Frame VerticalPlane(float x) => new(new(x, 0, 0), Vector3.UnitZ, Vector3.UnitX, Vector3.UnitY);

    /// <summary>Local +X runs from <paramref name="origin"/> along <paramref name="along"/>, local +Y leans toward world up.</summary>
    internal static Frame Along(Vector3 origin, Vector3 along)
    {
        var x = VectorMath.Unit(along);
        var y = VectorMath.Unit(Vector3.Subtract(Vector3.UnitY, Vector3.Multiply(x, (float)VectorMath.Dot(Vector3.UnitY, x))));
        return new(origin, x, y, VectorMath.Cross(x, y));
    }

    internal Vector3 Point(Vector3 p) => new(
        Origin.X + X.X * p.X + Y.X * p.Y + Z.X * p.Z,
        Origin.Y + X.Y * p.X + Y.Y * p.Y + Z.Y * p.Z,
        Origin.Z + X.Z * p.X + Y.Z * p.Y + Z.Z * p.Z);

    internal Vector3 Direction(Vector3 n) => new(
        X.X * n.X + Y.X * n.Y + Z.X * n.Z,
        X.Y * n.X + Y.Y * n.Y + Z.Y * n.Z,
        X.Z * n.X + Y.Z * n.Y + Z.Z * n.Z);

    /// <summary>Applies this frame first, then <paramref name="outer"/>.</summary>
    internal Frame Then(Frame outer) => new(outer.Point(Origin), outer.Direction(X), outer.Direction(Y), outer.Direction(Z));

    /// <summary>Row-vector System.Numerics matrix of this frame, as used by collider and placement contracts.</summary>
    internal Matrix4x4 ToMatrix() => new(X.X, X.Y, X.Z, 0, Y.X, Y.Y, Y.Z, 0, Z.X, Z.Y, Z.Z, 0, Origin.X, Origin.Y, Origin.Z, 1);
}
