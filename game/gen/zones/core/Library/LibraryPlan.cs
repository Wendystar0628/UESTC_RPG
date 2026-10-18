using Campus.Gen.Zones.Common;
using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Parts.Core;
using Campus.Gen.World;
using Campus.Gen.Zones.Core.Framework;
using S = Campus.Gen.Zones.Core.CoreStyle;
using FacadeRules = Campus.Gen.Buildings.Rules.FacadeRules;

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

    private LibraryPlan(IWorldModel world, float padY, IReadOnlyList<Block> blocks, IReadOnlyList<WallPiece> pieces, Vector2 centre, float faceAngle,
        IReadOnlyList<(Vector2 A, Vector2 B)> landing)
    {
        PadY = padY;
        Blocks = blocks;
        Pieces = pieces;
        Centre = centre;
        FaceAngle = faceAngle;
        NorthFacet = (int)world.Feature(CoreIds.LibraryRoofFace).Number("northFacet");
        Landing = landing;
        EntranceFace = CoreContours.Wall(world, CoreIds.LibraryFrontFace,
            pieces.Where(p => p.Block.Kind == BlockKind.FrontTower && MathF.Abs(p.Y0 - SecondFloorY) < 0.01f));
        var cafeFace = CoreContours.Wall(world, CoreIds.LibraryCafeFace,
            pieces.Where(p => p.Block.Kind == BlockKind.Podium && p.Y0 <= FloorY));
        CafeFace = cafeFace is null ? null : (cafeFace, world.Feature(CoreIds.LibraryCafeFace).Text("doorAt") == "start");
    }

    public float PadY { get; }
    public float FloorY => PadY + FacadeRules.FloorAbovePad;
    public float WallBaseY => PadY - FacadeRules.WallBelowPad;
    public float PlinthTopY => PadY + FacadeRules.PlinthTop;
    public IReadOnlyList<Block> Blocks { get; }
    public IReadOnlyList<WallPiece> Pieces { get; }
    /// <summary>Centre of the octagon (the core's centroid): the skylight apex stands above it.</summary>
    public Vector2 Centre { get; }
    /// <summary>Angle (x = cos, z = sin) of one long face's outward normal; the eight faces are 45° apart.</summary>
    public float FaceAngle { get; }
    public int NorthFacet { get; }
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
    public WallPiece? EntranceFace { get; }
    public float TerraceY => StoreyY(S.LibraryStoreys.Length - 1);
    public float RoofY => StoreyY(S.LibraryStoreys.Length);
    public float SkylightRadius => S.SkylightAcross * 0.5f / MathF.Cos(MathF.Tau / 16f);

    public static LibraryPlan? From(IWorldModel world)
    {
        ArgumentNullException.ThrowIfNull(world);
        if (!world.Features.TryGetValue(CoreIds.Library, out var library) || library.Parts.Count == 0) return null;
        var parts = library.Parts.GroupBy(p => p.SourceId, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.SelectMany(p => WorldFeatureGeometry.PolygonsOf(WorldFeatureGeometry.FacadeGeometryOf(p))).Select(r => PlanGeometry.OpenRing(r.Outer)).ToArray(), StringComparer.Ordinal);
        if (!parts.TryGetValue(CoreIds.LibraryCore, out var cores) || cores.Length != 1) return null;
        var padY = world.Terrain.PadY(CoreIds.Library);
        var floor = padY + FacadeRules.FloorAbovePad;
        var wallBase = padY - FacadeRules.WallBelowPad;
        float Storey(int k)
        {
            var y = floor;
            for (var i = 0; i < k; i++) y += i < S.LibraryStoreys.Length ? S.LibraryStoreys[i] : S.LibraryStoreys[^1];
            return y;
        }
        float second = Storey(1), terrace = Storey(S.LibraryStoreys.Length - 1), roof = Storey(S.LibraryStoreys.Length);

        var blocks = new List<Block> { new(CoreIds.LibraryCore, BlockKind.Core, cores[0], wallBase, roof) };
        foreach (var (source, kind, top) in new[]
        {
            (CoreIds.LibraryTowerNe, BlockKind.Tower, roof + S.TowerRise),
            (CoreIds.LibraryTowerSe, BlockKind.FrontTower, Storey(S.FrontTowerStoreys)),
            (CoreIds.LibraryTowerNw, BlockKind.Tower, roof + S.TowerRise),
            (CoreIds.LibraryTowerSw, BlockKind.Tower, roof + S.TowerRise),
            (CoreIds.LibraryPodium, BlockKind.Podium, second),
        })
            if (parts.TryGetValue(source, out var rings))
                blocks.AddRange(rings.Select(r => new Block(source, kind, r, wallBase, top)));
        var setbacks = CoreIds.LibraryWingSetbacks.Select(world.ContourSegment).ToArray();
        if (parts.TryGetValue(CoreIds.LibraryWings, out var wings))
            foreach (var wing in wings)
            {
                var contour = setbacks.Single(arc => wing.Contains(arc[0]) && wing.Contains(arc[^1]));
                var setback = SetBack(wing, contour, S.TopSetback);
                blocks.Add(new(CoreIds.LibraryWings, BlockKind.Wing, wing, wallBase, setback is null ? roof : terrace));
                if (setback is not null) blocks.Add(new(CoreIds.LibraryWings, BlockKind.Setback, setback, terrace, roof));
            }

        var centre = PlanGeometry.Centroid([cores[0]]);
        var landing = new List<(Vector2, Vector2)>();
        if (world.Features.TryGetValue(CoreIds.LibraryStair, out var stair))
            foreach (var podium in blocks.Where(b => b.Kind == BlockKind.Podium))
                foreach (var ring in WorldFeatureGeometry.PolygonsOf(stair.Parts.Count > 0 ? WorldFeatureGeometry.FacadeGeometryOf(stair.Parts[0]) : stair.Geometry))
                    landing.AddRange(Shared(podium.Ring, ring.Outer));
        var face = world.ContourSegment(CoreIds.LibraryRoofFace);
        var normal = (face[0] + face[^1]) * 0.5f - centre;
        return new LibraryPlan(world, padY, blocks, Exterior(blocks), centre, MathF.Atan2(normal.Y, normal.X), landing);
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
    public static IReadOnlyList<Vector2>? SetBack(IReadOnlyList<Vector2> wing, IReadOnlyList<Vector2> selected, float setback)
    {
        var n = wing.Count;
        var start = Enumerable.Range(0, n).Single(i => wing[i] == selected[0]);
        var count = selected.Count - 1;
        if (count >= n - 1) return null;

        (Vector2 P, Vector2 D) Line(int edge, float shift)
        {
            Vector2 a = wing[edge % n], b = wing[(edge + 1) % n];
            var d = Vector2.Normalize(b - a);
            return (a - new Vector2(d.Y, -d.X) * shift, d);
        }
        Vector2 Intersect(int e0, float s0, int e1, float s1)
        {
            var a = Line(e0, s0); var b = Line(e1, s1);
            return PlanLines.LineIntersection(a.P, a.D, b.P, b.D, 1e-6f) ?? b.P;
        }
        var result = new List<Vector2> { Intersect(start + n - 1, 0f, start, setback) };
        for (var k = 1; k < count; k++) result.Add(Intersect(start + k - 1, setback, start + k, setback));
        result.Add(Intersect(start + count - 1, setback, start + count, 0f));
        for (var k = count + 1; k < n; k++) result.Add(wing[(start + k) % n]);
        // The end points must stay on their neighbouring edges, and the strip must triangulate.
        if (!Between(result[0], wing[(start + n - 1) % n], wing[start]) || !Between(result[count], wing[(start + count) % n], wing[(start + count + 1) % n]))
            return null;
        try { _ = PolygonTriangulator.Triangulate(new PolygonRings(result)); }
        catch (ArgumentException) { return null; }
        return PlanGeometry.OpenRing(result);
    }

    // ------------------------------------------------------------------ helpers

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
        if (MathF.Abs(PlanGeometry.CrossFloat(d, d2)) > 0.05f) return null;
        float tc = Vector2.Dot(c - a, d), te = Vector2.Dot(e - a, d);
        float lo = MathF.Max(0f, MathF.Min(tc, te)), hi = MathF.Min(length, MathF.Max(tc, te));
        if (hi - lo < Tolerance) return null;
        if (MathF.Abs(PlanGeometry.CrossFloat(d2, a + d * lo - c)) > Tolerance || MathF.Abs(PlanGeometry.CrossFloat(d2, a + d * hi - c)) > Tolerance) return null;
        return (lo, hi);
    }

    private static bool Between(Vector2 p, Vector2 a, Vector2 b)
    {
        var d = b - a;
        var t = Vector2.Dot(p - a, d) / d.LengthSquared();
        return t > 0.02f && t < 0.98f;
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

}
