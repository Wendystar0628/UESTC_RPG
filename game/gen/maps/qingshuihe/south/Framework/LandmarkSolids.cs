using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Sites;

namespace Campus.Gen.Maps.Qingshuihe.South.Framework;

/// <summary>
/// Sloped solids for the 18b landmarks: regular-polygon roofs with upturned eaves (the hexagonal pavilion), a
/// rowing-boat hull and boxes stood along a segment. The shared <see cref="Solids"/> keep the lathes and draped coats.
/// Same conventions as <see cref="Shapes"/>: metres, origin at the base centre,
/// front faces clockwise from outside, AO 1, UV in metres, angles in radians.
/// </summary>
public static class LandmarkSolids
{
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
            return new(r * MathF.Cos(a), r * MathF.Sin(a));
        }
        for (var i = 0; i < sides; i++)
        {
            Vector2 c0 = Corner(i, eave), c1 = Corner(i + 1, eave), t0 = Corner(i, top), t1 = Corner(i + 1, top);
            var mid = (c0 + c1) * 0.5f;
            var midTop = (t0 + t1) * 0.5f;
            // Slope plane through the level mid-eave and the top edge: one normal for the whole side.
            float ax = c1.X - c0.X, az = c1.Y - c0.Y;
            float ux = midTop.X - mid.X, uy = rise, uz = midTop.Y - mid.Y;
            var normal = VectorMath.Unit(new Vector3(uy * az, uz * ax - ux * az, -uy * ax));
            if (normal.Y < 0) normal = -normal;
            var outward = VectorMath.Unit(new Vector3(mid.X, 0, mid.Y));
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
        var u = VectorMath.Unit(c - a, Vector3.UnitY);
        var n = VectorMath.Unit(normal - u * Vector3.Dot(normal, u), Vector3.UnitY);
        var s = VectorMath.Cross(u, n);
        return new(s.X, s.Y, s.Z, 0, u.X, u.Y, u.Z, 0, n.X, n.Y, n.Z, 0, a.X, a.Y, a.Z, 1);
    }

    /// <summary>Outward normal of a symmetric hull section at point j (perpendicular to the averaged section tangent).</summary>
    private static Vector2 SectionNormal(Vector2[] s, int j)
    {
        var t = s[Math.Min(s.Length - 1, j + 1)] - s[Math.Max(0, j - 1)];
        var n = new Vector2(t.Y, -t.X);
        return VectorMath.Unit(n, new Vector2(0, -1), 1e-9);
    }

}
