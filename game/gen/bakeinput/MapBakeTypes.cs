using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Atlas;
using Campus.Contracts.Maps;

namespace Campus.Gen.BakeInput;

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
