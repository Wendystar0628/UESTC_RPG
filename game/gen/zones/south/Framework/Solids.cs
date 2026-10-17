using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.Zones.South.Framework;

/// <summary>
/// Curved and sloped solids for the 18b landmarks: lathes (the dome, columns, balusters, the statue), regular-polygon
/// roofs with upturned eaves (the hexagonal pavilion) and a rowing-boat hull. Same conventions as <see cref="Shapes"/>:
/// metres, origin at the base centre, front faces clockwise from outside, AO 1, UV in metres, angles through DetMath.
/// </summary>
public static class Solids
{
    /// <summary>
    /// A surface of revolution about local Y. Profile points are (radius, y) from bottom to top; a zero radius closes the
    /// end to a point. Normals follow the profile (smooth, ART-MODELING); the section is scaled by (sx, sz) for an
    /// elliptical body. Flat caps close a nonzero first or last ring unless turned off (an open ring such as a bezel).
    /// </summary>
    public static PartGeometry Lathe(IReadOnlyList<Vector2> profile, int segments, ToonSpec material, float sx = 1f, float sz = 1f,
        bool capBottom = true, bool capTop = true)
    {
        ArgumentNullException.ThrowIfNull(profile);
        if (profile.Count < 2) throw new ArgumentException("A lathe needs at least two profile points.", nameof(profile));
        if (segments < 6) throw new ArgumentOutOfRangeException(nameof(segments));
        var m = new Shapes.Writer(material);
        var n = profile.Count;
        var normals = new Vector2[n];
        for (var k = 0; k < n; k++)
        {
            if (profile[k].X <= 1e-5f && (k == 0 || k == n - 1)) { normals[k] = new(0, k == 0 ? -1 : 1); continue; }
            var t = profile[Math.Min(n - 1, k + 1)] - profile[Math.Max(0, k - 1)];
            var nn = new Vector2(t.Y, -t.X);
            var length = MathF.Sqrt(nn.X * nn.X + nn.Y * nn.Y);
            normals[k] = length > 1e-9f ? nn / length : new Vector2(1, 0);
        }
        var rings = new int[n][];
        var arc = 0f;
        for (var k = 0; k < n; k++)
        {
            if (k > 0) arc += Vector2.Distance(profile[k], profile[k - 1]);
            rings[k] = new int[segments + 1];
            for (var i = 0; i <= segments; i++)
            {
                var a = 2f * MathF.PI * (i % segments) / segments;
                float c = DetMath.Cos(a), s = DetMath.Sin(a);
                var r = profile[k].X;
                var normal = Unit(new(c * normals[k].X / sx, normals[k].Y, s * normals[k].X / sz));
                rings[k][i] = m.Vertex(new(c * r * sx, profile[k].Y, s * r * sz), normal, new(2f * MathF.PI * i / segments, arc));
            }
        }
        for (var k = 0; k + 1 < n; k++)
            for (var i = 0; i < segments; i++)
                m.Quad(rings[k][i], rings[k][i + 1], rings[k + 1][i + 1], rings[k + 1][i], m.Normal(rings[k][i]) + m.Normal(rings[k + 1][i + 1]));
        if (capBottom && profile[0].X > 1e-5f) Cap(m, profile[0].X, profile[0].Y, segments, -1, sx, sz);
        if (capTop && profile[n - 1].X > 1e-5f) Cap(m, profile[n - 1].X, profile[n - 1].Y, segments, 1, sx, sz);
        return m.Finish();
    }

    /// <summary>
    /// A lathe whose radius also varies round the axis: radius(angle, y) = profile radius × (1 + <paramref name="fold"/>(angle, y)),
    /// for cloth that hangs in folds (the statue's coat skirt). Angle is in radians from local +X towards +Z. Normals come from
    /// the surface grid itself (central differences), so the folds shade as soft ridges; both ends are capped flat.
    /// </summary>
    public static PartGeometry Draped(IReadOnlyList<Vector2> profile, int segments, ToonSpec material, float sx, float sz, Func<float, float, float> fold)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(fold);
        if (profile.Count < 2) throw new ArgumentException("A lathe needs at least two profile points.", nameof(profile));
        if (segments < 6) throw new ArgumentOutOfRangeException(nameof(segments));
        var n = profile.Count;
        var p = new Vector3[n, segments];
        for (var k = 0; k < n; k++)
            for (var i = 0; i < segments; i++)
            {
                var a = 2f * MathF.PI * i / segments;
                var r = profile[k].X * (1f + fold(a, profile[k].Y));
                p[k, i] = new(DetMath.Cos(a) * r * sx, profile[k].Y, DetMath.Sin(a) * r * sz);
            }
        var m = new Shapes.Writer(material);
        var rings = new int[n][];
        var arc = 0f;
        for (var k = 0; k < n; k++)
        {
            if (k > 0) arc += Vector2.Distance(profile[k], profile[k - 1]);
            rings[k] = new int[segments + 1];
            for (var i = 0; i <= segments; i++)
            {
                var j = i % segments;
                var around = p[k, (j + 1) % segments] - p[k, (j + segments - 1) % segments];
                var up = p[Math.Min(n - 1, k + 1), j] - p[Math.Max(0, k - 1), j];
                var normal = Unit(new(up.Y * around.Z - up.Z * around.Y, up.Z * around.X - up.X * around.Z, up.X * around.Y - up.Y * around.X));
                if (normal.X * p[k, j].X + normal.Z * p[k, j].Z < 0) normal = -normal;
                rings[k][i] = m.Vertex(p[k, j], normal, new(2f * MathF.PI * profile[k].X * i / segments, arc));
            }
        }
        for (var k = 0; k + 1 < n; k++)
            for (var i = 0; i < segments; i++)
                m.Quad(rings[k][i], rings[k][i + 1], rings[k + 1][i + 1], rings[k + 1][i], m.Normal(rings[k][i]) + m.Normal(rings[k + 1][i + 1]));
        foreach (var (k, sign) in new[] { (0, -1f), (n - 1, 1f) })
        {
            var normal = new Vector3(0, sign, 0);
            var centre = m.Vertex(new(0, profile[k].Y, 0), normal, Vector2.Zero);
            var ring = new int[segments];
            for (var i = 0; i < segments; i++) ring[i] = m.Vertex(p[k, i], normal, new(p[k, i].X, p[k, i].Z));
            for (var i = 0; i < segments; i++) m.Triangle(centre, ring[i], ring[(i + 1) % segments], normal);
        }
        return m.Finish();
    }

    /// <summary>
    /// A regular roof of <paramref name="sides"/> slopes: from the eave ring (circumradius <paramref name="eave"/>, y 0) up to
    /// a ring of radius <paramref name="top"/> at <paramref name="rise"/>, or to an apex when top is 0. The eave line turns up
    /// towards every corner by <paramref name="upturn"/> (a parabola along each side, level at mid-side). The slab is
    /// <paramref name="thickness"/> thick with a soffit in <paramref name="soffit"/> and a fascia. Corner 0 sits at angle
    /// <paramref name="phase"/> (radians, from local +X towards +Z). Every slope keeps one flat normal, so the facets read as
    /// clean planes under two-step shading.
    /// </summary>
    public static PartGeometry PolyRoof(int sides, float eave, float top, float rise, float upturn, float thickness, int perSide,
        float phase, ToonSpec tiles, ToonSpec soffit)
    {
        if (sides < 3) throw new ArgumentOutOfRangeException(nameof(sides));
        if (perSide < 1) throw new ArgumentOutOfRangeException(nameof(perSide));
        var up = new Shapes.Writer(tiles);
        var down = new Shapes.Writer(soffit);
        Vector2 Corner(int i, float r)
        {
            var a = phase + 2f * MathF.PI * (i % sides) / sides;
            return new(r * DetMath.Cos(a), r * DetMath.Sin(a));
        }
        for (var i = 0; i < sides; i++)
        {
            Vector2 c0 = Corner(i, eave), c1 = Corner(i + 1, eave), t0 = Corner(i, top), t1 = Corner(i + 1, top);
            var mid = (c0 + c1) * 0.5f;
            var midTop = (t0 + t1) * 0.5f;
            // Slope plane through the level mid-eave and the top edge: one normal for the whole side.
            float ax = c1.X - c0.X, az = c1.Y - c0.Y;
            float ux = midTop.X - mid.X, uy = rise, uz = midTop.Y - mid.Y;
            var normal = Unit(new(uy * az, uz * ax - ux * az, -uy * ax));
            if (normal.Y < 0) normal = -normal;
            var outward = Unit(new(mid.X, 0, mid.Y));
            int[] eaveTop = new int[perSide + 1], eaveLow = new int[perSide + 1], ridgeTop = new int[perSide + 1], ridgeLow = new int[perSide + 1];
            int[] fasciaTop = new int[perSide + 1], fasciaLow = new int[perSide + 1];
            for (var j = 0; j <= perSide; j++)
            {
                var t = j / (float)perSide;
                var e = Vector2.Lerp(c0, c1, t);
                var lift = upturn * (2f * t - 1f) * (2f * t - 1f);
                var r = Vector2.Lerp(t0, t1, t);
                var uv = new Vector2(Vector2.Distance(c0, e), 0);
                eaveTop[j] = up.Vertex(new(e.X, lift, e.Y), normal, uv);
                ridgeTop[j] = up.Vertex(new(r.X, rise, r.Y), normal, uv + new Vector2(0, Vector2.Distance(mid, midTop)));
                eaveLow[j] = down.Vertex(new(e.X, lift - thickness, e.Y), -normal, uv);
                ridgeLow[j] = down.Vertex(new(r.X, rise - thickness, r.Y), -normal, uv);
                fasciaTop[j] = up.Vertex(new(e.X, lift, e.Y), outward, new(uv.X, thickness));
                fasciaLow[j] = up.Vertex(new(e.X, lift - thickness, e.Y), outward, uv);
            }
            for (var j = 0; j < perSide; j++)
            {
                up.Quad(eaveTop[j], eaveTop[j + 1], ridgeTop[j + 1], ridgeTop[j], normal);
                down.Quad(eaveLow[j], eaveLow[j + 1], ridgeLow[j + 1], ridgeLow[j], -normal);
                up.Quad(fasciaLow[j], fasciaLow[j + 1], fasciaTop[j + 1], fasciaTop[j], outward);
            }
        }
        var a = up.Finish();
        var b = down.Finish();
        return new([.. a.Meshes, .. b.Meshes], a.Bounds);
    }

    /// <summary>
    /// An open rowing-boat shell along local Z (bow at +Z, transom at −Z), keel at y 0: flat floor, flared sides, sheer rising
    /// towards the bow. Outside in <paramref name="hull"/>, inside and the transom's inner face in <paramref name="inside"/>,
    /// shell 0.04 m with a gunwale rim. Thwarts are the caller's.
    /// </summary>
    public static PartGeometry Hull(float length, float beam, float depth, ToonSpec hull, ToonSpec inside)
    {
        const int stations = 10;
        const float shell = 0.04f;
        var outer = new Shapes.Writer(hull);
        var inner = new Shapes.Writer(inside);
        // Section (lateral x, up y) at station parameter t: gunwale, chine, floor edge, mirrored.
        Vector2[] Section(float t, float inset)
        {
            var w = beam * 0.5f * (t <= 0.4f ? 0.72f + 0.28f * t / 0.4f : MathF.Sqrt(MathF.Max(0f, 1f - (t - 0.4f) * (t - 0.4f) / 0.36f)));
            var rocker = t > 0.6f ? 0.14f * (t - 0.6f) * (t - 0.6f) / 0.16f : 0f;
            var sheer = depth * (1f + 0.22f * t * t);
            w = MathF.Max(0f, w - inset);
            var floor = rocker + inset;
            return [new(-w, sheer), new(-0.82f * w, rocker + 0.32f * depth + inset * 0.5f), new(-0.45f * w, floor),
                new(0.45f * w, floor), new(0.82f * w, rocker + 0.32f * depth + inset * 0.5f), new(w, sheer)];
        }
        var outerRows = new int[stations + 1][];
        var innerRows = new int[stations + 1][];
        var rimOut = new int[stations + 1, 2];
        var rimIn = new int[stations + 1, 2];
        for (var k = 0; k <= stations; k++)
        {
            var t = k / (float)stations;
            var z = -length * 0.5f + length * t;
            var so = Section(t, 0f);
            var si = Section(t, shell);
            outerRows[k] = new int[so.Length];
            innerRows[k] = new int[si.Length];
            for (var j = 0; j < so.Length; j++)
            {
                var sn = SectionNormal(so, j);
                outerRows[k][j] = outer.Vertex(new(so[j].X, so[j].Y, z), new(sn.X, sn.Y, 0), new(z, so[j].Y));
                innerRows[k][j] = inner.Vertex(new(si[j].X, si[j].Y, z), new(-sn.X, -sn.Y, 0), new(z, si[j].Y));
            }
            for (var side = 0; side < 2; side++)
            {
                var j = side == 0 ? 0 : so.Length - 1;
                rimOut[k, side] = outer.Vertex(new(so[j].X, so[j].Y, z), Vector3.UnitY, new(z, so[j].X));
                rimIn[k, side] = outer.Vertex(new(si[j].X, so[j].Y, z), Vector3.UnitY, new(z, si[j].X));
            }
        }
        for (var k = 0; k < stations; k++)
        {
            for (var j = 0; j + 1 < outerRows[k].Length; j++)
            {
                outer.Quad(outerRows[k][j], outerRows[k + 1][j], outerRows[k + 1][j + 1], outerRows[k][j + 1], outer.Normal(outerRows[k][j]) + outer.Normal(outerRows[k + 1][j + 1]));
                inner.Quad(innerRows[k][j], innerRows[k][j + 1], innerRows[k + 1][j + 1], innerRows[k + 1][j], inner.Normal(innerRows[k][j]) + inner.Normal(innerRows[k + 1][j + 1]));
            }
            for (var side = 0; side < 2; side++)
                outer.Quad(rimOut[k, side], rimOut[k + 1, side], rimIn[k + 1, side], rimIn[k, side], Vector3.UnitY);
        }
        // Transom: the stern board, outer face −Z over the whole section, inner face +Z.
        var stern = Section(0f, 0f);
        var sternIn = Section(0f, shell);
        var z0 = -length * 0.5f;
        var back = stern.Select(p => outer.Vertex(new(p.X, p.Y, z0), -Vector3.UnitZ, p)).ToArray();
        for (var j = 1; j + 1 < back.Length; j++) outer.Triangle(back[0], back[j], back[j + 1], -Vector3.UnitZ);
        var front = sternIn.Select(p => inner.Vertex(new(p.X, p.Y, z0 + shell), Vector3.UnitZ, p)).ToArray();
        for (var j = 1; j + 1 < front.Length; j++) inner.Triangle(front[0], front[j], front[j + 1], Vector3.UnitZ);
        var a = outer.Finish();
        var b = inner.Finish();
        return new([.. a.Meshes, .. b.Meshes], a.Bounds);
    }

    /// <summary>
    /// Rigid placement (row-vector convention) that stands a kernel box (origin at its base centre, height along local Y) on the
    /// segment from <paramref name="a"/> to <paramref name="c"/>, its depth (local Z) along <paramref name="normal"/> made square to
    /// the segment: slanted mullions on a drum, seams down a roof.
    /// </summary>
    public static Matrix4x4 Along(Vector3 a, Vector3 c, Vector3 normal)
    {
        var u = Unit(c - a);
        var n = Unit(normal - u * (normal.X * u.X + normal.Y * u.Y + normal.Z * u.Z));
        var s = new Vector3(u.Y * n.Z - u.Z * n.Y, u.Z * n.X - u.X * n.Z, u.X * n.Y - u.Y * n.X);
        return new(s.X, s.Y, s.Z, 0, u.X, u.Y, u.Z, 0, n.X, n.Y, n.Z, 0, a.X, a.Y, a.Z, 1);
    }

    /// <summary>Outward normal of a symmetric hull section at point j (perpendicular to the averaged section tangent).</summary>
    private static Vector2 SectionNormal(Vector2[] s, int j)
    {
        var t = s[Math.Min(s.Length - 1, j + 1)] - s[Math.Max(0, j - 1)];
        var n = new Vector2(t.Y, -t.X);
        var length = MathF.Sqrt(n.X * n.X + n.Y * n.Y);
        return length > 1e-9f ? n / length : new Vector2(0, -1);
    }

    private static void Cap(Shapes.Writer m, float r, float y, int segments, float sign, float sx, float sz)
    {
        var normal = new Vector3(0, sign, 0);
        var centre = m.Vertex(new(0, y, 0), normal, Vector2.Zero);
        var ring = new int[segments];
        for (var i = 0; i < segments; i++)
        {
            var a = 2f * MathF.PI * i / segments;
            float c = DetMath.Cos(a), s = DetMath.Sin(a);
            ring[i] = m.Vertex(new(c * r * sx, y, s * r * sz), normal, new(c * r, s * r));
        }
        for (var i = 0; i < segments; i++) m.Triangle(centre, ring[i], ring[(i + 1) % segments], normal);
    }

    private static Vector3 Unit(Vector3 v)
    {
        var length = MathF.Sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z);
        return length > 1e-12f ? new(v.X / length, v.Y / length, v.Z / length) : Vector3.UnitY;
    }
}
