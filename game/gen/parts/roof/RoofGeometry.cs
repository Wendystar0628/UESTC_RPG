using System.Collections.ObjectModel;
using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Parts.Core;

namespace Campus.Gen.Parts.Roof;

/// <summary>Rigid row-vector transforms written as scalar arithmetic so bakes stay bitwise stable (D-165).</summary>
internal static class Xf
{
    public static readonly Matrix4x4 Identity = Matrix4x4.Identity;
    /// <summary>Local +X becomes world +Y (pipes swept along X stand up).</summary>
    public static readonly Matrix4x4 XUp = new(0, 1, 0, 0, -1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1);
    /// <summary>Local +X becomes world −Y, local +Z stays +Z (a path drawn from the top down).</summary>
    public static readonly Matrix4x4 XDown = new(0, -1, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1);
    /// <summary>Quarter turn about Y: local +X becomes world −Z, local +Z becomes world +X.</summary>
    public static readonly Matrix4x4 YQuarter = new(0, 0, -1, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 1);

    public static Matrix4x4 Move(float x, float y, float z) => new(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, x, y, z, 1);
    public static Matrix4x4 Move(Vector3 v) => Move(v.X, v.Y, v.Z);
    public static Matrix4x4 RotX(float degrees) => DetMath.RotationX(degrees * (float.Pi / 180f));
    public static Matrix4x4 RotY(float degrees) => DetMath.RotationY(degrees * (float.Pi / 180f));

    /// <summary>Apply a first, then b.</summary>
    public static Matrix4x4 Then(this Matrix4x4 a, Matrix4x4 b) => new(
        a.M11 * b.M11 + a.M12 * b.M21 + a.M13 * b.M31, a.M11 * b.M12 + a.M12 * b.M22 + a.M13 * b.M32, a.M11 * b.M13 + a.M12 * b.M23 + a.M13 * b.M33, 0,
        a.M21 * b.M11 + a.M22 * b.M21 + a.M23 * b.M31, a.M21 * b.M12 + a.M22 * b.M22 + a.M23 * b.M32, a.M21 * b.M13 + a.M22 * b.M23 + a.M23 * b.M33, 0,
        a.M31 * b.M11 + a.M32 * b.M21 + a.M33 * b.M31, a.M31 * b.M12 + a.M32 * b.M22 + a.M33 * b.M32, a.M31 * b.M13 + a.M32 * b.M23 + a.M33 * b.M33, 0,
        a.M41 * b.M11 + a.M42 * b.M21 + a.M43 * b.M31 + b.M41, a.M41 * b.M12 + a.M42 * b.M22 + a.M43 * b.M32 + b.M42, a.M41 * b.M13 + a.M42 * b.M23 + a.M43 * b.M33 + b.M43, 1);

    public static Vector3 Point(Matrix4x4 m, Vector3 p) => new(
        p.X * m.M11 + p.Y * m.M21 + p.Z * m.M31 + m.M41,
        p.X * m.M12 + p.Y * m.M22 + p.Z * m.M32 + m.M42,
        p.X * m.M13 + p.Y * m.M23 + p.Z * m.M33 + m.M43);

    public static Vector3 Direction(Matrix4x4 m, Vector3 n)
    {
        var x = n.X * m.M11 + n.Y * m.M21 + n.Z * m.M31;
        var y = n.X * m.M12 + n.Y * m.M22 + n.Z * m.M32;
        var z = n.X * m.M13 + n.Y * m.M23 + n.Z * m.M33;
        var length = Math.Sqrt((double)x * x + (double)y * y + (double)z * z);
        return length <= 1e-12 ? n : new((float)(x / length), (float)(y / length), (float)(z / length));
    }

    /// <summary>Right-handed frame whose local +X follows <paramref name="axis"/>; used to lay swept pipes along any direction.</summary>
    public static Matrix4x4 AlongX(Vector3 axis, Vector3 origin)
    {
        var x = Unit(axis);
        var hint = Math.Abs(x.Y) < 0.9f ? Vector3.UnitY : Vector3.UnitX;
        var dot = x.X * hint.X + x.Y * hint.Y + x.Z * hint.Z;
        var y = Unit(new(hint.X - x.X * dot, hint.Y - x.Y * dot, hint.Z - x.Z * dot));
        var z = new Vector3(x.Y * y.Z - x.Z * y.Y, x.Z * y.X - x.X * y.Z, x.X * y.Y - x.Y * y.X);
        return new(x.X, x.Y, x.Z, 0, y.X, y.Y, y.Z, 0, z.X, z.Y, z.Z, 0, origin.X, origin.Y, origin.Z, 1);
    }

    public static Vector3 Unit(Vector3 v)
    {
        var length = Math.Sqrt((double)v.X * v.X + (double)v.Y * v.Y + (double)v.Z * v.Z);
        if (!(length > 1e-12)) throw new ArgumentException("A direction must be nonzero.");
        return new((float)(v.X / length), (float)(v.Y / length), (float)(v.Z / length));
    }
}

/// <summary>
/// Collects closed kernel pieces, moves them rigidly and merges surfaces that share a ToonSpec into one bundle.
/// Kernel AO is visibility (1 = unoccluded, opening reveals below 1) and passes straight through, scaled by the piece's
/// own factor; a bundle that ends up fully unoccluded stores no AO (contract: missing AO = 1).
/// Groups keep first-use order, so output is independent of hash ordering.
/// </summary>
internal sealed class PartBuilder
{
    private readonly List<Group> _groups = [];

    public PartBuilder Add(PartGeometry piece, Matrix4x4 at, byte flags = 0, float ao = 1f)
    {
        ArgumentNullException.ThrowIfNull(piece);
        foreach (var mesh in piece.Meshes)
        {
            var group = Find(mesh.Material.Spec);
            var first = group.Count;
            var count = mesh.Position.Length / 3;
            for (var i = 0; i < count; i++)
            {
                var p = Xf.Point(at, new(mesh.Position[3 * i], mesh.Position[3 * i + 1], mesh.Position[3 * i + 2]));
                var n = Xf.Direction(at, new(mesh.Normal[3 * i], mesh.Normal[3 * i + 1], mesh.Normal[3 * i + 2]));
                group.Position.Add(p.X); group.Position.Add(p.Y); group.Position.Add(p.Z);
                group.Normal.Add(n.X); group.Normal.Add(n.Y); group.Normal.Add(n.Z);
                group.Uv.Add(mesh.Uv?[2 * i] ?? 0f); group.Uv.Add(mesh.Uv?[2 * i + 1] ?? 0f);
                var visibility = (mesh.Ao?[i] ?? 1f) * ao;
                group.Flags.Add(flags); group.Ao.Add(visibility);
                group.HasAo |= visibility < 1f;
            }
            foreach (var index in mesh.Index) group.Index.Add(first + index);
            group.HasFlags |= flags != 0;
            if (group.Count > 65536) throw new ArgumentException("A part surface exceeds 65,536 vertices.");
        }
        return this;
    }

    public PartBuilder Add(PartGeometry piece, byte flags = 0) => Add(piece, Matrix4x4.Identity, flags);

    /// <summary>Exact extents of everything added so far (for parts whose bounds follow tilted, curved pieces).</summary>
    public (Vector3 Min, Vector3 Max) Measure()
    {
        Vector3 min = new(float.MaxValue), max = new(float.MinValue);
        foreach (var group in _groups)
            for (var i = 0; i < group.Position.Count; i += 3)
            {
                var p = new Vector3(group.Position[i], group.Position[i + 1], group.Position[i + 2]);
                min = Vector3.Min(min, p); max = Vector3.Max(max, p);
            }
        return (min, max);
    }

    public PartGeometry Finish(Box3 bounds)
    {
        if (_groups.Count == 0) throw new InvalidOperationException("A part level must contain geometry.");
        var meshes = new GeometryBundle[_groups.Count];
        for (var g = 0; g < _groups.Count; g++)
        {
            var group = _groups[g];
            var mesh = new GeometryBundle(new ToonRef(group.Spec), [.. group.Position], [.. group.Normal], [.. group.Index],
                group.HasAo ? [.. group.Ao] : null, [.. group.Uv])
            {
                Flags = group.HasFlags ? [.. group.Flags] : null,
            };
            mesh.ValidateVertexData();
            meshes[g] = mesh;
        }
        return new(Array.AsReadOnly(meshes), bounds);
    }

    private Group Find(ToonSpec spec)
    {
        foreach (var group in _groups)
            if (group.Spec == spec) return group;
        var created = new Group(spec);
        _groups.Add(created);
        return created;
    }

    private sealed class Group(ToonSpec spec)
    {
        public ToonSpec Spec { get; } = spec;
        public List<float> Position { get; } = [];
        public List<float> Normal { get; } = [];
        public List<float> Uv { get; } = [];
        public List<int> Index { get; } = [];
        public List<byte> Flags { get; } = [];
        public List<float> Ao { get; } = [];
        public bool HasFlags { get; set; }
        public bool HasAo { get; set; }
        public int Count => Position.Count / 3;
    }
}

/// <summary>Vertex flag bits of the VF-A COLOR byte (ENG-PERF).</summary>
internal static class VertexFlag
{
    /// <summary>Interior surface: shade from palette row 2, lit from row 3 times the light-patch gain (WLD-INTERIORS).</summary>
    public const byte Interior = 1;
    public const byte NoShadow = 8;
}

/// <summary>Primitive shapes built on the WP-09 kernel. All are closed, clockwise and in metres.</summary>
internal static class Shapes
{
    /// <summary>Box from its minimum and maximum corners; the chamfer is clamped to stay valid.</summary>
    public static PartGeometry Box(Vector3 min, Vector3 max, float bevel, ToonSpec material, out Matrix4x4 at)
    {
        var size = new Vector3(max.X - min.X, max.Y - min.Y, max.Z - min.Z);
        var limit = Math.Min(size.X, Math.Min(size.Y, size.Z)) * 0.45f;
        at = Xf.Move((min.X + max.X) * 0.5f, min.Y, (min.Z + max.Z) * 0.5f);
        return PartGeometryKernel.BeveledBox(size.X, size.Y, size.Z, Math.Min(bevel, limit), material);
    }

    public static PartBuilder AddBox(this PartBuilder builder, Vector3 min, Vector3 max, float bevel, ToonSpec material, byte flags = 0)
    {
        var piece = Box(min, max, bevel, material, out var at);
        return builder.Add(piece, at, flags);
    }

    /// <summary>Box centred on <paramref name="centre"/> (all three axes), then moved by <paramref name="then"/>.</summary>
    public static PartBuilder AddCentredBox(this PartBuilder builder, Vector3 size, Matrix4x4 then, ToonSpec material, float bevel = 0, byte flags = 0)
    {
        var limit = Math.Min(size.X, Math.Min(size.Y, size.Z)) * 0.45f;
        var piece = PartGeometryKernel.BeveledBox(size.X, size.Y, size.Z, Math.Min(bevel, limit), material);
        return builder.Add(piece, Xf.Move(0, -size.Y * 0.5f, 0).Then(then), flags);
    }

    /// <summary>Counterclockwise circle section with radial smooth normals, starting on the +lateral axis.</summary>
    public static SweepProfile Circle(float radius, int segments, Vector2 centre = default)
    {
        if (segments < 3) throw new ArgumentOutOfRangeException(nameof(segments));
        var points = new Vector2[segments]; var normals = new Vector2[segments];
        for (var i = 0; i < segments; i++)
        {
            var angle = 2.0 * Math.PI * i / segments;
            var c = (float)DetMath.Cos(angle); var s = (float)DetMath.Sin(angle);
            points[i] = new(centre.X + radius * c, centre.Y + radius * s);
            normals[i] = Normal2(c, s);
        }
        return new(Array.AsReadOnly(points), Array.AsReadOnly(normals));
    }

    /// <summary>Rectangle section from lateral/up extents; flat faces (no interpolated normals).</summary>
    public static SweepProfile Rect(float lateral0, float lateral1, float up0, float up1) =>
        new([new(lateral0, up0), new(lateral1, up0), new(lateral1, up1), new(lateral0, up1)]);

    /// <summary>Chamfered rectangle section moved laterally and vertically; chamfer bands interpolate normals.</summary>
    public static SweepProfile BevelRect(float width, float height, float bevel, float lateralShift, float upShift)
    {
        var limit = Math.Min(width, height) * 0.45f;
        var source = PartGeometryKernel.BeveledRectangleProfile(width, height, Math.Min(bevel, limit));
        var points = source.Points.Select(p => new Vector2(p.X + lateralShift, p.Y + upShift)).ToArray();
        return new(Array.AsReadOnly(points), source.Normals);
    }

    /// <summary>Round pipe between two points (smooth normals, flat end caps).</summary>
    public static PartBuilder AddPipe(this PartBuilder builder, Vector3 a, Vector3 b, float radius, int segments, ToonSpec material, byte flags = 0)
    {
        var delta = new Vector3(b.X - a.X, b.Y - a.Y, b.Z - a.Z);
        var length = (float)Math.Sqrt((double)delta.X * delta.X + (double)delta.Y * delta.Y + (double)delta.Z * delta.Z);
        var piece = PartGeometryKernel.Sweep(Circle(radius, segments), [Vector2.Zero, new(length, 0)], material);
        return builder.Add(piece, Xf.AlongX(delta, a), flags);
    }

    /// <summary>Vertical cylinder standing on (x, y0, z).</summary>
    public static PartBuilder AddColumn(this PartBuilder builder, float x, float y0, float y1, float z, float radius, int segments, ToonSpec material, byte flags = 0)
    {
        var piece = PartGeometryKernel.Sweep(Circle(radius, segments), [Vector2.Zero, new(y1 - y0, 0)], material);
        return builder.Add(piece, Xf.XUp.Then(Xf.Move(x, y0, z)), flags);
    }

    /// <summary>Thin disc facing −Z (its axis along Z), from z0 back to z1.</summary>
    public static PartBuilder AddDisc(this PartBuilder builder, float x, float y, float zFront, float zBack, float radius, int segments, ToonSpec material, byte flags = 0) =>
        builder.AddPipe(new(x, y, zBack), new(x, y, zFront), radius, segments, material, flags);

    /// <summary>
    /// Closed solid of revolution about +Y. Each band is a profile edge (r, y) with its own end normals,
    /// so creases (rims) stay hard and curved faces stay smooth. Bands must form one closed loop that
    /// starts and ends on the axis (r = 0).
    /// </summary>
    public static PartGeometry Lathe(IReadOnlyList<LatheBand> bands, int segments, ToonSpec material)
    {
        if (segments < 3) throw new ArgumentOutOfRangeException(nameof(segments));
        var cos = new float[segments]; var sin = new float[segments];
        for (var i = 0; i < segments; i++)
        {
            var angle = 2.0 * Math.PI * i / segments;
            cos[i] = (float)DetMath.Cos(angle); sin[i] = (float)DetMath.Sin(angle);
        }
        var writer = new MeshWriter(material);
        foreach (var band in bands)
        {
            if (band.R0 == 0 && band.R1 == 0) continue;
            var ring0 = new int[segments]; var ring1 = new int[segments];
            for (var i = 0; i < segments; i++)
            {
                ring0[i] = writer.Vertex(new(band.R0 * cos[i], band.Y0, band.R0 * sin[i]), Radial(band.N0, cos[i], sin[i]), new(i / (float)segments, band.Y0));
                ring1[i] = writer.Vertex(new(band.R1 * cos[i], band.Y1, band.R1 * sin[i]), Radial(band.N1, cos[i], sin[i]), new(i / (float)segments, band.Y1));
            }
            for (var i = 0; i < segments; i++)
            {
                var j = (i + 1) % segments;
                // Outward: the band's own mean normal at this sector, so concave dishes wind correctly.
                var mid = Radial(new((band.N0.X + band.N1.X) * 0.5f, (band.N0.Y + band.N1.Y) * 0.5f), (cos[i] + cos[j]) * 0.5f, (sin[i] + sin[j]) * 0.5f);
                if (band.R0 == 0) writer.Triangle(ring0[i], ring1[i], ring1[j], mid);
                else if (band.R1 == 0) writer.Triangle(ring0[i], ring0[j], ring1[i], mid);
                else { writer.Triangle(ring0[i], ring0[j], ring1[j], mid); writer.Triangle(ring0[i], ring1[j], ring1[i], mid); }
            }
        }
        return writer.Finish();
    }

    private static Vector3 Radial(Vector2 n, float c, float s) => Xf.Unit(new(n.X * c, n.Y, n.X * s));

    private static Vector2 Normal2(float x, float y)
    {
        var length = Math.Sqrt((double)x * x + (double)y * y);
        return new((float)(x / length), (float)(y / length));
    }
}

/// <summary>One profile edge of a lathe: from (R0, Y0) to (R1, Y1) with normals (radial, up) at both ends.</summary>
internal readonly record struct LatheBand(float R0, float Y0, float R1, float Y1, Vector2 N0, Vector2 N1);

/// <summary>Minimal clockwise mesh writer for shapes the kernel has no constructor for (only the lathe).</summary>
internal sealed class MeshWriter(ToonSpec material)
{
    private readonly List<float> _position = [];
    private readonly List<float> _normal = [];
    private readonly List<float> _uv = [];
    private readonly List<int> _index = [];

    public int Vertex(Vector3 p, Vector3 n, Vector2 uv)
    {
        _position.Add(p.X); _position.Add(p.Y); _position.Add(p.Z);
        _normal.Add(n.X); _normal.Add(n.Y); _normal.Add(n.Z);
        _uv.Add(uv.X); _uv.Add(uv.Y);
        return _position.Count / 3 - 1;
    }

    public void Triangle(int a, int b, int c, Vector3 outward)
    {
        Vector3 P(int i) => new(_position[3 * i], _position[3 * i + 1], _position[3 * i + 2]);
        var pa = P(a); var pb = P(b); var pc = P(c);
        var u = new Vector3(pb.X - pa.X, pb.Y - pa.Y, pb.Z - pa.Z); var v = new Vector3(pc.X - pa.X, pc.Y - pa.Y, pc.Z - pa.Z);
        var cross = new Vector3(u.Y * v.Z - u.Z * v.Y, u.Z * v.X - u.X * v.Z, u.X * v.Y - u.Y * v.X);
        var direction = (double)cross.X * outward.X + (double)cross.Y * outward.Y + (double)cross.Z * outward.Z;
        if (!double.IsFinite(direction) || direction == 0) throw new ArgumentException("A face is degenerate.");
        // Godot's front face is clockwise when viewed from outside.
        _index.Add(a); _index.Add(direction < 0 ? b : c); _index.Add(direction < 0 ? c : b);
    }

    public PartGeometry Finish()
    {
        if (_index.Count == 0) throw new InvalidOperationException("A shape must contain geometry.");
        float[] p = [.. _position];
        var min = new Vector3(p[0], p[1], p[2]); var max = min;
        for (var i = 0; i < p.Length; i += 3)
        {
            min = new(Math.Min(min.X, p[i]), Math.Min(min.Y, p[i + 1]), Math.Min(min.Z, p[i + 2]));
            max = new(Math.Max(max.X, p[i]), Math.Max(max.Y, p[i + 1]), Math.Max(max.Z, p[i + 2]));
        }
        var mesh = new GeometryBundle(new ToonRef(material), p, [.. _normal], [.. _index], Uv: [.. _uv]);
        mesh.ValidateVertexData();
        return new([mesh], new(min, max));
    }
}

/// <summary>Shared LOD table and bounds helpers.</summary>
internal static class Levels
{
    /// <summary>One entry per listed level; levels left out are not drawn (far levels may be omitted, ART-MODELING).</summary>
    public static IReadOnlyDictionary<LodLevel, Func<T, PartGeometry>> Of<T>(Func<T, LodLevel, PartGeometry> build, params LodLevel[] levels)
    {
        var table = new SortedDictionary<LodLevel, Func<T, PartGeometry>>();
        foreach (var level in levels) table.Add(level, p => build(p, level));
        return new ReadOnlyDictionary<LodLevel, Func<T, PartGeometry>>(table);
    }

    public static Box3 Padded(Vector3 min, Vector3 max)
    {
        var pad = RoofStyle.BoundsPad;
        return new(new(min.X - pad, min.Y - pad, min.Z - pad), new(max.X + pad, max.Y + pad, max.Z + pad));
    }

    public static void Positive(float value, string name)
    {
        if (!float.IsFinite(value) || value <= 0) throw new ArgumentOutOfRangeException(name, "A dimension must be finite and positive.");
    }
}
