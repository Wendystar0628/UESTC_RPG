using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.Zones.Core.Framework;

/// <summary>
/// Places a catalogue part's own colliders (IPartDef.Collision, part-local) into the world by the same rigid row-vector
/// matrix as its geometry, so collision stays the part's parameters, not its mesh (SYS-WALK §13). Scalar arithmetic only.
/// </summary>
public static class Placed
{
    public static IEnumerable<Collider> Colliders(IReadOnlyList<Collider> local, Matrix4x4 at)
    {
        ArgumentNullException.ThrowIfNull(local);
        foreach (var collider in local) yield return Transform(collider, at);
    }

    private static Collider Transform(Collider collider, Matrix4x4 at) => collider switch
    {
        BoxCollider box => box with { At = Multiply(box.At, at) },
        RampCollider ramp => ramp with { P0 = Point(ramp.P0, at), P1 = Point(ramp.P1, at), P2 = Point(ramp.P2, at), P3 = Point(ramp.P3, at) },
        WallCollider wall => wall with { A = Plan(wall.A, at), B = Plan(wall.B, at), Y0 = wall.Y0 + at.M42, Y1 = wall.Y1 + at.M42 },
        PostCollider post => post with { C = Plan(post.C, at), Y0 = post.Y0 + at.M42, Y1 = post.Y1 + at.M42 },
        _ => throw new ArgumentException("Unsupported collider kind for a placed part.", nameof(collider)),
    };

    private static Vector3 Point(Vector3 p, Matrix4x4 at) => new(
        p.X * at.M11 + p.Y * at.M21 + p.Z * at.M31 + at.M41,
        p.X * at.M12 + p.Y * at.M22 + p.Z * at.M32 + at.M42,
        p.X * at.M13 + p.Y * at.M23 + p.Z * at.M33 + at.M43);

    /// <summary>A plan point (x, z) through a placement that turns only about +Y.</summary>
    private static Vector2 Plan(Vector2 p, Matrix4x4 at) => new(
        p.X * at.M11 + p.Y * at.M31 + at.M41,
        p.X * at.M13 + p.Y * at.M33 + at.M43);

    private static Matrix4x4 Multiply(Matrix4x4 a, Matrix4x4 b) => new(
        a.M11 * b.M11 + a.M12 * b.M21 + a.M13 * b.M31, a.M11 * b.M12 + a.M12 * b.M22 + a.M13 * b.M32, a.M11 * b.M13 + a.M12 * b.M23 + a.M13 * b.M33, 0,
        a.M21 * b.M11 + a.M22 * b.M21 + a.M23 * b.M31, a.M21 * b.M12 + a.M22 * b.M22 + a.M23 * b.M32, a.M21 * b.M13 + a.M22 * b.M23 + a.M23 * b.M33, 0,
        a.M31 * b.M11 + a.M32 * b.M21 + a.M33 * b.M31, a.M31 * b.M12 + a.M32 * b.M22 + a.M33 * b.M32, a.M31 * b.M13 + a.M32 * b.M23 + a.M33 * b.M33, 0,
        a.M41 * b.M11 + a.M42 * b.M21 + a.M43 * b.M31 + b.M41, a.M41 * b.M12 + a.M42 * b.M22 + a.M43 * b.M32 + b.M42,
        a.M41 * b.M13 + a.M42 * b.M23 + a.M43 * b.M33 + b.M43, 1);
}
