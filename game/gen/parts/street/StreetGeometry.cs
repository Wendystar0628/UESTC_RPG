using System.Collections.ObjectModel;
using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Parts.Core;

namespace Campus.Gen.Parts.Street;

/// <summary>Rigid row-vector transforms written as scalar arithmetic so bakes stay bitwise stable (D-165).</summary>
public static class Xf
{
    /// <summary>Local +X becomes world +Y (pieces swept along X stand up).</summary>
    public static readonly Matrix4x4 XUp = new(0, 1, 0, 0, -1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1);

    public static Matrix4x4 Move(float x, float y, float z) => new(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, x, y, z, 1);
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

    /// <summary>Right-handed frame whose local +X follows <paramref name="axis"/>; lays swept pipes along any direction.</summary>
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
/// Kernel AO is visibility (1 = unoccluded) and passes straight through; a bundle that stays fully unoccluded stores no AO.
/// Groups keep first-use order, so output never depends on hash ordering. Pieces of one material that touch must overlap
/// by a few millimetres instead of sharing an edge, so every merged surface stays a set of closed solids.
/// </summary>
public sealed class PartBuilder
{
    private readonly List<Group> _groups = [];

    public PartBuilder Add(PartGeometry piece, Matrix4x4 at, byte flags = 0)
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
                var visibility = mesh.Ao?[i] ?? 1f;
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
public static class VertexFlag
{
    /// <summary>Pieces under 0.25 m (and thin inserts) stay out of the shadow pass (ART-MODELING).</summary>
    public const byte NoShadow = 8;
}

/// <summary>One profile edge of a lathe: from (R0, Y0) to (R1, Y1) with (radial, up) normals at both ends.</summary>
public readonly record struct LatheBand(float R0, float Y0, float R1, float Y1, Vector2 N0, Vector2 N1)
{
    /// <summary>A crease-bounded band whose two ends share the edge's own outward normal.</summary>
    public static LatheBand Flat(float r0, float y0, float r1, float y1)
    {
        var n = Normal2(y1 - y0, -(r1 - r0));
        return new(r0, y0, r1, y1, n, n);
    }

    internal static Vector2 Normal2(float x, float y)
    {
        var length = Math.Sqrt((double)x * x + (double)y * y);
        if (!(length > 1e-12)) throw new ArgumentException("A lathe edge must have length.");
        return new((float)(x / length), (float)(y / length));
    }
}

/// <summary>Primitive shapes built on the WP-09 kernel. All are closed, clockwise and in metres.</summary>
public static class Shapes
{
    /// <summary>Box from its minimum and maximum corners; the chamfer is clamped to stay valid.</summary>
    public static PartBuilder AddBox(this PartBuilder builder, Vector3 min, Vector3 max, float bevel, ToonSpec material, byte flags = 0)
    {
        var size = new Vector3(max.X - min.X, max.Y - min.Y, max.Z - min.Z);
        var limit = Math.Min(size.X, Math.Min(size.Y, size.Z)) * 0.45f;
        var piece = PartGeometryKernel.BeveledBox(size.X, size.Y, size.Z, Math.Min(bevel, limit), material);
        return builder.Add(piece, Xf.Move((min.X + max.X) * 0.5f, min.Y, (min.Z + max.Z) * 0.5f), flags);
    }

    /// <summary>Box centred on the origin in all three axes, then moved by <paramref name="then"/>.</summary>
    public static PartBuilder AddCentredBox(this PartBuilder builder, Vector3 size, Matrix4x4 then, ToonSpec material, float bevel = 0, byte flags = 0)
    {
        var limit = Math.Min(size.X, Math.Min(size.Y, size.Z)) * 0.45f;
        var piece = PartGeometryKernel.BeveledBox(size.X, size.Y, size.Z, Math.Min(bevel, limit), material);
        return builder.Add(piece, Xf.Move(0, -size.Y * 0.5f, 0).Then(then), flags);
    }

    /// <summary>
    /// A convex side profile, points (z, y) along the part's length and up, extruded across X from <paramref name="x0"/> to
    /// <paramref name="x1"/> with single-band chamfers on both side faces (car bodies, cabins, arrow boards). Either winding is taken.
    /// </summary>
    public static PartBuilder AddProfile(this PartBuilder builder, IReadOnlyList<Vector2> profile, float x0, float x1, float bevel, ToonSpec material, byte flags = 0)
    {
        // The kernel extrudes a footprint (x, z) up Y; footprint (y, z) extruded up local Y, turned by XUp, runs across −X.
        var ring = profile.Select(p => new Vector2(p.Y, p.X)).ToList();
        var area = 0.0;
        for (var i = 0; i < ring.Count; i++)
        {
            var a = ring[i]; var b = ring[(i + 1) % ring.Count];
            area += (double)a.X * b.Y - (double)b.X * a.Y;
        }
        if (area < 0) ring.Reverse();
        var piece = PartGeometryKernel.BeveledExtrude(ring, x1 - x0, bevel, material, -x1);
        return builder.Add(piece, Xf.XUp, flags);
    }

    /// <summary>
    /// A convex face outline, points (x, y) on the front plane, extruded along Z from <paramref name="z0"/> to <paramref name="z1"/>
    /// with single-band chamfers on its front and back faces (arrow boards). Either winding is taken.
    /// </summary>
    public static PartBuilder AddPlate(this PartBuilder builder, IReadOnlyList<Vector2> outline, float z0, float z1, float bevel, ToonSpec material, byte flags = 0)
    {
        // Footprint (x, y) extruded up local Y, turned by RotX(−90): local Z becomes world Y, local Y becomes world −Z.
        var ring = outline.ToList();
        var area = 0.0;
        for (var i = 0; i < ring.Count; i++)
        {
            var a = ring[i]; var b = ring[(i + 1) % ring.Count];
            area += (double)a.X * b.Y - (double)b.X * a.Y;
        }
        if (area < 0) ring.Reverse();
        var piece = PartGeometryKernel.BeveledExtrude(ring, z1 - z0, bevel, material, -z1);
        return builder.Add(piece, Xf.RotX(-90f), flags);
    }

    /// <summary>A closed tetrahedron with flat faces (corners in any order): the flared wing of a kerb ramp.</summary>
    public static PartGeometry Tetra(Vector3 a, Vector3 b, Vector3 c, Vector3 d, ToonSpec material)
    {
        var writer = new MeshWriter(material);
        var centre = (a + b + c + d) * 0.25f;
        foreach (var (p, q, r) in new[] { (a, b, c), (a, b, d), (a, c, d), (b, c, d) })
        {
            var n = Xf.Unit(Vector3.Cross(q - p, r - p));
            if (Vector3.Dot(n, p - centre) < 0) n = -n;
            writer.Triangle(writer.Vertex(p, n, Vector2.Zero), writer.Vertex(q, n, Vector2.Zero), writer.Vertex(r, n, Vector2.Zero), n);
        }
        return writer.Finish();
    }

    /// <summary>Counterclockwise circle section with radial smooth normals, starting on the +lateral axis.</summary>
    public static SweepProfile Circle(float radius, int segments)
    {
        if (segments < 3) throw new ArgumentOutOfRangeException(nameof(segments));
        var points = new Vector2[segments]; var normals = new Vector2[segments];
        for (var i = 0; i < segments; i++)
        {
            var angle = 2.0 * Math.PI * i / segments;
            var c = (float)DetMath.Cos(angle); var s = (float)DetMath.Sin(angle);
            points[i] = new(radius * c, radius * s);
            normals[i] = LatheBand.Normal2(c, s);
        }
        return new(Array.AsReadOnly(points), Array.AsReadOnly(normals));
    }

    /// <summary>Round pipe between two points (smooth normals, flat end caps).</summary>
    public static PartBuilder AddPipe(this PartBuilder builder, Vector3 a, Vector3 b, float radius, int segments, ToonSpec material, byte flags = 0)
    {
        var delta = new Vector3(b.X - a.X, b.Y - a.Y, b.Z - a.Z);
        var length = (float)Math.Sqrt((double)delta.X * delta.X + (double)delta.Y * delta.Y + (double)delta.Z * delta.Z);
        var piece = PartGeometryKernel.Sweep(Circle(radius, segments), [Vector2.Zero, new(length, 0)], material);
        return builder.Add(piece, Xf.AlongX(delta, a), flags);
    }

    /// <summary>
    /// One flat, single-sided quad (corners in order) facing <paramref name="normal"/>: face geometry for print and pictograms
    /// (ART-MODELING), not a solid. Two triangles.
    /// </summary>
    public static PartGeometry FlatQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal, ToonSpec material)
    {
        var writer = new MeshWriter(material);
        var ia = writer.Vertex(a, normal, new(0, 0)); var ib = writer.Vertex(b, normal, new(1, 0));
        var ic = writer.Vertex(c, normal, new(1, 1)); var id = writer.Vertex(d, normal, new(0, 1));
        writer.Triangle(ia, ib, ic, normal); writer.Triangle(ia, ic, id, normal);
        return writer.Finish();
    }

    /// <summary>Solid of revolution about +Y, moved to <paramref name="at"/>.</summary>
    public static PartBuilder AddLathe(this PartBuilder builder, IReadOnlyList<LatheBand> bands, int segments, ToonSpec material, Matrix4x4 at, byte flags = 0) =>
        builder.Add(Lathe(bands, segments, material), at, flags);

    /// <summary>
    /// Closed solid of revolution about +Y. Each band is a profile edge (r, y) with its own end normals, so creases (rims)
    /// stay hard and curved faces stay smooth. Bands form one loop that starts and ends on the axis (r = 0).
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
                var mid = Radial(new((band.N0.X + band.N1.X) * 0.5f, (band.N0.Y + band.N1.Y) * 0.5f), (cos[i] + cos[j]) * 0.5f, (sin[i] + sin[j]) * 0.5f);
                if (band.R0 == 0) writer.Triangle(ring0[i], ring1[i], ring1[j], mid);
                else if (band.R1 == 0) writer.Triangle(ring0[i], ring0[j], ring1[i], mid);
                else { writer.Triangle(ring0[i], ring0[j], ring1[j], mid); writer.Triangle(ring0[i], ring1[j], ring1[i], mid); }
            }
        }
        return writer.Finish();
    }

    /// <summary>
    /// Profile of a capped cylinder stack: points (r, y) from the bottom axis up to the top axis, every corner a crease.
    /// Convenience for poles, sleeves, discs and caps built from straight edges only.
    /// </summary>
    public static IReadOnlyList<LatheBand> Creased(params Vector2[] points)
    {
        if (points.Length < 3 || points[0].X != 0 || points[^1].X != 0) throw new ArgumentException("A creased profile runs from axis to axis.", nameof(points));
        var bands = new LatheBand[points.Length - 1];
        for (var i = 0; i + 1 < points.Length; i++) bands[i] = LatheBand.Flat(points[i].X, points[i].Y, points[i + 1].X, points[i + 1].Y);
        return bands;
    }

    /// <summary>
    /// Arc bands on a circle about (0, <paramref name="centreY"/>) from angle <paramref name="fromDeg"/> to <paramref name="toDeg"/>
    /// (−90 = bottom pole, 90 = top pole), with radial smooth normals; <paramref name="squash"/> scales the arc vertically.
    /// </summary>
    public static IEnumerable<LatheBand> Arc(float radius, float centreY, float fromDeg, float toDeg, int steps, float squash = 1f)
    {
        if (steps < 1) throw new ArgumentOutOfRangeException(nameof(steps));
        for (var k = 0; k < steps; k++)
        {
            var a0 = (fromDeg + (toDeg - fromDeg) * k / steps) * Math.PI / 180.0;
            var a1 = (fromDeg + (toDeg - fromDeg) * (k + 1) / steps) * Math.PI / 180.0;
            var c0 = (float)DetMath.Cos(a0); var s0 = (float)DetMath.Sin(a0);
            var c1 = (float)DetMath.Cos(a1); var s1 = (float)DetMath.Sin(a1);
            // Snap the poles onto the axis exactly so the fan closes.
            var r0 = Math.Abs(c0) < 1e-6f ? 0 : radius * c0; var r1 = Math.Abs(c1) < 1e-6f ? 0 : radius * c1;
            yield return new(r0, centreY + radius * squash * s0, r1, centreY + radius * squash * s1,
                LatheBand.Normal2(c0 * squash, s0), LatheBand.Normal2(c1 * squash, s1));
        }
    }

    private static Vector3 Radial(Vector2 n, float c, float s) => Xf.Unit(new(n.X * c, n.Y, n.X * s));
}

/// <summary>Minimal clockwise mesh writer for shapes the kernel has no constructor for (lathes).</summary>
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

/// <summary>Shared LOD table, bounds and validation helpers.</summary>
public static class Levels
{
    /// <summary>One entry per listed level; levels left out are not drawn (ART-MODELING: L2 keeps identity pieces only).</summary>
    public static IReadOnlyDictionary<LodLevel, Func<T, PartGeometry>> Of<T>(Func<T, LodLevel, PartGeometry> build, params LodLevel[] levels)
    {
        var table = new SortedDictionary<LodLevel, Func<T, PartGeometry>>();
        foreach (var level in levels) table.Add(level, p => build(p, level));
        return new ReadOnlyDictionary<LodLevel, Func<T, PartGeometry>>(table);
    }

    public static Box3 Padded(Vector3 min, Vector3 max)
    {
        var pad = StreetStyle.BoundsPad;
        return new(new(min.X - pad, min.Y - pad, min.Z - pad), new(max.X + pad, max.Y + pad, max.Z + pad));
    }

    public static void Positive(float value, string name)
    {
        if (!float.IsFinite(value) || value <= 0) throw new ArgumentOutOfRangeException(name, "A dimension must be finite and positive.");
    }

    public static void Dimensions(Campus.Contracts.Parts.BoxPartParams p)
    {
        ArgumentNullException.ThrowIfNull(p);
        Positive(p.Width, nameof(p.Width)); Positive(p.Height, nameof(p.Height)); Positive(p.Depth, nameof(p.Depth));
    }
}
