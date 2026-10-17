using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Gen.Parts.Core;
using Campus.Gen.Zones.Core.Framework;
using Campus.Gen.Zones.Core.Library;
using S = Campus.Gen.Zones.Core.CoreStyle;

namespace Campus.Gen.Zones.Core.Annex;

/// <summary>
/// 宁静苑 (16e, MAP-QSH-CORE §1.9, MAP-QSH-LIBRARY §9): the closed court between the library's north podium, the low block
/// 8408, the court wing 8406 and the north-east connector 8404. Not open; it is a picture seen from the 1F north windows
/// and from the 3F G terrace above, so it is composed from above: a cut-granite court with a lighter stepping path, a red
/// timber pergola along the wing, a grey granite colonnade along the library wall, a hexagonal timber pavilion in the
/// widest free spot and two timber-edged planters with clipped shrubs, each holding a palm (棕榈, planted by WP-23 from
/// overlay/core.d/). Everything is laid out from the court's own walls, so it follows the data, not coordinates.
/// </summary>
public sealed class QuietGarden : ICorePiece
{
    private readonly Surface _ground;

    private QuietGarden(IReadOnlyList<WallPiece> walls, Surface ground)
    {
        Walls = walls;
        _ground = ground;
        Ring = walls.Select(w => w.A).Reverse().ToArray();
        Pergola = walls.Where(w => w.Block.Source == CoreFeatures.CourtWing.Value).OrderByDescending(w => w.Length).First();
        Colonnade = walls.Where(w => w.Block.Kind is BlockKind.Podium or BlockKind.Wing).OrderByDescending(w => w.Length).FirstOrDefault();
        Pavilion = Widest([], S.PavilionRadius + S.PavilionOverhang + 0.6f);
        var first = Widest([(Pavilion, S.PavilionRadius + S.PavilionOverhang + S.PlanterGap)], PlanterReach);
        var second = Widest([(Pavilion, S.PavilionRadius + S.PavilionOverhang + S.PlanterGap), (first, 2 * PlanterReach + S.PlanterGap)], PlanterReach);
        Planters = [first, second];
    }

    public static QuietGarden? From(IWorldModel world, AnnexSite? site)
    {
        ArgumentNullException.ThrowIfNull(world);
        if (site?.Courtyard is not { } walls || !walls.Any(w => w.Block.Source == CoreFeatures.CourtWing.Value)) return null;
        return new QuietGarden(walls, world.Terrain.HeightAt);
    }

    /// <summary>The court's walls in loop order; each piece's Out points into the court.</summary>
    public IReadOnlyList<WallPiece> Walls { get; }
    /// <summary>The court outline, counterclockwise.</summary>
    public IReadOnlyList<Vector2> Ring { get; }
    public WallPiece Pergola { get; }
    public WallPiece? Colonnade { get; }
    public Vector2 Pavilion { get; }
    /// <summary>Centres of the two planters; the palms stand at these points.</summary>
    public IReadOnlyList<Vector2> Planters { get; }

    private static float PlanterReach => MathF.Sqrt(S.PlanterLength * S.PlanterLength + S.PlanterWidth * S.PlanterWidth) * 0.5f;
    private Vector2 Along => Pergola.Along;
    private Vector2 Across => Pergola.Out;

    public string Id => "quiet-garden";
    public Vector2 Anchor => Plan.Centroid([Ring]);
    public IReadOnlyList<Claim> Claims => [];

    // ------------------------------------------------------------------ layout

    /// <summary>
    /// The grid point (0.5 m) inside the court with the largest clearance, counting the pergola and colonnade bands as
    /// wall and keeping <paramref name="keep"/> points at their distance; ties go to the first point in grid order.
    /// </summary>
    private Vector2 Widest(IReadOnlyList<(Vector2 P, float R)> keep, float need)
    {
        float x0 = Ring.Min(p => p.X), x1 = Ring.Max(p => p.X), z0 = Ring.Min(p => p.Y), z1 = Ring.Max(p => p.Y);
        var best = Plan.Centroid([Ring]);
        var bestClear = float.MinValue;
        for (var x = x0 + 0.25f; x < x1; x += 0.5f)
            for (var z = z0 + 0.25f; z < z1; z += 0.5f)
            {
                var p = new Vector2(x, z);
                if (!Inside(p)) continue;
                var clear = float.MaxValue;
                foreach (var w in Walls)
                {
                    var band = ReferenceEquals(w, Pergola) ? S.PergolaInset + S.PergolaDepth + S.PergolaOverhang
                        : ReferenceEquals(w, Colonnade) ? S.ColonnadeInset + S.ColonnadeColumn : 0f;
                    clear = MathF.Min(clear, Distance(p, w.A, w.B) - band);
                }
                foreach (var (q, r) in keep) clear = MathF.Min(clear, Vector2.Distance(p, q) - r);
                if (clear > bestClear + 1e-4f) { bestClear = clear; best = p; }
            }
        return best;
    }

    private bool Inside(Vector2 p)
    {
        var inside = false;
        for (int i = 0, j = Ring.Count - 1; i < Ring.Count; j = i++)
        {
            Vector2 a = Ring[i], b = Ring[j];
            if ((a.Y > p.Y) != (b.Y > p.Y) && p.X < (b.X - a.X) * (p.Y - a.Y) / (b.Y - a.Y) + a.X) inside = !inside;
        }
        return inside;
    }

    private static float Distance(Vector2 p, Vector2 a, Vector2 b)
    {
        var ab = b - a;
        var t = Math.Clamp(Vector2.Dot(p - a, ab) / MathF.Max(ab.LengthSquared(), 1e-9f), 0f, 1f);
        return Vector2.Distance(p, a + ab * t);
    }

    private float Y(Vector2 p) => _ground(p.X, p.Y) + S.PavingLift;

    // ------------------------------------------------------------------ build

    public void Build(CoreBuild build, LodLevel lod)
    {
        var mesh = build.Mesh;
        Court(mesh, lod);
        PergolaMesh(mesh, lod);
        if (Colonnade is { } c) ColonnadeMesh(mesh, c, lod);
        PavilionMesh(mesh, lod);
        foreach (var p in Planters) PlanterMesh(mesh, p, lod);
    }

    /// <summary>The cut-granite court and, from the colonnade to the pavilion, a stepping path of lighter slabs.</summary>
    private void Court(CoreMesh mesh, LodLevel lod)
    {
        var polygon = PolygonTriangulator.Triangulate(new PolygonRings(Ring));
        var v = polygon.Vertices.Select(p => new Vector3(p.X, Y(p), p.Y)).ToArray();
        for (var i = 0; i < polygon.Indices.Length; i += 3)
            mesh.Tri(S.Roof(S.CourtPaving), v[polygon.Indices[i]], v[polygon.Indices[i + 1]], v[polygon.Indices[i + 2]], Vector3.UnitY);
        if (lod > LodLevel.L1 || Colonnade is not { } c) return;
        var from = (c.A + c.B) * 0.5f + c.Out * (S.ColonnadeInset + S.ColonnadeColumn);
        var to = Pavilion - Vector2.Normalize(Pavilion - from) * (S.PavilionRadius + S.PavilionPlinthOut + 0.2f);
        var length = Vector2.Distance(from, to);
        if (length < 1f) return;
        var d = (to - from) / length;
        var count = (int)MathF.Floor(length / 0.85f);
        for (var k = 0; k < count; k++)
        {
            var m = from + d * (0.85f * k + 0.45f);
            var side = new Vector2(-d.Y, d.X);
            Shapes.FlatPolygon(mesh, S.Inlay(S.CourtPath), [m - d * 0.28f - side * 0.48f, m + d * 0.28f - side * 0.48f, m + d * 0.28f + side * 0.48f, m - d * 0.28f + side * 0.48f],
                _ground, S.PavingLift + S.DecalLift);
        }
    }

    /// <summary>A free-standing timber member in pergola coordinates (u along the wing wall, s out from it).</summary>
    private static void Member(CoreMesh mesh, in Paint paint, Face f, float u0, float u1, float s0, float s1, float y0, float y1)
    {
        LibraryFacade.Box(mesh, paint, f, u0, u1, s0, s1, y0, y1);
        LibraryFacade.Panel(mesh, paint, f, u0, u1, s0, y0, y1, -f.N);
    }

    /// <summary>Red timber pergola: two rows of posts, a beam on each row, rafters across with overhanging ends.</summary>
    private void PergolaMesh(CoreMesh mesh, LodLevel lod)
    {
        if (lod > LodLevel.L2) return;
        var red = S.Wall(S.PergolaRed);
        var f = Face.Of(Pergola);
        var length = MathF.Min(Pergola.Length * S.PergolaShare, 11.5f);
        float u0 = (Pergola.Length - length) * 0.5f, u1 = u0 + length;
        float y = Y((Pergola.A + Pergola.B) * 0.5f + Across * (S.PergolaInset + S.PergolaDepth * 0.5f));
        float s0 = S.PergolaInset, s1 = S.PergolaInset + S.PergolaDepth, p = S.PergolaPost * 0.5f, top = y + S.PergolaHeight;
        var posts = Math.Max(2, (int)MathF.Round(length / S.PergolaPostPitch) + 1);
        foreach (var s in new[] { s0 + p, s1 - p })
        {
            for (var i = 0; i < posts; i++)
            {
                var u = u0 + p + (length - 2 * p) * i / (posts - 1);
                Member(mesh, red, f, u - p, u + p, s - p, s + p, y - S.Sink, top - S.PergolaBeam);
                if (lod == LodLevel.L0) Member(mesh, S.Stone(S.ColonnadeGranite), f, u - p - 0.06f, u + p + 0.06f, s - p - 0.06f, s + p + 0.06f, y - S.Sink, y + 0.22f);
            }
            Member(mesh, red, f, u0 - S.PergolaOverhang * 0.6f, u1 + S.PergolaOverhang * 0.6f, s - p * 0.8f, s + p * 0.8f, top - S.PergolaBeam, top);
        }
        if (lod == LodLevel.L2) return;
        var pitch = lod == LodLevel.L0 ? S.PergolaRafterPitch : S.PergolaRafterPitch * 2f;
        var rafters = (int)MathF.Floor(length / pitch) + 1;
        var r = S.PergolaRafter * 0.5f;
        for (var i = 0; i < rafters; i++)
        {
            var u = u0 + length * i / (rafters - 1);
            Member(mesh, red, f, u - r, u + r, s0 - S.PergolaOverhang, s1 + S.PergolaOverhang, top, top + 0.16f);
        }
    }

    /// <summary>Grey granite colonnade: square columns on plinths, capitals, a continuous lintel and tie beams to the wall.</summary>
    private void ColonnadeMesh(CoreMesh mesh, WallPiece wall, LodLevel lod)
    {
        if (lod > LodLevel.L2) return;
        var stone = S.Stone(S.ColonnadeGranite);
        var f = Face.Of(wall);
        float length = wall.Length - 2 * S.ColonnadeMargin;
        if (length < S.ColonnadePitch) return;
        var count = (int)MathF.Floor(length / S.ColonnadePitch) + 1;
        float c = S.ColonnadeColumn * 0.5f, s = S.ColonnadeInset, y = Y((wall.A + wall.B) * 0.5f + wall.Out * s), top = y + S.ColonnadeHeight;
        float first = (wall.Length - (count - 1) * S.ColonnadePitch) * 0.5f;
        for (var i = 0; i < count; i++)
        {
            var u = first + i * S.ColonnadePitch;
            Member(mesh, stone, f, u - c - 0.07f, u + c + 0.07f, s - c - 0.07f, s + c + 0.07f, y - S.Sink, y + S.ColonnadePlinth);
            Member(mesh, stone, f, u - c, u + c, s - c, s + c, y + S.ColonnadePlinth, top - 0.16f);
            if (lod == LodLevel.L0) Member(mesh, stone, f, u - c - 0.05f, u + c + 0.05f, s - c - 0.05f, s + c + 0.05f, top - 0.16f, top);
            LibraryFacade.Box(mesh, stone, f, u - 0.14f, u + 0.14f, 0f, s - c, top + 0.05f, top + S.ColonnadeLintel - 0.08f);
        }
        float e0 = first - c - 0.35f, e1 = first + (count - 1) * S.ColonnadePitch + c + 0.35f;
        Member(mesh, stone, f, e0, e1, s - c - 0.06f, s + c + 0.06f, top, top + S.ColonnadeLintel);
    }

    /// <summary>
    /// Hexagonal timber pavilion: a two-step granite plinth, six posts with a ring beam and a low bench rail (open on the
    /// stepping path's side), a dark tile pyramid roof with a fascia and a closed soffit, and a timber finial.
    /// </summary>
    private void PavilionMesh(CoreMesh mesh, LodLevel lod)
    {
        var c = Pavilion;
        var y = Y(c);
        var turn = DetMath.Atan2(Along.Y, Along.X);
        Vector2 Corner(int k, float r) { var a = turn + Shapes.Tau * k / 6f; return c + new Vector2(DetMath.Cos(a), DetMath.Sin(a)) * r; }
        var granite = S.Stone(S.ColonnadeGranite);
        var timber = S.Wall(S.Timber);
        float rp = S.PavilionRadius + S.PavilionPlinthOut;
        Prism(mesh, granite, Corner, rp, rp, y - S.Sink, y + S.PavilionPlinth * 0.5f, 0.02f);
        Prism(mesh, granite, Corner, rp - 0.28f, rp - 0.28f, y + S.PavilionPlinth * 0.5f, y + S.PavilionPlinth, 0.025f);
        float floor = y + S.PavilionPlinth, eave = y + S.PavilionEave, apex = y + S.PavilionApex, re = S.PavilionRadius + S.PavilionOverhang;
        // Roof: pyramid faces from the eave ring to the apex, fascia below the eave, soffit closing the underside.
        var tile = S.Roof(S.PavilionRoof);
        var top = new Vector3(c.X, apex, c.Y);
        for (var k = 0; k < 6; k++)
        {
            Vector2 a = Corner(k, re), b = Corner(k + 1, re);
            var n = Vector3.Normalize(Vector3.Cross(new Vector3(b.X - a.X, 0, b.Y - a.Y), top - new Vector3(a.X, eave, a.Y)));
            if (n.Y < 0) n = -n;
            mesh.Tri(tile, new(a.X, eave, a.Y), new(b.X, eave, b.Y), top, n);
        }
        Prism(mesh, S.Wall(S.Timber), Corner, re, re, eave - S.PavilionFascia, eave, 0f, soffit: true);
        Prism(mesh, timber, Corner, S.PavilionRadius + 0.12f, S.PavilionRadius + 0.12f, eave - S.PavilionFascia - S.PavilionBeam, eave - S.PavilionFascia, 0f);
        Prism(mesh, timber, Corner, 0.09f, 0.05f, apex - 0.05f, apex + S.PavilionFinial, 0f);
        if (lod > LodLevel.L2) return;
        var path = Colonnade is { } col ? Vector2.Normalize((col.A + col.B) * 0.5f - c) : -Across;
        var gate = Enumerable.Range(0, 6).MaxBy(k => Vector2.Dot(Vector2.Normalize((Corner(k, 1f) + Corner(k + 1, 1f)) * 0.5f - c), path));
        for (var k = 0; k < 6; k++)
        {
            var p = Corner(k, S.PavilionRadius);
            float h = S.PavilionPost * 0.5f;
            Prism(mesh, timber, i => p + new Vector2(DetMath.Cos(turn + Shapes.Tau * i / 4f + 0.785398f), DetMath.Sin(turn + Shapes.Tau * i / 4f + 0.785398f)) * h * 1.414f,
                1f, 1f, floor - 0.02f, eave - S.PavilionFascia, 0f, sides: 4);
            if (k == gate || lod > LodLevel.L1) continue;
            // Bench rail between this post and the next: a seat board on two short legs.
            Vector2 a = Corner(k, S.PavilionRadius - 0.05f), b = Corner(k + 1, S.PavilionRadius - 0.05f);
            var f = new Face(a, Vector2.Normalize(b - a), Vector2.Normalize(c - (a + b) * 0.5f), Vector2.Distance(a, b));
            Member(mesh, timber, f, h, f.Length - h, -0.02f, 0.38f, floor + S.PavilionRail - 0.06f, floor + S.PavilionRail);
            Member(mesh, timber, f, h, f.Length - h, -0.02f, 0.06f, floor + S.PavilionRail + 0.28f, floor + S.PavilionRail + 0.36f);
        }
    }

    /// <summary>
    /// A flat-shaded prism on the corner function (k = 0 … sides − 1), corners scaled from r0 at y0 to r1 at y1 about the
    /// pavilion centre; a top chamfer of <paramref name="bevel"/>; optionally its underside (soffit) closed.
    /// </summary>
    private void Prism(CoreMesh mesh, in Paint paint, Func<int, float, Vector2> corner, float r0, float r1, float y0, float y1, float bevel, bool soffit = false)
    {
        Prism(mesh, paint, k => corner(k, r0), r1 / MathF.Max(r0, 1e-4f), y0, y1, bevel, soffit, 6);
    }

    private void Prism(CoreMesh mesh, in Paint paint, Func<int, Vector2> corner, float scale, float scaleTop, float y0, float y1, float bevel, int sides)
    {
        Prism(mesh, paint, corner, scaleTop / MathF.Max(scale, 1e-4f), y0, y1, bevel, false, sides);
    }

    private static void Prism(CoreMesh mesh, in Paint paint, Func<int, Vector2> corner, float topScale, float y0, float y1, float bevel, bool soffit, int sides)
    {
        var bottom = Enumerable.Range(0, sides).Select(corner).ToArray();
        var centre = bottom.Aggregate(Vector2.Zero, (s, p) => s + p) / sides;
        var top = bottom.Select(p => centre + (p - centre) * topScale).ToArray();
        var inset = top.Select(p => { var d = p - centre; var len = d.Length(); return len > bevel * 2 ? centre + d * ((len - bevel) / len) : p; }).ToArray();
        var yb = bevel > 0 ? y1 - bevel : y1;
        for (var k = 0; k < sides; k++)
        {
            Vector2 a = bottom[k], b = bottom[(k + 1) % sides], ta = top[k], tb = top[(k + 1) % sides];
            var mid = (a + b) * 0.5f - centre;
            var n = Vector3.Normalize(new Vector3(mid.X, 0, mid.Y));
            mesh.Quad(paint, new(a.X, y0, a.Y), new(b.X, y0, b.Y), new(tb.X, yb, tb.Y), new(ta.X, yb, ta.Y), n);
            if (bevel > 0)
            {
                Vector2 ia = inset[k], ib = inset[(k + 1) % sides];
                mesh.Quad(paint, new(ta.X, yb, ta.Y), new(tb.X, yb, tb.Y), new(ib.X, y1, ib.Y), new(ia.X, y1, ia.Y), Vector3.Normalize(n + Vector3.UnitY));
            }
        }
        var cap = bevel > 0 ? inset : top;
        for (var k = 1; k + 1 < sides; k++)
            mesh.Tri(paint, new(cap[0].X, y1, cap[0].Y), new(cap[k].X, y1, cap[k].Y), new(cap[k + 1].X, y1, cap[k + 1].Y), Vector3.UnitY);
        if (!soffit) return;
        for (var k = 1; k + 1 < sides; k++)
            mesh.Tri(paint, new(bottom[0].X, y0, bottom[0].Y), new(bottom[k].X, y0, bottom[k].Y), new(bottom[k + 1].X, y0, bottom[k + 1].Y), -Vector3.UnitY);
    }

    /// <summary>A timber-edged planter: four edge boards with a chamfered cap, dark soil, clipped shrub cushions round the palm.</summary>
    private void PlanterMesh(CoreMesh mesh, Vector2 c, LodLevel lod)
    {
        float hl = S.PlanterLength * 0.5f, hw = S.PlanterWidth * 0.5f, e = S.PlanterEdge, y = Y(c), top = y + S.PlanterHeight;
        var f = new Face(c - Along * hl - Across * hw, Along, -Across, S.PlanterLength);
        var wood = S.Wall(S.Timber);
        // Edge boards (u along, s across: the face runs along one long side, s = 0 there, the box spans s 0 … −width).
        Member(mesh, wood, f, 0f, S.PlanterLength, -e, 0f, y - S.Sink, top);
        Member(mesh, wood, f, 0f, S.PlanterLength, -S.PlanterWidth, -S.PlanterWidth + e, y - S.Sink, top);
        Member(mesh, wood, f, 0f, e, -S.PlanterWidth + e, -e, y - S.Sink, top);
        Member(mesh, wood, f, S.PlanterLength - e, S.PlanterLength, -S.PlanterWidth + e, -e, y - S.Sink, top);
        if (lod <= LodLevel.L1)
            foreach (var (s0, s1) in new[] { (-e - 0.02f, 0.02f), (-S.PlanterWidth - 0.02f, -S.PlanterWidth + e + 0.02f) })
                Member(mesh, wood, f, -0.02f, S.PlanterLength + 0.02f, s0, s1, top, top + 0.04f);
        var soil = top - 0.06f;
        Vector2 sa = c - Along * (hl - e) - Across * (hw - e), sb = c + Along * (hl - e) - Across * (hw - e), sc = c + Along * (hl - e) + Across * (hw - e), sd = c - Along * (hl - e) + Across * (hw - e);
        mesh.Quad(S.Roof(S.PlanterSoil), new(sa.X, soil, sa.Y), new(sb.X, soil, sb.Y), new(sc.X, soil, sc.Y), new(sd.X, soil, sd.Y), Vector3.UnitY);
        if (lod > LodLevel.L2) return;
        int rings = lod == LodLevel.L0 ? 4 : 2, sides = lod == LodLevel.L0 ? 10 : 6;
        // Two clipped rows along the long sides and a low cushion round the palm's foot, so the bed reads full with or without the tree.
        var hedge = new Paint(CoreMaterial.Foliage, S.BoxHedge);
        var row = hw - e - S.ShrubWidth * 0.5f - 0.04f;
        foreach (var sign in new[] { -1f, 1f })
            Shapes.Hedge(mesh, hedge, c - Along * (hl - e - 0.32f) + Across * (sign * row), c + Along * (hl - e - 0.32f) + Across * (sign * row),
                soil, S.ShrubWidth, S.ShrubHeight, rings, sides);
        Shapes.Cushion(mesh, hedge, new(c.X, soil, c.Y), Along, 0.75f, 0.42f, S.ShrubHeight * 0.6f, rings, sides);
    }

    // ------------------------------------------------------------------ collision

    /// <summary>The court is closed (no walking in); its solids still block a bypass camera's body: planters, pavilion, posts.</summary>
    public IReadOnlyList<Collider> Collide()
    {
        var list = new List<Collider>();
        var y = Y(Pavilion);
        list.Add(new PostCollider(Pavilion, S.PavilionRadius + S.PavilionPlinthOut, y - S.Sink, y + S.PavilionEave, SurfaceTag.Prop) { Foot = FootMat.StonePaving });
        foreach (var p in Planters)
        {
            var at = Shapes.Facing(new(p.X, Y(p) + S.PlanterHeight * 0.5f, p.Y), Across);
            list.Add(new BoxCollider(at, new(S.PlanterLength * 0.5f, S.PlanterHeight * 0.5f, S.PlanterWidth * 0.5f), SurfaceTag.Prop) { Foot = FootMat.WoodDeck });
        }
        return list;
    }
}
