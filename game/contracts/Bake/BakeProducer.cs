namespace Campus.Contracts.Bake;

public enum BakePayloadClass { Tile, Product }
public enum BakeProductKind { Collision, Map, Vegetation, Fauna, Nightlight }

public static class BakeCodeChains
{
    public const string Geometry = "geometry";
    public const string Collision = "collision";
    public const string Map = "map";
    public const string Vegetation = "vegetation";
    public const string Fauna = "fauna";
    public const string Nightlight = "nightlight";
    public const string Terrain = "terrain";
    public const string InputIndex = "input-index";
    public const string Resident = "resident";
}

/// <summary>The single packaged build-time code fingerprint table; no source or IL hashing at runtime.</summary>
public sealed record BakeCodeManifest(int Schema, IReadOnlyDictionary<string, string> Roots)
{
    public const int CurrentSchema = 1;
    public static string Chain(string key) => "chain:" + key;
    public static string TypeKey(string assembly, string metadataName) => assembly + ":" + metadataName;
    public static string TypeKey(Type type) => TypeKey(
        type.Assembly.GetName().Name ?? throw new ArgumentException("CPU root assembly has no name.", nameof(type)),
        type.FullName ?? throw new ArgumentException("CPU root type has no metadata name.", nameof(type)));
}

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class BakeProducerAttribute(string id) : Attribute
{
    public string Id { get; } = id;
    public string Owner { get; set; } = "";
}

/// <summary>
/// Pure CPU production extension, discovered by the kernel. A request must resolve to exactly one producer.
/// Build is invoked on a worker thread and may only return a registered fixed payload type
/// (geometry, collision, map, vegetation, fauna or nightlight). Codecs never deserialize runtime type names.
/// </summary>
public interface IBakeProducer
{
    BakeInputDependencies Inputs(BakeKey key);
    BakePayloadClass PayloadClass { get; }
    BakeProductKind ProductKind { get; }
    bool UsesModule(IContentModule module);
    /// <summary>Lists prepared auxiliary keys and influence bounds without generating geometry, instance records or sampling heights.</summary>
    IReadOnlyList<BakeTileEntry> Tiles(BuildContext context);
    bool CanBuild(BakeKey key);
    object Build(BakeKey key, BuildContext context);
}
