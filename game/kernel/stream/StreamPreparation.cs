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

    internal void PollHandles()
    {
        for (var i = 0; i < _pending.Count;)
        {
            var pending = _pending[i];
            if (pending.Handle.State is BakeState.Pending or BakeState.Running) { i++; continue; }
            if (pending.Handle.Failure is { } failure and not OperationCanceledException) _host.RecordFailure(failure);
            if (pending.Request.PreloadOnly)
            {
                if (pending.Pinned && _host.Context?.Descriptor.Id == pending.Request.Key.Map && pending.Revision == _host.Mount.Revision)
                { i++; continue; }
                if (_host.Context?.Descriptor.Id == pending.Request.Key.Map && pending.Revision == _host.Mount.Revision)
                    _issued.Remove(pending.Request.Key);
                _pending.RemoveAt(i); continue;
            }
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
            _preparing.Add(new(pending.Request.Tile, pending.Revision, _geometry.PrepareAsync(bundle), pending.Group!, pending.GeometryRevision));
            _pending.RemoveAt(i);
        }
    }

    internal void QueueCpuRead(BakeKey key, StreamTile tile, List<StreamRequest> requests)
    {
        if (_issued.Contains(key))
        {
            foreach (var pending in _pending)
                if (pending.Request.PreloadOnly && pending.Request.Key == key && pending.Revision == _host.Mount.Revision)
                    pending.Pinned = true;
            _host.Bake.Ensure(key, BakePriority.KnownDestination); return;
        }
        for (var i = 0; i < requests.Count; i++)
            if (requests[i].Key == key)
            {
                requests[i] = requests[i] with { Priority = BakePriority.KnownDestination };
                return;
            }
        requests.Add(new(key, tile, BakePriority.KnownDestination, 0) { PreloadOnly = true });
    }

    internal void PromotePreload(StreamRequest request, TileGroup group, ParticipantTile? participant)
    {
        for (var i = 0; i < _pending.Count; i++)
        {
            var pending = _pending[i];
            if (!pending.Request.PreloadOnly || pending.Request.Key != request.Key || pending.Revision != _host.Mount.Revision) continue;
            _pending[i] = pending with { Request = request, Group = group, GeometryRevision = group.GeometryRevision, Participant = participant, Pinned = false };
            return;
        }
    }

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
            if (request.PreloadOnly)
            {
                _pending.Add(new(request, handle, _host.Mount.Revision, null, 0, null) { Pinned = true });
                continue;
            }
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
            if (!preparing.Task.IsCompleted) continue;
            if (!preparing.Task.IsCompletedSuccessfully)
            {
                try { preparing.Task.GetAwaiter().GetResult(); }
                catch (OperationCanceledException) when (preparing.Task.IsCanceled) { }
                catch (Exception error) { _host.Fail(preparing.Tile, error); }
                _preparing.RemoveAt(i); continue;
            }
            var prepared = preparing.Task.Result;
            if (!_host.Residency.IsCurrentGeometry(preparing.Tile, preparing.Group, preparing.Revision, preparing.GeometryRevision))
            {
                _host.Residency.QueueRetirement(prepared); _preparing.RemoveAt(i); continue;
            }
            var group = _host.Residency.Groups[preparing.Tile]; group.Bundles.Add(prepared);
            _preparing.RemoveAt(i);
            if (group.Bundles.Count == group.Expected && !group.Queued)
            {
                group.Queued = true;
                group.Work = new VisualMountWork(group.Bundles, _host.Mount.Revision, _host.GetViewport().GetWorld3D().Scenario,
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
    internal void ClearPins()
    {
        foreach (var pending in _pending)
            if (pending.Request.PreloadOnly) pending.Pinned = false;
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
        foreach (var preparing in _preparing)
        {
            PreparedTile? prepared = null;
            try { prepared = await preparing.Task; }
            catch (OperationCanceledException) when (preparing.Task.IsCanceled) { }
            catch (Exception error) { _host.RecordFailure(error); }
            if (prepared is not null)
                try { prepared.Dispose(); }
                catch (Exception error) { _host.RecordFailure(error); }
        }
        _preparing.Clear();
    }
    internal sealed record Pending(StreamRequest Request, IBakeHandle Handle, long Revision, TileGroup? Group,
        long GeometryRevision, ParticipantTile? Participant)
    { public bool Pinned { get; set; } }
    private sealed record Preparing(StreamTile Tile, long Revision, Task<PreparedTile> Task, TileGroup Group, long GeometryRevision);

}
