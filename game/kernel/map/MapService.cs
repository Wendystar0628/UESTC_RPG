using Campus.Pipeline.Map;
using System.Numerics;
using System.Runtime;
using System.Text.Json.Serialization.Metadata;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Save;
using Campus.Contracts.Startup;

namespace Campus.Kernel.Maps;

[KernelService(typeof(IMapService), Owner = "WP-46")]
[SaveSection("maps", Owner = "WP-46", Version = 1, Phase = RestorePhase.World, Order = 0)]
public sealed class MapService : IMapService, ISaveSection<MapsSave>, IBootParticipant, IDisposable
{
    private readonly ICameraRig _camera;
    private readonly IViewControl _view;
    private readonly IWorldState _world;
    private readonly IWeatherProcess _weather;
    private readonly IVeilService _veil;
    private readonly IMapRuntime _runtime;
    private readonly IWorldModelLoader _loader;
    private readonly ICameraPoseValidator _poses;
    private readonly IWalkController? _walk;
    private readonly MapDataSource _data;
    private readonly Dictionary<MapId, MapVisit> _visits = [];
    private readonly HashSet<MapId> _attempted = [];
    private MapCatalog? _catalog;
    private Task<MapCatalog>? _catalogTask;
    private TaskCompletionSource? _initialization;
    private PendingMapLoad? _load;
    private Task? _operation;
    private Task<ModeSwitchResult>? _modeChange;
    private MapTransition? _active;
    private MapTransition? _queued;
    private MapTravelRequest? _request;
    private MapCatalogEntry? _target;
    private IMapContext? _prepared;
    private MapLoadSource? _preparedSource;
    private MapLoadSource? _currentSource;
    private MapLoadSource? _rollbackSource;
    private IDisposable? _input;
    private IDisposable? _readinessWait;
    private MapsSave? _stableSave;
    private MapId _previous;
    private Pose _destination;
    private ViewMode _destinationMode;
    private WaypointStand? _stand;
    private bool _started;
    private bool _sameMap;
    private bool _failed;
    private bool _warmStarted;
    private bool _spawnStarted;
    private bool _modeStarted;
    private bool _preservePresentationPose;
    private bool _ownsPresentation;
    private bool _stopped;
    private MapId _savedCurrent;

    public MapService(ICameraRig camera, IViewControl view, IWorldState world, IWeatherProcess weather,
        IVeilService veil, IMapRuntime runtime, IWorldModelLoader loader, ICameraPoseValidator poses, MapDataSource? data = null,
        IWalkController? walk = null)
    {
        _camera = camera; _view = view; _world = world; _weather = weather;
        _veil = veil; _runtime = runtime; _loader = loader; _data = data ?? new();
        _poses = poses ?? throw new ArgumentNullException(nameof(poses));
        _walk = walk;
    }

    public long Rev { get; private set; }
    public MapId Default => _catalog?.Default ?? default;
    public IReadOnlyList<MapDescriptor> Maps => _catalog?.Maps ?? [];
    public MapId Current { get; private set; }
    public IMapContext? Context { get; private set; }
    public IMapTransition? Transition => _active;
    public IReadOnlyList<MapVisit> Visits => SnapshotVisits();
    public event Action<MapId, MapTravelCause>? Arrived;
    public string? LastError { get; private set; }
    public bool IsSettled => _active is null && Context is not null;
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
        try
        {
            // This is only a protocol guard. The caller still owns fresh native covered-draw evidence.
            if (!_ownsPresentation && !HasOpaquePresentation())
                throw new InvalidOperationException("The caller-owned map presentation was released before travel completed.");
            Advance();
        }
        catch (Exception error) { Recover(error); }
    }

    public IMapTransition Travel(MapTravelRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (_stopped) return Completed(request, MapTravelResult.Rejected);
        if (!ValidateRequest(request)) return Completed(request, MapTravelResult.Rejected);
        if (_active is { } active)
        {
            // Without an explicit presentation handoff, a mixed queue could reveal another owner's
            // cinematic/boot cover. Reject before same-target coalescing and preserve the existing queue.
            if (OwnsPresentation(request.Cause) != _ownsPresentation)
                return Completed(request, MapTravelResult.Rejected);
            if (active.To == request.Target)
            {
                _queued?.Complete(MapTravelResult.Rejected);
                _queued = null;
                return active;
            }
            if (_queued?.To == request.Target) return _queued;
            _queued?.Complete(MapTravelResult.Rejected);
            _queued = new(Current, request);
            return _queued;
        }
        var transition = new MapTransition(Current, request);
        Begin(transition, false);
        return transition;
    }

    private bool ValidateRequest(MapTravelRequest request)
    {
        if (_catalog is null || !_catalog.TryGet(request.Target, out var target)) return false;
        var supported = (target.Descriptor.Kind, target.Descriptor.World.Time) switch
        {
            (MapKind.Outdoor, MapTimeRule.Shared) => true,
            (MapKind.Outdoor, MapTimeRule.Fixed) => false,
            (MapKind.Interior, _) or (MapKind.Instance, _) => false,
            _ => false
        };
        if (!supported) return false;
        if (!OwnsPresentation(request.Cause) && !HasOpaquePresentation()) return false;
        if (request.Mode is { } mode) ValidateMode(mode);
        try
        {
            if (request.Pose is { } pose) MapCatalog.ValidatePose(pose);
            else if (request.Spawn is { } spawn) _ = target.ResolveSpawn(spawn);
        }
        catch (Exception error) when (error is ArgumentException or InvalidDataException) { return false; }
        return request.Cause != MapTravelCause.Boot || Context is null && (_active is null || _active.To == request.Target);
    }

    private void Begin(MapTransition transition, bool covered)
    {
        _active = transition;
        _request = transition.Request;
        _ownsPresentation = OwnsPresentation(transition.Cause);
        _catalog!.TryGet(transition.To, out _target!);
        _previous = Current;
        _rollbackSource = _currentSource;
        ClearPendingLoad();
        _failed = false;
        LastError = null;
        _attempted.Clear();
        _attempted.Add(transition.To);
        _sameMap = Context is not null && transition.To == Current && (transition.Cause switch
        {
            MapTravelCause.NewGame or MapTravelCause.LoadSave => true,
            MapTravelCause.Boot or MapTravelCause.Ui or MapTravelCause.Portal or MapTravelCause.Teleport or MapTravelCause.Test => false,
            _ => throw new ArgumentOutOfRangeException(nameof(transition))
        });
        if (Context is not null && transition.Cause != MapTravelCause.LoadSave) _stableSave = CaptureCurrent();
        _input ??= _camera.LockInput(InputLockReason.MapTravel);
        _camera.ActiveTransit?.Cancel();
        Move(transition.Cause == MapTravelCause.Boot ? MapStage.LoadData :
            !_ownsPresentation || covered ? MapStage.Snapshot : MapStage.VeilIn);
    }

    private void Advance()
    {
        if (_ownsPresentation && _active!.Stage is MapStage.Snapshot or MapStage.Unload or MapStage.LoadData or MapStage.Mount or MapStage.Warm or MapStage.Spawn)
            ObserveReadiness();
        switch (_active!.Stage)
        {
            case MapStage.VeilIn:
                if (!_ownsPresentation) throw new InvalidOperationException("Borrowed map travel cannot acquire the presentation.");
                if (!_started) { _operation = _veil.Cover(VeilReason.MapTravel, .5f); _started = true; }
                if (!Finished(_operation)) return;
                Move(MapStage.Snapshot);
                break;
            case MapStage.Snapshot:
                if (_sameMap)
                {
                    PrepareDestination();
                    Move(MapStage.Mount);
                    _started = true; // The existing map stays mounted; still respect its resident-data barrier.
                    break;
                }
                if (!_failed && Context is not null && _request!.Cause != MapTravelCause.LoadSave) RememberCurrent();
                Move(MapStage.Unload);
                break;
            case MapStage.Unload:
                if (!_started)
                {
                    var from = Context?.Descriptor.Id ?? Current;
                    _runtime.Unload(from);
                    Context = null;
                    _currentSource = null;
                    ClearPendingLoad();
                    _started = true;
                }
                if (!_runtime.IsUnloaded) { ThrowRuntimeError(); return; }
                CollectUnloadedWorld();
                Move(MapStage.LoadData);
                break;
            case MapStage.LoadData:
                if (!_started)
                {
                    MapLoadSource source;
                    if (_failed && _rollbackSource is { } rollback && rollback.Input.Entry.Descriptor.Id == _target!.Descriptor.Id)
                        source = rollback; // Rebuild the previous world from its exact bytes, not a newer disk version.
                    else
                    {
                        var input = _data.ReadLoadInput(_target!);
                        source = new(input, _data.ReadClaims(_target!.Descriptor.Id));
                    }
                    _load = new(source, LoadSource(_data.PrepareLoadInput(source.Input), source.Claims));
                    _started = true;
                }
                var operation = _load ?? throw new InvalidOperationException("Map load operation is missing.");
                if (!operation.Work.IsCompleted) return;
                var (preparedInput, loaded) = operation.Work.GetAwaiter().GetResult();
                _prepared = MapWorldLoading.CreateContext(preparedInput.Entry, loaded, preparedInput.SourceInputs);
                _preparedSource = operation.Source with { Input = preparedInput };
                _load = null;
                PrepareDestination();
                Move(MapStage.Mount);
                break;
            case MapStage.Mount:
                if (!_started)
                {
                    Context = _prepared ?? throw new InvalidOperationException("Map world was not prepared.");
                    _currentSource = _preparedSource ?? throw new InvalidOperationException("Map source snapshot was not prepared.");
                    _prepared = null;
                    _preparedSource = null;
                    _data.WriteMapRect(Context.Descriptor.DataRect);
                    _runtime.Mount(Context);
                    _started = true;
                }
                ThrowRuntimeError();
                // Mount starts asynchronous data preparation. ResidentReady guarantees that all
                // preparation and MapAware callbacks (including the stream's map binding) completed.
                if (!_runtime.ResidentReady) return;
                LegalizeDestination();
                _runtime.Prefetch(_destination, _destinationMode);
                Move(MapStage.Warm);
                break;
            case MapStage.Warm:
                ThrowRuntimeError();
                if (!_warmStarted && (!_sameMap || !_runtime.DestinationReady))
                {
                    // Material readiness can itself depend on sample draws. Start warming before
                    // waiting for it, including a same-map restore after quality invalidated pipelines.
                    // An already-ready same-map destination keeps its zero-warm fast path.
                    _camera.SetPose(_destination); _runtime.BeginWarm(); _warmStarted = true;
                }
                if (!_runtime.ResidentReady || !_runtime.DestinationReady || (_warmStarted && !_runtime.WarmReady)) return;
                Move(MapStage.Spawn);
                break;
            case MapStage.Spawn:
                ThrowRuntimeError();
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
                if (_request!.Cause != MapTravelCause.LoadSave || _request.Pose is not null)
                { RememberCurrent(); _stableSave = CaptureCurrent(); }
                if (_queued is not null) { FinishAndChain(); return; }
                if (!_ownsPresentation) { Finish(); return; }
                Move(MapStage.VeilOut);
                break;
            case MapStage.VeilOut:
                if (!_ownsPresentation) throw new InvalidOperationException("Borrowed map travel cannot reveal the presentation.");
                if (!_started) { _operation = _veil.Reveal(VeilReason.MapTravel, 2.5f); _started = true; }
                if (Finished(_operation)) Finish();
                break;
            case MapStage.Done:
                throw new InvalidOperationException("Completed transition remained active.");
            default:
                throw new ArgumentOutOfRangeException(nameof(_active.Stage));
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
        else if (!_failed && (request.Cause == MapTravelCause.LoadSave || _sameMap && request.Cause == MapTravelCause.NewGame))
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

    private void LegalizeDestination()
    {
        if (_preservePresentationPose) return;
        var original = _destination;
        var legalized = _poses.Legalize(original);
        MapCatalog.ValidatePose(legalized);
        if (!_poses.IsLegal(legalized, outsideBuffer: false))
            throw new InvalidDataException("Map destination remains outside the reachable space after legalization.");
        _destination = legalized;
        // A projected eye position no longer names the authored feet location. Let the walk service
        // choose a legal support there; never warp back through the boundary to the original Stand.
        if (_stand is not null && legalized.Position != original.Position) _stand = null;
    }

    private void Recover(Exception error)
    {
        ClearPendingLoad();
        LastError = LastError is null ? error.Message : LastError + " | " + error.Message;
        // Keep the public summary concise; ReportError is the internal engine diagnostic sink.
        _data.ReportError($"Map travel failed [stage={_active?.Stage}, cause={_active?.Cause}, " +
            $"from={Current.Value ?? "-"}, target={_request?.Target.Value ?? "-"}]:\n{error.ToString()}");
        _failed = true;
        if (!_ownsPresentation && !HasOpaquePresentation())
        {
            // Only the external owner may cover again. Do not rebuild a fallback map while visible.
            _queued?.Complete(MapTravelResult.Failed);
            _queued = null;
            Finish(false);
            return;
        }
        var fallback = new[] { _previous, Default }.FirstOrDefault(id =>
            !string.IsNullOrEmpty(id.Value) && !_attempted.Contains(id) && _catalog!.TryGet(id, out _));
        if (string.IsNullOrEmpty(fallback.Value))
        {
            // Keep the veil opaque. The caller presents the finite startup/travel failure state.
            _queued?.Complete(MapTravelResult.Failed);
            _queued = null;
            Finish(false);
            return;
        }
        _attempted.Add(fallback);
        _catalog!.TryGet(fallback, out _target!);
        _request = new(fallback, Cause: _active!.Cause);
        _sameMap = false;
        Move(_ownsPresentation ? MapStage.VeilIn : MapStage.Unload);
    }

    private void FinishAndChain(bool covered = true)
    {
        StopReadinessObservation();
        var finished = _active!;
        var result = _failed ? MapTravelResult.Failed : MapTravelResult.Arrived;
        var next = _queued!;
        _queued = null;
        _active = null;
        Begin(next, covered);
        if (result == MapTravelResult.Arrived) PublishArrival(finished.Cause);
        finished.Complete(result);
    }

    private void Finish(bool reached = true)
    {
        StopReadinessObservation();
        if (reached && _queued is not null) { FinishAndChain(false); return; }
        var finished = _active!;
        var result = reached && !_failed ? MapTravelResult.Arrived : MapTravelResult.Failed;
        _active = null;
        _request = null;
        _target = null;
        ClearPendingLoad();
        _rollbackSource = null;
        _operation = null;
        _modeChange = null;
        _input?.Dispose();
        _input = null;
        _ownsPresentation = false;
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
        if (stage == MapStage.VeilOut) StopReadinessObservation();
        _active!.MoveTo(stage);
        _started = false;
        _operation = null;
        _warmStarted = false;
        _spawnStarted = false;
        _modeStarted = false;
    }

    private void ObserveReadiness()
    {
        var observed = _active!.Cause switch
        {
            MapTravelCause.Ui or MapTravelCause.Portal or MapTravelCause.Test => true,
            // Boot and save/new-game transactions have caller-owned preparation observations.
            // Teleport has its own transition and never shows a waiting indicator.
            MapTravelCause.Boot or MapTravelCause.NewGame or MapTravelCause.LoadSave or MapTravelCause.Teleport => false,
            _ => throw new ArgumentOutOfRangeException(nameof(_active.Cause))
        };
        if (observed) _readinessWait ??= _veil.ObserveReadinessWait();
    }

    internal void StopReadinessObservation()
    {
        _readinessWait?.Dispose();
        _readinessWait = null;
    }

    public void Dispose()
    {
        if (_stopped) return;
        _stopped = true;
        var active = _active;
        var queued = _queued;
        _active = null; _queued = null; _request = null; _target = null;
        _catalogTask = null; _operation = null; _modeChange = null;
        ClearPendingLoad();
        Context = null; _currentSource = null; _rollbackSource = null;
        StopReadinessObservation();
        _input?.Dispose(); _input = null;
        _initialization?.TrySetCanceled();
        active?.Complete(MapTravelResult.Failed);
        queued?.Complete(MapTravelResult.Failed);
    }

    private void ClearPendingLoad()
    {
        // A worker owns its immutable input through its closure. Dropping this operation prevents
        // late completion from publishing anywhere; no worker continuation mutates service state.
        _load = null;
        _prepared = null;
        _preparedSource = null;
    }

    private sealed record MapLoadSource(MapLoadInput Input, Campus.Pipeline.Map.MapClaimSnapshot? Claims);
    private async Task<(MapLoadInput Input, WorldModelLoadResult Loaded)> LoadSource(Task<MapLoadInput> prepared,
        Campus.Pipeline.Map.MapClaimSnapshot? claims)
    {
        var input = await prepared;
        var loaded = await Task.Run(() => MapWorldLoading.LoadPrepared(_loader, input, claims));
        return (input, loaded);
    }

    private sealed record PendingMapLoad(MapLoadSource Source, Task<(MapLoadInput Input, WorldModelLoadResult Loaded)> Work);

    private static bool OwnsPresentation(MapTravelCause cause) => cause switch
    {
        MapTravelCause.Boot or MapTravelCause.NewGame or MapTravelCause.LoadSave => false,
        MapTravelCause.Ui or MapTravelCause.Portal or MapTravelCause.Teleport or MapTravelCause.Test => true,
        _ => throw new ArgumentOutOfRangeException(nameof(cause))
    };

    private bool HasOpaquePresentation()
    {
        var opacity = _veil.Opacity;
        return float.IsFinite(opacity) && opacity >= 1f;
    }

    private void ThrowRuntimeError()
    {
        if (_runtime.Error is { Length: > 0 } error) throw new InvalidOperationException(error);
    }

    private static bool Finished(Task? operation)
    {
        if (operation is null || !operation.IsCompleted) return false;
        operation.GetAwaiter().GetResult();
        return true;
    }

    private IMapTransition Completed(MapTravelRequest request, MapTravelResult result)
    { var transition = new MapTransition(Current, request); transition.Complete(result); return transition; }

    private void RememberCurrent()
    {
        if (Context is null || string.IsNullOrEmpty(Current.Value)) return;
        var epoch = _weather.Capture(Current).Epoch;
        _visits[Current] = new(Current, _camera.Pose, _camera.Mode, epoch, _world.WorldHours);
    }

    private MapsSave CaptureCurrent()
    {
        RememberCurrent();
        return new(Current, SnapshotVisits());
    }

    private IReadOnlyList<MapVisit> SnapshotVisits() => Array.AsReadOnly(_visits.Values
        .OrderBy(v => v.Map.Value, StringComparer.Ordinal).ToArray());

    public MapsSave Capture(in CaptureContext context)
    {
        if (_active is not null) return _stableSave ?? new(_savedCurrent, SnapshotVisits());
        return CaptureCurrent();
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
            if (!double.IsFinite(visit.LeftAtHours) || visit.LeftAtHours < 0)
                throw new InvalidDataException("Saved map visit is invalid.");
            MapCatalog.ValidatePose(visit.LastPose);
            ValidateMode(visit.Mode);
            if (!restored.TryAdd(visit.Map, visit)) throw new InvalidDataException("Duplicate saved map visit.");
        }
        if (!restored.ContainsKey(dto.Current)) throw new InvalidDataException("Saved current map has no visit.");
        _visits.Clear();
        foreach (var visit in restored.Values.OrderBy(v => v.Map.Value, StringComparer.Ordinal)) _visits.Add(visit.Map, visit);
        _savedCurrent = dto.Current;
        _stableSave = new(dto.Current, SnapshotVisits());
    }

    public MapsSave CreateDefault(NewGameContext context)
    {
        if (_catalog is null || !_catalog.TryGet(context.Map, out var map)) throw new InvalidDataException("Unknown new-game map.");
        var pose = map.ResolveSpawn(map.Descriptor.DefaultSpawn).Fly;
        return new(context.Map, Array.AsReadOnly(new[] { new MapVisit(context.Map, pose, ViewMode.Fly, 0, context.Start.Clock) }));
    }

    private static void ValidateMode(ViewMode mode)
    {
        _ = mode switch { ViewMode.Fly => true, ViewMode.Walk => true, _ => throw new InvalidDataException("Invalid observer mode.") };
    }

    private static void CollectUnloadedWorld()
    {
        var previous = GCSettings.LatencyMode;
        try { GCSettings.LatencyMode = GCLatencyMode.Interactive; GC.Collect(); }
        finally { GCSettings.LatencyMode = previous; }
    }
}
