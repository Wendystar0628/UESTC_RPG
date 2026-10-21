using System.Collections.ObjectModel;
using System.Collections.Concurrent;
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
/// Map-scoped asset sampler. Only missing integer-metre nodes evaluate operations; all other positions use their centimetre lattice.
/// </summary>
public sealed class TerrainHeightField : ITerrain, ITerrainLatticeNodes
{
    public TerrainInputSummary SourceInputs { get; }
    private readonly MapRect _dataRect;
    private readonly Func<double, double, TerrainEvaluation> _sample;
    private readonly MapRect[] _supports;
    private readonly IReadOnlyDictionary<FeatureId, float> _pads;
    private readonly IReadOnlyDictionary<FeatureId, float> _waters;
    private readonly ConcurrentDictionary<(int X, int Z), (short Height, TerrainFacts Facts)> _nodes = new();
    private IPreparedTerrain? _prepared;
    internal IReadOnlyList<MapRect> Supports => _supports;
    internal TerrainFeatureLevel[] Pads => _pads.Select(pair => new TerrainFeatureLevel(pair.Key, pair.Value)).ToArray();
    internal TerrainFeatureLevel[] WaterLevels => _waters.Select(pair => new TerrainFeatureLevel(pair.Key, pair.Value)).ToArray();

    public TerrainHeightField(TerrainInputSummary sourceInputs, MapRect dataRect, Func<double, double, TerrainEvaluation> sample,
        IReadOnlyList<MapRect> nonZeroSupports, IReadOnlyDictionary<FeatureId, float>? pads = null,
        IReadOnlyDictionary<FeatureId, float>? waters = null)
    {
        _ = TerrainSampling.Grid(default, 4, dataRect);
        ArgumentNullException.ThrowIfNull(sample); ArgumentNullException.ThrowIfNull(nonZeroSupports);
        SourceInputs = sourceInputs ?? throw new ArgumentNullException(nameof(sourceInputs));
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

    public float HeightAt(float x, float z) => SampleAt(x, z).Height;

    public (float Height, TerrainFacts Facts) SampleAt(float x, float z)
    {
        return TerrainLattice.Sample(x, z, this);
    }

    public void FillTile(TileKey tile, int level, Span<short> cm)
    {
        if (cm.Length != TerrainSampling.SampleCount(level)) throw new ArgumentException("Unexpected terrain sample count.", nameof(cm));
        var grid = TerrainSampling.Grid(tile, level, _dataRect);
        for (var z = 0; z < grid.Height; z++)
            for (var x = 0; x < grid.Width; x++)
                cm[z * grid.Width + x] = TerrainMath.QuantizeCentimetres(HeightAt(grid.Origin.X + x * grid.Step.X,
                    grid.Origin.Y + z * grid.Step.Y));
    }

    public float PadY(FeatureId feature) => _pads.TryGetValue(feature, out var height) ? height
        : throw new KeyNotFoundException($"No compiled pad for feature '{feature.Value}'.");
    public bool TryGetPadY(FeatureId feature, out float y) => _pads.TryGetValue(feature, out y);
    public float? WaterY(FeatureId water) => _waters.TryGetValue(water, out var height) ? height : null;

    public TerrainFacts FactsAt(float x, float z) => SampleAt(x, z).Facts;

    private (short Height, TerrainFacts Facts) Node(int x, int z)
    {
        if (_prepared is { } prepared && prepared.TryReadNode(x, z, out var point)) return (point.Height, point.Facts);
        var value = _nodes.GetOrAdd((x, z), location => ReadNode(location.X, location.Z));
        // A worker may finish a cold sample while its prepared block is being published.
        if (_prepared is { } published && published.TryReadNode(x, z, out point))
        {
            _nodes.TryRemove((x, z), out _);
            return (point.Height, point.Facts);
        }
        return value;
    }
    private (short Height, TerrainFacts Facts) ReadNode(int x, int z)
    {
        var value = Evaluate(x, z);
        return (TerrainMath.QuantizeCentimetres(value.Surface.Height), Facts(value));
    }
    (short Height, TerrainFacts Facts) ITerrainLatticeNodes.Node(int x, int z) => Node(x, z);

    internal void Attach(IPreparedTerrain terrain)
    {
        ArgumentNullException.ThrowIfNull(terrain);
        if (_prepared is not null && !ReferenceEquals(_prepared, terrain))
            throw new InvalidOperationException("Terrain assets cannot attach to another prepared world.");
        _prepared = terrain;
        foreach (var node in _nodes)
            if (terrain.TryReadNode(node.Key.X, node.Key.Z, out _)) _nodes.TryRemove(node.Key, out _);
    }

    internal TerrainRasterBlock BuildBlock(TileKey block, IPreparedTerrain terrain)
    {
        Attach(terrain);
        var heights = new short[TerrainRasterBlock.Count];
        var facts = new TerrainFacts[TerrainRasterBlock.Count];
        for (var z = 0; z < TerrainRasterBlock.Width; z++)
            for (var x = 0; x < TerrainRasterBlock.Width; x++)
            {
                var worldX = block.X * TerrainRasterBlock.Edge + x;
                var worldZ = block.Z * TerrainRasterBlock.Edge + z;
                var sample = terrain.TryReadNode(worldX, worldZ, out var existing) ? (existing.Height, existing.Facts)
                    : _nodes.TryGetValue((worldX, worldZ), out var memo) ? memo : ReadNode(worldX, worldZ);
                var index = z * TerrainRasterBlock.Width + x;
                heights[index] = sample.Height; facts[index] = sample.Facts;
            }
        var result = new TerrainRasterBlock(block, heights, facts);
        terrain.Publish(result);
        for (var z = 0; z < TerrainRasterBlock.Width; z++)
            for (var x = 0; x < TerrainRasterBlock.Width; x++)
                _nodes.TryRemove((block.X * TerrainRasterBlock.Edge + x, block.Z * TerrainRasterBlock.Edge + z), out _);
        return result;
    }

    internal TerrainRasterPoint[] SnapshotPoints() => _nodes.OrderBy(pair => pair.Key.X).ThenBy(pair => pair.Key.Z)
        .Select(pair => new TerrainRasterPoint(pair.Key.X, pair.Key.Z, pair.Value.Height, pair.Value.Facts)).ToArray();

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
