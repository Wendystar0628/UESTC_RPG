using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Gen.Parts.Core;
using Campus.Gen.Zones.South.Framework;
using P = Campus.Gen.Zones.South.SouthTune.Park;
using L = Campus.Gen.Zones.South.SouthTune.Look;

namespace Campus.Gen.Zones.South.ForestPark;

/// <summary>
/// Forest park between the museum and the main building (§3.2). The red maples and cherry trees are WP-23's; this site
/// builds the peacock house, a white steel mesh cage with a hipped mesh roof on the hill's south-east foot, and the small
/// square pavilion with its stone seat wall on the top of the red-maple hill (MAP-QSH-TERRAIN §1.2, design addition
/// design:south-hongfeng-pavilion). Everything stands on the terrain height, so the pavilion rises with the hill once the
/// WP-08 evaluator lands; until then it stands on the flat stub.
/// </summary>
public sealed class ForestParkSite : SouthSite
{
    public const string Pavilion = "design:south-hongfeng-pavilion";

    private static readonly PlanFrame Cage = new(P.PeacockHouse, Vector2.UnitX);
    private static readonly PlanFrame Hill = new(P.HillTop, Vector2.UnitX);

    public override string Id => "18a.forest-park";

    public override void Build(SiteBuilder b)
    {
        PeacockHouse(b, b.Ground(P.PeacockHouse));
        HillPavilion(b, b.Ground(P.HillTop));
    }

    // ---- Peacock house (孔雀之家) ----

    private static void PeacockHouse(SiteBuilder b, float y)
    {
        float w = P.CageWidth, d = P.CageDepth, h = P.CageWall, curb = 0.3f;
        var bottom = y + curb;
        var top = bottom + h;
        b.Add(PartGeometryKernel.BeveledBox(w + 0.3f, curb + 0.1f, d + 0.3f, 0.03f, L.Concrete), Cage.At(0, y - 0.1f, 0));
        // Frame: posts every ~1.5 m, a ring beam at the eaves and a mid rail.
        var posts = Ring(w, d, 1.5f).ToArray();
        foreach (var p in posts) b.Add(PartGeometryKernel.BeveledBox(0.1f, h, 0.1f, 0.012f, L.WhiteSteel), Cage.At(p.X, bottom, p.Y));
        foreach (var (a, e) in Edges(w, d))
        {
            Strut(b, Cage, new(a.X, top, a.Y), new(e.X, top, e.Y), 0.12f);
            Strut(b, Cage, new(a.X, bottom + 1.0f, a.Y), new(e.X, bottom + 1.0f, e.Y), 0.06f);
        }
        // Hipped roof: hips and rafters to a short ridge, mesh rings that catch the winter snow lines (§11).
        var ridge = new[] { new Vector3(-(w - d) * 0.5f, top + P.CageRoofRise, 0), new Vector3((w - d) * 0.5f, top + P.CageRoofRise, 0) };
        Strut(b, Cage, ridge[0], ridge[1], 0.1f);
        foreach (var p in posts)
        {
            var apex = p.X < ridge[0].X ? ridge[0] : p.X > ridge[1].X ? ridge[1] : new Vector3(p.X, ridge[0].Y, 0);
            Strut(b, Cage, new(p.X, top, p.Y), apex, 0.07f);
        }
        // Door frame on the south side, facing the woodland path.
        b.Add(PartGeometryKernel.BeveledBox(0.08f, 2.1f, 0.08f, 0.01f, L.WhiteSteel), Cage.At(-0.55f, bottom, d * 0.5f));
        b.Add(PartGeometryKernel.BeveledBox(0.08f, 2.1f, 0.08f, 0.01f, L.WhiteSteel), Cage.At(0.55f, bottom, d * 0.5f));
        Strut(b, Cage, new(-0.55f, bottom + 2.1f, d * 0.5f), new(0.55f, bottom + 2.1f, d * 0.5f), 0.08f);
        if (b.Detail == SiteDetail.Mass) return;
        // Wire mesh: thin rods on a 0.5 m grid (L0), every other rod at L1.
        var mesh = P.CageMesh * (b.Detail == SiteDetail.Full ? 1 : 2);
        const float rod = 0.025f;
        foreach (var (a, e) in Edges(w, d))
        {
            var length = Vector2.Distance(a, e);
            var n = (int)MathF.Round(length / mesh);
            for (var k = 1; k < n; k++)
            {
                var p = a + (e - a) * (k / (float)n);
                b.Add(PartGeometryKernel.BeveledBox(rod, h, rod, 0, L.WhiteWire), Cage.At(p.X, bottom, p.Y));
            }
            for (var yy = bottom + mesh; yy < top - 0.1f; yy += mesh)
                if (MathF.Abs(yy - bottom - 1.0f) > 0.1f) Strut(b, Cage, new(a.X, yy, a.Y), new(e.X, yy, e.Y), rod, L.WhiteWire);
        }
        for (var k = 1; k <= 3; k++)
        {
            var t = k / 4f;
            float hx = w * 0.5f - (d * 0.5f) * t, hz = d * 0.5f * (1 - t), yy = top + P.CageRoofRise * t;
            Vector3[] r = [new(-hx, yy, -hz), new(hx, yy, -hz), new(hx, yy, hz), new(-hx, yy, hz)];
            for (var i = 0; i < 4; i++) Strut(b, Cage, r[i], r[(i + 1) % 4], rod * 1.4f, L.WhiteWire);
        }
        // Timber perch inside for the birds' night roost (WLD-LIFE §1; the peacocks are WP-25's).
        foreach (var x in new[] { -2.2f, 1.8f })
            b.Add(PartGeometryKernel.BeveledBox(0.12f, 2.3f, 0.12f, 0.015f, L.Wood), Cage.At(x, bottom, -1.0f));
        Strut(b, Cage, new(-2.4f, bottom + 2.1f, -1.0f), new(2.0f, bottom + 2.1f, -1.0f), 0.1f, L.Wood);
        Strut(b, Cage, new(-2.4f, bottom + 1.3f, -1.0f), new(2.0f, bottom + 1.3f, -1.0f), 0.08f, L.Wood);
    }

    /// <summary>Points around a w × d rectangle (local), corners included, sides divided near the pitch.</summary>
    private static IEnumerable<Vector2> Ring(float w, float d, float pitch)
    {
        foreach (var (a, e) in Edges(w, d))
        {
            var n = Math.Max(1, (int)MathF.Round(Vector2.Distance(a, e) / pitch));
            for (var k = 0; k < n; k++) yield return a + (e - a) * (k / (float)n);
        }
    }

    private static IEnumerable<(Vector2 A, Vector2 B)> Edges(float w, float d)
    {
        Vector2[] c = [new(-w * 0.5f, -d * 0.5f), new(w * 0.5f, -d * 0.5f), new(w * 0.5f, d * 0.5f), new(-w * 0.5f, d * 0.5f)];
        for (var i = 0; i < 4; i++) yield return (c[i], c[(i + 1) % 4]);
    }

    /// <summary>A square-section rod between two local points (any slope), built from its own basis without trigonometry.</summary>
    private static void Strut(SiteBuilder b, PlanFrame f, Vector3 a, Vector3 e, float size, ToonSpec? material = null)
    {
        var d = e - a;
        var length = MathF.Sqrt(d.X * d.X + d.Y * d.Y + d.Z * d.Z);
        if (length < 0.02f) return;
        var x = d / length;
        // Local Y: the up vector made perpendicular to the rod (or local Z for a vertical rod).
        var up = MathF.Abs(x.Y) > 0.95f ? new Vector3(0, 0, 1) : new Vector3(0, 1, 0);
        var dot = up.X * x.X + up.Y * x.Y + up.Z * x.Z;
        var yv = up - x * dot;
        var yl = MathF.Sqrt(yv.X * yv.X + yv.Y * yv.Y + yv.Z * yv.Z);
        yv /= yl;
        var zv = new Vector3(x.Y * yv.Z - x.Z * yv.Y, x.Z * yv.X - x.X * yv.Z, x.X * yv.Y - x.Y * yv.X);
        var mid = (a + e) * 0.5f - yv * (size * 0.5f);
        var local = new Matrix4x4(x.X, x.Y, x.Z, 0, yv.X, yv.Y, yv.Z, 0, zv.X, zv.Y, zv.Z, 0, mid.X, mid.Y, mid.Z, 1);
        b.Add(PartGeometryKernel.BeveledBox(length, size, size, 0, material ?? L.WhiteSteel), GeometryPlacement.Multiply(local, f.At(0, 0, 0)));
    }

    // ---- Hill-top pavilion and seat wall (MAP-QSH-TERRAIN §1.2) ----

    private static void HillPavilion(SiteBuilder b, float y)
    {
        float s = P.PavilionSize, post = P.PavilionPost, eave = P.PavilionEave;
        b.Add(PartGeometryKernel.BeveledBox(s + 0.8f, 0.4f, s + 0.8f, 0.03f, L.Granite), Hill.At(0, y - 0.1f, 0));
        var floor = y + 0.3f;
        var half = s * 0.5f - post * 0.5f;
        foreach (var x in new[] { -half, half })
            foreach (var z in new[] { -half, half })
            {
                b.Add(PartGeometryKernel.BeveledBox(post, eave, post, 0.02f, L.Wood), Hill.At(x, floor, z));
                b.Add(PartGeometryKernel.BeveledBox(post + 0.12f, 0.2f, post + 0.12f, 0.015f, L.StoneTrim), Hill.At(x, floor, z));
            }
        foreach (var (a, e) in Edges(s - post, s - post))
            Strut(b, Hill, new(a.X, floor + eave - 0.32f, a.Y), new(e.X, floor + eave - 0.32f, e.Y), 0.3f, L.Wood);
        b.Add(PartGeometryKernel.BeveledBox(s + 1.2f, 0.12f, s + 1.2f, 0.02f, L.Wood), Hill.At(0, floor + eave - 0.02f, 0));
        b.Add(Shapes.Hip(s + 1.1f, s + 1.1f, P.PavilionRoofRise, 0, L.RoofSlab), Hill.At(0, floor + eave + 0.1f, 0));
        if (b.Detail != SiteDetail.Mass)
            b.Add(Shapes.Shaft(0.1f, 0.03f, 0.35f, 10, L.Metal), Hill.At(0, floor + eave + 0.1f + P.PavilionRoofRise - 0.02f, 0));
        // Stone seat wall on the north rim, facing the lake and the museum: a polygonal arc of 0.45 m blocks.
        foreach (var (a, e) in SeatArc())
        {
            var seg = PlanFrame.Along((a + e) * 0.5f, e - a);
            var ground = b.Ground(seg.Origin);
            b.Add(PartGeometryKernel.BeveledBox(Vector2.Distance(a, e) + 0.02f, P.SeatWallHeight + 0.1f, 0.42f, 0.025f, L.Granite), seg.At(0, ground - 0.1f, 0));
        }
    }

    /// <summary>Seven chords of the seat-wall arc, bearings −60°…60° around the hill top (north rim).</summary>
    private static IEnumerable<(Vector2 A, Vector2 B)> SeatArc()
    {
        const int n = 7;
        Vector2 At(int k)
        {
            var bearing = (-60f + 120f * k / n) * MathF.PI / 180f;
            return P.HillTop + new Vector2(DetMath.Sin(bearing), -DetMath.Cos(bearing)) * P.SeatWallRadius;
        }
        for (var k = 0; k < n; k++) yield return (At(k), At(k + 1));
    }

    public override void Collide(SiteColliders c)
    {
        var y = c.Ground(P.PeacockHouse.X, P.PeacockHouse.Y);
        var ring = Cage.Rect(-P.CageWidth * 0.5f, -P.CageDepth * 0.5f, P.CageWidth * 0.5f, P.CageDepth * 0.5f);
        for (var i = 0; i < 4; i++) c.Wall(ring[i], ring[(i + 1) % 4], 0.08f, y, y + 0.3f + P.CageWall + P.CageRoofRise, SurfaceTag.Wall, FootMat.Metal);
        var hy = c.Ground(P.HillTop.X, P.HillTop.Y);
        var half = P.PavilionSize * 0.5f - P.PavilionPost * 0.5f;
        foreach (var x in new[] { -half, half })
            foreach (var z in new[] { -half, half })
                c.Add(new PostCollider(Hill.P(x, z), P.PavilionPost * 0.75f, hy, hy + 0.3f + P.PavilionEave, SurfaceTag.Prop) { Foot = FootMat.WoodDeck });
        foreach (var (a, e) in SeatArc())
        {
            var seg = PlanFrame.Along((a + e) * 0.5f, e - a);
            var ground = c.Ground(seg.Origin.X, seg.Origin.Y);
            c.Box(seg.At(0, ground + P.SeatWallHeight * 0.5f, 0), new(Vector2.Distance(a, e) * 0.5f, P.SeatWallHeight * 0.5f, 0.21f), SurfaceTag.Prop, FootMat.StonePaving);
        }
    }
}
