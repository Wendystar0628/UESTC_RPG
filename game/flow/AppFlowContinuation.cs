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
    private readonly ISettingsSection? _cameraSettings = profileSettings?.Section("ui");
    internal bool NewGamePreloadReady => _newGameDestination is { } destination &&
        destination.Pose.FovDeg == UserFov(ViewMode.Walk, false) && stream.PinnedDestinationReady(destination);
    internal void BeginEntry() { _newTimeline = false; RefreshTitlePrefetch(); }
    internal async Task StartNewTimeline(SlotId? slot, uint? seed, WorldPreset? start = null, NewGameBoot? boot = null)
    {
        if (_newTimeline) return;
        // Save owns the restore transaction and keeps existing same-map geometry.
        RequireLoaded(await saves.NewGame(slot, seed: seed, start: start, boot: boot));
        _newTimeline = true;
        RequireHealthyWorld(); resources.HoldLoadingFlow();
        resources.CompleteWorldPreparation();
    }
    internal async Task RestoreRequested(SlotId slot)
    {
        RequireLoaded(await saves.Load(slot));
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
    internal static bool Readable(SlotInfo? slot) => slot?.Health switch
    {
        SlotHealth.Ok => slot.Meta is not null,
        SlotHealth.Empty or SlotHealth.Corrupt or SlotHealth.Newer or null => false,
        _ => throw new InvalidOperationException("Unknown slot health."),
    };
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
    internal static bool CanLeave(SaveStatus status) => status switch
    {
        SaveStatus.Saved or SaveStatus.Deferred or SaveStatus.Disabled => true,
        SaveStatus.Failed => false,
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };
    internal void OnStateChange(AppState state)
    {
        _titleActive = state is AppState.Title;
        switch (state)
        {
            case AppState.Title:
                saves.SlotsChanged += RefreshTitlePrefetch;
                RefreshTitlePrefetch();
                break;
            case AppState.Entering: _titleActive = true; break;
            case AppState.Boot: case AppState.Playing: case AppState.Leaving: case AppState.Quitting:
                stream.SetPinnedDestinations([]);
                break;
            default: throw new ArgumentOutOfRangeException(nameof(state));
        }
    }
    internal void RefreshTitlePrefetch()
    {
        if (!_titleActive || maps.Context is null)
        { stream.SetPinnedDestinations([]); return; }
        var destinations = new List<MapStreamDestination>(2);
        if (maps.Default == maps.Current)
            destinations.Add(_newGameDestination = NewGameDestination()!);
        if (saves.Continue is { } slot && Info(slot) is { Health: SlotHealth.Ok, Meta: { } meta } && meta.Map == maps.Current)
            destinations.Add(new(meta.Pose with { FovDeg = meta.Pose.FovDeg ?? UserFov(meta.Mode, false) }, meta.Mode, meta.Space,
                meta.Mode == ViewMode.Walk ? meta.Pose.Position - System.Numerics.Vector3.UnitY * WalkRules.StandingEye : null));
        stream.SetPinnedDestinations(destinations);
    }
    internal void RetainTitlePrefetch() => _titleActive = true;
    internal void StopTitlePrefetch()
    {
        saves.SlotsChanged -= RefreshTitlePrefetch;
    }
    public void Dispose()
    {
        StopTitlePrefetch(); stream.SetPinnedDestinations([]);
    }
    private MapStreamDestination? NewGameDestination()
    {
        if (maps.Context is null || maps.Current != maps.Default) return null;
        var spawn = maps.ResolveSpawn(maps.Default);
        var pose = spawn.ViewPose(ViewMode.Walk) with { FovDeg = UserFov(ViewMode.Walk, false) };
        return new(pose, ViewMode.Walk, CollisionFeet: spawn.Stand!.Feet);
    }
}
