using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.Parts.Core;

/// <summary>One profile edge of a lathe: from (R0, Y0) to (R1, Y1) with (radial, up) normals at both ends.</summary>
public readonly record struct LatheBand(float R0, float Y0, float R1, float Y1, Vector2 N0, Vector2 N1)
{
    /// <summary>A crease-bounded band whose two ends share the edge's own outward normal.</summary>
    public static LatheBand Flat(float r0, float y0, float r1, float y1)
    {
        var n = VectorMath.Unit(new Vector2(y1 - y0, -(r1 - r0)));
        return new(r0, y0, r1, y1, n, n);
    }
}

/// <summary>Primitive shapes built on the game/gen/parts/core/ kernel. All are closed, clockwise and in metres.</summary>
public static class PartShapes
{
    /// <summary>Box from its minimum and maximum corners; the chamfer is clamped to stay valid. <paramref name="at"/> places it.</summary>
    public static PartGeometry Box(Vector3 min, Vector3 max, float bevel, ToonSpec material, out Matrix4x4 at)
    {
        var size = new Vector3(max.X - min.X, max.Y - min.Y, max.Z - min.Z);
        var limit = Math.Min(size.X, Math.Min(size.Y, size.Z)) * 0.45f;
        at = Xf.Move((min.X + max.X) * 0.5f, min.Y, (min.Z + max.Z) * 0.5f);
        return PartSolids.BeveledBox(size.X, size.Y, size.Z, Math.Min(bevel, limit), material);
    }

    /// <summary>Box from its minimum and maximum corners; the chamfer is clamped to stay valid.</summary>
    public static PartBuilder AddBox(this PartBuilder builder, Vector3 min, Vector3 max, float bevel, ToonSpec material, byte flags = 0)
    {
        var piece = Box(min, max, bevel, material, out var at);
        return builder.Add(piece, at, flags);
    }

    /// <summary>Box centred on the origin in all three axes, then moved by <paramref name="then"/>.</summary>
    public static PartBuilder AddCentredBox(this PartBuilder builder, Vector3 size, Matrix4x4 then, ToonSpec material, float bevel = 0, byte flags = 0)
    {
        var limit = Math.Min(size.X, Math.Min(size.Y, size.Z)) * 0.45f;
        var piece = PartSolids.BeveledBox(size.X, size.Y, size.Z, Math.Min(bevel, limit), material);
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
        if (PlanRings.SignedArea(ring) < 0) ring.Reverse();
        var piece = PartSolids.BeveledExtrude(ring, x1 - x0, bevel, material, -x1);
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
        if (PlanRings.SignedArea(ring) < 0) ring.Reverse();
        var piece = PartSolids.BeveledExtrude(ring, z1 - z0, bevel, material, -z1);
        return builder.Add(piece, Xf.RotX(-90f), flags);
    }

    /// <summary>A closed tetrahedron with flat faces (corners in any order): the flared wing of a kerb ramp.</summary>
    public static PartGeometry Tetra(Vector3 a, Vector3 b, Vector3 c, Vector3 d, ToonSpec material)
    {
        var writer = new PartShapeWriter(material);
        var centre = (a + b + c + d) * 0.25f;
        foreach (var (p, q, r) in new[] { (a, b, c), (a, b, d), (a, c, d), (b, c, d) })
        {
            var n = VectorMath.Unit(Vector3.Cross(q - p, r - p));
            if (Vector3.Dot(n, p - centre) < 0) n = -n;
            writer.Triangle(writer.Vertex(p, n, Vector2.Zero), writer.Vertex(q, n, Vector2.Zero), writer.Vertex(r, n, Vector2.Zero), n);
        }
        return writer.Finish();
    }

    /// <summary>Counterclockwise circle section with radial smooth normals, starting on the +lateral axis.</summary>
    public static SweepProfile Circle(float radius, int segments, Vector2 centre = default)
    {
        if (segments < 3) throw new ArgumentOutOfRangeException(nameof(segments));
        var points = new Vector2[segments]; var normals = new Vector2[segments];
        for (var i = 0; i < segments; i++)
        {
            var angle = 2.0 * Math.PI * i / segments;
            var c = (float)Math.Cos(angle); var s = (float)Math.Sin(angle);
            points[i] = new(centre.X + radius * c, centre.Y + radius * s);
            normals[i] = VectorMath.Unit(new Vector2(c, s));
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
        var source = PartSweeps.BeveledRectangleProfile(width, height, Math.Min(bevel, limit));
        var points = source.Points.Select(p => new Vector2(p.X + lateralShift, p.Y + upShift)).ToArray();
        return new(Array.AsReadOnly(points), source.Normals);
    }

    /// <summary>Round pipe between two points (smooth normals, flat end caps).</summary>
    public static PartBuilder AddPipe(this PartBuilder builder, Vector3 a, Vector3 b, float radius, int segments, ToonSpec material, byte flags = 0)
    {
        var delta = new Vector3(b.X - a.X, b.Y - a.Y, b.Z - a.Z);
        var length = (float)Math.Sqrt((double)delta.X * delta.X + (double)delta.Y * delta.Y + (double)delta.Z * delta.Z);
        var piece = PartSweeps.Sweep(Circle(radius, segments), [Vector2.Zero, new(length, 0)], material);
        return builder.Add(piece, Xf.AlongX(delta, a), flags);
    }

    /// <summary>Vertical cylinder standing on (x, y0, z).</summary>
    public static PartBuilder AddColumn(this PartBuilder builder, float x, float y0, float y1, float z, float radius, int segments, ToonSpec material, byte flags = 0)
    {
        var piece = PartSweeps.Sweep(Circle(radius, segments), [Vector2.Zero, new(y1 - y0, 0)], material);
        return builder.Add(piece, Xf.XUp.Then(Xf.Move(x, y0, z)), flags);
    }

    /// <summary>Thin disc facing −Z (its axis along Z), from zBack to zFront.</summary>
    public static PartBuilder AddDisc(this PartBuilder builder, float x, float y, float zFront, float zBack, float radius, int segments, ToonSpec material, byte flags = 0) =>
        builder.AddPipe(new(x, y, zBack), new(x, y, zFront), radius, segments, material, flags);

    /// <summary>
    /// One flat, single-sided quad (corners in order) facing <paramref name="normal"/>: face geometry for print and pictograms
    /// (ART-MODELING), not a solid. Two triangles.
    /// </summary>
    public static PartGeometry FlatQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal, ToonSpec material)
    {
        var writer = new PartShapeWriter(material);
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
            cos[i] = (float)Math.Cos(angle); sin[i] = (float)Math.Sin(angle);
        }
        var writer = new PartShapeWriter(material);
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
    /// Closed lathe profile through <paramref name="points"/> (radius, height), counter-clockwise: rings and washers whose profile
    /// does not touch the axis (<see cref="Creased"/> only runs axis to axis). Every corner is a crease.
    /// </summary>
    public static LatheBand[] Loop(params Vector2[] points)
    {
        var bands = new LatheBand[points.Length];
        for (var i = 0; i < points.Length; i++)
        {
            var a = points[i]; var b = points[(i + 1) % points.Length];
            bands[i] = LatheBand.Flat(a.X, a.Y, b.X, b.Y);
        }
        return bands;
    }

    /// <summary>
    /// Open lathe profile through <paramref name="points"/> (radius, height) with smooth shading: interior vertices average their two
    /// edges' normals, the first and last keep their own edge's. Add creased bands at the ends (flat caps, rims) with <see cref="LatheBand.Flat"/>.
    /// </summary>
    public static List<LatheBand> Smooth(params Vector2[] points)
    {
        static Vector2 Edge(Vector2 a, Vector2 b) => VectorMath.Unit(new Vector2(b.Y - a.Y, -(b.X - a.X)));
        var bands = new List<LatheBand>(points.Length - 1);
        for (var i = 0; i + 1 < points.Length; i++)
        {
            var a = points[i]; var b = points[i + 1];
            var n0 = i == 0 ? Edge(a, b) : VectorMath.Unit(Edge(points[i - 1], a) + Edge(a, b));
            var n1 = i + 2 == points.Length ? Edge(a, b) : VectorMath.Unit(Edge(a, b) + Edge(b, points[i + 2]));
            bands.Add(new(a.X, a.Y, b.X, b.Y, n0, n1));
        }
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
            var c0 = (float)Math.Cos(a0); var s0 = (float)Math.Sin(a0);
            var c1 = (float)Math.Cos(a1); var s1 = (float)Math.Sin(a1);
            // Snap the poles onto the axis exactly so the fan closes.
            var r0 = Math.Abs(c0) < 1e-6f ? 0 : radius * c0; var r1 = Math.Abs(c1) < 1e-6f ? 0 : radius * c1;
            yield return new(r0, centreY + radius * squash * s0, r1, centreY + radius * squash * s1,
                VectorMath.Unit(new Vector2(c0 * squash, s0)), VectorMath.Unit(new Vector2(c1 * squash, s1)));
        }
    }

    private static Vector3 Radial(Vector2 n, float c, float s) => VectorMath.Unit(new Vector3(n.X * c, n.Y, n.X * s));
}
