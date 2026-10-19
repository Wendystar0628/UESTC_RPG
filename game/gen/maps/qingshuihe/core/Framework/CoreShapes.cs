using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.Maps.Qingshuihe.Core.Framework;

/// <summary>Height of the ground surface a flat item lies on, at a plan point (terrain plus the surface's lift).</summary>
public delegate float Surface(float x, float z);

/// <summary>
/// Pure, deterministic shape builders for zone pieces: lathed solids (tiers, poles, discs, rings), draped flat inlays
/// and frames, using standard .NET math functions.
/// </summary>
public static class CoreShapes
{

    /// <summary>
    /// Revolves a profile of (radius, height) points about the local +Y axis of <paramref name="frame"/>.
    /// Profiles run bottom to top on the outside (for example (0, 0) → (r, 0) → (r, h) → (0, h)), so each segment's
    /// outward normal is (dy, −dr). Sides are smooth around the axis and flat along the profile, which keeps the toon
    /// bands crisp on chamfers while rounds read round.
    /// </summary>
    public static void Lathe(CoreMesh mesh, in Paint paint, Matrix4x4 frame, IReadOnlyList<Vector2> profile, int sides)
    {
        ArgumentNullException.ThrowIfNull(profile);
        if (profile.Count < 2) throw new ArgumentException("A lathe profile needs two points.", nameof(profile));
        var ring = Campus.Gen.Sites.Shapes.Circle(Vector2.Zero, 1f, sides);
        for (var i = 0; i + 1 < profile.Count; i++)
        {
            var p0 = profile[i]; var p1 = profile[i + 1];
            var dr = p1.X - p0.X; var dy = p1.Y - p0.Y;
            var len = MathF.Sqrt(dr * dr + dy * dy);
            if (len < 1e-6f || (p0.X <= 0f && p1.X <= 0f)) continue;
            var nr = dy / len; var ny = -dr / len;
            for (var j = 0; j < sides; j++)
            {
                var c0 = ring[j]; var c1 = ring[(j + 1) % sides];
                Vector3 V(Vector2 p, Vector2 c) => Vector3.Transform(new(p.X * c.X, p.Y, p.X * c.Y), frame);
                Vector3 N(Vector2 c) => Vector3.Normalize(Vector3.TransformNormal(new(nr * c.X, ny, nr * c.Y), frame));
                var a = V(p0, c0); var b = V(p0, c1); var c = V(p1, c1); var d = V(p1, c0);
                if (p0.X <= 0f) mesh.Tri(paint, a, c, d, N(c0), N(c1), N(c0));
                else if (p1.X <= 0f) mesh.Tri(paint, a, b, c, N(c0), N(c1), N(c1));
                else mesh.Quad(paint, a, b, c, d, N(c0), N(c1), N(c1), N(c0));
            }
        }
    }

    /// <summary>Profile of a solid round block from y0 to y1 with a chamfer of <paramref name="bevel"/> on its top edge.</summary>
    public static Vector2[] Tier(float radius, float y0, float y1, float bevel) =>
        bevel <= 0f
            ? [new(0, y0), new(radius, y0), new(radius, y1), new(0, y1)]
            : [new(0, y0), new(radius, y0), new(radius, y1 - bevel), new(radius - bevel, y1), new(0, y1)];

    /// <summary>
    /// A flat annulus sector [r0, r1] × [a0, a1] (radians in the x = cos, z = sin parameterisation) lying on a surface,
    /// split into pieces of at most <paramref name="step"/> metres along the arc so it follows the terrain grid
    /// (WLD-TERRAIN); the surface height comes from <paramref name="surface"/> plus <paramref name="lift"/>.
    /// </summary>
    public static void Annulus(CoreMesh mesh, in Paint paint, Vector2 centre, float r0, float r1, float a0, float a1,
        Surface surface, float lift, float step = 1f)
    {
        if (!(r1 > r0) || r0 < 0f) throw new ArgumentOutOfRangeException(nameof(r1));
        var pieces = Math.Max(1, (int)MathF.Ceiling((a1 - a0) * r1 / step));
        for (var k = 0; k < pieces; k++)
        {
            var t0 = a0 + (a1 - a0) * k / pieces;
            var t1 = a0 + (a1 - a0) * (k + 1) / pieces;
            var u0 = new Vector2(MathF.Cos(t0), MathF.Sin(t0));
            var u1 = new Vector2(MathF.Cos(t1), MathF.Sin(t1));
            mesh.Quad(paint, On(centre + u0 * r0), On(centre + u1 * r0), On(centre + u1 * r1), On(centre + u0 * r1), Vector3.UnitY);
        }

        Vector3 On(Vector2 p) => new(p.X, surface(p.X, p.Y) + lift, p.Y);
    }

    /// <summary>A flat convex polygon (plan points, any winding) lying on a surface; fanned from its first point.</summary>
    public static void FlatPolygon(CoreMesh mesh, in Paint paint, IReadOnlyList<Vector2> ring, Surface surface, float lift)
    {
        for (var i = 1; i + 1 < ring.Count; i++)
            mesh.Tri(paint, On(ring[0]), On(ring[i]), On(ring[i + 1]), Vector3.UnitY);

        Vector3 On(Vector2 p) => new(p.X, surface(p.X, p.Y) + lift, p.Y);
    }

    /// <summary>A flat strip of width <paramref name="width"/> along the plan segment a → b, lying on a surface.</summary>
    public static void FlatStrip(CoreMesh mesh, in Paint paint, Vector2 a, Vector2 b, float width, Surface surface, float lift)
    {
        var d = b - a; var len = d.Length();
        if (len < 1e-4f) return;
        var side = new Vector2(-d.Y, d.X) / len * (width * 0.5f);
        FlatPolygon(mesh, paint, [a - side, b - side, b + side, a + side], surface, lift);
    }

    /// <summary>
    /// A clipped hedge from plan point a to b standing on <paramref name="y"/>, as a row of overlapping soft cushions
    /// (half ellipsoids about 1.1 m apart and well overlapped, the loropetalum's cloud crown): smooth normals, soft vegetation shading, no
    /// long extruded box. Each cushion starts <see cref="CoreStyle.Sink"/> below its base so it never floats.
    /// </summary>
    public static void Hedge(CoreMesh mesh, in Paint paint, Vector2 a, Vector2 b, float y, float width, float height, int rings, int sides)
    {
        var d = b - a; var length = d.Length();
        if (length < 0.05f) return;
        var along = d / length;
        var count = Math.Max(1, (int)MathF.Round(length / 1.1f));
        var pitch = length / count;
        for (var k = 0; k < count; k++)
        {
            var c = a + along * (pitch * (k + 0.5f));
            Cushion(mesh, paint, new(c.X, y, c.Y), along, pitch * 0.75f, width * 0.5f, height, rings, sides);
        }
    }

    /// <summary>
    /// A half ellipsoid with semi-axes <paramref name="ra"/> along <paramref name="along"/>, <paramref name="rb"/> across
    /// and <paramref name="h"/> up from its base centre, with a short vertical skirt below the base.
    /// </summary>
    public static void Cushion(CoreMesh mesh, in Paint paint, Vector3 centre, Vector2 along, float ra, float rb, float h, int rings, int sides)
    {
        var u = new Vector3(along.X, 0, along.Y);
        var v = new Vector3(-along.Y, 0, along.X);
        var ring = Campus.Gen.Sites.Shapes.Circle(Vector2.Zero, 1f, sides);
        Vector3 P(int i, Vector2 c)
        {
            if (i < 0) return centre + u * (ra * c.X) + v * (rb * c.Y) - Vector3.UnitY * CoreStyle.Sink;
            var phi = MathF.Tau * 0.25f * i / rings;
            float cp = MathF.Cos(phi), sp = MathF.Sin(phi);
            return centre + u * (ra * cp * c.X) + v * (rb * cp * c.Y) + Vector3.UnitY * (h * sp);
        }
        Vector3 N(int i, Vector2 c)
        {
            var phi = MathF.Tau * 0.25f * Math.Max(i, 0) / rings;
            float cp = MathF.Cos(phi), sp = MathF.Sin(phi);
            return Vector3.Normalize(u * (cp * c.X / ra) + v * (cp * c.Y / rb) + Vector3.UnitY * (sp / h));
        }
        for (var i = -1; i < rings; i++)
            for (var j = 0; j < sides; j++)
            {
                Vector2 c0 = ring[j], c1 = ring[(j + 1) % sides];
                if (i + 1 == rings)
                    mesh.Tri(paint, P(i, c0), P(i, c1), P(i + 1, c0), N(i, c0), N(i, c1), N(i + 1, c0));
                else
                    mesh.Quad(paint, P(i, c0), P(i, c1), P(i + 1, c1), P(i + 1, c0), N(i, c0), N(i, c1), N(i + 1, c1), N(i + 1, c0));
            }
    }

    /// <summary>Row-vector frame whose local +Y is <paramref name="up"/> and local +Z leans toward <paramref name="forward"/>.</summary>
    public static Matrix4x4 Frame(Vector3 origin, Vector3 up, Vector3 forward)
    {
        var y = Vector3.Normalize(up);
        var x = Vector3.Normalize(Vector3.Cross(y, forward));
        var z = Vector3.Cross(x, y);
        return new(x.X, x.Y, x.Z, 0, y.X, y.Y, y.Z, 0, z.X, z.Y, z.Z, 0, origin.X, origin.Y, origin.Z, 1);
    }

    /// <summary>Row-vector frame turned about +Y so local +Z points along the plan direction <paramref name="along"/>.</summary>
    public static Matrix4x4 Facing(Vector3 origin, Vector2 along) => Frame(origin, Vector3.UnitY, new(along.X, 0, along.Y));

}
