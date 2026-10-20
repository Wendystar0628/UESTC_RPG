using Campus.Gen.Interior;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using S = Campus.Gen.Maps.Qingshuihe.Core.LibraryInterior.LibraryInteriorStyle;

namespace Campus.Gen.Maps.Qingshuihe.Core.LibraryInterior;

/// <summary>
/// 1F 中厅 (MAP-QSH-INTERIORS-LIBRARY): the atrium floor in polished stone with black strips parallel to the entrance axis, grey carpet
/// under the galleries and in the A and D sector fronts, the twin stair with the motto plaque between its feet, the curved
/// exhibition boards at the centre, the red reading table (the visual focus) toward the entrance, dark reading tables with
/// green lamps along the W and N edges, lounge seating and lanterns in the sector fronts, and the seals that close the
/// corridor mouths and sector fronts.
/// </summary>
internal static class Hall
{
    public static void Build(LibraryFrame f, InteriorGeometry sink, ref Rng rng)
    {
        var y = f.FloorY(1);
        Floor(f, sink, y);
        CorridorMouths(f, sink, y);
        SectorFronts(f, sink, y, ref rng);
        TwinStair.Build(f, sink);
        Plaque(f, sink, y);
        Exhibition(f, sink, y);
        if (!sink.Full) return;
        RedTable(f, sink, y);
        ReadingTables(f, sink, y, ref rng);
        Lounges(f, sink, y);
    }

    private static void Floor(LibraryFrame f, InteriorGeometry sink, float y)
    {
        var voidRing = f.Octagon(f.VoidApothem, S.OctagonChamfer);
        sink.Plate(S.Inlay(S.Carpet), new PolygonRings(f.Octagon(f.GalleryApothem), [voidRing]), y + 0.002f, true);
        foreach (var j in new[] { 1, 7 })
        {
            var t = Atrium.SectorHalf(f);
            sink.Flat(S.Inlay(S.Carpet), [f.At(j, f.GalleryApothem - 0.05f, -t), f.At(j, f.GalleryApothem + S.SectorDepth, -t),
                f.At(j, f.GalleryApothem + S.SectorDepth, t), f.At(j, f.GalleryApothem - 0.05f, t)], y + 0.002f);
        }
        // Black strips across the atrium's stone, parallel to the entrance axis (p) at a 3 m pitch in q.
        for (var q = -10.5f; q <= 10.6f; q += 3f)
        {
            var (p0, p1) = Chord(f, q, f.VoidApothem - 0.4f);
            if (p1 - p0 < 0.5f) continue;
            sink.Flat(S.Inlay(S.FloorStrip), [f.At(0, p0, q - 0.1f), f.At(0, p1, q - 0.1f), f.At(0, p1, q + 0.1f), f.At(0, p0, q + 0.1f)], y + 0.002f);
        }
    }

    /// <summary>The span along the entrance axis of the line at offset q inside the octagon of an apothem.</summary>
    private static (float P0, float P1) Chord(LibraryFrame f, float q, float apothem)
    {
        float p0 = float.MinValue, p1 = float.MaxValue;
        for (var j = 0; j < 8; j++)
        {
            float c = MathF.Cos(j * LibraryFrame.Step), s = MathF.Sin(j * LibraryFrame.Step);
            var limit = apothem - q * s;   // p·c + q·s ≤ apothem
            if (MathF.Abs(c) < 1e-4f) { if (limit < 0f) return (0f, 0f); continue; }
            if (c > 0f) p1 = MathF.Min(p1, limit / c);
            else p0 = MathF.Max(p0, limit / c);
        }
        return (p0, p1);
    }

    /// <summary>The four corridor mouths (四条走廊口): wood-faced side walls and a closed fire door pair at the far end.</summary>
    private static void CorridorMouths(LibraryFrame f, InteriorGeometry sink, float y)
    {
        var top = f.SoffitY(1);
        float r0 = f.GalleryApothem, r1 = r0 + S.CorridorMouth, half = S.CorridorWidth * 0.5f;
        for (var j = 0; j < 8; j += 2)
        {
            foreach (var s in new[] { -1f, 1f })
            {
                Vector2 a = f.At(j, r0, s * (half + 0.1f)), b = f.At(j, r1 + 0.1f, s * (half + 0.1f));
                sink.Run(S.Wall(S.Wood), a, b, 0.2f, y - 0.01f, top);
                sink.Wall(a, b, 0.1f, y, y + 3f, SurfaceTag.Wall, FootMat.Concrete);
            }
            Vector2 e0 = f.At(j, r1, -half - 0.2f), e1 = f.At(j, r1, half + 0.2f);
            sink.Run(S.Wall(S.WallWhite), e0, e1, 0.2f, y - 0.01f, top);
            sink.Wall(e0, e1, 0.1f, y, y + 3f, SurfaceTag.DoorClosed, FootMat.Metal);
            if (!sink.Draw) continue;
            // Closed fire door pair (封口的防火门) on the corridor side of the end wall.
            var face = f.At(j, r1 - 0.102f, 0f);
            sink.Box(S.Trim(S.DoorGrey), face, f.Dir(j + 2), 1.8f, 0.004f, y, y + 2.1f);
            sink.Box(S.Trim(S.Frame), face - f.Dir(j) * 0.003f, f.Dir(j + 2), 0.02f, 0.004f, y, y + 2.1f);
        }
    }

    /// <summary>
    /// Sector fronts A (E) and D (S), open 7 m past the hall edge: bookshelf seals along both sides and shelf ends joined
    /// into a wall at the back (扇区深处书架端板连成的墙).
    /// </summary>
    private static void SectorFronts(LibraryFrame f, InteriorGeometry sink, float y, ref Rng rng)
    {
        var top = f.SoffitY(1);
        float r0 = f.GalleryApothem, r1 = r0 + S.SectorDepth, t = Atrium.SectorHalf(f);
        foreach (var j in new[] { 1, 7 })
        {
            Furnishing.ShelfWall(sink, f.At(j, r0, t), f.At(j, r1, t), -f.Dir(j + 2), y, top, ref rng);
            Furnishing.ShelfWall(sink, f.At(j, r0, -t), f.At(j, r1, -t), f.Dir(j + 2), y, top, ref rng);
            ShelfEnds(sink, f.At(j, r1, -t), f.At(j, r1, t), -f.Dir(j), y, top);
        }
    }

    /// <summary>A wall of shelf end panels: wood with a white edge every 1.2 m, closing the stacks behind it.</summary>
    private static void ShelfEnds(InteriorGeometry sink, Vector2 a, Vector2 b, Vector2 room, float y, float top)
    {
        var back = room * -0.15f;
        sink.Run(S.Wall(S.Wood), a + back, b + back, 0.3f, y - 0.01f, top);
        sink.Wall(a + back, b + back, 0.15f, y, y + 3f, SurfaceTag.DoorClosed, FootMat.WoodDoor);
        if (!sink.Full) return;
        var length = Vector2.Distance(a, b);
        var u = (b - a) / length;
        for (var x = 0.6f; x < length - 0.3f; x += 1.2f)
            sink.Box(S.Trim(S.WallWhite), a + u * x + room * 0.005f, u, 0.05f, 0.01f, y, y + S.ShelfHeight);
        sink.Box(S.Trim(S.WallWhite), (a + b) * 0.5f + room * 0.005f, u, length, 0.01f, y + S.ShelfHeight, y + S.ShelfHeight + 0.05f);
    }

    /// <summary>
    /// The motto plaque (校训木匾) on the axis between the stair feet, facing the hall: stone plinth, two posts and a wood board
    /// with a gold frame. Its carved text waits for the glyphs (StrokeText has none of them yet).
    /// </summary>
    private static void Plaque(LibraryFrame f, InteriorGeometry sink, float y)
    {
        var feet = (TwinStair.FootRadius(f) + S.StairWidth * 0.5f) * MathF.Cos(LibraryFrame.Step);
        var c = f.At(4, feet + 0.8f, 0f);
        var across = f.Dir(2);
        var front = f.Dir(0);
        sink.Beveled(S.Stone(S.RingBeam), c, across, 3f, 0.6f, y, y + 0.45f, 0.03f);
        foreach (var s in new[] { -1f, 1f }) sink.Box(S.Trim(S.Wood), c + across * (s * 1.05f), across, 0.1f, 0.1f, y + 0.45f, y + 1.05f);
        sink.Beveled(S.Trim(S.Wood), c, across, 2.6f, 0.12f, y + 1.0f, y + 2.05f, 0.02f);
        var face = c + front * 0.062f;
        var gold = S.Trim(S.Gold);
        sink.Box(gold, face, across, 2.44f, 0.006f, y + 1.9f, y + 1.94f);
        sink.Box(gold, face, across, 2.44f, 0.006f, y + 1.11f, y + 1.15f);
        foreach (var s in new[] { -1f, 1f }) sink.Box(gold, face + across * (s * 1.2f), across, 0.04f, 0.006f, y + 1.11f, y + 1.94f);
        sink.Solid(c, across, 3f, 0.6f, y, y + 2.05f, SurfaceTag.Prop, FootMat.WoodDoor);
    }

    /// <summary>Three curved boards round the centre (白、蓝弧形展板), an opening toward the entrance; exhibits without portraits.</summary>
    private static void Exhibition(LibraryFrame f, InteriorGeometry sink, float y)
    {
        const float Radius = 1.9f, Span = 1.4f;
        const int Chords = 4;
        for (var k = 0; k < 3; k++)
        {
            var mid = f.Front + MathF.PI / 3f + k * MathF.Tau / 3f;
            var paint = S.Trim(k == 1 ? S.BoardBlue : S.WallWhite);
            for (var i = 0; i < Chords; i++)
            {
                float a0 = mid - Span * 0.5f + Span * i / Chords, a1 = a0 + Span / Chords;
                Vector2 p0 = f.Centre + LibraryFrame.Angle(a0) * Radius, p1 = f.Centre + LibraryFrame.Angle(a1) * Radius;
                sink.Run(paint, p0, p1, 0.06f, y, y + 2.1f);
                sink.Wall(p0, p1, 0.03f, y, y + 2.1f, SurfaceTag.Wall, FootMat.Metal);
                if (!sink.Full || i is 0 or Chords - 1) continue;
                var outward = LibraryFrame.Angle((a0 + a1) * 0.5f);
                var exhibit = S.Exhibits[(k * 2 + i) % S.Exhibits.Length];
                sink.Box(S.Inlay(exhibit), (p0 + p1) * 0.5f + outward * 0.032f, Vector2.Normalize(p1 - p0), Vector2.Distance(p0, p1) * 0.8f, 0.004f, y + 1.0f, y + 1.75f);
            }
        }
    }

    /// <summary>
    /// The red table (视觉落点) 3.6 m toward the entrance: an open book with a bookmark, a flask with its cap off, one chair
    /// pushed out about 30°, and the low white desk lamp whose pool covers book and flask.
    /// </summary>
    private static void RedTable(LibraryFrame f, InteriorGeometry sink, float y)
    {
        const float Along = 3.6f;
        var c = f.At(0, Along, 0f);
        var across = f.Dir(2);
        var front = f.Dir(0);
        Furnishing.Table(sink, c, across, 1.8f, 0.9f, y, S.RedTable, S.DarkWood);
        sink.Solid(c, across, 1.8f, 0.9f, y, y + WalkRules.BarrierHeight, SurfaceTag.Prop, FootMat.WoodDoor);
        var top = y + Furnishing.TableTop;
        foreach (var t in new[] { -0.45f, 0.45f })
        {
            Furnishing.Chair(sink, f.At(0, Along + 0.62f, t), -front, y, S.Wood);
            if (t < 0f) Furnishing.Chair(sink, f.At(0, Along - 0.62f, t), front, y, S.Wood);
        }
        // The chair someone just left: turned 30° and pulled back.
        var left = Furnishing.Rotate(front, MathF.PI / 6f);
        Furnishing.Chair(sink, f.At(0, Along - 0.85f, 0.55f), left, y, S.Wood);
        Furnishing.OpenBook(sink, f.At(0, Along - 0.08f, 0.12f), front, top, "street.blue");
        Furnishing.Flask(sink, f.At(0, Along + 0.12f, -0.22f), f.At(0, Along + 0.28f, -0.1f), top);
        Furnishing.DeskLamp(sink, f.At(0, Along + 0.32f, 0.6f), f.At(0, Along, 0f), top);
    }

    /// <summary>Dark wood reading tables with green lamps under the galleries along the W and N edges (阅览桌在中厅边缘).</summary>
    private static void ReadingTables(LibraryFrame f, InteriorGeometry sink, float y, ref Rng rng)
    {
        var r = (f.VoidApothem + f.GalleryApothem) * 0.5f + 0.4f;
        foreach (var j in S.StairFaces)
            foreach (var t in new[] { -3.6f, 3.6f })
            {
                var c = f.At(j, r, t);
                var u = f.Dir(j + 2);
                var v = f.Dir(j);
                Furnishing.Table(sink, c, u, 2.4f, 1.0f, y, S.DarkWood, S.DarkWood);
                sink.Solid(c, u, 2.6f, 2.2f, y, y + Furnishing.TableTop, SurfaceTag.Prop, FootMat.WoodDoor);
                foreach (var x in new[] { -0.8f, 0f, 0.8f })
                    foreach (var s in new[] { -1f, 1f })
                    {
                        var pushed = rng.NextFloat() < 0.2f ? 0.25f : 0f;
                        Furnishing.Chair(sink, c + u * x + v * (s * (0.72f + pushed)), -v * s, y, S.Wood);
                    }
                foreach (var x in new[] { -0.6f, 0.6f }) Furnishing.BankerLamp(sink, c + u * x, u, y + Furnishing.TableTop);
                if (rng.NextFloat() < 0.6f)
                    Furnishing.BookPile(sink, c + u * (rng.NextFloat() - 0.5f) + v * 0.15f, u, y + Furnishing.TableTop, 1 + rng.NextInt(3), ref rng);
            }
    }

    /// <summary>Lounge seating and paper lanterns in the sector fronts (橙黄、孔雀蓝扶手椅，奶白沙发，果绿桶形椅，木质台阶座).</summary>
    private static void Lounges(LibraryFrame f, InteriorGeometry sink, float y)
    {
        var a = f.GalleryApothem;
        var ceiling = f.SoffitY(1);
        // D (S): step seats against the back, two round tables with armchairs, a sofa facing the hall.
        var d = f.Dir(1);
        Furnishing.StepSeat(sink, f.At(1, a + S.SectorDepth - 1.85f, 0f), -d, 6f, 3, y);
        foreach (var t in new[] { -3.4f, 3.4f })
        {
            var table = f.At(1, a + 2.4f, t);
            Furnishing.RoundTable(sink, table, 0.45f, y);
            for (var k = 0; k < 3; k++)
            {
                var dir = LibraryFrame.Angle(f.Front + LibraryFrame.Step + k * MathF.Tau / 3f + t * 0.1f);
                Furnishing.Armchair(sink, table + dir * 0.85f, -dir, y, k % 2 == 0 ? S.ChairOrange : S.ChairPeacock);
            }
        }
        var sofa = f.At(1, a + 3.4f, 0f);
        Furnishing.Sofa(sink, sofa, -d, 2.1f, y, S.Sofa);
        sink.Solid(sofa, f.Dir(3), 2.1f, 0.86f, y, y + 0.43f, SurfaceTag.Prop, FootMat.Carpet);
        // A (E): barrel chairs round a low table, a pair of sofas facing each other, low step seats at the back.
        var e = f.Dir(7);
        var ring = f.At(7, a + 2.8f, -2.8f);
        Furnishing.RoundTable(sink, ring, 0.4f, y - 0.3f);
        for (var k = 0; k < 5; k++)
        {
            var dir = LibraryFrame.Angle(f.Front + k * MathF.Tau / 5f);
            var seat = ring + dir * 1.0f;
            Furnishing.Barrel(sink, seat, -dir, y, S.BarrelLime);
            sink.Solid(seat, dir, 0.6f, 0.6f, y, y + 0.45f, SurfaceTag.Prop, FootMat.Carpet);
        }
        foreach (var s in new[] { -1f, 1f })
        {
            var at = f.At(7, a + 3.2f + s * 0.95f, 3.0f);
            Furnishing.Sofa(sink, at, -e * s, 1.8f, y, S.Sofa);
            sink.Solid(at, f.Dir(1), 1.8f, 0.86f, y, y + 0.43f, SurfaceTag.Prop, FootMat.Carpet);
        }
        sink.Beveled(S.Trim(S.LightWood), f.At(7, a + 3.2f, 3.0f), f.Dir(1), 1.0f, 0.55f, y, y + 0.4f, 0.015f);
        Furnishing.StepSeat(sink, f.At(7, a + S.SectorDepth - 1.25f, 0f), -e, 5f, 2, y);
        // Paper lanterns over both fronts.
        foreach (var j in new[] { 1, 7 })
            foreach (var t in new[] { -1.4f, 0f, 1.4f })
                Furnishing.Lantern(sink, f.At(j, a + 3.0f + MathF.Abs(t) * 0.3f, t), f.Dir(j), y + 3.1f - MathF.Abs(t) * 0.2f, ceiling);
    }
}
