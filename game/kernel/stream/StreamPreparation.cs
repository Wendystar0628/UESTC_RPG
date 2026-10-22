using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Kernel.Bake;
using Campus.Kernel.Lod;
using Godot;
using TileGroup = Campus.Kernel.Stream.StreamResidency.TileGroup;
using ParticipantTile = Campus.Kernel.Stream.StreamParticipants.ParticipantTile;

namespace Campus.Kernel.Stream;

internal sealed class StreamPreparation(StreamDirector host, IStreamGeometryPreparation geometry)
{
    private readonly StreamDirector _host = host;
    private readonly IStreamGeometryPreparation _geometry = geometry;
    private readonly StreamRequestQueue _requests = new();
    private readonly List<Pending> _pending = [];
    private readonly List<Preparing> _preparing = [];
    private readonly HashSet<BakeKey> _issued = [];
    private ModuleTileCatalog? _catalog;
    private Task<ModuleTileCatalog>? _catalogTask;
    private long _catalogGeneration;
    internal ModuleTileCatalog? Catalog => _catalog;
    internal int Preparations => _preparing.Count;
    internal bool IsIdle => _pending.Count == 0 && _preparing.Count == 0 && _catalogTask is null;
    internal bool IsIssued(BakeKey key) => _issued.Contains(key);
    internal void Forget(BakeKey key) => _issued.Remove(key);
    internal void ClearRequests() => _requests.Clear();
    internal void ReplaceRequests(IEnumerable<StreamRequest> requests) => _requests.Replace(requests);

    internal void HarvestCatalog()
    {
        if (_catalogTask is not { IsCompleted: true } task) return;
        _catalogTask = null;
        try
        {
            var catalog = task.GetAwaiter().GetResult();
            if (_host.Context is null || _catalogGeneration != _host.MapGeneration) return;
            _host.Participants.LoadParticipantEntries(catalog); _catalog = catalog;
        }
        catch (OperationCanceledException) when (task.IsCanceled) { }
        catch (Exception error) { _host.Fail(StreamTile.Root, error); }
    }

    internal void PollHandles(bool background)
    {
        for (var i = 0; i < _pending.Count;)
        {
            var pending = _pending[i];
            var priority = pending.Participant is { } demanded ? StreamParticipants.ParticipantPriority(demanded) :
                pending.Group.Priority;
            if ((priority == BakePriority.Background) != background) { i++; continue; }
            if (pending.Handle.State is BakeState.Pending or BakeState.Running) { i++; continue; }
            if (pending.Handle.Failure is { } failure and not OperationCanceledException) _host.RecordFailure(failure);
            if (!_host.Residency.IsCurrent(pending)) { _pending.RemoveAt(i); continue; }
            if (pending.Handle.State == BakeState.Failed)
            {
                if (pending.Handle.Failure is { } error and not OperationCanceledException) _host.Fail(pending.Request.Tile, error, pending.Participant);
                _pending.RemoveAt(i); continue;
            }
            if (pending.Handle.State != BakeState.Ready) throw new InvalidDataException("Unknown bake state.");
            if (pending.Participant is { } participant)
            {
                _host.Participants.MountParticipant(pending, participant);
                _pending.RemoveAt(i); continue;
            }
            if (_preparing.Count + (_host.Interiors?.Preparations ?? 0) >= _host.Selection.Settings.MaximumPreparations) { i++; continue; }
            var bundle = pending.Handle.Tile;
            if (bundle is null || bundle.Key != pending.Request.Key || bundle.Tile != pending.Request.Tile.Tile || bundle.Level != pending.Request.Tile.Level)
            {
                _host.Fail(pending.Request.Tile, new InvalidDataException($"Bake returned the wrong product for {pending.Request.Key}."));
                _pending.RemoveAt(i); continue;
            }
            var owner = new PreparedTile(bundle);
            _preparing.Add(new(pending.Request.Tile, pending.Revision, owner, Initialize(owner, bundle), pending.Group, pending.GeometryRevision));
            _pending.RemoveAt(i);
        }
    }

    // Capture synchronous provider faults on the same real initialization task while retaining its owner.
    private async Task Initialize(PreparedTile owner, TileBundle bundle) => await _geometry.PrepareAsync(owner, bundle);

    private static async Task<ModuleTileCatalog> BuildCatalog(Task<PreparedBakeTiles> tiles)
    {
        var index = await tiles.ConfigureAwait(false);
        return await RunWorker(() => ModuleTileCatalog.Build(index)).ConfigureAwait(false);
    }

    internal static Task<T> RunWorker<T>(Func<T> work) => Task.Factory.StartNew(() =>
    {
        Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;
        return work();
    }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);

    internal void IssueQueued()
    {
        // Ensure only queues work. Bake owns CPU concurrency and the three ResourceLoader slots.
        for (var sent = 0; sent < 64 && _requests.TryDequeue(out var request); sent++)
        {
            if (!_issued.Add(request.Key)) continue;
            var handle = _host.Bake.Ensure(request.Key, request.Priority);
            var group = _host.Residency.Groups[request.Tile];
            group.Participants.TryGetValue(request.Key, out var participant);
            _pending.Add(new(request, handle, _host.Mount.Revision, group, group.GeometryRevision, participant));
        }
    }

    internal void FinishMountFrame()
    {
        // This method is called only inside the 900 budget accounting, including stale-result disposal.
        for (var i = _preparing.Count - 1; i >= 0; i--)
        {
            var preparing = _preparing[i];
            if (preparing.Failed || !preparing.Task.IsCompleted) continue;
            if (!preparing.Task.IsCompletedSuccessfully)
            {
                try { preparing.Task.GetAwaiter().GetResult(); }
                catch (OperationCanceledException) when (preparing.Task.IsCanceled) { }
                catch (Exception error) { _host.Fail(preparing.Tile, error); }
                // Failed initialization may also have failed cleanup. Keep its owner until explicit drain.
                preparing.Failed = true;
                continue;
            }
            var prepared = preparing.Owner;
            if (!_host.Residency.IsCurrentGeometry(preparing.Tile, preparing.Group, preparing.Revision, preparing.GeometryRevision))
            {
                _host.Residency.QueueRetirement(prepared); _preparing.RemoveAt(i); continue;
            }
            var group = _host.Residency.Groups[preparing.Tile]; group.Bundles.Add(prepared);
            _preparing.RemoveAt(i);
            if (group.Bundles.Count == group.Expected && !group.Queued)
            {
                group.Queued = true;
                var key = group.MountKey ?? throw new InvalidOperationException("Geometry mount has no source request identity.");
                group.Work = new VisualMountWork(key, group.Bundles, _host.Mount.Revision, _host.GetViewport().GetWorld3D().Scenario,
                    group.Priority, _host.Residency.Materials, _host.Reflections, () => group.Mounted = true, () => group.Mounted = false,
                    reason => _host.Fail(group.Tile, reason));
                _host.Mount.Enqueue(group.Work);
            }
        }
    }

    internal void LoadCatalog(Task<PreparedBakeTiles> tiles)
    {
        _catalogGeneration = _host.MapGeneration;
        _catalogTask = BuildCatalog(tiles);
    }
    internal void Unload()
    {
        _catalog = null; _requests.Clear(); _issued.Clear();
        // CPU and GPU tasks keep their input ownership until terminal and are harvested normally.
    }
    internal async Task DrainAsync()
    {
        if (_catalogTask is { } catalog)
        {
            try { await catalog; }
            catch (OperationCanceledException) when (catalog.IsCanceled) { }
            catch (Exception error) { _host.RecordFailure(error); }
            finally { _catalogTask = null; }
        }
        foreach (var pending in _pending)
            try { await pending.Handle.Done; }
            catch (OperationCanceledException) when (pending.Handle.Done.IsCanceled) { }
            catch (Exception error) { _host.RecordFailure(error); }
        _pending.Clear();
        for (var i = _preparing.Count - 1; i >= 0; i--)
        {
            var preparing = _preparing[i];
            var prepared = preparing.Owner;
            try { await preparing.Task; }
            catch (OperationCanceledException) when (preparing.Task.IsCanceled) { }
            catch (Exception error) { _host.RecordFailure(error); }
            try { prepared.Dispose(); }
            catch (Exception error) { _host.RecordFailure(error); }
            if (prepared.IsDisposed) _preparing.RemoveAt(i);
        }
    }
    internal sealed record Pending(StreamRequest Request, IBakeHandle Handle, long Revision, TileGroup Group,
        long GeometryRevision, ParticipantTile? Participant);
    private sealed record Preparing(StreamTile Tile, long Revision, PreparedTile Owner, Task Task, TileGroup Group, long GeometryRevision)
    { public bool Failed { get; set; } }

}
