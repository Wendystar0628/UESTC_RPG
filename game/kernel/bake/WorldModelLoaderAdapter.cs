using Campus.Contracts;
using Campus.Gen.World;

namespace Campus.Kernel.Bake;

/// <summary>The kernel owns I/O and registration; the world generator only consumes detached bytes.</summary>
[KernelService(typeof(IWorldModelLoader))]
public sealed class WorldModelLoaderAdapter : IWorldModelLoader
{
    private readonly TerrainCacheService _terrain;
    public WorldModelLoaderAdapter(TerrainCacheService terrain) => _terrain = terrain;
    public WorldModelLoadResult Load(MapId map, IReadOnlyDictionary<string, ReadOnlyMemory<byte>> files,
        WorldModelLoadInputs inputs)
    {
        if (inputs.Descriptor.Id != map) throw new InvalidDataException("World load descriptor does not match the target map.");
        return _terrain.Load(map, WorldModelLoader.PrepareWorld(inputs, files));
    }
}
