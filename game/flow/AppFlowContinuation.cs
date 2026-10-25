using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Save;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Settings;

namespace Campus.Flow;

internal sealed class AppFlowContinuation(ICameraRig camera, ISaveService saves, IMapService maps,
    IWorldRestore restore, IMapRuntime mapRuntime,
    IMapStreamState stream, AppFlowResources resources,
    ISettingsStore? profileSettings) : IDisposable
{
    private bool _newTimeline;
    private MapStreamDestination? _newGameDestination;
    private bool _titleActive;
    private EnterRequest? _entryRequest;
    private readonly Dictionary<MapStreamDestination, IDisposable> _titlePins = [];
    private IMapContext? _titlePinContext;
    private readonly ISettingsSection? _cameraSettings = profileSettings?.Section("ui");
    internal bool NewGamePreloadReady => _newGameDestination is { } destination &&
        destination.Pose.FovDeg == UserFov(ViewMode.Walk, false) && stream.PinnedDestinationReady(destination);
    internal void BeginEntry(EnterRequest request) { _newTimeline = false; _entryRequest = request; RefreshTitlePrefetch(); }
    internal async Task StartNewTimeline(SlotId? slot, uint? seed, WorldPreset? start = null, NewGameBoot? boot = null)
    {
        if (_newTimeline) return;
        // Save owns the restore transaction and keeps existing same-map geometry.
        RequireLoaded(await saves.NewGame(slot, seed: seed, start: start, boot: boot));
        _newTimeline = true;
        RequireHealthyWorld(); resources.HoldLoadingFlow();
        resources.CompleteWorldPreparation();
    }
    internal async Task RestoreRequested(SlotId slot, ulong? gen)
    {
        RequireLoaded(await (gen is { } selected ? saves.Load(slot, selected) : saves.Load(slot)));
        RequireHealthyWorld(); resources.HoldLoadingFlow();
        resources.CompleteWorldPreparation();
    }

    internal async Task PreparePlayableDestination(bool captureMode)
    {
        var mode = camera.Mode;
        camera.SetPose(camera.Pose with { FovDeg = camera.Pose.FovDeg ?? UserFov(mode, captureMode) });
        var pose = camera.Pose;
        var arrival = mapRuntime.Arrival;
        if (arrival is null || !Matches(arrival, pose, mode) || !arrival.Done.IsCompletedSuccessfully)
        {
            arrival = mapRuntime.Arrive(pose, mode);
            await WaitForArrival(arrival);
        }
        resources.ObserveArrival(arrival);
        RequireHealthyWorld();
        RequireArrival(arrival, pose, mode);
    }

    private async Task WaitForArrival(IMapArrival arrival)
    {
        try
        {
            await arrival.Done;
            if (!ReferenceEquals(mapRuntime.Arrival, arrival)) throw new OperationCanceledException("Flow arrival was replaced.");
        }
        catch { arrival.Cancel(); throw; }
    }

    private void RequireArrival(IMapArrival arrival, Pose pose, ViewMode mode)
    {
        if (!ReferenceEquals(mapRuntime.Arrival, arrival) || !Matches(arrival, pose, mode))
            throw new InvalidOperationException("Readiness belongs to a different arrival.");
        if (!arrival.Done.IsCompleted) throw new InvalidOperationException("Arrival is still pending.");
        arrival.Done.GetAwaiter().GetResult();
    }
    private static bool Matches(IMapArrival arrival, Pose pose, ViewMode mode) =>
        arrival.Destination.Mode == mode && SameLocation(arrival.Destination.Pose, pose);
    private static bool SameLocation(Pose a, Pose b) => a.Position == b.Position &&
        (a.YawDeg - b.YawDeg) % 360 == 0 && a.PitchDeg == b.PitchDeg && a.FovDeg == b.FovDeg;
    internal void RequireHealthyWorld()
    {
        if (restore.RestoreFaulted) throw new InvalidOperationException(AppFlowRecovery.RestoreErrorKey);
    }
    private float UserFov(ViewMode mode, bool captureMode)
    {
        var definition = mode switch
        {
            ViewMode.Fly => UiSettingDefinitions.FlyFov,
            ViewMode.Walk => UiSettingDefinitions.WalkFov,
            _ => throw new ArgumentOutOfRangeException(nameof(mode))
        };
        return captureMode ? definition.Default : _cameraSettings?.Get(definition) ?? definition.Default;
    }
    internal SlotInfo? Info(SlotId slot) => saves.Slots.FirstOrDefault(s => s.Slot == slot);
    internal static bool Readable(SlotInfo? slot, ulong? gen = null) => ReadableMeta(slot, gen) is not null;
    private static SaveMeta? ReadableMeta(SlotInfo? slot, ulong? gen = null)
    {
        if (slot is null) return null;
        switch (slot.Health)
        {
            case SlotHealth.Newer: return null;
            case SlotHealth.Ok: case SlotHealth.Empty: case SlotHealth.Corrupt: break;
            default: throw new InvalidOperationException("Unknown slot health.");
        }
        if (gen is null) return slot.Health is SlotHealth.Ok ? slot.Meta : null;
        var entry = slot.Entries.FirstOrDefault(entry => entry.Gen == gen.Value);
        return entry?.Health switch
        {
            SlotHealth.Ok => entry.Meta,
            SlotHealth.Empty or SlotHealth.Corrupt or SlotHealth.Newer or null => null,
            _ => throw new InvalidOperationException("Unknown entry health."),
        };
    }
    internal static bool Loaded(LoadStatus status) => status switch
    {
        LoadStatus.Loaded => true,
        LoadStatus.Empty or LoadStatus.Corrupt or LoadStatus.Failed => false,
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };
    internal static void RequireLoaded(LoadResult result)
    {
        if (!Loaded(result.Status)) throw new InvalidDataException(result.Error ?? "The requested world could not be restored.");
    }
    internal static bool InitialSaveAccepted(SaveStatus status) => status switch
    {
        // Explicit read-only runs keep their existing entry behavior; a pending write is not a first generation.
        SaveStatus.Saved or SaveStatus.Disabled => true,
        SaveStatus.Deferred or SaveStatus.Failed => false,
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };
    internal void OnStateChange(AppState state)
    {
        _titleActive = state is AppState.Title;
        switch (state)
        {
            case AppState.Title:
                _entryRequest = null;
                saves.SlotsChanged += RefreshTitlePrefetch;
                RefreshTitlePrefetch();
                break;
            case AppState.Entering: _titleActive = true; break;
            case AppState.Boot: case AppState.Playing: case AppState.Leaving: case AppState.Quitting:
                ReleaseTitlePins();
                break;
            default: throw new ArgumentOutOfRangeException(nameof(state));
        }
    }
    internal void RefreshTitlePrefetch()
    {
        if (!_titleActive || maps.Context is null)
        { ReleaseTitlePins(); return; }
        if (!ReferenceEquals(_titlePinContext, maps.Context)) ReleaseTitlePins();
        _titlePinContext = maps.Context;
        var destinations = new List<MapStreamDestination>(2);
        _newGameDestination = null;
        if ((_entryRequest is null || _entryRequest.Kind is EnterKind.NewGame) && maps.Default == maps.Current)
            destinations.Add(_newGameDestination = NewGameDestination()!);
        var meta = _entryRequest switch
        {
            { Kind: EnterKind.Continue or EnterKind.LoadSlot, Slot: { } requested } => ReadableMeta(Info(requested), _entryRequest.Gen),
            null => saves.Continue is { } slot ? ReadableMeta(Info(slot)) : null,
            { Kind: EnterKind.NewGame or EnterKind.Continue or EnterKind.LoadSlot } => null,
            _ => throw new ArgumentOutOfRangeException(nameof(_entryRequest))
        };
        if (meta is not null && meta.Map == maps.Current)
            destinations.Add(new(meta.Pose with { FovDeg = meta.Pose.FovDeg ?? UserFov(meta.Mode, false) }, meta.Mode, meta.Space,
                meta.Mode == ViewMode.Walk ? meta.Pose.Position - System.Numerics.Vector3.UnitY * WalkRules.StandingEye : null));
        foreach (var destination in destinations)
            if (!_titlePins.ContainsKey(destination)) _titlePins.Add(destination, stream.PinDestination(destination));
        foreach (var destination in _titlePins.Keys.Where(destination => !destinations.Contains(destination)).ToArray())
        { _titlePins[destination].Dispose(); _titlePins.Remove(destination); }
    }
    internal void ReleaseTitlePins()
    {
        foreach (var pin in _titlePins.Values) pin.Dispose();
        _titlePins.Clear(); _newGameDestination = null; _titlePinContext = null;
    }
    internal void RetainTitlePrefetch() => _titleActive = true;
    internal void StopTitlePrefetch()
    {
        saves.SlotsChanged -= RefreshTitlePrefetch;
    }
    public void Dispose()
    {
        _titleActive = false; StopTitlePrefetch(); ReleaseTitlePins();
    }
    private MapStreamDestination? NewGameDestination()
    {
        if (maps.Context is null || maps.Current != maps.Default) return null;
        var spawn = maps.ResolveSpawn(maps.Default);
        var pose = spawn.ViewPose(ViewMode.Walk) with { FovDeg = UserFov(ViewMode.Walk, false) };
        return new(pose, ViewMode.Walk, CollisionFeet: spawn.Stand!.Feet);
    }
}
