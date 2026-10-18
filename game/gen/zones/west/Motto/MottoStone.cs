using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Zones.Common;
using M = Campus.Gen.Zones.West.WestTune.Motto;

namespace Campus.Gen.Zones.West.Motto;

/// <summary>
/// 校训石 as a landscape stone of a few big planes (main session review of 19b): three rings of eight points (foot, waist,
/// top) joined by flat-shaded triangles, so the outline reads as broad cut faces the way stones are painted in theatrical
/// anime backgrounds, with little detail. The top is a low, gently tilted plane rising toward +X, not a dome. The front
/// column of every ring lies on one tilted plane, so the dressed face (the inscription) is a single flat, readable face.
/// Local frame: X along the stone, Y up, +Z toward the face; origin at the plan centre on the ground.
/// </summary>
internal static class MottoStone
{
    /// <summary>
    /// The three rings as (x, z, y), counter-clockwise from the face's left end: 0 and 1 are the dressed face's ends (their z
    /// is put on the face plane), 2 and 9 the angled cheeks either side of it, 3 and 8 the ends, 4–7 the back. Heights vary
    /// round the waist and the top, so the body bulges and the top breaks into a few low planes, higher at the back right. 初值.
    /// </summary>
    private static readonly Vector3[] Foot =
        [new(-2.20f, 0f, 0f), new(2.10f, 0f, 0f), new(3.00f, 0.62f, 0f), new(3.35f, -0.05f, 0f), new(3.00f, -0.70f, 0f),
         new(1.90f, -1.00f, 0f), new(-0.40f, -1.05f, 0f), new(-2.50f, -0.90f, 0f), new(-3.30f, -0.30f, 0f), new(-3.05f, 0.55f, 0f)];
    private static readonly Vector3[] Waist =
        [new(-2.15f, 0f, 1.00f), new(2.05f, 0f, 1.10f), new(3.05f, 0.50f, 1.00f), new(3.30f, -0.10f, 0.90f), new(2.95f, -0.72f, 1.00f),
         new(1.85f, -1.02f, 1.10f), new(-0.35f, -1.00f, 1.05f), new(-2.45f, -0.86f, 0.95f), new(-3.20f, -0.30f, 0.80f), new(-2.90f, 0.50f, 0.90f)];
    private static readonly Vector3[] Top =
        [new(-1.75f, 0f, 1.55f), new(1.70f, 0f, 1.72f), new(2.45f, 0.32f, 1.62f), new(2.75f, -0.15f, 1.45f), new(2.40f, -0.55f, 1.60f),
         new(1.40f, -0.72f, 1.78f), new(-0.30f, -0.70f, 1.70f), new(-1.90f, -0.60f, 1.55f), new(-2.60f, -0.25f, 1.30f), new(-2.35f, 0.25f, 1.38f)];
    /// <summary>How far the top's centre rises above the mean of its ring: a low crown, not a dome.</summary>
    private const float Crown = 0.05f;
    /// <summary>The dressed face: its line on the ground (z at y = 0) and how far it leans back per metre of height.</summary>
    private const float FaceZ = 0.92f, FaceLean = 0.16f;

    /// <summary>Unit normal of the dressed face (forward and a little up).</summary>
    public static Vector3 FaceNormal => Vector3.Normalize(new(0, FaceLean, 1));

    /// <summary>A point on the dressed face, x across and y the height on the stone.</summary>
    public static Vector3 OnFace(float x, float y) => new(x, y, FaceZ - FaceLean * y);

    public static float Peak => Top.Max(p => p.Z);

    public static PartGeometry Build(ToonSpec material)
    {
        Vector3[][] rings = [Ring(Foot), Ring(Waist), Ring(Top)];
        var centre = new Vector3(0, 0.8f, -0.1f);
        var m = new Shapes.Writer(material);
        var n = Foot.Length;
        for (var k = 0; k < 2; k++)
            for (var i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                Vector3 a = rings[k][i], b = rings[k][j], c = rings[k + 1][j], d = rings[k + 1][i];
                Flat(m, a, b, c, centre);
                Flat(m, a, c, d, centre);
            }
        // The top: a few low planes fanned from a slightly raised centre; the foot is closed under the bed.
        var top = rings[2];
        var tc = top.Aggregate(Vector3.Zero, (s, p) => s + p) / top.Length + new Vector3(0, Crown, 0);
        for (var i = 0; i < n; i++) Flat(m, tc, top[i], top[(i + 1) % n], tc - Vector3.UnitY);
        var foot = rings[0];
        var fc = foot.Aggregate(Vector3.Zero, (s, p) => s + p) / foot.Length;
        for (var i = 0; i < n; i++) Flat(m, fc, foot[i], foot[(i + 1) % n], fc + Vector3.UnitY);
        return m.Finish();
    }

    /// <summary>A ring in local (x, y, z); its first two points (the face's ends) sit on the dressed face plane.</summary>
    private static Vector3[] Ring(Vector3[] plan)
    {
        var ring = new Vector3[plan.Length];
        for (var i = 0; i < plan.Length; i++)
            ring[i] = new(plan[i].X, plan[i].Z, i < 2 ? FaceZ - FaceLean * plan[i].Z : plan[i].Y);
        return ring;
    }

    /// <summary>One flat-shaded triangle facing away from <paramref name="inside"/>.</summary>
    private static void Flat(Shapes.Writer m, Vector3 a, Vector3 b, Vector3 c, Vector3 inside)
    {
        var normal = Vector3.Cross(b - a, c - a);
        var length = normal.Length();
        if (length < 1e-8f) return;
        normal /= length;
        if (Vector3.Dot(normal, (a + b + c) / 3f - inside) < 0) normal = -normal;
        int ia = m.Vertex(a, normal, new(a.X, a.Y)), ib = m.Vertex(b, normal, new(b.X, b.Y)), ic = m.Vertex(c, normal, new(c.X, c.Y));
        m.Triangle(ia, ib, ic, normal);
    }
}
