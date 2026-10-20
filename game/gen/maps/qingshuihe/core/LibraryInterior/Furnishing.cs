using Campus.Gen.Interior;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using S = Campus.Gen.Maps.Qingshuihe.Core.LibraryInterior.LibraryInteriorStyle;
using CommonShapes = Campus.Gen.Sites.Shapes;

namespace Campus.Gen.Maps.Qingshuihe.Core.LibraryInterior;

/// <summary>
/// The library's furniture and small things, built in place into the storey sink (WP-52's room parts are not delivered yet).
/// Plan positions are world (x, z); "facing" is the way a seat's user looks. Chairs, lamps and traces have no collision
/// (WLD-INTERIORS-BUILD); callers add the boxes for tables, sofas and seats.
/// </summary>
internal static class Furnishing
{
    /// <summary>Unit normal on the left of a → b: the room side of a counterclockwise run.</summary>
    public static Vector2 LeftOf(Vector2 a, Vector2 b)
    {
        var d = Vector2.Normalize(b - a);
        return new(-d.Y, d.X);
    }

    public static Vector2 Across(Vector2 u) => new(-u.Y, u.X);

    /// <summary>
    /// A bookshelf seal (书架墙) on the run a → b: shelves facing the room side with books, a wood-faced wall behind them
    /// up to the ceiling, and one DoorClosed wall collider (WLD-INTERIORS-ACCESS 封口, top ≥ 1.5 m).
    /// </summary>
    public static void ShelfWall(InteriorGeometry sink, Vector2 a, Vector2 b, Vector2 room, float y, float top, ref Rng books)
    {
        var length = Vector2.Distance(a, b);
        if (length < 0.05f) return;
        var u = (b - a) / length;
        var back = -room * (S.ShelfDepth + S.SealBack * 0.5f);
        sink.Shapes.Run(S.Wall(S.Wood), a + back, b + back, S.SealBack, y - 0.01f, top);
        var half = (S.ShelfDepth + S.SealBack) * 0.5f;
        sink.Collision.Wall(a - room * half, b - room * half, half, y, MathF.Min(top, y + 3f), SurfaceTag.DoorClosed, FootMat.WoodDoor);
        var mid = -room * (S.ShelfDepth * 0.5f);
        var shelfTop = MathF.Min(y + S.ShelfHeight, top);
        var wood = S.Trim(S.Wood);
        if (!sink.Full || length < 0.6f)
        {
            sink.Shapes.Run(wood, a + mid, b + mid, S.ShelfDepth, y, shelfTop);
            return;
        }
        var units = Math.Max(1, (int)MathF.Round(length / S.ShelfUnit));
        var w = length / units;
        for (var i = 0; i <= units; i++) sink.Shapes.Box(wood, a + u * (i * w) + mid, u, S.ShelfPanel, S.ShelfDepth, y, shelfTop);
        var pitch = (shelfTop - y - 0.1f) / S.ShelfBoards;
        for (var i = 0; i < units; i++)
        {
            var c = a + u * ((i + 0.5f) * w) + mid;
            sink.Shapes.Box(wood, c, u, w - S.ShelfPanel, S.ShelfDepth, y, y + 0.055f);
            for (var k = 0; k <= S.ShelfBoards; k++)
            {
                var by = y + 0.08f + k * pitch;
                sink.Shapes.Box(wood, c, u, w - S.ShelfPanel, S.ShelfDepth, by - 0.025f, by);
                if (k < S.ShelfBoards) Books(sink, c, u, room, w - S.ShelfPanel - 0.02f, by, pitch - 0.07f, ref books);
            }
        }
    }

    /// <summary>One shelf's row of books: blocks of varied width, height and colour flush near the front, with a few gaps.</summary>
    public static void Books(InteriorGeometry sink, Vector2 c, Vector2 u, Vector2 room, float width, float y, float height, ref Rng rng)
    {
        var x = -width * 0.5f;
        while (x < width * 0.5f - 0.05f)
        {
            var w = MathF.Min(0.1f + rng.NextFloat() * 0.24f, width * 0.5f - x);
            var depth = S.ShelfDepth * (0.6f + rng.NextFloat() * 0.25f);
            var h = height * (0.68f + rng.NextFloat() * 0.3f);
            var colour = S.Books[rng.NextInt(S.Books.Length)];
            if (rng.NextFloat() > 0.16f && w > 0.04f)
                sink.Shapes.Box(S.Trim(colour), c + u * (x + w * 0.5f) + room * (S.ShelfDepth * 0.5f - depth * 0.5f - 0.02f), u, w - 0.008f, depth, y, y + h);
            x += w;
        }
    }

    /// <summary>A small pile of closed books lying on a surface, turned a little each.</summary>
    public static void BookPile(InteriorGeometry sink, Vector2 c, Vector2 u, float y, int count, ref Rng rng)
    {
        for (var i = 0; i < count; i++)
        {
            var angle = (rng.NextFloat() - 0.5f) * 0.5f;
            var d = Rotate(u, angle);
            var h = 0.025f + rng.NextFloat() * 0.02f;
            sink.Shapes.Box(S.Trim(S.Books[rng.NextInt(S.Books.Length)]), c + Across(u) * ((rng.NextFloat() - 0.5f) * 0.03f), d,
                0.26f - rng.NextFloat() * 0.05f, 0.19f, y, y + h);
            y += h;
        }
    }

    /// <summary>An open book with a plain bookmark: cover and two page blocks either side of the spine (spine along u).</summary>
    public static void OpenBook(InteriorGeometry sink, Vector2 c, Vector2 u, float y, string cover)
    {
        var side = Across(u);
        sink.Shapes.Box(S.Trim(cover), c, u, 0.25f, 0.36f, y, y + 0.006f);
        foreach (var s in new[] { -1f, 1f })
            sink.Shapes.Box(S.Trim(S.Paper), c + side * (s * 0.088f), u, 0.23f, 0.165f, y + 0.006f, y + 0.02f + (s > 0 ? 0.004f : 0f));
        sink.Shapes.Box(S.Trim("band.terracotta"), c + u * 0.13f + side * 0.02f, u, 0.05f, 0.008f, y + 0.005f, y + 0.026f);
    }

    /// <summary>A wooden chair: seat, four legs, a back toward the far side from where its user looks.</summary>
    public static void Chair(InteriorGeometry sink, Vector2 c, Vector2 facing, float y, string colour)
    {
        if (!sink.Full) return;
        var side = Across(facing);
        var paint = S.Trim(colour);
        sink.Shapes.Box(paint, c, side, 0.44f, 0.44f, y + 0.41f, y + 0.45f);
        foreach (var (s, f) in new[] { (-1f, -1f), (1f, -1f), (-1f, 1f), (1f, 1f) })
            sink.Shapes.Box(paint, c + side * (s * 0.19f) + facing * (f * 0.19f), side, 0.035f, 0.035f, y, y + 0.41f);
        var back = c - facing * 0.2f;
        sink.Shapes.Box(paint, back, side, 0.42f, 0.03f, y + 0.62f, y + 0.88f);
        foreach (var s in new[] { -1f, 1f }) sink.Shapes.Box(paint, back + side * (s * 0.19f), side, 0.035f, 0.035f, y + 0.45f, y + 0.88f);
    }

    /// <summary>A metal-framed armchair with a coloured shell (橙黄与孔雀蓝金属框扶手椅).</summary>
    public static void Armchair(InteriorGeometry sink, Vector2 c, Vector2 facing, float y, string colour)
    {
        if (!sink.Full) return;
        var side = Across(facing);
        var shell = S.Trim(colour);
        var frame = S.Metal(S.Frame);
        sink.Shapes.Box(shell, c, side, 0.56f, 0.54f, y + 0.36f, y + 0.44f);
        sink.Shapes.Box(shell, c - facing * 0.25f, side, 0.56f, 0.06f, y + 0.44f, y + 0.8f);
        foreach (var s in new[] { -1f, 1f })
        {
            var arm = c + side * (s * 0.29f);
            sink.Shapes.Box(frame, arm, facing, 0.56f, 0.03f, y + 0.6f, y + 0.63f);
            sink.Shapes.Box(frame, arm + facing * 0.24f, facing, 0.03f, 0.03f, y, y + 0.6f);
            sink.Shapes.Box(frame, arm - facing * 0.24f, facing, 0.03f, 0.03f, y, y + 0.6f);
        }
    }

    /// <summary>A sofa: seat block, back and two arms (奶白沙发).</summary>
    public static void Sofa(InteriorGeometry sink, Vector2 c, Vector2 facing, float length, float y, string colour)
    {
        var side = Across(facing);
        var paint = S.Trim(colour);
        sink.Shapes.Box(paint, c, side, length, 0.86f, y, y + 0.43f);
        sink.Shapes.Box(paint, c - facing * 0.33f, side, length, 0.2f, y + 0.43f, y + 0.82f);
        foreach (var s in new[] { -1f, 1f }) sink.Shapes.Box(paint, c + side * (s * (length * 0.5f - 0.09f)), side, 0.18f, 0.86f, y + 0.43f, y + 0.62f);
    }

    /// <summary>A barrel chair (果绿桶形椅): round seat and a low curved-looking back.</summary>
    public static void Barrel(InteriorGeometry sink, Vector2 c, Vector2 facing, float y, string colour)
    {
        var paint = S.Trim(colour);
        sink.Shapes.Cylinder(paint, c, 0.3f, y, y + 0.45f, 16);
        sink.Shapes.Box(paint, c - facing * 0.24f, Across(facing), 0.46f, 0.1f, y + 0.45f, y + 0.72f);
    }

    /// <summary>Wooden step seats (木质台阶座): tiers rising away from the front edge at c.</summary>
    public static void StepSeat(InteriorGeometry sink, Vector2 c, Vector2 facing, float width, int tiers, float y)
    {
        const float Depth = 0.6f, Rise = 0.42f;
        var paint = S.Trim(S.LightWood);
        for (var i = 0; i < tiers; i++)
        {
            var centre = c - facing * (Depth * (i + 0.5f));
            sink.Shapes.Beveled(paint, centre, Across(facing), width, Depth, y, y + Rise * (i + 1), 0.02f);
            sink.Collision.Solid(centre, Across(facing), width, Depth, y, y + Rise * (i + 1), SurfaceTag.Prop, FootMat.WoodFloor);
        }
    }

    /// <summary>Height of every table top (课桌椅盒顶 0.75–0.8 m, WLD-INTERIORS-BUILD).</summary>
    public const float TableTop = 0.76f;

    /// <summary>A rectangular table: chamfered top on four legs; u is the long side.</summary>
    public static void Table(InteriorGeometry sink, Vector2 c, Vector2 u, float length, float width, float y, string top, string legs)
    {
        const float Height = TableTop;
        sink.Shapes.Beveled(S.Trim(top), c, u, length, width, y + Height - 0.045f, y + Height, 0.012f);
        if (!sink.Full) return;
        var side = Across(u);
        foreach (var (s, t) in new[] { (-1f, -1f), (1f, -1f), (-1f, 1f), (1f, 1f) })
            sink.Shapes.Box(S.Trim(legs), c + u * (s * (length * 0.5f - 0.08f)) + side * (t * (width * 0.5f - 0.08f)), u, 0.055f, 0.055f, y, y + Height - 0.045f);
        sink.Shapes.Box(S.Trim(legs), c, u, length - 0.2f, width - 0.2f, y + Height - 0.12f, y + Height - 0.045f);
    }

    /// <summary>A white round table on a pedestal.</summary>
    public static void RoundTable(InteriorGeometry sink, Vector2 c, float radius, float y)
    {
        var white = S.Trim(S.WallWhite);
        sink.Shapes.Cylinder(white, c, radius, y + 0.72f, y + 0.75f, 20);
        if (!sink.Full) return;
        sink.Shapes.Cylinder(S.Metal(S.Frame), c, 0.04f, y + 0.02f, y + 0.72f, 8);
        sink.Shapes.Cylinder(S.Metal(S.Frame), c, 0.24f, y, y + 0.02f, 12);
    }

    /// <summary>A green-shaded reading lamp (绿罩台灯) on a table top: brass foot and stem, the shade, a self-lit mouth.</summary>
    public static void BankerLamp(InteriorGeometry sink, Vector2 c, Vector2 u, float y)
    {
        if (!sink.Full) return;
        var brass = S.Metal("panel.paleGold");
        sink.Shapes.Box(brass, c, u, 0.18f, 0.11f, y, y + 0.02f);
        sink.Shapes.Box(brass, c, u, 0.02f, 0.02f, y + 0.02f, y + 0.3f);
        sink.Shapes.Box(S.Trim(S.LampGreen), c, u, 0.36f, 0.13f, y + 0.3f, y + 0.37f);
        sink.Lights.Box(S.LampGlow, c, u, 0.31f, 0.09f, y + 0.292f, y + 0.3f);
    }

    /// <summary>
    /// The white folding desk lamp of the red table (白色折臂台灯，灯罩压低，不见灯泡): foot, two arm links, a low shade over
    /// <paramref name="target"/> with a self-lit mouth, and the warm pool of light on the table (window.library: faint by day,
    /// full after sunset).
    /// </summary>
    public static void DeskLamp(InteriorGeometry sink, Vector2 foot, Vector2 target, float y)
    {
        if (!sink.Full) return;
        var white = S.Trim(S.WallWhite);
        var dir = Vector2.Normalize(target - foot);
        sink.Shapes.Cylinder(white, foot, 0.08f, y, y + 0.022f, 16);
        var elbow = new Vector3(foot.X, y + 0.42f, foot.Y) - new Vector3(dir.X, 0f, dir.Y) * 0.06f;
        var head = new Vector3(target.X, y + 0.36f, target.Y);
        sink.Shapes.Bar(white, new Vector3(foot.X, y + 0.02f, foot.Y), elbow, 0.022f);
        sink.Shapes.Bar(white, elbow, head, 0.022f);
        var mouth = new Vector2(head.X, head.Z);
        sink.Shapes.Cylinder(white, mouth, 0.085f, head.Y - 0.11f, head.Y - 0.02f, 16);
        sink.Lights.Prism(S.LampGlow, CommonShapes.Circle(mouth, 0.07f, 12), head.Y - 0.115f, head.Y - 0.11f);
        sink.Lights.Prism(S.LampGlow, CommonShapes.Circle(mouth, 0.42f, 20), y + 0.001f, y + 0.003f);
    }

    /// <summary>A vacuum flask with its cap off beside it (没拧盖的保温杯).</summary>
    public static void Flask(InteriorGeometry sink, Vector2 c, Vector2 cap, float y)
    {
        if (!sink.Full) return;
        sink.Shapes.Cylinder(S.Metal(S.Steel), c, 0.036f, y, y + 0.21f, 14);
        sink.Shapes.Cylinder(S.Trim(S.ChairPeacock), c, 0.037f, y + 0.16f, y + 0.18f, 14);
        sink.Shapes.Cylinder(S.Metal(S.Steel), cap, 0.039f, y, y + 0.045f, 14);
    }

    /// <summary>A glass of water left on a table, its surface catching the light (桌上一杯没喝完的水).</summary>
    public static void Cup(InteriorGeometry sink, Vector2 c, float y)
    {
        if (!sink.Full) return;
        sink.Shapes.Cylinder(S.Trim("curtain.cream"), c, 0.035f, y, y + 0.1f, 14);
        sink.Shapes.Cylinder(S.Inlay("panel.paleGold"), c, 0.03f, y + 0.1f, y + 0.102f, 14);
    }

    /// <summary>A square paper lantern (方形纸灯笼) hanging from the ceiling on a rod.</summary>
    public static void Lantern(InteriorGeometry sink, Vector2 c, Vector2 u, float bottom, float ceiling)
    {
        sink.Lights.Box(S.LampGlow, c, u, 0.36f, 0.36f, bottom, bottom + 0.46f);
        sink.Shapes.Bar(S.Metal(S.Frame), new Vector3(c.X, bottom + 0.46f, c.Y), new Vector3(c.X, ceiling, c.Y), 0.012f);
    }

    public static Vector2 Rotate(Vector2 v, float angle)
    {
        float c = MathF.Cos(angle), s = MathF.Sin(angle);
        return new(v.X * c - v.Y * s, v.X * s + v.Y * c);
    }
}
