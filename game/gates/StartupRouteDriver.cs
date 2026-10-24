using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Atlas;
using Campus.Contracts.Bake;
using Campus.Contracts.Maps;
using Campus.Contracts.Startup;
using Campus.Kernel.Frame;
using Godot;
using N = System.Numerics;
using KernelServices = Campus.Kernel.Registry.Kernel;

namespace Campus.Gates;

/// <summary>Reaches an authored point through Atlas, then runs a straight native-input route through the live observer.</summary>
internal sealed class StartupRouteDriver(IStageGateHost host, StartupSampleInput input)
{
    private const double SampleMilliseconds = 1000;

    public async Task Run(StartupRouteOptions options)
    {
        var camera = KernelServices.Get<ICameraRig>();
        var observer = KernelServices.Get<IObserverState>();
        var walk = KernelServices.Get<IWalkController>();
        var bindings = KernelServices.Get<IInputRouter>();
        var maps = KernelServices.Get<IMapService>();
        await input.WaitForPlayingWalk();
        var stand = maps.ResolveSpawn(maps.Current, options.Start).Stand
            ?? throw new InvalidOperationException("Route start has no authored standing destination: " + options.Start);
        var requested = new Pose(stand.Feet, stand.YawDeg, 0);
        var milliseconds = options.DurationSeconds * 1000;
        GD.Print(FormattableString.Invariant($"STARTUP_ROUTE_REQUEST start={options.Start} mode={options.Mode} durationSeconds={options.DurationSeconds:R} feet_x={stand.Feet.X:R} feet_y={stand.Feet.Y:R} feet_z={stand.Feet.Z:R} yaw={stand.YawDeg:R}"));
        PrintPose("entry", options.Start, observer, 0);
        await ReachStart(options.Start, maps.Current, camera, observer, walk);
        PrintPose("arrival", options.Start, observer, 0);
        // Atlas arrival and the requested point must agree before any route input is held.
        if (RejectCollectedStart(StartupRouteProgress.StartFailure(requested, observer.Feet, observer.YawDeg))) return;
        _ = new StartupRouteProgress(requested, ViewMode.Walk, milliseconds,
            observer.Feet, observer.YawDeg, observer.Mode);
        switch (options.Mode)
        {
            case ViewMode.Walk: break;
            case ViewMode.Fly:
                await EnterFly(bindings, camera, observer);
                requested = requested with { Position = stand.Feet + N.Vector3.UnitY * WalkRules.StandingEye };
                break;
            default: throw new InvalidOperationException("Route requested an unsupported view mode.");
        }
        if (RejectCollectedStart(StartupRouteProgress.StartFailure(requested, observer.Feet, observer.YawDeg) ??
            ActiveFailure(options.Mode, camera, observer, walk))) return;
        PrintPose("start", options.Start, observer, 0);
        var progress = new StartupRouteProgress(requested, options.Mode, milliseconds,
            observer.Feet, observer.YawDeg, observer.Mode);
        RequireActive(options.Mode, camera, observer, walk);
        using var forwardAction = new StringName("move_forward");
        if (Input.IsActionPressed(forwardAction))
            throw new InvalidOperationException("The route inherited a held forward action.");
        var forwardKey = (Key)bindings.PhysicalKey("move_forward");
        GD.Print($"STARTUP_ROUTE_INPUT action=move_forward physicalKey={(int)forwardKey}");
        async Task<string?> Move()
        {
            var failure = StartupRouteProgress.StartFailure(requested, observer.Feet, observer.YawDeg) ??
                ActiveFailure(options.Mode, camera, observer, walk);
            if (failure is not null) return failure;
            await input.Press(forwardKey, true, "route-forward-start", "route-forward-end", async () =>
            {
                var started = FrameClock.MonotonicMilliseconds;
                var nextSample = SampleMilliseconds;
                while (true)
                {
                    await input.NextFrame();
                    failure = ActiveFailure(options.Mode, camera, observer, walk);
                    if (failure is not null) return;
                    var elapsed = FrameClock.MonotonicMilliseconds - started;
                    if (elapsed >= milliseconds || elapsed >= nextSample && milliseconds - elapsed >= SampleMilliseconds)
                    {
                        failure = progress.ObservationFailure(observer.Feet, observer.Mode, elapsed);
                        if (failure is not null) return;
                        progress.Observe(observer.Feet, observer.Mode, elapsed);
                        PrintPose("sample", options.Start, observer, elapsed);
                        nextSample = elapsed + SampleMilliseconds;
                    }
                    if (elapsed >= milliseconds) break;
                }
                failure = progress.CompletionFailure();
            }, milliseconds, () => input.WaitForRelease(forwardAction));
            return failure;
        }
        if (Campus.Kernel.Boot.Boot.Options.PerfCollection is { } collection)
        {
            if (!await new PerfCollectionDriver(host, collection).Run(Move)) return;
        }
        else if (await Move() is { } failure) throw new InvalidOperationException(failure);
        RequireActive(options.Mode, camera, observer, walk);
        PrintPose("settled", options.Start, observer, progress.ElapsedMilliseconds);
        GD.Print(FormattableString.Invariant($"STARTUP_ROUTE_COMPLETE start={options.Start} mode={observer.Mode} durationSeconds={options.DurationSeconds:R} elapsedMilliseconds={progress.ElapsedMilliseconds:R} distance={progress.Distance:R} forward={progress.ForwardDistance:R}"));
    }

    private bool RejectCollectedStart(string? reason)
    {
        if (reason is null || Campus.Kernel.Boot.Boot.Options.PerfCollection is not { } collection) return false;
        new PerfCollectionDriver(host, collection).RejectInput(reason);
        return true;
    }

    private async Task ReachStart(string start, MapId map, ICameraRig camera, IObserverState observer, IWalkController walk)
    {
        var atlas = KernelServices.Get<IAtlasService>();
        if (atlas.Map != map) throw new InvalidOperationException("The route Atlas does not belong to the active map.");
        var destination = atlas.Markers.SingleOrDefault(marker => marker.Kind == MarkerKind.Viewpoint &&
            marker.WaypointId == start && marker.Teleport?.Stand is not null)
            ?? throw new InvalidOperationException("Route start has no standing Atlas waypoint marker: " + start);
        if (!atlas.CanTravel(destination.Id, TravelStyle.Teleport, out var refusal))
        {
            // Atlas's Here radius is broader than the route start tolerance; Run still checks the exact readback.
            if (refusal == TravelRefusal.Here) return;
            throw new InvalidOperationException($"Route cannot reach its start {start}: {refusal}.");
        }
        var previousArrival = host.MapRuntime.Arrival;
        var transit = atlas.Travel(destination.Id, TravelStyle.Teleport)
            ?? throw new InvalidOperationException("The production Atlas refused the route start: " + start);
        GD.Print($"STARTUP_ROUTE_TRAVEL start={start} marker={destination.Id.Value}");
        var bake = KernelServices.Get<IBakeService>();
        var progress = (transit.Progress, bake.Progress, host.MapRuntime.Arrival, host.MapRuntime.Arrival?.Completed);
        await input.Wait(() => transit.Done.IsCompleted, "The production Atlas route arrival stopped making progress.", () =>
        {
            var next = (transit.Progress, bake.Progress, host.MapRuntime.Arrival, host.MapRuntime.Arrival?.Completed);
            var changed = next != progress;
            progress = next;
            return changed;
        });
        if (await transit.Done != TransitResult.Arrived)
            throw new InvalidOperationException("The production Atlas did not arrive at the route start: " + start);
        var arrival = host.MapRuntime.Arrival
            ?? throw new InvalidOperationException("The route start transit left no formal arrival.");
        if (ReferenceEquals(previousArrival, arrival) || !ReferenceEquals(walk.PlacementArrival, arrival))
            throw new InvalidOperationException("The route start transit did not publish its walking arrival.");
        var loading = KernelServices.Get<IAppLoadingPresentation>();
        await host.Readiness.WaitForReadyAsync(host.Stage, arrival, () =>
        {
            host.CheckActive();
            if (host.Flow.State != AppState.Playing || camera.Mode != ViewMode.Walk ||
                observer.Mode != ViewMode.Walk || !walk.Active || walk.ExitRequested)
                throw new InvalidOperationException("Route arrival lost the active Playing Walk controller.");
            // Here follows Atlas.Tick's release of the completed transit's input lease.
            atlas.CanTravel(destination.Id, TravelStyle.Teleport, out var currentRefusal);
            if (currentRefusal is not (TravelRefusal.Here or TravelRefusal.Busy or TravelRefusal.Unavailable))
                throw new InvalidOperationException($"Route start marker changed after arrival: {currentRefusal}.");
            return currentRefusal == TravelRefusal.Here && camera.ActiveTransit is null && !loading.Active &&
                walk.Grounded && walk.CollisionReady && walk.IsSettled;
        }, host.Lifetime);
    }

    private async Task EnterFly(IInputRouter bindings, ICameraRig camera, IObserverState observer)
    {
        using var toggleAction = new StringName("toggle_mode");
        if (Input.IsActionPressed(toggleAction)) throw new InvalidOperationException("The route inherited a held mode toggle.");
        await input.Press((Key)bindings.PhysicalKey("toggle_mode"), true,
            "route-fly-press", "route-fly-release", input.NextFrame);
        var loading = KernelServices.Get<IAppLoadingPresentation>();
        var progress = (observer.Mode, camera.ActiveTransit?.Progress, loading.Active, host.MapRuntime.Arrival?.Completed);
        await input.Wait(() => host.Flow.State == AppState.Playing && observer.Mode == ViewMode.Fly &&
            camera.Mode == ViewMode.Fly && camera.ActiveTransit is null && !loading.Active,
            "The production mode-toggle input did not release Fly mode.", () =>
            {
                var next = (observer.Mode, camera.ActiveTransit?.Progress, loading.Active, host.MapRuntime.Arrival?.Completed);
                var changed = next != progress;
                progress = next;
                return changed;
            });
    }

    private void RequireActive(ViewMode mode, ICameraRig camera, IObserverState observer, IWalkController walk)
    {
        if (ActiveFailure(mode, camera, observer, walk) is { } failure) throw new InvalidOperationException(failure);
    }

    private string? ActiveFailure(ViewMode mode, ICameraRig camera, IObserverState observer, IWalkController walk)
    {
        if (host.Flow.State != AppState.Playing || camera.Mode != mode || observer.Mode != mode || camera.ActiveTransit is not null)
            return $"Route left its active Playing mode: requested {mode}, camera {camera.Mode}, observer {observer.Mode}, flow {host.Flow.State}.";
        var valid = mode switch
        {
            ViewMode.Walk => walk.Active && !walk.ExitRequested && walk.CollisionReady,
            ViewMode.Fly => !walk.Active,
            _ => throw new InvalidOperationException("Route requested an unsupported view mode.")
        };
        return valid ? null : "Route lost the production controller for its requested mode.";
    }

    private static void PrintPose(string phase, string start, IObserverState observer, double elapsed)
    {
        var p = observer.Feet;
        GD.Print(FormattableString.Invariant($"STARTUP_ROUTE_POSE phase={phase} start={start} mode={observer.Mode} time={FrameClock.MonotonicMilliseconds:R} elapsedMilliseconds={elapsed:R} x={p.X:R} y={p.Y:R} z={p.Z:R} yaw={observer.YawDeg:R}"));
    }
}
