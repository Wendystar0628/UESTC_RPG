using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Gen.Maps.Qingshuihe.Core.Library;
using Campus.Gen.Maps.Qingshuihe.Core.Framework;
using S = Campus.Gen.Maps.Qingshuihe.Core.LibraryInterior.LibraryInteriorStyle;

namespace Campus.Gen.Maps.Qingshuihe.Core.LibraryInterior;

/// <summary>
/// What every storey has, inside the whole shell and not only the open range (WLD-INTERIORS-ACCESS: beyond the seals only the
/// baseline counts): the floor slab of each block standing in the storey, the ceiling under every roof that closes the storey,
/// and the inner lining of the shell's exterior walls with the shell's own window and door holes, so the hollow shell never
/// shows through from inside. Floor collision covers the same slabs.
/// </summary>
internal static class ShellInterior
{
    public static void Build(LibraryFrame f, int storey, InteriorSink sink)
    {
        Floors(f, storey, sink);
        Ceilings(f, storey, sink);
        Linings(f, storey, sink);
    }

    private static void Floors(LibraryFrame f, int storey, InteriorSink sink)
    {
        var y = f.FloorY(storey);
        var voidRing = f.Octagon(f.VoidApothem, S.OctagonChamfer);
        foreach (var block in f.BlocksAt(storey))
        {
            var core = ReferenceEquals(block, f.Core);
            IReadOnlyList<IReadOnlyList<Vector2>> holes = core && storey > 1 ? [voidRing] : [];
            var rings = new PolygonRings(block.Ring, holes);
            Paint? soffit = storey == 1 || core ? null : S.Wall(S.CeilingWhite);
            sink.Slab(rings, y, S.Stone(S.Floor), soffit, S.Wall(S.WallWhite));
            if (core && storey > 1)
            {
                // The gallery ring's soffit is the dark star ceiling (回廊顶棚深灰色); the sectors beyond take plain white.
                var ring = f.Octagon(f.GalleryApothem);
                sink.Plate(S.Wall(S.StarCeiling), new PolygonRings(ring, [voidRing]), y - S.SlabThick, false);
                sink.Plate(S.Wall(S.CeilingWhite), new PolygonRings(block.Ring, [ring]), y - S.SlabThick, false);
            }
            if (core && storey == 1)
            {
                // The hall's carpet and the atrium's polished stone are separate floors (SYS-AUDIO).
                sink.Floor(new PolygonRings(block.Ring, [voidRing]), y, FootMat.Carpet);
                sink.Floor(new PolygonRings(voidRing), y, FootMat.PolishedStone);
            }
            else sink.Floor(rings, y, FootMat.PolishedStone);
        }
    }

    private static void Ceilings(LibraryFrame f, int storey, InteriorSink sink)
    {
        foreach (var block in f.BlocksAt(storey).Where(b => !f.Continues(b, storey)))
        {
            var y = block.Y1 - S.CeilingGap;
            var core = ReferenceEquals(block, f.Core);
            IReadOnlyList<IReadOnlyList<Vector2>> holes = core ? [f.Octagon(f.GalleryApothem)] : [];
            sink.Plate(S.Wall(S.CeilingWhite), new PolygonRings(block.Ring, holes), y, false);
            if (core) RingBeam(f, sink, y);
        }
    }

    /// <summary>The beige ring beam round the skylight opening, closing the shell's curb from inside up to the glass.</summary>
    private static void RingBeam(LibraryFrame f, InteriorSink sink, float ceiling)
    {
        var inner = f.Octagon(f.GalleryApothem);
        var outer = f.Octagon(f.GalleryApothem + S.RingBeamWidth);
        var top = LibraryRoof.SkylightBaseY(f.Plan) - S.CeilingGap;
        for (var k = 0; k < 8; k++)
            sink.Prism(S.Wall(S.RingBeam), [inner[k], inner[(k + 1) % 8], outer[(k + 1) % 8], outer[k]], ceiling - S.RingBeamDrop, top);
    }

    /// <summary>
    /// Lines every exterior wall piece of a block in this storey from the floor to the next floor (the top storey to the piece's
    /// top), from <see cref="LibraryInteriorStyle.LiningOut"/> to <see cref="LibraryInteriorStyle.LiningIn"/> behind the face,
    /// cut by the shell's holes on that face, its ends mitred onto the neighbouring pieces.
    /// </summary>
    private static void Linings(LibraryFrame f, int storey, InteriorSink sink)
    {
        if (!sink.Draw) return;
        var blocks = f.BlocksAt(storey).ToHashSet();
        float y0 = f.FloorY(storey), y1 = storey < f.Storeys ? f.FloorY(storey + 1) : float.MaxValue;
        foreach (var piece in f.Plan.Pieces)
        {
            if (!blocks.Contains(piece.Block)) continue;
            float a = MathF.Max(piece.Y0, y0), b = MathF.Min(piece.Y1, y1);
            if (b - a < 0.3f) continue;
            var face = Face.Of(piece);
            var start = Mitre(f, piece, true, a, b);
            var end = Mitre(f, piece, false, a, b);
            Lining(sink, face, a, b, f.Holes[face].ToArray(), start, end);
        }
    }

    /// <summary>
    /// Where the lining's outer and inner faces end at one end of a piece (u along the face): on the lines of the
    /// neighbouring piece's lining, or square at the piece end when there is no turning neighbour within the storey.
    /// </summary>
    private static (float Outer, float Inner) Mitre(LibraryFrame f, WallPiece piece, bool atStart, float y0, float y1)
    {
        var end = atStart ? piece.A : piece.B;
        var square = atStart ? 0f : piece.Length;
        var neighbour = f.Plan.Pieces.Where(q => !ReferenceEquals(q, piece) && q.Y1 > y0 + 0.3f && q.Y0 < y1 - 0.3f &&
                Vector2.Distance(atStart ? q.B : q.A, end) < 0.02f)
            .OrderByDescending(q => MathF.Min(q.Y1, y1) - MathF.Max(q.Y0, y0)).FirstOrDefault();
        if (neighbour is null) return (square, square);
        return (MitreAt(piece, neighbour, square, S.LiningOut), MitreAt(piece, neighbour, square, S.LiningIn));
    }

    private static float MitreAt(WallPiece piece, WallPiece neighbour, float square, float depth)
    {
        var p = piece.A - piece.Out * depth;
        var q = neighbour.A - neighbour.Out * depth;
        if (PlanLines.LineIntersection(p, piece.Along, q, neighbour.Along, 1e-4f) is not { } x) return square;
        var u = Vector2.Dot(x - p, piece.Along);
        return MathF.Abs(u - square) <= 1f ? u : square;
    }

    private static void Lining(InteriorSink sink, Face face, float y0, float y1, IReadOnlyList<Rect> holes,
        (float Outer, float Inner) start, (float Outer, float Inner) end)
    {
        var paint = S.Wall(S.WallWhite);
        var cuts = new SortedSet<float> { 0f, face.Length };
        var band = holes.Where(h => h.Y1 > y0 + 0.01f && h.Y0 < y1 - 0.01f && h.U1 > h.U0 + 0.01f).ToArray();
        foreach (var h in band)
        {
            cuts.Add(Math.Clamp(h.U0, 0f, face.Length));
            cuts.Add(Math.Clamp(h.U1, 0f, face.Length));
        }
        var u = cuts.ToArray();
        for (var i = 0; i + 1 < u.Length; i++)
        {
            if (u[i + 1] - u[i] < 0.005f) continue;
            var mid = (u[i] + u[i + 1]) * 0.5f;
            float o0 = i == 0 ? start.Outer : u[i], n0 = i == 0 ? start.Inner : u[i];
            float o1 = i + 2 == u.Length ? end.Outer : u[i + 1], n1 = i + 2 == u.Length ? end.Inner : u[i + 1];
            Vector2[] ring = [Point(face, o0, S.LiningOut), Point(face, o1, S.LiningOut), Point(face, n1, S.LiningIn), Point(face, n0, S.LiningIn)];
            foreach (var (a, b) in Solid(y0, y1, band.Where(h => h.U0 <= mid && h.U1 >= mid)))
                sink.Prism(paint, ring, a, b);
        }
    }

    private static Vector2 Point(Face face, float u, float depth) => face.A + face.Along * u - face.Out * depth;

    /// <summary>The parts of y0…y1 no hole covers.</summary>
    private static List<(float, float)> Solid(float y0, float y1, IEnumerable<Rect> holes)
    {
        var result = new List<(float, float)>();
        var y = y0;
        foreach (var h in holes.OrderBy(x => x.Y0))
        {
            if (h.Y0 > y + 0.01f) result.Add((y, MathF.Min(h.Y0, y1)));
            y = MathF.Max(y, h.Y1);
            if (y >= y1) break;
        }
        if (y1 > y + 0.01f) result.Add((y, y1));
        return result;
    }
}
