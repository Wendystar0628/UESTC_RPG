using System.Numerics;

namespace Campus.Gen.Parts.Core;

/// <summary>Shared shallow circular section for north canopies and sports barrel roofs, using the part kernel's arc.</summary>
public static class ArcSection
{
    /// <summary>Left to right, endpoints at y=0, crown at y=rise; span is the full clear width.</summary>
    public static IReadOnlyList<Vector2> Sample(float span, float rise, int segments)
    {
        if (!(span > 0 && rise > 0 && rise <= span * .5f))
            throw new ArgumentOutOfRangeException(nameof(rise), "A shallow arch needs 0 < rise <= half its positive span.");
        if (segments < 2) throw new ArgumentOutOfRangeException(nameof(segments));
        var half = span * .5f;
        var radius = (half * half + rise * rise) / (2 * rise);
        var angle = (float)(Math.Acos(half / radius) * 180 / Math.PI);
        var bands = PartShapes.Arc(radius, rise - radius, 180 - angle, angle, segments).ToArray();
        return [.. bands.Select(b => new Vector2(b.R0, b.Y0)), new(bands[^1].R1, bands[^1].Y1)];
    }
}
