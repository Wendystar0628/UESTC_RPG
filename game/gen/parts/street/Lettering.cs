using System.Numerics;
using Campus.Contracts;

namespace Campus.Gen.Parts.Street;

/// <summary>
/// Printed stroke lettering for the few fixed labels the street parts carry (shuttle stop, open sign) and the arrow pictogram.
/// Glyphs are straight-stroke polylines in a unit em box (x right, y up), simplified Chinese only (INV-4). Each stroke is face
/// geometry (ART-MODELING §7.5): one flat quad, two triangles, lifted <see cref="StreetStyle.PrintLift"/> off the face, line
/// class "not drawn", out of the shadow pass. Mainline stand-in for scene text; arbitrary names (building names, place names)
/// wait for WP-04's scene-text atlas (contract-26 review).
/// </summary>
public static class Lettering
{
    // Strokes overrun their ends by about half a width so joints close; the two ends differ so corners never coincide.
    private const float StartOverrun = 0.53f;
    private const float EndOverrun = 0.47f;
    private const float CellMargin = 0.04f;

    private static readonly Dictionary<char, Vector2[][]> Glyphs = new()
    {
        ['开'] = [[new(0.12f, 0.86f), new(0.88f, 0.86f)], [new(0.05f, 0.52f), new(0.95f, 0.52f)],
            [new(0.36f, 0.86f), new(0.35f, 0.42f), new(0.12f, 0.04f)], [new(0.66f, 0.86f), new(0.66f, 0.04f)]],
        ['放'] = [[new(0.22f, 0.97f), new(0.28f, 0.86f)], [new(0.04f, 0.76f), new(0.48f, 0.76f)],
            [new(0.2f, 0.74f), new(0.18f, 0.36f), new(0.06f, 0.06f)], [new(0.21f, 0.52f), new(0.42f, 0.52f), new(0.4f, 0.1f), new(0.31f, 0.05f)],
            [new(0.62f, 0.97f), new(0.55f, 0.72f)], [new(0.57f, 0.76f), new(0.96f, 0.76f)],
            [new(0.84f, 0.74f), new(0.74f, 0.4f), new(0.5f, 0.05f)], [new(0.6f, 0.52f), new(0.96f, 0.05f)]],
        ['班'] = [[new(0.03f, 0.86f), new(0.3f, 0.86f)], [new(0.06f, 0.5f), new(0.27f, 0.5f)], [new(0.165f, 0.85f), new(0.165f, 0.16f)],
            [new(0.02f, 0.1f), new(0.32f, 0.2f)],
            [new(0.47f, 0.84f), new(0.44f, 0.36f), new(0.35f, 0.05f)], [new(0.5f, 0.62f), new(0.55f, 0.5f)],
            [new(0.62f, 0.86f), new(0.95f, 0.86f)], [new(0.65f, 0.5f), new(0.92f, 0.5f)], [new(0.785f, 0.85f), new(0.785f, 0.1f)],
            [new(0.6f, 0.07f), new(0.97f, 0.07f)]],
        ['车'] = [[new(0.08f, 0.8f), new(0.92f, 0.8f)], [new(0.42f, 0.97f), new(0.18f, 0.43f), new(0.86f, 0.43f)],
            [new(0.55f, 0.66f), new(0.55f, 0.02f)], [new(0.05f, 0.2f), new(0.95f, 0.2f)]],
        ['直'] = [[new(0.15f, 0.87f), new(0.85f, 0.87f)], [new(0.5f, 0.98f), new(0.5f, 0.74f)], [new(0.12f, 0.68f), new(0.12f, 0.04f), new(0.94f, 0.04f)],
            [new(0.3f, 0.7f), new(0.3f, 0.16f)], [new(0.3f, 0.7f), new(0.76f, 0.7f), new(0.76f, 0.16f)], [new(0.3f, 0.16f), new(0.76f, 0.16f)],
            [new(0.3f, 0.52f), new(0.76f, 0.52f)], [new(0.3f, 0.34f), new(0.76f, 0.34f)]],
        ['饮'] = [[new(0.24f, 0.97f), new(0.05f, 0.64f)], [new(0.16f, 0.79f), new(0.36f, 0.79f), new(0.29f, 0.66f)], [new(0.16f, 0.64f), new(0.16f, 0.1f), new(0.31f, 0.21f)],
            [new(0.58f, 0.97f), new(0.47f, 0.72f)], [new(0.53f, 0.8f), new(0.93f, 0.8f), new(0.85f, 0.66f)], [new(0.71f, 0.66f), new(0.67f, 0.4f), new(0.45f, 0.05f)],
            [new(0.7f, 0.42f), new(0.96f, 0.05f)]],
        ['水'] = [[new(0.5f, 0.97f), new(0.5f, 0.08f), new(0.4f, 0.15f)], [new(0.08f, 0.68f), new(0.38f, 0.68f), new(0.1f, 0.18f)],
            [new(0.88f, 0.8f), new(0.6f, 0.57f)], [new(0.59f, 0.62f), new(0.95f, 0.08f)]],
    };

    public static bool Covers(string text) => text.All(c => c == ' ' || Glyphs.ContainsKey(c));

    /// <summary>Advance of one CJK cell in em units.</summary>
    public const float Advance = 1.08f;

    public static float Width(string text, float height) => text.Length * Advance * height;

    /// <summary>
    /// Writes <paramref name="text"/> centred on face point (<paramref name="cx"/>, <paramref name="cy"/>) on the plane z = <paramref name="zFace"/>.
    /// A front face looks along −Z (the viewer's right is −X); a back face looks along +Z. Unknown characters throw.
    /// </summary>
    public static void Write(PartBuilder builder, string text, float cx, float cy, float zFace, bool back, float height, ToonSpec material)
    {
        ArgumentNullException.ThrowIfNull(text);
        var x = cx - Width(text, height) * 0.5f;
        var y0 = cy - height * 0.5f;
        var stroke = height * StreetStyle.CjkStroke;
        foreach (var c in text)
        {
            if (c != ' ')
            {
                if (!Glyphs.TryGetValue(c, out var strokes)) throw new ArgumentException($"No stroke glyph for '{c}'.", nameof(text));
                var left = x + CellMargin * height;
                foreach (var line in strokes)
                    for (var i = 0; i + 1 < line.Length; i++)
                        Segment(builder, new(left + line[i].X * height, y0 + line[i].Y * height), new(left + line[i + 1].X * height, y0 + line[i + 1].Y * height),
                            stroke, zFace, back, material);
            }
            x += Advance * height;
        }
    }

    /// <summary>Upward arrow ("this way in") centred on (cx, cy): a shaft and a two-stroke head.</summary>
    public static void ArrowUp(PartBuilder builder, float cx, float cy, float size, float zFace, bool back, ToonSpec material)
    {
        var stroke = size * 0.16f; var head = size * 0.36f;
        var top = cy + size * 0.5f; var bottom = cy - size * 0.5f;
        Segment(builder, new(cx, bottom), new(cx, top - stroke * 0.5f), stroke, zFace, back, material);
        Segment(builder, new(cx, top), new(cx - head, top - head), stroke, zFace, back, material);
        Segment(builder, new(cx, top), new(cx + head, top - head), stroke, zFace, back, material);
    }

    /// <summary>
    /// Bicycle pictogram (the shared-bicycle bay's icon, the same shape as its map marker, SYS-ATLAS §5) centred on (cx, cy):
    /// two octagon wheels, the frame triangle, seat and bar; <paramref name="size"/> is its width.
    /// </summary>
    public static void Bicycle(PartBuilder builder, float cx, float cy, float size, float zFace, bool back, ToonSpec material)
    {
        var stroke = size * 0.07f; var r = size * 0.21f;
        var rear = new Vector2(cx - size * 0.29f, cy - size * 0.1f); var front = new Vector2(cx + size * 0.29f, cy - size * 0.1f);
        foreach (var hub in new[] { rear, front })
            for (var i = 0; i < 8; i++)
            {
                var a0 = Math.PI * 2 * i / 8 + Math.PI / 8; var a1 = Math.PI * 2 * (i + 1) / 8 + Math.PI / 8;
                Segment(builder, hub + new Vector2((float)(r * DetMath.Cos(a0)), (float)(r * DetMath.Sin(a0))),
                    hub + new Vector2((float)(r * DetMath.Cos(a1)), (float)(r * DetMath.Sin(a1))), stroke, zFace, back, material);
            }
        var crank = new Vector2(cx - size * 0.02f, cy - size * 0.1f);
        var seat = new Vector2(cx - size * 0.12f, cy + size * 0.2f);
        var head = new Vector2(cx + size * 0.2f, cy + size * 0.16f);
        Segment(builder, rear, crank, stroke, zFace, back, material);
        Segment(builder, crank, seat, stroke, zFace, back, material);
        Segment(builder, crank, head, stroke, zFace, back, material);
        Segment(builder, head, front, stroke, zFace, back, material);
        Segment(builder, seat - new Vector2(size * 0.07f, 0), seat + new Vector2(size * 0.07f, 0), stroke, zFace, back, material);
        Segment(builder, head, head + new Vector2(-size * 0.06f, size * 0.07f), stroke, zFace, back, material);
    }

    /// <summary>
    /// One straight stroke printed on the face plane z = <paramref name="zFace"/>: a flat quad facing out of the face, its ends
    /// overrunning by about half a stroke so joints close. Face coordinates: u is the viewer's right, v is up.
    /// </summary>
    public static void Segment(PartBuilder builder, Vector2 a, Vector2 b, float width, float zFace, bool back, ToonSpec material)
    {
        var dx = b.X - a.X; var dy = b.Y - a.Y;
        var length = (float)Math.Sqrt((double)dx * dx + (double)dy * dy);
        if (!(length > 1e-5f)) throw new ArgumentException("A stroke must have length.");
        var ux = dx / length; var uy = dy / length;
        var start = width * StartOverrun; var end = width * EndOverrun;
        var px = -uy * width * 0.5f; var py = ux * width * 0.5f;
        Vector2[] corners =
        [
            new(a.X - ux * start - px, a.Y - uy * start - py), new(b.X + ux * end - px, b.Y + uy * end - py),
            new(b.X + ux * end + px, b.Y + uy * end + py), new(a.X - ux * start + px, a.Y - uy * start + py),
        ];
        // A front face looks along −Z and its u runs along −X; a back face looks along +Z and its u runs along +X.
        var outward = back ? 1f : -1f;
        var z = zFace + outward * StreetStyle.PrintLift;
        var points = corners.Select(c => new Vector3(back ? c.X : -c.X, c.Y, z)).ToArray();
        builder.Add(Shapes.FlatQuad(points[0], points[1], points[2], points[3], new(0, 0, outward), Print(material)), VertexFlag.NoShadow);
    }

    /// <summary>The print class of a colour: face geometry draws no line of its own (ART-MODELING §7.5).</summary>
    public static ToonSpec Print(ToonSpec material) => material with { Line = LineClass.None, Fx = ToonFx.None };
}
