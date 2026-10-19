using Campus.Contracts;
using Campus.Gen.World;

namespace Campus.Kernel.Maps;

/// <summary>The kernel owns I/O and registration; the world generator only consumes detached bytes.</summary>
[KernelService(typeof(IWorldModelLoader), Owner = "WP-00")]
public sealed class WorldModelLoaderAdapter : IWorldModelLoader
{
    public WorldModelLoadResult Load(MapId map, IReadOnlyDictionary<string, ReadOnlyMemory<byte>> files,
        WorldModelLoadInputs inputs)
    {
        if (inputs.Descriptor.Id != map) throw new InvalidDataException("World load descriptor does not match the target map.");
        return WorldModelLoader.LoadWorld(inputs, files);
    }
}
