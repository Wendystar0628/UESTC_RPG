namespace Campus.Contracts.Bake;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class BakeProducerAttribute(string id) : Attribute
{
    public string Id { get; } = id;
    public string Owner { get; set; } = "";
}

/// <summary>
/// Pure CPU production extension, discovered by the kernel. A request must resolve to exactly one producer.
/// Build is invoked on a worker thread and may only return a registered fixed payload type
/// (TileBundle, CollisionTile, or the map pipeline's MapBakeProduct). Codecs never deserialize runtime type names.
/// </summary>
public interface IBakeProducer
{
    bool CanBuild(BakeKey key);
    object Build(BakeKey key, BuildContext context);
}
