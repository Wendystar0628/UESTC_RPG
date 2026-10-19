using Campus.Contracts.Maps;

namespace Campus.Kernel.Bake;

public sealed partial class BakeService
{
    /// <summary>Explicit CPU batch, sharing the interactive completion catalogue.</summary>
    internal async Task BakeOffline(IMapContext context, BakePipelineCatalog? pipelines = null)
    {
        var map = Mount(context);
        map.PipelineStatistics = pipelines;
        try
        {
            _queue.BeginCatalog();
            try { await Task.Run(() => QueueCatalog(map)); }
            finally { map.EnumerationComplete = true; _queue.EndCatalog(); }

            while (true)
            {
                if (TryTakeError(out var failure)) throw new InvalidDataException(failure);
                var progress = Progress;
                if (progress.Done == progress.Total) break;
                await Task.Delay(50);
            }
            if (TryTakeError(out var lastFailure)) throw new InvalidDataException(lastFailure);
        }
        finally { map.PipelineStatistics = null; }
    }
}
