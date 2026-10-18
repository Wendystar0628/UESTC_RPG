using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.Zones.CenterEast.Framework;

/// <summary>
/// Curved solids for the 21b2 monuments: lathes (heads, collars, sockets), lathes whose radius also varies round the axis
/// (coats hanging in folds, the bust's lapels) and a rounded landscape stone with one flat inscription face. Same
/// conventions as <see cref="Shapes"/>: metres, origin at the base centre, front faces clockwise from outside, AO 1, UV in
/// metres, angles in radians. Lathe and Draped follow the south zone's statue helpers (zones never reference each other).
/// </summary>
public static class Solids
{
    /// <summary>
    /// A surface of revolution about local Y. Profile points are (radius, y) from bottom to top; a zero radius closes the
    /// end to a point. Normals follow the profile (smooth, ART-MODELING); the section is scaled by (sx, sz) for an
    /// elliptical body. Flat caps close a nonzero first or last ring.
    /// </summary>
    public static PartGeometry Lathe(IReadOnlyList<Vector2> profile, int segments, ToonSpec material, float sx = 1f, float sz = 1f)
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
                float c = MathF.Cos(a), s = MathF.Sin(a);
                var r = profile[k].X;
                var normal = Unit(new(c * normals[k].X / sx, normals[k].Y, s * normals[k].X / sz));
                rings[k][i] = m.Vertex(new(c * r * sx, profile[k].Y, s * r * sz), normal, new(2f * MathF.PI * i / segments, arc));
            }
        }
        for (var k = 0; k + 1 < n; k++)
            for (var i = 0; i < segments; i++)
                m.Quad(rings[k][i], rings[k][i + 1], rings[k + 1][i + 1], rings[k + 1][i], m.Normal(rings[k][i]) + m.Normal(rings[k + 1][i + 1]));
        if (profile[0].X > 1e-5f) Cap(m, profile[0].X, profile[0].Y, segments, -1, sx, sz);
        if (profile[n - 1].X > 1e-5f) Cap(m, profile[n - 1].X, profile[n - 1].Y, segments, 1, sx, sz);
        return m.Finish();
    }

    /// <summary>
    /// A lathe whose radius also varies round the axis: radius(angle, y) = profile radius × (1 + <paramref name="fold"/>(angle, y)),
    /// for cloth that hangs in folds or opens at the front. Angle is in radians from local +X towards +Z (the figure's front
    /// is +Z, angle π/2). Normals come from the surface grid itself (central differences), so folds shade as soft ridges;
    /// both ends are capped flat.
    /// </summary>
    public static PartGeometry Draped(IReadOnlyList<Vector2> profile, int segments, ToonSpec material, float sx, float sz, Func<float, float, float> fold)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(fold);
        if (profile.Count < 2) throw new ArgumentException("A lathe needs at least two profile points.", nameof(profile));
        if (segments < 6) throw new ArgumentOutOfRangeException(nameof(segments));
        var p = new Vector3[profile.Count, segments];
        for (var k = 0; k < profile.Count; k++)
            for (var i = 0; i < segments; i++)
            {
                var a = 2f * MathF.PI * i / segments;
                var r = profile[k].X * (1f + fold(a, profile[k].Y));
                p[k, i] = new(MathF.Cos(a) * r * sx, profile[k].Y, MathF.Sin(a) * r * sz);
            }
        return Grid(p, [.. profile.Select(q => new Vector3(0, q.Y, 0))], material);
    }

    /// <summary>
    /// A rounded landscape stone (景石) as a closed grid: an irregular loaf-shaped body of the stone's length (X), depth (Z)
    /// and height, its front (+Z) cut by the inscription plane of <paramref name="stone"/>. Everything is fixed by the
    /// shape's parameters, so the same stone always gives the same bytes.
    /// </summary>
    public static PartGeometry Boulder(BoulderShape stone, int segments, int rings, ToonSpec material)
    {
        ArgumentNullException.ThrowIfNull(stone);
        if (segments < 8 || rings < 3) throw new ArgumentOutOfRangeException(nameof(segments));
        var p = new Vector3[rings + 1, segments];
        var centres = new Vector3[rings + 1];
        for (var k = 0; k <= rings; k++)
        {
            var t = k / (float)rings;
            centres[k] = stone.Centre(t);
            for (var i = 0; i < segments; i++) p[k, i] = stone.Cut(stone.Surface(2f * MathF.PI * i / segments, t), centres[k]);
        }
        return Grid(p, centres, material);
    }

    /// <summary>
    /// Mesh of a closed grid of rings (bottom to top, each ring counter-clockwise from +X toward +Z) round the given ring
    /// centres. A ring whose points all meet its centre closes the end to a point; any other end ring gets a flat cap.
    /// </summary>
    private static PartGeometry Grid(Vector3[,] p, Vector3[] centres, ToonSpec material)
    {
        int n = p.GetLength(0), segments = p.GetLength(1);
        var m = new Shapes.Writer(material);
        var rings = new int[n][];
        var arc = 0f;
        for (var k = 0; k < n; k++)
        {
            if (k > 0) arc += Vector3.Distance(p[k, 0], p[k - 1, 0]);
            rings[k] = new int[segments + 1];
            for (var i = 0; i <= segments; i++)
            {
                var j = i % segments;
                var around = p[k, (j + 1) % segments] - p[k, (j + segments - 1) % segments];
                var up = p[Math.Min(n - 1, k + 1), j] - p[Math.Max(0, k - 1), j];
                var normal = Unit(new(up.Y * around.Z - up.Z * around.Y, up.Z * around.X - up.X * around.Z, up.X * around.Y - up.Y * around.X));
                var radial = p[k, j] - centres[k];
                if (normal.X * radial.X + normal.Z * radial.Z < 0) normal = -normal;
                if (Pole(p, centres, k)) normal = new(0, k == 0 ? -1 : 1, 0);
                rings[k][i] = m.Vertex(p[k, j], normal, new(2f * MathF.PI * i / segments, arc));
            }
        }
        for (var k = 0; k + 1 < n; k++)
            for (var i = 0; i < segments; i++)
                m.Quad(rings[k][i], rings[k][i + 1], rings[k + 1][i + 1], rings[k + 1][i], m.Normal(rings[k][i]) + m.Normal(rings[k + 1][i + 1]));
        foreach (var (k, sign) in new[] { (0, -1f), (n - 1, 1f) })
        {
            if (Pole(p, centres, k)) continue;
            var normal = new Vector3(0, sign, 0);
            var centre = m.Vertex(centres[k], normal, Vector2.Zero);
            var ring = new int[segments];
            for (var i = 0; i < segments; i++) ring[i] = m.Vertex(p[k, i], normal, new(p[k, i].X, p[k, i].Z));
            for (var i = 0; i < segments; i++) m.Triangle(centre, ring[i], ring[(i + 1) % segments], normal);
        }
        return m.Finish();
    }

    private static bool Pole(Vector3[,] p, Vector3[] centres, int k)
    {
        for (var i = 0; i < p.GetLength(1); i++)
            if (Vector3.DistanceSquared(p[k, i], centres[k]) > 1e-10f) return false;
        return true;
    }

    private static void Cap(Shapes.Writer m, float r, float y, int segments, float sign, float sx, float sz)
    {
        var normal = new Vector3(0, sign, 0);
        var centre = m.Vertex(new(0, y, 0), normal, Vector2.Zero);
        var ring = new int[segments];
        for (var i = 0; i < segments; i++)
        {
            var a = 2f * MathF.PI * i / segments;
            float c = MathF.Cos(a), s = MathF.Sin(a);
            ring[i] = m.Vertex(new(c * r * sx, y, s * r * sz), normal, new(c * r, s * r));
        }
        for (var i = 0; i < segments; i++) m.Triangle(centre, ring[i], ring[(i + 1) % segments], normal);
    }

    internal static Vector3 Unit(Vector3 v)
    {
        var length = MathF.Sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z);
        return length > 1e-12f ? new(v.X / length, v.Y / length, v.Z / length) : Vector3.UnitY;
    }
}

/// <summary>
/// A rounded landscape stone: a loaf of half extents (Length / 2, Depth / 2) whose section shrinks toward a rounded top,
/// leaning <see cref="Shear"/> metres toward +X over its height, its shoulders rising toward the +X end by <see cref="Rise"/>
/// of its height, with a few low-frequency bulges so it reads as a natural boulder rather than a turned shape. Its front
/// (+Z) is cut by a plane through (0, FaceHeight, FaceOut) tilted back by <see cref="FaceTilt"/> radians, the dressed face
/// that carries the inscription.
/// </summary>
public sealed record BoulderShape(float Length, float Depth, float Height, float FaceHeight, float FaceOut, float FaceTilt, float Shear)
{
    /// <summary>Lift of the shoulders at the +X end (and drop at −X) as a fraction of the height; zero at foot and crown.</summary>
    public float Rise { get; init; }

    // Section scale up the height (t = y / Height): a slightly tucked foot, full shoulders, then a rounded crown.
    private static readonly (float T, float R)[] Profile =
        [(0f, 0.93f), (0.1f, 1f), (0.32f, 0.99f), (0.52f, 0.92f), (0.7f, 0.79f), (0.84f, 0.6f), (0.94f, 0.36f), (1f, 0f)];

    /// <summary>Unit normal of the face plane, pointing forward and up.</summary>
    public Vector3 FaceNormal => new(0, MathF.Sin(FaceTilt), MathF.Cos(FaceTilt));

    /// <summary>Up direction within the face plane.</summary>
    public Vector3 FaceUp => new(0, MathF.Cos(FaceTilt), -MathF.Sin(FaceTilt));

    /// <summary>Point of the face plane at (x across, y up the face) from its centre.</summary>
    public Vector3 OnFace(float x, float y) => new Vector3(x, FaceHeight, FaceOut) + FaceUp * y;

    /// <summary>Rigid local placement of a face-mounted part: X across, Y up the face, Z out of it, origin on the plane.</summary>
    public Matrix4x4 FaceAt(float x, float y)
    {
        Vector3 up = FaceUp, n = FaceNormal, at = OnFace(x, y);
        return new(1, 0, 0, 0, up.X, up.Y, up.Z, 0, n.X, n.Y, n.Z, 0, at.X, at.Y, at.Z, 1);
    }

    internal Vector3 Centre(float t) => new(Shear * t, Height * t, 0);

    /// <summary>The uncut surface point at angle a (radians from +X toward +Z) and height fraction t.</summary>
    internal Vector3 Surface(float a, float t)
    {
        var r = Section(t) * (1f + 0.07f * MathF.Cos(2f * a + 0.9f) + 0.05f * MathF.Cos(3f * a + 2.3f) + 0.035f * (1f - t) * MathF.Cos(5f * a + 0.4f));
        var c = Centre(t);
        var x = MathF.Cos(a) * r;
        return new(c.X + x * Length * 0.5f, c.Y + Lift(t, x), MathF.Sin(a) * r * Depth * 0.5f);
    }

    /// <summary>Height added at height fraction t and normalised offset x (−1 at the −X end, 1 at the +X end).</summary>
    private float Lift(float t, float x) => Rise * Height * MathF.Sin(MathF.PI * t) * x;

    /// <summary>Height fraction of the ring through a point at height y and local x (two fixed-point steps of the lift).</summary>
    private float RingAt(float x, float y)
    {
        var t = y / Height;
        for (var i = 0; i < 2; i++) t = (y - Lift(t, (x - Centre(t).X) / (Length * 0.5f))) / Height;
        return t;
    }

    /// <summary>A surface point pulled back horizontally toward its ring centre onto the face plane when it lies in front of it.</summary>
    internal Vector3 Cut(Vector3 p, Vector3 centre)
    {
        var n = FaceNormal;
        var beyond = (p.Y - FaceHeight) * n.Y + (p.Z - FaceOut) * n.Z;
        if (beyond <= 0 || p.Z - centre.Z <= 1e-6f) return p;
        var t = ((FaceHeight - p.Y) * n.Y + (FaceOut - centre.Z) * n.Z) / ((p.Z - centre.Z) * n.Z);
        t = Math.Clamp(t, 0.1f, 1f);
        var level = new Vector3(centre.X, p.Y, centre.Z);
        return level + (p - level) * t;
    }

    /// <summary>
    /// Whether the face point (x, y) lies on the dressed face with <paramref name="margin"/> metres of stone round it: the
    /// uncut body at that height and direction reaches past the plane.
    /// </summary>
    public bool OnDressedFace(float x, float y, float margin)
    {
        var q = OnFace(x, y);
        foreach (var dx in new[] { -margin, 0f, margin })
        {
            var t = RingAt(q.X + dx, q.Y);
            if (t <= 0 || t >= 1) return false;
            var c = Centre(t);
            var d = new Vector2(q.X + dx - c.X, q.Z);
            var a = MathF.Atan2(d.Y / (Depth * 0.5f), d.X / (Length * 0.5f));
            var s = Surface(a, t);
            if (new Vector2(s.X - c.X, s.Z).Length() < d.Length() + margin) return false;
        }
        return true;
    }

    private static float Section(float t)
    {
        for (var i = 1; i < Profile.Length; i++)
            if (t <= Profile[i].T)
            {
                var (t0, r0) = Profile[i - 1];
                var (t1, r1) = Profile[i];
                return r0 + (r1 - r0) * (t - t0) / (t1 - t0);
            }
        return 0f;
    }
}
