using System.Numerics;

namespace Campus.Gen.Parts.Circulation.Geometry;

internal static class V3
{
    internal static Vector3 Add(Vector3 a, Vector3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    internal static Vector3 Sub(Vector3 a, Vector3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    internal static Vector3 Scale(Vector3 a, float s) => new(a.X * s, a.Y * s, a.Z * s);
    internal static Vector3 Cross(Vector3 a, Vector3 b) => new(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X);
    internal static double Dot(Vector3 a, Vector3 b) => (double)a.X * b.X + (double)a.Y * b.Y + (double)a.Z * b.Z;
    internal static Vector3 Unit(Vector3 a)
    {
        var length = Math.Sqrt(Dot(a, a));
        if (!double.IsFinite(length) || length <= 1e-12) throw new ArgumentException("A direction must be finite and nonzero.");
        return new((float)(a.X / length), (float)(a.Y / length), (float)(a.Z / length));
    }
    internal static float Length(Vector2 a) => (float)Math.Sqrt((double)a.X * a.X + (double)a.Y * a.Y);
}

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
        var x = V3.Unit(along);
        var y = V3.Unit(V3.Sub(Vector3.UnitY, V3.Scale(x, (float)V3.Dot(Vector3.UnitY, x))));
        return new(origin, x, y, V3.Cross(x, y));
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
