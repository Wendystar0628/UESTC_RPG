using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Save;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Startup;
using Campus.Gen.World;
using Campus.Gen.BakeInput;
using System.Text.Json.Serialization.Metadata;

namespace Campus.Kernel.Maps;

public interface IMapFrame : IDisposable { void Step(); }

[KernelService(typeof(IMapFrame))]
[KernelService(typeof(IMapService))]
[SaveSection("maps", Version = 1, Phase = RestorePhase.World, Order = 0)]
public sealed class MapService(ICameraRig camera, IMapLifetime lifetime, IWorldModelLoader loader,
    MapDataSource data) : IMapService, IMapFrame, ISaveSection<MapsSave>, IBootParticipant, IDisposable
{
    private readonly ICameraRig _camera = camera;
    private readonly IMapLifetime _lifetime = lifetime;
    private readonly IWorldModelLoader _loader = loader;
    private readonly MapDataSource _data = data;
    private readonly Dictionary<MapId, MapVisit> _visits = [];
    private MapCatalog? _catalog;
    private Task<MapCatalog>? _catalogTask;
    private TaskCompletionSource? _initialization;
    private MapTransition? _active;
    private MapCatalogEntry? _target;
    private Task<PendingMapLoad>? _load;
    private Task? _mount;
    private IDisposable? _input;
    private readonly CancellationTokenSource _exit = new();
    private Task? _drain;
    private bool _stopped;
    private bool _disposed;

    public long Rev { get; private set; }
    public MapId Default => (_catalog ?? throw new InvalidOperationException("Map catalogue is not initialized.")).Default;
    public IReadOnlyList<MapDescriptor> Maps => _catalog?.Maps ?? [];
    public MapId Current { get; private set; }
    public IMapContext? Context { get; private set; }
    public IMapTransition? Transition => _active;
    public IReadOnlyList<MapVisit> Visits => SnapshotVisits();
    public JsonTypeInfo<MapsSave> TypeInfo => MapsSaveJsonContext.Contract.MapsSave;
    public string Id => "map-catalog";
    public int Order => -100;

    public MapSpawn ResolveSpawn(MapId map, string? spawn = null)
    {
        var catalog = _catalog ?? throw new InvalidOperationException("Map catalogue must be initialized before resolving a spawn.");
        if (!catalog.TryGet(map, out var entry)) throw new ArgumentException($"Unknown map: {map.Value}.", nameof(map));
        return entry.ResolveSpawn(spawn ?? entry.Descriptor.DefaultSpawn);
    }

    public Task Initialize(BootPhase phase, BootContext context) => phase switch
    {
        BootPhase.BeforeMap => Initialize(),
        BootPhase.AfterMapWarm or BootPhase.BeforeReveal => Task.CompletedTask,
        _ => throw new ArgumentOutOfRangeException(nameof(phase))
    };

    public Task Initialize()
    {
        ObjectDisposedException.ThrowIf(_stopped, this);
        if (_catalog is not null) return Task.CompletedTask;
        if (_initialization is not null) return _initialization.Task;
        _initialization = new();
        try { _catalogTask = _data.ReadCatalog(); }
        catch (Exception error) { _initialization.TrySetException(error); }
        return _initialization.Task;
    }

    public async Task Prepare(MapId map)
    {
        ObjectDisposedException.ThrowIf(_stopped, this);
        if (_active is { } pending)
        {
            if (pending.To != map) throw new InvalidOperationException("A different map is already being prepared.");
            await AwaitPreparation(pending);
            return;
        }
        if (Context is not null)
        {
            if (Current != map) throw new InvalidOperationException("A different map is already mounted.");
            return;
        }
        if (_catalog is null || !_catalog.TryGet(map, out var target) ||
            target.Descriptor.Kind != MapKind.Outdoor || target.Descriptor.World.Time != MapTimeRule.Shared)
            throw new InvalidOperationException("Map preparation was rejected.");
        _target = target;
        var transition = _active = new MapTransition(Current, map);
        _input = _camera.LockInput(InputLockReason.MapTravel);
        _camera.ActiveTransit?.Cancel();
        await AwaitPreparation(transition);
    }

    private async Task AwaitPreparation(MapTransition transition)
    {
        var cancellation = _exit.Token;
        if (await transition.Done != MapTravelResult.Arrived)
            throw transition.Failure ?? new OperationCanceledException("Map preparation stopped.", cancellation);
    }

    /// <summary>Only MapServiceNode advances map state, at FramePriority.Map.</summary>
    public void Step()
    {
        if (_stopped) return;
        if (_catalogTask is { IsCompleted: true } catalogTask)
        {
            _catalogTask = null;
            try { _catalog = catalogTask.GetAwaiter().GetResult(); _initialization!.TrySetResult(); }
            catch (Exception error) { _initialization!.TrySetException(error); }
        }
        if (_active is null) return;
        try { Advance(); }
        catch (Exception error)
        {
            _data.ReportError($"Map preparation failed [stage={_active?.Stage}, target={_target?.Descriptor.Id.Value ?? "-"}]:", error);
            Finish(MapTravelResult.Failed, error);
        }
    }

    private void Advance()
    {
        switch (_active!.Stage)
        {
            case MapStage.LoadData:
                if (_load is null)
                {
                    _load = LoadMap(_target!);
                }
                if (!_load.IsCompleted) return;
                var loaded = _load.GetAwaiter().GetResult();
                Context = MapWorldLoading.CreateContext(loaded.Input.Entry, loaded.World, loaded.Input.SourceInputs);
                _load = null;
                _data.WriteMapRect(Context.Descriptor.DataRect);
                _active.MoveTo(MapStage.Mount);
                _mount = _lifetime.Mount(Context);
                break;
            case MapStage.Mount:
                if (_mount is not { IsCompleted: true }) return;
                _mount.GetAwaiter().GetResult();
                Current = _target!.Descriptor.Id;
                Rev++;
                Finish(MapTravelResult.Arrived);
                break;
            default: throw new InvalidOperationException("Unsupported map preparation stage.");
        }
    }

    private Exception? Finish(MapTravelResult result, [FailureHandler("SYS-MAPS")] Exception? failure = null)
    {
        var finished = _active!;
        _active = null; _target = null; _load = null; _mount = null;
        var input = _input; _input = null;
        try { input?.Dispose(); }
        catch (Exception error) { failure = Campus.Kernel.Diagnostics.RetainedFailure.Append(failure, error); }
        finished.Complete(failure is null ? result : MapTravelResult.Failed, failure);
        return failure;
    }

    public void StopNewWork()
    {
        if (_stopped) return;
        _stopped = true;
        _exit.Cancel();
    }

    public Task DrainAsync() => _drain ??= Drain();

    private async Task Drain()
    {
        StopNewWork();
        Exception? failure = null;
        // Step cannot consume these tasks after StopNewWork. Keep every started producer owned until observed.
        foreach (var task in new Task?[] { _catalogTask, _load, _mount })
        {
            if (task is null) continue;
            try { await task; }
            catch (OperationCanceledException) when (_exit.IsCancellationRequested && task.IsCanceled) { }
            catch (Exception error)
            { failure = Campus.Kernel.Diagnostics.RetainedFailure.Append(failure, task.Exception ?? error); }
        }
        _catalogTask = null;
        if (_active is not null) failure = Finish(MapTravelResult.Failed, failure);
        if (failure is null) _initialization?.TrySetCanceled(_exit.Token);
        else _initialization?.TrySetException(failure);
        if (failure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
    }

    public void Dispose()
    {
        if (_disposed) return;
        StopNewWork();
        if ((_catalogTask is not null || _load is not null || _mount is not null || _active is not null) &&
            _drain?.IsCompleted != true)
            throw new InvalidOperationException("Map preparation must be drained before disposing its dependencies.");
        _disposed = true;
        Context = null;
        _exit.Dispose();
    }

    private async Task<PendingMapLoad> LoadMap(MapCatalogEntry entry)
    {
        var cancellation = _exit.Token;
        // Registry claims and input-source selection are captured before the first await.
        var claims = _data.ReadClaims(entry.Descriptor.Id);
        var input = await _data.ReadLoadInputAsync(entry).ConfigureAwait(false);
        cancellation.ThrowIfCancellationRequested();
        var world = await Task.Run(() => MapWorldLoading.LoadPrepared(_loader, input, claims), cancellation).ConfigureAwait(false);
        return new(input, world);
    }

    private sealed record PendingMapLoad(MapLoadInput Input, WorldModelLoadResult World);

    private IReadOnlyList<MapVisit> SnapshotVisits() => Array.AsReadOnly(_visits.Values
        .OrderBy(v => v.Map.Value, StringComparer.Ordinal).ToArray());

    public MapsSave Capture(in CaptureContext context)
    {
        var observer = context.Observer;
        var visit = new MapVisit(observer.Map, observer.View.Pose, observer.View.Mode);
        return new(observer.Map, _visits.Values.Where(v => v.Map != observer.Map).Append(visit)
            .OrderBy(v => v.Map.Value, StringComparer.Ordinal).ToArray());
    }

    public void Restore(MapsSave dto, RestoreContext context)
    {
        if (_catalog is null || !_catalog.TryGet(dto.Current, out _) || dto.Visits is null)
            throw new InvalidDataException("Saved current map is unknown.");
        var restored = new Dictionary<MapId, MapVisit>();
        foreach (var visit in dto.Visits)
        {
            if (!_catalog.TryGet(visit.Map, out _))
            {
                context.Repair("Removed visit for unavailable map: " + visit.Map.Value);
                continue;
            }
            MapCatalog.ValidatePose(visit.LastPose);
            ValidateMode(visit.Mode);
            if (!restored.TryAdd(visit.Map, visit)) throw new InvalidDataException("Duplicate saved map visit.");
        }
        if (!restored.ContainsKey(dto.Current)) throw new InvalidDataException("Saved current map has no visit.");
        _visits.Clear();
        foreach (var visit in restored.Values.OrderBy(v => v.Map.Value, StringComparer.Ordinal)) _visits.Add(visit.Map, visit);
    }

    public MapsSave CreateDefault(NewGameContext context)
    {
        if (_catalog is null || !_catalog.TryGet(context.Map, out var map)) throw new InvalidDataException("Unknown new-game map.");
        var spawn = map.ResolveSpawn(map.Descriptor.DefaultSpawn);
        var pose = spawn.ViewPose(context.Mode);
        return new(context.Map, Array.AsReadOnly(new[] { new MapVisit(context.Map, pose, context.Mode) }));
    }

    private static void ValidateMode(ViewMode mode)
    {
        _ = mode switch { ViewMode.Fly => true, ViewMode.Walk => true, _ => throw new InvalidDataException("Invalid observer mode.") };
    }
}
