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
    private PendingMapLoad? _load;
    private Task? _mount;
    private IDisposable? _input;
    private bool _stopped;
    private MapId _savedCurrent;

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

    private static async Task AwaitPreparation(MapTransition transition)
    {
        if (await transition.Done != MapTravelResult.Arrived)
            throw transition.Failure ?? new OperationCanceledException("Map preparation stopped.");
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
                    var input = _data.ReadLoadInput(_target!);
                    var claims = _data.ReadClaims(_target!.Descriptor.Id);
                    _load = new(input, Task.Run(() => MapWorldLoading.LoadPrepared(_loader, input, claims)));
                }
                if (!_load.Work.IsCompleted) return;
                var loaded = _load.Work.GetAwaiter().GetResult();
                Context = MapWorldLoading.CreateContext(_load.Input.Entry, loaded, _load.Input.SourceInputs);
                _load = null;
                _data.WriteMapRect(Context.Descriptor.DataRect);
                _active.MoveTo(MapStage.Mount);
                _mount = _lifetime.Mount(Context);
                break;
            case MapStage.Mount:
                if (_mount is not { IsCompleted: true }) return;
                _mount.GetAwaiter().GetResult();
                Current = _target!.Descriptor.Id;
                _savedCurrent = Current;
                Rev++;
                Finish(MapTravelResult.Arrived);
                break;
            default: throw new InvalidOperationException("Unsupported map preparation stage.");
        }
    }

    private void Finish(MapTravelResult result, [FailureHandler("SYS-MAPS")] Exception? failure = null)
    {
        var finished = _active!;
        _active = null; _target = null; _load = null; _mount = null;
        var input = _input; _input = null;
        try { input?.Dispose(); }
        catch (Exception error) { failure = Campus.Kernel.Diagnostics.RetainedFailure.Append(failure, error); }
        finished.Complete(failure is null ? result : MapTravelResult.Failed, failure);
    }

    public void Dispose()
    {
        if (_stopped) return;
        _stopped = true;
        if (_active is not null) Finish(MapTravelResult.Failed);
        _catalogTask = null; Context = null;
        _initialization?.TrySetCanceled();
    }

    private sealed record PendingMapLoad(MapLoadInput Input, Task<WorldModelLoadResult> Work);

    private void RememberCurrent()
    {
        if (Context is null || string.IsNullOrEmpty(Current.Value)) return;
        _visits[Current] = new(Current, _camera.Pose with { FovDeg = null }, _camera.Mode);
    }

    private IReadOnlyList<MapVisit> SnapshotVisits() => Array.AsReadOnly(_visits.Values
        .OrderBy(v => v.Map.Value, StringComparer.Ordinal).ToArray());

    public MapsSave Capture(in CaptureContext context)
    {
        if (_active is null) RememberCurrent();
        return new(_active is null ? Current : _savedCurrent, SnapshotVisits());
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
        _savedCurrent = dto.Current;
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
