using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Parts.Core;

namespace Campus.Gen.Parts.Street;

/// <summary>
/// Printed scene text uses frozen native MSDF glyphs. Arrow and bicycle pictograms remain flat stroke geometry.
/// </summary>
public static class Lettering
{
    /// <summary>
    /// Writes an already laid-out run centred on the face, with the board's registered background colour.
    /// </summary>
    public static void Write(PartBuilder builder, SceneTextRun? text, PaletteIndex? baseColor, float cx, float cy,
        float zFace, bool back, float height, ToonSpec material)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (baseColor is not { } background) throw new ArgumentException("Printed text requires its board's palette index.", nameof(baseColor));
        var outward = back ? 1f : -1f;
        var z = zFace + outward * StreetStyle.PrintLift;
        foreach (var glyph in text.Glyphs)
        {
            var p = glyph.PlaneBounds;
            var left = cx + (p.X - text.Width * .5f) * height;
            var right = cx + (p.Z - text.Width * .5f) * height;
            var bottom = cy + p.Y * height; var top = cy + p.W * height;
            Vector3 Point(float u, float v) => new(back ? u : -u, v, z);
            var quad = PartShapes.FlatQuad(Point(left, top), Point(right, top), Point(right, bottom), Point(left, bottom),
                new(0, 0, outward), Print(material) with { Kind = ToonKind.SceneText });
            var mesh = quad.Meshes[0];
            var uv = glyph.UvRect;
            mesh = mesh with
            {
                Uv = [uv.X, uv.Y, uv.Z, uv.Y, uv.Z, uv.W, uv.X, uv.W],
                Uv2 = [glyph.Page, 0, glyph.Page, 0, glyph.Page, 0, glyph.Page, 0],
                Flags = [PaletteEncoding.DecalFlag, PaletteEncoding.DecalFlag, PaletteEncoding.DecalFlag, PaletteEncoding.DecalFlag],
                DecalBaseColorIndices = [background.Value, background.Value, background.Value, background.Value]
            };
            builder.Add(new PartGeometry([mesh], quad.Bounds), VertexFlag.NoShadow);
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
    /// Bicycle pictogram (the shared-bicycle bay's icon, the same shape as its map marker, SYS-ATLAS) centred on (cx, cy):
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
                Segment(builder, hub + new Vector2((float)(r * Math.Cos(a0)), (float)(r * Math.Sin(a0))),
                    hub + new Vector2((float)(r * Math.Cos(a1)), (float)(r * Math.Sin(a1))), stroke, zFace, back, material);
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
        var frame = StrokeText.Frame(a, b, width);
        var px = -frame.Dir.Y * width * 0.5f; var py = frame.Dir.X * width * 0.5f;
        Vector2[] corners =
        [
            new(frame.From.X - px, frame.From.Y - py), new(frame.To.X - px, frame.To.Y - py),
            new(frame.To.X + px, frame.To.Y + py), new(frame.From.X + px, frame.From.Y + py),
        ];
        // A front face looks along −Z and its u runs along −X; a back face looks along +Z and its u runs along +X.
        var outward = back ? 1f : -1f;
        var z = zFace + outward * StreetStyle.PrintLift;
        var points = corners.Select(c => new Vector3(back ? c.X : -c.X, c.Y, z)).ToArray();
        builder.Add(PartShapes.FlatQuad(points[0], points[1], points[2], points[3], new(0, 0, outward), Print(material)), VertexFlag.NoShadow);
    }

    /// <summary>The print class of a colour: face geometry draws no line of its own (ART-MODELING).</summary>
    public static ToonSpec Print(ToonSpec material) => material with { Line = LineClass.None, Fx = ToonFx.None };
}
