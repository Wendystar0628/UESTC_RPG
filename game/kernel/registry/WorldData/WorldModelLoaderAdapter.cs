using Campus.Contracts;

namespace Campus.Kernel.Maps;

/// <summary>The kernel owns I/O and registration; the world generator only consumes detached bytes.</summary>
[KernelService(typeof(IWorldModelLoader), Owner = "WP-00")]
public sealed class WorldModelLoaderAdapter : IWorldModelLoader
{
    public WorldModelLoadResult Load(MapId map, IReadOnlyDictionary<string, ReadOnlyMemory<byte>> files) =>
        Campus.Gen.World.WorldModelLoader.Load(map, files);

    public WorldModelLoadResult Load(MapId map, IReadOnlyDictionary<string, ReadOnlyMemory<byte>> files,
        WorldModelLoadInputs inputs)
    {
        if (inputs.Descriptor.Id != map) throw new InvalidDataException("World load descriptor does not match the target map.");
        return Campus.Gen.World.WorldModelLoader.Load(map, files, new()
        { Walk = inputs.Walk, ClaimedBy = inputs.ClaimedBy });
    }
}
