using System.Numerics;
using System.Text.Json.Serialization;

namespace Campus.Contracts;

public static class SceneTextInputs
{
    public const string AtlasKey = "scene-text/atlas.json";
}

/// <summary>One laid-out glyph: Y-up em bounds, normalized top-left UV bounds, and atlas page.</summary>
public readonly record struct SceneTextGlyph(Vector4 PlaneBounds, Vector4 UvRect, int Page);

/// <summary>Frozen CPU layout only; contains no font, path, engine resource or mutable input array.</summary>
public sealed record SceneTextRun
{
    [JsonConstructor]
    public SceneTextRun(float width, IReadOnlyList<SceneTextGlyph> glyphs)
    {
        Width = width;
        Glyphs = Array.AsReadOnly(glyphs.ToArray());
    }

    public float Width { get; }
    public IReadOnlyList<SceneTextGlyph> Glyphs { get; }

    public bool Equals(SceneTextRun? other) => ReferenceEquals(this, other) ||
        other is not null && Width.Equals(other.Width) && Glyphs.SequenceEqual(other.Glyphs);

    // Container equality only; generation seeds and persistent fingerprints use the existing deterministic contracts.
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Width);
        foreach (var glyph in Glyphs) hash.Add(glyph);
        return hash.ToHashCode();
    }
}
