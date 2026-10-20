using Campus.Gen.BakeInput;
using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Maps;
using static Campus.Kernel.Bake.BakeService;

namespace Campus.Kernel.Bake;

internal sealed class BakeOffline
{
    private readonly MapBuild _map;
    private readonly BakePipelineCatalog? _pipelines;
    private readonly BakeCatalog _catalog;
    private readonly BakeQueue _queue;

    internal BakeOffline(MapBuild map, BakePipelineCatalog? pipelines, BakeCatalog catalog, BakeQueue queue)
    { _map = map; _pipelines = pipelines; _catalog = catalog; _queue = queue; }

    internal async Task Run()
    {
        _map.PipelineStatistics = _pipelines;
        try
        {
            _queue.BeginCatalog();
            try { await Task.Run(() => _catalog.Queue(_map)); }
            finally { _queue.EndCatalog(); }

            while (true)
            {
                if (_queue.Failures.TryDequeue(out var failure)) throw new InvalidDataException(failure);
                var progress = _queue.Progress;
                if (progress.Done == progress.Total) break;
                await Task.Delay(50);
            }
            if (_queue.Failures.TryDequeue(out var lastFailure)) throw new InvalidDataException(lastFailure);
        }
        finally { _map.PipelineStatistics = null; }
    }
}
