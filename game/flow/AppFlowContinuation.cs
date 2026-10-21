using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Save;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Settings;

namespace Campus.Flow;

internal sealed class AppFlowContinuation(ICameraRig camera, ISaveService saves, IMapService maps,
    IWorldState world, IWorldRestore restore, IViewControl view, IMapRuntime mapRuntime,
    IMapStreamState stream, IAppTitlePresentation titlePresentation, AppFlowResources resources,
    ISettingsStore? profileSettings) : IDisposable
{
    private ResumeEndpoint? _resumeEndpoint;
    private SlotId? _resumeSlot;
    private SlotId? _preview;
    private bool _livePreview;
    private bool _newTimeline;
    private Pose _destination;
    private ViewMode _destinationMode;
    private IMapArrival? _titleArrival;
    private bool _titleActive;
    private readonly ISettingsSection? _cameraSettings = profileSettings?.Section("ui");
    internal bool TitlePrepared => _titleArrival is { } arrival &&
        ReferenceEquals(mapRuntime.Arrival, arrival) && arrival.Done.IsCompletedSuccessfully;

    internal void BeginEntry()
    {
        _newTimeline = false; _resumeEndpoint = null; _resumeSlot = null;
    }
    internal void ResetPreview()
    {
        _preview = null; _livePreview = false;
    }
    internal void ResetFaultedWorld()
    {
        ResetPreview(); _newTimeline = false; _resumeEndpoint = null;
    }
    internal void RetainLivePreview() { _preview = saves.Active; _livePreview = true; }
    internal void InvalidateTitleDestination() => _titleArrival = null;
    internal async Task PositionBootTitle(Func<bool> entryTaken)
    {
        var mode = await view.SetMode(ViewMode.Fly, ModeSwitchVia.Ui);
        if (mode == ModeSwitchResult.Refused) throw new InvalidOperationException("Title view mode was refused.");
        if (!entryTaken()) camera.SetPose(TitlePose());
    }
    internal async Task LoadBootWorld(BootFlowRequest request, SlotId? bootSlot)
    {
        if (request.LoadFile is { } file)
        {
            RequireLoaded(await saves.LoadFile(file));
            _preview = null;
        }
        else if (bootSlot is { } slot)
        {
            var result = await saves.Load(slot, request.ShowTitle ? LoadScope.Preview : LoadScope.Full);
            RequireHealthyWorld();
            if (Loaded(result.Status)) _preview = request.ShowTitle ? slot : null;
            else if (request.Slot is not null) RequireLoaded(result);
            else _preview = null;
        }
    }
    internal async Task StartNewTimeline(SlotId slot, uint? seed)
    {
        if (_newTimeline) return;
        // Save owns the restore transaction and keeps existing same-map geometry.
        RequireLoaded(await saves.NewGame(slot, seed: seed));
        _newTimeline = true; ResetPreview();
        RequireHealthyWorld(); resources.HoldLoadingFlow();
        _destination = camera.Pose; _destinationMode = camera.Mode;
        resources.CompleteWorldPreparation();
    }
    internal async Task RestoreRequested(SlotId slot)
    {
        if (restore.RestoreFaulted || _preview != slot || (!saves.HasPendingPose && !_livePreview))
        {
            RequireLoaded(await saves.Load(slot, LoadScope.Preview));
            _preview = slot; _livePreview = false;
        }
        RequireHealthyWorld(); resources.HoldLoadingFlow();
        _destination = Info(slot)?.Meta?.Pose ?? camera.Pose;
        _destinationMode = Info(slot)?.Meta?.Mode ?? camera.Mode;
        _resumeSlot = slot;
        resources.CompleteWorldPreparation();
    }

    internal async Task PrepareResumeEndpoint()
    {
        if (_resumeSlot is null) throw new InvalidOperationException("Continue did not request its saved world.");
        if (_resumeEndpoint is not null) { RequirePreparedResume(_resumeSlot.Value); return; }
        var slot = _resumeSlot.Value;
        var context = Context(); var instance = world.Instance;
        var savedFlow = world.BaseFlow;
        resources.HoldLoadingFlow();
        var frozenHours = world.WorldHours;
        camera.SetPose(_destination with { FovDeg = _destination.FovDeg ?? UserFov(_destinationMode, false) });
        IMapArrival arrival;
        if (saves.HasPendingPose)
        {
            // Save owns the pending pose's arrival and restore transaction.
            await saves.CompleteResume();
            arrival = mapRuntime.Arrival ?? throw new InvalidOperationException("Resume preparation has no arrival.");
            resources.ObserveArrival(arrival);
            await WaitForArrival(arrival);
        }
        else
        {
            var switched = await view.SetMode(_destinationMode, ModeSwitchVia.Ui);
            switch (switched)
            {
                case ModeSwitchResult.Ok: case ModeSwitchResult.Relocated: break;
                case ModeSwitchResult.Refused: throw new InvalidOperationException("Saved view mode was refused.");
                default: throw new ArgumentOutOfRangeException(nameof(switched));
            }
            _destination = camera.Pose;
            arrival = mapRuntime.Arrive(_destination, _destinationMode, Info(slot)?.Meta?.Space);
            // Save resumption retains its covered world without the ordinary arrival limit.
            await WaitForArrival(arrival);
        }
        RequireHealthyWorld();
        if (!ReferenceEquals(maps.Context, context) || maps.Current != context.Descriptor.Id || world.Instance != instance)
            throw new InvalidOperationException("World or map changed while preparing the resume destination.");
        if (world.Flow.Rate != 0 || world.WorldHours != frozenHours || world.BaseFlow != savedFlow)
            throw new InvalidOperationException("Continue preparation changed the frozen world time or saved flow.");
        if (saves.HasPendingPose) throw new InvalidOperationException("Resume preparation left the saved pose pending.");
        _destination = camera.Pose; _destinationMode = camera.Mode;
        RequireArrival(arrival, _destination, _destinationMode);
        _resumeEndpoint = new(slot, context, instance, _destination, _destinationMode, arrival);
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

    internal async Task PrepareTitleDestination()
    {
        titlePresentation.SetVisible(true);
        if (TitlePrepared) { resources.ObserveArrival(_titleArrival); return; }
        var pose = TitlePose();
        camera.SetPose(pose);
        var arrival = mapRuntime.Arrive(pose, ViewMode.Fly);
        await WaitForArrival(arrival);
        RequireHealthyWorld();
        RequireArrival(arrival, pose, ViewMode.Fly);
        _titleArrival = arrival;
    }

    internal void RequirePreparedResume(SlotId slot)
    {
        var endpoint = _resumeEndpoint ?? throw new InvalidOperationException("Continue reached handoff without preparing its covered destination.");
        if (slot != endpoint.Slot || !ReferenceEquals(maps.Context, endpoint.Context) ||
            maps.Current != endpoint.Context.Descriptor.Id || world.Instance != endpoint.Instance ||
            _destination != endpoint.Pose || _destinationMode != endpoint.Mode || saves.HasPendingPose)
            throw new InvalidOperationException("Prepared resume destination was invalidated before handoff.");
        RequireArrival(endpoint.Arrival, endpoint.Pose, endpoint.Mode);
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
    private IMapContext Context() => maps.Context ?? throw new InvalidOperationException("No map is loaded.");
    private Pose TitlePose() => Context().Descriptor.Title?.T0 ?? maps.Maps.FirstOrDefault(m => m.Id == maps.Default)?.Title?.T0 ?? maps.ResolveSpawn(Context().Descriptor.Id).Fly;
    internal SlotInfo? Info(SlotId slot) => saves.Slots.FirstOrDefault(s => s.Slot == slot);
    internal static bool Readable(SlotInfo? slot) => slot?.Health switch
    {
        SlotHealth.Ok => slot.Meta is not null,
        SlotHealth.Empty or SlotHealth.Corrupt or null => false,
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
            case AppState.Entering: break;
            case AppState.Boot: case AppState.Playing: case AppState.Leaving: case AppState.Quitting:
                stream.SetPinnedDestinations([]);
                break;
            default: throw new ArgumentOutOfRangeException(nameof(state));
        }
    }
    internal void RefreshTitlePrefetch()
    {
        if (!_titleActive || restore.RestoreFaulted || maps.Context is null)
        { stream.SetPinnedDestinations([]); return; }
        var destinations = new List<MapStreamDestination>(2);
        if (maps.Default == maps.Current)
            destinations.Add(new(maps.ResolveSpawn(maps.Default).ViewPose(ViewMode.Walk), ViewMode.Walk));
        if (saves.Continue is { } slot && Info(slot) is { Health: SlotHealth.Ok, Meta: { } meta } && meta.Map == maps.Current)
            destinations.Add(new(meta.Pose, meta.Mode, meta.Space));
        stream.SetPinnedDestinations(destinations);
    }
    internal void StopTitlePrefetch()
    {
        _titleActive = false;
        saves.SlotsChanged -= RefreshTitlePrefetch;
    }
    public void Dispose()
    {
        StopTitlePrefetch(); stream.SetPinnedDestinations([]);
    }
    private sealed record ResumeEndpoint(SlotId Slot, IMapContext Context, long Instance, Pose Pose,
        ViewMode Mode, IMapArrival Arrival);
}
