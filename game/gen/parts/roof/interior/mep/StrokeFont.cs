using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Parts.Core;

namespace Campus.Gen.Parts.Roof.Mep;

/// <summary>
/// Raised stroke lettering for the few fixed labels the equipment carries (exit sign, hydrant, scald warning, room numbers).
/// Glyphs and layout come from <see cref="StrokeText"/>; each stroke segment here is a thin box standing <see cref="MepStyle.Relief"/> proud of
/// a face that looks along −Z. Positions are in face coordinates (u, v): u is the viewer's right, which is −X for a face looking along −Z.
/// </summary>
internal static class StrokeFont
{
    /// <summary>
    /// Writes <paramref name="text"/> centred on face point (<paramref name="cx"/>, <paramref name="cy"/>) on the plane z = <paramref name="zFace"/>,
    /// strokes standing out towards −Z by <see cref="MepStyle.Relief"/>. Unknown characters throw, so labels are checked in tests.
    /// </summary>
    public static void Write(PartBuilder builder, string text, float cx, float cy, float zFace, float height, ToonSpec material, float weight = 1f) =>
        StrokeText.Write(text, cx, cy, height, weight, (a, b, width) => Segment(builder, a, b, width, zFace, material));

    /// <summary>
    /// One straight stroke on the face plane. Its ends overrun by about half a stroke so joints close; each stroke stays a
    /// clean closed solid.
    /// </summary>
    public static void Segment(PartBuilder builder, Vector2 a, Vector2 b, float width, float zFace, ToonSpec material)
    {
        var frame = StrokeText.Frame(a, b, width);
        // Face u runs along −X: mirror the centre and the stroke direction.
        var mx = -(frame.From.X + frame.To.X) * 0.5f; var my = (frame.From.Y + frame.To.Y) * 0.5f;
        var ux = -frame.Dir.X; var uy = frame.Dir.Y;
        // Local X along the stroke, local Y across it, local Z through the face.
        var at = new Matrix4x4(ux, uy, 0, 0, -uy, ux, 0, 0, 0, 0, 1, 0, mx, my, zFace - MepStyle.Relief * 0.5f + 0.0005f, 1);
        builder.AddCentredBox(new(frame.Total, width, MepStyle.Relief + 0.001f), at, material, 0, MepStyle.Flags);
    }
}
