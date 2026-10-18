using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Gen.Parts.Core;
using Campus.Gen.World;
using Campus.Gen.Zones.CenterEast.Framework;
using L = Campus.Gen.Zones.CenterEast.CenterEastTune.Look;
using M = Campus.Gen.Zones.CenterEast.CenterEastTune.Monuments;

namespace Campus.Gen.Zones.CenterEast.Research;

/// <summary>
/// The statue lawn east of 科 B/C (MAP-QSH-CENTER-EAST), a functional open space (MAP-QSH-ADDITIONS R6): its
/// middle stays open, the west edge is the buildings' doors and the east edge, along 银杏大道, is held by the two bronzes.
/// 林为干像 stands at the east tip of the plaza's tree island on 科 B's door axis, the big tree (WP-23) behind it;
/// 麦克斯韦像 stands on a granite apron at the lawn's east edge with the equation stone at his front right, both turned to
/// the plaza corner where R5 comes up from the bridge. Figures are sculpture volumes (clothes, posture, hair and beard as
/// masses; the face one smooth volume, no features or likeness, CORE-INDEX). Lawn, paving and trees are WP-15's and
/// WP-23's; this site builds the stone and the bronze. Every value lives in <see cref="M"/>.
/// </summary>
public sealed class StatueLawnSite : CenterEastSite
{
    internal static PlanFrame LinFrame => PlanMath.Facing(M.LinAt, M.LinFacing);
    internal static PlanFrame MaxwellFrame => PlanMath.Facing(M.MaxwellAt, M.MaxwellFacing);
    internal static PlanFrame StoneFrame => PlanMath.Facing(M.StoneAt, M.StoneFacing);
    private static PlanFrame R => ResearchSite.Frame;

    public override string Id => "21b2.statue-lawn";

    public override void Build(SiteBuilder b)
    {
        IslandKerb(b);
        LinBust(b, b.Ground(M.LinAt));
        Apron(b);
        Maxwell(b, b.Ground(M.MaxwellAt));
        EquationStone(b, b.Ground(M.StoneAt));
    }

    private static int Segments(SiteDetail detail) => detail switch { SiteDetail.Full => 48, SiteDetail.Plain => 28, _ => 16 };

    // ---- 林为干像 (§1.4): bust on a dark pedestal at the tip of the tree island ----

    /// <summary>The plaza hole nearest 林为干像: the lawn island with the big tree, kerbed in granite.</summary>
    internal static IReadOnlyList<Vector2>? Island(IWorldModel world)
    {
        if (!world.Features.TryGetValue(new(M.Plaza), out var plaza) || plaza.Geometry is not PolygonGeometry polygon) return null;
        return polygon.Rings.Holes.FirstOrDefault(h => PlanGeometry.InsideByCrossings(h, M.LinAt));
    }

    private static void IslandKerb(SiteBuilder b)
    {
        if (b.Detail == SiteDetail.Mass || Island(b.Context.World) is not { } hole) return;
        var g = b.Ground(PlanMath.Centroid(hole));
        var kerb = PartGeometryKernel.BeveledRectangleProfile(M.IslandKerbWidth, M.IslandKerbTop + 0.05f, 0.02f);
        b.Add(PartGeometryKernel.Sweep(kerb, hole.ToArray(), L.Granite, closed: true), Matrix4x4.CreateTranslation(0, g - 0.05f, 0));
    }

    internal static float LinPedestalTop => M.LinPlinth + M.LinShaft + M.LinCap - 0.05f;

    /// <summary>
    /// Dark pedestal (plinth, square shaft, cap) and the bronze bust: chest and shoulders of a suit jacket cut flat at
    /// mid-chest, its lapels a V that opens toward the shirt collar with the tie in it; the neck, and the head as an ovoid
    /// with a hair mass, turned along the island's axis to 银杏大道.
    /// </summary>
    private static void LinBust(SiteBuilder b, float y)
    {
        var f = LinFrame;
        var detail = b.Detail;
        var bevel = detail == SiteDetail.Full ? 0.025f : 0f;
        b.Add(PartGeometryKernel.BeveledBox(1.1f, M.LinPlinth, 1.1f, bevel, L.DarkStone), f.At(0, y - 0.05f, 0));
        b.Add(PartGeometryKernel.BeveledBox(0.66f, M.LinShaft, 0.66f, bevel, L.DarkStone), f.At(0, y - 0.05f + M.LinPlinth, 0));
        b.Add(PartGeometryKernel.BeveledBox(0.8f, M.LinCap, 0.8f, bevel, L.DarkStone), f.At(0, y - 0.05f + M.LinPlinth + M.LinShaft, 0));
        var s = M.LinScale;
        var segments = Segments(detail);
        var small = Math.Max(8, segments / 3);
        var body = y + LinPedestalTop;
        Vector2 P(float r, float h) => new(r * s, h * s);
        Matrix4x4 At(float x, float h, float z) => f.At(x * s, body + h * s, z * s);
        float[] heights = [0f, 0.05f, 0.12f, 0.2f, 0.26f, 0.3f, 0.33f, 0.355f, 0.37f];
        float[] radii = [0.205f, 0.215f, 0.228f, 0.24f, 0.245f, 0.232f, 0.195f, 0.13f, 0.075f];
        // Lapels: a V at the front (angle π/2) that closes at the button and widens toward the collar.
        float Lapels(float a, float h)
        {
            var width = 0.55f * Math.Clamp((h / s - 0.06f) / 0.27f, 0f, 1f);
            if (width <= 0) return 0f;
            var d = (a - MathF.PI * 0.5f) / width;
            return -0.11f * MathF.Max(0f, 1f - d * d);
        }
        b.Add(Solids.Draped([.. heights.Select((h, i) => P(radii[i], h))], segments, L.Bronze, 1.1f, 0.58f, detail == SiteDetail.Mass ? (_, _) => 0f : Lapels), At(0, 0, 0));
        b.Add(Shapes.Shaft(0.058f * s, 0.052f * s, 0.11f * s, small, L.Bronze), At(0, 0.35f, 0.01f));
        b.Add(Solids.Lathe(Head, small * 2, L.Bronze, 0.86f, 1f), At(0, 0.44f, 0.015f).Scaled(s));
        if (detail == SiteDetail.Mass) return;
        // Shirt collar and the tie in the lapels' V.
        b.Add(Solids.Lathe([P(0.068f, 0), P(0.075f, 0.03f), P(0.066f, 0.042f)], small * 2, L.Bronze, 1f, 0.9f), At(0, 0.355f, 0.01f));
        b.Add(PartGeometryKernel.BeveledBox(0.042f * s, 0.2f * s, 0.02f * s, 0, L.Bronze), At(0, 0.14f, 0.118f));
        if (detail != SiteDetail.Full) return;
        b.Add(Solids.Lathe(Hair, small * 2, L.Bronze, 0.9f, 1f), At(0, 0.565f, -0.006f).Scaled(s));
    }

    /// <summary>Head ovoid of a 2 m figure, wider front to back than side to side (scaled by the caller).</summary>
    private static readonly Vector2[] Head =
        [new(0, 0), new(0.045f, 0.01f), new(0.075f, 0.05f), new(0.088f, 0.1f), new(0.092f, 0.15f), new(0.088f, 0.19f), new(0.07f, 0.225f), new(0.04f, 0.245f), new(0, 0.252f)];

    /// <summary>Hair mass over the crown and back; the face stays one smooth volume (an outlined nose reads as an eye).</summary>
    private static readonly Vector2[] Hair = [new(0.097f, 0), new(0.099f, 0.035f), new(0.087f, 0.085f), new(0.052f, 0.118f), new(0, 0.128f)];

    /// <summary>Full beard round the jaw and chin (麦克斯韦's 络腮胡), below and either side of the face.</summary>
    private static readonly Vector2[] Beard = [new(0, 0), new(0.05f, 0.008f), new(0.078f, 0.035f), new(0.086f, 0.07f), new(0.08f, 0.1f), new(0.06f, 0.125f), new(0, 0.135f)];

    // ---- 麦克斯韦像 (§1.3) on its granite apron ----

    /// <summary>The apron's research-frame rectangle as a world ring (outside on the right).</summary>
    internal static Vector2[] ApronRing => PlanMath.OutsideRight(R.Rect(M.ApronMin.X, M.ApronMin.Y, M.ApronMax.X, M.ApronMax.Y));

    private static void Apron(SiteBuilder b)
    {
        Vector2 min = M.ApronMin, max = M.ApronMax, mid = (min + max) * 0.5f;
        var g = b.Ground(R.P(mid.X, mid.Y));
        b.Add(PartGeometryKernel.BeveledBox(max.X - min.X, M.ApronTop + 0.1f, max.Y - min.Y, b.Detail == SiteDetail.Mass ? 0f : 0.01f, L.Paving), R.At(mid.X, g - 0.1f, mid.Y));
        if (b.Detail == SiteDetail.Mass) return;
        var kerb = PartGeometryKernel.BeveledRectangleProfile(M.ApronKerb, M.ApronTop + 0.12f, 0.015f);
        b.Add(PartGeometryKernel.Sweep(kerb, ApronRing, L.Granite, closed: true), Matrix4x4.CreateTranslation(0, g - 0.1f, 0));
    }

    /// <summary>
    /// Light grey granite pedestal (base slab, die, cap; the die's front carries 电子工程学院 once the scene text atlas lands)
    /// and the bronze figure at 1.25 × a 2 m model: a frock coat to the knee whose skirt opens at the front and hangs in
    /// soft folds, square shoulders and a stand collar, trouser legs and shoes under the hem; the left arm hangs, the right
    /// forearm is raised across the chest holding a scroll upright against it. The head is an ovoid with a hair mass and a
    /// full beard round the jaw.
    /// </summary>
    private static void Maxwell(SiteBuilder b, float y)
    {
        var f = MaxwellFrame;
        var detail = b.Detail;
        var bevel = detail == SiteDetail.Full ? 0.03f : 0f;
        b.Add(PartGeometryKernel.BeveledBox(2.0f, 0.3f, 2.0f, bevel, L.PaleGranite), f.At(0, y - 0.05f, 0));
        b.Add(PartGeometryKernel.BeveledBox(1.3f, 1.5f, 1.3f, bevel, L.PaleGranite), f.At(0, y + 0.25f, 0));
        b.Add(PartGeometryKernel.BeveledBox(1.5f, 0.2f, 1.5f, bevel, L.PaleGranite), f.At(0, y + 1.75f, 0));
        var s = M.MaxwellScale;
        var segments = Segments(detail);
        var small = Math.Max(8, segments / 3);
        var foot = y + M.MaxwellPedestalTop;
        var body = foot + 0.06f * s;
        Vector2 P(float r, float h) => new(r * s, h * s);
        Matrix4x4 At(float x, float h, float z) => f.At(x * s, body + h * s, z * s);
        b.Add(Solids.Lathe([P(0.43f, 0), P(0.43f, 0.04f), P(0.41f, 0.06f)], segments, L.Bronze, 1f, 0.76f), f.At(0, foot, 0));
        // Frock coat: skirt to below the knee, folds strongest at the hem and gone by the waist, the fronts parting below it.
        float[] heights = [0.42f, 0.48f, 0.6f, 0.74f, 0.88f, 0.97f, 1.05f, 1.22f, 1.38f, 1.46f, 1.52f, 1.56f, 1.59f];
        float[] radii = [0.272f, 0.264f, 0.25f, 0.236f, 0.222f, 0.212f, 0.217f, 0.244f, 0.262f, 0.27f, 0.24f, 0.17f, 0.095f];
        float Coat(float a, float h)
        {
            var hem = Math.Clamp((0.95f * s - h) / (0.45f * s), 0f, 1f);
            var folds = 0.08f * hem * (0.6f * MathF.Cos(6f * a) + 0.4f * MathF.Cos(10f * a + 1.3f));
            var front = MathF.Max(0f, MathF.Sin(a));
            front *= front; front *= front; front *= front;
            var open = 0.5f * Math.Clamp((0.98f * s - h) / (0.4f * s), 0f, 1f) * front;
            return folds - open;
        }
        b.Add(Solids.Draped([.. heights.Select((h, i) => P(radii[i], h))], segments, L.Bronze, 1f, 0.66f, detail == SiteDetail.Mass ? (_, _) => 0f : Coat), At(0, 0, 0));
        b.Add(Shapes.Shaft(0.056f * s, 0.05f * s, 0.12f * s, small, L.Bronze), At(0, 1.55f, 0.012f));
        b.Add(Solids.Lathe(Head, small * 2, L.Bronze, 0.86f, 1f), At(0, 1.64f, 0.015f).Scaled(s));
        b.Add(Solids.Lathe(Beard, small * 2, L.Bronze, 1.05f, 0.85f), At(0, 1.6f, 0.03f).Scaled(s));
        // Trouser legs from inside the coat down to the shoes.
        foreach (var side in new[] { -1f, 1f })
            b.Add(Shapes.Shaft(0.08f * s, 0.074f * s, 0.55f * s, small, L.Bronze), At(side * 0.098f, 0.05f, 0f));
        if (detail == SiteDetail.Mass) return;
        b.Add(Solids.Lathe([P(0.1f, 0), P(0.122f, 0.04f), P(0.118f, 0.085f), P(0.098f, 0.095f)], small * 2, L.Bronze, 1f, 0.85f), At(0, 1.545f, 0.005f));
        foreach (var side in new[] { -1f, 1f })
            b.Add(Solids.Lathe([P(0.05f, 0), P(0.056f, 0.025f), P(0.046f, 0.065f), P(0, 0.075f)], small, L.Bronze, 1f, 2.1f), At(side * 0.105f, 0, 0.055f));
        // Arms in coat sleeves: shoulder, elbow, wrist; the hand continues the forearm.
        Vector3 shoulderL = new(0.215f, 1.43f, 0f), elbowL = new(0.262f, 1.11f, 0.02f), wristL = new(0.268f, 0.84f, 0.06f);
        Vector3 shoulderR = new(-0.215f, 1.43f, 0f), elbowR = new(-0.255f, 1.13f, 0.07f), wristR = new(-0.07f, 1.22f, 0.2f);
        foreach (var (sh, el, wr) in new[] { (shoulderL, elbowL, wristL), (shoulderR, elbowR, wristR) })
        {
            Limb(b, f, body, s, sh, el, 0.085f, 0.07f, small);
            Limb(b, f, body, s, el, wr, 0.07f, 0.068f, small);
            b.Add(Solids.Lathe([P(0, 0), P(0.047f, 0.019f), P(0.066f, 0.066f), P(0.047f, 0.113f), P(0, 0.132f)], small, L.Bronze), At(el.X, el.Y - 0.066f, el.Z));
            var along = Unit(wr - el);
            Hand(b, f, body, s, wr - along * 0.03f, along, small);
        }
        // The scroll stands in the right fist, leaning toward the left shoulder against the chest.
        var grip = wristR + Unit(wristR - elbowR) * 0.075f;
        var up = Unit(new(0.3f, 0.95f, -0.05f));
        Limb(b, f, body, s, grip - up * 0.17f, grip + up * 0.17f, 0.032f, 0.032f, small);
        if (detail != SiteDetail.Full) return;
        b.Add(Solids.Lathe(Hair, small * 2, L.Bronze, 0.9f, 1f), At(0, 1.765f, -0.006f).Scaled(s));
    }

    /// <summary>A round limb or rod between two model points (x left, y up from the plate top, z forward), scaled by s.</summary>
    private static void Limb(SiteBuilder b, PlanFrame f, float body, float s, Vector3 from, Vector3 to, float r0, float r1, int segments)
    {
        var axis = to - from;
        var length = MathF.Sqrt(axis.X * axis.X + axis.Y * axis.Y + axis.Z * axis.Z) * s;
        b.Add(Shapes.Shaft(r0 * s, r1 * s, length, segments, L.Bronze), Orient(f, body, s, from, Unit(axis)));
    }

    /// <summary>A hand as a flattened ovoid along the given model direction, its wrist starting inside the sleeve end.</summary>
    private static void Hand(SiteBuilder b, PlanFrame f, float body, float s, Vector3 wrist, Vector3 along, int segments) =>
        b.Add(Solids.Lathe([new(0.036f * s, 0), new(0.038f * s, 0.05f * s), new(0.046f * s, 0.09f * s), new(0.04f * s, 0.15f * s), new(0.022f * s, 0.19f * s), new(0, 0.2f * s)],
            segments, L.Bronze, 0.62f, 1f), Orient(f, body, s, wrist, along));

    /// <summary>Rigid placement whose local Y runs along a model direction from a model point (statue frame, scaled by s).</summary>
    private static Matrix4x4 Orient(PlanFrame f, float body, float s, Vector3 at, Vector3 direction)
    {
        Vector3 World(Vector3 v) => new(f.U.X * v.X + f.W.X * v.Z, v.Y, f.U.Y * v.X + f.W.Y * v.Z);
        var y = Unit(World(direction));
        var reference = MathF.Abs(y.Y) < 0.95f ? Vector3.UnitY : World(Vector3.UnitZ);
        var x = Unit(Cross(y, reference));
        var z = Cross(x, y);
        var p = f.P(at.X * s, at.Z * s);
        return new(x.X, x.Y, x.Z, 0, y.X, y.Y, y.Z, 0, z.X, z.Y, z.Z, 0, p.X, body + at.Y * s, p.Y, 1);
    }

    private static Vector3 Cross(Vector3 a, Vector3 c) => new(a.Y * c.Z - a.Z * c.Y, a.Z * c.X - a.X * c.Z, a.X * c.Y - a.Y * c.X);

    private static Vector3 Unit(Vector3 v) => Solids.Unit(v);

    // ---- Equation stone (§1.3) ----

    /// <summary>
    /// The rounded stone sunk 8 cm into the apron, its dressed face toward the approach. The four equations are filled
    /// line blocks of their lengths, left-aligned on the face, until the scene text atlas carries the formulae (contract-26).
    /// </summary>
    private static void EquationStone(SiteBuilder b, float y)
    {
        var at = StoneFrame.At(0, y - 0.08f, 0);
        var (segments, rings) = b.Detail switch { SiteDetail.Full => (40, 12), SiteDetail.Plain => (24, 8), _ => (14, 5) };
        b.Add(Solids.Boulder(M.Stone, segments, rings, L.FieldStone), at);
        if (b.Detail == SiteDetail.Mass) return;
        foreach (var (x, line, length) in StoneLines())
            b.Add(PartGeometryKernel.BeveledBox(length, M.StoneLineHeight, 0.012f, 0, L.Ink), GeometryPlacement.Multiply(M.Stone.FaceAt(x, line), at));
    }

    /// <summary>Centre x, bottom y (on the face, from its centre) and length of each inscription line, bottom to top.</summary>
    internal static IEnumerable<(float X, float Y, float Length)> StoneLines()
    {
        var left = -M.StoneLines.Max() * 0.5f;
        for (var i = 0; i < M.StoneLines.Length; i++)
            yield return (left + M.StoneLines[i] * 0.5f, M.StoneLineBottom + i * M.StoneLinePitch, M.StoneLines[i]);
    }

    // ---- Collision: pedestals and the stone are boxes with a top; the apron is paving ----

    public override void Collide(SiteColliders c)
    {
        var lg = c.Ground(M.LinAt.X, M.LinAt.Y);
        var lt = LinPedestalTop;
        c.Box(LinFrame.At(0, lg + (lt - 0.05f) * 0.5f, 0), new(0.55f, (lt + 0.05f) * 0.5f, 0.55f), SurfaceTag.Prop, FootMat.PolishedStone);
        var mg = c.Ground(M.MaxwellAt.X, M.MaxwellAt.Y);
        var mt = M.MaxwellPedestalTop;
        c.Box(MaxwellFrame.At(0, mg + (mt - 0.05f) * 0.5f, 0), new(1.0f, (mt + 0.05f) * 0.5f, 1.0f), SurfaceTag.Prop, FootMat.PolishedStone);
        Vector2 min = M.ApronMin, max = M.ApronMax, mid = (min + max) * 0.5f, apron = R.P(mid.X, mid.Y);
        var ag = c.Ground(apron.X, apron.Y);
        c.Box(R.At(mid.X, ag + (M.ApronTop - 0.1f) * 0.5f, mid.Y), new((max.X - min.X) * 0.5f, (M.ApronTop + 0.1f) * 0.5f, (max.Y - min.Y) * 0.5f), SurfaceTag.Ground, FootMat.StonePaving);
        // The stone: its body behind the dressed face, kept within 0.3 m of the rounded outline (SYS-WALK).
        var st = M.Stone;
        var sg = c.Ground(M.StoneAt.X, M.StoneAt.Y);
        var back = st.Depth * 0.5f;
        c.Box(StoneFrame.At(st.Shear * 0.5f, sg + st.Height * 0.5f - 0.08f, (st.FaceOut - back) * 0.5f),
            new(st.Length * 0.475f, st.Height * 0.5f, (st.FaceOut + back) * 0.5f), SurfaceTag.Prop, FootMat.StonePaving);
    }
}

file static class MatrixScale
{
    /// <summary>The same placement with its local axes scaled by s (for unit-sized profiles such as the head).</summary>
    public static Matrix4x4 Scaled(this Matrix4x4 m, float s) => new(
        m.M11 * s, m.M12 * s, m.M13 * s, 0,
        m.M21 * s, m.M22 * s, m.M23 * s, 0,
        m.M31 * s, m.M32 * s, m.M33 * s, 0,
        m.M41, m.M42, m.M43, 1);
}
