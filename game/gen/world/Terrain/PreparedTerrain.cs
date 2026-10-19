using System.Collections.Concurrent;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Gen.Geometry;

namespace Campus.Gen.World;

/// <summary>The published terrain contains only prepared raster blocks and frozen metadata.</summary>
internal sealed class PreparedTerrain(TerrainFrozenData data) : IPreparedTerrain, ITerrainBankSource, ITerrainWaterSource, ITerrainLatticeNodes
{
    private readonly ConcurrentDictionary<TileKey, TerrainRasterBlock> _blocks = new();
    private readonly ConcurrentDictionary<(int X, int Z), TerrainRasterPoint> _points = new();
    private readonly Dictionary<FeatureId, float> _pads = data.Pads.ToDictionary(value => value.Feature, value => value.Y);
    private readonly Dictionary<FeatureId, float> _waters = data.WaterLevels.ToDictionary(value => value.Feature, value => value.Y);
    private readonly Dictionary<string, TerrainBankProfile> _banks = data.Banks.ToDictionary(value => value.Span.Id, StringComparer.Ordinal);
    public TerrainInputSummary SourceInputs => data.SourceInputs;
    public IReadOnlyList<TerrainBankSpan> Banks { get; } = Array.AsReadOnly(data.Banks.Select(bank => bank.Span).ToArray());
    public IReadOnlyList<TerrainWaterBody> Waters => data.Waters;
    public bool HasBlock(TileKey block) => _blocks.ContainsKey(block);
    public bool TryReadNode(int x, int z, out TerrainRasterPoint point)
    {
        if (TryNode(x, z, out var value)) { point = new(x, z, value.Height, value.Facts); return true; }
        point = default; return false;
    }
    public bool IsPrepared(Vector2 point)
    {
        var grid = TerrainSampling.QueryGrid(point);
        for (var dz = 0; dz < grid.Height; dz++)
            for (var dx = 0; dx < grid.Width; dx++)
                if (!TryNode(checked((int)grid.Origin.X + dx), checked((int)grid.Origin.Y + dz), out _)) return false;
        return true;
    }
    public void Publish(IReadOnlyList<TerrainRasterPoint> points)
    {
        foreach (var point in points)
            if (!TryNode(point.X, point.Z, out _)) _points.TryAdd((point.X, point.Z), point);
    }
    public void Publish(TerrainRasterBlock block)
    {
        if (block.Heights.Length != TerrainRasterBlock.Count || block.Facts.Length != TerrainRasterBlock.Count)
            throw new ArgumentException("Terrain block dimensions do not match the metre lattice.", nameof(block));
        if (!_blocks.TryAdd(block.Block, block)) return;
        for (var z = 0; z < TerrainRasterBlock.Width; z++)
            for (var x = 0; x < TerrainRasterBlock.Width; x++)
                _points.TryRemove((block.Block.X * TerrainRasterBlock.Edge + x, block.Block.Z * TerrainRasterBlock.Edge + z), out _);
    }
    public bool IsFlat(TileKey tile)
    {
        var origin = TileGrid.Origin(tile, LodLevel.L0);
        return !data.Supports.Any(box => box.Min.X <= origin.X + TileGrid.BaseEdge && box.Max.X >= origin.X &&
            box.Min.Y <= origin.Y + TileGrid.BaseEdge && box.Max.Y >= origin.Y);
    }
    public float HeightAt(float x, float z) => TerrainLattice.Sample(x, z, this).Height;
    public (float Height, TerrainFacts Facts) SampleAt(float x, float z) => TerrainLattice.Sample(x, z, this);
    (short Height, TerrainFacts Facts) ITerrainLatticeNodes.Node(int x, int z) => Node(x, z);
    public TerrainFacts FactsAt(float x, float z) => SampleAt(x, z).Facts;
    public void FillTile(TileKey tile, int level, Span<short> cm)
    {
        if (cm.Length != TerrainSampling.SampleCount(level)) throw new ArgumentException("Unexpected terrain sample count.", nameof(cm));
        if (level == 4) { data.DistantHeights.AsSpan().CopyTo(cm); return; }
        var grid = TerrainSampling.Grid(tile, level, data.DataRect);
        for (var z = 0; z < grid.Height; z++)
            for (var x = 0; x < grid.Width; x++)
                cm[z * grid.Width + x] = Node((int)(grid.Origin.X + x * grid.Step.X), (int)(grid.Origin.Y + z * grid.Step.Y)).Height;
    }
    private (short Height, TerrainFacts Facts) Node(int x, int z)
    {
        if (TryNode(x, z, out var result)) return result;
        throw new InvalidOperationException($"Terrain region at ({x}, {z}) was queried before preparation.");
    }
    private bool TryNode(int x, int z, out (short Height, TerrainFacts Facts) result)
    {
        if (_points.TryGetValue((x, z), out var point)) { result = (point.Height, point.Facts); return true; }
        var bx = (int)Math.Floor(x / (double)TerrainRasterBlock.Edge);
        var bz = (int)Math.Floor(z / (double)TerrainRasterBlock.Edge);
        // Shared boundary nodes belong to either of their prepared blocks.
        for (var dz = 0; dz <= (z % TerrainRasterBlock.Edge == 0 ? 1 : 0); dz++)
            for (var dx = 0; dx <= (x % TerrainRasterBlock.Edge == 0 ? 1 : 0); dx++)
                if (_blocks.TryGetValue(new(bx - dx, bz - dz), out var block))
                {
                    var index = (z - block.Block.Z * TerrainRasterBlock.Edge) * TerrainRasterBlock.Width + x - block.Block.X * TerrainRasterBlock.Edge;
                    result = (block.Heights[index], block.Facts[index]); return true;
                }
        result = default; return false;
    }
    public float PadY(FeatureId feature) => _pads.TryGetValue(feature, out var y) ? y : throw new KeyNotFoundException($"No pad for '{feature.Value}'.");
    public bool TryGetPadY(FeatureId feature, out float y) => _pads.TryGetValue(feature, out y);
    public float? WaterY(FeatureId water) => _waters.TryGetValue(water, out var y) ? y : null;
    public (FeatureId Id, float Surface, bool Wade)? WaterAt(float x, float z)
    {
        TerrainWaterBody? first = null;
        foreach (var water in data.Waters)
        {
            if (!water.Polygons.Any(polygon => PlanGeometry.Contains(polygon, new Vector2(x, z)))) continue;
            first ??= water;
            if (water.WadeAreas.Any(polygon => PlanGeometry.Contains(polygon, new Vector2(x, z))))
                return (water.Water, _waters[water.Water], true);
        }
        return first is null ? null : (first.Water, _waters[first.Water], false);
    }
    public float BankTopY(string id, double station)
    {
        if (string.IsNullOrWhiteSpace(id) || id != id.Trim() || id.Any(char.IsControl)) throw new ArgumentException("Malformed bank span ID.", nameof(id));
        if (!_banks.TryGetValue(id, out var bank)) throw new KeyNotFoundException($"Unknown bank span '{id}'.");
        if (!double.IsFinite(station) || station < 0 || station > bank.Span.Length) throw new ArgumentOutOfRangeException(nameof(station));
        var index = Array.BinarySearch(bank.Stations, station);
        if (index >= 0) return bank.Heights[index] * TerrainSampling.Centimetre;
        index = ~index;
        var t = (station - bank.Stations[index - 1]) / (bank.Stations[index] - bank.Stations[index - 1]);
        return (float)((bank.Heights[index - 1] * (1 - t) + bank.Heights[index] * t) * TerrainSampling.Centimetre);
    }
}

/// <summary>One interpolation rule for cold assets and prepared runtime queries.</summary>
internal interface ITerrainLatticeNodes
{
    (short Height, TerrainFacts Facts) Node(int x, int z);
}

internal static class TerrainLattice
{
    public static (float Height, TerrainFacts Facts) Sample(float x, float z, ITerrainLatticeNodes source)
    {
        if (!float.IsFinite(x) || !float.IsFinite(z)) throw new ArgumentOutOfRangeException(nameof(x));
        var ix = checked((int)MathF.Floor(x)); var iz = checked((int)MathF.Floor(z));
        var u = x - ix; var v = z - iz;
        var a = source.Node(ix, iz);
        if (u == 0 && v == 0) return (a.Height * TerrainSampling.Centimetre, a.Facts);
        var b = source.Node(ix + 1, iz); var c = source.Node(ix, iz + 1);
        var d = u + v > 1 ? source.Node(ix + 1, iz + 1) : a;
        var p = u + v <= 1 ? a : d;
        var wp = u + v <= 1 ? 1 - u - v : u + v - 1;
        var wb = u + v <= 1 ? u : 1 - v;
        var wc = u + v <= 1 ? v : 1 - u;
        var height = (p.Height * wp + b.Height * wb + c.Height * wc) * TerrainSampling.Centimetre;
        var dx = (u + v <= 1 ? b.Height - a.Height : d.Height - c.Height) * TerrainSampling.Centimetre;
        var dz = (u + v <= 1 ? c.Height - a.Height : d.Height - b.Height) * TerrainSampling.Centimetre;
        var facts = wp >= wb && wp >= wc ? p.Facts : wb >= wc ? b.Facts : c.Facts;
        return (height, new(MathF.Sqrt(dx * dx + dz * dz), facts.Class,
            Distance(p.Facts.CrestDist, b.Facts.CrestDist, c.Facts.CrestDist), Distance(p.Facts.ToeDist, b.Facts.ToeDist, c.Facts.ToeDist)));
        float Distance(float pValue, float bValue, float cValue) =>
            wp > 0 && float.IsPositiveInfinity(pValue) || wb > 0 && float.IsPositiveInfinity(bValue) || wc > 0 && float.IsPositiveInfinity(cValue)
                ? float.PositiveInfinity : (wp > 0 ? pValue * wp : 0) + (wb > 0 ? bValue * wb : 0) + (wc > 0 ? cValue * wc : 0);
    }
}
