using System.Numerics;
using System.Text;
using System.Text.Json;
using Campus.Contracts;

namespace Campus.Gen.Parts.Core;

/// <summary>Layouts the fixed scene labels from Godot's exported metrics; workers only receive frozen bytes.</summary>
public sealed class SceneTextAtlas
{
    private readonly Dictionary<int, (SceneTextGlyph Glyph, float Advance)> _glyphs = [];
    private readonly float _ascent;
    private readonly float _descent;

    public SceneTextAtlas(ReadOnlyMemory<byte> bytes)
    {
        using var document = JsonDocument.Parse(bytes);
        var root = document.RootElement;
        if (root.GetProperty("format").GetString() != "godot-msdf") throw new InvalidDataException("Unsupported scene atlas format.");
        _ascent = root.GetProperty("ascent").GetSingle();
        _descent = root.GetProperty("descent").GetSingle();
        foreach (var item in root.GetProperty("glyphs").EnumerateArray())
            _glyphs.Add(item.GetProperty("codepoint").GetInt32(),
                (new(Bounds(item.GetProperty("planeBounds")), Bounds(item.GetProperty("uvRect")), item.GetProperty("page").GetInt32()),
                item.GetProperty("advance").GetSingle()));
    }

    public SceneTextRun Layout(string text)
    {
        var result = new List<SceneTextGlyph>();
        var cursor = 0f;
        var center = (_ascent - _descent) * .5f;
        foreach (var rune in text.EnumerateRunes())
        {
            if (!_glyphs.TryGetValue(rune.Value, out var entry))
                throw new InvalidDataException($"Scene text atlas lacks U+{rune.Value:X}: {text}");
            var plane = entry.Glyph.PlaneBounds;
            var uv = entry.Glyph.UvRect;
            // Native MSDF padding extends into the next glyph. Opaque printed cells must not paint over
            // their neighbour's ink: crop to the advance cell and interpolate the corresponding UV.
            var left = MathF.Max(0, plane.X); var right = MathF.Min(entry.Advance, plane.Z);
            var bottom = MathF.Max(-_descent, plane.Y); var top = MathF.Min(_ascent, plane.W);
            float U(float x) => uv.X + (uv.Z - uv.X) * ((x - plane.X) / (plane.Z - plane.X));
            float V(float y) => uv.Y + (uv.W - uv.Y) * ((plane.W - y) / (plane.W - plane.Y));
            result.Add(new(new(left + cursor, bottom - center, right + cursor, top - center),
                new(U(left), V(top), U(right), V(bottom)), entry.Glyph.Page));
            cursor += entry.Advance;
        }
        return new(cursor, result);
    }

    private static Vector4 Bounds(JsonElement value) => new(value[0].GetSingle(), value[1].GetSingle(), value[2].GetSingle(), value[3].GetSingle());
}
