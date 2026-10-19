using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Gen.Parts.Core;
using Campus.Gen.Sites;
using Campus.Gen.Maps.Qingshuihe.South.Framework;
using P = Campus.Gen.Maps.Qingshuihe.South.SouthTune.Park;
using L = Campus.Gen.Maps.Qingshuihe.South.SouthTune.Look;

namespace Campus.Gen.Maps.Qingshuihe.South.ForestPark;

/// <summary>
/// Forest park between the museum and the main building (§3.2). The red maples and cherry trees are WP-23's; this site
/// builds the peacock house, a white steel mesh cage with a hipped mesh roof on the hill's south-east foot, and the small
/// square pavilion with its stone seat wall on the top of the red-maple hill (MAP-QSH-TERRAIN, design addition
/// design:south-hongfeng-pavilion). Everything stands on the terrain height, so the pavilion rises with the hill once the
/// WP-08 evaluator lands; until then it stands on the flat stub.
/// </summary>
public sealed class ForestParkSite : ZoneSite
{
    public const string Pavilion = "design:south-hongfeng-pavilion";

    private static PlanFrame CageOf(IWorldModel world) => new(world.Feature(SouthIds.PeacockHouse).Point(), Vector2.UnitX);

    /// <summary>The pavilion's placement from its overlay feature (D-239): the hill top, the pavilion's side, the seat wall's radius, height and bearing range.</summary>
    private readonly record struct Hilltop(Vector2 At, float Size, float SeatRadius, float SeatHeight, float SeatFrom, float SeatTo)
    {
        public PlanFrame Frame => new(At, Vector2.UnitX);
    }

    private static Hilltop HilltopOf(IWorldModel world)
    {
        var f = world.Feature(Pavilion);
        var bearings = f.Numbers("seatWall", "bearings");
        return new(f.Point(), f.Number("size"), f.Number("seatWall", "radius"), f.Number("seatWall", "height"), bearings[0], bearings[1]);
    }

    public override string Id => "18a.forest-park";

    private static Campus.Contracts.Maps.MapRect CageRange(IWorldModel world) => SiteRanges.Bounds(
        CageOf(world).Rect(-P.CageWidth * .5f, -P.CageDepth * .5f, P.CageWidth * .5f, P.CageDepth * .5f), .15f);

    private static Campus.Contracts.Maps.MapRect HillRange(Hilltop hill) => SiteRanges.Bounds(
        hill.Frame.Rect(-(hill.Size + 1.2f) * .5f, -(hill.Size + 1.2f) * .5f,
            (hill.Size + 1.2f) * .5f, (hill.Size + 1.2f) * .5f)
            .Concat(SeatArc(hill).SelectMany(s => new[] { s.A, s.B })), .25f);

    public override IReadOnlyList<Campus.Contracts.Maps.MapRect> Ranges(IWorldModel world) =>
        [CageRange(world), HillRange(HilltopOf(world))];

    public override void Build(SiteBuilder b)
    {
        var hill = HilltopOf(b.Context.World);
        if (b.Includes(CageRange(b.Context.World))) PeacockHouse(b, b.Ground(CageOf(b.Context.World).Origin));
        if (b.Includes(HillRange(hill))) HillPavilion(b, hill, b.Ground(hill.At));
    }

    // ---- Peacock house (孔雀之家) ----

    private static void PeacockHouse(SiteBuilder b, float y)
    {
        var cage = CageOf(b.Context.World);
        float w = P.CageWidth, d = P.CageDepth, h = P.CageWall, curb = 0.3f;
        var bottom = y + curb;
        var top = bottom + h;
        b.Add(PartGeometryKernel.BeveledBox(w + 0.3f, curb + 0.1f, d + 0.3f, 0.03f, L.Concrete), cage.At(0, y - 0.1f, 0));
        // Frame: posts every ~1.5 m, a ring beam at the eaves and a mid rail.
        var posts = Ring(w, d, 1.5f).ToArray();
        foreach (var p in posts) b.Add(PartGeometryKernel.BeveledBox(0.1f, h, 0.1f, 0.012f, L.WhiteSteel), cage.At(p.X, bottom, p.Y));
        foreach (var (a, e) in Edges(w, d))
        {
            Strut(b, cage, new(a.X, top, a.Y), new(e.X, top, e.Y), 0.12f);
            Strut(b, cage, new(a.X, bottom + 1.0f, a.Y), new(e.X, bottom + 1.0f, e.Y), 0.06f);
        }
        // Hipped roof: hips and rafters to a short ridge, mesh rings that catch the winter snow lines (§11).
        var ridge = new[] { new Vector3(-(w - d) * 0.5f, top + P.CageRoofRise, 0), new Vector3((w - d) * 0.5f, top + P.CageRoofRise, 0) };
        Strut(b, cage, ridge[0], ridge[1], 0.1f);
        foreach (var p in posts)
        {
            var apex = p.X < ridge[0].X ? ridge[0] : p.X > ridge[1].X ? ridge[1] : new Vector3(p.X, ridge[0].Y, 0);
            Strut(b, cage, new(p.X, top, p.Y), apex, 0.07f);
        }
        // Door frame on the south side, facing the woodland path.
        b.Add(PartGeometryKernel.BeveledBox(0.08f, 2.1f, 0.08f, 0.01f, L.WhiteSteel), cage.At(-0.55f, bottom, d * 0.5f));
        b.Add(PartGeometryKernel.BeveledBox(0.08f, 2.1f, 0.08f, 0.01f, L.WhiteSteel), cage.At(0.55f, bottom, d * 0.5f));
        Strut(b, cage, new(-0.55f, bottom + 2.1f, d * 0.5f), new(0.55f, bottom + 2.1f, d * 0.5f), 0.08f);
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
                b.Add(PartGeometryKernel.BeveledBox(rod, h, rod, 0, L.WhiteWire), cage.At(p.X, bottom, p.Y));
            }
            for (var yy = bottom + mesh; yy < top - 0.1f; yy += mesh)
                if (MathF.Abs(yy - bottom - 1.0f) > 0.1f) Strut(b, cage, new(a.X, yy, a.Y), new(e.X, yy, e.Y), rod, L.WhiteWire);
        }
        for (var k = 1; k <= 3; k++)
        {
            var t = k / 4f;
            float hx = w * 0.5f - (d * 0.5f) * t, hz = d * 0.5f * (1 - t), yy = top + P.CageRoofRise * t;
            Vector3[] r = [new(-hx, yy, -hz), new(hx, yy, -hz), new(hx, yy, hz), new(-hx, yy, hz)];
            for (var i = 0; i < 4; i++) Strut(b, cage, r[i], r[(i + 1) % 4], rod * 1.4f, L.WhiteWire);
        }
        // Timber perch inside for the birds' night roost (WLD-LIFE; the peacocks are WP-25's).
        foreach (var x in new[] { -2.2f, 1.8f })
            b.Add(PartGeometryKernel.BeveledBox(0.12f, 2.3f, 0.12f, 0.015f, L.Wood), cage.At(x, bottom, -1.0f));
        Strut(b, cage, new(-2.4f, bottom + 2.1f, -1.0f), new(2.0f, bottom + 2.1f, -1.0f), 0.1f, L.Wood);
        Strut(b, cage, new(-2.4f, bottom + 1.3f, -1.0f), new(2.0f, bottom + 1.3f, -1.0f), 0.08f, L.Wood);
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
        var x = VectorMath.Unit(d);
        // Local Y: the up vector made perpendicular to the rod (or local Z for a vertical rod).
        var up = MathF.Abs(x.Y) > 0.95f ? new Vector3(0, 0, 1) : new Vector3(0, 1, 0);
        var dot = Vector3.Dot(up, x);
        var yv = up - x * dot;
        yv = VectorMath.Unit(yv);
        var zv = VectorMath.Cross(x, yv);
        var mid = (a + e) * 0.5f - yv * (size * 0.5f);
        var local = new Matrix4x4(x.X, x.Y, x.Z, 0, yv.X, yv.Y, yv.Z, 0, zv.X, zv.Y, zv.Z, 0, mid.X, mid.Y, mid.Z, 1);
        b.Add(PartGeometryKernel.BeveledBox(length, size, size, 0, material ?? L.WhiteSteel), GeometryPlacement.Multiply(local, f.At(0, 0, 0)));
    }

    // ---- Hill-top pavilion and seat wall (MAP-QSH-TERRAIN) ----

    private static void HillPavilion(SiteBuilder b, Hilltop hill, float y)
    {
        var pad = hill.Frame;
        float s = hill.Size, post = P.PavilionPost, eave = P.PavilionEave;
        b.Add(PartGeometryKernel.BeveledBox(s + 0.8f, 0.4f, s + 0.8f, 0.03f, L.Granite), pad.At(0, y - 0.1f, 0));
        var floor = y + 0.3f;
        var half = s * 0.5f - post * 0.5f;
        foreach (var x in new[] { -half, half })
            foreach (var z in new[] { -half, half })
            {
                b.Add(PartGeometryKernel.BeveledBox(post, eave, post, 0.02f, L.Wood), pad.At(x, floor, z));
                b.Add(PartGeometryKernel.BeveledBox(post + 0.12f, 0.2f, post + 0.12f, 0.015f, L.StoneTrim), pad.At(x, floor, z));
            }
        foreach (var (a, e) in Edges(s - post, s - post))
            Strut(b, pad, new(a.X, floor + eave - 0.32f, a.Y), new(e.X, floor + eave - 0.32f, e.Y), 0.3f, L.Wood);
        b.Add(PartGeometryKernel.BeveledBox(s + 1.2f, 0.12f, s + 1.2f, 0.02f, L.Wood), pad.At(0, floor + eave - 0.02f, 0));
        b.Add(Shapes.Hip(s + 1.1f, s + 1.1f, P.PavilionRoofRise, 0, L.RoofSlab), pad.At(0, floor + eave + 0.1f, 0));
        if (b.Detail != SiteDetail.Mass)
            b.Add(Shapes.Shaft(0.1f, 0.03f, 0.35f, 10, L.Metal), pad.At(0, floor + eave + 0.1f + P.PavilionRoofRise - 0.02f, 0));
        // Stone seat wall on the north rim, facing the lake and the museum: a polygonal arc of 0.45 m blocks.
        foreach (var (a, e) in SeatArc(hill))
        {
            if (!b.Includes(SiteRanges.Bounds([a, e], .25f))) continue;
            var seg = PlanFrame.Along((a + e) * 0.5f, e - a);
            var ground = b.Ground(seg.Origin);
            b.Add(PartGeometryKernel.BeveledBox(Vector2.Distance(a, e) + 0.02f, hill.SeatHeight + 0.1f, 0.42f, 0.025f, L.Granite), seg.At(0, ground - 0.1f, 0));
        }
    }

    /// <summary>Seven chords of the seat-wall arc round the hill top, over the bearing range the overlay gives (north rim).</summary>
    private static IEnumerable<(Vector2 A, Vector2 B)> SeatArc(Hilltop hill)
    {
        const int n = 7;
        Vector2 At(int k)
        {
            var bearing = (hill.SeatFrom + (hill.SeatTo - hill.SeatFrom) * k / n) * MathF.PI / 180f;
            return hill.At + new Vector2(MathF.Sin(bearing), -MathF.Cos(bearing)) * hill.SeatRadius;
        }
        for (var k = 0; k < n; k++) yield return (At(k), At(k + 1));
    }

    public override void Collide(SiteColliders c)
    {
        if (c.Includes(CageRange(c.Context.World))) CollideCage(c);
        var hill = HilltopOf(c.Context.World);
        if (c.Includes(HillRange(hill))) CollideHill(c, hill);
    }

    private static void CollideCage(SiteColliders c)
    {
        var cage = CageOf(c.Context.World);
        var y = c.Ground(cage.Origin.X, cage.Origin.Y);
        var ring = cage.Rect(-P.CageWidth * 0.5f, -P.CageDepth * 0.5f, P.CageWidth * 0.5f, P.CageDepth * 0.5f);
        for (var i = 0; i < 4; i++) c.Wall(ring[i], ring[(i + 1) % 4], 0.08f, y, y + 0.3f + P.CageWall + P.CageRoofRise, SurfaceTag.Wall, FootMat.Metal);
    }

    private static void CollideHill(SiteColliders c, Hilltop hill)
    {
        var pad = hill.Frame;
        var hy = c.Ground(hill.At.X, hill.At.Y);
        var half = hill.Size * 0.5f - P.PavilionPost * 0.5f;
        foreach (var x in new[] { -half, half })
            foreach (var z in new[] { -half, half })
                c.Add(new PostCollider(pad.P(x, z), P.PavilionPost * 0.75f, hy, hy + 0.3f + P.PavilionEave, SurfaceTag.Prop) { Foot = FootMat.WoodDeck });
        foreach (var (a, e) in SeatArc(hill))
        {
            if (!c.Includes(SiteRanges.Bounds([a, e], .25f))) continue;
            var seg = PlanFrame.Along((a + e) * 0.5f, e - a);
            var ground = c.Ground(seg.Origin.X, seg.Origin.Y);
            c.Box(seg.At(0, ground + hill.SeatHeight * 0.5f, 0), new(Vector2.Distance(a, e) * 0.5f, hill.SeatHeight * 0.5f, 0.21f), SurfaceTag.Prop, FootMat.StonePaving);
        }
    }
}
