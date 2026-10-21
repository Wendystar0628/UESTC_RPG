using System.Numerics;

namespace Campus.Gen.Parts.Core;

/// <summary>
/// Straight-stroke lettering for the few fixed labels the parts carry (exit sign, hydrant, scald warning, room numbers, shuttle
/// stop, open sign, drinking-water plate). Glyphs are hand-drawn polylines in a unit em box (x right, y up), simplified Chinese
/// forms only (INV-4). This layer owns the glyph table, the layout and the stroke ends; each surface (raised boxes, printed quads)
/// supplies its own <see cref="StrokeSink"/>. Mainline stand-in for scene text: arbitrary names wait for game/style/'s scene-text atlas.
/// </summary>
public static class StrokeText
{
    /// <summary>Stroke width as a share of the glyph height (CJK; Latin and digits use <see cref="LatinStroke"/>).</summary>
    public const float CjkStroke = 0.1f;
    public const float LatinStroke = 0.13f;

    // Strokes overrun their ends by about half a width so joints close; the two ends differ so corners never coincide.
    private const float StartOverrun = 0.53f;
    private const float EndOverrun = 0.47f;
    private const float LatinSqueeze = 0.6f;
    private const float CellMargin = 0.04f;

    private static readonly Dictionary<char, Vector2[][]> Glyphs = new()
    {
        ['0'] = [[new(0.2f, 0.05f), new(0.8f, 0.05f), new(0.8f, 0.95f), new(0.2f, 0.95f), new(0.2f, 0.05f)]],
        ['1'] = [[new(0.32f, 0.78f), new(0.52f, 0.95f), new(0.52f, 0.05f)]],
        ['2'] = [[new(0.2f, 0.95f), new(0.8f, 0.95f), new(0.8f, 0.5f), new(0.2f, 0.5f), new(0.2f, 0.05f), new(0.8f, 0.05f)]],
        ['3'] = [[new(0.2f, 0.95f), new(0.8f, 0.95f), new(0.8f, 0.05f), new(0.2f, 0.05f)], [new(0.32f, 0.5f), new(0.8f, 0.5f)]],
        ['4'] = [[new(0.2f, 0.95f), new(0.2f, 0.42f), new(0.85f, 0.42f)], [new(0.66f, 0.95f), new(0.66f, 0.05f)]],
        ['5'] = [[new(0.8f, 0.95f), new(0.2f, 0.95f), new(0.2f, 0.52f), new(0.8f, 0.52f), new(0.8f, 0.05f), new(0.2f, 0.05f)]],
        ['6'] = [[new(0.8f, 0.95f), new(0.2f, 0.95f), new(0.2f, 0.05f), new(0.8f, 0.05f), new(0.8f, 0.5f), new(0.2f, 0.5f)]],
        ['7'] = [[new(0.2f, 0.95f), new(0.8f, 0.95f), new(0.42f, 0.05f)]],
        ['8'] = [[new(0.2f, 0.05f), new(0.8f, 0.05f), new(0.8f, 0.95f), new(0.2f, 0.95f), new(0.2f, 0.05f)], [new(0.2f, 0.5f), new(0.8f, 0.5f)]],
        ['9'] = [[new(0.8f, 0.5f), new(0.2f, 0.5f), new(0.2f, 0.95f), new(0.8f, 0.95f), new(0.8f, 0.05f), new(0.2f, 0.05f)]],
        ['-'] = [[new(0.22f, 0.5f), new(0.78f, 0.5f)]],
        ['E'] = [[new(0.8f, 0.95f), new(0.2f, 0.95f), new(0.2f, 0.05f), new(0.8f, 0.05f)], [new(0.2f, 0.5f), new(0.7f, 0.5f)]],
        ['X'] = [[new(0.18f, 0.95f), new(0.82f, 0.05f)], [new(0.82f, 0.95f), new(0.18f, 0.05f)]],
        ['I'] = [[new(0.5f, 0.95f), new(0.5f, 0.05f)]],
        ['T'] = [[new(0.12f, 0.95f), new(0.88f, 0.95f)], [new(0.5f, 0.95f), new(0.5f, 0.05f)]],
        ['口'] = [[new(0.2f, 0.82f), new(0.8f, 0.82f), new(0.8f, 0.14f), new(0.2f, 0.14f), new(0.2f, 0.82f)]],
        ['出'] = [[new(0.5f, 0.96f), new(0.5f, 0.08f)], [new(0.25f, 0.86f), new(0.25f, 0.56f), new(0.75f, 0.56f), new(0.75f, 0.86f)],
            [new(0.12f, 0.46f), new(0.12f, 0.08f), new(0.88f, 0.08f), new(0.88f, 0.46f)]],
        ['安'] = [[new(0.5f, 0.98f), new(0.5f, 0.86f)], [new(0.12f, 0.7f), new(0.12f, 0.84f), new(0.88f, 0.84f), new(0.88f, 0.7f)],
            [new(0.46f, 0.68f), new(0.3f, 0.3f), new(0.8f, 0.04f)], [new(0.7f, 0.48f), new(0.2f, 0.04f)], [new(0.06f, 0.48f), new(0.94f, 0.48f)]],
        ['全'] = [[new(0.5f, 0.96f), new(0.06f, 0.6f)], [new(0.5f, 0.96f), new(0.94f, 0.6f)], [new(0.24f, 0.54f), new(0.76f, 0.54f)],
            [new(0.28f, 0.31f), new(0.72f, 0.31f)], [new(0.12f, 0.07f), new(0.88f, 0.07f)], [new(0.5f, 0.54f), new(0.5f, 0.07f)]],
        ['消'] = [[new(0.06f, 0.88f), new(0.15f, 0.8f)], [new(0.04f, 0.62f), new(0.13f, 0.55f)], [new(0.05f, 0.08f), new(0.2f, 0.36f)],
            [new(0.62f, 0.96f), new(0.62f, 0.76f)], [new(0.42f, 0.92f), new(0.48f, 0.8f)], [new(0.84f, 0.92f), new(0.76f, 0.8f)],
            [new(0.4f, 0.04f), new(0.4f, 0.68f), new(0.86f, 0.68f), new(0.86f, 0.04f), new(0.78f, 0.08f)],
            [new(0.4f, 0.48f), new(0.86f, 0.48f)], [new(0.4f, 0.29f), new(0.86f, 0.29f)]],
        ['火'] = [[new(0.22f, 0.76f), new(0.3f, 0.54f)], [new(0.8f, 0.78f), new(0.68f, 0.56f)],
            [new(0.52f, 0.96f), new(0.49f, 0.46f), new(0.1f, 0.04f)], [new(0.52f, 0.46f), new(0.92f, 0.04f)]],
        ['栓'] = [[new(0.04f, 0.7f), new(0.38f, 0.7f)], [new(0.21f, 0.96f), new(0.21f, 0.04f)], [new(0.21f, 0.66f), new(0.04f, 0.28f)],
            [new(0.22f, 0.6f), new(0.36f, 0.46f)], [new(0.71f, 0.96f), new(0.43f, 0.62f)], [new(0.71f, 0.96f), new(0.98f, 0.62f)],
            [new(0.55f, 0.55f), new(0.87f, 0.55f)], [new(0.57f, 0.32f), new(0.85f, 0.32f)], [new(0.47f, 0.07f), new(0.95f, 0.07f)],
            [new(0.71f, 0.55f), new(0.71f, 0.07f)]],
        ['小'] = [[new(0.5f, 0.96f), new(0.5f, 0.07f), new(0.4f, 0.14f)], [new(0.3f, 0.66f), new(0.1f, 0.28f)], [new(0.7f, 0.66f), new(0.9f, 0.28f)]],
        ['心'] = [[new(0.26f, 0.62f), new(0.3f, 0.12f), new(0.74f, 0.1f), new(0.8f, 0.26f)], [new(0.07f, 0.46f), new(0.15f, 0.28f)],
            [new(0.44f, 0.82f), new(0.56f, 0.62f)], [new(0.8f, 0.62f), new(0.93f, 0.42f)]],
        ['烫'] = [[new(0.05f, 0.93f), new(0.12f, 0.86f)], [new(0.03f, 0.77f), new(0.1f, 0.7f)], [new(0.04f, 0.47f), new(0.17f, 0.64f)],
            [new(0.3f, 0.93f), new(0.84f, 0.93f), new(0.6f, 0.79f), new(0.92f, 0.79f), new(0.88f, 0.5f), new(0.79f, 0.53f)],
            [new(0.58f, 0.74f), new(0.41f, 0.52f)], [new(0.75f, 0.74f), new(0.58f, 0.5f)],
            [new(0.32f, 0.36f), new(0.37f, 0.24f)], [new(0.8f, 0.37f), new(0.72f, 0.25f)],
            [new(0.56f, 0.43f), new(0.54f, 0.22f), new(0.22f, 0.03f)], [new(0.55f, 0.22f), new(0.9f, 0.03f)]],
        ['伤'] = [[new(0.28f, 0.96f), new(0.06f, 0.55f)], [new(0.18f, 0.74f), new(0.18f, 0.04f)], [new(0.53f, 0.96f), new(0.41f, 0.75f)],
            [new(0.42f, 0.83f), new(0.95f, 0.83f)], [new(0.4f, 0.6f), new(0.88f, 0.6f), new(0.86f, 0.09f), new(0.75f, 0.13f)],
            [new(0.66f, 0.76f), new(0.6f, 0.36f), new(0.37f, 0.04f)]],
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

    /// <summary>Receives one straight stroke of <paramref name="width"/> from <paramref name="a"/> to <paramref name="b"/> in face coordinates (u right, v up).</summary>
    public delegate void StrokeSink(Vector2 a, Vector2 b, float width);

    /// <summary>One stroke with its overrun ends: <see cref="From"/>/<see cref="To"/> are the extended end points, <see cref="Total"/> the extended length.</summary>
    public readonly record struct StrokeFrame(Vector2 Dir, Vector2 From, Vector2 To, float Total);

    public static bool Covers(string text) => text.All(c => c == ' ' || Glyphs.ContainsKey(c));

    public static bool IsCjk(char c) => c >= 0x4E00 && c <= 0x9FFF;

    /// <summary>Advance of one character cell in em units: CJK square, Latin and digits narrow (their glyphs are squeezed to fit).</summary>
    public static float Advance(char c) => IsCjk(c) ? 1.08f : 0.68f;

    public static float Width(string text, float height) => text.Sum(Advance) * height;

    /// <summary>
    /// Lays <paramref name="text"/> out centred on face point (<paramref name="cx"/>, <paramref name="cy"/>) and sends every stroke to
    /// <paramref name="sink"/>; <paramref name="weight"/> scales the stroke width. Unknown characters throw, so labels are checked in tests.
    /// </summary>
    public static void Write(string text, float cx, float cy, float height, float weight, StrokeSink sink)
    {
        ArgumentNullException.ThrowIfNull(text);
        var x = cx - Width(text, height) * 0.5f;
        var y0 = cy - height * 0.5f;
        foreach (var c in text)
        {
            if (c != ' ')
            {
                if (!Glyphs.TryGetValue(c, out var strokes)) throw new ArgumentException($"No stroke glyph for '{c}'.", nameof(text));
                var cjk = IsCjk(c);
                var stroke = height * weight * (cjk ? CjkStroke : LatinStroke);
                var sx = (cjk ? 1f : LatinSqueeze) * height; var left = x + CellMargin * height;
                foreach (var line in strokes)
                    for (var i = 0; i + 1 < line.Length; i++)
                        sink(new(left + line[i].X * sx, y0 + line[i].Y * height), new(left + line[i + 1].X * sx, y0 + line[i + 1].Y * height), stroke);
            }
            x += Advance(c) * height;
        }
    }

    /// <summary>The overrun frame of one stroke (ends extended by about half a width, unequally). Throws for a zero-length stroke.</summary>
    public static StrokeFrame Frame(Vector2 a, Vector2 b, float width)
    {
        var dx = b.X - a.X; var dy = b.Y - a.Y;
        var length = (float)Math.Sqrt((double)dx * dx + (double)dy * dy);
        if (!(length > 1e-5f)) throw new ArgumentException("A stroke must have length.");
        var dir = new Vector2(dx / length, dy / length);
        var start = width * StartOverrun; var end = width * EndOverrun;
        return new(dir, new(a.X - dir.X * start, a.Y - dir.Y * start), new(b.X + dir.X * end, b.Y + dir.Y * end), length + start + end);
    }
}
