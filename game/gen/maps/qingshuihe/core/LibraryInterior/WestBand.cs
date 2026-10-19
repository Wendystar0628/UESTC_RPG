using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Gen.Maps.Qingshuihe.Core.Library;
using S = Campus.Gen.Maps.Qingshuihe.Core.LibraryInterior.LibraryInteriorStyle;

namespace Campus.Gen.Maps.Qingshuihe.Core.LibraryInterior;

/// <summary>
/// 5F 西窗阅览带 (P16, MAP-QSH-INTERIORS-LIBRARY): the west aisle from the gallery through sector C, lined with shelves, and the reading
/// band along the shell's top-storey west wall: a table under each of the shell's windows with a chair facing the view over
/// the museum roof to the West Lake, green lamps, books, and the glass of water left in the sun. Seals close the band's back
/// and ends.
/// </summary>
internal static class WestBand
{
    private const int West = 3;

    /// <summary>The shell's west wall of the top storey (the set-back face where the wing steps back).</summary>
    public static WallPiece Wall(LibraryFrame f)
    {
        var y = f.FloorY(f.Storeys);
        var dir = f.Dir(West);
        return f.Plan.Pieces.Where(p => p.Y0 < y + 0.5f && p.Y1 > y + 2f && Vector2.Dot(p.Out, dir) > 0.97f)
            .OrderByDescending(p => p.Length).FirstOrDefault()
            ?? throw new InvalidOperationException("The library shell has no west wall on its top storey.");
    }

    /// <summary>Where the aisle meets the band's back line, along Dir(3) from the centre.</summary>
    public static float AisleEnd(LibraryFrame f, WallPiece wall)
    {
        var back = Vector2.Dot(wall.A - f.Centre, wall.Out) - S.LiningIn - S.BandDepth;
        return back / Vector2.Dot(f.Dir(West), wall.Out);
    }

    public static void Build(LibraryFrame f, InteriorSink sink, ref Rng rng)
    {
        var wall = Wall(f);
        float y = f.FloorY(f.Storeys), top = f.SoffitY(f.Storeys);
        var end = AisleEnd(f, wall);
        float half = S.AisleWidth * 0.5f, start = f.GalleryApothem + S.ShelfDepth + S.SealBack;
        // The aisle (西走廊): shelves facing in on both sides.
        Furnishing.ShelfWall(sink, f.At(West, start, half), f.At(West, end, half), -f.Dir(West + 2), y, top, ref rng);
        Furnishing.ShelfWall(sink, f.At(West, start, -half), f.At(West, end, -half), f.Dir(West + 2), y, top, ref rng);

        // The band: u along the wall, s = depth into the room from the wall face.
        var room = -wall.Out;
        Vector2 At(float u, float s) => wall.A + wall.Along * u + room * s;
        float u0 = S.BandEndMargin, u1 = wall.Length - S.BandEndMargin, back = S.LiningIn + S.BandDepth;
        var axis = Vector2.Dot(f.Centre + f.Dir(West) * end - wall.A, wall.Along);
        Furnishing.ShelfWall(sink, At(u1, back), At(axis + half, back), wall.Out, y, top, ref rng);
        Furnishing.ShelfWall(sink, At(axis - half, back), At(u0, back), wall.Out, y, top, ref rng);
        foreach (var (u, sign) in new[] { (u0, 1f), (u1, -1f) })
        {
            Vector2 a = At(u, S.LiningIn), b = At(u, back + S.ShelfDepth + S.SealBack);
            sink.Run(S.Wall(S.Wood), a - wall.Along * (sign * 0.1f), b - wall.Along * (sign * 0.1f), 0.2f, y - 0.01f, top);
            sink.Wall(a - wall.Along * (sign * 0.1f), b - wall.Along * (sign * 0.1f), 0.1f, y, y + 3f, SurfaceTag.DoorClosed, FootMat.WoodDoor);
        }
        if (!sink.Full) return;
        Desks(f, sink, wall, At, u0, u1, y, ref rng);
    }

    /// <summary>A desk under each of the shell's windows on this wall that lies inside the band, the chair facing the window.</summary>
    private static void Desks(LibraryFrame f, InteriorSink sink, WallPiece wall, Func<float, float, Vector2> at, float u0, float u1, float y, ref Rng rng)
    {
        var windows = f.Holes[Face.Of(wall)].Where(h => h.Y0 > y && h.Y0 < y + 2.5f)
            .Select(h => (h.U0 + h.U1) * 0.5f).Where(u => u > u0 + 0.9f && u < u1 - 0.9f).OrderBy(u => u).ToArray();
        if (windows.Length == 0)
        {
            var count = Math.Max(1, (int)((u1 - u0) / 2.4f));
            windows = Enumerable.Range(0, count).Select(i => u0 + (u1 - u0) * (i + 0.5f) / count).ToArray();
        }
        var surface = y + Furnishing.TableTop;
        var cup = windows.Length / 2;
        for (var i = 0; i < windows.Length; i++)
        {
            var c = at(windows[i], S.LiningIn + 0.47f);
            Furnishing.Table(sink, c, wall.Along, 1.5f, 0.8f, y, S.DarkWood, S.DarkWood);
            sink.Solid(at(windows[i], S.LiningIn + 0.85f), wall.Along, 1.6f, 1.6f, y, surface, SurfaceTag.Prop, FootMat.WoodDoor);
            var pushed = rng.NextFloat() < 0.3f;
            var facing = pushed ? Furnishing.Rotate(wall.Out, (rng.NextFloat() - 0.5f) * 0.9f) : wall.Out;
            Furnishing.Chair(sink, at(windows[i] + (pushed ? 0.2f : 0f), S.LiningIn + (pushed ? 1.35f : 1.05f)), facing, y, S.Wood);
            if (i % 2 == 0) Furnishing.BankerLamp(sink, c + wall.Along * 0.5f + wall.Out * 0.2f, wall.Along, surface);
            if (i == cup)
            {
                Furnishing.Cup(sink, c - wall.Along * 0.35f + wall.Out * 0.1f, surface);
                Furnishing.OpenBook(sink, c - wall.Out * 0.12f, -wall.Out, surface, "band.terracotta");
            }
            else if (rng.NextFloat() < 0.45f)
                Furnishing.BookPile(sink, c + wall.Along * ((rng.NextFloat() - 0.5f) * 0.6f), wall.Along, surface, 1 + rng.NextInt(3), ref rng);
        }
    }
}
