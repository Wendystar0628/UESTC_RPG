using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Atlas;
using Campus.Contracts.Maps;

namespace Campus.Pipeline.Map;

/// <summary>CPU-only input. The kernel owns scheduling, source loading, hashes and cache IO.</summary>
public sealed record MapBakeInput(
    IWorldModel World, MapDescriptor Map, MapRect AtlasRect, AtlasLabels Labels,
    string WorldModelHash, string CodeHash, string LabelInputHash)
{
    public MapBakeOptions Options { get; init; } = new();
    public LabelLayoutInput? LabelLayout { get; init; }
    public IReadOnlyList<MapRegionArea> RegionAreas { get; init; } = [];
}

public sealed record MapRegionArea(string Id, IReadOnlyList<PolygonRings> Polygons);

public sealed record MapBakeOptions
{
    public int MainSize { get; init; } = 2048;
    public int ClassSize { get; init; } = 1024;
    public int OutskirtsSize { get; init; } = 1024;
    public int RingSize { get; init; } = 896;
    public float RegionCellMetres { get; init; } = 8;
    public float BucketMetres { get; init; } = 32;
    public float CampusMarginMetres { get; init; } = 64;
}

public enum MapFeatureClass : byte { None, Building, Landmark, Plaza, Sport, Bridge }

/// <summary>Row-major RGBA8, no header/mips. Column = +X east, row = +Z south.</summary>
public sealed record MapTexture(string Name, int Width, int Height, MapRect Rect, byte[] Rgba8);
public sealed record MapTextureInfo(
    string Name, string File, int Width, int Height, MapRect Rect,
    Vector2 TexelMetres, string[] Channels, string Sha256);
public sealed record MapRegionInfo(
    string File, MapRect Rect, int Width, int Height, float CellMetres, string[] Ids, string Sha256);

public sealed record MapManifest(
    int Version, string Map, string WorldModelHash, string CodeHash, string LabelInputHash,
    MapRect DataRect, MapRect AtlasRect, int RegionTableVersion,
    IReadOnlyList<MapTextureInfo> Textures, MapRegionInfo Regions,
    string[] Warnings)
{
    public string Format { get; init; } = "rgba8-linear";
    public string RowOrder { get; init; } = "z-increasing";
    public float DistanceRangeMetres { get; init; } = 8;
    public int BoundaryByte { get; init; } = 128;
    public string DistanceEncoding { get; init; } = "clamp(round(128+d*16),0,255); negative-inside";
    public string CategorySampling { get; init; } = "nearest-base-level";
    public string RegionMethod { get; init; } = "authoritative-region-polygons; child-place-voronoi; unknown-if-missing";
}

/// <summary>Index zero means unknown/outside. Ids[0] is the empty string.</summary>
public sealed record MapRegionGrid(MapRect Rect, int Width, int Height, float CellMetres, string[] Ids, byte[] Cells)
{
    public string? RegionAt(float x, float z)
    {
        if (!float.IsFinite(x) || !float.IsFinite(z) || x < Rect.Min.X || z < Rect.Min.Y || x >= Rect.Max.X || z >= Rect.Max.Y)
            return null;
        int column = (int)((x - Rect.Min.X) / CellMetres);
        int row = (int)((z - Rect.Min.Y) / CellMetres);
        if (column >= Width || row >= Height) return null;
        byte index = Cells[row * Width + column];
        return index == 0 ? null : Ids[index];
    }
}

public sealed record MapBakeProduct(
    MapManifest Manifest, IReadOnlyList<MapTexture> Textures, MapRegionGrid Regions, LabelsBake Labels);

public sealed record AtlasLabel(
    string Id, string Kind, int Priority, LocalizedName? Name = null, string? Ref = null,
    Vector2? At = null, IReadOnlyList<Vector2>? Line = null);

public sealed record AtlasLabels(
    int Version, IReadOnlyList<AtlasRegion> Regions, IReadOnlyList<AtlasRegion> Places, IReadOnlyList<AtlasLabel> Labels);

/// <summary>Measured by the engine with the actual runtime font. Bounds include spacing and halo.</summary>
public sealed record LabelMeasurement(string Id, string Language, byte ZoomBand, string Text, Vector2 Size);
public sealed record LabelExclusion(string Language, byte ZoomBand, Vector2 At, Vector2 Size);
public sealed record LabelLayoutInput(
    IReadOnlyList<LabelMeasurement> Measurements, IReadOnlyList<LabelExclusion> Exclusions,
    IReadOnlyList<float> PixelsPerMetre, string FontMetricsHash);
public sealed record PlacedLabel(string Id, string Text, Vector2 At, Vector2 OffsetPixels, Vector2 Size);
public sealed record LabelBand(string Language, byte ZoomBand, float PixelsPerMetre, IReadOnlyList<PlacedLabel> Labels);
public sealed record LabelsBake(int Version, string InputHash, string FontMetricsHash, bool Ready, IReadOnlyList<LabelBand> Bands);
