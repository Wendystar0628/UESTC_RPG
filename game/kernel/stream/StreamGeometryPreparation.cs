using Campus.Contracts;
using Campus.Contracts.Bake;

namespace Campus.Kernel.Stream;

/// <summary>Initializes caller-owned native tile storage; faults remain on the actual task through drain.</summary>
public interface IStreamGeometryPreparation
{
    Task PrepareAsync(PreparedTile owner, TileBundle bundle);
}

[KernelService(typeof(IStreamGeometryPreparation))]
public sealed class StreamGeometryPreparation : IStreamGeometryPreparation
{
    public Task PrepareAsync(PreparedTile owner, TileBundle bundle) =>
        StreamPreparation.RunWorker(() =>
        {
            owner.Initialize(bundle);
            return owner;
        });
}
