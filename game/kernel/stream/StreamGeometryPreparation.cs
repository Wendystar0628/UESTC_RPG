using Campus.Contracts;
using Campus.Contracts.Bake;

namespace Campus.Kernel.Stream;

/// <summary>Prepares native tile storage off-thread; the caller owns the actual returned task through drain.</summary>
public interface IStreamGeometryPreparation
{
    Task<PreparedTile> PrepareAsync(TileBundle bundle);
}

[KernelService(typeof(IStreamGeometryPreparation))]
public sealed class StreamGeometryPreparation : IStreamGeometryPreparation
{
    public Task<PreparedTile> PrepareAsync(TileBundle bundle) => Task.Factory.StartNew(() =>
    {
        Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;
        return PreparedTile.Create(bundle);
    }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
}
