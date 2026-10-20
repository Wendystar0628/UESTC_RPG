using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Parts.Core;
using Campus.Gen.Geometry;

namespace Campus.Gen.Sites;

/// <summary>
/// Small closed solids the WP-09 kernel has no helper for: smooth round shafts, hipped pyramids, arch outlines, and the
/// circle and arc paths the kernel's sweep takes (basin rims, curved bays).
/// Same conventions as the kernel: metres, origin at the base centre, Godot front faces clockwise from outside,
/// AO visibility 1, UV in metres. Angles are in radians.
/// </summary>
public static class Shapes
{
    /// <summary>A vertical shaft from y = 0 to height, radius r0 at the foot and r1 at the top, smooth sides, flat caps.</summary>
    public static PartGeometry Shaft(float r0, float r1, float height, int segments, ToonSpec material)
    {
        if (segments < 6) throw new ArgumentOutOfRangeException(nameof(segments));
        var m = new Writer(material);
        var slope = (r0 - r1) / height;
        var foot = new int[segments + 1];
        var head = new int[segments + 1];
        for (var i = 0; i <= segments; i++)
        {
            var a = 2f * MathF.PI * (i % segments) / segments;
            float c = MathF.Cos(a), s = MathF.Sin(a);
            var n = Normalize(new(c, slope, s));
            var u = 2f * MathF.PI * r0 * i / segments;
            foot[i] = m.Vertex(new(c * r0, 0, s * r0), n, new(u, 0));
            head[i] = m.Vertex(new(c * r1, height, s * r1), n, new(u, height));
        }
        for (var i = 0; i < segments; i++)
            m.Quad(foot[i], foot[i + 1], head[i + 1], head[i], Mid(m, foot[i], head[i + 1]));
        Cap(m, r0, 0, segments, -1);
        Cap(m, r1, height, segments, 1);
        return m.Finish();
    }

    /// <summary>A hipped (pyramid) roof over a w × d rectangle from y = 0 to the apex; a flat ridge when ridge > 0 along X.</summary>
    public static PartGeometry Hip(float width, float depth, float rise, float ridge, ToonSpec material)
    {
        var m = new Writer(material);
        float hx = width * 0.5f, hz = depth * 0.5f, rx = ridge * 0.5f;
        Vector3 a = new(-hx, 0, -hz), b = new(hx, 0, -hz), c = new(hx, 0, hz), d = new(-hx, 0, hz);
        Vector3 r0 = new(-rx, rise, 0), r1 = new(rx, rise, 0);
        var inside = new Vector3(0, rise * 0.3f, 0);
        Face(m, [a, b, r1, r0], inside);
        Face(m, [c, d, r0, r1], inside);
        Face(m, [b, c, r1], inside);
        Face(m, [d, a, r0], inside);
        Face(m, [a, d, c, b], inside);
        return m.Finish();
    }

    /// <summary>Outline of a round-headed opening in a wall's local (X, Y): width w, springing at y0 + spring, semicircle on top.</summary>
    public static Vector2[] RoundArch(float centreX, float y0, float width, float spring, int segments)
    {
        var r = width * 0.5f;
        var points = new List<Vector2> { new(centreX - r, y0), new(centreX + r, y0) };
        for (var i = 0; i <= segments; i++)
        {
            var a = MathF.PI * i / segments;
            points.Add(new(centreX + r * MathF.Cos(a), y0 + spring + r * MathF.Sin(a)));
        }
        return [.. points];
    }

    /// <summary>Closed plan ring of a circle; the angle runs from +x toward +z, so a sweep's right side faces outward.</summary>
    public static Vector2[] Circle(Vector2 centre, float radius, int segments)
    {
        if (segments < 6) throw new ArgumentOutOfRangeException(nameof(segments));
        var points = new Vector2[segments];
        for (var i = 0; i < segments; i++)
        {
            var a = 2f * MathF.PI * i / segments;
            points[i] = new(centre.X + radius * MathF.Cos(a), centre.Y + radius * MathF.Sin(a));
        }
        return points;
    }

    /// <summary>
    /// Open plan arc from <paramref name="a"/> to <paramref name="b"/> bulging <paramref name="sagitta"/> metres toward
    /// <paramref name="side"/> (a unit plan direction across the chord), ends included.
    /// </summary>
    public static Vector2[] Arc(Vector2 a, Vector2 b, float sagitta, Vector2 side, int segments)
    {
        if (segments < 2 || !(sagitta > 0)) throw new ArgumentOutOfRangeException(nameof(sagitta));
        var half = Vector2.Distance(a, b) * 0.5f;
        var radius = (half * half + sagitta * sagitta) / (2f * sagitta);
        var centre = (a + b) * 0.5f - side * (radius - sagitta);
        var start = MathF.Atan2(a.Y - centre.Y, a.X - centre.X);
        var end = MathF.Atan2(b.Y - centre.Y, b.X - centre.X);
        var sweep = end - start;
        // Take the short way round, which is the side the bulge faces for a sagitta below the radius.
        if (sweep > MathF.PI) sweep -= 2f * MathF.PI;
        if (sweep < -MathF.PI) sweep += 2f * MathF.PI;
        var points = new Vector2[segments + 1];
        for (var i = 0; i <= segments; i++)
        {
            var t = start + sweep * i / segments;
            points[i] = new(centre.X + radius * MathF.Cos(t), centre.Y + radius * MathF.Sin(t));
        }
        points[0] = a;
        points[segments] = b;
        return points;
    }

    /// <summary>
    /// The same geometry with every triangle on its own three vertices and a flat normal, turned to agree with the source
    /// normals: broad planar facets for rough stone (ART-MODELING faceted surfaces).
    /// </summary>
    public static PartGeometry Faceted(PartGeometry geometry)
    {
        ArgumentNullException.ThrowIfNull(geometry);
        var sink = new GeometrySink();
        foreach (var mesh in geometry.Meshes)
            for (var t = 0; t < mesh.Index.Length; t += 3)
            {
                var vertices = new GeometryVertex[3];
                var smooth = Vector3.Zero;
                for (var k = 0; k < 3; k++)
                {
                    var v = mesh.Index[t + k];
                    var p = new Vector3(mesh.Position[3 * v], mesh.Position[3 * v + 1], mesh.Position[3 * v + 2]);
                    var n = new Vector3(mesh.Normal[3 * v], mesh.Normal[3 * v + 1], mesh.Normal[3 * v + 2]);
                    smooth += n;
                    vertices[k] = new(p, n, mesh.Uv is { } uv ? new Vector2(uv[2 * v], uv[2 * v + 1]) : null,
                        mesh.Uv2 is { } uv2 ? new Vector2(uv2[2 * v], uv2[2 * v + 1]) : null,
                        mesh.Ao?[v], mesh.ColorIndices?[v], mesh.Flags?[v], mesh.Reserved?[v],
                        mesh.DecalBaseColorIndices?[v]);
                }
                var normal = Vector3.Cross(vertices[1].Position - vertices[0].Position, vertices[2].Position - vertices[0].Position);
                if (Vector3.Dot(normal, smooth) < 0) normal = -normal;
                normal = Normalize(normal.LengthSquared() > 1e-20f ? normal : smooth);
                sink.Tri(mesh.Material.Spec, vertices[0] with { Normal = normal },
                    vertices[1] with { Normal = normal }, vertices[2] with { Normal = normal });
            }
        return new(sink.Finish(), geometry.Bounds);
    }

    /// <summary>A ring wall of rectangular section swept round a circle (basin rims, kerbs), base at y = 0.</summary>
    public static PartGeometry Ring(Vector2 centre, float radius, float width, float height, int segments, ToonSpec material, float bevel = 0.02f) =>
        PartSweeps.Sweep(PartSweeps.BeveledRectangleProfile(width, height, bevel), Circle(centre, radius, segments), material, closed: true);

    private static void Cap(Writer m, float r, float y, int segments, float sign)
    {
        if (r <= 0) return;
        var normal = new Vector3(0, sign, 0);
        var centre = m.Vertex(new(0, y, 0), normal, Vector2.Zero);
        var ring = new int[segments];
        for (var i = 0; i < segments; i++)
        {
            var a = 2f * MathF.PI * i / segments;
            float c = MathF.Cos(a), s = MathF.Sin(a);
            ring[i] = m.Vertex(new(c * r, y, s * r), normal, new(c * r, s * r));
        }
        for (var i = 0; i < segments; i++) m.Triangle(centre, ring[i], ring[(i + 1) % segments], normal);
    }

    /// <summary>A planar convex face of a convex solid; its normal points away from a point inside the solid.</summary>
    private static void Face(Writer m, Vector3[] p, Vector3 inside)
    {
        var e1 = p[1] - p[0];
        var e2 = p[2] - p[0];
        var n = Normalize(new(e1.Y * e2.Z - e1.Z * e2.Y, e1.Z * e2.X - e1.X * e2.Z, e1.X * e2.Y - e1.Y * e2.X));
        var away = p[0] - inside;
        if (n.X * away.X + n.Y * away.Y + n.Z * away.Z < 0) n = -n;
        var ids = new int[p.Length];
        for (var i = 0; i < p.Length; i++) ids[i] = m.Vertex(p[i], n, new(p[i].X + p[i].Z, p[i].Y));
        for (var i = 1; i + 1 < p.Length; i++) m.Triangle(ids[0], ids[i], ids[i + 1], n);
    }

    private static Vector3 Mid(Writer m, int a, int b) => m.Normal(a) + m.Normal(b);

    private static Vector3 Normalize(Vector3 v)
    {
        var length = MathF.Sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z);
        return new(v.X / length, v.Y / length, v.Z / length);
    }

    /// <summary>Site-facing indexed construction uses the shared part mesh publication path.</summary>
    public sealed class Writer(ToonSpec material)
    {
        private readonly PartShapeWriter _mesh = new(material);
        public int Vertex(Vector3 p, Vector3 n, Vector2 uv) => _mesh.Vertex(p, n, uv);
        public Vector3 Normal(int i) => _mesh.Normal(i);
        public void Triangle(int a, int b, int c, Vector3 outward) => _mesh.Triangle(a, b, c, outward);
        public void Quad(int a, int b, int c, int d, Vector3 outward) => _mesh.Quad(a, b, c, d, outward);
        public PartGeometry Finish() => _mesh.Finish();
    }
}
