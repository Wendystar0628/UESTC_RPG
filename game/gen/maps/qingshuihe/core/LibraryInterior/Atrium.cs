using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using S = Campus.Gen.Maps.Qingshuihe.Core.LibraryInterior.LibraryInteriorStyle;

namespace Campus.Gen.Maps.Qingshuihe.Core.LibraryInterior;

/// <summary>
/// The atrium as every storey sees it (MAP-QSH-INTERIORS-LIBRARY 中庭与回廊): the balustrade round the void on each gallery, the columns
/// behind the void corners, the star ceiling under each gallery, and the bookshelf seal along the outer edge of the gallery
/// ring with this storey's openings (1F corridor mouths and sector fronts, the 5F west aisle).
/// </summary>
internal static class Atrium
{
    public static void Build(LibraryFrame f, int storey, InteriorSink sink, ref Rng books)
    {
        if (storey > 1) Balustrade(f, storey, sink);
        if (storey < f.Storeys) Columns(f, storey, sink);
        if (storey > 1) Stars(f, storey, sink);
        RingSeal(f, storey, sink, ref books);
    }

    /// <summary>Open spans across face j of the gallery's outer edge in this storey.</summary>
    public static IReadOnlyList<(float T0, float T1)> Openings(LibraryFrame f, int storey, int j)
    {
        if (storey == 1)
        {
            if (j % 2 == 0) return [(-S.CorridorWidth * 0.5f, S.CorridorWidth * 0.5f)];
            if (j is 1 or 7) return [(-SectorHalf(f), SectorHalf(f))];
            return [];
        }
        if (storey == f.Storeys && j == 3) return [(-S.AisleWidth * 0.5f, S.AisleWidth * 0.5f)];
        return [];
    }

    /// <summary>Half width of the 1F sector fronts A and D, a little inside the hall's face.</summary>
    public static float SectorHalf(LibraryFrame f) => LibraryFrame.HalfFace(f.GalleryApothem) - S.SectorInset;

    private static void Balustrade(LibraryFrame f, int storey, InteriorSink sink)
    {
        var y = f.FloorY(storey);
        IReadOnlyList<(float T0, float T1)> Gaps(int j) => storey == 2 && S.StairFaces.Contains(j)
            ? [(-S.StairWidth * 0.5f, S.StairWidth * 0.5f)] : [];
        foreach (var (a, b) in f.OctagonRuns(f.VoidApothem + S.RailThick * 0.5f, Gaps))
        {
            Rail(sink, a, b, y);
            sink.Wall(a, b, S.RailThick * 0.5f, y, y + WalkRules.BarrierHeight, SurfaceTag.Barrier, FootMat.WoodDoor);
        }
    }

    /// <summary>White curb and cap with a red-brown wood infill panel (白色板边加红褐木格栅, solid panel by default).</summary>
    public static void Rail(InteriorSink sink, Vector2 a, Vector2 b, float y)
    {
        sink.Run(S.Wall(S.WallWhite), a, b, S.RailThick, y - 0.01f, y + S.RailCurb);
        sink.Run(S.Trim(S.Wood), a, b, S.RailInfill, y + S.RailCurb, y + S.RailHeight - S.RailCap);
        sink.Run(S.Trim(S.WallWhite), a, b, S.RailCapWidth, y + S.RailHeight - S.RailCap, y + S.RailHeight);
    }

    private static void Columns(LibraryFrame f, int storey, InteriorSink sink)
    {
        float y0 = f.FloorY(storey), y1 = f.SoffitY(storey);
        var radius = (f.VoidApothem + S.ColumnSetback + S.ColumnSize * 0.5f) / MathF.Cos(LibraryFrame.Step * 0.5f);
        for (var k = 0; k < 8; k++)
        {
            var u = LibraryFrame.Angle(f.Front + (k + 0.5f) * LibraryFrame.Step);
            var c = f.Centre + u * radius;
            var hall = storey == 1;
            sink.Beveled(hall ? S.Wall(S.ColumnBlue) : S.Wall(S.WallWhite), c, u, S.ColumnSize, S.ColumnSize, y0, y1, 0.03f);
            sink.Colliders.Add(new PostCollider(c, S.ColumnSize * 0.6f, y0, y1, SurfaceTag.Wall) { Foot = FootMat.Concrete });
            if (hall) HallColumn(sink, c, u, y0, y1, k);
        }
    }

    /// <summary>
    /// The 1F columns' dark blue panels carry self-lit circuit lines (电路板纹样) on the three faces toward the hall, and the
    /// face toward the void a white light box with a gold ginkgo fan (馆里现有灯具，不单独提亮).
    /// </summary>
    private static void HallColumn(InteriorSink sink, Vector2 c, Vector2 u, float y0, float y1, int index)
    {
        if (!sink.Full) return;
        var v = new Vector2(-u.Y, u.X);
        var half = S.ColumnSize * 0.5f + 0.003f;
        foreach (var (n, along) in new[] { (-u, v), (v, u), (-v, u) })
        {
            var face = c + n * half;
            for (var line = 0; line < 3; line++)
            {
                var offset = (line - 1) * 0.22f + ((index + line) % 3 - 1) * 0.03f;
                var top = y1 - 0.4f - ((index * 3 + line) % 4) * 0.5f;
                var foot = y0 + 0.3f + ((index + line * 2) % 3) * 0.35f;
                sink.Glow(S.CircuitBlue, face + along * offset, along, 0.025f, 0.004f, foot, top);
                // A short branch and a pad at the line's top end.
                var branch = (line % 2 == 0 ? 1f : -1f) * 0.08f;
                sink.Glow(S.CircuitBlue, face + along * (offset + branch * 0.5f), along, MathF.Abs(branch), 0.004f, top - 0.025f, top);
                sink.Glow(S.CircuitBlue, face + along * (offset + branch), along, 0.05f, 0.004f, top - 0.04f, top + 0.01f);
            }
        }
        // Light box with the ginkgo fan toward the void.
        var front = c - u * (S.ColumnSize * 0.5f + 0.04f);
        float b0 = y0 + 2.5f, b1 = y0 + 3.2f;
        sink.Box(S.Trim(S.WallWhite), front, v, 0.5f, 0.08f, b0, b1);
        Fan(sink, front - u * 0.042f, v, -u, (b0 + b1) * 0.5f - 0.1f, 0.2f);
    }

    /// <summary>A flat ginkgo-leaf fan on a vertical face: a stem and a widening stack of gold bars, notched at the top middle.</summary>
    private static void Fan(InteriorSink sink, Vector2 at, Vector2 across, Vector2 normal, float baseY, float size)
    {
        var gold = S.Inlay(S.Gold);
        var face = at + normal * 0.002f;
        const int Bars = 6;
        for (var k = 0; k < Bars; k++)
        {
            float y = baseY + k * size / Bars, half = size * (0.2f + 0.8f * k / (Bars - 1));
            if (k < Bars - 2) sink.Box(gold, face, across, 2f * half, 0.004f, y, y + size / Bars * 0.75f);
            else
                foreach (var side in new[] { -1f, 1f })
                    sink.Box(gold, face + across * (side * (half + 0.03f) * 0.5f), across, half - 0.03f, 0.004f, y, y + size / Bars * 0.75f);
        }
        sink.Box(gold, face, across, 0.02f, 0.004f, baseY - size * 0.6f, baseY);
    }

    /// <summary>Downlights in the dark soffit of this storey's slab over the ring below (亮起来像一片星空).</summary>
    private static void Stars(LibraryFrame f, int storey, InteriorSink sink)
    {
        var soffit = f.FloorY(storey) - S.SlabThick;
        for (var j = 0; j < 8; j++)
            for (var r = f.VoidApothem + 0.7f; r < f.GalleryApothem - 0.3f; r += S.StarPitch)
            {
                var h = LibraryFrame.HalfFace(r) - 0.5f;
                var count = (int)MathF.Floor(2f * h / S.StarPitch);
                for (var i = 0; i <= count; i++)
                {
                    var t = -h + (2f * h) * i / Math.Max(1, count);
                    sink.Glow(S.LampGlow, f.At(j, r, t), f.Dir(j + 2), S.StarSize, S.StarSize, soffit - 0.015f, soffit);
                }
            }
    }

    private static void RingSeal(LibraryFrame f, int storey, InteriorSink sink, ref Rng books)
    {
        float y = f.FloorY(storey), top = f.SoffitY(storey);
        foreach (var (a, b) in f.OctagonRuns(f.GalleryApothem, j => Openings(f, storey, j)).ToArray())
        {
            var room = Furnishing.LeftOf(a, b);
            Furnishing.ShelfWall(sink, a, b, room, y, top, ref books);
        }
    }
}
