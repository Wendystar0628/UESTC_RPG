using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Parts.Core;
using Campus.Gen.World;
using Campus.Gen.Zones.Core.Framework;
using S = Campus.Gen.Zones.Core.CoreStyle;

namespace Campus.Gen.Zones.Core.Library;

/// <summary>Role of one massing block of the library entity (MAP-QSH-CORE "数据定位").</summary>
public enum BlockKind
{
    /// <summary>8397: the one-storey ring around everything; its roof is the 2F terrace and the stair landing.</summary>
    Podium,
    /// <summary>8396: the four outer reading wings, 2F–4F above the podium, roofed by the 5F terrace.</summary>
    Wing,
    /// <summary>The 5F part of a wing, set back from its outer faces (derived, not in the data).</summary>
    Setback,
    /// <summary>8395: the octagonal core around the atrium, roofed by the ring roof and the skylight.</summary>
    Core,
    /// <summary>8391, 8393, 8394: corner blocks with the stair cores and vertical slits.</summary>
    Tower,
    /// <summary>8392: the front square tower facing the time plaza, with the glass slot and the 2F door.</summary>
    FrontTower,
    /// <summary>16e: one of the library's north blocks (8403, 8406, 8408, 8409, 8410), built by its AnnexShell.</summary>
    Annex,
    /// <summary>16e: a block that only covers walls standing against it (the library's own, 8404, 8405, 8407).</summary>
    Neighbour,
}

/// <summary>A vertical prism of the shell: plan ring (counterclockwise in x, z) from Y0 to Y1.</summary>
public sealed record Block(string Source, BlockKind Kind, IReadOnlyList<Vector2> Ring, float Y0, float Y1);

/// <summary>
/// One exterior wall rectangle: the part of a block edge from A to B, between Y0 and Y1, that no other block covers.
/// <see cref="Out"/> is the horizontal outward normal; <see cref="Top"/> says the piece reaches its block's top.
/// </summary>
public sealed record WallPiece(Block Block, Vector2 A, Vector2 B, Vector2 Out, float Y0, float Y1)
{
    public float Length => Vector2.Distance(A, B);
    public Vector2 Along => Vector2.Normalize(B - A);
    public bool Top => MathF.Abs(Y1 - Block.Y1) < 1e-3f;
}

/// <summary>
/// The library shell's plan and section, a pure function of the world model: the blocks from the entity's source parts
/// (MAP-QSH-CORE "数据定位"), the derived 5F set-back strips, every exterior wall piece, and the octagon frame the roof
/// is laid out on. Heights come from the pad (WLD-TERRAIN) and the storey table in <see cref="CoreStyle"/>.
/// </summary>
public sealed class LibraryPlan
{
    private const float Tolerance = 0.08f;

    private LibraryPlan(float padY, IReadOnlyList<Block> blocks, IReadOnlyList<WallPiece> pieces, Vector2 centre, float faceAngle,
        IReadOnlyList<(Vector2 A, Vector2 B)> landing, IReadOnlyList<Vector2>? store)
    {
        PadY = padY;
        Blocks = blocks;
        Pieces = pieces;
        Centre = centre;
        FaceAngle = faceAngle;
        Landing = landing;
        CafeFace = store is null ? null : CafeFaceBeside(pieces, store, PadY + S.FloorAbovePad);
    }

    public float PadY { get; }
    public float FloorY => PadY + S.FloorAbovePad;
    public float WallBaseY => PadY - S.WallBelowPad;
    public float PlinthTopY => PadY + S.PlinthTop;
    public IReadOnlyList<Block> Blocks { get; }
    public IReadOnlyList<WallPiece> Pieces { get; }
    /// <summary>Centre of the octagon (the core's centroid): the skylight apex stands above it.</summary>
    public Vector2 Centre { get; }
    /// <summary>Angle (x = cos, z = sin) of one long face's outward normal; the eight faces are 45° apart.</summary>
    public float FaceAngle { get; }
    /// <summary>Podium edges the grand stair base (8398) stands against: no parapet and a low collider (16c lands here).</summary>
    public IReadOnlyList<(Vector2 A, Vector2 B)> Landing { get; }
    /// <summary>
    /// The podium face the 1F cafe door (lib.cafe) opens in: the outward ground-floor podium face that starts at a corner of
    /// the 品·悦吧 store front (8407), and whether the door sits at its A end. Null without the store or such a face.
    /// </summary>
    public (WallPiece Piece, bool AtA)? CafeFace { get; }
    public float StoreyY(int k)
    {
        var y = FloorY;
        for (var i = 0; i < k; i++) y += i < S.LibraryStoreys.Length ? S.LibraryStoreys[i] : S.LibraryStoreys[^1];
        return y;
    }

    public float SecondFloorY => StoreyY(1);

    /// <summary>The front tower's face above the 2F terrace, toward the plaza: the glass slot and the front door are on it.</summary>
    public WallPiece? EntranceFace => Pieces.Where(p => p.Block.Kind == BlockKind.FrontTower && p.Length >= 8f && MathF.Abs(p.Y0 - SecondFloorY) < 0.01f)
        .OrderByDescending(p => p.Length).FirstOrDefault();
    public float TerraceY => StoreyY(S.LibraryStoreys.Length - 1);
    public float RoofY => StoreyY(S.LibraryStoreys.Length);
    public float SkylightRadius => S.SkylightAcross * 0.5f / DetMath.Cos(Shapes.Tau / 16f);

    public static LibraryPlan? From(IWorldModel world)
    {
        ArgumentNullException.ThrowIfNull(world);
        if (!world.Features.TryGetValue(CoreFeatures.Library, out var library) || library.Parts.Count == 0) return null;
        var parts = library.Parts.GroupBy(p => p.SourceId, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.SelectMany(p => Plan.Polygons(PlanGeometry.FacadeGeometryOf(p))).Select(r => Ccw(r.Outer)).ToArray(), StringComparer.Ordinal);
        if (!parts.TryGetValue(CoreFeatures.LibraryCore, out var cores) || cores.Length != 1) return null;
        var padY = world.Terrain.PadY(CoreFeatures.Library);
        var floor = padY + S.FloorAbovePad;
        var wallBase = padY - S.WallBelowPad;
        float Storey(int k)
        {
            var y = floor;
            for (var i = 0; i < k; i++) y += i < S.LibraryStoreys.Length ? S.LibraryStoreys[i] : S.LibraryStoreys[^1];
            return y;
        }
        float second = Storey(1), terrace = Storey(S.LibraryStoreys.Length - 1), roof = Storey(S.LibraryStoreys.Length);

        var blocks = new List<Block> { new(CoreFeatures.LibraryCore, BlockKind.Core, cores[0], wallBase, roof) };
        foreach (var (source, kind, top) in new[]
        {
            (CoreFeatures.LibraryTowerNe, BlockKind.Tower, roof + S.TowerRise),
            (CoreFeatures.LibraryTowerSe, BlockKind.FrontTower, Storey(S.FrontTowerStoreys)),
            (CoreFeatures.LibraryTowerNw, BlockKind.Tower, roof + S.TowerRise),
            (CoreFeatures.LibraryTowerSw, BlockKind.Tower, roof + S.TowerRise),
            (CoreFeatures.LibraryPodium, BlockKind.Podium, second),
        })
            if (parts.TryGetValue(source, out var rings))
                blocks.AddRange(rings.Select(r => new Block(source, kind, r, wallBase, top)));
        var solids = blocks.Where(b => b.Kind is BlockKind.Core or BlockKind.Tower or BlockKind.FrontTower).ToArray();
        if (parts.TryGetValue(CoreFeatures.LibraryWings, out var wings))
            foreach (var wing in wings)
            {
                var setback = SetBack(wing, solids, S.TopSetback);
                blocks.Add(new(CoreFeatures.LibraryWings, BlockKind.Wing, wing, wallBase, setback is null ? roof : terrace));
                if (setback is not null) blocks.Add(new(CoreFeatures.LibraryWings, BlockKind.Setback, setback, terrace, roof));
            }

        var centre = Plan.Centroid([cores[0]]);
        var landing = new List<(Vector2, Vector2)>();
        if (world.Features.TryGetValue(CoreFeatures.LibraryStair, out var stair))
            foreach (var podium in blocks.Where(b => b.Kind == BlockKind.Podium))
                foreach (var ring in Plan.Polygons(stair.Parts.Count > 0 ? PlanGeometry.FacadeGeometryOf(stair.Parts[0]) : stair.Geometry))
                    landing.AddRange(Shared(podium.Ring, ring.Outer));
        var store = world.Features.TryGetValue(CoreFeatures.CafeStore, out var cafe) ? Plan.Polygons(cafe.Geometry).FirstOrDefault()?.Outer : null;
        return new LibraryPlan(padY, blocks, Exterior(blocks), centre, FaceAngleOf(cores[0], centre), landing, store);
    }

    /// <summary>
    /// Ground-floor podium faces whose outside is open (not inside the store) and that end within 1 m of a store corner; of
    /// those the longest, the door going to the end at the store.
    /// </summary>
    private static (WallPiece, bool)? CafeFaceBeside(IReadOnlyList<WallPiece> pieces, IReadOnlyList<Vector2> store, float floor)
    {
        static float Near(Vector2 p, IReadOnlyList<Vector2> ring) => ring.Min(q => Vector2.Distance(p, q));
        var best = pieces
            .Where(p => p.Block.Kind == BlockKind.Podium && p.Y0 <= floor && !Inside((p.A + p.B) * 0.5f + p.Out * 0.5f, store))
            .Select(p => (Piece: p, AtA: Near(p.A, store) <= Near(p.B, store), Gap: MathF.Min(Near(p.A, store), Near(p.B, store))))
            .Where(c => c.Gap < 1f)
            .OrderByDescending(c => c.Piece.Length).FirstOrDefault();
        return best.Piece is null ? null : (best.Piece, best.AtA);
    }

    /// <summary>Even-odd point in polygon.</summary>
    private static bool Inside(Vector2 p, IReadOnlyList<Vector2> ring)
    {
        var inside = false;
        for (int i = 0, j = ring.Count - 1; i < ring.Count; j = i++)
        {
            Vector2 a = ring[i], b = ring[j];
            if ((a.Y > p.Y) != (b.Y > p.Y) && p.X < (b.X - a.X) * (p.Y - a.Y) / (b.Y - a.Y) + a.X) inside = !inside;
        }
        return inside;
    }

    // ------------------------------------------------------------------ exterior wall pieces

    /// <summary>
    /// Splits every block edge where other blocks' collinear edges start and end, and keeps each span's height range
    /// minus the ranges of the blocks standing against it. What is left is seen from outside.
    /// </summary>
    public static IReadOnlyList<WallPiece> Exterior(IReadOnlyList<Block> blocks)
    {
        var pieces = new List<WallPiece>();
        foreach (var block in blocks)
        {
            var ring = block.Ring;
            for (var i = 0; i < ring.Count; i++)
            {
                Vector2 a = ring[i], b = ring[(i + 1) % ring.Count];
                var length = Vector2.Distance(a, b);
                if (length < Tolerance) continue;
                var d = (b - a) / length;
                var outward = new Vector2(d.Y, -d.X);
                var covers = new List<(float T0, float T1, float Y0, float Y1)>();
                var cuts = new List<float> { 0f, length };
                foreach (var other in blocks)
                {
                    if (ReferenceEquals(other, block)) continue;
                    for (var j = 0; j < other.Ring.Count; j++)
                    {
                        if (Overlap(a, b, other.Ring[j], other.Ring[(j + 1) % other.Ring.Count]) is not (var lo, var hi)) continue;
                        covers.Add((lo, hi, other.Y0, other.Y1));
                        cuts.Add(lo); cuts.Add(hi);
                    }
                }
                cuts.Sort();
                WallPiece? open = null;
                List<(float, float)>? openRanges = null;
                for (var k = 0; k + 1 < cuts.Count; k++)
                {
                    float t0 = cuts[k], t1 = cuts[k + 1];
                    if (t1 - t0 < Tolerance) continue;
                    var mid = (t0 + t1) * 0.5f;
                    var ranges = Subtract(block.Y0, block.Y1, covers.Where(c => c.T0 <= mid && c.T1 >= mid).Select(c => (c.Y0, c.Y1)));
                    // Neighbouring spans with the same open heights stay one piece, so window rows run through.
                    if (open is not null && openRanges!.SequenceEqual(ranges) && Vector2.Distance(open.B, a + d * t0) < Tolerance)
                    {
                        var end = a + d * t1;
                        for (var p = pieces.Count - ranges.Count; p < pieces.Count; p++) pieces[p] = pieces[p] with { B = end };
                        open = open with { B = end };
                        continue;
                    }
                    foreach (var (y0, y1) in ranges) pieces.Add(new WallPiece(block, a + d * t0, a + d * t1, outward, y0, y1));
                    open = ranges.Count == 0 ? null : new WallPiece(block, a + d * t0, a + d * t1, outward, 0, 0);
                    openRanges = ranges;
                }
            }
        }
        return pieces;
    }

    private static List<(float, float)> Subtract(float y0, float y1, IEnumerable<(float Y0, float Y1)> covers)
    {
        var result = new List<(float, float)> { (y0, y1) };
        foreach (var (c0, c1) in covers.OrderBy(c => c.Y0).ThenBy(c => c.Y1))
        {
            var next = new List<(float, float)>();
            foreach (var (r0, r1) in result)
            {
                if (c1 <= r0 + 1e-3f || c0 >= r1 - 1e-3f) { next.Add((r0, r1)); continue; }
                if (c0 > r0 + Tolerance) next.Add((r0, c0));
                if (c1 < r1 - Tolerance) next.Add((c1, r1));
            }
            result = next;
        }
        return result;
    }

    // ------------------------------------------------------------------ set-back strips

    /// <summary>
    /// The 5F strip of a wing: its run of outer edges (those not standing against the core or a tower) moved inward by
    /// <paramref name="setback"/>, the two end points slid along the neighbouring edges. Null when the wing has no clean
    /// outer run or the result is not a simple polygon; the wing then rises to the roof without a terrace.
    /// </summary>
    public static IReadOnlyList<Vector2>? SetBack(IReadOnlyList<Vector2> wing, IReadOnlyList<Block> solids, float setback)
    {
        var n = wing.Count;
        var outer = new bool[n];
        for (var i = 0; i < n; i++)
        {
            Vector2 a = wing[i], b = wing[(i + 1) % n];
            var length = Vector2.Distance(a, b);
            if (length < Tolerance) return null;
            var covered = 0f;
            foreach (var solid in solids)
                for (var j = 0; j < solid.Ring.Count; j++)
                    if (Overlap(a, b, solid.Ring[j], solid.Ring[(j + 1) % solid.Ring.Count]) is (var lo, var hi))
                        covered += hi - lo;
            outer[i] = covered < 0.5f * length;
        }
        // The single cyclic run of outer edges.
        var starts = Enumerable.Range(0, n).Where(i => outer[i] && !outer[(i + n - 1) % n]).ToArray();
        if (starts.Length != 1) return null;
        var start = starts[0];
        var count = 0;
        while (count < n && outer[(start + count) % n]) count++;
        if (count >= n - 1) return null;

        (Vector2 P, Vector2 D) Line(int edge, float shift)
        {
            Vector2 a = wing[edge % n], b = wing[(edge + 1) % n];
            var d = Vector2.Normalize(b - a);
            return (a - new Vector2(d.Y, -d.X) * shift, d);
        }
        var result = new List<Vector2> { Intersect(Line(start + n - 1, 0f), Line(start, setback)) };
        for (var k = 1; k < count; k++) result.Add(Intersect(Line(start + k - 1, setback), Line(start + k, setback)));
        result.Add(Intersect(Line(start + count - 1, setback), Line(start + count, 0f)));
        for (var k = count + 1; k < n; k++) result.Add(wing[(start + k) % n]);
        // The end points must stay on their neighbouring edges, and the strip must triangulate.
        if (!Between(result[0], wing[(start + n - 1) % n], wing[start]) || !Between(result[count], wing[(start + count) % n], wing[(start + count + 1) % n]))
            return null;
        try { _ = PolygonTriangulator.Triangulate(new PolygonRings(result)); }
        catch (ArgumentException) { return null; }
        return Ccw(result);
    }

    // ------------------------------------------------------------------ helpers

    private static float Cross(Vector2 a, Vector2 b) => a.X * b.Y - a.Y * b.X;

    /// <summary>
    /// The span (distances from a along a → b) where segment c–e runs along segment a–b: nearly parallel (within about
    /// 3°) and within <see cref="Tolerance"/> of it over the whole span. OSM ways that share a wall do not always share
    /// its nodes, so the test is on the overlap, not on the far endpoints. Null when they do not run together.
    /// </summary>
    public static (float Lo, float Hi)? Overlap(Vector2 a, Vector2 b, Vector2 c, Vector2 e)
    {
        float length = Vector2.Distance(a, b), other = Vector2.Distance(c, e);
        if (length < Tolerance || other < Tolerance) return null;
        Vector2 d = (b - a) / length, d2 = (e - c) / other;
        if (MathF.Abs(Cross(d, d2)) > 0.05f) return null;
        float tc = Vector2.Dot(c - a, d), te = Vector2.Dot(e - a, d);
        float lo = MathF.Max(0f, MathF.Min(tc, te)), hi = MathF.Min(length, MathF.Max(tc, te));
        if (hi - lo < Tolerance) return null;
        if (MathF.Abs(Cross(d2, a + d * lo - c)) > Tolerance || MathF.Abs(Cross(d2, a + d * hi - c)) > Tolerance) return null;
        return (lo, hi);
    }

    private static Vector2 Intersect((Vector2 P, Vector2 D) l0, (Vector2 P, Vector2 D) l1)
    {
        var den = Cross(l0.D, l1.D);
        if (MathF.Abs(den) < 1e-6f) return l1.P;
        var t = Cross(l1.P - l0.P, l1.D) / den;
        return l0.P + l0.D * t;
    }

    private static bool Between(Vector2 p, Vector2 a, Vector2 b)
    {
        var d = b - a;
        var t = Vector2.Dot(p - a, d) / d.LengthSquared();
        return t > 0.02f && t < 0.98f;
    }

    /// <summary>Signed area in (x, z) is positive for the counterclockwise order every block uses.</summary>
    public static Vector2[] Ccw(IReadOnlyList<Vector2> ring)
    {
        double area = 0;
        for (var i = 0; i < ring.Count; i++)
        {
            Vector2 a = ring[i], b = ring[(i + 1) % ring.Count];
            area += (double)a.X * b.Y - (double)b.X * a.Y;
        }
        var list = ring.ToList();
        if (list.Count > 1 && Vector2.Distance(list[0], list[^1]) < 1e-4f) list.RemoveAt(list.Count - 1);
        if (area < 0) list.Reverse();
        return [.. list];
    }

    /// <summary>Edges of ring <paramref name="a"/> that lie along an edge of ring <paramref name="b"/> (the overlapping span).</summary>
    private static IEnumerable<(Vector2, Vector2)> Shared(IReadOnlyList<Vector2> a, IReadOnlyList<Vector2> b)
    {
        for (var i = 0; i < a.Count; i++)
        {
            Vector2 p = a[i], q = a[(i + 1) % a.Count];
            if (Vector2.Distance(p, q) < Tolerance) continue;
            var d = Vector2.Normalize(q - p);
            for (var j = 0; j < b.Count; j++)
                if (Overlap(p, q, b[j], b[(j + 1) % b.Count]) is (var lo, var hi))
                    yield return (p + d * lo, p + d * hi);
        }
    }

    /// <summary>Outward normal angle of the core's longest edge: the octagon's long faces sit at this angle plus k · 90°.</summary>
    private static float FaceAngleOf(IReadOnlyList<Vector2> core, Vector2 centre)
    {
        var best = 0;
        for (var i = 1; i < core.Count; i++)
            if (Vector2.DistanceSquared(core[i], core[(i + 1) % core.Count]) > Vector2.DistanceSquared(core[best], core[(best + 1) % core.Count])) best = i;
        Vector2 a = core[best], b = core[(best + 1) % core.Count];
        var mid = (a + b) * 0.5f - centre;
        return DetMath.Atan2(mid.Y, mid.X);
    }
}
