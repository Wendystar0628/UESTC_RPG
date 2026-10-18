using Campus.Contracts.Atlas;
using System.Numerics;

namespace Campus.Pipeline.Map;

/// <summary>Greedy, deterministic placement using engine-measured text, never estimated glyph widths.</summary>
public static class LabelLayouter
{
    private readonly record struct Box(double X, double Z, double Width, double Height)
    {
        internal bool Overlaps(Box b) => X < b.X + b.Width && X + Width > b.X && Z < b.Z + b.Height && Z + Height > b.Z;
    }

    public static LabelsBake Build(AtlasLabels source, string inputHash, LabelLayoutInput? input)
    {
        if (input is null) return new(1, inputHash, "", false, []);
        if (input.PixelsPerMetre.Count != 5 || input.PixelsPerMetre.Any(p => !float.IsFinite(p) || p <= 0) ||
            string.IsNullOrWhiteSpace(input.FontMetricsHash)) throw new ArgumentException("Five valid zoom scales and a font metrics hash are required.");
        var labels = source.Labels.ToDictionary(l => l.Id, StringComparer.Ordinal);
        var keys = new HashSet<(string, byte, string)>();
        foreach (var measurement in input.Measurements)
        {
            if (measurement.Language is not ("zh" or "en") || measurement.ZoomBand > 4 ||
                !labels.ContainsKey(measurement.Id) || !keys.Add((measurement.Language, measurement.ZoomBand, measurement.Id)) ||
                string.IsNullOrWhiteSpace(measurement.Text)) throw new ArgumentException("Invalid or duplicate measured label.");
            CheckSize(measurement.Size);
        }
        foreach (var exclusion in input.Exclusions)
        {
            if (exclusion.Language is not ("zh" or "en") || exclusion.ZoomBand > 4) throw new ArgumentException("Invalid label exclusion.");
            CheckSize(exclusion.Size);
            AtlasLabelData.ValidatePoint(exclusion.At);
        }
        List<LabelBand> bands = [];
        foreach (string language in new[] { "zh", "en" })
            for (byte band = 0; band < 5; band++)
            {
                float scale = input.PixelsPerMetre[band];
                List<Box> occupied = [];
                foreach (var exclusion in input.Exclusions.Where(e => e.Language == language && e.ZoomBand == band))
                    occupied.Add(Rect(exclusion.At, exclusion.Size, Vector2.Zero, scale));
                List<PlacedLabel> placed = [];
                foreach (var label in source.Labels.Where(l => MinBand(l.Kind) <= band)
                    .OrderByDescending(l => l.Priority).ThenBy(l => l.Id, StringComparer.Ordinal))
                {
                    var measure = input.Measurements.SingleOrDefault(m => m.Language == language && m.ZoomBand == band && m.Id == label.Id)
                        ?? throw new ArgumentException($"Missing {language}/Z{band} font measurement for {label.Id}.");
                    Vector2 at = AtlasLabelData.Anchor(label);
                    foreach (var offset in Offsets(measure.Size))
                    {
                        Box box = Rect(at, measure.Size, offset, scale);
                        if (occupied.Any(b => b.Overlaps(box))) continue;
                        occupied.Add(box);
                        placed.Add(new(label.Id, measure.Text, at, offset, measure.Size));
                        break;
                    }
                }
                bands.Add(new(language, band, scale, placed));
            }
        return new(1, inputHash, input.FontMetricsHash, true, bands);
    }

    public static byte MinBand(string kind) => kind switch
    {
        "region" or "outer-road" => 0,
        "landmark" => 1,
        "place" or "water" => 2,
        "road" => 3,
        "minor-road" => 4,
        _ => throw new ArgumentException($"Unknown label kind {kind}.")
    };

    private static IEnumerable<Vector2> Offsets(Vector2 size)
    {
        yield return Vector2.Zero;
        yield return new(0, -size.Y - 4);
        yield return new(0, size.Y + 4);
        yield return new(size.X / 2 + 6, 0);
        yield return new(-size.X / 2 - 6, 0);
    }

    private static Box Rect(Vector2 at, Vector2 size, Vector2 offset, float scale) =>
        new(at.X * (double)scale + offset.X - size.X / 2, at.Y * (double)scale + offset.Y - size.Y / 2, size.X, size.Y);

    private static void CheckSize(Vector2 size)
    {
        if (!float.IsFinite(size.X) || !float.IsFinite(size.Y) || size.X <= 0 || size.Y <= 0 || size.X > 4096 || size.Y > 4096)
            throw new ArgumentException("Invalid measured label size.");
    }
}
