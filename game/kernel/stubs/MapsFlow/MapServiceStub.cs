using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Kernel.Maps;

namespace Campus.Kernel.Stubs;

[KernelService(typeof(IMapService), Owner = "WP-00", IsStub = true)]
public sealed class MapServiceStub(ICameraRig camera, IMapRuntime? runtime = null) : IMapService
{
    private readonly IMapRuntime _runtime = runtime ?? new MapRuntimeStub();
    private MapCatalogEntry? _catalogEntry;
    private IMapContext? _configuredContext;
    private MapTransitionStub? _transition;
    private Func<Task>? _nextFrame;
    private IViewControl? _viewControl;
    public long Rev { get; private set; }
    public MapId Default { get; private set; }
    public IReadOnlyList<MapDescriptor> Maps { get; private set; } = [];
    public IReadOnlyList<MapVisit> Visits { get; private set; } = [];
    public MapId Current { get; private set; }
    public IMapContext? Context { get; private set; }
    public IMapTransition? Transition => _transition;
    public string? LastError { get; private set; }
    public event Action<MapId, MapTravelCause>? Arrived;

    /// <summary>The D0 boot adapter provides a main-thread frame awaiter; immediate runtime stubs need no awaiter.</summary>
    public void ConfigureRuntime(Func<Task> nextFrame, IViewControl viewControl)
    {
        ArgumentNullException.ThrowIfNull(nextFrame);
        ArgumentNullException.ThrowIfNull(viewControl);
        _nextFrame = nextFrame;
        _viewControl = viewControl;
    }

    public void LoadFromBytes(ReadOnlySpan<byte> indexBytes, ReadOnlySpan<byte> mapBytes,
        IWorldModel world, GeoFrame geo, Vector2 center, Box3 bounds,
        TileKey tileMin, TileKey tileMax, string bakeRoot, IReadOnlyList<Waypoint> waypoints)
    {
        var index = JsonSerializer.Deserialize<MapIndex>(indexBytes, ContractJson.Options)
            ?? throw new JsonException("Map index is null.");
        var map = JsonSerializer.Deserialize<MapFile>(mapBytes, ContractJson.Options)
            ?? throw new JsonException("Map file is null.");
        if (index.Version != 1 || map.Version < 1 || index.Maps.Count != 1)
            throw new InvalidDataException("D0 requires one versioned map in the index.");
        var entry = index.Maps[0];
        if (entry.Id != map.Id || index.DefaultMap != map.Id)
            throw new InvalidDataException("Map file and default index entry do not match.");
        if (!entry.Dir.StartsWith("res://", StringComparison.Ordinal) || !entry.Dir.EndsWith('/'))
            throw new InvalidDataException("Map data root must be an absolute res:// directory.");
        if (!bakeRoot.StartsWith("res://", StringComparison.Ordinal) || !bakeRoot.EndsWith('/'))
            throw new InvalidDataException("Map bake root must be an absolute res:// directory.");
        ArgumentNullException.ThrowIfNull(world);
        var descriptor = map.Complete(geo, center, bounds, tileMin, tileMax);
        var catalogEntry = new MapCatalogEntry(descriptor, entry.Dir, bakeRoot,
            Array.AsReadOnly(waypoints.ToArray()));
        var pose = catalogEntry.ResolveSpawn(descriptor.DefaultSpawn).Fly;
        _catalogEntry = catalogEntry;
        Default = map.Id;
        Current = map.Id;
        Maps = Array.AsReadOnly(new[] { descriptor });
        _configuredContext = new MapContextStub(descriptor, world, entry.Dir, bakeRoot);
        Context = _configuredContext;
        camera.SetPose(pose);
        Visits = Array.AsReadOnly(new[] { new MapVisit(Current, pose, camera.Mode, 0, 0) });
        // This only prepares the single-map D0 catalog. Travel(Boot) owns runtime mounting and the one Arrived event.
    }

    public IMapTransition Travel(MapTravelRequest r)
    {
        var from = Current;
        if (_transition is not null || _configuredContext is not { } context || r.Target != context.Descriptor.Id)
            return new MapTransitionStub(from, r.Target, r.Cause, MapTravelResult.Rejected);
        if (!Enum.IsDefined(r.Cause) || (r.Mode is { } requestedMode && !Enum.IsDefined(requestedMode)))
            return new MapTransitionStub(from, r.Target, r.Cause, MapTravelResult.Rejected);
        Pose pose;
        try
        {
            pose = r.Pose ?? ResolveSpawn(r.Target, r.Spawn).Fly;
        }
        catch (ArgumentException)
        {
            return new MapTransitionStub(from, r.Target, r.Cause, MapTravelResult.Rejected);
        }
        var transition = new MapTransitionStub(from, r.Target, r.Cause);
        _transition = transition;
        LastError = null;
        _ = CompleteTravel(transition, context, pose, r.Mode ?? camera.Mode);
        return transition;
    }

    /// <summary>Publish the exact frozen D0 input version before any worker or map-aware node sees the context.</summary>
    public IMapContext SetSourceInputs(BuildInputSnapshot inputs)
    {
        if (_configuredContext is not MapContextStub context || _transition is not null)
            throw new InvalidOperationException("D0 inputs must be configured before travel.");
        _configuredContext = context with { SourceInputs = inputs };
        Context = _configuredContext;
        return _configuredContext;
    }

    private async Task CompleteTravel(MapTransitionStub transition, IMapContext context, Pose pose, ViewMode mode)
    {
        try
        {
            using var inputLock = camera.LockInput(InputLockReason.MapTravel);
            transition.Advance(MapStage.Unload);
            Context = null;
            _runtime.Unload(transition.From);
            await WaitFor(() => _runtime.IsUnloaded);

            transition.Advance(MapStage.Mount);
            Context = context;
            _runtime.Mount(context);
            await WaitFor(() => _runtime.ResidentReady);

            transition.Advance(MapStage.Warm);
            _runtime.Prefetch(pose, mode);
            camera.SetPose(pose);
            if (camera.Mode != mode)
            {
                if (_viewControl is null)
                    throw new InvalidOperationException("D0 map destination mode could not be applied.");
                var switched = await _viewControl.SetMode(mode, ModeSwitchVia.Ui);
                _ = switched switch
                {
                    ModeSwitchResult.Ok or ModeSwitchResult.Relocated => true,
                    ModeSwitchResult.Refused => throw new InvalidOperationException("D0 map destination mode was refused."),
                    _ => throw new ArgumentOutOfRangeException(nameof(switched))
                };
            }
            // Warm the destination's real view, not the old camera position.
            // DestinationReady may itself require warmed materials, so warming starts before the joint readiness wait.
            camera.SetPose(pose);
            _runtime.BeginWarm();
            await WaitFor(() => _runtime.ResidentReady && _runtime.DestinationReady && _runtime.WarmReady);

            transition.Advance(MapStage.Spawn);
            Current = context.Descriptor.Id;
            Visits = Array.AsReadOnly(new[] { new MapVisit(Current, camera.Pose, camera.Mode, 0, 0) });
            Rev++;
            _transition = null;
            Arrived?.Invoke(Current, transition.Cause);
            transition.Complete(MapTravelResult.Arrived);
        }
        catch (Exception error)
        {
            LastError = error.Message;
            Context = null;
            _transition = null;
            transition.Complete(MapTravelResult.Failed);
        }
    }

    private async Task WaitFor(Func<bool> ready)
    {
        while (true)
        {
            if (_runtime.Error is { Length: > 0 } error) throw new InvalidOperationException(error);
            if (ready()) return;
            if (_nextFrame is null) throw new InvalidOperationException("D0 runtime is asynchronous but no frame awaiter is configured.");
            await _nextFrame();
        }
    }

    public MapSpawn ResolveSpawn(MapId map, string? spawn = null)
    {
        var entry = _catalogEntry
            ?? throw new InvalidOperationException("The D0 map catalogue has not been initialized.");
        if (map != entry.Descriptor.Id)
            throw new ArgumentException($"Unknown map: {map.Value}.", nameof(map));
        return entry.ResolveSpawn(spawn ?? entry.Descriptor.DefaultSpawn);
    }

    // Kept for existing D0 callers; all lookups now share the initialized catalogue's resolution rules.
    public Pose ResolvePose(MapDescriptor map, string id) => ResolveSpawn(map.Id, id).Fly;
}

internal sealed record MapContextStub(MapDescriptor Descriptor, IWorldModel World,
    string DataRoot, string BakeRoot) : IMapContext
{
    public BuildInputSnapshot SourceInputs { get; init; } = BuildInputSnapshot.Empty;
}

internal sealed class MapTransitionStub : IMapTransition
{
    private readonly TaskCompletionSource<MapTravelResult> _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public MapTransitionStub(MapId from, MapId to, MapTravelCause cause, MapTravelResult? result = null)
    {
        From = from; To = to; Cause = cause;
        if (result is { } completed) Complete(completed);
    }
    public MapId From { get; }
    public MapId To { get; }
    public MapTravelCause Cause { get; }
    public MapStage Stage { get; private set; } = MapStage.Unload;
    public float Progress { get; private set; }
    public Task<MapTravelResult> Done => _completion.Task;
    public void Advance(MapStage stage)
    {
        Progress = stage switch
        {
            MapStage.VeilIn => 0f,
            MapStage.Snapshot => 0.1f,
            MapStage.Unload => 0.2f,
            MapStage.LoadData => 0.3f,
            MapStage.Mount => 0.4f,
            MapStage.Warm => 0.6f,
            MapStage.Spawn => 0.9f,
            MapStage.VeilOut => 0.95f,
            MapStage.Done => 1f,
            _ => throw new ArgumentOutOfRangeException(nameof(stage))
        };
        Stage = stage;
    }
    public void Complete(MapTravelResult result) { Advance(MapStage.Done); _completion.TrySetResult(result); }
}
