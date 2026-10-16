using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Parts.Core;

namespace Campus.Gen.Parts.Roof.Mep;

/// <summary>
/// Raised stroke lettering for the few fixed labels the equipment carries (exit sign, hydrant, scald warning, room numbers).
/// Glyphs are hand-drawn straight-stroke polylines in a unit em box (x right, y up), simplified Chinese forms only (INV-4).
/// Each stroke segment is a thin box standing <see cref="MepStyle.Relief"/> proud of a face that looks along −Z. Positions are in face
/// coordinates (u, v): u is the viewer's right, which is −X for a face looking along −Z.
/// Mainline stand-in for scene text: a general glyph source belongs to a later text contract (see the report).
/// </summary>
internal static class StrokeFont
{
    private const float LatinSqueeze = 0.6f;
    private const float CellMargin = 0.04f;
    // Overruns differ from the half width, so strokes meeting at right angles (arrow heads, corners) never share a corner edge.
    private const float StartOverrun = 0.53f;
    private const float EndOverrun = 0.47f;

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
    };

    public static bool Covers(string text) => text.All(c => c == ' ' || Glyphs.ContainsKey(c));

    /// <summary>Advance of one character cell in em units: CJK square, Latin and digits narrow (their glyphs are squeezed to fit).</summary>
    public static float Advance(char c) => IsCjk(c) ? 1.08f : 0.68f;

    public static float Width(string text, float height) => text.Sum(Advance) * height;

    /// <summary>
    /// Writes <paramref name="text"/> centred on face point (<paramref name="cx"/>, <paramref name="cy"/>) on the plane z = <paramref name="zFace"/>,
    /// strokes standing out towards −Z by <see cref="MepStyle.Relief"/>. Unknown characters throw, so labels are checked in tests.
    /// </summary>
    public static void Write(PartBuilder builder, string text, float cx, float cy, float zFace, float height, ToonSpec material, float weight = 1f)
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
                var stroke = height * weight * (cjk ? MepStyle.CjkStroke : MepStyle.LatinStroke);
                var sx = (cjk ? 1f : LatinSqueeze) * height; var left = x + CellMargin * height;
                foreach (var line in strokes)
                    for (var i = 0; i + 1 < line.Length; i++)
                        Segment(builder, new(left + line[i].X * sx, y0 + line[i].Y * height), new(left + line[i + 1].X * sx, y0 + line[i + 1].Y * height),
                            stroke, zFace, material);
            }
            x += Advance(c) * height;
        }
    }

    /// <summary>
    /// One straight stroke on the face plane. Its ends overrun by about half a stroke so joints close; each stroke stays a
    /// clean closed solid.
    /// </summary>
    public static void Segment(PartBuilder builder, Vector2 a, Vector2 b, float width, float zFace, ToonSpec material)
    {
        var dx = b.X - a.X; var dy = b.Y - a.Y;
        var length = (float)Math.Sqrt((double)dx * dx + (double)dy * dy);
        if (!(length > 1e-5f)) throw new ArgumentException("A stroke must have length.");
        var ux = dx / length; var uy = dy / length;
        var start = width * StartOverrun; var end = width * EndOverrun;
        var mx = (a.X + b.X) * 0.5f + ux * (end - start) * 0.5f; var my = (a.Y + b.Y) * 0.5f + uy * (end - start) * 0.5f;
        // Face u runs along −X: mirror the centre and the stroke direction.
        mx = -mx; ux = -ux;
        // Local X along the stroke, local Y across it, local Z through the face.
        var at = new Matrix4x4(ux, uy, 0, 0, -uy, ux, 0, 0, 0, 0, 1, 0, mx, my, zFace - MepStyle.Relief * 0.5f + 0.0005f, 1);
        builder.AddCentredBox(new(length + start + end, width, MepStyle.Relief + 0.001f), at, material, 0, MepStyle.Flags);
    }

    private static bool IsCjk(char c) => c >= 0x4E00 && c <= 0x9FFF;
}
