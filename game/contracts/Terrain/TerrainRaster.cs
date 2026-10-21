using System.Numerics;

using Campus.Contracts.Maps;

namespace Campus.Contracts;

/// <summary>Frozen terrain data: no operation evaluators or runtime callbacks are retained.</summary>
public sealed record TerrainFrozenData(MapRect DataRect, TerrainInputSummary SourceInputs,
    TerrainFeatureLevel[] Pads, TerrainFeatureLevel[] WaterLevels, TerrainWaterBody[] Waters,
    TerrainBankProfile[] Banks, MapRect[] Supports, short[] DistantHeights, string[] Diagnostics);

public readonly record struct TerrainFeatureLevel(FeatureId Feature, float Y);
public sealed record TerrainBankProfile(TerrainBankSpan Span, double[] Stations, short[] Heights);

/// <summary>One 256 m block, including the shared boundary, on the authoritative integer-metre lattice.</summary>
public sealed record TerrainRasterBlock(TileKey Block, short[] Heights, TerrainFacts[] Facts)
{
    public const int Edge = 256;
    public const int Width = Edge + 1;
    public const int Count = Width * Width;
}

public readonly record struct TerrainRasterPoint(int X, int Z, short Height, TerrainFacts Facts);

/// <summary>Cold CPU asset construction only; never attached to the published runtime world.</summary>
public interface ITerrainAssetBuilder : ITerrain
{
    IReadOnlyList<string> Diagnostics { get; }
    void Attach(IPreparedTerrain terrain);
    TerrainFrozenData Freeze();
    TerrainRasterBlock BuildBlock(TileKey block, IPreparedTerrain terrain);
    TerrainRasterPoint[] SnapshotPoints();
}

/// <summary>Publishes immutable blocks before exposing the corresponding query region. Sampling itself performs no IO.</summary>
public interface IPreparedTerrain : ITerrain
{
    bool HasBlock(TileKey block);
    bool TryReadNode(int x, int z, out TerrainRasterPoint point);
    bool IsPrepared(Vector2 point);
    void Publish(TerrainRasterBlock block);
    void Publish(IReadOnlyList<TerrainRasterPoint> points);
}

