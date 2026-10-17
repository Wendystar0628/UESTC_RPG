using Campus.Contracts;
using Campus.Gen.Collision;
using Campus.Kernel.Renderer;
using Godot;
using N = System.Numerics;

namespace Campus.Kernel.Collision;

internal static class CollisionShapeFactory
{
    private const int HeightSide = 65;
    private const float TileSide = 64f;
    private const float JoltHullTolerance = .001f;
    public static (Shape3D Shape, Transform3D Transform) MakeShape(Collider source) => source switch
    {
        HeightFieldCollider height => HeightField(height),
        WallCollider wall => Wall(wall),
        BoxCollider box => Box(box),
        SlabCollider slab => Slab(slab),
        RampCollider ramp => Ramp(ramp),
        SlopedWallCollider wall => SlopedWall(wall),
        PostCollider post => Post(post),
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

    private static (Shape3D, Transform3D) Wall(WallCollider source)
    {
        var (axis, length) = WallAxis(source.A, source.B);
        Positive(source.HalfT);
        Positive(source.Y1 - source.Y0);
        Finite(source.Y0, source.Y1);
        var center = new N.Vector3((source.A.X + source.B.X) / 2, (source.Y0 + source.Y1) / 2,
            (source.A.Y + source.B.Y) / 2);
        var basis = new Basis(axis.ToGodot(), N.Vector3.UnitY.ToGodot(), new N.Vector3(-axis.Z, 0, axis.X).ToGodot());
        return (new BoxShape3D { Size = new Vector3(length, source.Y1 - source.Y0, source.HalfT * 2) },
            new Transform3D(basis, center.ToGodot()));
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
        {
            // Preserve a sheared affine box exactly instead of asking physics to handle scaled owners.
            var corners = new N.Vector3[8];
            for (var i = 0; i < corners.Length; i++)
                corners[i] = N.Vector3.Transform(new N.Vector3(
                    (i & 1) == 0 ? -source.Half.X : source.Half.X,
                    (i & 2) == 0 ? -source.Half.Y : source.Half.Y,
                    (i & 4) == 0 ? -source.Half.Z : source.Half.Z), m);
            return Convex(corners);
        }
        // A box is reflection-symmetric. Remove a negative determinant from the owner's rotation.
        if (N.Vector3.Dot(N.Vector3.Cross(x, y), z) < 0) x = -x;
        var rigid = new N.Matrix4x4(x.X, x.Y, x.Z, 0, y.X, y.Y, y.Z, 0, z.X, z.Y, z.Z, 0,
            m.M41, m.M42, m.M43, 1);
        return (new BoxShape3D { Size = (source.Half * scale * 2).ToGodot() }, rigid.ToGodot());
    }

    private static (Shape3D, Transform3D) Slab(SlabCollider source)
    {
        ArgumentNullException.ThrowIfNull(source.Ring);
        if (!CollisionGeometry.IsConvexRing(source.Ring)) throw new ArgumentException("A prepared slab must have a convex outline.", nameof(source));
        Positive(source.Thick);
        Finite(source.Y);
        var bottom = source.Y - source.Thick;
        Finite(bottom);
        if (bottom >= source.Y) throw new ArgumentException("A slab must retain a nonzero thickness in physics coordinates.", nameof(source));
        var points = new N.Vector3[source.Ring.Count * 2];
        for (var i = 0; i < source.Ring.Count; i++)
        {
            Finite(source.Ring[i].X, source.Ring[i].Y);
            points[i] = new N.Vector3(source.Ring[i].X, source.Y, source.Ring[i].Y);
            points[i + source.Ring.Count] = new N.Vector3(source.Ring[i].X, bottom, source.Ring[i].Y);
        }
        // Every floor piece retains the tile-local frame. Re-centering adjacent pieces on
        // different float centroids can reconstruct a shared edge differently and open a
        // native ray crack. The tile body already keeps these coordinates near the origin.
        // Convex floors remain whole; concave outlines were decomposed by the bake worker.
        var scale = SlabHullScale(source.Ring, (double)source.Y - bottom);
        if (scale == 1)
            return (new ConvexPolygonShape3D { Points = points.Select(point => point.ToGodot()).ToArray() }, At(N.Vector3.Zero));
        // Jolt builds the hull before shrinking its margin. Binary scaling clears its
        // absolute hull tolerance without moving a tile-local vertex or shared edge.
        for (var i = 0; i < points.Length; i++)
        {
            points[i] *= scale;
            Finite(points[i].X, points[i].Y, points[i].Z);
        }
        return (new ConvexPolygonShape3D { Margin = 0, Points = points.Select(point => point.ToGodot()).ToArray() },
            new Transform3D(Basis.Identity.Scaled(Vector3.One / scale), Vector3.Zero));
    }

    private static float SlabHullScale(IReadOnlyList<N.Vector2> ring, double thickness)
    {
        var width = thickness;
        for (var i = 0; i < ring.Count; i++)
        {
            var a = ring[i];
            var b = ring[(i + 1) % ring.Count];
            var dx = (double)b.X - a.X;
            var dz = (double)b.Y - a.Y;
            var length = Math.Sqrt(dx * dx + dz * dz);
            if (length == 0) throw new ArgumentException("A prepared slab cannot contain zero-length edges.", nameof(ring));
            var edgeWidth = 0d;
            foreach (var point in ring)
                edgeWidth = Math.Max(edgeWidth, Math.Abs(dx * ((double)point.Y - a.Y) - dz * ((double)point.X - a.X)) / length);
            if (edgeWidth == 0) throw new ArgumentException("A prepared slab must have a nonzero outline width.", nameof(ring));
            width = Math.Min(width, edgeWidth);
        }
        if (width >= JoltHullTolerance) return 1;
        var scale = 1f;
        // Keep the narrowest dimension at least four hull tolerances wide.
        while (width * scale < 4 * JoltHullTolerance)
        {
            scale *= 2;
            Positive(scale);
        }
        return scale;
    }

    private static (Shape3D, Transform3D) Ramp(RampCollider source)
    {
        Positive(source.Thick);
        N.Vector3[] top = [source.P0, source.P1, source.P2, source.P3];
        var points = new N.Vector3[8];
        for (var i = 0; i < top.Length; i++)
        {
            points[i] = top[i];
            points[i + 4] = top[i] - N.Vector3.UnitY * source.Thick;
        }
        return Convex(points);
    }

    private static (Shape3D, Transform3D) SlopedWall(SlopedWallCollider source)
    {
        var (axis, _) = WallAxis(source.A, source.B);
        Positive(source.HalfT, source.Y1a - source.Y0a, source.Y1b - source.Y0b);
        var offset = new N.Vector3(-axis.Z, 0, axis.X) * source.HalfT;
        var a0 = new N.Vector3(source.A.X, source.Y0a, source.A.Y);
        var a1 = new N.Vector3(source.A.X, source.Y1a, source.A.Y);
        var b0 = new N.Vector3(source.B.X, source.Y0b, source.B.Y);
        var b1 = new N.Vector3(source.B.X, source.Y1b, source.B.Y);
        return Convex([a0 - offset, a0 + offset, a1 - offset, a1 + offset,
            b0 - offset, b0 + offset, b1 - offset, b1 + offset]);
    }

    private static (Shape3D, Transform3D) Post(PostCollider source)
    {
        Positive(source.R, source.Y1 - source.Y0);
        Finite(source.C.X, source.C.Y, source.Y0, source.Y1);
        return (new CylinderShape3D { Radius = source.R, Height = source.Y1 - source.Y0 },
            At(new N.Vector3(source.C.X, (source.Y0 + source.Y1) / 2, source.C.Y)));
    }

    private static (N.Vector3 Axis, float Length) WallAxis(N.Vector2 a, N.Vector2 b)
    {
        Finite(a.X, a.Y, b.X, b.Y);
        var axis = new N.Vector3(b.X - a.X, 0, b.Y - a.Y);
        var length = axis.Length();
        Positive(length);
        return (axis / length, length);
    }

    private static (Shape3D, Transform3D) Convex(N.Vector3[] worldPoints)
    {
        var center = N.Vector3.Zero;
        foreach (var point in worldPoints)
        {
            Finite(point.X, point.Y, point.Z);
            center += point / worldPoints.Length;
        }
        // Keep points near the shape origin for better single-precision physics stability.
        var points = worldPoints.Select(point => (point - center).ToGodot()).ToArray();
        return (new ConvexPolygonShape3D { Points = points }, At(center));
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
