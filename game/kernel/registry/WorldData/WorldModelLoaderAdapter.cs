using Campus.Contracts;
using Campus.Gen.World;
using Campus.Pipeline.Walk;

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
        var loaded = WorldModelLoader.Load(map, files, new()
        { Walk = inputs.Walk, ClaimedBy = inputs.ClaimedBy });
        if (inputs.Walk is null) return loaded; // Source/area-claim parsing is deliberately not a playable world.
        return loaded with { World = WalkWorldPreparation.Restore((WorldModelSnapshot)loaded.World, inputs.Walk.Support) };
    }
}
