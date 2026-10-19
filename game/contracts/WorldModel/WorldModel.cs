using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Campus.Contracts;

/// <summary>Plan coordinates are (world X, world Z), in metres; rings omit the repeated closing vertex.</summary>
[method: JsonConstructor]
public sealed record PolygonRings(IReadOnlyList<Vector2> Outer, IReadOnlyList<IReadOnlyList<Vector2>> Holes)
{
    public PolygonRings(IReadOnlyList<Vector2> outer) : this(outer, []) { }
}
public sealed record Polyline(IReadOnlyList<Vector2> Points);

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(PolygonGeometry), "polygon")]
[JsonDerivedType(typeof(PolylineGeometry), "polyline")]
[JsonDerivedType(typeof(MultiPolygonGeometry), "multiPolygon")]
[JsonDerivedType(typeof(PointGeometry), "point")]
public abstract record OverlayGeometry;
public sealed record PolygonGeometry(PolygonRings Rings) : OverlayGeometry;
public sealed record PolylineGeometry(Polyline Line) : OverlayGeometry;
public sealed record MultiPolygonGeometry(IReadOnlyList<PolygonRings> Polygons) : OverlayGeometry;
/// <summary>A non-area POI in world X/Y/Z; Y is its known ground height or zero when unavailable.</summary>
public sealed record PointGeometry(Vector3 Position) : OverlayGeometry;

// Open-ended vocabulary keys. New building/appearance families are data additions, not enum renumbering.
public readonly record struct BuildingFamily(string Value);
public readonly record struct FacadePreset(string Value);
public readonly record struct DesignKind(string Value);

/// <summary>One source geometry of an entity, retaining its own height and storey count. SourceId is provenance, not a FeatureId.</summary>
public sealed record WorldFeaturePart(string SourceId, OverlayGeometry Geometry, float? Height, int? Storeys)
{
    /// <summary>Optional source-derived facade layout; Geometry retains the detailed physical footprint.</summary>
    public OverlayGeometry? FacadeGeometry { get; init; }
    public JsonElement? Props { get; init; }
}

/// <summary>
/// Id remains the campus entity identity. Geometry and Bounds are its aggregate query envelope.
/// When Parts is non-empty, generators build each part and must not additionally build aggregate Geometry.
/// Simple design features may leave Parts empty and generate directly from Geometry.
/// </summary>
public sealed record WorldFeature(FeatureId Id, DesignKind Kind, OverlayGeometry Geometry)
{
    public IReadOnlyList<WorldFeaturePart> Parts { get; init; } = [];
    public string? Name { get; init; }
    public ZoneId? Zone { get; init; }
    public float? Height { get; init; }
    public int? Storeys { get; init; }
    public BuildingFamily? Family { get; init; }
    public FacadePreset? Facade { get; init; }
    public Box3 Bounds { get; init; }
    public JsonElement? Props { get; init; }
}

public sealed record NetworkLine(FeatureId Id, Polyline Line, float Width = 0f)
{
    public JsonElement? Props { get; init; }
}
public sealed record NetworkTree(FeatureId Id, Vector3 Position, string? Species = null);
public sealed record WorldNetwork(
    IReadOnlyList<NetworkLine> Roads,
    IReadOnlyList<NetworkLine> Waterways,
    IReadOnlyList<NetworkLine> Bridges,
    IReadOnlyList<NetworkLine> Culverts,
    IReadOnlyList<NetworkTree> Trees)
{
    public static WorldNetwork Empty { get; } = new([], [], [], [], []);
}

public interface IWorldModel
{
    /// <summary>Current map terrain; maps without authored terrain use a flat surface.</summary>
    ITerrain Terrain => FlatTerrain.Default;
    /// <summary>Actual campus boundary from source geometry, including holes; never the map's bounding rectangle.</summary>
    PolygonRings Boundary { get; }
    IReadOnlyDictionary<FeatureId, WorldFeature> Features { get; }
    /// <summary>Authored terrain operations indexed by their stable overlay IDs.</summary>
    IReadOnlyDictionary<string, TerrainOp> TerrainOperations { get; }
    WorldNetwork Network { get; }
    ISpatialIndex Spatial { get; }
    IInteriorIndex Interiors { get; }
    IOutskirtsModel? Outskirts { get; }
    IFidelityModel? Fidelity { get; }
}
public interface ISpatialIndex
{
    IReadOnlyList<FeatureId> Query(Box3 box);
    /// <summary>Exactly the current world terrain's HeightAt(x, z), without a second low-resolution height table.</summary>
    float GroundY(float x, float z);
    CameraMedium MediumAt(Vector3 p);
    WalkClass WalkClassAt(float x, float z);
}
