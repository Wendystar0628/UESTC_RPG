using System.Collections.ObjectModel;
using Campus.Contracts;
using Campus.Contracts.Maps;

namespace Campus.Gen.World;

/// <summary>Compiled scalar result. Surface class and line distances come from authored operations, not gradient guessing.</summary>
public readonly record struct TerrainEvaluation(TerrainDifferential Surface, TerrainClass Class,
    double CrestDistance = double.PositiveInfinity, double ToeDistance = double.PositiveInfinity)
{
    // One immutable per-query boundary computation is shared by automatic constraints.
    internal TerrainDifferential? BoundaryWeight { get; init; }
}

/// <summary>
/// Format-independent implementation of the frozen query interface. The operation compiler supplies a pure sampler,
/// its complete conservative support bounds, and resolved pad/water tables. This type never reads JSON or invents defaults.
/// </summary>
public sealed class TerrainHeightField : ITerrain
{
    private readonly MapRect _dataRect;
    private readonly Func<double, double, TerrainEvaluation> _sample;
    private readonly MapRect[] _supports;
    private readonly IReadOnlyDictionary<FeatureId, float> _pads;
    private readonly IReadOnlyDictionary<FeatureId, float> _waters;

    public TerrainHeightField(MapRect dataRect, Func<double, double, TerrainEvaluation> sample,
        IReadOnlyList<MapRect> nonZeroSupports, IReadOnlyDictionary<FeatureId, float>? pads = null,
        IReadOnlyDictionary<FeatureId, float>? waters = null)
    {
        _ = TerrainSampling.Grid(default, 4, dataRect);
        ArgumentNullException.ThrowIfNull(sample); ArgumentNullException.ThrowIfNull(nonZeroSupports);
        _dataRect = dataRect; _sample = sample;
        _supports = nonZeroSupports.OrderBy(rect => rect.Min.X).ThenBy(rect => rect.Min.Y)
            .ThenBy(rect => rect.Max.X).ThenBy(rect => rect.Max.Y).ToArray();
        foreach (var support in _supports) _ = TerrainSampling.Grid(default, 4, support);
        _pads = Snapshot(pads); _waters = Snapshot(waters);
    }

    public bool IsFlat(TileKey tile)
    {
        var grid = TerrainSampling.Grid(tile, 0, _dataRect);
        var maxX = (double)grid.Origin.X + TerrainSampling.TileQuads;
        var maxZ = (double)grid.Origin.Y + TerrainSampling.TileQuads;
        foreach (var support in _supports)
            if (support.Min.X <= maxX && support.Max.X >= grid.Origin.X && support.Min.Y <= maxZ && support.Max.Y >= grid.Origin.Y) return false;
        return true;
    }

    public float HeightAt(float x, float z) => (float)Evaluate(x, z).Surface.Height;

    public (float Height, TerrainFacts Facts) SampleAt(float x, float z)
    {
        var value = Evaluate(x, z);
        return ((float)value.Surface.Height, Facts(value));
    }

    public void FillTile(TileKey tile, int level, Span<short> cm)
    {
        if (cm.Length != TerrainSampling.SampleCount(level)) throw new ArgumentException("Unexpected terrain sample count.", nameof(cm));
        var grid = TerrainSampling.Grid(tile, level, _dataRect);
        for (var z = 0; z < grid.Height; z++)
            for (var x = 0; x < grid.Width; x++)
                cm[z * grid.Width + x] = TerrainMath.QuantizeCentimetres(Evaluate(grid.Origin.X + x * (double)grid.Step.X,
                    grid.Origin.Y + z * (double)grid.Step.Y).Surface.Height);
    }

    public float PadY(FeatureId feature) => _pads.TryGetValue(feature, out var height) ? height
        : throw new KeyNotFoundException($"No compiled pad for feature '{feature.Value}'.");
    public bool TryGetPadY(FeatureId feature, out float y) => _pads.TryGetValue(feature, out y);
    public float? WaterY(FeatureId water) => _waters.TryGetValue(water, out var height) ? height : null;

    public TerrainFacts FactsAt(float x, float z) => Facts(Evaluate(x, z));

    private static TerrainFacts Facts(TerrainEvaluation value)
    {
        var surface = value.Surface;
        var grade = Math.Sqrt(surface.Dx * surface.Dx + surface.Dz * surface.Dz);
        if (!double.IsFinite(grade) || grade > float.MaxValue) throw new InvalidOperationException("Terrain gradient is not representable.");
        return new((float)grade, value.Class, (float)value.CrestDistance, (float)value.ToeDistance);
    }

    private TerrainEvaluation Evaluate(double x, double z)
    {
        TerrainMath.RequireFinite(x, nameof(x)); TerrainMath.RequireFinite(z, nameof(z));
        var result = _sample(x, z);
        var surface = result.Surface;
        TerrainMath.RequireFinite(surface.Height, nameof(surface.Height));
        TerrainMath.RequireFinite(surface.Dx, nameof(surface.Dx)); TerrainMath.RequireFinite(surface.Dz, nameof(surface.Dz));
        if (surface.Height < TerrainSampling.MinimumHeight || surface.Height > TerrainSampling.MaximumHeight)
            throw new InvalidOperationException($"Terrain height {surface.Height} at ({x}, {z}) is outside the authored range before clamping.");
        _ = result.Class switch { TerrainClass.Flat or TerrainClass.Slope or TerrainClass.Transition => true, _ => throw new InvalidOperationException("Unknown terrain class.") };
        if (double.IsNaN(result.CrestDistance) || double.IsNegativeInfinity(result.CrestDistance)
            || double.IsNaN(result.ToeDistance) || double.IsNegativeInfinity(result.ToeDistance))
            throw new InvalidOperationException("Terrain line distances must be finite or positive infinity.");
        return result;
    }

    private static IReadOnlyDictionary<FeatureId, float> Snapshot(IReadOnlyDictionary<FeatureId, float>? input)
    {
        var copy = new Dictionary<FeatureId, float>();
        if (input is not null)
            foreach (var pair in input.OrderBy(pair => pair.Key.Value, StringComparer.Ordinal))
            {
                if (string.IsNullOrWhiteSpace(pair.Key.Value) || !float.IsFinite(pair.Value)
                    || pair.Value < TerrainSampling.MinimumHeight || pair.Value > TerrainSampling.MaximumHeight)
                    throw new ArgumentException("Compiled terrain levels require IDs and finite in-range values.", nameof(input));
                copy.Add(pair.Key, pair.Value);
            }
        return new ReadOnlyDictionary<FeatureId, float>(copy);
    }
}
