using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Atlas;
using Campus.Contracts.Maps;
using Campus.Contracts.Perf;
using Campus.Kernel.Frame;
using Godot;
using KernelServices = Campus.Kernel.Registry.Kernel;

namespace Campus.Gates;

internal sealed record PerfTravelSegment(int Index, string MarkerId, double FromMs)
{
    public double? ToMs { get; set; }
    public bool Completed { get; set; }
}

/// <summary>Exercises production Atlas arrivals and native card input; Godot has no collection sequence driver.</summary>
internal sealed class PerfTravelDriver(IStageGateHost host)
{
    private readonly StartupSampleInput _input = new(host);
    private readonly List<PerfTravelSegment> _segments = [];
    public IReadOnlyList<PerfTravelSegment> Segments => _segments;

    public async Task<string?> Run(IReadOnlyList<string> sequence, bool prefetch)
    {
        if (!host.Flow.IsPlaying) throw new InvalidOperationException("Travel sequence requires Playing after the normal entry flow.");
        var atlas = KernelServices.Get<IAtlasService>();
        var observer = KernelServices.Get<IObserverState>();
        var walk = KernelServices.Get<IWalkController>();
        await _input.WaitForPlayingWalk();
        for (var index = 0; index < sequence.Count; index++)
        {
            var id = new MarkerId(sequence[index]);
            var stand = atlas.Find(id)?.Teleport?.Stand
                ?? throw new InvalidOperationException("Travel sequence requires a standing destination: " + id.Value);
            if (!atlas.CanTravel(id, TravelStyle.Teleport, out var refusal))
                throw new InvalidOperationException($"Travel sequence cannot start {id.Value}: {refusal}.");
            if (prefetch) await OpenPrefetchedCard(id, stand);
            var previousArrival = host.MapRuntime.Arrival;
            var segment = new PerfTravelSegment(index + 1, id.Value, FrameClock.MonotonicMilliseconds);
            _segments.Add(segment);
            var handle = prefetch ? await ConfirmCard() : atlas.Travel(id, TravelStyle.Teleport);
            if (handle is null) throw new InvalidOperationException("Travel sequence was refused: " + id.Value);
            var progress = handle.Progress;
            await _input.Wait(() => handle.Done.IsCompleted, "Travel sequence transit stopped making progress.", () =>
            {
                var changed = progress != handle.Progress;
                progress = handle.Progress;
                return changed;
            });
            if (await handle.Done != TransitResult.Arrived)
                throw new InvalidOperationException("Travel sequence did not arrive: " + id.Value);
            await AwaitArrival(id, previousArrival);
            if (StartupRouteProgress.StartFailure(new(stand.Feet, stand.YawDeg, 0), observer.Feet, observer.YawDeg) is { } failure)
                return failure;
            segment.ToMs = FrameClock.MonotonicMilliseconds;
            segment.Completed = true;
        }
        return null;
    }

    private async Task AwaitArrival(MarkerId id, IMapArrival? previous)
    {
        var arrival = host.MapRuntime.Arrival
            ?? throw new InvalidOperationException("Travel did not publish a formal arrival.");
        var atlas = KernelServices.Get<IAtlasService>();
        var camera = KernelServices.Get<ICameraRig>();
        var observer = KernelServices.Get<IObserverState>();
        var walk = KernelServices.Get<IWalkController>();
        var loading = KernelServices.Get<IAppLoadingPresentation>();
        if (ReferenceEquals(previous, arrival) || !ReferenceEquals(walk.PlacementArrival, arrival))
            throw new InvalidOperationException("Travel did not publish its own walking placement arrival.");
        await host.Readiness.WaitForReadyAsync(host.Stage, arrival, () =>
        {
            host.CheckActive();
            if (host.Flow.State != AppState.Playing || observer.Mode != ViewMode.Walk || camera.Mode != ViewMode.Walk ||
                !walk.Active || walk.ExitRequested || !ReferenceEquals(host.MapRuntime.Arrival, arrival))
                throw new InvalidOperationException("Travel lost its current Playing Walk arrival.");
            atlas.CanTravel(id, TravelStyle.Teleport, out var refusal);
            if (refusal is not (TravelRefusal.Busy or TravelRefusal.Unavailable or TravelRefusal.Here))
                throw new InvalidOperationException($"Travel destination changed after arrival: {id.Value}, {refusal}.");
            return refusal == TravelRefusal.Here && camera.ActiveTransit is null && !loading.Active &&
                walk.Grounded && walk.CollisionReady && walk.IsSettled;
        }, host.Lifetime);
    }

    private async Task OpenPrefetchedCard(MarkerId id, WaypointStand stand)
    {
        var bindings = KernelServices.Get<IInputRouter>();
        var surfaces = KernelServices.Get<IUiSurfaceState>();
        var stream = KernelServices.Get<IMapStreamState>();
        if (stream.PinnedDestinations.Count != 0)
            throw new InvalidOperationException("Travel prefetch inherited another owner's retained destination.");
        await Tap((Key)bindings.PhysicalKey("atlas_index"), true);
        await _input.Wait(() => (surfaces.Open & UiSurface.AtlasIndex) != 0 && Focus() is { } focus &&
            focus.IsVisibleInTree() && focus.GetFocusModeWithOverride() != Control.FocusModeEnum.None,
            "The production Atlas index did not accept keyboard focus.");
        var atlas = KernelServices.Get<IAtlasService>();
        var visited = new HashSet<string>(StringComparer.Ordinal);
        while (Focus()?.AccessibilityName is not { } name || !name.EndsWith(" · " + id.Value, StringComparison.Ordinal))
        {
            var current = Focus()?.AccessibilityName ?? "";
            if (!visited.Add(current) || visited.Count > atlas.Markers.Count)
                throw new InvalidOperationException("Native Atlas directory navigation did not reach: " + id.Value);
            await Tap(Key.Down, false);
        }
        var marker = atlas.Find(id)!;
        var pose = new MapSpawn(id.Value, marker.Teleport!.Fly, stand).ViewPose(ViewMode.Walk)
            with { FovDeg = KernelServices.Get<ICameraRig>().Pose.FovDeg };
        var expected = new MapStreamDestination(pose, ViewMode.Walk, CollisionFeet: stand.Feet);
        await Tap(Key.Enter, false);
        // The formal card owner must publish the destination after its own threshold. No timer claims readiness.
        var statistics = KernelServices.Get<IStreamStatistics>();
        var previous = PrefetchProgress(statistics.Statistics);
        await _input.Wait(() =>
        {
            if (stream.Failure is { } failure) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
            if (stream.PinnedDestinations.Count == 0) return false;
            if (stream.PinnedDestinations.Count != 1 || stream.PinnedDestinations[0] != expected)
                throw new InvalidOperationException("The production Atlas card retained a different destination.");
            return stream.PinnedDestinationReady(expected);
        }, "The production Atlas card destination stopped making prefetch progress.", () =>
        {
            var next = PrefetchProgress(statistics.Statistics);
            var changed = next != previous;
            previous = next;
            return changed;
        });
    }

    private static (long, int, int, int, int, long) PrefetchProgress(in StreamStatistics stats) =>
        (stats.Revision, stats.PendingRequests, stats.PendingBakes, stats.PendingPreparations, stats.PendingMounts, stats.MountedBytes);

    private async Task<ITransitHandle?> ConfirmCard()
    {
        ITransitHandle? handle = null;
        await _input.Press(Key.Enter, false, async () =>
        {
            handle = KernelServices.Get<ICameraRig>().ActiveTransit;
            await _input.NextFrame();
            handle ??= KernelServices.Get<ICameraRig>().ActiveTransit;
        });
        return handle ?? KernelServices.Get<ICameraRig>().ActiveTransit;
    }

    private Control? Focus() => host.Stage.GetViewport().GuiGetFocusOwner();
    private Task Tap(Key key, bool physical) => _input.Press(key, physical, _input.NextFrame);
}
