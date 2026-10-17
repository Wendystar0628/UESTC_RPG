using Campus.Contracts;
using Campus.Kernel.Renderer;
using Godot;
using N = System.Numerics;

namespace Campus.Kernel.Collision;

internal static class CollisionShapeFactory
{
    private const int HeightSide = 65;
    private const float TileSide = 64f;
    public static (Shape3D Shape, Transform3D Transform) MakeShape(Collider source) => source switch
    {
        HeightFieldCollider height => HeightField(height),
        BoxCollider box => Box(box),
        _ => throw new NotSupportedException($"Collision mapping does not support {source.GetType().Name}."),
    };

    private static (Shape3D, Transform3D) HeightField(HeightFieldCollider source)
    {
        ArgumentNullException.ThrowIfNull(source.HeightsCm);
        if (source.HeightsCm.Length != HeightSide * HeightSide)
            throw new ArgumentException("Height fields must contain exactly 65 x 65 samples.", nameof(source));
        var heights = new float[source.HeightsCm.Length];
        for (var i = 0; i < heights.Length; i++) heights[i] = TerrainSampling.DecodeHeight(source.HeightsCm[i]);
        // Set dimensions first: either dimension setter resizes MapData.
        var shape = new HeightMapShape3D { MapWidth = HeightSide, MapDepth = HeightSide, MapData = heights };
        // The body supplies the tile origin; height-field shapes are tile-local.
        var center = new N.Vector3(TileSide / 2, 0,
            TileSide / 2);
        return (shape, At(center));
    }

    private static (Shape3D, Transform3D) Box(BoxCollider source)
    {
        Positive(source.Half.X, source.Half.Y, source.Half.Z);
        var m = source.At;
        Finite(m.M11, m.M12, m.M13, m.M14, m.M21, m.M22, m.M23, m.M24,
            m.M31, m.M32, m.M33, m.M34, m.M41, m.M42, m.M43, m.M44);
        if (m.M14 != 0 || m.M24 != 0 || m.M34 != 0 || m.M44 != 1)
            throw new ArgumentException("Box transforms must be affine.", nameof(source));
        var x = new N.Vector3(m.M11, m.M12, m.M13);
        var y = new N.Vector3(m.M21, m.M22, m.M23);
        var z = new N.Vector3(m.M31, m.M32, m.M33);
        var scale = new N.Vector3(x.Length(), y.Length(), z.Length());
        Positive(scale.X, scale.Y, scale.Z);
        x /= scale.X;
        y /= scale.Y;
        z /= scale.Z;
        if (MathF.Abs(N.Vector3.Dot(x, y)) > 0.0001f || MathF.Abs(N.Vector3.Dot(x, z)) > 0.0001f ||
            MathF.Abs(N.Vector3.Dot(y, z)) > 0.0001f)
            throw new ArgumentException("Collision boxes must be normalized before native mounting.", nameof(source));
        // A box is reflection-symmetric. Remove a negative determinant from the owner's rotation.
        if (N.Vector3.Dot(N.Vector3.Cross(x, y), z) < 0) x = -x;
        var rigid = new N.Matrix4x4(x.X, x.Y, x.Z, 0, y.X, y.Y, y.Z, 0, z.X, z.Y, z.Z, 0,
            m.M41, m.M42, m.M43, 1);
        return (new BoxShape3D { Size = (source.Half * scale * 2).ToGodot() }, rigid.ToGodot());
    }

    private static Transform3D At(N.Vector3 point) => new(Basis.Identity, point.ToGodot());

    private static void Positive(params float[] values)
    {
        foreach (var value in values)
            if (!float.IsFinite(value) || value <= 0)
                throw new ArgumentException("Collision dimensions must be finite and positive.");
    }

    private static void Finite(params float[] values)
    {
        foreach (var value in values)
            if (!float.IsFinite(value)) throw new ArgumentException("Collision coordinates must be finite.");
    }
}
