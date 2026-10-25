using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Atlas;
using Campus.Contracts.Bake;
using Campus.Contracts.Maps;
using Campus.Contracts.Startup;
using Campus.Kernel.Boot;
using Campus.Kernel.Frame;
using Godot;
using N = System.Numerics;
using KernelServices = Campus.Kernel.Registry.Kernel;

namespace Campus.Gates;

/// <summary>Reaches an authored point through Atlas, then runs native-input routes through the live observer.</summary>
internal sealed class StartupRouteDriver(IStageGateHost host, StartupSampleInput input)
{
    private const double SampleMilliseconds = 1000;
    private readonly record struct RoutePoint(N.Vector3 Position, bool Planar = false);

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
        GD.Print(FormattableString.Invariant($"STARTUP_ROUTE_REQUEST start={options.Start} mode={options.Mode} durationSeconds={options.DurationSeconds:R} feet_x={stand.Feet.X:R} feet_y={stand.Feet.Y:R} feet_z={stand.Feet.Z:R} yaw={stand.YawDeg:R}"));
        PrintPose("entry", options.Start, observer, 0);
        await ReachStart(options.Start, maps.Current, camera, observer, walk);
        PrintPose("arrival", options.Start, observer, 0);
        // Atlas arrival and the requested point must agree before any route input is held.
        StartupRouteProgress.RequireStart(requested, observer.Feet, observer.YawDeg);
        switch (options.Mode)
        {
            case ViewMode.Walk: break;
            case ViewMode.Fly:
                await EnterFly(bindings, camera, observer);
                requested = requested with { Position = stand.Feet + N.Vector3.UnitY * WalkRules.StandingEye };
                break;
            default: throw new InvalidOperationException("Route requested an unsupported view mode.");
        }
        StartupRouteProgress.RequireStart(requested, observer.Feet, observer.YawDeg);
        RequireActive(options.Mode, camera, observer, walk);
        PrintPose("start", options.Start, observer, 0);
        if (Campus.Kernel.Boot.Boot.Options.PerfCollection is { RoutePointIds.Length: > 0 } authored)
        {
            await RunAuthored(options, authored, requested);
            return;
        }
        await RunStraight(options, requested);
    }

    private async Task RunStraight(StartupRouteOptions options, Pose requested)
    {
        var camera = KernelServices.Get<ICameraRig>();
        var observer = KernelServices.Get<IObserverState>();
        var walk = KernelServices.Get<IWalkController>();
        var bindings = KernelServices.Get<IInputRouter>();
        var milliseconds = options.DurationSeconds * 1000;
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
                var clock = MovementClock(options.Mode, camera, observer, walk);
                var nextSample = SampleMilliseconds;
                while (true)
                {
                    await input.NextFrame();
                    await clock.WaitForMovementReady();
                    var elapsed = clock.ElapsedMilliseconds;
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
            await new PerfCollectionDriver(host, collection).Run(Move);
        }
        else if (await Move() is { } failure) throw new InvalidOperationException(failure);
        RequireActive(options.Mode, camera, observer, walk);
        PrintPose("settled", options.Start, observer, progress.ElapsedMilliseconds);
        GD.Print(FormattableString.Invariant($"STARTUP_ROUTE_COMPLETE start={options.Start} mode={observer.Mode} durationSeconds={options.DurationSeconds:R} elapsedMilliseconds={progress.ElapsedMilliseconds:R} distance={progress.Distance:R} forward={progress.ForwardDistance:R}"));
    }

    private async Task RunAuthored(StartupRouteOptions options, PerfCollectionOptions collection, Pose requested)
    {
        var camera = KernelServices.Get<ICameraRig>();
        var observer = KernelServices.Get<IObserverState>();
        var walk = KernelServices.Get<IWalkController>();
        var context = KernelServices.Get<IMapService>().Context!;
        var points = collection.RoutePointIds.Select(id => ResolveRoutePoint(id, options.Mode, context)).ToArray();
        var actions = new[] { "move_forward", "move_backward", "move_right", "move_left", "move_up", "move_down" };
        var bindings = KernelServices.Get<IInputRouter>();
        var keys = actions.Select(action => (Key)bindings.PhysicalKey(action)).ToArray();
        var held = new bool[actions.Length];
        var steering = new RouteSteering(keys, held, observer.Feet);
        var stream = KernelServices.Get<IStreamStatistics>();
        var progress = new StartupRouteProgress(requested, options.Mode, options.DurationSeconds * 1000,
            observer.Feet, observer.YawDeg, observer.Mode, allowTurns: true);
        async Task<string?> Move()
        {
            foreach (var action in actions)
                if (Input.IsActionPressed(action)) throw new InvalidOperationException("The authored route inherited a held movement action.");
            var clock = MovementClock(options.Mode, camera, observer, walk);
            var nextSample = SampleMilliseconds;
            Exception? movementFailure = null;
            stream.TileChanged += progress.ObserveTile;
            try
            {
                for (var index = 0; index < points.Length; index++)
                {
                    await MoveToPoint(points[index], options, context, steering, clock, Track);
                    PrintPose("point-" + collection.RoutePointIds[index], options.Start, observer,
                        clock.ElapsedMilliseconds);
                }
                return null;
            }
            catch (Exception error) { movementFailure = error; throw; }
            finally
            {
                stream.TileChanged -= progress.ObserveTile;
                await input.ReleaseKeys(keys, held, movementFailure, () => input.Wait(
                    () => camera.Speed < .0001f && (!walk.Active || walk.IsSettled),
                    "The authored route did not settle after native movement release."));
            }

            void Track()
            {
                var elapsed = clock.ElapsedMilliseconds;
                progress.ObserveContext(observer.Feet, observer.Space, context.World);
                if (elapsed < nextSample) return;
                progress.Observe(observer.Feet, observer.Mode, elapsed);
                PrintPose("sample", options.Start, observer, elapsed);
                nextSample = elapsed + SampleMilliseconds;
            }
        }
        await new PerfCollectionDriver(host, collection).Run(Move);
        GD.Print($"STARTUP_ROUTE_COVERAGE tiles={progress.TileCount} mountedLevels={string.Join(",", progress.MountedLevels.Order())} enteredInterior={progress.EnteredInterior} elapsedMilliseconds={progress.ElapsedMilliseconds:R}");
    }

    private async Task MoveToPoint(RoutePoint target, StartupRouteOptions options, IMapContext context,
        RouteSteering steering, RouteMovementClock clock, Action track)
    {
        var camera = KernelServices.Get<ICameraRig>();
        var observer = KernelServices.Get<IObserverState>();
        var nearest = PointDistance(target, observer.Feet);
        var progressedAt = clock.ElapsedMilliseconds;
        while (true)
        {
            await clock.WaitForMovementReady();
            track();
            if (AtPoint(target, observer, options.Mode, context)) return;
            steering.Steer(target.Position, observer.Feet, camera.Pose.YawDeg, options.Mode,
                (float)host.Stage.GetProcessDeltaTime());
            var distance = PointDistance(target, observer.Feet);
            if (nearest - distance > StartupRouteProgress.PositionTolerance)
            {
                nearest = distance;
                progressedAt = clock.ElapsedMilliseconds;
            }
            if (clock.ElapsedMilliseconds - progressedAt > BootTuning.StalledSeconds * 1000)
                throw new TimeoutException("The authored route stopped making spatial progress.");
            await input.NextFrame();
        }
    }

    private RouteMovementClock MovementClock(ViewMode mode, ICameraRig camera, IObserverState observer, IWalkController walk)
    {
        var bake = KernelServices.Get<IBakeService>();
        var collision = KernelServices.Get<ICollisionWorld>();
        var stream = KernelServices.Get<IStreamStatistics>();
        bool Ready()
        {
            if ((host.MapRuntime.Failure ?? collision.Failure ?? bake.Failure) is { } failure)
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
            host.CheckActive();
            RequireActive(mode, camera, observer, walk);
            return mode != ViewMode.Walk || walk.CollisionReady;
        }
        return new(input, bake, stream, Ready);
    }

    /// <summary>SYS-WALK-BOUNDS collision freezes are real preparation waits, never moving sample time.</summary>
    private sealed class RouteMovementClock(StartupSampleInput input, IBakeService bake, IStreamStatistics stream, Func<bool> ready)
    {
        private readonly double _started = FrameClock.MonotonicMilliseconds;
        private double _excludedMilliseconds;
        private double? _lastReadyAt;
        public double ElapsedMilliseconds => FrameClock.MonotonicMilliseconds - _started - _excludedMilliseconds;

        public Task WaitForMovementReady()
        {
            if (!ready()) return WaitForCollision();
            _lastReadyAt = FrameClock.MonotonicMilliseconds;
            return Task.CompletedTask;
        }

        private async Task WaitForCollision()
        {
            var pausedAt = _lastReadyAt ?? _started;
            var progress = Progress();
            StartupSampleInput.Trace("route-collision-wait-start");
            try
            {
                await input.Wait(ready, "The route's production walking collision stopped making preparation or mount progress.", () =>
                {
                    var next = Progress();
                    var changed = next != progress;
                    progress = next;
                    return changed;
                });
            }
            finally
            {
                var resumedAt = FrameClock.MonotonicMilliseconds;
                _excludedMilliseconds += resumedAt - pausedAt;
                _lastReadyAt = resumedAt;
                StartupSampleInput.Trace("route-collision-wait-end");
            }
        }

        private (BakeProgress, int, int, int, int, int, long, int) Progress()
        {
            var pending = stream.Statistics;
            return (bake.Progress, pending.PendingRequests, pending.PendingBakes, pending.PendingPreparations,
                pending.PendingMounts, pending.PendingRetirements, pending.MountedBytes, pending.MountedTiles);
        }
    }

    private static RoutePoint ResolveRoutePoint(string id, ViewMode mode, IMapContext context)
    {
        var slash = id.LastIndexOf('/');
        if (id.StartsWith("road:", StringComparison.Ordinal))
        {
            if (mode != ViewMode.Walk || slash < 0 || !int.TryParse(id[(slash + 1)..],
                System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var index))
                throw new InvalidOperationException("Invalid planar route point: " + id);
            var road = context.World.Network.Roads.SingleOrDefault(line => line.Id.Value == id[5..slash])
                ?? throw new InvalidOperationException("Unknown planar route line: " + id);
            if (index >= road.Line.Points.Count) throw new InvalidOperationException("Planar route point is outside its authored line: " + id);
            var point = road.Line.Points[index];
            return new(new(point.X, 0, point.Y), Planar: true);
        }
        if (!id.StartsWith("door:", StringComparison.Ordinal) || slash < 0)
        {
            var maps = KernelServices.Get<IMapService>();
            return new(mode switch
            {
                ViewMode.Fly => ReviewPoseResolver.Resolve(id, context).Position,
                ViewMode.Walk => maps.ResolveSpawn(maps.Current, id).Stand?.Feet
                    ?? throw new InvalidOperationException("Walking route point has no authored standing destination: " + id),
                _ => throw new InvalidOperationException("Route requested an unsupported view mode.")
            });
        }
        if (mode != ViewMode.Walk) throw new InvalidOperationException("Door endpoints require a walking route.");
        var marker = KernelServices.Get<IAtlasService>().Find(new(id[..slash]));
        var interior = marker?.Feature is { } feature ? context.World.Interiors.Get(feature) : null;
        var door = interior is { Open: true } ? interior.Entrances.SingleOrDefault(entry => entry.Id == id[5..slash] && entry.Open) : null;
        if (door is null) throw new InvalidOperationException("Unknown or closed authored route entrance: " + id);
        return new(id[(slash + 1)..] switch
        {
            "outside" => door.Outside,
            "inside" => door.Inside,
            _ => throw new InvalidOperationException("Unknown authored entrance endpoint: " + id)
        });
    }

    private static float PointDistance(RoutePoint target, N.Vector3 actual)
    {
        var delta = target.Position - actual;
        if (target.Planar) delta.Y = 0;
        return delta.Length();
    }

    private static bool AtPoint(RoutePoint target, IObserverState observer, ViewMode mode, IMapContext context)
    {
        if (PointDistance(target, observer.Feet) > WalkRules.Radius) return false;
        if (target.Planar)
        {
            // Only query terrain once the live controller has brought this point into its prepared collision region.
            var y = context.World.Terrain.HeightAt(target.Position.X, target.Position.Z);
            return observer.Space is null && MathF.Abs(observer.Feet.Y - y) <= WalkRules.Radius;
        }
        if (mode == ViewMode.Fly) return true;
        var medium = context.World.Spatial.MediumAt(target.Position + N.Vector3.UnitY * WalkRules.StandingEye);
        NavSpace? space = medium.Kind == MediumKind.Interior && medium.Feature is { } building
            ? new(building, (short)medium.Storey) : null;
        return observer.Space == space;
    }

    private sealed class RouteSteering(Key[] keys, bool[] held, N.Vector3 previous)
    {
        // The measurement driver reads the same fixed rules as the production camera; it never owns motion.
        private readonly Campus.Camera.FlightTuning _flight = new();
        private readonly double[] _pressedAt = Enumerable.Repeat(double.NegativeInfinity, keys.Length).ToArray();
        private double _elapsed;
        private float _previousSeconds;

        public void Steer(N.Vector3 target, N.Vector3 actual, float yaw, ViewMode mode, float frameSeconds)
        {
            var seconds = Math.Min(frameSeconds, _flight.MaxFrameStep);
            _elapsed += seconds;
            var velocity = _previousSeconds > 0 ? (actual - previous) / _previousSeconds : N.Vector3.Zero;
            previous = actual;
            _previousSeconds = seconds;
            var delta = target - actual;
            // Releasing a flight key leaves exponential coast. Account for that coast and the next input frame
            // before deciding another native edge, rather than alternating Space/Shift around an overshot height.
            if (mode == ViewMode.Fly) delta -= velocity * (1 / _flight.Deceleration + seconds);
            var forward = Angles.Forward(yaw, 0);
            var right = new N.Vector3(-forward.Z, 0, forward.X);
            var front = N.Vector3.Dot(delta, forward);
            var side = N.Vector3.Dot(delta, right);
            for (var index = 0; index < held.Length; index++)
            {
                var wanted = index switch
                {
                    0 => front > StartupRouteProgress.PositionTolerance,
                    1 => front < -StartupRouteProgress.PositionTolerance,
                    2 => side > StartupRouteProgress.PositionTolerance,
                    3 => side < -StartupRouteProgress.PositionTolerance,
                    4 => mode == ViewMode.Fly && delta.Y > StartupRouteProgress.PositionTolerance,
                    5 => mode == ViewMode.Fly && delta.Y < -StartupRouteProgress.PositionTolerance,
                    _ => throw new InvalidOperationException("Unknown native route action.")
                };
                // Forward/Space are also double-press gestures. Steering corrections must remain movement,
                // measured in the camera's capped simulation seconds, including during slow native frames.
                var gestureSeconds = index switch { 0 => _flight.DoublePressSeconds, 4 => _flight.DoubleSpaceSeconds, _ => 0 };
                if (wanted && !held[index] && gestureSeconds > 0 &&
                    _elapsed - _pressedAt[index] <= gestureSeconds + seconds) wanted = false;
                if (wanted == held[index]) continue;
                StartupSampleInput.SendKey(keys[index], wanted, true);
                held[index] = wanted;
                if (wanted) _pressedAt[index] = _elapsed;
            }
        }
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
            ViewMode.Walk => walk.Active && !walk.ExitRequested,
            ViewMode.Fly => !walk.Active,
            _ => throw new InvalidOperationException("Route requested an unsupported view mode.")
        };
        if (valid) return null;
        var position = observer.Feet;
        var pending = KernelServices.Get<IStreamStatistics>().Statistics;
        return FormattableString.Invariant($"Route lost the production controller for its requested mode: requested={mode} active={walk.Active} exitRequested={walk.ExitRequested} collisionReady={walk.CollisionReady} x={position.X:R} y={position.Y:R} z={position.Z:R} streamRevision={pending.Revision} pendingRequests={pending.PendingRequests} pendingBakes={pending.PendingBakes} pendingPreparations={pending.PendingPreparations} pendingMounts={pending.PendingMounts} pendingRetirements={pending.PendingRetirements}.");
    }

    private static void PrintPose(string phase, string start, IObserverState observer, double elapsed)
    {
        var p = observer.Feet;
        GD.Print(FormattableString.Invariant($"STARTUP_ROUTE_POSE phase={phase} start={start} mode={observer.Mode} time={FrameClock.MonotonicMilliseconds:R} elapsedMilliseconds={elapsed:R} x={p.X:R} y={p.Y:R} z={p.Z:R} yaw={observer.YawDeg:R}"));
    }
}
