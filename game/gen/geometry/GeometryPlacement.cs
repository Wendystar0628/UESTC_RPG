using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.Geometry;

/// <summary>Shared scalar row-vector placement. Expression and normalisation order is fixed.</summary>
public static class GeometryPlacement
{
    public static Matrix4x4 Multiply(Matrix4x4 a, Matrix4x4 b) => new(
        a.M11 * b.M11 + a.M12 * b.M21 + a.M13 * b.M31, a.M11 * b.M12 + a.M12 * b.M22 + a.M13 * b.M32, a.M11 * b.M13 + a.M12 * b.M23 + a.M13 * b.M33, 0,
        a.M21 * b.M11 + a.M22 * b.M21 + a.M23 * b.M31, a.M21 * b.M12 + a.M22 * b.M22 + a.M23 * b.M32, a.M21 * b.M13 + a.M22 * b.M23 + a.M23 * b.M33, 0,
        a.M31 * b.M11 + a.M32 * b.M21 + a.M33 * b.M31, a.M31 * b.M12 + a.M32 * b.M22 + a.M33 * b.M32, a.M31 * b.M13 + a.M32 * b.M23 + a.M33 * b.M33, 0,
        a.M41 * b.M11 + a.M42 * b.M21 + a.M43 * b.M31 + b.M41, a.M41 * b.M12 + a.M42 * b.M22 + a.M43 * b.M32 + b.M42,
        a.M41 * b.M13 + a.M42 * b.M23 + a.M43 * b.M33 + b.M43, 1);

    public static IEnumerable<Collider> Colliders(IReadOnlyList<Collider> local, Matrix4x4 at)
    {
        ArgumentNullException.ThrowIfNull(local);
        foreach (var collider in local) yield return Transform(collider, at);
    }

    public static Collider Transform(Collider collider, Matrix4x4 at) => collider switch
    {
        BoxCollider box => box with { At = Multiply(box.At, at) },
        RampCollider ramp => ramp with { P0 = Point(ramp.P0, at), P1 = Point(ramp.P1, at), P2 = Point(ramp.P2, at), P3 = Point(ramp.P3, at) },
        WallCollider wall => wall with { A = Plan(wall.A, at), B = Plan(wall.B, at), Y0 = wall.Y0 + at.M42, Y1 = wall.Y1 + at.M42 },
        PostCollider post => post with { C = Plan(post.C, at), Y0 = post.Y0 + at.M42, Y1 = post.Y1 + at.M42 },
        _ => throw new ArgumentException("Unsupported collider kind for a placed part.", nameof(collider)),
    };

    public static Vector3 Point(Vector3 p, Matrix4x4 at) => new(
        p.X * at.M11 + p.Y * at.M21 + p.Z * at.M31 + at.M41,
        p.X * at.M12 + p.Y * at.M22 + p.Z * at.M32 + at.M42,
        p.X * at.M13 + p.Y * at.M23 + p.Z * at.M33 + at.M43);

    /// <summary>A plan point (x, z) through a placement that turns only about +Y.</summary>
    public static Vector2 Plan(Vector2 p, Matrix4x4 at) => new(
        p.X * at.M11 + p.Y * at.M31 + at.M41,
        p.X * at.M13 + p.Y * at.M33 + at.M43);


    public static Vector2 AnchorOf(Collider collider) => collider switch
    {
        BoxCollider box => new(box.At.M41, box.At.M43),
        WallCollider wall => (wall.A + wall.B) * 0.5f,
        PostCollider post => post.C,
        RampCollider ramp => new((ramp.P0.X + ramp.P2.X) * 0.5f, (ramp.P0.Z + ramp.P2.Z) * 0.5f),
        SlabCollider slab => slab.Ring[0],
        SlopedWallCollider sloped => (sloped.A + sloped.B) * 0.5f,
        _ => throw new ArgumentException("Unsupported collider kind for a zone site.", nameof(collider)),
    };

    public static (Vector2 Anchor, GeometryBundle Mesh) Place(GeometryBundle mesh, Matrix4x4 at)
    {
        ArgumentNullException.ThrowIfNull(mesh);
        var count = mesh.Position.Length / 3;
        var position = new float[mesh.Position.Length];
        var normal = new float[mesh.Normal.Length];
        float minX = float.MaxValue, minZ = float.MaxValue, maxX = float.MinValue, maxZ = float.MinValue;
        for (var i = 0; i < count; i++)
        {
            float x = mesh.Position[3 * i], y = mesh.Position[3 * i + 1], z = mesh.Position[3 * i + 2];
            var wx = x * at.M11 + y * at.M21 + z * at.M31 + at.M41;
            var wy = x * at.M12 + y * at.M22 + z * at.M32 + at.M42;
            var wz = x * at.M13 + y * at.M23 + z * at.M33 + at.M43;
            position[3 * i] = wx; position[3 * i + 1] = wy; position[3 * i + 2] = wz;
            minX = Math.Min(minX, wx); maxX = Math.Max(maxX, wx); minZ = Math.Min(minZ, wz); maxZ = Math.Max(maxZ, wz);
            float nx = mesh.Normal[3 * i], ny = mesh.Normal[3 * i + 1], nz = mesh.Normal[3 * i + 2];
            var tx = nx * at.M11 + ny * at.M21 + nz * at.M31;
            var ty = nx * at.M12 + ny * at.M22 + nz * at.M32;
            var tz = nx * at.M13 + ny * at.M23 + nz * at.M33;
            var length = MathF.Sqrt(tx * tx + ty * ty + tz * tz);
            if (length > 1e-12f) { tx /= length; ty /= length; tz /= length; }
            normal[3 * i] = tx; normal[3 * i + 1] = ty; normal[3 * i + 2] = tz;
        }
        var placed = mesh with { Position = position, Normal = normal };
        return (new((minX + maxX) * 0.5f, (minZ + maxZ) * 0.5f), placed);
    }
}
