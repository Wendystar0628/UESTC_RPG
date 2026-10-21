using Campus.Contracts;
using Campus.Gen.World;
using S = Campus.Gen.Furniture.FurnitureStyle;

namespace Campus.Gen.Furniture;

/// <summary>The offsets (metres off the centre line, both sides) of the planted tree rows of the road named <paramref name="Name"/>.</summary>
internal sealed record RoadRows(string Name, IReadOnlyList<float> Offsets);

/// <summary>
/// The planted tree rows of the roads, read from the map's frozen build inputs (the map input <see cref="FurnitureStyle.PlantingKey"/>,
/// <c>vegetation/planting.json</c> relative to the map's DataRoot), never from game/gen/vegetation/'s code: for each road it lists, the offsets of its
/// <c>rows</c> and its <c>band</c> from the centre line. A missing optional input or a road with invalid offsets leaves that road without
/// rows and adds one warning diagnostic. Malformed JSON propagates to the build failure flow (ENG-ARCH).
/// Pure and deterministic: the result depends on the bytes only.
/// </summary>
internal static class PlantingRows
{
    public const string MissingCode = "furniture.planting.missing";
    public const string RoadCode = "furniture.planting.road";

    public static (IReadOnlyList<RoadRows> Roads, IReadOnlyList<BuildDiagnostic> Diagnostics) Resolve(IReadOnlyDictionary<string, ReadOnlyMemory<byte>>? inputs)
    {
        if (inputs is null || !inputs.TryGetValue(S.PlantingKey, out var bytes))
            return ([], [Warn(MissingCode, $"Build input {S.PlantingKey} is not available; no tree rows are kept clear.")]);
        var file = PlantingJson.Read(bytes.Span);
        var roads = new List<RoadRows>();
        var diagnostics = new List<BuildDiagnostic>();
        foreach (var road in file.Roads)
        {
            if (Offsets(road) is { } offsets) roads.Add(new(road.Name, Array.AsReadOnly(offsets)));
            else diagnostics.Add(Warn(RoadCode, $"Build input {S.PlantingKey} has no readable tree rows for road {road.Name}; none are kept clear on it."));
        }
        return (roads.AsReadOnly(), diagnostics.AsReadOnly());
    }

    /// <summary>The offsets of a road's <c>rows</c>, then its <c>band</c> when it has one; null when an offset is negative or not finite.</summary>
    private static float[]? Offsets(RoadWire road)
    {
        float[] offsets = [.. road.Rows.Select(row => row.Offset), .. road.Band is { } band ? new[] { band.Offset } : []];
        return offsets.All(offset => float.IsFinite(offset) && offset >= 0f) ? offsets : null;
    }

    private static BuildDiagnostic Warn(string code, string message) => new(DiagnosticLevel.Warning, code, message);
}
