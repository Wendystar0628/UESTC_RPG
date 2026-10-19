using Campus.Gen.World;
using Campus.Pipeline.Map;
using System.Numerics;
using System.Text.Json.Serialization.Metadata;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Save;
using Campus.Contracts.Startup;

namespace Campus.Kernel.Maps;

/// <summary>
/// Mainline scope: loads the current map once at startup (Boot) and re-places the pose on that same
/// map for NewGame/LoadSave. Switching between maps is a later runtime and is rejected here.
/// </summary>
public interface IMapFrame : IDisposable
{
    void Step();
}

[KernelService(typeof(IMapFrame), Owner = "WP-46")]
[KernelService(typeof(IMapService), Owner = "WP-46")]
[SaveSection("maps", Owner = "WP-46", Version = 1, Phase = RestorePhase.World, Order = 0)]
public sealed class MapService : IMapService, IMapFrame, ISaveSection<MapsSave>, IBootParticipant, IDisposable
{
    private readonly ICameraRig _camera;
    private readonly IViewControl _view;
    private readonly IMapRuntime _runtime;
    private readonly IMapLifetime _lifetime;
    private Task? _mount;
    private IMapArrival? _arrival;
    private readonly IWorldModelLoader _loader;
    private readonly ICameraPoseValidator _poses;
    private readonly IWalkController? _walk;
    private readonly ITerrainPreparation? _terrain;
    private Task<Pose>? _legalize;
    private readonly MapDataSource _data;
    private readonly Dictionary<MapId, MapVisit> _visits = [];
    private MapCatalog? _catalog;
    private Task<MapCatalog>? _catalogTask;
    private TaskCompletionSource? _initialization;
    private PendingMapLoad? _load;
    private Task<ModeSwitchResult>? _modeChange;
    private MapTransition? _active;
    private MapTravelRequest? _request;
    private MapCatalogEntry? _target;
    private IMapContext? _prepared;
    private IDisposable? _input;
    private Pose _destination;
    private ViewMode _destinationMode;
    private WaypointStand? _stand;
    private bool _started;
    private bool _sameMap;
    private bool _spawnStarted;
    private bool _modeStarted;
    private bool _preservePresentationPose;
    private bool _stopped;
    private MapId _savedCurrent;

    public MapService(ICameraRig camera, IViewControl view, IMapRuntime runtime, IMapLifetime lifetime,
        IWorldModelLoader loader, ICameraPoseValidator poses, MapDataSource? data = null, IWalkController? walk = null,
        ITerrainPreparation? terrain = null)
    {
        _camera = camera; _view = view; _runtime = runtime; _lifetime = lifetime;
        _loader = loader; _data = data ?? new();
        _poses = poses ?? throw new ArgumentNullException(nameof(poses));
        _walk = walk;
        _terrain = terrain;
    }

    public long Rev { get; private set; }
    public MapId Default => (_catalog ?? throw new InvalidOperationException("Map catalogue is not initialized.")).Default;
    public IReadOnlyList<MapDescriptor> Maps => _catalog?.Maps ?? [];
    public MapId Current { get; private set; }
    public IMapContext? Context { get; private set; }
    public IMapTransition? Transition => _active;
    public IReadOnlyList<MapVisit> Visits => SnapshotVisits();
    public event Action<MapId, MapTravelCause>? Arrived;
    public JsonTypeInfo<MapsSave> TypeInfo => MapsSaveJsonContext.Contract.MapsSave;
    public string Id => "map-catalog";
    public int Order => -100;

    /// <summary>Resolves an authored spawn or waypoint without loading, mounting or changing a map.</summary>
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

    /// <summary>Only MapServiceNode calls this, at FramePriority.Map. No worker continuation mutates map state.</summary>
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
        catch (Exception error) { Fail(error); }
    }

    public IMapTransition Travel(MapTravelRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (_stopped || !ValidateRequest(request)) return Completed(request, MapTravelResult.Rejected);
        if (_active is { } active) return active.To == request.Target ? active : Completed(request, MapTravelResult.Rejected);
        var transition = new MapTransition(Current, request);
        Begin(transition);
        return transition;
    }

    private bool ValidateRequest(MapTravelRequest request)
    {
        if (_catalog is null || !_catalog.TryGet(request.Target, out var target)) return false;
        if (target.Descriptor.Kind != MapKind.Outdoor || target.Descriptor.World.Time != MapTimeRule.Shared) return false;
        var valid = request.Cause switch
        {
            MapTravelCause.Boot => Context is null,
            MapTravelCause.NewGame or MapTravelCause.LoadSave => Context is not null && request.Target == Current,
            _ => false
        };
        if (!valid) return false;
        if (request.Mode is { } mode) ValidateMode(mode);
        try
        {
            if (request.Pose is { } pose) MapCatalog.ValidatePose(pose);
            else if (request.Spawn is { } spawn) _ = target.ResolveSpawn(spawn);
        }
        catch (Exception error) when (error is ArgumentException or InvalidDataException) { return false; }
        return true;
    }

    private void Begin(MapTransition transition)
    {
        _active = transition;
        _request = transition.Request;
        _catalog!.TryGet(transition.To, out _target!);
        ClearPendingLoad();
        _sameMap = transition.Cause != MapTravelCause.Boot;
        _input ??= _camera.LockInput(InputLockReason.MapTravel);
        _camera.ActiveTransit?.Cancel();
        Move(_sameMap ? MapStage.Mount : MapStage.LoadData);
    }

    private void Advance()
    {
        switch (_active!.Stage)
        {
            case MapStage.LoadData:
                if (!_started)
                {
                    var input = _data.ReadLoadInput(_target!);
                    var claims = _data.ReadClaims(_target!.Descriptor.Id);
                    _load = new(input, Task.Run(() => MapWorldLoading.LoadPrepared(_loader, input, claims)));
                    _started = true;
                }
                var operation = _load ?? throw new InvalidOperationException("Map load operation is missing.");
                if (!operation.Work.IsCompleted) return;
                var loaded = operation.Work.GetAwaiter().GetResult();
                _prepared = MapWorldLoading.CreateContext(operation.Input.Entry, loaded, operation.Input.SourceInputs);
                _load = null;
                PrepareDestination();
                Move(MapStage.Mount);
                break;
            case MapStage.Mount:
                if (!_started)
                {
                    if (_sameMap) PrepareDestination(); // The mounted map stays; only the pose is re-placed.
                    else
                    {
                        Context = _prepared ?? throw new InvalidOperationException("Map world was not prepared.");
                        _prepared = null;
                        _data.WriteMapRect(Context.Descriptor.DataRect);
                        _mount = _lifetime.Mount(Context);
                    }
                    _started = true;
                }
                if (_mount is { } mounting)
                {
                    if (!mounting.IsCompleted) return;
                    mounting.GetAwaiter().GetResult();
                    _mount = null;
                }
                _legalize ??= PrepareDestinationTerrain();
                if (!_legalize.IsCompleted) return;
                LegalizeDestination(_legalize.GetAwaiter().GetResult());
                _camera.SetPose(_destination);
                _arrival = _runtime.Arrive(_destination, _destinationMode);
                Move(MapStage.Warm);
                break;
            case MapStage.Warm:
                var arrival = _arrival ?? throw new InvalidOperationException("Map arrival is missing.");
                if (!arrival.Done.IsCompleted) return;
                arrival.Done.GetAwaiter().GetResult();
                if (!ReferenceEquals(_runtime.Arrival, arrival))
                    throw new InvalidOperationException("Map arrival was replaced before spawn.");
                Move(MapStage.Spawn);
                break;
            case MapStage.Spawn:
                if (!ReferenceEquals(_runtime.Arrival, _arrival))
                    throw new InvalidOperationException("Map arrival was replaced during spawn.");
                if (!_spawnStarted) { _camera.SetPose(_destination); _spawnStarted = true; }
                if (!_modeStarted)
                {
                    _modeChange = _view.SetMode(_destinationMode, ModeSwitchVia.Ui);
                    _modeStarted = true;
                }
                if (!_modeChange!.IsCompleted) return;
                switch (_modeChange.GetAwaiter().GetResult())
                {
                    case ModeSwitchResult.Ok: break;
                    case ModeSwitchResult.Relocated: _stand = null; break;
                    case ModeSwitchResult.Refused: throw new InvalidOperationException("Destination observer mode was refused.");
                    default: throw new InvalidOperationException("Unknown mode-switch result.");
                }
                _modeChange = null;
                if (_stand is { } stand)
                {
                    if (_walk is null) throw new InvalidOperationException("Walking spawn requires the walk controller.");
                    _walk.Warp(new(stand.Feet, stand.YawDeg, 0));
                }
                Current = _target!.Descriptor.Id;
                _savedCurrent = Current;
                Rev++;
                if (_request!.Cause != MapTravelCause.LoadSave || _request.Pose is not null) RememberCurrent();
                Finish(MapTravelResult.Arrived);
                break;
            default:
                throw new InvalidOperationException("Map transition is in an unsupported stage: " + _active.Stage);
        }
    }

    private void PrepareDestination()
    {
        var request = _request!;
        _destinationMode = request.Mode ?? _camera.Mode;
        _stand = null;
        _preservePresentationPose = false;
        MapSpawn? spawn = null;
        if (request.Pose is { } pose) _destination = pose;
        else if (request.Spawn is { } spawnId) { spawn = _target!.ResolveSpawn(spawnId); _destination = spawn.Fly; }
        else if (_sameMap)
        {
            _destination = _camera.Pose;
            _preservePresentationPose = true; // Preview/Title T0 is intentionally allowed outside the roam volume.
        }
        else if (_visits.TryGetValue(request.Target, out var visit))
        { _destination = visit.LastPose; _destinationMode = request.Mode ?? visit.Mode; }
        else { spawn = _target!.ResolveSpawn(_target.Descriptor.DefaultSpawn); _destination = spawn.Fly; }
        if (spawn is not null)
        {
            switch (_destinationMode)
            {
                case ViewMode.Fly: break;
                case ViewMode.Walk:
                    if (spawn.Stand is { } stand)
                    { _stand = stand; _destination = new(stand.Feet + new Vector3(0, 1.62f, 0), stand.YawDeg, 0); }
                    else _destinationMode = ViewMode.Fly;
                    break;
                default: throw new InvalidDataException("Invalid observer mode.");
            }
        }
        ValidateMode(_destinationMode);
        MapCatalog.ValidatePose(_destination);
    }

    private async Task<Pose> PrepareDestinationTerrain()
    {
        var pose = _preservePresentationPose ? _destination : await _poses.LegalizePrepared(_destination);
        if (_terrain is not null && Context is { } context)
        {
            var point = new Vector2(pose.Position.X, pose.Position.Z);
            var reach = _destinationMode == ViewMode.Walk ? WalkRules.ArrivalCollisionRadius : 0f;
            await _terrain.PrepareRegion(context, new(point - new Vector2(reach), point + new Vector2(reach)));
        }
        return pose;
    }

    private void LegalizeDestination(Pose legalized)
    {
        if (_preservePresentationPose) return;
        var original = _destination;
        MapCatalog.ValidatePose(legalized);
        if (!_poses.IsLegal(legalized, outsideBuffer: false))
            throw new InvalidDataException("Map destination remains outside the reachable space after legalization.");
        _destination = legalized;
        // A projected eye position no longer names the authored feet location. Let the walk service
        // choose a legal support there; never warp back through the boundary to the original Stand.
        if (_stand is not null && legalized.Position != original.Position) _stand = null;
    }

    /// <summary>A failed startup load is terminal: the caller presents the failure; there is no fallback map.</summary>
    private void Fail(Exception error)
    {
        _data.ReportError($"Map load failed [stage={_active?.Stage}, cause={_active?.Cause}, " +
            $"target={_request?.Target.Value ?? "-"}]:\n{error}");
        Finish(MapTravelResult.Failed);
    }

    private void Finish(MapTravelResult result)
    {
        var finished = _active!;
        _active = null;
        _request = null;
        _target = null;
        ClearPendingLoad();
        _modeChange = null;
        _legalize = null;
        _mount = null;
        if (result != MapTravelResult.Arrived) _arrival?.Cancel();
        _arrival = null;
        _input?.Dispose();
        _input = null;
        if (result == MapTravelResult.Arrived) PublishArrival(finished.Cause);
        finished.Complete(result);
    }

    private void PublishArrival(MapTravelCause cause)
    {
        if (Arrived is not { } handlers) return;
        foreach (Action<MapId, MapTravelCause> handler in handlers.GetInvocationList())
        {
            try { handler(Current, cause); }
            catch (Exception error) { _data.ReportError("Map arrival subscriber failed: " + error.Message); }
        }
    }

    private void Move(MapStage stage)
    {
        _active!.MoveTo(stage);
        _started = false;
        _spawnStarted = false;
        _modeStarted = false;
    }

    public void Dispose()
    {
        if (_stopped) return;
        _stopped = true;
        var active = _active;
        _active = null; _request = null; _target = null;
        _catalogTask = null; _modeChange = null; _mount = null;
        _arrival?.Cancel(); _arrival = null;
        ClearPendingLoad();
        Context = null;
        _input?.Dispose(); _input = null;
        _initialization?.TrySetCanceled();
        active?.Complete(MapTravelResult.Failed);
    }

    private void ClearPendingLoad()
    {
        // A worker owns its immutable input through its closure. Dropping this operation prevents
        // late completion from publishing anywhere; no worker continuation mutates service state.
        _load = null;
        _prepared = null;
    }

    private sealed record PendingMapLoad(MapLoadInput Input, Task<WorldModelLoadResult> Work);

    private IMapTransition Completed(MapTravelRequest request, MapTravelResult result)
    { var transition = new MapTransition(Current, request); transition.Complete(result); return transition; }

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
